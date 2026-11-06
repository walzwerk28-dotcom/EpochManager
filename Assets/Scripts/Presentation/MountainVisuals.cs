using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EpochCity
{
    /// <summary>
    /// Zeigt die Berge der Karte (Vorkommen mit DepositDefinition.isMountain) als Höhenrelief statt einer Kachel je
    /// Feld. Ein regelmäßiges Gitter mit vier Eckpunkten je Feld (am Kartenrand wachsen die Abstände nach außen, damit
    /// das Gebirge weit über die Karte hinausläuft, ohne viele Dreiecke) liefert die Höhe: an der Außenkante der
    /// Bergfelder 0, nach innen steigt sie weich mit dem Abstand zum Rand (Distanzfeld), überlagert von Grat- und
    /// Talrauschen (Ridged-Noise), Rinnen entlang der Falllinie und einer welligen Fußlinie. Die Normalen sind weich
    /// (aus dem Höhenfeld), die Krümmung (Mulde oder Grat) steckt in der Vertexfarbe; Farbe und Gesteinsschichten
    /// entstehen im Shader. Abschnitte von chunkSize × chunkSize Feldern, nur geänderte werden neu gebaut. Dazu Deko
    /// (Steineichen, Pinien, Zypressen, Macchia, Felsen), fest an der Position verteilt: in Hainen nach Rauschen, unten
    /// und in Mulden dichter, an mittleren Hängen Pinien und Zypressen in Reihen, oben nur Felsen und Büsche; auf die
    /// Höhe des Reliefs gesetzt, Stämme bis in den Boden, Felsen eingesenkt und nach dem Hang ausgerichtet.
    /// GetHeight liefert die Höhe für die Kamera.
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
        [Min(4)] [SerializeField] private int chunkSize = 16;

        [Header("Deko")]
        [SerializeField] private GameObject[] oaks;
        [SerializeField] private GameObject[] pines;
        [SerializeField] private GameObject[] cypresses;
        [SerializeField] private GameObject[] bushes;
        [SerializeField] private GameObject[] rocks;
        [Tooltip("Anteil der Deko-Dichte außerhalb der Karte")]
        [Range(0f, 1f)] [SerializeField] private float outerDecorFactor = 0.2f;
        [Tooltip("Gesamtdichte der Deko (1 = Höchstdichte der Arten); lieber weniger Objekte, dafür bessere Modelle")]
        [Range(0.1f, 1f)] [SerializeField] private float decorDensity = 0.65f;

        // Abstände der Gitterlinien jenseits des Kartenrands (Meter), ihre Summe ist die Länge des Gebirges außerhalb
        private static readonly float[] ExtensionSteps = { 1, 1, 2, 2, 4, 4, 8, 8, 16, 16, 24, 24 };
        private static readonly int ExtCount = ExtensionSteps.Length;
        private const int Sub = 4;
        // Das Relief liegt 2 cm über dem Boden, damit es dort, wo es 0 ist, nicht mit der Wiese flackert
        private const float Lift = 0.02f;

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
        private float[,] ph;              // Höhe je Gitterpunkt
        private Vector3[,] nrm;           // weiche Normale je Gitterpunkt
        private float[,] curvature;       // >0 Mulde, <0 Grat (etwa -1 bis 1)
        private bool[,] mountainQuad;     // Quad (i, j) liegt in einem Bergfeld
        private bool[,] flip;             // Diagonale des Quads
        private int width, height;
        private float cell;
        private Vector3 origin;
        private bool dirty = true;
        private float mapW, mapH;
        private Material runtimeMaterial;
        private readonly Dictionary<GameObject, Vector2> prefabSize = new Dictionary<GameObject, Vector2>();

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

        private void OnDestroy()
        {
            if (runtimeMaterial != null) Destroy(runtimeMaterial);
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

        /// <summary>Weicher Übergang von 0 bei a nach 1 bei b.</summary>
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

        /// <summary>Ridged-Noise: scharfe Grate (1) und breite Täler (0).</summary>
        private static float Ridged(float x, float z, int salt)
        {
            float r = 1f - Mathf.Abs(2f * ValueNoise(x, z, salt) - 1f);
            return r * r;
        }

        private void ComputeField()
        {
            xs = BuildLines(width);
            zs = BuildLines(height);
            int nx = xs.Length, nz = zs.Length;

            mountainQuad = new bool[nx - 1, nz - 1];
            for (int i = 0; i < nx - 1; i++)
                for (int j = 0; j < nz - 1; j++)
                    mountainQuad[i, j] = IsMountainCell(QuadCell(xs[i], xs[i + 1], width), QuadCell(zs[j], zs[j + 1], height));

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

            // geglättetes Distanzfeld für die Richtung der Falllinie (Rinnen)
            var ds = new float[nx, nz];
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    float sum = 0f; int cnt = 0;
                    for (int di = -2; di <= 2; di++)
                        for (int dj = -2; dj <= 2; dj++)
                        {
                            int a = i + di, b = j + dj;
                            if (a < 0 || b < 0 || a >= nx || b >= nz) continue;
                            sum += Mathf.Min(dist[a, b], 200f); cnt++;
                        }
                    ds[i, j] = sum / cnt;
                }

            ph = new float[nx, nz];
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    if (!interior[i, j]) continue;
                    float x = xs[i], z = zs[j];
                    float outside = Mathf.Max(Mathf.Max(-x, x - mapW), Mathf.Max(-z, z - mapH));
                    outside = Mathf.Max(0f, outside);
                    float d = Mathf.Min(dist[i, j], 80f);

                    // wellige Fußlinie: die Steigung beginnt erst hinter einer unruhigen Linie, dann weich in 2 m
                    float off = 0.5f + 1.7f * ValueNoise(x / 7f, z / 7f, 81);
                    float dd = Mathf.Max(0f, d - off);
                    float foot = Smooth(0f, 2.2f, dd);
                    float ramp = Smooth(0f, 6f, dd);

                    float baseH = 0.34f * dd + 0.085f * Mathf.Pow(dd, 1.5f);
                    float broad = 0.5f + 0.9f * ValueNoise(x / 30f, z / 30f, 11) + 0.5f * Ridged(x / 20f, z / 20f, 13);
                    float h = baseH * broad;

                    // Grate und Täler
                    h += (Ridged(x / 11f, z / 11f, 23) - 0.35f) * 3.8f * ramp;
                    h += (Ridged(x / 6f, z / 6f, 29) - 0.35f) * 1.7f * Smooth(0f, 4f, dd);
                    h += (ValueNoise(x / 2.2f, z / 2.2f, 37) - 0.5f) * 0.35f * Smooth(0f, 3f, dd);

                    // Rinnen, die hangabwärts laufen: Rauschen quer zur Falllinie, entlang ihr langgestreckt
                    float gx = (ds[Mathf.Min(i + 1, nx - 1), j] - ds[Mathf.Max(i - 1, 0), j]) / Mathf.Max(0.01f, xs[Mathf.Min(i + 1, nx - 1)] - xs[Mathf.Max(i - 1, 0)]);
                    float gz = (ds[i, Mathf.Min(j + 1, nz - 1)] - ds[i, Mathf.Max(j - 1, 0)]) / Mathf.Max(0.01f, zs[Mathf.Min(j + 1, nz - 1)] - zs[Mathf.Max(j - 1, 0)]);
                    var tangent = new Vector2(-gz, gx);
                    if (tangent.sqrMagnitude < 1e-4f) tangent = Vector2.right; else tangent.Normalize();
                    float across = x * tangent.x + z * tangent.y;
                    float gullies = Ridged(across / 3.4f, dd / 22f, 91);
                    h += (gullies - 0.4f) * 2.4f * Smooth(2f, 9f, dd);

                    h = Mathf.Max(0f, h) * foot;
                    h *= 1f + 0.6f * Mathf.Clamp01(outside / 80f);
                    float cap = Mathf.Lerp(maxHeight, outerMaxHeight, Mathf.Clamp01(outside / 40f));
                    h = cap * (float)Math.Tanh(h / cap);
                    float fade = 1f - Mathf.SmoothStep(0f, 1f, (outside - fadeStart) / Mathf.Max(1f, ExtensionTotal - fadeStart));
                    ph[i, j] = Mathf.Max(0f, h * fade);
                }

            // weiche Normalen und Krümmung aus dem Höhenfeld
            nrm = new Vector3[nx, nz];
            curvature = new float[nx, nz];
            const int k = 3;
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    int i0 = Mathf.Max(i - 1, 0), i1 = Mathf.Min(i + 1, nx - 1);
                    int j0 = Mathf.Max(j - 1, 0), j1 = Mathf.Min(j + 1, nz - 1);
                    float dhdx = (ph[i1, j] - ph[i0, j]) / (xs[i1] - xs[i0]);
                    float dhdz = (ph[i, j1] - ph[i, j0]) / (zs[j1] - zs[j0]);
                    nrm[i, j] = new Vector3(-dhdx, 1f, -dhdz).normalized;

                    int a0 = Mathf.Max(i - k, 0), a1 = Mathf.Min(i + k, nx - 1);
                    int b0 = Mathf.Max(j - k, 0), b1 = Mathf.Min(j + k, nz - 1);
                    float sx = Mathf.Max(0.3f, (xs[a1] - xs[a0]) * 0.5f), sz = Mathf.Max(0.3f, (zs[b1] - zs[b0]) * 0.5f);
                    float lap = (ph[a0, j] + ph[a1, j] - 2f * ph[i, j]) / (sx * sx) + (ph[i, b0] + ph[i, b1] - 2f * ph[i, j]) / (sz * sz);
                    curvature[i, j] = Mathf.Clamp(lap * 0.8f, -1f, 1f);
                }

            // Diagonale je Quad: die mit dem kleineren Höhenunterschied, so bleiben Grate sauber
            flip = new bool[nx - 1, nz - 1];
            for (int i = 0; i < nx - 1; i++)
                for (int j = 0; j < nz - 1; j++)
                    flip[i, j] = Mathf.Abs(ph[i, j + 1] - ph[i + 1, j]) < Mathf.Abs(ph[i, j] - ph[i + 1, j + 1]);
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

        /// <summary>Höhe auf dem Dreiecksnetz an einer Position relativ zum Ursprung der Karte.</summary>
        private bool TrySample(float x, float z, out float h)
        {
            h = 0f;
            if (x < xs[0] || z < zs[0] || x > xs[xs.Length - 1] || z > zs[zs.Length - 1]) return false;
            int i = FindLine(xs, x), j = FindLine(zs, z);
            float u = Mathf.Clamp01((x - xs[i]) / (xs[i + 1] - xs[i]));
            float v = Mathf.Clamp01((z - zs[j]) / (zs[j + 1] - zs[j]));
            float ha = ph[i, j], hb = ph[i + 1, j], hc = ph[i + 1, j + 1], hd = ph[i, j + 1];
            if (!flip[i, j])
            {
                // Diagonale a-c: Dreiecke (a, d, c) für u <= v, (a, c, b) sonst
                h = u <= v ? ha + (hd - ha) * v + (hc - hd) * u : ha + (hb - ha) * u + (hc - hb) * v;
            }
            else
            {
                // Diagonale b-d: Dreiecke (a, d, b) für u + v <= 1, (b, d, c) sonst
                h = u + v <= 1f ? ha + (hb - ha) * u + (hd - ha) * v : hc + (hd - hc) * (1f - u) + (hb - hc) * (1f - v);
            }
            return true;
        }

        /// <summary>Höhe, Normale und Krümmung an einer Position (Normale und Krümmung vom nächsten Gitterpunkt).</summary>
        private bool Sample(float x, float z, out float h, out Vector3 normal, out float curv)
        {
            normal = Vector3.up; curv = 0f;
            if (!TrySample(x, z, out h)) return false;
            int i = FindLine(xs, x), j = FindLine(zs, z);
            if (x - xs[i] > xs[i + 1] - x) i++;
            if (z - zs[j] > zs[j + 1] - z) j++;
            normal = nrm[i, j];
            curv = curvature[i, j];
            return true;
        }

        // ---------------------------------------------------------------- Abschnitte

        private Vector2Int ChunkOfQuad(int i, int j) =>
            new Vector2Int(QuadCell(xs[i], xs[i + 1], width) / chunkSize, QuadCell(zs[j], zs[j + 1], height) / chunkSize);

        private void RebuildChunks()
        {
            EnsureMaterial();
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

        /// <summary>Eigene Materialkopie mit der Lage des Bodens (Welt-UV der Wiesentextur).</summary>
        private void EnsureMaterial()
        {
            if (mountainMaterial == null) return;
            if (runtimeMaterial == null) runtimeMaterial = new Material(mountainMaterial) { name = mountainMaterial.name + " (Laufzeit)" };
            runtimeMaterial.SetVector("_GroundRect", new Vector4(origin.x, origin.z, 1f / mapW, 1f / mapH));
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
            var verts = new List<Vector3>(quads.Count + 64);
            var normals = new List<Vector3>(quads.Count + 64);
            var colors = new List<Color>(quads.Count + 64);
            var uvs = new List<Vector2>(quads.Count + 64);
            var tris = new List<int>(quads.Count * 6);
            var index = new Dictionary<int, int>(quads.Count + 64);
            int nz = zs.Length;

            int Vertex(int i, int j)
            {
                int id = i * nz + j;
                if (index.TryGetValue(id, out int found)) return found;
                var p = new Vector3(xs[i], ph[i, j] + Lift, zs[j]);
                verts.Add(p);
                normals.Add(nrm[i, j]);
                colors.Add(new Color(0.5f + 0.5f * curvature[i, j], 0f, 0f, 1f));
                uvs.Add(new Vector2(HazeAt(p), 0f));
                index[id] = verts.Count - 1;
                return verts.Count - 1;
            }

            foreach (var q in quads)
            {
                int a = Vertex(q.x, q.y), b = Vertex(q.x + 1, q.y), c = Vertex(q.x + 1, q.y + 1), d = Vertex(q.x, q.y + 1);
                if (!flip[q.x, q.y]) { tris.Add(a); tris.Add(d); tris.Add(c); tris.Add(a); tris.Add(c); tris.Add(b); }
                else { tris.Add(a); tris.Add(d); tris.Add(b); tris.Add(b); tris.Add(d); tris.Add(c); }
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
            renderer.sharedMaterial = runtimeMaterial != null ? runtimeMaterial : mountainMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;

            var chunk = new Chunk { Root = root, Triangles = tris.Count / 3 };
            chunk.Decor = BuildDecor(root, quads);
            return chunk;
        }

        private float HazeAt(Vector3 p)
        {
            float outside = Mathf.Max(0f, Mathf.Max(Mathf.Max(-p.x, p.x - mapW), Mathf.Max(-p.z, p.z - mapH)));
            return 0.6f * Mathf.SmoothStep(0f, 1f, outside / 100f) * Smooth(0f, 6f, p.y);
        }

        // ---------------------------------------------------------------- Deko

        private static float Range(System.Random random, float min, float max) =>
            min + (float)random.NextDouble() * (max - min);

        private enum Kind { Oak, Pine, Cypress, Bush, Rock }

        private int BuildDecor(GameObject root, List<Vector2Int> quads)
        {
            int total = 0;
            var placed = new Dictionary<Vector2Int, List<Vector3>>();
            foreach (var q in quads)
            {
                float x0 = xs[q.x], x1 = xs[q.x + 1], z0 = zs[q.y], z1 = zs[q.y + 1];
                float cxm = (x0 + x1) * 0.5f, czm = (z0 + z1) * 0.5f;
                float outside = Mathf.Max(0f, Mathf.Max(Mathf.Max(-cxm, cxm - mapW), Mathf.Max(-czm, czm - mapH)));
                if (outside > 85f) continue;
                bool ext = outside > 0.01f;
                if (!ext)
                {
                    // Innerhalb der Karte ein ganzes Spielfeld auf einmal (an der Ecke seiner ersten Quads)
                    if ((q.x - ExtCount) % Sub != 0 || (q.y - ExtCount) % Sub != 0) continue;
                    x1 = xs[q.x + Sub]; z1 = zs[q.y + Sub];
                }
                var random = new System.Random((int)Hash(q.x, q.y, 99));
                total += Populate(root, placed, x0, x1, z0, z1, ext, random);
            }
            return total;
        }

        private int Populate(GameObject root, Dictionary<Vector2Int, List<Vector3>> placed,
            float x0, float x1, float z0, float z1, bool ext, System.Random random)
        {
            float area = (x1 - x0) * (z1 - z0);
            float factor = (ext ? outerDecorFactor : 1f) * decorDensity;
            float sizeBoost = ext ? 1.7f : 1f;
            int made = 0;
            made += PlaceKind(root, placed, Kind.Oak, oaks, 0.22f, area * factor, x0, x1, z0, z1, random, sizeBoost, 1.15f);
            made += PlaceKind(root, placed, Kind.Pine, pines, 0.14f, area * factor, x0, x1, z0, z1, random, sizeBoost, 1.0f);
            made += PlaceKind(root, placed, Kind.Cypress, cypresses, 0.5f, area * factor, x0, x1, z0, z1, random, sizeBoost, 0.6f);
            made += PlaceKind(root, placed, Kind.Bush, bushes, 0.45f, area * factor, x0, x1, z0, z1, random, sizeBoost, 0.45f);
            made += PlaceKind(root, placed, Kind.Rock, rocks, 0.2f, area * factor, x0, x1, z0, z1, random, sizeBoost, 0.6f);
            return made;
        }

        /// <summary>Dichte (je m²) einer Art an einer Stelle; Höhe, Hangneigung, Mulde und Hain-Rauschen bestimmen sie.</summary>
        private float Density(Kind kind, float x, float z, float h, float steep, float curv)
        {
            float valley = Mathf.Clamp01(curv * 1.4f);
            float slopeOk = 1f - Smooth(0.30f, 0.46f, steep);
            float low = 1f - Smooth(3.5f, 8f, h);
            float mid = Smooth(1.0f, 3.0f, h) * (1f - Smooth(7f, 10.5f, h));
            float g1 = ValueNoise(x / 6.5f, z / 6.5f, 201);
            switch (kind)
            {
                case Kind.Oak:
                    return 0.22f * low * (1f + 1.2f * valley) * slopeOk * Smooth(0.45f, 0.68f, g1);
                case Kind.Pine:
                {
                    float g2 = ValueNoise(x / 8f, z / 8f, 202);
                    return 0.14f * Mathf.Max(mid, 0.5f * low) * slopeOk * Smooth(0.42f, 0.66f, g2) * (1f - 0.7f * Smooth(0.6f, 0.8f, g1));
                }
                case Kind.Cypress:
                {
                    // Reihen entlang der Höhenlinien, nur dort, wo ein Rauschen es erlaubt; dazu seltene Einzelbäume
                    float gc = ValueNoise(x / 5f, z / 5f, 203);
                    float v = h / 1.15f + ValueNoise(x / 9f, z / 9f, 204) * 1.5f;
                    v -= Mathf.Floor(v);
                    float row = v < 0.2f ? 1f : 0f;
                    return (0.5f * row * Smooth(0.5f, 0.7f, gc) + 0.012f) * mid * slopeOk;
                }
                case Kind.Bush:
                {
                    float gm = ValueNoise(x / 4f, z / 4f, 205);
                    float high = 1f - 0.75f * Smooth(9f, 13f, h);
                    return 0.45f * high * (0.3f + 0.7f * Smooth(0.35f, 0.65f, gm)) * (1f - 0.7f * Smooth(0.45f, 0.7f, steep))
                           * (1f - 0.5f * Smooth(0.6f, 0.8f, g1));
                }
                default:
                {
                    float ridge = Mathf.Clamp01(-curv * 1.4f);
                    return 0.012f + 0.18f * Smooth(0.28f, 0.55f, steep) + 0.05f * Smooth(8f, 12f, h) + 0.06f * ridge;
                }
            }
        }

        private Vector2 PrefabSize(GameObject prefab)
        {
            if (prefabSize.TryGetValue(prefab, out var s)) return s;
            var filter = prefab.GetComponentInChildren<MeshFilter>();
            s = filter != null && filter.sharedMesh != null
                ? new Vector2(filter.sharedMesh.bounds.size.y, filter.sharedMesh.bounds.min.y) : new Vector2(1f, 0f);
            prefabSize[prefab] = s;
            return s;
        }

        private int PlaceKind(GameObject root, Dictionary<Vector2Int, List<Vector3>> placed, Kind kind, GameObject[] prefabs,
            float maxDensity, float effectiveArea, float x0, float x1, float z0, float z1, System.Random random,
            float sizeBoost, float spacing)
        {
            if (prefabs == null || prefabs.Length == 0) return 0;
            float expected = maxDensity * effectiveArea;
            int n = Mathf.FloorToInt(expected);
            if (random.NextDouble() < expected - n) n++;
            n = Mathf.Min(n, 40);
            int made = 0;
            for (int k = 0; k < n; k++)
            {
                float x = Range(random, x0, x1), z = Range(random, z0, z1);
                float yaw = Range(random, 0f, 360f);
                float accept = (float)random.NextDouble();
                int pick = random.Next(prefabs.Length);
                float scaleRoll = (float)random.NextDouble();
                float tiltAngle = Range(random, 0f, 5f), tiltAz = Range(random, 0f, 360f);
                var prefab = prefabs[pick];
                if (prefab == null) continue;
                if (!Sample(x, z, out float h, out Vector3 normal, out float curv) || h < 0.2f) continue;
                float steep = 1f - Mathf.Clamp01(normal.y);
                if (accept > Density(kind, x, z, h, steep, curv) / maxDensity) continue;
                if (kind != Kind.Bush && TooClose(placed, x, z, spacing * sizeBoost)) continue;

                float scale;
                Quaternion rotation;
                Vector3 position;
                var size = PrefabSize(prefab);
                if (kind == Kind.Rock)
                {
                    scale = Mathf.Lerp(0.6f, 1.3f, scaleRoll) * (0.8f + 0.4f * steep) * sizeBoost;
                    var up = Vector3.Slerp(Vector3.up, normal, 0.7f);
                    rotation = Quaternion.FromToRotation(Vector3.up, up) * Quaternion.Euler(0f, yaw, 0f);
                    // etwa ein Drittel der Höhe im Hang
                    position = origin + new Vector3(x, h - 0.33f * size.x * scale - size.y * scale, z);
                }
                else
                {
                    scale = Mathf.Lerp(0.7f, 1.2f, scaleRoll) * sizeBoost;
                    var axis = new Vector3(Mathf.Cos(tiltAz * Mathf.Deg2Rad), 0f, Mathf.Sin(tiltAz * Mathf.Deg2Rad));
                    rotation = Quaternion.AngleAxis(tiltAngle, axis) * Quaternion.Euler(0f, yaw, 0f);
                    // Fußpunkt unter der tiefsten Bodenhöhe der Stammfläche, der Stamm reicht ohnehin 0,25 m unter den Ursprung
                    float r = (kind == Kind.Bush ? 0.3f : 0.3f) * scale;
                    float low = h;
                    for (int s = 0; s < 4; s++)
                    {
                        float a = s * Mathf.PI * 0.5f;
                        if (TrySample(x + Mathf.Cos(a) * r, z + Mathf.Sin(a) * r, out float hs)) low = Mathf.Min(low, hs);
                    }
                    position = origin + new Vector3(x, low - (kind == Kind.Bush ? 0.08f : 0.04f) * scale, z);
                }

                var go = Instantiate(prefab, position, rotation, root.transform);
                go.transform.localScale *= scale;
                if (kind != Kind.Bush) Register(placed, x, z);
                made++;
            }
            return made;
        }

        private static bool TooClose(Dictionary<Vector2Int, List<Vector3>> placed, float x, float z, float distance)
        {
            var key = new Vector2Int(Mathf.FloorToInt(x), Mathf.FloorToInt(z));
            for (int dx = -1; dx <= 1; dx++)
                for (int dz = -1; dz <= 1; dz++)
                    if (placed.TryGetValue(new Vector2Int(key.x + dx, key.y + dz), out var list))
                        foreach (var p in list)
                        {
                            float ddx = p.x - x, ddz = p.z - z;
                            if (ddx * ddx + ddz * ddz < distance * distance) return true;
                        }
            return false;
        }

        private static void Register(Dictionary<Vector2Int, List<Vector3>> placed, float x, float z)
        {
            var key = new Vector2Int(Mathf.FloorToInt(x), Mathf.FloorToInt(z));
            if (!placed.TryGetValue(key, out var list)) placed[key] = list = new List<Vector3>();
            list.Add(new Vector3(x, 0f, z));
        }
    }
}
