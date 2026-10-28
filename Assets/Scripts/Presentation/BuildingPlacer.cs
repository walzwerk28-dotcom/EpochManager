using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace EpochCity
{
    /// <summary>
    /// Platzieren und Abreißen per Maus (neues Input System).
    /// 1-9: Gebäude aus der Liste wählen · R: Straße · Linksklick: bauen / abreißen
    /// Straße: Maustaste halten und ziehen, die Strecke folgt dem Raster in L-Form
    /// Rechtsklick oder Esc: Auswahl aufheben · X: Abriss-Modus an/aus (Gebäude und Straßen)
    /// </summary>
    public class BuildingPlacer : MonoBehaviour
    {
        public CityController city;
        public Camera cam;
        [Tooltip("Gebäude, die mit den Tasten 1-9 wählbar sind")]
        public BuildingDefinition[] hotbar;
        [Tooltip("Straße, die mit R gewählt wird")]
        public RoadDefinition road;

        public Color validColor = new Color(0.4f, 1f, 0.4f, 1f);
        public Color invalidColor = new Color(1f, 0.35f, 0.35f, 1f);

        private BuildingDefinition selected;
        private bool demolishMode;
        private bool roadMode;
        private GameObject ghost;
        private Renderer[] ghostRenderers;
        private Vector2Int hoverCell;
        private bool hasHover;

        // Straßenvorschau: ein Platzhalter pro Feld der Strecke, wiederverwendet
        private readonly List<GameObject> roadGhosts = new List<GameObject>();
        private readonly List<Renderer[]> roadGhostRenderers = new List<Renderer[]>();
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
            else
                HandlePlacement(mouse, overUi);

            if (mouse.rightButton.wasPressedThisFrame) ClearSelection();
        }

        private void HandleHotkeys(Keyboard kb)
        {
            if (hotbar != null)
            {
                for (int i = 0; i < hotbar.Length && i < 9; i++)
                {
                    var key = kb[(Key)((int)Key.Digit1 + i)];
                    if (key.wasPressedThisFrame) Select(hotbar[i]);
                }
            }
            if (kb.escapeKey.wasPressedThisFrame) ClearSelection();
            if (kb.xKey.wasPressedThisFrame) ToggleDemolish();
            if (kb.rKey.wasPressedThisFrame) SelectRoad();
        }

        private void HandlePlacement(Mouse mouse, bool overUi)
        {
            if (selected == null || !hasHover)
            {
                if (ghost != null) ghost.SetActive(false);
                return;
            }

            bool canPlace = city.Grid.CanPlace(selected, hoverCell) && city.Money >= selected.buildCost
                            && city.IsEpochReached(selected.epoch);

            if (ghost != null)
            {
                ghost.SetActive(true);
                ghost.transform.position = city.GetFootprintCenter(selected, hoverCell);
                Tint(ghostRenderers, canPlace ? validColor : invalidColor);
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
                ShowRoadGhosts(null, false);
                return;
            }

            var path = dragging ? GridMap.LPath(dragStart, hoverCell) : new List<Vector2Int> { hoverCell };
            bool canBuild = city.CanBuildRoad(road, path, out _);
            ShowRoadGhosts(path, canBuild);

            if (dragging && mouse.leftButton.wasReleasedThisFrame)
            {
                dragging = false;
                city.TryBuildRoad(road, path);
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

        public void SelectRoad()
        {
            ClearSelection();
            roadMode = road != null;
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
            dragging = false;
            DestroyGhost();
            ShowRoadGhosts(null, false);
        }

        private void DestroyGhost()
        {
            if (ghost != null) Destroy(ghost);
            ghost = null;
            ghostRenderers = null;
        }

        private void ShowRoadGhosts(List<Vector2Int> path, bool valid)
        {
            int count = path != null && road != null && road.prefab != null ? path.Count : 0;
            while (roadGhosts.Count < count)
            {
                roadGhosts.Add(CreateGhost(road.prefab, out var renderers));
                roadGhostRenderers.Add(renderers);
            }

            for (int i = 0; i < roadGhosts.Count; i++)
            {
                bool active = i < count;
                roadGhosts[i].SetActive(active);
                if (!active) continue;

                // Leicht angehoben, damit die Vorschau nicht mit vorhandenen Straßen flimmert
                roadGhosts[i].transform.position =
                    city.Grid.CellCenterToWorld(path[i], city.transform.position) + Vector3.up * 0.03f;
                Tint(roadGhostRenderers[i], valid ? validColor : invalidColor);
            }
        }

        private static GameObject CreateGhost(GameObject prefab, out Renderer[] renderers)
        {
            var go = Instantiate(prefab);
            foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false;
            renderers = go.GetComponentsInChildren<Renderer>();
            return go;
        }

        private static void Tint(Renderer[] renderers, Color color)
        {
            if (renderers == null) return;
            foreach (var r in renderers)
            {
                var mat = r.material; // erzeugt eine Kopie, nur für die Vorschau
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            }
        }
    }
}
