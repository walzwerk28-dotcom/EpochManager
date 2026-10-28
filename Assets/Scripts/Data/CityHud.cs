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
                string status = string.IsNullOrEmpty(building.Status) ? "" : $"  |  {building.Status}";
                return $"{building.Definition.displayName}{status}  |  Anbindung {building.Connection * 100f:0} %";
            }

            var road = city.Grid.GetRoad(cell);
            return road != null ? road.displayName : "";
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
                    var button = UiFactory.CreateButton(bottom, "Build_" + def.name, label, new Vector2(170f, 88f),
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
                var roadButton = UiFactory.CreateButton(bottom, "RoadButton", label, new Vector2(170f, 88f),
                    22f, buttonColor, textColor, () => placer.SelectRoad());
                roadImage = (UnityEngine.UI.Image)roadButton.targetGraphic;
            }

            var demolish = UiFactory.CreateButton(bottom, "DemolishButton", "X  Abreißen", new Vector2(150f, 88f),
                22f, buttonColor, textColor, () => { if (placer != null) placer.ToggleDemolish(); });
            demolishImage = (UnityEngine.UI.Image)demolish.targetGraphic;

            hoverText = CreateBottomLine(canvas, "HoverInfo", "", 22f, textColor, 166f);
            CreateBottomLine(canvas, "Hint",
                "1-9: bauen  |  R: Straße ziehen  |  X: abreißen  |  Rechtsklick: abbrechen  |  WASD, Q/E, Mausrad: Kamera",
                20f, new Color(1f, 1f, 1f, 0.75f), 130f);
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
