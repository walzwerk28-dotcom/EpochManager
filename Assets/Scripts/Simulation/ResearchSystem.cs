using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Gemeinsamer Forschungspool: Gebäude produzieren die Forschungs-Ressource ins Lager,
    /// von dort fließt sie in das gewählte Projekt. Ohne Projekt sammelt sie sich im Lager.
    /// Es läuft immer höchstens ein Projekt; angefangener Fortschritt bleibt beim Wechsel erhalten.
    /// </summary>
    public class ResearchSystem : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [Tooltip("Ressource, die als Forschungspunkte zählt")]
        public ResourceDefinition researchResource;
        [Tooltip("Alle Technologien des Spiels (Reihenfolge = Anzeige)")]
        public TechnologyDefinition[] technologies;

        public TechnologyDefinition Current { get; private set; }

        private readonly Dictionary<TechnologyDefinition, float> progress = new Dictionary<TechnologyDefinition, float>();

        public float GetProgress(TechnologyDefinition tech) =>
            tech != null && progress.TryGetValue(tech, out var value) ? value : 0f;

        public bool IsUnlocked(TechnologyDefinition tech) => city.UnlockedTechnologies.Contains(tech);

        /// <summary>Noch nicht erforscht, Voraussetzungen erfüllt und Epoche bereits erreicht.</summary>
        public bool CanResearch(TechnologyDefinition tech)
        {
            if (tech == null || city == null || IsUnlocked(tech)) return false;
            if (!tech.PrerequisitesMet(city.UnlockedTechnologies)) return false;
            return city.IsEpochReached(tech.epoch);
        }

        public bool Select(TechnologyDefinition tech)
        {
            if (!CanResearch(tech)) return false;
            Current = tech;
            return true;
        }

        private void Update()
        {
            if (city == null || researchResource == null || Current == null) return;

            float pool = city.Stock.Get(researchResource);
            float needed = Current.researchCost - GetProgress(Current);
            float take = Mathf.Min(pool, needed);
            if (take > 0f)
            {
                city.Stock.Add(researchResource, -take);
                progress[Current] = GetProgress(Current) + take;
            }

            if (GetProgress(Current) >= Current.researchCost)
            {
                city.UnlockTechnology(Current);
                progress.Remove(Current);
                Current = null;
            }
        }
    }
}
