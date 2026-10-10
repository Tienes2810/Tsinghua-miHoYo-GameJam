using NUnit.Framework;
using PerspectivePuzzle.Level1;
using UnityEngine;

namespace PerspectivePuzzle.EditMode
{
    public class StairSpanTests
    {
        [Test]
        public void PlankMeetsBothStairLipsOnlyInsideTheJoinWindow()
        {
            Vector3 near = StairSpan.NearLip;
            Vector3 far = StairSpan.FarLip;
            StairSpan.Placement open = StairSpan.Evaluate(StairSpan.JoinYaw, near, far);
            Assert.Less(Vector3.Distance(open.nearEnd, near), StairSpan.Thickness);
            Assert.Less(Vector3.Distance(open.farEnd, far), StairSpan.Thickness);
            Assert.IsTrue(open.walkOpen);

            StairSpan.Placement shut = StairSpan.Evaluate(StairSpan.JoinYaw + 90f, near, far);
            float miss = Vector3.Distance(shut.farEnd, far);
            Assert.Greater(miss, Vector3.Distance(open.farEnd, far) + StairSpan.Thickness * 3f);
            Assert.Less(Vector3.Distance(shut.nearEnd, near), StairSpan.Thickness);
            Assert.IsFalse(shut.walkOpen);
        }
    }
}
