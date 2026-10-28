using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Lässt Zonen von selbst bebauen: In festen Abständen (im Takt der Spielgeschwindigkeit) wächst auf
    /// einem freien Zonenfeld mit angrenzender Straße ein Gebäude der aktuellen Epoche. Gibt es für die
    /// aktuelle Epoche keines, wird das neueste ältere genommen. Bestehende Gebäude werden nie ersetzt;
    /// nach einem Abriss wächst auf der freien Fläche neu.
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

        private float accumulator;
        private readonly List<Vector2Int> candidates = new List<Vector2Int>();
        private readonly List<BuildingDefinition> choices = new List<BuildingDefinition>();

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

        /// <summary>Versucht, auf einem zufälligen freien Zonenfeld ein Gebäude wachsen zu lassen.</summary>
        public bool TryGrowOne()
        {
            candidates.Clear();
            foreach (var c in city.Grid.ZonedCells)
                if (city.Grid.GetAt(c) == null && city.Grid.TouchesRoad(c, Vector2Int.one))
                    candidates.Add(c);

            // Zufällige Reihenfolge; das erste Feld, auf dem ein passendes Gebäude Platz hat, wird bebaut
            while (candidates.Count > 0)
            {
                int i = Random.Range(0, candidates.Count);
                var cell = candidates[i];
                candidates[i] = candidates[candidates.Count - 1];
                candidates.RemoveAt(candidates.Count - 1);

                var def = PickBuilding(city.Grid.GetZone(cell));
                if (def != null && city.TryGrow(def, cell, out _)) return true;
            }
            return false;
        }

        /// <summary>Ein Gebäude dieser Zone aus der neuesten bereits erreichten Epoche, zufällig gewählt.</summary>
        public BuildingDefinition PickBuilding(ZoneDefinition zone)
        {
            choices.Clear();
            int bestOrder = int.MinValue;
            if (zone == null || zoneBuildings == null) return null;

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
            return choices.Count > 0 ? choices[Random.Range(0, choices.Count)] : null;
        }
    }
}
