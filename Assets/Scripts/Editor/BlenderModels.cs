using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace EpochCity
{
    /// <summary>
    /// Bindet Gebäudemodelle aus Blender ein (entschieden 2026-11-03: neue Modelle entstehen in Blender). Die Scripts
    /// unter Blender/scripts/ bauen die Modelle und exportieren sie als FBX nach Assets/Content/Models/; dieser Schritt
    /// (Menü EpochCity → Blender-Modelle einbinden) setzt daraus je Gebäude ein Prefab unter Assets/Content/Buildings/
    /// zusammen: das Modell und eine Bodenplatte wie bei den übrigen Gebäuden (98 % der Grundfläche, eine Texturkachel
    /// je Rasterfeld), und trägt es in die Gebäudedaten ein. Vorhandene Prefabs werden am selben Pfad ersetzt, damit
    /// Verweise erhalten bleiben.
    /// </summary>
    public static class BlenderModels
    {
        private const float CellSize = 2f;       // Standardwert von CityController.cellSize
        private const float GroundFill = 0.98f;  // wie KenneyBuildingFactory
        private const float GroundHeight = 0.015f;
        private const string Out = "Assets/Content/Buildings";

        private struct Entry
        {
            public string fbx, prefab, building, ground;
            public Vector2Int cells;
        }

        private static readonly Entry[] Models =
        {
            new Entry
            {
                fbx = "Assets/Content/Models/Holzfaeller.fbx", prefab = "Holzfaeller", building = "Bld_Holzfaeller",
                cells = new Vector2Int(2, 2), ground = "Mat_Boden_Erde"
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Markt.fbx", prefab = "Markt", building = "Bld_Markt",
                cells = new Vector2Int(2, 4), ground = "Mat_Boden_Pflaster"
            }
        };

        [MenuItem("EpochCity/Blender-Modelle einbinden")]
        public static void Import()
        {
            foreach (var e in Models)
            {
                ConfigureImporter(e.fbx);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(e.fbx);
                if (model == null)
                {
                    Debug.LogWarning($"Blender-Modell fehlt: {e.fbx} (Script unter Blender/scripts/ ausführen)");
                    continue;
                }

                var root = new GameObject(e.prefab);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                instance.name = "Model";
                // Blender baut mit der Vorderseite nach −Y; nach dem FBX-Import zeigt sie nach +z. Gebäude zeigen im
                // Spiel mit der Vorderseite nach −z (zur Straße), daher eine halbe Drehung (keine Spiegelung).
                instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                AddGroundPlate(root, e.cells, AssetDatabase.LoadAssetAtPath<Material>($"{Out}/{e.ground}.mat"));
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Out}/{e.prefab}.prefab");
                Object.DestroyImmediate(root);

                var def = AssetDatabase.LoadAssetAtPath<BuildingDefinition>($"Assets/Content/Test/{e.building}.asset");
                if (def != null)
                {
                    def.prefab = prefab;
                    EditorUtility.SetDirty(def);
                }
                var bounds = MeasureModel(model);
                Debug.Log($"{e.prefab}: {bounds.size.x:0.00} × {bounds.size.y:0.00} × {bounds.size.z:0.00} m " +
                          $"(Grundfläche {e.cells.x * CellSize:0} × {e.cells.y * CellSize:0} m)");
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>Import wie bei Gebäuden üblich: 1 Einheit = 1 m, keine Kameras, Lichter oder Animation.</summary>
        private static void ConfigureImporter(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is ModelImporter importer)) return;
            bool changed = importer.globalScale != 1f || importer.importCameras || importer.importLights ||
                           importer.importAnimation || importer.animationType != ModelImporterAnimationType.None;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            if (changed) importer.SaveAndReimport();
        }

        private static Bounds MeasureModel(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<MeshRenderer>();
            var bounds = new Bounds();
            for (int i = 0; i < renderers.Length; i++)
            {
                var mesh = renderers[i].GetComponent<MeshFilter>().sharedMesh;
                var b = mesh.bounds;
                var t = renderers[i].transform;
                var world = new Bounds(t.TransformPoint(b.center), Vector3.Scale(b.size, t.lossyScale));
                if (i == 0) bounds = world;
                else bounds.Encapsulate(world);
            }
            return bounds;
        }

        /// <summary>Bodenplatte als eigenes Objekt; das Mesh wird je Grundfläche einmal gespeichert.</summary>
        private static void AddGroundPlate(GameObject root, Vector2Int cells, Material material)
        {
            float halfW = cells.x * CellSize * GroundFill * 0.5f, halfD = cells.y * CellSize * GroundFill * 0.5f;
            var mesh = new Mesh { name = $"Boden_{cells.x}x{cells.y}" };
            var corners = new[]
            {
                new Vector3(-halfW, GroundHeight, -halfD), new Vector3(halfW, GroundHeight, -halfD),
                new Vector3(-halfW, GroundHeight, halfD), new Vector3(halfW, GroundHeight, halfD)
            };
            mesh.vertices = corners;
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.uv = System.Array.ConvertAll(corners, c => new Vector2(c.x / CellSize, c.z / CellSize));
            mesh.tangents = new[] { new Vector4(1, 0, 0, -1), new Vector4(1, 0, 0, -1), new Vector4(1, 0, 0, -1), new Vector4(1, 0, 0, -1) };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 }; // von oben im Uhrzeigersinn, Fläche zeigt nach oben
            mesh.RecalculateBounds();
            mesh = KenneyBuildingFactory.SaveMeshAsset(mesh, $"{Out}/Meshes/Boden_{cells.x}x{cells.y}.asset");

            var plate = new GameObject("Boden", typeof(MeshFilter), typeof(MeshRenderer));
            plate.transform.SetParent(root.transform, false);
            plate.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = plate.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}
