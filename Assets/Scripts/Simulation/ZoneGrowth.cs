using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Lässt Zonen von selbst bebauen: In festen Abständen (im Takt der Spielgeschwindigkeit) wächst an einem
    /// freien Zonenfeld mit angrenzender Straße ein Gebäude der aktuellen Epoche. Gibt es für die aktuelle Epoche
    /// keines, wird das neueste ältere genommen. Zonengebäude sind 1 × 1 bis 4 × 4 groß, auch rechteckig: Geprüft
    /// werden alle Größen, Drehungen und Lagen des Grundstücks, die das Feld enthalten, ganz in der Zone liegen
    /// und ihre Vorderseite an der Straße haben; gewählt wird zufällig, gewichtet nach Fläche.
    /// Bestehende Gebäude werden nie ersetzt; nach einem Abriss wächst neu.
    /// Jede Zone hat ihren eigenen Takt, verlangsamt durch ihre Nachfrage (ZoneDemand): bei 100 % ein Gebäude je
    /// growthInterval, bei 0 % keines. Ohne ZoneDemand wachsen alle Zonen mit voller Rate.
    /// </summary>
    public class ZoneGrowth : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [SerializeField] private GameClock clock;
        [SerializeField] private ZoneDemand demand;
        [Tooltip("Alle Gebäude, die in Zonen wachsen können (BuildingDefinition.zone gesetzt)")]
        public BuildingDefinition[] zoneBuildings;
        [Tooltip("Spielsekunden zwischen zwei neuen Gebäuden")]
        [Min(0.1f)] public float growthInterval = 2f;
        [Tooltip("Gewicht je Grundstücksgröße (Index 0 = 1 × 1, 1 = 2 × 2, ...; Rechtecke nach Fläche dazwischen); höher = häufiger")]
        public float[] sizeWeights = { 1f, 2f, 2f, 2f };
        [Tooltip("1 × 1 nur, wo kein größeres Grundstück passt; sonst versperren kleine Häuser an der Straße " +
                 "den Zugang zu den Feldern dahinter")]
        public bool smallOnlyAsFallback = true;

        private readonly Dictionary<ZoneDefinition, float> accumulators = new Dictionary<ZoneDefinition, float>();
        private readonly List<ZoneDefinition> zones = new List<ZoneDefinition>();
        private readonly List<Vector2Int> candidates = new List<Vector2Int>();
        private readonly List<BuildingDefinition> choices = new List<BuildingDefinition>();
        private readonly List<(BuildingDefinition def, Vector2Int origin, float facing, float weight)> options =
            new List<(BuildingDefinition, Vector2Int, float, float)>();

        private static readonly Vector2Int[] Sides =
            { Vector2Int.down, Vector2Int.up, Vector2Int.left, Vector2Int.right };

        private void Update()
        {
            if (city == null || clock == null || city.Grid == null) return;

            if (zones.Count == 0 && zoneBuildings != null)
                foreach (var def in zoneBuildings)
                    if (def != null && def.zone != null && !zones.Contains(def.zone)) zones.Add(def.zone);

            float dt = Time.deltaTime * clock.Speed;
            foreach (var zone in zones)
            {
                float rate = demand != null ? demand.GetDemand(zone) : 1f;
                accumulators.TryGetValue(zone, out float acc);
                acc += dt * rate;
                while (acc >= growthInterval)
                {
                    acc -= growthInterval;
                    if (TryGrowOne(zone)) continue;
                    acc = 0f; // nichts frei: keinen Vorrat ansammeln, der später auf einmal wächst
                    break;
                }
                accumulators[zone] = acc;
            }
        }

        /// <summary>
        /// Versucht, an einem zufälligen freien Zonenfeld ein Gebäude wachsen zu lassen; mit zone nur in dieser Zone,
        /// ohne Angabe in allen. Die Nachfrage prüft hier niemand – sie bestimmt nur, wie oft Update dies aufruft.
        /// </summary>
        public bool TryGrowOne(ZoneDefinition zone = null)
        {
            candidates.Clear();
            foreach (var c in city.Grid.ZonedCells)
                if ((zone == null || city.Grid.GetZone(c) == zone) &&
                    city.Grid.GetAt(c) == null && city.Grid.TouchesRoad(c, Vector2Int.one))
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
                var (def, origin, facing) = PickWeighted();
                if (city.TryGrow(def, origin, facing, out _)) return true;
            }
            return false;
        }

        /// <summary>
        /// Alle passenden Gebäude, Lagen und Drehungen, deren Grundfläche das Feld enthält. Quadratische Gebäude
        /// drehen sich wie bisher nach dem Vorrang zur Straße (CityController.GetFacingAngle); rechteckige probieren
        /// alle vier Drehungen, bei denen die Vorderseite an einer Straße liegt.
        /// </summary>
        private void CollectOptions(Vector2Int cell)
        {
            options.Clear();
            foreach (var def in GetBuildings(city.Grid.GetZone(cell)))
            {
                bool square = def.size.x == def.size.y;
                foreach (float facing in square ? SquareOnly : CityController.Facings)
                {
                    var footprint = def.GetFootprint(facing);
                    for (int dx = 0; dx < footprint.x; dx++)
                        for (int dy = 0; dy < footprint.y; dy++)
                        {
                            var origin = new Vector2Int(cell.x - dx, cell.y - dy);
                            float f = square ? city.GetFacingAngle(def, origin) : facing;
                            if (!city.CanGrow(def, origin, f)) continue;
                            // Grundstücke, die freie Zonenfelder dahinter von der Straße abschneiden, seltener wählen
                            float weight = Weight(def) / (1f + CutOffCells(def.zone, footprint, origin));
                            options.Add((def, origin, f, weight));
                        }
                }
            }

            if (smallOnlyAsFallback && options.Exists(o => o.def.size.x > 1 || o.def.size.y > 1))
                options.RemoveAll(o => o.def.size.x == 1 && o.def.size.y == 1);
        }

        private static readonly float[] SquareOnly = { 0f };

        /// <summary>
        /// Freie Felder derselben Zone direkt hinter dem Grundstück, von der Straße aus gesehen. Sie wären nur noch
        /// über eine andere Straße erreichbar. Liegt auch dahinter eine Straße, wird nichts abgeschnitten.
        /// </summary>
        private int CutOffCells(ZoneDefinition zone, Vector2Int size, Vector2Int origin)
        {
            var grid = city.Grid;
            foreach (var side in Sides)
            {
                if (!grid.SideHasRoad(origin, size, side)) continue;
                var behind = -side;
                if (grid.SideHasRoad(origin, size, behind)) return 0;

                int count = 0;
                int length = side.x == 0 ? size.x : size.y;
                for (int k = 0; k < length; k++)
                {
                    var c = side.x == 0
                        ? new Vector2Int(origin.x + k, behind.y > 0 ? origin.y + size.y : origin.y - 1)
                        : new Vector2Int(behind.x > 0 ? origin.x + size.x : origin.x - 1, origin.y + k);
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

        private (BuildingDefinition, Vector2Int, float) PickWeighted()
        {
            float total = 0f;
            foreach (var o in options) total += o.weight;
            float roll = Random.value * total;
            foreach (var o in options)
            {
                roll -= o.weight;
                if (roll <= 0f) return (o.def, o.origin, o.facing);
            }
            var last = options[options.Count - 1];
            return (last.def, last.origin, last.facing);
        }

        /// <summary>
        /// Gewicht nach Fläche: sizeWeights gilt für n × n, Rechtecke liegen dazwischen (Seitenlänge √Fläche),
        /// z. B. 1 × 2 zwischen 1 × 1 und 2 × 2.
        /// </summary>
        private float Weight(BuildingDefinition def)
        {
            if (sizeWeights == null || sizeWeights.Length == 0) return 1f;
            float side = Mathf.Sqrt(def.size.x * def.size.y);
            int last = sizeWeights.Length - 1;
            int i0 = Mathf.Clamp(Mathf.FloorToInt(side) - 1, 0, last);
            int i1 = Mathf.Clamp(Mathf.CeilToInt(side) - 1, 0, last);
            return Mathf.Max(0f, Mathf.Lerp(sizeWeights[i0], sizeWeights[i1], side - Mathf.Floor(side)));
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
