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
        [Tooltip("Größe auf dem Raster in Zellen: x = Breite der Vorderseite, y = Tiefe (Vorderseite im Modell nach −z)")]
        public Vector2Int size = Vector2Int.one;
        [Tooltip("Zone, in der dieses Gebäude von selbst wächst. Leer = nur einzeln platzierbar.")]
        public ZoneDefinition zone;
        [Tooltip("Nur Wohngebäude: Bevölkerungsstufe der Bewohner; Kapazität = Wohnplätze")]
        public PopulationTierDefinition populationTier;
        [Tooltip("Erst baubar, wenn diese Technologie erforscht ist (leer = ab der Epoche)")]
        public TechnologyDefinition requiredTechnology;

        [Header("Arbeitskräfte")]
        [Tooltip("Stufe der Arbeitskräfte, die dieses Gebäude braucht (leer = keine)")]
        public PopulationTierDefinition workerTier;
        [Tooltip("Arbeitsplätze; fehlen Arbeitskräfte dieser Stufe, arbeitet das Gebäude anteilig langsamer")]
        [Min(0)] public int workers;

        [Header("Rohstoff-Vorkommen (z. B. Holzfäller, Steinbruch)")]
        [Tooltip("Nur baubar mit diesem Vorkommen im Umkreis; die Menge bestimmt die Effizienz (leer = keins nötig)")]
        public DepositDefinition requiredDeposit;
        [Tooltip("Umkreis um die Grundfläche in Feldern (0 = nur die Grundfläche selbst)")]
        [Min(0)] public int depositRange;
        [Tooltip("Ab so vielen Feldern mit Vorkommen arbeitet das Gebäude zu 100 %, darunter anteilig")]
        [Min(1)] public int depositCellsForFull = 1;

        [Header("Kosten")]
        public float buildCost = 100f;
        [Tooltip("Baumaterial aus dem Lager zusätzlich zum Geld (Zonengebäude wachsen trotzdem kostenlos)")]
        public ResourceAmount[] materialCosts;
        [Tooltip("Unterhalt pro Spielmonat. Sollte über Epochen kaum wachsen.")]
        public float upkeepPerMonth = 1f;

        [Header("Grundfunktion & Kapazität")]
        [Tooltip("Wohnhäuser: Wohnplätze. Lager (Kategorie Storage): Lagerplatz je Ware.")]
        public int baseCapacity;
        public BuildingFunction[] baseFunctions;

        [Header("Schichten durch Technologien")]
        public BuildingLayer[] layers;

        [Header("Lager (nur Kategorie Storage)")]
        [Tooltip("Einzugsweite über das Straßennetz, in Straßenfeldern (bessere Straßen zählen kürzer)")]
        [Min(1f)] public float catchmentRange = 20f;
        [Tooltip("Effizienz über die Entfernung: x = 0 am Lager bis 1 an der Einzugsgrenze, y = Effizienz")]
        public AnimationCurve efficiencyByDistance = AnimationCurve.Linear(0f, 1f, 1f, 0.25f);
        [Tooltip("Mindestwert, solange ein Gebäude über Straßen mit dem Lager verbunden ist")]
        [Range(0f, 1f)] public float minEfficiency = 0.25f;

        /// <summary>Einzugsweite inklusive Zuschlägen durch Technologien.</summary>
        public float GetCatchmentRange(HashSet<TechnologyDefinition> unlocked)
        {
            float range = catchmentRange;
            if (layers == null) return range;
            foreach (var l in layers)
                if (l.requiredTechnology != null && unlocked.Contains(l.requiredTechnology))
                    range += l.extraCatchment;
            return range;
        }

        /// <summary>Effizienz eines Gebäudes in dieser Entfernung zu diesem Lager mit dieser Einzugsweite.</summary>
        public float EvaluateStorageEfficiency(float distance, float range)
        {
            if (distance > range) return minEfficiency;
            float e = efficiencyByDistance != null && efficiencyByDistance.length > 0
                ? efficiencyByDistance.Evaluate(distance / range)
                : 1f;
            return Mathf.Clamp(e, minEfficiency, 1f);
        }

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

        /// <summary>Belegte Felder bei dieser Drehung: bei 90° und 270° sind Breite und Tiefe vertauscht.</summary>
        public Vector2Int GetFootprint(float facing) =>
            Mathf.RoundToInt(facing / 90f) % 2 != 0 ? new Vector2Int(size.y, size.x) : size;

        /// <summary>Kapazität: (Grund + Zuschläge) × Multiplikatoren aller freigeschalteten Schichten, gerundet.</summary>
        public int GetCapacity(HashSet<TechnologyDefinition> unlocked)
        {
            int c = baseCapacity;
            if (layers == null) return c;
            float multiplier = 1f;
            foreach (var l in layers)
                if (l.requiredTechnology != null && unlocked.Contains(l.requiredTechnology))
                {
                    c += l.extraCapacity;
                    if (l.capacityMultiplier > 0f) multiplier *= l.capacityMultiplier;
                }
            return Mathf.RoundToInt(c * multiplier);
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
