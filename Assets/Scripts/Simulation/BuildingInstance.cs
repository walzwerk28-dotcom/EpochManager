using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
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

        /// <summary>Anbindungswert von 0 bis 1; multipliziert die Produktionsgeschwindigkeit.</summary>
        public float Connection { get; set; }

        /// <summary>Grund für fehlende oder schwache Anbindung, z. B. "keine Straße".</summary>
        public string ConnectionIssue { get; set; } = "";

        /// <summary>Anteil des nötigen Rohstoff-Vorkommens im Umkreis (0–1, 1 ohne Bedarf); berechnet von CityController.RefreshConnections.</summary>
        public float DepositFactor { get; set; } = 1f;

        /// <summary>Grund für einen DepositFactor unter 1, z. B. „wenig Wald“.</summary>
        public string DepositIssue { get; set; } = "";

        /// <summary>Besetzter Anteil der Arbeitsplätze (0–1, 1 ohne Bedarf); berechnet von EconomySimulation.</summary>
        public float WorkforceFactor { get; set; } = 1f;

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
