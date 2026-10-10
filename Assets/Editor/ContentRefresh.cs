using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PerspectivePuzzle.Level1
{
    public static class ContentRefresh
    {
        const string ScenePath = "Assets/Scenes/Level1.unity";

        [MenuItem("Level 1/Apply new house and character")]
        public static void ApplyMenu()
        {
            Debug.Log(Apply());
        }

        public static string Apply()
        {
            GameObject housePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterClips.HouseModel);
            if (housePrefab == null)
                return "missing " + CharacterClips.HouseModel;

            Scene scene = EditorSceneManager.OpenScene(ScenePath);
            GameObject oldHouse = GameObject.Find("House");
            if (oldHouse == null)
                return "Level 1 has no House";

            Bounds oldBounds = RendererBounds(oldHouse);
            GameObject neu = (GameObject)PrefabUtility.InstantiatePrefab(housePrefab, oldHouse.transform.parent);
            neu.name = "House";
            neu.transform.localRotation = oldHouse.transform.localRotation;
            neu.transform.localScale = oldHouse.transform.localScale;
            neu.transform.localPosition = oldHouse.transform.localPosition;
            Bounds fresh = RendererBounds(neu);
            if (fresh.size.sqrMagnitude < 0.01f)
            {
                Object.DestroyImmediate(neu);
                return "new house imported without renderers";
            }

            float ratio = oldBounds.size.y > 0.01f ? fresh.size.y / oldBounds.size.y : 1f;
            if (ratio < 0.85f || ratio > 1.15f)
            {
                neu.transform.localScale *= oldBounds.size.y / fresh.size.y;
                fresh = RendererBounds(neu);
            }

            neu.transform.position += oldBounds.center - fresh.center;
            Object.DestroyImmediate(oldHouse);

            string clips = "no actor";
            ClipActor actor = Object.FindAnyObjectByType<ClipActor>();
            if (actor != null)
            {
                actor.ApplyClips();
                clips = "clips idle=" + (actor.idleClip != null) + " walk=" + (actor.walkClip != null)
                    + " carry=" + (actor.carryClip != null) + " fall=" + (actor.fallClip != null);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "house ratio " + ratio.ToString("0.00") + " | " + clips;
        }

        static Bounds RendererBounds(GameObject go)
        {
            Bounds bounds = new Bounds(go.transform.position, Vector3.zero);
            bool any = false;
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!any)
                {
                    bounds = renderers[i].bounds;
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            return bounds;
        }
    }
}
