using System.Collections.Generic;
using NUnit.Framework;
using PerspectivePuzzle.Level1;
using PerspectivePuzzle.Movement;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PerspectivePuzzle.EditMode
{
    public class StoryRulesTests
    {
        [Test]
        public void YawInsideToleranceOpensTheCorridorAndOutsideDoesNot()
        {
            var root = new GameObject("yaw-root");
            var board = root.AddComponent<RotationBoard>();
            var director = root.AddComponent<Level1Director>();
            var from = new GameObject("from").AddComponent<PathNode>();
            var to = new GameObject("to").AddComponent<PathNode>();
            var stairFrom = new GameObject("stair-from").AddComponent<PathNode>();
            var stairTo = new GameObject("stair-to").AddComponent<PathNode>();
            board.Bind(root.transform, null);
            board.SetLinks(new List<YawLink>
            {
                new YawLink
                {
                    from = from,
                    to = to,
                    yaw = StoryProgress.CorridorYaw,
                    tolerance = StoryProgress.Tolerance
                },
                new YawLink
                {
                    from = stairFrom,
                    to = stairTo,
                    yaw = StoryProgress.StairYaw,
                    tolerance = StoryProgress.Tolerance
                }
            });

            Assert.IsFalse(director.story.CorridorOpen(StoryProgress.StairYaw));
            Assert.IsTrue(director.story.CorridorOpen(StoryProgress.CorridorYaw));
            Assert.IsFalse(director.CorridorGateOpen(StoryProgress.StairYaw));
            Assert.IsTrue(director.CorridorGateOpen(StoryProgress.CorridorYaw));
            Assert.AreEqual(StoryProgress.CorridorYaw, Level1Director.ReleaseSnapYaw(Level1Phase.NeedRotate));
            Assert.AreEqual(StoryProgress.StairYaw, Level1Director.ReleaseSnapYaw(Level1Phase.Round2));

            root.transform.eulerAngles = new Vector3(0f, StoryProgress.StairYaw, 0f);
            board.Refresh(true);
            Assert.IsTrue(board.AnyOpen);
            Assert.IsFalse(from.IsLinked(to));
            Assert.IsTrue(stairFrom.IsLinked(stairTo));
            Assert.IsFalse(director.story.CorridorOpen(board.CurrentYaw));
            Assert.IsFalse(director.CorridorGateOpen(board.CurrentYaw));

            root.transform.eulerAngles = new Vector3(0f, StoryProgress.CorridorYaw, 0f);
            board.Refresh(true);
            Assert.IsTrue(from.IsLinked(to));
            Assert.IsFalse(stairFrom.IsLinked(stairTo));
            Assert.IsTrue(director.story.CorridorOpen(board.CurrentYaw));
            Assert.IsTrue(director.CorridorGateOpen(board.CurrentYaw));

            var floors = new List<CorridorBridge.FloorRect>
            {
                new CorridorBridge.FloorRect { minX = -0.6f, maxX = 8f, minZ = -6.97f, maxZ = 7.03f, top = 7.80f },
                new CorridorBridge.FloorRect { minX = -8.05f, maxX = -1f, minZ = -6.62f, maxZ = -0.97f, top = 8.05f }
            };
            Assert.IsTrue(CorridorBridge.TryJoin(floors, out Vector3 approachLip, out Vector3 otherLip));
            Vector3 join = otherLip - approachLip;
            float openGap = PerspectiveGap.ScreenGap(StoryProgress.CorridorYaw, join);
            float shutGap = PerspectiveGap.ScreenGap(StoryProgress.StairYaw, join);
            Assert.Less(openGap, shutGap); // isometric pitch keeps a smaller gap at the corridor yaw
            Assert.Greater(shutGap - openGap, 0.2f);

            Object.DestroyImmediate(stairTo.gameObject);
            Object.DestroyImmediate(stairFrom.gameObject);
            Object.DestroyImmediate(to.gameObject);
            Object.DestroyImmediate(from.gameObject);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void CorridorAndStairStayInsideTheirOwnTolerance()
        {
            var story = new StoryProgress();
            float edge = StoryProgress.Tolerance;
            Assert.IsTrue(story.CorridorOpen(StoryProgress.CorridorYaw));
            Assert.IsTrue(story.CorridorOpen(StoryProgress.CorridorYaw + edge));
            Assert.IsFalse(story.CorridorOpen(StoryProgress.CorridorYaw + edge + 1f));
            Assert.IsFalse(story.CorridorOpen(StoryProgress.StairYaw));
            Assert.IsTrue(story.StairOpen(StoryProgress.StairYaw));
            Assert.IsTrue(story.StairOpen(StoryProgress.StairYaw - edge));
            Assert.IsFalse(story.StairOpen(StoryProgress.StairYaw - edge - 1f));
            Assert.IsFalse(story.StairOpen(StoryProgress.CorridorYaw));
        }

        [Test]
        public void RoofOrderWaitsForItsOwnRotation()
        {
            var story = new StoryProgress();
            Assert.IsFalse(story.RoofPointOpen("M", StoryProgress.YawForRoof("M")));
            Assert.IsTrue(story.RoofPointOpen("E", StoryProgress.StairYaw));
            Assert.IsFalse(story.RoofPointOpen("E", StoryProgress.CorridorYaw));
            Assert.IsFalse(story.RoofPointOpen("F", StoryProgress.YawForRoof("F")));
            Assert.IsTrue(story.ReachRoof("E"));
            Assert.IsFalse(story.RoofPointOpen("F", StoryProgress.YawForRoof("E")));
            Assert.IsTrue(story.RoofPointOpen("F", StoryProgress.YawForRoof("F")));
            Assert.IsFalse(story.RoofPointOpen("I", StoryProgress.YawForRoof("I")));
            Assert.IsFalse(story.ReachRoof("M"));
        }

        [Test]
        public void ShippedSeamIsOneWalkOnlyAtTheCorridorYaw()
        {
            Vector3 seam = CorridorBridge.StairLandingSeam;
            float side = PerspectiveGap.ScreenGap(315f, seam);
            float house = PerspectiveGap.ScreenGap(250f, seam);
            Assert.Greater(side, 0.15f);
            Assert.Greater(house, 0.15f);
        }

        [Test]
        public void PositiveDragFollowsTheHand()
        {
            Assert.Less(DragYaw.DegreesForDrag(40f), 0f);
            Assert.IsFalse(DragYaw.MovesTowardScreenRight(40f, new Vector3(4f, 1f, 0f)));
        }

        [Test]
        public void DogFailRestoresTheStartAndKeepsUnlockedRotations()
        {
            var story = new StoryProgress();
            story.Observe(StoryProgress.CorridorYaw);
            story.Observe(StoryProgress.StairYaw);
            story.Point = "Dog";
            story.DogFail();

            Assert.AreEqual("A", story.Point);
            Assert.IsTrue(story.CorridorUnlocked);
            Assert.IsTrue(story.StairUnlocked);
        }

        [Test]
        public void ClueDoesNotRepeat()
        {
            var story = new StoryProgress();
            const string line = "Before every meal, Grandma would turn it on.";
            Assert.IsTrue(story.TryHearClue(line));
            Assert.IsFalse(story.TryHearClue(line));
        }

        [Test]
        public void SpeakerMovesFromHeldToPlacedOnlyOnThePole()
        {
            var story = new StoryProgress();
            Assert.IsFalse(story.TryPlace());
            story.Speaker = SpeakerPhase.Ready;
            Assert.IsFalse(story.TryPlace());
            Assert.IsTrue(story.TryPickup());
            Assert.AreEqual(SpeakerPhase.Held, story.Speaker);
            Assert.IsTrue(story.TryPlace());
            Assert.AreEqual(SpeakerPhase.Placed, story.Speaker);
            Assert.IsFalse(story.TryPlace());

            Assert.Less(CorridorBridge.SpeakerRest.magnitude, 1f);
            var pole = new GameObject("Pole");
            var speakerGo = new GameObject("Loudspeaker");
            pole.transform.position = CorridorBridge.PoleStand;
            speakerGo.transform.position = Vector3.up * 30f;
            Level1Director.MountOnPole(speakerGo.transform, pole.transform);
            Assert.AreSame(pole.transform, speakerGo.transform.parent);
            Assert.AreEqual(
                CorridorBridge.SpeakerRest.magnitude,
                Vector3.Distance(speakerGo.transform.position, pole.transform.position),
                0.05f);

            var flat = new GameObject("FlatPole");
            flat.transform.localScale = new Vector3(1.1f, 0.02f, 1.1f);
            var flatSpeaker = new GameObject("FlatSpeaker");
            Level1Director.MountOnPole(flatSpeaker.transform, flat.transform);
            Assert.AreSame(flat.transform, flatSpeaker.transform.parent);
            Assert.AreEqual(
                CorridorBridge.SpeakerRest.magnitude,
                Vector3.Distance(flatSpeaker.transform.position, flat.transform.position),
                0.05f);
            Assert.Greater(flatSpeaker.transform.lossyScale.y, 0.2f);

            Object.DestroyImmediate(flatSpeaker);
            Object.DestroyImmediate(flat);
            Object.DestroyImmediate(speakerGo);
            Object.DestroyImmediate(pole);
        }

        [Test]
        public void RoofStepStandsOnTheExistingSlope()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
            Transform root = GameObject.Find("BuildingRoot").transform;
            Vector3 foot = CorridorBridge.SlopeFoot;
            Vector3 lip = CorridorBridge.ShelfLip;
            Assert.IsTrue(CorridorBridge.TryMeshHeight(root, "Plane", foot.x, foot.z, out float surface));
            Assert.AreEqual(surface + 0.04f, foot.y, 0.08f);
            Assert.IsTrue(CorridorBridge.TryMeshHeight(root, "Cube.041", lip.x, lip.z, out float shelf));
            Assert.AreEqual(shelf + 0.04f, lip.y, 0.08f);
            Assert.AreEqual(lip.x, foot.x, 0.02f);
            Assert.AreEqual(lip.z, foot.z, 0.02f);
            float rise = foot.y - lip.y;
            Assert.Greater(rise, 0.3f);
            Assert.Less(rise, 0.6f);
        }

        [Test]
        public void PresenterColorsAreNotBlackOnBlack()
        {
            Assert.IsTrue(HudColors.Readable(HudColors.Ink, HudColors.Plate));
            bool blackOnBlack = HudColors.Luminance(HudColors.Ink) < 0.2f
                && HudColors.Luminance(HudColors.Plate) < 0.2f;
            Assert.IsFalse(blackOnBlack);
        }
    }
}
