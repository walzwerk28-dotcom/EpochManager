using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Ein Zonentyp (Wohnen, Gewerbe, ...). Zonen füllen sich von selbst mit Gebäuden der aktuellen
    /// Epoche, deren BuildingDefinition.zone auf diesen Typ verweist.
    /// </summary>
    [CreateAssetMenu(menuName = "EpochCity/Zone", fileName = "Zone_")]
    public class ZoneDefinition : ScriptableObject
    {
        public string displayName;
        [Tooltip("Flache Kachel für genau ein Rasterfeld, zeigt die Zone an")]
        public GameObject tilePrefab;
    }
}
