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
            foreach (var n in new[] { "Laden", "Markt", "Handelshaus", "Taberna", "Kontor", "Bibliothek", "Tempel", "Brunnen",
                         "Schreibschule", "Feuerwache", "Werkstatthaus", "Handwerkerhaus", "Handwerkerzeile", "Werkhof", "Zunfthaus",
                         "Oelpresse", "Akademie", "Gelehrtenhaus", "Stadtvilla", "Villa", "Peristylhaus", "Kelterei", "GrossesTheater" })
                grounds[n] = paving;
            foreach (var n in new[] { "Werkstatt", "Werkstatthof", "Speicherhof", "Lager", "Muehle",
                         "Holzfaeller", "Holzhacker", "Foerster", "Zimmerei", "Steinbruch", "Olivenhain", "Lehmgrube", "Toepferei", "Weinberg" })
                grounds[n] = earth;
            grounds["Feld"] = GroundMaterial("Acker", GroundKind.Furrows);

            // Antike: Kalkstein und Terrakotta-Dächer
            Assign("Bld_Wohnhaus", Make("Wohnhaus", 1, antique,
                b => House(b, "wall", 1, doorLeft: true, chimney: false, roof: "roof"),
                b => House(b, "wall", 1, doorLeft: false, chimney: true, roof: "roof"),
                b => House(b, "wall", 1, doorLeft: true, chimney: false, roof: "roof-high")));
            Assign("Bld_Laden", Make("Laden", 1, antique, Shop));
            // Markt: Modell aus Blender (BlenderModels), hier nicht mehr erzeugen
            // Lager: Modell aus Blender (BlenderModels), hier nicht mehr erzeugen
            Assign("Bld_Bibliothek", Make("Bibliothek", 2, antique, Library));
            Assign("Bld_Tempel", Make("Tempel", new Vector2Int(2, 3), antique, Temple));
            Assign("Bld_Brunnen", Make("Brunnen", 1, antique, Well));
            Assign("Bld_Schreibschule", Make("Schreibschule", 2, antique, School));
            Assign("Bld_Feuerwache", Make("Feuerwache", 2, antique, FireStation));
            // Feld: vier Wachstumsstufen als Varianten in fester Reihenfolge; CropStages zeigt die passende
            var field = Make("Feld", 2, original,
                b => Field(b, FieldStage.Stubble), b => Field(b, FieldStage.Sown),
                b => Field(b, FieldStage.Green), b => Field(b, FieldStage.Ripe));
            AddToPrefab<CropStages>(field);
            Assign("Bld_Feld", field);
            // Holzfäller: Modell aus Blender (BlenderModels), hier nicht mehr erzeugen
            Assign("Bld_Holzhacker", Make("Holzhacker", 2, antique, WoodChopper));
            Assign("Bld_Foerster", Make("Foerster", 2, antique, Forester));
            Assign("Bld_Zimmerei", Make("Zimmerei", 2, antique, Carpentry));
            Assign("Bld_Steinbruch", Make("Steinbruch", 2, antique, Quarry));
            Assign("Bld_Olivenhain", Make("Olivenhain", 2, antique, OliveGrove));
            Assign("Bld_Oelpresse", Make("Oelpresse", 2, antique, OilPress));
            Assign("Bld_Lehmgrube", Make("Lehmgrube", 2, antique, ClayPit));
            Assign("Bld_Toepferei", Make("Toepferei", 2, antique, Pottery));
            Assign("Bld_Weinberg", Make("Weinberg", 2, antique, Vineyard));
            Assign("Bld_Kelterei", Make("Kelterei", 2, antique, WinePress));

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

            // Handwerkerzone: zweistöckige Häuser mit Laden unten, Bannern und Werkhöfen, Pflaster statt Kies
            Assign("Bld_Werkstatthaus", Make("Werkstatthaus", 1, antique,
                b => ArtisanHouse(b, 2, doorLeft: true, roof: "roof", banner: "banner-red"),
                b => ArtisanHouse(b, 2, doorLeft: false, roof: "roof-high", banner: "banner-green")));
            Assign("Bld_Handwerkerhaus", Make("Handwerkerhaus", new Vector2Int(1, 2), antique, ArtisanHouseShed, ArtisanHouseYard));
            Assign("Bld_Handwerkerzeile", Make("Handwerkerzeile", 2, antique, ArtisanRowAwnings, ArtisanRowTall));
            Assign("Bld_Werkhof", Make("Werkhof", new Vector2Int(2, 3), antique, WorkyardHall, WorkyardShed));
            Assign("Bld_Zunfthaus", Make("Zunfthaus", 3, antique, GuildHall, GuildCourt));

            // Gelehrtenzone und Akademie: Steinbauten mit Säulen, Innenhöfen und Gärten, auf Pflaster
            Assign("Bld_Akademie", Make("Akademie", new Vector2Int(2, 3), antique, Academy));
            // Großes Theater (Wunderbau): fünf Baustufen in fester Reihenfolge, ConstructionStages zeigt die zur Bauphase
            var theater = Make("GrossesTheater", 4, antique,
                b => Theater(b, 0), b => Theater(b, 1), b => Theater(b, 2), b => Theater(b, 3), b => Theater(b, 4));
            AddToPrefab<ConstructionStages>(theater);
            Assign("Bld_GrossesTheater", theater);
            Assign("Bld_Gelehrtenhaus", Make("Gelehrtenhaus", new Vector2Int(1, 2), antique, ScholarHouseGarden, ScholarHousePortico));
            Assign("Bld_Stadtvilla", Make("Stadtvilla", 2, antique, TownVillaPortico, TownVillaAtrium));
            Assign("Bld_Villa", Make("Villa", new Vector2Int(2, 3), antique, VillaGardenFront, VillaPeristyleBack));
            Assign("Bld_Peristylhaus", Make("Peristylhaus", 3, antique, PeristyleCourt, PeristyleHall));

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
            NatureColors(b);
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
            NatureColors(b);
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

        // ---------------------------------------------------------------- Handwerkerzone

        /// <summary>
        /// Handwerkerhaus 2 Teile breit und d tief: unten Laden (breite Öffnung) und Tür zur Straße mit Vordach,
        /// oben Wohnung mit Fensterläden und einem Banner über dem Laden, Satteldach.
        /// </summary>
        private static void ArtisanHouse(Builder b, int d, bool doorLeft, string roof, string banner, int storeys = 2)
        {
            const int w = 2;
            int shop = doorLeft ? 1 : 0;
            for (int level = 0; level < storeys; level++)
            {
                int l = level;
                Walls(b, w, d, l, (i, j, dir) =>
                {
                    if (dir == Vector2Int.down)
                        return l > 0 ? "wall-window-shutters" : i == shop ? "wall-doorway-square" : "wall-door";
                    if (dir == Vector2Int.up) return l == 0 ? "wall" : "wall-window-small";
                    return j % 2 == 0 ? "wall-window-small" : "wall";
                });
            }
            Side(b, "overhang", shop, 0, Vector2Int.down, 0, w, d);
            Side(b, banner, shop, 0, Vector2Int.down, 1, w, d);
            GableRoof(b, w, d, storeys, roof);
        }

        /// <summary>Offener Werkschuppen 2 × 2 Teile: vier Holzpfosten, Rückwand, Satteldach, Stämme darunter.</summary>
        private static void WorkShed(Builder b)
        {
            const int w = 2, d = 2;
            for (int x = 0; x <= w; x += w)
                for (int z = 0; z <= d; z += d)
                    b.Put(FT + "pillar-wood", new Vector3(x - w * 0.5f, 0f, z - d * 0.5f), 0f);
            Walls(b, w, d, 0, (i, j, dir) => dir == Vector2Int.up ? "wall-wood" : null);
            GableRoof(b, w, d, 1, "roof");
            b.Put(NK + "log_stack", new Vector3(0f, 0f, 0.2f), 90f);
        }

        /// <summary>Handwerkerhaus 1 × 2: Haus an der Straße, offener Werkschuppen dahinter.</summary>
        private static void ArtisanHouseShed(Builder b)
        {
            ArtisanHouse(b.At(0f, -1f), 2, doorLeft: true, roof: "roof", banner: "banner-green");
            WorkShed(b.At(0f, 1.2f));
        }

        /// <summary>Handwerkerhaus 1 × 2: tieferes Haus, dahinter ein Werkhof mit Bohlen und Karren.</summary>
        private static void ArtisanHouseYard(Builder b)
        {
            ArtisanHouse(b.At(0f, -0.5f), 3, doorLeft: false, roof: "roof-high", banner: "banner-red");
            Planks(b.At(0f, 1.5f), 2, 1);
            b.Put(FT + "cart", new Vector3(0f, 0f, 1.6f), 90f, 0.7f);
        }

        /// <summary>Handwerkerzeile 2 × 2: zwei Läden nebeneinander mit Vordächern, dahinter ein schmaler Hof.</summary>
        private static void ArtisanRowAwnings(Builder b)
        {
            ArtisanHouse(b.At(-1f, -0.5f), 3, doorLeft: true, roof: "roof", banner: "banner-red");
            ArtisanHouse(b.At(1f, -0.5f), 3, doorLeft: false, roof: "roof", banner: "banner-green");
            b.Put(FT + "stall-bench", new Vector3(-1f, 0f, 1.6f), 0f);
            b.Put(NK + "log_stack", new Vector3(1f, 0f, 1.6f), 90f);
        }

        /// <summary>Handwerkerzeile 2 × 2: ein dreistöckiges und ein zweistöckiges Haus, Werkhof dahinter.</summary>
        private static void ArtisanRowTall(Builder b)
        {
            ArtisanHouse(b.At(-1f, -1f), 2, doorLeft: true, roof: "roof-high", banner: "banner-green", storeys: 3);
            ArtisanHouse(b.At(1f, -1f), 2, doorLeft: false, roof: "roof", banner: "banner-red");
            Planks(b.At(0f, 1f), 4, 2);
            b.Put(FT + "cart", new Vector3(1f, 0f, 1.1f), 80f, 0.8f);
        }

        /// <summary>Werkhof 2 × 3: zwei Läden an der Straße, Hof mit Werkbank, hinten eine Holzhalle.</summary>
        private static void WorkyardHall(Builder b)
        {
            ArtisanHouse(b.At(-1f, -2f), 2, doorLeft: true, roof: "roof", banner: "banner-red");
            ArtisanHouse(b.At(1f, -2f), 2, doorLeft: false, roof: "roof-high", banner: "banner-green");
            Planks(b.At(0f, 0f), 4, 2);
            b.Put(FT + "stall-bench", new Vector3(-1f, 0f, 0f), 0f);
            b.Put(FT + "cart", new Vector3(1f, 0f, 0f), 80f, 0.8f);
            Hall(b.At(0f, 2f), 4, 2, 1, "roof", (i, j, dir, level) =>
                dir == Vector2Int.down ? (i == 1 || i == 2 ? "wall-wood-doorway-square-wide" : "wall-wood-window-small") :
                "wall-wood");
        }

        /// <summary>Werkhof 2 × 3: tiefes Haus links, offener Schuppen und Stapel rechts.</summary>
        private static void WorkyardShed(Builder b)
        {
            ArtisanHouse(b.At(-1f, -1f), 4, doorLeft: true, roof: "roof", banner: "banner-green");
            WorkShed(b.At(1f, -1.5f));
            WorkShed(b.At(1f, 1f));
            b.Put(NK + "log_stackLarge", new Vector3(-1f, 0f, 2.4f), 90f);
            b.Put(FT + "cart", new Vector3(1f, 0f, 2.6f), 90f, 0.8f);
        }

        /// <summary>Zunfthaus 3 × 3: zweistöckiger Steinbau mit Bogentor, Balkon und zwei Bannern, Werkhof dahinter.</summary>
        private static void GuildHall(Builder b)
        {
            Hall(b.At(0f, -1f), 6, 4, 2, "roof", (i, j, dir, level) =>
                dir == Vector2Int.down
                    ? (level == 0 ? (i == 2 || i == 3 ? "wall-doorway-round" : "wall-doorway-square")
                        : i == 2 || i == 3 ? "balcony-wall" : "wall-window-shutters")
                    : j % 2 == 1 || level == 1 ? "wall-window-small" : "wall");
            var front = b.At(0f, -1f);
            Side(front, "banner-red", 0, 0, Vector2Int.down, 1, 6, 4);
            Side(front, "banner-green", 5, 0, Vector2Int.down, 1, 6, 4);
            Planks(b.At(0f, 2.5f), 6, 1);
            b.Put(NK + "log_stack", new Vector3(-2f, 0f, 2.5f), 90f);
            b.Put(FT + "cart", new Vector3(2f, 0f, 2.5f), 90f, 0.8f);
        }

        /// <summary>Zunfthaus 3 × 3: drei Läden an der Straße, dahinter ein Hof mit Brunnen und Werkschuppen.</summary>
        private static void GuildCourt(Builder b)
        {
            ArtisanHouse(b.At(-2f, -2f), 2, doorLeft: true, roof: "roof", banner: "banner-red");
            ArtisanHouse(b.At(0f, -2f), 2, doorLeft: false, roof: "roof-high", banner: "banner-green", storeys: 3);
            ArtisanHouse(b.At(2f, -2f), 2, doorLeft: true, roof: "roof", banner: "banner-red");
            b.Overrides["Water"] = Solid("Wasser", new Color(0.42f, 0.70f, 0.85f));
            b.Put(FT + "fountain-round", new Vector3(-1f, 0f, 1.2f), 0f, 0.6f);
            WorkShed(b.At(1.8f, 1.8f));
        }

        // ---------------------------------------------------------------- Gelehrtenzone und Akademie

        /// <summary>Säulenreihe entlang x (count Säulen im Abstand 1) an der Kante z.</summary>
        private static void Colonnade(Builder b, float x0, float z, int count, float scale = 1f)
        {
            for (int k = 0; k < count; k++)
                b.Put(FT + "pillar-stone", new Vector3(x0 + k, 0f, z), 0f, scale);
        }

        /// <summary>
        /// Steinbau w × d mit offener Säulenvorhalle in der vordersten Reihe (wie Bibliothek und Tempel):
        /// Wände um die hinteren Reihen, Bogentür und Steinfenster, Säulen an der Vorderkante, Satteldächer.
        /// </summary>
        private static void PorticoHall(Builder b, int w, int d, int storeys = 1)
        {
            for (int level = 0; level < storeys; level++)
            {
                int l = level;
                for (int i = 0; i < w; i++)
                    for (int j = 1; j < d; j++)
                        foreach (var dir in Sides)
                        {
                            var n = new Vector2Int(i, j) + dir;
                            bool outside = n.x < 0 || n.x >= w || n.y < 1 || n.y >= d;
                            if (!outside) continue;
                            string part = dir == Vector2Int.down
                                ? (l == 0 && i == w / 2 ? "wall-doorway-round" : "wall-window-stone")
                                : dir == Vector2Int.up ? "wall" : "wall-window-stone";
                            b.Put(FT + part, Cell(n.x, n.y, w, d) + Vector3.up * l, WallRot(dir));
                        }
            }
            Colonnade(b, Cell(0, 0, w, d).x - 0.5f, Cell(0, 0, w, d).z - 0.5f, w + 1, storeys);
            GableRoof(b, w, d, storeys, "roof");
        }

        /// <summary>Akademie 2 × 3: breite Säulenhalle hinten, davor ein gepflasterter Hof mit Bänken und Brunnen.</summary>
        private static void Academy(Builder b)
        {
            PorticoHall(b.At(0f, 1f), 4, 4);
            b.Overrides["Water"] = Solid("Wasser", new Color(0.42f, 0.70f, 0.85f));
            b.Put(FT + "fountain-round", new Vector3(0f, 0f, -2.2f), 0f, 0.6f);
            b.Put(FT + "stall-bench", new Vector3(-1.5f, 0f, -2.2f), 0f);
            b.Put(FT + "stall-bench", new Vector3(1.5f, 0f, -2.2f), 0f);
        }

        /// <summary>Gelehrtenhaus 1 × 2: einstöckiger Steinbau mit Säulenvorhalle, Garten mit Baum dahinter.</summary>
        private static void ScholarHouseGarden(Builder b)
        {
            PorticoHall(b.At(0f, -0.5f), 2, 3);
            b.Put(FT + "tree-high-round", new Vector3(0f, 0f, 1.6f), 0f);
        }

        /// <summary>Gelehrtenhaus 1 × 2: zweistöckiges Haus an der Straße, Gartenhof mit Brunnen dahinter.</summary>
        private static void ScholarHousePortico(Builder b)
        {
            NatureColors(b);
            House(b.At(0f, -1f), "wall", 2, doorLeft: true, chimney: false, roof: "roof", d: 2);
            b.Overrides["Water"] = Solid("Wasser", new Color(0.42f, 0.70f, 0.85f));
            b.Put(FT + "fountain-round", new Vector3(0f, 0f, 1.1f), 0f, 0.4f);
            b.Put(NK + "plant_bushLarge", new Vector3(0.6f, 0f, 1.6f), 0f, 2f);
        }

        /// <summary>Stadtvilla 2 × 2: breite Säulenvorhalle über die ganze Front, Steinbau dahinter.</summary>
        private static void TownVillaPortico(Builder b)
        {
            PorticoHall(b, 4, 4);
        }

        /// <summary>Stadtvilla 2 × 2: einstöckiger Ring um ein Atrium mit Baum, zur Straße zweistöckig.</summary>
        private static void TownVillaAtrium(Builder b)
        {
            const int w = 4, d = 4;
            var ring = Mask(w, d, (i, j) => !(i >= 1 && i < 3 && j >= 1 && j < 3));
            var upper = Mask(w, d, (i, j) => j == 0);
            Walls(b, ring, 0, (i, j, dir) => Facade(i, j, dir, 0, "wall", door: i == 1));
            Walls(b, upper, 1, (i, j, dir) => Facade(i, j, dir, 1, "wall", door: false));
            FlatRoof(b, Mask(w, d, (i, j) => ring[i, j] && !upper[i, j]), 1);
            FlatRoof(b, upper, 2);
            b.Put(FT + "tree", new Vector3(0f, 0f, 0f), 0f);
        }

        /// <summary>Villa 2 × 3: Säulenhalle hinten, davor ein Garten mit Bäumen, Büschen und Brunnen.</summary>
        private static void VillaGardenFront(Builder b)
        {
            NatureColors(b);
            PorticoHall(b.At(0f, 1f), 4, 4);
            b.Overrides["Water"] = Solid("Wasser", new Color(0.42f, 0.70f, 0.85f));
            b.Put(FT + "fountain-round", new Vector3(0f, 0f, -2.2f), 0f, 0.6f);
            b.Put(FT + "tree-high-round", new Vector3(-1.6f, 0f, -2.3f), 0f);
            b.Put(NK + "plant_bushLarge", new Vector3(1.6f, 0f, -2.3f), 0f, 2.5f);
        }

        /// <summary>Villa 2 × 3: zweistöckiges Haus vorn, dahinter ein Säulenhof (Peristyl) mit Bäumen.</summary>
        private static void VillaPeristyleBack(Builder b)
        {
            Row(b, "wall", 2, -2f, (2, "roof", true), (2, "roof", false));
            Colonnade(b, -2f, 0f, 5);
            Colonnade(b, -2f, 3f, 5);
            b.Put(FT + "tree", new Vector3(-1f, 0f, 1.5f), 0f);
            b.Put(FT + "tree-high-round", new Vector3(1f, 0f, 1.5f), 40f);
        }

        /// <summary>Peristylhaus 3 × 3: Ring aus Steinbauten um einen Säulenhof mit Brunnen und Bäumen.</summary>
        private static void PeristyleCourt(Builder b)
        {
            const int w = 6, d = 6;
            var ring = Mask(w, d, (i, j) => !(i >= 2 && i < 4 && j >= 2 && j < 4));
            var upper = Mask(w, d, (i, j) => j < 2);
            Walls(b, ring, 0, (i, j, dir) => Facade(i, j, dir, 0, "wall", door: i == 2));
            Walls(b, upper, 1, (i, j, dir) => Facade(i, j, dir, 1, "wall", door: false));
            FlatRoof(b, Mask(w, d, (i, j) => ring[i, j] && !upper[i, j]), 1);
            FlatRoof(b, upper, 2);
            b.Overrides["Water"] = Solid("Wasser", new Color(0.42f, 0.70f, 0.85f));
            b.Put(FT + "fountain-round", new Vector3(0f, 0f, 0f), 0f, 0.5f);
            foreach (var p in new[] { new Vector3(-1f, 0f, -1f), new Vector3(1f, 0f, -1f), new Vector3(-1f, 0f, 1f), new Vector3(1f, 0f, 1f) })
                b.Put(FT + "pillar-stone", p, 0f);
        }

        /// <summary>Peristylhaus 3 × 3: zweistöckige Säulenhalle vorn, großer Garten mit Bäumen dahinter.</summary>
        private static void PeristyleHall(Builder b)
        {
            NatureColors(b);
            PorticoHall(b.At(0f, -1f), 6, 4, 2);
            b.Put(FT + "tree-high-round", new Vector3(-2f, 0f, 2.3f), 0f);
            b.Put(NK + "plant_bushLarge", new Vector3(0f, 0f, 2.3f), 0f, 3f);
            b.Put(FT + "tree", new Vector3(2f, 0f, 2.3f), 60f);
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
            NatureColors(b);
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

        /// <summary>
        /// Großes Theater 4 × 4 in einer Baustufe (0 = Baustelle, 1 = Fundament, 2 = Sitzränge, 3 = Bühnenhaus,
        /// 4 = fertig): halbrunde Sitzränge hinten (+z), Orchestra in der Mitte, Bühnenhaus mit Säulen vorn zur Straße
        /// (−z). Vier Ecksteine in allen Stufen, damit alle Varianten gleich skaliert werden.
        /// </summary>
        private static void Theater(Builder b, int stage)
        {
            NatureColors(b);
            var limestone = Solid("Kalkstein", new Color(0.86f, 0.82f, 0.72f));
            var limestoneDark = Solid("KalksteinDunkel", new Color(0.76f, 0.72f, 0.62f));
            var foundation = Solid("Fundament", new Color(0.62f, 0.58f, 0.52f));
            var dirt = Solid("Baugrube", new Color(0.45f, 0.34f, 0.24f));
            var marble = Solid("Marmor", new Color(0.93f, 0.92f, 0.88f));
            var timber = Solid("Gerüstholz", new Color(0.55f, 0.40f, 0.25f));
            const float edge = 4f;
            var center = new Vector3(0f, 0f, -1.2f); // Mitte der Orchestra

            // Ecksteine (in allen Stufen gleich)
            foreach (var x in new[] { -edge + 0.15f, edge - 0.15f })
                foreach (var z in new[] { -edge + 0.15f, edge - 0.15f })
                    b.PutBox(new Vector3(x, 0.15f, z), new Vector3(0.3f, 0.3f, 0.3f), limestoneDark);

            if (stage == 0)
            {
                // Baustelle: ausgehobene Grube in Theaterform, Pflöcke, Baustoffe, Karren
                b.PutBox(new Vector3(0f, 0.02f, 0.6f), new Vector3(7.4f, 0.04f, 5.2f), dirt);
                for (int k = 0; k <= 8; k++)
                {
                    float a = Mathf.PI * k / 8f;
                    b.PutBox(center + new Vector3(Mathf.Cos(a) * 3.8f, 0.25f, Mathf.Sin(a) * 3.8f),
                        new Vector3(0.08f, 0.5f, 0.08f), timber);
                }
                b.Put(NK + "stone_largeA", new Vector3(-2.8f, 0f, -3.2f), 20f, 0.9f);
                b.Put(NK + "stone_largeC", new Vector3(-1.6f, 0f, -3.3f), 70f, 0.9f);
                b.Put(FT + "planks-half", new Vector3(2.2f, 0f, -3.2f), 0f);
                b.Put(FT + "planks-half", new Vector3(2.2f, 0.2f, -3.2f), 90f);
                b.Put(FT + "cart", new Vector3(0.4f, 0f, -3.3f), 90f);
                return;
            }

            // Fundament: Sockel der Sitzränge als Halbring, Orchestra-Boden, Platte des Bühnenhauses
            const int segments = 14;
            for (int k = 0; k < segments; k++)
            {
                float a = Mathf.PI * (k + 0.5f) / segments;
                float deg = -a * Mathf.Rad2Deg + 90f;
                b.At(center.x + Mathf.Cos(a) * 2.6f, center.z + Mathf.Sin(a) * 2.6f, deg)
                    .PutBox(new Vector3(0f, 0.1f, 0f), new Vector3(2.6f * Mathf.PI / segments + 0.05f, 0.2f, 2.6f), foundation);
            }
            b.PutBox(center + new Vector3(0f, 0.04f, 0f), new Vector3(2.4f, 0.08f, 2.4f), stage >= 4 ? marble : foundation);
            b.PutBox(new Vector3(0f, 0.15f, -3.1f), new Vector3(7.2f, 0.3f, 1.3f), foundation);

            if (stage == 1)
            {
                b.Put(NK + "stone_largeA", new Vector3(-3.2f, 0f, 2.8f), 40f, 0.9f);
                b.Put(NK + "stone_largeC", new Vector3(3.2f, 0f, 2.8f), 10f, 0.9f);
                b.Put(FT + "cart", new Vector3(2.6f, 0f, -2.2f), 0f);
                return;
            }

            // Sitzränge: konzentrische Stufen, nach außen höher
            const int rows = 6;
            for (int r = 0; r < rows; r++)
            {
                float radius = 1.45f + r * 0.42f;
                float height = 0.25f + r * 0.22f;
                int segs = 10 + r * 2;
                for (int k = 0; k < segs; k++)
                {
                    float a = Mathf.PI * (k + 0.5f) / segs;
                    float deg = -a * Mathf.Rad2Deg + 90f;
                    b.At(center.x + Mathf.Cos(a) * radius, center.z + Mathf.Sin(a) * radius, deg)
                        .PutBox(new Vector3(0f, height * 0.5f, 0f),
                            new Vector3(radius * Mathf.PI / segs + 0.04f, height, 0.44f), r % 2 == 0 ? limestone : limestoneDark);
                }
            }

            if (stage == 2)
            {
                // Gerüst für das Bühnenhaus
                for (int k = 0; k < 4; k++)
                    b.Put(FT + "poles", new Vector3(-2.7f + k * 1.8f, 0.3f, -3.1f), 0f);
                b.Put(FT + "planks-half", new Vector3(3.3f, 0f, -2.2f), 0f);
                return;
            }

            // Bühnenhaus: Wand mit Bogentor, zur Straße eine Säulenhalle unter einem Gesims, innen die Holzbühne
            b.PutBox(new Vector3(-2.15f, 1.3f, -3.1f), new Vector3(2.7f, 2.0f, 0.4f), limestone);
            b.PutBox(new Vector3(2.15f, 1.3f, -3.1f), new Vector3(2.7f, 2.0f, 0.4f), limestone);
            b.PutBox(new Vector3(0f, 1.95f, -3.1f), new Vector3(1.6f, 0.7f, 0.4f), limestone); // über dem Tor
            b.PutBox(new Vector3(0f, 2.45f, -3.45f), new Vector3(7.3f, 0.3f, 1.1f), limestoneDark);
            b.PutBox(new Vector3(0f, 0.45f, -2.4f), new Vector3(6.6f, 0.3f, 1.0f), timber);
            for (int k = 0; k < 8; k++)
                b.Put(FT + "pillar-stone", new Vector3(-3.5f + k, 0.3f, -3.75f), 0f, new Vector3(0.7f, 1.8f, 0.7f));

            if (stage == 3)
            {
                b.Put(FT + "cart", new Vector3(3.2f, 0f, 3.2f), 30f);
                return;
            }

            // Fertig: Banner, Laternen, Büsche an den Rängen
            // Banner hängen an der Wand zur Straße (die Kenney-Banner sitzen an einer Zellkante, +x nach außen)
            b.Put(FT + "banner-red", new Vector3(-2.15f, 0.3f, -3.8f), 90f);
            b.Put(FT + "banner-red", new Vector3(2.15f, 0.3f, -3.8f), 90f);
            b.Put(FT + "lantern", new Vector3(-3.6f, 0f, -2.0f), 0f);
            b.Put(FT + "lantern", new Vector3(3.6f, 0f, -2.0f), 0f);
            foreach (var x in new[] { -3.6f, 3.6f })
                b.Put(NK + "plant_bushLarge", new Vector3(x, 0f, 1.6f), 0f, 0.8f);
        }

        /// <summary>Feuerwache: zweistöckiges Wachhaus mit zwei Bogentoren und rotem Banner, davor Wasserbecken, Eimer und Karren.</summary>
        private static void FireStation(Builder b)
        {
            NatureColors(b);
            b.Overrides["Water"] = Solid("Wasser", new Color(0.42f, 0.70f, 0.85f));
            const int w = 2, d = 2;
            var hall = b.At(0f, 0.7f);
            Walls(hall, w, d, 0, (i, j, dir) =>
                dir == Vector2Int.down ? "wall-doorway-round" : "wall");
            Walls(hall, w, d, 1, (i, j, dir) =>
                dir == Vector2Int.down || j == 0 ? "wall-window-stone" : "wall");
            GableRoof(hall, w, d, 2, "roof-high");
            Side(hall, "banner-red", 0, 0, Vector2Int.down, 1, w, d);

            b.Put(FT + "fountain-square", new Vector3(-1.1f, 0f, -1.35f), 0f, 0.45f);
            for (int k = 0; k < 3; k++)
                b.Put(NK + "stump_round", new Vector3(0.3f + k * 0.35f, 0f, -1.5f), k * 40f, 0.55f);
            b.Put(FT + "cart", new Vector3(1.55f, 0f, -0.9f), 90f);
            b.Put(FT + "lantern", new Vector3(-1.75f, 0f, -0.3f), 0f);
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

        private enum FieldStage { Stubble, Sown, Green, Ripe }

        /// <summary>
        /// Feld in einer Wachstumsstufe: Furchenboden (Bodenplatte), 6 Reihen entlang x, Zaun hinten und links (in allen
        /// Stufen gleich, damit alle Varianten gleich skaliert werden). Reihen aus Quadern mit welliger Oberkante, damit
        /// das Feld von oben geschlossen wirkt; Kenney-Halme obendrauf für die Struktur.
        /// </summary>
        private static void Field(Builder b, FieldStage stage)
        {
            // Zwei Töne je Stufe, damit die Reihen nicht wie Blöcke wirken; Kenney-Halme nur im reifen Feld
            var ripe = Solid("Weizen", new Color(0.84f, 0.67f, 0.30f));
            var ripeDark = Solid("WeizenDunkel", new Color(0.74f, 0.56f, 0.24f));
            var green = Solid("WeizenGruen", new Color(0.42f, 0.58f, 0.22f));
            var greenDark = Solid("WeizenGruenDunkel", new Color(0.33f, 0.49f, 0.18f));
            var stubble = Solid("Stoppeln", new Color(0.60f, 0.48f, 0.28f));
            var stubbleDark = Solid("StoppelnDunkel", new Color(0.50f, 0.39f, 0.22f));
            b.Overrides["woodInner"] = ripe;
            b.Overrides["_defaultMat"] = ripe;

            const int rows = 6, segments = 12;
            const float rowSpacing = 0.64f, segment = 0.293f, rowStart = -1.8f;
            for (int r = 0; r < rows; r++)
            {
                float z = (r - (rows - 1) * 0.5f) * rowSpacing;
                for (int s = 0; s < segments; s++)
                {
                    float x = rowStart + (s + 0.5f) * segment;
                    float jitter = Hash(r, s, 7);
                    bool dark = Hash(r, s, 11) < 0.4f;
                    switch (stage)
                    {
                        case FieldStage.Stubble:
                        {
                            float h = 0.05f * (0.6f + 0.8f * jitter);
                            b.PutBox(new Vector3(x, h * 0.5f, z), new Vector3(segment, h, 0.42f), dark ? stubbleDark : stubble);
                            break;
                        }
                        case FieldStage.Sown:
                            b.PutBox(new Vector3(x, 0.025f, z), new Vector3(segment, 0.05f, 0.3f),
                                     Solid("AckerDamm", new Color(0.40f, 0.28f, 0.18f)));
                            b.PutBox(new Vector3(x, 0.07f, z + (jitter - 0.5f) * 0.1f), new Vector3(0.06f, 0.06f, 0.06f), green);
                            break;
                        case FieldStage.Green:
                        {
                            float h = 0.18f * (0.75f + 0.5f * jitter);
                            b.PutBox(new Vector3(x, h * 0.5f, z), new Vector3(segment, h, 0.48f), dark ? greenDark : green);
                            break;
                        }
                        default:
                        {
                            float h = 0.28f * (0.75f + 0.5f * jitter);
                            b.PutBox(new Vector3(x, h * 0.5f, z), new Vector3(segment, h, 0.52f), dark ? ripeDark : ripe);
                            if (s % 3 == r % 3) b.Put(NK + "crops_wheatStageB", new Vector3(x, 0f, z), jitter * 360f);
                            break;
                        }
                    }
                }
            }

            // Nach der Ernte: Garben am Feldrand
            if (stage == FieldStage.Stubble)
                for (int k = 0; k < 3; k++)
                    b.Put(NK + "crops_wheatStageB", new Vector3(1.85f, 0f, -1.1f + k * 0.8f), k * 70f, 0.9f);

            FieldFence(b);
        }

        /// <summary>Niedriger Holzzaun hinten und links: Pfosten je Meter, zwei Latten.</summary>
        private static void FieldFence(Builder b)
        {
            var wood = Solid("Zaunholz", new Color(0.45f, 0.32f, 0.20f));
            const float edge = 2f, post = 0.08f, height = 0.38f;
            for (int k = 0; k <= 4; k++)
            {
                float t = k - edge;
                b.PutBox(new Vector3(t, height * 0.5f, edge), new Vector3(post, height, post), wood);
                if (k < 4) b.PutBox(new Vector3(-edge, height * 0.5f, t), new Vector3(post, height, post), wood);
            }
            foreach (float y in new[] { 0.16f, 0.32f })
            {
                b.PutBox(new Vector3(0f, y, edge), new Vector3(edge * 2f, 0.04f, 0.03f), wood);
                b.PutBox(new Vector3(-edge, y, 0f), new Vector3(0.03f, 0.04f, edge * 2f), wood);
            }
        }

        /// <summary>Holzhacker: offener Holzschuppen rechts mit Brennholzstapeln, links Hackklotz mit Scheiten, hinten Karren und Stämme.</summary>
        private static void WoodChopper(Builder b)
        {
            NatureColors(b);
            WorkShed(b.At(1f, 0.6f));
            // Der Schuppen voller Brennholz: zwei Lagen Stapel bis unters Dach
            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 2; y++)
                    b.Put(NK + "log_stack", new Vector3(0.35f + x * 0.65f, y * 0.32f, 1.1f), 0f);
            b.Put(NK + "log_stack", new Vector3(1.6f, 0f, -1.1f), 0f);
            b.Put(NK + "log_stack", new Vector3(1.6f, 0.32f, -1.1f), 0f);

            // Hackklotz mit Scheiten ringsum
            b.Put(NK + "stump_roundDetailed", new Vector3(-0.9f, 0f, -0.6f), 0f, 1.5f);
            for (int k = 0; k < 7; k++)
            {
                float a = k * 51f * Mathf.Deg2Rad;
                b.Put(NK + "log", new Vector3(-0.9f + Mathf.Cos(a) * 0.55f, 0f, -0.6f + Mathf.Sin(a) * 0.45f), k * 47f, 0.45f);
            }
            b.Put(NK + "log_stackLarge", new Vector3(-1.2f, 0f, 1.3f), 0f);
            b.Put(FT + "cart", new Vector3(-0.2f, 0f, 1.6f), 90f);
        }

        /// <summary>Förster: Holzhütte hinten links, rechts und vorn Reihen junger Bäume (Setzlinge), vorn ein Karren.</summary>
        private static void Forester(Builder b)
        {
            NatureColors(b);
            var hut = b.At(-1f, 1f);
            Walls(hut, 2, 2, 0, (i, j, dir) =>
                dir == Vector2Int.down && i == 1 ? "wall-wood-door" :
                dir == Vector2Int.down ? "wall-wood-window-small" : "wall-wood");
            GableRoof(hut, 2, 2, 1, "roof");

            for (int row = 0; row < 3; row++)
                for (int k = 0; k < 2; k++)
                    b.Put(NK + (row % 2 == 0 ? "tree_small" : "tree_pineSmallA"),
                        new Vector3(0.75f + k * 0.8f, 0f, 1.5f - row * 0.85f), (row * 2 + k) * 55f, 0.9f);
            for (int k = 0; k < 4; k++)
                b.Put(NK + "plant_bushSmall", new Vector3(-1.6f + k * 0.5f, 0f, -0.6f), k * 80f, 0.9f);
            b.Put(FT + "cart", new Vector3(-0.9f, 0f, -1.5f), 0f, 0.8f);
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

        /// <summary>Olivenhain: drei Reihen breiter, niedriger Bäume mit graugrünem Laub, vorn Karren und Bank.</summary>
        private static void OliveGrove(Builder b)
        {
            NatureColors(b);
            b.Overrides["leafsGreen"] = Solid("Olivenlaub", new Color(0.50f, 0.58f, 0.40f));
            for (int row = 0; row < 3; row++)
                for (int k = 0; k < 3; k++)
                    b.Put(NK + "tree_oak", new Vector3(-1.3f + k * 1.3f, 0f, -0.6f + row * 1.0f),
                        (row * 3 + k) * 47f, 1.0f);
            b.Put(FT + "cart", new Vector3(-0.8f, 0f, -1.7f), 0f, 0.8f);
            b.Put(FT + "stall-bench", new Vector3(1f, 0f, -1.7f), 0f, 0.8f);
        }

        /// <summary>Ölpresse: kleiner Steinbau hinten, vorn ein großer Mühlstein, daneben Holz für die Presse.</summary>
        private static void OilPress(Builder b)
        {
            Hall(b.At(0f, 0.8f), 2, 2, 1, "roof", (i, j, dir, level) =>
                dir == Vector2Int.down ? (i == 0 ? "wall-door" : "wall-window-stone") :
                dir == Vector2Int.up ? "wall" : "wall-window-small");
            b.Put(FT + "wheel", new Vector3(-0.6f, 0f, -1.3f), 90f, 1.6f);
            b.Put(NK + "log_stack", new Vector3(1.3f, 0f, -1.3f), 90f, 0.8f);
        }

        /// <summary>Weinberg: vier Rebzeilen parallel zur Straße (Hecken mit Pfosten an den Enden), vorn Karren und Bank.</summary>
        private static void Vineyard(Builder b)
        {
            NatureColors(b);
            for (int row = 0; row < 4; row++)
            {
                float z = -0.6f + row * 0.7f;
                for (int k = 0; k < 4; k++)
                    b.Put(FT + "hedge", new Vector3(-1.35f + k * 0.9f, 0f, z), 90f, 0.85f);
                b.Put(FT + "pillar-wood", new Vector3(-1.8f, 0f, z), 0f, 0.35f);
                b.Put(FT + "pillar-wood", new Vector3(1.8f, 0f, z), 0f, 0.35f);
            }
            b.Put(FT + "cart", new Vector3(-0.8f, 0f, -1.65f), 0f, 0.8f);
            b.Put(FT + "stall-bench", new Vector3(1f, 0f, -1.65f), 0f, 0.8f);
        }

        /// <summary>Kelterei: Steinbau hinten, vorn Bottiche mit Wein (Baumstümpfe in dunklem Holz) und ein Rad der Presse.</summary>
        private static void WinePress(Builder b)
        {
            Hall(b.At(0f, 0.8f), 2, 2, 1, "roof", (i, j, dir, level) =>
                dir == Vector2Int.down ? (i == 1 ? "wall-door" : "wall-window-shutters") :
                dir == Vector2Int.up ? "wall" : "wall-window-small");
            b.Overrides["woodBark"] = Solid("Fassholz", new Color(0.42f, 0.26f, 0.16f));
            b.Overrides["woodInner"] = Solid("Weinrot", new Color(0.45f, 0.12f, 0.18f));
            for (int k = 0; k < 3; k++)
                b.Put(NK + "stump_round", new Vector3(-1.4f + k * 0.6f, 0f, -1.35f), k * 40f, 1.4f);
            b.Put(FT + "wheel", new Vector3(1.1f, 0f, -1.35f), 90f, 1.2f);
        }

        /// <summary>Lehmgrube: Erdhügel aus Lehm (Nature-Kit-Hügel ohne Gras), Bohlen am Rand, ein Karren.</summary>
        private static void ClayPit(Builder b)
        {
            NatureColors(b);
            var clay = Solid("Lehm", new Color(0.62f, 0.42f, 0.28f));
            b.Overrides["dirt"] = clay;
            b.Overrides["grass"] = clay;
            b.Put(NK + "rock_largeA", new Vector3(-1f, 0f, 0.9f), 0f, 1.6f);
            b.Put(NK + "rock_largeC", new Vector3(1.1f, 0f, 1.1f), 120f, 1.4f);
            b.Put(NK + "rock_smallA", new Vector3(0.3f, 0f, -0.2f), 40f, 1.6f);
            Planks(b.At(-1.2f, -1.4f), 2, 1);
            b.Put(FT + "cart", new Vector3(1.2f, 0f, -1.3f), 80f, 0.8f);
        }

        /// <summary>Töpferei: kleiner Steinbau, davor zwei Reihen Gefäße in Terrakotta und eine Bank.</summary>
        private static void Pottery(Builder b)
        {
            const int w = 2, d = 2;
            var hall = b.At(0f, 0.8f);
            Walls(hall, w, d, 0, (i, j, dir) =>
                dir == Vector2Int.down ? (i == 0 ? "wall-door" : "wall-window-shutters") :
                dir == Vector2Int.up ? "wall" : "wall-window-small");
            GableRoof(hall, w, d, 1, "roof");
            b.Put(FT + "stall-bench", new Vector3(1.5f, 0f, -0.9f), 0f);
            // Gefäße: Kürbisse aus dem Nature Kit, ganz in Terrakotta (Körper heißt „leafsFall“)
            var terracotta = Solid("Keramik", new Color(0.72f, 0.38f, 0.22f));
            foreach (var name in new[] { "_defaultMat", "leafsFall", "woodBark", "grass", "dirt" })
                b.Overrides[name] = terracotta;
            for (int row = 0; row < 2; row++)
                for (int k = 0; k < 4; k++)
                    b.Put(NK + "crop_pumpkin", new Vector3(-1.5f + k * 0.55f, 0f, -1.6f + row * 0.55f),
                        (row * 4 + k) * 50f, 0.9f);
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

        private enum GroundKind { Gravel, Paving, Earth, Furrows }

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
