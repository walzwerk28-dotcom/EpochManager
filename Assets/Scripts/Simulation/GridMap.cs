using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>Reines Raster ohne Unity-Objekte: wer belegt welche Zelle, wo liegen Straßen?</summary>
    public class GridMap
    {
        public int Width { get; }
        public int Height { get; }
        public float CellSize { get; }

        private readonly BuildingInstance[,] cells;
        private readonly RoadDefinition[,] roads;

        private static readonly Vector2Int[] Neighbours =
            { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        public GridMap(int width, int height, float cellSize)
        {
            Width = width;
            Height = height;
            CellSize = cellSize;
            cells = new BuildingInstance[width, height];
            roads = new RoadDefinition[width, height];
        }

        public bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < Width && c.y < Height;

        public BuildingInstance GetAt(Vector2Int c) => InBounds(c) ? cells[c.x, c.y] : null;

        public bool CanPlace(BuildingDefinition def, Vector2Int origin)
        {
            for (int x = 0; x < def.size.x; x++)
                for (int y = 0; y < def.size.y; y++)
                {
                    var c = new Vector2Int(origin.x + x, origin.y + y);
                    if (!InBounds(c) || cells[c.x, c.y] != null || roads[c.x, c.y] != null) return false;
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

        // ---------------------------------------------------------------- Straßen

        public RoadDefinition GetRoad(Vector2Int c) => InBounds(c) ? roads[c.x, c.y] : null;

        public bool HasRoad(Vector2Int c) => GetRoad(c) != null;

        /// <summary>Auf dieser Zelle darf eine Straße liegen (im Raster, kein Gebäude).</summary>
        public bool CanPlaceRoad(Vector2Int c) => InBounds(c) && cells[c.x, c.y] == null;

        public void SetRoad(Vector2Int c, RoadDefinition road)
        {
            if (InBounds(c)) roads[c.x, c.y] = road;
        }

        /// <summary>Zellen, die seitlich an die Grundfläche des Gebäudes grenzen (ohne Ecken).</summary>
        public IEnumerable<Vector2Int> GetAdjacentCells(BuildingInstance inst)
        {
            var size = inst.Definition.size;
            for (int x = 0; x < size.x; x++)
            {
                yield return new Vector2Int(inst.Origin.x + x, inst.Origin.y - 1);
                yield return new Vector2Int(inst.Origin.x + x, inst.Origin.y + size.y);
            }
            for (int y = 0; y < size.y; y++)
            {
                yield return new Vector2Int(inst.Origin.x - 1, inst.Origin.y + y);
                yield return new Vector2Int(inst.Origin.x + size.x, inst.Origin.y + y);
            }
        }

        public bool TouchesRoad(BuildingInstance inst)
        {
            foreach (var c in GetAdjacentCells(inst))
                if (HasRoad(c)) return true;
            return false;
        }

        public static IReadOnlyList<Vector2Int> NeighbourOffsets => Neighbours;

        /// <summary>
        /// Weg in L-Form wie beim Ziehen einer Straße: erst entlang x, dann entlang y.
        /// Start- und Zielzelle sind enthalten.
        /// </summary>
        public static List<Vector2Int> LPath(Vector2Int from, Vector2Int to)
        {
            var path = new List<Vector2Int>();
            int stepX = to.x >= from.x ? 1 : -1;
            for (int x = from.x; x != to.x + stepX; x += stepX)
                path.Add(new Vector2Int(x, from.y));

            int stepY = to.y >= from.y ? 1 : -1;
            for (int y = from.y + stepY; y != to.y + stepY; y += stepY)
                path.Add(new Vector2Int(to.x, y));
            return path;
        }

        // ---------------------------------------------------------------- Umrechnung

        public Vector3 CellToWorld(Vector2Int c, Vector3 gridOrigin) =>
            gridOrigin + new Vector3(c.x * CellSize, 0f, c.y * CellSize);

        public Vector3 CellCenterToWorld(Vector2Int c, Vector3 gridOrigin) =>
            CellToWorld(c, gridOrigin) + new Vector3(CellSize * 0.5f, 0f, CellSize * 0.5f);

        public Vector2Int WorldToCell(Vector3 world, Vector3 gridOrigin)
        {
            var local = world - gridOrigin;
            return new Vector2Int(Mathf.FloorToInt(local.x / CellSize), Mathf.FloorToInt(local.z / CellSize));
        }
    }
}
