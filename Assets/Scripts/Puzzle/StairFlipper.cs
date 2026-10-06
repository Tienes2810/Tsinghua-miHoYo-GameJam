using DG.Tweening;
using PerspectivePuzzle.Movement;
using UnityEngine;

namespace PerspectivePuzzle.Puzzle
{
    public class StairFlipper : MonoBehaviour
    {
        [SerializeField] bool flipped;
        [SerializeField] bool passageBlocked;
        [SerializeField] bool interactionEnabled;
        [SerializeField] float duration = 0.65f;
        [SerializeField] Vector3 localAxis = Vector3.right;
        [SerializeField] GameObject blockedFace;
        [SerializeField] PathNode lowerNode;
        [SerializeField] PathNode upperNode;
        [SerializeField] PathNode balconyNode;

        Quaternion closedRotation;
        Tween spin;

        public bool IsFlipped => flipped;
        public bool InteractionEnabled => interactionEnabled;
        public bool IsBusy => spin != null && spin.IsActive() && spin.IsPlaying();

        void Awake()
        {
            closedRotation = transform.localRotation;
            ApplyLinks();
        }

        public void SetInteractionEnabled(bool enabled)
        {
            interactionEnabled = enabled;
        }

        public void SetBlocked(bool blocked)
        {
            passageBlocked = blocked;
            ApplyLinks();
        }

        public void Toggle()
        {
            if (!interactionEnabled || IsBusy)
                return;

            flipped = !flipped;
            Quaternion target = flipped
                ? closedRotation * Quaternion.AngleAxis(180f, localAxis.normalized)
                : closedRotation;

            spin?.Kill();
            spin = transform
                .DOLocalRotateQuaternion(target, duration)
                .SetEase(Ease.InOutSine)
                .SetLink(gameObject);
            ApplyLinks();
        }

        void ApplyLinks()
        {
            if (lowerNode != null && upperNode != null)
            {
                bool stairOpen = !passageBlocked || flipped;
                if (stairOpen)
                    lowerNode.Link(upperNode);
                else
                    lowerNode.Unlink(upperNode);
            }

            if (upperNode != null && balconyNode != null)
            {
                if (flipped)
                    upperNode.Link(balconyNode);
                else
                    upperNode.Unlink(balconyNode);
            }

            if (blockedFace != null)
                blockedFace.SetActive(passageBlocked && !flipped);
        }
    }
}
