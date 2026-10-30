using System;
using UnityEngine;

namespace EpochCity
{
    /// <summary>Anzeigename einer Bevölkerungsstufe ab einer bestimmten Epoche.</summary>
    [Serializable]
    public struct EpochName
    {
        public EpochDefinition epoch;
        public string name;
    }

    /// <summary>
    /// Eine Bevölkerungsstufe (1 bis 3). Sie läuft durch alle Epochen; nur Name und Bedürfnisse ändern sich.
    /// </summary>
    [CreateAssetMenu(menuName = "EpochCity/Population Tier", fileName = "Tier_")]
    public class PopulationTierDefinition : ScriptableObject
    {
        [Min(1)] public int tier = 1;
        [Tooltip("Name, falls für die Epoche keiner eingetragen ist")]
        public string displayName;
        public EpochName[] namesByEpoch;
        public NeedDefinition[] needs;

        [Header("Aufstieg (pro Haus, Modell bleibt)")]
        [Tooltip("Stufe, in die ein Haus aufsteigen kann (leer = höchste Stufe)")]
        public PopulationTierDefinition nextTier;
        [Tooltip("Zufriedenheit, ab der ein Haus auf den Aufstieg zuarbeitet")]
        [Range(0f, 1f)] public float upgradeSatisfaction = 0.75f;
        [Tooltip("So viele Monate in Folge müssen die Bedingungen erfüllt sein")]
        [Min(1)] public int upgradeMonths = 3;
        [Tooltip("Zusätzlich nötig für den Aufstieg, z. B. Bildung (Schule in Reichweite)")]
        public NeedDefinition[] upgradeRequires;

        [Header("Abstieg (zurück in die Stufe darunter)")]
        [Tooltip("Unter dieser Zufriedenheit arbeitet ein Haus dieser Stufe auf den Abstieg zu")]
        [Range(0f, 1f)] public float downgradeSatisfaction = 0.4f;
        [Min(1)] public int downgradeMonths = 3;

        [Header("Steuern")]
        [Min(0f)] public float taxPerResidentPerMonth = 1f;
        [Tooltip("Faktor auf die Zufriedenheit je Steuersatz: x = Satz (1 = 100 %), y = Faktor. " +
                 "Kleine Erhöhungen fallen kaum auf, bei hohen Sätzen kippt die Stimmung.")]
        public AnimationCurve satisfactionByTaxRate = new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(1f, 0.95f), new Keyframe(1.5f, 0.75f), new Keyframe(2f, 0.35f));

        public float TaxFactor(float rate) =>
            satisfactionByTaxRate != null && satisfactionByTaxRate.length > 0
                ? Mathf.Clamp01(satisfactionByTaxRate.Evaluate(rate))
                : 1f;

        /// <summary>Name aus der neuesten Epoche, die höchstens der aktuellen entspricht.</summary>
        public string GetName(EpochDefinition current)
        {
            string result = displayName;
            int best = int.MinValue;
            if (namesByEpoch == null) return result;
            foreach (var entry in namesByEpoch)
            {
                if (entry.epoch == null || string.IsNullOrEmpty(entry.name)) continue;
                if (current != null && entry.epoch.order > current.order) continue;
                if (entry.epoch.order > best)
                {
                    best = entry.epoch.order;
                    result = entry.name;
                }
            }
            return result;
        }
    }
}
