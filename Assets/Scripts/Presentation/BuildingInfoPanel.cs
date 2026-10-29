using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpochCity
{
    /// <summary>
    /// Infofenster links unten zum ausgewählten Gebäude (Linksklick außerhalb der Baumodi): Bau-Epoche, Status,
    /// Anbindung, Produktion pro Zyklus inklusive Technologie-Multiplikator, Unterhalt, bei Wohnhäusern Einwohner
    /// und Zufriedenheit. Unten Denkmalschutz an/aus und Abreißen. Baut sich wie die anderen HUDs selbst auf.
    /// </summary>
    public class BuildingInfoPanel : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [SerializeField] private BuildingPlacer placer;

        [Header("Aussehen")]
        [SerializeField] private Color panelColor = new Color(0.08f, 0.09f, 0.12f, 0.92f);
        [SerializeField] private Color buttonColor = new Color(0.22f, 0.25f, 0.32f, 1f);
        [SerializeField] private Color activeColor = new Color(0.25f, 0.55f, 0.35f, 1f);
        [SerializeField] private Color demolishColor = new Color(0.6f, 0.25f, 0.22f, 1f);
        [SerializeField] private Color lockedColor = new Color(0.14f, 0.15f, 0.18f, 1f);
        [SerializeField] private Color textColor = Color.white;

        private RectTransform panel;
        private TextMeshProUGUI title;
        private TextMeshProUGUI body;
        private Button protectButton;
        private TextMeshProUGUI protectLabel;
        private Button demolishButton;
        private readonly StringBuilder builder = new StringBuilder();
        private BuildingInstance shown;
        private float refreshTimer;

        private void Start()
        {
            UiFactory.EnsureEventSystem();
            Build();
        }

        private void Update()
        {
            if (panel == null || placer == null || city == null) return;

            // Abgerissene Gebäude schließen das Fenster
            var building = placer.Inspected;
            if (building != null && !city.Buildings.Contains(building))
            {
                placer.Inspect(null);
                building = null;
            }

            panel.gameObject.SetActive(building != null);
            if (building == null)
            {
                shown = null;
                return;
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (building == shown && refreshTimer > 0f) return;
            refreshTimer = 0.25f;
            shown = building;
            Refresh(building);
        }

        private void Refresh(BuildingInstance b)
        {
            var def = b.Definition;
            title.text = def.displayName;

            builder.Clear();
            string built = b.BuiltInEpoch != null ? $"gebaut in der {b.BuiltInEpoch.displayName}" : "";
            builder.AppendLine($"{CategoryName(def.category)}  ·  {built}");
            if (!string.IsNullOrEmpty(b.Status)) builder.AppendLine($"Status: {b.Status}");

            string issue = string.IsNullOrEmpty(b.ConnectionIssue) ? "" : $" ({b.ConnectionIssue})";
            builder.AppendLine($"Anbindung: {b.Connection * 100f:0} %{issue}");

            if (def.populationTier != null)
            {
                int capacity = def.GetCapacity(city.UnlockedTechnologies);
                builder.AppendLine($"Einwohner: {b.Residents} / {capacity} {def.populationTier.GetName(city.currentEpoch)}");
                builder.AppendLine($"Zufriedenheit: {b.Satisfaction * 100f:0} %");
                if (!string.IsNullOrEmpty(b.MissingNeeds)) builder.AppendLine($"Fehlt: {b.MissingNeeds}");
            }

            float multiplier = city.GetMultiplier(b);
            foreach (var function in def.GetActiveFunctions(city.UnlockedTechnologies))
            {
                if (function == null) continue;
                string inputs = Amounts(function.inputs, 1f);
                string outputs = Amounts(function.outputs, multiplier);
                string arrow = string.IsNullOrEmpty(inputs) ? outputs : $"{inputs} → {outputs}";
                builder.AppendLine($"{function.label}: {arrow} je {function.cycleSeconds:0.#} s");
            }
            if (multiplier > 1f) builder.AppendLine($"Technologie-Bonus: ×{multiplier:0.##}");

            if (def.category == BuildingCategory.Storage)
                builder.AppendLine($"Einzugsweite: {def.catchmentRange:0} Straßenfelder");

            builder.AppendLine($"Unterhalt: {def.upkeepPerMonth:0.#} {Currency()} pro Monat");
            if (b.IsProtected) builder.AppendLine("Steht unter Denkmalschutz");
            body.text = builder.ToString().TrimEnd();

            protectLabel.text = b.IsProtected ? "Denkmalschutz: an" : "Denkmalschutz: aus";
            protectButton.targetGraphic.color = b.IsProtected ? activeColor : buttonColor;
            demolishButton.interactable = !b.IsProtected;
            demolishButton.targetGraphic.color = b.IsProtected ? lockedColor : demolishColor;
        }

        /// <summary>„2 Weizen + 5 Drachmen“; Geld erscheint unter dem Währungsnamen der Epoche.</summary>
        private string Amounts(ResourceAmount[] amounts, float multiplier)
        {
            if (amounts == null) return "";
            var sb = new StringBuilder();
            foreach (var a in amounts)
            {
                if (a.resource == null) continue;
                if (sb.Length > 0) sb.Append(" + ");
                string name = a.resource.isCurrency ? Currency() : a.resource.displayName;
                sb.Append($"{a.amount * multiplier:0.##} {name}");
            }
            return sb.ToString();
        }

        private string Currency() => city.currentEpoch != null ? city.currentEpoch.currencyName : "";

        private static string CategoryName(BuildingCategory category)
        {
            switch (category)
            {
                case BuildingCategory.Housing: return "Wohnen";
                case BuildingCategory.Commerce: return "Handel";
                case BuildingCategory.Production: return "Produktion";
                case BuildingCategory.Service: return "Dienste";
                case BuildingCategory.Storage: return "Lager";
                case BuildingCategory.Wonder: return "Wunderbau";
                default: return "Denkmal";
            }
        }

        // ---------------------------------------------------------------- Aufbau

        private void Build()
        {
            var canvas = UiFactory.CreateCanvas("BuildingInfoCanvas", transform);

            // Links unten über der Bauleiste; Höhe passt sich dem Inhalt an
            panel = UiFactory.CreatePanel("BuildingInfo", canvas,
                Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(440f, 0f), panelColor);
            panel.anchoredPosition = new Vector2(16f, 112f);
            UiFactory.AddColumn(panel.gameObject, 14, 10f);
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(panel, false);
            UiFactory.AddRow(header, TextAnchor.MiddleLeft, 0, 8f);
            title = UiFactory.CreateText("Title", header.transform, "", 28f, TextAlignmentOptions.MidlineLeft, textColor);
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            UiFactory.CreateButton(header.transform, "Close", "×", new Vector2(44f, 40f), 26f, buttonColor, textColor,
                () => placer.Inspect(null));

            body = UiFactory.CreateText("Body", panel, "", 20f, TextAlignmentOptions.TopLeft, textColor);

            var actions = new GameObject("Actions", typeof(RectTransform));
            actions.transform.SetParent(panel, false);
            UiFactory.AddRow(actions, TextAnchor.MiddleLeft, 0, 10f);
            protectButton = UiFactory.CreateButton(actions.transform, "Protect", "Denkmalschutz: aus",
                new Vector2(220f, 52f), 20f, buttonColor, textColor, ToggleProtection);
            protectButton.transition = Selectable.Transition.None;
            protectLabel = protectButton.GetComponentInChildren<TextMeshProUGUI>();
            demolishButton = UiFactory.CreateButton(actions.transform, "Demolish", "Abreißen",
                new Vector2(160f, 52f), 20f, demolishColor, textColor, Demolish);
            demolishButton.transition = Selectable.Transition.None;

            panel.gameObject.SetActive(false);
        }

        private void ToggleProtection()
        {
            var b = placer.Inspected;
            if (b == null) return;
            city.SetProtected(b, !b.IsProtected);
            refreshTimer = 0f;
        }

        private void Demolish()
        {
            var b = placer.Inspected;
            if (b != null && city.TryRemove(b)) placer.Inspect(null);
        }
    }
}
