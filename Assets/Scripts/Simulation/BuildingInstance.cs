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

        /// <summary>Fortschritt des aktuellen Zyklus pro Funktion (in Sekunden).</summary>
        public Dictionary<BuildingFunction, float> Progress { get; } = new Dictionary<BuildingFunction, float>();

        /// <summary>Kurzer Status für die Anzeige, z. B. "läuft" oder "wartet: Holz".</summary>
        public string Status { get; set; } = "";

        /// <summary>Denkmalschutz: lässt sich nicht abreißen, bringt Kultur und Tourismus.</summary>
        public bool IsProtected { get; set; }

        public BuildingInstance(BuildingDefinition definition, Vector2Int origin, EpochDefinition builtIn)
        {
            Definition = definition;
            Origin = origin;
            BuiltInEpoch = builtIn;
        }
    }
}
