using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Prüft, ob die nächste Epoche verdient ist (Schlüsseltechnologien, Einwohner und Gebäude-Meilensteine),
    /// und führt den Wechsel auf Wunsch des Spielers aus. Der Wechsel stellt nur die Epoche um:
    /// Gebäude, Technologien und Lager bleiben, wie sie sind.
    /// </summary>
    public class EpochProgression : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [Tooltip("Alle Epochen des Spiels; die Reihenfolge ergibt sich aus EpochDefinition.order")]
        public EpochDefinition[] epochs;

        /// <summary>Die Epoche mit der kleinsten Ordnung über der aktuellen, sonst null.</summary>
        public EpochDefinition NextEpoch
        {
            get
            {
                if (city == null || epochs == null) return null;
                int currentOrder = city.currentEpoch != null ? city.currentEpoch.order : 0;
                EpochDefinition next = null;
                foreach (var e in epochs)
                    if (e != null && e.order > currentOrder && (next == null || e.order < next.order))
                        next = e;
                return next;
            }
        }

        public int CountKeyTechnologies(EpochDefinition epoch)
        {
            int count = 0;
            if (epoch == null || epoch.keyTechnologies == null) return count;
            foreach (var t in epoch.keyTechnologies)
                if (t != null && city.UnlockedTechnologies.Contains(t)) count++;
            return count;
        }

        public int CountBuildings(BuildingDefinition def)
        {
            int count = 0;
            foreach (var b in city.Buildings)
                if (b.Definition == def) count++;
            return count;
        }

        /// <summary>Einwohner aller Wohnhäuser (Bevölkerungs-Meilenstein).</summary>
        public int CountPopulation()
        {
            int residents = 0;
            foreach (var b in city.Buildings) residents += b.Residents;
            return residents;
        }

        public bool RequirementsMet(EpochDefinition epoch)
        {
            if (epoch == null || city == null) return false;
            if (CountKeyTechnologies(epoch) < epoch.requiredKeyTechnologies) return false;
            if (CountPopulation() < epoch.requiredPopulation) return false;
            if (epoch.milestones != null)
                foreach (var m in epoch.milestones)
                    if (m.building != null && CountBuildings(m.building) < m.count) return false;
            return true;
        }

        public bool CanAdvance => RequirementsMet(NextEpoch);

        public bool TryAdvance()
        {
            var next = NextEpoch;
            if (!RequirementsMet(next)) return false;
            city.SetEpoch(next);
            return true;
        }
    }
}
