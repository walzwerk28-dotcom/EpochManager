using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Bevölkerung, abstrakt pro Wohnhaus und einmal pro Spielmonat gerechnet:
    /// Waren werden aus dem Lager verbraucht, Dienste über ihre Reichweite im Straßennetz geprüft.
    /// Aus der gewichteten Erfüllung der Bedürfnisse ergibt sich die Zufriedenheit; die Einwohner bewegen
    /// sich in Schritten auf Kapazität × Zufriedenheit zu (Zuzug und Wegzug).
    /// </summary>
    public class PopulationSimulation : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [SerializeField] private GameClock clock;
        [Tooltip("Höchstens dieser Anteil der Kapazität zieht pro Monat zu oder weg (mindestens 1 Person)")]
        [Range(0.01f, 1f)] public float maxChangePerMonth = 0.25f;
        [Tooltip("Ab dieser Erfüllung gilt ein Bedürfnis nicht mehr als fehlend")]
        [Range(0f, 1f)] public float missingThreshold = 0.5f;

        public int TotalResidents { get; private set; }

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

            foreach (var house in houses)
            {
                UpdateHouse(house);
                int capacity = house.Definition.GetCapacity(city.UnlockedTechnologies);
                residents += house.Residents;
                totalCapacity += capacity;
                weightedSatisfaction += house.Satisfaction * capacity;
            }

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

            house.Satisfaction = weightSum > 0f ? fulfilled / weightSum : 1f;
            house.MissingNeeds = missing.ToString();

            int capacity = house.Definition.GetCapacity(city.UnlockedTechnologies);
            int target = Mathf.RoundToInt(capacity * house.Satisfaction);
            int step = Mathf.Max(1, Mathf.CeilToInt(capacity * maxChangePerMonth));
            house.Residents = Mathf.Clamp(
                house.Residents + Mathf.Clamp(target - house.Residents, -step, step), 0, capacity);
        }

        /// <summary>Bedürfnisse der Stufe, deren Epoche bereits erreicht ist.</summary>
        private List<NeedDefinition> GetActiveNeeds(BuildingInstance house)
        {
            activeNeeds.Clear();
            var tier = house.Definition.populationTier;
            if (tier.needs == null) return activeNeeds;
            foreach (var need in tier.needs)
                if (need != null && city.IsEpochReached(need.epoch)) activeNeeds.Add(need);
            return activeNeeds;
        }

        /// <summary>Bester Abdeckungswert über die Straßenfelder am Haus; Entfernungen werden pro Monat zwischengespeichert.</summary>
        private float GetServiceCoverage(NeedDefinition need, BuildingInstance house)
        {
            if (!serviceDistances.TryGetValue(need, out var distances))
            {
                var starts = new List<Vector2Int>();
                foreach (var b in city.Buildings)
                    if (need.IsProvider(b.Definition)) starts.AddRange(city.Grid.GetAdjacentCells(b));
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
