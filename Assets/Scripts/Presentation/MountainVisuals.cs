using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EpochCity
{
    /// <summary>
    /// Zeigt die Berge der Karte (Vorkommen mit DepositDefinition.isMountain) als Höhenrelief statt einer Kachel je
    /// Feld. Ein Gitter mit zwei Eckpunkten je Feld (am Kartenrand wachsen die Abstände nach außen, damit das
    /// Gebirge weit über die Karte hinausläuft, ohne viele Dreiecke) liefert die Höhe: an der Außenkante der Bergfelder
    /// 0, damit die Hänge bündig in die Wiese übergehen, nach innen steigt sie mit dem Abstand zum Rand (Distanzfeld)
    /// plus Rauschen. Die Eckpunkte im Inneren sind leicht gegeneinander versetzt, Schattierung und Farbe je
    /// Dreieck (Low-Poly): unten Gras, darüber Buschland in Oliv und Ocker, steil und oben Kalkfels. Abschnitte von
    /// chunkSize × chunkSize Feldern, nur geänderte werden neu gebaut. Dazu Deko (Pinien, Zypressen, Macchia, Felsen),
    /// fest an der Position verteilt und auf die Höhe des Reliefs gesetzt. GetHeight liefert die Höhe für die Kamera.
    /// </summary>
    public class MountainVisuals : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [SerializeField] private Material mountainMaterial;

        [Header("Höhe (Meter)")]
        [Tooltip("Höchste Höhe innerhalb der Karte")]
        [SerializeField] private float maxHeight = 14f;
        [Tooltip("Höchste Höhe weit außerhalb der Karte")]
        [SerializeField] private float outerMaxHeight = 24f;
        [Tooltip("Ab dieser Entfernung vom Kartenrand (Meter) sinkt das Gebirge wieder auf 0")]
        [SerializeField] private float fadeStart = 60f;
        [Range(0f, 0.45f)] [SerializeField] private float jitter = 0.3f;
        [Min(4)] [SerializeField] private int chunkSize = 16;

        [Header("Deko")]
        [SerializeField] private GameObject[] pines;
        [SerializeField] private GameObject[] cypresses;
        [SerializeField] private GameObject[] bushes;
        [SerializeField] private GameObject[] rocks;
        [Tooltip("Anteil der Deko-Dichte außerhalb der Karte")]
        [Range(0f, 1f)] [SerializeField] private float outerDecorFactor = 0.2f;

        // Abstände der Gitterlinien jenseits des Kartenrands (Meter), ihre Summe ist die Länge des Gebirges außerhalb
        private static readonly float[] ExtensionSteps = { 1, 1, 2, 2, 4, 4, 8, 8, 16, 16, 24, 24 };
        private const int Sub = 2;

        // Farben (sRGB, im Code in linear umgerechnet)
        private static readonly Color Grass = new Color(0.37f, 0.52f, 0.27f);
        private static readonly Color Olive = new Color(0.44f, 0.51f, 0.27f);
        private static readonly Color Ochre = new Color(0.60f, 0.55f, 0.33f);
        private static readonly Color RockWarm = new Color(0.74f, 0.69f, 0.58f);
        private static readonly Color RockGrey = new Color(0.62f, 0.59f, 0.54f);
        private static readonly Color RockDark = new Color(0.50f, 0.46f, 0.41f);

        private class Chunk
        {
            public GameObject Root;
            public ulong Signature;
            public int Triangles;
            public int Decor;
        }

        private readonly Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();
        private Dictionary<Vector2Int, List<Vector2Int>> chunkQuads = new Dictionary<Vector2Int, List<Vector2Int>>();
        private bool[,] mask;
        private float[] xs, zs;
        private float[,] px, pz, ph;      // Eckpunkte nach dem Versatz und Höhe
        private bool[,] mountainQuad;     // Quad (i, j) liegt in einem Bergfeld
        private bool[,] flip;             // Diagonale des Quads
        private int width, height;
        private float cell;
        private Vector3 origin;
        private bool dirty = true;
        private float mapW, mapH;

        /// <summary>Anzahl der Bergfelder (für Tests).</summary>
        public int MountainCellCount { get; private set; }
        /// <summary>Anzahl der Dreiecke des Reliefs (für Tests und Messungen).</summary>
        public int TriangleCount { get; private set; }
        /// <summary>Anzahl der Deko-Objekte (für Tests und Messungen).</summary>
        public int DecorCount { get; private set; }
        /// <summary>Anzahl der Abschnitte (für Tests).</summary>
        public int ChunkCount => chunks.Count;
        /// <summary>True, sobald ein Relief berechnet ist.</summary>
        public bool Ready => ph != null && MountainCellCount > 0;

        private void OnEnable()
        {
            if (city == null) return;
            city.DepositsChanged += MarkDirty;
            dirty = true;
        }

        private void OnDisable()
        {
            if (city == null) return;
            city.DepositsChanged -= MarkDirty;
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
            mapW = width * cell;
            mapH = height * cell;

            if (!ReadMask(grid)) return;
            ComputeField();
            RebuildChunks();
        }

        // ---------------------------------------------------------------- Bergfelder

        private bool ReadMask(GridMap grid)
        {
            var fresh = new bool[width, height];
            int count = 0;
            foreach (var c in grid.DepositCells)
            {
                var d = grid.GetDeposit(c);
                if (d == null || !d.isMountain) continue;
                fresh[c.x, c.y] = true;
                count++;
            }
            bool changed = mask == null || mask.GetLength(0) != width || mask.GetLength(1) != height;
            if (!changed)
                for (int x = 0; x < width && !changed; x++)
                    for (int y = 0; y < height; y++)
                        if (fresh[x, y] != mask[x, y]) { changed = true; break; }
            if (!changed) return false;
            mask = fresh;
            MountainCellCount = count;
            return true;
        }

        private bool IsMountainCell(int x, int y) =>
            mask[Mathf.Clamp(x, 0, width - 1), Mathf.Clamp(y, 0, height - 1)];

        // ---------------------------------------------------------------- Höhenfeld

        private float[] BuildLines(int cells)
        {
            var list = new List<float>();
            float total = 0f;
            var cum = new float[ExtensionSteps.Length];
            for (int k = 0; k < ExtensionSteps.Length; k++) { total += ExtensionSteps[k]; cum[k] = total; }
            for (int k = cum.Length - 1; k >= 0; k--) list.Add(-cum[k]);
            int n = cells * Sub;
            for (int k = 0; k <= n; k++) list.Add(k * cell / Sub);
            for (int k = 0; k < cum.Length; k++) list.Add(cells * cell + cum[k]);
            return list.ToArray();
        }

        private static uint Hash(int a, int b, int salt)
        {
            unchecked
            {
                uint h = (uint)(a * 73856093) ^ (uint)(b * 19349663) ^ (uint)(salt * 83492791);
                h ^= h >> 13; h *= 1274126177u; h ^= h >> 16;
                return h;
            }
        }

        /// <summary>Weicher Übergang von 0 bei a nach 1 bei b (anders als Mathf.SmoothStep, das zwischen zwei Werten mischt).</summary>
        private static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        private static float Hash01(int a, int b, int salt) => (Hash(a, b, salt) & 0xFFFFFF) / (float)0x1000000;

        private static float ValueNoise(float x, float z, int salt)
        {
            int ix = Mathf.FloorToInt(x), iz = Mathf.FloorToInt(z);
            float fx = x - ix, fz = z - iz;
            fx = fx * fx * (3f - 2f * fx);
            fz = fz * fz * (3f - 2f * fz);
            float a = Hash01(ix, iz, salt), b = Hash01(ix + 1, iz, salt);
            float c = Hash01(ix, iz + 1, salt), d = Hash01(ix + 1, iz + 1, salt);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fz);
        }

        private void ComputeField()
        {
            xs = BuildLines(width);
            zs = BuildLines(height);
            int nx = xs.Length, nz = zs.Length;

            mountainQuad = new bool[nx - 1, nz - 1];
            flip = new bool[nx - 1, nz - 1];
            for (int i = 0; i < nx - 1; i++)
                for (int j = 0; j < nz - 1; j++)
                {
                    mountainQuad[i, j] = IsMountainCell(QuadCell(xs[i], xs[i + 1], width), QuadCell(zs[j], zs[j + 1], height));
                    flip[i, j] = (Hash(i, j, 7) & 1) == 1;
                }

            // Eckpunkte im Inneren (alle angrenzenden Quads sind Berg) steigen, alle anderen bleiben bei 0
            var interior = new bool[nx, nz];
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    bool all = true;
                    for (int di = -1; di <= 0 && all; di++)
                        for (int dj = -1; dj <= 0; dj++)
                        {
                            int qi = i + di, qj = j + dj;
                            if (qi < 0 || qj < 0 || qi >= nx - 1 || qj >= nz - 1) continue;
                            if (!mountainQuad[qi, qj]) { all = false; break; }
                        }
                    interior[i, j] = all;
                }

            // Abstand zum Rand der Bergfelder in Metern (Chamfer-Verfahren über das Gitter, zwei Durchläufe)
            var dist = new float[nx, nz];
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                    dist[i, j] = interior[i, j] ? 1e6f : 0f;
            for (int iteration = 0; iteration < 2; iteration++)
            {
                for (int j = 0; j < nz; j++)
                    for (int i = 0; i < nx; i++)
                        Relax(dist, i, j, nx, nz, -1, 0, -1, -1, 0, -1, 1, -1);
                for (int j = nz - 1; j >= 0; j--)
                    for (int i = nx - 1; i >= 0; i--)
                        Relax(dist, i, j, nx, nz, 1, 0, 1, 1, 0, 1, -1, 1);
            }

            px = new float[nx, nz];
            pz = new float[nx, nz];
            ph = new float[nx, nz];
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    float x = xs[i], z = zs[j];
                    px[i, j] = x;
                    pz[i, j] = z;
                    if (!interior[i, j]) continue;

                    float outside = Mathf.Max(Mathf.Max(-x, x - mapW), Mathf.Max(-z, z - mapH));
                    outside = Mathf.Max(0f, outside);
                    float d = Mathf.Min(dist[i, j], 60f);

                    float baseH = 0.5f * d + 0.1f * Mathf.Pow(d, 1.5f);
                    float broad = 0.7f + 0.75f * ValueNoise(x / 18f, z / 18f, 11);
                    float detail = (ValueNoise(x / 9f, z / 9f, 23) - 0.5f) * 2.2f
                                   + (ValueNoise(x / 3.5f, z / 3.5f, 37) - 0.5f) * 0.7f
                                   + (ValueNoise(x / 40f, z / 40f, 71) - 0.5f) * 7f * Mathf.SmoothStep(0f, 1f, d / 8f);
                    float h = baseH * broad + detail * Mathf.SmoothStep(0f, 1f, d / 3f);
                    h *= 1f + 0.6f * Mathf.Clamp01(outside / 80f);
                    float cap = Mathf.Lerp(maxHeight, outerMaxHeight, Mathf.Clamp01(outside / 40f));
                    h = cap * (float)Math.Tanh(Mathf.Max(0f, h) / cap);
                    float fade = 1f - Mathf.SmoothStep(0f, 1f, (outside - fadeStart) / Mathf.Max(1f, ExtensionTotal - fadeStart));
                    ph[i, j] = Mathf.Max(0f, h * fade);

                    // Versatz der Eckpunkte, höchstens ein Drittel des kleinsten Nachbarabstands
                    float gapX = Mathf.Min(i > 0 ? xs[i] - xs[i - 1] : 99f, i < nx - 1 ? xs[i + 1] - xs[i] : 99f);
                    float gapZ = Mathf.Min(j > 0 ? zs[j] - zs[j - 1] : 99f, j < nz - 1 ? zs[j + 1] - zs[j] : 99f);
                    px[i, j] += (Hash01(i, j, 3) - 0.5f) * 2f * jitter * gapX;
                    pz[i, j] += (Hash01(i, j, 5) - 0.5f) * 2f * jitter * gapZ;
                }
        }

        private float ExtensionTotal
        {
            get
            {
                float t = 0f;
                foreach (var s in ExtensionSteps) t += s;
                return t;
            }
        }

        private int QuadCell(float a, float b, int cells) =>
            Mathf.Clamp(Mathf.FloorToInt((a + b) * 0.5f / cell), 0, cells - 1);

        private void Relax(float[,] dist, int i, int j, int nx, int nz, int ax, int ay, int bx, int by, int cx, int cy, int dx, int dy)
        {
            float best = dist[i, j];
            if (best <= 0f) return;
            best = Try(dist, i, j, i + ax, j + ay, nx, nz, best);
            best = Try(dist, i, j, i + bx, j + by, nx, nz, best);
            best = Try(dist, i, j, i + cx, j + cy, nx, nz, best);
            best = Try(dist, i, j, i + dx, j + dy, nx, nz, best);
            dist[i, j] = best;
        }

        private float Try(float[,] dist, int i, int j, int ni, int nj, int nx, int nz, float best)
        {
            if (ni < 0 || nj < 0 || ni >= nx || nj >= nz) return best;
            float dxm = xs[ni] - xs[i], dzm = zs[nj] - zs[j];
            float cand = dist[ni, nj] + Mathf.Sqrt(dxm * dxm + dzm * dzm);
            return cand < best ? cand : best;
        }

        // ---------------------------------------------------------------- Höhe abfragen

        /// <summary>Höhe des Reliefs an einer Weltposition (Meter über dem Boden); 0 ohne Berg.</summary>
        public float GetHeight(Vector3 world)
        {
            if (!Ready) return 0f;
            var local = world - origin;
            return TrySample(local.x, local.z, out float h) ? h : 0f;
        }

        /// <summary>Höchste Höhe im Umkreis (Meter) um eine Weltposition, für die Kamera.</summary>
        public float GetMaxHeight(Vector3 world, float radius)
        {
            if (!Ready) return 0f;
            float best = GetHeight(world);
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI / 4f;
                best = Mathf.Max(best, GetHeight(world + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius));
            }
            return best;
        }

        private static int FindLine(float[] lines, float v)
        {
            int idx = Array.BinarySearch(lines, v);
            if (idx < 0) idx = ~idx - 1;
            return Mathf.Clamp(idx, 0, lines.Length - 2);
        }

        /// <summary>Höhe auf dem tatsächlichen Dreiecksnetz (mit Versatz) an einer Position relativ zum Ursprung der Karte.</summary>
        private bool TrySample(float x, float z, out float h)
        {
            h = 0f;
            if (x < xs[0] || z < zs[0] || x > xs[xs.Length - 1] || z > zs[zs.Length - 1]) return false;
            int i0 = FindLine(xs, x), j0 = FindLine(zs, z);
            for (int di = -1; di <= 1; di++)
                for (int dj = -1; dj <= 1; dj++)
                {
                    int i = i0 + di, j = j0 + dj;
                    if (i < 0 || j < 0 || i >= xs.Length - 1 || j >= zs.Length - 1) continue;
                    GetTriangles(i, j, out var t1, out var t2);
                    if (InTriangle(x, z, t1, out h)) return true;
                    if (InTriangle(x, z, t2, out h)) return true;
                }
            return false;
        }

        private void GetTriangles(int i, int j, out Vector3Int[] t1, out Vector3Int[] t2)
        {
            // Eckpunkte a (i, j), b (i + 1, j), c (i + 1, j + 1), d (i, j + 1); Dreiecke im Uhrzeigersinn von oben
            var a = new Vector3Int(i, j, 0);
            var b = new Vector3Int(i + 1, j, 0);
            var c = new Vector3Int(i + 1, j + 1, 0);
            var d = new Vector3Int(i, j + 1, 0);
            if (!flip[i, j]) { t1 = new[] { a, d, c }; t2 = new[] { a, c, b }; }
            else { t1 = new[] { a, d, b }; t2 = new[] { b, d, c }; }
        }

        private bool InTriangle(float x, float z, Vector3Int[] t, out float h)
        {
            h = 0f;
            var p0 = new Vector2(px[t[0].x, t[0].y], pz[t[0].x, t[0].y]);
            var p1 = new Vector2(px[t[1].x, t[1].y], pz[t[1].x, t[1].y]);
            var p2 = new Vector2(px[t[2].x, t[2].y], pz[t[2].x, t[2].y]);
            float denom = (p1.y - p2.y) * (p0.x - p2.x) + (p2.x - p1.x) * (p0.y - p2.y);
            if (Mathf.Abs(denom) < 1e-8f) return false;
            float w0 = ((p1.y - p2.y) * (x - p2.x) + (p2.x - p1.x) * (z - p2.y)) / denom;
            float w1 = ((p2.y - p0.y) * (x - p2.x) + (p0.x - p2.x) * (z - p2.y)) / denom;
            float w2 = 1f - w0 - w1;
            const float eps = -1e-4f;
            if (w0 < eps || w1 < eps || w2 < eps) return false;
            h = w0 * ph[t[0].x, t[0].y] + w1 * ph[t[1].x, t[1].y] + w2 * ph[t[2].x, t[2].y];
            return true;
        }

        // ---------------------------------------------------------------- Abschnitte

        private Vector2Int ChunkOfQuad(int i, int j) =>
            new Vector2Int(QuadCell(xs[i], xs[i + 1], width) / chunkSize, QuadCell(zs[j], zs[j + 1], height) / chunkSize);

        private void RebuildChunks()
        {
            chunkQuads = new Dictionary<Vector2Int, List<Vector2Int>>();
            for (int i = 0; i < xs.Length - 1; i++)
                for (int j = 0; j < zs.Length - 1; j++)
                {
                    if (!mountainQuad[i, j]) continue;
                    var key = ChunkOfQuad(i, j);
                    if (!chunkQuads.TryGetValue(key, out var list)) chunkQuads[key] = list = new List<Vector2Int>();
                    list.Add(new Vector2Int(i, j));
                }

            var stale = new List<Vector2Int>();
            foreach (var key in chunks.Keys)
                if (!chunkQuads.ContainsKey(key)) stale.Add(key);
            foreach (var key in stale)
            {
                Destroy(chunks[key].Root);
                chunks.Remove(key);
            }

            foreach (var entry in chunkQuads)
            {
                ulong signature = Signature(entry.Value);
                if (chunks.TryGetValue(entry.Key, out var existing))
                {
                    if (existing.Signature == signature) continue;
                    Destroy(existing.Root);
                }
                var chunk = BuildChunk(entry.Key, entry.Value);
                chunk.Signature = signature;
                chunks[entry.Key] = chunk;
            }

            int triangles = 0, decor = 0;
            foreach (var c in chunks.Values) { triangles += c.Triangles; decor += c.Decor; }
            TriangleCount = triangles;
            DecorCount = decor;
        }

        private ulong Signature(List<Vector2Int> quads)
        {
            ulong h = 1469598103934665603UL;
            foreach (var q in quads)
                for (int di = 0; di <= 1; di++)
                    for (int dj = 0; dj <= 1; dj++)
                    {
                        h ^= (ulong)(long)Mathf.RoundToInt(ph[q.x + di, q.y + dj] * 100f) + (ulong)(q.x * 977 + q.y);
                        h *= 1099511628211UL;
                    }
            return h;
        }

        private Chunk BuildChunk(Vector2Int key, List<Vector2Int> quads)
        {
            var verts = new List<Vector3>(quads.Count * 6);
            var normals = new List<Vector3>(quads.Count * 6);
            var colors = new List<Color>(quads.Count * 6);
            var uvs = new List<Vector2>(quads.Count * 6);
            var tris = new List<int>(quads.Count * 6);

            foreach (var q in quads)
            {
                GetTriangles(q.x, q.y, out var t1, out var t2);
                AddTriangle(t1, q, 0, verts, normals, colors, uvs, tris);
                AddTriangle(t2, q, 1, verts, normals, colors, uvs, tris);
            }

            var root = new GameObject($"Berg_{key.x}_{key.y}");
            root.transform.SetParent(transform, false);
            root.transform.position = origin;

            var mesh = new Mesh { name = $"Berg_{key.x}_{key.y}" };
            if (verts.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            var go = new GameObject("Relief", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(root.transform, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = mountainMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;

            var chunk = new Chunk { Root = root, Triangles = tris.Count / 3 };
            chunk.Decor = BuildDecor(root, quads);
            return chunk;
        }

        private void AddTriangle(Vector3Int[] t, Vector2Int quad, int which, List<Vector3> verts, List<Vector3> normals,
            List<Color> colors, List<Vector2> uvs, List<int> tris)
        {
            var p = new Vector3[3];
            for (int k = 0; k < 3; k++) p[k] = new Vector3(px[t[k].x, t[k].y], ph[t[k].x, t[k].y], pz[t[k].x, t[k].y]);
            var normal = Vector3.Cross(p[1] - p[0], p[2] - p[0]).normalized;
            float steep = 1f - Mathf.Clamp01(normal.y);
            float faceNoise = Hash01(quad.x * 2 + which, quad.y, 41);
            int start = verts.Count;
            for (int k = 0; k < 3; k++)
            {
                verts.Add(p[k]);
                normals.Add(normal);
                colors.Add(VertexColor(p[k], steep, faceNoise));
                uvs.Add(new Vector2(HazeAt(p[k]), 0f));
                tris.Add(start + k);
            }
        }

        private float HazeAt(Vector3 p)
        {
            float outside = Mathf.Max(0f, Mathf.Max(Mathf.Max(-p.x, p.x - mapW), Mathf.Max(-p.z, p.z - mapH)));
            return 0.6f * Mathf.SmoothStep(0f, 1f, outside / 100f) * Smooth(0f, 6f, p.y);
        }

        /// <summary>Farbe nach Höhe und Steilheit; unten Gras wie die Wiese, damit der Fuß unsichtbar übergeht.</summary>
        private static Color VertexColor(Vector3 p, float steep, float faceNoise)
        {
            float n1 = ValueNoise(p.x / 5f, p.z / 5f, 51);
            float n2 = ValueNoise(p.x / 9f, p.z / 9f, 61);
            float h = p.y;

            var scrub = Color.Lerp(Olive, Ochre, Smooth(0.25f, 0.75f, n2));
            scrub = Color.Lerp(scrub, Grass, 0.4f * (1f - n1) * (1f - Smooth(2f, 6f, h)));
            float toScrub = Mathf.SmoothStep(0f, 1f, (h + (n1 - 0.5f) * 2.2f - 0.4f) / 3.2f) * 0.92f;
            var c = Color.Lerp(Grass, scrub, toScrub);

            var rock = Color.Lerp(RockWarm, RockGrey, Smooth(0.2f, 0.8f, n1));
            if (faceNoise > 0.72f) rock = Color.Lerp(rock, RockDark, 0.5f);
            float toRock = Mathf.SmoothStep(0f, 1f, (h + (n2 - 0.5f) * 3f - 7.5f) / 4.5f);
            float steepRock = Smooth(0.34f, 0.65f, steep + (n1 - 0.5f) * 0.12f) * Smooth(0.6f, 3f, h);
            c = Color.Lerp(c, rock, Mathf.Max(toRock, steepRock));

            float shade = 0.94f + 0.12f * faceNoise;
            c = new Color(c.r * shade, c.g * shade, c.b * shade, 1f);
            return c.linear;
        }

        // ---------------------------------------------------------------- Deko

        private int Count(float expected, System.Random random, int cap)
        {
            int n = Mathf.FloorToInt(expected);
            if (random.NextDouble() < expected - n) n++;
            return Mathf.Min(n, cap);
        }

        private static float Range(System.Random random, float min, float max) =>
            min + (float)random.NextDouble() * (max - min);

        private int BuildDecor(GameObject root, List<Vector2Int> quads)
        {
            int total = 0;
            foreach (var q in quads)
            {
                float x0 = xs[q.x], x1 = xs[q.x + 1], z0 = zs[q.y], z1 = zs[q.y + 1];
                float cxm = (x0 + x1) * 0.5f, czm = (z0 + z1) * 0.5f;
                float outside = Mathf.Max(0f, Mathf.Max(Mathf.Max(-cxm, cxm - mapW), Mathf.Max(-czm, czm - mapH)));
                if (outside > 85f) continue;
                bool ext = outside > 0.01f;
                float area = (x1 - x0) * (z1 - z0);
                float dens = ext ? outerDecorFactor : 1f;
                int cap = ext ? 2 : 1;
                float hq = (ph[q.x, q.y] + ph[q.x + 1, q.y] + ph[q.x, q.y + 1] + ph[q.x + 1, q.y + 1]) * 0.25f;
                float hMax = Mathf.Max(Mathf.Max(ph[q.x, q.y], ph[q.x + 1, q.y]), Mathf.Max(ph[q.x, q.y + 1], ph[q.x + 1, q.y + 1]));
                float hMin = Mathf.Min(Mathf.Min(ph[q.x, q.y], ph[q.x + 1, q.y]), Mathf.Min(ph[q.x, q.y + 1], ph[q.x + 1, q.y + 1]));
                float steep = ext ? Mathf.Clamp01((hMax - hMin) / Mathf.Max(1f, Mathf.Sqrt(area)) * 0.5f)
                                  : Mathf.Clamp01((hMax - hMin) / 1.4f * 0.5f);
                if (hq < 0.35f) continue;

                var random = new System.Random((int)Hash(q.x, q.y, 99));
                float scaleBoost = ext ? 1.8f : 1f;

                if (hq < 7.5f && steep < 0.38f)
                    total += Scatter(root, pines, Count(area * 0.06f * dens, random, cap), x0, x1, z0, z1, random,
                        0.85f * scaleBoost, 1.3f * scaleBoost, 0.18f, 0.5f);
                if (hq < 6.5f && steep < 0.42f)
                    total += Scatter(root, cypresses, Count(area * 0.03f * dens, random, cap), x0, x1, z0, z1, random,
                        0.8f * scaleBoost, 1.25f * scaleBoost, 0.18f, 0.5f);
                if (hq < 11f)
                    total += Scatter(root, bushes, Count(area * 0.12f * dens, random, cap + 1), x0, x1, z0, z1, random,
                        0.8f * scaleBoost, 1.5f * scaleBoost, 0.18f, 0.5f);
                float rockDensity = 0.012f + 0.1f * Smooth(0.25f, 0.6f, steep) + (hq > 7f ? 0.03f : 0f);
                total += Scatter(root, rocks, Count(area * rockDensity * dens, random, cap + 1), x0, x1, z0, z1, random,
                    (0.5f + 0.5f * steep) * scaleBoost, (0.9f + 0.6f * steep) * scaleBoost, 0.1f, 0.6f);
            }
            return total;
        }

        private int Scatter(GameObject root, GameObject[] prefabs, int count, float x0, float x1, float z0, float z1,
            System.Random random, float scaleMin, float scaleMax, float marginMin, float marginMax)
        {
            if (prefabs == null || prefabs.Length == 0) return 0;
            int made = 0;
            for (int k = 0; k < count; k++)
            {
                var prefab = prefabs[random.Next(prefabs.Length)];
                float x = Range(random, x0, x1), z = Range(random, z0, z1);
                float yaw = Range(random, 0f, 360f);
                float scale = Range(random, scaleMin, scaleMax);
                if (prefab == null || !TrySample(x, z, out float h) || h < 0.3f) continue;
                var go = Instantiate(prefab, origin + new Vector3(x, h - 0.12f * scale, z), Quaternion.Euler(0f, yaw, 0f), root.transform);
                go.transform.localScale *= scale;
                made++;
            }
            return made;
        }
    }
}
