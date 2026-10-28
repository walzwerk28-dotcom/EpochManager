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

        /// <summary>
        /// Lässt sich das Gebäude hier bauen? Freie Fläche, Epoche erreicht und mindestens ein angrenzendes
        /// Straßenfeld – jedes Gebäude braucht beim Bau einen Straßenanschluss. Geld prüft TryPlace.
        /// </summary>
        public bool CanPlace(BuildingDefinition def, Vector2Int origin) =>
            def != null && IsEpochReached(def.epoch) && Grid.CanPlace(def, origin) && Grid.TouchesRoad(origin, def.size);

        public bool TryPlace(BuildingDefinition def, Vector2Int origin, out BuildingInstance placed)
        {
            placed = null;
            if (def == null || Money < def.buildCost || !CanPlace(def, origin)) return false;

            Money -= def.buildCost;
            placed = Place(def, origin);
            return true;
        }

        /// <summary>
        /// Lässt ein Zonengebäude wachsen: kostenlos (private Bauherren), nur auf Feldern seiner Zone
        /// und wie jedes Gebäude nur mit angrenzender Straße.
        /// </summary>
        public bool TryGrow(BuildingDefinition def, Vector2Int origin, out BuildingInstance grown)
        {
            grown = null;
            if (def == null || def.zone == null || !CanPlace(def, origin)) return false;
            for (int x = 0; x < def.size.x; x++)
                for (int y = 0; y < def.size.y; y++)
                    if (Grid.GetZone(new Vector2Int(origin.x + x, origin.y + y)) != def.zone) return false;

            grown = Place(def, origin);
            return true;
        }

        /// <summary>Summe der Wohnplätze aller Wohngebäude, inklusive Zusatzkapazität durch Technologien.</summary>
        public int HousingCapacity
        {
            get
            {
                int sum = 0;
                foreach (var b in Buildings)
                    if (b.Definition.category == BuildingCategory.Housing)
                        sum += b.Definition.GetCapacity(UnlockedTechnologies);
                return sum;
            }
        }

        private BuildingInstance Place(BuildingDefinition def, Vector2Int origin)
        {
            var placed = new BuildingInstance(def, origin, currentEpoch);

            // Ein einzeln gesetztes Gebäude verdrängt die Zone unter sich; Zonengebäude behalten sie
            if (def.zone == null)
                for (int x = 0; x < def.size.x; x++)
                    for (int y = 0; y < def.size.y; y++)
                        ClearZone(new Vector2Int(origin.x + x, origin.y + y));

            Grid.Place(placed);
            Buildings.Add(placed);

            if (def.prefab != null)
                placed.View = Instantiate(def.prefab, GetFootprintCenter(def, origin), Quaternion.identity, transform);
            RefreshConnections();
            return placed;
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
            RefreshConnections();
            return true;
        }

        // ---------------------------------------------------------------- Straßen

        /// <summary>Summe des Straßenunterhalts pro Spielmonat.</summary>
        public float RoadUpkeepPerMonth { get; private set; }

        /// <summary>Wird ausgelöst, nachdem der Anschluss aller Gebäude neu berechnet wurde.</summary>
        public event Action ConnectionsChanged;

        private readonly Dictionary<Vector2Int, GameObject> roadViews = new Dictionary<Vector2Int, GameObject>();

        /// <summary>
        /// Prüft eine Strecke: alle Felder im Raster und ohne Gebäude, Epoche erreicht, genug Geld.
        /// Felder mit vorhandener Straße kosten nichts.
        /// </summary>
        public bool CanBuildRoad(RoadDefinition road, IReadOnlyList<Vector2Int> path, out float cost)
        {
            cost = 0f;
            if (road == null || path == null || path.Count == 0) return false;
            if (!IsEpochReached(road.epoch)) return false;
            foreach (var c in path)
            {
                if (!Grid.CanPlaceRoad(c)) return false;
                if (!Grid.HasRoad(c)) cost += road.costPerCell;
            }
            return Money >= cost;
        }

        public bool TryBuildRoad(RoadDefinition road, IReadOnlyList<Vector2Int> path)
        {
            if (!CanBuildRoad(road, path, out float cost)) return false;

            Money -= cost;
            foreach (var c in path)
            {
                if (Grid.HasRoad(c)) continue;
                ClearZone(c); // Straßen verdrängen Zonen
                Grid.SetRoad(c, road);
                RoadUpkeepPerMonth += road.upkeepPerCellPerMonth;
                if (road.prefab != null)
                    roadViews[c] = Instantiate(road.prefab, Grid.CellCenterToWorld(c, transform.position),
                        Quaternion.identity, transform);
            }
            RefreshConnections();
            return true;
        }

        public bool TryRemoveRoad(Vector2Int cell)
        {
            var road = Grid.GetRoad(cell);
            if (road == null) return false;

            Grid.SetRoad(cell, null);
            RoadUpkeepPerMonth = Mathf.Max(0f, RoadUpkeepPerMonth - road.upkeepPerCellPerMonth);
            if (roadViews.TryGetValue(cell, out var view))
            {
                Destroy(view);
                roadViews.Remove(cell);
            }
            RefreshConnections();
            return true;
        }

        // ---------------------------------------------------------------- Zonen

        /// <summary>Wird ausgelöst, nachdem Zonen gesetzt oder entfernt wurden.</summary>
        public event Action ZonesChanged;

        private readonly Dictionary<Vector2Int, GameObject> zoneViews = new Dictionary<Vector2Int, GameObject>();

        /// <summary>
        /// Setzt die Zone auf allen freien Feldern; mit zone = null wird sie entfernt (auch unter Gebäuden,
        /// die stehen bleiben). Zonen kosten nichts. Gibt die Anzahl der geänderten Felder zurück.
        /// </summary>
        public int SetZone(ZoneDefinition zone, IEnumerable<Vector2Int> cells)
        {
            int changed = 0;
            foreach (var c in cells)
            {
                if (zone == null)
                {
                    if (ClearZone(c)) changed++;
                    continue;
                }
                if (!Grid.CanZone(c) || Grid.GetZone(c) == zone) continue;

                ClearZone(c);
                Grid.SetZone(c, zone);
                if (zone.tilePrefab != null)
                    zoneViews[c] = Instantiate(zone.tilePrefab, Grid.CellCenterToWorld(c, transform.position),
                        Quaternion.identity, transform);
                changed++;
            }
            if (changed > 0) ZonesChanged?.Invoke();
            return changed;
        }

        private bool ClearZone(Vector2Int c)
        {
            if (Grid.GetZone(c) == null) return false;
            Grid.SetZone(c, null);
            if (zoneViews.TryGetValue(c, out var view))
            {
                Destroy(view);
                zoneViews.Remove(c);
            }
            return true;
        }

        /// <summary>Anbindungswerte über das Straßennetz, auch je Straßenfeld (für das Overlay).</summary>
        public ConnectionMap Connections { get; } = new ConnectionMap();

        /// <summary>Platzierte Straßenfelder und ihre Darstellung.</summary>
        public IReadOnlyDictionary<Vector2Int, GameObject> RoadViews => roadViews;

        /// <summary>
        /// Anschluss aller Gebäude neu berechnen. Läuft nur, wenn sich Straßen oder Gebäude ändern,
        /// nicht in jedem Simulationsschritt.
        /// </summary>
        public void RefreshConnections()
        {
            Connections.Recalculate(Grid, Buildings);
            ConnectionsChanged?.Invoke();
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
                Debug.LogWarning("Platzieren nicht möglich (Zelle belegt, außerhalb, keine Straße angrenzend oder zu wenig Geld).");
        }
    }
}
