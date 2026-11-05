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
    /// Berge auf der Karte (MapGenerator): ein Randgebirge an einer Kartenseite (nicht der Küste) und einzelne Massive.
    /// Berge sind ein Vorkommen mit DepositDefinition.isMountain; ohne deposit gibt es keine. Sie halten Abstand zur
    /// Mitte und zum Wasser und trennen nie Land, das eine Straße tragen könnte, von der Mitte.
    /// </summary>
    [System.Serializable]
    public class MountainFeatures
    {
        [Tooltip("Das Berg-Vorkommen (isMountain); leer = keine Berge")]
        public DepositDefinition deposit;

        [Header("Randgebirge")]
        [Range(0f, 1f)] public float rangeChance = 0.5f;
        [Tooltip("Tiefe des Streifens in Feldern (von, bis)")]
        public Vector2Int rangeDepth = new Vector2Int(3, 7);
        [Tooltip("Wie unruhig die Kante ist: Wahrscheinlichkeit je Feld, dass sich die Tiefe um 1 ändert")]
        [Range(0f, 1f)] public float rangeRoughness = 0.5f;
        [Tooltip("Wahrscheinlichkeit je Feld, dass vom Gebirge ein Ausläufer nach innen wächst")]
        [Range(0f, 1f)] public float spurChance = 0.04f;

        [Header("Massive")]
        [Tooltip("Anzahl einzelner Massive (von, bis einschließlich)")]
        public Vector2Int massifCount = new Vector2Int(1, 2);
        [Tooltip("Größe eines Massivs in Feldern (von, bis)")]
        public Vector2Int massifCells = new Vector2Int(15, 35);

        [Header("Abstände")]
        [Tooltip("Freie Felder zwischen Bergen und Wasser")]
        [Min(0)] public int waterClearance = 1;
        [Tooltip("Mindestabstand zur Kartenmitte; wirksam mindestens startAreaRadius + 4")]
        [Min(0)] public float startClearance = 8f;
        [Tooltip("Steinflecken (außer dem ersten) beginnen bevorzugt am Fuß der Berge")]
        public bool stoneNearMountains = true;
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
        [Tooltip("Randgebirge und Massive; werden nach dem Wasser und vor Wald und Stein erzeugt")]
        public MountainFeatures mountains;
    }
}
