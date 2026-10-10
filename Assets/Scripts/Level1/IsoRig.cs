using UnityEngine;

namespace PerspectivePuzzle.Level1
{
    /// <summary>
    /// Fixed isometric camera. The house yaws underneath it.
    /// </summary>
    public class IsoRig : MonoBehaviour
    {
        public const float DefaultYaw = 45f;
        // Fixed isometric. The house turns underneath, the same way as the reference level.
        public const float DefaultPitch = 32f;

        public float yaw = DefaultYaw;
        public float pitch = DefaultPitch;
        public float distance = 18f;
        public float ortho = 8f;
        public Vector3 lookAt;

        Vector3 lookTarget;
        float orthoTarget;
        float distanceTarget;
        Vector3 lookVelocity;
        float orthoVelocity;
        float distanceVelocity;

        void Awake()
        {
            pitch = DefaultPitch;
            lookTarget = lookAt;
            orthoTarget = ortho;
            distanceTarget = distance;
            Apply();
        }

        public void Frame(Vector3 focus, float size, float away)
        {
            lookAt = focus;
            lookTarget = focus;
            ortho = size;
            orthoTarget = size;
            distance = away;
            distanceTarget = away;
            Apply();
        }

        public void MoveFocus(Vector3 focus, float size)
        {
            lookTarget = focus;
            orthoTarget = size;
        }

        void LateUpdate()
        {
            lookAt = Vector3.SmoothDamp(lookAt, lookTarget, ref lookVelocity, 0.7f);
            ortho = Mathf.SmoothDamp(ortho, orthoTarget, ref orthoVelocity, 0.7f);
            distance = Mathf.SmoothDamp(distance, distanceTarget, ref distanceVelocity, 0.7f);
            Apply();
        }

        void Apply()
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.rotation = rotation;
            transform.position = lookAt - rotation * Vector3.forward * distance;
            Camera view = GetComponent<Camera>();
            if (view == null)
                return;
            view.orthographic = true;
            view.orthographicSize = ortho;
        }
    }
}
