using PerspectivePuzzle.Puzzle;
using UnityEngine;

namespace PerspectivePuzzle.Level1
{
    public class Level1Input : MonoBehaviour
    {
        public Camera view;
        public Level1Director director;
        public BuildingRotator rotator;
        public float degreesPerPixel = DragYaw.DegreesPerPixel;

        bool held;
        bool dragged;
        Vector3 down;
        float lastX;

        void Update()
        {
            if (director != null && director.CapturePointer)
            {
                held = false;
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                held = true;
                dragged = false;
                down = Input.mousePosition;
                lastX = down.x;
            }

            if (!held)
                return;

            if (Input.GetMouseButton(0))
            {
                Vector3 now = Input.mousePosition;
                if ((now - down).sqrMagnitude > 36f)
                    dragged = true;
                float dx = now.x - lastX;
                lastX = now.x;
                if (dragged && director != null && director.AllowRotate && rotator != null)
                    rotator.AddYaw(dx * degreesPerPixel * Mathf.Sign(DragYaw.DegreesForDrag(1f)));
            }

            if (!Input.GetMouseButtonUp(0))
                return;

            held = false;
            if (director == null)
                return;
            if (dragged)
                director.OnRotateReleased();
            else if (view != null)
                director.OnClick(view.ScreenPointToRay(Input.mousePosition));
        }
    }
}
