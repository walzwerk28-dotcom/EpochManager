using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Gemeinsames Aussehen aller HUDs: Farben, Eckenradius, Schatten und Schriftgrößen. Die HUDs bauen ihre
    /// Oberfläche über UiFactory auf und lesen die Werte von hier; ohne zugewiesenes Asset gelten die Standardwerte.
    /// Buttons zeigen ihre Grundfarbe beim Überfahren voll und sonst leicht abgedunkelt (siehe UiFactory).
    /// </summary>
    [CreateAssetMenu(menuName = "EpochCity/UI-Farbschema", fileName = "UiTheme")]
    public class UiTheme : ScriptableObject
    {
        [Header("Flächen")]
        public Color panel = new Color(0.075f, 0.085f, 0.11f, 0.94f);
        [Tooltip("Aufklappende Reihen und Nebenflächen")]
        public Color panelSoft = new Color(0.10f, 0.115f, 0.15f, 0.92f);
        [Tooltip("Hintergrund von Balken und Leisten")]
        public Color track = new Color(1f, 1f, 1f, 0.10f);

        [Header("Buttons")]
        public Color button = new Color(0.17f, 0.20f, 0.26f, 1f);
        [Tooltip("Ausgewählt, aktiv, laufendes Projekt")]
        public Color accent = new Color(0.27f, 0.60f, 0.43f, 1f);
        [Tooltip("Erledigt, z. B. erforschte Technologien")]
        public Color done = new Color(0.16f, 0.32f, 0.24f, 1f);
        [Tooltip("Abreißen, Zone aufheben")]
        public Color danger = new Color(0.70f, 0.29f, 0.25f, 1f);
        [Tooltip("Gesperrt, nicht verfügbar")]
        public Color locked = new Color(0.11f, 0.125f, 0.16f, 1f);

        [Header("Schrift")]
        public Color text = new Color(0.94f, 0.95f, 0.97f, 1f);
        [Tooltip("Hinweise, Nebeninfos")]
        public Color textMuted = new Color(0.66f, 0.70f, 0.77f, 1f);
        [Tooltip("Überschriften, warmer Ton passend zur Antike")]
        public Color heading = new Color(0.93f, 0.76f, 0.42f, 1f);
        [Tooltip("Meldungen über Erfolg, z. B. „Gespeichert“")]
        public Color positive = new Color(0.62f, 0.88f, 0.54f, 1f);
        public float titleSize = 26f;
        public float bodySize = 20f;
        public float smallSize = 17f;

        [Header("Form")]
        [Range(0, 24)] public int cornerRadius = 10;
        public Color shadow = new Color(0f, 0f, 0f, 0.45f);
        public Vector2 shadowOffset = new Vector2(0f, -4f);
        [Tooltip("Button-Helligkeit ohne Maus darüber (1 = Grundfarbe)")]
        [Range(0.5f, 1f)] public float idleBrightness = 0.86f;
        [Range(0.3f, 1f)] public float pressedBrightness = 0.70f;

        private static UiTheme fallback;

        /// <summary>Zuletzt von einem HUD übergebenes Farbschema; UiFactory nutzt es für Form und Schatten.</summary>
        public static UiTheme Current { get; private set; }

        /// <summary>Das zugewiesene Schema oder die Standardwerte; merkt es sich als Current.</summary>
        public static UiTheme Resolve(UiTheme assigned)
        {
            if (assigned != null) Current = assigned;
            if (Current == null)
            {
                if (fallback == null) fallback = CreateInstance<UiTheme>();
                Current = fallback;
            }
            return Current;
        }

        /// <summary>Hex-Code für TextMeshPro-Farbtags, z. B. &lt;color=#9fe08a&gt;.</summary>
        public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGBA(c);
    }
}
