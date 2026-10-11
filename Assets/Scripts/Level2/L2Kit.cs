using System.Collections.Generic;
using UnityEngine;

namespace PerspectivePuzzle.Level2
{
    /// <summary>
    /// Tiny procedural modelling kit for the Hanoi diorama: boxes, prisms, pyramids, discs and wires,
    /// all built at runtime from three shared material templates so the shaders ship in the build.
    /// </summary>
    public class L2Kit
    {
        readonly Material lit;
        readonly Material vertex;
        readonly Material glow;
        readonly Dictionary<Color, Material> litCache = new();
        readonly Dictionary<Color, Material> glowCache = new();
        static Mesh cube;

        public L2Kit(Material litTemplate, Material vertexTemplate, Material glowTemplate)
        {
            lit = litTemplate != null ? litTemplate : new Material(Shader.Find("Standard"));
            vertex = vertexTemplate != null ? vertexTemplate : new Material(Shader.Find("Sprites/Default"));
            glow = glowTemplate != null ? glowTemplate : new Material(Shader.Find("Unlit/Color"));
        }

        public Material VertexMaterial => vertex;

        public Material Lit(Color color, float smooth = -1f)
        {
            if (smooth < 0f && litCache.TryGetValue(color, out Material cached))
                return cached;
            var m = new Material(lit) { color = color };
            if (smooth >= 0f)
            {
                m.SetFloat("_Glossiness", smooth);
                return m;
            }
            litCache[color] = m;
            return m;
        }

        public Material Glow(Color color)
        {
            if (glowCache.TryGetValue(color, out Material cached))
                return cached;
            var m = new Material(glow) { color = color };
            glowCache[color] = m;
            return m;
        }

        public Material NewGlow(Color color)
        {
            return new Material(glow) { color = color };
        }

        public static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }

        // ---------- primitives ----------

        static Mesh Cube
        {
            get
            {
                if (cube == null)
                {
                    var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube = tmp.GetComponent<MeshFilter>().sharedMesh;
                    SafeDestroy(tmp);
                }
                return cube;
            }
        }

