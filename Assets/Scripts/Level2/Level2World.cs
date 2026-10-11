using System.Collections.Generic;
using PerspectivePuzzle.Level1;
using PerspectivePuzzle.Movement;
using PerspectivePuzzle.Visual;
using UnityEngine;
using UnityEngine.Rendering;

namespace PerspectivePuzzle.Level2
{
    public class Lantern
    {
        public Transform pivot;
        public Transform shade;
        public Renderer shadeRenderer;
        public Light light;
        public bool lit;
        public float phase;

        public void Ignite(L2Kit kit)
        {
            lit = true;
            shadeRenderer.sharedMaterial = kit.Glow(new Color(1f, 0.62f, 0.3f));
            light.enabled = true;
        }
    }

    public partial class Level2Game
    {
        // ---------- palette ----------
        static readonly Color Sand = new(0.87f, 0.76f, 0.6f);
        static readonly Color SandDark = new(0.6f, 0.46f, 0.38f);
        static readonly Color Rock = new(0.42f, 0.32f, 0.33f);
        static readonly Color PathTile = new(0.98f, 0.93f, 0.8f);
        static readonly Color Ochre = new(0.94f, 0.7f, 0.34f);
        static readonly Color Mustard = new(0.97f, 0.8f, 0.42f);
        static readonly Color Cream = new(0.98f, 0.94f, 0.85f);
        static readonly Color Shutter = new(0.22f, 0.46f, 0.36f);
        static readonly Color Terracotta = new(0.74f, 0.33f, 0.22f);
        static readonly Color Lacquer = new(0.78f, 0.13f, 0.12f);
        static readonly Color Gold = new(0.96f, 0.74f, 0.32f);
        static readonly Color Stone = new(0.8f, 0.78f, 0.72f);
        static readonly Color StoneDark = new(0.6f, 0.6f, 0.56f);
        static readonly Color Moss = new(0.45f, 0.56f, 0.38f);
        static readonly Color Leaf = new(0.38f, 0.58f, 0.34f);
        static readonly Color LeafDark = new(0.27f, 0.46f, 0.31f);
        static readonly Color Flame = new(0.95f, 0.38f, 0.22f);
        static readonly Color Wood = new(0.36f, 0.24f, 0.19f);
        static readonly Color Water = new(0.26f, 0.55f, 0.6f);
        static readonly Color Glass = new(0.2f, 0.2f, 0.27f);

        Transform skyRoot;
        Mesh skyMesh;
        Transform sunDisc;
        Transform sunHalo;
        readonly List<Transform> clouds = new();
        readonly List<Transform> stars = new();
        readonly List<Transform> kites = new();
        readonly List<SkyLantern> skyLanterns = new();

        static readonly Color[] DuskSky =
        {
            new(1f, 0.72f, 0.46f), new(0.99f, 0.56f, 0.5f), new(0.66f, 0.42f, 0.62f), new(0.27f, 0.24f, 0.5f)
        };

        static readonly Color[] NightSky =
        {
            new(0.55f, 0.36f, 0.5f), new(0.33f, 0.27f, 0.5f), new(0.17f, 0.18f, 0.4f), new(0.07f, 0.09f, 0.24f)
        };

        float cloudTint = -1f;

        class SkyLantern
        {
            public Transform t;
            public float speed;
            public float sway;
            public float phase;
        }

        // =====================================================================
        // Camera, sky and mood
        // =====================================================================

        void SetupCamera()
        {
            if (view == null)
                view = Camera.main;
            if (view == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                view = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }

            view.orthographic = true;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = DuskSky[1];
            view.nearClipPlane = 0.3f;
            view.farClipPlane = 260f;
            focus = focusTarget = Vector3.up * 1.6f;
            view.orthographicSize = baseSize * 1.25f;

            if (sun == null)
            {
                var sunGo = new GameObject("Sun");
                sun = sunGo.AddComponent<Light>();
                sun.type = LightType.Directional;
            }

            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.62f;
            sun.shadowBias = 0.03f;
            sun.shadowNormalBias = 0.25f;
            sun.transform.rotation = Quaternion.LookRotation(new Vector3(0.35f, -0.62f, 0.7f));
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = false;
            QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, 120f);
        }

        void BuildSky()
        {
            skyRoot = new GameObject("Sky").transform;
            skyRoot.SetParent(view.transform, false);
            skyRoot.localPosition = new Vector3(0f, 0f, 240f);
            GameObject grad = kit.Gradient(skyRoot, "Gradient", 1f, 1f, DuskSky);
            skyMesh = grad.GetComponent<MeshFilter>().sharedMesh;

            sunHalo = kit.Disc(skyRoot, "SunHalo", 1f, new Color(1f, 0.78f, 0.55f, 0.45f), new Color(1f, 0.6f, 0.45f, 0f)).transform;
            sunHalo.localPosition = new Vector3(0f, 0f, -1f);
            sunDisc = kit.Disc(skyRoot, "SunDisc", 1f, new Color(1f, 0.93f, 0.75f, 1f), new Color(1f, 0.8f, 0.55f, 0.85f)).transform;
            sunDisc.localPosition = new Vector3(0f, 0f, -2f);

            var rng = new System.Random(7);
            for (int i = 0; i < 70; i++)
            {
                Transform s = kit.Disc(skyRoot, "Star", 1f, new Color(1f, 0.97f, 0.9f, 1f), new Color(1f, 1f, 1f, 0f), 8).transform;
                s.localPosition = new Vector3((float)rng.NextDouble() - 0.5f, 0.05f + (float)rng.NextDouble() * 0.45f, -3f);
                s.localScale = Vector3.zero;
                stars.Add(s);
            }

            for (int i = 0; i < 7; i++)
            {
                var c = new GameObject("Cloud").transform;
                c.SetParent(skyRoot, false);
                c.localPosition = new Vector3(-0.6f + i * 0.2f + (float)rng.NextDouble() * 0.1f, -0.25f + (float)rng.NextDouble() * 0.6f, -6f - i);
                int puffs = 3 + rng.Next(3);
                for (int p = 0; p < puffs; p++)
                {
                    Transform puff = kit.Disc(c, "Puff", 1f, new Color(1f, 0.9f, 0.88f, 0.75f), new Color(1f, 0.85f, 0.85f, 0f)).transform;
                    puff.localPosition = new Vector3((p - puffs * 0.5f) * 0.035f, (float)rng.NextDouble() * 0.012f, 0f);
                    float s = 0.05f + (float)rng.NextDouble() * 0.04f;
                    puff.localScale = new Vector3(s * 1.7f, s, 1f);
                }
                clouds.Add(c);
            }

            Color[] kiteColors = { new(0.92f, 0.2f, 0.18f), new(1f, 0.82f, 0.25f) };
            for (int i = 0; i < 2; i++)
            {
                var k = new GameObject("Kite").transform;
                k.SetParent(skyRoot, false);
                k.localPosition = new Vector3(i == 0 ? 0.36f : 0.29f, i == 0 ? 0.1f : 0.0f, -12f - i);
                Transform body = kit.Disc(k, "Body", 0.02f, kiteColors[i], kiteColors[i], 4).transform;
                body.localScale = new Vector3(0.75f, 1.2f, 1f);
                Transform tail = kit.Disc(k, "Tail", 0.006f, kiteColors[1 - i], kiteColors[1 - i], 4).transform;
                tail.localPosition = new Vector3(0.004f, -0.03f, 0f);
                kites.Add(k);
            }
        }

