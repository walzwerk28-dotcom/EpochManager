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

            // Eigener Boden unter jedem Gebäude (wie die Grundstücke in Cities: Skylines); das Feld hat schon Acker
            var gravel = GroundMaterial("Kies", GroundKind.Gravel);
            var paving = GroundMaterial("Pflaster", GroundKind.Paving);
            var earth = GroundMaterial("Erde", GroundKind.Earth);
            grounds.Clear();
            foreach (var n in new[] { "Wohnhaus", "Stadthaus", "Domus", "Insula", "Schmalhaus", "Hofhaus", "Langhaus", "Fachwerkhaus" })
                grounds[n] = gravel;
            foreach (var n in new[] { "Laden", "Markt", "Handelshaus", "Taberna", "Kontor", "Bibliothek", "Tempel", "Brunnen",
                         "Schreibschule" })
                grounds[n] = paving;
            foreach (var n in new[] { "Werkstatt", "Werkstatthof", "Speicherhof", "Lager", "Muehle",
                         "Holzfaeller", "Zimmerei", "Steinbruch" })
                grounds[n] = earth;

            // Antike: Kalkstein und Terrakotta-Dächer
            Assign("Bld_Wohnhaus", Make("Wohnhaus", 1, antique,
                b => House(b, "wall", 1, doorLeft: true, chimney: false, roof: "roof"),
                b => House(b, "wall", 1, doorLeft: false, chimney: true, roof: "roof"),
                b => House(b, "wall", 1, doorLeft: true, chimney: false, roof: "roof-high")));
            Assign("Bld_Laden", Make("Laden", 1, antique, Shop));
            Assign("Bld_Markt", Make("Markt", 2, antique, Market));
            Assign("Bld_Lager", Make("Lager", 2, antique, Storage));
            Assign("Bld_Bibliothek", Make("Bibliothek", 2, antique, Library));
            Assign("Bld_Tempel", Make("Tempel", new Vector2Int(2, 3), antique, Temple));
            Assign("Bld_Brunnen", Make("Brunnen", 1, antique, Well));
            Assign("Bld_Schreibschule", Make("Schreibschule", 2, antique, School));
            Assign("Bld_Feld", Make("Feld", 2, original, Field));
            Assign("Bld_Holzfaeller", Make("Holzfaeller", 2, antique, Woodcutter));
            Assign("Bld_Zimmerei", Make("Zimmerei", 2, antique, Carpentry));
            Assign("Bld_Steinbruch", Make("Steinbruch", 2, antique, Quarry));

            // Große Zonengebäude: Häuserzeilen und Hofanlagen aus 2 Teile breiten Häusern
            Assign("Bld_Stadthaus", Make("Stadthaus", 2, antique,
                b => Row(b, "wall", 4, 0f, (2, "roof", true), (1, "roof-high", false)),
                b => Row(b, "wall", 4, 0f, (1, "roof-high", false), (2, "roof", true))));
            Assign("Bld_Domus", Make("Domus", 3, antique, DomusRow, DomusAtrium));
            Assign("Bld_Insula", Make("Insula", 4, antique, InsulaBlock, InsulaRow));
            Assign("Bld_Werkstatt", Make("Werkstatt", 2, antique, WorkshopYard, WorkshopSide));
            Assign("Bld_Werkstatthof", Make("Werkstatthof", 3, antique, WorkshopCourtFront, WorkshopCourtWings));
            Assign("Bld_Handelshaus", Make("Handelshaus", 4, antique, TradeHall, TradeCourt));

            // Rechteckige Zonengebäude (Breite × Tiefe, Vorderseite nach −z an der Schmalseite)
            Assign("Bld_Schmalhaus", Make("Schmalhaus", new Vector2Int(1, 2), antique,
                b => House(b, "wall", 2, doorLeft: true, chimney: true, roof: "roof", d: 4),
                NarrowHouseGarden));
            Assign("Bld_Hofhaus", Make("Hofhaus", new Vector2Int(2, 3), antique, CourtHouseGarden,
                b => Row(b, "wall", 6, 0f, (1, "roof", false), (2, "roof", true))));
            Assign("Bld_Langhaus", Make("Langhaus", new Vector2Int(2, 4), antique, LongHouseCourt, LongHouseGarden));
            Assign("Bld_Taberna", Make("Taberna", new Vector2Int(1, 2), antique, TabernaStall, TabernaHall));
            Assign("Bld_Kontor", Make("Kontor", new Vector2Int(2, 3), antique, OfficeHall, OfficeYard));
            Assign("Bld_Speicherhof", Make("Speicherhof", new Vector2Int(2, 4), antique, GranaryFront, GranaryYard));

            // Mittelalter: Fachwerk mit der Originalpalette (grüne Dächer)
            Assign("Bld_Fachwerkhaus", Make("Fachwerkhaus", 2, original,
                b => Row(b, "wall-wood", 4, 0f, (2, "roof-high", true), (2, "roof-high", false)),
                b => Row(b, "wall-wood", 4, 0f, (3, "roof-high", false), (2, "roof", true))));
            Assign("Bld_Muehle", Make("Muehle", 2, original, Mill));

            // Deko der Rohstoff-Vorkommen: einzelne Bäume und Felsen in natürlicher Größe (nicht auf ein Feld skaliert)
            AssignDecor("Dep_Wald",
                MakeDecor("Baum_Eiche", NK + "tree_oak", 1.8f),
                MakeDecor("Baum_Laub", NK + "tree_default", 1.8f),
                MakeDecor("Baum_Kiefer", NK + "tree_pineRoundB", 1.8f, defaultMatAs: "leafsDark"),
                MakeDecor("Baum_Kiefer_Hoch", NK + "tree_pineTallA", 1.6f));
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

        /// <summary>Domus mit Garten: drei Häuser an der Straße, dahinter Bäume.</summary>
        private static void DomusRow(Builder b)
        {
            Row(b, "wall", 4, -1f, (1, "roof", false), (2, "roof", true), (1, "roof-high", false));
            b.Put(FT + "tree-high-round", new Vector3(-2f, 0f, 2.2f), 0f);
            b.Put(NK + "plant_bushLarge", new Vector3(0f, 0f, 2.2f), 0f, 3f);
            b.Put(FT + "tree", new Vector3(2f, 0f, 2.2f), 40f);
        }

        /// <summary>Domus mit Atrium: einstöckiger Ring um einen Innenhof, zur Straße zweistöckig.</summary>
        private static void DomusAtrium(Builder b)
        {
            const int w = 6, d = 6;
            var ring = Mask(w, d, (i, j) => !(i >= 2 && i < 4 && j >= 2 && j < 4));
            var upper = Mask(w, d, (i, j) => j < 2);
            Walls(b, ring, 0, (i, j, dir) => Facade(i, j, dir, 0, "wall", door: i == 2));
            Walls(b, upper, 1, (i, j, dir) => Facade(i, j, dir, 1, "wall", door: false));
            FlatRoof(b, Mask(w, d, (i, j) => ring[i, j] && !upper[i, j]), 1);
            FlatRoof(b, upper, 2);
            b.Put(FT + "tree", new Vector3(0f, 0f, 0.5f), 0f);
        }

        /// <summary>Insula: dreistöckiger Block um einen Innenhof, unten Läden zur Straße.</summary>
        private static void InsulaBlock(Builder b)
        {
            const int w = 8, d = 8, storeys = 3;
            var ring = Mask(w, d, (i, j) => !(i >= 2 && i < 6 && j >= 2 && j < 6));
            for (int level = 0; level < storeys; level++)
            {
                int l = level;
                Walls(b, ring, l, (i, j, dir) =>
                    l == 0 && dir == Vector2Int.down && j == 0
                        ? (i == 3 ? "wall-doorway-round" : i % 2 == 0 ? "wall-doorway-square" : "wall-window-shutters")
                        : Facade(i, j, dir, l, "wall", door: false));
            }
            FlatRoof(b, ring, storeys);
            b.Put(FT + "tree-high-round", new Vector3(0f, 0f, 0.3f), 0f);
        }

        /// <summary>Insula als Zeile: vier hohe, schmale Häuser mit Hinterhof.</summary>
        private static void InsulaRow(Builder b)
        {
            Row(b, "wall", 6, -1f, (3, "roof", false), (2, "roof-high", true), (3, "roof", true), (2, "roof", false));
            b.Put(FT + "tree", new Vector3(-2.5f, 0f, 3f), 0f);
            b.Put(NK + "plant_bushLarge", new Vector3(0.5f, 0f, 3f), 0f, 3f);
            b.Put(FT + "tree-high-crooked", new Vector3(3f, 0f, 3f), 120f);
        }

        /// <summary>Werkstatt: Halle hinten, Hof mit Stand und Karren zur Straße.</summary>
        private static void WorkshopYard(Builder b)
        {
            Hall(b.At(0f, 1f), 4, 2, 1, "roof", (i, j, dir, level) =>
                dir == Vector2Int.down ? (i == 1 || i == 2 ? "wall-wood-doorway-square-wide" : "wall-wood-window-small") :
                "wall-wood");
            Planks(b.At(0f, -1f), 4, 2);
            b.Put(FT + "stall-red", new Vector3(-1.5f, 0f, -1.4f), 0f);
            b.Put(NK + "log_stack", new Vector3(0.2f, 0f, -0.8f), 90f);
            b.Put(FT + "cart", new Vector3(1.5f, 0f, -1.1f), 80f);
        }

        /// <summary>Werkstatt: tiefe Halle links, offener Hof rechts.</summary>
        private static void WorkshopSide(Builder b)
        {
            Hall(b.At(-1f, 0f), 2, 4, 1, "roof-high", (i, j, dir, level) =>
                dir == Vector2Int.down ? (i == 0 ? "wall-wood-doorway-square-wide" : "wall-wood-window-small") :
                j % 2 == 1 ? "wall-wood-window-small" : "wall-wood");
            Planks(b.At(1f, 0f), 2, 4);
            b.Put(FT + "stall-green", new Vector3(1f, 0f, -1.4f), 0f);
            b.Put(NK + "log_stack", new Vector3(1f, 0f, 0.2f), 0f);
            b.Put(FT + "cart", new Vector3(1f, 0f, 1.4f), 90f);
        }

        /// <summary>Werkstatthof: breite Halle mit drei Dächern hinten, Hof mit Ständen vorn.</summary>
        private static void WorkshopCourtFront(Builder b)
        {
            Hall(b.At(0f, 1.5f), 6, 3, 1, "roof", (i, j, dir, level) =>
                dir == Vector2Int.down ? (i == 1 || i == 4 ? "wall-wood-doorway-square-wide" : "wall-wood-window-small") :
                j == 1 ? "wall-wood-window-small" : "wall-wood");
            Planks(b.At(0f, -1.5f), 6, 3);
            b.Put(FT + "stall-red", new Vector3(-2f, 0f, -2.4f), 0f);
            b.Put(FT + "stall-green", new Vector3(0f, 0f, -2.4f), 0f);
            b.Put(FT + "stall", new Vector3(2f, 0f, -2.4f), 0f);
            b.Put(NK + "log_stack", new Vector3(-2f, 0f, -0.7f), 90f);
            b.Put(FT + "cart", new Vector3(2f, 0f, -0.8f), 80f);
        }

        /// <summary>Werkstatthof: zwei tiefe Hallen links und rechts, schmaler Hof dazwischen.</summary>
        private static void WorkshopCourtWings(Builder b)
        {
            Func<int, int, Vector2Int, int, string> pick = (i, j, dir, level) =>
                dir == Vector2Int.down ? (i == 0 ? "wall-wood-doorway-square-wide" : "wall-wood-window-small") :
                j % 2 == 1 ? "wall-wood-window-small" : "wall-wood";
            Hall(b.At(-2f, 0f), 2, 6, 1, "roof", pick);
            Hall(b.At(2f, 0f), 2, 6, 1, "roof-high", pick);
            Planks(b.At(0f, 0f), 2, 6);
            b.Put(FT + "stall-red", new Vector3(0f, 0f, -2.4f), 0f);
            b.Put(NK + "log_stack", new Vector3(0f, 0f, 0f), 0f);
            b.Put(FT + "cart", new Vector3(0f, 0f, 2f), 90f);
        }

        /// <summary>Handelshaus: zweistöckige Steinhalle mit Bögen, davor ein Platz mit zwei Reihen Ständen.</summary>
        private static void TradeHall(Builder b)
        {
            Hall(b.At(0f, 2f), 8, 4, 2, "roof", (i, j, dir, level) =>
                dir == Vector2Int.down
                    ? (level == 0 ? (i % 2 == 1 ? "wall-doorway-round" : "wall-window-stone") : "wall-window-shutters")
                    : j % 2 == 1 || level == 1 ? "wall-window-small" : "wall");
            string[] stalls = { "stall-red", "stall-green", "stall", "stall-red" };
            for (int k = 0; k < 4; k++)
            {
                float x = 2f * k - 3f;
                b.Put(FT + stalls[k], new Vector3(x, 0f, -3.3f), 0f);
                b.Put(FT + stalls[3 - k], new Vector3(x, 0f, -1.4f), 0f);
            }
            b.Put(FT + "lantern", new Vector3(-4f + 0.3f, 0f, -0.3f), 0f);
            b.Put(FT + "lantern", new Vector3(4f - 0.3f, 0f, -0.3f), 0f);
        }

        /// <summary>Handelshaus: Hofanlage mit Brunnen und Ständen im Innenhof, zur Straße zweistöckig.</summary>
        private static void TradeCourt(Builder b)
        {
            const int w = 8, d = 8;
            var ring = Mask(w, d, (i, j) => !(i >= 2 && i < 6 && j >= 2 && j < 6));
            var upper = Mask(w, d, (i, j) => j < 2);
            Walls(b, ring, 0, (i, j, dir) =>
                dir == Vector2Int.down && j == 0 ? (i == 3 || i == 4 ? "wall-doorway-round" : "wall-window-stone") :
                Facade(i, j, dir, 0, "wall", door: false));
            Walls(b, upper, 1, (i, j, dir) => Facade(i, j, dir, 1, "wall", door: false));
            FlatRoof(b, Mask(w, d, (i, j) => ring[i, j] && !upper[i, j]), 1);
            FlatRoof(b, upper, 2);

            b.Overrides["Water"] = Solid("Wasser", new Color(0.42f, 0.70f, 0.85f));
            b.Put(FT + "fountain-round", new Vector3(0f, 0f, 0f), 0f, 0.7f);
            b.Put(FT + "stall-red", new Vector3(-1.5f, 0f, 1.6f), 0f);
            b.Put(FT + "stall-green", new Vector3(1.5f, 0f, 1.6f), 0f);
        }

        // ---------------------------------------------------------------- Rechteckige Zonengebäude

        /// <summary>Schmalhaus 1 × 2: einstöckiges Haus an der Straße, Baum im Garten dahinter.</summary>
        private static void NarrowHouseGarden(Builder b)
        {
            House(b.At(0f, -0.5f), "wall", 1, doorLeft: false, chimney: false, roof: "roof-high", d: 3);
            b.Put(FT + "tree", new Vector3(0f, 0f, 1.6f), 30f);
        }

        /// <summary>Hofhaus 2 × 3: zwei Häuser an der Straße, Garten dahinter.</summary>
        private static void CourtHouseGarden(Builder b)
        {
            Row(b, "wall", 4, -1f, (2, "roof", true), (1, "roof-high", false));
            b.Put(FT + "tree-high-round", new Vector3(-1f, 0f, 2.2f), 0f);
            b.Put(NK + "plant_bushLarge", new Vector3(1f, 0f, 2.2f), 0f, 3f);
        }

        /// <summary>Langhaus 2 × 4: Vorderhaus zweistöckig, Hinterhaus einstöckig, Hof dazwischen.</summary>
        private static void LongHouseCourt(Builder b)
        {
            Row(b, "wall", 4, -2f, (2, "roof", true), (2, "roof-high", false));
            Row(b, "wall", 3, 2.5f, (1, "roof", false), (1, "roof", false));
        }

        /// <summary>Langhaus 2 × 4: tiefe Häuserzeile mit Bäumen im Hinterhof.</summary>
        private static void LongHouseGarden(Builder b)
        {
            Row(b, "wall", 6, -1f, (2, "roof", false), (3, "roof", true));
            b.Put(FT + "tree", new Vector3(-1f, 0f, 3.2f), 0f);
            b.Put(FT + "tree-high-crooked", new Vector3(1f, 0f, 3.2f), 200f);
        }

        /// <summary>Taberna 1 × 2: Stand auf Holzboden an der Straße, kleines Haus dahinter.</summary>
        private static void TabernaStall(Builder b)
        {
            House(b.At(0f, 1f), "wall", 1, doorLeft: true, chimney: false, roof: "roof", d: 2);
            Planks(b.At(0f, -1f), 2, 2);
            b.Put(FT + "stall-red", new Vector3(0f, 0f, -1.3f), 0f);
        }

        /// <summary>Taberna 1 × 2: schmale Halle mit breitem Tor, Stand davor.</summary>
        private static void TabernaHall(Builder b)
        {
            Hall(b.At(0f, 0.5f), 2, 3, 1, "roof-high", (i, j, dir, level) =>
                dir == Vector2Int.down ? (i == 0 ? "wall-wood-doorway-square-wide" : "wall-wood-window-small") :
                j == 1 ? "wall-wood-window-small" : "wall-wood");
            b.Put(FT + "stall-green", new Vector3(0f, 0f, -1.5f), 0f);
        }

        /// <summary>Kontor 2 × 3: zweistöckiger Steinbau mit Bögen, Stände davor.</summary>
        private static void OfficeHall(Builder b)
        {
            Hall(b.At(0f, 1f), 4, 4, 2, "roof", (i, j, dir, level) =>
                dir == Vector2Int.down
                    ? (level == 0 ? (i % 2 == 1 ? "wall-doorway-round" : "wall-window-stone") : "wall-window-shutters")
                    : j % 2 == 1 || level == 1 ? "wall-window-small" : "wall");
            b.Put(FT + "stall-red", new Vector3(-1f, 0f, -2.3f), 0f);
            b.Put(FT + "stall-green", new Vector3(1f, 0f, -2.3f), 0f);
        }

        /// <summary>Kontor 2 × 3: zwei Häuser an der Straße, Werkhof mit Holz und Karren dahinter.</summary>
        private static void OfficeYard(Builder b)
        {
            Row(b, "wall", 4, -1f, (2, "roof", false), (1, "roof", false));
            Planks(b.At(0f, 2f), 4, 2);
            b.Put(NK + "log_stack", new Vector3(-1f, 0f, 2f), 90f);
            b.Put(FT + "cart", new Vector3(1.1f, 0f, 2f), 80f);
        }

        /// <summary>Speicherhof 2 × 4: lange Holzhalle hinten, Hof mit Ständen vorn.</summary>
        private static void GranaryFront(Builder b)
        {
            Hall(b.At(0f, 1.5f), 4, 5, 1, "roof", (i, j, dir, level) =>
                dir == Vector2Int.down ? (i == 1 || i == 2 ? "wall-wood-doorway-square-wide" : "wall-wood-window-small") :
                j % 2 == 1 ? "wall-wood-window-small" : "wall-wood");
            Planks(b.At(0f, -2.5f), 4, 3);
            b.Put(FT + "stall-red", new Vector3(-1f, 0f, -3.4f), 0f);
            b.Put(FT + "stall", new Vector3(1f, 0f, -3.4f), 0f);
            b.Put(FT + "cart", new Vector3(1.2f, 0f, -1.8f), 80f);
        }

        /// <summary>Speicherhof 2 × 4: hohe Halle hinten, offener Platz mit zwei Reihen Ständen vorn.</summary>
        private static void GranaryYard(Builder b)
        {
            Hall(b.At(0f, 2f), 4, 4, 1, "roof-high", (i, j, dir, level) =>
                dir == Vector2Int.down ? (i == 1 ? "wall-wood-doorway-square-wide" : "wall-wood-window-small") :
                j % 2 == 1 ? "wall-wood-window-small" : "wall-wood");
            b.Put(FT + "stall-red", new Vector3(-1f, 0f, -3.3f), 0f);
            b.Put(FT + "stall-green", new Vector3(1f, 0f, -3.3f), 0f);
            b.Put(FT + "stall-green", new Vector3(-1f, 0f, -1.6f), 0f);
            b.Put(FT + "stall-red", new Vector3(1f, 0f, -1.6f), 0f);
            b.Put(NK + "log_stack", new Vector3(-1.2f, 0f, -0.3f), 90f);
        }

        /// <summary>
        /// Wandteil einer Fassade: zur Straße (Süden, äußerste Reihe) Fensterläden oder die Tür, sonst kleine
        /// Fenster im Wechsel mit glatter Wand.
        /// </summary>
        private static string Facade(int i, int j, Vector2Int dir, int level, string wall, bool door)
        {
            if (dir == Vector2Int.down && j == 0)
                return level == 0 && door ? $"{wall}-door" : $"{wall}-window-shutters";
            return (i + j) % 2 == 0 ? $"{wall}-window-small" : wall;
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

        /// <summary>
        /// Tempel 2 × 3: Cella aus Stein mit zwei Bogentüren, davor eine offene Vorhalle mit drei Säulen und
        /// darüber ein Satteldach aus Terrakotta (wie die Bibliothek, aber tiefer und ohne Fenster).
        /// </summary>
        private static void Temple(Builder b)
        {
            const int w = 2, d = 4;
            // Wände nur um die hinteren drei Reihen; die vordere Reihe ist die offene Vorhalle
            for (int i = 0; i < w; i++)
                for (int j = 1; j < d; j++)
                    foreach (var dir in Sides)
                    {
                        var n = new Vector2Int(i, j) + dir;
                        bool outside = n.x < 0 || n.x >= w || n.y < 1 || n.y >= d;
                        if (!outside) continue;
                        string part = dir == Vector2Int.down ? "wall-doorway-round" : "wall";
                        b.Put(FT + part, Cell(n.x, n.y, w, d), WallRot(dir));
                    }
            for (int k = 0; k <= w; k++)
                b.Put(FT + "pillar-stone", Cell(0, 0, w, d) + new Vector3(k - 0.5f, 0f, -0.5f), 0f);
            GableRoof(b, w, d, 1, "roof");
        }

        /// <summary>
        /// Schreibschule 2 × 2: kleiner Steinbau hinten mit Tür und Fenstern, davor ein Hof mit zwei Bänken unter
        /// freiem Himmel (Unterricht im Freien) und einer Laterne.
        /// </summary>
        private static void School(Builder b)
        {
            const int w = 2, d = 2;
            var hall = b.At(0f, 0.8f);
            Walls(hall, w, d, 0, (i, j, dir) =>
                dir == Vector2Int.down ? (i == 0 ? "wall-door" : "wall-window-stone") :
                dir == Vector2Int.up ? "wall" : "wall-window-stone");
            GableRoof(hall, w, d, 1, "roof");
            b.Put(FT + "stall-bench", new Vector3(-0.8f, 0f, -1.4f), 0f);
            b.Put(FT + "stall-bench", new Vector3(0.8f, 0f, -1.4f), 0f);
            b.Put(FT + "lantern", new Vector3(1.7f, 0f, -0.6f), 0f);
        }

        /// <summary>Brunnen 1 × 1: runder Brunnen mit klarem Wasser.</summary>
        private static void Well(Builder b)
        {
            b.Overrides["Water"] = Solid("Wasser", new Color(0.42f, 0.70f, 0.85f));
            b.Put(FT + "fountain-round", Vector3.zero, 0f);
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

        /// <summary>Holzfäller: Holzhütte hinten links, davor Stammstapel und Baumstümpfe, hinten rechts ein Baum.</summary>
        private static void Woodcutter(Builder b)
        {
            NatureColors(b);
            var hut = b.At(-1f, 1f);
            Walls(hut, 2, 2, 0, (i, j, dir) =>
                dir == Vector2Int.down && i == 0 ? "wall-wood-door" :
                dir == Vector2Int.down ? "wall-wood-window-small" : "wall-wood");
            GableRoof(hut, 2, 2, 1, "roof");

            b.Put(NK + "tree_oak", new Vector3(1.3f, 0f, 1.3f), 30f, 1.6f);
            b.Put(NK + "log_stackLarge", new Vector3(1.1f, 0f, -0.6f), 90f);
            b.Put(NK + "log_stack", new Vector3(-0.4f, 0f, -1.2f), 0f);
            b.Put(NK + "stump_roundDetailed", new Vector3(-1.6f, 0f, -1.4f), 0f);
            b.Put(NK + "stump_old", new Vector3(0.9f, 0f, -1.6f), 60f);
            b.Put(NK + "log", new Vector3(-1.5f, 0f, -0.5f), 75f);
        }

        /// <summary>Zimmerei: offener Schuppen auf Holzpfosten mit Rückwand, darunter ein Stamm, ringsum Stämme und Bretterstapel.</summary>
        private static void Carpentry(Builder b)
        {
            const int w = 2, d = 2;
            NatureColors(b);
            var shed = b.At(-0.6f, 0.8f);
            for (int x = 0; x <= w; x++)
                for (int z = 0; z <= d; z += d)
                    shed.Put(FT + "pillar-wood", new Vector3(x - w * 0.5f, 0f, z - d * 0.5f), 0f);
            Walls(shed, w, d, 0, (i, j, dir) => dir == Vector2Int.up ? "wall-wood" : null);
            GableRoof(shed, w, d, 1, "roof");
            shed.Put(NK + "log_large", Vector3.zero, 90f);

            // Bretterstapel aus halben Bohlen
            for (int k = 0; k < 5; k++)
            {
                b.Put(FT + "planks-half", new Vector3(-1.2f, k * 0.07f, -1.4f), 0f, 0.8f);
                b.Put(FT + "planks-half", new Vector3(0.2f, k * 0.07f, -1.4f), 0f, 0.8f);
            }
            b.Put(NK + "log_stack", new Vector3(1.5f, 0f, 1.0f), 90f);
            b.Put(NK + "log_stackLarge", new Vector3(1.4f, 0f, -0.6f), 0f);
            b.Put(FT + "cart", new Vector3(1.6f, 0f, -1.7f), 100f, 0.8f);
        }

        /// <summary>Steinbruch: Felswand hinten, davor Bruchsteine, behauene Blöcke und ein Karren.</summary>
        private static void Quarry(Builder b)
        {
            NatureColors(b);
            // Nature-Kit-Teile mit „stone“ sind graue Felsen; „rock“ sind Erdhügel mit Gras
            b.Put(NK + "stone_largeA", new Vector3(-1.2f, 0f, 1.2f), 0f, 2f);
            b.Put(NK + "stone_tallB", new Vector3(0.2f, 0f, 1.4f), 40f, 1.8f);
            b.Put(NK + "stone_largeC", new Vector3(1.4f, 0f, 1.1f), 200f, 1.8f);
            b.Put(NK + "stone_smallA", new Vector3(-1.4f, 0f, -0.2f), 0f, 1.6f);
            b.Put(NK + "stone_smallB", new Vector3(0.3f, 0f, 0.1f), 30f, 1.6f);
            b.Put(NK + "stone_smallFlatA", new Vector3(1.3f, 0f, -0.5f), 0f, 1.6f);
            b.Put(FT + "rock-wide", new Vector3(-0.3f, 0f, -0.9f), 15f, 0.8f);
            b.Put(FT + "rock-small", new Vector3(-1.4f, 0f, -1.5f), 0f, 0.8f);
            b.Put(FT + "cart", new Vector3(1.2f, 0f, -1.6f), 80f, 0.8f);
        }

        /// <summary>
        /// Eigene Farben für Holz, Laub und Stein aus dem Nature Kit; dessen Materialfarben wirken im Spiel blass
        /// oder türkis.
        /// </summary>
        private static void NatureColors(Builder b)
        {
            b.Overrides["woodBark"] = Solid("Rinde", new Color(0.45f, 0.30f, 0.18f));
            b.Overrides["woodInner"] = Solid("Holz", new Color(0.82f, 0.66f, 0.44f));
            b.Overrides["leafsGreen"] = Solid("Laub", new Color(0.33f, 0.52f, 0.22f));
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
            for (int i = 0; i < w; i++)
                for (int j = 0; j < d; j++)
                {
                    if (!mask[i, j]) continue;
                    foreach (var dir in Sides)
                    {
                        var n = new Vector2Int(i, j) + dir;
                        if (Filled(mask, n)) continue;
                        string part = pick(i, j, dir);
                        if (part == null) continue;
                        b.Put(FT + part, Cell(n.x, n.y, w, d) + Vector3.up * y, WallRot(dir));
                    }
                }
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

        /// <summary>Flachdach auf allen belegten Zellen der Maske.</summary>
        private static void FlatRoof(Builder b, bool[,] mask, float y)
        {
            int w = mask.GetLength(0), d = mask.GetLength(1);
            for (int i = 0; i < w; i++)
                for (int j = 0; j < d; j++)
                    if (mask[i, j]) b.Put(FT + "roof-flat", Cell(i, j, w, d) + Vector3.up * y, 0f);
        }

        /// <summary>Halle w × d (w gerade) über mehrere Stockwerke, mit parallelen Satteldächern.</summary>
        private static void Hall(Builder b, int w, int d, int storeys, string roof,
            Func<int, int, Vector2Int, int, string> pick)
        {
            for (int level = 0; level < storeys; level++)
            {
                int l = level;
                Walls(b, w, d, l, (i, j, dir) => pick(i, j, dir, l));
            }
            GableRoof(b, w, d, storeys, roof);
        }

        /// <summary>Holzboden für einen Werkhof.</summary>
        private static void Planks(Builder b, int w, int d)
        {
            for (int i = 0; i < w; i++)
                for (int j = 0; j < d; j++)
                    b.Put(FT + "planks", Cell(i, j, w, d), 0f);
        }

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
                    b.Put(FT + part, Cell(i, j, w, d) + Vector3.up * y, 180f); // Traufe nach Westen
                    b.Put(FT + part, Cell(i + 1, j, w, d) + Vector3.up * y, 0f); // Traufe nach Osten
                }
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

        private enum GroundKind { Gravel, Paving, Earth }

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
