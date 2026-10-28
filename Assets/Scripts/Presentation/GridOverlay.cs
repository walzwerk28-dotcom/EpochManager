using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Raster über der Karte, das nur in Baumodi sichtbar ist, und eine dezente, immer sichtbare
    /// Kartengrenze. Beides wird zur Laufzeit aus der Rastergröße des CityController aufgebaut.
    /// </summary>
    public class GridOverlay : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [SerializeField] private BuildingPlacer placer;
        [Tooltip("Transparentes Material mit einer Kachel pro Rasterfeld (Linien am Rand)")]
        [SerializeField] private Material gridMaterial;
        [Tooltip("Material der Kartengrenze")]
        [SerializeField] private Material borderMaterial;
        [Min(0.01f)] public float borderWidth = 0.3f;

        private GameObject grid;

        private void Start()
        {
            if (city == null || city.Grid == null) return;
            float width = city.Grid.Width * city.Grid.CellSize;
            float depth = city.Grid.Height * city.Grid.CellSize;
            var origin = city.transform.position;

            grid = GameObject.CreatePrimitive(PrimitiveType.Quad);
            grid.name = "Grid";
            Destroy(grid.GetComponent<Collider>());
            grid.transform.SetParent(transform, false);
            grid.transform.position = origin + new Vector3(width * 0.5f, 0.06f, depth * 0.5f);
            grid.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            grid.transform.localScale = new Vector3(width, depth, 1f);
            var gridRenderer = grid.GetComponent<Renderer>();
            gridRenderer.sharedMaterial = gridMaterial;
            gridRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // Eine Texturkachel pro Rasterfeld; eigene Instanz, damit das Asset unverändert bleibt
            gridRenderer.material.mainTextureScale = new Vector2(city.Grid.Width, city.Grid.Height);
            grid.SetActive(false);

            BuildBorder(origin, width, depth);
        }

        private void Update()
        {
            if (grid != null && placer != null && grid.activeSelf != placer.IsBuildMode)
                grid.SetActive(placer.IsBuildMode);
        }

        private void BuildBorder(Vector3 origin, float width, float depth)
        {
            float w = borderWidth;
            AddBorderStrip(origin + new Vector3(width * 0.5f, 0f, -w * 0.5f), new Vector3(width + 2f * w, 0.04f, w));
            AddBorderStrip(origin + new Vector3(width * 0.5f, 0f, depth + w * 0.5f), new Vector3(width + 2f * w, 0.04f, w));
            AddBorderStrip(origin + new Vector3(-w * 0.5f, 0f, depth * 0.5f), new Vector3(w, 0.04f, depth));
            AddBorderStrip(origin + new Vector3(width + w * 0.5f, 0f, depth * 0.5f), new Vector3(w, 0.04f, depth));
        }

        private void AddBorderStrip(Vector3 center, Vector3 size)
        {
            var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = "Border";
            Destroy(strip.GetComponent<Collider>());
            strip.transform.SetParent(transform, false);
            strip.transform.position = center + Vector3.up * size.y * 0.5f;
            strip.transform.localScale = size;
            var r = strip.GetComponent<Renderer>();
            r.sharedMaterial = borderMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
