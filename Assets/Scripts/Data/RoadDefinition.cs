using UnityEngine;

namespace EpochCity
{
    /// <summary>Ein Straßentyp (Trampelpfad, Pflaster, ...). Gebaut wird feldweise auf dem Raster.</summary>
    [CreateAssetMenu(menuName = "EpochCity/Road", fileName = "Road_")]
    public class RoadDefinition : ScriptableObject
    {
        public string displayName;
        public EpochDefinition epoch;
        [Tooltip("Platzhalter oder Modell für genau ein Rasterfeld")]
        public GameObject prefab;
        [Tooltip("Breite der Fahrbahn in Metern; Stücke verbinden sich automatisch mit den Nachbarfeldern")]
        [Min(0.1f)] public float visualWidth = 1.3f;

        [Header("Kosten")]
        [Min(0f)] public float costPerCell = 5f;
        [Min(0f)] public float upkeepPerCellPerMonth = 0.1f;

        [Tooltip("Höhere Qualität lässt Entfernungen zum Lager kürzer wirken (ab der Lager-Anbindung)")]
        [Min(0.1f)] public float quality = 1f;
    }
}
