using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Passt die Darstellung der Straßenfelder an ihre Nachbarn an, sobald Straßen gebaut oder entfernt werden.
    /// Das Straßen-Prefab braucht dafür einen MeshFilter (Fahrbahn und Rand als zwei Materialien).
    /// Auf Wasserfeldern (CityController.IsBridge) ersetzt eine Holzbrücke aus Blender das Straßenstück: ein
    /// Mittelstück, je Nachbarfeld mit Straße ein Arm (eben zu einem anderen Brückenfeld, als Rampe auf die Straße am
    /// Ufer) und an jeder Seite ohne Anschluss ein Geländerstück. So passen Gerade, Kurve, T und Kreuzung aus denselben
    /// Teilen, und der Belag schließt ohne Stufe an die Straße an.
    /// </summary>
    public class RoadVisuals : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [Tooltip("Färbt Brückenstücke mit, solange die Anbindungskarte an ist")]
        [SerializeField] private ConnectionOverlay overlay;
        [Tooltip("Breite des Randstreifens neben der Fahrbahn in Metern")]
        [Min(0f)] public float edgeWidth = 0.22f;

        [Header("Brücke (Teile aus Blender/scripts/bruecke.py)")]
        [Tooltip("Belag ohne Geländer, Planken quer zur Fahrtrichtung")]
        [SerializeField] private GameObject bridgeCenter;
        [Tooltip("Geländer an einer Seite des Mittelstücks")]
        [SerializeField] private GameObject bridgeRail;
        [Tooltip("Arm bis zum Feldrand auf gleicher Höhe (nächstes Feld ist wieder Brücke)")]
        [SerializeField] private GameObject bridgeArmFlat;
        [Tooltip("Arm, der bis zum Feldrand auf die Höhe der Straße abfällt (nächstes Feld ist Land)")]
        [SerializeField] private GameObject bridgeArmRamp;
        [Tooltip("Richtung, in die Arm und Geländer der Prefabs zeigen (Welt, waagerecht)")]
        [SerializeField] private Vector3 bridgeBaseDirection = new Vector3(0f, 0f, -1f);

        private const string BridgeName = "Bruecke";

        private readonly HashSet<Vector2Int> dirty = new HashSet<Vector2Int>();

        private void OnEnable()
        {
            if (city != null) city.RoadsChanged += OnRoadsChanged;
        }

        private void OnDisable()
        {
            if (city != null) city.RoadsChanged -= OnRoadsChanged;
        }

        private void OnRoadsChanged(IReadOnlyList<Vector2Int> cells)
        {
            // Geänderte Felder und ihre Nachbarn bekommen ein neues Stück
            dirty.Clear();
            foreach (var c in cells)
            {
                dirty.Add(c);
                foreach (var offset in GridMap.NeighbourOffsets) dirty.Add(c + offset);
            }
            bool bridges = false;
            foreach (var c in dirty) bridges |= Refresh(c);
            if (bridges && overlay != null) overlay.Reapply();
        }

        /// <summary>Setzt das passende Stück; true, wenn es ein Brückenfeld ist (neue Teile brauchen die Overlay-Farbe).</summary>
        private bool Refresh(Vector2Int cell)
        {
            if (!city.RoadViews.TryGetValue(cell, out var view) || view == null) return false;
            var road = city.Grid.GetRoad(cell);
            var filter = view.GetComponentInChildren<MeshFilter>();
            if (road == null || filter == null) return false;

            int mask = RoadMeshBuilder.GetMask(city.Grid, cell);
            filter.sharedMesh = RoadMeshBuilder.Get(mask, city.Grid.CellSize, road.visualWidth, edgeWidth);

            bool bridge = city.IsBridge(cell) && bridgeCenter != null;
            var roadRenderer = filter.GetComponent<MeshRenderer>();
            if (roadRenderer != null) roadRenderer.enabled = !bridge;

            var old = view.transform.Find(BridgeName);
            if (old != null)
            {
                old.name = BridgeName + "_alt";
                old.gameObject.SetActive(false);
                Destroy(old.gameObject);
            }
            if (bridge) BuildBridge(cell, view.transform, mask);
            return bridge;
        }

        private void BuildBridge(Vector2Int cell, Transform parent, int mask)
        {
            var root = new GameObject(BridgeName).transform;
            root.SetParent(parent, false);
            root.localPosition = Vector3.zero;

            var baseDir = new Vector3(bridgeBaseDirection.x, 0f, bridgeBaseDirection.z).normalized;
            Spawn(bridgeCenter, root, CenterRotation(mask, baseDir));
            AddSide(RoadMeshBuilder.East, Vector2Int.right, Vector3.right);
            AddSide(RoadMeshBuilder.West, Vector2Int.left, Vector3.left);
            AddSide(RoadMeshBuilder.North, Vector2Int.up, Vector3.forward);
            AddSide(RoadMeshBuilder.South, Vector2Int.down, Vector3.back);

            void AddSide(int bit, Vector2Int offset, Vector3 direction)
            {
                var rotation = Quaternion.FromToRotation(baseDir, direction);
                if ((mask & bit) == 0)
                {
                    Spawn(bridgeRail, root, rotation);
                    return;
                }
                Spawn(city.IsBridge(cell + offset) ? bridgeArmFlat : bridgeArmRamp, root, rotation);
            }
        }

        /// <summary>Das Mittelstück liegt quer zur Fahrtrichtung; läuft die Brücke nur in x, wird es gedreht.</summary>
        private static Quaternion CenterRotation(int mask, Vector3 baseDir)
        {
            bool alongX = (mask & (RoadMeshBuilder.East | RoadMeshBuilder.West)) != 0 &&
                          (mask & (RoadMeshBuilder.North | RoadMeshBuilder.South)) == 0;
            bool baseAlongX = Mathf.Abs(baseDir.x) > Mathf.Abs(baseDir.z);
            return alongX != baseAlongX ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
        }

        private static void Spawn(GameObject prefab, Transform parent, Quaternion rotation)
        {
            if (prefab == null) return;
            var go = Instantiate(prefab, parent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = rotation;
        }
    }
}
