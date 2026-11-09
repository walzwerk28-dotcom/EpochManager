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
            /// <summary>Die FBX enthält je Variante ein Objekt (nach Namen sortiert); das Prefab bekommt RandomVariant.</summary>
            public bool variants;
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
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Lager.fbx", prefab = "Lager", building = "Bld_Lager",
                cells = new Vector2Int(2, 2), ground = "Mat_Boden_Erde"
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Wohnhaus.fbx", prefab = "Wohnhaus", building = "Bld_Wohnhaus",
                cells = new Vector2Int(1, 1), ground = "Mat_Boden_Kies", variants = true
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Schmalhaus.fbx", prefab = "Schmalhaus", building = "Bld_Schmalhaus",
                cells = new Vector2Int(1, 2), ground = "Mat_Boden_Kies", variants = true
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Stadthaus.fbx", prefab = "Stadthaus", building = "Bld_Stadthaus",
                cells = new Vector2Int(2, 2), ground = "Mat_Boden_Kies", variants = true
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Hofhaus.fbx", prefab = "Hofhaus", building = "Bld_Hofhaus",
                cells = new Vector2Int(2, 3), ground = "Mat_Boden_Kies", variants = true
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Langhaus.fbx", prefab = "Langhaus", building = "Bld_Langhaus",
                cells = new Vector2Int(2, 4), ground = "Mat_Boden_Kies", variants = true
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Domus.fbx", prefab = "Domus", building = "Bld_Domus",
                cells = new Vector2Int(3, 3), ground = "Mat_Boden_Kies", variants = true
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Insula.fbx", prefab = "Insula", building = "Bld_Insula",
                cells = new Vector2Int(4, 4), ground = "Mat_Boden_Kies", variants = true
            },
            new Entry
            {
                fbx = "Assets/Content/Models/LagerMittel.fbx", prefab = "LagerMittel", building = "Bld_LagerMittel",
                cells = new Vector2Int(2, 3), ground = "Mat_Boden_Erde"
            },
            new Entry
            {
                fbx = "Assets/Content/Models/LagerGross.fbx", prefab = "LagerGross", building = "Bld_LagerGross",
                cells = new Vector2Int(3, 3), ground = "Mat_Boden_Erde"
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Gutshof.fbx", prefab = "Gutshof", building = "Bld_Gutshof",
                cells = new Vector2Int(2, 2), ground = "Mat_Boden_Erde"
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Kraeutergarten.fbx", prefab = "Kraeutergarten", building = "Bld_Kraeutergarten",
                cells = new Vector2Int(1, 1), ground = "Mat_Boden_Kies"
            },
            new Entry
            {
                fbx = "Assets/Content/Models/HeiligerHain.fbx", prefab = "HeiligerHain", building = "Bld_HeiligerHain",
                cells = new Vector2Int(2, 2), ground = "Mat_Boden_Erde"
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Saeulengarten.fbx", prefab = "Saeulengarten", building = "Bld_Saeulengarten",
                cells = new Vector2Int(2, 2), ground = "Mat_Boden_Kies"
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Fischer.fbx", prefab = "Fischer", building = "Bld_Fischer",
                cells = new Vector2Int(2, 2), ground = "Mat_Boden_Erde"
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Hafen.fbx", prefab = "Hafen", building = "Bld_Hafen",
                cells = new Vector2Int(3, 3), ground = "Mat_Boden_Pflaster"
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Tempel.fbx", prefab = "Tempel", building = "Bld_Tempel",
                cells = new Vector2Int(2, 3), ground = "Mat_Boden_Pflaster"
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Brunnen.fbx", prefab = "Brunnen", building = "Bld_Brunnen",
                cells = new Vector2Int(1, 1), ground = "Mat_Boden_Pflaster"
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Feuerwache.fbx", prefab = "Feuerwache", building = "Bld_Feuerwache",
                cells = new Vector2Int(2, 2), ground = "Mat_Boden_Pflaster"
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Schreibschule.fbx", prefab = "Schreibschule", building = "Bld_Schreibschule",
                cells = new Vector2Int(2, 2), ground = "Mat_Boden_Pflaster"
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Bibliothek.fbx", prefab = "Bibliothek", building = "Bld_Bibliothek",
                cells = new Vector2Int(2, 2), ground = "Mat_Boden_Pflaster"
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Akademie.fbx", prefab = "Akademie", building = "Bld_Akademie",
                cells = new Vector2Int(2, 3), ground = "Mat_Boden_Pflaster"
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Holzhacker.fbx", prefab = "Holzhacker", building = "Bld_Holzhacker",
                cells = new Vector2Int(2, 2), ground = "Mat_Boden_Erde"
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Foerster.fbx", prefab = "Foerster", building = "Bld_Foerster",
                cells = new Vector2Int(2, 2), ground = "Mat_Boden_Erde"
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Zimmerei.fbx", prefab = "Zimmerei", building = "Bld_Zimmerei",
                cells = new Vector2Int(2, 2), ground = "Mat_Boden_Erde"
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Steinbruch.fbx", prefab = "Steinbruch", building = "Bld_Steinbruch",
                cells = new Vector2Int(2, 2), ground = "Mat_Boden_Stein"
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Olivenhain.fbx", prefab = "Olivenhain", building = "Bld_Olivenhain",
                cells = new Vector2Int(2, 2), ground = "Mat_Boden_Erde"
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Oelpresse.fbx", prefab = "Oelpresse", building = "Bld_Oelpresse",
                cells = new Vector2Int(2, 2), ground = "Mat_Boden_Pflaster"
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Lehmgrube.fbx", prefab = "Lehmgrube", building = "Bld_Lehmgrube",
                cells = new Vector2Int(2, 2), ground = "Mat_Boden_Erde"
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Toepferei.fbx", prefab = "Toepferei", building = "Bld_Toepferei",
                cells = new Vector2Int(2, 2), ground = "Mat_Boden_Pflaster"
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Weinberg.fbx", prefab = "Weinberg", building = "Bld_Weinberg",
                cells = new Vector2Int(2, 2), ground = "Mat_Boden_Erde"
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Kelterei.fbx", prefab = "Kelterei", building = "Bld_Kelterei",
                cells = new Vector2Int(2, 2), ground = "Mat_Boden_Pflaster"
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Laden.fbx", prefab = "Laden", building = "Bld_Laden",
                cells = new Vector2Int(1, 1), ground = "Mat_Boden_Pflaster", variants = true
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Taberna.fbx", prefab = "Taberna", building = "Bld_Taberna",
                cells = new Vector2Int(1, 2), ground = "Mat_Boden_Pflaster", variants = true
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Werkstatt.fbx", prefab = "Werkstatt", building = "Bld_Werkstatt",
                cells = new Vector2Int(2, 2), ground = "Mat_Boden_Erde", variants = true
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Kontor.fbx", prefab = "Kontor", building = "Bld_Kontor",
                cells = new Vector2Int(2, 3), ground = "Mat_Boden_Pflaster", variants = true
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Werkstatthof.fbx", prefab = "Werkstatthof", building = "Bld_Werkstatthof",
                cells = new Vector2Int(3, 3), ground = "Mat_Boden_Erde", variants = true
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Speicherhof.fbx", prefab = "Speicherhof", building = "Bld_Speicherhof",
                cells = new Vector2Int(2, 4), ground = "Mat_Boden_Erde", variants = true
            },
            new Entry
            {
                fbx = "Assets/Content/Models/Handelshaus.fbx", prefab = "Handelshaus", building = "Bld_Handelshaus",
                cells = new Vector2Int(4, 4), ground = "Mat_Boden_Pflaster", variants = true
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
                var ground = AssetDatabase.LoadAssetAtPath<Material>($"{Out}/{e.ground}.mat");
                if (ground == null && e.ground == "Mat_Boden_Stein") ground = KenneyBuildingFactory.EnsureStoneGround();
                if (e.variants) AddVariants(root, model, e.cells, ground);
                else
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                    instance.name = "Model";
                    // Blender baut mit der Vorderseite nach −Y; nach dem FBX-Import zeigt sie nach +z. Gebäude zeigen im
                    // Spiel mit der Vorderseite nach −z (zur Straße), daher eine halbe Drehung (keine Spiegelung).
                    instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    AddGroundPlate(root, e.cells, ground);
                }
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

        /// <summary>
        /// Je Objekt der FBX (nach Namen sortiert) ein Kind „Variant_i“ mit dem Modell (um 180° gedreht wie oben) und
        /// einer eigenen Bodenplatte; RandomVariant zeigt beim Bau eine davon, der Spielstand merkt sich den Index.
        /// </summary>
        private static void AddVariants(GameObject root, GameObject model, Vector2Int cells, Material ground)
        {
            var parts = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in model.transform)
                if (child.GetComponent<MeshFilter>() != null) parts.Add(child);
            parts.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            for (int i = 0; i < parts.Count; i++)
            {
                var variant = new GameObject($"Variant_{i}");
                variant.transform.SetParent(root.transform, false);
                var pivot = new GameObject("Model");
                pivot.transform.SetParent(variant.transform, false);
                pivot.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                var mesh = new GameObject(parts[i].name, typeof(MeshFilter), typeof(MeshRenderer));
                mesh.transform.SetParent(pivot.transform, false);
                mesh.transform.localPosition = parts[i].localPosition;
                mesh.transform.localRotation = parts[i].localRotation;
                mesh.transform.localScale = parts[i].localScale;
                mesh.GetComponent<MeshFilter>().sharedMesh = parts[i].GetComponent<MeshFilter>().sharedMesh;
                mesh.GetComponent<MeshRenderer>().sharedMaterials = parts[i].GetComponent<MeshRenderer>().sharedMaterials;
                AddGroundPlate(variant, cells, ground);
            }
            if (parts.Count > 1) root.AddComponent<RandomVariant>();
        }

        /// <summary>Import wie bei Gebäuden üblich: 1 Einheit = 1 m, keine Kameras, Lichter oder Animation.</summary>
        private static void ConfigureImporter(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is ModelImporter importer)) return;
            bool changed = importer.globalScale != 1f || importer.importCameras || importer.importLights ||
                           importer.importAnimation || importer.animationType != ModelImporterAnimationType.None ||
                           !importer.isReadable;
            importer.globalScale = 1f;
            importer.isReadable = true; // ConstructionVisuals liest die Eckpunkte, um das Gerüst um den Hauskörper zu stellen
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
