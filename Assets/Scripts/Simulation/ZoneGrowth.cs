using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Lässt Zonen von selbst bebauen: In festen Abständen (im Takt der Spielgeschwindigkeit) wächst an einem
    /// freien Zonenfeld mit angrenzender Straße ein Gebäude der aktuellen Epoche. Gibt es für die aktuelle Epoche
    /// keines, wird das neueste ältere genommen. Zonengebäude sind 1 × 1 bis 4 × 4 groß: Geprüft werden alle
    /// Größen und alle Lagen des Grundstücks, die das Feld enthalten und ganz in der Zone liegen; gewählt wird
    /// zufällig, gewichtet nach Größe. Bestehende Gebäude werden nie ersetzt; nach einem Abriss wächst neu.
    /// Nachfrage gibt es noch nicht – sie folgt mit der Bevölkerung.
    /// </summary>
    public class ZoneGrowth : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [SerializeField] private GameClock clock;
        [Tooltip("Alle Gebäude, die in Zonen wachsen können (BuildingDefinition.zone gesetzt)")]
        public BuildingDefinition[] zoneBuildings;
        [Tooltip("Spielsekunden zwischen zwei neuen Gebäuden")]
        [Min(0.1f)] public float growthInterval = 2f;
        [Tooltip("Gewicht je Grundstücksgröße (Index 0 = 1 × 1, 1 = 2 × 2, ...); höher = häufiger")]
        public float[] sizeWeights = { 1f, 2f, 2f, 2f };
        [Tooltip("1 × 1 nur, wo kein größeres Grundstück passt; sonst versperren kleine Häuser an der Straße " +
                 "den Zugang zu den Feldern dahinter")]
        public bool smallOnlyAsFallback = true;

        private float accumulator;
        private readonly List<Vector2Int> candidates = new List<Vector2Int>();
        private readonly List<BuildingDefinition> choices = new List<BuildingDefinition>();
        private readonly List<(BuildingDefinition def, Vector2Int origin, float weight)> options =
            new List<(BuildingDefinition, Vector2Int, float)>();

        private static readonly Vector2Int[] Sides =
            { Vector2Int.down, Vector2Int.up, Vector2Int.left, Vector2Int.right };

        private void Update()
        {
            if (city == null || clock == null || city.Grid == null) return;

            accumulator += Time.deltaTime * clock.Speed;
            while (accumulator >= growthInterval)
            {
                accumulator -= growthInterval;
                TryGrowOne();
            }
        }

        /// <summary>Versucht, an einem zufälligen freien Zonenfeld ein Gebäude wachsen zu lassen.</summary>
        public bool TryGrowOne()
        {
            candidates.Clear();
            foreach (var c in city.Grid.ZonedCells)
                if (city.Grid.GetAt(c) == null && city.Grid.TouchesRoad(c, Vector2Int.one))
                    candidates.Add(c);

            // Zufällige Reihenfolge; am ersten Feld, an dem irgendein Grundstück passt, wird gebaut
            while (candidates.Count > 0)
            {
                int i = Random.Range(0, candidates.Count);
                var cell = candidates[i];
                candidates[i] = candidates[candidates.Count - 1];
                candidates.RemoveAt(candidates.Count - 1);

                CollectOptions(cell);
                if (options.Count == 0) continue;
                var (def, origin) = PickWeighted();
                if (city.TryGrow(def, origin, out _)) return true;
            }
            return false;
        }

        /// <summary>Alle passenden Gebäude und Lagen, deren Grundfläche das Feld enthält.</summary>
        private void CollectOptions(Vector2Int cell)
        {
            options.Clear();
            foreach (var def in GetBuildings(city.Grid.GetZone(cell)))
                for (int dx = 0; dx < def.size.x; dx++)
                    for (int dy = 0; dy < def.size.y; dy++)
                    {
                        var origin = new Vector2Int(cell.x - dx, cell.y - dy);
                        if (!city.CanGrow(def, origin)) continue;
                        // Grundstücke, die freie Zonenfelder dahinter von der Straße abschneiden, seltener wählen
                        float weight = Weight(def) / (1f + CutOffCells(def, origin));
                        options.Add((def, origin, weight));
                    }

            if (smallOnlyAsFallback && options.Exists(o => o.def.size.x > 1 || o.def.size.y > 1))
                options.RemoveAll(o => o.def.size.x == 1 && o.def.size.y == 1);
        }

        /// <summary>
        /// Freie Felder derselben Zone direkt hinter dem Grundstück, von der Straße aus gesehen. Sie wären nur noch
        /// über eine andere Straße erreichbar. Liegt auch dahinter eine Straße, wird nichts abgeschnitten.
        /// </summary>
        private int CutOffCells(BuildingDefinition def, Vector2Int origin)
        {
            var grid = city.Grid;
            var zone = def.zone;
            foreach (var side in Sides)
            {
                if (!grid.SideHasRoad(origin, def.size, side)) continue;
                var behind = -side;
                if (grid.SideHasRoad(origin, def.size, behind)) return 0;

                int count = 0;
                int length = side.x == 0 ? def.size.x : def.size.y;
                for (int k = 0; k < length; k++)
                {
                    var c = side.x == 0
                        ? new Vector2Int(origin.x + k, behind.y > 0 ? origin.y + def.size.y : origin.y - 1)
                        : new Vector2Int(behind.x > 0 ? origin.x + def.size.x : origin.x - 1, origin.y + k);
                    if (grid.GetZone(c) == zone && grid.GetAt(c) == null && !ReachableFromBehind(c, behind)) count++;
                }
                return count;
            }
            return 0;
        }

        /// <summary>Liegt in dieser Richtung höchstens 4 Felder weiter eine Straße? Dann kann ein Grundstück von dort es erreichen.</summary>
        private bool ReachableFromBehind(Vector2Int cell, Vector2Int direction)
        {
            for (int k = 1; k <= 4; k++)
                if (city.Grid.HasRoad(cell + direction * k)) return true;
            return false;
        }

        private (BuildingDefinition, Vector2Int) PickWeighted()
        {
            float total = 0f;
            foreach (var o in options) total += o.weight;
            float roll = Random.value * total;
            foreach (var o in options)
            {
                roll -= o.weight;
                if (roll <= 0f) return (o.def, o.origin);
            }
            var last = options[options.Count - 1];
            return (last.def, last.origin);
        }

        private float Weight(BuildingDefinition def)
        {
            int index = Mathf.Max(def.size.x, def.size.y) - 1;
            if (sizeWeights == null || sizeWeights.Length == 0) return 1f;
            return Mathf.Max(0f, sizeWeights[Mathf.Clamp(index, 0, sizeWeights.Length - 1)]);
        }

        /// <summary>Alle Gebäude dieser Zone aus der neuesten bereits erreichten Epoche (alle Größen).</summary>
        public List<BuildingDefinition> GetBuildings(ZoneDefinition zone)
        {
            choices.Clear();
            int bestOrder = int.MinValue;
            if (zone == null || zoneBuildings == null) return choices;

            foreach (var def in zoneBuildings)
            {
                if (def == null || def.zone != zone || !city.IsEpochReached(def.epoch)) continue;
                int order = def.epoch != null ? def.epoch.order : 0;
                if (order > bestOrder)
                {
                    bestOrder = order;
                    choices.Clear();
                }
                if (order == bestOrder) choices.Add(def);
            }
            return choices;
        }
    }
}
