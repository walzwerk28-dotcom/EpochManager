using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpochCity
{
    /// <summary>
    /// Stadt-Fenster oben links (wie die Übersichtsfenster in Anno, über TopBar „Stadt“ geöffnet): Abrechnung des
    /// letzten Monats, Einwohner und Zufriedenheit, Warenvorrat und die Steuerregler je Bevölkerungsstufe.
    /// Datum, Geschwindigkeit, Speichern und Laden liegen in der TopBar. Baut sich wie die anderen HUDs selbst auf.
    /// </summary>
    public class SimulationHud : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [SerializeField] private EconomySimulation economy;
        [SerializeField] private PopulationSimulation population;
        [Tooltip("Beim Start geöffnet")]
        [SerializeField] private bool startVisible;

        [Header("Aussehen")]
        [Tooltip("Gemeinsames Farbschema; leer = Standardwerte")]
        [SerializeField] private UiTheme theme;

        private RectTransform panel;
        private TextMeshProUGUI infoText;
        private readonly List<PopulationTierDefinition> taxTiers = new List<PopulationTierDefinition>();
        private readonly List<TextMeshProUGUI> taxLabels = new List<TextMeshProUGUI>();
        private const float TaxStep = 0.1f;
        private readonly StringBuilder builder = new StringBuilder();
        private float refreshTimer;

        public bool Visible => panel != null && panel.gameObject.activeSelf;

        public void Toggle() => SetVisible(!Visible);

        public void SetVisible(bool visible)
        {
            if (panel == null) return;
            panel.gameObject.SetActive(visible);
            refreshTimer = 0f;
        }

        private void Awake()
        {
            theme = UiTheme.Resolve(theme);
            UiFactory.EnsureEventSystem();
            Build();
            SetVisible(startVisible);
        }

        private void Update()
        {
            if (city == null || !Visible) return;

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.25f;

            string muted = UiTheme.Hex(theme.textMuted);
            builder.Clear();
            builder.AppendLine($"<color={UiTheme.Hex(theme.heading)}>Finanzen</color> <size=80%><color={muted}>letzter Monat</color></size>");
            if (economy != null)
                builder.AppendLine($"Einnahmen +{economy.LastMonthIncome:0}   Unterhalt -{economy.LastMonthUpkeep:0}");
            if (population != null)
                builder.AppendLine($"Steuern +{population.LastMonthTaxes:0}");

            // Prognose: bei dieser Bilanz ist die Kasse in X Monaten leer
            float balance = (economy != null ? economy.LastMonthIncome - economy.LastMonthUpkeep : 0f) +
                            (population != null ? population.LastMonthTaxes : 0f);
            string danger = UiTheme.Hex(theme.danger);
            if (city.Money < 0f)
                builder.AppendLine($"<color={danger}>Kasse leer: Gebäude mit Unterhalt arbeiten langsamer</color>");
            else if (balance < 0f)
                builder.AppendLine($"<color={danger}>Bei dieser Bilanz in {Mathf.FloorToInt(city.Money / -balance)} Monaten leer</color>");

            builder.AppendLine();
            builder.AppendLine($"<color={UiTheme.Hex(theme.heading)}>Bevölkerung</color>");
            if (population != null)
            {
                builder.AppendLine($"Einwohner {population.TotalResidents} / {city.HousingCapacity}");
                builder.AppendLine($"Zufriedenheit {population.AverageSatisfaction * 100f:0} %");
            }
            else builder.AppendLine($"Wohnplätze {city.HousingCapacity}");

            builder.AppendLine();
            int capacity = city.StorageCapacity;
            builder.AppendLine($"<color={UiTheme.Hex(theme.heading)}>Vorrat</color> " +
                               $"<size=80%><color={muted}>Lagerplatz {capacity} je Ware</color></size>");
            bool any = false;
            foreach (var entry in city.Stock.All)
            {
                if (entry.Key == null || entry.Value < 0.05f) continue;
                if (!CityController.IsLimited(entry.Key))
                    builder.AppendLine($"{entry.Key.displayName} {entry.Value:0.#}");
                else if (entry.Value >= capacity) // voll: Betriebe dieser Ware warten
                    builder.AppendLine($"<color={danger}>{entry.Key.displayName} {entry.Value:0.#} / {capacity} voll</color>");
                else
                    builder.AppendLine($"{entry.Key.displayName} {entry.Value:0.#} <color={muted}>/ {capacity}</color>");
                any = true;
            }
            if (!any) builder.AppendLine($"<color={muted}>leer</color>");

            infoText.text = builder.ToString().TrimEnd();

            for (int i = 0; i < taxTiers.Count; i++)
                taxLabels[i].text =
                    $"Steuer {taxTiers[i].GetName(city.currentEpoch)}: {population.GetTaxRate(taxTiers[i]) * 100f:0} %";
        }

        private void Build()
        {
            var canvas = UiFactory.CreateCanvas("SimulationHudCanvas", transform);

            // Fenster oben links unter der TopBar; Höhe passt sich dem Inhalt an
            panel = UiFactory.CreatePanel("CityWindow", canvas,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(360f, 0f), theme.panel);
            panel.anchoredPosition = new Vector2(16f, -TopBar.Height - 12f);
            UiFactory.AddColumn(panel.gameObject, 14, 10f);
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            UiFactory.WindowHeader(panel, "Stadt", theme, () => SetVisible(false));

            infoText = UiFactory.CreateText("InfoText", panel, "", theme.bodySize, TextAlignmentOptions.TopLeft, theme.text);

            if (population != null && population.tiers != null)
                foreach (var tier in population.tiers)
                    if (tier != null)
                    {
                        taxTiers.Add(tier);
                        BuildTaxRow(tier);
                    }
        }

        private void BuildTaxRow(PopulationTierDefinition tier)
        {
            var rowGo = new GameObject("TaxRow_" + tier.name, typeof(RectTransform));
            rowGo.transform.SetParent(panel, false);
            UiFactory.AddRow(rowGo, TextAnchor.MiddleLeft, 0, 8f);

            var label = UiFactory.CreateText("Label", rowGo.transform, "", theme.bodySize, TextAlignmentOptions.MidlineLeft,
                theme.text);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            taxLabels.Add(label);

            UiFactory.CreateButton(rowGo.transform, "TaxDown", "-", new Vector2(44f, 40f), 24f, theme.button, theme.text,
                () => population.SetTaxRate(tier, population.GetTaxRate(tier) - TaxStep));
            UiFactory.CreateButton(rowGo.transform, "TaxUp", "+", new Vector2(44f, 40f), 24f, theme.button, theme.text,
                () => population.SetTaxRate(tier, population.GetTaxRate(tier) + TaxStep));
        }
    }
}
