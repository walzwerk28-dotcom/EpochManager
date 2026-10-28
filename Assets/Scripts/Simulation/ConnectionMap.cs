using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Anbindungswert über das Straßennetz: Von jedem angebundenen Lager aus werden die kürzesten Wege
    /// berechnet (bessere Straßen zählen kürzer). Jedes Gebäude erhält den besten Wert über seine
    /// angrenzenden Straßenfelder. Reine Zahlen, wird nur bei Änderungen neu berechnet.
    /// </summary>
    public class ConnectionMap
    {
        private struct RoadValue
        {
            public float Efficiency;
            public bool InRange;
        }

        private readonly Dictionary<Vector2Int, RoadValue> roadValues = new Dictionary<Vector2Int, RoadValue>();

        /// <summary>Bester Anbindungswert eines Straßenfelds; false, wenn kein Lager erreichbar ist.</summary>
        public bool TryGetRoadEfficiency(Vector2Int cell, out float efficiency)
        {
            bool found = roadValues.TryGetValue(cell, out var value);
            efficiency = value.Efficiency;
            return found;
        }

        public void Recalculate(GridMap grid, List<BuildingInstance> buildings)
        {
            roadValues.Clear();

            foreach (var storage in buildings)
            {
                if (storage.Definition.category != BuildingCategory.Storage) continue;

                var distances = ShortestDistances(grid, grid.GetAdjacentCells(storage));
                foreach (var entry in distances)
                {
                    var def = storage.Definition;
                    var value = new RoadValue
                    {
                        Efficiency = def.EvaluateStorageEfficiency(entry.Value),
                        InRange = entry.Value <= def.catchmentRange
                    };
                    if (!roadValues.TryGetValue(entry.Key, out var old) || value.Efficiency > old.Efficiency)
                        roadValues[entry.Key] = value;
                }
            }

            foreach (var b in buildings) Apply(grid, b);
        }

        private void Apply(GridMap grid, BuildingInstance b)
        {
            bool touchesRoad = false;
            bool reachable = false;
            var best = default(RoadValue);

            foreach (var c in grid.GetAdjacentCells(b))
            {
                if (!grid.HasRoad(c)) continue;
                touchesRoad = true;
                if (!roadValues.TryGetValue(c, out var value)) continue;
                if (!reachable || value.Efficiency > best.Efficiency) best = value;
                reachable = true;
            }

            // Ein Lager braucht nur selbst eine Straße, um als Knotenpunkt zu dienen
            if (b.Definition.category == BuildingCategory.Storage)
            {
                b.Connection = touchesRoad ? 1f : 0f;
                b.ConnectionIssue = touchesRoad ? "" : "keine Straße";
                return;
            }

            if (!touchesRoad)
            {
                b.Connection = 0f;
                b.ConnectionIssue = "keine Straße";
            }
            else if (!reachable)
            {
                b.Connection = 0f;
                b.ConnectionIssue = "kein Lager erreichbar";
            }
            else
            {
                b.Connection = best.Efficiency;
                b.ConnectionIssue = best.InRange ? "" : "zu weit vom Lager";
            }
        }

        /// <summary>
        /// Dijkstra über Straßenfelder; Startfelder haben die Entfernung 0. Bessere Straßen zählen kürzer.
        /// Auch für die Reichweite von Diensten genutzt.
        /// </summary>
        public static Dictionary<Vector2Int, float> ShortestDistances(GridMap grid, IEnumerable<Vector2Int> starts)
        {
            var dist = new Dictionary<Vector2Int, float>();
            var open = new SortedSet<(float d, int x, int y)>();

            foreach (var s in starts)
            {
                if (!grid.HasRoad(s) || dist.ContainsKey(s)) continue;
                dist[s] = 0f;
                open.Add((0f, s.x, s.y));
            }

            while (open.Count > 0)
            {
                var current = open.Min;
                open.Remove(current);
                var cell = new Vector2Int(current.x, current.y);

                foreach (var offset in GridMap.NeighbourOffsets)
                {
                    var next = cell + offset;
                    var road = grid.GetRoad(next);
                    if (road == null) continue;

                    float candidate = current.d + 1f / road.quality;
                    if (dist.TryGetValue(next, out float known))
                    {
                        if (known <= candidate) continue;
                        open.Remove((known, next.x, next.y));
                    }
                    dist[next] = candidate;
                    open.Add((candidate, next.x, next.y));
                }
            }
            return dist;
        }
    }
}
