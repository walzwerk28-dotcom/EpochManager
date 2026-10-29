using System;
using UnityEngine;

namespace EpochCity
{
    /// <summary>Stadt-Meilenstein: mindestens so viele Gebäude dieses Typs stehen in der Stadt.</summary>
    [Serializable]
    public struct EpochMilestone
    {
        public BuildingDefinition building;
        [Min(1)] public int count;
    }

    /// <summary>Eine Epoche (Antike, Mittelalter, ...). Reine Daten.</summary>
    [CreateAssetMenu(menuName = "EpochCity/Epoch", fileName = "Epoch_")]
    public class EpochDefinition : ScriptableObject
    {
        [Tooltip("Reihenfolge: 1 = Antike, 2 = Mittelalter, ...")]
        public int order = 1;
        public string displayName;
        [Tooltip("Rein kosmetischer Name der Währung dieser Epoche")]
        public string currencyName = "Münzen";
        [TextArea] public string description;

        [Header("Bedingungen, um diese Epoche zu erreichen")]
        [Tooltip("Schlüsseltechnologien der vorherigen Epoche")]
        public TechnologyDefinition[] keyTechnologies;
        [Tooltip("Wie viele der Schlüsseltechnologien nötig sind (ein Teil genügt)")]
        [Min(0)] public int requiredKeyTechnologies;
        public EpochMilestone[] milestones;
        [Tooltip("Mindestens so viele Einwohner (Bevölkerungs-Meilenstein, 0 = keiner)")]
        [Min(0)] public int requiredPopulation;
    }
}
