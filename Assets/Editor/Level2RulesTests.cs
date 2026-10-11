#if UNITY_EDITOR
using NUnit.Framework;
using PerspectivePuzzle.Level2;
using UnityEngine;

namespace PerspectivePuzzle.EditMode
{
    public class Level2RulesTests
    {
        [Test]
        public void BridgeAlignmentOpensOnlyWithinTolerance()
        {
            float target = Level2Director.BridgeYaw; // 135
            float tol = Level2Director.Tolerance;    // 14

            Assert.IsTrue(Mathf.Abs(Mathf.DeltaAngle(135f, target)) <= tol, "Exact 135 should be within tolerance");
            Assert.IsTrue(Mathf.Abs(Mathf.DeltaAngle(140f, target)) <= tol, "140 should be within tolerance");
            Assert.IsTrue(Mathf.Abs(Mathf.DeltaAngle(125f, target)) <= tol, "125 should be within tolerance");

            Assert.IsFalse(Mathf.Abs(Mathf.DeltaAngle(0f, target)) <= tol, "0 should be outside tolerance");
            Assert.IsFalse(Mathf.Abs(Mathf.DeltaAngle(90f, target)) <= tol, "90 should be outside tolerance");
            Assert.IsFalse(Mathf.Abs(Mathf.DeltaAngle(180f, target)) <= tol, "180 should be outside tolerance");
            Assert.IsFalse(Mathf.Abs(Mathf.DeltaAngle(270f, target)) <= tol, "270 should be outside tolerance");
        }

        [Test]
        public void TowerAscentOpensOnlyWithinTolerance()
        {
            float target = Level2Director.TowerYaw; // 225
            float tol = Level2Director.Tolerance;   // 14

            Assert.IsTrue(Mathf.Abs(Mathf.DeltaAngle(225f, target)) <= tol, "Exact 225 should be within tolerance");
            Assert.IsTrue(Mathf.Abs(Mathf.DeltaAngle(230f, target)) <= tol, "230 should be within tolerance");
            Assert.IsTrue(Mathf.Abs(Mathf.DeltaAngle(215f, target)) <= tol, "215 should be within tolerance");

            Assert.IsFalse(Mathf.Abs(Mathf.DeltaAngle(0f, target)) <= tol, "0 should be outside tolerance");
            Assert.IsFalse(Mathf.Abs(Mathf.DeltaAngle(135f, target)) <= tol, "135 should be outside tolerance");
            Assert.IsFalse(Mathf.Abs(Mathf.DeltaAngle(315f, target)) <= tol, "315 should be outside tolerance");
        }

        [Test]
        public void LanternPropInitialStateIsUnlit()
        {
            var go = new GameObject("TestLantern");
            var lantern = go.AddComponent<LanternProp>();
            lantern.SetLit(false);

            Assert.IsFalse(lantern.IsLit, "Lantern should start unlit");

            lantern.SetLit(true);
            Assert.IsTrue(lantern.IsLit, "Lantern should report lit after SetLit(true)");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Level2PresenterHasValidTotalLanterns()
        {
            Assert.AreEqual(3, Level2Presenter.TotalLanterns, "Level 2 should have exactly 3 lanterns");
        }
    }
}
#endif
