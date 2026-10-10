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
        // Yaw where the CauThang1 landing and the tầng 2 room floor read as one edge on screen.
        public const float CorridorYaw = 315f;
        // Yaw where the cut face shows the whole CauThang2 flight and the dog has left it.
        public const float StairYaw = 250f;
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
        /// Measured in Play Mode at pitch 32: the point is unoccluded on screen at its yaw.
        /// E on the CauThang2 landing (240-310), F and I on the open east strip (40-220),
        /// L in the middle corridor (230-260), G on the west wing (170-280), M on the roof (210-60).
        /// </summary>
        public static float YawForRoof(string point)
        {
            if (point == "E")
                return StairYaw;
            if (point == "F")
                return 200f;
            if (point == "I")
                return 160f;
            if (point == "L")
                return 245f;
            if (point == "G")
                return 190f;
            if (point == "M")
                return 280f;
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
