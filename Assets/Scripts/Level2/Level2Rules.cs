using UnityEngine;

namespace PerspectivePuzzle.Level2
{
    /// <summary>
    /// Pure rules for Chapter 2 (no scene access) so they can be unit tested.
    /// Diorama space: tiles are 1 unit, a node sits on a tile's top face.
    /// The camera is a true isometric orthographic camera (pitch 35.26°, yaw 45°), so in diorama
    /// space at world-yaw 0 its forward axis is parallel to (1,-1,1).
    /// </summary>
    public static class Level2Rules
    {
        public const float CameraPitch = 35.26439f;
        public const float CameraYaw = 45f;
        public static readonly Vector3 ViewAxis = new Vector3(1f, -1f, 1f);

        public const float LiftLow = 0f;
        public const float LiftHigh = 3f;

        /// <summary>World yaw at which the far tower lines up with the villa roof.</summary>
        public const float SeamYaw = 0f;
        public const float StartWorldYaw = 90f;
        public const float StartBridgeYaw = 90f;

        // Graph anchors (diorama space).
        public static readonly Vector3 SeamFar = new Vector3(0f, 3f, 6f);   // last villa-roof tile
        public static readonly Vector3 SeamNear = new Vector3(-3f, 5f, 4f); // first tower tile

        /// <summary>Offset from the tower tile to the point that projects onto the tile beside the villa roof.</summary>
        public static Vector3 SeamOffset => 2f * ViewAxis;

        public static float SnapLift(float height)
        {
            return height >= (LiftLow + LiftHigh) * 0.5f ? LiftHigh : LiftLow;
        }

        public static float SnapQuarter(float degrees)
        {
            return Mathf.Round(degrees / 90f) * 90f;
        }

        public static float Wrap(float degrees)
        {
            float w = Mathf.Repeat(degrees, 360f);
            return w > 359.5f ? 0f : w;
        }

        /// <summary>Bridge arms run along local Z at 0° and 180°.</summary>
        public static bool BridgeNorthSouth(float degrees)
        {
            float w = Wrap(SnapQuarter(degrees));
            return Mathf.Approximately(w, 0f) || Mathf.Approximately(w, 180f);
        }

        public static bool SeamAligned(float worldYaw)
        {
            return Mathf.Abs(Mathf.DeltaAngle(worldYaw, SeamYaw)) < 0.5f;
        }

        public static bool LiftAt(float height, float level)
        {
            return Mathf.Abs(height - level) < 0.02f;
        }

        /// <summary>Screen-plane coordinates for an orthographic iso camera (diorama at yaw 0).</summary>
        public static Vector2 Project(Vector3 p)
        {
            Quaternion r = Quaternion.Euler(CameraPitch, CameraYaw, 0f);
            Vector3 right = r * Vector3.right;
            Vector3 up = r * Vector3.up;
            return new Vector2(Vector3.Dot(p, right), Vector3.Dot(p, up));
        }

        /// <summary>
        /// Which mechanism a drag should drive given what was pressed.
        /// </summary>
        public static int DragYawSign => -1;
        public const float DegreesPerPixel = 0.25f;
    }
}
