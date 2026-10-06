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
        [SerializeField] Animator animator;
        [SerializeField] string movingBool = "isMoving";

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
            if (IsMoving || destination == null || currentNode == null)
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
                PathNode node = path[i];
                Vector3 target = LocalPoint(node);
                float distance = Vector3.Distance(transform.parent == null
                    ? path[i - 1].transform.position
                    : LocalPoint(path[i - 1]), target);
                float duration = Mathf.Max(0.08f, distance * secondsPerUnit);
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

        Vector3 LocalPoint(PathNode node)
        {
            if (transform.parent == null)
                return node.transform.position;
            return transform.parent.InverseTransformPoint(node.transform.position);
        }

        void SetMoving(bool moving)
        {
            if (animator != null && !string.IsNullOrEmpty(movingBool))
                animator.SetBool(movingBool, moving);
        }
    }
}
