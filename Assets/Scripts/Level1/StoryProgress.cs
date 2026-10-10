using System.Collections.Generic;
using UnityEngine;

namespace PerspectivePuzzle.Level1
{
    public enum SpeakerPhase
    {
        Hidden,
        Ready,
        Held,
        Placed
    }

    /// <summary>
    /// Round rules that do not need the map mesh.
    /// </summary>
    public sealed class StoryProgress
    {
        // Yaw where Cube.047's lip and the balcony tongue share one screen edge.
        public const float CorridorYaw = 315f;
        public const float StairYaw = 200f;
        public const float Tolerance = 12f;

        public static readonly string[] RoofPoints = { "E", "F", "I", "L", "G", "M" };

        public bool CorridorUnlocked;
        public bool StairUnlocked;
        public SpeakerPhase Speaker = SpeakerPhase.Hidden;
        public string Point = "A";
        public int RoofReached;
        public readonly HashSet<string> HeardClues = new HashSet<string>();

        public string NextRoof => RoofReached >= 0 && RoofReached < RoofPoints.Length ? RoofPoints[RoofReached] : null;

        /// <summary>
        /// Each roof point has its own yaw. Neighbors stay more than two tolerances apart,
        /// and none sits inside the corridor tolerance.
        /// </summary>
        public static float YawForRoof(string point)
        {
            if (point == "E")
                return StairYaw;
            if (point == "F")
                return 250f;
            if (point == "I")
                return 280f;
            if (point == "L")
                return 40f;
            if (point == "G")
                return 80f;
            if (point == "M")
                return 155f;
            return StairYaw;
        }

        public bool Within(float yaw, float target)
        {
            return Mathf.Abs(Mathf.DeltaAngle(yaw, target)) <= Tolerance;
        }

        public bool CorridorOpen(float yaw)
        {
            return Within(yaw, CorridorYaw);
        }

        public bool StairOpen(float yaw)
        {
            return Within(yaw, StairYaw);
        }

        public bool RoofPointOpen(string point, float yaw)
        {
            return point == NextRoof && Within(yaw, YawForRoof(point));
        }

        public bool ReachRoof(string point)
        {
            if (point != NextRoof)
                return false;
            RoofReached++;
            Point = point;
            return true;
        }

        public void Observe(float yaw)
        {
            if (CorridorOpen(yaw))
                CorridorUnlocked = true;
            if (StairOpen(yaw))
                StairUnlocked = true;
        }

        public bool TryHearClue(string id)
        {
            if (string.IsNullOrEmpty(id) || HeardClues.Contains(id))
                return false;
            HeardClues.Add(id);
            return true;
        }

        public bool TryPickup()
        {
            if (Speaker != SpeakerPhase.Ready)
                return false;
            Speaker = SpeakerPhase.Held;
            return true;
        }

        public bool TryPlace()
        {
            if (Speaker != SpeakerPhase.Held)
                return false;
            Speaker = SpeakerPhase.Placed;
            return true;
        }

        public void DogFail()
        {
            Point = "A";
        }
    }
}
