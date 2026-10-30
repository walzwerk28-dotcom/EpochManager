using UnityEngine;

namespace EpochCity
{
    /// <summary>Woraus sich die Nachfrage einer Zone ergibt.</summary>
    public enum ZoneDemandKind
    {
        /// <summary>Wohnen: Belegung der Wohnhäuser (Zuzugsdruck).</summary>
        Residential,
        /// <summary>Gewerbe: Einwohner als Kundschaft je Gewerbefeld.</summary>
        Commercial
    }

    /// <summary>
    /// Ein Zonentyp (Wohnen, Gewerbe, ...). Zonen füllen sich von selbst mit Gebäuden der aktuellen
    /// Epoche, deren BuildingDefinition.zone auf diesen Typ verweist – so schnell, wie die Nachfrage es zulässt.
    /// </summary>
    [CreateAssetMenu(menuName = "EpochCity/Zone", fileName = "Zone_")]
    public class ZoneDefinition : ScriptableObject
    {
        public string displayName;
        [Tooltip("Kurzer Name für den Nachfrage-Balken (leer = displayName)")]
        public string shortName;
        [Tooltip("Erst nutzbar, wenn diese Technologie erforscht ist (leer = immer)")]
        public TechnologyDefinition requiredTechnology;

        [Header("Wohnzonen nach Stufen")]
        [Tooltip("Hier wachsen die Gebäude dieser Zone (leer = eigene Gebäude mit BuildingDefinition.zone = diese Zone), " +
                 "z. B. Handwerkerzone mit den Häusern der Bauernzone")]
        public ZoneDefinition buildingsOf;
        [Tooltip("Höchste Bevölkerungsstufe, bis zu der Häuser hier aufsteigen (0 = ohne Grenze)")]
        [Min(0)] public int maxTier;

        /// <summary>Zone, deren Gebäude hier wachsen.</summary>
        public ZoneDefinition BuildingZone => buildingsOf != null ? buildingsOf : this;

        /// <summary>Name für den Nachfrage-Balken.</summary>
        public string ShortName => string.IsNullOrEmpty(shortName) ? displayName : shortName;

        [Tooltip("Flache Kachel für genau ein Rasterfeld, zeigt die Zone an")]
        public GameObject tilePrefab;
        [Tooltip("Farbe des Nachfrage-Balkens")]
        public Color demandColor = Color.white;

        [Header("Nachfrage")]
        public ZoneDemandKind demandKind;

        [Tooltip("Wohnen: Belegung (Einwohner / Wohnplätze), ab der Nachfrage entsteht")]
        [Range(0f, 1f)] public float occupancyForNoDemand = 0.5f;
        [Tooltip("Wohnen: Belegung, bei der die Nachfrage voll ist")]
        [Range(0f, 1f)] public float occupancyForFullDemand = 0.9f;
        [Tooltip("Wohnen: Unter so vielen Wohnplätzen gilt mindestens die Startnachfrage, damit eine Stadt beginnen kann")]
        [Min(0)] public int starterCapacity = 24;
        [Tooltip("Wohnen: Mindestnachfrage, solange die Startkapazität nicht erreicht ist")]
        [Range(0f, 1f)] public float starterDemand = 0.6f;

        [Tooltip("Gewerbe: so viele Einwohner tragen ein Gewerbefeld")]
        [Min(1f)] public float residentsPerCell = 20f;
    }
}
