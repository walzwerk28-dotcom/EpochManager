using UnityEngine;
using UnityEngine.InputSystem;

namespace EpochCity
{
    /// <summary>
    /// Overlay-Karte für den Anbindungswert (Taste C oder HUD-Button): färbt Gebäude und Straßen
    /// von Grün (100 %) über Gelb bis Rot (keine Anbindung). Die Materialien selbst bleiben unverändert,
    /// die Farbe kommt über einen MaterialPropertyBlock und wird beim Ausschalten entfernt.
    /// </summary>
    public class ConnectionOverlay : MonoBehaviour
    {
        [SerializeField] private CityController city;
        public Color goodColor = new Color(0.25f, 0.8f, 0.3f, 1f);
        public Color mediumColor = new Color(0.95f, 0.85f, 0.2f, 1f);
        public Color badColor = new Color(0.9f, 0.2f, 0.2f, 1f);

        public bool Visible { get; private set; }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock block;

        private void OnEnable()
        {
            if (city != null) city.ConnectionsChanged += Refresh;
        }

        private void OnDisable()
        {
            if (city != null) city.ConnectionsChanged -= Refresh;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.cKey.wasPressedThisFrame) Toggle();
        }

        public void Toggle()
        {
            Visible = !Visible;
            Refresh();
        }

        public Color ColorFor(float efficiency)
        {
            if (efficiency <= 0f) return badColor;
            return efficiency < 0.5f
                ? Color.Lerp(badColor, mediumColor, efficiency / 0.5f)
                : Color.Lerp(mediumColor, goodColor, (efficiency - 0.5f) / 0.5f);
        }

        private void Refresh()
        {
            if (city == null) return;

            foreach (var b in city.Buildings)
                Apply(b.View, Visible ? ColorFor(b.Connection) : (Color?)null);

            foreach (var entry in city.RoadViews)
            {
                Color? color = null;
                if (Visible)
                    color = city.Connections.TryGetRoadEfficiency(entry.Key, out float e) ? ColorFor(e) : badColor;
                Apply(entry.Value, color);
            }
        }

        private void Apply(GameObject view, Color? color)
        {
            if (view == null) return;
            if (block == null) block = new MaterialPropertyBlock();

            foreach (var r in view.GetComponentsInChildren<Renderer>())
            {
                if (color == null)
                {
                    r.SetPropertyBlock(null);
                    continue;
                }
                block.Clear();
                block.SetColor(BaseColorId, color.Value);
                block.SetColor(ColorId, color.Value);
                r.SetPropertyBlock(block);
            }
        }
    }
}
