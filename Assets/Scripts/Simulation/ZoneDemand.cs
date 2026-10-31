using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Nachfrage je Zone (0 bis 1), wie die Nachfrage-Balken in Cities: Skylines. Sie steuert nur, wie schnell
    /// Zonen neu bebaut werden; bestehende Gebäude bleiben immer stehen.
    /// Wohnen: Belegung der Wohnhäuser in dieser Zone (jede Wohnzone für sich), zwischen zwei Schwellen auf 0..1 abgebildet. Leere oder unzufriedene
    /// Häuser (die Einwohner streben Kapazität × Zufriedenheit an) senken sie; Häuser, die sich noch füllen, zählen
    /// ihre erwarteten Zuzüge zum Teil mit (pendingWeight), neue Häuser ohne eigene Zufriedenheit nehmen den Schnitt der
    /// Zone; unter der Startkapazität gilt eine Mindestnachfrage. Gewerbe: Kunden × Waren × Arbeitskräfte (siehe Commercial);
    /// GetIssue nennt, was bremst. Der angezeigte Wert gleicht sich über smoothingSeconds
    /// Spielzeit an (keine Sprünge, wenn ein Haus wächst). Wird aus dem Zustand berechnet und deshalb nicht gespeichert.
    /// </summary>
    public class ZoneDemand : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [SerializeField] private GameClock clock;
        [SerializeField] private EconomySimulation economy;
        [Tooltip("Zonen in der Reihenfolge der Anzeige")]
        public ZoneDefinition[] zones;
        [Tooltip("Sekunden (Echtzeit) zwischen zwei Neuberechnungen")]
        [Min(0f)] public float refreshSeconds = 0.5f;
        [Tooltip("Spielsekunden, in denen sich die Nachfrage zu etwa zwei Dritteln an den neuen Wert angleicht (0 = sofort)")]
        [Min(0f)] public float smoothingSeconds = 30f;
        [Tooltip("Anteil der noch erwarteten Zuzüge (Kapazität × Zufriedenheit − Einwohner), der schon als belegt zählt")]
        [Range(0f, 1f)] public float pendingWeight = 0.7f;
        [Tooltip("Angenommene Zufriedenheit neuer Häuser, solange die Zone noch keine bewohnten Häuser hat")]
        [Range(0f, 1f)] public float newHouseSatisfaction = 0.75f;
        [Tooltip("Gewerbe: Besetzung der Arbeitsplätze einer Stufe, bei der die Nachfrage auf 0 fällt (bei 100 % voll)")]
        [Range(0f, 1f)] public float minWorkforce = 0.85f;

        private readonly Dictionary<ZoneDefinition, float> demand = new Dictionary<ZoneDefinition, float>();
        private readonly Dictionary<ZoneDefinition, string> issues = new Dictionary<ZoneDefinition, string>();
        private float timer;

        /// <summary>Einwohner und Wohnplätze aller Wohnhäuser bei der letzten Berechnung.</summary>
        public int Residents { get; private set; }
        public int Capacity { get; private set; }

        /// <summary>Nachfrage der Zone (0..1); unbekannte Zonen ohne Nachfrageregeln gelten als voll gefragt.</summary>
        public float GetDemand(ZoneDefinition zone) =>
            zone != null && demand.TryGetValue(zone, out float value) ? value : 1f;

        /// <summary>Was die Nachfrage der Zone gerade bremst (für den Tooltip), null wenn nichts.</summary>
        public string GetIssue(ZoneDefinition zone) =>
            zone != null && issues.TryGetValue(zone, out string issue) ? issue : null;

        private void Update()
        {
            timer -= Time.unscaledDeltaTime;
            if (timer > 0f) return;
            timer = refreshSeconds;
            Recalculate(refreshSeconds * (clock != null ? clock.Speed : 1));
        }

        /// <summary>
        /// Neu berechnen; gameSeconds = seit der letzten Berechnung vergangene Spielzeit für die Glättung
        /// (ohne Angabe springt der Wert sofort auf das Ergebnis).
        /// </summary>
        public void Recalculate(float gameSeconds = float.PositiveInfinity)
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

            float blend = smoothingSeconds <= 0f ? 1f : 1f - Mathf.Exp(-gameSeconds / smoothingSeconds);
            foreach (var zone in zones)
            {
                if (zone == null) continue;
                float target = zone.demandKind == ZoneDemandKind.Residential
                    ? Residential(zone)
                    : Commercial(zone);
                demand[zone] = demand.TryGetValue(zone, out float current)
                    ? Mathf.Lerp(current, target, blend)
                    : target;
            }
        }

        /// <summary>
        /// Wohnen: Belegung der Häuser in dieser Zone (jede Wohnzone, z. B. Bauern und Handwerker, für sich); erwartete
        /// Zuzüge in Häuser, die sich noch füllen, zählen zum Teil schon mit.
        /// </summary>
        private float Residential(ZoneDefinition zone)
        {
            // Schnitt der Zufriedenheit bewohnter Häuser (nach Wohnplätzen) für neue Häuser ohne eigenen Wert
            float satisfactionSum = 0f;
            int rated = 0;
            foreach (var b in city.Buildings)
            {
                if (!IsHouseIn(b, zone) || b.Residents <= 0) continue;
                int cap = b.Definition.GetCapacity(city.UnlockedTechnologies);
                satisfactionSum += b.Satisfaction * cap;
                rated += cap;
            }
            float average = rated > 0 ? satisfactionSum / rated : newHouseSatisfaction;

            int residents = 0, capacity = 0;
            float pending = 0f;
            foreach (var b in city.Buildings)
            {
                if (!IsHouseIn(b, zone)) continue;
                int cap = b.Definition.GetCapacity(city.UnlockedTechnologies);
                float satisfaction = b.Residents > 0 || b.Satisfaction > 0f ? b.Satisfaction : average;
                residents += b.Residents;
                capacity += cap;
                pending += Mathf.Max(0f, cap * satisfaction - b.Residents);
            }
            float occupancy = capacity > 0 ? (residents + pendingWeight * pending) / capacity : 0f;
            float value = Mathf.InverseLerp(zone.occupancyForNoDemand, zone.occupancyForFullDemand, occupancy);
            if (capacity < zone.starterCapacity) value = Mathf.Max(value, zone.starterDemand);
            issues[zone] = value >= 0.99f ? null : "Häuser nicht voll belegt (Zufriedenheit, freie Wohnungen)";
            return value;
        }

        private bool IsHouseIn(BuildingInstance b, ZoneDefinition zone) =>
            b.Definition.populationTier != null && city.Grid.GetZone(b.Origin) == zone;

        /// <summary>
        /// Gewerbe wie in Cities: Skylines aus drei Faktoren (multipliziert), die bremsenden nennen den Grund:
        /// Kunden (Einwohner × Gewicht ihrer Stufe je Gewerbefeld gegen bebaute Felder), Waren (Anteil der Gewerbegebäude,
        /// die auf Ware warten) und Arbeitskräfte (Besetzung der Stufen, die im Gewerbe arbeiten).
        /// </summary>
        private float Commercial(ZoneDefinition zone)
        {
            float customers = 0f;
            foreach (var b in city.Buildings)
                if (b.Definition.populationTier != null && b.Tier != null) customers += b.Residents * b.Tier.commerceWeight;
            float wanted = customers / zone.residentsPerCell;
            if (wanted <= 0f) { issues[zone] = "keine Kunden"; return 0f; }
            float customerFactor = Mathf.Clamp01(1f - BuiltCells(zone) / wanted);

            int shops = 0, waiting = 0;
            string missingGood = null;
            PopulationTierDefinition shortTier = null;
            float workerFactor = 1f;
            foreach (var b in city.Buildings)
            {
                if (b.Definition.zone != zone) continue;
                shops++;
                if (b.Status != null && b.Status.StartsWith(WaitingPrefix))
                {
                    waiting++;
                    missingGood = b.Status.Substring(WaitingPrefix.Length);
                }
                var tier = b.Definition.workerTier;
                if (economy == null || tier == null || b.Definition.workers <= 0) continue;
                float f = Mathf.InverseLerp(minWorkforce, 1f, economy.GetWorkforceFactor(tier));
                if (f < workerFactor) { workerFactor = f; shortTier = tier; }
            }
            float goodsFactor = shops > 0 ? 1f - Mathf.InverseLerp(0.1f, 0.5f, (float)waiting / shops) : 1f;

            // Alle bremsenden Faktoren nennen, der schwächste zuerst
            var reasons = new List<(float factor, string text)>();
            if (customerFactor < 0.99f) reasons.Add((customerFactor, "genug Gewerbe für die Kunden"));
            if (goodsFactor < 0.99f) reasons.Add((goodsFactor, $"Waren fehlen: {missingGood}"));
            if (workerFactor < 0.99f) reasons.Add((workerFactor, $"zu wenig {shortTier.GetName(city.currentEpoch)}"));
            reasons.Sort((a, b) => a.factor.CompareTo(b.factor));
            issues[zone] = reasons.Count > 0 ? string.Join(", ", reasons.ConvertAll(r => r.text)) : null;
            return customerFactor * goodsFactor * workerFactor;
        }

        private const string WaitingPrefix = "wartet: ";

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
