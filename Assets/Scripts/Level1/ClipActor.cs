using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace PerspectivePuzzle.Level1
{
    public class ClipActor : MonoBehaviour
    {
        public AnimationClip walkClip;
        public AnimationClip fallClip;
        public PerspectivePuzzle.Movement.PlayerMovement movement;

        Animator animator;
        PlayableGraph graph;
        AnimationClip playing;

        void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
                animator = gameObject.AddComponent<Animator>();
        }

        void OnDestroy()
        {
            if (graph.IsValid())
                graph.Destroy();
        }

        void LateUpdate()
        {
            if (movement == null || walkClip == null)
                return;
            if (movement.IsMoving)
                Play(walkClip);
            else if (playing == walkClip)
                Stop();
        }

        public void PlayFall()
        {
            if (fallClip != null)
                Play(fallClip);
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
    }
}
