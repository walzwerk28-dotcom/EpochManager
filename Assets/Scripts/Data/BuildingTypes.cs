using System;
using UnityEngine;

namespace EpochCity
{
    public enum BuildingCategory
    {
        Housing, Commerce, Production, Service, Storage, Wonder, Monument, Park
    }

    [Serializable]
    public struct ResourceAmount
    {
        public ResourceDefinition resource;
        public float amount;
    }

    /// <summary>
    /// Eine Bauphase eines Großbaus (wie die Bauabschnitte großer Bauten in Anno): Waren und Geld, die die Baustelle
    /// nach und nach aus dem Lager holt, und eine Mindestdauer.
    /// </summary>
    [Serializable]
    public class WonderStage
    {
        public string name;
        public ResourceAmount[] materials;
        [Min(0f)] public float money;
        [Tooltip("Spielsekunden, die die Phase mindestens dauert (auch wenn alles schon geliefert ist)")]
        [Min(0f)] public float minSeconds = 30f;
    }

    /// <summary>Eine Ware im Sortiment eines Gewerbegebäudes: Menge je Zyklus und Preis je Einheit.</summary>
    [Serializable]
    public struct SaleOffer
    {
        public ResourceDefinition resource;
        [Min(0f)] public float amount;
        [Tooltip("Erlös je Einheit in der Stadtkasse (vor dem Technologie-Bonus)")]
        [Min(0f)] public float price;
    }

    /// <summary>Eine Funktion eines Gebäudes: Eingänge -> Ausgänge pro Zyklus.</summary>
    [Serializable]
    public class BuildingFunction
    {
        public string label;
        public ResourceAmount[] inputs;
        public ResourceAmount[] outputs;
        [Min(0.1f)] public float cycleSeconds = 10f;
        [Tooltip("Gewerbe: verkauft je Zyklus jede Ware, die über der Rücklage für die Einwohner vorrätig ist; " +
                 "wartet nur, wenn gar nichts da ist")]
        public SaleOffer[] sales;

        public bool IsSale => sales != null && sales.Length > 0;
    }

    /// <summary>
    /// Eine Schicht, die eine Technologie über ein Gebäude legt.
    /// Das Aussehen des Gebäudes ändert sich dadurch NICHT.
    /// </summary>
    [Serializable]
    public class BuildingLayer
    {
        public TechnologyDefinition requiredTechnology;
        [Tooltip("Multiplikator auf die Ausgaben aller Funktionen")]
        public float outputMultiplier = 1f;
        [Tooltip("Zusätzliche Funktionen, die mit dieser Technologie dazukommen")]
        public BuildingFunction[] additionalFunctions;
        [Tooltip("Zusätzliche Kapazität (z. B. Wohnplätze)")]
        public int extraCapacity;
        [Tooltip("Multiplikator auf die Kapazität, z. B. 1,25 = +25 % Wohnplätze (0 oder 1 = keine Wirkung)")]
        public float capacityMultiplier = 1f;
        [Tooltip("Nur Lager: zusätzliche Einzugsweite in Straßenfeldern")]
        public float extraCatchment;
    }
}
