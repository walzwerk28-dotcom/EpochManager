using UnityEditor;
using UnityEditor.SceneManagement;

namespace EpochCity
{
    /// <summary>
    /// Play im Editor startet wie das fertige Spiel im Hauptmenü, egal welche Szene offen ist
    /// (EditorSceneManager.playModeStartScene). Abschaltbar über EpochCity → Play startet im Hauptmenü, z. B. für
    /// den Antike-Durchlauf, der eine frische Spielszene braucht; die Wahl gilt pro Rechner (EditorPrefs).
    /// </summary>
    [InitializeOnLoad]
    public static class PlayFromMainMenu
    {
        private const string MenuPath = "EpochCity/Play startet im Hauptmenü";
        private const string PrefKey = "EpochCity.PlayFromMainMenu";
        private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";

        static PlayFromMainMenu() => EditorApplication.delayCall += Apply;

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set
            {
                EditorPrefs.SetBool(PrefKey, value);
                Apply();
            }
        }

        [MenuItem(MenuPath, priority = 1)]
        private static void Toggle() => Enabled = !Enabled;

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        private static void Apply()
        {
            EditorSceneManager.playModeStartScene =
                Enabled ? AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath) : null;
        }
    }
}
