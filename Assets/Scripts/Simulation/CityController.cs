using System;
using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Hält den Zustand der Stadt: Raster, Gebäude, freigeschaltete Technologien.
    /// Die Simulation (Zahlen) ist hier getrennt von der Darstellung (Prefabs).
    /// </summary>
    public class CityController : MonoBehaviour
    {
        [Header("Raster")]
        public int gridWidth = 64;
        public int gridHeight = 64;
        public float cellSize = 2f;

        [Header("Start")]
        public EpochDefinition currentEpoch;
        public TechnologyDefinition[] startTechnologies;
        public float startMoney = 1000f;

        [Header("Debug (Rechtsklick auf die Komponente -> Debug: Platzieren)")]
        public BuildingDefinition debugBuilding;
        public Vector2Int debugCell;

        public GridMap Grid { get; private set; }
        public float Money { get; private set; }

        /// <summary>Warenvorrat. Im Prototyp gibt es ein gemeinsames Lager; regionale Lager folgen später.</summary>
        public ResourceStock Stock { get; } = new ResourceStock();

        public void AddMoney(float amount) => Money += amount;
        public HashSet<TechnologyDefinition> UnlockedTechnologies { get; } = new HashSet<TechnologyDefinition>();
        public List<BuildingInstance> Buildings { get; } = new List<BuildingInstance>();

        private void Awake()
        {
            Grid = new GridMap(gridWidth, gridHeight, cellSize);
            Money = startMoney;
            if (startTechnologies != null)
                foreach (var t in startTechnologies)
                    if (t != null) UnlockedTechnologies.Add(t);
        }

        /// <summary>Ist diese Epoche schon erreicht? Inhalte ohne Epoche gelten immer als erreicht.</summary>
        public bool IsEpochReached(EpochDefinition epoch) =>
            epoch == null || currentEpoch == null || epoch.order <= currentEpoch.order;

        /// <summary>Wird nach einem Epochenwechsel ausgelöst. Bestehende Gebäude bleiben unverändert.</summary>
        public event Action<EpochDefinition> EpochChanged;

        public void SetEpoch(EpochDefinition epoch)
        {
            if (epoch == null || epoch == currentEpoch) return;
            currentEpoch = epoch;
            EpochChanged?.Invoke(epoch);
        }

        public bool TryPlace(BuildingDefinition def, Vector2Int origin, out BuildingInstance placed)
        {
            placed = null;
            if (def == null) return false;
            if (!IsEpochReached(def.epoch)) return false;
            if (Money < def.buildCost) return false;
            if (!Grid.CanPlace(def, origin)) return false;

            Money -= def.buildCost;
            placed = new BuildingInstance(def, origin, currentEpoch);
            Grid.Place(placed);
            Buildings.Add(placed);

            if (def.prefab != null)
                placed.View = Instantiate(def.prefab, GetFootprintCenter(def, origin), Quaternion.identity, transform);
            return true;
        }

        /// <summary>Weltposition der Mitte der vom Gebäude belegten Fläche.</summary>
        public Vector3 GetFootprintCenter(BuildingDefinition def, Vector2Int origin)
        {
            var basePos = Grid.CellToWorld(origin, transform.position);
            var offset = new Vector3(def.size.x * cellSize * 0.5f, 0f, def.size.y * cellSize * 0.5f);
            return basePos + offset;
        }

        /// <summary>Abriss. Gebäude unter Denkmalschutz lassen sich nicht abreißen.</summary>
        public bool TryRemove(BuildingInstance inst)
        {
            if (inst == null || inst.IsProtected) return false;
            Grid.Remove(inst);
            Buildings.Remove(inst);
            if (inst.View != null) Destroy(inst.View);
            return true;
        }

        /// <summary>Wird ausgelöst, sobald eine Technologie neu freigeschaltet ist.</summary>
        public event Action<TechnologyDefinition> TechnologyUnlocked;

        public bool UnlockTechnology(TechnologyDefinition tech)
        {
            if (tech == null || UnlockedTechnologies.Contains(tech)) return false;
            if (!tech.PrerequisitesMet(UnlockedTechnologies)) return false;
            UnlockedTechnologies.Add(tech); // alle bestehenden Gebäude profitieren automatisch
            TechnologyUnlocked?.Invoke(tech);
            return true;
        }

        /// <summary>Multiplikator, den dieses Gebäude durch die aktuellen Technologien hat.</summary>
        public float GetMultiplier(BuildingInstance inst) =>
            inst.Definition.GetOutputMultiplier(UnlockedTechnologies);

        [ContextMenu("Debug: Platzieren")]
        private void DebugPlace()
        {
            if (Grid == null) Awake();
            if (TryPlace(debugBuilding, debugCell, out var b))
                Debug.Log($"Gebaut: {b.Definition.displayName} bei {b.Origin}, Multiplikator {GetMultiplier(b):0.00}");
            else
                Debug.LogWarning("Platzieren nicht möglich (Zelle belegt, außerhalb oder zu wenig Geld).");
        }
    }
}
