using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EpochCity
{
    /// <summary>
    /// Hinweisfenster beim Überfahren eines UI-Elements, über dem Element angezeigt (wie die Gebäude-Tooltips in
    /// Cities: Skylines und Anno). Attach hängt es an ein Element; der Text wird erst beim Zeigen erzeugt, damit er
    /// aktuell ist (z. B. gesperrt / verfügbar). Alle Elemente teilen sich ein Fenster auf eigenem Canvas.
    /// </summary>
    public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Func<string> text;

        private static RectTransform panel;
        private static TextMeshProUGUI label;
        private static Tooltip current;

        public static Tooltip Attach(GameObject target, Func<string> text)
        {
            var tooltip = target.GetComponent<Tooltip>() ?? target.AddComponent<Tooltip>();
            tooltip.text = text;
            return tooltip;
        }

        public void OnPointerEnter(PointerEventData eventData) => Show();

        public void OnPointerExit(PointerEventData eventData) => Hide();

        private void OnDisable() => Hide();

        public void Show()
        {
            string content = text?.Invoke();
            if (string.IsNullOrEmpty(content)) return;
            EnsurePanel();
            current = this;
            label.text = content;
            panel.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            Place();
        }

        public void Hide()
        {
            if (current != this || panel == null) return;
            panel.gameObject.SetActive(false);
            current = null;
        }

        /// <summary>Über dem Element, waagerecht mittig; am Bildschirmrand nach innen geschoben.</summary>
        private void Place()
        {
            Canvas.ForceUpdateCanvases(); // Reihe kann im selben Bild aufgebaut worden sein
            var target = (RectTransform)transform;
            var corners = new Vector3[4];
            target.GetWorldCorners(corners); // Overlay-Canvas: Weltkoordinaten = Bildschirmpixel
            float scale = panel.lossyScale.x;
            float halfWidth = panel.rect.width * scale * 0.5f;
            float x = Mathf.Clamp((corners[1].x + corners[2].x) * 0.5f, halfWidth + 8f, Screen.width - halfWidth - 8f);
            panel.position = new Vector3(x, corners[1].y + 10f * scale, 0f);
        }

        private static void EnsurePanel()
        {
            if (panel != null) return;
            var theme = UiTheme.Resolve(null);
            var canvas = UiFactory.CreateCanvas("TooltipCanvas", null);
            canvas.GetComponent<Canvas>().sortingOrder = 30;
            UnityEngine.Object.DontDestroyOnLoad(canvas.gameObject);

            panel = UiFactory.CreatePanel("Tooltip", canvas, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0f),
                new Vector2(360f, 0f), theme.panel);
            panel.GetComponent<Image>().raycastTarget = false; // darf das Element darunter nicht verdecken
            UiFactory.AddColumn(panel.gameObject, 12, 4f);
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            label = UiFactory.CreateText("Text", panel, "", theme.bodySize - 2f, TextAlignmentOptions.TopLeft, theme.text);
            panel.gameObject.SetActive(false);
        }
    }
}
