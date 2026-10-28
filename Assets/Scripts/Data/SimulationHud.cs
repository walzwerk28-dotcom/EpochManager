using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Kleines Panel oben links: Datum, Geschwindigkeitstasten, Abrechnung des letzten Monats
    /// und der Warenvorrat. Baut sich wie CityHud zur Laufzeit selbst auf.
    /// </summary>
    public class SimulationHud : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [SerializeField] private GameClock clock;
        [SerializeField] private EconomySimulation economy;

        [Header("Aussehen")]
        [SerializeField] private Color panelColor = new Color(0.08f, 0.09f, 0.12f, 0.88f);
        [SerializeField] private Color buttonColor = new Color(0.22f, 0.25f, 0.32f, 1f);
        [SerializeField] private Color selectedColor = new Color(0.25f, 0.55f, 0.35f, 1f);
        [SerializeField] private Color textColor = Color.white;

        private TextMeshProUGUI infoText;
        private readonly List<UnityEngine.UI.Image> speedImages = new List<UnityEngine.UI.Image>();
        private readonly StringBuilder builder = new StringBuilder();
        private float refreshTimer;

        private void Start()
        {
            UiFactory.EnsureEventSystem();
            Build();
        }

        private void Update()
        {
            if (city == null || clock == null || infoText == null) return;

            for (int i = 0; i < speedImages.Count; i++)
                speedImages[i].color = clock.Speed == i ? selectedColor : buttonColor;

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.25f;

            builder.Clear();
            builder.AppendLine(clock.DateString);
            if (economy != null)
                builder.AppendLine($"Letzter Monat: +{economy.LastMonthIncome:0} / -{economy.LastMonthUpkeep:0}");
            builder.AppendLine();
            builder.AppendLine("Vorrat:");

            bool any = false;
            foreach (var entry in city.Stock.All)
            {
                if (entry.Key == null || entry.Value < 0.05f) continue;
                builder.AppendLine($"{entry.Key.displayName}: {entry.Value:0.#}");
                any = true;
            }
            if (!any) builder.AppendLine("(leer)");

            infoText.text = builder.ToString();
        }

        private void Build()
        {
            var canvas = UiFactory.CreateCanvas("SimulationHudCanvas", transform);

            // Panel oben links, unter der oberen Leiste von CityHud
            var panel = UiFactory.CreatePanel("SimulationPanel", canvas,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(340f, 340f), panelColor);
            panel.anchoredPosition = new Vector2(16f, -80f);

            // Reihe mit Geschwindigkeitstasten
            var rowGo = new GameObject("SpeedRow", typeof(RectTransform));
            rowGo.transform.SetParent(panel, false);
            var row = (RectTransform)rowGo.transform;
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.anchoredPosition = new Vector2(0f, -8f);
            row.sizeDelta = new Vector2(-24f, 52f);
            UiFactory.AddRow(rowGo, TextAnchor.MiddleCenter, 0, 8f);

            string[] labels = { "II", "1x", "2x", "3x" };
            for (int i = 0; i < labels.Length; i++)
            {
                int speed = i;
                var button = UiFactory.CreateButton(row, "Speed_" + labels[i], labels[i], new Vector2(72f, 44f),
                    24f, buttonColor, textColor, () => { if (clock != null) clock.SetSpeed(speed); });
                speedImages.Add((UnityEngine.UI.Image)button.targetGraphic);
            }

            // Infotext darunter
            infoText = UiFactory.CreateText("InfoText", panel, "", 22f, TextAlignmentOptions.TopLeft, textColor);
            infoText.overflowMode = TextOverflowModes.Ellipsis;

            var infoRect = infoText.rectTransform;
            infoRect.anchorMin = Vector2.zero;
            infoRect.anchorMax = Vector2.one;
            infoRect.offsetMin = new Vector2(12f, 12f);
            infoRect.offsetMax = new Vector2(-12f, -68f);
        }
    }
}
