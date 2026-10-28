using System;
using UnityEngine;

namespace EpochCity
{
    /// <summary>Spielzeit: Geschwindigkeit (0 = Pause bis 3) und Spielmonate.</summary>
    public class GameClock : MonoBehaviour
    {
        [Min(1f)] public float secondsPerMonth = 30f;

        public int Speed { get; private set; } = 1;
        public int TotalMonths { get; private set; }

        /// <summary>Wird einmal pro vergangenem Spielmonat ausgelöst (Abrechnung).</summary>
        public event Action MonthPassed;

        private float monthProgress;

        public void SetSpeed(int speed) => Speed = Mathf.Clamp(speed, 0, 3);

        public string DateString => $"Jahr {TotalMonths / 12 + 1}, Monat {TotalMonths % 12 + 1}";

        private void Update()
        {
            monthProgress += Time.deltaTime * Speed;
            while (monthProgress >= secondsPerMonth)
            {
                monthProgress -= secondsPerMonth;
                TotalMonths++;
                MonthPassed?.Invoke();
            }
        }
    }
}
