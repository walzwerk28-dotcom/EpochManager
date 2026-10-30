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
        [Tooltip("Flache Bodenkachel für genau ein Rasterfeld (z. B. Waldboden), unter der Deko")]
        public GameObject tilePrefab;

        [Header("Deko (Bäume, Felsen)")]
        [Tooltip("Modelle, aus denen je Feld zufällig gewählt wird")]
        public GameObject[] decorPrefabs;
        [Tooltip("Anzahl je Feld (von, bis einschließlich)")]
        public Vector2Int decorPerCell = new Vector2Int(2, 3);
        [Tooltip("Zufällige Größe (Faktor von, bis)")]
        public Vector2 decorScale = new Vector2(0.85f, 1.15f);

        [Tooltip("Gebäude, Straßen und gewachsene Zonengebäude entfernen das Vorkommen endgültig (z. B. Wald roden)")]
        public bool clearedByBuilding;
        [Tooltip("Hier stehen nur Gebäude, die dieses Vorkommen brauchen, und keine Zonen; Straßen dürfen darüber")]
        public bool blocksBuilding;

        [Header("Nachwachsen (Wald)")]
        [Tooltip("Monate, bis ein gefälltes oder gepflanztes Feld wieder zählt (0 = wächst nicht nach)")]
        [Min(0)] public int regrowMonths;
        [Tooltip("Monate bis dahin im Umkreis eines Gebäudes, das dieses Vorkommen pflanzt (Förster)")]
        [Min(1)] public int regrowMonthsTended = 4;
    }
}
