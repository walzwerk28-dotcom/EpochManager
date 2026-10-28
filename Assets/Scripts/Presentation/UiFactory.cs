using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace EpochCity
{
    /// <summary>
    /// Gemeinsame Helfer für die HUDs, die ihre uGUI zur Laufzeit selbst aufbauen.
    /// </summary>
    public static class UiFactory
    {
        /// <summary>Sorgt für ein EventSystem mit dem Eingabemodul des neuen Input Systems.</summary>
        public static void EnsureEventSystem()
        {
            var es = Object.FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                return;
            }

            // Das alte Eingabemodul funktioniert mit dem neuen Input System nicht.
            var legacy = es.GetComponent<StandaloneInputModule>();
            if (legacy != null)
            {
                Object.DestroyImmediate(legacy);
                if (es.GetComponent<InputSystemUIInputModule>() == null)
                    es.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        /// <summary>Overlay-Canvas, skaliert auf 1920 x 1080.</summary>
        public static Transform CreateCanvas(string name, Transform parent)
        {
            var canvasGo = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(parent, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvasGo.transform;
        }

        public static RectTransform CreatePanel(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = sizeDelta;

            go.GetComponent<Image>().color = color;
            return rt;
        }

        public static void AddRow(GameObject go, TextAnchor alignment, int padding, float spacing)
        {
            var row = go.AddComponent<HorizontalLayoutGroup>();
            row.childAlignment = alignment;
            row.padding = new RectOffset(padding, padding, padding, padding);
            row.spacing = spacing;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
        }

        public static void AddColumn(GameObject go, int padding, float spacing)
        {
            var column = go.AddComponent<VerticalLayoutGroup>();
            column.childAlignment = TextAnchor.UpperCenter;
            column.padding = new RectOffset(padding, padding, padding, padding);
            column.spacing = spacing;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
        }

        public static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size,
            TextAlignmentOptions alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = alignment;
            t.raycastTarget = false; // Text soll Klicks nicht blockieren
            return t;
        }

        /// <summary>Button mit Beschriftung, die sich bis <paramref name="fontSize"/> automatisch anpasst.</summary>
        public static Button CreateButton(Transform parent, string name, string label, Vector2 size,
            float fontSize, Color color, Color textColor, UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.color = color;

            var element = go.GetComponent<LayoutElement>();
            element.preferredWidth = size.x;
            element.preferredHeight = size.y;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var text = CreateText("Label", go.transform, label, fontSize, TextAlignmentOptions.Center, textColor);
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = fontSize;

            var rt = text.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8f, 6f);
            rt.offsetMax = new Vector2(-8f, -6f);

            return button;
        }
    }
}
