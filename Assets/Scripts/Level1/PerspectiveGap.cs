using UnityEngine;

namespace PerspectivePuzzle.Level1
{
    /// <summary>
    /// Local offset that hides a gap along the fixed iso view at one building yaw.
    /// Orthographic pixels ignore camera-forward, so the two ledges read as one block only then.
    /// </summary>
    public static class PerspectiveGap
    {
        public static void Split(float alignYaw, float depth, float length, out Vector3 depthLocal, out Vector3 walkLocal)
        {
            Quaternion camera = Quaternion.Euler(IsoRig.DefaultPitch, IsoRig.DefaultYaw, 0f);
            Quaternion toLocal = Quaternion.Inverse(Quaternion.Euler(0f, alignYaw, 0f));
            depthLocal = toLocal * (camera * Vector3.forward * depth);
            walkLocal = toLocal * (camera * Vector3.right * length);
        }

        public static float ScreenGap(float yaw, Vector3 localOffset)
        {
            Project(yaw, localOffset, out float sx, out float sy);
            return Mathf.Sqrt(sx * sx + sy * sy);
        }

        public static void Project(float yaw, Vector3 localPoint, out float sx, out float sy)
        {
            Quaternion camera = Quaternion.Euler(IsoRig.DefaultPitch, IsoRig.DefaultYaw, 0f);
            Vector3 world = Quaternion.Euler(0f, yaw, 0f) * localPoint;
            sx = Vector3.Dot(world, camera * Vector3.right);
            sy = Vector3.Dot(world, camera * Vector3.up);
        }
    }
}
