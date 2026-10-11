using System.Collections.Generic;
using UnityEngine;

namespace PerspectivePuzzle
{
    /// <summary>
    /// Single entry point for pointer and key input.
    /// Reads the real mouse/keyboard, and also accepts injected frames so automated
    /// play-tests drive the exact same code path a player does (screen pixel -> ray -> collider).
    /// </summary>
    public static class GameInput
    {
        public struct Frame
        {
            public Vector2 position;
            public int button;      // 0 left, 1 right
            public bool held;
            public KeyCode key;     // KeyCode.None when no key this frame
        }

        static readonly Queue<Frame> queue = new();
        static bool active;
        static Vector2 position;
        static int button;
        static bool held;
        static bool down;
        static bool up;
        static KeyCode key = KeyCode.None;
        static int stampedFrame = -1;

        public static bool Busy => queue.Count > 0 || held;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            queue.Clear();
            active = false;
            held = false;
            down = false;
            up = false;
            key = KeyCode.None;
            var go = new GameObject("[GameInput]");
            go.hideFlags = HideFlags.HideInHierarchy;
            Object.DontDestroyOnLoad(go);
            go.AddComponent<GameInputPump>();
        }

        /// <summary>Called once per frame, before every other script.</summary>
        internal static void Pump()
        {
            if (stampedFrame == Time.frameCount)
                return;
            stampedFrame = Time.frameCount;

            down = false;
            up = false;
            key = KeyCode.None;

            if (queue.Count == 0)
            {
                if (held)
                {
                    held = false;
                    up = true;
                    return;
                }
                active = false;
                return;
            }

            Frame f = queue.Dequeue();
            active = true;
            position = f.position;
            key = f.key;
            if (f.held && !held)
                down = true;
            if (!f.held && held)
                up = true;
            held = f.held;
            button = f.button;
        }

        public static Vector3 mousePosition => active ? (Vector3)position : Input.mousePosition;

        public static bool GetMouseButtonDown(int b) => Input.GetMouseButtonDown(b) || (active && down && button == b);
        public static bool GetMouseButton(int b) => Input.GetMouseButton(b) || (active && held && button == b);
        public static bool GetMouseButtonUp(int b) => Input.GetMouseButtonUp(b) || (active && up && button == b);
        public static bool GetKeyDown(KeyCode k) => Input.GetKeyDown(k) || (active && key == k && k != KeyCode.None);
        public static bool GetKey(KeyCode k) => Input.GetKey(k);
        public static bool anyKeyDown => Input.anyKeyDown || (active && (down || key != KeyCode.None));

        // ---- Injection helpers (used by play-tests) ----

        public static void Click(Vector2 screen, int mouseButton = 0)
        {
            queue.Enqueue(new Frame { position = screen, button = mouseButton, held = true });
            queue.Enqueue(new Frame { position = screen, button = mouseButton, held = true });
            queue.Enqueue(new Frame { position = screen, button = mouseButton, held = false });
            queue.Enqueue(new Frame { position = screen, button = mouseButton, held = false });
        }

        public static void Drag(Vector2 from, Vector2 to, int frames = 30)
        {
            frames = Mathf.Max(2, frames);
            queue.Enqueue(new Frame { position = from, held = true });
            for (int i = 1; i <= frames; i++)
                queue.Enqueue(new Frame { position = Vector2.Lerp(from, to, i / (float)frames), held = true });
            queue.Enqueue(new Frame { position = to, held = true });
            queue.Enqueue(new Frame { position = to, held = false });
            queue.Enqueue(new Frame { position = to, held = false });
        }

        public static void DragPath(IList<Vector2> points, int framesPerSegment = 6)
        {
            if (points == null || points.Count < 2)
                return;
            queue.Enqueue(new Frame { position = points[0], held = true });
            for (int s = 1; s < points.Count; s++)
                for (int i = 1; i <= framesPerSegment; i++)
                    queue.Enqueue(new Frame { position = Vector2.Lerp(points[s - 1], points[s], i / (float)framesPerSegment), held = true });
            Vector2 end = points[points.Count - 1];
            queue.Enqueue(new Frame { position = end, held = true });
            queue.Enqueue(new Frame { position = end, held = false });
            queue.Enqueue(new Frame { position = end, held = false });
        }

        public static void Key(KeyCode k)
        {
            queue.Enqueue(new Frame { position = Input.mousePosition, key = k });
            queue.Enqueue(new Frame { position = Input.mousePosition });
        }

        public static void Wait(int frames)
        {
            for (int i = 0; i < frames; i++)
                queue.Enqueue(new Frame { position = position });
        }
    }

    [DefaultExecutionOrder(-32000)]
    public class GameInputPump : MonoBehaviour
    {
        void Update()
        {
            GameInput.Pump();
        }
    }
}
