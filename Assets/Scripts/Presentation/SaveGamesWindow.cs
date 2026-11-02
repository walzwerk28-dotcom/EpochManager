using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpochCity
{
    /// <summary>
    /// Fenster „Spielstände“ wie in Anno und Cities: Skylines: Liste aller Stände (neuester zuerst) mit Laden und
    /// Löschen, im Spiel zusätzlich ein Namensfeld zum Speichern. Überschreiben und Löschen fragen nach. Im Spiel lädt
    /// es direkt (SaveSystem.Load), im Hauptmenü über die Spielszene mit Ladebildschirm (SaveSystem.PendingLoad).
    /// </summary>
    public class SaveGamesWindow : MonoBehaviour
    {
        private UiTheme theme;
        private SaveSystem saveSystem; // null = Hauptmenü
        private string gameScene;
        private RectTransform window, list, confirm;
        private TMP_InputField nameInput;
        private TextMeshProUGUI confirmText, hint;
        private Action confirmAction;

        /// <summary>Nach Speichern oder Löschen (z. B. damit das Hauptmenü „Fortsetzen“ anpasst).</summary>
        public event Action Changed;

        public bool Visible => window.gameObject.activeSelf;

        /// <summary>Das Fenster selbst, z. B. um es zu verschieben.</summary>
        public RectTransform Root => window;

        /// <summary>Für Tests: Anzahl der Zeilen in der Liste.</summary>
        public int RowCount => list.childCount;

        /// <summary>Baut das Fenster in den Canvas; saveSystem null = im Hauptmenü (nur laden und löschen).</summary>
        public static SaveGamesWindow Create(Transform canvas, UiTheme theme, SaveSystem saveSystem, string gameScene)
        {
            var w = canvas.gameObject.AddComponent<SaveGamesWindow>();
            w.theme = theme;
            w.saveSystem = saveSystem;
            w.gameScene = gameScene;
            w.Build(canvas);
            return w;
        }

        public void Toggle() => Show(!Visible);

        public void Show(bool show)
        {
            window.gameObject.SetActive(show);
            confirm.gameObject.SetActive(false);
            if (show) Refresh();
        }

        // ---------------------------------------------------------------- Aktionen

        /// <summary>Speichert unter dem eingegebenen Namen; ein vorhandener Stand wird erst nach Rückfrage ersetzt.</summary>
        public void SaveTyped()
        {
            string slot = SaveSystem.ToSlotName(nameInput.text);
            if (slot == null)
            {
                hint.text = "Bitte einen Namen eingeben (nicht „quicksave“ oder „autosave_…“).";
                return;
            }
            if (SaveSystem.Exists(slot)) Ask($"„{slot}“ überschreiben?", () => SaveAs(slot));
            else SaveAs(slot);
        }

        public void LoadSlot(string slot)
        {
            if (saveSystem != null)
            {
                Show(false);
                saveSystem.Load(slot);
                return;
            }
            GameSettings.Active = null; // Modus und Brände kommen aus dem Spielstand
            SaveSystem.PendingLoad = slot;
            SceneLoader.Load(gameScene, "Spielstand wird geladen …");
        }

        public void DeleteSlot(string slot) => Ask($"„{Title(slot)}“ endgültig löschen?", () =>
        {
            SaveSystem.Delete(slot);
            Refresh();
            Changed?.Invoke();
        });

        private void SaveAs(string slot)
        {
            if (saveSystem.Save(slot))
            {
                nameInput.text = "";
                hint.text = "";
            }
            Refresh();
            Changed?.Invoke();
        }

        private void Ask(string question, Action onYes)
        {
            confirmText.text = question;
            confirmAction = onYes;
            confirm.gameObject.SetActive(true);
        }

        // ---------------------------------------------------------------- Aufbau

        private void Build(Transform canvas)
        {
            window = UiFactory.CreatePanel("SaveGamesWindow", canvas, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(820f, 600f), theme.panel);
            UiFactory.AddColumn(window.gameObject, 20, 12f);
            var column = window.GetComponent<VerticalLayoutGroup>();
            column.childForceExpandHeight = false;
            UiFactory.WindowHeader(window, "Spielstände", theme, () => Show(false));

            if (saveSystem != null)
            {
                var row = new GameObject("SaveRow", typeof(RectTransform));
                row.transform.SetParent(window, false);
                UiFactory.AddRow(row, TextAnchor.MiddleLeft, 0, 8f);
                nameInput = UiFactory.CreateInputField(row.transform, "Name des Spielstands", theme);
                nameInput.characterLimit = 40;
                nameInput.onSubmit.AddListener(_ => SaveTyped());
                UiFactory.CreateButton(row.transform, "Save", "Speichern", new Vector2(170f, 52f), theme.bodySize,
                    theme.accent, theme.text, SaveTyped, Icon(i => i.save));
            }
            hint = UiFactory.CreateText("Hint", window, "", theme.smallSize, TextAlignmentOptions.MidlineLeft,
                theme.danger);
            hint.gameObject.AddComponent<LayoutElement>().preferredHeight = saveSystem != null ? 22f : 0f;

            // Liste mit Bildlauf
            var scroll = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image),
                typeof(Mask), typeof(LayoutElement));
            scroll.transform.SetParent(window, false);
            scroll.GetComponent<LayoutElement>().flexibleHeight = 1f;
            var scrollImage = scroll.GetComponent<Image>();
            scrollImage.color = theme.panelSoft;
            UiFactory.MakeRounded(scrollImage);
            scroll.GetComponent<Mask>().showMaskGraphic = true;

            var content = new GameObject("List", typeof(RectTransform));
            content.transform.SetParent(scroll.transform, false);
            list = (RectTransform)content.transform;
            list.anchorMin = new Vector2(0f, 1f);
            list.anchorMax = new Vector2(1f, 1f);
            list.pivot = new Vector2(0.5f, 1f);
            list.sizeDelta = Vector2.zero;
            UiFactory.AddColumn(content, 8, 6f);
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = scroll.GetComponent<ScrollRect>();
            scrollRect.content = list;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            BuildConfirm();
            window.gameObject.SetActive(false);
        }

        /// <summary>Rückfrage über der abgedunkelten Liste: Frage, Ja und Abbrechen.</summary>
        private void BuildConfirm()
        {
            // Abdunkelung über dem ganzen Fenster; fängt auch Klicks auf die Liste ab
            confirm = UiFactory.CreatePanel("Confirm", window, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Color(0f, 0f, 0f, 0.6f), PanelStyle.Flat);
            confirm.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var box = UiFactory.CreatePanel("Box", confirm, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(480f, 0f), theme.panel);
            UiFactory.AddColumn(box.gameObject, 20, 12f);
            box.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            confirmText = UiFactory.CreateText("Question", box, "", theme.bodySize, TextAlignmentOptions.Center,
                theme.text);
            confirmText.gameObject.AddComponent<LayoutElement>().preferredHeight = 60f;
            var row = new GameObject("Buttons", typeof(RectTransform));
            row.transform.SetParent(box, false);
            UiFactory.AddRow(row, TextAnchor.MiddleCenter, 0, 10f);
            UiFactory.CreateButton(row.transform, "Yes", "Ja", new Vector2(160f, 46f), theme.bodySize, theme.danger,
                theme.text, () =>
                {
                    confirm.gameObject.SetActive(false);
                    confirmAction?.Invoke();
                });
            UiFactory.CreateButton(row.transform, "No", "Abbrechen", new Vector2(160f, 46f), theme.bodySize,
                theme.button, theme.text, () => confirm.gameObject.SetActive(false));
            confirm.gameObject.SetActive(false);
        }

        /// <summary>Baut die Liste neu auf (beim Öffnen und nach Änderungen).</summary>
        public void Refresh()
        {
            for (int i = list.childCount - 1; i >= 0; i--)
            {
                var child = list.GetChild(i).gameObject;
                child.transform.SetParent(null);
                Destroy(child);
            }

            var saves = SaveSystem.ListSaves();
            if (saves.Count == 0)
            {
                var empty = UiFactory.CreateText("Empty", list, "Noch keine Spielstände.", theme.bodySize,
                    TextAlignmentOptions.Center, theme.textMuted);
                empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 60f;
                return;
            }
            foreach (var info in saves)
            {
                string slot = info.slot;
                var row = UiFactory.CreatePanel(slot, list, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                    Vector2.zero, theme.panel, PanelStyle.Flat);
                UiFactory.AddRow(row.gameObject, TextAnchor.MiddleLeft, 8, 8f);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 68f;

                var texts = new GameObject("Texts", typeof(RectTransform), typeof(LayoutElement));
                texts.transform.SetParent(row, false);
                texts.GetComponent<LayoutElement>().flexibleWidth = 1f;
                UiFactory.AddColumn(texts, 0, 0f);
                texts.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
                var title = UiFactory.CreateText("Title", texts.transform, Title(slot), theme.bodySize,
                    TextAlignmentOptions.MidlineLeft, info.IsAutosave || info.IsQuickSave ? theme.textMuted : theme.text);
                title.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;
                var details = UiFactory.CreateText("Details", texts.transform, Details(info), theme.smallSize,
                    TextAlignmentOptions.MidlineLeft, theme.textMuted);
                details.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

                UiFactory.CreateButton(row, "Load", "Laden", new Vector2(130f, 46f), theme.bodySize, theme.accent,
                    theme.text, () => LoadSlot(slot), Icon(i => i.load));
                var deleteIcon = Icon(i => i.demolish);
                var delete = UiFactory.CreateButton(row, "Delete", deleteIcon != null ? "" : "×",
                    new Vector2(46f, 46f), theme.bodySize, theme.button, theme.text, () => DeleteSlot(slot), deleteIcon);
                Tooltip.Attach(delete.gameObject, () => "<b>Löschen</b>\nDen Spielstand endgültig löschen.");
            }
        }

        /// <summary>Anzeigename: Schnellspeicher, Autosave 2 oder der eingegebene Name.</summary>
        public static string Title(string slot) =>
            slot == SaveSystem.QuickSaveSlot ? "Schnellspeicher (F5)"
            : slot.StartsWith(SaveSystem.AutosavePrefix) ? "Autosave " + slot.Substring(SaveSystem.AutosavePrefix.Length)
            : slot;

        /// <summary>„02.11. 23:44 · Antike · 1 240 Einwohner · Jahr 12“; ältere Stände ohne Kopf mit „–“.</summary>
        public static string Details(SaveSystem.SaveInfo info)
        {
            string city = string.IsNullOrEmpty(info.epochName) ? "–"
                : $"{info.epochName} · {info.population:N0} Einwohner · Jahr {info.totalMonths / 12 + 1}";
            return $"{info.writtenAt:dd.MM.yyyy HH:mm} · {city}";
        }

        private Sprite Icon(Func<UiIcons, Sprite> pick) => theme.icons != null ? pick(theme.icons) : null;
    }
}
