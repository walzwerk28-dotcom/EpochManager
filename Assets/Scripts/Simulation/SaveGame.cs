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
        /// 4: Feuer je Gebäude (BuildingEntry.fire, fireTimer); ältere Stände ohne Brände.
        /// 5: Spielmodus und Brand-Häufigkeit (mode, fireFrequency); ältere Stände laden als Freies Bauen, „normal“.
        /// 6: Bauphasen von Großbauten (BuildingEntry.stage, delivered, stageMoney, stageTimer); ältere: fertig.
        /// 7: Baustellen (BuildingEntry.buildTimer, −1 = fertig); ältere Stände: alle Gebäude fertig.
        /// 8: Arbeitsbereich von Holzfäller und Förster (BuildingEntry.workArea); ältere Stände: Standardbereich.
        /// 9: Handel am Hafen (tradeSettings, lastMonthTradeIncome, lastMonthTradeExpense); ältere Stände: kein Handel.
        /// </summary>
        public const int CurrentVersion = 9;

        public int version = CurrentVersion;
        public string savedAt;

        [Header("Kopf für die Liste der Spielstände (ältere Stände ohne: leer bzw. 0)")]
        public string epochName;
        public int population;

        [Header("Spiel")]
        public int mode = (int)GameMode.FreeBuild;
        public int fireFrequency = (int)FireFrequency.Normal;

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

        [Header("Handel am Hafen (ab Version 9)")]
        public List<TradeSettingEntry> tradeSettings = new List<TradeSettingEntry>();
        public float lastMonthTradeIncome;
        public float lastMonthTradeExpense;

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

    /// <summary>Handelseinstellung einer Ware: Modus (TradeMode) und Grenze.</summary>
    [Serializable]
    public struct TradeSettingEntry
    {
        public string id;
        public int mode;
        public int limit;
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
        [Tooltip("Feuer: 0 = nichts, 1 = brennt, 2 = beschädigt (ältere Spielstände 0)")]
        public int fire;
        [Tooltip("Brennend: verbleibende Spielsekunden; beschädigt: verbleibende Spielmonate")]
        public float fireTimer;
        [Tooltip("Großbau: Bauphase (−1 in älteren Spielständen = fertig)")]
        public int stage = -1;
        public List<AmountEntry> delivered = new List<AmountEntry>();
        public float stageMoney;
        public float stageTimer;
        /// <summary>Baustelle: Spielsekunden seit Baubeginn; −1 = fertig (auch für ältere Stände ohne das Feld).</summary>
        public float buildTimer = -1f;
        [Tooltip("Arbeitsbereich (Holzfäller, Förster) als Paare x, y relativ zum Ursprung; leer = Standardbereich")]
        public int[] workArea;
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
