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

        private TextMeshProUGUI epochText, dateText, residentsText, workersText, materialsText, moneyText;
        private readonly List<Image> speedImages = new List<Image>();
        private Image cityImage, researchImage, menuImage, hintImage;
        private RectTransform menu;
        private float refreshTimer;

        private class DemandBar
        {
            public ZoneDefinition Zone;
            public GameObject Group;
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
            // Leere Kasse: Betrag rot (Gebäude mit Unterhalt arbeiten dann langsamer)
            string money = city.Money < 0f
                ? $"<color={UiTheme.Hex(theme.danger)}>{Mathf.FloorToInt(city.Money)}</color>"
                : $"{Mathf.FloorToInt(city.Money)}";
            moneyText.text = $"{money} {currency}  " +
                             $"<size=75%><color={UiTheme.Hex(balanceColor)}>{balance:+0;-0;±0}/Monat</color></size>";

            // Baumaterial im Lager, wie die Baustoffe in der Leiste bei Anno
            var materials = new List<string>();
            int capacity = city.StorageCapacity;
            string muted = UiTheme.Hex(theme.textMuted);
            foreach (var entry in city.Stock.All)
                if (entry.Key != null && entry.Key.isBuildingMaterial)
                {
                    // „14/80 Bretter“; volle Ware in Rot
                    string amount = entry.Value >= capacity
                        ? $"<color={UiTheme.Hex(theme.danger)}>{Mathf.FloorToInt(entry.Value)}</color>"
                        : $"{Mathf.FloorToInt(entry.Value)}";
                    materials.Add($"{amount}<color={muted}>/{capacity} {entry.Key.displayName}</color>");
                }
            materialsText.text = string.Join("  ·  ", materials);
            materialsText.gameObject.SetActive(materials.Count > 0);

            int residents = population != null ? population.TotalResidents : 0;
            residentsText.text = $"{residents}<size=75%><color={UiTheme.Hex(theme.textMuted)}>/{city.HousingCapacity}</color></size>";

            // Arbeitsplätze aller Stufen: besetzt / benötigt, rot bei Mangel
            int required = 0, filled = 0;
            if (economy != null && population != null && population.tiers != null)
                foreach (var tier in population.tiers)
                {
                    if (tier == null) continue;
                    int need = economy.GetRequiredWorkers(tier);
                    required += need;
                    filled += Mathf.Min(need, economy.GetAvailableWorkers(tier));
                }
            string workColor = filled < required ? UiTheme.Hex(theme.danger) : UiTheme.Hex(theme.text);
            workersText.text = $"<color={workColor}>{filled}</color><color={muted}>/{required} Arbeit</color>";
            workersText.gameObject.SetActive(required > 0);

            if (demand != null)
                foreach (var bar in demandBars)
                {
                    // Zonen, die noch gesperrt sind (Technologie fehlt), erscheinen erst mit der Freischaltung
                    bool unlocked = city.IsUnlocked(bar.Zone.requiredTechnology);
                    if (bar.Group.activeSelf != unlocked) bar.Group.SetActive(unlocked);
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
            // Einwohner und darunter klein die Arbeitskräfte (besetzt / Arbeitsplätze)
            var people = new GameObject("People", typeof(RectTransform));
            people.transform.SetParent(bar, false);
            UiFactory.AddColumn(people, 0, 0f);
            people.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            people.AddComponent<LayoutElement>().preferredWidth = 96f;
            residentsText = Text(people.transform, "Residents", theme.bodySize + 2f, theme.text, 96f);
            workersText = Text(people.transform, "Workers", theme.smallSize, theme.text, 96f);
            workersText.raycastTarget = true; // für den Tooltip
            Tooltip.Attach(workersText.gameObject, WorkersTooltip);

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
            Tooltip.Attach(materialsText.gameObject, () => "<b>Baumaterial</b>\nVorrat / Lagerplatz je Ware; Gebäude " +
                                                           "und manche Straßen kosten neben Geld auch Material. " +
                                                           "Mehr Lagerplatz bringen Lager.");

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

        /// <summary>Arbeitskräfte je Stufe: verfügbar, benötigt, besetzter Anteil.</summary>
        private string WorkersTooltip()
        {
            var sb = new System.Text.StringBuilder("<b>Arbeitskräfte</b>\nBesetzte Arbeitsplätze / Arbeitsplätze; " +
                                                   "fehlen Arbeitskräfte, arbeiten Betriebe langsamer.");
            if (economy == null || population == null || population.tiers == null) return sb.ToString();
            foreach (var tier in population.tiers)
                if (tier != null)
                    sb.Append($"\n{tier.GetName(city.currentEpoch)}: {economy.GetAvailableWorkers(tier)} verfügbar, " +
                              $"{economy.GetRequiredWorkers(tier)} benötigt");
            return sb.ToString();
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
            // Kurzer Name über Balken und Wert, damit drei und mehr Zonen in die Leiste passen
            var group = new GameObject("Demand_" + zone.name, typeof(RectTransform));
            group.transform.SetParent(parent, false);
            UiFactory.AddColumn(group, 0, 2f);
            group.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            group.AddComponent<LayoutElement>().preferredWidth = 108f;

            var label = Text(group.transform, "Label", theme.smallSize - 1f, theme.textMuted, 108f);
            label.text = zone.ShortName;

            var row = new GameObject("Row", typeof(RectTransform));
            row.transform.SetParent(group.transform, false);
            UiFactory.AddRow(row, TextAnchor.MiddleLeft, 0, 6f);
            var track = UiFactory.CreatePanel("Track", row.transform, Vector2.zero, Vector2.zero, Vector2.zero,
                new Vector2(60f, 10f), theme.track, PanelStyle.Flat);
            var trackLayout = track.gameObject.AddComponent<LayoutElement>();
            trackLayout.preferredWidth = 60f;
            trackLayout.preferredHeight = 10f;

            var fill = UiFactory.CreatePanel("Fill", track, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, zone.demandColor, PanelStyle.Flat);
            fill.offsetMin = fill.offsetMax = Vector2.zero;

            var value = Text(row.transform, "Value", theme.smallSize, theme.text, 42f);
            Tooltip.Attach(group, () => $"<b>Nachfrage {zone.displayName}</b>\nWie schnell die Zone neu bebaut wird");
            demandBars.Add(new DemandBar { Zone = zone, Group = group, Fill = fill, Value = value });
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