        public GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Color color)
        {
            return Box(parent, name, center, size, Lit(color));
        }

        public GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.AddComponent<MeshFilter>().sharedMesh = Cube;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        /// <summary>Box from min corner to max corner.</summary>
        public GameObject Block(Transform parent, string name, Vector3 min, Vector3 max, Color color)
        {
            return Box(parent, name, (min + max) * 0.5f, max - min, color);
        }

        public GameObject Prim(Transform parent, PrimitiveType type, string name, Vector3 center, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            SafeDestroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        public GameObject Cyl(Transform parent, string name, Vector3 baseCenter, float radius, float height, Color color)
        {
            return Prim(parent, PrimitiveType.Cylinder, name, baseCenter + Vector3.up * height * 0.5f,
                new Vector3(radius * 2f, height * 0.5f, radius * 2f), Lit(color));
        }

        public GameObject Ball(Transform parent, string name, Vector3 center, Vector3 size, Color color)
        {
            return Prim(parent, PrimitiveType.Sphere, name, center, size, Lit(color));
        }

        /// <summary>Gable roof: a triangular prism whose ridge runs along local X (or Z when alongZ).</summary>
        public GameObject Gable(Transform parent, string name, Vector3 baseCenter, Vector3 size, Color color, bool alongZ = false)
        {
            float hx = size.x * 0.5f, hz = size.z * 0.5f, h = size.y;
            Vector3[] p;
            if (!alongZ)
            {
                // ridge along X at z=0
                p = new[]
                {
                    new Vector3(-hx, 0, -hz), new Vector3(hx, 0, -hz), new Vector3(hx, h, 0), new Vector3(-hx, h, 0),
                    new Vector3(-hx, 0, hz), new Vector3(hx, 0, hz)
                };
            }
            else
            {
                p = new[]
                {
                    new Vector3(-hx, 0, hz), new Vector3(-hx, 0, -hz), new Vector3(0, h, -hz), new Vector3(0, h, hz),
                    new Vector3(hx, 0, hz), new Vector3(hx, 0, -hz)
                };
            }

            var tris = new List<Vector3>();
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) { tris.Add(a); tris.Add(c); tris.Add(b); tris.Add(a); tris.Add(d); tris.Add(c); }
            void Tri(Vector3 a, Vector3 b, Vector3 c) { tris.Add(a); tris.Add(b); tris.Add(c); }
            Quad(p[0], p[1], p[2], p[3]);           // front slope
            Quad(p[5], p[4], p[3], p[2]);           // back slope
            Tri(p[0], p[4], p[3]);                  // gable end
            Tri(p[1], p[2], p[5]);                  // gable end
            Quad(p[0], p[4], p[5], p[1]);           // bottom
            return MeshObject(parent, name, baseCenter, tris, Lit(color));
        }

        /// <summary>Four-sided pyramid roof with optional upturned eaves feel (flared base).</summary>
        public GameObject Pyramid(Transform parent, string name, Vector3 baseCenter, float width, float height, Color color)
        {
            float h = width * 0.5f;
            Vector3 a = new(-h, 0, -h), b = new(h, 0, -h), c = new(h, 0, h), d = new(-h, 0, h), top = new(0, height, 0);
            var tris = new List<Vector3> { a, top, b, b, top, c, c, top, d, d, top, a, a, b, c, a, c, d };
            return MeshObject(parent, name, baseCenter, tris, Lit(color));
        }

        GameObject MeshObject(Transform parent, string name, Vector3 at, List<Vector3> tris, Material material)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(tris);
            var idx = new int[tris.Count];
            for (int i = 0; i < idx.Length; i++)
                idx[i] = i;
            mesh.SetTriangles(idx, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        /// <summary>Hanging wire between two points (local to parent), with sag.</summary>
        public LineRenderer Wire(Transform parent, Vector3 a, Vector3 b, float sag, Color color, float width = 0.025f)
        {
            var go = new GameObject("Wire");
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.sharedMaterial = vertex;
            line.startColor = color;
            line.endColor = color;
            line.widthMultiplier = width;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            const int n = 14;
            line.positionCount = n;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                Vector3 p = Vector3.Lerp(a, b, t);
                p.y -= sag * 4f * t * (1f - t);
                line.SetPosition(i, p);
            }
            return line;
        }

        /// <summary>Full-screen style vertical gradient quad (vertex coloured, unlit).</summary>
        public GameObject Gradient(Transform parent, string name, float width, float height, Color[] stops)
        {
            var verts = new List<Vector3>();
            var cols = new List<Color>();
            var idx = new List<int>();
            for (int i = 0; i < stops.Length; i++)
            {
                float y = Mathf.Lerp(-height * 0.5f, height * 0.5f, i / (float)(stops.Length - 1));
                verts.Add(new Vector3(-width * 0.5f, y, 0f));
                verts.Add(new Vector3(width * 0.5f, y, 0f));
                cols.Add(stops[i]);
                cols.Add(stops[i]);
                if (i > 0)
                {
                    int b = (i - 1) * 2;
                    idx.Add(b); idx.Add(b + 2); idx.Add(b + 1);
                    idx.Add(b + 1); idx.Add(b + 2); idx.Add(b + 3);
                }
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetColors(cols);
            mesh.SetTriangles(idx, 0);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = vertex;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        /// <summary>Soft radial disc (vertex coloured): centre colour fading to transparent edge.</summary>
        public GameObject Disc(Transform parent, string name, float radius, Color centre, Color edge, int segments = 40)
        {
            var verts = new List<Vector3> { Vector3.zero };
            var cols = new List<Color> { centre };
            var idx = new List<int>();
            for (int i = 0; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius);
                cols.Add(edge);
                if (i > 0)
                {
                    idx.Add(0); idx.Add(i + 1); idx.Add(i);
                }
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetColors(cols);
            mesh.SetTriangles(idx, 0);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = vertex;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        public TextMesh Sign(Transform parent, string text, Vector3 at, Quaternion rotation, float size, Color color)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go = new GameObject("Sign_" + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            go.transform.localRotation = rotation;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = font;
            tm.fontSize = 64;
            tm.characterSize = size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            tm.fontStyle = FontStyle.Bold;
            if (font != null)
                go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            return tm;
        }

        public static void NoShadow(GameObject go)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
