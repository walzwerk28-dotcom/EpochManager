using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Zeigt die Rohstoff-Vorkommen: je Feld eine flache Bodenkachel (DepositDefinition.tilePrefab, z. B. Waldboden)
    /// und einige Deko-Modelle (Bäume, Felsen) mit Versatz, Drehung und Größe aus einem Zufall, der fest an der
    /// Feldposition hängt – nach dem Laden sieht der Wald gleich aus. Unter Gebäuden und Straßen ausgeblendet,
    /// gerodete Felder verlieren ihre Darstellung; junge Felder (gefällt oder gepflanzt) zeigen kleine Bäume, die mit
    /// dem Alter wachsen. Gleicht sich bei jeder Änderung von Vorkommen, Gebäuden oder
    /// Straßen ab.
    /// </summary>
    public class DepositVisuals : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [Tooltip("Anteil der Feldbreite, um den die Deko höchstens aus der Feldmitte rückt")]
        [Range(0f, 0.5f)] [SerializeField] private float spread = 0.32f;

        [Tooltip("Größe der Deko auf einem gerade gefällten oder gepflanzten Feld (Anteil der vollen Größe)")]
        [Range(0.05f, 1f)] [SerializeField] private float saplingScale = 0.3f;

        private class CellView
        {
            public DepositDefinition Deposit;
            public GameObject Root;
            public int Decor;
            public readonly List<Transform> DecorTransforms = new List<Transform>();
            public readonly List<Vector3> FullScales = new List<Vector3>();
            public float Growth = 1f;
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
                SetGrowth(view, GetGrowth(c, view.Deposit));
            }
        }

        /// <summary>Wuchs eines Felds: 1 ausgewachsen, junge Felder von saplingScale bis kurz vor 1 nach Alter.</summary>
        private float GetGrowth(Vector2Int c, DepositDefinition deposit)
        {
            if (!city.Grid.YoungDeposits.TryGetValue(c, out int age)) return 1f;
            int needed = city.IsTended(c, deposit)
                ? Mathf.Min(deposit.regrowMonthsTended, deposit.regrowMonths) : deposit.regrowMonths;
            return Mathf.Lerp(saplingScale, 0.9f, Mathf.Clamp01(age / (float)Mathf.Max(1, needed)));
        }

        private static void SetGrowth(CellView view, float growth)
        {
            if (Mathf.Approximately(view.Growth, growth)) return;
            view.Growth = growth;
            for (int i = 0; i < view.DecorTransforms.Count; i++)
                view.DecorTransforms[i].localScale = view.FullScales[i] * growth;
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
                    view.DecorTransforms.Add(decor.transform);
                    view.FullScales.Add(decor.transform.localScale);
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
