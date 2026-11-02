using System;
using UnityEngine;

namespace EpochCity
{
    /// <summary>Spielmodus (entschieden 2026-10-31): zuerst Freies Bauen, der Story-Modus folgt.</summary>
    public enum GameMode { FreeBuild, Story }

    /// <summary>
    /// Einstellungen für ein neues Spiel, wie der Bildschirm „Neues Spiel“ in Anno und Cities: Skylines.
    /// Das Startmenü setzt Active vor dem Laden der Spielszene; CityController und FireSimulation übernehmen die Werte
    /// in Awake. Ohne Active (Szene direkt gestartet, Antike-Durchlauf) gelten die Werte aus der Szene.
    /// </summary>
    [Serializable]
    public class GameSettings
    {
        public GameMode mode = GameMode.FreeBuild;
        [Tooltip("Startwert der Karte (0 = zufällig)")]
        public int mapSeed;
        [Min(0f)] public float startMoney = 5000f;
        public FireFrequency fireFrequency = FireFrequency.Normal;

        /// <summary>Startgeld-Stufen fürs Menü (Name, Betrag); Normal ist der bisherige Wert der Szene.</summary>
        public static readonly (string name, float money)[] StartMoneyLevels =
            { ("Reich", 20000f), ("Normal", 5000f), ("Knapp", 2500f) };

        /// <summary>Einstellungen des nächsten Spiels (null = Werte der Szene).</summary>
        public static GameSettings Active { get; set; }

        // Ohne Domain-Reload beim Start des Play-Modus blieben sonst die Werte der letzten Sitzung stehen
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Active = null;
    }
}
