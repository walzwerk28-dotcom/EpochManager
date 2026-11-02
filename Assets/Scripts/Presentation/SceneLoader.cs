using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EpochCity
{
    /// <summary>
    /// Szenenwechsel mit Ladebildschirm: Eine Fläche mit Text und Fortschrittsbalken deckt alles ab, die Szene lädt im
    /// Hintergrund (LoadSceneAsync). Die Fläche bleibt, bis ein angeforderter Spielstand (SaveSystem.PendingLoad)
    /// geladen ist, damit man die leere Stadt nicht kurz sieht.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        /// <summary>Läuft gerade ein Wechsel? Weitere Aufrufe werden dann ignoriert.</summary>
        public static bool IsLoading { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => IsLoading = false;

        public static void Load(string scene, string text)
        {
            if (IsLoading) return;
            IsLoading = true;
            var go = new GameObject("LoadingScreen");
            DontDestroyOnLoad(go);
            var loader = go.AddComponent<SceneLoader>();
            loader.StartCoroutine(loader.Run(scene, text));
        }

        private IEnumerator Run(string scene, string text)
        {
            var theme = UiTheme.Resolve(null);
            var canvas = UiFactory.CreateCanvas("LoadingCanvas", transform);
            canvas.GetComponent<Canvas>().sortingOrder = 100; // über allem, auch über Meldungen und Tooltips

            var cover = UiFactory.CreatePanel("Cover", canvas, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Color(theme.panel.r, theme.panel.g, theme.panel.b, 1f), PanelStyle.Plain);
            var label = UiFactory.CreateText("Text", cover, text, theme.titleSize, TextAlignmentOptions.Center,
                theme.heading);
            var lrt = label.rectTransform;
            lrt.anchorMin = lrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrt.sizeDelta = new Vector2(800f, 50f);
            lrt.anchoredPosition = new Vector2(0f, 30f);

            var track = UiFactory.CreatePanel("Track", cover, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(420f, 12f), theme.track, PanelStyle.Flat);
            track.anchoredPosition = new Vector2(0f, -20f);
            var fill = UiFactory.CreatePanel("Fill", track, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, theme.accent, PanelStyle.Flat);

            yield return null; // erst die Fläche zeigen, dann laden
            var op = SceneManager.LoadSceneAsync(scene);
            while (op != null && !op.isDone)
            {
                fill.anchorMax = new Vector2(Mathf.Clamp01(op.progress / 0.9f) * 0.8f, 1f);
                yield return null;
            }
            // SaveSystem.Start lädt einen angeforderten Spielstand einen Frame später
            while (SaveSystem.PendingLoad != null || SaveSystem.IsLoadPending) yield return null;
            fill.anchorMax = Vector2.one;
            yield return null;
            yield return null;
            IsLoading = false;
            Destroy(gameObject);
        }
    }
}