        void LayoutSky(float aspect)
        {
            float h = view.orthographicSize * 2f * 1.15f;
            float w = h * aspect * 1.1f;
            skyRoot.localScale = new Vector3(w, h, 1f);
            float t = Time.time;
            for (int i = 0; i < clouds.Count; i++)
            {
                Vector3 p = clouds[i].localPosition;
                p.x += Time.deltaTime * (0.004f + i * 0.0012f);
                if (p.x > 0.75f)
                    p.x = -0.75f;
                clouds[i].localPosition = p;
                clouds[i].localScale = new Vector3(1f, aspect, 1f);
            }

            for (int i = 0; i < kites.Count; i++)
            {
                kites[i].localScale = new Vector3(1f, aspect, 1f);
                kites[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.3f + i) * 12f);
            }

            float sunY = Mathf.Lerp(-0.12f, -0.48f, night);
            sunDisc.localPosition = new Vector3(-0.28f, sunY, -2f);
            sunHalo.localPosition = new Vector3(-0.28f, sunY, -1f);
            float discR = 0.06f;
            sunDisc.localScale = new Vector3(discR, discR * aspect, 1f);
            sunHalo.localScale = new Vector3(discR * 4.5f, discR * 4.5f * aspect, 1f);

            float starA = Mathf.InverseLerp(0.35f, 1f, night);
            for (int i = 0; i < stars.Count; i++)
            {
                float tw = 0.75f + 0.25f * Mathf.Sin(t * 2f + i * 1.7f);
                float r = 0.0022f * starA * tw * (i % 5 == 0 ? 1.7f : 1f);
                stars[i].localScale = new Vector3(r, r * aspect, 1f);
            }
        }

        void ApplyMood(float n)
        {
            if (skyMesh != null)
            {
                var cols = new Color[DuskSky.Length * 2];
                for (int i = 0; i < DuskSky.Length; i++)
                {
                    Color c = Color.Lerp(DuskSky[i], NightSky[i], n);
                    cols[i * 2] = c;
                    cols[i * 2 + 1] = c;
                }
                skyMesh.colors = cols;
            }

            view.backgroundColor = Color.Lerp(DuskSky[1], NightSky[1], n);
            sun.color = Color.Lerp(new Color(1f, 0.76f, 0.55f), new Color(0.72f, 0.74f, 1f), n);
            sun.intensity = Mathf.Lerp(1.12f, 0.62f, n);
            RenderSettings.ambientSkyColor = Color.Lerp(new Color(0.66f, 0.56f, 0.7f), new Color(0.38f, 0.38f, 0.6f), n);
            RenderSettings.ambientEquatorColor = Color.Lerp(new Color(0.8f, 0.58f, 0.5f), new Color(0.46f, 0.38f, 0.5f), n);
            RenderSettings.ambientGroundColor = Color.Lerp(new Color(0.34f, 0.27f, 0.33f), new Color(0.18f, 0.16f, 0.26f), n);

            if (Mathf.Abs(cloudTint - n) > 0.02f)
            {
                cloudTint = n;
                Color centre = Color.Lerp(new Color(1f, 0.9f, 0.88f, 0.75f), new Color(0.42f, 0.38f, 0.6f, 0.55f), n);
                Color edge = new Color(centre.r, centre.g, centre.b, 0f);
                foreach (Transform c in clouds)
                    foreach (MeshFilter mf in c.GetComponentsInChildren<MeshFilter>())
                    {
                        Mesh mesh = mf.mesh;
                        var cols = new Color[mesh.vertexCount];
                        cols[0] = centre;
                        for (int i = 1; i < cols.Length; i++)
                            cols[i] = edge;
                        mesh.colors = cols;
                    }
            }
        }

        // =====================================================================
        // World
        // =====================================================================

        void BuildWorld()
        {
            root = new GameObject("Diorama").transform;
            layout = new GameObject("Layout").transform;
            layout.SetParent(root, false);
            layout.localPosition = -Center;

            windowDark = kit.Lit(Glass);
            windowWarm = kit.Glow(new Color(1f, 0.78f, 0.45f));
            fairyOff = kit.Lit(new Color(0.55f, 0.5f, 0.42f));
            fairyOn = kit.Glow(new Color(1f, 0.85f, 0.55f));
            liftMat = kit.Lit(Lacquer, 0.35f);
            bridgeMat = kit.Lit(Lacquer, 0.35f);

            BuildBase();
            BuildGround();
            BuildLift();
            BuildHouseA();
            BuildBridge();
            BuildVilla();
            BuildTower();
            BuildProps();
            LinkStatic();
            BuildMarker();
            AtmosphericDust.Create(root, new Color(1f, 0.86f, 0.55f, 0.8f));
        }

        PathNode MakeNode(string id, Transform parent, Vector3 local, PickGroup group, bool cap = true, float thick = 0.3f)
        {
            var go = new GameObject(id);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var node = go.AddComponent<PathNode>();
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(0.98f, thick, 0.98f);
            box.center = new Vector3(0f, -thick * 0.5f, 0f);
            var pick = go.AddComponent<L2Pick>();
            pick.group = group;
            pick.node = node;
            if (cap)
            {
                kit.Box(go.transform, "Cap", new Vector3(0f, -0.05f, 0f), new Vector3(0.9f, 0.1f, 0.9f), PathTile);
                kit.Box(go.transform, "Inlay", new Vector3(0f, -0.004f, 0f), new Vector3(0.36f, 0.012f, 0.36f), new Color(0.92f, 0.84f, 0.68f));
            }
            nodes[id] = node;
            return node;
        }

        public PathNode NodeById(string id)
        {
            return nodes.TryGetValue(id, out PathNode n) ? n : null;
        }

        void BuildBase()
        {
            Transform p = layout;
            kit.Block(p, "SlabTop", new Vector3(-5.2f, -1.0f, -2.2f), new Vector3(6.4f, -0.6f, 8.2f), Sand);
            kit.Block(p, "SlabBand", new Vector3(-5.25f, -1.7f, -2.25f), new Vector3(6.45f, -1.0f, 8.25f), SandDark);
            GameObject rock = kit.Pyramid(p, "Rock", new Vector3(0.6f, -1.7f, 3.0f), 1f, 1f, Rock);
            rock.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
            rock.transform.localScale = new Vector3(11.6f, 3.2f, 10.4f);
            GameObject rock2 = kit.Pyramid(p, "Rock2", new Vector3(-2.5f, -1.7f, 1.5f), 1f, 1f, new Color(0.5f, 0.38f, 0.38f));
            rock2.transform.localRotation = Quaternion.Euler(180f, 20f, 0f);
            rock2.transform.localScale = new Vector3(4.5f, 2.2f, 4.5f);

            // the lake (Hoan Kiem nod) between the two houses
            kit.Box(p, "Lake", new Vector3(0.6f, -0.57f, 4.0f), new Vector3(11.2f, 0.06f, 2.9f), kit.Lit(Water, 0.75f));
            kit.Block(p, "LakeRimS", new Vector3(-5.2f, -0.6f, 2.45f), new Vector3(3.5f, -0.48f, 2.55f), Cream);
            kit.Block(p, "LakeRimN", new Vector3(-5.2f, -0.6f, 5.45f), new Vector3(-0.5f, -0.48f, 5.55f), Cream);
            for (int i = 0; i < 6; i++)
            {
                float x = -1.4f + i * 1.15f;
                float z = 3.1f + (i % 2) * 1.6f;
                GameObject pad = kit.Cyl(layout, "LilyPad", new Vector3(x, -0.54f, z), 0.16f, 0.02f, Moss);
                L2Kit.NoShadow(pad);
            }
        }

        void BuildGround()
        {
            Transform p = layout;
            kit.Block(p, "Plinth", new Vector3(0.5f, -0.6f, -1.5f), new Vector3(3.5f, -0.06f, -0.5f), Stone);
            kit.Block(p, "PlinthLip", new Vector3(0.45f, -0.2f, -1.55f), new Vector3(3.5f, -0.1f, -0.45f), Cream);
            s0 = MakeNode("S0", p, new Vector3(1f, 0f, -1f), PickGroup.World);
            s1 = MakeNode("S1", p, new Vector3(2f, 0f, -1f), PickGroup.World);
            s2 = MakeNode("S2", p, new Vector3(3f, 0f, -1f), PickGroup.World);
            // steps down to the street, just decoration
            kit.Block(p, "Step", new Vector3(0.05f, -0.6f, -1.4f), new Vector3(0.5f, -0.35f, -0.6f), Cream);
            kit.Block(p, "Step2", new Vector3(-0.35f, -0.6f, -1.3f), new Vector3(0.05f, -0.48f, -0.7f), Cream);
        }

        void BuildLift()
        {
            Transform p = layout;
            const float lx = 4f, lz = -1f;
            kit.Block(p, "LiftPit", new Vector3(lx - 0.5f, -0.6f, lz - 0.5f), new Vector3(lx + 0.5f, -0.4f, lz + 0.5f), StoneDark);
            Color post = new(0.3f, 0.22f, 0.22f);
            for (int ix = -1; ix <= 1; ix += 2)
                for (int iz = -1; iz <= 1; iz += 2)
                    kit.Block(p, "LiftPost", new Vector3(lx + ix * 0.5f - 0.04f, -0.6f, lz + iz * 0.5f - 0.04f), new Vector3(lx + ix * 0.5f + 0.04f, 3.8f, lz + iz * 0.5f + 0.04f), post);
            kit.Block(p, "LiftBeamX", new Vector3(lx - 0.54f, 3.74f, lz - 0.04f), new Vector3(lx + 0.54f, 3.86f, lz + 0.04f), post);
            kit.Block(p, "LiftBeamZ", new Vector3(lx - 0.04f, 3.74f, lz - 0.54f), new Vector3(lx + 0.04f, 3.86f, lz + 0.54f), post);
            GameObject wheel = kit.Prim(p, PrimitiveType.Cylinder, "Pulley", new Vector3(lx, 3.72f, lz), new Vector3(0.36f, 0.04f, 0.36f), kit.Lit(Gold, 0.6f));
            wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            rope = kit.Box(p, "Rope", new Vector3(lx, 2f, lz), new Vector3(0.035f, 1f, 0.035f), new Color(0.25f, 0.2f, 0.18f)).transform;

            liftT = new GameObject("Lift").transform;
            liftT.SetParent(p, false);
            GameObject plate = kit.Box(liftT, "Platform", new Vector3(0f, -0.16f, 0f), new Vector3(0.94f, 0.32f, 0.94f), liftMat);
            kit.Box(liftT, "Trim", new Vector3(0f, -0.33f, 0f), new Vector3(0.98f, 0.05f, 0.98f), kit.Lit(Gold, 0.6f));
            // chunky red handle so it reads as "grab me"
            kit.Box(liftT, "Handle", new Vector3(0f, -0.16f, -0.56f), new Vector3(0.5f, 0.12f, 0.16f), liftMat);
            kit.Box(liftT, "HandleKnobL", new Vector3(-0.28f, -0.16f, -0.6f), new Vector3(0.12f, 0.2f, 0.2f), kit.Lit(Gold, 0.6f));
            kit.Box(liftT, "HandleKnobR", new Vector3(0.28f, -0.16f, -0.6f), new Vector3(0.12f, 0.2f, 0.2f), kit.Lit(Gold, 0.6f));
            var grab = plate.AddComponent<BoxCollider>();
            grab.size = new Vector3(1.1f, 1.6f, 1.6f);
            grab.center = new Vector3(0f, -0.9f, -0.1f);
            var pick = plate.AddComponent<L2Pick>();
            pick.group = PickGroup.Lift;
            liftNode = MakeNode("Lift", liftT, Vector3.zero, PickGroup.Lift, false, 0.1f);
            kit.Box(liftT, "Deck", new Vector3(0f, -0.02f, 0f), new Vector3(0.8f, 0.04f, 0.8f), new Color(0.88f, 0.72f, 0.55f));
        }

        void BuildHouseA()
        {
            Transform p = layout;
            Vector3 min = new(3.5f, -0.6f, -0.5f);
            Vector3 max = new(5.5f, 2.94f, 2.5f);
            kit.Block(p, "HouseA", min, max, Ochre);
            kit.Block(p, "HouseA_Base", new Vector3(3.46f, -0.6f, -0.54f), new Vector3(5.54f, -0.3f, 2.54f), new Color(0.55f, 0.42f, 0.36f));
            kit.Block(p, "HouseA_Cornice", new Vector3(3.45f, 2.74f, -0.55f), new Vector3(5.55f, 2.9f, 2.55f), Cream);
            Facade(p, min + new Vector3(0, 0.3f, 0), max - new Vector3(0, 0.25f, 0), 3, 1f, windowGroups[0], true);
            kit.Block(p, "ParapetE", new Vector3(5.4f, 2.94f, -0.5f), new Vector3(5.5f, 3.18f, 2.5f), Cream);
            kit.Block(p, "RoofStrip", new Vector3(4.5f, 2.94f, -0.5f), new Vector3(5.4f, 2.97f, 2.5f), new Color(0.75f, 0.6f, 0.5f));

            u0 = MakeNode("U0", p, new Vector3(4f, 3f, 0f), PickGroup.World);
            u1 = MakeNode("U1", p, new Vector3(4f, 3f, 1f), PickGroup.World);
            u2 = MakeNode("U2", p, new Vector3(4f, 3f, 2f), PickGroup.World);

            // balcony with plants on the street side (right of the lift shaft)
            kit.Block(p, "BalconyA", new Vector3(4.62f, 1.45f, -0.95f), new Vector3(5.4f, 1.53f, -0.5f), Cream);
            for (int i = 0; i < 3; i++)
                kit.Block(p, "Rail", new Vector3(4.66f + i * 0.33f, 1.53f, -0.95f), new Vector3(4.7f + i * 0.33f, 1.8f, -0.91f), Shutter);
            kit.Block(p, "RailTop", new Vector3(4.62f, 1.8f, -0.97f), new Vector3(5.4f, 1.84f, -0.89f), Shutter);
            kit.Ball(p, "Plant", new Vector3(5.1f, 1.7f, -0.75f), new Vector3(0.34f, 0.3f, 0.34f), LeafDark);
            kit.Ball(p, "Flower", new Vector3(5.2f, 1.82f, -0.8f), new Vector3(0.1f, 0.1f, 0.1f), new Color(0.95f, 0.45f, 0.6f));

            // shop sign "PHỞ" and awning
            kit.Block(p, "Awning", new Vector3(4.58f, 0.55f, -0.9f), new Vector3(5.45f, 0.62f, -0.5f), new Color(0.2f, 0.42f, 0.6f));
            kit.Block(p, "SignBoard", new Vector3(4.62f, 0.68f, -0.56f), new Vector3(5.42f, 1.08f, -0.52f), new Color(0.75f, 0.12f, 0.1f));
            kit.Sign(p, "PHỞ", new Vector3(5.02f, 0.88f, -0.575f), Quaternion.identity, 0.05f, new Color(1f, 0.86f, 0.3f));
            kit.Block(p, "SignBoard2", new Vector3(5.52f, 0.68f, 0.2f), new Vector3(5.56f, 1.08f, 1.8f), new Color(0.12f, 0.35f, 0.3f));
            kit.Sign(p, "CÀ PHÊ", new Vector3(5.575f, 0.88f, 1f), Quaternion.Euler(0f, -90f, 0f), 0.045f, new Color(1f, 0.95f, 0.85f));

            // rooftop: inox water tank, plants, laundry
            kit.Cyl(p, "TankLegs", new Vector3(5f, 2.94f, 0.2f), 0.18f, 0.2f, new Color(0.4f, 0.4f, 0.42f));
            GameObject tank = kit.Prim(p, PrimitiveType.Cylinder, "WaterTank", new Vector3(5f, 3.38f, 0.2f), new Vector3(0.42f, 0.28f, 0.42f), kit.Lit(new Color(0.85f, 0.87f, 0.9f), 0.8f));
            tank.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            kit.Cyl(p, "Pot", new Vector3(5f, 2.94f, 1.2f), 0.14f, 0.18f, Terracotta);
            kit.Ball(p, "PotPlant", new Vector3(5f, 3.25f, 1.2f), new Vector3(0.34f, 0.3f, 0.34f), Leaf);
            kit.Block(p, "LaundryPole1", new Vector3(5.12f, 2.94f, 0.6f), new Vector3(5.16f, 3.7f, 0.64f), Wood);
            kit.Block(p, "LaundryPole2", new Vector3(5.12f, 2.94f, 1.85f), new Vector3(5.16f, 3.7f, 1.89f), Wood);
            kit.Wire(p, new Vector3(5.14f, 3.68f, 0.62f), new Vector3(5.14f, 3.68f, 1.87f), 0.08f, new Color(0.2f, 0.2f, 0.2f), 0.015f);
            kit.Block(p, "Shirt", new Vector3(5.12f, 3.35f, 0.95f), new Vector3(5.16f, 3.62f, 1.2f), new Color(0.95f, 0.95f, 0.98f));
            kit.Block(p, "Shirt2", new Vector3(5.12f, 3.4f, 1.35f), new Vector3(5.16f, 3.62f, 1.6f), new Color(0.98f, 0.55f, 0.4f));

            // lantern 1 post
            lanterns[0] = LanternOnPost(p, new Vector3(5.05f, 2.94f, 2.15f), new Vector3(4.55f, 4.25f, 2.15f));
        }

        void BuildBridge()
        {
            Transform p = layout;
            kit.Cyl(p, "BridgePier", new Vector3(4f, -0.6f, 4f), 0.3f, 3.3f, Stone);
            kit.Cyl(p, "BridgePierCap", new Vector3(4f, 2.55f, 4f), 0.38f, 0.18f, Cream);
            kit.Cyl(p, "BridgePierFoot", new Vector3(4f, -0.6f, 4f), 0.42f, 0.2f, StoneDark);
            var pierGrab = new GameObject("PierGrab");
            pierGrab.transform.SetParent(p, false);
            pierGrab.transform.localPosition = new Vector3(4f, 1.05f, 4f);
            pierGrab.AddComponent<BoxCollider>().size = new Vector3(0.9f, 3.2f, 0.9f);
            pierGrab.AddComponent<L2Pick>().group = PickGroup.Bridge;
            crankT = new GameObject("Crank").transform;
            crankT.SetParent(p, false);
            crankT.localPosition = new Vector3(4f, 1.5f, 4f);
            kit.Prim(crankT, PrimitiveType.Cylinder, "Collar", Vector3.zero, new Vector3(0.72f, 0.09f, 0.72f), bridgeMat);
            for (int k = 0; k < 4; k++)
            {
                Quaternion q = Quaternion.Euler(0f, k * 90f, 0f);
                kit.Box(crankT, "Spoke", q * new Vector3(0f, 0f, 0.5f), new Vector3(0.07f, 0.07f, 0.34f), kit.Lit(Gold, 0.6f))
                    .transform.localRotation = q;
                kit.Ball(crankT, "Grip", q * new Vector3(0f, 0f, 0.7f), Vector3.one * 0.13f, Lacquer);
            }

            bridgeT = new GameObject("Bridge").transform;
            bridgeT.SetParent(p, false);
            bridgeT.localPosition = new Vector3(4f, 3f, 4f);
            GameObject deck = kit.Box(bridgeT, "BridgeDeck", new Vector3(0f, -0.13f, 0f), new Vector3(0.92f, 0.26f, 2.96f), bridgeMat);
            kit.Box(bridgeT, "BridgeKeel", new Vector3(0f, -0.3f, 0f), new Vector3(0.6f, 0.12f, 2.6f), kit.Lit(Gold, 0.6f));
            for (int side = -1; side <= 1; side += 2)
            {
                kit.Box(bridgeT, "Rail", new Vector3(side * 0.43f, 0.2f, 0f), new Vector3(0.05f, 0.05f, 1.9f), bridgeMat);
                for (int k = -2; k <= 2; k++)
                    kit.Box(bridgeT, "Baluster", new Vector3(side * 0.43f, 0.1f, k * 0.46f), new Vector3(0.06f, 0.2f, 0.06f), bridgeMat);
            }
            kit.Prim(bridgeT, PrimitiveType.Cylinder, "PivotMedal", new Vector3(0f, 0.005f, 0f), new Vector3(0.42f, 0.01f, 0.42f), kit.Lit(Gold, 0.7f));
            var grab = deck.AddComponent<BoxCollider>();
            grab.size = new Vector3(1.3f, 0.6f, 3.1f);
            grab.center = new Vector3(0f, -0.32f, 0f);
            var pick = deck.AddComponent<L2Pick>();
            pick.group = PickGroup.Bridge;

            bSouth = MakeNode("BridgeS", bridgeT, new Vector3(0f, 0f, -1f), PickGroup.Bridge, false, 0.1f);
            bPivot = MakeNode("BridgeP", bridgeT, Vector3.zero, PickGroup.Bridge, false, 0.1f);
            bNorth = MakeNode("BridgeN", bridgeT, new Vector3(0f, 0f, 1f), PickGroup.Bridge, false, 0.1f);
        }

        void BuildVilla()
        {
            Transform p = layout;
            Vector3 min = new(-0.5f, -0.6f, 5.5f);
            Vector3 max = new(4.5f, 2.94f, 7.5f);
            kit.Block(p, "Villa", min, max, Mustard);
            kit.Block(p, "Villa_Base", new Vector3(-0.54f, -0.6f, 5.46f), new Vector3(4.54f, -0.25f, 7.54f), new Color(0.6f, 0.5f, 0.42f));
            kit.Block(p, "Villa_Cornice", new Vector3(-0.55f, 2.72f, 5.45f), new Vector3(4.55f, 2.9f, 7.55f), Cream);
            kit.Block(p, "Villa_Band", new Vector3(-0.53f, 1.15f, 5.47f), new Vector3(4.53f, 1.25f, 7.53f), Cream);
            Facade(p, min + new Vector3(0, 0.35f, 0), max - new Vector3(0, 0.25f, 0), 3, 1f, windowGroups[1], true);

            c0 = MakeNode("C0", p, new Vector3(4f, 3f, 6f), PickGroup.World);
            c1 = MakeNode("C1", p, new Vector3(3f, 3f, 6f), PickGroup.World);
            c2 = MakeNode("C2", p, new Vector3(2f, 3f, 6f), PickGroup.World);
            c3 = MakeNode("C3", p, new Vector3(1f, 3f, 6f), PickGroup.World);
            c4 = MakeNode("C4", p, new Vector3(0f, 3f, 6f), PickGroup.World);

            // tiled roof behind the walkway
            kit.Gable(p, "VillaRoof", new Vector3(2f, 2.94f, 7.0f), new Vector3(5.1f, 0.85f, 1.0f), Terracotta);
            kit.Block(p, "Chimney", new Vector3(0.3f, 2.94f, 6.9f), new Vector3(0.6f, 3.95f, 7.2f), new Color(0.7f, 0.5f, 0.4f));
            kit.Block(p, "Dormer", new Vector3(3.2f, 2.94f, 6.55f), new Vector3(3.8f, 3.5f, 6.9f), Cream);
            kit.Block(p, "DormerGlass", new Vector3(3.3f, 3.0f, 6.53f), new Vector3(3.7f, 3.4f, 6.56f), Glass);
            kit.Gable(p, "DormerRoof", new Vector3(3.5f, 3.5f, 6.72f), new Vector3(0.7f, 0.3f, 0.45f), Terracotta, true);

            // French balcony on the lake side
            kit.Block(p, "VillaBalcony", new Vector3(0.6f, 1.25f, 5.1f), new Vector3(3.4f, 1.33f, 5.5f), Cream);
            for (int i = 0; i < 10; i++)
                kit.Block(p, "Rail", new Vector3(0.62f + i * 0.31f, 1.33f, 5.1f), new Vector3(0.66f + i * 0.31f, 1.58f, 5.14f), Cream);
            kit.Block(p, "RailTop", new Vector3(0.6f, 1.58f, 5.08f), new Vector3(3.4f, 1.62f, 5.16f), Cream);
            kit.Ball(p, "Bougainvillea", new Vector3(0.75f, 1.6f, 5.3f), new Vector3(0.4f, 0.35f, 0.3f), new Color(0.88f, 0.3f, 0.55f));
            kit.Ball(p, "Bougainvillea", new Vector3(3.25f, 1.55f, 5.3f), new Vector3(0.36f, 0.3f, 0.3f), new Color(0.82f, 0.25f, 0.5f));

            // string lights along the walkway edge (light up at the end)
            for (int i = 0; i < 12; i++)
            {
                float x = -0.3f + i * 0.42f;
                GameObject bulb = kit.Ball(p, "Fairy", new Vector3(x, 2.84f - Mathf.Sin(i * 0.9f) * 0.04f, 5.44f), Vector3.one * 0.07f, Color.white);
                Renderer r = bulb.GetComponent<Renderer>();
                r.sharedMaterial = fairyOff;
                fairyLights.Add(r);
            }

            lanterns[1] = LanternOnPost(p, new Vector3(2f, 2.94f, 6.62f), new Vector3(2f, 4.25f, 6.2f));
        }

        void BuildTower()
        {
            Transform p = layout;
            kit.Block(p, "Islet", new Vector3(-4.75f, -0.6f, 3.25f), new Vector3(-2.25f, -0.38f, 4.75f), StoneDark);
            kit.Block(p, "TowerT1", new Vector3(-4.5f, -0.38f, 3.5f), new Vector3(-2.5f, 1.9f, 4.5f), Stone);
            kit.Block(p, "TowerLedge1", new Vector3(-4.62f, 1.84f, 3.38f), new Vector3(-2.38f, 1.98f, 4.62f), Cream);
            kit.Block(p, "TowerT2", new Vector3(-4.45f, 1.98f, 3.55f), new Vector3(-2.55f, 3.5f, 4.45f), new Color(0.84f, 0.82f, 0.76f));
            kit.Block(p, "TowerLedge2", new Vector3(-4.6f, 3.44f, 3.4f), new Vector3(-2.4f, 3.58f, 4.6f), Cream);
            kit.Block(p, "TowerT3", new Vector3(-4.5f, 3.58f, 3.5f), new Vector3(-2.5f, 4.94f, 4.5f), Stone);
            kit.Block(p, "TowerMoss", new Vector3(-4.52f, -0.38f, 3.48f), new Vector3(-3.9f, 0.4f, 4.52f), Moss);
            // arched openings
            for (int f = 0; f < 3; f++)
            {
                float y = 0.5f + f * 1.6f;
                for (int k = 0; k < 2; k++)
                {
                    float x = -4f + k;
                    kit.Block(p, "Arch", new Vector3(x - 0.18f, y, 3.47f), new Vector3(x + 0.18f, y + 0.6f, 3.5f), Glass);
                    kit.Box(p, "ArchTop", new Vector3(x, y + 0.6f, 3.485f), new Vector3(0.36f, 0.18f, 0.03f), Glass).transform.localRotation = Quaternion.Euler(0, 0, 45f);
                    kit.Block(p, "ArchB", new Vector3(x - 0.18f, y, 4.5f), new Vector3(x + 0.18f, y + 0.6f, 4.53f), Glass);
                }
                kit.Block(p, "ArchW", new Vector3(-4.53f, y, 3.82f), new Vector3(-4.5f, y + 0.6f, 4.18f), Glass);
                kit.Block(p, "ArchE", new Vector3(-2.5f, y, 3.82f), new Vector3(-2.47f, y + 0.6f, 4.18f), Glass);
            }

            t0 = MakeNode("T0", p, new Vector3(-3f, 5f, 4f), PickGroup.World);
            t1 = MakeNode("T1", p, new Vector3(-4f, 5f, 4f), PickGroup.World);

            // pagoda gazebo over T1
            Color red = Lacquer;
            for (int ix = -1; ix <= 1; ix += 2)
                for (int iz = -1; iz <= 1; iz += 2)
                    kit.Block(p, "GazeboPost", new Vector3(-4f + ix * 0.42f - 0.04f, 4.94f, 4f + iz * 0.42f - 0.04f), new Vector3(-4f + ix * 0.42f + 0.04f, 6.4f, 4f + iz * 0.42f + 0.04f), red);
            kit.Block(p, "GazeboBeam", new Vector3(-4.5f, 6.36f, 3.5f), new Vector3(-3.5f, 6.46f, 4.5f), red);
            kit.Pyramid(p, "GazeboRoof1", new Vector3(-4f, 6.44f, 4f), 1.55f, 0.5f, Terracotta);
            kit.Pyramid(p, "GazeboRoof2", new Vector3(-4f, 6.82f, 4f), 0.95f, 0.45f, new Color(0.66f, 0.28f, 0.2f));
            kit.Ball(p, "Finial", new Vector3(-4f, 7.3f, 4f), Vector3.one * 0.14f, Gold);
            bell = new GameObject("BellPivot").transform;
            bell.SetParent(p, false);
            bell.localPosition = new Vector3(-4f, 6.36f, 4f);
            kit.Box(bell, "BellRope", new Vector3(0f, -0.06f, 0f), new Vector3(0.03f, 0.12f, 0.03f), Wood);
            kit.Cyl(bell, "Bell", new Vector3(0f, -0.42f, 0f), 0.16f, 0.32f, new Color(0.72f, 0.5f, 0.25f));
            kit.Ball(bell, "BellTop", new Vector3(0f, -0.1f, 0f), new Vector3(0.32f, 0.16f, 0.32f), new Color(0.72f, 0.5f, 0.25f));

            Lantern l = MakeLantern(p, new Vector3(-3.56f, 6.36f, 3.56f), 0.28f);
            lanterns[2] = l;
        }

        void BuildProps()
        {
            Transform p = layout;
            // banyan tree by the street
            Tree(p, new Vector3(-1.7f, -0.6f, -0.2f), 1.1f, Leaf, LeafDark);
            // phượng (flame tree) by the villa and behind the house
            Tree(p, new Vector3(5.7f, -0.6f, 6.9f), 1.0f, Flame, new Color(0.85f, 0.28f, 0.18f));
            Tree(p, new Vector3(-4.3f, -0.6f, 7.2f), 0.9f, Leaf, Flame);
            Tree(p, new Vector3(-4.4f, -0.6f, -0.8f), 0.8f, LeafDark, Leaf);

            // street-food corner: tiny plastic stools and a table
            Color[] stool = { new(0.2f, 0.45f, 0.85f), new(0.9f, 0.25f, 0.2f), new(0.2f, 0.45f, 0.85f) };
            for (int i = 0; i < 3; i++)
                kit.Block(p, "Stool", new Vector3(-1.4f + i * 0.35f, -0.6f, -1.35f), new Vector3(-1.22f + i * 0.35f, -0.42f, -1.17f), stool[i]);
            kit.Block(p, "LowTable", new Vector3(-1.3f, -0.6f, -1.05f), new Vector3(-0.7f, -0.36f, -0.75f), new Color(0.85f, 0.85f, 0.88f));
            kit.Cyl(p, "Bowl", new Vector3(-1.1f, -0.36f, -0.9f), 0.06f, 0.04f, Cream);
            kit.Cyl(p, "Bowl", new Vector3(-0.9f, -0.36f, -0.9f), 0.06f, 0.04f, Cream);

            // a little Cub motorbike
            Transform bike = new GameObject("Motorbike").transform;
            bike.SetParent(p, false);
            bike.localPosition = new Vector3(1.8f, -0.6f, 0.4f);
            bike.localRotation = Quaternion.Euler(0f, 20f, 0f);
            kit.Box(bike, "Body", new Vector3(0f, 0.22f, 0f), new Vector3(0.5f, 0.16f, 0.14f), new Color(0.2f, 0.5f, 0.75f));
            kit.Box(bike, "Seat", new Vector3(-0.08f, 0.33f, 0f), new Vector3(0.26f, 0.06f, 0.14f), new Color(0.15f, 0.12f, 0.12f));
            kit.Box(bike, "Front", new Vector3(0.22f, 0.36f, 0f), new Vector3(0.06f, 0.3f, 0.08f), new Color(0.2f, 0.5f, 0.75f));
            GameObject w1 = kit.Prim(bike, PrimitiveType.Cylinder, "Wheel", new Vector3(0.22f, 0.1f, 0f), new Vector3(0.2f, 0.03f, 0.2f), kit.Lit(new Color(0.1f, 0.1f, 0.1f)));
            w1.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject w2 = kit.Prim(bike, PrimitiveType.Cylinder, "Wheel", new Vector3(-0.22f, 0.1f, 0f), new Vector3(0.2f, 0.03f, 0.2f), kit.Lit(new Color(0.1f, 0.1f, 0.1f)));
            w2.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // electricity pole with the famous tangle of wires
            kit.Block(p, "Pole", new Vector3(-0.45f, -0.6f, -2.0f), new Vector3(-0.35f, 3.4f, -1.9f), new Color(0.55f, 0.52f, 0.5f));
            kit.Block(p, "PoleArm", new Vector3(-0.7f, 3.1f, -1.97f), new Vector3(-0.1f, 3.16f, -1.93f), new Color(0.4f, 0.38f, 0.36f));
            Color wire = new(0.12f, 0.1f, 0.12f);
            kit.Wire(p, new Vector3(-0.4f, 3.15f, -1.95f), new Vector3(3.5f, 2.6f, 0.8f), 0.35f, wire);
            kit.Wire(p, new Vector3(-0.4f, 3.0f, -1.95f), new Vector3(3.5f, 2.2f, 1.6f), 0.5f, wire);
            kit.Wire(p, new Vector3(-0.6f, 3.12f, -1.95f), new Vector3(-0.5f, 2.6f, 5.5f), 0.6f, wire);
            kit.Wire(p, new Vector3(-0.2f, 3.12f, -1.95f), new Vector3(3.5f, 2.9f, 2.2f), 0.4f, wire);
            kit.Wire(p, new Vector3(5.5f, 2.6f, 2.3f), new Vector3(4.5f, 2.5f, 5.5f), 0.45f, wire);
            // bunting with little bulbs from the house to the villa
            for (int i = 0; i < 7; i++)
            {
                float t = (i + 1) / 8f;
                Vector3 a = new(5.45f, 2.65f, 2.5f), b = new(4.5f, 2.65f, 5.5f);
                Vector3 q = Vector3.Lerp(a, b, t);
                q.y -= 0.5f * 4f * t * (1f - t);
                GameObject bulb = kit.Ball(p, "Fairy", q, Vector3.one * 0.07f, Color.white);
                Renderer r = bulb.GetComponent<Renderer>();
                r.sharedMaterial = fairyOff;
                fairyLights.Add(r);
            }
            kit.Wire(p, new Vector3(5.45f, 2.65f, 2.5f), new Vector3(4.5f, 2.65f, 5.5f), 0.5f, wire, 0.015f);

            // boat on the lake
            Transform boat = new GameObject("Boat").transform;
            boat.SetParent(p, false);
            boat.localPosition = new Vector3(1.4f, -0.54f, 4.3f);
            boat.localRotation = Quaternion.Euler(0f, 30f, 0f);
            kit.Box(boat, "Hull", new Vector3(0f, 0.04f, 0f), new Vector3(0.7f, 0.08f, 0.22f), Wood);
            kit.Gable(boat, "Canopy", new Vector3(0f, 0.08f, 0f), new Vector3(0.3f, 0.14f, 0.2f), new Color(0.55f, 0.45f, 0.3f));
        }

        void Tree(Transform p, Vector3 at, float scale, Color a, Color b)
        {
            kit.Cyl(p, "Trunk", at, 0.09f * scale, 1.1f * scale, Wood);
            kit.Ball(p, "Canopy", at + new Vector3(0f, 1.3f, 0f) * scale, new Vector3(1.1f, 0.8f, 1.1f) * scale, a);
            kit.Ball(p, "Canopy", at + new Vector3(0.35f, 1.1f, 0.2f) * scale, new Vector3(0.7f, 0.55f, 0.7f) * scale, b);
            kit.Ball(p, "Canopy", at + new Vector3(-0.3f, 1.15f, -0.25f) * scale, new Vector3(0.65f, 0.5f, 0.65f) * scale, b);
        }

        /// <summary>Windows with open green shutters on all four faces.</summary>
        void Facade(Transform p, Vector3 min, Vector3 max, int floors, float spacing, List<Renderer> windows, bool shutters)
        {
            float floorH = (max.y - min.y) / floors;
            for (int f = 0; f < floors; f++)
            {
                float y = min.y + floorH * (f + 0.5f);
                float wh = floorH * 0.55f;
                FaceRow(p, new Vector3(0, 0, -1), new Vector3(min.x, y, min.z), new Vector3(max.x, y, min.z), wh, spacing, windows, shutters);
                FaceRow(p, new Vector3(0, 0, 1), new Vector3(min.x, y, max.z), new Vector3(max.x, y, max.z), wh, spacing, windows, shutters);
                FaceRow(p, new Vector3(-1, 0, 0), new Vector3(min.x, y, min.z), new Vector3(min.x, y, max.z), wh, spacing, windows, shutters);
                FaceRow(p, new Vector3(1, 0, 0), new Vector3(max.x, y, min.z), new Vector3(max.x, y, max.z), wh, spacing, windows, shutters);
            }
        }

        void FaceRow(Transform p, Vector3 normal, Vector3 a, Vector3 b, float wh, float spacing, List<Renderer> windows, bool shutters)
        {
            float length = Vector3.Distance(a, b);
            int count = Mathf.Max(1, Mathf.FloorToInt(length / spacing + 0.01f));
            Vector3 along = (b - a).normalized;
            bool alongX = Mathf.Abs(along.x) > 0.5f;
            for (int i = 0; i < count; i++)
            {
                Vector3 c = Vector3.Lerp(a, b, (i + 0.5f) / count) + normal * 0.015f;
                Vector3 glassSize = alongX ? new Vector3(0.34f, wh, 0.03f) : new Vector3(0.03f, wh, 0.34f);
                GameObject glass = kit.Box(p, "Window", c, glassSize, windowDark);
                windows.Add(glass.GetComponent<Renderer>());
                Vector3 sill = alongX ? new Vector3(0.46f, 0.05f, 0.08f) : new Vector3(0.08f, 0.05f, 0.46f);
                kit.Box(p, "Sill", c + Vector3.down * (wh * 0.5f + 0.02f) + normal * 0.03f, sill, Cream);
                if (!shutters)
                    continue;
                Vector3 sh = alongX ? new Vector3(0.12f, wh + 0.04f, 0.025f) : new Vector3(0.025f, wh + 0.04f, 0.12f);
                kit.Box(p, "Shutter", c + along * 0.25f + normal * 0.02f, sh, Shutter);
                kit.Box(p, "Shutter", c - along * 0.25f + normal * 0.02f, sh, Shutter);
            }
        }

        Lantern LanternOnPost(Transform p, Vector3 foot, Vector3 hang)
        {
            float top = hang.y + 0.06f;
            kit.Block(p, "LanternPost", new Vector3(foot.x - 0.04f, foot.y, foot.z - 0.04f), new Vector3(foot.x + 0.04f, top, foot.z + 0.04f), Wood);
            Vector3 armA = new(foot.x, top - 0.03f, foot.z);
            Vector3 mid = (armA + new Vector3(hang.x, top - 0.03f, hang.z)) * 0.5f;
            Vector3 dir = new Vector3(hang.x, top, hang.z) - new Vector3(foot.x, top, foot.z);
            GameObject arm = kit.Box(p, "LanternArm", mid, new Vector3(0.05f, 0.05f, Mathf.Max(0.05f, dir.magnitude + 0.06f)), kit.Lit(Wood));
            if (dir.sqrMagnitude > 0.0001f)
                arm.transform.localRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            return MakeLantern(p, hang, 0.3f);
        }

        Lantern MakeLantern(Transform p, Vector3 hang, float drop)
        {
            var lantern = new Lantern();
            lantern.pivot = new GameObject("Lantern").transform;
            lantern.pivot.SetParent(p, false);
            lantern.pivot.localPosition = hang;
            lantern.phase = Random.value * 6f;
            kit.Box(lantern.pivot, "String", new Vector3(0f, -drop * 0.5f, 0f), new Vector3(0.02f, drop, 0.02f), Wood);
            GameObject shade = kit.Ball(lantern.pivot, "Shade", new Vector3(0f, -drop - 0.2f, 0f), new Vector3(0.34f, 0.4f, 0.34f), new Color(0.55f, 0.12f, 0.1f));
            lantern.shade = shade.transform;
            lantern.shadeRenderer = shade.GetComponent<Renderer>();
            kit.Cyl(lantern.pivot, "CapTop", new Vector3(0f, -drop - 0.03f, 0f), 0.09f, 0.05f, Gold);
            kit.Cyl(lantern.pivot, "CapBottom", new Vector3(0f, -drop - 0.42f, 0f), 0.08f, 0.05f, Gold);
            kit.Box(lantern.pivot, "Tassel", new Vector3(0f, -drop - 0.55f, 0f), new Vector3(0.04f, 0.18f, 0.04f), Lacquer);
            var lightGo = new GameObject("Glow");
            lightGo.transform.SetParent(lantern.shade, false);
            lantern.light = lightGo.AddComponent<Light>();
            lantern.light.type = LightType.Point;
            lantern.light.range = 3.2f;
            lantern.light.intensity = 2.2f;
            lantern.light.color = new Color(1f, 0.62f, 0.32f);
            lantern.light.shadows = LightShadows.None;
            lantern.light.enabled = false;
            return lantern;
        }

        void LinkStatic()
        {
            s0.Link(s1);
            s1.Link(s2);
            u0.Link(u1);
            u1.Link(u2);
            bSouth.Link(bPivot);
            bPivot.Link(bNorth);
            c0.Link(c1);
            c1.Link(c2);
            c2.Link(c3);
            c3.Link(c4);
            t0.Link(t1);
            // impossible seam: the tower tile projects next to the villa roof when the city faces yaw 0
            t0.seamPeer = c4;
            t0.seamOffset = Level2Rules.SeamOffset;
            c4.seamPeer = t0;
        }

        void BuildMarker()
        {
            GameObject m = kit.Box(root, "HintMarker", Vector3.zero, Vector3.one * 0.2f, kit.Glow(new Color(1f, 0.85f, 0.4f)));
            L2Kit.NoShadow(m);
            marker = m.transform;
            marker.SetParent(null, true);
            m.SetActive(false);
        }

        void BuildPlayer()
        {
            var go = new GameObject("Player");
            go.transform.SetParent(layout, false);
            if (bodyPrefab != null)
            {
                GameObject body = Instantiate(bodyPrefab, go.transform);
                body.name = "Body";
                body.transform.localPosition = Vector3.zero;
                body.transform.localRotation = Quaternion.identity;
                foreach (Collider c in body.GetComponentsInChildren<Collider>())
                    L2Kit.SafeDestroy(c);
                Bounds b = RendererBounds(body);
                if (b.size.y > 0.01f)
                    body.transform.localScale = Vector3.one * (PlayerHeight / b.size.y);
                Material charMat = Resources.Load<Material>("Mat_Character_Ch09");
#if UNITY_EDITOR
                if (charMat == null)
                    charMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Mat_Character_Ch09.mat");
#endif
                if (charMat != null)
                {
                    foreach (var r in body.GetComponentsInChildren<Renderer>(true))
                        r.sharedMaterial = charMat;
                }
            }
            else
            {
                kit.Prim(go.transform, PrimitiveType.Capsule, "Body", new Vector3(0f, PlayerHeight * 0.5f, 0f), new Vector3(0.3f, PlayerHeight * 0.5f, 0.3f), kit.Lit(Cream));
            }

            player = go.AddComponent<PlayerMovement>();
            var actor = go.AddComponent<ClipActor>();
            actor.movement = player;
            if (actor.idleClip == null) actor.idleClip = idleClip;
            if (actor.walkClip == null) actor.walkClip = walkClip;
            if (actor.carryClip == null) actor.carryClip = carryClip;
            foreach (Animator a in go.GetComponentsInChildren<Animator>())
                a.applyRootMotion = false;
            foreach (Collider c in go.GetComponentsInChildren<Collider>())
                L2Kit.SafeDestroy(c);
            player.SetPace(0.34f);
            player.PlaceAt(s0);
            go.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            lastNode = s0;
        }

        static Bounds RendererBounds(GameObject go)
        {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0)
                return new Bounds(go.transform.position, Vector3.zero);
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++)
                b.Encapsulate(rs[i].bounds);
            return b;
        }

        // =====================================================================
        // Ambient animation
        // =====================================================================

        void AnimateWorld(float t)
        {
            for (int i = 0; i < lanterns.Length; i++)
            {
                Lantern l = lanterns[i];
                if (l == null)
                    continue;
                l.pivot.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.1f + l.phase) * 3f, 0f, Mathf.Sin(t * 1.4f + l.phase) * 5f);
                if (l.lit)
                    l.light.intensity = 2.0f + Mathf.PerlinNoise(t * 3f, l.phase) * 0.8f;
            }

            for (int i = skyLanterns.Count - 1; i >= 0; i--)
            {
                SkyLantern s = skyLanterns[i];
                if (s.t == null)
                {
                    skyLanterns.RemoveAt(i);
                    continue;
                }
                Vector3 pos = s.t.position;
                pos.y += s.speed * Time.deltaTime;
                pos.x += Mathf.Sin(t * 0.7f + s.phase) * s.sway * Time.deltaTime;
                s.t.position = pos;
                if (pos.y > 26f)
                {
                    Destroy(s.t.gameObject);
                    skyLanterns.RemoveAt(i);
                }
            }

            if (State == Phase.End && skyLanterns.Count < 34 && Random.value < Time.deltaTime * 3f)
                SpawnSkyLanterns(1);
        }

        void SpawnSkyLanterns(int count)
        {
            Material m = kit.Glow(new Color(1f, 0.68f, 0.35f));
            for (int i = 0; i < count; i++)
            {
                GameObject go = kit.Box(null, "SkyLantern", Vector3.zero, new Vector3(0.18f, 0.24f, 0.18f), m);
                L2Kit.NoShadow(go);
                Vector2 ring = Random.insideUnitCircle * 6.5f;
                go.transform.position = new Vector3(ring.x, -1f - Random.value * 3f, ring.y);
                skyLanterns.Add(new SkyLantern
                {
                    t = go.transform,
                    speed = 0.35f + Random.value * 0.45f,
                    sway = 0.3f + Random.value * 0.4f,
                    phase = Random.value * 6f
                });
            }
        }
    }
}
