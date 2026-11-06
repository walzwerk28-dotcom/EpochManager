using UnityEditor;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Richtet die Natur-Optik ein (Menü EpochCity → Natur einrichten): Material mit dem Shader EpochCity/Gebirge (mit der
    /// Wiesentextur des Bodens), Prefabs aus Gebirge.fbx (Steineichen, Pinien, Aleppokiefern, Zypressen, Macchia, Felsen),
    /// entfernt die Kachel aus Dep_Berg, trägt die Bäume in Dep_Wald ein und verdrahtet in der offenen Szene
    /// MountainVisuals und die Kamera. Die FBX-Datei entsteht mit Blender/scripts/gebirge.py.
    /// Mehrfaches Ausführen überschreibt nur die Werte.
    /// </summary>
    public static class MountainSetup
    {
        private const string Root = "Assets/Content/Mountains";
        private const string Models = "Assets/Content/Models";

        [MenuItem("EpochCity/Natur einrichten")]
        public static void Run()
        {
            if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets/Content", "Mountains");

            var material = GetOrCreateMaterial("Mat_Berg", "EpochCity/Gebirge");
            material.SetColor("_HazeColor", new Color(0.74f, 0.81f, 0.86f));
            material.SetColor("_Tint", Color.white);
            material.SetFloat("_Ambient", 1f);
            material.SetTexture("_GroundTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Content/Look/Tex_Ground.png"));
            EditorUtility.SetDirty(material);

            var oakA = MakePrefab("Gebirge", "Steineiche_A");
            var oakB = MakePrefab("Gebirge", "Steineiche_B");
            var pineA = MakePrefab("Gebirge", "Pinie_A");
            var pineB = MakePrefab("Gebirge", "Pinie_B");
            var pineC = MakePrefab("Gebirge", "Pinie_C");
            var aleppoA = MakePrefab("Gebirge", "Aleppokiefer_A");
            var aleppoB = MakePrefab("Gebirge", "Aleppokiefer_B");
            var cypA = MakePrefab("Gebirge", "Zypresse_A");
            var cypB = MakePrefab("Gebirge", "Zypresse_B");
            var bushes = MakePrefabs("Macchia_A", "Macchia_B", "Macchia_C");
            var rocks = MakePrefabs("Fels_A", "Fels_B", "Fels_C", "Fels_D");

            var oaks = new[] { oakA, oakB };
            var pines = new[] { pineA, pineB, pineC, aleppoA, aleppoB };
            var cypresses = new[] { cypA, cypB };

            var mountain = AssetDatabase.LoadAssetAtPath<DepositDefinition>("Assets/Content/Test/Dep_Berg.asset");
            if (mountain != null)
            {
                mountain.tilePrefab = null;
                EditorUtility.SetDirty(mountain);
            }

            // Wald der Karte: Steineichen, Pinien, Aleppokiefern und einzelne Zypressen (Gewichte über Wiederholung)
            var forest = AssetDatabase.LoadAssetAtPath<DepositDefinition>("Assets/Content/Test/Dep_Wald.asset");
            if (forest != null)
            {
                forest.decorPrefabs = new[] { oakA, oakA, oakB, oakB, pineA, pineB, pineC, aleppoA, aleppoB, cypA };
                forest.decorPerCell = new Vector2Int(3, 4);
                forest.decorScale = new Vector2(0.8f, 1.15f);
                EditorUtility.SetDirty(forest);
            }
            AssetDatabase.SaveAssets();

            WireScene(material, oaks, pines, cypresses, bushes, rocks);
            Debug.Log("Natur eingerichtet: Material, Prefabs, Wald, Szene verdrahtet.");
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

        private static GameObject[] MakePrefabs(params string[] names)
        {
            var list = new GameObject[names.Length];
            for (int i = 0; i < names.Length; i++) list[i] = MakePrefab("Gebirge", names[i]);
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

        private static void WireScene(Material material, GameObject[] oaks, GameObject[] pines, GameObject[] cypresses,
            GameObject[] bushes, GameObject[] rocks)
        {
            var city = Object.FindAnyObjectByType<CityController>();
            var depositVisuals = Object.FindAnyObjectByType<DepositVisuals>();
            var camera = Object.FindAnyObjectByType<CityCameraController>();
            if (city == null || depositVisuals == null)
            {
                Debug.LogWarning("Szene nicht verdrahtet: CityController oder DepositVisuals fehlt (Szene öffnen).");
                return;
            }

            var visuals = depositVisuals.GetComponent<MountainVisuals>();
            if (visuals == null) visuals = depositVisuals.gameObject.AddComponent<MountainVisuals>();
            var so = new SerializedObject(visuals);
            so.FindProperty("city").objectReferenceValue = city;
            so.FindProperty("mountainMaterial").objectReferenceValue = material;
            SetArray(so.FindProperty("oaks"), oaks);
            SetArray(so.FindProperty("pines"), pines);
            SetArray(so.FindProperty("cypresses"), cypresses);
            SetArray(so.FindProperty("bushes"), bushes);
            SetArray(so.FindProperty("rocks"), rocks);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(visuals);

            if (camera != null)
            {
                var cam = new SerializedObject(camera);
                cam.FindProperty("mountains").objectReferenceValue = visuals;
                cam.ApplyModifiedProperties();
                EditorUtility.SetDirty(camera);
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(visuals.gameObject.scene);
        }

        private static void SetArray(SerializedProperty property, GameObject[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
