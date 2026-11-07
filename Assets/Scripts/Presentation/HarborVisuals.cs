using System.Collections.Generic;
using UnityEngine;

namespace EpochCity
{
    /// <summary>
    /// Zeigt Handelsschiffe an den Häfen (reine Optik, beeinflusst nichts): Nach jedem Handel (TradeSystem.Traded)
    /// fährt an jedem Hafen in Betrieb ein Schiff über das Wasser heran, liegt eine Weile am Kai, legt ab und fährt
    /// wieder hinaus. Der Liegeplatz liegt im Wasser an der Rückseite des Hafens (Kaiseite), sonst an einer Seite;
    /// reicht dort kein Wasser (mindestens 2 Felder entlang der Kante, 1 Feld tief), kommt kein Schiff. Die Anfahrt
    /// läuft nur über Wasserfelder; gibt es keinen freien Weg, wird das Schiff stattdessen mit Skalierung ein- und
    /// ausgeblendet. Die Zeit läuft mit der Spielgeschwindigkeit (Pause = Stillstand). Nichts wird gespeichert; nach
    /// dem Laden ist kein Schiff da. Das Modell kommt aus Blender (Blender/scripts/schiff.py), Bug nach +z.
    /// </summary>
    public class HarborVisuals : MonoBehaviour
    {
        public enum ShipPhase { Arriving, Moored, Departing }

        /// <summary>Ein Schiff am Hafen (öffentlich für Tests).</summary>
        public class HarborShip
        {
            public BuildingInstance Building;
            public GameObject Root;
            public Transform Model;
            public ShipPhase Phase;
            public float Timer;            // Fortschritt der Phase in Spielsekunden
            public float PhaseLength;
            public Vector3 Berth;          // Mitte des Schiffs am Kai (Wasserhöhe)
            public Vector3 Heading;        // Fahrtrichtung am Kai (Bug), waagerecht
            public Vector3 ArriveStart, ArriveControl, DepartControl, DepartEnd;
            public float ArriveScale = 1f, DepartScale = 1f;   // < 1: Ein-/Ausblenden statt Fahrt
            public float AnimTime;
            public float Phase0;
            public ParticleSystem Bow, Wake;
        }

        [SerializeField] private CityController city;
        [SerializeField] private TradeSystem trade;
        [SerializeField] private GameClock clock;
        [Tooltip("Prefab des Schiffs (Bug nach +z, Wasserlinie bei y = 0)")]
        [SerializeField] private GameObject shipPrefab;
        [Tooltip("Wasserhöhe über dem Boden (wie WaterVisuals); leer = 0,04")]
        [SerializeField] private WaterVisuals water;

        [Header("Zeiten (Spielsekunden)")]
        [Min(0.5f)] public float arriveSeconds = 6f;
        [Min(1f)] public float moorSeconds = 20f;
        [Min(0.5f)] public float departSeconds = 6f;

        [Header("Weg")]
        [Tooltip("Abstand der Bordwand von der Grundflächengrenze des Hafens (Meter)")]
        public float quayGap = 0.3f;
        [Tooltip("Halbe Breite des Rumpfs (Meter)")]
        public float halfBeam = 0.55f;
        [Tooltip("Halbe Länge des Schiffs (Meter)")]
        public float halfLength = 1.85f;
        [Tooltip("Länge der Anfahrt entlang der Kante (Meter)")]
        public float approachAlong = 5f;
        [Tooltip("Abstand der Anfahrt von der Kante (Meter)")]
        public float approachOut = 3.5f;
        [Tooltip("Mindestabstand von allem, was über Wasser liegt, zur Grundflächengrenze (Meter)")]
        public float bowClearance = 0.15f;
        [Tooltip("Größte Neigung (Rollen) beim Schaukeln in Grad; vergrößert den Abstand nach oben")]
        public float maxRoll = 1.8f;

        private readonly List<HarborShip> ships = new List<HarborShip>();
        private const float WaterFallback = 0.04f;

        public int ShipCount => ships.Count;

        private float WaterY => (city != null ? city.transform.position.y : 0f) + (water != null ? water.WaterHeight : WaterFallback);

        private void OnEnable()
        {
            if (trade != null) trade.Traded += OnTraded;
        }

        private void OnDisable()
        {
            if (trade != null) trade.Traded -= OnTraded;
            Clear();
        }

        private void Clear()
        {
            foreach (var s in ships) if (s.Root != null) Destroy(s.Root);
            ships.Clear();
        }

        // ---------------------------------------------------------------- Ereignisse

        private void OnTraded()
        {
            if (city == null || shipPrefab == null) return;
            foreach (var b in city.Buildings)
                if (IsActiveHarbor(b)) SendShip(b);
        }

