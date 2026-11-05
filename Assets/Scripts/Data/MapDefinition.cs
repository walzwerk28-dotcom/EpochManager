using UnityEngine;

namespace EpochCity
{
    /// <summary>Ein Vorkommen-Typ auf der Karte: wie viele Flecken in welcher Größe.</summary>
    [System.Serializable]
    public class DepositPatches
    {
        public DepositDefinition deposit;
        [Min(0)] public int minCount = 3;
        [Min(0)] public int maxCount = 4;
        [Min(1)] public int minCells = 4;
        [Min(1)] public int maxCells = 8;
        [Tooltip("Der erste Fleck liegt in diesem Abstand (Felder) zur Kartenmitte, damit der Start immer klappt")]
        public Vector2 firstPatchDistance = new Vector2(6f, 12f);
    }

    /// <summary>
    /// Wasser auf der Karte (MapGenerator): eine Küste an einer Kartenseite, ein Fluss von Rand zu Rand und Seen.
    /// Wasser ist ein Vorkommen mit DepositDefinition.isWater; ohne deposit gibt es keins. Um die Mitte bleibt es
    /// mindestens startAreaRadius + 3 Felder weit weg.
    /// </summary>
    [System.Serializable]
    public class WaterFeatures
    {
        [Tooltip("Das Wasser-Vorkommen (isWater); leer = kein Wasser")]
        public DepositDefinition deposit;

        [Header("Seen")]
        [Tooltip("Anzahl Seen (von, bis einschließlich)")]
        public Vector2Int lakeCount = new Vector2Int(0, 2);
        [Tooltip("Größe eines Sees in Feldern (von, bis)")]
        public Vector2Int lakeCells = new Vector2Int(20, 60);

        [Header("Fluss")]
        [Range(0f, 1f)] public float riverChance = 0.6f;
        [Tooltip("Breite in Feldern (von, bis)")]
        public Vector2Int riverWidth = new Vector2Int(1, 2);
        [Tooltip("Wahrscheinlichkeit je Feld, dass der Fluss seitlich versetzt wird (0 = gerade)")]
        [Range(0f, 1f)] public float riverMeander = 0.35f;

        [Header("Küste")]
        [Range(0f, 1f)] public float coastChance = 0.4f;
        [Tooltip("Tiefe des Streifens in Feldern (von, bis)")]
        public Vector2Int coastDepth = new Vector2Int(4, 8);
        [Tooltip("Wie unruhig die Kante ist: Wahrscheinlichkeit je Feld, dass sich die Tiefe um 1 ändert")]
        [Range(0f, 1f)] public float coastRoughness = 0.5f;
    }

    /// <summary>
    /// Parameter für die Karte beim Spielstart (MapGenerator): Rohstoff-Vorkommen als zufällige Flecken.
    /// Der Startwert 0 wählt bei jedem Spiel einen neuen.
    /// </summary>
    [CreateAssetMenu(menuName = "EpochCity/Map", fileName = "Map_")]
    public class MapDefinition : ScriptableObject
    {
        [Tooltip("0 = zufällig")]
        public int seed;
        [Tooltip("Abstand der Flecken zum Kartenrand in Feldern")]
        [Min(0)] public int border = 2;
        [Tooltip("Um die Kartenmitte bleibt ein Kreis mit diesem Radius (Felder) frei, dort beginnt man zu bauen")]
        [Min(0)] public float startAreaRadius = 4f;
        [Tooltip("Reihenfolge zählt: Spätere Typen lassen bereits belegte Felder aus")]
        public DepositPatches[] patches;
        [Tooltip("Küste, Fluss und Seen; wird vor den Flecken erzeugt")]
        public WaterFeatures water;
    }
}
