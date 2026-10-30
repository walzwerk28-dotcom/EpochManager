using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Lässt junge Felder (gefällter oder gepflanzter Wald) einmal pro Spielmonat altern. Nach
    /// DepositDefinition.regrowMonths zählen sie wieder, im Umkreis eines Försters schon nach regrowMonthsTended.
    /// Fällen und Pflanzen selbst geschehen in EconomySimulation über die Zyklen der Betriebe.
    /// </summary>
    public class ForestSimulation : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [SerializeField] private GameClock clock;

        private void OnEnable()
        {
            if (clock != null) clock.MonthPassed += OnMonthPassed;
        }

        private void OnDisable()
        {
            if (clock != null) clock.MonthPassed -= OnMonthPassed;
        }

        private void OnMonthPassed() => Grow();

        /// <summary>Ein Monat Wachstum; gibt die Zahl der Felder zurück, die ausgewachsen sind.</summary>
        public int Grow()
        {
            if (city == null || city.Grid == null || city.Grid.YoungDeposits.Count == 0) return 0;
            var grid = city.Grid;
            int grown = 0;
            foreach (var entry in new List<KeyValuePair<Vector2Int, int>>(grid.YoungDeposits))
            {
                var deposit = grid.GetDeposit(entry.Key);
                int age = entry.Value + 1;
                int needed = Mathf.Max(1, city.IsTended(entry.Key, deposit)
                    ? Mathf.Min(deposit.regrowMonthsTended, deposit.regrowMonths) : deposit.regrowMonths);
                if (age >= needed)
                {
                    grid.SetYoung(entry.Key, -1);
                    grown++;
                }
                else grid.SetYoung(entry.Key, age);
            }
            // Alter allein ändert nichts an den Anteilen; die Darstellung (Größe der jungen Bäume) folgt trotzdem
            city.OnDepositsAltered();
            return grown;
        }
    }
}
