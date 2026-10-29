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
        public ContentRegistry registry;
        public string quickSaveSlot = "quicksave";

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

        public bool Save(string slot)
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

                Report($"Gespeichert ({DateText()})");
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

            foreach (var b in city.Buildings)
            {
                var entry = new BuildingEntry
                {
                    definition = ContentRegistry.IdOf(b.Definition),
                    x = b.Origin.x,
                    y = b.Origin.y,
                    builtInEpoch = ContentRegistry.IdOf(b.BuiltInEpoch),
                    isProtected = b.IsProtected,
                    residents = b.Residents,
                    satisfaction = b.Satisfaction,
                    missingNeeds = b.MissingNeeds,
                    facing = b.Facing,
                    variant = GetVariant(b)
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

            // Technologien vor den Gebäuden, damit die Reihenfolge der aktiven Funktionen stimmt
            var techs = new List<TechnologyDefinition>();
            foreach (var id in data.technologies)
                if (Resolve<TechnologyDefinition>(id, out var tech)) techs.Add(tech);
            Resolve<EpochDefinition>(data.epoch, out var epoch);
            city.RestoreState(data.money, epoch, techs);
            foreach (var entry in data.stock)
                if (Resolve<ResourceDefinition>(entry.id, out var resource)) city.Stock.Add(resource, entry.amount);

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
                b.Residents = entry.residents;
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
