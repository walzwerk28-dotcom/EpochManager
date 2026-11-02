using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Speichert und lädt den Spielstand als JSON unter persistentDataPath/saves/. Laden leert die Stadt ohne
    /// Erstattung und baut sie neu auf: Zustand der Stadt, Straßen, Zonen, Gebäude, dann Zeit, Forschung,
    /// Bevölkerung. Inhalte, die es nicht mehr gibt, werden übersprungen und gemeldet.
    /// Darstellungen (z. B. die Kamera) hängen sich über Saving und Loaded an.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [SerializeField] private GameClock clock;
        [SerializeField] private ResearchSystem research;
        [SerializeField] private PopulationSimulation population;
        [SerializeField] private EconomySimulation economy;
        [Tooltip("Für die Brand-Häufigkeit im Spielstand (leer = vom selben Objekt)")]
        [SerializeField] private FireSimulation fire;
        public ContentRegistry registry;
        public const string QuickSaveSlot = "quicksave";
        public string quickSaveSlot = QuickSaveSlot;

        /// <summary>Spielstand, den die Spielszene nach dem Start lädt (vom Startmenü, „Fortsetzen“; null = keiner).</summary>
        public static string PendingLoad { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            PendingLoad = null;
            IsLoadPending = false;
        }

        /// <summary>Gibt es den Spielstand? Ohne Instanz, z. B. für das Startmenü.</summary>
        public static bool Exists(string slot) => File.Exists(Path.Combine(SaveFolder, slot + ".json"));

        [Header("Autosave (wie in Anno und Cities: Skylines)")]
        [Tooltip("Echte Minuten zwischen zwei automatischen Spielständen, nur solange die Spielzeit läuft (0 = aus)")]
        [Min(0f)] public float autosaveMinutes = 5f;
        [Tooltip("So viele Plätze autosave_1 … autosave_N werden reihum überschrieben (der älteste zuerst)")]
        [Min(1)] public int autosaveSlots = 3;
        public const string AutosavePrefix = "autosave_";

        /// <summary>Sekunden seit dem letzten automatischen Speichern (für Tests).</summary>
        public float AutosaveTimer { get; private set; }

        /// <summary>Eckdaten eines Spielstands für die Liste (Anzeige ohne Laden).</summary>
        public struct SaveInfo
        {
            public string slot;
            public DateTime writtenAt;
            public string epochName;
            public int population;
            public int totalMonths;
            public bool IsAutosave => slot.StartsWith(AutosavePrefix);
            public bool IsQuickSave => slot == QuickSaveSlot;
        }

        /// <summary>Alle lesbaren Spielstände, der neueste zuerst; beschädigte werden übersprungen.</summary>
        public static List<SaveInfo> ListSaves()
        {
            var list = new List<SaveInfo>();
            if (!Directory.Exists(SaveFolder)) return list;
            foreach (var path in Directory.GetFiles(SaveFolder, "*.json"))
            {
                try
                {
                    var data = JsonUtility.FromJson<SaveGame>(File.ReadAllText(path));
                    if (data == null) continue;
                    list.Add(new SaveInfo
                    {
                        slot = Path.GetFileNameWithoutExtension(path),
                        writtenAt = File.GetLastWriteTime(path),
                        epochName = data.epochName,
                        population = data.population,
                        totalMonths = data.totalMonths
                    });
                }
                catch (Exception)
                {
                    // beschädigt: nicht in der Liste
                }
            }
            list.Sort((a, b) => b.writtenAt.CompareTo(a.writtenAt));
            return list;
        }

        /// <summary>Platz des neuesten Spielstands (auch Autosave und Schnellspeicher), null = keiner.</summary>
        public static string NewestSlot()
        {
            var list = ListSaves();
            return list.Count > 0 ? list[0].slot : null;
        }

        /// <summary>Löscht einen Spielstand endgültig.</summary>
        public static bool Delete(string slot)
        {
            try
            {
                string path = Path.Combine(SaveFolder, slot + ".json");
                if (!File.Exists(path)) return false;
                File.Delete(path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        /// <summary>Speichert in den ältesten (oder ersten freien) Autosave-Platz.</summary>
        public bool Autosave()
        {
            AutosaveTimer = 0f;
            string target = null;
            var oldest = DateTime.MaxValue;
            for (int i = 1; i <= autosaveSlots; i++)
            {
                string slot = AutosavePrefix + i;
                if (!HasSave(slot)) { target = slot; break; }
                var time = File.GetLastWriteTime(GetPath(slot));
                if (time < oldest) { oldest = time; target = slot; }
            }
            return Save(target, "Automatisch gespeichert");
        }

        private void Update()
        {
            if (autosaveMinutes <= 0f || clock == null || clock.Speed == 0) return;
            AutosaveTimer += Time.unscaledDeltaTime;
            if (AutosaveTimer >= autosaveMinutes * 60f) Autosave();
        }

        /// <summary>Vor dem Schreiben: Darstellungen können eigene Werte eintragen.</summary>
        public event Action<SaveGame> Saving;

        /// <summary>Nach dem Laden: Darstellungen können eigene Werte übernehmen.</summary>
        public event Action<SaveGame> Loaded;

        /// <summary>Ergebnis des letzten Speicherns oder Ladens für die Anzeige, mit Zeitpunkt (unskalierte Zeit).</summary>
        /// <summary>Ergebnis von Speichern oder Laden als Meldung (Text, erfolgreich?), z. B. für Toasts.</summary>
        public event Action<string, bool> Reported;

        public string LastMessage { get; private set; } = "";
        public float LastMessageTime { get; private set; } = float.NegativeInfinity;

        public static string SaveFolder => Path.Combine(Application.persistentDataPath, "saves");

        public string GetPath(string slot) => Path.Combine(SaveFolder, slot + ".json");

        public bool HasSave(string slot) => File.Exists(GetPath(slot));

        public bool QuickSave() => Save(quickSaveSlot);

        public bool QuickLoad() => Load(quickSaveSlot);

        private readonly HashSet<string> missing = new HashSet<string>();

        private FireSimulation Fire => fire != null ? fire : fire = GetComponent<FireSimulation>();

        // Einen Frame nach dem Start, damit alle Systeme und Darstellungen ihr Start schon hinter sich haben
        private System.Collections.IEnumerator Start()
        {
            if (string.IsNullOrEmpty(PendingLoad)) yield break;
            string slot = PendingLoad;
            PendingLoad = null;
            IsLoadPending = true;
            yield return null;
            try { Load(slot); }
            finally { IsLoadPending = false; } // sonst bliebe der Ladebildschirm bei einem Fehler stehen
        }

        /// <summary>Wird ein angeforderter Spielstand gerade geladen (für den Ladebildschirm)?</summary>
        public static bool IsLoadPending { get; private set; }

        /// <summary>
        /// Macht aus einem eingegebenen Namen einen Platz (Dateiname): Zeichen, die Dateinamen nicht erlauben, werden
        /// zu „_“, höchstens 40 Zeichen; null, wenn nichts übrig bleibt oder der Name für Schnellspeicher und
        /// Autosave reserviert ist.
        /// </summary>
        public static string ToSlotName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var chars = name.Trim().ToCharArray();
            var invalid = Path.GetInvalidFileNameChars();
            for (int i = 0; i < chars.Length; i++)
                if (Array.IndexOf(invalid, chars[i]) >= 0) chars[i] = '_';
            string slot = new string(chars);
            if (slot.Length > 40) slot = slot.Substring(0, 40).TrimEnd();
            if (slot == QuickSaveSlot || slot.StartsWith(AutosavePrefix) || slot.Trim('.', '_').Length == 0) return null;
            return slot;
        }

        public bool Save(string slot) => Save(slot, "Gespeichert");

        /// <param name="message">Anfang der Meldung, dahinter das Spieldatum</param>
        private bool Save(string slot, string message)
        {
            try
            {
                var data = Capture();
                Saving?.Invoke(data);

                // Erst in eine Hilfsdatei schreiben, damit ein Abbruch den alten Stand nicht zerstört
                Directory.CreateDirectory(SaveFolder);
                string path = GetPath(slot), temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(data, true));
                if (File.Exists(path)) File.Delete(path);
                File.Move(temp, path);

                Report($"{message} ({DateText()})");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Report("Speichern fehlgeschlagen", false);
                return false;
            }
        }

        public bool Load(string slot)
        {
            if (!HasSave(slot))
            {
                Report("Kein Spielstand vorhanden", false);
                return false;
            }

            SaveGame data;
            try
            {
                data = JsonUtility.FromJson<SaveGame>(File.ReadAllText(GetPath(slot)));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Report("Spielstand ist beschädigt", false);
                return false;
            }
            if (data == null || data.version > SaveGame.CurrentVersion)
            {
                Report("Spielstand stammt aus einer neueren Version", false);
                return false;
            }

            missing.Clear();
            Apply(data);
            AutosaveTimer = 0f;
            Loaded?.Invoke(data);

            if (missing.Count > 0)
            {
                Debug.LogWarning("Beim Laden übersprungen (Inhalt fehlt): " + string.Join(", ", missing));
                Report($"Geladen, {missing.Count} unbekannte Inhalte übersprungen");
            }
            else Report($"Geladen ({DateText()})");
            return true;
        }

        private string DateText() => clock != null ? clock.DateString : "";

        private void Report(string message, bool success = true)
        {
            LastMessage = message;
            LastMessageTime = Time.unscaledTime;
            Reported?.Invoke(message, success);
        }

        // ---------------------------------------------------------------- Speichern

        private SaveGame Capture()
        {
            var data = new SaveGame
            {
                savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                epochName = city.currentEpoch != null ? city.currentEpoch.displayName : "",
                population = population != null ? population.TotalResidents : 0,
                mode = (int)city.Mode,
                fireFrequency = (int)(Fire != null ? Fire.frequency : FireFrequency.Normal),
                money = city.Money,
                epoch = ContentRegistry.IdOf(city.currentEpoch)
            };
            foreach (var tech in city.UnlockedTechnologies) data.technologies.Add(ContentRegistry.IdOf(tech));
            foreach (var entry in city.Stock.All)
                if (entry.Key != null) data.stock.Add(new AmountEntry(ContentRegistry.IdOf(entry.Key), entry.Value));

            if (clock != null)
            {
                data.totalMonths = clock.TotalMonths;
                data.monthProgress = clock.MonthProgress;
                data.speed = clock.Speed;
            }

            if (research != null)
            {
                data.currentResearch = ContentRegistry.IdOf(research.Current);
                foreach (var entry in research.Progress)
                    data.researchProgress.Add(new AmountEntry(ContentRegistry.IdOf(entry.Key), entry.Value));
            }

            if (population != null)
            {
                if (population.tiers != null)
                    foreach (var tier in population.tiers)
                        if (tier != null)
                            data.taxRates.Add(new AmountEntry(ContentRegistry.IdOf(tier), population.GetTaxRate(tier)));
                data.lastMonthTaxes = population.LastMonthTaxes;
            }
            if (economy != null)
            {
                data.lastMonthIncome = economy.LastMonthIncome;
                data.lastMonthUpkeep = economy.LastMonthUpkeep;
            }

            var grid = city.Grid;
            for (int x = 0; x < grid.Width; x++)
                for (int y = 0; y < grid.Height; y++)
                {
                    var c = new Vector2Int(x, y);
                    var road = grid.GetRoad(c);
                    if (road != null) data.roads.Add(new CellEntry(c, ContentRegistry.IdOf(road)));
                }
            foreach (var c in grid.ZonedCells)
                data.zones.Add(new CellEntry(c, ContentRegistry.IdOf(grid.GetZone(c))));
            foreach (var c in grid.DepositCells)
                data.deposits.Add(new CellEntry(c, ContentRegistry.IdOf(grid.GetDeposit(c))));
            foreach (var entry in grid.YoungDeposits)
                data.youngDeposits.Add(new YoungEntry(entry.Key, entry.Value));

            foreach (var b in city.Buildings)
            {
                var entry = new BuildingEntry
                {
                    definition = ContentRegistry.IdOf(b.Definition),
                    x = b.Origin.x,
                    y = b.Origin.y,
                    builtInEpoch = ContentRegistry.IdOf(b.BuiltInEpoch),
                    isProtected = b.IsProtected,
                    isPaused = b.IsPaused,
                    residents = b.Residents,
                    tier = ContentRegistry.IdOf(b.Tier),
                    tierMonths = b.TierMonths,
                    satisfaction = b.Satisfaction,
                    missingNeeds = b.MissingNeeds,
                    facing = b.Facing,
                    variant = GetVariant(b),
                    fire = (int)b.Fire,
                    fireTimer = b.FireTimer
                };
                foreach (var function in b.Definition.GetActiveFunctions(city.UnlockedTechnologies))
                    entry.progress.Add(function != null && b.Progress.TryGetValue(function, out float p) ? p : 0f);
                data.buildings.Add(entry);
            }
            return data;
        }

        // ---------------------------------------------------------------- Laden

        private void Apply(SaveGame data)
        {
            city.ClearForLoad();

            // Ab Version 5; ältere Stände: Freies Bauen mit normaler Brand-Häufigkeit
            bool hasSettings = data.version >= 5;
            city.Mode = hasSettings ? (GameMode)data.mode : GameMode.FreeBuild;
            if (Fire != null) Fire.frequency = hasSettings ? (FireFrequency)data.fireFrequency : FireFrequency.Normal;

            // Technologien vor den Gebäuden, damit die Reihenfolge der aktiven Funktionen stimmt
            var techs = new List<TechnologyDefinition>();
            foreach (var id in data.technologies)
                if (Resolve<TechnologyDefinition>(id, out var tech)) techs.Add(tech);
            Resolve<EpochDefinition>(data.epoch, out var epoch);
            city.RestoreState(data.money, epoch, techs);
            foreach (var entry in data.stock)
                if (Resolve<ResourceDefinition>(entry.id, out var resource)) city.Stock.Add(resource, entry.amount);

            // Vorkommen vor Straßen und Gebäuden; Stände vor Version 2 behalten die beim Start erzeugte Karte
            // (Straßen und Gebäude roden darunter den Wald)
            if (data.version >= 2)
            {
                var deposits = new List<(Vector2Int, DepositDefinition)>();
                foreach (var entry in data.deposits)
                    if (Resolve<DepositDefinition>(entry.id, out var deposit)) deposits.Add((entry.Cell, deposit));
                city.RestoreDeposits(deposits);
                // Jungwald ab Version 3; ältere Stände laden mit ganz ausgewachsenem Wald
                var young = new List<(Vector2Int, int)>();
                foreach (var entry in data.youngDeposits) young.Add((entry.Cell, entry.months));
                city.RestoreYoungDeposits(young);
            }

            var roads = new List<(Vector2Int, RoadDefinition)>();
            foreach (var entry in data.roads)
                if (Resolve<RoadDefinition>(entry.id, out var road)) roads.Add((entry.Cell, road));
            city.RestoreRoads(roads);

            var zones = new Dictionary<ZoneDefinition, List<Vector2Int>>();
            foreach (var entry in data.zones)
            {
                if (!Resolve<ZoneDefinition>(entry.id, out var zone)) continue;
                if (!zones.TryGetValue(zone, out var cells)) zones[zone] = cells = new List<Vector2Int>();
                cells.Add(entry.Cell);
            }
            foreach (var z in zones) city.SetZone(z.Key, z.Value);

            foreach (var entry in data.buildings)
            {
                if (!Resolve<BuildingDefinition>(entry.definition, out var def)) continue;
                Resolve<EpochDefinition>(entry.builtInEpoch, out var builtIn);
                var b = city.RestoreBuilding(def, new Vector2Int(entry.x, entry.y), builtIn, entry.facing);
                if (b == null)
                {
                    Debug.LogWarning($"Beim Laden übersprungen: {def.name} bei ({entry.x}, {entry.y}) – Fläche belegt");
                    continue;
                }
                b.IsProtected = entry.isProtected;
                b.IsPaused = entry.isPaused; // Anbindung (stillgelegte Lager) folgt mit RefreshConnections
                b.Fire = (FireState)entry.fire; // Brandschutz folgt mit ConnectionsChanged (FireSimulation)
                b.FireTimer = entry.fireTimer;
                b.Residents = entry.residents;
                if (!string.IsNullOrEmpty(entry.tier) && Resolve<PopulationTierDefinition>(entry.tier, out var tier))
                    b.Tier = tier;
                b.TierMonths = entry.tierMonths;
                b.Satisfaction = entry.satisfaction;
                b.MissingNeeds = entry.missingNeeds ?? "";
                var functions = def.GetActiveFunctions(city.UnlockedTechnologies);
                for (int i = 0; i < functions.Count && i < entry.progress.Count; i++)
                    if (functions[i] != null) b.Progress[functions[i]] = entry.progress[i];
                SetVariant(b, entry.variant);
            }
            city.RefreshConnections();

            if (clock != null) clock.Restore(data.totalMonths, data.monthProgress, data.speed);

            if (research != null)
            {
                var progress = new Dictionary<TechnologyDefinition, float>();
                foreach (var entry in data.researchProgress)
                    if (Resolve<TechnologyDefinition>(entry.id, out var tech)) progress[tech] = entry.amount;
                Resolve<TechnologyDefinition>(data.currentResearch, out var current);
                research.Restore(current, progress);
            }

            if (population != null)
            {
                foreach (var entry in data.taxRates)
                    if (Resolve<PopulationTierDefinition>(entry.id, out var tier)) population.SetTaxRate(tier, entry.amount);
                population.RestoreTotals(data.lastMonthTaxes);
            }
            if (economy != null) economy.Restore(data.lastMonthIncome, data.lastMonthUpkeep);
        }

        /// <summary>Löst einen gespeicherten Namen auf; fehlende Inhalte werden für die Meldung gesammelt.</summary>
        private bool Resolve<T>(string id, out T content) where T : ScriptableObject
        {
            content = null;
            if (string.IsNullOrEmpty(id)) return false;
            content = registry != null ? registry.Find<T>(id) : null;
            if (content == null) missing.Add(id);
            return content != null;
        }

        // Die Modellvariante ist reine Optik, wird aber mitgespeichert, damit die Stadt nach dem Laden gleich aussieht
        private static int GetVariant(BuildingInstance b)
        {
            var variant = b.View != null ? b.View.GetComponent<RandomVariant>() : null;
            return variant != null ? variant.Current : -1;
        }

        private static void SetVariant(BuildingInstance b, int index)
        {
            var variant = b.View != null ? b.View.GetComponent<RandomVariant>() : null;
            if (variant != null && index >= 0) variant.Show(index);
        }
    }
}
