using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpochCity
{
    /// <summary>
    /// Infofenster links unten zum ausgewählten Gebäude (Linksklick außerhalb der Baumodi, wie in Anno): Kopf mit
    /// Kategoriesymbol, Kategorie und Bau-Epoche, dann Balken für Anbindung, bei Betrieben mit Rohstoff-Vorkommen
    /// dessen Anteil im Umkreis, bei Wohnhäusern Belegung und
    /// Zufriedenheit samt fehlenden Bedürfnissen, bei Produktion den Fortschritt je Funktion mit Ein- und Ausgaben
    /// (inklusive Technologie-Multiplikator), darunter Unterhalt und Besonderheiten. Unten Stilllegen/Fortsetzen
    /// (nur Gebäude mit Unterhalt, keine Wohnhäuser), Denkmalschutz an/aus und Abreißen. Der Inhalt wird beim Wechsel des Gebäudes neu aufgebaut und danach nur noch aktualisiert.
    /// </summary>
    public class BuildingInfoPanel : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [SerializeField] private BuildingPlacer placer;
        [Tooltip("Für den Unterhalt stillgelegter Gebäude")]
        [SerializeField] private EconomySimulation economy;

        [Header("Aussehen")]
        [Tooltip("Gemeinsames Farbschema; leer = Standardwerte")]
        [SerializeField] private UiTheme theme;

        private RectTransform panel;
        private RectTransform content;
        private TextMeshProUGUI title;
        private Image categoryIcon;
        private Button pauseButton;
        private TextMeshProUGUI pauseLabel;
        private Button protectButton;
        private TextMeshProUGUI protectLabel;
        private Button demolishButton;
        private readonly List<Action> updaters = new List<Action>();
        private BuildingInstance shown;
        private int shownFunctions;
        private float refreshTimer;

        private void Start()
        {
            theme = UiTheme.Resolve(theme);
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

            // Fortschrittsbalken laufen flüssig; nur bei neuem Gebäude oder neuen Funktionen wird neu aufgebaut
            int functions = building.Definition.GetActiveFunctions(city.UnlockedTechnologies).Count;
            if (building != shown || functions != shownFunctions)
            {
                shown = building;
                shownFunctions = functions;
                Rebuild(building);
                refreshTimer = 0f;
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.1f;
            foreach (var update in updaters) update();
            RefreshButtons(building);
        }

        private void RefreshButtons(BuildingInstance b)
        {
            bool canPause = CityController.CanPause(b.Definition);
            pauseButton.gameObject.SetActive(canPause);
            pauseLabel.text = b.IsPaused ? "Fortsetzen" : "Stilllegen";
            pauseButton.targetGraphic.color = b.IsPaused ? theme.accent : theme.button;
            protectLabel.text = b.IsProtected ? "Schutz: an" : "Schutz: aus";
            protectButton.targetGraphic.color = b.IsProtected ? theme.accent : theme.button;
            demolishButton.interactable = !b.IsProtected;
            demolishButton.targetGraphic.color = b.IsProtected ? theme.locked : theme.danger;
        }

        // ---------------------------------------------------------------- Inhalt

        private void Rebuild(BuildingInstance b)
        {
            for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
            updaters.Clear();

            var def = b.Definition;
            title.text = def.displayName;
            var icon = theme.icons != null ? theme.icons.ForCategory(def.category) : null;
            categoryIcon.sprite = icon;
            categoryIcon.gameObject.SetActive(icon != null);

            Line(theme.textMuted, () =>
            {
                string built = b.BuiltInEpoch != null ? $"  ·  gebaut in der {b.BuiltInEpoch.displayName}" : "";
                string protectedNote = b.IsProtected ? "  ·  Denkmalschutz" : "";
                return CategoryName(def.category) + built + protectedNote;
            });

            var connection = UiFactory.CreateBar(content, "Anbindung", theme);
            updaters.Add(() => connection.Set(b.Connection, UiFactory.QualityColor(b.Connection, theme),
                $"{b.Connection * 100f:0} %"));
            Line(theme.textMuted, () => b.ConnectionIssue);

            // Arbeitskräfte: besetzter Anteil der Arbeitsplätze (für alle Betriebe einer Stufe gleich)
            if (def.workerTier != null && def.workers > 0 && economy != null)
            {
                var workforce = UiFactory.CreateBar(content, "Arbeitskräfte", theme);
                updaters.Add(() =>
                {
                    float f = b.IsPaused ? 0f : economy.GetWorkforceFactor(def.workerTier);
                    workforce.Set(f, UiFactory.QualityColor(f, theme),
                        $"{Mathf.RoundToInt(def.workers * f)} / {def.workers}");
                });
                Line(theme.textMuted, () => def.workerTier.GetName(city.currentEpoch));
            }

            // Rohstoff-Vorkommen im Umkreis (Holzfäller, Steinbruch): gezählte Felder von den nötigen
            if (def.requiredDeposit != null)
            {
                var deposit = UiFactory.CreateBar(content, def.requiredDeposit.displayName, theme);
                updaters.Add(() =>
                {
                    city.GetDepositFactor(def, b.Origin, b.Footprint, out int count);
                    deposit.Set(b.DepositFactor, UiFactory.QualityColor(b.DepositFactor, theme),
                        $"{count} / {def.depositCellsForFull}");
                });
            }

            if (def.populationTier != null)
            {
                var residents = UiFactory.CreateBar(content, "Einwohner", theme);
                updaters.Add(() =>
                {
                    int capacity = def.GetCapacity(city.UnlockedTechnologies);
                    residents.Set(capacity > 0 ? (float)b.Residents / capacity : 0f, theme.accent,
                        $"{b.Residents} / {capacity}");
                    residents.Label.text = b.Tier.GetName(city.currentEpoch);
                });
                var satisfaction = UiFactory.CreateBar(content, "Zufriedenheit", theme);
                updaters.Add(() => satisfaction.Set(b.Satisfaction, UiFactory.QualityColor(b.Satisfaction, theme),
                    $"{b.Satisfaction * 100f:0} %"));
                Line(theme.danger, () => string.IsNullOrEmpty(b.MissingNeeds) ? "" : $"Fehlt: {b.MissingNeeds}");
                Line(theme.textMuted, () => TierLine(b));
            }

            foreach (var function in def.GetActiveFunctions(city.UnlockedTechnologies))
            {
                if (function == null) continue;
                var f = function;
                var bar = UiFactory.CreateBar(content, f.label, theme);
                updaters.Add(() =>
                {
                    b.Progress.TryGetValue(f, out float progress);
                    bar.Set(progress / f.cycleSeconds, theme.accent, $"{progress:0.0} / {f.cycleSeconds:0.#} s");
                });
                Line(theme.textMuted, () =>
                {
                    float multiplier = city.GetMultiplier(b);
                    string inputs = Amounts(f.inputs, 1f);
                    string outputs = Amounts(f.outputs, multiplier);
                    return string.IsNullOrEmpty(inputs) ? $"ergibt {outputs}" : $"{inputs} → {outputs}";
                });
            }
            if (def.GetActiveFunctions(city.UnlockedTechnologies).Count > 0)
                Line(theme.text, () => string.IsNullOrEmpty(b.Status) ? "" : $"Status: {b.Status}");

            Line(theme.text, () =>
            {
                var sb = new StringBuilder();
                float multiplier = city.GetMultiplier(b);
                if (multiplier > 1f) sb.AppendLine($"Technologie-Bonus ×{multiplier:0.##}");
                if (def.category == BuildingCategory.Storage)
                {
                    sb.AppendLine($"Lagerplatz +{def.GetCapacity(city.UnlockedTechnologies)} je Ware" +
                                  (b.IsPaused ? " (stillgelegt, zählt nicht)" : "") +
                                  $"  ·  gesamt {city.StorageCapacity}");
                    sb.AppendLine($"Einzugsweite {def.GetCatchmentRange(city.UnlockedTechnologies):0} Straßenfelder");
                }
                if (def.upkeepPerMonth > 0f)
                {
                    float upkeep = economy != null ? economy.GetUpkeep(b) : def.upkeepPerMonth;
                    sb.Append($"Unterhalt {upkeep:0.##} {Currency()} pro Monat" + (b.IsPaused ? " (stillgelegt)" : ""));
                }
                return sb.ToString().TrimEnd();
            });
        }

        /// <summary>
        /// Stand von Aufstieg bzw. Abstieg: „Aufstieg (Handwerker): 2 / 3 Monate“, „Abstieg droht: 1 / 3 Monate“
        /// oder der Grund, warum das Haus nicht aufsteigt (z. B. „braucht Bildung“).
        /// </summary>
        private string TierLine(BuildingInstance b)
        {
            var tier = b.Tier;
            if (tier == null) return "";
            if (b.TierMonths < 0)
                return $"Abstieg droht: {-b.TierMonths} / {tier.downgradeMonths} Monate (Zufriedenheit unter " +
                       $"{tier.downgradeSatisfaction * 100f:0} %)";
            if (tier.nextTier == null) return "";
            string next = tier.nextTier.GetName(city.currentEpoch);
            if (b.TierMonths > 0) return $"Aufstieg ({next}): {b.TierMonths} / {tier.upgradeMonths} Monate";
            return string.IsNullOrEmpty(b.UpgradeIssue)
                ? $"Aufstieg ({next}): ab Zufriedenheit {tier.upgradeSatisfaction * 100f:0} % für {tier.upgradeMonths} Monate"
                : $"Aufstieg ({next}): {b.UpgradeIssue}";
        }

        /// <summary>Textzeile, die sich bei jeder Aktualisierung neu füllt und bei leerem Text verschwindet.</summary>
        private void Line(Color color, Func<string> text)
        {
            var line = UiFactory.CreateText("Line", content, "", theme.smallSize, TextAlignmentOptions.TopLeft, color);
            updaters.Add(() =>
            {
                string value = text() ?? "";
                line.text = value;
                line.gameObject.SetActive(value.Length > 0);
            });
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
                Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(440f, 0f), theme.panel);
            panel.anchoredPosition = new Vector2(16f, 112f);
            UiFactory.AddColumn(panel.gameObject, 14, 10f);
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            title = UiFactory.WindowHeader(panel, "", theme, () => placer.Inspect(null));
            categoryIcon = UiFactory.AddIcon(title.transform.parent, null, 30f, theme.heading);
            categoryIcon.transform.SetAsFirstSibling();

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(panel, false);
            content = (RectTransform)contentGo.transform;
            UiFactory.AddColumn(contentGo, 0, 6f);

            var actions = new GameObject("Actions", typeof(RectTransform));
            actions.transform.SetParent(panel, false);
            UiFactory.AddRow(actions, TextAnchor.MiddleLeft, 0, 10f);
            var icons = theme.icons;
            pauseButton = UiFactory.CreateButton(actions.transform, "Pause", "Stilllegen",
                new Vector2(136f, 48f), theme.smallSize + 1f, theme.button, theme.text, TogglePause,
                icons != null ? icons.pause : null);
            pauseLabel = pauseButton.GetComponentInChildren<TextMeshProUGUI>();
            Tooltip.Attach(pauseButton.gameObject, () =>
                "<b>Stilllegen</b>\nDas Gebäude arbeitet nicht und bietet keine Dienste, zahlt aber nur " +
                $"{(economy != null ? economy.pausedUpkeepFactor : 0.25f) * 100f:0} % seines Unterhalts.");
            protectButton = UiFactory.CreateButton(actions.transform, "Protect", "Schutz: aus",
                new Vector2(136f, 48f), theme.smallSize + 1f, theme.button, theme.text, ToggleProtection,
                icons != null ? icons.monument : null);
            protectLabel = protectButton.GetComponentInChildren<TextMeshProUGUI>();
            Tooltip.Attach(protectButton.gameObject, () => "<b>Denkmalschutz</b>\nVerhindert den Abriss.");
            demolishButton = UiFactory.CreateButton(actions.transform, "Demolish", "Abreißen",
                new Vector2(124f, 48f), theme.smallSize + 1f, theme.danger, theme.text, Demolish,
                icons != null ? icons.demolish : null);

            panel.gameObject.SetActive(false);
        }

        private void TogglePause()
        {
            var b = placer.Inspected;
            if (b == null) return;
            city.SetPaused(b, !b.IsPaused);
            refreshTimer = 0f;
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
