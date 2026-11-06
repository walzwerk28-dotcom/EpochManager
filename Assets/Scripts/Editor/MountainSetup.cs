using UnityEditor;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Richtet die Berg-Optik ein (Menü EpochCity → Berge einrichten): Material mit dem Shader EpochCity/Gebirge,
    /// Prefabs aus Gebirge.fbx (Pinien, Zypresse, Macchia, Felsen), entfernt die Kachel aus Dep_Berg und verdrahtet
    /// in der offenen Szene MountainVisuals und die Kamera. Die FBX-Datei entsteht mit Blender/scripts/gebirge.py.
    /// Mehrfaches Ausführen überschreibt nur die Werte.
    /// </summary>
    public static class MountainSetup
    {
        private const string Root = "Assets/Content/Mountains";
        private const string Models = "Assets/Content/Models";

        [MenuItem("EpochCity/Berge einrichten")]
        public static void Run()
        {
            if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets/Content", "Mountains");

            var material = GetOrCreateMaterial("Mat_Berg", "EpochCity/Gebirge");
            material.SetColor("_HazeColor", new Color(0.74f, 0.81f, 0.86f));
            material.SetColor("_Tint", Color.white);
            material.SetFloat("_Ambient", 1f);
            EditorUtility.SetDirty(material);

            var pines = MakePrefabs("Pinie_A", "Pinie_B");
            var cypresses = MakePrefabs("Zypresse_A");
            var bushes = MakePrefabs("Macchia_A", "Macchia_B");
            var rocks = MakePrefabs("Fels_A", "Fels_B", "Fels_C");

            var deposit = AssetDatabase.LoadAssetAtPath<DepositDefinition>("Assets/Content/Test/Dep_Berg.asset");
            if (deposit != null)
            {
                deposit.tilePrefab = null;
                EditorUtility.SetDirty(deposit);
            }
            AssetDatabase.SaveAssets();

            WireScene(material, pines, cypresses, bushes, rocks);
            Debug.Log("Berge eingerichtet: Material, Prefabs, Szene verdrahtet.");
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

        private static void WireScene(Material material, GameObject[] pines, GameObject[] cypresses, GameObject[] bushes,
            GameObject[] rocks)
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
