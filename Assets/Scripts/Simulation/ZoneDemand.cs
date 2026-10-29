using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Nachfrage je Zone (0 bis 1), wie die Nachfrage-Balken in Cities: Skylines. Sie steuert nur, wie schnell
    /// Zonen neu bebaut werden; bestehende Gebäude bleiben immer stehen.
    /// Wohnen: Belegung aller Wohnhäuser, zwischen zwei Schwellen auf 0..1 abgebildet. Leere oder unzufriedene
    /// Häuser (die Einwohner streben Kapazität × Zufriedenheit an) senken sie; unter der Startkapazität gilt
    /// eine Mindestnachfrage. Gewerbe: 1 − bebaute Gewerbefelder / (Einwohner / Einwohner je Feld); ohne
    /// Einwohner keine Nachfrage. Wird aus dem Zustand berechnet und deshalb nicht gespeichert.
    /// </summary>
    public class ZoneDemand : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [Tooltip("Zonen in der Reihenfolge der Anzeige")]
        public ZoneDefinition[] zones;
        [Tooltip("Sekunden (Echtzeit) zwischen zwei Neuberechnungen")]
        [Min(0f)] public float refreshSeconds = 0.5f;

        private readonly Dictionary<ZoneDefinition, float> demand = new Dictionary<ZoneDefinition, float>();
        private float timer;

        /// <summary>Einwohner und Wohnplätze aller Wohnhäuser bei der letzten Berechnung.</summary>
        public int Residents { get; private set; }
        public int Capacity { get; private set; }

        /// <summary>Nachfrage der Zone (0..1); unbekannte Zonen ohne Nachfrageregeln gelten als voll gefragt.</summary>
        public float GetDemand(ZoneDefinition zone) =>
            zone != null && demand.TryGetValue(zone, out float value) ? value : 1f;

        private void Update()
        {
            timer -= Time.unscaledDeltaTime;
            if (timer > 0f) return;
            timer = refreshSeconds;
            Recalculate();
        }

        public void Recalculate()
        {
            if (city == null || zones == null) return;

            int residents = 0, capacity = 0;
            foreach (var b in city.Buildings)
            {
                if (b.Definition.populationTier == null) continue;
                residents += b.Residents;
                capacity += b.Definition.GetCapacity(city.UnlockedTechnologies);
            }
            Residents = residents;
            Capacity = capacity;

            foreach (var zone in zones)
                if (zone != null)
                    demand[zone] = zone.demandKind == ZoneDemandKind.Residential
                        ? Residential(zone, residents, capacity)
                        : Commercial(zone, residents);
        }

        private static float Residential(ZoneDefinition zone, int residents, int capacity)
        {
            float occupancy = capacity > 0 ? (float)residents / capacity : 0f;
            float value = Mathf.InverseLerp(zone.occupancyForNoDemand, zone.occupancyForFullDemand, occupancy);
            if (capacity < zone.starterCapacity) value = Mathf.Max(value, zone.starterDemand);
            return value;
        }

        private float Commercial(ZoneDefinition zone, int residents)
        {
            float wanted = residents / zone.residentsPerCell;
            if (wanted <= 0f) return 0f;
            return Mathf.Clamp01(1f - BuiltCells(zone) / wanted);
        }

        /// <summary>Felder, die Gebäude dieser Zone belegen.</summary>
        public int BuiltCells(ZoneDefinition zone)
        {
            int cells = 0;
            foreach (var b in city.Buildings)
                if (b.Definition.zone == zone) cells += b.Definition.size.x * b.Definition.size.y;
            return cells;
        }
    }
}
