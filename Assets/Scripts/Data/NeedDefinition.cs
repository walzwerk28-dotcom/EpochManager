using UnityEngine;

namespace EpochCity
{
    public enum NeedKind
    {
        Goods,   // Ware aus dem Lager, z. B. Nahrung
        Service  // Versorgungsgebäude in Reichweite über Straßen, z. B. Markt
    }

    /// <summary>
    /// Ein Bedürfnis einer Bevölkerungsstufe. Gilt ab seiner Epoche (und ggf. ab einer Technologie, wie neue
    /// Bedürfnisse in Anno) und bleibt danach bestehen (neue Epochen fügen Bedürfnisse hinzu, entfernen keine).
    /// </summary>
    [CreateAssetMenu(menuName = "EpochCity/Need", fileName = "Need_")]
    public class NeedDefinition : ScriptableObject
    {
        public string displayName;
        [Tooltip("Ab dieser Epoche gilt das Bedürfnis (leer = immer)")]
        public EpochDefinition epoch;
        [Tooltip("Gilt erst, wenn diese Technologie erforscht ist (leer = ab der Epoche), z. B. Wasser ab Wasserbau")]
        public TechnologyDefinition requiredTechnology;
        [Tooltip("Ebene der Bedürfnispyramide: 1 Grundbedarf, 2 Sicherheit, 3 Komfort, 4 Bildung, 5 Luxus")]
        [Range(1, 5)] public int pyramidLevel = 1;
        [Tooltip("Anteil an der Zufriedenheit im Verhältnis zu den anderen Bedürfnissen")]
        [Min(0f)] public float weight = 1f;
        public NeedKind kind;

        [Header("Ware")]
        public ResourceDefinition resource;
        [Min(0f)] public float amountPerResidentPerMonth = 0.5f;

        [Header("Dienst")]
        [Tooltip("Gebäude, die dieses Bedürfnis erfüllen")]
        public BuildingDefinition[] providers;
        [Tooltip("Reichweite über das Straßennetz, in Straßenfeldern")]
        [Min(1f)] public float range = 15f;
        [Tooltip("Erfüllung über die Entfernung: x = 0 am Gebäude bis 1 an der Reichweitengrenze")]
        public AnimationCurve coverageByDistance = AnimationCurve.Linear(0f, 1f, 1f, 0.2f);

        /// <summary>Erfüllung eines Dienstes in dieser Entfernung; außerhalb der Reichweite 0.</summary>
        public float EvaluateCoverage(float distance)
        {
            if (distance > range) return 0f;
            return coverageByDistance != null && coverageByDistance.length > 0
                ? Mathf.Clamp01(coverageByDistance.Evaluate(distance / range))
                : 1f;
        }

        public bool IsProvider(BuildingDefinition def)
        {
            if (providers == null) return false;
            foreach (var p in providers)
                if (p == def) return true;
            return false;
        }
    }
}
