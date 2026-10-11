using PerspectivePuzzle.Movement;
using UnityEngine;

namespace PerspectivePuzzle.Level2
{
    public enum PickGroup { World, Lift, Bridge }

    /// <summary>Tag on every clickable collider in Chapter 2.</summary>
    public class L2Pick : MonoBehaviour
    {
        public PickGroup group;
        public PathNode node;
    }
}
