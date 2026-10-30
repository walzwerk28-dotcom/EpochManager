using System.IO;
using UnityEditor;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Legt einen kleinen Testaufbau als Assets an: Epoche Antike, Münzen, Weizen und Forschung,
    /// Feld, Markt und Bibliothek sowie die Technologien Bewässerung (Feld x1,5), Schrift und
    /// Münzprägung (setzt Schrift voraus, Markt x1,25). Dazu das Mittelalter (2 der 3 Technologien
    /// und eine Bibliothek nötig) mit der Mühle als erstem Gebäude der neuen Epoche, die Straße Trampelpfad
    /// und das Lager (Einzugsweite 20 Straßenfelder, Effizienz fällt linear von 100 % auf 25 %)
    /// sowie die Zonen Wohnen und Gewerbe mit Wohnhaus und Laden (Antike) und Fachwerkhaus (Mittelalter).
    /// Bevölkerungsstufe 1 (Bauern) mit den Bedürfnissen Nahrung (Weizen, Gewicht 2) und Markt (Gewicht 1).
    /// Mehrfaches Ausführen überschreibt die Werte, erzeugt aber keine Duplikate.
    /// </summary>
    public static class TestSetupGenerator
    {
        private const string Root = "Assets/Content/Test";

        [MenuItem("EpochCity/Testaufbau erzeugen")]
        public static void Generate()
        {
            Directory.CreateDirectory(Root);

            var antike = GetOrCreate<EpochDefinition>("Epoch_Antike");
            antike.order = 1;
            antike.displayName = "Antike";
            antike.currencyName = "Drachmen";
            antike.description = "Testepoche";

            var coins = GetOrCreate<ResourceDefinition>("Res_Muenzen");
            coins.displayName = "Münzen";
            coins.introducedInEpoch = antike;
            coins.isCurrency = true;

            var wheat = GetOrCreate<ResourceDefinition>("Res_Weizen");
            wheat.displayName = "Weizen";
            wheat.introducedInEpoch = antike;
            wheat.isCurrency = false;

            var irrigation = GetOrCreate<TechnologyDefinition>("Tech_Bewaesserung");
            irrigation.displayName = "Bewässerung";
            irrigation.epoch = antike;
            irrigation.path = TechPath.Science;
            irrigation.researchCost = 50f;
            irrigation.prerequisites = new TechnologyDefinition[0];
            irrigation.description = "Felder liefern 50 % mehr Weizen.";

            var research = GetOrCreate<ResourceDefinition>("Res_Forschung");
            research.displayName = "Forschung";
            research.introducedInEpoch = antike;
            research.isCurrency = false;
            research.unlimitedStorage = true; // Forschungspool ohne Lagergrenze

            var writing = GetOrCreate<TechnologyDefinition>("Tech_Schrift");
            writing.displayName = "Schrift";
            writing.epoch = antike;
            writing.path = TechPath.Culture;
            writing.researchCost = 60f;
            writing.prerequisites = new TechnologyDefinition[0];
            writing.description = "Grundlage für Verwaltung und Handel.";

            var minting = GetOrCreate<TechnologyDefinition>("Tech_Muenzpraegung");
            minting.displayName = "Münzprägung";
            minting.epoch = antike;
            minting.path = TechPath.Trade;
            minting.researchCost = 80f;
            minting.prerequisites = new[] { writing };
            minting.description = "Märkte erlösen 25 % mehr Münzen.";

            var field = GetOrCreate<BuildingDefinition>("Bld_Feld");
            field.displayName = "Feld";
            field.epoch = antike;
            field.category = BuildingCategory.Production;
            field.size = new Vector2Int(2, 2);
            field.buildCost = 100f;
            field.upkeepPerMonth = 2f;
            field.baseCapacity = 0;
            field.baseFunctions = new[]
            {
                new BuildingFunction
                {
                    label = "Weizen anbauen",
                    inputs = new ResourceAmount[0],
                    outputs = new[] { new ResourceAmount { resource = wheat, amount = 5f } },
                    cycleSeconds = 5f
                }
            };
            field.layers = new[]
            {
                new BuildingLayer { requiredTechnology = irrigation, outputMultiplier = 1.5f }
            };
            field.prefab = KeepModel(field.prefab, () => CreatePrefab("Prefab_Feld", field.size, 0.3f, new Color(0.85f, 0.75f, 0.3f)));

            var market = GetOrCreate<BuildingDefinition>("Bld_Markt");
            market.displayName = "Markt";
            market.epoch = antike;
            market.category = BuildingCategory.Commerce;
            market.size = new Vector2Int(2, 2);
            market.buildCost = 150f;
            market.upkeepPerMonth = 3f;
            market.baseCapacity = 0;
            market.baseFunctions = new[]
            {
                new BuildingFunction
                {
                    label = "Weizen verkaufen",
                    inputs = new[] { new ResourceAmount { resource = wheat, amount = 4f } },
                    outputs = new[] { new ResourceAmount { resource = coins, amount = 12f } },
                    cycleSeconds = 5f
                }
            };
            market.layers = new[]
            {
                new BuildingLayer { requiredTechnology = minting, outputMultiplier = 1.25f }
            };
            market.prefab = KeepModel(market.prefab, () => CreatePrefab("Prefab_Markt", market.size, 2f, new Color(0.75f, 0.35f, 0.25f)));

            var library = GetOrCreate<BuildingDefinition>("Bld_Bibliothek");
            library.displayName = "Bibliothek";
            library.epoch = antike;
            library.category = BuildingCategory.Service;
            library.size = new Vector2Int(2, 2);
            library.buildCost = 200f;
            library.upkeepPerMonth = 4f;
            library.baseCapacity = 0;
            library.baseFunctions = new[]
            {
                new BuildingFunction
                {
                    label = "Forschen",
                    inputs = new[] { new ResourceAmount { resource = coins, amount = 2f } },
                    outputs = new[] { new ResourceAmount { resource = research, amount = 1f } },
                    cycleSeconds = 5f
                }
            };
            library.layers = new BuildingLayer[0];
            library.prefab = KeepModel(library.prefab, () => CreatePrefab("Prefab_Bibliothek", library.size, 3f, new Color(0.35f, 0.45f, 0.8f)));

            var mittelalter = GetOrCreate<EpochDefinition>("Epoch_Mittelalter");
            mittelalter.order = 2;
            mittelalter.displayName = "Mittelalter";
            mittelalter.currencyName = "Pfennige";
            mittelalter.description = "Testepoche";
            mittelalter.keyTechnologies = new[] { irrigation, writing, minting };
            mittelalter.requiredKeyTechnologies = 2;
            mittelalter.milestones = new[] { new EpochMilestone { building = library, count = 1 } };

            var mill = GetOrCreate<BuildingDefinition>("Bld_Muehle");
            mill.displayName = "Mühle";
            mill.epoch = mittelalter;
            mill.category = BuildingCategory.Production;
            mill.size = new Vector2Int(2, 2);
            mill.buildCost = 300f;
            mill.upkeepPerMonth = 5f;
            mill.baseCapacity = 0;
            mill.baseFunctions = new[]
            {
                new BuildingFunction
                {
                    label = "Mahlen und verkaufen",
                    inputs = new[] { new ResourceAmount { resource = wheat, amount = 6f } },
                    outputs = new[] { new ResourceAmount { resource = coins, amount = 24f } },
                    cycleSeconds = 5f
                }
            };
            mill.layers = new BuildingLayer[0];
            mill.prefab = KeepModel(mill.prefab, () => CreatePrefab("Prefab_Muehle", mill.size, 4f, new Color(0.6f, 0.6f, 0.55f)));

            var path = GetOrCreate<RoadDefinition>("Road_Trampelpfad");
            path.displayName = "Trampelpfad";
            path.epoch = antike;
            path.costPerCell = 5f;
            path.upkeepPerCellPerMonth = 0.1f;
            path.quality = 1f;
            path.visualWidth = 1.3f;
            path.prefab = CreateRoadPrefab("Prefab_Trampelpfad",
                new Color(0.66f, 0.56f, 0.41f), new Color(0.38f, 0.31f, 0.22f));

            var storage = GetOrCreate<BuildingDefinition>("Bld_Lager");
            storage.displayName = "Lager";
            storage.epoch = antike;
            storage.category = BuildingCategory.Storage;
            storage.size = new Vector2Int(2, 2);
            storage.buildCost = 120f;
            storage.upkeepPerMonth = 2f;
            storage.baseCapacity = 50; // Lagerplatz je Ware (Grundlagerplatz 30 steht am CityController)
            storage.baseFunctions = new BuildingFunction[0];
            storage.layers = new BuildingLayer[0];
            storage.catchmentRange = 20f;
            storage.efficiencyByDistance = AnimationCurve.Linear(0f, 1f, 1f, 0.25f);
            storage.minEfficiency = 0.25f;
            storage.prefab = KeepModel(storage.prefab, () => CreatePrefab("Prefab_Lager", storage.size, 1.5f, new Color(0.5f, 0.38f, 0.25f)));

            // Wohnzonen nach Stufen: in der Bauernzone steigen Häuser nicht auf, in der Handwerkerzone bis Stufe 2
            var residential = GetOrCreate<ZoneDefinition>("Zone_Wohnen");
            residential.displayName = "Bauernzone";
            residential.shortName = "Bauern";
            residential.maxTier = 1;
            residential.buildingsOf = null;
            residential.requiredTechnology = null;
            residential.tilePrefab =
                CreatePrefab("Prefab_Zone_Wohnen", Vector2Int.one, 0.02f, new Color(0.62f, 0.95f, 0.45f, 0.4f), 0.92f, true);
            residential.demandColor = new Color(0.45f, 0.85f, 0.35f);
            residential.demandKind = ZoneDemandKind.Residential;
            residential.occupancyForNoDemand = 0.5f;
            residential.occupancyForFullDemand = 0.9f;
            residential.starterCapacity = 24;
            residential.starterDemand = 0.6f;

            // Gleiche Häuser wie die Bauernzone; freigeschaltet mit der Schrift (wie die Schreibschule)
            var upperResidential = GetOrCreate<ZoneDefinition>("Zone_Handwerker");
            upperResidential.displayName = "Handwerkerzone";
            upperResidential.shortName = "Handwerker";
            upperResidential.maxTier = 2;
            upperResidential.buildingsOf = residential;
            upperResidential.requiredTechnology = writing;
            upperResidential.tilePrefab =
                CreatePrefab("Prefab_Zone_Handwerker", Vector2Int.one, 0.02f, new Color(1f, 0.78f, 0.30f, 0.45f), 0.92f, true);
            upperResidential.demandColor = new Color(0.95f, 0.72f, 0.25f);
            upperResidential.demandKind = ZoneDemandKind.Residential;
            upperResidential.occupancyForNoDemand = 0.5f;
            upperResidential.occupancyForFullDemand = 0.9f;
            upperResidential.starterCapacity = 24;
            upperResidential.starterDemand = 0.6f;

            var commercial = GetOrCreate<ZoneDefinition>("Zone_Gewerbe");
            commercial.displayName = "Gewerbezone";
            commercial.shortName = "Gewerbe";
            commercial.maxTier = 0;
            commercial.buildingsOf = null;
            commercial.requiredTechnology = null;
            commercial.tilePrefab =
                CreatePrefab("Prefab_Zone_Gewerbe", Vector2Int.one, 0.02f, new Color(0.40f, 0.62f, 1f, 0.45f), 0.92f, true);
            commercial.demandColor = new Color(0.40f, 0.62f, 1f);
            commercial.demandKind = ZoneDemandKind.Commercial;
            commercial.residentsPerCell = 20f;

            var food = GetOrCreate<NeedDefinition>("Need_Nahrung");
            food.displayName = "Nahrung";
            food.epoch = antike;
            food.pyramidLevel = 1;
            food.weight = 2f;
            food.kind = NeedKind.Goods;
            food.resource = wheat;
            food.amountPerResidentPerMonth = 0.5f;
            food.providers = new BuildingDefinition[0];

            var marketNeed = GetOrCreate<NeedDefinition>("Need_Markt");
            marketNeed.displayName = "Markt";
            marketNeed.epoch = antike;
            marketNeed.pyramidLevel = 3;
            marketNeed.weight = 1f;
            marketNeed.kind = NeedKind.Service;
            marketNeed.resource = null;
            marketNeed.providers = new[] { market };
            marketNeed.range = 15f;
            marketNeed.coverageByDistance = AnimationCurve.Linear(0f, 1f, 1f, 0.2f);

            var farmers = GetOrCreate<PopulationTierDefinition>("Tier_1");
            farmers.tier = 1;
            farmers.displayName = "Bauern";
            farmers.namesByEpoch = new[]
            {
                new EpochName { epoch = antike, name = "Bauern" },
                new EpochName { epoch = mittelalter, name = "Leibeigene und Bauern" }
            };
            farmers.needs = new[] { food, marketNeed };
            farmers.taxPerResidentPerMonth = 1f;
            farmers.satisfactionByTaxRate = new AnimationCurve(
                new Keyframe(0f, 1f), new Keyframe(1f, 0.95f), new Keyframe(1.5f, 0.75f), new Keyframe(2f, 0.35f));

            var house = CreateZoneBuilding("Bld_Wohnhaus", "Wohnhaus", antike, BuildingCategory.Housing, residential,
                6, new BuildingFunction[0], 1f, new Color(0.85f, 0.8f, 0.7f));

            var shop = CreateZoneBuilding("Bld_Laden", "Laden", antike, BuildingCategory.Commerce, commercial, 0,
                SellWheat(wheat, coins, 2f, 4f), 1.2f, new Color(0.55f, 0.65f, 0.9f));

            // Größere Zonengebäude: Kapazität und Umsatz grob nach Fläche (etwa 4 Einwohner pro Feld)
            var townHouse = CreateZoneBuilding("Bld_Stadthaus", "Stadthaus", antike, BuildingCategory.Housing,
                residential, 16, new BuildingFunction[0], 1.6f, new Color(0.85f, 0.8f, 0.7f), 2);
            var domus = CreateZoneBuilding("Bld_Domus", "Domus", antike, BuildingCategory.Housing,
                residential, 36, new BuildingFunction[0], 1.8f, new Color(0.85f, 0.8f, 0.7f), 3);
            var insula = CreateZoneBuilding("Bld_Insula", "Insula", antike, BuildingCategory.Housing,
                residential, 64, new BuildingFunction[0], 3f, new Color(0.85f, 0.8f, 0.7f), 4);
            var workshop = CreateZoneBuilding("Bld_Werkstatt", "Werkstatt", antike, BuildingCategory.Commerce,
                commercial, 0, SellWheat(wheat, coins, 4f, 10f), 1.5f, new Color(0.55f, 0.65f, 0.9f), 2);
            var workyard = CreateZoneBuilding("Bld_Werkstatthof", "Werkstatthof", antike, BuildingCategory.Commerce,
                commercial, 0, SellWheat(wheat, coins, 8f, 22f), 1.8f, new Color(0.55f, 0.65f, 0.9f), 3);
            var tradeHouse = CreateZoneBuilding("Bld_Handelshaus", "Handelshaus", antike, BuildingCategory.Commerce,
                commercial, 0, SellWheat(wheat, coins, 14f, 40f), 2.4f, new Color(0.55f, 0.65f, 0.9f), 4);

            // Rechteckige Zonengebäude (Breite × Tiefe, schmale Front zur Straße), Werte ebenfalls nach Fläche
            var narrowHouse = CreateZoneBuilding("Bld_Schmalhaus", "Schmalhaus", antike, BuildingCategory.Housing,
                residential, 8, new BuildingFunction[0], 1.4f, new Color(0.85f, 0.8f, 0.7f), new Vector2Int(1, 2));
            var courtHouse = CreateZoneBuilding("Bld_Hofhaus", "Hofhaus", antike, BuildingCategory.Housing,
                residential, 24, new BuildingFunction[0], 1.7f, new Color(0.85f, 0.8f, 0.7f), new Vector2Int(2, 3));
            var longHouse = CreateZoneBuilding("Bld_Langhaus", "Langhaus", antike, BuildingCategory.Housing,
                residential, 32, new BuildingFunction[0], 1.9f, new Color(0.85f, 0.8f, 0.7f), new Vector2Int(2, 4));
            var taberna = CreateZoneBuilding("Bld_Taberna", "Taberna", antike, BuildingCategory.Commerce,
                commercial, 0, SellWheat(wheat, coins, 3f, 7f), 1.3f, new Color(0.55f, 0.65f, 0.9f), new Vector2Int(1, 2));
            var office = CreateZoneBuilding("Bld_Kontor", "Kontor", antike, BuildingCategory.Commerce,
                commercial, 0, SellWheat(wheat, coins, 6f, 15f), 1.6f, new Color(0.55f, 0.65f, 0.9f), new Vector2Int(2, 3));
            var granaryYard = CreateZoneBuilding("Bld_Speicherhof", "Speicherhof", antike, BuildingCategory.Commerce,
                commercial, 0, SellWheat(wheat, coins, 7f, 19f), 1.8f, new Color(0.55f, 0.65f, 0.9f), new Vector2Int(2, 4));

            // Nur zum Testen des Epochenbezugs: im Mittelalter wachsen Fachwerkhäuser statt Wohnhäusern
            var timberHouse = CreateZoneBuilding("Bld_Fachwerkhaus", "Fachwerkhaus", mittelalter,
                BuildingCategory.Housing, residential, 16, new BuildingFunction[0], 1.6f, new Color(0.9f, 0.88f, 0.8f), 2);

            var homes = new[] { house, townHouse, domus, insula, narrowHouse, courtHouse, longHouse, timberHouse };
            foreach (var home in homes) home.populationTier = farmers;

            // ---------------------------------------------------------------- Forschung der Antike
            // Vier Pfade mit Grund- und Aufbautechnologien (GDD Abschnitt 10); je Pfad eine Schlüsseltechnologie.

            // Handel
            var barter = Tech("Tech_Tauschhandel", "Tauschhandel", antike, TechPath.Trade, 80f,
                "Läden und Tabernen erlösen 25 % mehr.");
            var warehousing = Tech("Tech_Lagerwesen", "Lagerwesen", antike, TechPath.Trade, 100f,
                "Lager versorgen 10 Straßenfelder weiter und fassen 50 % mehr.");
            Tech(minting, "Münzprägung", antike, TechPath.Trade, 200f,
                "Märkte, Kontore und Handelshäuser erlösen 25 % mehr.", writing);
            // Kultur
            Tech(writing, "Schrift", antike, TechPath.Culture, 100f, "Bibliotheken forschen 50 % schneller.");
            var cults = Tech("Tech_Kulte", "Kulte", antike, TechPath.Culture, 120f,
                "Tempel baubar. Die Bewohner verlangen danach Glauben.");
            var philosophy = Tech("Tech_Philosophie", "Philosophie", antike, TechPath.Culture, 240f,
                "Alle Wohnhäuser bieten 25 % mehr Wohnplätze.", writing, cults);
            // Industrie
            Tech(irrigation, "Bewässerung", antike, TechPath.Industry, 100f, "Felder liefern 50 % mehr Weizen.");
            var crafts = Tech("Tech_Handwerk", "Handwerk", antike, TechPath.Industry, 100f,
                "Werkstätten, Werkhöfe und Speicherhöfe erlösen 25 % mehr; Zimmereien und Steinbrüche liefern 25 % mehr.");
            var plough = Tech("Tech_Pflug", "Pflug", antike, TechPath.Industry, 220f,
                "Felder liefern weitere 30 % mehr Weizen.", irrigation, crafts);
            // Wissenschaft und Infrastruktur
            var surveying = Tech("Tech_Vermessung", "Vermessung", antike, TechPath.Science, 120f,
                "Pflasterstraße baubar: bessere Anbindung ans Lager.");
            var mathematics = Tech("Tech_Mathematik", "Mathematik", antike, TechPath.Science, 180f,
                "Bibliotheken forschen weitere 25 % schneller.", writing, surveying);
            var waterworks = Tech("Tech_Wasserbau", "Wasserbau", antike, TechPath.Science, 240f,
                "Brunnen baubar. Die Bewohner verlangen danach Wasser.", surveying);

            // Wirkungen als Schichten auf den Gebäuden (das Aussehen bleibt)
            shop.layers = taberna.layers = new[] { Boost(barter, 1.25f) };
            storage.layers = new[]
            {
                new BuildingLayer { requiredTechnology = warehousing, outputMultiplier = 1f, extraCatchment = 10f, capacityMultiplier = 1.5f }
            };
            market.layers = office.layers = tradeHouse.layers = new[] { Boost(minting, 1.25f) };
            library.layers = new[] { Boost(writing, 1.5f), Boost(mathematics, 1.25f) };
            field.layers = new[] { Boost(irrigation, 1.5f), Boost(plough, 1.3f) };
            workshop.layers = workyard.layers = granaryYard.layers = new[] { Boost(crafts, 1.25f) };
            foreach (var home in homes)
                home.layers = new[] { new BuildingLayer { requiredTechnology = philosophy, outputMultiplier = 1f, capacityMultiplier = 1.25f } };

            // Neue Gebäude und Straßen durch Forschung
            var temple = GetOrCreate<BuildingDefinition>("Bld_Tempel");
            temple.displayName = "Tempel";
            temple.epoch = antike;
            temple.requiredTechnology = cults;
            temple.category = BuildingCategory.Service;
            temple.size = new Vector2Int(2, 3);
            temple.buildCost = 250f;
            temple.upkeepPerMonth = 4f;
            temple.baseCapacity = 0;
            temple.baseFunctions = new BuildingFunction[0];
            temple.layers = new BuildingLayer[0];
            temple.prefab = KeepModel(temple.prefab, () => CreatePrefab("Prefab_Tempel", temple.size, 3f, new Color(0.9f, 0.86f, 0.75f)));

            var well = GetOrCreate<BuildingDefinition>("Bld_Brunnen");
            well.displayName = "Brunnen";
            well.epoch = antike;
            well.requiredTechnology = waterworks;
            well.category = BuildingCategory.Service;
            well.size = Vector2Int.one;
            well.buildCost = 60f;
            well.upkeepPerMonth = 1f;
            well.baseCapacity = 0;
            well.baseFunctions = new BuildingFunction[0];
            well.layers = new BuildingLayer[0];
            well.prefab = KeepModel(well.prefab, () => CreatePrefab("Prefab_Brunnen", well.size, 0.8f, new Color(0.5f, 0.7f, 0.85f)));

            var paved = GetOrCreate<RoadDefinition>("Road_Pflaster");
            paved.displayName = "Pflasterstraße";
            paved.epoch = antike;
            paved.requiredTechnology = surveying;
            paved.costPerCell = 15f;
            paved.upkeepPerCellPerMonth = 0.25f;
            paved.quality = 1.6f;
            paved.visualWidth = 1.5f;
            paved.prefab = CreateRoadPrefab("Prefab_Pflaster",
                new Color(0.74f, 0.71f, 0.65f), new Color(0.48f, 0.45f, 0.40f));

            // Neue Bedürfnisse, sobald die Technologie erforscht ist (wie neue Ansprüche in Anno)
            var water = GetOrCreate<NeedDefinition>("Need_Wasser");
            water.displayName = "Wasser";
            water.epoch = antike;
            water.requiredTechnology = waterworks;
            water.pyramidLevel = 1;
            water.weight = 1.5f;
            water.kind = NeedKind.Service;
            water.resource = null;
            water.providers = new[] { well };
            water.range = 10f;
            water.coverageByDistance = AnimationCurve.Linear(0f, 1f, 1f, 0.4f);

            var faith = GetOrCreate<NeedDefinition>("Need_Glaube");
            faith.displayName = "Glaube";
            faith.epoch = antike;
            faith.requiredTechnology = cults;
            faith.pyramidLevel = 4;
            faith.weight = 1f;
            faith.kind = NeedKind.Service;
            faith.resource = null;
            faith.providers = new[] { temple };
            faith.range = 20f;
            faith.coverageByDistance = AnimationCurve.Linear(0f, 1f, 1f, 0.3f);
            farmers.needs = new[] { food, water, marketNeed, faith };

            // ---------------------------------------------------------------- Holz und Stein
            // Holzfäller → Stämme → Zimmerei → Bretter, Steinbruch → Stein; Bretter und Stein sind Baumaterial.
            // Die drei Betriebe kosten nur Geld, damit eine Stadt ohne Vorrat immer anfangen kann.

            var logs = Resource("Res_Staemme", "Stämme", antike, false);
            var boards = Resource("Res_Bretter", "Bretter", antike, true);
            var stone = Resource("Res_Stein", "Stein", antike, true);

            var woodcutter = ProductionBuilding("Bld_Holzfaeller", "Holzfäller", antike, 60f, 1f,
                "Bäume fällen", null, 0f, logs, 1f, 5f, new Color(0.45f, 0.35f, 0.2f));
            var carpentry = ProductionBuilding("Bld_Zimmerei", "Zimmerei", antike, 100f, 2f,
                "Bretter sägen", logs, 1f, boards, 1f, 5f, new Color(0.7f, 0.55f, 0.35f));
            var quarry = ProductionBuilding("Bld_Steinbruch", "Steinbruch", antike, 100f, 2f,
                "Stein brechen", null, 0f, stone, 1f, 8f, new Color(0.6f, 0.6f, 0.6f));
            carpentry.layers = quarry.layers = new[] { Boost(crafts, 1.25f) };

            // Arbeitsplätze für Bauern (Stufe 1); Dienste ohne Produktion (Tempel, Brunnen) brauchen vorerst keine
            foreach (var (def, workers) in new[]
                     {
                         (field, 4), (woodcutter, 3), (carpentry, 3), (quarry, 4), (storage, 2), (market, 2),
                         (library, 3), (mill, 4)
                     })
            {
                def.workerTier = farmers;
                def.workers = workers;
            }

            // Rohstoff-Vorkommen: Wald wird beim Bauen gerodet, auf Stein stehen nur Steinbrüche (Straßen dürfen darüber)
            var forest = GetOrCreate<DepositDefinition>("Dep_Wald");
            forest.displayName = "Wald";
            forest.color = new Color(0.22f, 0.42f, 0.18f);
            forest.clearedByBuilding = true;
            forest.blocksBuilding = false;
            // Dezent dunklerer Waldboden unter den Bäumen; die Bäume trägt „Gebäude-Modelle erzeugen“ ein
            forest.tilePrefab = CreatePrefab("Prefab_Vorkommen_Wald", Vector2Int.one, 0.015f,
                new Color(0.30f, 0.38f, 0.19f), 1f);
            forest.decorPerCell = new Vector2Int(2, 3);
            forest.decorScale = new Vector2(0.8f, 1.2f);

            var stoneDeposit = GetOrCreate<DepositDefinition>("Dep_Stein");
            stoneDeposit.displayName = "Steinvorkommen";
            stoneDeposit.color = new Color(0.55f, 0.54f, 0.50f);
            stoneDeposit.clearedByBuilding = false;
            stoneDeposit.blocksBuilding = true;
            stoneDeposit.tilePrefab = CreatePrefab("Prefab_Vorkommen_Stein", Vector2Int.one, 0.015f,
                new Color(0.50f, 0.48f, 0.42f), 1f);
            stoneDeposit.decorPerCell = new Vector2Int(1, 2);
            stoneDeposit.decorScale = new Vector2(0.8f, 1.2f);

            // Holzfäller: Wald im Umkreis 3, ab 16 Feldern 100 %; Steinbruch: 2 seiner 4 Felder auf Stein
            woodcutter.requiredDeposit = forest;
            woodcutter.depositRange = 3;
            woodcutter.depositCellsForFull = 16;
            quarry.requiredDeposit = stoneDeposit;
            quarry.depositRange = 0;
            quarry.depositCellsForFull = 2;

            var mapDef = GetOrCreate<MapDefinition>("Map_Standard");
            mapDef.seed = 0;
            mapDef.border = 2;
            mapDef.startAreaRadius = 4f;
            mapDef.patches = new[]
            {
                // Stein zuerst: kleine Flecken, die der Wald danach auslässt
                new DepositPatches { deposit = stoneDeposit, minCount = 3, maxCount = 4, minCells = 4, maxCells = 8,
                    firstPatchDistance = new Vector2(6f, 12f) },
                new DepositPatches { deposit = forest, minCount = 6, maxCount = 8, minCells = 15, maxCells = 40,
                    firstPatchDistance = new Vector2(8f, 14f) }
            };

            // Baukosten in Material zusätzlich zum Geld (Trampelpfad, Zonen und Zonengebäude bleiben ohne)
            field.materialCosts = Cost((boards, 2f));
            storage.materialCosts = Cost((boards, 6f));
            market.materialCosts = Cost((boards, 4f), (stone, 2f));
            library.materialCosts = Cost((boards, 4f), (stone, 6f));
            temple.materialCosts = Cost((boards, 6f), (stone, 10f));
            well.materialCosts = Cost((stone, 4f));
            mill.materialCosts = Cost((boards, 6f), (stone, 4f));
            paved.materialCostsPerCell = Cost((stone, 0.5f));
            path.materialCostsPerCell = new ResourceAmount[0];

            // ---------------------------------------------------------------- Stufe 2: Handwerker
            // Häuser steigen auf, wenn die Bauern zufrieden sind und eine Schule in Reichweite liegt (GDD: Bildung
            // hebt in die nächste Stufe). Modell und Grundfläche bleiben, nur Bedürfnisse und Steuern ändern sich.

            var school = GetOrCreate<BuildingDefinition>("Bld_Schreibschule");
            school.displayName = "Schreibschule";
            school.epoch = antike;
            school.requiredTechnology = writing;
            school.category = BuildingCategory.Service;
            school.size = new Vector2Int(2, 2);
            school.buildCost = 150f;
            school.materialCosts = Cost((boards, 4f), (stone, 4f));
            school.upkeepPerMonth = 3f;
            school.baseCapacity = 0;
            school.baseFunctions = new BuildingFunction[0];
            school.layers = new BuildingLayer[0];
            school.workerTier = farmers;
            school.workers = 2;
            school.prefab = KeepModel(school.prefab, () => CreatePrefab("Prefab_Schreibschule", school.size, 2f, new Color(0.8f, 0.75f, 0.6f)));

            var education = GetOrCreate<NeedDefinition>("Need_Bildung");
            education.displayName = "Bildung";
            education.epoch = antike;
            education.requiredTechnology = writing;
            education.pyramidLevel = 4;
            education.weight = 1f;
            education.kind = NeedKind.Service;
            education.resource = null;
            education.providers = new[] { school };
            education.range = 15f;
            education.coverageByDistance = AnimationCurve.Linear(0f, 1f, 1f, 0.4f);

            var artisans = GetOrCreate<PopulationTierDefinition>("Tier_2");
            artisans.tier = 2;
            artisans.displayName = "Handwerker";
            artisans.namesByEpoch = new[]
            {
                new EpochName { epoch = antike, name = "Handwerker" },
                new EpochName { epoch = mittelalter, name = "Bürger" }
            };
            artisans.needs = new[] { food, water, marketNeed, faith }; // Öl folgt mit der Ölpresse
            artisans.taxPerResidentPerMonth = 2f;
            artisans.satisfactionByTaxRate = farmers.satisfactionByTaxRate;
            artisans.nextTier = null;
            artisans.upgradeRequires = new NeedDefinition[0];
            artisans.downgradeSatisfaction = 0.4f;
            artisans.downgradeMonths = 3;

            farmers.nextTier = artisans;
            farmers.upgradeSatisfaction = 0.75f;
            farmers.upgradeMonths = 3;
            farmers.upgradeRequires = new[] { education };

            // Mittelalter: 3 der 4 Schlüsseltechnologien (je Pfad eine), eine Bibliothek und 300 Einwohner
            mittelalter.keyTechnologies = new[] { minting, philosophy, plough, waterworks };
            mittelalter.requiredKeyTechnologies = 3;
            mittelalter.requiredPopulation = 300;

            var content = new ScriptableObject[]
            {
                antike, mittelalter, coins, wheat, research, irrigation, writing, minting,
                barter, warehousing, cults, philosophy, crafts, plough, surveying, mathematics, waterworks,
                field, market, library, mill, path, paved, storage, temple, well, residential, commercial,
                house, shop, townHouse, domus, insula, workshop, workyard, tradeHouse,
                narrowHouse, courtHouse, longHouse, taberna, office, granaryYard, timberHouse,
                food, marketNeed, water, faith, farmers,
                logs, boards, stone, woodcutter, carpentry, quarry, forest, stoneDeposit, mapDef,
                school, education, artisans, upperResidential
            };

            // Inhaltsverzeichnis für Spielstände: alles, worauf ein Spielstand per Namen verweisen kann
            var registry = GetOrCreate<ContentRegistry>("ContentRegistry");
            registry.entries = content;
            EditorUtility.SetDirty(registry);

            foreach (var asset in content) EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            KenneyBuildingFactory.ReimportPrefabs(Root);

            Debug.Log($"Testaufbau erzeugt unter {Root}.");
        }

        /// <summary>Technologie anlegen oder vorhandene überschreiben (Name, Pfad, Kosten, Voraussetzungen).</summary>
        private static TechnologyDefinition Tech(string assetName, string displayName, EpochDefinition epoch, TechPath path,
            float cost, string description, params TechnologyDefinition[] prerequisites) =>
            Tech(GetOrCreate<TechnologyDefinition>(assetName), displayName, epoch, path, cost, description, prerequisites);

        private static TechnologyDefinition Tech(TechnologyDefinition tech, string displayName, EpochDefinition epoch,
            TechPath path, float cost, string description, params TechnologyDefinition[] prerequisites)
        {
            tech.displayName = displayName;
            tech.epoch = epoch;
            tech.path = path;
            tech.researchCost = cost;
            tech.prerequisites = prerequisites;
            tech.description = description;
            EditorUtility.SetDirty(tech);
            return tech;
        }

        private static ResourceDefinition Resource(string assetName, string displayName, EpochDefinition epoch,
            bool buildingMaterial)
        {
            var res = GetOrCreate<ResourceDefinition>(assetName);
            res.displayName = displayName;
            res.introducedInEpoch = epoch;
            res.isCurrency = false;
            res.isBuildingMaterial = buildingMaterial;
            return res;
        }

        /// <summary>
        /// Einzeln gesetzter Produktionsbetrieb (2 × 2) mit einer Funktion: Eingang (oder keiner) → Ausgang je Zyklus.
        /// </summary>
        private static BuildingDefinition ProductionBuilding(string assetName, string displayName, EpochDefinition epoch,
            float cost, float upkeep, string label, ResourceDefinition input, float inputAmount,
            ResourceDefinition output, float outputAmount, float cycleSeconds, Color placeholderColor)
        {
            var def = GetOrCreate<BuildingDefinition>(assetName);
            def.displayName = displayName;
            def.epoch = epoch;
            def.category = BuildingCategory.Production;
            def.size = new Vector2Int(2, 2);
            def.buildCost = cost;
            def.upkeepPerMonth = upkeep;
            def.baseCapacity = 0;
            def.materialCosts = new ResourceAmount[0];
            def.baseFunctions = new[]
            {
                new BuildingFunction
                {
                    label = label,
                    inputs = input != null
                        ? new[] { new ResourceAmount { resource = input, amount = inputAmount } }
                        : new ResourceAmount[0],
                    outputs = new[] { new ResourceAmount { resource = output, amount = outputAmount } },
                    cycleSeconds = cycleSeconds
                }
            };
            def.layers = new BuildingLayer[0];
            def.prefab = KeepModel(def.prefab,
                () => CreatePrefab("Prefab_" + assetName.Substring(4), def.size, 1.5f, placeholderColor));
            return def;
        }

        private static ResourceAmount[] Cost(params (ResourceDefinition resource, float amount)[] items)
        {
            var result = new ResourceAmount[items.Length];
            for (int i = 0; i < items.Length; i++)
                result[i] = new ResourceAmount { resource = items[i].resource, amount = items[i].amount };
            return result;
        }

        /// <summary>Schicht, die die Ausgaben aller Funktionen vervielfacht.</summary>
        private static BuildingLayer Boost(TechnologyDefinition tech, float multiplier) =>
            new BuildingLayer { requiredTechnology = tech, outputMultiplier = multiplier, capacityMultiplier = 1f };

        /// <summary>
        /// Straßen-Prefab für RoadVisuals: ein MeshFilter mit Fahrbahn- und Randmaterial. Das gespeicherte Mesh
        /// ist nur die Vorschau beim Ziehen (ein volles Feld); im Spiel setzt RoadVisuals das passende Stück.
        /// </summary>
        private static GameObject CreateRoadPrefab(string name, Color surfaceColor, Color edgeColor)
        {
            const float cellSize = 2f; // Standardwert von CityController.cellSize

            var previewMesh = KenneyBuildingFactory.SaveMeshAsset(
                RoadMeshBuilder.Build(RoadMeshBuilder.East | RoadMeshBuilder.West |
                                      RoadMeshBuilder.North | RoadMeshBuilder.South,
                    cellSize, cellSize * 0.95f, 0f),
                $"{Root}/Mesh_{name}_Vorschau.asset");

            var surface = CreateMatteMaterial($"Mat_{name}", surfaceColor);
            var edge = CreateMatteMaterial($"Mat_{name}_Rand", edgeColor);

            var root = new GameObject(name);
            var body = new GameObject("Surface", typeof(MeshFilter), typeof(MeshRenderer));
            body.transform.SetParent(root.transform, false);
            body.GetComponent<MeshFilter>().sharedMesh = previewMesh;
            var renderer = body.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { surface, edge };
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Root}/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static Material CreateMatteMaterial(string name, Color color)
        {
            string path = $"{Root}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>
        /// Behält ein echtes Modell (z. B. aus „Gebäude-Modelle erzeugen“) und legt nur dann einen Platzhalter-Würfel
        /// an, wenn noch keins oder nur ein Platzhalter aus diesem Ordner zugewiesen ist.
        /// </summary>
        private static GameObject KeepModel(GameObject current, System.Func<GameObject> createPlaceholder)
        {
            if (current != null && !AssetDatabase.GetAssetPath(current).StartsWith(Root)) return current;
            return createPlaceholder();
        }

        private static BuildingDefinition CreateZoneBuilding(string assetName, string displayName,
            EpochDefinition epoch, BuildingCategory category, ZoneDefinition zone, int capacity,
            BuildingFunction[] functions, float height, Color color, int size = 1) =>
            CreateZoneBuilding(assetName, displayName, epoch, category, zone, capacity, functions, height, color,
                new Vector2Int(size, size));

        /// <summary>Zonengebäude (Breite × Tiefe in Feldern): kostenlos, ohne Unterhalt, wächst in der angegebenen Zone.</summary>
        private static BuildingDefinition CreateZoneBuilding(string assetName, string displayName,
            EpochDefinition epoch, BuildingCategory category, ZoneDefinition zone, int capacity,
            BuildingFunction[] functions, float height, Color color, Vector2Int size)
        {
            var def = GetOrCreate<BuildingDefinition>(assetName);
            def.displayName = displayName;
            def.epoch = epoch;
            def.category = category;
            def.zone = zone;
            def.size = size;
            def.buildCost = 0f;
            def.upkeepPerMonth = 0f;
            def.baseCapacity = capacity;
            def.baseFunctions = functions;
            def.layers = new BuildingLayer[0];
            def.prefab = KeepModel(def.prefab, () => CreatePrefab("Prefab_" + assetName.Substring(4), def.size, height, color, 0.8f));
            return def;
        }

        /// <summary>Gewerbe-Funktion: Weizen gegen Geld verkaufen.</summary>
        private static BuildingFunction[] SellWheat(ResourceDefinition wheat, ResourceDefinition coins,
            float wheatIn, float coinsOut) =>
            new[]
            {
                new BuildingFunction
                {
                    label = "Weizen verkaufen",
                    inputs = new[] { new ResourceAmount { resource = wheat, amount = wheatIn } },
                    outputs = new[] { new ResourceAmount { resource = coins, amount = coinsOut } },
                    cycleSeconds = 5f
                }
            };

        private static T GetOrCreate<T>(string name) where T : ScriptableObject
        {
            string path = $"{Root}/{name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        /// <summary>
        /// Platzhalter-Prefab: ein Quader, der die Grundfläche ausfüllt. Der Ursprung liegt
        /// unten mittig, weil CityController das Prefab in die Mitte der Grundfläche setzt.
        /// </summary>
        /// <param name="transparent">Für Anzeigen wie Zonen: halbtransparent (Alpha aus der Farbe), ohne Schatten.</param>
        private static GameObject CreatePrefab(string name, Vector2Int size, float height, Color color,
            float footprint = 0.9f, bool transparent = false)
        {
            const float cellSize = 2f; // Standardwert von CityController.cellSize

            var material = AssetDatabase.LoadAssetAtPath<Material>($"{Root}/Mat_{name}.mat");
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, $"{Root}/Mat_{name}.mat");
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else material.color = color;
            // Matte Oberflächen passen zum stilisierten Look; der Standardwert glänzt im Sonnenlicht zu stark
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.15f);
            if (transparent) MaterialUtil.SetTransparent(material);
            EditorUtility.SetDirty(material);

            var root = new GameObject(name);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale =
                new Vector3(size.x * cellSize * footprint, height, size.y * cellSize * footprint);
            body.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            body.GetComponent<Renderer>().sharedMaterial = material;
            if (transparent)
            {
                body.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Object.DestroyImmediate(body.GetComponent<Collider>());
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Root}/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }
    }
}
