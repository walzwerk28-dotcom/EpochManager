using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Zeigt Feuer (FireSimulation) nur an: brennende Gebäude bekommen Flammen und Rauch (Partikel, im Code erzeugt),
    /// brennende und beschädigte werden rußig eingefärbt (MaterialPropertyBlock, wie das Anbindungs-Overlay; ist das
    /// Overlay an, zeigt es seine Farben), abgebrannte Zonengebäude hinterlassen Trümmer mit Rauch, bis auf dem Feld
    /// neu gebaut wird oder rubbleSeconds vergangen sind. Nach dem Laden wird alles aus dem Zustand neu aufgebaut.
    /// </summary>
    public class FireVisuals : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [SerializeField] private FireSimulation fire;
        [SerializeField] private GameClock clock;
        [SerializeField] private SaveSystem saveSystem;
        [SerializeField] private ConnectionOverlay overlay;
        [Tooltip("Farbe, mit der brennende und beschädigte Gebäude multipliziert werden (Ruß)")]
        public Color sootColor = new Color(0.38f, 0.34f, 0.31f);
        [Tooltip("Spielsekunden, die Trümmer höchstens liegen bleiben (verschwinden früher, wenn neu gebaut wird)")]
        [Min(1f)] public float rubbleSeconds = 90f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private readonly Dictionary<BuildingInstance, GameObject> flames = new Dictionary<BuildingInstance, GameObject>();
        private readonly List<Rubble> rubble = new List<Rubble>();
        private MaterialPropertyBlock block;
        private Material particleMaterial;
        private Material charredMaterial;

        private class Rubble
        {
            public GameObject View;
            public Vector2Int Origin;
            public float Seconds;
        }

        private void OnEnable()
        {
            if (fire != null)
            {
                fire.Ignited += OnIgnited;
                fire.Extinguished += OnFireEnded;
                fire.Damaged += OnFireEnded;
                fire.Repaired += OnRepaired;
                fire.Destroyed += OnDestroyed;
            }
            if (saveSystem != null) saveSystem.Loaded += OnLoaded;
        }

        private void OnDisable()
        {
            if (fire != null)
            {
                fire.Ignited -= OnIgnited;
                fire.Extinguished -= OnFireEnded;
                fire.Damaged -= OnFireEnded;
                fire.Repaired -= OnRepaired;
                fire.Destroyed -= OnDestroyed;
            }
            if (saveSystem != null) saveSystem.Loaded -= OnLoaded;
        }

        /// <summary>Farbe eines Gebäudes außerhalb des Overlays: rußig, solange es brennt oder beschädigt ist.</summary>
        public Color? TintFor(BuildingInstance b) => b != null && b.Fire != FireState.None ? sootColor : (Color?)null;

        private void Update()
        {
            if (rubble.Count == 0 || city == null) return;
            float dt = Time.deltaTime * (clock != null ? clock.Speed : 1);
            for (int i = rubble.Count - 1; i >= 0; i--)
            {
                var r = rubble[i];
                r.Seconds -= dt;
                if (r.Seconds > 0f && city.Grid.GetAt(r.Origin) == null) continue;
                if (r.View != null) Destroy(r.View);
                rubble.RemoveAt(i);
            }
        }

        // ---------------------------------------------------------------- Ereignisse

        private void OnIgnited(BuildingInstance b)
        {
            ApplyTint(b);
            if (b.View == null || flames.ContainsKey(b)) return;
            flames[b] = CreateFlames(b.View.transform, b.Footprint, b.View.transform.position, ModelHeight(b.View));
        }

        private void OnFireEnded(BuildingInstance b)
        {
            RemoveFlames(b);
            ApplyTint(b);
        }

        private void OnRepaired(BuildingInstance b) => ApplyTint(b);

        private void OnDestroyed(BuildingDefinition def, Vector2Int origin, float facing)
        {
            foreach (var key in new List<BuildingInstance>(flames.Keys))
                if (key.Origin == origin) RemoveFlames(key);
            var size = def.GetFootprint(facing);
            var center = city.GetFootprintCenter(size, origin);
            rubble.Add(new Rubble { View = CreateRubble(size, center), Origin = origin, Seconds = rubbleSeconds });
        }

        private void OnLoaded(SaveGame _) => Rebuild();

        /// <summary>Nach dem Laden: Flammen, Ruß und Trümmer aus dem Zustand neu aufbauen.</summary>
        private void Rebuild()
        {
            foreach (var f in flames.Values) if (f != null) Destroy(f);
            flames.Clear();
            foreach (var r in rubble) if (r.View != null) Destroy(r.View);
            rubble.Clear();
            foreach (var b in city.Buildings)
            {
                if (b.Fire == FireState.Burning) OnIgnited(b);
                else ApplyTint(b);
            }
        }

        private void RemoveFlames(BuildingInstance b)
        {
            if (!flames.TryGetValue(b, out var f)) return;
            flames.Remove(b);
            if (f == null) return;
            // Flammen aus, der Rauch zieht noch ab
            foreach (var ps in f.GetComponentsInChildren<ParticleSystem>())
                ps.Stop(true, ps.name == "Flames" ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
            Destroy(f, 5f);
        }

        private void ApplyTint(BuildingInstance b)
        {
            if (b?.View == null || (overlay != null && overlay.Visible)) return;
            if (block == null) block = new MaterialPropertyBlock();
            var tint = TintFor(b);
            foreach (var r in b.View.GetComponentsInChildren<MeshRenderer>())
            {
                if (tint == null) { r.SetPropertyBlock(null); continue; }
                block.Clear();
                block.SetColor(BaseColorId, tint.Value);
                block.SetColor(ColorId, tint.Value);
                r.SetPropertyBlock(block);
            }
        }

        // ---------------------------------------------------------------- Partikel und Trümmer

        /// <summary>Höhe des sichtbaren Modells über dem Boden (Flammen schlagen aus dem Dach).</summary>
        private static float ModelHeight(GameObject view)
        {
            float top = 1f;
            foreach (var r in view.GetComponentsInChildren<MeshRenderer>())
                if (r.enabled && r.gameObject.activeInHierarchy) top = Mathf.Max(top, r.bounds.max.y - view.transform.position.y);
            return top;
        }

        /// <summary>Flammen und Rauch über der Grundfläche (Breite × Tiefe in Feldern), height = Höhe des Modells.</summary>
        private GameObject CreateFlames(Transform parent, Vector2Int footprint, Vector3 position, float height)
        {
            var root = new GameObject("Fire");
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.rotation = Quaternion.identity;

            float cell = city.Grid.CellSize;
            var area = new Vector3(footprint.x * cell * 0.7f, 0.2f, footprint.y * cell * 0.7f);
            float scale = Mathf.Sqrt(footprint.x * footprint.y);

            var flame = CreateSystem(root.transform, "Flames", area, height * 0.7f);
            var main = flame.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f * scale, 0.8f * scale);
            main.maxParticles = 400;
            var emission = flame.emission;
            emission.rateOverTime = 30f * footprint.x * footprint.y;
            var col = flame.colorOverLifetime;
            col.enabled = true;
            col.color = Gradient(new[] { new Color(1f, 0.85f, 0.35f), new Color(1f, 0.45f, 0.1f), new Color(0.7f, 0.15f, 0.05f) },
                new[] { 0.9f, 0.8f, 0f });
            var size = flame.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

            var smoke = CreateSystem(root.transform, "Smoke", area, height);
            main = smoke.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f * scale, 1.1f * scale);
            main.maxParticles = 300;
            emission = smoke.emission;
            emission.rateOverTime = 10f * footprint.x * footprint.y;
            col = smoke.colorOverLifetime;
            col.enabled = true;
            col.color = Gradient(new[] { new Color(0.25f, 0.23f, 0.22f), new Color(0.45f, 0.44f, 0.43f), new Color(0.6f, 0.6f, 0.6f) },
                new[] { 0.7f, 0.45f, 0f });
            size = smoke.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.2f));
            var velocity = smoke.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(0.15f, 0.35f); // leichter Wind
            velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            return root;
        }

        private ParticleSystem CreateSystem(Transform parent, string name, Vector3 area, float height)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.up * height;
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // Partikel steigen nach oben
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;
            main.playOnAwake = true;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(area.x, area.z, area.y); // gedreht: z der Form zeigt nach oben
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = ParticleMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            return ps;
        }

        private static Gradient Gradient(Color[] colors, float[] alphas)
        {
            var g = new Gradient();
            var ck = new GradientColorKey[colors.Length];
            var ak = new GradientAlphaKey[alphas.Length];
            for (int i = 0; i < colors.Length; i++) ck[i] = new GradientColorKey(colors[i], i / (float)(colors.Length - 1));
            for (int i = 0; i < alphas.Length; i++) ak[i] = new GradientAlphaKey(alphas[i], i / (float)(alphas.Length - 1));
            g.SetKeys(ck, ak);
            return g;
        }

        /// <summary>Weicher runder Fleck als Partikeltextur, unbeleuchtet und transparent (URP).</summary>
        private Material ParticleMaterial()
        {
            if (particleMaterial != null) return particleMaterial;
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f - 0.5f, size / 2f - 0.5f)) / (size / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            tex.Apply();
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            particleMaterial = new Material(shader);
            particleMaterial.SetTexture("_BaseMap", tex);
            particleMaterial.SetFloat("_Surface", 1f); // transparent
            particleMaterial.SetFloat("_Blend", 0f);   // Alpha
            particleMaterial.SetOverrideTag("RenderType", "Transparent");
            particleMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            particleMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            particleMaterial.SetInt("_ZWrite", 0);
            particleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            particleMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return particleMaterial;
        }

        /// <summary>Trümmer: dunkle Brandfläche mit verkohlten Balken und etwas Rauch.</summary>
        private GameObject CreateRubble(Vector2Int footprint, Vector3 center)
        {
            if (charredMaterial == null)
            {
                charredMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                charredMaterial.SetColor(BaseColorId, new Color(0.13f, 0.11f, 0.1f));
                charredMaterial.SetFloat("_Smoothness", 0f);
            }
            float cell = city.Grid.CellSize;
            var root = new GameObject("Rubble");
            root.transform.position = center;
            var rng = new System.Random(footprint.x * 73 + footprint.y * 31 + Mathf.RoundToInt(center.x * 7 + center.z * 13));

            var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(plate.GetComponent<Collider>());
            plate.transform.SetParent(root.transform, false);
            plate.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            plate.transform.localScale = new Vector3(footprint.x * cell * 0.9f, 0.04f, footprint.y * cell * 0.9f);
            plate.GetComponent<Renderer>().sharedMaterial = charredMaterial;

            int beams = 4 * footprint.x * footprint.y;
            for (int i = 0; i < beams; i++)
            {
                var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(beam.GetComponent<Collider>());
                beam.transform.SetParent(root.transform, false);
                float x = ((float)rng.NextDouble() - 0.5f) * footprint.x * cell * 0.75f;
                float z = ((float)rng.NextDouble() - 0.5f) * footprint.y * cell * 0.75f;
                beam.transform.localPosition = new Vector3(x, 0.12f, z);
                beam.transform.localRotation = Quaternion.Euler((float)rng.NextDouble() * 30f - 15f, (float)rng.NextDouble() * 180f,
                    (float)rng.NextDouble() * 20f - 10f);
                beam.transform.localScale = new Vector3(0.12f, 0.12f, 0.6f + (float)rng.NextDouble() * 0.9f);
                beam.GetComponent<Renderer>().sharedMaterial = charredMaterial;
            }

            var smoke = CreateFlames(root.transform, footprint, center, 0.3f);
            foreach (var ps in smoke.GetComponentsInChildren<ParticleSystem>())
            {
                if (ps.name == "Flames") { ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); continue; }
                var emission = ps.emission;
                emission.rateOverTime = 4f * footprint.x * footprint.y;
            }
            return root;
        }
    }
}
