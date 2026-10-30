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
        [Tooltip("Startvorrat im Lager, z. B. Baumaterial für die ersten Gebäude")]
        public ResourceAmount[] startStock;

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
            if (startStock != null)
                foreach (var a in startStock)
                    if (a.resource != null && !a.resource.isCurrency) Stock.Add(a.resource, a.amount);
            if (startTechnologies != null)
                foreach (var t in startTechnologies)
                    if (t != null) UnlockedTechnologies.Add(t);
        }

        /// <summary>Ist diese Epoche schon erreicht? Inhalte ohne Epoche gelten immer als erreicht.</summary>
        public bool IsEpochReached(EpochDefinition epoch) =>
            epoch == null || currentEpoch == null || epoch.order <= currentEpoch.order;

        /// <summary>Ist die Technologie erforscht? Keine Technologie gilt als erforscht.</summary>
        public bool IsUnlocked(TechnologyDefinition tech) => tech == null || UnlockedTechnologies.Contains(tech);

        /// <summary>Darf dieses Gebäude gebaut werden oder wachsen: Epoche erreicht und nötige Technologie erforscht.</summary>
        public bool IsAvailable(BuildingDefinition def) =>
            def != null && IsEpochReached(def.epoch) && IsUnlocked(def.requiredTechnology);

        public bool IsAvailable(RoadDefinition road) =>
            road != null && IsEpochReached(road.epoch) && IsUnlocked(road.requiredTechnology);

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
        /// Straßenfeld – jedes Gebäude braucht beim Bau einen Straßenanschluss. Geld und Material prüft CanAfford.
        /// </summary>
        public bool CanPlace(BuildingDefinition def, Vector2Int origin) =>
            IsAvailable(def) && Grid.CanPlace(def, origin) && Grid.TouchesRoad(origin, def.size);

        public bool TryPlace(BuildingDefinition def, Vector2Int origin, out BuildingInstance placed)
        {
            placed = null;
            if (def == null || !CanAfford(def) || !CanPlace(def, origin)) return false;

            Pay(def.buildCost, def.materialCosts);
            placed = Place(def, origin);
            return true;
        }

        // ---------------------------------------------------------------- Baukosten

        /// <summary>Reichen Geld und Baumaterial für dieses Gebäude?</summary>
        public bool CanAfford(BuildingDefinition def) =>
            def != null && GetMissingCost(def.buildCost, def.materialCosts) == null;

        /// <summary>
        /// Was fehlt für diese Kosten (Geld plus Material × Anzahl), z. B. „3 Stein, 12 Drachmen“; null, wenn alles reicht.
        /// </summary>
        public string GetMissingCost(float money, ResourceAmount[] materials, float count = 1f)
        {
            var missing = new List<string>();
            if (materials != null)
                foreach (var m in materials)
                {
                    if (m.resource == null || m.resource.isCurrency) continue;
                    float lack = m.amount * count - Stock.Get(m.resource);
                    if (lack > 0.001f) missing.Add($"{lack:0.##} {m.resource.displayName}");
                }
            float totalMoney = money + CurrencyIn(materials) * count;
            if (Money < totalMoney)
                missing.Add($"{totalMoney - Money:0} {(currentEpoch != null ? currentEpoch.currencyName : "Geld")}");
            return missing.Count > 0 ? string.Join(", ", missing) : null;
        }

        /// <summary>Zieht Geld und Material (× Anzahl) ab; vorher mit GetMissingCost prüfen.</summary>
        private void Pay(float money, ResourceAmount[] materials, float count = 1f)
        {
            Money -= money + CurrencyIn(materials) * count;
            if (materials == null) return;
            foreach (var m in materials)
                if (m.resource != null && !m.resource.isCurrency) Stock.Add(m.resource, -m.amount * count);
        }

        /// <summary>Geld, das als Material eingetragen ist (Ressource mit isCurrency), zählt zur Stadtkasse.</summary>
        private static float CurrencyIn(ResourceAmount[] materials)
        {
            float sum = 0f;
            if (materials != null)
                foreach (var m in materials)
                    if (m.resource != null && m.resource.isCurrency) sum += m.amount;
            return sum;
        }

        /// <summary>
        /// Lässt ein Zonengebäude wachsen: kostenlos (private Bauherren), nur auf Feldern seiner Zone
        /// und wie jedes Gebäude nur mit angrenzender Straße.
        /// </summary>
        public bool TryGrow(BuildingDefinition def, Vector2Int origin, out BuildingInstance grown) =>
            TryGrow(def, origin, GetFacingAngle(def, origin), out grown);

        /// <summary>Wie TryGrow, mit vorgegebener Drehung (die Grundfläche dreht sich mit, siehe GetFootprint).</summary>
        public bool TryGrow(BuildingDefinition def, Vector2Int origin, float facing, out BuildingInstance grown)
        {
            grown = null;
            if (!CanGrow(def, origin, facing)) return false;
            grown = Place(def, origin, currentEpoch, facing, refresh: true);
            return true;
        }

        public bool CanGrow(BuildingDefinition def, Vector2Int origin) =>
            def != null && CanGrow(def, origin, GetFacingAngle(def, origin));

        /// <summary>
        /// Passt das Zonengebäude mit dieser Drehung hier hin? Gedrehte Grundfläche frei und ganz in seiner Zone,
        /// Epoche erreicht, und an der Vorderseite liegt eine Straße.
        /// </summary>
        public bool CanGrow(BuildingDefinition def, Vector2Int origin, float facing)
        {
            if (def == null || def.zone == null || !IsAvailable(def)) return false;
            var footprint = def.GetFootprint(facing);
            if (!Grid.CanPlace(footprint, origin) || !Grid.SideHasRoad(origin, footprint, FrontSide(facing))) return false;
            for (int x = 0; x < footprint.x; x++)
                for (int y = 0; y < footprint.y; y++)
                    if (Grid.GetZone(new Vector2Int(origin.x + x, origin.y + y)) != def.zone) return false;
            return true;
        }

        /// <summary>Die vier Drehungen um die Hochachse (Vorderseite nach Süden, Westen, Norden, Osten).</summary>
        public static readonly float[] Facings = { 0f, 90f, 180f, 270f };

        /// <summary>Rasterrichtung, in die die Vorderseite (im Modell −z) bei dieser Drehung zeigt.</summary>
        public static Vector2Int FrontSide(float facing)
        {
            switch (Mathf.RoundToInt(facing / 90f) & 3)
            {
                case 1: return Vector2Int.left;
                case 2: return Vector2Int.up;
                case 3: return Vector2Int.right;
                default: return Vector2Int.down;
            }
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

        private BuildingInstance Place(BuildingDefinition def, Vector2Int origin) =>
            Place(def, origin, currentEpoch, GetFacingAngle(def, origin), refresh: true);

        private BuildingInstance Place(BuildingDefinition def, Vector2Int origin, EpochDefinition builtIn, float facing,
            bool refresh)
        {
            var placed = new BuildingInstance(def, origin, builtIn, facing);
            var footprint = placed.Footprint;

            // Ein einzeln gesetztes Gebäude verdrängt die Zone unter sich; Zonengebäude behalten sie
            if (def.zone == null)
                for (int x = 0; x < footprint.x; x++)
                    for (int y = 0; y < footprint.y; y++)
                        ClearZone(new Vector2Int(origin.x + x, origin.y + y));

            Grid.Place(placed);
            Buildings.Add(placed);

            if (def.prefab != null)
                placed.View = Instantiate(def.prefab, GetFootprintCenter(footprint, origin),
                    Quaternion.Euler(0f, facing, 0f), transform);
            if (refresh) RefreshConnections();
            return placed;
        }

        // ---------------------------------------------------------------- Laden

        /// <summary>
        /// Leert die Stadt vor dem Laden eines Spielstands: alle Gebäude (auch unter Denkmalschutz), Straßen und
        /// Zonen, ohne Erstattung. Geld, Epoche, Technologien und Lager setzt danach RestoreState.
        /// </summary>
        public void ClearForLoad()
        {
            foreach (var b in Buildings)
            {
                Grid.Remove(b);
                if (b.View != null) Destroy(b.View);
            }
            Buildings.Clear();

            var removed = new List<Vector2Int>();
            for (int x = 0; x < Grid.Width; x++)
                for (int y = 0; y < Grid.Height; y++)
                {
                    var c = new Vector2Int(x, y);
                    if (Grid.HasRoad(c)) removed.Add(c);
                }
            foreach (var c in removed) Grid.SetRoad(c, null);
            foreach (var view in roadViews.Values) Destroy(view);
            roadViews.Clear();
            RoadUpkeepPerMonth = 0f;

            foreach (var c in new List<Vector2Int>(Grid.ZonedCells)) ClearZone(c);

            Stock.Clear();
            RefreshConnections();
            if (removed.Count > 0) RoadsChanged?.Invoke(removed);
            ZonesChanged?.Invoke();
        }

        /// <summary>Geld, Epoche und Technologien aus einem Spielstand übernehmen (ohne Freischalt-Ereignisse).</summary>
        public void RestoreState(float money, EpochDefinition epoch, IEnumerable<TechnologyDefinition> technologies)
        {
            Money = money;
            UnlockedTechnologies.Clear();
            foreach (var t in technologies)
                if (t != null) UnlockedTechnologies.Add(t);
            if (epoch != null && epoch != currentEpoch)
            {
                currentEpoch = epoch;
                EpochChanged?.Invoke(epoch);
            }
        }

        /// <summary>Straßen aus einem Spielstand setzen: kostenlos, ohne Epochenprüfung, ein gemeinsames Ereignis.</summary>
        public void RestoreRoads(IEnumerable<(Vector2Int cell, RoadDefinition road)> roads)
        {
            var built = new List<Vector2Int>();
            foreach (var (c, road) in roads)
            {
                if (road == null || !Grid.CanPlaceRoad(c) || Grid.HasRoad(c)) continue;
                AddRoad(c, road);
                built.Add(c);
            }
            RefreshConnections();
            if (built.Count > 0) RoadsChanged?.Invoke(built);
        }

        /// <summary>
        /// Gebäude aus einem Spielstand setzen: kostenlos, ohne Epochen- und Straßenprüfung (der Stand war beim
        /// Speichern gültig), mit der gespeicherten Bau-Epoche und Drehung (negativ = wie beim Bauen bestimmen,
        /// für Spielstände ohne Drehung). Null, wenn die Fläche belegt ist.
        /// Anbindungen berechnet erst RefreshConnections nach dem letzten Gebäude.
        /// </summary>
        public BuildingInstance RestoreBuilding(BuildingDefinition def, Vector2Int origin, EpochDefinition builtIn,
            float facing = -1f)
        {
            if (def == null) return null;
            if (facing < 0f) facing = GetFacingAngle(def, origin);
            if (!Grid.CanPlace(def.GetFootprint(facing), origin)) return null;
            return Place(def, origin, builtIn, facing, refresh: false);
        }

        /// <summary>
        /// Drehung um die Hochachse, damit die Vorderseite eines Modells (im Prefab nach −z/Süden) zu einer
        /// angrenzenden Straße zeigt. Bevorzugt Süden, dann Osten, Westen, Norden. Nur bei quadratischer
        /// Grundfläche, weil sich sonst die belegten Felder ändern; rechteckige Zonengebäude wählen ihre Drehung
        /// beim Wachsen selbst (ZoneGrowth), einzeln gesetzte bleiben bei 0°.
        /// </summary>
        public float GetFacingAngle(BuildingDefinition def, Vector2Int origin)
        {
            if (def == null || def.size.x != def.size.y || Grid == null) return 0f;
            if (Grid.SideHasRoad(origin, def.size, Vector2Int.down)) return 0f;
            if (Grid.SideHasRoad(origin, def.size, Vector2Int.right)) return 270f;
            if (Grid.SideHasRoad(origin, def.size, Vector2Int.left)) return 90f;
            if (Grid.SideHasRoad(origin, def.size, Vector2Int.up)) return 180f;
            return 0f;
        }

        /// <summary>Weltposition der Mitte der vom Gebäude belegten Fläche.</summary>
        public Vector3 GetFootprintCenter(BuildingDefinition def, Vector2Int origin) =>
            GetFootprintCenter(def.size, origin);

        public Vector3 GetFootprintCenter(Vector2Int footprint, Vector2Int origin)
        {
            var basePos = Grid.CellToWorld(origin, transform.position);
            var offset = new Vector3(footprint.x * cellSize * 0.5f, 0f, footprint.y * cellSize * 0.5f);
            return basePos + offset;
        }

        /// <summary>Denkmalschutz setzen oder aufheben; geschützte Gebäude lassen sich nicht abreißen.</summary>
        public void SetProtected(BuildingInstance inst, bool isProtected)
        {
            if (inst != null) inst.IsProtected = isProtected;
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

        /// <summary>Wird mit den gebauten oder entfernten Straßenfeldern ausgelöst (für die Darstellung).</summary>
        public event Action<IReadOnlyList<Vector2Int>> RoadsChanged;

        private readonly Dictionary<Vector2Int, GameObject> roadViews = new Dictionary<Vector2Int, GameObject>();

        /// <summary>
        /// Prüft eine Strecke: alle Felder im Raster und ohne Gebäude, Epoche erreicht, genug Geld und Material.
        /// Felder mit vorhandener Straße kosten nichts.
        /// </summary>
        public bool CanBuildRoad(RoadDefinition road, IReadOnlyList<Vector2Int> path, out float cost) =>
            CanBuildRoad(road, path, out cost, out int newCells) &&
            GetMissingCost(cost, road.materialCostsPerCell, newCells) == null;

        /// <summary>Wie CanBuildRoad, aber ohne Kostenprüfung; liefert Geldkosten und Anzahl neuer Felder.</summary>
        private bool CanBuildRoad(RoadDefinition road, IReadOnlyList<Vector2Int> path, out float cost, out int newCells)
        {
            cost = 0f;
            newCells = 0;
            if (road == null || path == null || path.Count == 0) return false;
            if (!IsAvailable(road)) return false;
            foreach (var c in path)
            {
                if (!Grid.CanPlaceRoad(c)) return false;
                if (Grid.HasRoad(c)) continue;
                cost += road.costPerCell;
                newCells++;
            }
            return true;
        }

        public bool TryBuildRoad(RoadDefinition road, IReadOnlyList<Vector2Int> path)
        {
            if (!CanBuildRoad(road, path, out float cost, out int newCells) ||
                GetMissingCost(cost, road.materialCostsPerCell, newCells) != null) return false;

            Pay(cost, road.materialCostsPerCell, newCells);
            var built = new List<Vector2Int>();
            foreach (var c in path)
            {
                if (Grid.HasRoad(c)) continue;
                AddRoad(c, road);
                built.Add(c);
            }
            RefreshConnections();
            if (built.Count > 0) RoadsChanged?.Invoke(built);
            return true;
        }

        private void AddRoad(Vector2Int c, RoadDefinition road)
        {
            ClearZone(c); // Straßen verdrängen Zonen
            Grid.SetRoad(c, road);
            RoadUpkeepPerMonth += road.upkeepPerCellPerMonth;
            if (road.prefab != null)
                roadViews[c] = Instantiate(road.prefab, Grid.CellCenterToWorld(c, transform.position),
                    Quaternion.identity, transform);
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
            RoadsChanged?.Invoke(new[] { cell });
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

        /// <summary>Zonenfelder und ihre Kacheln (für die Darstellung, z. B. Ein- und Ausblenden).</summary>
        public IReadOnlyDictionary<Vector2Int, GameObject> ZoneViews => zoneViews;

        /// <summary>Platzierte Straßenfelder und ihre Darstellung.</summary>
        public IReadOnlyDictionary<Vector2Int, GameObject> RoadViews => roadViews;

        /// <summary>
        /// Anschluss aller Gebäude neu berechnen. Läuft nur, wenn sich Straßen oder Gebäude ändern,
        /// nicht in jedem Simulationsschritt.
        /// </summary>
        public void RefreshConnections()
        {
            Connections.Recalculate(Grid, Buildings, UnlockedTechnologies);
            ConnectionsChanged?.Invoke();
        }

        /// <summary>Wird ausgelöst, sobald eine Technologie neu freigeschaltet ist.</summary>
        public event Action<TechnologyDefinition> TechnologyUnlocked;

        public bool UnlockTechnology(TechnologyDefinition tech)
        {
            if (tech == null || UnlockedTechnologies.Contains(tech)) return false;
            if (!tech.PrerequisitesMet(UnlockedTechnologies)) return false;
            UnlockedTechnologies.Add(tech); // alle bestehenden Gebäude profitieren automatisch
            RefreshConnections(); // Schichten können die Einzugsweite von Lagern ändern
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
                Debug.LogWarning("Platzieren nicht möglich (Zelle belegt, außerhalb, keine Straße angrenzend oder zu wenig Geld bzw. Material).");
        }
    }
}
