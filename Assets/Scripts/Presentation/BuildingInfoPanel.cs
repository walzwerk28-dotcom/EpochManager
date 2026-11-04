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
        [Tooltip("Für Brandzustand und Brandschutz")]
        [SerializeField] private FireSimulation fire;

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

            // Baustelle: Bauzeit als Balken, verschwindet, sobald das Gebäude fertig ist
            if (def.buildSeconds > 0f && !def.HasStages)
            {
                var site = UiFactory.CreateBar(content, "Bau", theme);
                updaters.Add(() =>
                {
                    site.Root.SetActive(b.IsBuilding);
                    if (b.IsBuilding)
                        site.Set(b.BuildTimer / def.buildSeconds, theme.heading,
                            $"{Mathf.FloorToInt(b.BuildTimer)} / {def.buildSeconds:0} s");
                });
                Line(theme.textMuted, () => b.IsBuilding
                    ? "Baustelle: arbeitet erst, wenn sie fertig ist" + (def.zone == null ? " · Abriss erstattet die Baukosten" : "")
                    : "");
            }

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
                    city.GetDepositFactor(b, out int count);
                    deposit.Set(b.DepositFactor, UiFactory.QualityColor(b.DepositFactor, theme),
                        $"{count} / {def.depositCellsForFull}");
                });
                // Gefällter Wald wächst nach: junge Felder im Umkreis
                if (def.fellsDeposit)
                    Line(theme.textMuted, () =>
                    {
                        int young = CountInArea(b, def.requiredDeposit, true);
                        return young > 0 ? $"{young} Felder wachsen nach" : "nichts wächst nach";
                    });
            }

            // Förster: Pflanzumkreis mit Wald und jungen Feldern
            if (def.plantsDeposit != null)
                Line(theme.textMuted, () =>
                    $"Pflanzt {def.plantsDeposit.displayName} im Arbeitsbereich: " +
                    $"{CountInArea(b, def.plantsDeposit, false)} ausgewachsen, " +
                    $"{CountInArea(b, def.plantsDeposit, true)} jung");

            // Weg zum Arbeitsbereich (Holzfäller, Förster): nur wenn er Zeit kostet
            if (def.HasWorkArea)
                Line(theme.textMuted, () => b.TravelFactor < 0.995f ? $"Weg zum Arbeitsbereich {b.TravelFactor * 100f:0} %" : "");

            // Großbau: Bauphase mit dem Gelieferten, danach die Wirkung
            if (def.HasStages)
            {
                var phase = UiFactory.CreateBar(content, "Bau", theme);
                updaters.Add(() =>
                {
                    int n = def.stages.Length;
                    bool building = b.UnderConstruction;
                    phase.Set(building ? (b.Stage + StageShare(b)) / n : 1f, building ? theme.heading : theme.accent,
                        building ? $"Phase {b.Stage + 1} / {n}" : "fertig");
                });
                Line(theme.textMuted, () => StageLine(b));
            }
            if (def.satisfactionBonus > 0f)
                Line(theme.textMuted, () =>
                    $"Wirkung{(b.UnderConstruction ? " (wenn fertig)" : "")}: +{def.satisfactionBonus * 100f:0} % " +
                    $"Zufriedenheit für Wohnhäuser im Umkreis von {def.bonusRange:0} Straßenfeldern");
            if (def.satisfactionBonus > 0f)
            {
                // Wie viele Wohnhäuser der Bonus erreicht: Straßenentfernung, höchstens einmal je Sekunde berechnet
                int reached = 0;
                float reachedAt = -10f;
                Line(theme.textMuted, () =>
                {
                    if (b.UnderConstruction) return "";
                    if (Time.unscaledTime - reachedAt >= 1f)
                    {
                        reachedAt = Time.unscaledTime;
                        var distances = ConnectionMap.ShortestDistances(city.Grid,
                            new List<Vector2Int>(city.Grid.GetAdjacentCells(b)));
                        reached = 0;
                        foreach (var h in city.Buildings)
                        {
                            if (h.Definition.populationTier == null) continue;
                            foreach (var c in city.Grid.GetAdjacentCells(h))
                                if (distances.TryGetValue(c, out float d) && d <= def.bonusRange)
                                {
                                    reached++;
                                    break;
                                }
                        }
                    }
                    return $"Erreicht {reached} Wohnhäuser";
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
                Line(theme.positive, () => b.WonderBonus > 0f ? $"Wunderbau in der Nähe: +{b.WonderBonus * 100f:0} % Zufriedenheit" : "");
                Line(theme.positive, () => b.ParkBonus > 0f && b.ParkSource != null
                    ? $"{b.ParkSource.displayName} in der Nähe: +{b.ParkBonus * 100f:0} % Zufriedenheit" : "");
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
                if (f.IsSale)
                {
                    // Gewerbe: Sortiment mit Preisen, darunter was zuletzt verkauft wurde
                    Line(theme.textMuted, () => "verkauft je Zyklus: " +
                                                UiFactory.SalesText(f.sales, city.GetMultiplier(b), Currency()));
                    Line(theme.textMuted, () => string.IsNullOrEmpty(b.LastSale) ? "" : $"zuletzt verkauft: {b.LastSale}");
                    continue;
                }
                Line(theme.textMuted, () =>
                {
                    float multiplier = city.GetMultiplier(b);
                    string inputs = Amounts(f.inputs, 1f);
                    string outputs = Amounts(f.outputs, multiplier);
                    if (string.IsNullOrEmpty(outputs))
                        return def.plantsDeposit != null ? $"pflanzt 1 Feld {def.plantsDeposit.displayName}" : inputs;
                    return string.IsNullOrEmpty(inputs) ? $"ergibt {outputs}" : $"{inputs} → {outputs}";
                });
            }
            if (def.GetActiveFunctions(city.UnlockedTechnologies).Count > 0 || def.HasStages || def.buildSeconds > 0f)
                Line(theme.text, () => string.IsNullOrEmpty(b.Status) ? "" : $"Status: {b.Status}");

            if (fire != null && (def.fireRisk > 0f || def.fireProtectionRange > 0f))
            {
                Line(theme.danger, () => FireLine(b));
                Line(theme.textMuted, () =>
                {
                    string own = def.fireProtectionRange > 0f
                        ? $"Feuerwache: Reichweite {def.fireProtectionRange:0} Straßenfelder  ·  " : "";
                    return b.Fire == FireState.None && def.fireRisk > 0f
                        ? $"{own}Brandschutz {fire.GetProtection(b) * 100f:0} %" : own.TrimEnd(' ', '·');
                });
            }

            Line(theme.text, () =>
            {
                var sb = new StringBuilder();
                float tech = def.GetOutputMultiplier(city.UnlockedTechnologies);
                if (tech > 1f) sb.AppendLine($"Technologie-Bonus ×{tech:0.##}");
                if (b.BoostSource != null)
                    sb.AppendLine($"{b.BoostSource.displayName} in der Nähe: +{(b.Boost - 1f) * 100f:0} %" +
                                  (tech > 1f ? $"  ·  zusammen ×{tech * b.Boost:0.##}" : ""));
                if (def.HasBoost)
                {
                    int count = city.GetBoostTargets(def, b.Origin, b.Footprint).Count;
                    string names = string.Join(", ", System.Array.ConvertAll(def.boostTargets, t => t != null ? t.displayName : "?"));
                    sb.AppendLine($"Verbessert {count} × {names} im Umkreis {def.boostRadius} um +{(def.boostMultiplier - 1f) * 100f:0} %" +
                                  (b.IsOutOfService ? " (wirkt gerade nicht)" : ""));
                }
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

        /// <summary>Brand: „Brennt! Ohne Löschen in 18 s abgebrannt“ bzw. „Beschädigt: noch 2 Monate“, sonst leer.</summary>
        private string FireLine(BuildingInstance b)
        {
            if (b.Fire == FireState.Damaged)
                return $"Beschädigt: noch {Mathf.CeilToInt(b.FireTimer)} {(b.FireTimer > 1f ? "Monate" : "Monat")}, dann instand gesetzt";
            if (b.Fire != FireState.Burning) return "";
            string outcome = b.Definition.zone != null && !b.IsProtected ? "abgebrannt" : "schwer beschädigt";
            return fire.GetProtection(b) > 0f
                ? $"Brennt! Die Feuerwehr löscht (Brandschutz {fire.GetProtection(b) * 100f:0} %)"
                : $"Brennt! Ohne Löschen in {Mathf.CeilToInt(b.FireTimer)} s {outcome} – Feuerwache bauen";
        }

        /// <summary>Felder des Vorkommens im Arbeitsbereich: ausgewachsene oder junge.</summary>
        private int CountInArea(BuildingInstance b, DepositDefinition deposit, bool young)
        {
            int n = 0;
            if (b.WorkArea == null) return n;
            foreach (var c in b.WorkArea)
                if (city.Grid.GetDeposit(c) == deposit && city.Grid.IsYoung(c) == young) n++;
            return n;
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

        /// <summary>Anteil der aktuellen Bauphase: Mittel aus Waren, Geld und Mindestdauer (je 0 bis 1).</summary>
        private static float StageShare(BuildingInstance b)
        {
            if (!b.UnderConstruction) return 1f;
            var stage = b.Definition.stages[b.Stage];
            float sum = 0f;
            int parts = 0;
            if (stage.materials != null)
                foreach (var m in stage.materials)
                {
                    if (m.resource == null || m.amount <= 0f) continue;
                    b.Delivered.TryGetValue(m.resource, out float have);
                    sum += Mathf.Clamp01(have / m.amount);
                    parts++;
                }
            if (stage.money > 0f) { sum += Mathf.Clamp01(b.StageMoney / stage.money); parts++; }
            if (stage.minSeconds > 0f) { sum += Mathf.Clamp01(b.StageTimer / stage.minSeconds); parts++; }
            return parts > 0 ? sum / parts : 1f;
        }

        /// <summary>„Sitzränge: 100 / 150 Stein · noch 10 s“; fertig leer.</summary>
        private string StageLine(BuildingInstance b)
        {
            if (!b.UnderConstruction) return "";
            var stage = b.Definition.stages[b.Stage];
            var parts = new List<string>();
            if (stage.materials != null)
                foreach (var m in stage.materials)
                {
                    if (m.resource == null || m.amount <= 0f) continue;
                    b.Delivered.TryGetValue(m.resource, out float have);
                    parts.Add($"{Mathf.FloorToInt(have)} / {m.amount:0} {m.resource.displayName}");
                }
            if (stage.money > 0f) parts.Add($"{b.StageMoney:0} / {stage.money:0} {Currency()}");
            float rest = stage.minSeconds - b.StageTimer;
            if (rest > 0f) parts.Add($"noch mindestens {rest:0} s");
            return $"{stage.name}: {string.Join(" · ", parts)}";
        }

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
                case BuildingCategory.Park: return "Park";
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
