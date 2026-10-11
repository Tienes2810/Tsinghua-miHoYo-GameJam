using NUnit.Framework;
using PerspectivePuzzle.Level2;
using UnityEngine;

namespace PerspectivePuzzle.Tests
{
    public class Level2RulesTests
    {
        [Test]
        public void SeamTileProjectsOntoTileBesideVillaRoof()
        {
            // The tower tile is 2 view-axes nearer to the camera than the tile beside the villa roof.
            Vector3 beside = Level2Rules.SeamFar + Vector3.left;
            Vector2 a = Level2Rules.Project(Level2Rules.SeamNear);
            Vector2 b = Level2Rules.Project(beside);
            Assert.Less(Vector2.Distance(a, b), 1e-3f);
            Assert.AreEqual(beside, Level2Rules.SeamNear + Level2Rules.SeamOffset);
        }

        [Test]
        public void SeamTileIsReallyFarAwayInSpace()
        {
            Vector3 beside = Level2Rules.SeamFar + Vector3.left;
            Assert.Greater(Vector3.Distance(Level2Rules.SeamNear, beside), 3f);
        }

        [Test]
        public void ViewAxisMatchesIsometricCamera()
        {
            Vector3 forward = Quaternion.Euler(Level2Rules.CameraPitch, Level2Rules.CameraYaw, 0f) * Vector3.forward;
            Assert.Greater(Vector3.Dot(forward, Level2Rules.ViewAxis.normalized), 0.9999f);
        }

        [TestCase(0f, 0f)]
        [TestCase(1.4f, 0f)]
        [TestCase(1.6f, 3f)]
        [TestCase(3f, 3f)]
        public void LiftSnapsToFloors(float h, float expected)
        {
            Assert.AreEqual(expected, Level2Rules.SnapLift(h));
        }

        [TestCase(44f, 0f)]
        [TestCase(46f, 90f)]
        [TestCase(-100f, -90f)]
        [TestCase(181f, 180f)]
        public void QuarterSnap(float deg, float expected)
        {
            Assert.AreEqual(expected, Level2Rules.SnapQuarter(deg));
        }

        [TestCase(0f, true)]
        [TestCase(180f, true)]
        [TestCase(-180f, true)]
        [TestCase(360f, true)]
        [TestCase(90f, false)]
        [TestCase(-90f, false)]
        [TestCase(270f, false)]
        public void BridgeOrientation(float deg, bool northSouth)
        {
            Assert.AreEqual(northSouth, Level2Rules.BridgeNorthSouth(deg));
        }

        [TestCase(0f, true)]
        [TestCase(360f, true)]
        [TestCase(-0.2f, true)]
        [TestCase(90f, false)]
        [TestCase(180f, false)]
        [TestCase(270f, false)]
        public void SeamOnlyAlignsAtOneYaw(float yaw, bool aligned)
        {
            Assert.AreEqual(aligned, Level2Rules.SeamAligned(yaw));
        }

        [Test]
        public void PuzzleStartsUnsolved()
        {
            Assert.IsFalse(Level2Rules.SeamAligned(Level2Rules.StartWorldYaw), "seam must start broken");
            Assert.IsFalse(Level2Rules.BridgeNorthSouth(Level2Rules.StartBridgeYaw), "bridge must start blocked");
        }

        [Test]
        public void DraggingRightTurnsCityNegativeYaw()
        {
            Assert.AreEqual(-1, Level2Rules.DragYawSign);
            Assert.Greater(Level2Rules.DegreesPerPixel, 0f);
        }
    }
}
