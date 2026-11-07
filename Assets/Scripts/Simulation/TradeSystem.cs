using System;
using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    public enum TradeMode { Off, Sell, Buy }

    /// <summary>
    /// Handel am Hafen (wie das Kontor in Anno): je Ware ein Modus (Aus, Verkaufen, Kaufen) mit einer Grenze. Einmal
    /// im Spielmonat, nach dem Verbrauch der Häuser, handeln alle Häfen in Betrieb zusammen höchstens
    /// <see cref="Capacity"/> Einheiten. Verkaufen: der Vorrat über der Grenze und der Rücklage für die Häuser geht
    /// zum Verkaufspreis weg. Kaufen: der Vorrat wird bis zur Grenze zum Kaufpreis aufgefüllt, soweit Geld, Lagerplatz
    /// und Kapazität reichen. Das Geld läuft über die Stadtkasse; die Monatsbilanz führt der Handel getrennt
    /// (LastMonthTradeIncome, LastMonthTradeExpense). Ohne Hafen in Betrieb gibt es keinen Handel.
    /// </summary>
    public class TradeSystem : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [SerializeField] private EconomySimulation economy;
        [Tooltip("Hängt den Handel an das Monatsende (leer = vom selben Objekt)")]
        [SerializeField] private PopulationSimulation population;
        [Tooltip("Verzeichnis, aus dem die handelbaren Waren kommen (leer = das des SaveSystems auf demselben Objekt)")]
        [SerializeField] private ContentRegistry registry;

        [Header("Preise")]
        [Tooltip("Verkaufspreis = Grundpreis × dieser Faktor")]
        [Min(0f)] public float sellFactor = 0.75f;
        [Tooltip("Kaufpreis = Grundpreis × dieser Faktor")]
        [Min(0f)] public float buyFactor = 1.25f;

        /// <summary>Einstellung einer Ware: Modus und Grenze (Verkaufen: Vorrat, der bleibt; Kaufen: Zielvorrat).</summary>
        public struct Setting
        {
            public TradeMode mode;
            public int limit;
        }

        /// <summary>Gehandelte Menge einer Ware im letzten Monat: positiv verkauft, negativ gekauft.</summary>
        public struct TradeEntry
        {
            public ResourceDefinition resource;
            public int amount;
            public float money;
        }

        private readonly Dictionary<ResourceDefinition, Setting> settings = new Dictionary<ResourceDefinition, Setting>();
        private readonly List<TradeEntry> lastTrades = new List<TradeEntry>();

        public float LastMonthTradeIncome { get; private set; }
        public float LastMonthTradeExpense { get; private set; }
        /// <summary>Beitrag des Handels zur Monatsbilanz: Einnahmen − Ausgaben.</summary>
        public float LastMonthTradeNet => LastMonthTradeIncome - LastMonthTradeExpense;
        /// <summary>Im letzten Monat genutzte Kapazität (Einheiten).</summary>
        public int LastMonthTradeUsed { get; private set; }
        public IReadOnlyList<TradeEntry> LastTrades => lastTrades;

        /// <summary>Nach jedem Handelsmonat, in dem etwas gehandelt wurde.</summary>
        public event Action Traded;

        // ---------------------------------------------------------------- Einstellungen

        /// <summary>Handelbar: Preis über 0, kein Geld, mit Lagergrenze.</summary>
        public static bool IsTradable(ResourceDefinition resource) =>
            resource != null && resource.tradePrice > 0f && CityController.IsLimited(resource);

        public Setting GetSetting(ResourceDefinition resource) =>
            resource != null && settings.TryGetValue(resource, out var s) ? s : default;

        /// <summary>Setzt Modus und Grenze (auf ganze Zahlen ≥ 0 gebracht); unhandelbare Waren bleiben Aus.</summary>
        public void SetSetting(ResourceDefinition resource, TradeMode mode, int limit)
        {
            if (resource == null) return;
            if (!IsTradable(resource)) mode = TradeMode.Off;
            settings[resource] = new Setting { mode = mode, limit = Mathf.Max(0, limit) };
        }

        public float SellPrice(ResourceDefinition resource) => resource.tradePrice * sellFactor;
        public float BuyPrice(ResourceDefinition resource) => resource.tradePrice * buyFactor;

        /// <summary>Einheiten je Monat: Summe der Häfen, die nicht stillgelegt, brennend, beschädigt oder im Bau sind.</summary>
        public int Capacity
        {
            get
            {
                int sum = 0;
                if (city == null) return 0;
                foreach (var b in city.Buildings)
                    if (b.Definition.tradeCapacity > 0 && !b.IsOutOfService) sum += b.Definition.tradeCapacity;
                return sum;
            }
        }

        // ---------------------------------------------------------------- Handel

        private void OnEnable()
        {
            if (population == null) population = GetComponent<PopulationSimulation>();
            if (population != null) population.MonthSimulated += Trade;
        }

        private void OnDisable()
        {
            if (population != null) population.MonthSimulated -= Trade;
        }

        private ContentRegistry Registry
        {
            get
            {
                if (registry == null)
                {
                    var save = GetComponent<SaveSystem>();
                    if (save != null) registry = save.registry;
                }
                return registry;
            }
        }

        /// <summary>
        /// Der Handel eines Monats (läuft am Monatsende von selbst, auch für Tests): die Waren der Reihe nach im
        /// Inhaltsverzeichnis, die Kapazität wird nacheinander verbraucht.
        /// </summary>
        public void Trade()
        {
            LastMonthTradeIncome = 0f;
            LastMonthTradeExpense = 0f;
            LastMonthTradeUsed = 0;
            lastTrades.Clear();
            if (city == null || Registry == null || Registry.entries == null) return;

            int remaining = Capacity;
            if (remaining <= 0) return;
            if (economy != null) economy.InvalidateReserves();

            foreach (var entry in Registry.entries)
            {
                if (remaining <= 0) break;
                var res = entry as ResourceDefinition;
                if (res == null || !IsTradable(res)) continue;
                var setting = GetSetting(res);
                if (setting.mode == TradeMode.Off) continue;

                float stock = city.Stock.Get(res);
                int amount;
                if (setting.mode == TradeMode.Sell)
                {
                    float reserve = economy != null ? economy.GetReserve(res) : 0f;
                    amount = Mathf.FloorToInt(Mathf.Min(stock - Mathf.Max(setting.limit, reserve), remaining) + 1e-4f);
                    if (amount <= 0) continue;
                    float income = amount * SellPrice(res);
                    city.Stock.Add(res, -amount);
                    city.AddMoney(income);
                    LastMonthTradeIncome += income;
                    lastTrades.Add(new TradeEntry { resource = res, amount = amount, money = income });
                }
                else
                {
                    float price = BuyPrice(res);
                    if (city.Money <= 0f) continue;
                    float room = city.StorageCapacity - stock;
                    float want = Mathf.Min(setting.limit - stock, remaining, room, city.Money / price);
                    amount = Mathf.FloorToInt(want + 1e-4f);
                    if (amount <= 0) continue;
                    float cost = amount * price;
                    city.Stock.Add(res, amount);
                    city.AddMoney(-cost);
                    LastMonthTradeExpense += cost;
                    lastTrades.Add(new TradeEntry { resource = res, amount = -amount, money = cost });
                }
                remaining -= amount;
                LastMonthTradeUsed += amount;
            }
            if (lastTrades.Count > 0) Traded?.Invoke();
        }

        // ---------------------------------------------------------------- Spielstand

        /// <summary>Alle Einstellungen, die nicht „Aus“ sind (für den Spielstand).</summary>
        public IEnumerable<KeyValuePair<ResourceDefinition, Setting>> Settings
        {
            get
            {
                foreach (var s in settings)
                    if (s.Value.mode != TradeMode.Off) yield return s;
            }
        }

        /// <summary>Einstellungen und Bilanz aus einem Spielstand übernehmen (leer = alles Aus).</summary>
        public void Restore(IEnumerable<(ResourceDefinition resource, TradeMode mode, int limit)> loaded,
            float lastIncome, float lastExpense)
        {
            settings.Clear();
            lastTrades.Clear();
            LastMonthTradeUsed = 0;
            if (loaded != null)
                foreach (var (resource, mode, limit) in loaded) SetSetting(resource, mode, limit);
            LastMonthTradeIncome = lastIncome;
            LastMonthTradeExpense = lastExpense;
        }
    }
}
