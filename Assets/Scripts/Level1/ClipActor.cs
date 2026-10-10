using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PerspectivePuzzle.Level1
{
    public static class CharacterClips
    {
        public const string IdleModel = "Assets/Art/Characters/StandingIdle.fbx";
        public const string WalkModel = "Assets/Art/Characters/Walking.fbx";
        public const string CarryModel = "Assets/Art/Characters/Carry.fbx";
        public const string FallModel = "Assets/Art/Characters/Falling.fbx";
        public const string HouseModel = "Assets/Art/Models/House/GỜ ÊM GÊM.fbx";

        public static AnimationClip FirstClip(string path)
        {
#if UNITY_EDITOR
            Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            AnimationClip best = null;
            for (int i = 0; i < assets.Length; i++)
            {
                AnimationClip clip = assets[i] as AnimationClip;
                if (clip == null || clip.name.StartsWith("__preview__"))
                    continue;
                if (best == null || clip.length > best.length)
                    best = clip;
            }

            return best;
#else
            return null;
#endif
        }
    }

    /// <summary>
    /// Plays the imported takes: idle while stopped, walk or carry while moving, fall on the dog.
    /// </summary>
    public class ClipActor : MonoBehaviour
    {
        public AnimationClip idleClip;
        public AnimationClip walkClip;
        public AnimationClip carryClip;
        public AnimationClip fallClip;
        public PerspectivePuzzle.Movement.PlayerMovement movement;
        public bool Carrying;

        Animator animator;
        PlayableGraph graph;
        AnimationClip playing;
        float fallUntil;

        void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
                animator = gameObject.AddComponent<Animator>();
            ApplyClips();
        }

        void OnDestroy()
        {
            if (graph.IsValid())
                graph.Destroy();
        }

        public void ApplyClips()
        {
            AnimationClip idle = CharacterClips.FirstClip(CharacterClips.IdleModel);
            AnimationClip walk = CharacterClips.FirstClip(CharacterClips.WalkModel);
            AnimationClip carry = CharacterClips.FirstClip(CharacterClips.CarryModel);
            AnimationClip fall = CharacterClips.FirstClip(CharacterClips.FallModel);
            if (idle != null)
                idleClip = idle;
            if (walk != null)
                walkClip = walk;
            if (carry != null)
                carryClip = carry;
            if (fall != null)
                fallClip = fall;
            EnsureBody();
        }

        void LateUpdate()
        {
            if (movement == null)
                return;
            if (Time.time < fallUntil)
                return;

            AnimationClip clip = movement.IsMoving
                ? (Carrying && carryClip != null ? carryClip : walkClip)
                : idleClip;
            if (clip == null)
            {
                if (!movement.IsMoving && playing == walkClip)
                    Stop();
                return;
            }

            Play(clip);
        }

        public void PlayFall()
        {
            if (fallClip == null)
                return;
            Play(fallClip);
            fallUntil = Time.time + Mathf.Max(0.8f, fallClip.length);
        }

        public void Play(AnimationClip clip)
        {
            if (animator == null || clip == null)
                return;
            if (playing == clip && graph.IsValid() && graph.IsPlaying())
                return;
            if (graph.IsValid())
                graph.Destroy();

            graph = PlayableGraph.Create("ClipActor");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "out", animator);
            AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clip);
            output.SetSourcePlayable(playable);
            graph.Play();
            playing = clip;
        }

        public void Stop()
        {
            if (graph.IsValid())
                graph.Stop();
            playing = null;
        }

        void EnsureBody()
        {
#if UNITY_EDITOR
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(CharacterClips.IdleModel);
            if (prefab == null)
                return;

            Transform body = transform.Find("Body");
            if (body != null && UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(body.gameObject) == prefab)
            {
                AttachHand(body);
                return;
            }

            float targetHeight = 1.7f;
            if (body != null)
            {
                Bounds previous = RendererBounds(body.gameObject);
                if (previous.size.y > 0.05f)
                    targetHeight = previous.size.y;
                DestroyImmediate(body.gameObject);
            }

            GameObject next = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, transform);
            next.name = "Body";
            next.transform.localPosition = Vector3.zero;
            next.transform.localRotation = Quaternion.identity;
            Bounds now = RendererBounds(next);
            if (now.size.y > 0.01f)
                next.transform.localScale = Vector3.one * (targetHeight / now.size.y);
            foreach (Collider col in next.GetComponentsInChildren<Collider>())
                DestroyImmediate(col);
            AttachHand(next.transform);
            Animator bodyAnimator = next.GetComponentInChildren<Animator>();
            if (bodyAnimator != null)
                animator = bodyAnimator;
#endif
        }

#if UNITY_EDITOR
        void AttachHand(Transform body)
        {
            Transform hand = transform.Find("Hand");
            if (hand == null)
                return;
            Transform bone = FindBone(body);
            if (bone == null)
                return;
            hand.SetParent(bone, false);
            hand.localPosition = Vector3.zero;
            hand.localRotation = Quaternion.identity;
        }

        static Transform FindBone(Transform root)
        {
            Transform fuzzy = null;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                string name = all[i].name.Replace(" ", "");
                if (name.IndexOf("RightHand", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Hand_R", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("righthand", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return all[i];
                if (fuzzy == null && name.EndsWith("Hand", System.StringComparison.OrdinalIgnoreCase))
                    fuzzy = all[i];
            }

            return fuzzy;
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
#endif
    }
}
