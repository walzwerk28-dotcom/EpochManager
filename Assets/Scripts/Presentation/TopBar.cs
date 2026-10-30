using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EpochCity
{
    /// <summary>
    /// Obere Leiste wie in Cities: Skylines: links Epoche, Datum und Geschwindigkeit, in der Mitte die
    /// Nachfrage-Balken der Zonen, rechts Einwohner, Geld mit Bilanz des letzten Monats und die Buttons für das
    /// Stadt- und das Forschungsfenster (wie in Anno bei Bedarf geöffnet), das Menü (Speichern, Laden) und die
    /// Tastenhilfe. F5 / F9 speichern und laden schnell. Baut sich wie die anderen HUDs selbst auf.
    /// </summary>
    public class TopBar : MonoBehaviour
    {
        [Header("Referenzen")]
        [SerializeField] private CityController city;
        [SerializeField] private GameClock clock;
        [SerializeField] private EconomySimulation economy;
        [SerializeField] private PopulationSimulation population;
        [SerializeField] private ZoneDemand demand;
        [SerializeField] private SaveSystem saveSystem;

        [Header("Fenster und Hilfe")]
        [SerializeField] private SimulationHud cityWindow;
        [SerializeField] private ResearchHud researchWindow;
        [SerializeField] private CityHud cityHud;

        [Header("Aussehen")]
        [Tooltip("Gemeinsames Farbschema; leer = Standardwerte")]
        [SerializeField] private UiTheme theme;

        public const float Height = 64f;

        private TextMeshProUGUI epochText, dateText, residentsText, materialsText, moneyText;
        private readonly List<Image> speedImages = new List<Image>();
        private Image cityImage, researchImage, menuImage, hintImage;
        private RectTransform menu;
        private float refreshTimer;

        private class DemandBar
        {
            public ZoneDefinition Zone;
            public RectTransform Fill;
            public TextMeshProUGUI Value;
        }

        private readonly List<DemandBar> demandBars = new List<DemandBar>();

        private void Awake()
        {
            theme = UiTheme.Resolve(theme);
            UiFactory.EnsureEventSystem();
            Build();
        }

        private void Update()
        {
            if (city == null) return;

            // Schnellspeichern und -laden wie in vielen Aufbauspielen
            var kb = Keyboard.current;
            if (saveSystem != null && kb != null)
            {
                if (kb.f5Key.wasPressedThisFrame) saveSystem.QuickSave();
                else if (kb.f9Key.wasPressedThisFrame) saveSystem.QuickLoad();
            }

            if (clock != null)
                for (int i = 0; i < speedImages.Count; i++)
                    speedImages[i].color = clock.Speed == i ? theme.accent : theme.button;
            SetActive(cityImage, cityWindow != null && cityWindow.Visible);
            SetActive(researchImage, researchWindow != null && researchWindow.Visible);
            SetActive(menuImage, menu != null && menu.gameObject.activeSelf);
            SetActive(hintImage, cityHud != null && cityHud.HintsVisible);

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.2f;
            Refresh();
        }

        private void SetActive(Image image, bool active)
        {
            if (image != null) image.color = active ? theme.accent : theme.button;
        }

        private void Refresh()
        {
            epochText.text = city.currentEpoch != null ? city.currentEpoch.displayName : "-";
            dateText.text = clock != null ? clock.DateString : "";

            string currency = city.currentEpoch != null ? city.currentEpoch.currencyName : "";
            float balance = (economy != null ? economy.LastMonthIncome - economy.LastMonthUpkeep : 0f) +
                            (population != null ? population.LastMonthTaxes : 0f);
            var balanceColor = balance >= 0f ? theme.positive : theme.danger;
            moneyText.text = $"{Mathf.FloorToInt(city.Money)} {currency}  " +
                             $"<size=75%><color={UiTheme.Hex(balanceColor)}>{balance:+0;-0;±0}/Monat</color></size>";

            // Baumaterial im Lager, wie die Baustoffe in der Leiste bei Anno
            var materials = new List<string>();
            foreach (var entry in city.Stock.All)
                if (entry.Key != null && entry.Key.isBuildingMaterial)
                    materials.Add($"{Mathf.FloorToInt(entry.Value)} <color={UiTheme.Hex(theme.textMuted)}>" +
                                  $"{entry.Key.displayName}</color>");
            materialsText.text = string.Join("  ·  ", materials);
            materialsText.gameObject.SetActive(materials.Count > 0);

            int residents = population != null ? population.TotalResidents : 0;
            residentsText.text = $"{residents}<size=75%><color={UiTheme.Hex(theme.textMuted)}>/{city.HousingCapacity}</color></size>";

            if (demand != null)
                foreach (var bar in demandBars)
                {
                    float value = demand.GetDemand(bar.Zone);
                    bar.Fill.anchorMax = new Vector2(value, 1f);
                    bar.Value.text = $"{value * 100f:0} %";
                }
        }

        // ---------------------------------------------------------------- Aufbau

        private void Build()
        {
            var canvas = UiFactory.CreateCanvas("TopBarCanvas", transform);
            canvas.GetComponent<Canvas>().sortingOrder = 10; // Menü liegt über den Fenstern

            var bar = UiFactory.CreatePanel("TopBar", canvas,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, Height), theme.panel);
            UiFactory.AddRow(bar.gameObject, TextAnchor.MiddleLeft, 10, 10f);
            bar.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(18, 12, 10, 10);

            // Links: Epoche, Datum, Geschwindigkeit
            epochText = Text(bar, "Epoch", theme.titleSize + 2f, theme.heading, 120f);
            dateText = Text(bar, "Date", theme.bodySize, theme.text, 140f);
            string[] labels = { "II", "1x", "2x", "3x" };
            for (int i = 0; i < labels.Length; i++)
            {
                int speed = i;
                var b = UiFactory.CreateButton(bar, "Speed_" + labels[i], labels[i], new Vector2(46f, 44f), 20f,
                    theme.button, theme.text, () => { if (clock != null) clock.SetSpeed(speed); });
                speedImages.Add((Image)b.targetGraphic);
            }

            Spacer(bar);

            // Mitte: Nachfrage
            if (demand != null && demand.zones != null)
                foreach (var zone in demand.zones)
                    if (zone != null) AddDemandBar(bar, zone);

            Spacer(bar);

            // Rechts: Einwohner, Geld, Fenster, Menü, Hilfe
            var residentsIcon = Icon(i => i.residents);
            if (residentsIcon != null) UiFactory.AddIcon(bar, residentsIcon, 24f, theme.textMuted);
            residentsText = Text(bar, "Residents", theme.bodySize + 2f, theme.text, 80f);

            // Geld und darunter klein das Baumaterial; nebeneinander reicht die Breite der Leiste nicht
            var purse = new GameObject("Purse", typeof(RectTransform));
            purse.transform.SetParent(bar, false);
            UiFactory.AddColumn(purse, 0, 0f);
            purse.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleRight;
            purse.AddComponent<LayoutElement>().preferredWidth = 290f;
            moneyText = Text(purse.transform, "Money", theme.bodySize + 4f, theme.text, 290f);
            moneyText.alignment = TextAlignmentOptions.MidlineRight;
            materialsText = Text(purse.transform, "Materials", theme.smallSize, theme.text, 290f);
            materialsText.alignment = TextAlignmentOptions.MidlineRight;
            materialsText.raycastTarget = true; // für den Tooltip
            Tooltip.Attach(materialsText.gameObject, () => "<b>Baumaterial</b>\nVorrat im Lager; Gebäude und manche " +
                                                           "Straßen kosten neben Geld auch Material");

            if (cityWindow != null)
            {
                cityImage = BarButton(bar, "CityWindow", "Stadt", 98f, () => cityWindow.Toggle(), Icon(i => i.city));
                Tooltip.Attach(cityImage.gameObject, () => "<b>Stadt</b>\nFinanzen, Bevölkerung, Vorrat und Steuern");
            }
            if (researchWindow != null)
            {
                researchImage = BarButton(bar, "ResearchWindow", "Forschung", 136f, () => researchWindow.Toggle(),
                    Icon(i => i.research));
                Tooltip.Attach(researchImage.gameObject, () => "<b>Forschung</b>\nTechnologien und Epochenwechsel");
            }
            if (saveSystem != null)
            {
                var menuIcon = Icon(i => i.menu);
                menuImage = BarButton(bar, "Menu", menuIcon != null ? "" : "Menü", 48f, ToggleMenu, menuIcon);
                Tooltip.Attach(menuImage.gameObject, () => "<b>Menü</b>\nSpeichern (F5), Laden (F9)");
                BuildMenu(canvas);
            }
            if (cityHud != null)
            {
                var helpIcon = Icon(i => i.help);
                hintImage = BarButton(bar, "Help", helpIcon != null ? "" : "?", 44f, () => cityHud.ToggleHints(), helpIcon);
                Tooltip.Attach(hintImage.gameObject, () => "<b>Tastenhilfe</b>\nZeigt die Tastenkürzel über der Bauleiste");
            }

            Refresh();
        }

        private TextMeshProUGUI Text(Transform parent, string name, float size, Color color, float width)
        {
            var t = UiFactory.CreateText(name, parent, "", size, TextAlignmentOptions.MidlineLeft, color);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.gameObject.AddComponent<LayoutElement>().preferredWidth = width;
            return t;
        }

        /// <summary>Symbol aus dem Farbschema oder null, wenn keine Symbole eingerichtet sind.</summary>
        private Sprite Icon(System.Func<UiIcons, Sprite> pick) => theme.icons != null ? pick(theme.icons) : null;

        private static void Spacer(Transform parent)
        {
            var go = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private Image BarButton(Transform parent, string name, string label, float width, UnityEngine.Events.UnityAction onClick,
            Sprite icon = null)
        {
            var b = UiFactory.CreateButton(parent, name, label, new Vector2(width, 44f), theme.bodySize,
                theme.button, theme.text, onClick, icon);
            return (Image)b.targetGraphic;
        }

        private void AddDemandBar(Transform parent, ZoneDefinition zone)
        {
            var group = new GameObject("Demand_" + zone.name, typeof(RectTransform));
            group.transform.SetParent(parent, false);
            UiFactory.AddRow(group, TextAnchor.MiddleLeft, 0, 8f);

            var label = Text(group.transform, "Label", theme.smallSize, theme.textMuted, 100f);
            label.text = zone.displayName;
            label.alignment = TextAlignmentOptions.MidlineRight;

            var track = UiFactory.CreatePanel("Track", group.transform, Vector2.zero, Vector2.zero, Vector2.zero,
                new Vector2(80f, 12f), theme.track, PanelStyle.Flat);
            var trackLayout = track.gameObject.AddComponent<LayoutElement>();
            trackLayout.preferredWidth = 80f;
            trackLayout.preferredHeight = 12f;

            var fill = UiFactory.CreatePanel("Fill", track, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, zone.demandColor, PanelStyle.Flat);
            fill.offsetMin = fill.offsetMax = Vector2.zero;

            var value = Text(group.transform, "Value", theme.smallSize, theme.text, 44f);
            demandBars.Add(new DemandBar { Zone = zone, Fill = fill, Value = value });
        }

        /// <summary>Kleines Menü unter dem Menü-Button: Speichern und Laden.</summary>
        private void BuildMenu(Transform canvas)
        {
            menu = UiFactory.CreatePanel("MenuPanel", canvas, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(220f, 0f), theme.panel);
            menu.anchoredPosition = new Vector2(-12f, -Height - 6f);
            UiFactory.AddColumn(menu.gameObject, 10, 8f);
            menu.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            UiFactory.CreateButton(menu, "Save", "Speichern  (F5)", new Vector2(0f, 46f), theme.bodySize,
                theme.button, theme.text, () => { saveSystem.QuickSave(); ToggleMenu(); }, Icon(i => i.save));
            UiFactory.CreateButton(menu, "Load", "Laden  (F9)", new Vector2(0f, 46f), theme.bodySize,
                theme.button, theme.text, () => { saveSystem.QuickLoad(); ToggleMenu(); }, Icon(i => i.load));
            menu.gameObject.SetActive(false);
        }

        private void ToggleMenu()
        {
            if (menu != null) menu.gameObject.SetActive(!menu.gameObject.activeSelf);
        }
    }
}
