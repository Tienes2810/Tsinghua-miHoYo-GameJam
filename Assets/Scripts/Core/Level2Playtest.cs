#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using PerspectivePuzzle.Level2;
using PerspectivePuzzle.Movement;
using UnityEngine;

namespace PerspectivePuzzle
{
    /// <summary>
    /// Automated play-test for Chapter 2. Drives the game ONLY through GameInput pixel clicks, drags
    /// and key presses - the same path a real mouse/keyboard takes (screen pixel -> ray -> collider).
    /// Eval: PerspectivePuzzle.Level2Playtest.Run(false);  // drag route
    ///       PerspectivePuzzle.Level2Playtest.Run(true);   // click / keyboard route
    /// </summary>
    public class Level2Playtest : MonoBehaviour
    {
        public static string Report = "";
        public static bool Done;
        public static bool Passed;
        public static int StopAfter = 99;
        public static int Stage;
        static readonly StringBuilder log = new();
        bool altRoute;
        int failures;

        public static void Run(bool alt = false, int stopAfter = 99)
        {
            log.Clear();
            Report = "";
            Done = false;
            Passed = false;
            Stage = 0;
            StopAfter = stopAfter;
            var go = new GameObject("[Level2Playtest]");
            go.AddComponent<Level2Playtest>().altRoute = alt;
        }

        // Player autorun (development builds): PerspectivePuzzle.exe -pt1 | -pt2drag | -pt2click
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoRunFromCommandLine()
        {
            if (Application.isEditor)
                return;
            string mode = null;
            foreach (string a in Environment.GetCommandLineArgs())
                if (a == "-pt1" || a == "-pt2drag" || a == "-pt2click")
                    mode = a;
            if (mode == null)
                return;
            var go = new GameObject("[PlaytestBoot]");
            DontDestroyOnLoad(go);
            go.AddComponent<PlaytestBoot>().mode = mode;
        }

        class PlaytestBoot : MonoBehaviour
        {
            public string mode;

            IEnumerator Start()
            {
                Debug.Log($"[BOOT] mode={mode} screen={Screen.width}x{Screen.height}");
                float limit = Time.realtimeSinceStartup + 240f;
                if (mode == "-pt1")
                {
                    yield return new WaitForSeconds(1f);
                    Level1Playtest.Run();
                    while (!Level1Playtest.Done && Time.realtimeSinceStartup < limit)
                        yield return null;
                    bool ok = Level1Playtest.Done && Level1Playtest.Report.Contains("end screen: True");
                    Debug.Log("[BOOT] L1 report:\n" + Level1Playtest.Report);
                    Debug.Log("[BOOT] RESULT " + (ok ? "PASS" : "FAIL"));
                    Application.Quit(ok ? 0 : 1);
                    yield break;
                }
                UnityEngine.SceneManagement.SceneManager.LoadScene("Level2");
                while (Level2Game.Instance == null && Time.realtimeSinceStartup < limit)
                    yield return null;
                yield return new WaitForSeconds(1f);
                Run(mode == "-pt2click");
                while (!Done && Time.realtimeSinceStartup < limit)
                    yield return null;
                Debug.Log("[BOOT] RESULT " + (Passed ? "PASS" : "FAIL"));
                Application.Quit(Passed ? 0 : 1);
            }
        }

        static void Log(string line)
        {
            log.AppendLine(line);
            Report = log.ToString();
            Debug.Log("[PT2] " + line);
        }

        void Check(bool ok, string what)
        {
            if (!ok)
                failures++;
            Log((ok ? "ok   " : "FAIL ") + what);
        }

        Level2Game g;
        Camera cam;

