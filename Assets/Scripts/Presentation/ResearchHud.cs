using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpochCity
{
    /// <summary>
    /// Forschungsfenster oben rechts (über TopBar „Forschung“ geöffnet, wie in Anno): aktuelles Projekt,
    /// Forschungspool, je ein Button pro Technologie und darunter die Bedingungen der nächsten Epoche mit dem
    /// Button für den Wechsel. Baut sich wie die anderen HUDs selbst auf.
    /// </summary>
    public class ResearchHud : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [SerializeField] private ResearchSystem research;
        [SerializeField] private EpochProgression progression;
        [Tooltip("Beim Start geöffnet")]
        [SerializeField] private bool startVisible;

        [Header("Aussehen")]
        [Tooltip("Gemeinsames Farbschema; leer = Standardwerte")]
        [SerializeField] private UiTheme theme;

        private Color panelColor, buttonColor, selectedColor, unlockedColor, lockedColor, textColor;

        private RectTransform panel;
        private TextMeshProUGUI statusText;
        private TextMeshProUGUI epochText;
        private Button advanceButton;
        private readonly StringBuilder builder = new StringBuilder();
        private readonly List<TechnologyDefinition> techs = new List<TechnologyDefinition>();
        private readonly List<Button> buttons = new List<Button>();
        private readonly List<TextMeshProUGUI> labels = new List<TextMeshProUGUI>();
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
            panelColor = theme.panel;
            buttonColor = theme.button;
            selectedColor = theme.accent;
            unlockedColor = theme.done;
            lockedColor = theme.locked;
            textColor = theme.text;
            UiFactory.EnsureEventSystem();
            Build();
            SetVisible(startVisible);
        }

        private void Update()
        {
            if (city == null || research == null || statusText == null || !Visible) return;

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.25f;

            var current = research.Current;
            float pool = research.researchResource != null ? city.Stock.Get(research.researchResource) : 0f;
            statusText.text = current != null
                ? $"{current.displayName}: {research.GetProgress(current):0} / {current.researchCost:0}\nVorrat: {pool:0}"
                : $"Kein Projekt gewählt\nVorrat: {pool:0}";

            for (int i = 0; i < techs.Count; i++)
            {
                var tech = techs[i];
                bool unlocked = research.IsUnlocked(tech);
                bool available = research.CanResearch(tech);

                string state = unlocked ? "erforscht" : available ? $"{tech.researchCost:0}" : "gesperrt";
                labels[i].text = $"{tech.displayName}\n<size=80%>{PathName(tech.path)} · {state}</size>";
                labels[i].alpha = unlocked || available ? 1f : 0.5f;

                buttons[i].interactable = available;
                buttons[i].targetGraphic.color =
                    unlocked ? unlockedColor : tech == current ? selectedColor : available ? buttonColor : lockedColor;
            }

            RefreshEpoch();
        }

        private void RefreshEpoch()
        {
            if (progression == null || epochText == null) return;

            var next = progression.NextEpoch;
            if (next == null)
            {
                epochText.text = "Letzte Epoche erreicht";
                advanceButton.gameObject.SetActive(false);
                return;
            }

            builder.Clear();
            builder.AppendLine($"Nächste Epoche: {next.displayName}");
            if (next.requiredKeyTechnologies > 0)
                builder.AppendLine(
                    $"Schlüsseltechnologien: {progression.CountKeyTechnologies(next)} / {next.requiredKeyTechnologies}");
            if (next.requiredPopulation > 0)
                builder.AppendLine($"Einwohner: {Mathf.Min(progression.CountPopulation(), next.requiredPopulation)} / {next.requiredPopulation}");
            if (next.milestones != null)
                foreach (var m in next.milestones)
                    if (m.building != null)
                        builder.AppendLine(
                            $"{m.building.displayName}: {Mathf.Min(progression.CountBuildings(m.building), m.count)} / {m.count}");
            epochText.text = builder.ToString().TrimEnd();

            bool canAdvance = progression.CanAdvance;
            advanceButton.gameObject.SetActive(true);
            advanceButton.interactable = canAdvance;
            advanceButton.targetGraphic.color = canAdvance ? selectedColor : lockedColor;
        }

        private void Build()
        {
            var canvas = UiFactory.CreateCanvas("ResearchHudCanvas", transform);

            // Fenster oben rechts unter der TopBar; Höhe passt sich dem Inhalt an
            panel = UiFactory.CreatePanel("ResearchPanel", canvas,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(340f, 0f), panelColor);
            panel.anchoredPosition = new Vector2(-16f, -TopBar.Height - 12f);
            UiFactory.AddColumn(panel.gameObject, 12, 8f);
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            UiFactory.WindowHeader(panel, "Forschung", theme, () => SetVisible(false));
            statusText = UiFactory.CreateText("Status", panel, "", 20f, TextAlignmentOptions.TopLeft, textColor);

            BuildTechButtons(panel);
            BuildEpochSection(panel);
        }

        private void BuildEpochSection(Transform panel)
        {
            if (progression == null) return;

            UiFactory.CreateText("EpochTitle", panel, "Epoche", theme.titleSize, TextAlignmentOptions.Left, theme.heading);
            epochText = UiFactory.CreateText("EpochStatus", panel, "", 20f, TextAlignmentOptions.TopLeft, textColor);

            advanceButton = UiFactory.CreateButton(panel, "AdvanceEpoch", "Epoche wechseln", new Vector2(0f, 56f),
                24f, lockedColor, textColor, () => progression.TryAdvance());
        }

        private void BuildTechButtons(Transform panel)
        {
            if (research == null || research.technologies == null) return;
            foreach (var tech in research.technologies)
            {
                if (tech == null) continue;
                var t = tech;
                var button = UiFactory.CreateButton(panel, "Tech_" + tech.name, tech.displayName, new Vector2(0f, 76f),
                    24f, buttonColor, textColor, () => research.Select(t));

                techs.Add(tech);
                buttons.Add(button);
                labels.Add(button.GetComponentInChildren<TextMeshProUGUI>());
            }
        }

        private static string PathName(TechPath path)
        {
            switch (path)
            {
                case TechPath.Trade: return "Handel";
                case TechPath.Culture: return "Kultur";
                case TechPath.Industry: return "Industrie";
                default: return "Wissenschaft";
            }
        }
    }
}
