using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Passt die Darstellung der Straßenfelder an ihre Nachbarn an, sobald Straßen gebaut oder entfernt werden.
    /// Das Straßen-Prefab braucht dafür einen MeshFilter (Fahrbahn und Rand als zwei Materialien).
    /// </summary>
    public class RoadVisuals : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [Tooltip("Breite des Randstreifens neben der Fahrbahn in Metern")]
        [Min(0f)] public float edgeWidth = 0.22f;

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
            foreach (var c in dirty) Refresh(c);
        }

        private void Refresh(Vector2Int cell)
        {
            if (!city.RoadViews.TryGetValue(cell, out var view) || view == null) return;
            var road = city.Grid.GetRoad(cell);
            var filter = view.GetComponentInChildren<MeshFilter>();
            if (road == null || filter == null) return;

            int mask = RoadMeshBuilder.GetMask(city.Grid, cell);
            filter.sharedMesh = RoadMeshBuilder.Get(mask, city.Grid.CellSize, road.visualWidth, edgeWidth);
        }
    }
}
