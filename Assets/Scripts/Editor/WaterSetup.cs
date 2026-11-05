using UnityEditor;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Richtet die Wasser-Optik ein (Menü EpochCity → Wasser einrichten): Materialien für Wasser und Sandbett,
    /// Prefabs aus Ufer.fbx (Schilf, Uferstein, Seerose) und Bruecke.fbx (Mittelstück, Geländer, Arme), entfernt die
    /// Wasserkachel aus Dep_Wasser und verdrahtet in der offenen Szene WaterVisuals und RoadVisuals. Die FBX-Dateien
    /// entstehen mit Blender/scripts/ufer.py und bruecke.py. Mehrfaches Ausführen überschreibt nur die Werte.
    /// </summary>
    public static class WaterSetup
    {
        private const string Root = "Assets/Content/Water";
        private const string Models = "Assets/Content/Models";

        // Farben (sRGB) des Sandbetts und des Wassers
        private static readonly Color Sand = new Color(0.70f, 0.65f, 0.52f);

        [MenuItem("EpochCity/Wasser einrichten")]
        public static void Run()
        {
            if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets/Content", "Water");

            var water = GetOrCreateMaterial("Mat_Wasser", "EpochCity/Wasser");
            water.SetColor("_ShallowColor", new Color(0.22f, 0.56f, 0.58f));
            water.SetColor("_DeepColor", new Color(0.07f, 0.34f, 0.46f));
            water.SetColor("_FoamColor", new Color(0.92f, 0.97f, 0.95f));
            water.SetColor("_SkyColor", new Color(0.62f, 0.80f, 0.92f));
            water.SetFloat("_ShallowAlpha", 0.78f);
            water.SetFloat("_DeepAlpha", 0.94f);
            water.SetFloat("_FoamWidth", 0.10f);
            water.SetFloat("_WaveScale", 1.1f);
            water.SetFloat("_WaveSpeed", 0.18f);
            water.SetFloat("_WaveStrength", 0.3f);
            water.SetFloat("_Glossiness", 220f);
            EditorUtility.SetDirty(water);
            var shore = GetOrCreateMaterial("Mat_Ufer", "Universal Render Pipeline/Lit");
            shore.SetColor("_BaseColor", Sand);
            shore.SetFloat("_Smoothness", 0.08f);
            shore.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(shore);

            var reeds = MakePrefabs("Ufer", "Schilf_A", "Schilf_B", "Schilf_C");
            var stones = MakePrefabs("Ufer", "Ufersteine_A", "Ufersteine_B");
            var lilies = MakePrefabs("Ufer", "Seerose_A", "Seerose_B");
            var center = MakePrefab("Bruecke", "Mitte");
            var rail = MakePrefab("Bruecke", "Rand");
            var armFlat = MakePrefab("Bruecke", "ArmFlach");
            var armRamp = MakePrefab("Bruecke", "ArmRampe");

            var deposit = AssetDatabase.LoadAssetAtPath<DepositDefinition>("Assets/Content/Test/Dep_Wasser.asset");
            if (deposit != null)
            {
                deposit.tilePrefab = null;
                EditorUtility.SetDirty(deposit);
            }
            AssetDatabase.SaveAssets();

            WireScene(water, shore, reeds, stones, lilies, center, rail, armFlat, armRamp);
            Debug.Log("Wasser eingerichtet: Materialien, Prefabs, Szene verdrahtet.");
        }

        private static Material GetOrCreateMaterial(string name, string shaderName)
        {
            string path = $"{Root}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find(shaderName);
            if (shader == null) throw new System.Exception($"Shader nicht gefunden: {shaderName}");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else material.shader = shader;
            return material;
        }

        private static void ConfigureImporter(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is ModelImporter importer)) return;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.SaveAndReimport();
        }

        private static GameObject[] MakePrefabs(string fbx, params string[] names)
        {
            var list = new GameObject[names.Length];
            for (int i = 0; i < names.Length; i++) list[i] = MakePrefab(fbx, names[i]);
            return list;
        }

        /// <summary>Prefab aus einem Objekt der FBX: ein Kind mit Mesh und Materialien, Drehung wie im Modell.</summary>
        private static GameObject MakePrefab(string fbxName, string part)
        {
            string fbx = $"{Models}/{fbxName}.fbx";
            ConfigureImporter(fbx);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            if (model == null) throw new System.Exception($"FBX fehlt: {fbx}");
            Transform source = null;
            foreach (Transform child in model.transform)
                if (child.name == part) source = child;
            if (source == null) throw new System.Exception($"Objekt {part} fehlt in {fbx}");

            var root = new GameObject($"Prefab_{part}");
            var mesh = new GameObject(part, typeof(MeshFilter), typeof(MeshRenderer));
            mesh.transform.SetParent(root.transform, false);
            mesh.transform.localPosition = source.localPosition;
            mesh.transform.localRotation = source.localRotation;
            mesh.transform.localScale = source.localScale;
            mesh.GetComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
            var renderer = mesh.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Root}/Prefab_{part}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void WireScene(Material water, Material shore, GameObject[] reeds, GameObject[] stones,
            GameObject[] lilies, GameObject center, GameObject rail, GameObject armFlat, GameObject armRamp)
        {
            var city = Object.FindAnyObjectByType<CityController>();
            var depositVisuals = Object.FindAnyObjectByType<DepositVisuals>();
            var roadVisuals = Object.FindAnyObjectByType<RoadVisuals>();
            var overlay = Object.FindAnyObjectByType<ConnectionOverlay>();
            if (city == null || depositVisuals == null || roadVisuals == null)
            {
                Debug.LogWarning("Szene nicht verdrahtet: CityController, DepositVisuals oder RoadVisuals fehlt (Szene öffnen).");
                return;
            }

            var visuals = depositVisuals.GetComponent<WaterVisuals>();
            if (visuals == null) visuals = depositVisuals.gameObject.AddComponent<WaterVisuals>();
            var so = new SerializedObject(visuals);
            so.FindProperty("city").objectReferenceValue = city;
            so.FindProperty("waterMaterial").objectReferenceValue = water;
            so.FindProperty("shoreMaterial").objectReferenceValue = shore;
            SetArray(so.FindProperty("reeds"), reeds);
            SetArray(so.FindProperty("shoreStones"), stones);
            SetArray(so.FindProperty("lilies"), lilies);
            so.FindProperty("lilyChance").floatValue = 0.24f;
            so.ApplyModifiedProperties();

            var road = new SerializedObject(roadVisuals);
            road.FindProperty("overlay").objectReferenceValue = overlay;
            road.FindProperty("bridgeCenter").objectReferenceValue = center;
            road.FindProperty("bridgeRail").objectReferenceValue = rail;
            road.FindProperty("bridgeArmFlat").objectReferenceValue = armFlat;
            road.FindProperty("bridgeArmRamp").objectReferenceValue = armRamp;
            road.ApplyModifiedProperties();

            EditorUtility.SetDirty(visuals);
            EditorUtility.SetDirty(roadVisuals);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(visuals.gameObject.scene);
        }

        private static void SetArray(SerializedProperty property, GameObject[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
