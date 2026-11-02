using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Zeigt den Baufortschritt eines Großbaus wie in Anno: Die Varianten des Modells (RandomVariant) sind Baustufen in
    /// fester Reihenfolge, Variante i = i fertige Bauphasen (0 = Baustelle, letzte = fertig). Reine Optik, liest nur
    /// BuildingInstance.Stage; nach dem Laden stimmt die Stufe damit von selbst.
    /// </summary>
    [RequireComponent(typeof(RandomVariant))]
    public class ConstructionStages : MonoBehaviour
    {
        private CityController city;
        private RandomVariant variant;
        private BuildingInstance building;

        private void Start()
        {
            city = FindAnyObjectByType<CityController>();
            variant = GetComponent<RandomVariant>();
        }

        private void Update()
        {
            if (building == null && !FindBuilding()) return;
            int stage = Mathf.Clamp(building.Stage, 0, transform.childCount - 1);
            if (stage != variant.Current) variant.Show(stage);
        }

        private bool FindBuilding()
        {
            if (city == null) return false;
            foreach (var b in city.Buildings)
                if (b.View == gameObject)
                {
                    building = b;
                    return true;
                }
            return false;
        }
    }
}
