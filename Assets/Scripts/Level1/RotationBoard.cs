using System;
using System.Collections.Generic;
using PerspectivePuzzle.Movement;
using UnityEngine;

namespace PerspectivePuzzle.Level1
{
    [Serializable]
    public class YawLink
    {
        public PathNode from;
        public PathNode to;
        public float yaw = 90f;
        public float tolerance = 12f;
    }

    /// <summary>
    /// Opens a path only while the building yaw matches that link.
    /// The mesh can be replaced later; the link stays on the nodes.
    /// </summary>
    public class RotationBoard : MonoBehaviour
    {
        [SerializeField] Transform buildingRoot;
        [SerializeField] PlayerMovement player;
        [SerializeField] List<YawLink> links = new();

        public bool AnyOpen { get; private set; }
        public event Action<bool> AlignmentChanged;

        public IReadOnlyList<YawLink> Links => links;
        public float CurrentYaw => buildingRoot != null ? buildingRoot.eulerAngles.y : 0f;

        public void Bind(Transform root, PlayerMovement mover)
        {
            buildingRoot = root;
            player = mover;
        }

        public void SetLinks(List<YawLink> value)
        {
            links = value ?? new List<YawLink>();
            Refresh(true);
        }

        public void KeepPair(PathNode from, PathNode to)
        {
            for (int i = links.Count - 1; i >= 0; i--)
            {
                YawLink link = links[i];
                bool keep = link.from == from && link.to == to;
                if (!keep && link.from != null && link.to != null)
                    link.from.Unlink(link.to);
                if (!keep)
                    links.RemoveAt(i);
            }

            Refresh(true);
        }

        public void MatchCorridor(PathNode from, PathNode to, float yaw)
        {
            for (int i = 0; i < links.Count; i++)
            {
                if (links[i].from == from && links[i].to == to)
                    links[i].yaw = yaw;
            }

            Refresh(true);
        }

        public void ReleasePair(PathNode from, PathNode to)
        {
            for (int i = links.Count - 1; i >= 0; i--)
            {
                if (links[i].from == from && links[i].to == to)
                    links.RemoveAt(i);
            }
        }

        void Update()
        {
            Refresh(false);
        }

        public void Refresh(bool force)
        {
            if (!force && player != null && player.IsMoving)
                return;

            bool any = false;
            for (int i = 0; i < links.Count; i++)
            {
                YawLink link = links[i];
                if (link.from == null || link.to == null)
                    continue;

                bool open = Mathf.Abs(Mathf.DeltaAngle(CurrentYaw, link.yaw)) <= link.tolerance;
                if (open)
                    link.from.Link(link.to);
                else
                    link.from.Unlink(link.to);
                any |= open;
            }

            if (any == AnyOpen)
                return;

            AnyOpen = any;
            AlignmentChanged?.Invoke(any);
        }

        public bool TryNearest(float catchDegrees, out float yaw)
        {
            yaw = CurrentYaw;
            float best = catchDegrees;
            bool found = false;
            for (int i = 0; i < links.Count; i++)
            {
                float delta = Mathf.Abs(Mathf.DeltaAngle(CurrentYaw, links[i].yaw));
                if (delta > best)
                    continue;
                best = delta;
                yaw = links[i].yaw;
                found = true;
            }

            return found;
        }
    }
}
