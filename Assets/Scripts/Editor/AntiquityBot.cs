using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Antike-Durchlauf für das Balancing: Ein Bot spielt im Play-Modus eine frische Stadt im Zeitraffer
    /// (GameClock / EconomySimulation / ZoneGrowth.Advance, ResearchSystem.Tick) nach festen Regeln wie ein
    /// ordentlicher Spieler: Straßenblöcke um die Mitte, Zonen nach Bedarf, Betriebe wenn eine Ware fehlt,
    /// Dienste wo sie fehlen, Forschung nach fester Reihenfolge. Er misst je Monat Geld, Einwohner je Stufe,
    /// Zufriedenheit, Arbeitskräfte, Lager und Engpässe (Statustexte), schreibt eine CSV-Datei nach
    /// persistentDataPath/balance/ und gibt eine Zusammenfassung mit den Meilensteinen zurück.
    /// Die Stadt im Play-Modus ist danach verbraucht (Play-Modus neu starten).
    /// </summary>
    public static class AntiquityBot
    {
        [MenuItem("EpochCity/Antike-Durchlauf (Play-Modus)")]
        private static void RunFromMenu()
        {
            if (!EditorApplication.isPlaying) { Debug.LogWarning("Antike-Durchlauf: erst den Play-Modus starten."); return; }
            Debug.Log(Run(1234, 240, 5000f));
        }

        /// <summary>Spielt months Spielmonate auf der Karte mit diesem Startwert; gibt die Zusammenfassung zurück.</summary>
        public static string Run(int seed, int months, float startMoney)
        {
            var session = new Session();
            string error = session.Setup(seed, startMoney);
            return error ?? session.Play(months);
        }

        private class Session
        {
            // ---------------------------------------------------------------- Bezüge
            private CityController city;
            private GameClock clock;
            private EconomySimulation economy;
            private PopulationSimulation population;
            private ZoneGrowth growth;
            private ZoneDemand demand;
            private FireSimulation fire;
            private ResearchSystem research;
            private EpochProgression progression;
            private BuildingPlacer placer;
            private RoadDefinition road;
            private int seed;

            private const int BlockWidth = 9, BlockHeight = 7; // Abstand der Straßen: Blöcke 8 × 6 Felder
            private const float Reserve = 150f;                // so viel Geld bleibt immer übrig
            private const float DecisionSeconds = 10f;         // drei Entscheidungen pro Monat

            private readonly List<RectInt> openBlocks = new List<RectInt>();
            private readonly Dictionary<string, int> lastBuilt = new Dictionary<string, int>();
            // Monat, in dem zuletzt ein Bau an diesem Material scheiterte (Bretter haben keinen laufenden Verbrauch)
            private readonly Dictionary<ResourceDefinition, int> materialShort = new Dictionary<ResourceDefinition, int>();
            private readonly List<string> actions = new List<string>();
            private readonly Dictionary<string, int> builtCount = new Dictionary<string, int>();
            private readonly Dictionary<string, int> lowReasons = new Dictionary<string, int>();
            private readonly Dictionary<string, int> issues = new Dictionary<string, int>();
            private readonly Dictionary<string, int> milestones = new Dictionary<string, int>();
            private readonly StringBuilder csv = new StringBuilder();
            private List<ResourceDefinition> goods;
            private float minMoney = float.MaxValue;
            private int Month => clock.TotalMonths;

            private static readonly string[] ResearchOrder =
            {
                "Schrift", "Kulte", "Bewässerung", "Handwerk", "Philosophie", "Tauschhandel", "Lagerwesen",
                "Vermessung", "Wasserbau", "Münzprägung", "Mathematik", "Pflug"
            };

            public string Setup(int seed, float startMoney)
            {
                this.seed = seed;
                city = Object.FindAnyObjectByType<CityController>();
                clock = Object.FindAnyObjectByType<GameClock>();
                economy = Object.FindAnyObjectByType<EconomySimulation>();
                population = Object.FindAnyObjectByType<PopulationSimulation>();
                growth = Object.FindAnyObjectByType<ZoneGrowth>();
                demand = Object.FindAnyObjectByType<ZoneDemand>();
                fire = Object.FindAnyObjectByType<FireSimulation>();
                if (fire != null) fire.Reseed(seed);
                research = Object.FindAnyObjectByType<ResearchSystem>();
                progression = Object.FindAnyObjectByType<EpochProgression>();
                placer = Object.FindAnyObjectByType<BuildingPlacer>();
                if (city == null || city.Grid == null || clock == null || economy == null || population == null)
                    return PlayFromMainMenu.Enabled
                        ? "Antike-Durchlauf: Szene nicht gefunden – Play startet im Hauptmenü; dort „Freies Bauen“ " +
                          "starten oder EpochCity → Play startet im Hauptmenü ausschalten."
                        : "Antike-Durchlauf: Szene nicht gefunden – im Play-Modus starten.";
                if (city.Buildings.Count > 0) return "Antike-Durchlauf: Die Stadt ist nicht leer – Play-Modus neu starten.";
                road = placer.road;
                clock.SetSpeed(0); // die Zeit läuft nur über Advance

                // Karte mit festem Startwert
                if (city.map != null)
                {
                    var map = Object.Instantiate(city.map);
                    map.seed = seed;
                    city.RestoreDeposits(new List<(Vector2Int, DepositDefinition)>());
                    MapGenerator.Generate(city.Grid, map);
                    city.OnDepositsAltered();
                    Object.Destroy(map);
                }
                city.AddMoney(startMoney - city.Money);

                goods = new List<ResourceDefinition>();
                foreach (var def in placer.hotbar)
                    if (def != null)
                        foreach (var f in def.baseFunctions)
                            foreach (var o in f.outputs)
                                if (o.resource != null && !o.resource.isCurrency && !goods.Contains(o.resource)) goods.Add(o.resource);
                city.TechnologyUnlocked += tech => Milestone(tech.displayName);
                return null;
            }

            // ---------------------------------------------------------------- Ablauf

            public string Play(int months)
            {
                WriteHeader();
                float sinceDecision = DecisionSeconds;
                int recordedMonth = Month;
                double started = Time.realtimeSinceStartupAsDouble;
                while (Month < months)
                {
                    clock.Advance(1f);
                    economy.Advance(1f);
                    research.Tick();
                    if (demand != null) { demand.Recalculate(1f); TraceDemand(); }
                    growth.Advance(1f);
                    if (fire != null) fire.Advance(1f);

                    sinceDecision += 1f;
                    if (sinceDecision >= DecisionSeconds)
                    {
                        sinceDecision = 0f;
                        Decide();
                    }
                    if (Month != recordedMonth)
                    {
                        recordedMonth = Month;
                        Record();
                    }
                }
                return Summary(Time.realtimeSinceStartupAsDouble - started);
            }

            // ---------------------------------------------------------------- Entscheidungen

            /// <summary>Höchstens eine Bau-Entscheidung je Aufruf, nach Vorrang.</summary>
            private void Decide()
            {
                ChooseResearch();
                if (progression != null && progression.CanAdvance) Milestone("Mittelalter möglich");

                if (Count("Lager") == 0) { Build("Lager", Center); return; }
                if (EnsureHousing()) return;
                if (EnsureUpgrades()) return;
                if (EnsureGoods()) return;
                if (EnsureServices()) return;
                if (EnsureFireStations()) return;
                if (EnsureResearchBuildings()) return;
                if (EnsureStorage()) return;
                if (EnsureForesters()) return;
                EnsureCommerce();
            }

            private void ChooseResearch()
            {
                if (research == null || research.Current != null) return;
                foreach (var name in ResearchOrder)
                {
                    var tech = research.technologies.FirstOrDefault(t => t != null && t.displayName == name);
                    if (tech != null && research.CanResearch(tech)) { research.Select(tech); return; }
                }
            }

            /// <summary>Wohnzonen: neue Blöcke, wenn freie Zonenfelder fehlen und Nachfrage oder Arbeitskräftemangel besteht.</summary>
            private bool EnsureHousing()
            {
                var tiers = population.tiers;
                // Zu wenig Arbeitskräfte: immer ein neuer Block
                if (economy.GetWorkforceFactor(tiers[0]) < 1f && FreeZoneCells(placer.residentialZone) < 6)
                    return ZoneBlock(placer.residentialZone);

                // Gelehrte ab Philosophie und 30 Handwerkern, Handwerker ab Schrift und 40 Bauern
                var top = placer.topResidentialZone;
                if (top != null && tiers.Length > 2 && city.IsUnlocked(top.requiredTechnology) &&
                    population.CountResidents(tiers[1]) >= 30)
                {
                    bool needed = ZoneCells(top) == 0 || economy.GetWorkforceFactor(tiers[2]) < 1f ||
                                  Demand(top) > 0.3f && Supplied(top);
                    if (needed && FreeZoneCells(top) < 6) return ZoneBlock(top);
                }
                var upper = placer.upperResidentialZone;
                if (upper != null && city.IsUnlocked(upper.requiredTechnology) && population.CountResidents(tiers[0]) >= 40)
                {
                    bool needed = ZoneCells(upper) == 0 || (tiers.Length > 1 && economy.GetWorkforceFactor(tiers[1]) < 1f) ||
                                  Demand(upper) > 0.3f && Supplied(upper);
                    if (needed && FreeZoneCells(upper) < 6) return ZoneBlock(upper);
                }

                // Bauern über den Bedarf hinaus nur, wenn die vorhandenen versorgt sind und nicht zu viele (Steuern)
                bool surplusOk = economy.GetAvailableWorkers(tiers[0]) < economy.GetRequiredWorkers(tiers[0]) * 1.25f + 40;
                if (Demand(placer.residentialZone) > 0.3f && surplusOk && Supplied(placer.residentialZone) &&
                    FreeZoneCells(placer.residentialZone) < 6)
                    return ZoneBlock(placer.residentialZone);
                return false;
            }

            /// <summary>Wie ein Spieler erst erweitern, wenn mindestens 80 % der Häuser einer Zone nichts fehlt.</summary>
            private bool Supplied(ZoneDefinition zone)
            {
                var houses = Houses().Where(h => city.Grid.GetZone(h.Origin) == zone && h.Residents > 0).ToList();
                return houses.Count == 0 || houses.Count(h => string.IsNullOrEmpty(h.MissingNeeds)) >= houses.Count * 0.8f;
            }

            /// <summary>Waren: fehlt eine Ware (Lager knapp und Bedarf über der möglichen Produktion), ein Betrieb mehr.</summary>
            private bool EnsureGoods()
            {
                foreach (var good in goods)
                {
                    var producer = ProducerOf(good);
                    if (producer == null || !city.IsAvailable(producer) || Recently(producer.displayName, 1)) continue;
                    // Fehlen den vorhandenen Betrieben Arbeitskräfte, hilft ein weiterer nicht (dafür sorgt EnsureHousing)
                    if (city.Buildings.Any(b => b.Definition == producer && b.WorkforceFactor < 0.9f)) continue;
                    bool material = good.isBuildingMaterial;
                    float need = MonthlyNeed(good);
                    if (need <= 0f && !material) continue;
                    float prod = MonthlyProduction(good);
                    float stock = city.Stock.Get(good);
                    bool waiting = city.Buildings.Any(b => b.Status.Contains("wartet: " + good.displayName));
                    // Wartet ein vorhandener Betrieb auf seine Eingänge, hilft ein weiterer nicht (dafür sorgt dessen Kette)
                    bool producerWaiting = city.Buildings.Any(b => b.Definition == producer && b.Status.Contains("wartet"));
                    bool shortage = material
                        ? (stock < 20f && prod < Mathf.Max(6f, need * 1.1f)) ||
                          (materialShort.TryGetValue(good, out int m) && Month - m < 3 && !producerWaiting)
                        : need > prod * 0.95f && (stock < need || waiting);
                    if (!shortage) continue;
                    if (Build(producer.displayName, Center)) return true;
                }
                return false;
            }

            /// <summary>Dienste (Markt, Tempel, Brunnen …): wo Häuser sie vermissen, ein Anbieter in ihrer Mitte.</summary>
            private bool EnsureServices()
            {
                var houses = Houses().ToList();
                foreach (var tier in population.tiers)
                    foreach (var need in tier.needs)
                    {
                        if (need == null || need.kind != NeedKind.Service || !city.IsUnlocked(need.requiredTechnology)) continue;
                        var missing = houses.Where(h => h.Tier == tier && h.Residents > 0 &&
                                                        h.MissingNeeds.Contains(need.displayName)).ToList();
                        if (missing.Count == 0) continue;
                        if (BuildProvider(need, missing)) return true;
                    }
                return false;
            }

            /// <summary>Aufstieg: Häuser, denen ein Dienst für den Aufstieg fehlt („braucht Bildung“), bekommen ihn.</summary>
            private bool EnsureUpgrades()
            {
                var houses = Houses().ToList();
                foreach (var tier in population.tiers)
                {
                    if (tier.upgradeRequires == null) continue;
                    foreach (var need in tier.upgradeRequires)
                    {
                        if (need == null || !city.IsUnlocked(need.requiredTechnology)) continue;
                        var missing = houses.Where(h => h.Tier == tier && h.UpgradeIssue == "braucht " + need.displayName).ToList();
                        if (missing.Count == 0) continue;
                        if (BuildProvider(need, missing)) return true;
                    }
                }
                return false;
            }

            private bool BuildProvider(NeedDefinition need, List<BuildingInstance> missing)
            {
                var provider = need.providers.FirstOrDefault(p => p != null && city.IsAvailable(p));
                if (provider == null || Recently(provider.displayName + need.displayName, 2)) return false;
                // Größte Gruppe unversorgter Häuser innerhalb der halben Reichweite; der Schnitt über alle
                // läge oft zwischen zwei Vierteln an einer schon versorgten Stelle
                float radius = need.range * 0.5f;
                var group = missing
                    .Select(seed => missing.Where(h => Vector2Int.Distance(h.Origin, seed.Origin) <= radius).ToList())
                    .OrderByDescending(g => g.Count).First();
                var center = new Vector2Int(Mathf.RoundToInt((float)group.Average(h => h.Origin.x)),
                                            Mathf.RoundToInt((float)group.Average(h => h.Origin.y)));
                MarkRecent(provider.displayName + need.displayName);
                return Build(provider.displayName, center);
            }

            /// <summary>Feuerwachen: ab 12 Gebäuden mit unter 30 % Brandschutz eine in der Mitte der größten ungeschützten Gruppe.</summary>
            private bool EnsureFireStations()
            {
                if (fire == null) return false;
                var station = placer.hotbar.FirstOrDefault(d => d != null && d.fireProtectionRange > 0f && city.IsAvailable(d));
                if (station == null || Recently(station.displayName, 3)) return false;
                var unprotected = city.Buildings.Where(b => b.Definition.fireRisk > 0f && fire.GetProtection(b) < 0.3f).ToList();
                if (unprotected.Count < 12) return false;
                float radius = station.fireProtectionRange * 0.5f;
                var group = unprotected
                    .Select(seed => unprotected.Where(h => Vector2Int.Distance(h.Origin, seed.Origin) <= radius).ToList())
                    .OrderByDescending(g => g.Count).First();
                if (group.Count < 6) return false;
                var center = new Vector2Int(Mathf.RoundToInt((float)group.Average(h => h.Origin.x)),
                                            Mathf.RoundToInt((float)group.Average(h => h.Origin.y)));
                MarkRecent(station.displayName);
                return Build(station.displayName, center);
            }

            /// <summary>Forschung: früh eine Schreibschule, nach der Schrift zwei Bibliotheken, nach Philosophie eine dritte.</summary>
            private bool EnsureResearchBuildings()
            {
                int residents = population.TotalResidents;
                if (Count("Schreibschule") == 0 && residents >= 10) return Build("Schreibschule", Center);
                int libraries = Count("Bibliothek");
                int wanted = !IsAvailable("Bibliothek") ? 0 : IsUnlocked("Philosophie") ? 3 : 2;
                if (libraries < wanted && residents >= 30 + libraries * 40 && !Recently("Bibliothek", 3))
                    return Build("Bibliothek", Center);
                return false;
            }

            /// <summary>Lager: bei „Lager voll“ oder schlecht angebundenen Betrieben ein weiteres in deren Nähe.</summary>
            private bool EnsureStorage()
            {
                // Höchstens 2 Lager plus eines je 60 Einwohner
                if (Recently("Lager", 3) || Count("Lager") >= 2 + population.TotalResidents / 60) return false;
                // Lager voll nur bei Waren, die gebraucht werden (Überproduktion braucht kein Lager)
                if (city.Buildings.Any(b => b.Status.StartsWith("Lager voll") &&
                                            goods.Any(g => b.Status.Contains(g.displayName) && MonthlyNeed(g) > 0f)))
                    return Build("Lager", Center);
                var far = city.Buildings.Where(b => b.Definition.category != BuildingCategory.Storage &&
                                                    b.ConnectionIssue == "zu weit vom Lager").ToList();
                if (far.Count >= 3)
                {
                    var c = new Vector2Int(Mathf.RoundToInt((float)far.Average(b => b.Origin.x)),
                                           Mathf.RoundToInt((float)far.Average(b => b.Origin.y)));
                    // Nur, wenn dort noch kein Lager steht
                    if (city.Buildings.Any(b => b.Definition.category == BuildingCategory.Storage &&
                                                (b.Origin - c).sqrMagnitude < 64)) return false;
                    return Build("Lager", c);
                }
                return false;
            }

            /// <summary>Förster neben Holzfällern, deren Wald unter 60 % liegt.</summary>
            private bool EnsureForesters()
            {
                if (!IsAvailable("Förster") || Recently("Förster", 2)) return false;
                foreach (var cutter in city.Buildings.Where(b => b.Definition.fellsDeposit && b.DepositFactor < 0.6f))
                {
                    bool tended = city.Buildings.Any(f => f.Definition.plantsDeposit != null &&
                                                          (f.Origin - cutter.Origin).sqrMagnitude <= 36);
                    if (!tended) return Build("Förster", cutter.Origin);
                }
                return false;
            }

            private void EnsureCommerce()
            {
                var zone = placer.commercialZone;
                if (zone != null && Demand(zone) > 0.5f && FreeZoneCells(zone) < 4) ZoneBlock(zone);
            }

            // ---------------------------------------------------------------- Bauen

            private Vector2Int Center => new Vector2Int(city.Grid.Width / 2, city.Grid.Height / 2);

            /// <summary>Baut ein Gebäude in der Nähe von near: an vorhandener Straße, sonst mit neuem Block oder Stichstraße.</summary>
            private bool Build(string name, Vector2Int near)
            {
                var def = Def(name);
                if (def == null || !city.IsAvailable(def)) return false;
                if (!city.CanAfford(def))
                {
                    NoteMissingMaterial(def);
                    return false;
                }
                if (city.Money - def.buildCost < Reserve) return false;
                var deposit = def.requiredDeposit;
                bool ok = deposit != null ? BuildAtDeposit(def, deposit)
                    : def.plantsDeposit != null ? BuildBeside(def, near, 5)
                    : BuildNear(def, near);
                if (ok) MarkRecent(name);
                return ok;
            }

            /// <summary>Merkt sich jedes Baumaterial, das für diesen Bau fehlt.</summary>
            private void NoteMissingMaterial(BuildingDefinition def)
            {
                if (def.materialCosts == null) return;
                foreach (var cost in def.materialCosts)
                    if (cost.resource != null && city.Stock.Get(cost.resource) < cost.amount)
                        materialShort[cost.resource] = Month;
            }

            private bool BuildNear(BuildingDefinition def, Vector2Int near)
            {
                if (OpenBlocksCount == 0) OpenBlock(near);
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    var spot = FindSpot(def, near, 30); // nächster freier Platz, notfalls am Stadtrand
                    if (spot.HasValue && Place(def, spot.Value)) return true;
                    if (!OpenBlock(near)) break;
                }
                return false;
            }

            private int OpenBlocksCount => openBlocks.Count;

            /// <summary>Förster: freie Fläche dicht bei near, notfalls mit Stichstraße.</summary>
            private bool BuildBeside(BuildingDefinition def, Vector2Int near, int radius)
            {
                var candidates = new List<Vector2Int>();
                for (int x = near.x - radius; x <= near.x + radius; x++)
                    for (int y = near.y - radius; y <= near.y + radius; y++)
                    {
                        var o = new Vector2Int(x, y);
                        if (city.Grid.CanPlace(def.size, o) && !Zoned(o, def.size) && !OnRoadLine(o, def.size)) candidates.Add(o);
                    }
                foreach (var o in candidates.OrderBy(o => (o - near).sqrMagnitude).Take(8))
                {
                    if (!city.CanPlace(def, o) && !ConnectRoad(o, def.size)) continue;
                    if (Place(def, o)) return true;
                }
                return false;
            }

            /// <summary>Freie, nicht zonierte Grundfläche an einer Straße, möglichst nah an near.</summary>
            private Vector2Int? FindSpot(BuildingDefinition def, Vector2Int near, int radius)
            {
                Vector2Int? best = null;
                float bestDistance = float.MaxValue;
                for (int x = near.x - radius; x <= near.x + radius; x++)
                    for (int y = near.y - radius; y <= near.y + radius; y++)
                    {
                        var o = new Vector2Int(x, y);
                        float d = (o - near).sqrMagnitude;
                        if (d >= bestDistance || !city.CanPlace(def, o) || Zoned(o, def.size) || OnRoadLine(o, def.size)) continue;
                        best = o;
                        bestDistance = d;
                    }
                return best;
            }

            /// <summary>Holzfäller, Steinbruch: Platz mit dem meisten Vorkommen nahe der Stadt, notfalls mit Stichstraße.</summary>
            private bool BuildAtDeposit(BuildingDefinition def, DepositDefinition deposit)
            {
                var grid = city.Grid;
                var candidates = new List<(Vector2Int o, float score)>();
                for (int x = 1; x < grid.Width - def.size.x; x++)
                    for (int y = 1; y < grid.Height - def.size.y; y++)
                    {
                        var o = new Vector2Int(x, y);
                        if (!grid.CanPlace(def.size, o) || Zoned(o, def.size) || OnRoadLine(o, def.size)) continue;
                        int count = grid.CountDeposit(deposit, o, def.size, def.depositRange);
                        if (count == 0) continue;
                        // Nicht in den Umkreis eines anderen Betriebs desselben Vorkommens
                        if (city.Buildings.Any(b => b.Definition.requiredDeposit == deposit &&
                                                    (b.Origin - o).sqrMagnitude < 25)) continue;
                        float score = Mathf.Min(count, def.depositCellsForFull) * 10f - (o - Center).magnitude;
                        candidates.Add((o, score));
                    }
                foreach (var (o, _) in candidates.OrderByDescending(c => c.score).Take(12))
                {
                    if (!city.CanPlace(def, o) && !ConnectRoad(o, def.size)) continue;
                    if (Place(def, o)) return true;
                }
                return false;
            }

            private bool Place(BuildingDefinition def, Vector2Int origin)
            {
                if (!city.TryPlace(def, origin, out _)) return false;
                actions.Add($"M{Month}: {def.displayName} ({origin.x},{origin.y})");
                builtCount.TryGetValue(def.displayName, out int n);
                builtCount[def.displayName] = n + 1;
                return true;
            }

            /// <summary>Stichstraße von einer Seite der Grundfläche zur nächsten Straße (L-Form).</summary>
            private bool ConnectRoad(Vector2Int origin, Vector2Int size)
            {
                var grid = city.Grid;
                var roads = new List<Vector2Int>();
                for (int x = 0; x < grid.Width; x++)
                    for (int y = 0; y < grid.Height; y++)
                        if (grid.HasRoad(new Vector2Int(x, y))) roads.Add(new Vector2Int(x, y));
                if (roads.Count == 0) return false;

                var sides = new List<Vector2Int>();
                for (int x = 0; x < size.x; x++) { sides.Add(origin + new Vector2Int(x, -1)); sides.Add(origin + new Vector2Int(x, size.y)); }
                for (int y = 0; y < size.y; y++) { sides.Add(origin + new Vector2Int(-1, y)); sides.Add(origin + new Vector2Int(size.x, y)); }
                var best = sides.Where(grid.InBounds)
                    .Select(s => (s, r: roads.OrderBy(r => (r - s).sqrMagnitude).First()))
                    .OrderBy(p => (p.r - p.s).sqrMagnitude).Take(3);
                foreach (var (s, r) in best)
                {
                    if ((r - s).sqrMagnitude > 400) continue; // höchstens etwa 20 Felder
                    if (city.TryBuildRoad(road, GridMap.LPath(r, s)) || city.TryBuildRoad(road, GridMap.LPath(s, r)))
                        return true;
                }
                return false;
            }

            /// <summary>Straßen um den nächsten freien Block nahe near; gibt den Block zurück oder null.</summary>
            private bool OpenBlock(Vector2Int near) => OpenBlockRect(near).HasValue;

            private RectInt? OpenBlockRect(Vector2Int near)
            {
                var grid = city.Grid;
                RectInt? best = null;
                float bestDistance = float.MaxValue;
                // Straßenraster: senkrechte Straßen bei Mitte ± k·9, waagerechte bei Mitte ± k·7
                for (int i = -4; i <= 3; i++)
                    for (int j = -5; j <= 4; j++)
                    {
                        int x0 = Center.x + i * BlockWidth, y0 = Center.y + j * BlockHeight;
                        var rect = new RectInt(x0, y0, BlockWidth, BlockHeight); // Straßen auf x0, y0, x0+9, y0+7
                        if (x0 < 1 || y0 < 1 || x0 + BlockWidth >= grid.Width - 1 || y0 + BlockHeight >= grid.Height - 1) continue;
                        if (openBlocks.Contains(rect)) continue;
                        float d = (rect.center - (Vector2)near).sqrMagnitude;
                        // Blöcke nahe der Stadtmitte bevorzugen, damit die Stadt zusammenhängt
                        d += (rect.center - (Vector2)Center).sqrMagnitude * 0.5f;
                        if (d < bestDistance) { bestDistance = d; best = rect; }
                    }
                if (!best.HasValue) return null;
                var b = best.Value;
                var corners = new[]
                {
                    new Vector2Int(b.xMin, b.yMin), new Vector2Int(b.xMax, b.yMin),
                    new Vector2Int(b.xMax, b.yMax), new Vector2Int(b.xMin, b.yMax)
                };
                float cost = 0f;
                for (int k = 0; k < 4; k++)
                    foreach (var c in GridMap.LPath(corners[k], corners[(k + 1) % 4]))
                        if (!grid.HasRoad(c)) cost += road.costPerCell;
                if (city.Money - cost < Reserve) return null;
                int built = 0;
                for (int k = 0; k < 4; k++)
                    if (city.TryBuildRoad(road, GridMap.LPath(corners[k], corners[(k + 1) % 4]))) built++;
                openBlocks.Add(b);
                if (built == 0) return null;
                actions.Add($"M{Month}: Block ({b.xMin},{b.yMin}) mit {built}/4 Straßen");
                return b;
            }

            /// <summary>Öffnet einen Block und weist seine freien Felder der Zone zu.</summary>
            private bool ZoneBlock(ZoneDefinition zone)
            {
                if (Recently("Zone " + zone.displayName, 1)) return false;
                var rect = OpenBlockRect(ZoneCenter(zone));
                if (!rect.HasValue) return false;
                var r = rect.Value;
                int set = city.SetZone(zone, GridMap.RectCells(new Vector2Int(r.xMin + 1, r.yMin + 1),
                                                               new Vector2Int(r.xMax - 1, r.yMax - 1)));
                MarkRecent("Zone " + zone.displayName);
                actions.Add($"M{Month}: {zone.displayName} {set} Felder");
                return set > 0;
            }

            private Vector2Int ZoneCenter(ZoneDefinition zone)
            {
                var cells = city.Grid.ZonedCells.Where(c => city.Grid.GetZone(c) == zone).ToList();
                if (cells.Count == 0) return Center;
                return new Vector2Int(Mathf.RoundToInt((float)cells.Average(c => c.x)), Mathf.RoundToInt((float)cells.Average(c => c.y)));
            }

            // ---------------------------------------------------------------- Hilfen

            private BuildingDefinition Def(string name) => placer.hotbar.FirstOrDefault(d => d != null && d.displayName == name);
            private bool IsAvailable(string name) => city.IsAvailable(Def(name));
            private bool IsUnlocked(string tech) => city.UnlockedTechnologies.Any(t => t.displayName == tech);
            private int Count(string name) => city.Buildings.Count(b => b.Definition.displayName == name);
            private IEnumerable<BuildingInstance> Houses() => city.Buildings.Where(b => b.Definition.populationTier != null);
            private float Demand(ZoneDefinition zone) => zone != null && demand != null ? demand.GetDemand(zone) : 0f;
            private int ZoneCells(ZoneDefinition zone) => city.Grid.ZonedCells.Count(c => city.Grid.GetZone(c) == zone);
            /// <summary>Freie Zonenfelder an einer Straße; Restfelder ohne Straße können nie bebaut werden.</summary>
            private int FreeZoneCells(ZoneDefinition zone) =>
                city.Grid.ZonedCells.Count(c => city.Grid.GetZone(c) == zone && city.Grid.GetAt(c) == null &&
                                                city.Grid.TouchesRoad(c, Vector2Int.one));

            /// <summary>Liegt die Grundfläche auf einer Linie des Straßenrasters? Dort bleibt Platz für spätere Blöcke.</summary>
            private bool OnRoadLine(Vector2Int origin, Vector2Int size)
            {
                for (int x = origin.x; x < origin.x + size.x; x++)
                    if (Mod(x - Center.x, BlockWidth) == 0) return true;
                for (int y = origin.y; y < origin.y + size.y; y++)
                    if (Mod(y - Center.y, BlockHeight) == 0) return true;
                return false;
            }

            private static int Mod(int a, int m) => ((a % m) + m) % m;

            private bool Zoned(Vector2Int origin, Vector2Int size)
            {
                for (int x = 0; x < size.x; x++)
                    for (int y = 0; y < size.y; y++)
                        if (city.Grid.GetZone(origin + new Vector2Int(x, y)) != null) return true;
                return false;
            }

            private bool Recently(string key, int months) => lastBuilt.TryGetValue(key, out int m) && Month - m < months;
            private void MarkRecent(string key) => lastBuilt[key] = Month;

            private BuildingDefinition ProducerOf(ResourceDefinition good) =>
                placer.hotbar.FirstOrDefault(d => d != null && city.IsEpochReached(d.epoch) &&
                    d.baseFunctions.Any(f => f.outputs.Any(o => o.resource == good)));

            /// <summary>Monatlicher Bedarf: Bedürfnisse der Bewohner plus Eingänge laufender Betriebe.</summary>
            private float MonthlyNeed(ResourceDefinition good)
            {
                float need = 0f;
                foreach (var tier in population.tiers)
                {
                    int residents = population.CountResidents(tier);
                    foreach (var n in tier.needs)
                        if (n != null && n.kind == NeedKind.Goods && n.resource == good && city.IsUnlocked(n.requiredTechnology))
                            need += residents * n.amountPerResidentPerMonth;
                }
                foreach (var b in city.Buildings)
                    foreach (var f in b.Definition.GetActiveFunctions(city.UnlockedTechnologies))
                    {
                        if (f.inputs != null)
                            foreach (var i in f.inputs)
                                if (i.resource == good) need += i.amount * clock.secondsPerMonth / f.cycleSeconds;
                        if (f.IsSale)
                            foreach (var s in f.sales)
                                if (s.resource == good) need += s.amount * clock.secondsPerMonth / f.cycleSeconds;
                    }
                return need;
            }

            /// <summary>Mögliche Monatsproduktion bei voller Besetzung (Anbindung × Vorkommen, ohne Arbeitskräfte).</summary>
            private float MonthlyProduction(ResourceDefinition good)
            {
                float prod = 0f;
                foreach (var b in city.Buildings)
                    foreach (var f in b.Definition.GetActiveFunctions(city.UnlockedTechnologies))
                        foreach (var o in f.outputs)
                            if (o.resource == good)
                                prod += o.amount * city.GetMultiplier(b) * clock.secondsPerMonth / f.cycleSeconds *
                                        b.Connection * b.DepositFactor;
                return prod;
            }

            private void Milestone(string name)
            {
                if (!milestones.ContainsKey(name)) milestones[name] = Month;
            }

            // ---------------------------------------------------------------- Messen

            private void WriteHeader()
            {
                csv.Append("Monat;Geld;Einnahmen;Steuern;Unterhalt;Einwohner");
                foreach (var t in population.tiers) csv.Append($";{t.displayName};Zufr. {t.displayName};Arbeit {t.displayName}");
                csv.Append(";Gebäude;Technologien");
                foreach (var g in goods) csv.Append(";" + g.displayName);
                csv.AppendLine();
            }

            private void Record()
            {
                minMoney = Mathf.Min(minMoney, city.Money);
                var tiers = population.tiers;
                if (population.CountResidents(tiers[0]) > 0) Milestone("erste Bauern");
                if (tiers.Length > 1 && population.CountResidents(tiers[1]) > 0) Milestone("erste Handwerker");
                if (tiers.Length > 2 && population.CountResidents(tiers[2]) > 0) Milestone("erste Gelehrte");
                if (population.TotalResidents >= 100) Milestone("100 Einwohner");
                if (population.TotalResidents >= 300) Milestone("300 Einwohner");
                if (city.Money < 0f) Milestone("Kasse erstmals leer");

                csv.Append($"{Month};{city.Money:0};{economy.LastMonthIncome:0};{population.LastMonthTaxes:0};{economy.LastMonthUpkeep:0};{population.TotalResidents}");
                foreach (var t in tiers)
                {
                    var houses = Houses().Where(h => h.Tier == t && h.Residents > 0).ToList();
                    float sat = houses.Count > 0 ? houses.Average(h => h.Satisfaction) : 0f;
                    csv.Append($";{population.CountResidents(t)};{sat * 100f:0};{economy.GetWorkforceFactor(t) * 100f:0}");
                }
                csv.Append($";{city.Buildings.Count};{city.UnlockedTechnologies.Count}");
                foreach (var g in goods) csv.Append($";{city.Stock.Get(g):0}");
                csv.AppendLine();

                // Engpässe: Gründe aus den Statustexten und fehlende Bedürfnisse, je Gebäude und Monat
                foreach (var b in city.Buildings)
                {
                    foreach (var reason in Reasons(b.Status)) Count(issues, reason);
                    if (!string.IsNullOrEmpty(b.MissingNeeds))
                        foreach (var n in b.MissingNeeds.Split(',')) Count(issues, "fehlt " + n.Trim());
                    if (b.UpgradeIssue == "Zufriedenheit unter 75 %") Count(issues, "Aufstieg: Zufriedenheit unter 75 %");
                }
            }

            private static IEnumerable<string> Reasons(string status)
            {
                if (string.IsNullOrEmpty(status) || status == "läuft" || status == "Wald gepflegt") yield break;
                if (status.StartsWith("läuft ("))
                {
                    int comma = status.IndexOf(", ");
                    if (comma < 0) yield break;
                    foreach (var r in status.Substring(comma + 2).TrimEnd(')').Split(new[] { ", " }, System.StringSplitOptions.None))
                        yield return r;
                }
                else foreach (var r in status.Split(new[] { ", " }, System.StringSplitOptions.None)) yield return r;
            }

            private static void Count(Dictionary<string, int> map, string key)
            {
                map.TryGetValue(key, out int n);
                map[key] = n + 1;
            }

            // Verlauf der Nachfrage je Zone (ab der ersten Bebauung): Sekunden nahe 0 und Einbrüche von über 30 % auf unter 5 %
            private readonly Dictionary<ZoneDefinition, (int seconds, int low, int drops, bool high)> demandTrace =
                new Dictionary<ZoneDefinition, (int, int, int, bool)>();

            private void TraceDemand()
            {
                foreach (var zone in demand.zones)
                {
                    if (zone == null || ZoneCells(zone) == 0) continue;
                    demandTrace.TryGetValue(zone, out var t);
                    float d = demand.GetDemand(zone);
                    t.seconds++;
                    if (d < 0.05f)
                    {
                        t.low++;
                        // Welche Faktoren bremsen, solange die Nachfrage fast 0 ist (mehrere gleichzeitig möglich)
                        string issue = demand.GetIssue(zone) ?? "";
                        foreach (var reason in new[] { "genug Gewerbe für die Kunden", "Waren fehlen", "zu wenig",
                                                       "keine Kunden", "Häuser nicht voll belegt" })
                            if (issue.Contains(reason)) Count(lowReasons, $"{zone.ShortName}: {reason}");
                    }
                    if (d > 0.3f) t.high = true;
                    else if (d < 0.05f && t.high) { t.drops++; t.high = false; }
                    demandTrace[zone] = t;
                }
            }

            private string Summary(double realSeconds)
            {
                string dir = Path.Combine(Application.persistentDataPath, "balance");
                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, $"antike_{seed}.csv");
                File.WriteAllText(file, csv.ToString(), Encoding.UTF8);

                var sb = new StringBuilder();
                sb.AppendLine($"Antike-Durchlauf Startwert {seed}: {Month} Monate ({Month / 2f:0} min bei Tempo 1) in {realSeconds:0.0} s");
                sb.AppendLine("Meilensteine (Monat / Minuten bei Tempo 1):");
                foreach (var m in milestones.OrderBy(m => m.Value))
                    sb.AppendLine($"  {m.Key}: M{m.Value} / {m.Value / 2f:0.#} min");
                var tiers = population.tiers;
                sb.AppendLine($"Ende: Geld {city.Money:0} (Tiefstand {minMoney:0}), Einwohner {population.TotalResidents} (" +
                              string.Join(", ", tiers.Select(t => $"{t.displayName} {population.CountResidents(t)}")) +
                              $"), Gebäude {city.Buildings.Count}, Technologien {city.UnlockedTechnologies.Count}, " +
                              $"Bilanz {economy.LastMonthIncome + population.LastMonthTaxes - economy.LastMonthUpkeep:0}/Monat");
                sb.AppendLine("Gebaut: " + string.Join(", ", builtCount.OrderByDescending(b => b.Value).Select(b => $"{b.Key} {b.Value}")));
                sb.AppendLine("Häufigste Engpässe (Gebäude-Monate): " +
                              string.Join(", ", issues.OrderByDescending(i => i.Value).Take(12).Select(i => $"{i.Key} {i.Value}")));
                if (fire != null)
                    sb.AppendLine($"Feuer: {fire.IgnitionCount} Brände, {fire.ExtinguishedCount} gelöscht, {fire.DestroyedCount} Zonengebäude abgebrannt, " +
                                  $"am Ende {city.Buildings.Count(b => b.Fire == FireState.Damaged)} beschädigt, {city.Buildings.Count(b => b.Fire == FireState.Burning)} brennend");
                sb.AppendLine("Nachfrage (Anteil der Zeit unter 5 %, Einbrüche von über 30 %): " + string.Join(", ",
                    demandTrace.Select(t => $"{t.Key.ShortName} {100f * t.Value.low / Mathf.Max(1, t.Value.seconds):0} % / {t.Value.drops}")));
                if (lowReasons.Count > 0)
                    sb.AppendLine("Gründe bei Nachfrage unter 5 % (Spielsekunden): " + string.Join(", ",
                        lowReasons.OrderByDescending(r => r.Value).Select(r => $"{r.Key} {r.Value}")));
                sb.AppendLine("Lager am Ende: " + string.Join(", ", goods.Select(g => $"{g.displayName} {city.Stock.Get(g):0}")));
                sb.AppendLine($"CSV: {file}");
                sb.AppendLine("Erste Aktionen: " + string.Join(" | ", actions.Take(25)));
                return sb.ToString();
            }
        }
    }
}
