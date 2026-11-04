using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Zeigt den Arbeitsbereich eines Holzfällers oder Försters, solange sein Infofenster offen ist oder der Bereich
    /// gezeichnet wird (wie bei den Arbeitsbereichen in Manor Lords): jedes Feld als flache, halbtransparente Kachel
    /// (ausgewachsener Wald kräftig grün, junger hellgrün, übrige Felder blass gelblich), ein Umriss an den Außenkanten
    /// des Bereichs und eine Linie vom Gebäude zur Mitte des Bereichs, wenn der Bereich nicht am Gebäude liegt. Beim
    /// Zeichnen zusätzlich die Grenze der erlaubten Entfernung. Die Meshes werden nur neu gebaut, wenn sich Gebäude,
    /// Bereich oder Vorkommen ändern.
    /// </summary>
    public class WorkAreaVisuals : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [SerializeField] private BuildingPlacer placer;

        [Header("Farben")]
        public Color forestColor = new Color(0.15f, 0.75f, 0.2f, 0.55f);
        public Color youngColor = new Color(0.6f, 0.95f, 0.45f, 0.5f);
        public Color otherColor = new Color(0.95f, 0.9f, 0.5f, 0.22f);
        public Color outlineColor = new Color(1f, 0.82f, 0.2f, 0.95f);
        public Color limitColor = new Color(1f, 1f, 1f, 0.45f);
        [Min(0.02f)] public float outlineWidth = 0.14f;
        [Min(0.02f)] public float linkWidth = 0.22f;

        private GameObject root;
        private MeshRenderer forestR, youngR, otherR, outlineR, limitR;
        private MeshFilter forestF, youngF, otherF, outlineF, limitF;
        private LineRenderer link;
        private Material[] materials;

        private BuildingInstance shown;
        private bool shownEditing;
        private bool dirty = true;

        /// <summary>Kacheln des gezeigten Bereichs (für Tests).</summary>
        public int TileCount { get; private set; }
        public bool OutlineVisible => outlineR != null && outlineR.gameObject.activeSelf;
        public bool LinkVisible => link != null && link.gameObject.activeSelf;
        public bool LimitVisible => limitR != null && limitR.gameObject.activeSelf;
        public bool Visible => root != null && root.activeSelf;

        private void Awake()
        {
            root = new GameObject("WorkAreaVisuals");
            root.transform.SetParent(transform, false);
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            materials = new Material[5];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = new Material(shader);
                MaterialUtil.SetTransparent(materials[i]);
                materials[i].SetFloat("_Cull", 0f); // von beiden Seiten sichtbar
            }
            Make("Wald", 0, out forestF, out forestR);
            Make("Jung", 1, out youngF, out youngR);
            Make("Sonst", 2, out otherF, out otherR);
            Make("Umriss", 3, out outlineF, out outlineR);
            Make("Grenze", 4, out limitF, out limitR);
            var lineGo = new GameObject("Link");
            lineGo.transform.SetParent(root.transform, false);
            link = lineGo.AddComponent<LineRenderer>();
            link.useWorldSpace = true;
            link.positionCount = 2;
            link.alignment = LineAlignment.View;
            link.sharedMaterial = materials[3];
            link.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            root.SetActive(false);
        }

        private void Make(string name, int material, out MeshFilter filter, out MeshRenderer renderer)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = new Mesh { name = "WorkArea_" + name };
            renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = materials[material];
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void OnEnable()
        {
            if (city == null) return;
            city.WorkAreaChanged += OnChanged;
            city.DepositsChanged += OnChanged;
        }

        private void OnDisable()
        {
            if (city == null) return;
            city.WorkAreaChanged -= OnChanged;
            city.DepositsChanged -= OnChanged;
        }

        private void OnChanged() => dirty = true;
        private void OnChanged(BuildingInstance b) => dirty = true;

        private void OnDestroy()
        {
            if (materials == null) return;
            foreach (var m in materials) if (m != null) Destroy(m);
        }

        private void Update()
        {
            if (city == null || placer == null || city.Grid == null) return;

            var target = placer.WorkAreaEditing;
            bool editing = target != null;
            if (target == null)
            {
                target = placer.Inspected;
                if (target != null && (!target.Definition.HasWorkArea || !city.Buildings.Contains(target))) target = null;
            }

            if (target != shown || editing != shownEditing) dirty = true;
            if (!dirty) return;
            dirty = false;
            shown = target;
            shownEditing = editing;

            if (target == null || target.WorkArea == null)
            {
                root.SetActive(false);
                TileCount = 0;
                return;
            }
            Rebuild(target, editing);
            root.SetActive(true);
        }

        private void Rebuild(BuildingInstance b, bool editing)
        {
            var grid = city.Grid;
            var origin = city.transform.position;
            float s = grid.CellSize;
            var def = b.Definition;
            var deposit = def.requiredDeposit != null ? def.requiredDeposit : def.plantsDeposit;
            float yTile = origin.y + 0.09f, yLine = origin.y + 0.11f;

            var forest = new MeshData(); var young = new MeshData(); var other = new MeshData();
            var outline = new MeshData(); var limit = new MeshData();
            float w = outlineWidth;

            foreach (var c in b.WorkArea)
            {
                var min = grid.CellToWorld(c, origin);
                var data = deposit != null && grid.GetDeposit(c) == deposit ? (grid.IsYoung(c) ? young : forest) : other;
                data.Rect(min.x, min.z, min.x + s, min.z + s, yTile, 0.04f);
                // Außenkanten: Streifen innerhalb des Felds, wo kein Nachbar zum Bereich gehört
                if (!b.WorkArea.Contains(c + Vector2Int.left)) outline.Rect(min.x, min.z, min.x + w, min.z + s, yLine, 0f);
                if (!b.WorkArea.Contains(c + Vector2Int.right)) outline.Rect(min.x + s - w, min.z, min.x + s, min.z + s, yLine, 0f);
                if (!b.WorkArea.Contains(c + Vector2Int.down)) outline.Rect(min.x, min.z, min.x + s, min.z + w, yLine, 0f);
                if (!b.WorkArea.Contains(c + Vector2Int.up)) outline.Rect(min.x, min.z + s - w, min.x + s, min.z + s, yLine, 0f);
            }
            TileCount = b.WorkArea.Count;

            // Grenze der erlaubten Entfernung (Quadrat um die Grundfläche), nur beim Zeichnen
            if (editing)
            {
                int m = def.workAreaMaxDistance;
                var lo = grid.CellToWorld(new Vector2Int(b.Origin.x - m, b.Origin.y - m), origin);
                var hi = grid.CellToWorld(new Vector2Int(b.Origin.x + b.Footprint.x + m, b.Origin.y + b.Footprint.y + m), origin);
                float lw = w * 1.6f, yl = origin.y + 0.1f;
                limit.Rect(lo.x - lw * 0.5f, lo.z - lw * 0.5f, hi.x + lw * 0.5f, lo.z + lw * 0.5f, yl, 0f);
                limit.Rect(lo.x - lw * 0.5f, hi.z - lw * 0.5f, hi.x + lw * 0.5f, hi.z + lw * 0.5f, yl, 0f);
                limit.Rect(lo.x - lw * 0.5f, lo.z + lw * 0.5f, lo.x + lw * 0.5f, hi.z - lw * 0.5f, yl, 0f);
                limit.Rect(hi.x - lw * 0.5f, lo.z + lw * 0.5f, hi.x + lw * 0.5f, hi.z - lw * 0.5f, yl, 0f);
            }

            forest.Apply(forestF.sharedMesh, forestR, forestColor, materials[0]);
            young.Apply(youngF.sharedMesh, youngR, youngColor, materials[1]);
            other.Apply(otherF.sharedMesh, otherR, otherColor, materials[2]);
            outline.Apply(outlineF.sharedMesh, outlineR, outlineColor, materials[3]);
            limit.Apply(limitF.sharedMesh, limitR, limitColor, materials[4]);

            // Linie vom Gebäude zur Mitte des Bereichs, wenn die Mitte nicht in der Grundfläche liegt
            float sx = 0f, sy = 0f;
            foreach (var c in b.WorkArea) { sx += c.x; sy += c.y; }
            sx /= b.WorkArea.Count; sy /= b.WorkArea.Count;
            var center = origin + new Vector3((sx + 0.5f) * s, 0f, (sy + 0.5f) * s);
            var from = origin + new Vector3((b.Origin.x + b.Footprint.x * 0.5f) * s, 0f, (b.Origin.y + b.Footprint.y * 0.5f) * s);
            var centerCell = new Vector2Int(Mathf.FloorToInt(sx + 0.5f), Mathf.FloorToInt(sy + 0.5f));
            bool apart = CityController.DistanceToBuilding(b, centerCell) > 0;
            link.gameObject.SetActive(apart);
            if (apart)
            {
                float yk = origin.y + 0.35f;
                link.startWidth = link.endWidth = linkWidth;
                link.startColor = link.endColor = outlineColor;
                link.SetPosition(0, new Vector3(from.x, yk, from.z));
                link.SetPosition(1, new Vector3(center.x, yk, center.z));
            }
        }

        /// <summary>Sammelt flache Rechtecke zu einem Mesh (Normale nach oben).</summary>
        private class MeshData
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<int> triangles = new List<int>();

            /// <summary>Rechteck von (x0, z0) bis (x1, z1) in Höhe y; inset verkleinert es an allen Seiten.</summary>
            public void Rect(float x0, float z0, float x1, float z1, float y, float inset)
            {
                x0 += inset; z0 += inset; x1 -= inset; z1 -= inset;
                int i = vertices.Count;
                vertices.Add(new Vector3(x0, y, z0));
                vertices.Add(new Vector3(x0, y, z1));
                vertices.Add(new Vector3(x1, y, z1));
                vertices.Add(new Vector3(x1, y, z0));
                triangles.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }

            public void Apply(Mesh mesh, MeshRenderer renderer, Color color, Material material)
            {
                mesh.Clear();
                renderer.gameObject.SetActive(vertices.Count > 0);
                MaterialUtil.SetColor(material, color);
                if (vertices.Count == 0) return;
                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateBounds();
            }
        }
    }
}
