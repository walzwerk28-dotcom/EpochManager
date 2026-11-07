using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EpochCity
{
    /// <summary>
    /// Zeigt das Wasser der Karte (Vorkommen mit DepositDefinition.isWater) als zusammenhängende Fläche statt einer
    /// Kachel je Feld: ein Mesh je Abschnitt von chunkSize × chunkSize Feldern, aus Marching Squares über die
    /// Feldmitten, damit die Ufer abgeschrägt und abgerundet statt treppig verlaufen (Außenecken werden abgeschnitten,
    /// Innenecken laufen ein Stück ins Land). Der Abstand zum Ufer steht in uv.x (0 am Ufer, 1 tief) und färbt das
    /// Wasser im Shader. Darunter liegt ein Sandbett, das als schmaler Streifen (shoreLevel) über die Wasserkante hinaus
    /// ins Land reicht. Am Kartenrand läuft das Wasser weit hinaus. Dazu Uferdeko (Schilf, Steine am Ufer, Seerosen im
    /// Wasser), die zufällig, aber fest an der Feldposition verteilt wird und unter Gebäuden und Straßen ausgeblendet ist.
    /// Wasser bleibt unter Brücken sichtbar. Wird bei Änderungen der Vorkommen und nach dem Laden neu aufgebaut, nur
    /// für Abschnitte, deren Wasser sich geändert hat.
    /// </summary>
    public class WaterVisuals : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [SerializeField] private Material waterMaterial;
        [Tooltip("Sandbett unter dem Wasser und Uferstreifen")]
        [SerializeField] private Material shoreMaterial;

        [Header("Höhen (Meter über dem Boden)")]
        [SerializeField] private float waterHeight = 0.04f;
        [SerializeField] private float shoreHeight = 0.01f;

        /// <summary>Höhe der Wasseroberfläche über dem Boden (für Schiffe).</summary>
        public float WaterHeight => waterHeight;

        [Header("Form")]
        [Tooltip("Ab diesem Feldwert (Wasser = 1, Land = 0) gilt der Sandstreifen; 0,3 = 0,4 m über die Wasserkante hinaus")]
        [Range(0.05f, 0.49f)] [SerializeField] private float shoreLevel = 0.3f;
        [Tooltip("Länge des Wassers über den Kartenrand hinaus (Meter)")]
        [SerializeField] private float edgeExtension = 120f;
        [Min(4)] [SerializeField] private int chunkSize = 16;

        [Header("Uferdeko")]
        [SerializeField] private GameObject[] reeds;
        [SerializeField] private GameObject[] shoreStones;
        [SerializeField] private GameObject[] lilies;
        [Range(0f, 1f)] [SerializeField] private float lilyChance = 0.24f;
        [Range(0f, 1f)] [SerializeField] private float stoneChance = 0.22f;

        private class Chunk
        {
            public GameObject Root;
            public ulong Signature;
        }

        private readonly Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();
        private readonly Dictionary<Vector2Int, DecorCell> decor = new Dictionary<Vector2Int, DecorCell>();
        private bool[,] water;
        private float[,] depth;
        private int width, height;
        private float cell;
        private Vector3 origin;
        private bool dirty = true;

        private struct DecorCell
        {
            public GameObject Root;
            public bool OnWater;
        }

        /// <summary>Anzahl der Wasserfelder (für Tests und Messungen).</summary>
        public int WaterCellCount { get; private set; }
        /// <summary>Anzahl der Dreiecke von Wasser und Sandbett (für Tests und Messungen).</summary>
        public int TriangleCount { get; private set; }
        /// <summary>Anzahl der Deko-Gruppen (für Tests und Messungen).</summary>
        public int DecorCount => decor.Count;

        private void OnEnable()
        {
            if (city == null) return;
            city.DepositsChanged += MarkDirty;
            city.ConnectionsChanged += MarkDirty; // Gebäude und Straßen verdecken die Uferdeko
            dirty = true;
        }

        private void OnDisable()
        {
            if (city == null) return;
            city.DepositsChanged -= MarkDirty;
            city.ConnectionsChanged -= MarkDirty;
        }

        private void MarkDirty() => dirty = true;

        private void LateUpdate()
        {
            if (!dirty || city == null || city.Grid == null) return;
            dirty = false;
            var grid = city.Grid;
            width = grid.Width;
            height = grid.Height;
            cell = grid.CellSize;
            origin = city.transform.position;

            bool changed = ReadWater(grid);
            if (changed)
            {
                RebuildChunks();
                RebuildDecor();
            }
            UpdateDecorVisibility(grid);
        }

        // ---------------------------------------------------------------- Wasserfelder

        /// <summary>Liest die Wasserfelder und die Ufer-Abstände; true, wenn sich etwas geändert hat.</summary>
        private bool ReadWater(GridMap grid)
        {
            var fresh = new bool[width, height];
            int count = 0;
            foreach (var c in grid.DepositCells)
            {
                var d = grid.GetDeposit(c);
                if (d == null || !d.isWater) continue;
                fresh[c.x, c.y] = true;
                count++;
            }

            bool changed = water == null || water.GetLength(0) != width || water.GetLength(1) != height;
            if (!changed)
                for (int x = 0; x < width && !changed; x++)
                    for (int y = 0; y < height; y++)
                        if (fresh[x, y] != water[x, y]) { changed = true; break; }
            if (!changed) return false;

            water = fresh;
            WaterCellCount = count;
            ComputeDepth();
            return true;
        }

        /// <summary>Abstand jedes Wasserfelds zum nächsten Landfeld (Chebyshev, in Feldern) als Wert 0..1: 0,35 neben dem Land, 1 ab drei Feldern.</summary>
        private void ComputeDepth()
        {
            var dist = new int[width, height];
            var queue = new Queue<Vector2Int>();
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (!water[x, y]) { dist[x, y] = 0; queue.Enqueue(new Vector2Int(x, y)); }
                    else dist[x, y] = int.MaxValue;
                }
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = c.x + dx, ny = c.y + dy;
                        if (nx < 0 || ny < 0 || nx >= width || ny >= height || dist[nx, ny] <= dist[c.x, c.y] + 1) continue;
                        dist[nx, ny] = dist[c.x, c.y] + 1;
                        queue.Enqueue(new Vector2Int(nx, ny));
                    }
            }
            depth = new float[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    depth[x, y] = water[x, y] ? Mathf.Clamp01(0.35f * Mathf.Min(dist[x, y], 100)) : 0f;
        }

        private bool IsWaterAt(int x, int y) =>
            water[Mathf.Clamp(x, 0, width - 1), Mathf.Clamp(y, 0, height - 1)];

        private float DepthAt(int x, int y)
        {
            x = Mathf.Clamp(x, 0, width - 1);
            y = Mathf.Clamp(y, 0, height - 1);
            return depth[x, y];
        }

        /// <summary>Feldmitte (x, z in Metern relativ zum Ursprung der Karte).</summary>
        private Vector2 SamplePos(int x, int y) => new Vector2((x + 0.5f) * cell, (y + 0.5f) * cell);

        // ---------------------------------------------------------------- Mesh

        private class MeshData
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Vector2> Uvs = new List<Vector2>();
            public readonly List<int> Triangles = new List<int>();

            public void AddFan(List<Vector3> poly, List<Vector2> uv)
            {
                int i = Vertices.Count;
                Vertices.AddRange(poly);
                Uvs.AddRange(uv);
                for (int k = 1; k < poly.Count - 1; k++)
                {
                    Triangles.Add(i); Triangles.Add(i + k); Triangles.Add(i + k + 1);
                }
            }
        }

        private void RebuildChunks()
        {
            // Abschnitte mit Wasser bestimmen (Quadrate von -1 bis Breite - 1, jeweils eine Reihe über den Rand hinaus)
            var wanted = new HashSet<Vector2Int>();
            for (int i = -1; i < width; i++)
                for (int j = -1; j < height; j++)
                    if (IsWaterAt(i, j) || IsWaterAt(i + 1, j) || IsWaterAt(i, j + 1) || IsWaterAt(i + 1, j + 1))
                        wanted.Add(ChunkOf(i, j));

            var stale = new List<Vector2Int>();
            foreach (var key in chunks.Keys)
                if (!wanted.Contains(key)) stale.Add(key);
            foreach (var key in stale)
            {
                Destroy(chunks[key].Root);
                chunks.Remove(key);
            }

            int triangles = 0;
            foreach (var key in wanted)
            {
                ulong signature = Signature(key);
                if (chunks.TryGetValue(key, out var existing) && existing.Signature == signature)
                {
                    triangles += CountTriangles(existing.Root);
                    continue;
                }
                if (existing != null) Destroy(existing.Root);
                var chunk = BuildChunk(key);
                chunk.Signature = signature;
                chunks[key] = chunk;
                triangles += CountTriangles(chunk.Root);
            }
            TriangleCount = triangles;
        }

        private static int CountTriangles(GameObject root)
        {
            int n = 0;
            foreach (var f in root.GetComponentsInChildren<MeshFilter>())
                if (f.sharedMesh != null) n += (int)(f.sharedMesh.GetIndexCount(0) / 3);
            return n;
        }

        private Vector2Int ChunkOf(int squareX, int squareY) =>
            new Vector2Int(Mathf.FloorToInt((squareX + 1f) / chunkSize), Mathf.FloorToInt((squareY + 1f) / chunkSize));

        /// <summary>Prüfsumme der Wasserfelder eines Abschnitts samt einer Reihe Rand, um unveränderte zu überspringen.</summary>
        private ulong Signature(Vector2Int key)
        {
            ulong h = 1469598103934665603UL;
            int x0 = key.x * chunkSize - 2, y0 = key.y * chunkSize - 2;
            for (int x = x0; x < x0 + chunkSize + 4; x++)
                for (int y = y0; y < y0 + chunkSize + 4; y++)
                {
                    h ^= IsWaterAt(x, y) ? 1UL : 0UL;
                    h *= 1099511628211UL;
                }
            return h;
        }

        private Chunk BuildChunk(Vector2Int key)
        {
            var waterMesh = new MeshData();
            var bedMesh = new MeshData();
            int i0 = key.x * chunkSize - 1, j0 = key.y * chunkSize - 1;
            for (int i = i0; i < i0 + chunkSize; i++)
                for (int j = j0; j < j0 + chunkSize; j++)
                {
                    if (i < -1 || j < -1 || i >= width || j >= height) continue;
                    AddSquare(i, j, 0.5f, waterHeight, true, waterMesh);
                    AddSquare(i, j, shoreLevel, shoreHeight, false, bedMesh);
                    AddEdgeExtension(i, j, waterMesh, bedMesh);
                }

            var root = new GameObject($"Wasser_{key.x}_{key.y}");
            root.transform.SetParent(transform, false);
            root.transform.position = origin;
            if (waterMesh.Triangles.Count > 0)
                AddRenderer(root, "Wasser", ToMesh(waterMesh, $"Wasser_{key.x}_{key.y}"), waterMaterial, true);
            if (bedMesh.Triangles.Count > 0)
                AddRenderer(root, "Ufer", ToMesh(bedMesh, $"Ufer_{key.x}_{key.y}"), shoreMaterial, false);
            return new Chunk { Root = root };
        }

        private static Mesh ToMesh(MeshData data, string meshName)
        {
            var mesh = new Mesh { name = meshName };
            if (data.Vertices.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(data.Vertices);
            mesh.SetUVs(0, data.Uvs);
            mesh.SetTriangles(data.Triangles, 0);
            var normals = new Vector3[data.Vertices.Count];
            for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.up;
            mesh.normals = normals;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddRenderer(GameObject root, string objectName, Mesh mesh, Material material, bool transparent)
        {
            var go = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(root.transform, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = !transparent;
        }

        /// <summary>
        /// Marching Squares für das Quadrat zwischen den Feldmitten (i, j) und (i + 1, j + 1): Fläche, auf der der
        /// Feldwert (Wasser 1, Land 0) mindestens level beträgt; die Wasserkante schneidet die Kanten bei level,
        /// bei 0,5 genau auf der Feldgrenze.
        /// </summary>
        private void AddSquare(int i, int j, float level, float y, bool withDepth, MeshData mesh)
        {
            // Ecken im Uhrzeigersinn von oben: SW, NW, NO, SO
            int[] cx = { i, i, i + 1, i + 1 };
            int[] cy = { j, j + 1, j + 1, j };
            bool any = false;
            var inside = new bool[4];
            for (int k = 0; k < 4; k++)
            {
                inside[k] = IsWaterAt(cx[k], cy[k]);
                any |= inside[k];
            }
            if (!any) return;

            var poly = new List<Vector3>(6);
            var uvs = new List<Vector2>(6);
            for (int k = 0; k < 4; k++)
            {
                int n = (k + 1) % 4;
                var a = SamplePos(cx[k], cy[k]);
                var b = SamplePos(cx[n], cy[n]);
                float da = withDepth ? DepthAt(cx[k], cy[k]) : 0f;
                float db = withDepth ? DepthAt(cx[n], cy[n]) : 0f;
                if (inside[k])
                {
                    poly.Add(new Vector3(a.x, y, a.y));
                    uvs.Add(new Vector2(da, 0f));
                }
                if (inside[k] != inside[n])
                {
                    // Feldwert springt zwischen 1 (Wasser) und 0 (Land): Kante bei level, von Land aus gerechnet
                    float t = inside[k] ? (1f - level) : level;
                    var p = Vector2.Lerp(a, b, t);
                    poly.Add(new Vector3(p.x, y, p.y));
                    uvs.Add(new Vector2(withDepth ? 0f : Mathf.Lerp(da, db, t), 0f)); // die Wasserkante selbst hat Tiefe 0 (Schaum)
                }
            }
            if (poly.Count >= 3) mesh.AddFan(poly, uvs);
        }

        /// <summary>
        /// Wasser und Bett laufen am Kartenrand weit hinaus, damit das Ende der Fläche nicht sichtbar wird. Der
        /// Streifen setzt die Randfelder fort (gleiche Tiefe, Ufer endet an derselben Stelle) und an den Ecken der Karte
        /// folgt ein großes Viereck.
        /// </summary>
        private void AddEdgeExtension(int i, int j, MeshData waterMesh, MeshData bedMesh)
        {
            // nur Quadrate in der äußersten Reihe (zwischen dem virtuellen Feld außerhalb und dem Randfeld)
            if (i == -1) AddEdgeStrip(i, j, i, j + 1, new Vector2(-1f, 0f), waterMesh, bedMesh);
            if (i == width - 1) AddEdgeStrip(i + 1, j, i + 1, j + 1, new Vector2(1f, 0f), waterMesh, bedMesh);
            if (j == -1) AddEdgeStrip(i, j, i + 1, j, new Vector2(0f, -1f), waterMesh, bedMesh);
            if (j == height - 1) AddEdgeStrip(i, j + 1, i + 1, j + 1, new Vector2(0f, 1f), waterMesh, bedMesh);

            // Ecken: das Viereck hinter dem Eckfeld, wenn es Wasser ist
            if (i == -1 && j == -1) AddCorner(i, j, new Vector2(-1f, -1f), waterMesh, bedMesh);
            if (i == width - 1 && j == -1) AddCorner(i + 1, j, new Vector2(1f, -1f), waterMesh, bedMesh);
            if (i == -1 && j == height - 1) AddCorner(i, j + 1, new Vector2(-1f, 1f), waterMesh, bedMesh);
            if (i == width - 1 && j == height - 1) AddCorner(i + 1, j + 1, new Vector2(1f, 1f), waterMesh, bedMesh);
        }

        private void AddCorner(int x, int y, Vector2 outward, MeshData waterMesh, MeshData bedMesh)
        {
            if (!IsWaterAt(x, y)) return;
            var p = SamplePos(x, y);
            var u = new Vector2(outward.x * edgeExtension, 0f);
            var v = new Vector2(0f, outward.y * edgeExtension);
            float d = DepthAt(x, y);
            AddQuad(waterMesh, p, p + u, p + u + v, p + v, waterHeight, d, d, d, d);
            AddQuad(bedMesh, p, p + u, p + u + v, p + v, shoreHeight, 0f, 0f, 0f, 0f);
        }

        /// <summary>
        /// Streifen von der Randlinie zwischen den Feldmitten (ax, ay) und (bx, by) nach außen; liegt nur eine Mitte im
        /// Wasser, reicht er bis zur Wasserkante (wie die Marching Squares innen).
        /// </summary>
        private void AddEdgeStrip(int ax, int ay, int bx, int by, Vector2 outward, MeshData waterMesh, MeshData bedMesh)
        {
            bool ia = IsWaterAt(ax, ay), ib = IsWaterAt(bx, by);
            if (!ia && !ib) return;
            var a = SamplePos(ax, ay);
            var b = SamplePos(bx, by);
            AddStrip(waterMesh, a, b, outward, ia, ib, 0.5f, waterHeight, DepthAt(ax, ay), DepthAt(bx, by), true);
            AddStrip(bedMesh, a, b, outward, ia, ib, shoreLevel, shoreHeight, 0f, 0f, false);
        }

        private void AddStrip(MeshData mesh, Vector2 a, Vector2 b, Vector2 outward, bool ia, bool ib, float level, float y,
            float da, float db, bool withDepth)
        {
            float t0 = 0f, t1 = 1f;
            float d0 = da, d1 = db;
            if (ia && !ib) { t1 = 1f - level; d1 = 0f; }
            else if (!ia && ib) { t0 = level; d0 = 0f; }
            var p0 = Vector2.Lerp(a, b, t0);
            var p1 = Vector2.Lerp(a, b, t1);
            AddQuad(mesh, p0, p1, p1 + outward * edgeExtension, p0 + outward * edgeExtension, y, d0, d1, d1, d0);
        }

        /// <summary>Viereck a–b–c–d mit der Tiefe je Ecke; die Reihenfolge wird so gewählt, dass die Fläche nach oben zeigt.</summary>
        private static void AddQuad(MeshData mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, float y,
            float da, float db, float dc, float dd)
        {
            var poly = new List<Vector3>
            {
                new Vector3(a.x, y, a.y), new Vector3(b.x, y, b.y), new Vector3(c.x, y, c.y), new Vector3(d.x, y, d.y)
            };
            var uvs = new List<Vector2> { new Vector2(da, 0f), new Vector2(db, 0f), new Vector2(dc, 0f), new Vector2(dd, 0f) };
            var cross = Vector3.Cross(poly[1] - poly[0], poly[2] - poly[1]);
            if (cross.y < 0f) // gegen den Uhrzeigersinn → umdrehen (im Uhrzeigersinn von oben zeigt die Fläche nach oben)
            {
                poly.Reverse();
                uvs.Reverse();
            }
            mesh.AddFan(poly, uvs);
        }

        // ---------------------------------------------------------------- Uferdeko

        private static System.Random CellRandom(Vector2Int c) =>
            new System.Random((c.x * 73856093) ^ (c.y * 19349663) ^ 0x2f6e2b1);

        private static float Range(System.Random random, float min, float max) =>
            min + (float)random.NextDouble() * (max - min);

        private static readonly Vector2Int[] Orthogonal =
            { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
        private static readonly Vector2Int[] Diagonal =
            { new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1) };

        private bool Water(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < width && c.y < height && water[c.x, c.y];

        private void RebuildDecor()
        {
            foreach (var d in decor.Values)
                if (d.Root != null) Destroy(d.Root);
            decor.Clear();

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    var c = new Vector2Int(x, y);
                    if (water[x, y]) BuildWaterDecor(c);
                    else BuildShoreDecor(c);
                }
        }

        private Vector3 CenterOf(Vector2Int c) => origin + new Vector3((c.x + 0.5f) * cell, 0f, (c.y + 0.5f) * cell);

        private GameObject Place(GameObject[] prefabs, GameObject root, Vector3 position, System.Random random,
            float scaleMin, float scaleMax)
        {
            if (prefabs == null || prefabs.Length == 0) return null;
            var prefab = prefabs[random.Next(prefabs.Length)];
            if (prefab == null) return null;
            var go = Instantiate(prefab, position, Quaternion.Euler(0f, Range(random, 0f, 360f), 0f), root.transform);
            go.transform.localScale *= Range(random, scaleMin, scaleMax);
            return go;
        }

        private GameObject NewRoot(Vector2Int c, bool onWater)
        {
            var root = new GameObject($"Ufer_{c.x}_{c.y}");
            root.transform.SetParent(transform, false);
            root.transform.position = CenterOf(c);
            decor[c] = new DecorCell { Root = root, OnWater = onWater };
            return root;
        }

        private void BuildShoreDecor(Vector2Int c)
        {
            bool orthogonalWater = false;
            foreach (var d in Orthogonal) orthogonalWater |= Water(c + d);
            bool diagonalWater = false;
            foreach (var d in Diagonal) diagonalWater |= Water(c + d);
            if (!orthogonalWater && !diagonalWater) return;

            var random = CellRandom(c);
            GameObject root = null;
            var center = CenterOf(c);
            float half = cell * 0.5f;
            foreach (var d in Orthogonal)
            {
                if (!Water(c + d)) continue;
                var dir = new Vector3(d.x, 0f, d.y);
                var perp = new Vector3(-d.y, 0f, d.x);
                int clumps = random.NextDouble() < 0.15 ? 0 : random.NextDouble() < 0.5 ? 1 : 2;
                for (int k = 0; k < clumps; k++)
                {
                    if (root == null) root = NewRoot(c, false);
                    var p = center + dir * Range(random, half * 0.72f, half * 1.02f) + perp * Range(random, -half * 0.85f, half * 0.85f);
                    p.y = shoreHeight;
                    Place(reeds, root, p, random, 0.9f, 1.4f);
                }
                if (random.NextDouble() < stoneChance)
                {
                    if (root == null) root = NewRoot(c, false);
                    var p = center + dir * Range(random, half * 0.7f, half * 0.95f) + perp * Range(random, -half * 0.8f, half * 0.8f);
                    p.y = shoreHeight;
                    Place(shoreStones, root, p, random, 0.8f, 1.3f);
                }
            }
            if (!orthogonalWater && random.NextDouble() < 0.6)
            {
                foreach (var d in Diagonal)
                {
                    if (!Water(c + d)) continue;
                    if (root == null) root = NewRoot(c, false);
                    var p = center + new Vector3(d.x, 0f, d.y) * Range(random, half * 0.7f, half * 0.95f);
                    p.y = shoreHeight;
                    Place(reeds, root, p, random, 0.8f, 1.2f);
                    break;
                }
            }
        }

        private void BuildWaterDecor(Vector2Int c)
        {
            var random = CellRandom(c);
            var center = CenterOf(c);
            float half = cell * 0.5f;
            GameObject root = null;

            // Schilf im flachen Wasser am Ufer
            foreach (var d in Orthogonal)
            {
                if (Water(c + d) || !InMap(c + d)) continue;
                if (random.NextDouble() > 0.3) continue;
                var toLand = new Vector3(d.x, 0f, d.y);
                var perp = new Vector3(-d.y, 0f, d.x);
                if (root == null) root = NewRoot(c, true);
                var p = center + toLand * Range(random, half * 0.4f, half * 0.9f) + perp * Range(random, -half * 0.8f, half * 0.8f);
                p.y = waterHeight;
                Place(reeds, root, p, random, 0.85f, 1.25f);
            }

            // Seerosen im ruhigen Wasser, seltener direkt am Ufer
            if (depth[c.x, c.y] >= 0.3f && random.NextDouble() < lilyChance * (depth[c.x, c.y] >= 0.7f ? 1f : 0.4f))
            {
                if (root == null) root = NewRoot(c, true);
                var p = center + new Vector3(Range(random, -half * 0.7f, half * 0.7f), 0f, Range(random, -half * 0.7f, half * 0.7f));
                p.y = waterHeight + 0.004f;
                Place(lilies, root, p, random, 0.9f, 1.3f);
            }
        }

        private bool InMap(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < width && c.y < height;

        /// <summary>Uferdeko auf Land verschwindet unter Gebäuden und Straßen, Seerosen und Schilf im Wasser unter Brücken.</summary>
        private void UpdateDecorVisibility(GridMap grid)
        {
            foreach (var entry in decor)
            {
                if (entry.Value.Root == null) continue;
                bool blocked = entry.Value.OnWater
                    ? grid.HasRoad(entry.Key)
                    : grid.GetAt(entry.Key) != null || grid.HasRoad(entry.Key);
                if (entry.Value.Root.activeSelf == blocked) entry.Value.Root.SetActive(!blocked);
            }
        }
    }
}
