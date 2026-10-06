using PerspectivePuzzle.Core;
using PerspectivePuzzle.Movement;
using PerspectivePuzzle.Puzzle;
using UnityEngine;

namespace PerspectivePuzzle.Interaction
{
    public class PointerInput : MonoBehaviour
    {
        [SerializeField] Camera viewCamera;
        [SerializeField] PlayerMovement player;
        [SerializeField] GameFlowManager flow;

        void Update()
        {
            if (!Input.GetMouseButtonDown(0) || player == null)
                return;
            if (viewCamera == null)
                viewCamera = Camera.main;
            if (viewCamera == null)
                return;

            Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 300f))
                return;

            StairFlipper stair = hit.collider.GetComponentInParent<StairFlipper>();
            if (stair != null && stair.InteractionEnabled)
            {
                stair.Toggle();
                return;
            }

            QuestItem item = hit.collider.GetComponentInParent<QuestItem>();
            if (item != null && flow != null)
            {
                flow.TryCollect(item);
                return;
            }

            PathNode node = hit.collider.GetComponentInParent<PathNode>();
            if (node != null)
                player.TryMoveTo(node);
        }
    }
}
