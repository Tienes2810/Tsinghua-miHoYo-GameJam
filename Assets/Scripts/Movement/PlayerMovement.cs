using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace PerspectivePuzzle.Movement
{
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] PathNode currentNode;
        [SerializeField] float secondsPerUnit = 0.4f;
        [SerializeField] bool faceTravel = true;
        [SerializeField] Animator animator;
        [SerializeField] string movingBool = "isMoving";

        public bool MovementLocked;

        readonly List<PathNode> path = new();
        Tween moveTween;

        public PathNode CurrentNode => currentNode;
        public bool IsMoving => moveTween != null && moveTween.IsActive() && moveTween.IsPlaying();
        public event Action<PathNode> Arrived;

        void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        void Start()
        {
            if (currentNode != null)
                WarpTo(currentNode);
        }

        public void PlaceAt(PathNode node)
        {
            currentNode = node;
            WarpTo(node);
        }

        public void SetPace(float seconds)
        {
            secondsPerUnit = Mathf.Max(0.01f, seconds);
        }

        public void Halt()
        {
            moveTween?.Kill();
            moveTween = null;
            SetMoving(false);
        }

        public void WarpTo(PathNode node)
        {
            moveTween?.Kill();
            currentNode = node;
            if (node != null)
                transform.localPosition = LocalPoint(node);
            SetMoving(false);
        }

        public bool TryMoveTo(PathNode destination)
        {
            if (MovementLocked || IsMoving || destination == null || currentNode == null)
                return false;
            if (!NodePath.TryFind(currentNode, destination, path))
                return false;
            if (path.Count <= 1)
            {
                Arrived?.Invoke(currentNode);
                return true;
            }

            Sequence sequence = DOTween.Sequence().SetLink(gameObject);
            for (int i = 1; i < path.Count; i++)
            {
                PathNode from = path[i - 1];
                PathNode node = path[i];
                Vector3 target = LocalPoint(node);
                if (from.seamPeer == node || node.seamPeer == from)
                {
                    AppendSeamHop(sequence, from, node);
                    continue;
                }

                float distance = Vector3.Distance(transform.parent == null
                    ? from.transform.position
                    : LocalPoint(from), target);
                float duration = Mathf.Max(0.08f, distance * secondsPerUnit);
                if (faceTravel)
                    sequence.Append(FaceLocal(from.transform.position, node.transform.position));
                sequence.Append(transform.DOLocalMove(target, duration).SetEase(Ease.InOutSine));
                sequence.AppendCallback(() => currentNode = node);
            }

            sequence.OnStart(() => SetMoving(true));
            sequence.OnComplete(() =>
            {
                SetMoving(false);
                Arrived?.Invoke(currentNode);
            });
            sequence.OnKill(() => SetMoving(false));
            moveTween = sequence;
            return true;
        }

        Tween FaceLocal(Vector3 worldFrom, Vector3 worldTo)
        {
            Vector3 flat = worldTo - worldFrom;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0004f)
                return DOTween.Sequence().SetLink(gameObject);

            Quaternion worldLook = Quaternion.LookRotation(flat, Vector3.up);
            Quaternion localLook = transform.parent == null
                ? worldLook
                : Quaternion.Inverse(transform.parent.rotation) * worldLook;
            return transform.DOLocalRotateQuaternion(localLook, 0.16f).SetEase(Ease.OutSine);
        }

        void AppendSeamHop(Sequence sequence, PathNode from, PathNode node)
        {
            // "near" owns the offset: near.position + offset projects onto the tile next to "far".
            bool toNear = node.seamOffset.sqrMagnitude > 0.0001f;
            PathNode near = toNear ? node : from;
            PathNode far = toNear ? from : node;
            Vector3 ghost = near.transform.TransformPoint(near.seamOffset);
            Vector3 farPos = far.transform.position;
            Vector3 nearPos = near.transform.position;
            Vector3 edgeFar = (farPos + ghost) * 0.5f;
            Vector3 edgeNear = nearPos + (farPos - ghost) * 0.5f;

            Vector3 startPos = toNear ? farPos : nearPos;
            Vector3 firstEdge = toNear ? edgeFar : edgeNear;
            Vector3 secondEdge = toNear ? edgeNear : edgeFar;
            Vector3 endPos = toNear ? nearPos : farPos;
            Vector3 heading = toNear ? ghost - farPos : farPos - ghost;
            float half = Mathf.Max(0.06f, Vector3.Distance(startPos, firstEdge) * secondsPerUnit);

            if (faceTravel)
                sequence.Append(FaceLocal(Vector3.zero, heading));
            sequence.Append(transform.DOLocalMove(LocalOf(firstEdge), half).SetEase(Ease.InSine));
            sequence.AppendCallback(() => transform.localPosition = LocalOf(secondEdge));
            sequence.Append(transform.DOLocalMove(LocalOf(endPos), half).SetEase(Ease.OutSine));
            sequence.AppendCallback(() => currentNode = node);
        }

        Vector3 LocalOf(Vector3 world)
        {
            return transform.parent == null ? world : transform.parent.InverseTransformPoint(world);
        }

        Vector3 LocalPoint(PathNode node)
        {
            if (transform.parent == null)
                return node.transform.position;
            return transform.parent.InverseTransformPoint(node.transform.position);
        }

        void SetMoving(bool moving)
        {
            if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrEmpty(movingBool))
                return;
            animator.SetBool(movingBool, moving);
        }
    }
}
