using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace EpochCity
{
    /// <summary>
    /// Platzieren und Abreißen per Maus (neues Input System).
    /// 1-9: Gebäude der in der Bauleiste offenen Kategorie wählen · R: Straße · Linksklick: bauen / abreißen
    /// Straße: Maustaste halten und ziehen, die Strecke folgt dem Raster in L-Form
    /// Z / G: Wohn- / Gewerbezone, U: Zone aufheben – jeweils ein Rechteck ziehen
    /// Rechtsklick oder Esc: Auswahl aufheben · X: Abriss-Modus an/aus (Gebäude und Straßen)
    /// </summary>
    public class BuildingPlacer : MonoBehaviour
    {
        public CityController city;
        public Camera cam;
        [Tooltip("Alle einzeln baubaren Gebäude; die Bauleiste ordnet sie nach ihrer Kategorie")]
        public BuildingDefinition[] hotbar;
        [Tooltip("Aktuell gewählter Straßentyp (Startwert für R)")]
        public RoadDefinition road;
        [Tooltip("Alle Straßentypen der Bauleiste; R wechselt im Straßenmodus zum nächsten baubaren")]
        public RoadDefinition[] roads;

        /// <summary>Straßentypen der Bauleiste (ohne Liste nur der eine Straßentyp).</summary>
        public RoadDefinition[] Roads => roads != null && roads.Length > 0 ? roads : new[] { road };

        [Header("Zonen")]
        [Tooltip("Zone für Taste Z")]
        public ZoneDefinition residentialZone;
        [Tooltip("Zone für Taste G")]
        public ZoneDefinition commercialZone;

        public Color validColor = new Color(0.4f, 1f, 0.4f, 1f);
        public Color invalidColor = new Color(1f, 0.35f, 0.35f, 1f);
        [Tooltip("Deckkraft der Bau-Vorschau")]
        [Range(0.1f, 1f)] public float ghostAlpha = 0.55f;

        /// <summary>Ist gerade ein Baumodus aktiv (Gebäude, Straße, Zone oder Abriss)?</summary>
        public bool IsBuildMode => selected != null || roadMode || zoneMode || demolishMode;

        private BuildingDefinition selected;
        private bool demolishMode;
        private bool roadMode;
        private GameObject ghost;
        private Renderer[] ghostRenderers;
        private Vector2Int hoverCell;
        private bool hasHover;

        private bool zoneMode;
        private ZoneDefinition selectedZone; // im Zonenmodus: null = Zone aufheben

        // Vorschau beim Ziehen: ein Platzhalter pro Feld, wiederverwendet
        private readonly CellGhostPool roadGhosts = new CellGhostPool();
        private readonly CellGhostPool zoneGhosts = new CellGhostPool();
        private bool dragging;
        private Vector2Int dragStart;

        private void Reset()
        {
            cam = Camera.main;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null || mouse == null || city == null || city.Grid == null) return;
            if (cam == null) cam = Camera.main;

            HandleHotkeys(kb);

            hasHover = TryGetHoverCell(mouse, out hoverCell);
            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            if (demolishMode)
                HandleDemolish(mouse, overUi);
            else if (roadMode)
                HandleRoad(mouse, overUi);
            else if (zoneMode)
                HandleZone(mouse, overUi);
            else
                HandlePlacement(mouse, overUi);

            if (mouse.rightButton.wasPressedThisFrame) ClearSelection();
        }

        /// <summary>In der Bauleiste geöffnete Gebäudekategorie; die Zifferntasten wählen darin.</summary>
        public BuildingCategory? ActiveCategory { get; set; }

        /// <summary>Baubare Gebäude einer Kategorie in der Reihenfolge der Liste.</summary>
        public List<BuildingDefinition> GetCategoryBuildings(BuildingCategory category)
        {
            var list = new List<BuildingDefinition>();
            if (hotbar == null) return list;
            foreach (var def in hotbar)
                if (def != null && def.category == category) list.Add(def);
            return list;
        }

        private void HandleHotkeys(Keyboard kb)
        {
            if (ActiveCategory.HasValue)
            {
                var list = GetCategoryBuildings(ActiveCategory.Value);
                for (int i = 0; i < list.Count && i < 9; i++)
                {
                    var key = kb[(Key)((int)Key.Digit1 + i)];
                    if (key.wasPressedThisFrame) Select(list[i]);
                }
            }
            if (kb.escapeKey.wasPressedThisFrame) ClearSelection();
            if (kb.xKey.wasPressedThisFrame) ToggleDemolish();
            if (kb.rKey.wasPressedThisFrame) SelectRoad();
            if (kb.zKey.wasPressedThisFrame && residentialZone != null) SelectZone(residentialZone);
            if (kb.gKey.wasPressedThisFrame && commercialZone != null) SelectZone(commercialZone);
            if (kb.uKey.wasPressedThisFrame) SelectZone(null);
        }

        /// <summary>Gebäude, dessen Infofenster offen ist (Linksklick außerhalb der Baumodi), sonst null.</summary>
        public BuildingInstance Inspected { get; private set; }

        public void Inspect(BuildingInstance building) => Inspected = building;

        private void HandlePlacement(Mouse mouse, bool overUi)
        {
            // Ohne Werkzeug wählt ein Linksklick das Gebäude unter der Maus aus (leeres Feld schließt das Fenster)
            if (selected == null && !overUi && hasHover && mouse.leftButton.wasPressedThisFrame)
                Inspected = city.Grid.GetAt(hoverCell);

            if (selected == null || !hasHover)
            {
                if (ghost != null) ghost.SetActive(false);
                return;
            }

            bool canPlace = city.CanPlace(selected, hoverCell) && city.CanAfford(selected);

            if (ghost != null)
            {
                ghost.SetActive(true);
                ghost.transform.SetPositionAndRotation(city.GetFootprintCenter(selected, hoverCell),
                    Quaternion.Euler(0f, city.GetFacingAngle(selected, hoverCell), 0f));
                Tint(ghostRenderers, GhostColor(canPlace ? validColor : invalidColor));
            }

            if (!overUi && mouse.leftButton.wasPressedThisFrame && canPlace)
                city.TryPlace(selected, hoverCell, out _);
        }

        private void HandleRoad(Mouse mouse, bool overUi)
        {
            if (!dragging && !overUi && hasHover && mouse.leftButton.wasPressedThisFrame)
            {
                dragging = true;
                dragStart = hoverCell;
            }

            if (!hasHover && !dragging)
            {
                roadGhosts.Hide();
                return;
            }

            var path = dragging ? GridMap.LPath(dragStart, hoverCell) : new List<Vector2Int> { hoverCell };
            bool canBuild = city.CanBuildRoad(road, path, out _);
            roadGhosts.Show(city, road.prefab, path, _ => canBuild, GhostColor(validColor), GhostColor(invalidColor));

            if (dragging && mouse.leftButton.wasReleasedThisFrame)
            {
                dragging = false;
                city.TryBuildRoad(road, path);
            }
        }

        private void HandleZone(Mouse mouse, bool overUi)
        {
            if (!dragging && !overUi && hasHover && mouse.leftButton.wasPressedThisFrame)
            {
                dragging = true;
                dragStart = hoverCell;
            }

            if (!hasHover && !dragging)
            {
                zoneGhosts.Hide();
                return;
            }

            var cells = dragging ? GridMap.RectCells(dragStart, hoverCell) : new List<Vector2Int> { hoverCell };

            // Grün: Feld wird geändert, Rot: Feld bleibt, wie es ist
            var previewZone = selectedZone != null ? selectedZone : residentialZone;
            var prefab = previewZone != null ? previewZone.tilePrefab : null;
            zoneGhosts.Show(city, prefab, cells, c => selectedZone == null
                    ? city.Grid.GetZone(c) != null
                    : city.Grid.CanZone(c) && city.Grid.GetZone(c) != selectedZone,
                GhostColor(validColor), GhostColor(invalidColor));

            if (dragging && mouse.leftButton.wasReleasedThisFrame)
            {
                dragging = false;
                city.SetZone(selectedZone, cells);
            }
        }

        private void HandleDemolish(Mouse mouse, bool overUi)
        {
            if (overUi || !hasHover || !mouse.leftButton.wasPressedThisFrame) return;
            var target = city.Grid.GetAt(hoverCell);
            if (target == null)
            {
                city.TryRemoveRoad(hoverCell);
                return;
            }
            if (!city.TryRemove(target))
                Debug.Log($"{target.Definition.displayName} steht unter Denkmalschutz und lässt sich nicht abreißen.");
        }

        private bool TryGetHoverCell(Mouse mouse, out Vector2Int cell)
        {
            cell = default;
            if (cam == null) return false;
            var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            var ground = new Plane(Vector3.up, city.transform.position);
            if (!ground.Raycast(ray, out float t)) return false;
            cell = city.Grid.WorldToCell(ray.GetPoint(t), city.transform.position);
            return city.Grid.InBounds(cell);
        }

        /// <summary>Aktuell gewähltes Gebäude (null, wenn keins gewählt ist).</summary>
        public BuildingDefinition SelectedBuilding => selected;

        /// <summary>Ist der Abriss-Modus aktiv?</summary>
        public bool DemolishMode => demolishMode;

        /// <summary>Ist der Straßenbau aktiv?</summary>
        public bool RoadMode => roadMode;

        /// <summary>Ist der Zonenmodus aktiv? SelectedZone null bedeutet dann: Zone aufheben.</summary>
        public bool ZoneMode => zoneMode;

        public ZoneDefinition SelectedZone => selectedZone;

        /// <summary>Zelle unter der Maus, falls sie im Raster liegt.</summary>
        public bool TryGetHover(out Vector2Int cell)
        {
            cell = hoverCell;
            return hasHover;
        }

        public void ToggleDemolish()
        {
            bool enable = !demolishMode;
            ClearSelection();
            demolishMode = enable;
        }

        /// <summary>Straßenmodus; ist er schon an, wechselt R zum nächsten baubaren Straßentyp.</summary>
        public void SelectRoad()
        {
            var list = Roads;
            int start = Mathf.Max(0, System.Array.IndexOf(list, road));
            int offset = roadMode ? 1 : 0;
            for (int i = 0; i < list.Length; i++)
            {
                var candidate = list[(start + offset + i) % list.Length];
                if (candidate != null && city.IsAvailable(candidate))
                {
                    SelectRoad(candidate);
                    return;
                }
            }
            SelectRoad(road);
        }

        public void SelectRoad(RoadDefinition type)
        {
            ClearSelection();
            if (type != null) road = type;
            roadMode = road != null;
        }

        /// <summary>Zonenmodus; zone = null hebt Zonen auf.</summary>
        public void SelectZone(ZoneDefinition zone)
        {
            ClearSelection();
            zoneMode = true;
            selectedZone = zone;
        }

        public void Select(BuildingDefinition def)
        {
            ClearSelection();
            selected = def;
            if (def == null || def.prefab == null) return;

            ghost = CreateGhost(def.prefab, out ghostRenderers);
            ghost.SetActive(false);
        }

        public void ClearSelection()
        {
            selected = null;
            demolishMode = false;
            roadMode = false;
            zoneMode = false;
            selectedZone = null;
            Inspected = null;
            dragging = false;
            DestroyGhost();
            roadGhosts.Hide();
            zoneGhosts.Hide();
        }

        private void DestroyGhost()
        {
            if (ghost != null) Destroy(ghost);
            ghost = null;
            ghostRenderers = null;
        }

        /// <summary>Vorschau-Platzhalter für einzelne Rasterfelder, wiederverwendet zwischen den Frames.</summary>
        private class CellGhostPool
        {
            private readonly List<GameObject> ghosts = new List<GameObject>();
            private readonly List<Renderer[]> renderers = new List<Renderer[]>();
            private GameObject prefab;

            public void Show(CityController city, GameObject newPrefab, IReadOnlyList<Vector2Int> cells,
                System.Func<Vector2Int, bool> isValid, Color valid, Color invalid)
            {
                if (newPrefab != prefab)
                {
                    Clear();
                    prefab = newPrefab;
                }

                int count = prefab != null && cells != null ? cells.Count : 0;
                while (ghosts.Count < count)
                {
                    ghosts.Add(CreateGhost(prefab, out var r));
                    renderers.Add(r);
                }

                for (int i = 0; i < ghosts.Count; i++)
                {
                    bool active = i < count;
                    ghosts[i].SetActive(active);
                    if (!active) continue;

                    // Leicht angehoben, damit die Vorschau nicht mit Vorhandenem flimmert
                    ghosts[i].transform.position =
                        city.Grid.CellCenterToWorld(cells[i], city.transform.position) + Vector3.up * 0.03f;
                    Tint(renderers[i], isValid(cells[i]) ? valid : invalid);
                }
            }

            public void Hide()
            {
                foreach (var g in ghosts) g.SetActive(false);
            }

            private void Clear()
            {
                foreach (var g in ghosts) UnityEngine.Object.Destroy(g);
                ghosts.Clear();
                renderers.Clear();
            }
        }

        /// <summary>Vorschau-Kopie eines Prefabs: ohne Collider, durchscheinend und ohne Schattenwurf.</summary>
        private static GameObject CreateGhost(GameObject prefab, out Renderer[] renderers)
        {
            var go = Instantiate(prefab);
            foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false;
            renderers = go.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                // r.materials erzeugt Kopien aller Materialien, nur für die Vorschau
                foreach (var m in r.materials) MaterialUtil.SetTransparent(m);
            }
            return go;
        }

        private Color GhostColor(Color color) => new Color(color.r, color.g, color.b, ghostAlpha);

        private static void Tint(Renderer[] renderers, Color color)
        {
            if (renderers == null) return;
            foreach (var r in renderers)
                foreach (var m in r.materials) MaterialUtil.SetColor(m, color);
        }
    }
}
