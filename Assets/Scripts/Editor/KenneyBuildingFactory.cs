using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace EpochCity
{
    /// <summary>
    /// Baut die Gebäude-Modelle aus den Kenney-Bausätzen (Fantasy Town Kit, Nature Kit) zusammen, verschmilzt
    /// jede Variante zu einem Mesh pro Material und speichert die Prefabs unter Assets/Content/Buildings/.
    /// Danach werden sie in die Gebäudedaten des Testaufbaus eingetragen.
    ///
    /// Regeln der Fantasy-Town-Bauteile (ein Teil = 1 m, Ursprung in der Zellmitte):
    /// Wände und Anbauteile liegen am −x-Rand ihrer Zelle, ihre Außenseite zeigt nach +x. Sie werden in die Zelle
    /// außerhalb des Hauses gesetzt und so gedreht, dass +x nach außen zeigt. Dachflächen haben die Traufe auf +x
    /// und kommen in die Hauszelle. Die Vorderseite eines Gebäudes zeigt nach −z (Süden).
    /// </summary>
    public static class KenneyBuildingFactory
    {
        private const string FT = "Assets/ThirdParty/Kenney/FantasyTownKit/Models/FBX format/";
        private const string NK = "Assets/ThirdParty/Kenney/NatureKit/Models/FBX format/";
        private const string Out = "Assets/Content/Buildings";
        private const string Defs = "Assets/Content/Test";
        private const float CellSize = 2f;   // Standardwert von CityController.cellSize
        private const float Fill = 0.92f;    // Anteil der Grundfläche, den ein Modell höchstens einnimmt

        private static readonly Vector2Int[] Sides =
            { Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up };

        private static Material antique;
        private static Material original;

        [MenuItem("EpochCity/Gebäude-Modelle erzeugen")]
        public static void Generate()
        {
            Directory.CreateDirectory($"{Out}/Meshes");
            antique = CreatePalette("Antike", recolor: true);
            original = CreatePalette("Original", recolor: false);

            // Antike: Kalkstein und Terrakotta-Dächer
            Assign("Bld_Wohnhaus", Make("Wohnhaus", 1, antique,
                b => House(b, "wall", 1, doorLeft: true, chimney: false, roof: "roof"),
                b => House(b, "wall", 1, doorLeft: false, chimney: true, roof: "roof"),
                b => House(b, "wall", 1, doorLeft: true, chimney: false, roof: "roof-high")));
            Assign("Bld_Laden", Make("Laden", 1, antique, Shop));
            Assign("Bld_Markt", Make("Markt", 2, antique, Market));
            Assign("Bld_Lager", Make("Lager", 2, antique, Storage));
            Assign("Bld_Bibliothek", Make("Bibliothek", 2, antique, Library));
            Assign("Bld_Feld", Make("Feld", 2, original, Field));

            // Mittelalter: Fachwerk mit der Originalpalette (grüne Dächer)
            Assign("Bld_Fachwerkhaus", Make("Fachwerkhaus", 1, original,
                b => House(b, "wall-wood", 2, doorLeft: true, chimney: true, roof: "roof-high"),
                b => House(b, "wall-wood", 2, doorLeft: false, chimney: false, roof: "roof-high")));
            Assign("Bld_Muehle", Make("Muehle", 2, original, Mill));

            AssetDatabase.SaveAssets();
            ReimportPrefabs(Out);
            Debug.Log("Gebäude-Modelle erzeugt.");
        }

        /// <summary>
        /// Beim erneuten Erzeugen behält Unity sonst eine veraltete Kopie der Prefabs im Speicher, deren
        /// Mesh-Verweise ins Leere zeigen, bis die Prefabs neu importiert werden.
        /// </summary>
        public static void ReimportPrefabs(string folder)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
        }

        // ---------------------------------------------------------------- Rezepte

        private static void House(Builder b, string wall, int storeys, bool doorLeft, bool chimney, string roof)
        {
            const int w = 2, d = 2;
            for (int level = 0; level < storeys; level++)
            {
                int y = level;
                Walls(b, w, d, y, (i, j, dir) =>
                {
                    if (dir == Vector2Int.down)
                    {
                        bool doorHere = level == 0 && (i == 0) == doorLeft;
                        return doorHere ? $"{wall}-door" : $"{wall}-window-shutters";
                    }
                    if (dir == Vector2Int.up) return level == 0 ? wall : $"{wall}-window-small";
                    return j == 0 ? $"{wall}-window-small" : wall;
                });
            }
            if (storeys == 1) Side(b, "overhang", doorLeft ? 0 : 1, 0, Vector2Int.down, 0, w, d);
            GableRoof(b, w, d, storeys, roof);
            if (chimney) Side(b, "chimney", 1, 1, Vector2Int.right, storeys, w, d);
        }

        /// <summary>Lager: Holzhalle 2 × 3 mit breitem Tor nach Süden, Karren daneben.</summary>
        private static void Storage(Builder b)
        {
            const int w = 2, d = 3;
            Walls(b, w, d, 0, (i, j, dir) =>
                dir == Vector2Int.down ? (i == 0 ? "wall-wood-doorway-square-wide" : "wall-wood") :
                j == 1 ? "wall-wood-window-small" : "wall-wood");
            GableRoof(b, w, d, 1, "roof");
            b.Put(FT + "cart", new Vector3(1.6f, 0f, -1.2f), 10f);
        }

        /// <summary>Bibliothek: Steinbau 2 × 2 mit Säulenvorhalle nach Süden, wie ein kleiner Tempel.</summary>
        private static void Library(Builder b)
        {
            const int w = 2, d = 3;
            // Wände nur um die hinteren zwei Reihen; die vordere Reihe ist die offene Vorhalle
            for (int i = 0; i < w; i++)
                for (int j = 1; j < d; j++)
                    foreach (var dir in Sides)
                    {
                        var n = new Vector2Int(i, j) + dir;
                        bool outside = n.x < 0 || n.x >= w || n.y < 1 || n.y >= d;
                        if (!outside) continue;
                        string part = dir == Vector2Int.down ? (i == 0 ? "wall-door" : "wall-window-stone") :
                            dir == Vector2Int.up ? "wall" : "wall-window-stone";
                        b.Put(FT + part, Cell(n.x, n.y, w, d), WallRot(dir));
                    }
            // Säulen an der Vorderkante der Vorhalle
            for (int k = 0; k <= w; k++)
                b.Put(FT + "pillar-stone", Cell(0, 0, w, d) + new Vector3(k - 0.5f, 0f, -0.5f), 0f);
            GableRoof(b, w, d, 1, "roof");
        }

        /// <summary>Laden: zwei Marktstände auf Holzboden.</summary>
        private static void Shop(Builder b)
        {
            const int w = 2, d = 2;
            for (int i = 0; i < w; i++)
                for (int j = 0; j < d; j++)
                    b.Put(FT + "planks", Cell(i, j, w, d), 0f);
            b.Put(FT + "stall-red", Cell(0, 1, w, d), 0f);
            b.Put(FT + "stall-green", Cell(1, 1, w, d), 0f);
            b.Put(FT + "stall-bench", Cell(0, 0, w, d), 0f);
            b.Put(FT + "lantern", Cell(1, 0, w, d), 0f);
        }

        /// <summary>Markt: Brunnen in der Mitte, Stände ringsum.</summary>
        private static void Market(Builder b)
        {
            // Eigenes, einfarbiges Wasser statt des Kenney-Materials
            b.Overrides["Water"] = Solid("Wasser", new Color(0.42f, 0.70f, 0.85f));
            b.Put(FT + "fountain-round", Vector3.zero, 0f);
            b.Put(FT + "stall-red", new Vector3(-1.6f, 0f, 1.6f), 0f);
            b.Put(FT + "stall-green", new Vector3(1.6f, 0f, 1.6f), 0f);
            b.Put(FT + "stall-red", new Vector3(1.6f, 0f, -1.6f), 180f);
            b.Put(FT + "stall", new Vector3(-1.6f, 0f, -1.6f), 90f);
            b.Put(FT + "cart", new Vector3(-1.9f, 0f, 0f), 0f);
            b.Put(FT + "lantern", new Vector3(1.9f, 0f, 0f), 0f);
        }

        /// <summary>Feld: Doppelreihen Acker mit Weizen (ein Büschel pro Stück, die Halme sind recht detailliert).</summary>
        private static void Field(Builder b)
        {
            // Die Kenney-Farben sind für Weizen und Acker zu blass
            b.Overrides["woodInner"] = Solid("Weizen", new Color(0.86f, 0.70f, 0.32f));
            b.Overrides["_defaultMat"] = b.Overrides["woodInner"];
            b.Overrides["dirt"] = Solid("Acker", new Color(0.50f, 0.36f, 0.24f));
            b.Overrides["dirtDark"] = Solid("AckerDunkel", new Color(0.38f, 0.27f, 0.18f));

            const int rows = 4, tiles = 4;
            for (int r = 0; r < rows; r++)
            {
                float z = (r - (rows - 1) * 0.5f) * 0.95f;
                for (int t = 0; t < tiles; t++)
                {
                    float x = t - (tiles - 1) * 0.5f;
                    b.Put(NK + "crops_dirtDoubleRow", new Vector3(x, 0f, z), 0f);
                    b.Put(NK + "crops_wheatStageB", new Vector3(x, 0f, z), t % 2 == 0 ? 0f : 180f);
                }
            }
        }

        /// <summary>Mühle: zweistöckiger Steinturm mit Windmühlenflügeln an der Vorderseite.</summary>
        private static void Mill(Builder b)
        {
            const int w = 2, d = 2;
            for (int level = 0; level < 2; level++)
            {
                int y = level;
                Walls(b, w, d, y, (i, j, dir) =>
                    level == 0 && dir == Vector2Int.down && i == 0 ? "wall-door" :
                    level == 1 ? "wall-window-small" : "wall");
            }
            GableRoof(b, w, d, 2, "roof-high");
            // Die Flügel stehen in der y-z-Ebene; gedreht, damit sie nach Süden zeigen, und im Giebel angebracht
            b.Put(FT + "windmill", new Vector3(0f, 2.75f, -1.3f), 90f, 0.7f);
        }

        // ---------------------------------------------------------------- Bausteine

        /// <summary>Mitte einer Bauteilzelle bei einem w × d großen Grundriss, um den Ursprung zentriert.</summary>
        private static Vector3 Cell(int i, int j, int w, int d) =>
            new Vector3(i - (w - 1) * 0.5f, 0f, j - (d - 1) * 0.5f);

        /// <summary>Drehung, die die Außenseite (+x) eines Wandteils in Richtung dir bringt.</summary>
        private static float WallRot(Vector2Int dir) =>
            dir == Vector2Int.right ? 0f : dir == Vector2Int.left ? 180f : dir == Vector2Int.up ? 270f : 90f;

        /// <summary>Wände rund um einen w × d großen Grundriss auf Höhe y; pick liefert den Bauteilnamen oder null.</summary>
        private static void Walls(Builder b, int w, int d, float y, Func<int, int, Vector2Int, string> pick)
        {
            for (int i = 0; i < w; i++)
                for (int j = 0; j < d; j++)
                    foreach (var dir in Sides)
                    {
                        var n = new Vector2Int(i, j) + dir;
                        if (n.x >= 0 && n.x < w && n.y >= 0 && n.y < d) continue;
                        string part = pick(i, j, dir);
                        if (part == null) continue;
                        b.Put(FT + part, Cell(n.x, n.y, w, d) + Vector3.up * y, WallRot(dir));
                    }
        }

        /// <summary>Anbauteil an der Außenseite der Zelle (i, j) in Richtung dir.</summary>
        private static void Side(Builder b, string part, int i, int j, Vector2Int dir, float y, int w, int d)
        {
            var n = new Vector2Int(i, j) + dir;
            b.Put(FT + part, Cell(n.x, n.y, w, d) + Vector3.up * y, WallRot(dir));
        }

        /// <summary>Satteldach mit First in z-Richtung über einem 2 Teile breiten Grundriss.</summary>
        private static void GableRoof(Builder b, int w, int d, float y, string part)
        {
            for (int j = 0; j < d; j++)
            {
                b.Put(FT + part, Cell(0, j, w, d) + Vector3.up * y, 180f); // Traufe nach Westen
                b.Put(FT + part, Cell(w - 1, j, w, d) + Vector3.up * y, 0f); // Traufe nach Osten
            }
        }

        // ---------------------------------------------------------------- Zusammenbau

        private class Builder
        {
            public readonly Transform Root;
            private readonly Material palette;

            /// <summary>Ersetzt Materialien der Bauteile nach ihrem Namen (z. B. für goldenen Weizen).</summary>
            public readonly Dictionary<string, Material> Overrides = new Dictionary<string, Material>();

            public Builder(Transform root, Material palette)
            {
                Root = root;
                this.palette = palette;
            }

            public void Put(string path, Vector3 position, float rotY, float scale = 1f)
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(path + ".fbx");
                if (source == null)
                {
                    Debug.LogWarning($"Bauteil fehlt: {path}");
                    return;
                }
                var go = (GameObject)Object.Instantiate(source, Root);
                go.transform.localPosition = position;
                go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
                go.transform.localScale *= scale;

                // Fantasy-Town-Teile nutzen die Farbpalette „colormap“; jede FBX bringt eine eigene Kopie mit. Alle
                // bekommen ein gemeinsames Material (weniger Draw Calls), für die Antike eine umgefärbte Palette.
                // Andere Materialien (z. B. Wasser) bleiben, außer es gibt einen Ersatz in Overrides.
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                {
                    var mats = r.sharedMaterials;
                    for (int k = 0; k < mats.Length; k++)
                    {
                        if (mats[k] == null) continue;
                        if (palette != null && path.StartsWith(FT) && mats[k].name == "colormap") mats[k] = palette;
                        else if (Overrides.TryGetValue(mats[k].name, out var replacement)) mats[k] = replacement;
                    }
                    r.sharedMaterials = mats;
                }
            }
        }

        /// <summary>
        /// Baut jede Variante, skaliert sie auf die Grundfläche, verschmilzt sie und speichert ein Prefab.
        /// Bei mehreren Varianten wählt RandomVariant beim Bau eine aus.
        /// </summary>
        private static GameObject Make(string name, int cells, Material palette, params Action<Builder>[] variants)
        {
            var root = new GameObject(name);
            for (int v = 0; v < variants.Length; v++)
            {
                var temp = new GameObject("temp");
                variants[v](new Builder(temp.transform, palette));

                // Auf die Grundfläche skalieren (gleichmäßig), Unterkante auf 0, mittig
                var bounds = GetBounds(temp);
                float target = cells * CellSize * Fill;
                float scale = Mathf.Min(target / bounds.size.x, target / bounds.size.z);
                var offset = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z) * scale;

                var combined = Combine(temp, scale, offset, out var materials);
                combined.name = $"{name}_{v}";
                var mesh = SaveMeshAsset(combined, $"{Out}/Meshes/{name}_{v}.asset");
                Object.DestroyImmediate(temp);

                var variant = new GameObject(variants.Length > 1 ? $"Variant_{v}" : "Model",
                    typeof(MeshFilter), typeof(MeshRenderer));
                variant.transform.SetParent(root.transform, false);
                variant.GetComponent<MeshFilter>().sharedMesh = mesh;
                variant.GetComponent<MeshRenderer>().sharedMaterials = materials;

                var size = mesh.bounds.size;
                Debug.Log($"{name} Variante {v}: {size.x:0.00} × {size.y:0.00} × {size.z:0.00} m " +
                          $"(Grundfläche {cells * CellSize:0} m, Maßstab {scale:0.00}, {mesh.vertexCount} Vertices)");
            }
            if (variants.Length > 1) root.AddComponent<RandomVariant>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Out}/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>
        /// Speichert ein Mesh als Asset. Ein vorhandenes Asset wird überschrieben statt gelöscht und neu angelegt,
        /// sonst verlieren Prefabs, die es schon verwenden, ihren Verweis.
        /// </summary>
        public static Mesh SaveMeshAsset(Mesh mesh, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }
            string name = existing.name;
            EditorUtility.CopySerialized(mesh, existing);
            existing.name = name;
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(mesh);
            return existing;
        }

        private static Bounds GetBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        /// <summary>Ein Mesh mit einer Teilfläche pro Material; Skalierung und Versatz werden eingerechnet.</summary>
        private static Mesh Combine(GameObject go, float scale, Vector3 offset, out Material[] materials)
        {
            var byMaterial = new Dictionary<Material, List<CombineInstance>>();
            var placement = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one * scale);
            var toRoot = placement * go.transform.worldToLocalMatrix;

            foreach (var filter in go.GetComponentsInChildren<MeshFilter>())
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || filter.sharedMesh == null) continue;
                var mats = renderer.sharedMaterials;
                for (int s = 0; s < filter.sharedMesh.subMeshCount; s++)
                {
                    var mat = mats[Mathf.Min(s, mats.Length - 1)];
                    if (!byMaterial.TryGetValue(mat, out var list)) byMaterial[mat] = list = new List<CombineInstance>();
                    list.Add(new CombineInstance
                    {
                        mesh = filter.sharedMesh,
                        subMeshIndex = s,
                        transform = toRoot * filter.transform.localToWorldMatrix
                    });
                }
            }

            // Erst je Material zusammenfassen, dann die Teilflächen zu einem Mesh
            var parts = new List<CombineInstance>();
            materials = new Material[byMaterial.Count];
            int index = 0;
            foreach (var entry in byMaterial)
            {
                var part = new Mesh { indexFormat = IndexFormat.UInt32 };
                part.CombineMeshes(entry.Value.ToArray(), true, true);
                parts.Add(new CombineInstance { mesh = part, transform = Matrix4x4.identity });
                materials[index++] = entry.Key;
            }
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(parts.ToArray(), false, false);
            mesh.RecalculateBounds();
            foreach (var p in parts) Object.DestroyImmediate(p.mesh);
            return mesh;
        }

        private static void Assign(string definition, GameObject prefab)
        {
            var def = AssetDatabase.LoadAssetAtPath<BuildingDefinition>($"{Defs}/{definition}.asset");
            if (def == null)
            {
                Debug.LogWarning($"Gebäudedaten fehlen: {definition} (zuerst „Testaufbau erzeugen“)");
                return;
            }
            def.prefab = prefab;
            EditorUtility.SetDirty(def);
        }

        // ---------------------------------------------------------------- Paletten

        /// <summary>
        /// Gemeinsames Material mit einer Kopie der Kenney-Farbpalette. Mit recolor: Terrakotta statt Grün für
        /// Dächer und warmer Kalkstein statt Blaugrau für Wände; die Helligkeitsverläufe der Felder bleiben erhalten.
        /// </summary>
        private static Material CreatePalette(string name, bool recolor)
        {
            string sourcePath = FT + "Textures/colormap.png";
            var source = new Texture2D(2, 2);
            source.LoadImage(File.ReadAllBytes(sourcePath));

            if (recolor)
            {
                // Felder der 512er-Palette (Pixel, y von unten): Dachgrün unten links, Wandstein in der Mitte links
                Recolor(source, new RectInt(64, 0, 64, 128), new Color(0.80f, 0.42f, 0.28f));
                Recolor(source, new RectInt(64, 128, 64, 128), new Color(0.88f, 0.82f, 0.70f));
            }

            string texPath = $"{Out}/Tex_Palette_{name}.png";
            File.WriteAllBytes(texPath, source.EncodeToPNG());
            Object.DestroyImmediate(source);
            AssetDatabase.ImportAsset(texPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            string matPath = $"{Out}/Mat_Palette_{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Einfarbiges, mattes Material unter Assets/Content/Buildings (wird bei Bedarf angelegt).</summary>
        private static Material Solid(string name, Color color)
        {
            string path = $"{Out}/Mat_{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", 0.1f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Färbt ein Feld um: neue Farbe × relative Helligkeit des Originalpixels.</summary>
        private static void Recolor(Texture2D tex, RectInt area, Color target)
        {
            var pixels = tex.GetPixels(area.x, area.y, area.width, area.height);
            float avg = 0f;
            foreach (var p in pixels) avg += p.grayscale;
            avg /= pixels.Length;
            for (int k = 0; k < pixels.Length; k++)
            {
                float f = avg > 0f ? pixels[k].grayscale / avg : 1f;
                pixels[k] = new Color(
                    Mathf.Clamp01(target.r * f), Mathf.Clamp01(target.g * f), Mathf.Clamp01(target.b * f), 1f);
            }
            tex.SetPixels(area.x, area.y, area.width, area.height, pixels);
            tex.Apply();
        }
    }
}
