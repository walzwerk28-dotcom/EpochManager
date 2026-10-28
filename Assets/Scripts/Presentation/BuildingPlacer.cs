using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace EpochCity
{
    /// <summary>
    /// Platzieren und Abreißen per Maus (neues Input System).
    /// 1-9: Gebäude aus der Liste wählen · Linksklick: bauen / abreißen
    /// Rechtsklick oder Esc: Auswahl aufheben · X: Abriss-Modus an/aus
    /// </summary>
    public class BuildingPlacer : MonoBehaviour
    {
        public CityController city;
        public Camera cam;
        [Tooltip("Gebäude, die mit den Tasten 1-9 wählbar sind")]
        public BuildingDefinition[] hotbar;

        public Color validColor = new Color(0.4f, 1f, 0.4f, 1f);
        public Color invalidColor = new Color(1f, 0.35f, 0.35f, 1f);

        private BuildingDefinition selected;
        private bool demolishMode;
        private GameObject ghost;
        private Renderer[] ghostRenderers;
        private Vector2Int hoverCell;
        private bool hasHover;

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
                Tint(canPlace ? validColor : invalidColor);
            }

            if (!overUi && mouse.leftButton.wasPressedThisFrame && canPlace)
                city.TryPlace(selected, hoverCell, out _);
        }

        private void HandleDemolish(Mouse mouse, bool overUi)
        {
            if (overUi || !hasHover || !mouse.leftButton.wasPressedThisFrame) return;
            var target = city.Grid.GetAt(hoverCell);
            if (target == null) return;
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

        public void ToggleDemolish()
        {
            demolishMode = !demolishMode;
            if (demolishMode) { selected = null; DestroyGhost(); }
        }

        public void Select(BuildingDefinition def)
        {
            demolishMode = false;
            DestroyGhost();
            selected = def;
            if (def == null || def.prefab == null) return;

            ghost = Instantiate(def.prefab);
            foreach (var c in ghost.GetComponentsInChildren<Collider>()) c.enabled = false;
            ghostRenderers = ghost.GetComponentsInChildren<Renderer>();
            ghost.SetActive(false);
        }

        public void ClearSelection()
        {
            selected = null;
            demolishMode = false;
            DestroyGhost();
        }

        private void DestroyGhost()
        {
            if (ghost != null) Destroy(ghost);
            ghost = null;
            ghostRenderers = null;
        }

        private void Tint(Color color)
        {
            if (ghostRenderers == null) return;
            foreach (var r in ghostRenderers)
            {
                var mat = r.material; // erzeugt eine Kopie, nur für die Vorschau
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            }
        }
    }
}
