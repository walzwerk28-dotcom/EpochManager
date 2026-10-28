using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Ein Gebäudetyp. Aussehen (Prefab) und Grundfunktion sind fest.
    /// Technologien legen Schichten (Layers) darüber.
    /// </summary>
    [CreateAssetMenu(menuName = "EpochCity/Building", fileName = "Bld_")]
    public class BuildingDefinition : ScriptableObject
    {
        [Header("Allgemein")]
        public string displayName;
        public EpochDefinition epoch;
        public BuildingCategory category;
        public GameObject prefab;
        [Tooltip("Größe auf dem Raster in Zellen")]
        public Vector2Int size = Vector2Int.one;

        [Header("Kosten")]
        public float buildCost = 100f;
        [Tooltip("Unterhalt pro Spielmonat. Sollte über Epochen kaum wachsen.")]
        public float upkeepPerMonth = 1f;

        [Header("Grundfunktion & Kapazität")]
        public int baseCapacity;
        public BuildingFunction[] baseFunctions;

        [Header("Schichten durch Technologien")]
        public BuildingLayer[] layers;

        /// <summary>Gesamt-Multiplikator aller freigeschalteten Schichten.</summary>
        public float GetOutputMultiplier(HashSet<TechnologyDefinition> unlocked)
        {
            float m = 1f;
            if (layers == null) return m;
            foreach (var l in layers)
                if (l.requiredTechnology != null && unlocked.Contains(l.requiredTechnology))
                    m *= l.outputMultiplier;
            return m;
        }

        public int GetCapacity(HashSet<TechnologyDefinition> unlocked)
        {
            int c = baseCapacity;
            if (layers == null) return c;
            foreach (var l in layers)
                if (l.requiredTechnology != null && unlocked.Contains(l.requiredTechnology))
                    c += l.extraCapacity;
            return c;
        }

        /// <summary>Grundfunktionen plus alle durch Technologien freigeschalteten.</summary>
        public List<BuildingFunction> GetActiveFunctions(HashSet<TechnologyDefinition> unlocked)
        {
            var result = new List<BuildingFunction>();
            if (baseFunctions != null) result.AddRange(baseFunctions);
            if (layers == null) return result;
            foreach (var l in layers)
            {
                if (l.requiredTechnology == null || !unlocked.Contains(l.requiredTechnology)) continue;
                if (l.additionalFunctions != null) result.AddRange(l.additionalFunctions);
            }
            return result;
        }
    }
}
