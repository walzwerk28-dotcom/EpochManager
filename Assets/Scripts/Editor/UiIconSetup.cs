using System.IO;
using UnityEditor;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Richtet die Symbole ein: importiert die weißen 100-px-Symbole des Kenney-Pakets „Game Icons“ als Sprites,
    /// legt Assets/Content/UI/UiIcons.asset an, trägt die Symbole ein und hängt es an das Farbschema.
    /// Mehrfaches Ausführen überschreibt nur die Zuordnung.
    /// </summary>
    public static class UiIconSetup
    {
        private const string Source = "Assets/ThirdParty/Kenney/GameIcons/PNG/White/2x";
        private const string IconsPath = "Assets/Content/UI/UiIcons.asset";
        private const string ThemePath = "Assets/Content/UI/UiTheme.asset";

        [MenuItem("EpochCity/UI-Symbole einrichten")]
        public static void Setup()
        {
            if (!AssetDatabase.IsValidFolder(Source))
            {
                Debug.LogWarning($"Symbole fehlen: {Source} (Kenney „Game Icons“ dort ablegen)");
                return;
            }

            var icons = AssetDatabase.LoadAssetAtPath<UiIcons>(IconsPath);
            if (icons == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(IconsPath));
                icons = ScriptableObject.CreateInstance<UiIcons>();
                AssetDatabase.CreateAsset(icons, IconsPath);
            }

            icons.city = Icon("home");
            icons.research = Icon("star");
            icons.menu = Icon("menuList");
            icons.help = Icon("question");
            icons.close = Icon("cross");
            icons.save = Icon("save");
            icons.load = Icon("import");
            icons.pause = Icon("pause");
            icons.residents = Icon("multiplayer");

            icons.road = Icon("scrollHorizontal");
            icons.zones = Icon("menuGrid");
            icons.production = Icon("gear");
            icons.commerce = Icon("shoppingCart");
            icons.service = Icon("information");
            icons.storage = Icon("shoppingBasket");
            icons.housing = Icon("home");
            icons.monument = Icon("medal1");
            icons.wonder = Icon("trophy");
            icons.demolish = Icon("trashcan");
            icons.overlay = Icon("signal3");

            icons.locked = Icon("locked");
            icons.done = Icon("checkmark");
            icons.warning = Icon("warning");
            icons.epoch = Icon("trophy");
            EditorUtility.SetDirty(icons);

            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(ThemePath);
            if (theme != null)
            {
                theme.icons = icons;
                EditorUtility.SetDirty(theme);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("UI-Symbole eingerichtet.");
        }

        /// <summary>Stellt die Datei auf Sprite um (ohne Mipmaps, scharf skaliert) und lädt sie.</summary>
        private static Sprite Icon(string name)
        {
            string path = $"{Source}/{name}.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Symbol fehlt: {path}");
                return null;
            }
            if (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
