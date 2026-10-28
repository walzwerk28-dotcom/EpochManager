using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    public enum TechPath
    {
        Trade,      // Handel & Wirtschaft
        Culture,    // Kultur & Gesellschaft
        Industry,   // Industrie & Produktion
        Science     // Wissenschaft & Infrastruktur
    }

    /// <summary>Eine Technologie im verzweigten Baum.</summary>
    [CreateAssetMenu(menuName = "EpochCity/Technology", fileName = "Tech_")]
    public class TechnologyDefinition : ScriptableObject
    {
        public string displayName;
        public EpochDefinition epoch;
        public TechPath path;
        [Min(0)] public float researchCost = 100f;
        [Tooltip("Voraussetzungen, gern aus anderen Pfaden (Querverbindungen)")]
        public TechnologyDefinition[] prerequisites;
        [TextArea] public string description;

        public bool PrerequisitesMet(HashSet<TechnologyDefinition> unlocked)
        {
            if (prerequisites == null) return true;
            foreach (var p in prerequisites)
                if (p != null && !unlocked.Contains(p)) return false;
            return true;
        }
    }
}
