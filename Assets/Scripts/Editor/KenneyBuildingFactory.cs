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

            // Eigener Boden unter jedem Gebäude (wie die Grundstücke in Cities: Skylines), unter dem Feld Ackerfurchen
            var gravel = GroundMaterial("Kies", GroundKind.Gravel);
            var paving = GroundMaterial("Pflaster", GroundKind.Paving);
            var earth = GroundMaterial("Erde", GroundKind.Earth);
            grounds.Clear();
            foreach (var n in new[] { "Wohnhaus", "Stadthaus", "Domus", "Insula", "Schmalhaus", "Hofhaus", "Langhaus", "Fachwerkhaus" })
                grounds[n] = gravel;
            foreach (var n in new[] { "Markt", "Bibliothek", "Tempel", "Brunnen",
                         "Schreibschule", "Feuerwache",
                         "Akademie" })
                grounds[n] = paving;
            foreach (var n in new[] { "Lager", "Muehle",
                         "Holzfaeller" })
                grounds[n] = earth;
            GroundMaterial("Acker", GroundKind.Furrows); // Bodenplatte von Feld (BlenderModels, Mat_Boden_Acker)

            // Antike: Kalkstein und Terrakotta-Dächer
            // Wohnhaus und Schmalhaus: Modelle aus Blender (BlenderModels), hier nicht mehr erzeugen
            // Laden, Taberna, Werkstatt und Kontor: Modelle aus Blender (BlenderModels), hier nicht mehr erzeugen
            // Markt: Modell aus Blender (BlenderModels), hier nicht mehr erzeugen
            // Lager: Modell aus Blender (BlenderModels), hier nicht mehr erzeugen
            // Bibliothek: Modell aus Blender (BlenderModels), hier nicht mehr erzeugen
            // Tempel und Brunnen: Modelle aus Blender (BlenderModels), hier nicht mehr erzeugen
            // Schreibschule: Modell aus Blender (BlenderModels), hier nicht mehr erzeugen
            // Feuerwache: Modell aus Blender (BlenderModels), hier nicht mehr erzeugen
            // Feld: vier Wachstumsstufen aus Blender (BlenderModels, CropStages am Prefab), hier nicht mehr erzeugen
            // Holzfäller: Modell aus Blender (BlenderModels), hier nicht mehr erzeugen
            // Holzhacker, Förster, Zimmerei und Steinbruch: Modelle aus Blender (BlenderModels), hier nicht mehr erzeugen
            // Olivenhain, Ölpresse, Lehmgrube und Töpferei: Modelle aus Blender (BlenderModels), hier nicht mehr erzeugen
            // Weinberg und Kelterei: Modelle aus Blender (BlenderModels), hier nicht mehr erzeugen

            // Große Zonengebäude: Häuserzeilen und Hofanlagen aus 2 Teile breiten Häusern
            // Stadthaus, Hofhaus, Langhaus: Modelle aus Blender (BlenderModels), hier nicht mehr erzeugen
            // Domus und Insula: Modelle aus Blender (BlenderModels), hier nicht mehr erzeugen
            // Werkstatthof, Handelshaus und Speicherhof: Modelle aus Blender (BlenderModels), hier nicht mehr erzeugen


            // Werkstatthaus, Handwerkerhaus, Handwerkerzeile, Werkhof und Zunfthaus: Modelle aus Blender (BlenderModels), hier nicht mehr erzeugen

            // Gelehrtenhaus, Stadtvilla, Villa, Peristylhaus: Modelle aus Blender (BlenderModels), hier nicht mehr erzeugen
            // Gelehrtenzone und Akademie: Steinbauten mit Säulen, Innenhöfen und Gärten, auf Pflaster
            // Akademie: Modell aus Blender (BlenderModels), hier nicht mehr erzeugen
            // Großes Theater (Wunderbau): fünf Baustufen aus Blender (BlenderModels, ConstructionStages am Prefab), hier nicht mehr erzeugen

            // Mittelalter: Fachwerk mit der Originalpalette (grüne Dächer)
            Assign("Bld_Fachwerkhaus", Make("Fachwerkhaus", 2, original,
                b => Row(b, "wall-wood", 4, 0f, (2, "roof-high", true), (2, "roof-high", false)),
                b => Row(b, "wall-wood", 4, 0f, (3, "roof-high", false), (2, "roof", true))));
            Assign("Bld_Muehle", Make("Muehle", 2, original, Mill));

            // Deko der Rohstoff-Vorkommen: einzelne Felsen in natürlicher Größe (nicht auf ein Feld skaliert).
            // Die Bäume von Dep_Wald stammen seit 2026-11-06 aus Blender (gebirge.py, EpochCity → Natur einrichten).
            AssignDecor("Dep_Stein",
                MakeDecor("Fels_Gross", NK + "stone_largeA", 1.8f),
                MakeDecor("Fels_Hoch", NK + "stone_tallB", 1.6f),
                MakeDecor("Fels_Breit", NK + "stone_largeC", 1.6f));

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

        /// <summary>Haus 2 Teile breit und d tief, Giebel und Tür nach Süden.</summary>
        private static void House(Builder b, string wall, int storeys, bool doorLeft, bool chimney, string roof, int d = 2)
        {
            const int w = 2;
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
                    return j % 2 == 0 ? $"{wall}-window-small" : wall;
                });
            }
            if (storeys == 1) Side(b, "overhang", doorLeft ? 0 : 1, 0, Vector2Int.down, 0, w, d);
            GableRoof(b, w, d, storeys, roof);
            if (chimney) Side(b, "chimney", 1, d - 1, Vector2Int.right, storeys, w, d);
        }

        /// <summary>
        /// Häuserzeile: Häuser (Stockwerke, Dach, Schornstein) nebeneinander von West nach Ost, jedes d Teile tief,
        /// um z verschoben. Türen abwechselnd links und rechts; Schornsteine nur am Ostende sichtbar, sonst im Nachbarn.
        /// </summary>
        private static void Row(Builder b, string wall, int d, float z,
            params (int storeys, string roof, bool chimney)[] houses)
        {
            for (int k = 0; k < houses.Length; k++)
            {
                var h = houses[k];
                float x = 2f * k - (houses.Length - 1);
                bool chimney = h.chimney && (k == houses.Length - 1 || houses[k + 1].storeys < h.storeys);
                House(b.At(x, z), wall, h.storeys, doorLeft: k % 2 == 0, chimney, h.roof, d);
            }
        }

        // ---------------------------------------------------------------- Rechteckige Zonengebäude



        /// <summary>
        /// Eigene Farben für Holz, Laub und Stein aus dem Nature Kit; dessen Materialfarben wirken im Spiel blass
        /// oder türkis.
        /// </summary>
        private static void NatureColors(Builder b)
        {
            b.Overrides["woodBark"] = Solid("Rinde", new Color(0.45f, 0.30f, 0.18f));
            b.Overrides["woodInner"] = Solid("Holz", new Color(0.82f, 0.66f, 0.44f));
            b.Overrides["leafsGreen"] = Solid("Laub", new Color(0.33f, 0.52f, 0.22f));
            b.Overrides["grass"] = b.Overrides["leafsGreen"]; // Büsche (plant_bush*) tragen „grass“
            b.Overrides["leafsDark"] = Solid("Nadeln", new Color(0.20f, 0.38f, 0.22f));
            b.Overrides["woodBarkDark"] = Solid("RindeDunkel", new Color(0.33f, 0.22f, 0.14f));
            b.Overrides["stone"] = Solid("Stein", new Color(0.62f, 0.60f, 0.56f));
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
        private static void Walls(Builder b, int w, int d, float y, Func<int, int, Vector2Int, string> pick) =>
            Walls(b, Mask(w, d, (i, j) => true), y, pick);

        /// <summary>Wände um die belegten Zellen der Maske: überall, wo die Nachbarzelle frei ist (auch zum Innenhof).</summary>
        private static void Walls(Builder b, bool[,] mask, float y, Func<int, int, Vector2Int, string> pick)
        {
            int w = mask.GetLength(0), d = mask.GetLength(1);
            bool closed = true;
            var outer = new List<Vector2Int>();
            for (int i = 0; i < w; i++)
                for (int j = 0; j < d; j++)
                {
                    if (!mask[i, j]) continue;
                    foreach (var dir in Sides)
                    {
                        var n = new Vector2Int(i, j) + dir;
                        if (Filled(mask, n)) continue;
                        if (!outer.Contains(new Vector2Int(i, j))) outer.Add(new Vector2Int(i, j));
                        string part = pick(i, j, dir);
                        if (part == null) { closed = false; continue; }
                        b.Put(FT + part, Cell(n.x, n.y, w, d) + Vector3.up * y, WallRot(dir));
                    }
                }

            // Dunkler Innenraum hinter Fenstern und Türen (die Bauteile sind innen hohl); nicht bei offenen Schuppen
            if (closed)
                foreach (var c in outer)
                    b.PutBox(Cell(c.x, c.y, w, d) + Vector3.up * (y + InteriorHeight * 0.5f),
                             new Vector3(InteriorSize, InteriorHeight, InteriorSize), InteriorMaterial());
        }

        /// <summary>Innenraum je Zelle und Stockwerk, knapp hinter der Innenseite der Wände (an der Zellkante).</summary>
        private const float InteriorSize = 0.98f, InteriorHeight = 0.98f;
        private static readonly Color InteriorColor = new Color(0.16f, 0.11f, 0.08f);

        private static Material interiorMaterial;

        private static Material InteriorMaterial()
        {
            if (interiorMaterial != null) return interiorMaterial;
            string path = $"{Out}/Mat_Innenraum.mat";
            interiorMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (interiorMaterial == null)
            {
                interiorMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(interiorMaterial, path);
            }
            interiorMaterial.SetColor("_BaseColor", InteriorColor);
            interiorMaterial.SetFloat("_Smoothness", 0f);
            EditorUtility.SetDirty(interiorMaterial);
            return interiorMaterial;
        }

        private static bool[,] Mask(int w, int d, Func<int, int, bool> filled)
        {
            var mask = new bool[w, d];
            for (int i = 0; i < w; i++)
                for (int j = 0; j < d; j++)
                    mask[i, j] = filled(i, j);
            return mask;
        }

        private static bool Filled(bool[,] mask, Vector2Int c) =>
            c.x >= 0 && c.y >= 0 && c.x < mask.GetLength(0) && c.y < mask.GetLength(1) && mask[c.x, c.y];

        /// <summary>Anbauteil an der Außenseite der Zelle (i, j) in Richtung dir.</summary>
        private static void Side(Builder b, string part, int i, int j, Vector2Int dir, float y, int w, int d)
        {
            var n = new Vector2Int(i, j) + dir;
            b.Put(FT + part, Cell(n.x, n.y, w, d) + Vector3.up * y, WallRot(dir));
        }

        /// <summary>
        /// Satteldächer mit First in z-Richtung, je eins pro 2 Teile Breite (breite Firste setzt der Bausatz nicht
        /// sauber zusammen); bei breiten Hallen stehen sie nebeneinander.
        /// </summary>
        private static void GableRoof(Builder b, int w, int d, float y, string part)
        {
            for (int i = 0; i + 1 < w; i += 2)
                for (int j = 0; j < d; j++)
                {
                    RoofPiece(b, part, Cell(i, j, w, d) + Vector3.up * y, -1, j, d); // Traufe nach Westen
                    RoofPiece(b, part, Cell(i + 1, j, w, d) + Vector3.up * y, 1, j, d); // Traufe nach Osten
                }
        }

        /// <summary>Überstand der Traufe über die Zellkante (die Wand reicht 0,1 hinaus, das Dachteil nur 0,07).</summary>
        private const float EaveOverhang = 0.25f;
        /// <summary>Überstand am Giebel: das Dachteil endet an der Zellkante, die Wand 0,1 dahinter.</summary>
        private const float GableOverhang = 0.15f;

        /// <summary>
        /// Ein Dachteil, zur Traufe hin gestreckt (der First bleibt an der Zellkante zum Nachbarteil) und am
        /// ersten bzw. letzten Teil eines Firsts über den Giebel verlängert; mittlere Teile überlappen sich so nicht.
        /// </summary>
        private static void RoofPiece(Builder b, string part, Vector3 cell, int eaveSide, int j, int d)
        {
            const float pieceWidth = 1.07f; // vom First (−0,5) bis zur Traufe (0,57)
            float sx = (pieceWidth + EaveOverhang - 0.07f) / pieceWidth;
            float south = j == 0 ? GableOverhang : 0f, north = j == d - 1 ? GableOverhang : 0f;
            var position = cell + new Vector3(eaveSide * 0.5f * (sx - 1f), 0f, (north - south) * 0.5f);
            b.Put(FT + part, position, eaveSide > 0 ? 0f : 180f, new Vector3(sx, 1f, 1f + south + north));
        }

        // ---------------------------------------------------------------- Zusammenbau

        private class Builder
        {
            public readonly Transform Root;
            private readonly Material palette;

            /// <summary>Ersetzt Materialien der Bauteile nach ihrem Namen (z. B. für goldenen Weizen).</summary>
            public readonly Dictionary<string, Material> Overrides;

            public Builder(Transform root, Material palette) : this(root, palette, new Dictionary<string, Material>()) { }

            private Builder(Transform root, Material palette, Dictionary<string, Material> overrides)
            {
                Root = root;
                this.palette = palette;
                Overrides = overrides;
            }

            /// <summary>Teilbauplatz, um (x, z) verschoben und gedreht; teilt Palette und Ersatzmaterialien.</summary>
            public Builder At(float x, float z, float rotY = 0f)
            {
                var child = new GameObject("Part").transform;
                child.SetParent(Root, false);
                child.localPosition = new Vector3(x, 0f, z);
                child.localRotation = Quaternion.Euler(0f, rotY, 0f);
                return new Builder(child, palette, Overrides);
            }

            /// <summary>Einfacher Quader mit eigenem Material (z. B. Innenraum).</summary>
            public void PutBox(Vector3 center, Vector3 size, Material material)
            {
                var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(box.GetComponent<Collider>());
                box.transform.SetParent(Root, false);
                box.transform.localPosition = center;
                box.transform.localScale = size;
                box.GetComponent<MeshRenderer>().sharedMaterial = material;
            }

            /// <summary>Bauteil je Achse gestreckt (in der gedrehten Lage des Teils, nicht der FBX-Achsen).</summary>
            public void Put(string path, Vector3 position, float rotY, Vector3 scale)
            {
                var holder = new GameObject("Stretched").transform;
                holder.SetParent(Root, false);
                holder.localPosition = position;
                holder.localRotation = Quaternion.Euler(0f, rotY, 0f);
                holder.localScale = scale;
                new Builder(holder, palette, Overrides).Put(path, Vector3.zero, 0f);
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
        private static GameObject Make(string name, int cells, Material palette, params Action<Builder>[] variants) =>
            Make(name, new Vector2Int(cells, cells), palette, variants);

        /// <summary>Wie oben für rechteckige Grundflächen: cells.x = Breite (x), cells.y = Tiefe (z).</summary>
        private static GameObject Make(string name, Vector2Int cells, Material palette, params Action<Builder>[] variants)
        {
            var root = new GameObject(name);
            for (int v = 0; v < variants.Length; v++)
            {
                var temp = new GameObject("temp");
                variants[v](new Builder(temp.transform, palette));

                // Auf die Grundfläche skalieren (gleichmäßig), Unterkante auf 0, mittig
                var bounds = GetBounds(temp);
                float scale = Mathf.Min(cells.x * CellSize * Fill / bounds.size.x, cells.y * CellSize * Fill / bounds.size.z);
                var offset = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z) * scale;

                var combined = Combine(temp, scale, offset, out var materials);
                var modelSize = combined.bounds.size;
                if (grounds.TryGetValue(name, out var ground)) combined = AddGroundPlate(combined, cells, ground, ref materials);
                combined.name = $"{name}_{v}";
                var mesh = SaveMeshAsset(combined, $"{Out}/Meshes/{name}_{v}.asset");
                Object.DestroyImmediate(temp);

                var variant = new GameObject(variants.Length > 1 ? $"Variant_{v}" : "Model",
                    typeof(MeshFilter), typeof(MeshRenderer));
                variant.transform.SetParent(root.transform, false);
                variant.GetComponent<MeshFilter>().sharedMesh = mesh;
                variant.GetComponent<MeshRenderer>().sharedMaterials = materials;

                var size = modelSize;
                Debug.Log($"{name} Variante {v}: {size.x:0.00} × {size.y:0.00} × {size.z:0.00} m " +
                          $"(Grundfläche {cells.x * CellSize:0} × {cells.y * CellSize:0} m, Maßstab {scale:0.00}, {mesh.vertexCount} Vertices)");
            }
            if (variants.Length > 1) root.AddComponent<RandomVariant>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Out}/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>Hängt eine Komponente an die Wurzel eines gespeicherten Prefabs (einmal).</summary>
        private static void AddToPrefab<T>(GameObject prefab) where T : Component
        {
            string path = AssetDatabase.GetAssetPath(prefab);
            var contents = PrefabUtility.LoadPrefabContents(path);
            if (contents.GetComponent<T>() == null) contents.AddComponent<T>();
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        /// <summary>
        /// Ein Nature-Kit-Teil als Deko-Prefab (Baum, Fels): in eigener Farbe, um scale vergrößert, Unterkante auf 0
        /// und mittig, zu einem Mesh verschmolzen; ohne Bodenplatte.
        /// </summary>
        private static GameObject MakeDecor(string name, string part, float scale, string defaultMatAs = null)
        {
            var temp = new GameObject("temp");
            var b = new Builder(temp.transform, null);
            NatureColors(b);
            // Manche Teile tragen einen Teil ihrer Fläche als „_defaultMat“ (weiß); er bekommt dann diese Farbe
            if (defaultMatAs != null) b.Overrides["_defaultMat"] = b.Overrides[defaultMatAs];
            b.Put(part, Vector3.zero, 0f);

            var bounds = GetBounds(temp);
            var offset = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z) * scale;
            var mesh = Combine(temp, scale, offset, out var materials);
            mesh.name = $"Deko_{name}";
            mesh = SaveMeshAsset(mesh, $"{Out}/Meshes/Deko_{name}.asset");
            Object.DestroyImmediate(temp);

            var root = new GameObject($"Deko_{name}", typeof(MeshFilter), typeof(MeshRenderer));
            root.GetComponent<MeshFilter>().sharedMesh = mesh;
            root.GetComponent<MeshRenderer>().sharedMaterials = materials;
            var size = mesh.bounds.size;
            Debug.Log($"Deko {name}: {size.x:0.00} × {size.y:0.00} × {size.z:0.00} m, {mesh.vertexCount} Vertices");

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Out}/Deko_{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>Trägt die Deko-Modelle in ein Vorkommen des Testaufbaus ein.</summary>
        private static void AssignDecor(string deposit, params GameObject[] prefabs)
        {
            var def = AssetDatabase.LoadAssetAtPath<DepositDefinition>($"{Defs}/{deposit}.asset");
            if (def == null)
            {
                Debug.LogWarning($"Vorkommen fehlt: {deposit} (erst „Testaufbau erzeugen“ ausführen)");
                return;
            }
            def.decorPrefabs = prefabs;
            EditorUtility.SetDirty(def);
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
            // Über die Mesh-API befüllen: Nach CopySerialized zeichnet der Editor weiter die alte Geometrie
            existing.Clear();
            existing.indexFormat = mesh.indexFormat;
            existing.SetVertices(mesh.vertices);
            existing.SetNormals(mesh.normals);
            existing.SetTangents(mesh.tangents);
            existing.SetUVs(0, mesh.uv);
            existing.SetColors(mesh.colors);
            existing.subMeshCount = mesh.subMeshCount;
            for (int s = 0; s < mesh.subMeshCount; s++)
                existing.SetIndices(mesh.GetIndices(s), mesh.GetTopology(s), s);
            existing.RecalculateBounds();
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

        // ---------------------------------------------------------------- Boden

        private enum GroundKind { Gravel, Paving, Earth, Furrows, Rubble }

        /// <summary>Bodenmaterial je Modellname; Modelle ohne Eintrag (Feld) bekommen keine Platte.</summary>
        private static readonly Dictionary<string, Material> grounds = new Dictionary<string, Material>();

        private const float GroundFill = 0.98f;  // Platte etwas kleiner als die Grundfläche: schmaler Wiesenrand
        private const float GroundHeight = 0.015f;

        /// <summary>
        /// Hängt eine flache Bodenplatte über die Grundfläche als eigene Teilfläche an. Die Textur wiederholt sich
        /// je Rasterfeld; die Platte dreht sich mit dem Modell, passt also auch bei rechteckigen Gebäuden.
        /// </summary>
        private static Mesh AddGroundPlate(Mesh mesh, Vector2Int cells, Material material, ref Material[] materials)
        {
            float halfW = cells.x * CellSize * GroundFill * 0.5f, halfD = cells.y * CellSize * GroundFill * 0.5f;

            var vertices = new List<Vector3>(); mesh.GetVertices(vertices);
            var normals = new List<Vector3>(); mesh.GetNormals(normals);
            var uvs = new List<Vector2>(); mesh.GetUVs(0, uvs);
            var tangents = new List<Vector4>(); mesh.GetTangents(tangents);
            var colors = new List<Color>(); mesh.GetColors(colors);

            int start = vertices.Count;
            var corners = new[]
            {
                new Vector3(-halfW, GroundHeight, -halfD), new Vector3(halfW, GroundHeight, -halfD),
                new Vector3(-halfW, GroundHeight, halfD), new Vector3(halfW, GroundHeight, halfD)
            };
            foreach (var c in corners)
            {
                vertices.Add(c);
                normals.Add(Vector3.up);
                uvs.Add(new Vector2(c.x / CellSize, c.z / CellSize));
                if (tangents.Count > 0) tangents.Add(new Vector4(1f, 0f, 0f, -1f));
                if (colors.Count > 0) colors.Add(Color.white);
            }

            var result = new Mesh { indexFormat = IndexFormat.UInt32 };
            result.SetVertices(vertices);
            result.SetNormals(normals);
            result.SetUVs(0, uvs);
            if (tangents.Count > 0) result.SetTangents(tangents);
            if (colors.Count > 0) result.SetColors(colors);
            result.subMeshCount = mesh.subMeshCount + 1;
            for (int s = 0; s < mesh.subMeshCount; s++) result.SetIndices(mesh.GetIndices(s), mesh.GetTopology(s), s);
            // Von oben gesehen im Uhrzeigersinn, damit die Fläche nach oben zeigt
            result.SetTriangles(new[] { start, start + 2, start + 1, start + 1, start + 2, start + 3 }, mesh.subMeshCount);
            result.RecalculateBounds();
            Object.DestroyImmediate(mesh);

            var list = new List<Material>(materials) { material };
            materials = list.ToArray();
            return result;
        }

        /// <summary>
        /// Bodenmaterial „Stein“ (Schotter) für Gebäude auf Steinvorkommen, bei Bedarf angelegt; BlenderModels nutzt es
        /// für den Steinbruch.
        /// </summary>
        public static Material EnsureStoneGround()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>($"{Out}/Mat_Boden_Stein.mat");
            if (existing != null) return existing;
            Directory.CreateDirectory(Out);
            return GroundMaterial("Stein", GroundKind.Rubble);
        }

        /// <summary>
        /// Bodenmaterial mit einer im Code erzeugten, nahtlos kachelbaren Textur (256 px je Rasterfeld):
        /// Kies (heller Sand mit Steinchen), Pflaster (Kalksteinplatten im Verband) oder gestampfte Erde.
        /// </summary>
        private static Material GroundMaterial(string name, GroundKind kind)
        {
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = GroundPixel(kind, x, y, size);
            tex.SetPixels(pixels);
            tex.Apply();

            string texPath = $"{Out}/Tex_Boden_{name}.png";
            File.WriteAllBytes(texPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(texPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();

            var mat = Solid("Boden_" + name, Color.white);
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            mat.SetFloat("_Smoothness", 0.05f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Color GroundPixel(GroundKind kind, int x, int y, int size)
        {
            float coarse = TileNoise(x, y, size, 16, 1), fine = TileNoise(x, y, size, 64, 2);
            float speck = Hash(x, y, 3);
            switch (kind)
            {
                case GroundKind.Gravel:
                {
                    var c = new Color(0.76f, 0.70f, 0.57f) * (0.88f + 0.12f * coarse + 0.08f * fine);
                    if (speck > 0.93f) c *= 0.78f;        // dunkle Steinchen
                    else if (speck < 0.05f) c *= 1.12f;   // helle Steinchen
                    c.a = 1f;
                    return c;
                }
                case GroundKind.Paving:
                {
                    // Platten 64 × 32 px im Verband, jede zweite Reihe um eine halbe Platte versetzt
                    int row = y / 32;
                    int shifted = (x + (row % 2) * 32) % size;
                    int col = shifted / 64;
                    int inX = shifted % 64, inY = y % 32;
                    int edge = Mathf.Min(Mathf.Min(inX, 63 - inX), Mathf.Min(inY, 31 - inY));
                    if (edge < 2) return new Color(0.50f, 0.46f, 0.40f, 1f); // Fuge
                    float stone = 0.88f + 0.14f * Hash(col, row, 4);
                    var c = new Color(0.86f, 0.81f, 0.71f) * (stone + 0.06f * fine - (edge < 4 ? 0.06f : 0f));
                    c.a = 1f;
                    return c;
                }
                case GroundKind.Rubble:
                {
                    // Steinbruchboden: hellgrauer Schotter mit Splittern in mehreren Grautönen
                    var c = new Color(0.64f, 0.60f, 0.52f) * (0.84f + 0.16f * coarse + 0.10f * fine);
                    if (speck > 0.90f) c *= 0.74f;
                    else if (speck < 0.08f) c *= 1.16f;
                    c.a = 1f;
                    return c;
                }
                case GroundKind.Furrows:
                {
                    // Acker: 8 Furchen je Rasterfeld entlang x, Kämme heller, Rinnen dunkler
                    float ridge = 0.5f + 0.5f * Mathf.Sin((float)y / size * Mathf.PI * 2f * 8f);
                    var c = new Color(0.47f, 0.33f, 0.22f) * (0.72f + 0.3f * ridge + 0.1f * coarse + 0.06f * fine);
                    if (speck > 0.97f) c *= 0.8f;
                    c.a = 1f;
                    return c;
                }
                default:
                {
                    var c = new Color(0.56f, 0.45f, 0.32f) * (0.84f + 0.18f * coarse + 0.08f * fine);
                    if (speck > 0.96f) c *= 0.8f;
                    c.a = 1f;
                    return c;
                }
            }
        }

        /// <summary>Wertrauschen auf einem Gitter mit cells × cells Zellen, das über den Rand umläuft (nahtlos).</summary>
        private static float TileNoise(int x, int y, int size, int cells, int seed)
        {
            float fx = (float)x / size * cells, fy = (float)y / size * cells;
            int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
            float tx = fx - x0, ty = fy - y0;
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);
            float a = Hash(x0 % cells, y0 % cells, seed), b = Hash((x0 + 1) % cells, y0 % cells, seed);
            float c = Hash(x0 % cells, (y0 + 1) % cells, seed), d = Hash((x0 + 1) % cells, (y0 + 1) % cells, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
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
