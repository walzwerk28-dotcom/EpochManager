using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EpochCity
{
    /// <summary>
    /// Startmenü (entschieden 2026-10-31) wie das Hauptmenü in Anno und Cities: Skylines: Fortsetzen (Schnellspeicher),
    /// Freies Bauen mit einem Fenster für das neue Spiel (Karte, Startgeld, Brände), Story-Modus (sichtbar, noch
    /// gesperrt) und Beenden. Überträgt die Wahl über GameSettings.Active bzw. SaveSystem.PendingLoad in die
    /// Spielszene. Baut seine uGUI zur Laufzeit selbst auf wie die HUDs.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        public const string MenuScene = "MainMenu";
        public const string DefaultGameScene = "SampleScene";

        [SerializeField] private UiTheme theme;
        [Tooltip("Szene mit der Stadt")]
        public string gameScene = DefaultGameScene;

        private readonly GameSettings settings = new GameSettings();
        private RectTransform newGameWindow;
        private TMP_InputField seedInput;
        private Button continueButton;

        /// <summary>Für Tests: ist das Fenster „Freies Bauen“ offen?</summary>
        public bool NewGameOpen => newGameWindow != null && newGameWindow.gameObject.activeSelf;

        private void Awake()
        {
            theme = UiTheme.Resolve(theme);
            UiFactory.EnsureEventSystem();
            Build();
        }

        // ---------------------------------------------------------------- Aktionen

        /// <summary>Startet ein neues Spiel mit den gewählten Einstellungen.</summary>
        public void StartNewGame()
        {
            settings.mode = GameMode.FreeBuild;
            settings.mapSeed = seedInput != null && int.TryParse(seedInput.text, out int seed) ? Mathf.Max(0, seed) : 0;
            GameSettings.Active = settings;
            SaveSystem.PendingLoad = null;
            SceneManager.LoadScene(gameScene);
        }

        /// <summary>Lädt die Spielszene und darin den neuesten Spielstand (wie „Fortsetzen“ in Anno).</summary>
        public void Continue()
        {
            string slot = SaveSystem.NewestSlot();
            if (slot == null) return;
            GameSettings.Active = null; // Modus und Brände kommen aus dem Spielstand
            SaveSystem.PendingLoad = slot;
            SceneManager.LoadScene(gameScene);
        }

        public void ToggleNewGame() => newGameWindow.gameObject.SetActive(!newGameWindow.gameObject.activeSelf);

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------------------------------------------------------------- Aufbau

        private void Build()
        {
            var canvas = UiFactory.CreateCanvas("MainMenuCanvas", transform);

            // Spalte links wie in Anno, die Bildmitte bleibt frei für den Hintergrund
            var column = UiFactory.CreatePanel("Menu", canvas, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(480f, 0f), theme.panel);
            column.anchoredPosition = new Vector2(120f, 0f);
            UiFactory.AddColumn(column.gameObject, 28, 14f);
            column.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var title = UiFactory.CreateText("Title", column, "Epochen-City-Builder", 40f,
                TextAlignmentOptions.Center, theme.heading);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.enableAutoSizing = true;
            title.fontSizeMin = 24f;
            title.fontSizeMax = 40f;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
            var subtitle = UiFactory.CreateText("Subtitle", column, "Von der Antike bis in die Zukunft", theme.bodySize,
                TextAlignmentOptions.Center, theme.textMuted);
            subtitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
            Gap(column, 10f);

            var size = new Vector2(0f, 58f);
            continueButton = UiFactory.CreateButton(column, "Continue", "Fortsetzen", size, theme.titleSize,
                theme.button, theme.text, Continue, Icon(i => i.load));
            var saves = SaveSystem.ListSaves();
            continueButton.interactable = saves.Count > 0;
            if (saves.Count == 0) continueButton.image.color = theme.locked;
            Tooltip.Attach(continueButton.gameObject, () => saves.Count > 0
                ? $"<b>Fortsetzen</b>\nLädt den neuesten Spielstand:\n{Describe(saves[0])}"
                : "<b>Fortsetzen</b>\nNoch kein Spielstand – im Spiel mit F5 speichern.");

            UiFactory.CreateButton(column, "FreeBuild", "Freies Bauen", size, theme.titleSize, theme.accent,
                theme.text, ToggleNewGame, Icon(i => i.city));

            var story = UiFactory.CreateButton(column, "Story", "Story-Modus", size, theme.titleSize, theme.locked,
                theme.textMuted, () => { }, Icon(i => i.locked));
            story.interactable = false;
            Tooltip.Attach(story.gameObject, () => "<b>Story-Modus</b>\nKapitel mit Zielen – kommt später.");

            Gap(column, 6f);
            UiFactory.CreateButton(column, "Quit", "Beenden", size, theme.titleSize, theme.button, theme.text, Quit,
                Icon(i => i.close));

            BuildNewGameWindow(canvas);
        }

        private void BuildNewGameWindow(Transform canvas)
        {
            newGameWindow = UiFactory.CreatePanel("NewGameWindow", canvas, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(620f, 0f), theme.panel);
            newGameWindow.anchoredPosition = new Vector2(-260f, 0f);
            UiFactory.AddColumn(newGameWindow.gameObject, 20, 14f);
            newGameWindow.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            UiFactory.WindowHeader(newGameWindow, "Freies Bauen", theme, ToggleNewGame);

            // Karte: leer = zufällig, sonst fester Startwert (gleiche Zahl = gleiche Karte)
            var mapRow = Row(newGameWindow, "Karte");
            seedInput = CreateInput(mapRow, "Startwert (leer = zufällig)");
            Tooltip.Attach(seedInput.gameObject, () =>
                "<b>Karte</b>\nLeer lassen für eine zufällige Karte; gleiche Zahl ergibt dieselbe Karte.");

            var money = new string[GameSettings.StartMoneyLevels.Length];
            int normal = 0;
            for (int i = 0; i < money.Length; i++)
            {
                var level = GameSettings.StartMoneyLevels[i];
                money[i] = $"{level.name}\n<size=70%>{level.money:N0}</size>";
                if (Mathf.Approximately(level.money, settings.startMoney)) normal = i;
            }
            Options(Row(newGameWindow, "Startgeld"), money, normal,
                i => settings.startMoney = GameSettings.StartMoneyLevels[i].money);

            Options(Row(newGameWindow, "Brände"), new[] { "Aus", "Selten", "Normal", "Häufig" },
                (int)settings.fireFrequency, i => settings.fireFrequency = (FireFrequency)i);

            Gap(newGameWindow, 4f);
            UiFactory.CreateButton(newGameWindow, "Start", "Starten", new Vector2(0f, 56f), theme.titleSize,
                theme.accent, theme.text, StartNewGame, Icon(i => i.done));

            newGameWindow.gameObject.SetActive(false);
        }

        /// <summary>Zeile mit Beschriftung links; der Rückgabewert nimmt die Bedienelemente auf.</summary>
        private Transform Row(Transform parent, string label)
        {
            var row = new GameObject(label, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            UiFactory.AddRow(row, TextAnchor.MiddleLeft, 0, 8f);
            var text = UiFactory.CreateText("Label", row.transform, label, theme.bodySize,
                TextAlignmentOptions.MidlineLeft, theme.textMuted);
            text.gameObject.AddComponent<LayoutElement>().preferredWidth = 130f;
            return row.transform;
        }

        /// <summary>Auswahl aus festen Werten als Buttonreihe, die gewählte in der Akzentfarbe.</summary>
        private void Options(Transform row, string[] names, int selected, Action<int> onSelect)
        {
            var images = new List<Image>();
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                var button = UiFactory.CreateButton(row, names[i].Split('\n')[0], names[i], new Vector2(0f, 52f), theme.bodySize,
                    theme.button, theme.text, () =>
                    {
                        onSelect(index);
                        for (int k = 0; k < images.Count; k++) images[k].color = k == index ? theme.accent : theme.button;
                    });
                button.GetComponent<LayoutElement>().flexibleWidth = 1f;
                images.Add(button.image);
            }
            for (int k = 0; k < images.Count; k++) images[k].color = k == selected ? theme.accent : theme.button;
        }

        private TMP_InputField CreateInput(Transform parent, string placeholder)
        {
            var go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = theme.button;
            UiFactory.MakeRounded(image);
            var element = go.AddComponent<LayoutElement>();
            element.preferredHeight = 52f;
            element.flexibleWidth = 1f;

            var input = go.GetComponent<TMP_InputField>();
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.characterLimit = 9;
            input.textComponent.fontSize = theme.bodySize;
            input.textComponent.color = theme.text;
            input.textComponent.alignment = TextAlignmentOptions.MidlineLeft;
            if (input.placeholder is TextMeshProUGUI hint)
            {
                hint.text = placeholder;
                hint.fontSize = theme.bodySize;
                hint.color = theme.textMuted;
                hint.alignment = TextAlignmentOptions.MidlineLeft;
            }
            return input;
        }

        /// <summary>„Autosave 2 · 02.11. 14:30 · Antike, 1 240 Einwohner, Jahr 12“; ältere Stände ohne Kopf mit „–“.</summary>
        public static string Describe(SaveSystem.SaveInfo info)
        {
            string name = info.IsQuickSave ? "Schnellspeicher"
                : info.IsAutosave ? "Autosave " + info.slot.Substring(SaveSystem.AutosavePrefix.Length)
                : info.slot;
            string city = string.IsNullOrEmpty(info.epochName) ? "–"
                : $"{info.epochName}, {info.population:N0} Einwohner, Jahr {info.totalMonths / 12 + 1}";
            return $"{name} · {info.writtenAt:dd.MM. HH:mm} · {city}";
        }

        private static void Gap(Transform parent, float height)
        {
            var gap = new GameObject("Gap", typeof(RectTransform), typeof(LayoutElement));
            gap.transform.SetParent(parent, false);
            gap.GetComponent<LayoutElement>().preferredHeight = height;
        }

        private Sprite Icon(Func<UiIcons, Sprite> pick) => theme.icons != null ? pick(theme.icons) : null;
    }
}
