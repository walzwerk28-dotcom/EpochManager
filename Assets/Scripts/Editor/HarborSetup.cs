using UnityEditor;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Richtet das Handelsschiff ein (Menü EpochCity → Hafen-Schiff einrichten): Prefab Assets/Content/Water/
    /// Prefab_Schiff.prefab aus Schiff.fbx (Blender/scripts/schiff.py; Bug nach +z, Wasserlinie bei y = 0) und
    /// verdrahtet in der offenen Szene HarborVisuals auf dem Objekt mit ConstructionVisuals. Mehrfaches Ausführen
    /// überschreibt nur die Werte.
    /// </summary>
    public static class HarborSetup
    {
        private const string Root = "Assets/Content/Water";
        private const string Fbx = "Assets/Content/Models/Schiff.fbx";

        [MenuItem("EpochCity/Hafen-Schiff einrichten")]
        public static void Run()
        {
            var prefab = MakePrefab();
            WireScene(prefab);
            Debug.Log("Hafen-Schiff eingerichtet: Prefab und Szene.");
        }

        private static GameObject MakePrefab()
        {
            if (AssetImporter.GetAtPath(Fbx) is ModelImporter importer)
            {
                importer.globalScale = 1f;
                importer.useFileScale = true;
                importer.importCameras = false;
                importer.importLights = false;
                importer.importAnimation = false;
                importer.animationType = ModelImporterAnimationType.None;
                importer.SaveAndReimport();
            }
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
            if (model == null) throw new System.Exception($"FBX fehlt: {Fbx}");
            Transform source = null;
            foreach (Transform child in model.transform)
                if (child.name == "Schiff") source = child;
            if (source == null && model.GetComponent<MeshFilter>() != null) source = model.transform;   // ein Objekt: die Wurzel ist das Mesh
            if (source == null) throw new System.Exception("Objekt Schiff fehlt in der FBX");

            var root = new GameObject("Prefab_Schiff");
            var mesh = new GameObject("Schiff", typeof(MeshFilter), typeof(MeshRenderer));
            mesh.transform.SetParent(root.transform, false);
            mesh.transform.localPosition = source.localPosition;
            mesh.transform.localRotation = source.localRotation;
            mesh.transform.localScale = source.localScale;
            mesh.GetComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
            var renderer = mesh.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Root}/Prefab_Schiff.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void WireScene(GameObject prefab)
        {
            var construction = Object.FindAnyObjectByType<ConstructionVisuals>();
            var city = Object.FindAnyObjectByType<CityController>();
            var trade = Object.FindAnyObjectByType<TradeSystem>();
            var clock = Object.FindAnyObjectByType<GameClock>();
            var water = Object.FindAnyObjectByType<WaterVisuals>();
            if (construction == null || city == null || trade == null)
            {
                Debug.LogWarning("Szene nicht verdrahtet: ConstructionVisuals, CityController oder TradeSystem fehlt (Szene öffnen).");
                return;
            }
            var visuals = construction.GetComponent<HarborVisuals>();
            if (visuals == null) visuals = construction.gameObject.AddComponent<HarborVisuals>();
            var so = new SerializedObject(visuals);
            so.FindProperty("city").objectReferenceValue = city;
            so.FindProperty("trade").objectReferenceValue = trade;
            so.FindProperty("clock").objectReferenceValue = clock;
            so.FindProperty("shipPrefab").objectReferenceValue = prefab;
            so.FindProperty("water").objectReferenceValue = water;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(visuals);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(visuals.gameObject.scene);
        }
    }
}
