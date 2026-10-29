using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace EpochCity
{
    /// <summary>Aussehen eines Panels: Fenster mit Schatten, flache Fläche (Balken, Leisten) oder ohne Grafik.</summary>
    public enum PanelStyle { Window, Flat, Plain }

    /// <summary>
    /// Gemeinsame Helfer für die HUDs, die ihre uGUI zur Laufzeit selbst aufbauen. Form, Schatten und Button-Zustände
    /// kommen aus UiTheme.Current; die abgerundete Grafik entsteht im Code als 9-Slice-Sprite (keine Bilddateien).
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
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Color color,
            PanelStyle style = PanelStyle.Window)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = sizeDelta;

            var image = go.GetComponent<Image>();
            image.color = color;
            if (style != PanelStyle.Plain) MakeRounded(image);
            if (style == PanelStyle.Window) AddShadow(go);
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
            MakeRounded(image);

            var element = go.GetComponent<LayoutElement>();
            element.preferredWidth = size.x;
            element.preferredHeight = size.y;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.colors = ButtonColors();
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

        /// <summary>Kopfzeile eines Fensters: Titel in der Überschriftfarbe, rechts ein Schließen-Button.</summary>
        public static TextMeshProUGUI WindowHeader(Transform window, string title, UiTheme theme, UnityAction onClose)
        {
            var header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(window, false);
            AddRow(header, TextAnchor.MiddleLeft, 0, 8f);
            var text = CreateText("Title", header.transform, title, theme.titleSize, TextAlignmentOptions.MidlineLeft,
                theme.heading);
            text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            CreateButton(header.transform, "Close", "×", new Vector2(40f, 36f), 24f, theme.button, theme.text, onClose);
            return text;
        }

        // ---------------------------------------------------------------- Stil

        /// <summary>
        /// Zustände als Helligkeit über der Grundfarbe: Die HUDs setzen die Grundfarbe je nach Zustand (ausgewählt,
        /// gesperrt, …) direkt am Image, der Button dunkelt sie im Ruhezustand leicht ab und zeigt sie beim Überfahren voll.
        /// </summary>
        public static ColorBlock ButtonColors()
        {
            var theme = UiTheme.Resolve(null);
            float idle = theme.idleBrightness, pressed = theme.pressedBrightness;
            var block = ColorBlock.defaultColorBlock;
            block.normalColor = new Color(idle, idle, idle, 1f);
            block.highlightedColor = Color.white;
            block.pressedColor = new Color(pressed, pressed, pressed, 1f);
            block.selectedColor = block.normalColor;
            block.disabledColor = new Color(idle, idle, idle, 0.9f);
            block.colorMultiplier = 1f;
            block.fadeDuration = 0.08f;
            return block;
        }

        /// <summary>Abgerundete Ecken über ein 9-Slice-Sprite mit dem Radius aus dem Farbschema.</summary>
        public static void MakeRounded(Image image)
        {
            var sprite = RoundedSprite(UiTheme.Resolve(null).cornerRadius);
            if (sprite == null) return;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
        }

        public static void AddShadow(GameObject go)
        {
            var theme = UiTheme.Resolve(null);
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = theme.shadow;
            shadow.effectDistance = theme.shadowOffset;
        }

        private static Sprite rounded;
        private static int roundedRadius = -1;

        /// <summary>Weißes Quadrat mit weich abgerundeten Ecken; bei kleinen Flächen verkleinert Unity die Ränder (Pillenform).</summary>
        private static Sprite RoundedSprite(int radius)
        {
            if (radius <= 0) return null;
            if (rounded != null && roundedRadius == radius) return rounded;

            int size = radius * 2 + 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "UiRounded", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // Abstand zum nächsten Eckmittelpunkt; innen 1, an der Kante weich auf 0
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    var c = new Vector2(Mathf.Clamp(p.x, radius, size - radius), Mathf.Clamp(p.y, radius, size - radius));
                    float alpha = Mathf.Clamp01(radius - Vector2.Distance(p, c) + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            tex.SetPixels32(pixels);
            tex.Apply();

            float border = radius + 1;
            rounded = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            rounded.name = "UiRounded";
            roundedRadius = radius;
            return rounded;
        }
    }
}
