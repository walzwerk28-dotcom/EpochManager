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

        // Balancing nach dem Antike-Durchlauf (2026-10-31), auf die Grundwerte angewendet
        private const float UpkeepScale = 3f;          // Unterhalt einzeln gesetzter Gebäude
        private const float CommerceIncomeScale = 0.25f; // Erlös der Gewerbegebäude

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
            market.size = new Vector2Int(2, 4);   // 2026-11-03: Marktplatz mit sechs Ständen und Halle (vorher 2 × 2)
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

            // Eigene Häuser (siehe unten); freigeschaltet mit der Schrift (wie die Schreibschule)
            var upperResidential = GetOrCreate<ZoneDefinition>("Zone_Handwerker");
            upperResidential.displayName = "Handwerkerzone";
            upperResidential.shortName = "Handwerker";
            upperResidential.maxTier = 2;
            upperResidential.buildingsOf = null; // eigene Häuser (Werkstatthaus bis Zunfthaus)
            upperResidential.requiredTechnology = writing;
            upperResidential.tilePrefab =
                CreatePrefab("Prefab_Zone_Handwerker", Vector2Int.one, 0.02f, new Color(1f, 0.78f, 0.30f, 0.45f), 0.92f, true);
            upperResidential.demandColor = new Color(0.95f, 0.72f, 0.25f);
            upperResidential.demandKind = ZoneDemandKind.Residential;
            upperResidential.occupancyForNoDemand = 0.5f;
            upperResidential.occupancyForFullDemand = 0.9f;
            upperResidential.starterCapacity = 24;
            upperResidential.starterDemand = 0.6f;

            // Gelehrtenzone: eigene Häuser, Aufstieg bis Stufe 3; freigeschaltet mit Philosophie (siehe unten)
            var topResidential = GetOrCreate<ZoneDefinition>("Zone_Gelehrte");
            topResidential.displayName = "Gelehrtenzone";
            topResidential.shortName = "Gelehrte";
            topResidential.maxTier = 3;
            topResidential.buildingsOf = null;
            topResidential.tilePrefab =
                CreatePrefab("Prefab_Zone_Gelehrte", Vector2Int.one, 0.02f, new Color(0.72f, 0.50f, 0.95f, 0.45f), 0.92f, true);
            topResidential.demandColor = new Color(0.68f, 0.48f, 0.92f);
            topResidential.demandKind = ZoneDemandKind.Residential;
            topResidential.occupancyForNoDemand = 0.5f;
            topResidential.occupancyForFullDemand = 0.9f;
            topResidential.starterCapacity = 24;
            topResidential.starterDemand = 0.6f;

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
            farmers.commerceWeight = 1f;
            farmers.satisfactionByTaxRate = new AnimationCurve(
                new Keyframe(0f, 1f), new Keyframe(1f, 0.95f), new Keyframe(1.5f, 0.75f), new Keyframe(2f, 0.35f));

            var house = CreateZoneBuilding("Bld_Wohnhaus", "Wohnhaus", antike, BuildingCategory.Housing, residential,
                6, new BuildingFunction[0], 1f, new Color(0.85f, 0.8f, 0.7f));

            var shop = CreateZoneBuilding("Bld_Laden", "Laden", antike, BuildingCategory.Commerce, commercial, 0,
                Sale(wheat, 2f, 2f), 1.2f, new Color(0.55f, 0.65f, 0.9f));

            // Größere Zonengebäude: Kapazität und Umsatz grob nach Fläche (etwa 4 Einwohner pro Feld)
            var townHouse = CreateZoneBuilding("Bld_Stadthaus", "Stadthaus", antike, BuildingCategory.Housing,
                residential, 16, new BuildingFunction[0], 1.6f, new Color(0.85f, 0.8f, 0.7f), 2);
            var domus = CreateZoneBuilding("Bld_Domus", "Domus", antike, BuildingCategory.Housing,
                residential, 36, new BuildingFunction[0], 1.8f, new Color(0.85f, 0.8f, 0.7f), 3);
            var insula = CreateZoneBuilding("Bld_Insula", "Insula", antike, BuildingCategory.Housing,
                residential, 64, new BuildingFunction[0], 3f, new Color(0.85f, 0.8f, 0.7f), 4);
            var workshop = CreateZoneBuilding("Bld_Werkstatt", "Werkstatt", antike, BuildingCategory.Commerce,
                commercial, 0, Sale(wheat, 4f, 2.5f), 1.5f, new Color(0.55f, 0.65f, 0.9f), 2);
            var workyard = CreateZoneBuilding("Bld_Werkstatthof", "Werkstatthof", antike, BuildingCategory.Commerce,
                commercial, 0, Sale(wheat, 8f, 2.75f), 1.8f, new Color(0.55f, 0.65f, 0.9f), 3);
            var tradeHouse = CreateZoneBuilding("Bld_Handelshaus", "Handelshaus", antike, BuildingCategory.Commerce,
                commercial, 0, Sale(wheat, 14f, 40f / 14f), 2.4f, new Color(0.55f, 0.65f, 0.9f), 4);

            // Rechteckige Zonengebäude (Breite × Tiefe, schmale Front zur Straße), Werte ebenfalls nach Fläche
            var narrowHouse = CreateZoneBuilding("Bld_Schmalhaus", "Schmalhaus", antike, BuildingCategory.Housing,
                residential, 8, new BuildingFunction[0], 1.4f, new Color(0.85f, 0.8f, 0.7f), new Vector2Int(1, 2));
            var courtHouse = CreateZoneBuilding("Bld_Hofhaus", "Hofhaus", antike, BuildingCategory.Housing,
                residential, 24, new BuildingFunction[0], 1.7f, new Color(0.85f, 0.8f, 0.7f), new Vector2Int(2, 3));
            var longHouse = CreateZoneBuilding("Bld_Langhaus", "Langhaus", antike, BuildingCategory.Housing,
                residential, 32, new BuildingFunction[0], 1.9f, new Color(0.85f, 0.8f, 0.7f), new Vector2Int(2, 4));
            var taberna = CreateZoneBuilding("Bld_Taberna", "Taberna", antike, BuildingCategory.Commerce,
                commercial, 0, Sale(wheat, 3f, 7f / 3f), 1.3f, new Color(0.55f, 0.65f, 0.9f), new Vector2Int(1, 2));
            var office = CreateZoneBuilding("Bld_Kontor", "Kontor", antike, BuildingCategory.Commerce,
                commercial, 0, Sale(wheat, 6f, 2.5f), 1.6f, new Color(0.55f, 0.65f, 0.9f), new Vector2Int(2, 3));
            var granaryYard = CreateZoneBuilding("Bld_Speicherhof", "Speicherhof", antike, BuildingCategory.Commerce,
                commercial, 0, Sale(wheat, 7f, 19f / 7f), 1.8f, new Color(0.55f, 0.65f, 0.9f), new Vector2Int(2, 4));

            // Nur zum Testen des Epochenbezugs: im Mittelalter wachsen Fachwerkhäuser statt Wohnhäusern
            var timberHouse = CreateZoneBuilding("Bld_Fachwerkhaus", "Fachwerkhaus", mittelalter,
                BuildingCategory.Housing, residential, 16, new BuildingFunction[0], 1.6f, new Color(0.9f, 0.88f, 0.8f), 2);

            // Eigene Häuser der Handwerkerzone: zuerst ziehen Bauern ein, die dort aufsteigen; das Modell zeigt von
            // Anfang an ein Handwerkerhaus (Laden unten, Werkhof). Etwa 4 Wohnplätze je Feld wie bei den Bauern.
            var workshopHouse = CreateZoneBuilding("Bld_Werkstatthaus", "Werkstatthaus", antike, BuildingCategory.Housing,
                upperResidential, 6, new BuildingFunction[0], 1.6f, new Color(0.9f, 0.75f, 0.55f));
            var artisanHouse = CreateZoneBuilding("Bld_Handwerkerhaus", "Handwerkerhaus", antike, BuildingCategory.Housing,
                upperResidential, 8, new BuildingFunction[0], 1.6f, new Color(0.9f, 0.75f, 0.55f), new Vector2Int(1, 2));
            var artisanRow = CreateZoneBuilding("Bld_Handwerkerzeile", "Handwerkerzeile", antike, BuildingCategory.Housing,
                upperResidential, 16, new BuildingFunction[0], 1.8f, new Color(0.9f, 0.75f, 0.55f), 2);
            var workyardHouse = CreateZoneBuilding("Bld_Werkhof", "Werkhof", antike, BuildingCategory.Housing,
                upperResidential, 24, new BuildingFunction[0], 1.8f, new Color(0.9f, 0.75f, 0.55f), new Vector2Int(2, 3));
            var guildHouse = CreateZoneBuilding("Bld_Zunfthaus", "Zunfthaus", antike, BuildingCategory.Housing,
                upperResidential, 36, new BuildingFunction[0], 2.2f, new Color(0.9f, 0.75f, 0.55f), 3);

            // Häuser der Gelehrtenzone: Steinbauten mit Säulen und Gärten, etwas mehr Platz (etwa 3 je Feld);
            // zuerst Bauern, die über Handwerker zu Gelehrten aufsteigen
            var scholarHouse = CreateZoneBuilding("Bld_Gelehrtenhaus", "Gelehrtenhaus", antike, BuildingCategory.Housing,
                topResidential, 6, new BuildingFunction[0], 1.6f, new Color(0.9f, 0.88f, 0.8f), new Vector2Int(1, 2));
            var townVilla = CreateZoneBuilding("Bld_Stadtvilla", "Stadtvilla", antike, BuildingCategory.Housing,
                topResidential, 12, new BuildingFunction[0], 1.8f, new Color(0.9f, 0.88f, 0.8f), 2);
            var gardenVilla = CreateZoneBuilding("Bld_Villa", "Villa mit Garten", antike, BuildingCategory.Housing,
                topResidential, 18, new BuildingFunction[0], 1.8f, new Color(0.9f, 0.88f, 0.8f), new Vector2Int(2, 3));
            var peristyle = CreateZoneBuilding("Bld_Peristylhaus", "Peristylhaus", antike, BuildingCategory.Housing,
                topResidential, 27, new BuildingFunction[0], 2f, new Color(0.9f, 0.88f, 0.8f), 3);

            var homes = new[]
            {
                house, townHouse, domus, insula, narrowHouse, courtHouse, longHouse, timberHouse,
                workshopHouse, artisanHouse, artisanRow, workyardHouse, guildHouse,
                scholarHouse, townVilla, gardenVilla, peristyle
            };
            foreach (var home in homes) home.populationTier = farmers;

            // ---------------------------------------------------------------- Forschung der Antike
            // Vier Pfade mit Grund- und Aufbautechnologien (GDD Abschnitt 10); je Pfad eine Schlüsseltechnologie.

            // Handel
            var barter = Tech("Tech_Tauschhandel", "Tauschhandel", antike, TechPath.Trade, 320f,
                "Läden und Tabernen erlösen 25 % mehr.");
            var warehousing = Tech("Tech_Lagerwesen", "Lagerwesen", antike, TechPath.Trade, 400f,
                "Lager versorgen 10 Straßenfelder weiter und fassen 50 % mehr.");
            Tech(minting, "Münzprägung", antike, TechPath.Trade, 800f,
                "Märkte, Kontore und Handelshäuser erlösen 25 % mehr.", writing);
            // Kultur
            Tech(writing, "Schrift", antike, TechPath.Culture, 50f, "Bibliotheken forschen 50 % schneller.");
            var cults = Tech("Tech_Kulte", "Kulte", antike, TechPath.Culture, 480f,
                "Tempel baubar. Die Bewohner verlangen danach Glauben.");
            var philosophy = Tech("Tech_Philosophie", "Philosophie", antike, TechPath.Culture, 960f,
                "Alle Wohnhäuser bieten 25 % mehr Wohnplätze.", writing, cults);
            // Industrie
            Tech(irrigation, "Bewässerung", antike, TechPath.Industry, 400f, "Felder liefern 50 % mehr Weizen.");
            var crafts = Tech("Tech_Handwerk", "Handwerk", antike, TechPath.Industry, 400f,
                "Werkstätten, Werkhöfe und Speicherhöfe erlösen 25 % mehr; Zimmereien, Steinbrüche, Ölpressen und Töpfereien liefern 25 % mehr.");
            var plough = Tech("Tech_Pflug", "Pflug", antike, TechPath.Industry, 880f,
                "Felder liefern weitere 30 % mehr Weizen.", irrigation, crafts);
            // Wissenschaft und Infrastruktur
            var surveying = Tech("Tech_Vermessung", "Vermessung", antike, TechPath.Science, 480f,
                "Pflasterstraße baubar: bessere Anbindung ans Lager.");
            var mathematics = Tech("Tech_Mathematik", "Mathematik", antike, TechPath.Science, 720f,
                "Bibliotheken forschen weitere 25 % schneller.", writing, surveying);
            var waterworks = Tech("Tech_Wasserbau", "Wasserbau", antike, TechPath.Science, 960f,
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
            water.range = 15f; // vorher 10 Felder bis 40 %: Brunnen wirkten kaum (Antike-Durchlauf 2026-10-31)
            water.coverageByDistance = AnimationCurve.Linear(0f, 1f, 1f, 0.5f);

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

            // Holzfäller fällen alle 4 Stämme ein Waldfeld; es wächst in 8 Monaten nach, beim Förster in 4
            woodcutter.fellsDeposit = true;
            woodcutter.cyclesPerFelling = 4;
            forest.regrowMonths = 8;
            forest.regrowMonthsTended = 4;
            stoneDeposit.regrowMonths = 0;

            // Förster: pflanzt alle 15 s ein Waldfeld im Umkreis 3, bevorzugt neben vorhandenem Wald
            var forester = ProductionBuilding("Bld_Foerster", "Förster", antike, 60f, 1f,
                "Bäume pflanzen", null, 0f, null, 0f, 15f, new Color(0.3f, 0.5f, 0.25f));
            forester.baseFunctions[0].outputs = new ResourceAmount[0];
            forester.materialCosts = Cost((boards, 2f));
            forester.plantsDeposit = forest;
            forester.depositRange = 3;
            forester.workerTier = farmers;
            forester.workers = 2;

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
            school.requiredTechnology = null; // ab Start: erste Forschung für die Schrift
            school.category = BuildingCategory.Service;
            school.size = new Vector2Int(2, 2);
            school.buildCost = 150f;
            school.materialCosts = Cost((boards, 4f), (stone, 4f));
            school.upkeepPerMonth = 3f;
            school.baseCapacity = 0;
            school.baseFunctions = new[]
            {
                new BuildingFunction
                {
                    label = "Schreiben lehren",
                    inputs = new[] { new ResourceAmount { resource = coins, amount = 1f } },
                    outputs = new[] { new ResourceAmount { resource = research, amount = 0.5f } },
                    cycleSeconds = 5f
                }
            };
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
            // Bedürfnisse siehe unten (Bauern-Bedürfnisse plus Öl und Keramik)
            artisans.taxPerResidentPerMonth = 1.5f;
            artisans.commerceWeight = 1.5f;

            // Arbeitsplätze im Gewerbe (wie in Cities: Skylines): kleine Läden mit Bauern, große Häuser mit
            // Handwerkern; Gebäude wachsen erst, wenn ihre Arbeiterstufe in der Stadt lebt (ZoneGrowth)
            foreach (var (def, tier, count) in new[]
                     {
                         (shop, farmers, 1), (taberna, farmers, 2), (workshop, artisans, 3), (office, artisans, 4),
                         (workyard, artisans, 5), (granaryYard, artisans, 5), (tradeHouse, artisans, 8)
                     })
            {
                def.workerTier = tier;
                def.workers = count;
            }
            artisans.satisfactionByTaxRate = farmers.satisfactionByTaxRate;
            artisans.nextTier = null;
            artisans.upgradeRequires = new NeedDefinition[0];
            artisans.downgradeSatisfaction = 0.4f;
            artisans.downgradeMonths = 3;

            // Öl für Handwerker: Olivenhain (Bauern) → Oliven → Ölpresse (Handwerker) → Öl
            var olives = Resource("Res_Oliven", "Oliven", antike, false);
            var oil = Resource("Res_Oel", "Öl", antike, false);
            var oliveGrove = ProductionBuilding("Bld_Olivenhain", "Olivenhain", antike, 100f, 2f,
                "Oliven ernten", null, 0f, olives, 3f, 5f, new Color(0.55f, 0.6f, 0.35f));
            oliveGrove.materialCosts = Cost((boards, 2f));
            oliveGrove.workerTier = farmers;
            oliveGrove.workers = 4;
            var oilPress = ProductionBuilding("Bld_Oelpresse", "Ölpresse", antike, 150f, 3f,
                "Öl pressen", olives, 3f, oil, 2f, 5f, new Color(0.75f, 0.65f, 0.4f));
            oilPress.materialCosts = Cost((boards, 4f), (stone, 2f));
            oilPress.workerTier = artisans;
            oilPress.workers = 3;
            oilPress.layers = new[] { Boost(crafts, 1.25f) };

            var oilNeed = GetOrCreate<NeedDefinition>("Need_Oel");
            oilNeed.displayName = "Öl";
            oilNeed.epoch = antike;
            oilNeed.requiredTechnology = null;
            oilNeed.pyramidLevel = 2;
            oilNeed.weight = 1.5f;
            oilNeed.kind = NeedKind.Goods;
            oilNeed.resource = oil;
            oilNeed.amountPerResidentPerMonth = 0.25f;
            oilNeed.providers = new BuildingDefinition[0];
            // Keramik für Handwerker: Lehmgrube (Bauern) → Lehm → Töpferei (Handwerker) → Keramik
            var clay = Resource("Res_Lehm", "Lehm", antike, false);
            var ceramics = Resource("Res_Keramik", "Keramik", antike, false);
            var clayPit = ProductionBuilding("Bld_Lehmgrube", "Lehmgrube", antike, 80f, 1f,
                "Lehm stechen", null, 0f, clay, 2f, 5f, new Color(0.6f, 0.45f, 0.3f));
            clayPit.workerTier = farmers;
            clayPit.workers = 3;
            var pottery = ProductionBuilding("Bld_Toepferei", "Töpferei", antike, 150f, 3f,
                "Töpfern", clay, 2f, ceramics, 1f, 5f, new Color(0.8f, 0.5f, 0.35f));
            pottery.materialCosts = Cost((boards, 4f), (stone, 4f));
            pottery.workerTier = artisans;
            pottery.workers = 4;
            pottery.layers = new[] { Boost(crafts, 1.25f) };

            var ceramicNeed = GetOrCreate<NeedDefinition>("Need_Keramik");
            ceramicNeed.displayName = "Keramik";
            ceramicNeed.epoch = antike;
            ceramicNeed.requiredTechnology = null;
            ceramicNeed.pyramidLevel = 3;
            ceramicNeed.weight = 1f;
            ceramicNeed.kind = NeedKind.Goods;
            ceramicNeed.resource = ceramics;
            ceramicNeed.amountPerResidentPerMonth = 0.15f;
            ceramicNeed.providers = new BuildingDefinition[0];

            artisans.needs = new[] { food, water, marketNeed, faith, oilNeed, ceramicNeed };

            farmers.nextTier = artisans;
            farmers.upgradeSatisfaction = 0.75f;
            farmers.upgradeMonths = 3;
            farmers.upgradeRequires = new[] { education };

            // ---------------------------------------------------------------- Stufe 3: Gelehrte
            // Handwerker steigen in der Gelehrtenzone auf, wenn eine Akademie in Reichweite liegt (GDD: Bildung hebt).
            // Die Akademie bietet höhere Bildung auch ohne Personal; ihre Forschung braucht Gelehrte.

            var scholars = GetOrCreate<PopulationTierDefinition>("Tier_3");
            scholars.tier = 3;
            scholars.displayName = "Gelehrte"; // GDD: „Gelehrte und Priester“, im Spiel kurz
            scholars.namesByEpoch = new[]
            {
                new EpochName { epoch = antike, name = "Gelehrte" },
                new EpochName { epoch = mittelalter, name = "Gelehrte und Kaufleute" }
            };
            scholars.needs = new[] { food, water, marketNeed, faith, oilNeed, ceramicNeed }; // Wein siehe unten
            scholars.taxPerResidentPerMonth = 3f;
            scholars.commerceWeight = 2.5f;
            scholars.satisfactionByTaxRate = farmers.satisfactionByTaxRate;
            scholars.nextTier = null;
            scholars.upgradeRequires = new NeedDefinition[0];
            scholars.downgradeSatisfaction = 0.4f;
            scholars.downgradeMonths = 3;

            var academy = GetOrCreate<BuildingDefinition>("Bld_Akademie");
            academy.displayName = "Akademie";
            academy.epoch = antike;
            academy.requiredTechnology = philosophy;
            academy.category = BuildingCategory.Service;
            academy.size = new Vector2Int(2, 3);
            academy.buildCost = 300f;
            academy.materialCosts = Cost((boards, 6f), (stone, 10f));
            academy.upkeepPerMonth = 5f;
            academy.baseCapacity = 0;
            academy.baseFunctions = new[]
            {
                new BuildingFunction
                {
                    label = "Lehren und forschen",
                    inputs = new[] { new ResourceAmount { resource = coins, amount = 2f } },
                    outputs = new[] { new ResourceAmount { resource = research, amount = 2f } },
                    cycleSeconds = 5f
                }
            };
            academy.layers = new BuildingLayer[0];
            academy.workerTier = scholars;
            academy.workers = 3;
            academy.prefab = KeepModel(academy.prefab, () => CreatePrefab("Prefab_Akademie", academy.size, 2.5f, new Color(0.92f, 0.9f, 0.82f)));

            var higherEducation = GetOrCreate<NeedDefinition>("Need_HoehereBildung");
            higherEducation.displayName = "Höhere Bildung";
            higherEducation.epoch = antike;
            higherEducation.requiredTechnology = philosophy;
            higherEducation.pyramidLevel = 5;
            higherEducation.weight = 1f;
            higherEducation.kind = NeedKind.Service;
            higherEducation.resource = null;
            higherEducation.providers = new[] { academy };
            higherEducation.range = 20f;
            higherEducation.coverageByDistance = AnimationCurve.Linear(0f, 1f, 1f, 0.4f);

            artisans.nextTier = scholars;
            artisans.upgradeSatisfaction = 0.75f;
            artisans.upgradeMonths = 3;
            artisans.upgradeRequires = new[] { higherEducation };
            topResidential.requiredTechnology = philosophy;

            // Wein für Gelehrte: Weinberg (Bauern) → Trauben → Kelterei (Handwerker) → Wein
            var grapes = Resource("Res_Trauben", "Trauben", antike, false);
            var wine = Resource("Res_Wein", "Wein", antike, false);
            // Sortiment des Gewerbes (2026-11-02; wie in Cities: Skylines verkauft Gewerbe Waren, wie in Anno nur den
            // Überschuss über einem Monatsbedarf der Einwohner): Laden und Taberna (Bauern) nur Weizen; die Gebäude mit
            // Handwerkern dazu Öl und Keramik zum 2,5-fachen, das Handelshaus auch Wein zum 3,5-fachen Weizenpreis
            AddSales(workshop, (oil, 2f, 2.5f), (ceramics, 2f, 2.5f));
            AddSales(office, (oil, 3f, 2.5f), (ceramics, 3f, 2.5f));
            AddSales(workyard, (oil, 4f, 2.5f), (ceramics, 4f, 2.5f));
            AddSales(granaryYard, (oil, 3f, 2.5f), (ceramics, 3f, 2.5f));
            AddSales(tradeHouse, (oil, 7f, 2.5f), (ceramics, 7f, 2.5f), (wine, 4f, 3.5f));

            var vineyard = ProductionBuilding("Bld_Weinberg", "Weinberg", antike, 100f, 2f,
                "Trauben lesen", null, 0f, grapes, 3f, 5f, new Color(0.45f, 0.3f, 0.5f));
            vineyard.materialCosts = Cost((boards, 2f));
            vineyard.workerTier = farmers;
            vineyard.workers = 4;
            var winePress = ProductionBuilding("Bld_Kelterei", "Kelterei", antike, 150f, 3f,
                "Keltern", grapes, 3f, wine, 2f, 5f, new Color(0.55f, 0.2f, 0.3f));
            winePress.materialCosts = Cost((boards, 4f), (stone, 2f));
            winePress.workerTier = artisans;
            winePress.workers = 3;
            winePress.layers = new[] { Boost(crafts, 1.25f) };

            var wineNeed = GetOrCreate<NeedDefinition>("Need_Wein");
            wineNeed.displayName = "Wein";
            wineNeed.epoch = antike;
            wineNeed.requiredTechnology = null;
            wineNeed.pyramidLevel = 4;
            wineNeed.weight = 1f;
            wineNeed.kind = NeedKind.Goods;
            wineNeed.resource = wine;
            wineNeed.amountPerResidentPerMonth = 0.2f;
            wineNeed.providers = new BuildingDefinition[0];
            scholars.needs = new[] { food, water, marketNeed, faith, oilNeed, ceramicNeed, wineNeed };

            // ---------------------------------------------------------------- Brennholz (2026-10-31)
            // Holzfäller → Stämme → Holzhacker → Brennholz; alle Stufen brauchen es von Anfang an (Ebene 2 nach Nahrung).
            // Belastet den Wald zusätzlich, der Förster wird früher wichtig. Köhlerei (Holzkohle) folgt im Mittelalter.
            var firewood = Resource("Res_Brennholz", "Brennholz", antike, false);
            var woodChopper = ProductionBuilding("Bld_Holzhacker", "Holzhacker", antike, 60f, 1f,
                "Holz hacken", logs, 1f, firewood, 5f, 5f, new Color(0.55f, 0.4f, 0.25f));
            woodChopper.materialCosts = Cost((boards, 2f));
            woodChopper.workerTier = farmers;
            woodChopper.workers = 2;

            var firewoodNeed = GetOrCreate<NeedDefinition>("Need_Brennholz");
            firewoodNeed.displayName = "Brennholz";
            firewoodNeed.epoch = antike;
            firewoodNeed.requiredTechnology = null;
            firewoodNeed.pyramidLevel = 2;
            firewoodNeed.weight = 1f;
            firewoodNeed.kind = NeedKind.Goods;
            firewoodNeed.resource = firewood;
            firewoodNeed.amountPerResidentPerMonth = 0.1f;
            firewoodNeed.providers = new BuildingDefinition[0];
            farmers.needs = new[] { food, firewoodNeed, water, marketNeed, faith };
            artisans.needs = new[] { food, firewoodNeed, water, marketNeed, faith, oilNeed, ceramicNeed };
            scholars.needs = new[] { food, firewoodNeed, water, marketNeed, faith, oilNeed, ceramicNeed, wineNeed };

            // ---------------------------------------------------------------- Feuer (2026-10-31, Variante a)
            // Brandrisiko je Gebäude (Holzbau höher, Steinbau niedriger); Feuerwache als Dienst mit Reichweite wie in
            // Cities: Skylines. Wie Tempel und Brunnen ohne Arbeitskräfte (Dienste ohne Funktion bremsen sie nicht).
            var fireStation = GetOrCreate<BuildingDefinition>("Bld_Feuerwache");
            fireStation.displayName = "Feuerwache";
            fireStation.epoch = antike;
            fireStation.requiredTechnology = null;
            fireStation.category = BuildingCategory.Service;
            fireStation.size = new Vector2Int(2, 2);
            fireStation.buildCost = 120f;
            fireStation.materialCosts = Cost((boards, 4f), (stone, 4f));
            fireStation.upkeepPerMonth = 2f;
            fireStation.baseCapacity = 0;
            fireStation.baseFunctions = new BuildingFunction[0];
            fireStation.layers = new BuildingLayer[0];
            fireStation.workerTier = null;
            fireStation.workers = 0;
            fireStation.fireProtectionRange = 20f;
            fireStation.fireProtectionByDistance = AnimationCurve.Linear(0f, 1f, 1f, 0.4f);
            fireStation.prefab = KeepModel(fireStation.prefab, () => CreatePrefab("Prefab_Feuerwache", fireStation.size, 2f, new Color(0.7f, 0.3f, 0.25f)));

            // Wunderbau der Antike (2026-11-03): Großes Theater, einmalig, in vier Bauphasen wie die Großbauten in Anno;
            // fertig +10 % Zufriedenheit für alle Wohnhäuser im Umkreis von 40 Straßenfeldern
            var theater = GetOrCreate<BuildingDefinition>("Bld_GrossesTheater");
            theater.displayName = "Großes Theater";
            theater.epoch = antike;
            theater.requiredTechnology = philosophy;
            theater.category = BuildingCategory.Wonder;
            theater.size = new Vector2Int(4, 4);
            theater.buildCost = 1000f;
            theater.materialCosts = new ResourceAmount[0];
            theater.upkeepPerMonth = 5f; // ×3 (UpkeepScale) = 15, erst wenn fertig
            theater.baseCapacity = 0;
            theater.baseFunctions = new BuildingFunction[0];
            theater.layers = new BuildingLayer[0];
            theater.workerTier = null;
            theater.workers = 0;
            theater.isUnique = true;
            theater.requiredTier = scholars;
            theater.requiredTierResidents = 50;
            theater.satisfactionBonus = 0.1f;
            theater.bonusRange = 40f;
            theater.stages = new[]
            {
                Stage("Fundament", 0f, 60f, (stone, 60f), (boards, 30f)),
                Stage("Sitzränge", 0f, 60f, (stone, 150f)),
                Stage("Bühnenhaus", 0f, 60f, (boards, 60f), (ceramics, 40f)),
                Stage("Ausstattung", 2000f, 60f, (oil, 40f), (wine, 30f))
            };
            theater.prefab = KeepModel(theater.prefab, () => CreatePrefab("Prefab_GrossesTheater", theater.size, 2.5f, new Color(0.9f, 0.86f, 0.75f)));

            foreach (var def in new[] { woodcutter, woodChopper, carpentry, forester })
                def.fireRisk = 1.5f;
            foreach (var def in new[] { temple, academy, library, school, quarry, fireStation, field, oilPress, winePress, theater })
                def.fireRisk = 0.5f;
            well.fireRisk = 0f;

            // Reiter der Bauleiste (wie in Anno): die Stufe, deren Kette ein Gebäude dient, nicht die seiner Arbeiter
            mill.menuTier = null; // nur Testgebäude fürs Mittelalter: Reiter „Sonstiges“
            foreach (var def in new[] { field, woodcutter, forester, carpentry, woodChopper, quarry, storage, market, school, well, temple, fireStation })
                def.menuTier = farmers;
            foreach (var def in new[] { oliveGrove, oilPress, clayPit, pottery, library })
                def.menuTier = artisans;
            foreach (var def in new[] { vineyard, winePress, academy, theater })
                def.menuTier = scholars;
            // Ketten einer Stufe erst mit deren Wohnzone baubar; die erste Forschung für die Schrift liefert die Schreibschule
            foreach (var def in new[] { oliveGrove, oilPress, clayPit, pottery, library })
                def.requiredTechnology = writing;
            foreach (var def in new[] { vineyard, winePress })
                def.requiredTechnology = philosophy;

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
                workshopHouse, artisanHouse, artisanRow, workyardHouse, guildHouse,
                food, marketNeed, water, faith, farmers,
                logs, boards, stone, woodcutter, carpentry, quarry, forest, stoneDeposit, mapDef,
                school, education, artisans, upperResidential, olives, oil, oliveGrove, oilPress, oilNeed,
                clay, ceramics, clayPit, pottery, ceramicNeed,
                topResidential, scholarHouse, townVilla, gardenVilla, peristyle, scholars, academy, higherEducation,
                grapes, wine, vineyard, winePress, wineNeed, forester, firewood, woodChopper, firewoodNeed, fireStation, theater
            };

            // Balancing nach dem Antike-Durchlauf (2026-10-31): Geld war nie knapp. Unterhalt aller einzeln gesetzten
            // Gebäude ×3, Erlös der Gewerbegebäude halbiert (die Werte oben sind die Grundwerte davor).
            foreach (var entry in content)
            {
                if (!(entry is BuildingDefinition def)) continue;
                if (def.zone == null) def.upkeepPerMonth *= UpkeepScale;
                else if (def.zone == commercial)
                    foreach (var function in def.baseFunctions)
                        if (function.IsSale)
                            for (int i = 0; i < function.sales.Length; i++)
                                function.sales[i].price *= CommerceIncomeScale;
            }

            // Bauen in Etappen (2026-11-03, wie die Bauzeit in Cities: Skylines): Baustelle nach Größe; Zonengebäude
            // kürzer, Lager und Schreibschule schneller, damit der Start nicht zäh wird; Großbauten nutzen ihre Phasen
            foreach (var entry in content)
            {
                if (!(entry is BuildingDefinition def)) continue;
                int area = def.size.x * def.size.y;
                if (def.HasStages) def.buildSeconds = 0f;
                else if (def.zone != null) def.buildSeconds = 8f + area * 0.5f;          // 1 × 1: 8,5 s … 4 × 4: 16 s
                else if (def == storage || def == school) def.buildSeconds = 12f;
                else def.buildSeconds = area <= 1 ? 10f : area <= 4 ? 20f : 30f;
                EditorUtility.SetDirty(def);
            }

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

        /// <summary>Bauphase eines Großbaus: Name, Geld, Mindestdauer in Spielsekunden und Waren.</summary>
        private static WonderStage Stage(string name, float money, float minSeconds,
            params (ResourceDefinition resource, float amount)[] materials) =>
            new WonderStage { name = name, money = money, minSeconds = minSeconds, materials = Cost(materials) };

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

        /// <summary>
        /// Gewerbe-Funktion „Verkaufen“ (je 5 s) mit Weizen im Sortiment; weitere Waren ergänzt AddSales, sobald es sie
        /// gibt. Preis je Einheit wie bei den früheren Funktionen (Münzen / Weizen), vor CommerceIncomeScale.
        /// </summary>
        private static BuildingFunction[] Sale(ResourceDefinition wheat, float amount, float price) =>
            new[]
            {
                new BuildingFunction
                {
                    label = "Verkaufen",
                    inputs = new ResourceAmount[0],
                    outputs = new ResourceAmount[0],
                    cycleSeconds = 5f,
                    sales = new[] { new SaleOffer { resource = wheat, amount = amount, price = price } }
                }
            };

        /// <summary>Weitere Waren ins Sortiment; Preis = Weizenpreis des Gebäudes × Faktor.</summary>
        private static void AddSales(BuildingDefinition def,
            params (ResourceDefinition resource, float amount, float priceFactor)[] goods)
        {
            var function = def.baseFunctions[0];
            var wheat = function.sales[0];
            var sales = new SaleOffer[1 + goods.Length];
            sales[0] = wheat;
            for (int i = 0; i < goods.Length; i++)
                sales[i + 1] = new SaleOffer
                {
                    resource = goods[i].resource, amount = goods[i].amount, price = wheat.price * goods[i].priceFactor
                };
            function.sales = sales;
        }

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
