using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Symbole der Oberfläche (weiße Kenney Game Icons, CC0), über UiTheme.icons erreichbar. Die Sprites trägt
    /// UiIconSetup (Menü EpochCity → UI-Symbole einrichten) ein; fehlende Symbole lassen die HUDs einfach weg.
    /// Das Paket hat keine Stadtbau-Motive; Straße und Forschung nutzen die nächstbesten Symbole.
    /// </summary>
    [CreateAssetMenu(menuName = "EpochCity/UI-Symbole", fileName = "UiIcons")]
    public class UiIcons : ScriptableObject
    {
        [Header("Leiste und Fenster")]
        public Sprite city;
        public Sprite research;
        public Sprite menu;
        public Sprite help;
        public Sprite close;
        public Sprite save;
        public Sprite load;
        public Sprite pause;
        public Sprite residents;

        [Header("Bauleiste")]
        public Sprite road;
        public Sprite zones;
        public Sprite production;
        public Sprite commerce;
        public Sprite service;
        public Sprite storage;
        public Sprite housing;
        public Sprite monument;
        public Sprite wonder;
        public Sprite demolish;
        public Sprite overlay;

        [Header("Zustände und Meldungen")]
        public Sprite locked;
        public Sprite done;
        public Sprite warning;
        public Sprite epoch;

        public Sprite ForCategory(BuildingCategory category)
        {
            switch (category)
            {
                case BuildingCategory.Production: return production;
                case BuildingCategory.Commerce: return commerce;
                case BuildingCategory.Service: return service;
                case BuildingCategory.Storage: return storage;
                case BuildingCategory.Housing: return housing;
                case BuildingCategory.Wonder: return wonder;
                default: return monument;
            }
        }
    }
}
