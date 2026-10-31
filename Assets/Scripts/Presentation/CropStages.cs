using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Zeigt das Wachstum eines Felds wie in Anno: Die Varianten des Modells (RandomVariant) sind Stufen in fester
    /// Reihenfolge (Stoppeln, gesät, grün, reif). Gezählt werden die abgeschlossenen Produktionszyklen des Gebäudes;
    /// cyclesPerSeason Zyklen ergeben einen Durchlauf. Reine Optik, liest nur den Fortschritt der Simulation: Steht das
    /// Feld (keine Arbeitskräfte, Lager voll), bleibt auch die Stufe stehen. Der Zähler wird nicht gespeichert; nach
    /// dem Laden setzt er bei der gespeicherten Variante an.
    /// </summary>
    [RequireComponent(typeof(RandomVariant))]
    public class CropStages : MonoBehaviour
    {
        [Tooltip("Produktionszyklen von der Ernte bis zur nächsten Reife (Feld: 6 × 5 s = ein Spielmonat bei voller Leistung)")]
        [Min(1)] public int cyclesPerSeason = 6;
        [Tooltip("Ab welchem Anteil des Durchlaufs die Stufe mit diesem Index gilt (je Kindobjekt eine)")]
        public float[] stageStarts = { 0f, 0.15f, 0.4f, 0.7f };

        private CityController city;
        private RandomVariant variant;
        private BuildingInstance building;
        private BuildingFunction function;
        private float lastProgress;
        private int cycles;

        private void Start()
        {
            city = FindAnyObjectByType<CityController>();
            variant = GetComponent<RandomVariant>();
        }

        private void Update()
        {
            if (building == null && !FindBuilding()) return;
            if (!building.Progress.TryGetValue(function, out float progress)) progress = 0f;

            // Fortschritt springt nach einem Zyklus zurück (der Überschuss geht in den nächsten über)
            if (progress < lastProgress - 0.001f) cycles = (cycles + 1) % cyclesPerSeason;
            lastProgress = progress;

            float growth = (cycles + Mathf.Clamp01(progress / function.cycleSeconds)) / cyclesPerSeason;
            int stage = StageAt(growth);
            if (stage != variant.Current) variant.Show(stage);
        }

        private bool FindBuilding()
        {
            if (city == null) return false;
            foreach (var b in city.Buildings)
            {
                if (b.View != gameObject) continue;
                if (b.Definition.baseFunctions == null || b.Definition.baseFunctions.Length == 0) return false;
                building = b;
                function = b.Definition.baseFunctions[0];
                // Beim Ansetzen der gezeigten Stufe folgen (zufällig beim Bau, gespeichert beim Laden)
                int current = Mathf.Clamp(variant.Current, 0, stageStarts.Length - 1);
                cycles = Mathf.Clamp(Mathf.CeilToInt(stageStarts[current] * cyclesPerSeason), 0, cyclesPerSeason - 1);
                building.Progress.TryGetValue(function, out lastProgress);
                return true;
            }
            return false;
        }

        private int StageAt(float growth)
        {
            int stage = 0;
            int count = Mathf.Min(stageStarts.Length, transform.childCount);
            for (int i = 0; i < count; i++)
                if (growth >= stageStarts[i]) stage = i;
            return stage;
        }
    }
}
