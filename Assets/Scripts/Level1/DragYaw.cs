using UnityEngine;

namespace PerspectivePuzzle.Level1
{
    /// <summary>
    /// Maps a horizontal drag to building yaw.
    /// A rightward drag applies a negative yaw so the building follows the hand.
    /// </summary>
    public static class DragYaw
    {
        public const float DegreesPerPixel = 0.22f;

        public static float DegreesForDrag(float dragPixels)
        {
            return -dragPixels * DegreesPerPixel;
        }

        public static bool MovesTowardScreenRight(float dragPixels, Vector3 point)
        {
            if (dragPixels <= 0f || point.sqrMagnitude < 0.0001f)
                return false;

            Quaternion camera = Quaternion.Euler(IsoRig.DefaultPitch, IsoRig.DefaultYaw, 0f);
            Vector3 right = camera * Vector3.right;
            Vector3 rotated = Quaternion.Euler(0f, DegreesForDrag(dragPixels), 0f) * point;
            return Vector3.Dot(right, rotated - point) > 0f;
        }
    }
}
