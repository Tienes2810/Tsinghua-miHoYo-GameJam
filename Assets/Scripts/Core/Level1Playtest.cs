#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Text;
using PerspectivePuzzle.Level1;
using PerspectivePuzzle.Movement;
using PerspectivePuzzle.Puzzle;
using UnityEngine;

namespace PerspectivePuzzle
{
    /// <summary>
    /// Automated play-test for Chapter 1. Uses only GameInput clicks/drags at screen pixels,
    /// the same path a player's mouse takes. Start from eval: PerspectivePuzzle.Level1Playtest.Run();
    /// </summary>
    public class Level1Playtest : MonoBehaviour
    {
        public static string Report = "";
        public static bool Done;
        static readonly StringBuilder log = new();

        public static void Run()
        {
            log.Clear();
            Report = "";
            Done = false;
            new GameObject("[Level1Playtest]").AddComponent<Level1Playtest>();
        }

        static void Log(string line)
        {
            log.AppendLine(line);
            Report = log.ToString();
            Debug.Log("[PT1] " + line);
        }

        Level1Director dir;
        PlayerMovement player;
        BuildingRotator rot;

        IEnumerator Start()
        {
            dir = FindAnyObjectByType<Level1Director>();
            player = FindAnyObjectByType<PlayerMovement>();
            rot = FindAnyObjectByType<BuildingRotator>();
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            for (int i = 0; i < 6 && dir.Phase == Level1Phase.Intro; i++)
            {
                GameInput.Click(center);
                yield return Frames(20);
            }
            yield return Until(() => dir.Phase == Level1Phase.ToGap, 6f);
            Log("intro done: " + dir.Phase);

            yield return ClickPoint("B");
            yield return Until(() => dir.Phase == Level1Phase.NeedRotate || dir.Phase == Level1Phase.ToRoom, 15f);
            Log("at B: " + dir.Phase);

            yield return DragTo(StoryProgress.CorridorYaw);
            Log("yaw " + rot.CurrentYaw.ToString("F0"));
            yield return ClickPoint("C");
            yield return Until(() => dir.Phase == Level1Phase.InRoom && !player.MovementLocked, 15f);
            Log("room: " + dir.Phase);

            RoomInteractable cab = null;
            foreach (var r in FindObjectsByType<RoomInteractable>(FindObjectsSortMode.None))
                if (r.kind == InteractKind.Cabinet) cab = r;
            yield return ClickWorld(cab.transform.position, "Cabinet");
            yield return Until(() => CabinetView.Current != null && CabinetView.Current.IsOpen, 4f);
            Log("cabinet open: " + (CabinetView.Current != null));
            yield return Frames(20);
            Camera pc = GameObject.Find("CabinetCamera").GetComponent<Camera>();
            Transform a = GameObject.Find("BlockA").transform;
            Transform b = GameObject.Find("BlockB").transform;
            yield return ClickWith(pc, a.position); yield return Frames(25);
            for (int i = 0; i < 3; i++) { yield return ClickWith(pc, b.position); yield return Frames(25); }
            Log("cabinet turns " + CabinetView.Current.TurnsA + "," + CabinetView.Current.TurnsB + " tape=" + CabinetView.Current.IsTapeVisible);
            GameObject tapeGo = GameObject.Find("Cassette");
            if (tapeGo == null) { Log("FAIL cassette not revealed"); Done = true; Destroy(gameObject); yield break; }
            Transform tape = tapeGo.transform;
            yield return ClickWith(pc, tape.position);
            yield return Until(() => dir.Phase == Level1Phase.ToExit, 4f);
            Log("tape taken: " + dir.Phase + " speaker=" + dir.story.Speaker);

            yield return ClickPoint("Door");
            yield return Until(() => dir.Phase == Level1Phase.Round2, 20f);
            Log("round2: " + dir.Phase);

            // Dog trap at the current yaw, if the dog point is offered.
            if (Visible("Dog"))
            {
                yield return ClickPoint("Dog");
                yield return Until(() => dir.Phase == Level1Phase.NeedStair, 20f);
                yield return Until(() => dir.Phase == Level1Phase.Round2 && !player.MovementLocked, 10f);
                Log("dog fall, back at " + (player.CurrentNode ? player.CurrentNode.name : "-"));
            }
            else Log("dog point not offered at yaw " + rot.CurrentYaw.ToString("F0"));

            foreach (string p in StoryProgress.RoofPoints)
            {
                yield return DragTo(StoryProgress.YawForRoof(p));
                yield return Frames(15);
                if (!Visible(p)) { Log("FAIL roof " + p + " not visible at yaw " + rot.CurrentYaw.ToString("F0")); break; }
                yield return ClickPoint(p);
                int before = dir.story.RoofReached;
                yield return Until(() => dir.story.RoofReached > before && !player.IsMoving, 30f);
                Log("roof " + p + " reached=" + dir.story.RoofReached + " phase=" + dir.Phase);
            }

            yield return Until(() => dir.Phase == Level1Phase.AtPole, 10f);
            GameInput.Click(center);
            yield return Until(() => dir.Phase == Level1Phase.Ending, 5f);
            Log("ending: " + dir.Phase);
            var pres = FindAnyObjectByType<Level1Presenter>();
            yield return Until(() => pres.EndScreen, 40f);
            Log("end screen: " + pres.EndScreen);
            Done = true;
            Destroy(gameObject);
        }

