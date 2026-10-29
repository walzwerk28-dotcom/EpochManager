using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Zeigt beim Erzeugen zufällig genau eines der direkten Kindobjekte (Varianten eines Gebäudes),
    /// damit Straßenzüge aus gleichen Gebäuden nicht eintönig wirken. Reine Optik; der Spielstand merkt
    /// sich die Variante, damit die Stadt nach dem Laden gleich aussieht.
    /// </summary>
    public class RandomVariant : MonoBehaviour
    {
        /// <summary>Index der gezeigten Variante (−1 ohne Kindobjekte).</summary>
        public int Current { get; private set; } = -1;

        private void Awake()
        {
            if (transform.childCount > 0) Show(Random.Range(0, transform.childCount));
        }

        public void Show(int index)
        {
            int count = transform.childCount;
            if (index < 0 || index >= count) return;
            Current = index;
            for (int i = 0; i < count; i++)
                transform.GetChild(i).gameObject.SetActive(i == index);
        }
    }
}