        IEnumerator Start()
        {
            yield return Until(() => Level2Game.Instance != null, 5f);
            g = Level2Game.Instance;
            cam = g.View;
            Log($"route={(altRoute ? "click/keys" : "drag")} screen={Screen.width}x{Screen.height}");

            // 1. intro -> play
            yield return Frames(30);
            for (int i = 0; i < 5 && g.State == Level2Game.Phase.Intro; i++)
            {
                GameInput.Click(new Vector2(Screen.width * 0.5f, Screen.height * 0.45f));
                yield return Frames(15);
            }
            yield return Until(() => g.State == Level2Game.Phase.Play && !g.Busy, 5f);
            Check(g.State == Level2Game.Phase.Play, "intro dismissed by click -> Play");
            if (!Next()) yield break;
            yield return Gate();

            // 2. unreachable click must not move the player
            yield return ClickNode("U0");
            yield return Frames(30);
            Check(g.Player.CurrentNode.NodeId == "S0" && !g.Player.IsMoving, "click on unreachable roof keeps player at S0");
            if (!Next()) yield break;
            yield return Gate();

            // 3. walk onto the lift
            yield return ClickNode("Lift");
            yield return Until(() => !g.Player.IsMoving && g.Player.CurrentNode.NodeId == "Lift", 8f);
            Check(g.Player.CurrentNode.NodeId == "Lift", "walked S0 -> lift by clicking the lift tile");
            if (!Next()) yield break;
            yield return Gate();

            // 4. raise the lift (drag up, or click the platform side)
            if (altRoute)
            {
                yield return ClickWorld(g.LiftTransform.position + Vector3.down * 0.6f, "lift body");
            }
            else
            {
                Vector2 a = cam.WorldToScreenPoint(g.LiftTransform.position + Vector3.down * 0.5f);
                Vector2 top = cam.WorldToScreenPoint(g.LiftTransform.position + Vector3.down * 0.5f + g.Diorama.up * 3.4f);
                Log($"drag lift {a} -> {top} hits {Describe(a)}");
                GameInput.Drag(a, top, 40);
                yield return WaitInput();
            }
            yield return Until(() => !g.Busy && Mathf.Abs(g.LiftHeight - 3f) < 0.01f, 5f);
            yield return Frames(5);
            Check(Mathf.Abs(g.LiftHeight - 3f) < 0.01f, $"lift raised to rooftop (h={g.LiftHeight:F2})");
            Check(g.Player.CurrentNode.NodeId == "Lift" && g.Player.transform.position.y > g.Diorama.position.y + 2.5f, "player rode the lift up");
            if (!Next()) yield break;
            yield return Gate();

            // 5. first lantern
            yield return ClickNode("U2");
            yield return Until(() => !g.Player.IsMoving && g.Player.CurrentNode.NodeId == "U2", 8f);
            yield return Frames(3);
            Check(g.Player.CurrentNode.NodeId == "U2", "walked to rooftop end U2");
            Check(g.LanternsLit == 1, "lantern 1 lit");
            if (!Next()) yield break;
            yield return Gate();

            // 6. bridge
            yield return ClickNode("C2");
            yield return Frames(20);
            Check(g.Player.CurrentNode.NodeId == "U2", "villa unreachable before turning the bridge");
            if (altRoute)
            {
                for (int i = 0; i < 4 && !Level2Rules.BridgeNorthSouth(g.BridgeYaw); i++)
                {
                    yield return ClickWorld(g.BridgeTransform.parent.TransformPoint(new Vector3(4f, 1.5f, 4f)), "bridge crank");
                    yield return Until(() => !g.Busy, 3f);
                }
            }
            else
            {
                yield return DragBridgeToNorthSouth();
            }
            yield return Until(() => !g.Busy, 3f);
            Check(Level2Rules.BridgeNorthSouth(g.BridgeYaw), $"bridge turned north-south (yaw={g.BridgeYaw:F0})");
            if (!Next()) yield break;
            yield return Gate();

            // 7. second lantern
            yield return ClickNode("C2");
            yield return Until(() => !g.Player.IsMoving && g.Player.CurrentNode.NodeId == "C2", 10f);
            yield return Frames(3);
            Check(g.Player.CurrentNode.NodeId == "C2", "crossed the bridge to C2");
            Check(g.LanternsLit == 2, "lantern 2 lit");
            yield return ClickNode("C4");
            yield return Until(() => !g.Player.IsMoving && g.Player.CurrentNode.NodeId == "C4", 8f);
            Check(g.Player.CurrentNode.NodeId == "C4", "walked to the dead end C4");
            yield return ClickNode("T1");
            yield return Frames(20);
            Check(g.Player.CurrentNode.NodeId == "C4", "pagoda unreachable before turning the city");
            if (!Next()) yield break;
            yield return Gate();

            // 8. turn the city until the seam closes
            if (altRoute)
            {
                for (int i = 0; i < 4 && !g.SeamOpen; i++)
                {
                    GameInput.Key(KeyCode.Q);
                    yield return WaitInput();
                    yield return Until(() => !g.Busy, 3f);
                    Log($"Q -> world yaw {g.WorldYaw:F0}");
                }
            }
            else
            {
                for (int i = 0; i < 3 && !g.SeamOpen; i++)
                {
                    Vector2 empty = FindEmpty();
                    float need = Mathf.DeltaAngle(g.WorldYaw, Level2Rules.SeamYaw);
                    float dx = need / (Level2Rules.DragYawSign * Level2Rules.DegreesPerPixel);
                    dx = Mathf.Clamp(dx, -Screen.width * 0.45f, Screen.width * 0.45f);
                    Vector2 to = empty + new Vector2(dx, 0f);
                    Log($"drag sky {empty} -> {to} (need {need:F0} deg)");
                    GameInput.Drag(empty, to, 40);
                    yield return WaitInput();
                    yield return Until(() => !g.Busy, 3f);
                    Log($"world yaw now {g.WorldYaw:F0}");
                }
            }
            yield return Frames(5);
            Check(g.SeamOpen, $"impossible seam opened (world yaw={g.WorldYaw:F0})");
            if (!Next()) yield break;
            yield return Gate();

            // 9. walk across the seam to the bell
            yield return ClickNode("T1");
            yield return Until(() => g.State != Level2Game.Phase.Play, 12f);
            Check(g.State == Level2Game.Phase.Ending || g.State == Level2Game.Phase.End, "crossed the seam, reached the bell -> ending");
            Check(g.LanternsLit == 3, "lantern 3 lit");
            yield return Until(() => g.State == Level2Game.Phase.End, 12f);
            Check(g.State == Level2Game.Phase.End, "end screen shown");
            Next();

            Passed = failures == 0;
            Log(Passed ? "PASS - Chapter 2 completed through real input path" : $"FAILED checks: {failures}");
            Done = true;
            Destroy(gameObject);
        }

