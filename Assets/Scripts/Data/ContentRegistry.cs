using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Verzeichnis aller Inhalte, auf die ein Spielstand verweisen kann (Gebäude, Straßen, Zonen, Epochen,
    /// Technologien, Ressourcen, Bevölkerungsstufen). Spielstände speichern nur den Asset-Namen; zur Laufzeit
    /// gibt es keine AssetDatabase, deshalb wird hier nachgeschlagen. Asset-Namen müssen eindeutig sein.
    /// </summary>
    [CreateAssetMenu(menuName = "EpochCity/Inhaltsverzeichnis", fileName = "ContentRegistry")]
    public class ContentRegistry : ScriptableObject
    {
        [Tooltip("Alle Inhalte, auf die ein Spielstand verweisen kann")]
        public ScriptableObject[] entries;

        private Dictionary<string, ScriptableObject> byName;

        private void OnEnable() => byName = null;
        private void OnValidate() => byName = null;

        /// <summary>Name, unter dem ein Inhalt im Spielstand steht (leer für null).</summary>
        public static string IdOf(ScriptableObject content) => content != null ? content.name : "";

        /// <summary>Inhalt zu einem gespeicherten Namen; null, wenn er fehlt oder einen anderen Typ hat.</summary>
        public T Find<T>(string id) where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (byName == null) BuildIndex();
            return byName.TryGetValue(id, out var content) ? content as T : null;
        }

        private void BuildIndex()
        {
            byName = new Dictionary<string, ScriptableObject>();
            if (entries == null) return;
            foreach (var e in entries)
            {
                if (e == null) continue;
                if (byName.ContainsKey(e.name))
                    Debug.LogWarning($"Inhaltsverzeichnis: Name doppelt vergeben: {e.name}");
                else
                    byName.Add(e.name, e);
            }
        }
    }
}
