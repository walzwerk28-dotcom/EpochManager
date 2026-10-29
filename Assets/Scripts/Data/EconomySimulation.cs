using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Rechnet die Wirtschaft in festen Zeitschritten: jedes Gebäude arbeitet seine
    /// aktiven Funktionen ab (Grundfunktionen plus durch Technologien freigeschaltete).
    /// Reine Zahlen, keine Optik. Einmal pro Spielmonat wird der Unterhalt abgezogen.
    /// </summary>
    public class EconomySimulation : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [SerializeField] private GameClock clock;
        [Tooltip("Länge eines Simulationsschritts in Spielsekunden")]
        [SerializeField, Min(0.1f)] private float stepSeconds = 1f;

        public float LastMonthIncome { get; private set; }
        public float LastMonthUpkeep { get; private set; }

        private float accumulator;
        private float incomeThisMonth;

        /// <summary>Abrechnung des letzten Monats aus einem Spielstand übernehmen.</summary>
        public void Restore(float lastMonthIncome, float lastMonthUpkeep)
        {
            LastMonthIncome = lastMonthIncome;
            LastMonthUpkeep = lastMonthUpkeep;
            incomeThisMonth = 0f;
        }

        private void OnEnable()
        {
            if (clock != null) clock.MonthPassed += OnMonthPassed;
        }

        private void OnDisable()
        {
            if (clock != null) clock.MonthPassed -= OnMonthPassed;
        }

        private void Update()
        {
            if (city == null || clock == null || city.Grid == null) return;

            accumulator += Time.deltaTime * clock.Speed;
            while (accumulator >= stepSeconds)
            {
                accumulator -= stepSeconds;
                Step(stepSeconds);
            }
        }

        private void Step(float dt)
        {
            foreach (var building in city.Buildings)
            {
                float efficiency = GetEfficiency(building);
                float multiplier = city.GetMultiplier(building);
                var functions = building.Definition.GetActiveFunctions(city.UnlockedTechnologies);

                if (functions.Count == 0) building.Status = "";
                else if (efficiency <= 0f) building.Status = building.ConnectionIssue;
                else if (efficiency >= 1f) building.Status = "läuft";
                else if (string.IsNullOrEmpty(building.ConnectionIssue))
                    building.Status = $"läuft ({efficiency * 100f:0} %)";
                else building.Status = $"läuft ({efficiency * 100f:0} %, {building.ConnectionIssue})";

                // Ohne Anbindung steht das Gebäude still, auch wenn ein Zyklus schon voll ist
                if (efficiency <= 0f) continue;

                foreach (var function in functions)
                {
                    if (function == null) continue;

                    building.Progress.TryGetValue(function, out float progress);
                    progress = Mathf.Min(progress + dt * efficiency, function.cycleSeconds);

                    if (progress >= function.cycleSeconds)
                    {
                        if (CanAfford(function.inputs, out string missing))
                        {
                            Consume(function.inputs);
                            Produce(function.outputs, multiplier);
                            progress -= function.cycleSeconds;
                        }
                        else
                        {
                            building.Status = "wartet: " + missing;
                        }
                    }
                    building.Progress[function] = progress;
                }
            }
        }

        /// <summary>Anbindungswert des Gebäudes, berechnet von CityController.RefreshConnections.</summary>
        private float GetEfficiency(BuildingInstance building) => building.Connection;

        private bool CanAfford(ResourceAmount[] inputs, out string missing)
        {
            missing = null;
            if (inputs == null) return true;
            foreach (var input in inputs)
            {
                if (input.resource == null) continue;
                float have = input.resource.isCurrency ? city.Money : city.Stock.Get(input.resource);
                if (have < input.amount)
                {
                    missing = input.resource.displayName;
                    return false;
                }
            }
            return true;
        }

        private void Consume(ResourceAmount[] inputs)
        {
            if (inputs == null) return;
            foreach (var input in inputs)
            {
                if (input.resource == null) continue;
                if (input.resource.isCurrency) city.AddMoney(-input.amount);
                else city.Stock.Add(input.resource, -input.amount);
            }
        }

        private void Produce(ResourceAmount[] outputs, float multiplier)
        {
            if (outputs == null) return;
            foreach (var output in outputs)
            {
                if (output.resource == null) continue;
                float amount = output.amount * multiplier;
                if (output.resource.isCurrency)
                {
                    city.AddMoney(amount);
                    incomeThisMonth += amount;
                }
                else
                {
                    city.Stock.Add(output.resource, amount);
                }
            }
        }

        private void OnMonthPassed()
        {
            if (city == null) return;

            float upkeep = 0f;
            foreach (var building in city.Buildings) upkeep += building.Definition.upkeepPerMonth;
            upkeep += city.RoadUpkeepPerMonth;
            city.AddMoney(-upkeep);

            LastMonthUpkeep = upkeep;
            LastMonthIncome = incomeThisMonth;
            incomeThisMonth = 0f;
        }
    }
}