        private static bool IsActiveHarbor(BuildingInstance b) =>
            b != null && b.Definition != null && b.Definition.tradeCapacity > 0 && !b.IsOutOfService;

        /// <summary>Das Schiff dieses Hafens (oder null).</summary>
        public HarborShip GetShip(BuildingInstance harbor)
        {
            foreach (var s in ships) if (s.Building == harbor) return s;
            return null;
        }

        /// <summary>
        /// Schickt ein Schiff zu diesem Hafen (auch ohne Handel, für Tests); liegt schon eins da, wird nur die Liegezeit
        /// verlängert. Gibt das Schiff zurück oder null, wenn der Hafen keinen Liegeplatz hat.
        /// </summary>
        public HarborShip SendShip(BuildingInstance harbor)
        {
            if (city == null || shipPrefab == null || !IsActiveHarbor(harbor)) return null;
            var existing = GetShip(harbor);
            if (existing != null)
            {
                if (existing.Phase == ShipPhase.Moored) existing.Timer = 0f;
                return existing;
            }
            if (!FindBerth(harbor, out var ship)) return null;
            ship.Building = harbor;
            ship.Root = Instantiate(shipPrefab, transform);
            ship.Root.name = "Schiff";
            ship.Model = ship.Root.transform;
            ship.Phase0 = Random.value * 6.28f;
            foreach (var r in ship.Root.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            CreateWake(ship);
            ship.Phase = ShipPhase.Arriving;
            ship.Timer = 0f;
            ship.PhaseLength = arriveSeconds;
            ships.Add(ship);
            Apply(ship);
            return ship;
        }

        // ---------------------------------------------------------------- Liegeplatz und Weg

        private float lateralExtent = -1f;

        /// <summary>Größte seitliche Ausdehnung des Schiffs (halbe Breite über Wasser inkl. Rahen und Ruder, plus Rollen) aus den Mesh-Grenzen des Prefabs.</summary>
        private float LateralExtent()
        {
            if (lateralExtent >= 0f) return lateralExtent;
            float ext = halfBeam, top = 2.5f;
            foreach (var mf in shipPrefab.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                var b = mf.sharedMesh.bounds;
                ext = Mathf.Max(ext, Mathf.Abs(b.min.x), Mathf.Abs(b.max.x));
                top = Mathf.Max(top, b.max.y);
            }
            lateralExtent = ext + top * Mathf.Sin(maxRoll * Mathf.Deg2Rad);
            return lateralExtent;
        }

        private Vector3 CellCenter(Vector2Int c) => city.Grid.CellCenterToWorld(c, city.transform.position);

        /// <summary>Kann ein Schiff über dieses Feld fahren (Wasser, keine Brücke; außerhalb der Karte gilt der Rand)?</summary>
        private bool Sailable(Vector3 world)
        {
            var grid = city.Grid;
            var c = grid.WorldToCell(world, city.transform.position);
            c.x = Mathf.Clamp(c.x, 0, grid.Width - 1);
            c.y = Mathf.Clamp(c.y, 0, grid.Height - 1);
            return city.IsWater(c) && !city.IsBridge(c);
        }

        private bool ShipFits(Vector3 p, Vector3 heading)
        {
            var side = new Vector3(heading.z, 0f, -heading.x);
            return Sailable(p) && Sailable(p + heading * halfLength) && Sailable(p - heading * halfLength) &&
                   Sailable(p + side * halfBeam) && Sailable(p - side * halfBeam);
        }

        private static Vector3 Bezier(Vector3 a, Vector3 c, Vector3 b, float u)
        {
            float v = 1f - u;
            return v * v * a + 2f * v * u * c + u * u * b;
        }

        private bool PathFree(Vector3 a, Vector3 c, Vector3 b)
        {
            const int steps = 14;
            for (int i = 0; i <= steps; i++)
            {
                float u = i / (float)steps;
                var p = Bezier(a, c, b, u);
                var tangent = 2f * ((1f - u) * (c - a) + u * (b - c));
                tangent.y = 0f;
                if (tangent.sqrMagnitude < 1e-4f) tangent = b - a;
                if (!ShipFits(p, tangent.normalized)) return false;
            }
            return true;
        }

        private struct Candidate
        {
            public Vector3 Berth, Heading, ArriveStart, ArriveControl, DepartControl, DepartEnd;
            public float ArriveScale, DepartScale, Score;
        }

        /// <summary>Sucht den Liegeplatz mit der besten Anfahrt; false, wenn keiner im Wasser liegt.</summary>
        private bool FindBerth(BuildingInstance b, out HarborShip ship)
        {
            ship = null;
            var grid = city.Grid;
            float cell = grid.CellSize;
            var fp = b.Footprint;
            var origin = b.Origin;
            var back = -CityController.FrontSide(b.Facing);
            float y = WaterY;

            var sides = new List<Vector2Int> { back, new Vector2Int(back.y, back.x), new Vector2Int(-back.y, -back.x) };
            bool found = false;
            Candidate best = default;
            for (int si = 0; si < sides.Count; si++)
            {
                var outward = sides[si];
                var along = new Vector2Int(outward.y != 0 ? 1 : 0, outward.x != 0 ? 1 : 0);
                var outV = new Vector3(outward.x, 0f, outward.y);
                var alongV = new Vector3(along.x, 0f, along.y);
                int count = along.x != 0 ? fp.x : fp.y;
                int depthCells = along.x != 0 ? fp.y : fp.x;
                // Mitte der Hafenkante auf dieser Seite (Weltkoordinaten) und Abstand der Schiffsmitte von der Grundflächengrenze
                var boundaryMid = city.GetFootprintCenter(fp, origin) + outV * (depthCells * cell * 0.5f);
                float standOff = bowClearance + LateralExtent();
                float span = count * cell * 0.5f;
                float sideBonus = si == 0 ? 10f : 0f;
                // Schiffsmitte in 0,25-m-Schritten entlang der Kante, auch zwischen Feldmitten
                for (float off = -span; off <= span + 0.001f; off += 0.25f)
                {
                    var berth = boundaryMid + alongV * off + outV * standOff;
                    berth.y = y;
                    if (!ShipFits(berth, alongV)) continue;
                    for (int h = 0; h < 2; h++)
                    {
                        var heading = h == 0 ? alongV : -alongV;
                        var cand = new Candidate { Berth = berth, Heading = heading };
                        float score = sideBonus - Mathf.Abs(off) * 0.02f;
                        float[] scales = { 1f, 0.65f, 0.35f };
                        foreach (float k in scales)
                        {
                            var s = berth - heading * approachAlong * k + outV * approachOut * k;
                            var c = berth - heading * approachAlong * k * 0.6f;
                            if (!PathFree(s, c, berth)) continue;
                            cand.ArriveStart = s; cand.ArriveControl = c; cand.ArriveScale = k; break;
                        }
                        foreach (float k in scales)
                        {
                            var e = berth + heading * approachAlong * k + outV * approachOut * k;
                            var c = berth + heading * approachAlong * k * 0.6f;
                            if (!PathFree(berth, c, e)) continue;
                            cand.DepartControl = c; cand.DepartEnd = e; cand.DepartScale = k; break;
                        }
                        score += cand.ArriveScale + cand.DepartScale;
                        cand.Score = score;
                        if (!found || cand.Score > best.Score) { best = cand; found = true; }
                    }
                }
                if (found && si == 0) break;   // Rückseite hat einen Platz: dort bleiben
            }
            if (!found) return false;

            ship = new HarborShip { Berth = best.Berth, Heading = best.Heading, ArriveScale = best.ArriveScale, DepartScale = best.DepartScale };
            if (best.ArriveScale > 0f) { ship.ArriveStart = best.ArriveStart; ship.ArriveControl = best.ArriveControl; }
            else { ship.ArriveStart = ship.ArriveControl = best.Berth; }
            if (best.DepartScale > 0f) { ship.DepartControl = best.DepartControl; ship.DepartEnd = best.DepartEnd; }
            else { ship.DepartControl = ship.DepartEnd = best.Berth; }
            return true;
        }

        /// <summary>Erstes Feld der Kante außerhalb der Grundfläche auf der Seite outward.</summary>
        private static Vector2Int EdgeStart(Vector2Int origin, Vector2Int fp, Vector2Int outward)
        {
            if (outward.y > 0) return new Vector2Int(origin.x, origin.y + fp.y);
            if (outward.y < 0) return new Vector2Int(origin.x, origin.y - 1);
            if (outward.x > 0) return new Vector2Int(origin.x + fp.x, origin.y);
            return new Vector2Int(origin.x - 1, origin.y);
        }

        // ---------------------------------------------------------------- Ablauf

        private void Update()
        {
            if (ships.Count == 0) return;
            float dt = Time.deltaTime * (clock != null ? clock.Speed : 1);
            for (int i = ships.Count - 1; i >= 0; i--)
            {
                var s = ships[i];
                var b = s.Building;
                bool valid = b != null && city.Grid.GetAt(b.Origin) == b && IsActiveHarbor(b);
                if (!valid)
                {
                    if (s.Root != null) Destroy(s.Root);
                    ships.RemoveAt(i);
                    continue;
                }
                s.AnimTime += dt;
                s.Timer += dt;
                if (s.Timer >= s.PhaseLength)
                {
                    s.Timer = 0f;
                    if (s.Phase == ShipPhase.Arriving) { s.Phase = ShipPhase.Moored; s.PhaseLength = moorSeconds; }
                    else if (s.Phase == ShipPhase.Moored) { s.Phase = ShipPhase.Departing; s.PhaseLength = departSeconds; }
                    else
                    {
                        if (s.Root != null) Destroy(s.Root);
                        ships.RemoveAt(i);
                        continue;
                    }
                }
                Apply(s);
            }
        }

        private static float EaseOut(float u) => 1f - (1f - u) * (1f - u);
        private static float EaseIn(float u) => u * u;

        /// <summary>Setzt Lage, Drehung und Wellen des Schiffs aus Phase und Zeit.</summary>
        private void Apply(HarborShip s)
        {
            if (s.Root == null) return;
            float u = s.PhaseLength > 0f ? Mathf.Clamp01(s.Timer / s.PhaseLength) : 1f;
            Vector3 pos;
            Vector3 dir = s.Heading;
            float scale = 1f;
            bool sailing = false;
            switch (s.Phase)
            {
                case ShipPhase.Arriving:
                    if (s.ArriveScale > 0f)
                    {
                        float e = EaseOut(u);
                        pos = Bezier(s.ArriveStart, s.ArriveControl, s.Berth, e);
                        var t = 2f * ((1f - e) * (s.ArriveControl - s.ArriveStart) + e * (s.Berth - s.ArriveControl));
                        t.y = 0f;
                        if (t.sqrMagnitude > 1e-4f) dir = t.normalized;
                        sailing = u < 0.97f;
                    }
                    else { pos = s.Berth; scale = EaseOut(u); }
                    break;
                case ShipPhase.Departing:
                    if (s.DepartScale > 0f)
                    {
                        float e = EaseIn(u);
                        pos = Bezier(s.Berth, s.DepartControl, s.DepartEnd, e);
                        var t = 2f * ((1f - e) * (s.DepartControl - s.Berth) + e * (s.DepartEnd - s.DepartControl));
                        t.y = 0f;
                        if (t.sqrMagnitude > 1e-4f) dir = t.normalized;
                        sailing = u > 0.03f;
                    }
                    else { pos = s.Berth; scale = 1f - EaseIn(u); }
                    break;
                default:
                    pos = s.Berth;
                    break;
            }

            float amp = sailing ? 1f : 0.55f;
            float t0 = s.AnimTime + s.Phase0;
            float roll = Mathf.Sin(t0 * 1.3f) * 1.8f * amp;
            float pitch = Mathf.Sin(t0 * 0.9f + 1.1f) * 1.4f * amp;
            pos.y += Mathf.Sin(t0 * 1.7f) * 0.012f * amp;
            s.Root.transform.position = pos;
            s.Root.transform.rotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(-pitch, 0f, roll);
            s.Root.transform.localScale = Vector3.one * Mathf.Max(scale, 0.001f);
            s.Root.SetActive(scale > 0.01f);
            SetWake(s, sailing);
        }

        // ---------------------------------------------------------------- Kielwasser

        private void CreateWake(HarborShip s)
        {
            s.Bow = CreateSystem(s.Root.transform, "Bugwelle", new Vector3(0f, 0.02f, 1.75f), 0.5f, 1.1f, 0.25f, 0.5f, 0.55f, 12f);
            s.Wake = CreateSystem(s.Root.transform, "Kielwasser", new Vector3(0f, 0.02f, -1.6f), 0.9f, 2.4f, 0.3f, 0.65f, 0.4f, 16f);
        }

        private static ParticleSystem CreateSystem(Transform parent, string name, Vector3 localPos, float width,
            float life, float sizeMin, float sizeMax, float alpha, float rate)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = life;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor = new Color(1f, 1f, 1f, alpha);
            main.maxParticles = 120;
            main.loop = true;
            main.playOnAwake = false;
            var emission = ps.emission;
            emission.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(width, 0f, 0.15f);
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.8f));
            var color = ps.colorOverLifetime;
            color.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            color.color = grad;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            renderer.sharedMaterial = FireVisuals.SoftParticleMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        private void SetWake(HarborShip s, bool sailing)
        {
            float speed = clock != null ? clock.Speed : 1;
            foreach (var ps in new[] { s.Bow, s.Wake })
            {
                if (ps == null) continue;
                var main = ps.main;
                main.simulationSpeed = Mathf.Max(speed, 0.0001f);
                var emission = ps.emission;
                emission.enabled = sailing && speed > 0f;
                if (speed <= 0f) { if (!ps.isPaused) ps.Pause(true); }
                else
                {
                    if (ps.isPaused || !ps.isPlaying) ps.Play(true);
                }
            }
        }
    }
}
