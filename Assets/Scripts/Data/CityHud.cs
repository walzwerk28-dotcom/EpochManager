using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Einfache Spiel-Oberfläche (uGUI + TextMeshPro), die sich beim Start selbst aufbaut:
    /// oben Epoche und Geld, unten die Bauleiste mit Kategorien (Straßen, Zonen, Gebäudekategorien aus den
    /// Daten), deren Werkzeuge in einer Reihe darüber aufklappen, dazu Abriss und Anbindungs-Overlay.
    /// Es muss weder ein Canvas noch ein EventSystem manuell angelegt werden.
    /// </summary>
    public class CityHud : MonoBehaviour
    {
        [Header("Referenzen")]
        [SerializeField] private CityController city;
        [SerializeField] private BuildingPlacer placer;
        [SerializeField] private ConnectionOverlay overlay;

        [Header("Aussehen")]
        [SerializeField] private Color panelColor = new Color(0.08f, 0.09f, 0.12f, 0.88f);
        [SerializeField] private Color buttonColor = new Color(0.22f, 0.25f, 0.32f, 1f);
        [SerializeField] private Color selectedColor = new Color(0.25f, 0.55f, 0.35f, 1f);
        [SerializeField] private Color demolishColor = new Color(0.6f, 0.25f, 0.22f, 1f);
        [SerializeField] private Color lockedColor = new Color(0.14f, 0.15f, 0.18f, 1f);
        [SerializeField] private Color textColor = Color.white;

        private TextMeshProUGUI epochText;
        private TextMeshProUGUI moneyText;
        private UnityEngine.UI.Image demolishImage;
        private UnityEngine.UI.Image overlayImage;
        private TextMeshProUGUI hoverText;
        private TextMeshProUGUI hintText;
        private string lastHover;
        private string lastEpoch;
        private string lastMoney;

        // Bauleiste: unten die Kategorien, darüber klappt die Reihe der offenen Kategorie auf
        private enum TabKind { Roads, Zones, Buildings }

        private class Tab
        {
            public string Label;
            public TabKind Kind;
            public BuildingCategory Category;
            public UnityEngine.UI.Image Image;
        }

        /// <summary>Ein Eintrag der aufgeklappten Reihe mit Zustandsabfragen für die Einfärbung.</summary>
        private class Entry
        {
            public UnityEngine.UI.Button Button;
            public UnityEngine.UI.Image Image;
            public TextMeshProUGUI Label;
            public System.Func<bool> IsSelected;
            public System.Func<bool> IsLocked;
            public bool Removes; // Zone aufheben: rot statt grün hervorheben
        }

        private readonly List<Tab> tabs = new List<Tab>();
        private readonly List<Entry> entries = new List<Entry>();
        private Tab openTab;
        private RectTransform subMenu;

        private static readonly (BuildingCategory category, string label)[] CategoryOrder =
        {
            (BuildingCategory.Production, "Produktion"),
            (BuildingCategory.Commerce, "Handel"),
            (BuildingCategory.Service, "Dienste"),
            (BuildingCategory.Storage, "Lager"),
            (BuildingCategory.Housing, "Wohnen"),
            (BuildingCategory.Monument, "Denkmäler"),
            (BuildingCategory.Wonder, "Wunderbauten")
        };

        private void Awake()
        {
            UiFactory.EnsureEventSystem();
            BuildUi();
        }

        private void Update()
        {
            if (city == null) return;

            string epoch = city.currentEpoch != null ? city.currentEpoch.displayName : "-";
            if (epoch != lastEpoch)
            {
                epochText.text = epoch;
                lastEpoch = epoch;
            }

            string currency = city.currentEpoch != null ? city.currentEpoch.currencyName : "";
            string money = $"{Mathf.FloorToInt(city.Money)} {currency}";
            if (money != lastMoney)
            {
                moneyText.text = money;
                lastMoney = money;
            }

            if (placer == null) return;
            FollowPlacer();

            foreach (var tab in tabs)
                tab.Image.color = tab == openTab ? selectedColor : buttonColor;
            foreach (var entry in entries)
            {
                // Gebäude späterer Epochen bleiben sichtbar, aber ausgegraut
                bool locked = entry.IsLocked != null && entry.IsLocked();
                bool selected = !locked && entry.IsSelected();
                entry.Button.interactable = !locked;
                entry.Image.color = locked ? lockedColor
                    : selected ? (entry.Removes ? demolishColor : selectedColor) : buttonColor;
                entry.Label.alpha = locked ? 0.5f : 1f;
            }
            demolishImage.color = placer.DemolishMode ? demolishColor : buttonColor;
            if (overlayImage != null) overlayImage.color = overlay.Visible ? selectedColor : buttonColor;

            // Die Tastenhilfe würde das Infofenster links unten überdecken
            hintText.enabled = placer.Inspected == null;

            string hover = GetHoverInfo();
            if (hover != lastHover)
            {
                hoverText.text = hover;
                lastHover = hover;
            }
        }

        /// <summary>Kurzinfo zu dem Gebäude oder der Straße unter der Maus.</summary>
        private string GetHoverInfo()
        {
            if (!placer.TryGetHover(out var cell)) return "";

            var building = city.Grid.GetAt(cell);
            if (building != null)
            {
                var def = building.Definition;
                string connection = $"Anbindung {building.Connection * 100f:0} %";
                if (def.populationTier != null)
                {
                    string missing = string.IsNullOrEmpty(building.MissingNeeds) ? "" : $"  |  fehlt: {building.MissingNeeds}";
                    return $"{def.displayName}  |  {building.Residents} / {def.GetCapacity(city.UnlockedTechnologies)} " +
                           $"{def.populationTier.GetName(city.currentEpoch)}  |  Zufriedenheit {building.Satisfaction * 100f:0} %" +
                           $"{missing}  |  {connection}";
                }
                string status = string.IsNullOrEmpty(building.Status) ? "" : $"  |  {building.Status}";
                return $"{def.displayName}{status}  |  {connection}";
            }

            var road = city.Grid.GetRoad(cell);
            if (road != null) return road.displayName;

            var zone = city.Grid.GetZone(cell);
            return zone != null ? $"{zone.displayName} (frei)" : "";
        }

        // ---------------------------------------------------------------- Aufbau

        private void BuildUi()
        {
            var canvas = UiFactory.CreateCanvas("CityHudCanvas", transform);
            BuildTopBar(canvas);
            BuildBottomBar(canvas);
        }

        private void BuildTopBar(Transform canvas)
        {
            var top = UiFactory.CreatePanel("TopBar", canvas,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 64f), panelColor);
            UiFactory.AddRow(top.gameObject, TextAnchor.MiddleLeft, 16, 24f);

            epochText = UiFactory.CreateText("EpochText", top, "", 30f, TextAlignmentOptions.MidlineLeft, textColor);
            epochText.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 1f;

            moneyText = UiFactory.CreateText("MoneyText", top, "", 30f, TextAlignmentOptions.MidlineRight, textColor);
            moneyText.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredWidth = 420f;
        }

        private const float BarHeight = 96f;
        private const float SubMenuHeight = 96f;

        private void BuildBottomBar(Transform canvas)
        {
            var bottom = UiFactory.CreatePanel("BottomBar", canvas,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, BarHeight), panelColor);
            UiFactory.AddRow(bottom.gameObject, TextAnchor.MiddleCenter, 12, 10f);

            // Aufklappende Reihe direkt über der Leiste, zunächst verborgen
            subMenu = UiFactory.CreatePanel("SubMenu", canvas,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, SubMenuHeight),
                new Color(panelColor.r, panelColor.g, panelColor.b, panelColor.a * 0.9f));
            subMenu.anchoredPosition = new Vector2(0f, BarHeight + 6f);
            UiFactory.AddRow(subMenu.gameObject, TextAnchor.MiddleCenter, 10, 10f);
            subMenu.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().horizontalFit =
                UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            subMenu.gameObject.SetActive(false);

            if (placer != null)
            {
                if (placer.road != null) AddTab(bottom, "R  Straßen", TabKind.Roads, default);
                if (placer.residentialZone != null || placer.commercialZone != null)
                    AddTab(bottom, "Z  Zonen", TabKind.Zones, default);

                // Kategorien aus den Daten: nur die, für die es baubare Gebäude gibt
                foreach (var (category, label) in CategoryOrder)
                    if (placer.GetCategoryBuildings(category).Count > 0)
                        AddTab(bottom, label, TabKind.Buildings, category);
            }

            var demolish = UiFactory.CreateButton(bottom, "DemolishButton", "X  Abreißen", new Vector2(140f, 72f),
                22f, buttonColor, textColor, () => { if (placer != null) placer.ToggleDemolish(); });
            demolishImage = (UnityEngine.UI.Image)demolish.targetGraphic;

            if (overlay != null)
            {
                var overlayButton = UiFactory.CreateButton(bottom, "OverlayButton", "C  Anbindung", new Vector2(140f, 72f),
                    22f, buttonColor, textColor, () => overlay.Toggle());
                overlayImage = (UnityEngine.UI.Image)overlayButton.targetGraphic;
            }

            float above = BarHeight + SubMenuHeight + 12f;
            hintText = CreateBottomLine(canvas, "Hint",
                "1-9: Gebäude der offenen Kategorie  |  R: Straße  |  Z / G / U: Zonen  |  X: abreißen  |  C: Anbindung  |  Rechtsklick: abbrechen  |  WASD, Q/E, Mausrad: Kamera",
                18f, new Color(1f, 1f, 1f, 0.7f), above);
            hoverText = CreateBottomLine(canvas, "HoverInfo", "", 22f, textColor, above + 28f);
        }

        private void AddTab(Transform bar, string label, TabKind kind, BuildingCategory category)
        {
            var tab = new Tab { Label = label, Kind = kind, Category = category };
            var button = UiFactory.CreateButton(bar, "Tab_" + label, label, new Vector2(140f, 72f),
                22f, buttonColor, textColor, () => ToggleTab(tab));
            tab.Image = (UnityEngine.UI.Image)button.targetGraphic;
            tabs.Add(tab);
        }

        /// <summary>Klick auf eine Kategorie: öffnet sie, ein zweiter Klick schließt sie wieder.</summary>
        private void ToggleTab(Tab tab)
        {
            OpenTab(openTab == tab ? null : tab);
            if (openTab == null) placer.ClearSelection();
        }

        private void OpenTab(Tab tab)
        {
            if (tab == openTab) return;
            openTab = tab;
            placer.ActiveCategory = tab != null && tab.Kind == TabKind.Buildings ? tab.Category : (BuildingCategory?)null;
            RebuildSubMenu();
        }

        /// <summary>Hält die offene Kategorie passend zum Werkzeug, auch wenn es per Taste gewählt wurde.</summary>
        private void FollowPlacer()
        {
            Tab wanted = null;
            if (placer.RoadMode) wanted = tabs.Find(t => t.Kind == TabKind.Roads);
            else if (placer.ZoneMode) wanted = tabs.Find(t => t.Kind == TabKind.Zones);
            else if (placer.SelectedBuilding != null)
                wanted = tabs.Find(t => t.Kind == TabKind.Buildings && t.Category == placer.SelectedBuilding.category);
            if (wanted != null) OpenTab(wanted);
        }

        private void RebuildSubMenu()
        {
            for (int i = subMenu.childCount - 1; i >= 0; i--) Destroy(subMenu.GetChild(i).gameObject);
            entries.Clear();
            subMenu.gameObject.SetActive(openTab != null);
            if (openTab == null) return;

            switch (openTab.Kind)
            {
                case TabKind.Roads:
                    var road = placer.road;
                    AddEntry($"{road.displayName}\n{road.costPerCell:0} / Feld", () => placer.SelectRoad(),
                        () => placer.RoadMode, () => !city.IsEpochReached(road.epoch));
                    break;

                case TabKind.Zones:
                    if (placer.residentialZone != null)
                    {
                        var res = placer.residentialZone;
                        AddEntry($"Z  {res.displayName}", () => placer.SelectZone(res),
                            () => placer.ZoneMode && placer.SelectedZone == res, null);
                    }
                    if (placer.commercialZone != null)
                    {
                        var com = placer.commercialZone;
                        AddEntry($"G  {com.displayName}", () => placer.SelectZone(com),
                            () => placer.ZoneMode && placer.SelectedZone == com, null);
                    }
                    AddEntry("U  Zone aufheben", () => placer.SelectZone(null),
                        () => placer.ZoneMode && placer.SelectedZone == null, null).Removes = true;
                    break;

                case TabKind.Buildings:
                    var list = placer.GetCategoryBuildings(openTab.Category);
                    for (int i = 0; i < list.Count; i++)
                    {
                        var def = list[i];
                        string key = i < 9 ? $"{i + 1}  " : "";
                        AddEntry($"{key}{def.displayName}\n{Mathf.FloorToInt(def.buildCost)}", () => placer.Select(def),
                            () => placer.SelectedBuilding == def, () => !city.IsEpochReached(def.epoch));
                    }
                    break;
            }
        }

        private Entry AddEntry(string label, UnityEngine.Events.UnityAction onClick, System.Func<bool> isSelected,
            System.Func<bool> isLocked)
        {
            var button = UiFactory.CreateButton(subMenu, "Entry_" + entries.Count, label, new Vector2(160f, 76f),
                22f, buttonColor, textColor, onClick);
            // Eigene Einfärbung statt der Button-Farbübergänge (auch für gesperrte Einträge)
            button.transition = UnityEngine.UI.Selectable.Transition.None;
            var entry = new Entry
            {
                Button = button,
                Image = (UnityEngine.UI.Image)button.targetGraphic,
                Label = button.GetComponentInChildren<TextMeshProUGUI>(),
                IsSelected = isSelected,
                IsLocked = isLocked
            };
            entries.Add(entry);
            return entry;
        }

        /// <summary>Zentrierte Textzeile über der unteren Leiste.</summary>
        private static TextMeshProUGUI CreateBottomLine(Transform canvas, string name, string text, float size,
            Color color, float y)
        {
            var line = UiFactory.CreateText(name, canvas, text, size, TextAlignmentOptions.Center, color);
            var rect = line.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(0f, 32f);
            return line;
        }
    }
}