        bool Visible(string name)
        {
            foreach (var w in FindObjectsByType<Waypoint>(FindObjectsSortMode.None))
                if (w.name == name && w.click != null && w.click.enabled) return true;
            return false;
        }

        IEnumerator ClickPoint(string name)
        {
            Waypoint target = null;
            foreach (var w in FindObjectsByType<Waypoint>(FindObjectsSortMode.None))
                if (w.name == name && w.click != null && w.click.enabled) target = w;
            if (target == null) { Log("FAIL point " + name + " not clickable"); yield break; }
            yield return ClickWorld(target.click.bounds.center, name);
        }

        IEnumerator ClickWorld(Vector3 world, string label)
        {
            Camera cam = Camera.main;
            Vector2 px = cam.WorldToScreenPoint(world);
            string hit = Physics.Raycast(cam.ScreenPointToRay(px), out RaycastHit h, 800f) ? h.collider.name : "none";
            Log("click " + label + " at " + px.ToString("F0") + " ray->" + hit);
            yield return ClickWith(cam, world);
        }

        IEnumerator ClickWith(Camera cam, Vector3 world)
        {
            GameInput.Click(cam.WorldToScreenPoint(world));
            yield return Until(() => !GameInput.Busy, 2f);
            yield return Frames(3);
        }

        IEnumerator DragTo(float target)
        {
            for (int pass = 0; pass < 4; pass++)
            {
                float delta = Mathf.DeltaAngle(rot.CurrentYaw, target);
                if (Mathf.Abs(delta) < 1f) yield break;
                float perPixel = DragYaw.DegreesPerPixel * Mathf.Sign(DragYaw.DegreesForDrag(1f));
                float dx = delta / perPixel;
                float max = Screen.width * 0.8f;
                dx = Mathf.Clamp(dx, -max, max);
                Vector2 from = new Vector2(Screen.width * 0.5f - dx * 0.5f, Screen.height * 0.82f);
                GameInput.Drag(from, from + new Vector2(dx, 0f), 30);
                yield return Until(() => !GameInput.Busy, 3f);
                yield return Frames(25);
            }
        }

        static IEnumerator Frames(int n)
        {
            // at least n frames AND n/60 s, so fast player frame rates don't outrun animations
            float until = Time.realtimeSinceStartup + n / 60f;
            for (int i = 0; i < n || Time.realtimeSinceStartup < until; i++) yield return null;
        }

        static IEnumerator Until(System.Func<bool> ok, float seconds)
        {
            float t = 0f;
            while (!ok() && t < seconds) { t += Time.deltaTime; yield return null; }
            if (!ok()) Log("TIMEOUT after " + seconds + "s");
        }
    }
}
#endif
