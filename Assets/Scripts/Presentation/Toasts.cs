using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpochCity
{
    /// <summary>
    /// Kurze Meldungen oben rechts unter der TopBar (neben dem mittigen Forschungsfenster), die nach einigen
    /// Sekunden ausblenden (wie die Hinweise in Anno). Quellen: Speichern und Laden, erforschte Technologien, ein möglicher und ein vollzogener Epochenwechsel,
    /// leere und wieder ausgeglichene Stadtkasse.
    /// Andere Darstellungen können über Show eigene Meldungen zeigen.
    /// </summary>
    public class Toasts : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [SerializeField] private SaveSystem saveSystem;
        [SerializeField] private EpochProgression progression;

        [Tooltip("Sekunden, die eine Meldung voll sichtbar bleibt")]
        [Min(0.5f)] public float duration = 3.5f;
        [Tooltip("Sekunden zum Ausblenden")]
        [Min(0.05f)] public float fadeTime = 0.6f;
        [Min(1)] public int maxVisible = 4;

        [Header("Aussehen")]
        [Tooltip("Gemeinsames Farbschema; leer = Standardwerte")]
        [SerializeField] private UiTheme theme;

        private RectTransform stack;
        private readonly List<(CanvasGroup group, float shownAt)> visible = new List<(CanvasGroup, float)>();
        private bool couldAdvance;
        private bool wasInDebt;
        private float epochTimer;

        private void Awake()
        {
            theme = UiTheme.Resolve(theme);
            var canvas = UiFactory.CreateCanvas("ToastCanvas", transform);
            canvas.GetComponent<Canvas>().sortingOrder = 20;

            var go = new GameObject("Toasts", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            stack = (RectTransform)go.transform;
            stack.anchorMin = stack.anchorMax = new Vector2(1f, 1f);
            stack.pivot = new Vector2(1f, 1f);
            stack.anchoredPosition = new Vector2(-16f, -TopBar.Height - 14f);
            stack.sizeDelta = new Vector2(560f, 0f);
            var column = go.AddComponent<VerticalLayoutGroup>();
            column.childAlignment = TextAnchor.UpperRight;
            column.spacing = 8f;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = column.childForceExpandHeight = false;
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void OnEnable()
        {
            if (saveSystem != null) saveSystem.Reported += OnSaveReported;
            if (city != null)
            {
                city.TechnologyUnlocked += OnTechnologyUnlocked;
                city.EpochChanged += OnEpochChanged;
            }
        }

        private void OnDisable()
        {
            if (saveSystem != null) saveSystem.Reported -= OnSaveReported;
            if (city != null)
            {
                city.TechnologyUnlocked -= OnTechnologyUnlocked;
                city.EpochChanged -= OnEpochChanged;
            }
        }

        private void OnSaveReported(string message, bool success) =>
            Show(message, success ? theme.positive : theme.danger, Icon(i => success ? i.done : i.warning));

        private void OnTechnologyUnlocked(TechnologyDefinition tech) =>
            Show($"Erforscht: {tech.displayName}", theme.positive, Icon(i => i.research));

        private Sprite Icon(System.Func<UiIcons, Sprite> pick) => theme.icons != null ? pick(theme.icons) : null;

        private void OnEpochChanged(EpochDefinition epoch)
        {
            couldAdvance = false;
            Show($"Neue Epoche: {epoch.displayName}", theme.heading, Icon(i => i.epoch));
        }

        /// <summary>Zeigt eine Meldung; die Farbe gilt für den schmalen Streifen links und das Symbol.</summary>
        public void Show(string message, Color accent, Sprite icon = null)
        {
            if (stack == null) return;
            while (visible.Count >= maxVisible) Remove(0);

            var panel = UiFactory.CreatePanel("Toast", stack, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero,
                theme.panel);
            var row = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(12, 18, 10, 10);
            row.spacing = 12f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false; // Breite nach Text, die Spalte zentriert

            var stripe = UiFactory.CreatePanel("Accent", panel, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero,
                accent, PanelStyle.Flat);
            var stripeLayout = stripe.gameObject.AddComponent<LayoutElement>();
            stripeLayout.preferredWidth = 6f;
            stripeLayout.preferredHeight = 28f;

            if (icon != null) UiFactory.AddIcon(panel, icon, 24f, accent);

            UiFactory.CreateText("Text", panel, message, theme.bodySize, TextAlignmentOptions.MidlineLeft, theme.text);

            var group = panel.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            visible.Add((group, Time.unscaledTime));
        }

        private void Update()
        {
            // Epochenwechsel möglich: nur beim Übergang melden, zweimal pro Sekunde prüfen
            epochTimer -= Time.unscaledDeltaTime;
            if (progression != null && epochTimer <= 0f)
            {
                epochTimer = 0.5f;
                bool can = progression.CanAdvance;
                if (can && !couldAdvance && progression.NextEpoch != null)
                    Show($"Epochenwechsel zu {progression.NextEpoch.displayName} möglich", theme.heading, Icon(i => i.epoch));
                couldAdvance = can;
            }

            // Kasse unter 0 bzw. wieder ausgeglichen: nur beim Übergang melden
            if (city != null)
            {
                bool inDebt = city.Money < 0f;
                if (inDebt && !wasInDebt)
                    Show("Die Stadtkasse ist leer: Gebäude mit Unterhalt arbeiten langsamer", theme.danger,
                        Icon(i => i.warning));
                else if (!inDebt && wasInDebt)
                    Show("Die Stadtkasse ist wieder ausgeglichen", theme.positive, Icon(i => i.done));
                wasInDebt = inDebt;
            }

            for (int i = visible.Count - 1; i >= 0; i--)
            {
                float age = Time.unscaledTime - visible[i].shownAt;
                if (age >= duration + fadeTime) Remove(i);
                else visible[i].group.alpha = age <= duration ? 1f : 1f - (age - duration) / fadeTime;
            }
        }

        private void Remove(int index)
        {
            if (visible[index].group != null) Destroy(visible[index].group.gameObject);
            visible.RemoveAt(index);
        }
    }
}
