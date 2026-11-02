using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Antike-Durchlauf auf mehreren Karten hintereinander, ohne Handgriffe: startet für jede Karte einen frischen
    /// Play-Modus direkt in der Spielszene (Start im Hauptmenü ist so lange aus), lässt AntiquityBot laufen, schreibt die
    /// Zusammenfassung nach persistentDataPath/balance/summary_&lt;Startwert&gt;.txt und beendet den Play-Modus wieder.
    /// Der Stand der Warteschlange überlebt den Domain-Reload in SessionState. Autosave ist während des Laufs aus.
    /// </summary>
    [InitializeOnLoad]
    public static class AntiquityBatch
    {
        private const string QueueKey = "EpochCity.AntiquityBatch.Queue";      // „1234,42,777“
        private const string MonthsKey = "EpochCity.AntiquityBatch.Months";
        private const string MoneyKey = "EpochCity.AntiquityBatch.Money";
        private const string RestoreMenuKey = "EpochCity.AntiquityBatch.RestoreMenu";

        public static readonly int[] ReferenceSeeds = { 1234, 42, 777 };

        static AntiquityBatch()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            // Ein abgeschlossener Stapel, dessen Abschluss verloren ging (z. B. Domain-Reload), wird hier beendet
            if (!EditorApplication.isPlayingOrWillChangePlaymode && SessionState.GetString(QueueKey, "") == Done)
                EditorApplication.delayCall += Finish;
        }

        private const string Done = "done";

        /// <summary>Läuft gerade ein Stapel?</summary>
        public static bool Running => !string.IsNullOrEmpty(SessionState.GetString(QueueKey, ""));

        public static string SummaryPath(int seed) =>
            Path.Combine(Application.persistentDataPath, "balance", $"summary_{seed}.txt");

        [MenuItem("EpochCity/Antike-Durchlauf auf den Referenzkarten (1234, 42, 777)")]
        private static void RunReferenceFromMenu() => Run(ReferenceSeeds, 240, 5000f);

        /// <summary>Startet den Stapel (nur außerhalb des Play-Modus).</summary>
        public static void Run(int[] seeds, int months, float startMoney)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || seeds.Length == 0) return;
            SessionState.SetString(QueueKey, string.Join(",", seeds));
            SessionState.SetInt(MonthsKey, months);
            SessionState.SetFloat(MoneyKey, startMoney);
            SessionState.SetBool(RestoreMenuKey, PlayFromMainMenu.Enabled);
            PlayFromMainMenu.Enabled = false;
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (!Running) return;
            if (change == PlayModeStateChange.EnteredPlayMode) EditorApplication.delayCall += RunNext;
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                if (SessionState.GetString(QueueKey, "") == Done) Finish();
                else EditorApplication.delayCall += EditorApplication.EnterPlaymode; // nächste Karte
            }
        }

        private static void Finish()
        {
            SessionState.EraseString(QueueKey);
            PlayFromMainMenu.Enabled = SessionState.GetBool(RestoreMenuKey, true);
            Debug.Log("Antike-Durchlauf: alle Karten fertig.");
        }

        private static void RunNext()
        {
            var queue = SessionState.GetString(QueueKey, "").Split(',').Where(s => s.Length > 0 && s != Done).ToList();
            if (queue.Count == 0 || !EditorApplication.isPlaying) return;
            int seed = int.Parse(queue[0]);
            SessionState.SetString(QueueKey, string.Join(",", queue.Skip(1)));

            Application.runInBackground = true; // ohne Fokus berechnet Unity sonst keine Frames
            var save = Object.FindAnyObjectByType<SaveSystem>();
            if (save != null) save.autosaveMinutes = 0f;
            string summary = AntiquityBot.Run(seed, SessionState.GetInt(MonthsKey, 240), SessionState.GetFloat(MoneyKey, 5000f));
            Directory.CreateDirectory(Path.GetDirectoryName(SummaryPath(seed)));
            File.WriteAllText(SummaryPath(seed), summary);
            Debug.Log(summary);
            if (!Running) SessionState.SetString(QueueKey, Done);
            EditorApplication.ExitPlaymode();
        }
    }
}
