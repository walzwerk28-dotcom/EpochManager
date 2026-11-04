using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>Feuerzustand eines Gebäudes (FireSimulation).</summary>
    public enum FireState { None, Burning, Damaged }

    /// <summary>Ein tatsächlich gebautes Gebäude in der Stadt.</summary>
    public class BuildingInstance
    {
        public BuildingDefinition Definition { get; }
        public Vector2Int Origin { get; }
        public EpochDefinition BuiltInEpoch { get; }
        public GameObject View { get; set; }

        /// <summary>Drehung um die Hochachse (0, 90, 180, 270): wohin die Vorderseite zeigt, siehe CityController.FrontSide.</summary>
        public float Facing { get; }

        /// <summary>Tatsächlich belegte Felder ab Origin (bei 90° / 270° Breite und Tiefe vertauscht).</summary>
        public Vector2Int Footprint => Definition.GetFootprint(Facing);

        /// <summary>Fortschritt des aktuellen Zyklus pro Funktion (in Sekunden).</summary>
        public Dictionary<BuildingFunction, float> Progress { get; } = new Dictionary<BuildingFunction, float>();

        /// <summary>Kurzer Status für die Anzeige, z. B. "läuft" oder "wartet: Holz".</summary>
        public string Status { get; set; } = "";
        /// <summary>Gewerbe: im letzten Zyklus verkaufte Waren („Weizen, Öl“; nicht gespeichert).</summary>
        public string LastSale { get; set; } = "";

        /// <summary>Anbindungswert von 0 bis 1; multipliziert die Produktionsgeschwindigkeit.</summary>
        public float Connection { get; set; }

        /// <summary>Grund für fehlende oder schwache Anbindung, z. B. "keine Straße".</summary>
        public string ConnectionIssue { get; set; } = "";

        /// <summary>Anteil des nötigen Rohstoff-Vorkommens im Umkreis (0–1, 1 ohne Bedarf); berechnet von CityController.RefreshConnections.</summary>
        public float DepositFactor { get; set; } = 1f;

        /// <summary>
        /// Nur Holzfäller und Förster (BuildingDefinition.HasWorkArea): Felder, auf denen gefällt bzw. gepflanzt wird
        /// (absolute Rasterfelder); sonst null. Gesetzt von CityController (Standard = Grundfläche plus Umkreis).
        /// </summary>
        public HashSet<Vector2Int> WorkArea { get; set; }

        /// <summary>Faktor auf die Arbeitsgeschwindigkeit durch den Weg zur Mitte des Arbeitsbereichs (1 = nah).</summary>
        public float TravelFactor { get; set; } = 1f;

        /// <summary>Grund für einen DepositFactor unter 1, z. B. „wenig Wald“.</summary>
        public string DepositIssue { get; set; } = "";

        /// <summary>Besetzter Anteil der Arbeitsplätze (0–1, 1 ohne Bedarf); berechnet von EconomySimulation.</summary>
        public float WorkforceFactor { get; set; } = 1f;

        /// <summary>Zyklen seit dem letzten Fällen (BuildingDefinition.cyclesPerFelling); nicht im Spielstand.</summary>
        public int CyclesSinceFelling { get; set; }

        /// <summary>Nur Wohngebäude: aktuelle Einwohner.</summary>
        public int Residents { get; set; }

        /// <summary>
        /// Aktuelle Bevölkerungsstufe der Bewohner (nur Wohnhäuser; Start = BuildingDefinition.populationTier).
        /// Steigt ein Haus auf, ändern sich Bedürfnisse und Steuern, Modell und Grundfläche bleiben.
        /// </summary>
        public PopulationTierDefinition Tier { get; set; }

        /// <summary>Monate in Folge mit erfüllten Aufstiegsbedingungen (positiv) bzw. Abstiegsbedingungen (negativ).</summary>
        public int TierMonths { get; set; }

        /// <summary>Warum das Haus gerade nicht auf den Aufstieg zuarbeitet, z. B. „braucht Bildung“ (leer = es arbeitet darauf zu).</summary>
        public string UpgradeIssue { get; set; } = "";

        /// <summary>Nur Wohngebäude: Zufriedenheit von 0 bis 1, einmal pro Spielmonat berechnet.</summary>
        public float Satisfaction { get; set; }

        /// <summary>Nur Wohngebäude: schlecht erfüllte Bedürfnisse, z. B. "Markt, Nahrung".</summary>
        public string MissingNeeds { get; set; } = "";

        /// <summary>Denkmalschutz: lässt sich nicht abreißen, bringt Kultur und Tourismus.</summary>
        public bool IsProtected { get; set; }

        /// <summary>Stillgelegt (wie in Anno): produziert nichts, bietet keine Dienste, zahlt nur einen Teil des Unterhalts.</summary>
        public bool IsPaused { get; set; }

        /// <summary>Feuer (FireSimulation): brennt oder ist beschädigt; arbeitet dann nicht und bietet keine Dienste.</summary>
        public FireState Fire { get; set; }

        /// <summary>Brennend: verbleibende Spielsekunden bis zum Ende des Brands; beschädigt: verbleibende Spielmonate.</summary>
        public float FireTimer { get; set; }

        /// <summary>Stillgelegt, brennend, beschädigt oder im Bau: produziert nichts und bietet keine Dienste.</summary>
        public bool IsOutOfService => IsPaused || Fire != FireState.None || UnderConstruction;

        /// <summary>Großbau: aktuelle Bauphase (0 … Anzahl Phasen; gleich der Anzahl = fertig).</summary>
        public int Stage { get; set; }
        /// <summary>In der aktuellen Phase schon gelieferte Waren.</summary>
        public Dictionary<ResourceDefinition, float> Delivered { get; } = new Dictionary<ResourceDefinition, float>();
        /// <summary>In der aktuellen Phase schon bezahltes Geld.</summary>
        public float StageMoney { get; set; }
        /// <summary>Spielsekunden in der aktuellen Phase.</summary>
        public float StageTimer { get; set; }
        /// <summary>Baustelle: Spielsekunden seit Baubeginn (BuildingDefinition.buildSeconds); neue Gebäude beginnen bei 0.</summary>
        public float BuildTimer { get; set; }
        /// <summary>Noch in der Bauzeit (nicht bei Großbauten mit Bauphasen, die nutzen Stage).</summary>
        public bool IsBuilding => !Definition.HasStages && BuildTimer < Definition.buildSeconds;
        /// <summary>Baustelle oder Großbau, der noch nicht fertig ist: arbeitet nicht, bietet keine Dienste und keinen Bonus.</summary>
        public bool UnderConstruction => IsBuilding || (Definition.HasStages && Stage < Definition.stages.Length);
        /// <summary>Faktor auf den Ertrag durch ein Gebäude im Umkreis (Gutshof), 1 ohne; CityController.RefreshBoosts.</summary>
        public float Boost { get; set; } = 1f;
        /// <summary>Gebäude, das den Bonus gibt (für die Anzeige), sonst null.</summary>
        public BuildingDefinition BoostSource { get; set; }
        /// <summary>Wohnhaus: Zufriedenheitsbonus durch einen Wunderbau im Umkreis (im letzten Monat).</summary>
        public float WonderBonus { get; set; }
        /// <summary>Wohnhaus: Zufriedenheitsbonus durch einen Park im Umkreis (im letzten Monat) und der Park dazu.</summary>
        public float ParkBonus { get; set; }
        public BuildingDefinition ParkSource { get; set; }

        public BuildingInstance(BuildingDefinition definition, Vector2Int origin, EpochDefinition builtIn, float facing = 0f)
        {
            Definition = definition;
            Origin = origin;
            BuiltInEpoch = builtIn;
            Facing = facing;
            Tier = definition.populationTier;
        }
    }
}
