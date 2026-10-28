using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EpochCity
{
    /// <summary>
    /// Richtet Boden, Horizont, Licht und Post-Processing der offenen Szene ein (stilisierter, warmer Look).
    /// Erzeugte Assets liegen unter Assets/Content/Look/. Mehrfaches Ausführen überschreibt nur die Werte.
    /// </summary>
    public static class SceneLookSetup
    {
        private const string Root = "Assets/Content/Look";

        // Farben an einer Stelle, damit sich der Look leicht nachjustieren lässt
        private static readonly Color GrassLight = new Color(0.42f, 0.57f, 0.30f);
        private static readonly Color GrassDark = new Color(0.30f, 0.45f, 0.22f);
        private static readonly Color GrassDry = new Color(0.50f, 0.55f, 0.33f);
        private static readonly Color OuterGround = new Color(0.35f, 0.49f, 0.25f);
        private static readonly Color Horizon = new Color(0.74f, 0.81f, 0.86f);
        private static readonly Color SunColor = new Color(1f, 0.94f, 0.83f);

        [MenuItem("EpochCity/Szenen-Look anwenden")]
        public static void Apply()
        {
            Directory.CreateDirectory(Root);

            SetupGround();
            SetupSkyAndFog();
            SetupLight();
            SetupShadows();
            SetupPostProcessing();

            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Szenen-Look angewendet.");
        }

        // ---------------------------------------------------------------- Boden

        private static void SetupGround()
        {
            var texture = CreateGroundTexture($"{Root}/Tex_Ground.png", 512);
            var ground = GetOrCreateMaterial("Mat_Ground");
            ground.SetTexture("_BaseMap", texture);
            ground.SetColor("_BaseColor", Color.white);
            ground.SetFloat("_Smoothness", 0.05f);
            EditorUtility.SetDirty(ground);

            var outer = GetOrCreateMaterial("Mat_GroundOuter");
            outer.SetTexture("_BaseMap", null);
            outer.SetColor("_BaseColor", OuterGround);
            outer.SetFloat("_Smoothness", 0.05f);
            EditorUtility.SetDirty(outer);

            var groundGo = GameObject.Find("Ground");
            if (groundGo != null) groundGo.GetComponent<Renderer>().sharedMaterial = ground;

            // Große Fläche unter der Karte, damit man am Rand nicht ins Leere schaut; der Nebel blendet sie aus
            var outerGo = GameObject.Find("GroundOuter");
            if (outerGo == null)
            {
                outerGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
                outerGo.name = "GroundOuter";
                Object.DestroyImmediate(outerGo.GetComponent<Collider>());
            }
            var center = groundGo != null ? groundGo.transform.position : new Vector3(64f, 0f, 64f);
            outerGo.transform.position = new Vector3(center.x, -0.05f, center.z);
            outerGo.transform.localScale = new Vector3(150f, 1f, 150f);
            outerGo.GetComponent<Renderer>().sharedMaterial = outer;
        }

        /// <summary>Wiesentextur mit großflächiger Farbvariation aus zwei Rauschebenen.</summary>
        private static Texture2D CreateGroundTexture(string path, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGB24, true);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size, v = (float)y / size;
                    float large = Mathf.PerlinNoise(u * 6f + 13.1f, v * 6f + 7.3f);
                    float small = Mathf.PerlinNoise(u * 40f + 3.7f, v * 40f + 1.9f);
                    float dry = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.62f, 0.85f,
                        Mathf.PerlinNoise(u * 3f + 31.5f, v * 3f + 17.2f)));

                    var c = Color.Lerp(GrassDark, GrassLight, large * 0.8f + small * 0.2f);
                    c = Color.Lerp(c, GrassDry, dry * 0.4f);

                    // Zum Rand hin in die Farbe der äußeren Fläche übergehen, damit keine harte Kante entsteht
                    float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
                    pixels[y * size + x] = Color.Lerp(OuterGround, c, Mathf.SmoothStep(0f, 1f, edge / 0.08f));
                }
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ---------------------------------------------------------------- Himmel und Nebel

        private static void SetupSkyAndFog()
        {
            string path = $"{Root}/Mat_Sky.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(sky, path);
            }
            sky.SetFloat("_SunSize", 0.03f);
            sky.SetFloat("_AtmosphereThickness", 0.85f);
            sky.SetColor("_SkyTint", new Color(0.52f, 0.62f, 0.78f));
            sky.SetColor("_GroundColor", Horizon);
            sky.SetFloat("_Exposure", 1.15f);
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Horizon;
            RenderSettings.fogStartDistance = 180f;
            RenderSettings.fogEndDistance = 600f;

            // Dreifarbiges Umgebungslicht: kühler Himmel, neutraler Horizont, warmer Boden
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.50f, 0.57f, 0.68f);
            RenderSettings.ambientEquatorColor = new Color(0.44f, 0.46f, 0.43f);
            RenderSettings.ambientGroundColor = new Color(0.26f, 0.25f, 0.21f);
        }

        // ---------------------------------------------------------------- Licht und Schatten

        private static void SetupLight()
        {
            Light sun = null;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
                if (l.type == LightType.Directional) { sun = l; break; }
            if (sun == null) return;

            // Warmer Nachmittagswinkel, seitlich zur Standardkamera (Blick nach Nordosten):
            // Schatten fallen sichtbar zur Seite, die Kamera schaut nicht in Gegenlicht
            sun.transform.rotation = Quaternion.Euler(42f, 140f, 0f);
            sun.color = SunColor;
            sun.intensity = 2f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            RenderSettings.sun = sun;
            EditorUtility.SetDirty(sun);
        }

        private static void SetupShadows()
        {
            // Schatten auch beim Herauszoomen (maximaler Kameraabstand 150)
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.shadowDistance = 220f;
                urp.shadowCascadeCount = 4;
                EditorUtility.SetDirty(urp);
            }
        }

        // ---------------------------------------------------------------- Post-Processing

        private static void SetupPostProcessing()
        {
            var volume = Object.FindAnyObjectByType<Volume>();
            if (volume == null || volume.sharedProfile == null) return;
            var profile = volume.sharedProfile;

            var tone = GetOrAdd<Tonemapping>(profile);
            tone.mode.Override(TonemappingMode.Neutral);

            var bloom = GetOrAdd<Bloom>(profile);
            bloom.intensity.Override(0.12f);
            bloom.threshold.Override(1.1f);

            var vignette = GetOrAdd<Vignette>(profile);
            vignette.intensity.Override(0.12f);

            var color = GetOrAdd<ColorAdjustments>(profile);
            color.postExposure.Override(0f);
            color.contrast.Override(10f);
            color.saturation.Override(0f);

            var white = GetOrAdd<WhiteBalance>(profile);
            white.temperature.Override(0f);

            EditorUtility.SetDirty(profile);
        }

        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet<T>(out var component))
            {
                component = profile.Add<T>(true);
                // Neue Komponenten müssen als Unterobjekt ins Profil-Asset, sonst gehen sie beim Neuladen verloren
                if (EditorUtility.IsPersistent(profile)) AssetDatabase.AddObjectToAsset(component, profile);
            }
            component.active = true;
            EditorUtility.SetDirty(component);
            return component;
        }

        private static Material GetOrCreateMaterial(string name)
        {
            string path = $"{Root}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
