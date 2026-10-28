using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Eine Ressource (Holz, Stahl, Strom ...). Auch Forschung und Kultur
    /// sind Ressourcen, damit alles über dasselbe System läuft.
    /// </summary>
    [CreateAssetMenu(menuName = "EpochCity/Resource", fileName = "Res_")]
    public class ResourceDefinition : ScriptableObject
    {
        public string displayName;
        public EpochDefinition introducedInEpoch;
        public Sprite icon;

        [Tooltip("Wenn aktiv, zählt diese Ressource als Geld: Ausgaben landen in der Stadtkasse, Kosten werden von ihr abgezogen.")]
        public bool isCurrency;
    }
}
