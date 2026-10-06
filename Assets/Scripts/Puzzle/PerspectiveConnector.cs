using PerspectivePuzzle.Movement;
using UnityEngine;

namespace PerspectivePuzzle.Puzzle
{
    public class PerspectiveConnector : MonoBehaviour
    {
        [SerializeField] Transform buildingRoot;
        [Tooltip("Góc Y của BuildingRoot khi hai đoạn đường đứt nhìn thẳng hàng. Đo lại sau khi gắn model.")]
        [SerializeField] float alignAngleY = 90f;
        [SerializeField] float tolerance = 2f;
        [SerializeField] PathNode nodeA;
        [SerializeField] PathNode nodeB;

        public bool IsAligned { get; private set; }
        public float AlignAngle => alignAngleY;
        public float Tolerance => tolerance;

        void OnEnable()
        {
            Refresh();
        }

        void Update()
        {
            Refresh();
        }

        void Refresh()
        {
            if (buildingRoot == null || nodeA == null || nodeB == null)
                return;

            float delta = Mathf.DeltaAngle(buildingRoot.eulerAngles.y, alignAngleY);
            bool aligned = Mathf.Abs(delta) <= tolerance;
            IsAligned = aligned;
            if (aligned)
                nodeA.Link(nodeB);
            else
                nodeA.Unlink(nodeB);
        }
    }
}
