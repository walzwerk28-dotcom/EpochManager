using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EpochCity
{
    /// <summary>
    /// Handelsfenster (wie das Kontor in Anno): je handelbarer Ware ein Modus (Aus, Verkaufen, Kaufen) mit einer
    /// Grenze, dazu Vorrat, Preise und die Menge des letzten Monats; im Kopf die Kapazität der Häfen, im Fuß die
    /// Bilanz. Öffnet sich über den Button im Infofenster des Hafens. Es ändert nur die Einstellungen des
    /// <see cref="TradeSystem"/>, gerechnet wird dort. Baut sich wie die anderen HUDs selbst auf.
    /// </summary>
    public class TradeWindow : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [SerializeField] private TradeSystem trade;
        [Tooltip("Verzeichnis der handelbaren Waren (leer = das des SaveSystems in der Szene)")]
        [SerializeField] private ContentRegistry registry;

        [Header("Aussehen")]
        [Tooltip("Gemeinsames Farbschema; leer = Standardwerte")]
        [SerializeField] private UiTheme theme;

        private const int Step = 10;

        private class Row
        {
            public ResourceDefinition resource;
            public RectTransform root;
            public TextMeshProUGUI stock, prices, limit, last;
            public Button off, sell, buy, minus, plus;
        }

        private RectTransform window, list;
        private TextMeshProUGUI header, ruleText, footer;
        private readonly List<Row> rows = new List<Row>();
        private float refreshTimer;

        public bool Visible => window != null && window.gameObject.activeSelf;
        public RectTransform Root => window;
        public int RowCount => rows.Count;
        public string HeaderText => header != null ? header.text : "";
        public string FooterText => footer != null ? footer.text : "";

        /// <summary>Die Zeile einer Ware (für Tests und Bilder), sonst null.</summary>
        public RectTransform RowOf(ResourceDefinition resource)
        {
            foreach (var r in rows) if (r.resource == resource) return r.root;
            return null;
        }

        public void Toggle() => SetVisible(!Visible);

        public void Show() => SetVisible(true);

        public void SetVisible(bool visible)
        {
            if (window == null) return;
            window.gameObject.SetActive(visible);
            if (visible)
            {
                RebuildRows();
                Refresh();
            }
        }

        private void Awake()
        {
            theme = UiTheme.Resolve(theme);
            UiFactory.EnsureEventSystem();
            Build();
            SetVisible(false);
        }

        private void Update()
        {
            if (!Visible) return;
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.2f;
            Refresh();
        }

        // ---------------------------------------------------------------- Aufbau

        private void Build()
        {
            var canvas = UiFactory.CreateCanvas("TradeCanvas", transform);
            // Über den anderen Fenstern, aber unter der TopBar (Menü) und den Tooltips
            canvas.GetComponent<Canvas>().sortingOrder = 5;
            window = UiFactory.CreatePanel("TradeWindow", canvas, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(1120f, 720f), theme.panel);
            window.anchoredPosition = new Vector2(70f, -10f); // etwas nach rechts, damit Stadt- und Infofenster frei bleiben
            UiFactory.AddColumn(window.gameObject, 20, 10f);
            window.GetComponent<VerticalLayoutGroup>().childForceExpandHeight = false;
            UiFactory.WindowHeader(window, "Handel", theme, () => SetVisible(false));

            header = UiFactory.CreateText("Capacity", window, "", theme.bodySize, TextAlignmentOptions.MidlineLeft, theme.text);
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
            ruleText = UiFactory.CreateText("Rule", window, "", theme.smallSize, TextAlignmentOptions.MidlineLeft,
                theme.textMuted);
            ruleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            var scroll = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image),
                typeof(Mask), typeof(LayoutElement));
            scroll.transform.SetParent(window, false);
            scroll.GetComponent<LayoutElement>().flexibleHeight = 1f;
            var scrollImage = scroll.GetComponent<Image>();
            scrollImage.color = theme.panelSoft;
            UiFactory.MakeRounded(scrollImage);
            scroll.GetComponent<Mask>().showMaskGraphic = true;

            var content = new GameObject("List", typeof(RectTransform));
            content.transform.SetParent(scroll.transform, false);
            list = (RectTransform)content.transform;
            list.anchorMin = new Vector2(0f, 1f);
            list.anchorMax = new Vector2(1f, 1f);
            list.pivot = new Vector2(0.5f, 1f);
            list.sizeDelta = Vector2.zero;
            UiFactory.AddColumn(content, 8, 6f);
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = scroll.GetComponent<ScrollRect>();
            scrollRect.content = list;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            footer = UiFactory.CreateText("Footer", window, "", theme.bodySize, TextAlignmentOptions.MidlineLeft, theme.text);
            footer.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
        }

        private ContentRegistry Registry
        {
            get
            {
                if (registry == null)
                {
                    var save = FindAnyObjectByType<SaveSystem>();
                    if (save != null) registry = save.registry;
                }
                return registry;
            }
        }

        /// <summary>Baut die Zeilen neu, wenn sich die Liste der handelbaren Waren geändert hat (oder beim Öffnen).</summary>
        private void RebuildRows()
        {
            if (trade == null) trade = FindAnyObjectByType<TradeSystem>();
            if (city == null) city = FindAnyObjectByType<CityController>();
            var wanted = new List<ResourceDefinition>();
            if (Registry != null && Registry.entries != null)
                foreach (var entry in Registry.entries)
                    if (entry is ResourceDefinition res && TradeSystem.IsTradable(res)) wanted.Add(res);

            bool same = wanted.Count == rows.Count;
            for (int i = 0; same && i < wanted.Count; i++) same = rows[i].resource == wanted[i];
            if (same) return;

            for (int i = list.childCount - 1; i >= 0; i--)
            {
                var child = list.GetChild(i).gameObject;
                child.transform.SetParent(null);
                Destroy(child);
            }
            rows.Clear();
            foreach (var res in wanted) rows.Add(BuildRow(res));
            if (wanted.Count == 0)
            {
                var empty = UiFactory.CreateText("Empty", list, "Keine handelbaren Waren.", theme.bodySize,
                    TextAlignmentOptions.Center, theme.textMuted);
                empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 60f;
            }
        }

        private Row BuildRow(ResourceDefinition res)
        {
            var row = new Row { resource = res };
            row.root = UiFactory.CreatePanel("Row_" + res.name, list, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, theme.panel, PanelStyle.Flat);
            UiFactory.AddRow(row.root.gameObject, TextAnchor.MiddleLeft, 8, 8f);
            row.root.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;

            if (res.icon != null) UiFactory.AddIcon(row.root, res.icon, 30f, Color.white);
            var name = UiFactory.CreateText("Name", row.root, res.displayName, theme.bodySize, TextAlignmentOptions.MidlineLeft,
                theme.text);
            Fixed(name, res.icon != null ? 118f : 150f);

            row.stock = Cell("Stock", row, 96f);
            row.prices = Cell("Prices", row, 232f);
            row.off = ModeButton(row, "Off", "Aus", TradeMode.Off);
            row.sell = ModeButton(row, "Sell", "Verkaufen", TradeMode.Sell);
            row.buy = ModeButton(row, "Buy", "Kaufen", TradeMode.Buy);
            row.minus = StepButton(row, "Minus", "−", -1);
            row.plus = StepButton(row, "Plus", "+", 1);
            row.limit = Cell("Limit", row, 150f);
            row.last = Cell("Last", row, 64f);
            row.last.alignment = TextAlignmentOptions.MidlineRight;
            return row;
        }

        private TextMeshProUGUI Cell(string name, Row row, float width)
        {
            var t = UiFactory.CreateText(name, row.root, "", theme.smallSize, TextAlignmentOptions.MidlineLeft, theme.text);
            Fixed(t, width);
            return t;
        }

        private static void Fixed(TextMeshProUGUI t, float width)
        {
            var e = t.gameObject.AddComponent<LayoutElement>();
            e.preferredWidth = e.minWidth = width;
            t.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private Button ModeButton(Row row, string name, string label, TradeMode mode)
        {
            var button = UiFactory.CreateButton(row.root, name, label, new Vector2(mode == TradeMode.Sell ? 104f : 74f, 40f),
                theme.smallSize, theme.button, theme.text, () => SetMode(row, mode));
            Tooltip.Attach(button.gameObject, () => ModeTooltip(row.resource, mode));
            return button;
        }

        private Button StepButton(Row row, string name, string label, int direction)
        {
            var button = UiFactory.CreateButton(row.root, name, label, new Vector2(40f, 40f), 24f, theme.button, theme.text,
                () => ChangeLimit(row, direction));
            Tooltip.Attach(button.gameObject, () =>
                "<b>Grenze</b>\nVerkaufen: Vorrat, der bleibt (mindestens die Rücklage für die Häuser). " +
                $"Kaufen: Vorrat, bis zu dem aufgefüllt wird.\nSchritt {Step}, mit Umschalttaste 1.");
            return button;
        }

        private string ModeTooltip(ResourceDefinition res, TradeMode mode)
        {
            switch (mode)
            {
                case TradeMode.Sell:
                    return $"<b>Verkaufen</b>\nDer Vorrat über der Grenze und über der Rücklage für die Häuser geht jeden " +
                           $"Monat zum Verkaufspreis ({trade.SellPrice(res):0.##}) weg. Nie unter die Rücklage.\n" +
                           "Die Kapazität der Häfen wird in der Reihenfolge der Liste verbraucht.";
                case TradeMode.Buy:
                    return $"<b>Kaufen</b>\nDer Vorrat wird jeden Monat zum Kaufpreis ({trade.BuyPrice(res):0.##}) bis zur " +
                           "Grenze aufgefüllt, soweit Geld, Lagerplatz und Kapazität der Häfen reichen.\n" +
                           "Die Kapazität wird in der Reihenfolge der Liste verbraucht.";
                default:
                    return "<b>Aus</b>\nDiese Ware wird nicht gehandelt.";
            }
        }

        // ---------------------------------------------------------------- Aktionen

        private void SetMode(Row row, TradeMode mode)
        {
            if (trade == null) return;
            var s = trade.GetSetting(row.resource);
            trade.SetSetting(row.resource, mode, s.limit);
            Refresh();
        }

        private void ChangeLimit(Row row, int direction)
        {
            if (trade == null) return;
            var s = trade.GetSetting(row.resource);
            var kb = Keyboard.current;
            int step = kb != null && kb.shiftKey.isPressed ? 1 : Step;
            trade.SetSetting(row.resource, s.mode, Mathf.Max(0, s.limit + direction * step));
            Refresh();
        }

        // ---------------------------------------------------------------- Anzeige

        /// <summary>Füllt alle Texte und Button-Farben neu (alle 0,2 s und nach jeder Eingabe).</summary>
        public void Refresh()
        {
            if (trade == null || city == null || window == null) return;
            string currency = city.currentEpoch != null ? city.currentEpoch.currencyName : "";
            int capacity = trade.Capacity;
            header.text = capacity > 0
                ? $"Kapazität {capacity} Einheiten je Monat ({CountHarbors()} {(CountHarbors() == 1 ? "Hafen" : "Häfen")})"
                : $"<color={UiTheme.Hex(theme.danger)}>Kein Hafen in Betrieb – es wird nicht gehandelt</color>";
            ruleText.text = $"Verkauf {trade.sellFactor * 100f:0} %, Kauf {trade.buyFactor * 100f:0} % des Grundpreises";

            string muted = UiTheme.Hex(theme.textMuted);
            string good = UiTheme.Hex(theme.positive), bad = UiTheme.Hex(theme.danger);
            int storage = city.StorageCapacity;
            var buyColor = Color.Lerp(theme.button, theme.heading, 0.55f);
            var offColor = Color.Lerp(theme.button, theme.text, 0.25f);
            foreach (var row in rows)
            {
                var res = row.resource;
                var s = trade.GetSetting(res);
                float have = city.Stock.Get(res);
                row.stock.text = $"{have:0.#} <color={muted}>/ {storage}</color>";
                row.prices.text = $"<color={muted}>verkauft</color> {trade.SellPrice(res):0.00} · " +
                                  $"<color={muted}>kauft</color> {trade.BuyPrice(res):0.00}";

                row.off.targetGraphic.color = s.mode == TradeMode.Off ? offColor : theme.button;
                row.sell.targetGraphic.color = s.mode == TradeMode.Sell ? theme.accent : theme.button;
                row.buy.targetGraphic.color = s.mode == TradeMode.Buy ? buyColor : theme.button;

                bool active = s.mode != TradeMode.Off;
                row.minus.interactable = row.plus.interactable = active;
                row.minus.targetGraphic.color = row.plus.targetGraphic.color = active ? theme.button : theme.locked;
                row.limit.text = !active ? $"<color={muted}>–</color>"
                    : s.mode == TradeMode.Sell ? $"über {s.limit} halten" : $"bis {s.limit} auffüllen";

                int amount = 0;
                foreach (var t in trade.LastTrades) if (t.resource == res) amount = t.amount;
                row.last.text = amount > 0 ? $"<color={good}>+{amount}</color>"
                    : amount < 0 ? $"<color={bad}>−{-amount}</color>" : "";
            }

            footer.text = $"Letzter Monat: +{trade.LastMonthTradeIncome:0.0} {currency} Einnahmen · " +
                          $"−{trade.LastMonthTradeExpense:0.0} {currency} Ausgaben · " +
                          $"{trade.LastMonthTradeUsed} / {capacity} Einheiten";
        }

        private int CountHarbors()
        {
            int n = 0;
            foreach (var b in city.Buildings)
                if (b.Definition.tradeCapacity > 0 && !b.IsOutOfService) n++;
            return n;
        }
    }
}
