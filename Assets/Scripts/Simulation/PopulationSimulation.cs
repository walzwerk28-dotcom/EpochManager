using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Bevölkerung, abstrakt pro Wohnhaus und einmal pro Spielmonat gerechnet:
    /// Waren werden aus dem Lager verbraucht, Dienste über ihre Reichweite im Straßennetz geprüft.
    /// Aus der gewichteten Erfüllung der Bedürfnisse ergibt sich die Zufriedenheit; die Einwohner bewegen
    /// sich in Schritten auf Kapazität × Zufriedenheit zu (Zuzug und Wegzug), an einer Straße aber mindestens auf
    /// Kapazität × minOccupancy. Danach zahlen sie Steuern
    /// nach dem Satz ihrer Stufe; der Satz senkt die Zufriedenheit über die Steuerkurve der Stufe.
    /// </summary>
    public class PopulationSimulation : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [SerializeField] private GameClock clock;
        [Tooltip("Höchstens dieser Anteil der Kapazität zieht pro Monat zu oder weg (mindestens 1 Person)")]
        [Range(0.01f, 1f)] public float maxChangePerMonth = 0.25f;
        [Tooltip("Ab dieser Erfüllung gilt ein Bedürfnis nicht mehr als fehlend")]
        [Range(0f, 1f)] public float missingThreshold = 0.5f;
        [Tooltip("Häuser an einer Straße füllen sich immer mindestens zu diesem Anteil, auch ohne erfüllte Bedürfnisse " +
                 "(wie neue Häuser in Anno); sonst gäbe es ohne Arbeitskräfte keine Nahrung und ohne Nahrung keine Einwohner")]
        [Range(0f, 1f)] public float minOccupancy = 0.25f;
        [Tooltip("Alle Bevölkerungsstufen (für Steuerregler und Anzeige)")]
        public PopulationTierDefinition[] tiers;

        public const float MinTaxRate = 0f;
        public const float MaxTaxRate = 2f;

        public int TotalResidents { get; private set; }

        /// <summary>Steuereinnahmen des letzten Spielmonats.</summary>
        public float LastMonthTaxes { get; private set; }

        private readonly Dictionary<PopulationTierDefinition, float> taxRates =
            new Dictionary<PopulationTierDefinition, float>();

        /// <summary>Steuersatz der Stufe (1 = 100 %), Standard 100 %.</summary>
        public float GetTaxRate(PopulationTierDefinition tier) =>
            tier != null && taxRates.TryGetValue(tier, out float rate) ? rate : 1f;

        public void SetTaxRate(PopulationTierDefinition tier, float rate)
        {
            if (tier == null) return;
            // Auf ganze Prozent runden, damit Schritte von 10 % nicht abdriften
            taxRates[tier] = Mathf.Clamp(Mathf.Round(rate * 100f) / 100f, MinTaxRate, MaxTaxRate);
        }

        /// <summary>Durchschnittliche Zufriedenheit, gewichtet nach Wohnplätzen.</summary>
        public float AverageSatisfaction { get; private set; }

        private readonly List<BuildingInstance> houses = new List<BuildingInstance>();
        private readonly List<NeedDefinition> activeNeeds = new List<NeedDefinition>();
        private readonly Dictionary<NeedDefinition, float> goodsRatio = new Dictionary<NeedDefinition, float>();
        private readonly Dictionary<NeedDefinition, Dictionary<Vector2Int, float>> serviceDistances =
            new Dictionary<NeedDefinition, Dictionary<Vector2Int, float>>();
        private readonly StringBuilder missing = new StringBuilder();

        private void OnEnable()
        {
            if (clock != null) clock.MonthPassed += SimulateMonth;
        }

        private void OnDisable()
        {
            if (clock != null) clock.MonthPassed -= SimulateMonth;
        }

        public void SimulateMonth()
        {
            if (city == null || city.Grid == null) return;

            houses.Clear();
            foreach (var b in city.Buildings)
                if (b.Definition.populationTier != null) houses.Add(b);

            PrepareGoods();
            serviceDistances.Clear();

            int residents = 0;
            float weightedSatisfaction = 0f;
            int totalCapacity = 0;
            float taxes = 0f;

            foreach (var house in houses)
            {
                UpdateHouse(house);
                var tier = house.Tier;
                int capacity = house.Definition.GetCapacity(city.UnlockedTechnologies);
                residents += house.Residents;
                totalCapacity += capacity;
                weightedSatisfaction += house.Satisfaction * capacity;
                taxes += house.Residents * tier.taxPerResidentPerMonth * GetTaxRate(tier);
            }

            // Aufstieg und Abstieg wirken ab dem nächsten Monat (Steuern zahlt noch die alte Stufe)
            int upgrades = 0, downgrades = 0;
            foreach (var house in houses)
            {
                int change = UpdateTier(house);
                if (change > 0) upgrades++;
                else if (change < 0) downgrades++;
            }
            if (upgrades > 0 || downgrades > 0) TiersChanged?.Invoke(upgrades, downgrades);

            // Steuern zahlen die Einwohner nach Zu- und Wegzug dieses Monats
            city.AddMoney(taxes);
            LastMonthTaxes = taxes;
            TotalResidents = residents;
            AverageSatisfaction = totalCapacity > 0 ? weightedSatisfaction / totalCapacity : 0f;
        }

        /// <summary>
        /// Nach dem Laden: Summen aus den gespeicherten Werten der Häuser neu bilden, ohne einen Monat zu
        /// simulieren (kein Verbrauch, keine Steuern).
        /// </summary>
        public void RestoreTotals(float lastMonthTaxes)
        {
            int residents = 0, totalCapacity = 0;
            float weightedSatisfaction = 0f;
            foreach (var b in city.Buildings)
            {
                if (b.Definition.populationTier == null) continue;
                int capacity = b.Definition.GetCapacity(city.UnlockedTechnologies);
                residents += b.Residents;
                totalCapacity += capacity;
                weightedSatisfaction += b.Satisfaction * capacity;
            }
            LastMonthTaxes = lastMonthTaxes;
            TotalResidents = residents;
            AverageSatisfaction = totalCapacity > 0 ? weightedSatisfaction / totalCapacity : 0f;
        }

        /// <summary>
        /// Verbraucht alle Waren-Bedürfnisse gemeinsam aus dem Lager und merkt sich, welcher Anteil gedeckt war.
        /// Ohne Einwohner zählt eine Ware als gedeckt, sobald etwas davon vorrätig ist.
        /// </summary>
        private void PrepareGoods()
        {
            goodsRatio.Clear();
            var demand = new Dictionary<NeedDefinition, float>();

            foreach (var house in houses)
                foreach (var need in GetActiveNeeds(house))
                    if (need.kind == NeedKind.Goods && need.resource != null)
                    {
                        demand.TryGetValue(need, out float d);
                        demand[need] = d + house.Residents * need.amountPerResidentPerMonth;
                    }

            foreach (var entry in demand)
            {
                var resource = entry.Key.resource;
                float available = city.Stock.Get(resource);
                if (entry.Value <= 0f)
                {
                    goodsRatio[entry.Key] = available > 0f ? 1f : 0f;
                    continue;
                }
                float used = Mathf.Min(available, entry.Value);
                city.Stock.Add(resource, -used);
                goodsRatio[entry.Key] = used / entry.Value;
            }
        }

        private void UpdateHouse(BuildingInstance house)
        {
            float weightSum = 0f;
            float fulfilled = 0f;
            missing.Clear();

            foreach (var need in GetActiveNeeds(house))
            {
                float f = need.kind == NeedKind.Goods
                    ? (goodsRatio.TryGetValue(need, out float ratio) ? ratio : 0f) * house.Connection
                    : GetServiceCoverage(need, house);

                weightSum += need.weight;
                fulfilled += need.weight * f;
                if (f < missingThreshold)
                {
                    if (missing.Length > 0) missing.Append(", ");
                    missing.Append(need.displayName);
                }
            }

            // Bedürfnisse ergeben die Grundzufriedenheit, der Steuersatz drückt sie über eine weiche Kurve
            var tier = house.Tier;
            float needs = weightSum > 0f ? fulfilled / weightSum : 1f;
            house.Satisfaction = needs * tier.TaxFactor(GetTaxRate(tier));
            house.MissingNeeds = missing.ToString();

            int capacity = house.Definition.GetCapacity(city.UnlockedTechnologies);
            float share = city.Grid.TouchesRoad(house) ? Mathf.Max(house.Satisfaction, minOccupancy) : house.Satisfaction;
            int target = Mathf.RoundToInt(capacity * share);
            int step = Mathf.Max(1, Mathf.CeilToInt(capacity * maxChangePerMonth));
            house.Residents = Mathf.Clamp(
                house.Residents + Mathf.Clamp(target - house.Residents, -step, step), 0, capacity);
        }

        // ---------------------------------------------------------------- Aufstieg

        /// <summary>Nach einem Monat mit Auf- bzw. Abstiegen: Anzahl der Häuser (für Meldungen).</summary>
        public event System.Action<int, int> TiersChanged;

        /// <summary>
        /// Zählt Monate mit erfüllten Aufstiegsbedingungen (Zufriedenheit ab upgradeSatisfaction und alle
        /// upgradeRequires erfüllt) bzw. Abstiegsbedingungen (Zufriedenheit unter downgradeSatisfaction) und wechselt
        /// nach genug Monaten in Folge die Stufe. Einwohner und Modell bleiben. Gibt +1, −1 oder 0 zurück.
        /// </summary>
        private int UpdateTier(BuildingInstance house)
        {
            var tier = house.Tier;
            var lower = GetLowerTier(tier);

            if (lower != null && house.Satisfaction < tier.downgradeSatisfaction)
            {
                house.TierMonths = Mathf.Min(house.TierMonths, 0) - 1;
                house.UpgradeIssue = "Zufriedenheit zu niedrig";
                if (-house.TierMonths < tier.downgradeMonths) return 0;
                house.Tier = lower;
                house.TierMonths = 0;
                return -1;
            }

            house.UpgradeIssue = GetUpgradeIssue(house);
            if (house.UpgradeIssue.Length > 0)
            {
                house.TierMonths = 0;
                return 0;
            }
            house.TierMonths = Mathf.Max(house.TierMonths, 0) + 1;
            if (house.TierMonths < tier.upgradeMonths) return 0;
            house.Tier = tier.nextTier;
            house.TierMonths = 0;
            house.UpgradeIssue = GetUpgradeIssue(house);
            return 1;
        }

        /// <summary>Warum das Haus nicht aufsteigt (leer = Bedingungen erfüllt).</summary>
        private string GetUpgradeIssue(BuildingInstance house)
        {
            var tier = house.Tier;
            if (tier.nextTier == null) return "höchste Stufe";
            if (house.Satisfaction < tier.upgradeSatisfaction)
                return $"Zufriedenheit unter {tier.upgradeSatisfaction * 100f:0} %";
            if (tier.upgradeRequires != null)
                foreach (var need in tier.upgradeRequires)
                {
                    if (need == null) continue;
                    float f = need.kind == NeedKind.Service ? GetServiceCoverage(need, house)
                        : (goodsRatio.TryGetValue(need, out float ratio) ? ratio : 0f) * house.Connection;
                    if (f < missingThreshold) return $"braucht {need.displayName}";
                }
            return "";
        }

        /// <summary>Stufe, deren nächste Stufe diese ist (aus tiers), sonst null.</summary>
        public PopulationTierDefinition GetLowerTier(PopulationTierDefinition tier)
        {
            if (tiers == null || tier == null) return null;
            foreach (var t in tiers)
                if (t != null && t.nextTier == tier) return t;
            return null;
        }

        /// <summary>Einwohner einer Stufe (Summe der Häuser), z. B. für Anzeigen.</summary>
        public int CountResidents(PopulationTierDefinition tier)
        {
            int sum = 0;
            foreach (var b in city.Buildings)
                if (b.Tier == tier && b.Definition.populationTier != null) sum += b.Residents;
            return sum;
        }

        /// <summary>Bedürfnisse der Stufe, deren Epoche erreicht und deren Technologie erforscht ist.</summary>
        private List<NeedDefinition> GetActiveNeeds(BuildingInstance house)
        {
            activeNeeds.Clear();
            var tier = house.Tier;
            if (tier.needs == null) return activeNeeds;
            foreach (var need in tier.needs)
                if (need != null && city.IsEpochReached(need.epoch) && city.IsUnlocked(need.requiredTechnology))
                    activeNeeds.Add(need);
            return activeNeeds;
        }

        /// <summary>Bester Abdeckungswert über die Straßenfelder am Haus; Entfernungen werden pro Monat zwischengespeichert.</summary>
        private float GetServiceCoverage(NeedDefinition need, BuildingInstance house)
        {
            if (!serviceDistances.TryGetValue(need, out var distances))
            {
                var starts = new List<Vector2Int>();
                foreach (var b in city.Buildings)
                    if (need.IsProvider(b.Definition) && !b.IsPaused) starts.AddRange(city.Grid.GetAdjacentCells(b));
                distances = ConnectionMap.ShortestDistances(city.Grid, starts);
                serviceDistances[need] = distances;
            }

            float best = 0f;
            foreach (var c in city.Grid.GetAdjacentCells(house))
                if (distances.TryGetValue(c, out float d))
                    best = Mathf.Max(best, need.EvaluateCoverage(d));
            return best;
        }
    }
}
