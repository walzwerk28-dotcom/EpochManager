using System;
using UnityEngine;

namespace EpochCity
{
    public enum BuildingCategory
    {
        Housing, Commerce, Production, Service, Storage, Wonder, Monument
    }

    [Serializable]
    public struct ResourceAmount
    {
        public ResourceDefinition resource;
        public float amount;
    }

    /// <summary>Eine Funktion eines Gebäudes: Eingänge -> Ausgänge pro Zyklus.</summary>
    [Serializable]
    public class BuildingFunction
    {
        public string label;
        public ResourceAmount[] inputs;
        public ResourceAmount[] outputs;
        [Min(0.1f)] public float cycleSeconds = 10f;
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
    }
}
