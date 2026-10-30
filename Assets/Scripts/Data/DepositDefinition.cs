using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Ein Rohstoff-Vorkommen auf der Karte (Wald, Steinvorkommen, ...). Liegt feldweise im Raster;
    /// Betriebe wie Holzfäller oder Steinbruch brauchen es im Umkreis (BuildingDefinition.requiredDeposit).
    /// </summary>
    [CreateAssetMenu(menuName = "EpochCity/Deposit", fileName = "Dep_")]
    public class DepositDefinition : ScriptableObject
    {
        public string displayName;
        [Tooltip("Farbe für Anzeigen (Kachel, später Overlay)")]
        public Color color = Color.green;
        [Tooltip("Flache Kachel für genau ein Rasterfeld; vorläufige Darstellung, bis Bäume und Felsen kommen")]
        public GameObject tilePrefab;

        [Tooltip("Gebäude, Straßen und gewachsene Zonengebäude entfernen das Vorkommen endgültig (z. B. Wald roden)")]
        public bool clearedByBuilding;
        [Tooltip("Hier stehen nur Gebäude, die dieses Vorkommen brauchen, und keine Zonen; Straßen dürfen darüber")]
        public bool blocksBuilding;
    }
}
