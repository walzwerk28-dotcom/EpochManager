using UnityEngine;

namespace EpochCity
{
    /// <summary>Reines Raster ohne Unity-Objekte: wer belegt welche Zelle?</summary>
    public class GridMap
    {
        public int Width { get; }
        public int Height { get; }
        public float CellSize { get; }

        private readonly BuildingInstance[,] cells;

        public GridMap(int width, int height, float cellSize)
        {
            Width = width;
            Height = height;
            CellSize = cellSize;
            cells = new BuildingInstance[width, height];
        }

        public bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < Width && c.y < Height;

        public BuildingInstance GetAt(Vector2Int c) => InBounds(c) ? cells[c.x, c.y] : null;

        public bool CanPlace(BuildingDefinition def, Vector2Int origin)
        {
            for (int x = 0; x < def.size.x; x++)
                for (int y = 0; y < def.size.y; y++)
                {
                    var c = new Vector2Int(origin.x + x, origin.y + y);
                    if (!InBounds(c) || cells[c.x, c.y] != null) return false;
                }
            return true;
        }

        public void Place(BuildingInstance inst)
        {
            var size = inst.Definition.size;
            for (int x = 0; x < size.x; x++)
                for (int y = 0; y < size.y; y++)
                    cells[inst.Origin.x + x, inst.Origin.y + y] = inst;
        }

        public void Remove(BuildingInstance inst)
        {
            var size = inst.Definition.size;
            for (int x = 0; x < size.x; x++)
                for (int y = 0; y < size.y; y++)
                    cells[inst.Origin.x + x, inst.Origin.y + y] = null;
        }

        public Vector3 CellToWorld(Vector2Int c, Vector3 gridOrigin) =>
            gridOrigin + new Vector3(c.x * CellSize, 0f, c.y * CellSize);

        public Vector2Int WorldToCell(Vector3 world, Vector3 gridOrigin)
        {
            var local = world - gridOrigin;
            return new Vector2Int(Mathf.FloorToInt(local.x / CellSize), Mathf.FloorToInt(local.z / CellSize));
        }
    }
}
