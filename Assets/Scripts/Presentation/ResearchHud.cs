using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpochCity
{
    /// <summary>
    /// Forschungsfenster als Baum (über TopBar „Forschung“ geöffnet, wie in Anno): oben das laufende Projekt mit
    /// Fortschritt und der Forschungsvorrat, darunter vier Spalten für die Pfade Handel, Kultur, Industrie und
    /// Wissenschaft. In jeder Spalte stehen zuerst die Grundtechnologien, darunter die Aufbautechnologien; Linien
    /// führen von den Voraussetzungen zu ihnen, auch über Pfade hinweg. Schlüsseltechnologien der nächsten Epoche
    /// tragen einen Stern. Klick wählt das Projekt, der Tooltip nennt Wirkung, Kosten und Voraussetzungen. Unten
    /// stehen die Bedingungen der nächsten Epoche mit dem Button für den Wechsel.
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

        private const float ColumnWidth = 236f, NodeWidth = 208f, NodeHeight = 64f, RowHeight = 80f, LevelGap = 30f;

        private static readonly (TechPath path, string name, Color color)[] Paths =
        {
            (TechPath.Trade, "Handel", new Color(0.93f, 0.76f, 0.42f)),
            (TechPath.Culture, "Kultur", new Color(0.78f, 0.60f, 0.92f)),
            (TechPath.Industry, "Industrie", new Color(0.90f, 0.55f, 0.38f)),
            (TechPath.Science, "Wissenschaft", new Color(0.48f, 0.72f, 0.96f))
        };

        private class Node
        {
            public TechnologyDefinition Tech;
            public Button Button;
            public Image Image;
            public TextMeshProUGUI Title;
            public TextMeshProUGUI State;
            public RectTransform Progress;
            public GameObject Key;
            public Vector2 Center;
        }

        private class Link
        {
            public TechnologyDefinition From;
            public Image Image;
        }

        private RectTransform panel;
        private TextMeshProUGUI statusText;
        private UiFactory.StatBar projectBar;
        private TextMeshProUGUI epochText;
        private Button advanceButton;
        private readonly StringBuilder builder = new StringBuilder();
        private readonly List<Node> nodes = new List<Node>();
        private readonly List<Link> links = new List<Link>();
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
            if (city == null || research == null || statusText == null || !Visible) return;

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.2f;

            var current = research.Current;
            float pool = research.researchResource != null ? city.Stock.Get(research.researchResource) : 0f;
            if (current != null)
            {
                float progress = research.GetProgress(current);
                statusText.text = $"Projekt: <b>{current.displayName}</b>   " + Muted($"Vorrat {pool:0} Forschung");
                projectBar.Root.SetActive(true);
                projectBar.Set(progress / Mathf.Max(1f, current.researchCost), theme.accent,
                    $"{progress:0} / {current.researchCost:0}");
            }
            else
            {
                statusText.text = "Kein Projekt gewählt – Technologie anklicken   " + Muted($"Vorrat {pool:0} Forschung");
                projectBar.Root.SetActive(false);
            }

            var keys = KeyTechnologies();
            foreach (var node in nodes) RefreshNode(node, current, keys);
            foreach (var link in links)
                link.Image.color = research.IsUnlocked(link.From)
                    ? new Color(theme.accent.r, theme.accent.g, theme.accent.b, 0.9f)
                    : new Color(theme.textMuted.r, theme.textMuted.g, theme.textMuted.b, 0.35f);

            RefreshEpoch();
        }

        private void RefreshNode(Node node, TechnologyDefinition current, HashSet<TechnologyDefinition> keys)
        {
            var tech = node.Tech;
            bool unlocked = research.IsUnlocked(tech);
            bool available = research.CanResearch(tech);
            float progress = research.GetProgress(tech);

            node.Image.color = unlocked ? theme.done : tech == current ? theme.accent : available ? theme.button : theme.locked;
            node.Button.interactable = available;
            node.Title.alpha = unlocked || available ? 1f : 0.55f;
            node.State.text = unlocked ? "erforscht"
                : tech == current ? $"läuft · {progress:0} / {tech.researchCost:0}"
                : available ? (progress > 0f ? $"{progress:0} / {tech.researchCost:0}" : $"{tech.researchCost:0} Forschung")
                : !city.IsEpochReached(tech.epoch) ? "spätere Epoche" : "Voraussetzung fehlt";
            node.Progress.anchorMax = new Vector2(unlocked ? 1f : Mathf.Clamp01(progress / Mathf.Max(1f, tech.researchCost)), 1f);
            node.Key.SetActive(keys.Contains(tech));
        }

        private HashSet<TechnologyDefinition> KeyTechnologies()
        {
            var keys = new HashSet<TechnologyDefinition>();
            var next = progression != null ? progression.NextEpoch : null;
            if (next != null && next.keyTechnologies != null)
                foreach (var t in next.keyTechnologies)
                    if (t != null) keys.Add(t);
            return keys;
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
            builder.AppendLine($"Nächste Epoche: <b>{next.displayName}</b>");
            if (next.requiredKeyTechnologies > 0)
                builder.AppendLine(Condition($"Schlüsseltechnologien (Stern) {progression.CountKeyTechnologies(next)} / {next.requiredKeyTechnologies}",
                    progression.CountKeyTechnologies(next) >= next.requiredKeyTechnologies));
            if (next.requiredPopulation > 0)
            {
                int population = progression.CountPopulation();
                builder.AppendLine(Condition($"Einwohner {Mathf.Min(population, next.requiredPopulation)} / {next.requiredPopulation}",
                    population >= next.requiredPopulation));
            }
            if (next.milestones != null)
                foreach (var m in next.milestones)
                    if (m.building != null)
                    {
                        int count = progression.CountBuildings(m.building);
                        builder.AppendLine(Condition($"{m.building.displayName} {Mathf.Min(count, m.count)} / {m.count}", count >= m.count));
                    }
            epochText.text = builder.ToString().TrimEnd();

            bool canAdvance = progression.CanAdvance;
            advanceButton.gameObject.SetActive(true);
            advanceButton.interactable = canAdvance;
            advanceButton.targetGraphic.color = canAdvance ? theme.accent : theme.locked;
        }

        private string Condition(string text, bool met) =>
            met ? $"<color={UiTheme.Hex(theme.positive)}>{text}  erfüllt</color>" : $"<color={UiTheme.Hex(theme.textMuted)}>{text}</color>";

        private string Muted(string text) => $"<color={UiTheme.Hex(theme.textMuted)}>{text}</color>";

        /// <summary>Wirkung, Kosten, Voraussetzungen (mit Stand) und ob die Technologie ein Schlüssel ist.</summary>
        private string TechTooltip(TechnologyDefinition tech)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<b>{tech.displayName}</b>");
            sb.AppendLine(Muted($"{PathName(tech.path)}  ·  {tech.researchCost:0} Forschung"));
            if (!string.IsNullOrEmpty(tech.description)) sb.AppendLine(tech.description);
            if (tech.prerequisites != null && tech.prerequisites.Length > 0)
            {
                var parts = new List<string>();
                foreach (var p in tech.prerequisites)
                    if (p != null) parts.Add(research.IsUnlocked(p) ? $"{p.displayName} (erforscht)" : p.displayName);
                sb.AppendLine("Voraussetzung: " + string.Join(", ", parts));
            }
            var next = progression != null ? progression.NextEpoch : null;
            if (next != null && KeyTechnologies().Contains(tech))
                sb.AppendLine($"<color={UiTheme.Hex(theme.heading)}>Schlüsseltechnologie für das {next.displayName}</color>");
            return sb.ToString().TrimEnd();
        }

        private static string PathName(TechPath path)
        {
            foreach (var p in Paths)
                if (p.path == path) return p.name;
            return "";
        }

        // ---------------------------------------------------------------- Aufbau

        private void Build()
        {
            var canvas = UiFactory.CreateCanvas("ResearchHudCanvas", transform);

            // Fenster oben mittig unter der TopBar; Höhe passt sich dem Inhalt an
            panel = UiFactory.CreatePanel("ResearchPanel", canvas, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(Paths.Length * ColumnWidth + 28f, 0f), theme.panel);
            panel.anchoredPosition = new Vector2(0f, -TopBar.Height - 12f);
            UiFactory.AddColumn(panel.gameObject, 14, 10f);
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            UiFactory.WindowHeader(panel, "Forschung", theme, () => SetVisible(false));
            statusText = UiFactory.CreateText("Status", panel, "", theme.bodySize, TextAlignmentOptions.MidlineLeft, theme.text);
            projectBar = UiFactory.CreateBar(panel, "Fortschritt", theme);

            // Spaltenköpfe in den Pfadfarben
            var heads = new GameObject("PathHeads", typeof(RectTransform));
            heads.transform.SetParent(panel, false);
            UiFactory.AddRow(heads, TextAnchor.MiddleLeft, 0, 0f);
            foreach (var (_, name, color) in Paths)
            {
                var head = UiFactory.CreateText("Head_" + name, heads.transform, name, theme.bodySize, TextAlignmentOptions.Center, color);
                head.gameObject.AddComponent<LayoutElement>().preferredWidth = ColumnWidth;
            }

            BuildTree();

            var epochTitle = UiFactory.CreateText("EpochTitle", panel, "Epoche", theme.titleSize - 4f, TextAlignmentOptions.Left, theme.heading);
            epochTitle.margin = new Vector4(0f, 6f, 0f, 0f);
            epochText = UiFactory.CreateText("EpochStatus", panel, "", theme.smallSize + 1f, TextAlignmentOptions.TopLeft, theme.text);
            advanceButton = UiFactory.CreateButton(panel, "AdvanceEpoch", "Epoche wechseln", new Vector2(0f, 48f),
                theme.bodySize, theme.locked, theme.text, () => progression.TryAdvance(),
                theme.icons != null ? theme.icons.epoch : null);
            if (progression == null) { epochTitle.gameObject.SetActive(false); advanceButton.gameObject.SetActive(false); }
        }

        /// <summary>
        /// Ordnet die Technologien in Spalten (Pfad) und Ebenen (0 = ohne Voraussetzung, sonst 1 + höchste Ebene der
        /// Voraussetzungen) und legt erst die Linien, dann die Knoten an, damit die Linien dahinter liegen.
        /// </summary>
        private void BuildTree()
        {
            var techs = new List<TechnologyDefinition>();
            if (research != null && research.technologies != null)
                foreach (var t in research.technologies)
                    if (t != null) techs.Add(t);

            var levels = new Dictionary<TechnologyDefinition, int>();
            int Level(TechnologyDefinition t)
            {
                if (levels.TryGetValue(t, out int known)) return known;
                levels[t] = 0; // schützt vor Kreisen
                int level = 0;
                if (t.prerequisites != null)
                    foreach (var p in t.prerequisites)
                        if (p != null && techs.Contains(p)) level = Mathf.Max(level, Level(p) + 1);
                return levels[t] = level;
            }

            // Position je Technologie: Spalte nach Pfad, darin nach Ebene sortiert untereinander
            var centers = new Dictionary<TechnologyDefinition, Vector2>();
            int maxRow = 0, maxLevel = 0;
            for (int col = 0; col < Paths.Length; col++)
            {
                var column = techs.FindAll(t => t.path == Paths[col].path);
                column.Sort((a, b) => Level(a) != Level(b) ? Level(a).CompareTo(Level(b)) : techs.IndexOf(a).CompareTo(techs.IndexOf(b)));
                for (int row = 0; row < column.Count; row++)
                {
                    int level = Level(column[row]);
                    centers[column[row]] = new Vector2(col * ColumnWidth + ColumnWidth * 0.5f,
                        -(row * RowHeight + level * LevelGap + NodeHeight * 0.5f));
                    maxRow = Mathf.Max(maxRow, row);
                    maxLevel = Mathf.Max(maxLevel, level);
                }
            }

            var treeGo = new GameObject("Tree", typeof(RectTransform), typeof(LayoutElement));
            treeGo.transform.SetParent(panel, false);
            var tree = (RectTransform)treeGo.transform;
            var layout = treeGo.GetComponent<LayoutElement>();
            layout.preferredWidth = Paths.Length * ColumnWidth;
            layout.preferredHeight = maxRow * RowHeight + maxLevel * LevelGap + NodeHeight + 4f;

            foreach (var tech in techs)
                if (tech.prerequisites != null)
                    foreach (var p in tech.prerequisites)
                        if (p != null && centers.ContainsKey(p))
                            AddLink(tree, p, centers[p] - new Vector2(0f, NodeHeight * 0.5f),
                                centers[tech] + new Vector2(0f, NodeHeight * 0.5f));

            foreach (var tech in techs) AddNode(tree, tech, centers[tech]);
        }

        private void AddLink(RectTransform tree, TechnologyDefinition from, Vector2 a, Vector2 b)
        {
            var go = new GameObject("Link", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(tree, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = a;
            var delta = b - a;
            rt.sizeDelta = new Vector2(delta.magnitude, 3f);
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            links.Add(new Link { From = from, Image = image });
        }

        private void AddNode(RectTransform tree, TechnologyDefinition tech, Vector2 center)
        {
            var button = UiFactory.CreateButton(tree, "Tech_" + tech.name, "", new Vector2(NodeWidth, NodeHeight),
                theme.bodySize, theme.button, theme.text, () => research.Select(tech));
            Destroy(button.GetComponentInChildren<TextMeshProUGUI>().gameObject);
            var rt = (RectTransform)button.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = center;
            rt.sizeDelta = new Vector2(NodeWidth, NodeHeight);

            // Streifen in der Pfadfarbe links
            Color pathColor = theme.text;
            foreach (var p in Paths) if (p.path == tech.path) pathColor = p.color;
            var stripe = UiFactory.CreatePanel("Path", rt, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                new Vector2(5f, -14f), pathColor, PanelStyle.Flat);
            stripe.anchoredPosition = new Vector2(7f, 0f);

            var title = UiFactory.CreateText("Title", rt, tech.displayName, theme.bodySize - 1f, TextAlignmentOptions.TopLeft, theme.text);
            Place(title.rectTransform, new Vector2(18f, -8f), new Vector2(-34f, 24f));
            var state = UiFactory.CreateText("State", rt, "", theme.smallSize - 2f, TextAlignmentOptions.TopLeft, theme.textMuted);
            Place(state.rectTransform, new Vector2(18f, -33f), new Vector2(-24f, 20f));

            // Schlüssel-Stern oben rechts (Symbol, die Standardschrift hat kein Sternzeichen)
            var keyGo = new GameObject("Key", typeof(RectTransform), typeof(Image));
            keyGo.transform.SetParent(rt, false);
            var keyImage = keyGo.GetComponent<Image>();
            keyImage.sprite = theme.icons != null ? theme.icons.research : null;
            keyImage.color = theme.heading;
            keyImage.preserveAspect = true;
            keyImage.raycastTarget = false;
            Place((RectTransform)keyGo.transform, new Vector2(-6f, -4f), new Vector2(26f, 26f), right: true);

            // Fortschritt als dünner Balken am unteren Rand
            var track = UiFactory.CreatePanel("Track", rt, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-28f, 4f), theme.track, PanelStyle.Flat);
            track.anchoredPosition = new Vector2(4f, 6f);
            var fill = UiFactory.CreatePanel("Fill", track, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, theme.positive, PanelStyle.Flat);
            fill.offsetMin = fill.offsetMax = Vector2.zero;

            Tooltip.Attach(button.gameObject, () => TechTooltip(tech));
            nodes.Add(new Node
            {
                Tech = tech, Button = button, Image = (Image)button.targetGraphic, Title = title, State = state,
                Progress = fill, Key = keyGo, Center = center
            });
        }

        /// <summary>Text oben links (oder oben rechts) im Knoten; size.x negativ = Breite relativ zum Knoten.</summary>
        private static void Place(RectTransform rt, Vector2 offset, Vector2 size, bool right = false)
        {
            float anchorX = right ? 1f : 0f;
            if (size.x < 0f)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
            }
            else rt.anchorMin = rt.anchorMax = new Vector2(anchorX, 1f);
            rt.pivot = new Vector2(anchorX, 1f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = size;
        }
    }
}
