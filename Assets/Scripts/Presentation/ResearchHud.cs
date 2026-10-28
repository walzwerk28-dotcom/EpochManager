using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpochCity
{
    /// <summary>
    /// Panel oben rechts: aktuelles Forschungsprojekt, Forschungspool, je ein Button pro Technologie
    /// und darunter die Bedingungen der nächsten Epoche mit dem Button für den Wechsel.
    /// Baut sich wie CityHud zur Laufzeit selbst auf.
    /// </summary>
    public class ResearchHud : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [SerializeField] private ResearchSystem research;
        [SerializeField] private EpochProgression progression;

        [Header("Aussehen")]
        [SerializeField] private Color panelColor = new Color(0.08f, 0.09f, 0.12f, 0.88f);
        [SerializeField] private Color buttonColor = new Color(0.22f, 0.25f, 0.32f, 1f);
        [SerializeField] private Color selectedColor = new Color(0.25f, 0.55f, 0.35f, 1f);
        [SerializeField] private Color unlockedColor = new Color(0.16f, 0.32f, 0.22f, 1f);
        [SerializeField] private Color lockedColor = new Color(0.14f, 0.15f, 0.18f, 1f);
        [SerializeField] private Color textColor = Color.white;

        private TextMeshProUGUI statusText;
        private TextMeshProUGUI epochText;
        private Button advanceButton;
        private readonly StringBuilder builder = new StringBuilder();
        private readonly List<TechnologyDefinition> techs = new List<TechnologyDefinition>();
        private readonly List<Button> buttons = new List<Button>();
        private readonly List<TextMeshProUGUI> labels = new List<TextMeshProUGUI>();
        private float refreshTimer;

        private void Start()
        {
            UiFactory.EnsureEventSystem();
            Build();
        }

        private void Update()
        {
            if (city == null || research == null || statusText == null) return;

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

            // Panel oben rechts, unter der oberen Leiste von CityHud; Höhe passt sich dem Inhalt an
            var panel = UiFactory.CreatePanel("ResearchPanel", canvas,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(340f, 0f), panelColor);
            panel.anchoredPosition = new Vector2(-16f, -80f);
            UiFactory.AddColumn(panel.gameObject, 12, 8f);
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            UiFactory.CreateText("Title", panel, "Forschung", 26f, TextAlignmentOptions.Left, textColor);
            statusText = UiFactory.CreateText("Status", panel, "", 20f, TextAlignmentOptions.TopLeft, textColor);

            BuildTechButtons(panel);
            BuildEpochSection(panel);
        }

        private void BuildEpochSection(Transform panel)
        {
            if (progression == null) return;

            UiFactory.CreateText("EpochTitle", panel, "Epoche", 26f, TextAlignmentOptions.Left, textColor);
            epochText = UiFactory.CreateText("EpochStatus", panel, "", 20f, TextAlignmentOptions.TopLeft, textColor);

            advanceButton = UiFactory.CreateButton(panel, "AdvanceEpoch", "Epoche wechseln", new Vector2(0f, 56f),
                24f, lockedColor, textColor, () => progression.TryAdvance());
            advanceButton.transition = Selectable.Transition.None;
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

                // Ohne eigene Farbwechsel des Buttons, die Farbe setzt Update je nach Zustand
                button.transition = Selectable.Transition.None;

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
