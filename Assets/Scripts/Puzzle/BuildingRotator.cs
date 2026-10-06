using UnityEngine;

namespace PerspectivePuzzle.Puzzle
{
    public class BuildingRotator : MonoBehaviour
    {
        [SerializeField] float dragDegrees = 14f;
        [SerializeField] float keyDegreesPerSecond = 45f;

        public float CurrentYaw => transform.eulerAngles.y;

        void Update()
        {
            float yaw = 0f;
            if (Input.GetMouseButton(1))
                yaw += Input.GetAxis("Mouse X") * dragDegrees;
            if (Input.GetKey(KeyCode.E))
                yaw += keyDegreesPerSecond * Time.deltaTime;
            if (Input.GetKey(KeyCode.Q))
                yaw -= keyDegreesPerSecond * Time.deltaTime;

            if (Mathf.Abs(yaw) > 0.001f)
                transform.Rotate(0f, yaw, 0f, Space.World);
        }
    }
}
