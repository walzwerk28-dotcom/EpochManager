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
    }
}
