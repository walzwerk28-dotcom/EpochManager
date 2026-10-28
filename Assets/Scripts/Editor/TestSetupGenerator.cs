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
            field.prefab = CreatePrefab("Prefab_Feld", field.size, 0.3f, new Color(0.85f, 0.75f, 0.3f));

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
            market.prefab = CreatePrefab("Prefab_Markt", market.size, 2f, new Color(0.75f, 0.35f, 0.25f));

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
                    inputs = new[] { new ResourceAmount { resource = coins, amount = 5f } },
                    outputs = new[] { new ResourceAmount { resource = research, amount = 5f } },
                    cycleSeconds = 5f
                }
            };
            library.layers = new BuildingLayer[0];
            library.prefab = CreatePrefab("Prefab_Bibliothek", library.size, 3f, new Color(0.35f, 0.45f, 0.8f));

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
            mill.prefab = CreatePrefab("Prefab_Muehle", mill.size, 4f, new Color(0.6f, 0.6f, 0.55f));

            var path = GetOrCreate<RoadDefinition>("Road_Trampelpfad");
            path.displayName = "Trampelpfad";
            path.epoch = antike;
            path.costPerCell = 5f;
            path.upkeepPerCellPerMonth = 0.1f;
            path.quality = 1f;
            // Füllt das Feld ganz aus, damit die Straße ohne Lücken durchläuft
            path.prefab = CreatePrefab("Prefab_Trampelpfad", Vector2Int.one, 0.05f, new Color(0.55f, 0.45f, 0.32f), 1f);

            var storage = GetOrCreate<BuildingDefinition>("Bld_Lager");
            storage.displayName = "Lager";
            storage.epoch = antike;
            storage.category = BuildingCategory.Storage;
            storage.size = new Vector2Int(2, 2);
            storage.buildCost = 120f;
            storage.upkeepPerMonth = 2f;
            storage.baseCapacity = 0;
            storage.baseFunctions = new BuildingFunction[0];
            storage.layers = new BuildingLayer[0];
            storage.catchmentRange = 20f;
            storage.efficiencyByDistance = AnimationCurve.Linear(0f, 1f, 1f, 0.25f);
            storage.minEfficiency = 0.25f;
            storage.prefab = CreatePrefab("Prefab_Lager", storage.size, 1.5f, new Color(0.5f, 0.38f, 0.25f));

            var residential = GetOrCreate<ZoneDefinition>("Zone_Wohnen");
            residential.displayName = "Wohnzone";
            residential.tilePrefab =
                CreatePrefab("Prefab_Zone_Wohnen", Vector2Int.one, 0.02f, new Color(0.45f, 0.72f, 0.4f), 0.96f);

            var commercial = GetOrCreate<ZoneDefinition>("Zone_Gewerbe");
            commercial.displayName = "Gewerbezone";
            commercial.tilePrefab =
                CreatePrefab("Prefab_Zone_Gewerbe", Vector2Int.one, 0.02f, new Color(0.38f, 0.55f, 0.85f), 0.96f);

            var house = CreateZoneBuilding("Bld_Wohnhaus", "Wohnhaus", antike, BuildingCategory.Housing, residential,
                6, new BuildingFunction[0], 1f, new Color(0.85f, 0.8f, 0.7f));

            var shop = CreateZoneBuilding("Bld_Laden", "Laden", antike, BuildingCategory.Commerce, commercial, 0,
                new[]
                {
                    new BuildingFunction
                    {
                        label = "Weizen verkaufen",
                        inputs = new[] { new ResourceAmount { resource = wheat, amount = 2f } },
                        outputs = new[] { new ResourceAmount { resource = coins, amount = 4f } },
                        cycleSeconds = 5f
                    }
                }, 1.2f, new Color(0.55f, 0.65f, 0.9f));

            // Nur zum Testen des Epochenbezugs: im Mittelalter wachsen Fachwerkhäuser statt Wohnhäusern
            var timberHouse = CreateZoneBuilding("Bld_Fachwerkhaus", "Fachwerkhaus", mittelalter,
                BuildingCategory.Housing, residential, 10, new BuildingFunction[0], 1.6f, new Color(0.9f, 0.88f, 0.8f));

            foreach (var asset in new Object[]
                     { antike, mittelalter, coins, wheat, research, irrigation, writing, minting,
                       field, market, library, mill, path, storage, residential, commercial,
                       house, shop, timberHouse })
                EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            Debug.Log($"Testaufbau erzeugt unter {Root}.");
        }

        /// <summary>Zonengebäude: 1 × 1, kostenlos, ohne Unterhalt, wächst in der angegebenen Zone.</summary>
        private static BuildingDefinition CreateZoneBuilding(string assetName, string displayName,
            EpochDefinition epoch, BuildingCategory category, ZoneDefinition zone, int capacity,
            BuildingFunction[] functions, float height, Color color)
        {
            var def = GetOrCreate<BuildingDefinition>(assetName);
            def.displayName = displayName;
            def.epoch = epoch;
            def.category = category;
            def.zone = zone;
            def.size = Vector2Int.one;
            def.buildCost = 0f;
            def.upkeepPerMonth = 0f;
            def.baseCapacity = capacity;
            def.baseFunctions = functions;
            def.layers = new BuildingLayer[0];
            def.prefab = CreatePrefab("Prefab_" + assetName.Substring(4), def.size, height, color, 0.8f);
            return def;
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
        private static GameObject CreatePrefab(string name, Vector2Int size, float height, Color color,
            float footprint = 0.9f)
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
            EditorUtility.SetDirty(material);

            var root = new GameObject(name);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale =
                new Vector3(size.x * cellSize * footprint, height, size.y * cellSize * footprint);
            body.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            body.GetComponent<Renderer>().sharedMaterial = material;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Root}/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }
    }
}
