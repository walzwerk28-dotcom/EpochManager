using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Erzeugt die Karte beim Spielstart. Zuerst das Wasser (MapDefinition.water: Küste, Fluss, Seen), dann die Berge
    /// (MapDefinition.mountains: Randgebirge und Massive, nie Land abschneidend), dann je Typ
    /// einige Flecken Rohstoff-Vorkommen, die von einem Mittelpunkt aus zufällig wachsen. Der erste Fleck jedes Typs
    /// liegt nahe der Kartenmitte, damit der Start immer klappt; die Mitte selbst (MapDefinition.startAreaRadius)
    /// bleibt frei, Fluss und Küste halten startAreaRadius + 3 Felder Abstand.
    /// Gleicher Startwert ergibt gleiche Karte.
    /// </summary>
    public static class MapGenerator
    {
        /// <summary>Zusätzlicher Abstand von Wasser zum Startbereich in Feldern.</summary>
        private const float WaterMargin = 3f;

        /// <summary>Setzt die Vorkommen ins Raster und gibt den verwendeten Startwert zurück.</summary>
        /// <param name="seedOverride">Startwert statt map.seed (0 = den aus der Karte nehmen)</param>
        public static int Generate(GridMap grid, MapDefinition map, int seedOverride = 0)
        {
            int seed = seedOverride != 0 ? seedOverride : map.seed != 0 ? map.seed : Random.Range(1, int.MaxValue);
            var random = new System.Random(seed);

            var center = new Vector2(grid.Width * 0.5f, grid.Height * 0.5f);
            int coastSide = -1;
            if (map.water != null && map.water.deposit != null) coastSide = GenerateWater(grid, map, center, random);
            bool mountains = map.mountains != null && map.mountains.deposit != null;
            if (mountains) GenerateMountains(grid, map, center, random, coastSide);

            if (map.patches == null) return seed;
            foreach (var type in map.patches)
            {
                if (type == null || type.deposit == null) continue;
                var foot = mountains && map.mountains.stoneNearMountains && type.deposit.blocksBuilding &&
                           !type.deposit.isWater && !type.deposit.isMountain;
                int count = random.Next(type.minCount, Mathf.Max(type.minCount, type.maxCount) + 1);
                for (int i = 0; i < count; i++)
                {
                    int size = random.Next(type.minCells, Mathf.Max(type.minCells, type.maxCells) + 1);
                    Vector2Int start;
                    if (i == 0)
                    {
                        // Erster Fleck in einem Ring um die Mitte; liegt das Feld auf Wasser, das nächste freie im Ring
                        double angle = random.NextDouble() * Mathf.PI * 2f;
                        float distance = Mathf.Lerp(type.firstPatchDistance.x, type.firstPatchDistance.y, (float)random.NextDouble());
                        start = Vector2Int.RoundToInt(center + new Vector2(Mathf.Cos((float)angle), Mathf.Sin((float)angle)) * distance);
                        var onWater = grid.GetDeposit(start);
                        if (onWater != null && onWater.isWater) start = NearestFree(grid, map, center, start);
                    }
                    else
                    {
                        start = new Vector2Int(random.Next(map.border, grid.Width - map.border),
                            random.Next(map.border, grid.Height - map.border));
                        // Am Fuß der Berge beginnen, wenn dort Platz ist (feste Reihenfolge, ein Zufallswert)
                        if (foot)
                        {
                            var feet = MountainFoot(grid, map, center);
                            if (feet.Count > 0) start = feet[random.Next(feet.Count)];
                        }
                    }
                    Grow(grid, map, center, type.deposit, start, size, random, 0f);
                }
            }
            return seed;
        }

        // ---------------------------------------------------------------- Wasser

        /// <summary>Erzeugt das Wasser; gibt die Küstenseite zurück (0 unten, 1 oben, 2 links, 3 rechts; −1 = keine Küste).</summary>
        private static int GenerateWater(GridMap grid, MapDefinition map, Vector2 center, System.Random random)
        {
            var water = map.water;
            int coastSide = -1;
            if (random.NextDouble() < water.coastChance) coastSide = Coast(grid, map, center, random);
            if (random.NextDouble() < water.riverChance) River(grid, map, center, random);

            int lakes = random.Next(water.lakeCount.x, Mathf.Max(water.lakeCount.x, water.lakeCount.y) + 1);
            for (int i = 0; i < lakes; i++)
            {
                int size = random.Next(water.lakeCells.x, Mathf.Max(water.lakeCells.x, water.lakeCells.y) + 1);
                for (int attempt = 0; attempt < 30; attempt++)
                {
                    var start = new Vector2Int(random.Next(map.border, grid.Width - map.border),
                        random.Next(map.border, grid.Height - map.border));
                    if (!Free(grid, map, center, start, WaterMargin)) continue;
                    Grow(grid, map, center, water.deposit, start, size, random, WaterMargin);
                    break;
                }
            }
            return coastSide;
        }

        /// <summary>Streifen an einer zufälligen Kartenseite mit unruhiger Kante, bis an den Rand. Gibt die Seite zurück.</summary>
        private static int Coast(GridMap grid, MapDefinition map, Vector2 center, System.Random random)
        {
            var water = map.water;
            int side = random.Next(4); // 0 unten, 1 oben, 2 links, 3 rechts
            int maxDepth = random.Next(water.coastDepth.x, Mathf.Max(water.coastDepth.x, water.coastDepth.y) + 1);
            int minDepth = Mathf.Max(1, maxDepth / 2);
            int depth = random.Next(minDepth, maxDepth + 1);
            int along = side < 2 ? grid.Width : grid.Height;
            for (int t = 0; t < along; t++)
            {
                if (random.NextDouble() < water.coastRoughness)
                    depth = Mathf.Clamp(depth + random.Next(3) - 1, minDepth, maxDepth);
                for (int k = 0; k < depth; k++)
                {
                    Vector2Int c = side == 0 ? new Vector2Int(t, k)
                        : side == 1 ? new Vector2Int(t, grid.Height - 1 - k)
                        : side == 2 ? new Vector2Int(k, t)
                        : new Vector2Int(grid.Width - 1 - k, t);
                    SetWater(grid, map, center, c);
                }
            }
            return side;
        }

        /// <summary>
        /// Geschlängelter Fluss von einem Rand zum gegenüberliegenden. Er bleibt in einem Streifen auf einer Seite
        /// der Mitte, hat überall dieselbe Breite und ist über die 4er-Nachbarschaft zusammenhängend: bei jedem
        /// Versatz wird die Zeile zwischen alter und neuer Lage aufgefüllt.
        /// </summary>
        private static void River(GridMap grid, MapDefinition map, Vector2 center, System.Random random)
        {
            var water = map.water;
            bool vertical = random.Next(2) == 0; // fließt entlang y, sonst entlang x
            int along = vertical ? grid.Height : grid.Width;
            int across = vertical ? grid.Width : grid.Height;
            float centerAcross = vertical ? center.x : center.y;
            int width = random.Next(water.riverWidth.x, Mathf.Max(water.riverWidth.x, water.riverWidth.y) + 1);
            float margin = map.startAreaRadius + WaterMargin;

            // Erlaubte Lage der unteren Flusskante: vor oder hinter der Mitte
            int lowMin = 1, lowMax = Mathf.FloorToInt(centerAcross - margin - width + 0.5f);
            int highMin = Mathf.CeilToInt(centerAcross + margin - 0.5f), highMax = across - width - 1;
            bool lowOk = lowMax >= lowMin, highOk = highMax >= highMin;
            if (!lowOk && !highOk) return;
            bool useLow = lowOk && (!highOk || random.Next(2) == 0);
            int bandMin = useLow ? lowMin : highMin, bandMax = useLow ? lowMax : highMax;

            int pos = random.Next(bandMin, bandMax + 1);
            int dir = random.Next(2) == 0 ? -1 : 1;
            for (int t = 0; t < along; t++)
            {
                int next = pos;
                if (random.NextDouble() < water.riverMeander)
                {
                    if (random.NextDouble() < 0.3) dir = -dir;
                    next = Mathf.Clamp(pos + dir, bandMin, bandMax);
                    if (next == pos) dir = -dir; // am Rand des Streifens umkehren
                }
                for (int a = Mathf.Min(pos, next); a <= Mathf.Max(pos, next) + width - 1; a++)
                    SetWater(grid, map, center, vertical ? new Vector2Int(a, t) : new Vector2Int(t, a));
                pos = next;
            }
        }

        /// <summary>Wasser auf ein Feld, nur im Raster und außerhalb des Startbereichs plus Abstand (ohne Randstreifen).</summary>
        private static void SetWater(GridMap grid, MapDefinition map, Vector2 center, Vector2Int c)
        {
            if (!grid.InBounds(c)) return;
            if (Vector2.Distance(center, c + new Vector2(0.5f, 0.5f)) <= map.startAreaRadius + WaterMargin) return;
            grid.SetDeposit(c, map.water.deposit);
        }

        // ---------------------------------------------------------------- Berge

        private const int MountainAttempts = 4;

        /// <summary>
        /// Randgebirge und Massive. Nach jedem Teil prüft eine Flutfüllung, dass alles Land und Wasser (Brücken sind
        /// erlaubt) ohne Berg von der Mitte aus erreichbar bleibt; sonst wird der Teil entfernt und neu versucht.
        /// </summary>
        private static void GenerateMountains(GridMap grid, MapDefinition map, Vector2 center, System.Random random, int coastSide)
        {
            var m = map.mountains;
            if (random.NextDouble() < m.rangeChance)
            {
                for (int attempt = 0; attempt < MountainAttempts; attempt++)
                {
                    var cells = MountainRange(grid, map, center, random, coastSide);
                    if (cells.Count > 0 && AllReachable(grid)) break;
                    RemoveAll(grid, cells);
                }
            }

            int massifs = random.Next(m.massifCount.x, Mathf.Max(m.massifCount.x, m.massifCount.y) + 1);
            for (int i = 0; i < massifs; i++)
            {
                int size = random.Next(m.massifCells.x, Mathf.Max(m.massifCells.x, m.massifCells.y) + 1);
                for (int attempt = 0; attempt < MountainAttempts; attempt++)
                {
                    var start = new Vector2Int(random.Next(map.border, grid.Width - map.border),
                        random.Next(map.border, grid.Height - map.border));
                    if (!MountainOk(grid, map, center, start)) continue;
                    var cells = GrowMountain(grid, map, center, start, size, random);
                    if (AllReachable(grid)) break;
                    RemoveAll(grid, cells);
                }
            }
        }

        /// <summary>Streifen an einer Kartenseite (nicht der Küste) bis an den Rand, mit unruhiger Kante und Ausläufern nach innen.</summary>
        private static List<Vector2Int> MountainRange(GridMap grid, MapDefinition map, Vector2 center, System.Random random, int coastSide)
        {
            var m = map.mountains;
            var cells = new List<Vector2Int>();
            var sides = new List<int>();
            for (int s = 0; s < 4; s++) if (s != coastSide) sides.Add(s);
            int side = sides[random.Next(sides.Count)]; // 0 unten, 1 oben, 2 links, 3 rechts
            int maxDepth = random.Next(m.rangeDepth.x, Mathf.Max(m.rangeDepth.x, m.rangeDepth.y) + 1);
            int minDepth = Mathf.Max(1, maxDepth / 2);
            int depth = random.Next(minDepth, maxDepth + 1);
            int along = side < 2 ? grid.Width : grid.Height;
            for (int t = 0; t < along; t++)
            {
                if (random.NextDouble() < m.rangeRoughness)
                    depth = Mathf.Clamp(depth + random.Next(3) - 1, minDepth, maxDepth);
                int length = depth;
                if (random.NextDouble() < m.spurChance) length += random.Next(2, 5);
                for (int k = 0; k < length; k++)
                {
                    Vector2Int c = side == 0 ? new Vector2Int(t, k)
                        : side == 1 ? new Vector2Int(t, grid.Height - 1 - k)
                        : side == 2 ? new Vector2Int(k, t)
                        : new Vector2Int(grid.Width - 1 - k, t);
                    if (!MountainOk(grid, map, center, c)) continue;
                    grid.SetDeposit(c, m.deposit);
                    cells.Add(c);
                }
            }
            return cells;
        }

        /// <summary>Massiv: wächst wie ein See an zufälligen Randfeldern.</summary>
        private static List<Vector2Int> GrowMountain(GridMap grid, MapDefinition map, Vector2 center, Vector2Int start,
            int size, System.Random random)
        {
            var cells = new List<Vector2Int>();
            var frontier = new List<Vector2Int> { start };
            while (cells.Count < size && frontier.Count > 0)
            {
                int index = random.Next(frontier.Count);
                var c = frontier[index];
                frontier.RemoveAt(index);
                if (!MountainOk(grid, map, center, c)) continue;
                grid.SetDeposit(c, map.mountains.deposit);
                cells.Add(c);
                foreach (var offset in GridMap.NeighbourOffsets)
                    if (MountainOk(grid, map, center, c + offset)) frontier.Add(c + offset);
            }
            return cells;
        }

        private static void RemoveAll(GridMap grid, List<Vector2Int> cells)
        {
            foreach (var c in cells) grid.SetDeposit(c, null);
            cells.Clear();
        }

        /// <summary>Im Raster, frei, weit genug von der Mitte und vom Wasser.</summary>
        private static bool MountainOk(GridMap grid, MapDefinition map, Vector2 center, Vector2Int c)
        {
            var m = map.mountains;
            if (!grid.InBounds(c) || grid.GetDeposit(c) != null || grid.GetAt(c) != null || grid.HasRoad(c)) return false;
            if (Vector2.Distance(center, c + new Vector2(0.5f, 0.5f)) <= Mathf.Max(m.startClearance, map.startAreaRadius + 4f)) return false;
            for (int dx = -m.waterClearance; dx <= m.waterClearance; dx++)
                for (int dy = -m.waterClearance; dy <= m.waterClearance; dy++)
                {
                    var d = grid.GetDeposit(c + new Vector2Int(dx, dy));
                    if (d != null && d.isWater) return false;
                }
            return true;
        }

        /// <summary>
        /// Flutfüllung von der Mitte über alle Felder, die eine Straße tragen könnten (alles außer Berg; Wasser als
        /// Brücke): True, wenn kein solches Feld abgeschnitten ist.
        /// </summary>
        public static bool AllReachable(GridMap grid)
        {
            int w = grid.Width, h = grid.Height;
            var seen = new bool[w, h];
            var queue = new Queue<Vector2Int>();
            var start = new Vector2Int(w / 2, h / 2);
            int total = 0;
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    var d = grid.GetDeposit(new Vector2Int(x, y));
                    if (d == null || !d.isMountain) total++;
                }
            if (grid.GetDeposit(start) != null && grid.GetDeposit(start).isMountain) return false;
            seen[start.x, start.y] = true;
            queue.Enqueue(start);
            int reached = 0;
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                reached++;
                foreach (var offset in GridMap.NeighbourOffsets)
                {
                    var n = c + offset;
                    if (!grid.InBounds(n) || seen[n.x, n.y]) continue;
                    var d = grid.GetDeposit(n);
                    if (d != null && d.isMountain) continue;
                    seen[n.x, n.y] = true;
                    queue.Enqueue(n);
                }
            }
            return reached == total;
        }

        /// <summary>Freie Felder (für Vorkommen) mit einem Bergfeld als 4er-Nachbarn, in fester Rasterreihenfolge.</summary>
        private static List<Vector2Int> MountainFoot(GridMap grid, MapDefinition map, Vector2 center)
        {
            var result = new List<Vector2Int>();
            for (int x = 0; x < grid.Width; x++)
                for (int y = 0; y < grid.Height; y++)
                {
                    var c = new Vector2Int(x, y);
                    if (!Free(grid, map, center, c, 0f)) continue;
                    foreach (var offset in GridMap.NeighbourOffsets)
                    {
                        var d = grid.GetDeposit(c + offset);
                        if (d != null && d.isMountain) { result.Add(c); break; }
                    }
                }
            return result;
        }

        // ---------------------------------------------------------------- Flecken

        /// <summary>Fleck um start: wächst an zufälligen Randfeldern, bis er size Felder hat oder nicht weiter kann.</summary>
        private static void Grow(GridMap grid, MapDefinition map, Vector2 center, DepositDefinition deposit,
            Vector2Int start, int size, System.Random random, float extraDistance)
        {
            if (!Free(grid, map, center, start, extraDistance)) return;
            var frontier = new List<Vector2Int> { start };
            int placed = 0;
            while (placed < size && frontier.Count > 0)
            {
                int index = random.Next(frontier.Count);
                var c = frontier[index];
                frontier.RemoveAt(index);
                if (!Free(grid, map, center, c, extraDistance)) continue;
                grid.SetDeposit(c, deposit);
                placed++;
                foreach (var offset in GridMap.NeighbourOffsets)
                {
                    var n = c + offset;
                    if (Free(grid, map, center, n, extraDistance)) frontier.Add(n);
                }
            }
        }

        /// <summary>Das nächste freie Feld um start, Ring für Ring (feste Reihenfolge, ohne Zufall); sonst start.</summary>
        private static Vector2Int NearestFree(GridMap grid, MapDefinition map, Vector2 center, Vector2Int start)
        {
            for (int r = 1; r <= 20; r++)
                for (int dx = -r; dx <= r; dx++)
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                        var c = start + new Vector2Int(dx, dy);
                        if (Free(grid, map, center, c, 0f)) return c;
                    }
            return start;
        }

        /// <summary>Im Raster ohne Randstreifen, außerhalb des Startbereichs, noch ohne Vorkommen, Gebäude und Straße.</summary>
        private static bool Free(GridMap grid, MapDefinition map, Vector2 center, Vector2Int c, float extraDistance) =>
            c.x >= map.border && c.y >= map.border && c.x < grid.Width - map.border && c.y < grid.Height - map.border &&
            Vector2.Distance(center, c + new Vector2(0.5f, 0.5f)) > map.startAreaRadius + extraDistance &&
            grid.GetDeposit(c) == null && grid.GetAt(c) == null && !grid.HasRoad(c);
    }
}
