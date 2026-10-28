using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Einfache Spiel-Oberfläche (uGUI + TextMeshPro), die sich beim Start selbst aufbaut:
    /// oben Epoche und Geld, unten die Leiste mit den baubaren Gebäuden und dem Abriss-Button.
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
        private UnityEngine.UI.Image roadImage;
        private UnityEngine.UI.Image overlayImage;
        private UnityEngine.UI.Image unzoneImage;
        private readonly Dictionary<ZoneDefinition, UnityEngine.UI.Image> zoneButtons =
            new Dictionary<ZoneDefinition, UnityEngine.UI.Image>();
        private TextMeshProUGUI hoverText;
        private string lastHover;
        private readonly List<BuildingDefinition> buttonDefs = new List<BuildingDefinition>();
        private readonly List<UnityEngine.UI.Image> buttonImages = new List<UnityEngine.UI.Image>();
        private readonly List<TextMeshProUGUI> buttonLabels = new List<TextMeshProUGUI>();
        private string lastEpoch;
        private string lastMoney;

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
            for (int i = 0; i < buttonDefs.Count; i++)
            {
                // Gebäude späterer Epochen bleiben sichtbar, aber ausgegraut
                bool reached = city.IsEpochReached(buttonDefs[i].epoch);
                buttonImages[i].color = !reached ? lockedColor
                    : placer.SelectedBuilding == buttonDefs[i] ? selectedColor : buttonColor;
                buttonLabels[i].alpha = reached ? 1f : 0.5f;
            }
            demolishImage.color = placer.DemolishMode ? demolishColor : buttonColor;
            if (roadImage != null) roadImage.color = placer.RoadMode ? selectedColor : buttonColor;
            if (overlayImage != null) overlayImage.color = overlay.Visible ? selectedColor : buttonColor;
            foreach (var entry in zoneButtons)
                entry.Value.color = placer.ZoneMode && placer.SelectedZone == entry.Key ? selectedColor : buttonColor;
            if (unzoneImage != null)
                unzoneImage.color = placer.ZoneMode && placer.SelectedZone == null ? demolishColor : buttonColor;

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

        private void BuildBottomBar(Transform canvas)
        {
            var bottom = UiFactory.CreatePanel("BottomBar", canvas,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), panelColor);
            UiFactory.AddRow(bottom.gameObject, TextAnchor.MiddleCenter, 16, 12f);

            if (placer != null && placer.hotbar != null)
            {
                for (int i = 0; i < placer.hotbar.Length; i++)
                {
                    var def = placer.hotbar[i];
                    if (def == null) continue;

                    string label = $"{i + 1}  {def.displayName}\n{Mathf.FloorToInt(def.buildCost)}";
                    var button = UiFactory.CreateButton(bottom, "Build_" + def.name, label, new Vector2(150f, 88f),
                        22f, buttonColor, textColor, () => placer.Select(def));
                    buttonDefs.Add(def);
                    buttonImages.Add((UnityEngine.UI.Image)button.targetGraphic);
                    buttonLabels.Add(button.GetComponentInChildren<TextMeshProUGUI>());
                }
            }

            if (placer != null && placer.road != null)
            {
                var road = placer.road;
                string label = $"R  {road.displayName}\n{road.costPerCell:0} / Feld";
                var roadButton = UiFactory.CreateButton(bottom, "RoadButton", label, new Vector2(150f, 88f),
                    22f, buttonColor, textColor, () => placer.SelectRoad());
                roadImage = (UnityEngine.UI.Image)roadButton.targetGraphic;
            }

            if (placer != null)
            {
                AddZoneButton(bottom, placer.residentialZone, "Z");
                AddZoneButton(bottom, placer.commercialZone, "G");
                var unzone = UiFactory.CreateButton(bottom, "UnzoneButton", "U  Zone\naufheben", new Vector2(140f, 88f),
                    22f, buttonColor, textColor, () => placer.SelectZone(null));
                unzoneImage = (UnityEngine.UI.Image)unzone.targetGraphic;
            }

            var demolish = UiFactory.CreateButton(bottom, "DemolishButton", "X  Abreißen", new Vector2(140f, 88f),
                22f, buttonColor, textColor, () => { if (placer != null) placer.ToggleDemolish(); });
            demolishImage = (UnityEngine.UI.Image)demolish.targetGraphic;

            if (overlay != null)
            {
                var overlayButton = UiFactory.CreateButton(bottom, "OverlayButton", "C  Anbindung", new Vector2(140f, 88f),
                    22f, buttonColor, textColor, () => overlay.Toggle());
                overlayImage = (UnityEngine.UI.Image)overlayButton.targetGraphic;
            }

            hoverText = CreateBottomLine(canvas, "HoverInfo", "", 22f, textColor, 166f);
            CreateBottomLine(canvas, "Hint",
                "1-9: bauen  |  R: Straße  |  Z / G / U: Zonen ziehen  |  X: abreißen  |  C: Anbindung  |  Rechtsklick: abbrechen  |  WASD, Q/E, Mausrad: Kamera",
                20f, new Color(1f, 1f, 1f, 0.75f), 130f);
        }

        private void AddZoneButton(Transform bottom, ZoneDefinition zone, string key)
        {
            if (zone == null) return;
            var button = UiFactory.CreateButton(bottom, "Zone_" + zone.name, $"{key}  {zone.displayName}",
                new Vector2(140f, 88f), 22f, buttonColor, textColor, () => placer.SelectZone(zone));
            zoneButtons[zone] = (UnityEngine.UI.Image)button.targetGraphic;
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