        public static int HoldAt = -1;

        static IEnumerator Gate()
        {
            while (Stage == HoldAt)
                yield return null;
        }

        bool Next()
        {
            Stage++;
            if (Stage < StopAfter)
                return true;
            Log($"stopped after stage {Stage}");
            Passed = failures == 0;
            Done = true;
            Destroy(gameObject);
            return false;
        }

        string Describe(Vector2 screen)
        {
            Physics.SyncTransforms();
            if (Physics.Raycast(cam.ScreenPointToRay(screen), out RaycastHit hit, 500f))
            {
                L2Pick p = hit.collider.GetComponentInParent<L2Pick>();
                return p == null ? hit.collider.name : (p.node != null ? p.node.NodeId : p.group.ToString());
            }
            return "nothing";
        }

        IEnumerator ClickNode(string id)
        {
            PathNode n = g.NodeById(id);
            Vector2 s = cam.WorldToScreenPoint(n.transform.position + Vector3.down * 0.02f);
            Log($"click {id} at ({s.x:F0},{s.y:F0}) ray->{Describe(s)}");
            GameInput.Click(s);
            yield return WaitInput();
        }

        IEnumerator ClickWorld(Vector3 world, string label)
        {
            Vector2 s = cam.WorldToScreenPoint(world);
            Log($"click {label} at ({s.x:F0},{s.y:F0}) ray->{Describe(s)}");
            GameInput.Click(s);
            yield return WaitInput();
        }

        IEnumerator DragBridgeToNorthSouth()
        {
            for (int attempt = 0; attempt < 3 && !Level2Rules.BridgeNorthSouth(g.BridgeYaw); attempt++)
            {
                Transform b = g.BridgeTransform;
                Vector3 pivot = b.position;
                // pick a target yaw (0 or 180) nearest to the current one
                float current = g.BridgeYaw;
                float target = Mathf.Abs(Mathf.DeltaAngle(current, 0f)) <= 90f ? 0f : 180f;
                var pts = new List<Vector2>();
                const int steps = 8;
                for (int i = 0; i <= steps; i++)
                {
                    float yaw = Mathf.LerpAngle(current, target, i / (float)steps);
                    Quaternion world = b.parent.rotation * Quaternion.Euler(0f, yaw, 0f);
                    pts.Add(cam.WorldToScreenPoint(pivot + world * Vector3.forward * 1.25f + Vector3.down * 0.15f));
                }
                Log($"drag bridge arc {pts[0]} -> {pts[steps]} (yaw {current:F0} -> {target:F0}) starts on {Describe(pts[0])}");
                GameInput.DragPath(pts, 6);
                yield return WaitInput();
                yield return Until(() => !g.Busy, 3f);
                Log($"bridge yaw now {g.BridgeYaw:F0}");
            }
        }

        Vector2 FindEmpty()
        {
            for (float y = 0.85f; y > 0.1f; y -= 0.05f)
                for (float x = 0.1f; x < 0.6f; x += 0.05f)
                {
                    Vector2 s = new(Screen.width * x, Screen.height * y);
                    if (Describe(s) == "nothing" && !g.Hud.OverUi(s))
                        return s;
                }
            return new Vector2(Screen.width * 0.1f, Screen.height * 0.5f);
        }

        static IEnumerator WaitInput()
        {
            int guard = 0;
            while (GameInput.Busy && guard++ < 600)
                yield return null;
            yield return null;
        }

        static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++)
                yield return null;
        }

        static IEnumerator Until(Func<bool> cond, float timeout)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (!cond() && Time.realtimeSinceStartup < end)
                yield return null;
        }
    }
}
#endif
