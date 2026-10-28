using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Erzeugt Straßenstücke aus der Nachbarschaft eines Felds: ein Mittelstück plus je ein Arm zu jedem
    /// Nachbarfeld mit Straße. Daraus ergeben sich Ende, Gerade, Kurve, T-Kreuzung und Kreuzung von selbst.
    /// Das Mesh hat zwei Teilflächen: 0 = Fahrbahn, 1 = etwas breiterer, tiefer liegender Rand.
    /// </summary>
    public static class RoadMeshBuilder
    {
        public const int East = 1, West = 2, North = 4, South = 8;

        private const float SurfaceHeight = 0.05f;
        private const float EdgeHeight = 0.035f;

        private static readonly Dictionary<(int, float, float, float), Mesh> Cache =
            new Dictionary<(int, float, float, float), Mesh>();

        /// <summary>Bitmaske der Nachbarfelder mit Straße (Osten = +x, Norden = +z).</summary>
        public static int GetMask(GridMap grid, Vector2Int c)
        {
            int mask = 0;
            if (grid.HasRoad(c + Vector2Int.right)) mask |= East;
            if (grid.HasRoad(c + Vector2Int.left)) mask |= West;
            if (grid.HasRoad(c + Vector2Int.up)) mask |= North;
            if (grid.HasRoad(c + Vector2Int.down)) mask |= South;
            return mask;
        }

        /// <summary>Zwischengespeichertes Stück für diese Form; alle Felder mit gleicher Nachbarschaft teilen es.</summary>
        public static Mesh Get(int mask, float cellSize, float width, float edgeWidth)
        {
            var key = (mask, cellSize, width, edgeWidth);
            if (!Cache.TryGetValue(key, out var mesh) || mesh == null)
            {
                mesh = Build(mask, cellSize, width, edgeWidth);
                Cache[key] = mesh;
            }
            return mesh;
        }

        public static Mesh Build(int mask, float cellSize, float width, float edgeWidth)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var surface = new List<int>();
            var edge = new List<int>();

            float cellHalf = cellSize * 0.5f;
            AddShape(mask, width * 0.5f, cellHalf, SurfaceHeight, cellSize, vertices, uvs, surface);
            if (edgeWidth > 0f)
                AddShape(mask, width * 0.5f + edgeWidth, cellHalf, EdgeHeight, cellSize, vertices, uvs, edge);

            var mesh = new Mesh { name = $"Road_{mask}" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(surface, 0);
            mesh.SetTriangles(edge, 1);
            var normals = new Vector3[vertices.Count];
            for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.up;
            mesh.normals = normals;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddShape(int mask, float half, float cellHalf, float y, float cellSize,
            List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
        {
            half = Mathf.Min(half, cellHalf);
            AddQuad(-half, -half, half, half, y, cellSize, vertices, uvs, triangles);
            if (half >= cellHalf) return; // füllt das ganze Feld, keine Arme nötig

            if ((mask & East) != 0) AddQuad(half, -half, cellHalf, half, y, cellSize, vertices, uvs, triangles);
            if ((mask & West) != 0) AddQuad(-cellHalf, -half, -half, half, y, cellSize, vertices, uvs, triangles);
            if ((mask & North) != 0) AddQuad(-half, half, half, cellHalf, y, cellSize, vertices, uvs, triangles);
            if ((mask & South) != 0) AddQuad(-half, -cellHalf, half, -half, y, cellSize, vertices, uvs, triangles);
        }

        /// <summary>Waagerechtes Rechteck, von oben gesehen im Uhrzeigersinn (Vorderseite nach oben).</summary>
        private static void AddQuad(float x0, float z0, float x1, float z1, float y, float cellSize,
            List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
        {
            int i = vertices.Count;
            vertices.Add(new Vector3(x0, y, z0));
            vertices.Add(new Vector3(x0, y, z1));
            vertices.Add(new Vector3(x1, y, z1));
            vertices.Add(new Vector3(x1, y, z0));
            foreach (var v in vertices.GetRange(i, 4))
                uvs.Add(new Vector2(v.x / cellSize + 0.5f, v.z / cellSize + 0.5f));
            triangles.Add(i); triangles.Add(i + 1); triangles.Add(i + 2);
            triangles.Add(i); triangles.Add(i + 2); triangles.Add(i + 3);
        }
    }
}
