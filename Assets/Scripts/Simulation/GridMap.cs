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
        private readonly ZoneDefinition[,] zones;
        private readonly HashSet<Vector2Int> zonedCells = new HashSet<Vector2Int>();

        private static readonly Vector2Int[] Neighbours =
            { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        public GridMap(int width, int height, float cellSize)
        {
            Width = width;
            Height = height;
            CellSize = cellSize;
            cells = new BuildingInstance[width, height];
            roads = new RoadDefinition[width, height];
            zones = new ZoneDefinition[width, height];
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
        public IEnumerable<Vector2Int> GetAdjacentCells(BuildingInstance inst) =>
            GetAdjacentCells(inst.Origin, inst.Definition.size);

        /// <summary>Zellen, die seitlich an eine Grundfläche grenzen (ohne Ecken).</summary>
        public static IEnumerable<Vector2Int> GetAdjacentCells(Vector2Int origin, Vector2Int size)
        {
            for (int x = 0; x < size.x; x++)
            {
                yield return new Vector2Int(origin.x + x, origin.y - 1);
                yield return new Vector2Int(origin.x + x, origin.y + size.y);
            }
            for (int y = 0; y < size.y; y++)
            {
                yield return new Vector2Int(origin.x - 1, origin.y + y);
                yield return new Vector2Int(origin.x + size.x, origin.y + y);
            }
        }

        /// <summary>Liegt an dieser Seite der Grundfläche mindestens ein Straßenfeld?</summary>
        public bool SideHasRoad(Vector2Int origin, Vector2Int size, Vector2Int side)
        {
            if (side == Vector2Int.down || side == Vector2Int.up)
            {
                int y = side == Vector2Int.down ? origin.y - 1 : origin.y + size.y;
                for (int x = 0; x < size.x; x++)
                    if (HasRoad(new Vector2Int(origin.x + x, y))) return true;
            }
            else
            {
                int x = side == Vector2Int.left ? origin.x - 1 : origin.x + size.x;
                for (int y = 0; y < size.y; y++)
                    if (HasRoad(new Vector2Int(x, origin.y + y))) return true;
            }
            return false;
        }

        public bool TouchesRoad(BuildingInstance inst) => TouchesRoad(inst.Origin, inst.Definition.size);

        /// <summary>Grenzt die Grundfläche an mindestens ein Straßenfeld?</summary>
        public bool TouchesRoad(Vector2Int origin, Vector2Int size)
        {
            foreach (var c in GetAdjacentCells(origin, size))
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

        // ---------------------------------------------------------------- Zonen

        public ZoneDefinition GetZone(Vector2Int c) => InBounds(c) ? zones[c.x, c.y] : null;

        /// <summary>Alle Felder mit Zone (auch bebaute), damit nicht das ganze Raster durchsucht werden muss.</summary>
        public IReadOnlyCollection<Vector2Int> ZonedCells => zonedCells;

        /// <summary>Auf diesem Feld darf eine Zone angelegt werden (im Raster, frei von Gebäude und Straße).</summary>
        public bool CanZone(Vector2Int c) => InBounds(c) && cells[c.x, c.y] == null && roads[c.x, c.y] == null;

        /// <summary>Setzt oder entfernt (null) die Zone eines Felds.</summary>
        public void SetZone(Vector2Int c, ZoneDefinition zone)
        {
            if (!InBounds(c)) return;
            zones[c.x, c.y] = zone;
            if (zone != null) zonedCells.Add(c);
            else zonedCells.Remove(c);
        }

        /// <summary>Alle Felder im Rechteck zwischen zwei Ecken (beide eingeschlossen).</summary>
        public static List<Vector2Int> RectCells(Vector2Int a, Vector2Int b)
        {
            var list = new List<Vector2Int>();
            for (int x = Mathf.Min(a.x, b.x); x <= Mathf.Max(a.x, b.x); x++)
                for (int y = Mathf.Min(a.y, b.y); y <= Mathf.Max(a.y, b.y); y++)
                    list.Add(new Vector2Int(x, y));
            return list;
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
