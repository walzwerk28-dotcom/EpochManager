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

        private TextMeshProUGUI epochText, dateText, residentsText, moneyText;
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

            int residents = population != null ? population.TotalResidents : 0;
            string muted = UiTheme.Hex(theme.textMuted);
            residentsText.text = $"<size=75%><color={muted}>Einw.</color></size> {residents}<size=75%><color={muted}>/{city.HousingCapacity}</color></size>";

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
            dateText = Text(bar, "Date", theme.bodySize, theme.text, 150f);
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
            residentsText = Text(bar, "Residents", theme.bodySize + 2f, theme.text, 140f);
            residentsText.alignment = TextAlignmentOptions.MidlineRight;
            moneyText = Text(bar, "Money", theme.bodySize + 4f, theme.text, 280f);
            moneyText.alignment = TextAlignmentOptions.MidlineRight;

            if (cityWindow != null)
                cityImage = BarButton(bar, "CityWindow", "Stadt", 80f, () => cityWindow.Toggle());
            if (researchWindow != null)
                researchImage = BarButton(bar, "ResearchWindow", "Forschung", 112f, () => researchWindow.Toggle());
            if (saveSystem != null)
            {
                menuImage = BarButton(bar, "Menu", "Menü", 76f, ToggleMenu);
                BuildMenu(canvas);
            }
            if (cityHud != null)
                hintImage = BarButton(bar, "Help", "?", 40f, () => cityHud.ToggleHints());

            Refresh();
        }

        private TextMeshProUGUI Text(Transform parent, string name, float size, Color color, float width)
        {
            var t = UiFactory.CreateText(name, parent, "", size, TextAlignmentOptions.MidlineLeft, color);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.gameObject.AddComponent<LayoutElement>().preferredWidth = width;
            return t;
        }

        private static void Spacer(Transform parent)
        {
            var go = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private Image BarButton(Transform parent, string name, string label, float width, UnityEngine.Events.UnityAction onClick)
        {
            var b = UiFactory.CreateButton(parent, name, label, new Vector2(width, 44f), theme.bodySize,
                theme.button, theme.text, onClick);
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
            menu.anchoredPosition = new Vector2(-64f, -Height - 6f);
            UiFactory.AddColumn(menu.gameObject, 10, 8f);
            menu.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            UiFactory.CreateButton(menu, "Save", "Speichern  (F5)", new Vector2(0f, 46f), theme.bodySize,
                theme.button, theme.text, () => { saveSystem.QuickSave(); ToggleMenu(); });
            UiFactory.CreateButton(menu, "Load", "Laden  (F9)", new Vector2(0f, 46f), theme.bodySize,
                theme.button, theme.text, () => { saveSystem.QuickLoad(); ToggleMenu(); });
            menu.gameObject.SetActive(false);
        }

        private void ToggleMenu()
        {
            if (menu != null) menu.gameObject.SetActive(!menu.gameObject.activeSelf);
        }
    }
}
