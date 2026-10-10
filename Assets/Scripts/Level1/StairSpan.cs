using PerspectivePuzzle.Movement;
using UnityEngine;

namespace PerspectivePuzzle.Level1
{
    /// <summary>
    /// Imported floor plank between the lower stair's top lip and the upper stair's first tread.
    /// Full length only while the isometric view joins those lips.
    /// </summary>
    public class StairSpan : MonoBehaviour
    {
        const string ModelPath = "Assets/Art/Models/Connect/connect/connect1.fbx";
        const string TexturePath = "Assets/Art/Models/Connect/connect/basecolor.png";

        public const float JoinYaw = 297f;
        public const float FullDegrees = 14f;
        public const float GoneDegrees = 42f;
        public const float StubLength = 0.15f;
        public const float Thickness = 0.12f;

        // Plane.002 top exit edge, and the center of Plane.003's lowest tread.
        public static readonly Vector3 NearLip = new Vector3(1.50f, 5.07f, 5.655f);
        public static readonly Vector3 FarLip = new Vector3(2.255f, 4.75f, 5.41f);

        public struct Placement
        {
            public Vector3 nearEnd;
            public Vector3 farEnd;
            public bool walkOpen;
        }

        Transform building;
        Transform plank;
        PathNode nearNode;
        PathNode farNode;

        public static Placement Evaluate(float yaw, Vector3 nearLip, Vector3 farLip)
        {
            float delta = Mathf.Abs(Mathf.DeltaAngle(yaw, JoinYaw));
            float open = 1f - Mathf.InverseLerp(FullDegrees, GoneDegrees, delta);
            Vector3 span = farLip - nearLip;
            float full = span.magnitude;
            Vector3 dir = full < 0.0001f ? Vector3.right : span / full;
            float length = Mathf.Lerp(Mathf.Min(StubLength, full), full, open);
            return new Placement
            {
                nearEnd = nearLip,
                farEnd = nearLip + dir * length,
                walkOpen = delta <= FullDegrees
            };
        }

        public static void Ensure(Transform buildingRoot)
        {
            if (buildingRoot == null || buildingRoot.Find("StairSpan") != null)
                return;
            var host = new GameObject("StairSpan");
            host.transform.SetParent(buildingRoot, false);
            var span = host.AddComponent<StairSpan>();
            span.building = buildingRoot;
            span.Build();
        }

        void Build()
        {
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (prefab == null)
                return;
            var model = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, transform);
            plank = model.transform;
            plank.name = "connect1";
            plank.localRotation = Quaternion.identity;
            plank.localPosition = Vector3.zero;
            Mesh imported = null;
            var subAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(ModelPath);
            for (int i = 0; i < subAssets.Length; i++)
            {
                if (subAssets[i] is Mesh source)
                    imported = source;
            }
            var filter = model.GetComponentInChildren<MeshFilter>(true);
            if (filter != null && imported != null)
                filter.sharedMesh = imported;
            var shader = Shader.Find("Unlit/Texture");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null && tex != null)
            {
                var mat = new Material(shader) { mainTexture = tex };
                var renderers = model.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].sharedMaterial = mat;
                    renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
#endif
            var nodes = building.GetComponentsInChildren<PathNode>(true);
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].name == "Stair")
                    nearNode = nodes[i];
                else if (nodes[i].name == "SpanFar")
                    farNode = nodes[i];
            }
        }

        void LateUpdate()
        {
            if (building == null)
                return;
            Placement place = Evaluate(building.eulerAngles.y, NearLip, FarLip);
            if (nearNode != null && farNode != null)
            {
                if (place.walkOpen)
                    nearNode.Link(farNode);
                else
                    nearNode.Unlink(farNode);
            }

            if (plank == null)
                return;
            Vector3 span = place.farEnd - place.nearEnd;
            float length = Mathf.Max(span.magnitude, 0.02f);
            Vector3 dir = span.sqrMagnitude < 0.0001f ? Vector3.right : span.normalized;
            transform.localPosition = (place.nearEnd + place.farEnd) * 0.5f;
            transform.localRotation = Quaternion.FromToRotation(Vector3.right, dir);
            plank.localRotation = Quaternion.identity;
            plank.localPosition = Vector3.zero;
            plank.localScale = new Vector3(length / 0.02f, Thickness / 0.02f, 1.2f / 0.02f);
        }
    }
}
