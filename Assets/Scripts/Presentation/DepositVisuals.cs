using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Zeigt die Rohstoff-Vorkommen: je Feld eine flache Bodenkachel (DepositDefinition.tilePrefab, z. B. Waldboden)
    /// und einige Deko-Modelle (Bäume, Felsen) mit Versatz, Drehung und Größe aus einem Zufall, der fest an der
    /// Feldposition hängt – nach dem Laden sieht der Wald gleich aus. Unter Gebäuden und Straßen ausgeblendet,
    /// gerodete Felder verlieren ihre Darstellung. Gleicht sich bei jeder Änderung von Vorkommen, Gebäuden oder
    /// Straßen ab.
    /// </summary>
    public class DepositVisuals : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [Tooltip("Anteil der Feldbreite, um den die Deko höchstens aus der Feldmitte rückt")]
        [Range(0f, 0.5f)] [SerializeField] private float spread = 0.32f;

        private class CellView
        {
            public DepositDefinition Deposit;
            public GameObject Root;
            public int Decor;
        }

        private readonly Dictionary<Vector2Int, CellView> views = new Dictionary<Vector2Int, CellView>();
        private bool dirty = true;

        /// <summary>Anzahl der Deko-Modelle (für Tests und Messungen).</summary>
        public int DecorCount { get; private set; }

        private void OnEnable()
        {
            if (city == null) return;
            city.DepositsChanged += MarkDirty;
            city.ConnectionsChanged += MarkDirty; // nach jedem Bau oder Abriss von Gebäuden und Straßen
            dirty = true;
        }

        private void OnDisable()
        {
            if (city == null) return;
            city.DepositsChanged -= MarkDirty;
            city.ConnectionsChanged -= MarkDirty;
        }

        private void MarkDirty() => dirty = true;

        private void LateUpdate()
        {
            if (!dirty || city == null || city.Grid == null) return;
            dirty = false;
            var grid = city.Grid;

            // Entfernte oder geänderte Vorkommen
            var stale = new List<Vector2Int>();
            foreach (var entry in views)
                if (grid.GetDeposit(entry.Key) != entry.Value.Deposit) stale.Add(entry.Key);
            foreach (var c in stale)
            {
                DecorCount -= views[c].Decor;
                Destroy(views[c].Root);
                views.Remove(c);
            }

            foreach (var c in grid.DepositCells)
            {
                if (!views.TryGetValue(c, out var view))
                    views[c] = view = Create(c, grid.GetDeposit(c));
                bool free = grid.GetAt(c) == null && !grid.HasRoad(c);
                if (view.Root.activeSelf != free) view.Root.SetActive(free);
            }
        }

        private CellView Create(Vector2Int c, DepositDefinition deposit)
        {
            var center = city.Grid.CellCenterToWorld(c, city.transform.position);
            var root = new GameObject($"Vorkommen_{c.x}_{c.y}");
            root.transform.SetParent(transform, false);
            root.transform.position = center;

            if (deposit.tilePrefab != null) Instantiate(deposit.tilePrefab, center, Quaternion.identity, root.transform);

            var view = new CellView { Deposit = deposit, Root = root };
            var prefabs = deposit.decorPrefabs;
            if (prefabs != null && prefabs.Length > 0)
            {
                // Fester Zufall je Feld, unabhängig von der Reihenfolge des Aufbaus
                var random = new System.Random(c.x * 73856093 ^ c.y * 19349663);
                int count = random.Next(deposit.decorPerCell.x, Mathf.Max(deposit.decorPerCell.x, deposit.decorPerCell.y) + 1);
                float half = city.cellSize * spread;
                for (int i = 0; i < count; i++)
                {
                    var prefab = prefabs[random.Next(prefabs.Length)];
                    if (prefab == null) continue;
                    var offset = new Vector3(Range(random, -half, half), 0f, Range(random, -half, half));
                    var decor = Instantiate(prefab, center + offset,
                        Quaternion.Euler(0f, Range(random, 0f, 360f), 0f), root.transform);
                    decor.transform.localScale *= Range(random, deposit.decorScale.x, deposit.decorScale.y);
                    view.Decor++;
                }
            }
            DecorCount += view.Decor;
            return view;
        }

        private static float Range(System.Random random, float min, float max) =>
            min + (float)random.NextDouble() * (max - min);
    }
}
