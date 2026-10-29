using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Zeigt die Zonen-Kacheln nur, solange das Zonen-Werkzeug aktiv ist (Wohnen, Gewerbe oder Aufheben), wie in
    /// Cities: Skylines. Sonst bleiben die Zonen bestehen und wachsen weiter, nur ihre Farbe ist ausgeblendet.
    /// Neue Kacheln (Zonen setzen, Spielstand laden) bekommen über ZonesChanged sofort den richtigen Zustand.
    /// </summary>
    public class ZoneTileVisibility : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [SerializeField] private BuildingPlacer placer;

        private bool shown = true;
        private bool dirty = true;

        private void OnEnable()
        {
            if (city != null) city.ZonesChanged += MarkDirty;
            dirty = true;
        }

        private void OnDisable()
        {
            if (city != null) city.ZonesChanged -= MarkDirty;
        }

        private void MarkDirty() => dirty = true;

        private void LateUpdate()
        {
            if (city == null || placer == null) return;
            bool show = placer.ZoneMode;
            if (show == shown && !dirty) return;
            shown = show;
            dirty = false;
            foreach (var view in city.ZoneViews.Values)
                if (view != null && view.activeSelf != show) view.SetActive(show);
        }
    }
}
