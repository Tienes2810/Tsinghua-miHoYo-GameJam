using System.Collections.Generic;
using UnityEngine;

namespace PerspectivePuzzle.Movement
{
    public class PathNode : MonoBehaviour
    {
        [SerializeField] string nodeId;
        [SerializeField] List<PathNode> neighbors = new();
        [SerializeField] bool walkable = true;

        public string NodeId => string.IsNullOrEmpty(nodeId) ? name : nodeId;
        public IReadOnlyList<PathNode> Neighbors => neighbors;
        public bool Walkable => walkable;

        public bool IsLinked(PathNode other)
        {
            return other != null && neighbors.Contains(other);
        }

        public void Link(PathNode other)
        {
            if (other == null || other == this)
                return;

            if (!neighbors.Contains(other))
                neighbors.Add(other);
            if (!other.neighbors.Contains(this))
                other.neighbors.Add(this);
        }

        public void Unlink(PathNode other)
        {
            if (other == null)
                return;

            neighbors.Remove(other);
            other.neighbors.Remove(this);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = walkable ? new Color(0.25f, 0.75f, 1f) : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.22f);
            Gizmos.color = new Color(1f, 1f, 1f, 0.85f);
            for (int i = 0; i < neighbors.Count; i++)
            {
                PathNode other = neighbors[i];
                if (other == null || other.GetInstanceID() < GetInstanceID())
                    continue;
                Gizmos.DrawLine(transform.position, other.transform.position);
            }
        }
    }
}
