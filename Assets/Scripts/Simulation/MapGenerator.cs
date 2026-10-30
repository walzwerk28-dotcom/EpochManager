using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Erzeugt die Rohstoff-Vorkommen beim Spielstart: je Typ einige Flecken, die von einem Mittelpunkt aus
    /// zufällig wachsen. Der erste Fleck jedes Typs liegt nahe der Kartenmitte, damit der Start immer klappt; die
    /// Mitte selbst (MapDefinition.startAreaRadius) bleibt frei.
    /// Gleicher Startwert ergibt gleiche Karte.
    /// </summary>
    public static class MapGenerator
    {
        /// <summary>Setzt die Vorkommen ins Raster und gibt den verwendeten Startwert zurück.</summary>
        public static int Generate(GridMap grid, MapDefinition map)
        {
            int seed = map.seed != 0 ? map.seed : Random.Range(1, int.MaxValue);
            var random = new System.Random(seed);
            if (map.patches == null) return seed;

            var center = new Vector2(grid.Width * 0.5f, grid.Height * 0.5f);
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
                        // Erster Fleck in einem Ring um die Mitte
                        double angle = random.NextDouble() * Mathf.PI * 2f;
                        float distance = Mathf.Lerp(type.firstPatchDistance.x, type.firstPatchDistance.y, (float)random.NextDouble());
                        start = Vector2Int.RoundToInt(center + new Vector2(Mathf.Cos((float)angle), Mathf.Sin((float)angle)) * distance);
                    }
                    else
                    {
                        start = new Vector2Int(random.Next(map.border, grid.Width - map.border),
                            random.Next(map.border, grid.Height - map.border));
                    }
                    Grow(grid, map, center, type.deposit, start, size, random);
                }
            }
            return seed;
        }

        /// <summary>Fleck um start: wächst an zufälligen Randfeldern, bis er size Felder hat oder nicht weiter kann.</summary>
        private static void Grow(GridMap grid, MapDefinition map, Vector2 center, DepositDefinition deposit,
            Vector2Int start, int size, System.Random random)
        {
            if (!Free(grid, map, center, start)) return;
            var frontier = new List<Vector2Int> { start };
            int placed = 0;
            while (placed < size && frontier.Count > 0)
            {
                int index = random.Next(frontier.Count);
                var c = frontier[index];
                frontier.RemoveAt(index);
                if (!Free(grid, map, center, c)) continue;
                grid.SetDeposit(c, deposit);
                placed++;
                foreach (var offset in GridMap.NeighbourOffsets)
                {
                    var n = c + offset;
                    if (Free(grid, map, center, n)) frontier.Add(n);
                }
            }
        }

        /// <summary>Im Raster ohne Randstreifen, außerhalb des Startbereichs, noch ohne Vorkommen, Gebäude und Straße.</summary>
        private static bool Free(GridMap grid, MapDefinition map, Vector2 center, Vector2Int c) =>
            c.x >= map.border && c.y >= map.border && c.x < grid.Width - map.border && c.y < grid.Height - map.border &&
            Vector2.Distance(center, c + new Vector2(0.5f, 0.5f)) > map.startAreaRadius &&
            grid.GetDeposit(c) == null && grid.GetAt(c) == null && !grid.HasRoad(c);
    }
}
