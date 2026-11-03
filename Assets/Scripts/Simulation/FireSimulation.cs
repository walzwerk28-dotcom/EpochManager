using System;
using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>Wie oft es brennt (einstellbar, wie die Ereignis-Häufigkeit in der GDD).</summary>
    public enum FireFrequency { Off, Rare, Normal, Frequent }

    /// <summary>
    /// Feuer (entschieden 2026-10-31, Variante a, wie in Cities: Skylines): Einmal im Spielmonat kann jedes Gebäude
    /// Feuer fangen, mit Grundrate × Häufigkeit × BuildingDefinition.fireRisk × (1 − Brandschutz). Brandschutz bieten
    /// Feuerwachen (fireProtectionRange) über das Straßennetz mit Abfallkurve. Ein Brand dauert burnSeconds (Warnphase);
    /// im Brandschutz wird er nach extinguishSeconds / Schutz gelöscht und das Gebäude ist kurz beschädigt. Brennt er
    /// aus, ist ein gewachsenes Zonengebäude zerstört (das Feld bleibt Zone, ZoneGrowth baut in der aktuellen Epoche
    /// neu), alles andere und Zonengebäude unter Denkmalschutz sind damagedMonths lang beschädigt und danach kostenlos
    /// instand gesetzt. Brennende und beschädigte Gebäude arbeiten nicht und bieten keine Dienste
    /// (BuildingInstance.IsOutOfService). Reine Zahlen; die Optik hängt sich an die Ereignisse.
    /// </summary>
    public class FireSimulation : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [SerializeField] private GameClock clock;

        public FireFrequency frequency = FireFrequency.Normal;
        [Tooltip("Brandwahrscheinlichkeit je Gebäude und Spielmonat bei Häufigkeit „normal“, Risiko 1 und ohne Brandschutz")]
        [Range(0f, 0.05f)] public float baseChancePerMonth = 0.0015f;
        [Tooltip("Spielsekunden, die ein Brand dauert, bis er ausgebrannt ist (Warnphase)")]
        [Min(1f)] public float burnSeconds = 30f;
        [Tooltip("Spielsekunden bis zum Löschen bei vollem Brandschutz (bei Schutz s: extinguishSeconds / s)")]
        [Min(0.1f)] public float extinguishSeconds = 6f;
        [Tooltip("Spielmonate, die ein ausgebranntes, nicht zerstörtes Gebäude beschädigt bleibt")]
        [Min(1)] public int damagedMonths = 3;
        [Tooltip("Spielmonate, die ein gelöschtes Gebäude beschädigt bleibt")]
        [Min(1)] public int extinguishedDamageMonths = 1;
        [Tooltip("Startwert des Zufalls (0 = zufällig); gleicher Startwert = gleiche Brände")]
        public int seed;

        /// <summary>Ein Gebäude hat Feuer gefangen.</summary>
        public event Action<BuildingInstance> Ignited;
        /// <summary>Ein Brand wurde gelöscht (das Gebäude ist kurz beschädigt).</summary>
        public event Action<BuildingInstance> Extinguished;
        /// <summary>Ein Brand ist ausgebrannt und hat das Gebäude beschädigt.</summary>
        public event Action<BuildingInstance> Damaged;
        /// <summary>Ein Zonengebäude ist abgebrannt und entfernt (Definition, Ursprung, Drehung).</summary>
        public event Action<BuildingDefinition, Vector2Int, float> Destroyed;
        /// <summary>Ein beschädigtes Gebäude ist wieder instand gesetzt.</summary>
        public event Action<BuildingInstance> Repaired;

        /// <summary>Zähler seit dem Start (für Messungen).</summary>
        public int IgnitionCount { get; private set; }
        public int DestroyedCount { get; private set; }
        public int ExtinguishedCount { get; private set; }

        private static readonly float[] FrequencyFactor = { 0f, 0.5f, 1f, 2f };

        private System.Random random;
        private readonly Dictionary<Vector2Int, float> protection = new Dictionary<Vector2Int, float>();
        private readonly List<BuildingInstance> scratch = new List<BuildingInstance>();
        private bool protectionDirty = true;

        private void Awake()
        {
            if (GameSettings.Active != null) frequency = GameSettings.Active.fireFrequency;
            random = seed != 0 ? new System.Random(seed) : new System.Random();
        }

        private void OnEnable()
        {
            if (clock != null) clock.MonthPassed += OnMonthPassed;
            if (city != null) city.ConnectionsChanged += MarkDirty;
        }

        private void OnDisable()
        {
            if (clock != null) clock.MonthPassed -= OnMonthPassed;
            if (city != null) city.ConnectionsChanged -= MarkDirty;
        }

        private void Update()
        {
            if (clock != null) Advance(Time.deltaTime * clock.Speed);
        }

        /// <summary>Zufall neu setzen (Tests, Antike-Durchlauf).</summary>
        public void Reseed(int value) => random = new System.Random(value);

        private void MarkDirty() => protectionDirty = true;

        // ---------------------------------------------------------------- Brandschutz

        /// <summary>Brandschutz eines Gebäudes (0..1): bester Wert über die Straßenfelder an seiner Grundfläche.</summary>
        public float GetProtection(BuildingInstance building)
        {
            RefreshProtection();
            float best = 0f;
            foreach (var c in city.Grid.GetAdjacentCells(building))
                if (protection.TryGetValue(c, out float p)) best = Mathf.Max(best, p);
            return best;
        }

        /// <summary>Brandschutz an einem Straßenfeld (für Infozeile und Vorschau).</summary>
        public float GetProtectionAt(Vector2Int cell)
        {
            RefreshProtection();
            return protection.TryGetValue(cell, out float p) ? p : 0f;
        }

        private void RefreshProtection()
        {
            if (!protectionDirty) return;
            protectionDirty = false;
            protection.Clear();
            foreach (var station in city.Buildings)
            {
                float range = station.Definition.fireProtectionRange;
                if (range <= 0f || station.IsOutOfService) continue;
                var distances = ConnectionMap.ShortestDistances(city.Grid, city.Grid.GetAdjacentCells(station));
                foreach (var entry in distances)
                {
                    if (entry.Value > range) continue;
                    float p = station.Definition.fireProtectionByDistance.Evaluate(entry.Value / range);
                    if (!protection.TryGetValue(entry.Key, out float old) || p > old) protection[entry.Key] = p;
                }
            }
        }

        // ---------------------------------------------------------------- Ablauf

        /// <summary>Brände voranbringen (Spielsekunden); auch für Zeitraffer per Code.</summary>
        public void Advance(float seconds)
        {
            if (city == null || seconds <= 0f) return;
            scratch.Clear();
            foreach (var b in city.Buildings)
                if (b.Fire == FireState.Burning) scratch.Add(b);

            foreach (var b in scratch)
            {
                b.FireTimer -= seconds;
                float p = GetProtection(b);
                bool extinguished = p > 0f && burnSeconds - b.FireTimer >= extinguishSeconds / p;
                if (extinguished)
                {
                    SetDamaged(b, extinguishedDamageMonths);
                    ExtinguishedCount++;
                    Extinguished?.Invoke(b);
                }
                else if (b.FireTimer <= 0f) BurnOut(b);
            }
        }

        private void BurnOut(BuildingInstance b)
        {
            // Variante a: nur gewachsene Zonengebäude ohne Denkmalschutz brennen ganz ab
            if (b.Definition.zone != null && !b.IsProtected)
            {
                var def = b.Definition;
                var origin = b.Origin;
                float facing = b.Facing;
                b.Fire = FireState.None;
                if (city.TryRemove(b))
                {
                    DestroyedCount++;
                    Destroyed?.Invoke(def, origin, facing);
                    return;
                }
            }
            SetDamaged(b, damagedMonths);
            Damaged?.Invoke(b);
        }

        private void SetDamaged(BuildingInstance b, int months)
        {
            bool stationChanged = b.Definition.fireProtectionRange > 0f;
            b.Fire = FireState.Damaged;
            b.FireTimer = months;
            if (stationChanged) protectionDirty = true;
        }

        private void OnMonthPassed()
        {
            if (city == null) return;

            // Instandsetzung beschädigter Gebäude
            scratch.Clear();
            foreach (var b in city.Buildings)
                if (b.Fire == FireState.Damaged) scratch.Add(b);
            foreach (var b in scratch)
            {
                b.FireTimer -= 1f;
                if (b.FireTimer > 0f) continue;
                b.Fire = FireState.None;
                b.FireTimer = 0f;
                if (b.Definition.fireProtectionRange > 0f) protectionDirty = true;
                Repaired?.Invoke(b);
            }

            // Neue Brände
            float factor = FrequencyFactor[(int)frequency] * baseChancePerMonth;
            if (factor <= 0f) return;
            scratch.Clear();
            foreach (var b in city.Buildings)
            {
                if (b.Fire != FireState.None || b.Definition.fireRisk <= 0f || b.UnderConstruction) continue; // Baustellen brennen nicht
                float chance = factor * b.Definition.fireRisk * (1f - GetProtection(b));
                if (random.NextDouble() < chance) scratch.Add(b);
            }
            foreach (var b in scratch) Ignite(b);
        }

        /// <summary>Ein Gebäude in Brand setzen (auch für Tests).</summary>
        public void Ignite(BuildingInstance b)
        {
            if (b == null || b.Fire == FireState.Burning) return;
            bool stationChanged = b.Definition.fireProtectionRange > 0f;
            b.Fire = FireState.Burning;
            b.FireTimer = burnSeconds;
            if (stationChanged) protectionDirty = true;
            IgnitionCount++;
            Ignited?.Invoke(b);
        }
    }
}
