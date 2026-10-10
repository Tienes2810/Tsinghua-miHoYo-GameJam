using UnityEngine;

namespace PerspectivePuzzle.Puzzle
{
    public class BuildingRotator : MonoBehaviour
    {
        [SerializeField] float dragDegrees = 14f;
        [SerializeField] float keyDegreesPerSecond = 45f;
        [SerializeField] bool pointerDrives;

        public bool CanRotate = true;

        public bool PointerDrives
        {
            get => pointerDrives;
            set => pointerDrives = value;
        }

        public float CurrentYaw => transform.eulerAngles.y;

        void Update()
        {
            if (!CanRotate)
                return;

            float yaw = 0f;
            if (!pointerDrives && Input.GetMouseButton(1))
                yaw += Input.GetAxis("Mouse X") * dragDegrees;
            if (Input.GetKey(KeyCode.E))
                yaw += keyDegreesPerSecond * Time.deltaTime;
            if (Input.GetKey(KeyCode.Q))
                yaw -= keyDegreesPerSecond * Time.deltaTime;

            if (Mathf.Abs(yaw) > 0.001f)
                AddYaw(yaw);
        }

        public void AddYaw(float degrees)
        {
            if (!CanRotate || Mathf.Abs(degrees) < 0.0001f)
                return;
            transform.Rotate(0f, degrees, 0f, Space.World);
        }

        public void SnapTo(float yaw, float seconds)
        {
            StopAllCoroutines();
            StartCoroutine(SnapRoutine(yaw, seconds));
        }

        System.Collections.IEnumerator SnapRoutine(float yaw, float seconds)
        {
            float start = transform.eulerAngles.y;
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / seconds));
                float current = Mathf.LerpAngle(start, yaw, t);
                Vector3 euler = transform.eulerAngles;
                euler.y = current;
                transform.eulerAngles = euler;
                yield return null;
            }

            Vector3 done = transform.eulerAngles;
            done.y = yaw;
            transform.eulerAngles = done;
        }
    }
}
