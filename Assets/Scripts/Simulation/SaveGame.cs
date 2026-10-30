using System;
using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Inhalt eines Spielstands für JsonUtility: nur Zahlen und Namen, keine Unity-Objekte. Inhalte
    /// (Gebäude, Epochen, …) stehen unter ihrem Asset-Namen und werden über das ContentRegistry aufgelöst.
    /// Abgeleitete Werte (Anbindung, Unterhalt der Straßen) werden beim Laden neu berechnet.
    /// </summary>
    [Serializable]
    public class SaveGame
    {
        /// <summary>
        /// Formatversion; bei inkompatiblen Änderungen erhöhen und alte Stände umwandeln.
        /// 2: Rohstoff-Vorkommen (deposits); ältere Stände behalten die beim Start erzeugte Karte.
        /// 3: junge Felder (youngDeposits, gefällter oder gepflanzter Wald); ältere Stände sind ganz ausgewachsen.
        /// </summary>
        public const int CurrentVersion = 3;

        public int version = CurrentVersion;
        public string savedAt;

        [Header("Stadt")]
        public float money;
        public string epoch;
        public List<string> technologies = new List<string>();
        public List<AmountEntry> stock = new List<AmountEntry>();

        [Header("Zeit")]
        public int totalMonths;
        public float monthProgress;
        public int speed = 1;

        [Header("Forschung")]
        public string currentResearch;
        public List<AmountEntry> researchProgress = new List<AmountEntry>();

        [Header("Bevölkerung und Wirtschaft")]
        public List<AmountEntry> taxRates = new List<AmountEntry>();
        public float lastMonthTaxes;
        public float lastMonthIncome;
        public float lastMonthUpkeep;

        [Header("Karte")]
        public List<CellEntry> roads = new List<CellEntry>();
        public List<CellEntry> zones = new List<CellEntry>();
        public List<CellEntry> deposits = new List<CellEntry>();
        public List<YoungEntry> youngDeposits = new List<YoungEntry>();
        public List<BuildingEntry> buildings = new List<BuildingEntry>();

        [Header("Darstellung")]
        public bool hasCamera;
        public CameraState camera = new CameraState();
    }

    /// <summary>Menge zu einem Inhalt, z. B. Lagerbestand, Forschungsfortschritt oder Steuersatz.</summary>
    [Serializable]
    public struct AmountEntry
    {
        public string id;
        public float amount;

        public AmountEntry(string id, float amount)
        {
            this.id = id;
            this.amount = amount;
        }
    }

    /// <summary>Ein Feld mit einem Inhalt (Straßentyp oder Zone).</summary>
    [Serializable]
    public struct CellEntry
    {
        public int x;
        public int y;
        public string id;

        public CellEntry(Vector2Int cell, string id)
        {
            x = cell.x;
            y = cell.y;
            this.id = id;
        }

        public Vector2Int Cell => new Vector2Int(x, y);
    }

    /// <summary>Ein junges Feld mit Vorkommen (gefällt oder gepflanzt) und seinem Alter in Monaten.</summary>
    [Serializable]
    public struct YoungEntry
    {
        public int x;
        public int y;
        public int months;

        public YoungEntry(Vector2Int cell, int months)
        {
            x = cell.x;
            y = cell.y;
            this.months = months;
        }

        public Vector2Int Cell => new Vector2Int(x, y);
    }

    [Serializable]
    public class BuildingEntry
    {
        public string definition;
        public int x;
        public int y;
        public string builtInEpoch;
        public bool isProtected;
        public bool isPaused;
        public int residents;
        [Tooltip("Aktuelle Bevölkerungsstufe (leer in älteren Spielständen = Stufe aus den Gebäudedaten)")]
        public string tier;
        public int tierMonths;
        public float satisfaction;
        public string missingNeeds;
        [Tooltip("Fortschritt je aktiver Funktion, in der Reihenfolge von GetActiveFunctions")]
        public List<float> progress = new List<float>();
        [Tooltip("Drehung (0, 90, 180, 270); −1 in älteren Spielständen = wie beim Bauen bestimmen")]
        public float facing = -1f;
        [Tooltip("Gezeigte Modellvariante (RandomVariant), −1 = keine")]
        public int variant = -1;
    }

    [Serializable]
    public class CameraState
    {
        public Vector3 focus;
        public float distance;
        public float yaw;
        public float pitch;
    }
}
