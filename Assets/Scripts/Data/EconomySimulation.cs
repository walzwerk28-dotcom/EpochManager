using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Rechnet die Wirtschaft in festen Zeitschritten: jedes Gebäude arbeitet seine
    /// aktiven Funktionen ab (Grundfunktionen plus durch Technologien freigeschaltete).
    /// Reine Zahlen, keine Optik. Einmal pro Spielmonat wird der Unterhalt abgezogen.
    /// Leere Kasse: Gebäude mit Unterhalt arbeiten langsamer (unpaidEfficiency); stillgelegte Gebäude arbeiten
    /// nicht und zahlen nur einen Teil ihres Unterhalts (pausedUpkeepFactor). Ist für die Ausgabe eines Zyklus kein
    /// Platz im Lager (CityController.StorageCapacity), wartet der Betrieb mit „Lager voll“. Arbeitskräfte: Gebäude mit
    /// Arbeitsplätzen (workerTier, workers) teilen sich die Einwohner ihrer Stufe; reichen sie nicht, arbeiten alle
    /// anteilig langsamer (Effizienz = Anbindung × Vorkommen × Arbeitskräfte).
    /// </summary>
    public class EconomySimulation : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [SerializeField] private GameClock clock;
        [Tooltip("Länge eines Simulationsschritts in Spielsekunden")]
        [SerializeField, Min(0.1f)] private float stepSeconds = 1f;

        [Header("Leere Kasse und Stilllegen")]
        [Tooltip("Effizienz von Gebäuden mit Unterhalt, solange die Kasse unter 0 steht")]
        [Range(0f, 1f)] public float unpaidEfficiency = 0.5f;
        [Tooltip("Anteil des Unterhalts, den ein stillgelegtes Gebäude weiter zahlt")]
        [Range(0f, 1f)] public float pausedUpkeepFactor = 0.25f;

        // ---------------------------------------------------------------- Arbeitskräfte

        /// <summary>Arbeitskräfte einer Stufe: alle Einwohner der Wohnhäuser dieser Stufe (gemeinsamer Pool wie in Anno).</summary>
        public int GetAvailableWorkers(PopulationTierDefinition tier)
        {
            int sum = 0;
            foreach (var b in city.Buildings)
                if (b.Tier == tier) sum += b.Residents;
            return sum;
        }

        /// <summary>Arbeitsplätze einer Stufe in allen Gebäuden, die nicht stillgelegt sind.</summary>
        public int GetRequiredWorkers(PopulationTierDefinition tier)
        {
            int sum = 0;
            foreach (var b in city.Buildings)
                if (b.Definition.workerTier == tier && !b.IsPaused) sum += b.Definition.workers;
            return sum;
        }

        /// <summary>Besetzter Anteil der Arbeitsplätze einer Stufe; reichen die Arbeitskräfte nicht, für alle gleich.</summary>
        public float GetWorkforceFactor(PopulationTierDefinition tier)
        {
            int required = GetRequiredWorkers(tier);
            return required <= 0 ? 1f : Mathf.Clamp01(GetAvailableWorkers(tier) / (float)required);
        }

        /// <summary>Unterhalt pro Monat, den dieses Gebäude gerade kostet (stillgelegt nur ein Teil).</summary>
        public float GetUpkeep(BuildingInstance building) =>
            building.Definition.upkeepPerMonth * (building.IsPaused ? pausedUpkeepFactor : 1f);

        public float LastMonthIncome { get; private set; }
        public float LastMonthUpkeep { get; private set; }

        private readonly System.Collections.Generic.Dictionary<PopulationTierDefinition, float> workforce =
            new System.Collections.Generic.Dictionary<PopulationTierDefinition, float>();
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
            bool unpaid = city.Money < 0f;

            // Arbeitskräfte je Stufe einmal pro Schritt
            workforce.Clear();
            foreach (var building in city.Buildings)
            {
                var tier = building.Definition.workerTier;
                if (tier != null && building.Definition.workers > 0 && !workforce.ContainsKey(tier))
                    workforce[tier] = GetWorkforceFactor(tier);
            }

            foreach (var building in city.Buildings)
            {
                var functions = building.Definition.GetActiveFunctions(city.UnlockedTechnologies);
                if (building.IsPaused)
                {
                    building.Status = functions.Count > 0 ? "stillgelegt" : "";
                    continue;
                }

                var workerTier = building.Definition.workerTier;
                building.WorkforceFactor = workerTier != null && workforce.TryGetValue(workerTier, out float wf) ? wf : 1f;

                float efficiency = GetEfficiency(building) * building.WorkforceFactor;
                float multiplier = city.GetMultiplier(building);
                bool upkeepUnpaid = unpaid && building.Definition.upkeepPerMonth > 0f;
                if (upkeepUnpaid) efficiency *= unpaidEfficiency;

                string workforceIssue = building.WorkforceFactor < 1f
                    ? $"zu wenig {workerTier.GetName(city.currentEpoch)}" : null;
                string issue = string.Join(", ", System.Array.FindAll(
                    new[] { building.ConnectionIssue, building.DepositIssue, workforceIssue,
                            upkeepUnpaid ? "Unterhalt nicht bezahlt" : null },
                    s => !string.IsNullOrEmpty(s)));
                if (functions.Count == 0) building.Status = "";
                else if (efficiency <= 0f) building.Status = issue;
                else if (efficiency >= 1f) building.Status = "läuft";
                else if (string.IsNullOrEmpty(issue))
                    building.Status = $"läuft ({efficiency * 100f:0} %)";
                else building.Status = $"läuft ({efficiency * 100f:0} %, {issue})";

                // Ohne Anbindung steht das Gebäude still, auch wenn ein Zyklus schon voll ist
                if (efficiency <= 0f) continue;

                foreach (var function in functions)
                {
                    if (function == null) continue;

                    building.Progress.TryGetValue(function, out float progress);
                    progress = Mathf.Min(progress + dt * efficiency, function.cycleSeconds);

                    if (progress >= function.cycleSeconds)
                    {
                        if (!CanAfford(function.inputs, out string missing))
                            building.Status = "wartet: " + missing;
                        else if (!HasRoom(function.outputs, multiplier, out string full))
                            building.Status = "Lager voll: " + full; // wartet, ohne Eingänge zu verbrauchen
                        else
                        {
                            Consume(function.inputs);
                            Produce(function.outputs, multiplier);
                            progress -= function.cycleSeconds;
                        }
                    }
                    building.Progress[function] = progress;
                }
            }
        }

        /// <summary>Anbindung × Anteil des nötigen Vorkommens, beides berechnet von CityController.RefreshConnections.</summary>
        private float GetEfficiency(BuildingInstance building) => building.Connection * building.DepositFactor;

        /// <summary>Passt die ganze Ausgabe eines Zyklus noch ins Lager (StorageCapacity je Ware)?</summary>
        private bool HasRoom(ResourceAmount[] outputs, float multiplier, out string full)
        {
            full = null;
            if (outputs == null) return true;
            foreach (var output in outputs)
            {
                if (output.resource == null || city.HasRoom(output.resource, output.amount * multiplier)) continue;
                full = output.resource.displayName;
                return false;
            }
            return true;
        }

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
            foreach (var building in city.Buildings) upkeep += GetUpkeep(building);
            upkeep += city.RoadUpkeepPerMonth;
            city.AddMoney(-upkeep);

            LastMonthUpkeep = upkeep;
            LastMonthIncome = incomeThisMonth;
            incomeThisMonth = 0f;
        }
    }
}
