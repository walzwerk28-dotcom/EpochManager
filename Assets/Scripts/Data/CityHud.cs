using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Bauleiste (uGUI + TextMeshPro), die sich beim Start selbst aufbaut: unten die Kategorien (Straßen, Zonen,
    /// Gebäudekategorien aus den Daten), deren Werkzeuge in einer Reihe darüber aufklappen, dazu Abriss und
    /// Anbindungs-Overlay, darüber die Infozeile zum Feld unter der Maus und die Tastenhilfe (über TopBar „?“).
    /// Epoche, Geld und Nachfrage zeigt die TopBar.
    /// Es muss weder ein Canvas noch ein EventSystem manuell angelegt werden.
    /// </summary>
    public class CityHud : MonoBehaviour
    {
        [Header("Referenzen")]
        [SerializeField] private CityController city;
        [SerializeField] private BuildingPlacer placer;
        [SerializeField] private ConnectionOverlay overlay;

        [Header("Aussehen")]
        [Tooltip("Gemeinsames Farbschema; leer = Standardwerte")]
        [SerializeField] private UiTheme theme;

        private Color panelColor, buttonColor, selectedColor, demolishColor, lockedColor, textColor;

        private UnityEngine.UI.Image demolishImage;
        private UnityEngine.UI.Image overlayImage;
        private TextMeshProUGUI hoverText;
        private TextMeshProUGUI hintText;
        private string lastHover;

        /// <summary>Tastenhilfe über der Bauleiste; anfangs verborgen, TopBar schaltet sie mit „?“.</summary>
        public bool HintsVisible { get; private set; }

        public void ToggleHints() => HintsVisible = !HintsVisible;

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
            public CanvasGroup Content;
            public System.Func<bool> IsSelected;
            public System.Func<bool> IsLocked;
            public TextMeshProUGUI Meta;
            public System.Func<bool> CanAfford; // Kosten rot, solange Geld oder Material fehlen
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
            theme = UiTheme.Resolve(theme);
            panelColor = theme.panel;
            buttonColor = theme.button;
            selectedColor = theme.accent;
            demolishColor = theme.danger;
            lockedColor = theme.locked;
            textColor = theme.text;
            UiFactory.EnsureEventSystem();
            BuildUi();
        }

        private void Update()
        {
            if (city == null || placer == null) return;
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
                entry.Content.alpha = locked ? 0.45f : 1f;
                if (entry.CanAfford != null)
                    entry.Meta.color = locked || entry.CanAfford() ? theme.textMuted
                        : selected ? Color.Lerp(demolishColor, Color.white, 0.55f) : demolishColor; // lesbar auf Grün
            }
            demolishImage.color = placer.DemolishMode ? demolishColor : buttonColor;
            if (overlayImage != null) overlayImage.color = overlay.Visible ? selectedColor : buttonColor;

            // Nur auf Wunsch; sie würde sonst das Infofenster links unten überdecken
            hintText.enabled = HintsVisible && placer.Inspected == null;

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

            // Mit gewähltem Gebäude: was zum Bau noch fehlt
            var selected = placer.SelectedBuilding;
            if (selected != null && city.IsAvailable(selected))
            {
                string lack = city.GetMissingCost(selected.buildCost, selected.materialCosts);
                if (lack != null) return $"{selected.displayName}  |  fehlt: {lack}";
            }

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
            BuildBottomBar(canvas);
        }

        private const float BarHeight = 96f;
        private const float SubMenuHeight = 126f;

        private void BuildBottomBar(Transform canvas)
        {
            var bottom = UiFactory.CreatePanel("BottomBar", canvas,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, BarHeight), panelColor);
            UiFactory.AddRow(bottom.gameObject, TextAnchor.MiddleCenter, 12, 10f);

            // Aufklappende Reihe direkt über der Leiste, zunächst verborgen
            subMenu = UiFactory.CreatePanel("SubMenu", canvas,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, SubMenuHeight),
                theme.panelSoft);
            subMenu.anchoredPosition = new Vector2(0f, BarHeight + 6f);
            UiFactory.AddRow(subMenu.gameObject, TextAnchor.MiddleCenter, 10, 10f);
            subMenu.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().horizontalFit =
                UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            subMenu.gameObject.SetActive(false);

            if (placer != null)
            {
                var icons = theme.icons;
                if (placer.road != null)
                    AddTab(bottom, "Straßen", TabKind.Roads, default, icons != null ? icons.road : null, "Taste R");
                if (placer.residentialZone != null || placer.commercialZone != null)
                    AddTab(bottom, "Zonen", TabKind.Zones, default, icons != null ? icons.zones : null,
                        "Wohnen Z  ·  Gewerbe G  ·  aufheben U");

                // Kategorien aus den Daten: nur die, für die es baubare Gebäude gibt
                foreach (var (category, label) in CategoryOrder)
                    if (placer.GetCategoryBuildings(category).Count > 0)
                        AddTab(bottom, label, TabKind.Buildings, category,
                            icons != null ? icons.ForCategory(category) : null, "Tasten 1–9 wählen ein Gebäude");
            }

            var demolish = UiFactory.CreateButton(bottom, "DemolishButton", "Abreißen", new Vector2(150f, 72f),
                22f, buttonColor, textColor, () => { if (placer != null) placer.ToggleDemolish(); },
                theme.icons != null ? theme.icons.demolish : null);
            Tooltip.Attach(demolish.gameObject, () =>
                "<b>Abreißen</b>\nGebäude und Straßen anklicken; Denkmalschutz verhindert den Abriss.\n" + Muted("Taste X"));
            demolishImage = (UnityEngine.UI.Image)demolish.targetGraphic;

            if (overlay != null)
            {
                var overlayButton = UiFactory.CreateButton(bottom, "OverlayButton", "Anbindung", new Vector2(150f, 72f),
                    22f, buttonColor, textColor, () => overlay.Toggle(), theme.icons != null ? theme.icons.overlay : null);
                Tooltip.Attach(overlayButton.gameObject, () =>
                    "<b>Anbindung</b>\nFärbt Gebäude und Straßen nach ihrer Anbindung ans Lager.\n" + Muted("Taste C"));
                overlayImage = (UnityEngine.UI.Image)overlayButton.targetGraphic;
            }

            float above = BarHeight + SubMenuHeight + 12f;
            hintText = CreateBottomLine(canvas, "Hint",
                "1-9: Gebäude der offenen Kategorie  |  R: Straße  |  Z / G / U: Zonen  |  X: abreißen  |  C: Anbindung  |  Rechtsklick: abbrechen  |  WASD, Q/E, Mausrad: Kamera",
                18f, theme.textMuted, above);
            hoverText = CreateBottomLine(canvas, "HoverInfo", "", 22f, textColor, above + 28f);
        }

        private void AddTab(Transform bar, string label, TabKind kind, BuildingCategory category, Sprite icon, string keys)
        {
            var tab = new Tab { Label = label, Kind = kind, Category = category };
            var button = UiFactory.CreateButton(bar, "Tab_" + label, label, new Vector2(150f, 72f),
                22f, buttonColor, textColor, () => ToggleTab(tab), icon);
            Tooltip.Attach(button.gameObject, () => $"<b>{label}</b>\n" + Muted(keys));
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

            var icons = theme.icons;
            switch (openTab.Kind)
            {
                case TabKind.Roads:
                    foreach (var type in placer.Roads)
                    {
                        if (type == null) continue;
                        var road = type;
                        string roadMaterials = Amounts(road.materialCostsPerCell);
                        AddEntry(icons != null ? icons.road : null, road.displayName,
                            $"{road.costPerCell:0} {Currency()} / Feld" +
                            (roadMaterials != "" ? $"\n{roadMaterials} / Feld" : ""), "R", () => placer.SelectRoad(road),
                            () => placer.RoadMode && placer.road == road, () => !city.IsAvailable(road),
                            () => $"<b>{road.displayName}</b>\n{road.costPerCell:0.#} {Currency()}" +
                                  (roadMaterials != "" ? $" + {roadMaterials}" : "") + " pro Feld, " +
                                  $"Unterhalt {road.upkeepPerCellPerMonth:0.##} pro Feld und Monat\n" +
                                  $"Qualität {road.quality:0.#}: Wege zum Lager zählen {(1f / road.quality) * 100f:0} %\n" +
                                  (city.IsAvailable(road) ? MissingLine(road.costPerCell, road.materialCostsPerCell) : "") +
                                  (LockReason(road.epoch, road.requiredTechnology) ??
                                   Muted("Mit der Maus ziehen, R wechselt den Typ, Rechtsklick bricht ab")));
                    }
                    break;

                case TabKind.Zones:
                    if (placer.residentialZone != null)
                        AddZoneEntry(placer.residentialZone, "Z", "Wohnhäuser wachsen von selbst an Straßen.");
                    if (placer.commercialZone != null)
                        AddZoneEntry(placer.commercialZone, "G", "Läden und Werkstätten wachsen von selbst an Straßen.");
                    AddEntry(icons != null ? icons.close : null, "Aufheben", "Zone entfernen", "U",
                        () => placer.SelectZone(null), () => placer.ZoneMode && placer.SelectedZone == null, null,
                        () => "<b>Zone aufheben</b>\nEntfernt die Zone; gewachsene Gebäude bleiben stehen.").Removes = true;
                    break;

                case TabKind.Buildings:
                    var list = placer.GetCategoryBuildings(openTab.Category);
                    for (int i = 0; i < list.Count; i++)
                    {
                        var def = list[i];
                        string materials = Amounts(def.materialCosts);
                        AddEntry(icons != null ? icons.ForCategory(def.category) : null, def.displayName,
                            $"{def.size.x} × {def.size.y}  ·  {Mathf.FloorToInt(def.buildCost)}" +
                            (materials != "" ? "\n" + materials : ""), i < 9 ? $"{i + 1}" : "",
                            () => placer.Select(def), () => placer.SelectedBuilding == def,
                            () => !city.IsAvailable(def), () => BuildingTooltip(def)).CanAfford =
                            () => city.CanAfford(def);
                    }
                    break;
            }
        }

        private void AddZoneEntry(ZoneDefinition zone, string key, string description)
        {
            AddEntry(theme.icons != null ? theme.icons.zones : null, zone.displayName, "kostenlos", key,
                () => placer.SelectZone(zone), () => placer.ZoneMode && placer.SelectedZone == zone, null,
                () => $"<b>{zone.displayName}</b>\n{description}\n" + Muted("Rechteck ziehen, Rechtsklick bricht ab"));
        }

        /// <summary>Karte in der aufgeklappten Reihe: Symbol, Name, Kurzinfo, Taste oben links; Details im Tooltip.</summary>
        private Entry AddEntry(Sprite icon, string title, string meta, string key, UnityEngine.Events.UnityAction onClick,
            System.Func<bool> isSelected, System.Func<bool> isLocked, System.Func<string> tooltip)
        {
            var button = UiFactory.CreateButton(subMenu, "Entry_" + entries.Count, "", new Vector2(150f, 106f),
                theme.bodySize, buttonColor, textColor, onClick);
            Destroy(button.GetComponentInChildren<TextMeshProUGUI>().gameObject);

            var content = new GameObject("Content", typeof(RectTransform), typeof(CanvasGroup));
            content.transform.SetParent(button.transform, false);
            var rt = (RectTransform)content.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(6f, 6f);
            rt.offsetMax = new Vector2(-6f, -6f);
            var column = content.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            column.childAlignment = TextAnchor.MiddleCenter;
            column.spacing = 2f;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true; // volle Kartenbreite, sonst schrumpft der gekürzte Name auf 0
            column.childForceExpandHeight = false;

            if (icon != null) UiFactory.AddIcon(content.transform, icon, 30f, textColor);
            var name = UiFactory.CreateText("Title", content.transform, title, theme.bodySize - 1f,
                TextAlignmentOptions.Center, textColor);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            var metaText = UiFactory.CreateText("Meta", content.transform, meta, theme.smallSize - 2f,
                TextAlignmentOptions.Center, theme.textMuted);
            metaText.textWrappingMode = TextWrappingModes.NoWrap;

            if (!string.IsNullOrEmpty(key))
            {
                var keyText = UiFactory.CreateText("Key", button.transform, key, theme.smallSize - 3f,
                    TextAlignmentOptions.TopLeft, theme.textMuted);
                var k = keyText.rectTransform;
                k.anchorMin = k.anchorMax = new Vector2(0f, 1f);
                k.pivot = new Vector2(0f, 1f);
                k.anchoredPosition = new Vector2(8f, -5f);
                k.sizeDelta = new Vector2(30f, 20f);
            }

            if (tooltip != null) Tooltip.Attach(button.gameObject, tooltip);

            var entry = new Entry
            {
                Button = button,
                Image = (UnityEngine.UI.Image)button.targetGraphic,
                Content = content.GetComponent<CanvasGroup>(),
                IsSelected = isSelected,
                IsLocked = isLocked,
                Meta = metaText
            };
            entries.Add(entry);
            return entry;
        }

        /// <summary>Details zu einem Gebäude: Größe, Kosten, Unterhalt, Produktion, Wohnplätze, Sperre.</summary>
        private string BuildingTooltip(BuildingDefinition def)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<b>{def.displayName}</b>");
            string materials = Amounts(def.materialCosts);
            sb.AppendLine($"{def.size.x} × {def.size.y} Felder  ·  {def.buildCost:0} {Currency()}" +
                          (materials != "" ? $" + {materials}" : "") + $"  ·  Unterhalt {def.upkeepPerMonth:0.#} pro Monat");
            foreach (var function in def.GetActiveFunctions(city.UnlockedTechnologies))
            {
                if (function == null) continue;
                string inputs = Amounts(function.inputs);
                string outputs = Amounts(function.outputs);
                sb.AppendLine(string.IsNullOrEmpty(inputs)
                    ? $"{function.label}: {outputs} je {function.cycleSeconds:0.#} s"
                    : $"{function.label}: {inputs} → {outputs} je {function.cycleSeconds:0.#} s");
            }
            int capacity = def.GetCapacity(city.UnlockedTechnologies);
            if (capacity > 0) sb.AppendLine($"Wohnplätze {capacity}");
            if (def.category == BuildingCategory.Storage)
                sb.AppendLine($"Einzugsweite {def.GetCatchmentRange(city.UnlockedTechnologies):0} Straßenfelder");
            if (city.IsAvailable(def)) sb.Append(MissingLine(def.buildCost, def.materialCosts));
            sb.AppendLine(LockReason(def.epoch, def.requiredTechnology) ?? Muted("Braucht ein angrenzendes Straßenfeld"));
            return sb.ToString().TrimEnd();
        }

        private string Amounts(ResourceAmount[] amounts)
        {
            if (amounts == null) return "";
            var parts = new List<string>();
            foreach (var a in amounts)
                if (a.resource != null)
                    parts.Add($"{a.amount:0.##} {(a.resource.isCurrency ? Currency() : a.resource.displayName)}");
            return string.Join(" + ", parts);
        }

        /// <summary>Rote Zeile „Fehlt: …“ mit Zeilenumbruch, wenn Geld oder Material nicht reichen, sonst leer.</summary>
        private string MissingLine(float money, ResourceAmount[] materials)
        {
            string lack = city.GetMissingCost(money, materials);
            return lack == null ? "" : $"<color={UiTheme.Hex(theme.danger)}>Fehlt: {lack}</color>\n";
        }

        /// <summary>Warum etwas noch nicht baubar ist (Epoche oder Forschung), sonst null.</summary>
        private string LockReason(EpochDefinition epoch, TechnologyDefinition tech)
        {
            if (epoch != null && !city.IsEpochReached(epoch))
                return $"<color={UiTheme.Hex(theme.danger)}>Gesperrt bis zur Epoche {epoch.displayName}</color>";
            if (!city.IsUnlocked(tech))
                return $"<color={UiTheme.Hex(theme.danger)}>Benötigt Forschung: {tech.displayName}</color>";
            return null;
        }

        private string Currency() => city.currentEpoch != null ? city.currentEpoch.currencyName : "";

        private string Muted(string text) => $"<color={UiTheme.Hex(theme.textMuted)}>{text}</color>";

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
