using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Zeigt beim Erzeugen zufällig genau eines der direkten Kindobjekte (Varianten eines Gebäudes),
    /// damit Straßenzüge aus gleichen Gebäuden nicht eintönig wirken. Reine Optik.
    /// </summary>
    public class RandomVariant : MonoBehaviour
    {
        private void Awake()
        {
            int count = transform.childCount;
            if (count == 0) return;
            int pick = Random.Range(0, count);
            for (int i = 0; i < count; i++)
                transform.GetChild(i).gameObject.SetActive(i == pick);
        }
    }
}
