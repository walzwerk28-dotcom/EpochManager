using System;
using UnityEngine;

namespace EpochCity
{
    /// <summary>Anzeigename einer Bevölkerungsstufe ab einer bestimmten Epoche.</summary>
    [Serializable]
    public struct EpochName
    {
        public EpochDefinition epoch;
        public string name;
    }

    /// <summary>
    /// Eine Bevölkerungsstufe (1 bis 3). Sie läuft durch alle Epochen; nur Name und Bedürfnisse ändern sich.
    /// </summary>
    [CreateAssetMenu(menuName = "EpochCity/Population Tier", fileName = "Tier_")]
    public class PopulationTierDefinition : ScriptableObject
    {
        [Min(1)] public int tier = 1;
        [Tooltip("Name, falls für die Epoche keiner eingetragen ist")]
        public string displayName;
        public EpochName[] namesByEpoch;
        public NeedDefinition[] needs;

        [Header("Steuern")]
        [Min(0f)] public float taxPerResidentPerMonth = 1f;

        /// <summary>Name aus der neuesten Epoche, die höchstens der aktuellen entspricht.</summary>
        public string GetName(EpochDefinition current)
        {
            string result = displayName;
            int best = int.MinValue;
            if (namesByEpoch == null) return result;
            foreach (var entry in namesByEpoch)
            {
                if (entry.epoch == null || string.IsNullOrEmpty(entry.name)) continue;
                if (current != null && entry.epoch.order > current.order) continue;
                if (entry.epoch.order > best)
                {
                    best = entry.epoch.order;
                    result = entry.name;
                }
            }
            return result;
        }
    }
}
