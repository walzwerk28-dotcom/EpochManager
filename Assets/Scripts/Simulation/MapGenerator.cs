using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Erzeugt die Karte beim Spielstart. Zuerst das Wasser (MapDefinition.water: Küste, Fluss, Seen), dann je Typ
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
            if (map.water != null && map.water.deposit != null) GenerateWater(grid, map, center, random);

            if (map.patches == null) return seed;
            foreach (var type in map.patches)
            {
                if (type == null || type.deposit == null) continue;
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
                    }
                    Grow(grid, map, center, type.deposit, start, size, random, 0f);
                }
            }
            return seed;
        }

        // ---------------------------------------------------------------- Wasser

        private static void GenerateWater(GridMap grid, MapDefinition map, Vector2 center, System.Random random)
        {
            var water = map.water;
            if (random.NextDouble() < water.coastChance) Coast(grid, map, center, random);
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
        }

        /// <summary>Streifen an einer zufälligen Kartenseite mit unruhiger Kante, bis an den Rand.</summary>
        private static void Coast(GridMap grid, MapDefinition map, Vector2 center, System.Random random)
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
