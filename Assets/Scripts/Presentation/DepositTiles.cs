using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Vorläufige Darstellung der Rohstoff-Vorkommen: eine flache Kachel (DepositDefinition.tilePrefab) auf jedem
    /// freien Feld mit Vorkommen; unter Gebäuden und Straßen ausgeblendet. Bäume und Felsen folgen später.
    /// Gleicht sich bei jeder Änderung von Vorkommen, Gebäuden oder Straßen ab.
    /// </summary>
    public class DepositTiles : MonoBehaviour
    {
        [SerializeField] private CityController city;

        private readonly Dictionary<Vector2Int, (DepositDefinition deposit, GameObject view)> tiles =
            new Dictionary<Vector2Int, (DepositDefinition, GameObject)>();
        private bool dirty = true;

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
            foreach (var entry in tiles)
                if (grid.GetDeposit(entry.Key) != entry.Value.deposit) stale.Add(entry.Key);
            foreach (var c in stale)
            {
                if (tiles[c].view != null) Destroy(tiles[c].view);
                tiles.Remove(c);
            }

            foreach (var c in grid.DepositCells)
            {
                var deposit = grid.GetDeposit(c);
                if (!tiles.TryGetValue(c, out var tile))
                {
                    var view = deposit.tilePrefab != null
                        ? Instantiate(deposit.tilePrefab, grid.CellCenterToWorld(c, city.transform.position),
                            Quaternion.identity, transform)
                        : null;
                    tiles[c] = tile = (deposit, view);
                }
                bool free = grid.GetAt(c) == null && !grid.HasRoad(c);
                if (tile.view != null && tile.view.activeSelf != free) tile.view.SetActive(free);
            }
        }
    }
}
