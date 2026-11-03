using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Zeigt Baustellen (BuildingInstance.IsBuilding) nur an, für alle Gebäude gleich (wie die Baustellen in Cities:
    /// Skylines): zuerst das Fundament mit Pflöcken, Schnur und Baustoffen, dann wächst das Modell von unten nach oben in
    /// einem Holzgerüst, das mit ihm in die Höhe geht; ist der Bau fertig, verschwinden Gerüst und Baustoffe mit einer
    /// Staubwolke. Die Teile kommen aus dem Blender-Bausatz (Blender/scripts/baustelle.py, Assets/Content/Models/
    /// Baustelle.fbx) und werden je Grundfläche zusammengesetzt. Großbauten mit Bauphasen zeigen ihre eigenen Stufen.
    /// Nach dem Laden findet die Darstellung Baustellen von selbst wieder.
    /// </summary>
    public class ConstructionVisuals : MonoBehaviour
    {
        [SerializeField] private CityController city;
        [Tooltip("Bausatz aus Blender (Baustelle.fbx): Pfosten, Riegel, Brett, Leiter, Pflock, Schnur, Baustoffe")]
        [SerializeField] private GameObject kit;
        [Tooltip("Anteil der Bauzeit, in dem nur das Fundament zu sehen ist")]
        [Range(0f, 0.8f)] public float foundationShare = 0.2f;
        [Tooltip("Höhe einer Gerüstlage in Metern")]
        [Min(0.2f)] public float levelHeight = 0.5f;
        [Tooltip("Größter Abstand der Gerüstpfosten entlang einer Seite in Metern")]
        [Min(0.3f)] public float postSpacing = 1.0f;
        [Tooltip("Abstand des Gerüsts vom Hauskörper in Metern")]
        [Min(0f)] public float clearance = 0.10f;

        private class Site
        {
            public BuildingInstance Building;
            public GameObject Root, Scaffold, Materials;
            public Vector3 ViewScale;
            public List<MeshRenderer> Renderers = new List<MeshRenderer>(); // Modell ohne Bodenplatte
            public bool Hidden;
            public Rect Core;        // Hauskörper (xz) in Weltkoordinaten
            public float Height;     // volle Höhe des Modells
            public int Levels = -1;
        }

        private struct Part
        {
            public Mesh Mesh;
            public Material[] Materials;
        }

        private readonly Dictionary<BuildingInstance, Site> sites = new Dictionary<BuildingInstance, Site>();
        private readonly List<BuildingInstance> scratch = new List<BuildingInstance>();
        private Dictionary<string, Part> parts;

        /// <summary>Anzahl der gezeigten Baustellen (für Tests).</summary>
        public int SiteCount => sites.Count;

        private void Update()
        {
            if (city == null || kit == null) return;
            if (parts == null) LoadParts();

            foreach (var b in city.Buildings)
                if (b.IsBuilding && b.View != null && !sites.ContainsKey(b)) sites[b] = Create(b);

            scratch.Clear();
            scratch.AddRange(sites.Keys);
            foreach (var b in scratch)
            {
                var site = sites[b];
                if (b.View == null)                      // abgerissen oder neu geladen
                {
                    Remove(site, dust: false);
                    continue;
                }
                if (!b.IsBuilding)
                {
                    Remove(site, dust: true);
                    continue;
                }
                Refresh(site);
            }
        }

        // ---------------------------------------------------------------- Aufbau

        private void LoadParts()
        {
            parts = new Dictionary<string, Part>();
            foreach (Transform child in kit.transform)
            {
                var filter = child.GetComponent<MeshFilter>();
                var renderer = child.GetComponent<MeshRenderer>();
                if (filter == null || renderer == null) continue;
                parts[child.name] = new Part { Mesh = filter.sharedMesh, Materials = renderer.sharedMaterials };
            }
        }

        private Site Create(BuildingInstance b)
        {
            var view = b.View.transform;
            var site = new Site { Building = b, ViewScale = view.localScale };
            MeasureModel(b, site);
            foreach (var r in b.View.GetComponentsInChildren<MeshRenderer>())
                if (r.enabled && r.gameObject.name != "Boden") site.Renderers.Add(r);

            site.Root = new GameObject($"Baustelle {b.Definition.displayName}");
            site.Root.transform.SetParent(transform, false);
            site.Materials = new GameObject("Fundament");
            site.Materials.transform.SetParent(site.Root.transform, false);
            BuildFoundation(b, site);
            Refresh(site);
            return site;
        }

        /// <summary>
        /// Hauskörper und Höhe des Modells: die Eckpunkte über der halben Höhe bestimmen die Fläche, um die das Gerüst
        /// steht (Gärten und Höfe bleiben außen vor); ohne lesbares Mesh die ganze Grundfläche des Modells.
        /// </summary>
        private void MeasureModel(BuildingInstance b, Site site)
        {
            var view = b.View.transform;
            float ground = view.position.y;
            Bounds all = new Bounds(view.position, Vector3.zero);
            var renderers = new List<MeshRenderer>();
            foreach (var r in b.View.GetComponentsInChildren<MeshRenderer>())
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                renderers.Add(r);
                all.Encapsulate(r.bounds);
            }
            site.Height = Mathf.Max(0.3f, all.max.y - ground);

            float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
            float threshold = ground + site.Height * 0.45f;
            foreach (var r in renderers)
            {
                var mesh = r.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh == null || !mesh.isReadable) continue;
                var matrix = r.transform.localToWorldMatrix;
                foreach (var v in mesh.vertices)
                {
                    var p = matrix.MultiplyPoint3x4(v);
                    if (p.y < threshold) continue;
                    minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                    minZ = Mathf.Min(minZ, p.z); maxZ = Mathf.Max(maxZ, p.z);
                }
            }
            if (minX > maxX)
            {
                minX = all.min.x; maxX = all.max.x; minZ = all.min.z; maxZ = all.max.z;
            }
            // Gerüst mit Abstand um den Hauskörper, aber innerhalb der Grundfläche
            float half = city.Grid.CellSize * 0.5f;
            var fp = b.Footprint;
            var c = view.position;
            float limX = fp.x * half - 0.04f, limZ = fp.y * half - 0.04f;
            minX = Mathf.Max(c.x - limX, minX - clearance);
            maxX = Mathf.Min(c.x + limX, maxX + clearance);
            minZ = Mathf.Max(c.z - limZ, minZ - clearance);
            maxZ = Mathf.Min(c.z + limZ, maxZ + clearance);
            site.Core = Rect.MinMaxRect(minX, minZ, maxX, maxZ);
        }

        /// <summary>Fundament: Pflöcke an den Ecken des Hauskörpers, Schnur dazwischen, Baustoffe an der Vorderseite.</summary>
        private void BuildFoundation(BuildingInstance b, Site site)
        {
            var parent = site.Materials.transform;
            var core = site.Core;
            float y = b.View.transform.position.y;
            var corners = new[]
            {
                new Vector3(core.xMin, y, core.yMin), new Vector3(core.xMax, y, core.yMin),
                new Vector3(core.xMax, y, core.yMax), new Vector3(core.xMin, y, core.yMax)
            };
            for (int i = 0; i < 4; i++)
            {
                Spawn("Pflock", parent, corners[i], Quaternion.identity, Vector3.one);
                var a = corners[i];
                var e = corners[(i + 1) % 4];
                var mid = (a + e) * 0.5f + Vector3.up * 0.17f;
                Spawn("Schnur", parent, mid, Quaternion.FromToRotation(Vector3.right, e - a), new Vector3((e - a).magnitude, 1f, 1f));
            }

            // Baustoffe an der Vorderseite (zur Straße), innerhalb der Grundfläche
            var view = b.View.transform;
            float half = city.Grid.CellSize * 0.5f;
            var fp = b.Footprint;
            var rot = view.rotation;
            Vector3 right = rot * Vector3.right, back = rot * Vector3.back;
            float width = (Mathf.Abs(right.x) > 0.5f ? fp.x : fp.y) * half;
            float depth = (Mathf.Abs(back.z) > 0.5f ? fp.y : fp.x) * half;
            var front = view.position + back * (depth - 0.30f);
            string[] piles = { "Bretterstapel", "Steinhaufen", "Sandhaufen", "Moertelkasten" };
            int count = Mathf.Clamp(Mathf.RoundToInt(width / 0.55f), 1, piles.Length);
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0f : (i / (float)(count - 1) - 0.5f) * 2f;
                var pos = front + right * (t * (width - 0.35f));
                Spawn(piles[i], parent, pos, rot * Quaternion.Euler(0f, (i * 37) % 20 - 10f, 0f), Vector3.one);
            }
        }

        /// <summary>Modell wachsen lassen und das Gerüst neu setzen, sobald eine Lage dazukommt.</summary>
        private void Refresh(Site site)
        {
            var b = site.Building;
            float progress = b.Definition.buildSeconds > 0f ? Mathf.Clamp01(b.BuildTimer / b.Definition.buildSeconds) : 1f;
            float rise = Mathf.Clamp01((progress - foundationShare) / Mathf.Max(0.01f, 1f - foundationShare));
            float scale = Mathf.Lerp(0.08f, 1f, rise);
            var view = b.View.transform;
            view.localScale = new Vector3(site.ViewScale.x, site.ViewScale.y * scale, site.ViewScale.z);
            // Im Fundament ist vom Modell nichts zu sehen (die Bodenplatte bleibt)
            bool hide = rise <= 0f;
            if (hide != site.Hidden)
            {
                site.Hidden = hide;
                foreach (var r in site.Renderers) if (r != null) r.enabled = !hide;
            }

            // Baustoffe werden verbaut: ab drei Vierteln der Bauzeit sind sie weg
            site.Materials.SetActive(progress < 0.75f);

            int levels = rise <= 0f ? 0 : Mathf.Max(1, Mathf.CeilToInt((site.Height * scale + 0.1f) / levelHeight));
            levels = Mathf.Min(levels, Mathf.CeilToInt(site.Height / levelHeight));
            if (levels == site.Levels) return;
            site.Levels = levels;
            if (site.Scaffold != null) Destroy(site.Scaffold);
            if (levels > 0) site.Scaffold = BuildScaffold(site, levels, view.position.y);
        }

        /// <summary>Gerüst um den Hauskörper: Pfosten an den Ecken und entlang der Seiten, je Lage Riegel und Bohlen, eine Leiter.</summary>
        private GameObject BuildScaffold(Site site, int levels, float ground)
        {
            var root = new GameObject("Geruest");
            root.transform.SetParent(site.Root.transform, false);
            var parent = root.transform;
            var r = site.Core;
            float top = levels * levelHeight;
            var corners = new[]
            {
                new Vector3(r.xMin, ground, r.yMin), new Vector3(r.xMax, ground, r.yMin),
                new Vector3(r.xMax, ground, r.yMax), new Vector3(r.xMin, ground, r.yMax)
            };
            for (int side = 0; side < 4; side++)
            {
                var a = corners[side];
                var e = corners[(side + 1) % 4];
                var dir = e - a;
                float length = dir.magnitude;
                if (length < 0.05f) continue;
                var along = Quaternion.FromToRotation(Vector3.right, dir);
                var outward = Quaternion.Euler(0f, 90f, 0f) * (dir / length) * 0.09f; // Bohlen liegen außen am Gerüst
                int posts = Mathf.Max(1, Mathf.CeilToInt(length / postSpacing));
                for (int k = 0; k < posts; k++)                                    // Ecke a und Zwischenpfosten
                    Spawn("Pfosten", parent, a + dir * (k / (float)posts), Quaternion.identity, new Vector3(1f, top + 0.15f, 1f));
                for (int level = 1; level <= levels; level++)
                {
                    float y = level * levelHeight;
                    Spawn("Riegel", parent, (a + e) * 0.5f + Vector3.up * y, along, new Vector3(length, 1f, 1f));
                    Spawn("Brett", parent, (a + e) * 0.5f + Vector3.up * (y - 0.02f) + outward, along, new Vector3(length, 1f, 1f));
                    if (level == levels)                                            // Geländer der obersten Lage
                        Spawn("Riegel", parent, (a + e) * 0.5f + Vector3.up * (y + 0.12f) + outward * 2f, along, new Vector3(length, 1f, 1f));
                }
            }
            // Leiter an der Vorderseite, schräg an die erste Lage gelehnt
            var front = corners[0] + (corners[1] - corners[0]) * 0.2f + Vector3.back * 0.15f;
            Spawn("Leiter", parent, front, Quaternion.Euler(-12f, 0f, 0f), new Vector3(1f, Mathf.Min(top, levelHeight * 2f) + 0.1f, 1f));
            return root;
        }

        private void Spawn(string name, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (!parts.TryGetValue(name, out var part)) return;
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = scale;
            go.GetComponent<MeshFilter>().sharedMesh = part.Mesh;
            go.GetComponent<MeshRenderer>().sharedMaterials = part.Materials;
        }

        // ---------------------------------------------------------------- Abbau

        private void Remove(Site site, bool dust)
        {
            sites.Remove(site.Building);
            var view = site.Building.View;
            if (view != null) view.transform.localScale = site.ViewScale;
            foreach (var r in site.Renderers) if (r != null) r.enabled = true;
            if (dust && view != null)
                Dust(new Vector3(site.Core.center.x, view.transform.position.y, site.Core.center.y),
                    new Vector3(site.Core.width, 0.2f, site.Core.height));
            if (site.Root != null)
            {
                site.Root.SetActive(false); // sofort weg, zerstört wird am Ende des Frames
                Destroy(site.Root);
            }
        }

        /// <summary>Kurze Staubwolke, wenn Gerüst und Baustoffe verschwinden.</summary>
        private void Dust(Vector3 position, Vector3 area)
        {
            var go = new GameObject("Baustaub");
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(position + Vector3.up * 0.2f, Quaternion.Euler(-90f, 0f, 0f));
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startColor = new Color(0.78f, 0.72f, 0.60f, 0.7f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(area.x * area.z * 4f, 8f, 60f)) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(area.x, area.z, area.y);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.8f));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = FireVisuals.SoftParticleMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            Destroy(go, 3f);
        }
    }
}
