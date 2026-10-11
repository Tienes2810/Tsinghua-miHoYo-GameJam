#if UNITY_EDITOR
using System.Collections;
using System.Diagnostics;
using System.IO;
using PerspectivePuzzle.Audio;
using PerspectivePuzzle.Level1;
using PerspectivePuzzle.Level2;
using PerspectivePuzzle.Movement;
using PerspectivePuzzle.Puzzle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PerspectivePuzzle
{
    public class CinematicRecorder : MonoBehaviour
    {
        public static string RawVideo = "E:/Tsinghua_Mihoyo/Tsinghua-miHoYo-GameJam/Builds/raw_cinematic.mp4";
        public static string RawAudio = "E:/Tsinghua_Mihoyo/Tsinghua-miHoYo-GameJam/Builds/raw_audio.pcm";
        public static string FinalVideo = "E:/Tsinghua_Mihoyo/Tsinghua-miHoYo-GameJam/Builds/PerspectivePuzzle_Gameplay_Demo.mp4";
        public static bool IsRunning;
        public static bool IsFinished;
        public static string Status = "Idle";
        public static int FrameCount;
        public static long TotalAudioSamplesWritten;
        public static readonly object AudioLock = new object();

        Process ffmpegProc;
        Stream videoStream;
        static MemoryStream memoryAudioStream;
        static BinaryWriter memoryAudioWriter;

        // Virtual Mouse & Visual Ripple System
        Vector2 virtualMousePos = new Vector2(640, 360);
        bool isMouseDown;
        float clickRippleTime = -10f;
        Vector2 clickRipplePos;
        Texture2D cursorTex;
        Texture2D ringTex;

        [UnityEditor.MenuItem("GameJam/Record Cinematic Gameplay Demo")]
        public static void StartRecording()
        {
            if (IsRunning) return;
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
                UnityEditor.EditorApplication.isPlaying = true;
                return;
            }
            DoStart();
        }

        static void OnPlayModeChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.EnteredPlayMode)
            {
                UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                DoStart();
            }
        }

        static void DoStart()
        {
            var go = new GameObject("[CinematicRecorder]");
            DontDestroyOnLoad(go);
            go.AddComponent<CinematicRecorder>();
        }

        void Awake()
        {
            InitTextures();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            foreach (var al in listeners)
            {
                if (al != null && al.gameObject != gameObject)
                    al.enabled = true;
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            HookAudioListener();
        }

        void InitTextures()
        {
            // Crisp Arrow Pointer (32x32)
            cursorTex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color[] c = new Color[32 * 32];
            for (int i = 0; i < c.Length; i++) c[i] = Color.clear;

            // Draw an arrow pointer
            int[,] arrow = new int[24, 16] {
                { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                { 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                { 1, 2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                { 1, 2, 2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                { 1, 2, 2, 2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                { 1, 2, 2, 2, 2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                { 1, 2, 2, 2, 2, 2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                { 1, 2, 2, 2, 2, 2, 2, 1, 0, 0, 0, 0, 0, 0, 0, 0 },
                { 1, 2, 2, 2, 2, 2, 2, 2, 1, 0, 0, 0, 0, 0, 0, 0 },
                { 1, 2, 2, 2, 2, 2, 2, 2, 2, 1, 0, 0, 0, 0, 0, 0 },
                { 1, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0 },
                { 1, 2, 2, 1, 2, 2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                { 1, 2, 1, 0, 1, 2, 2, 1, 0, 0, 0, 0, 0, 0, 0, 0 },
                { 1, 1, 0, 0, 1, 2, 2, 1, 0, 0, 0, 0, 0, 0, 0, 0 },
                { 1, 0, 0, 0, 0, 1, 2, 2, 1, 0, 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 1, 2, 2, 1, 0, 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0, 1, 2, 2, 1, 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0, 1, 2, 2, 1, 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }
            };

            for (int y = 0; y < 24; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int val = arrow[y, x];
                    int texY = 31 - y;
                    int texX = x;
                    if (val == 1) c[texY * 32 + texX] = new Color(0.08f, 0.08f, 0.08f, 0.95f);
                    else if (val == 2) c[texY * 32 + texX] = new Color(0.98f, 0.98f, 0.98f, 1f);
                }
            }
            cursorTex.SetPixels(c);
            cursorTex.Apply();

            // Ring texture for ripple
            ringTex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            Color[] rc = new Color[64 * 64];
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));
                    float alpha = Mathf.Clamp01(1f - Mathf.Abs(d - 24f) / 4.5f);
                    rc[y * 64 + x] = new Color(1f, 0.85f, 0.3f, alpha);
                }
            }
            ringTex.SetPixels(rc);
            ringTex.Apply();
        }

        void OnGUI()
        {
            // 1. Draw expanding click ripple
            float rippleAge = Time.unscaledTime - clickRippleTime;
            if (rippleAge >= 0f && rippleAge < 0.6f && ringTex != null)
            {
                float t = rippleAge / 0.6f;
                float size = Mathf.Lerp(12f, 68f, t);
                float a = Mathf.Lerp(0.95f, 0f, t * t);
                Color prev = GUI.color;
                GUI.color = new Color(1f, 0.84f, 0.32f, a);
                Rect r = new Rect(clickRipplePos.x - size * 0.5f, clickRipplePos.y - size * 0.5f, size, size);
                GUI.DrawTexture(r, ringTex);
                GUI.color = prev;
            }

            // 2. Draw virtual mouse cursor
            if (cursorTex != null)
            {
                float scale = isMouseDown ? 0.85f : 1.0f;
                float w = 28f * scale;
                float h = 42f * scale;
                Vector2 drawPos = virtualMousePos + (isMouseDown ? new Vector2(2f, 2f) : Vector2.zero);
                GUI.DrawTexture(new Rect(drawPos.x, drawPos.y, w, h), cursorTex);
            }
        }

        IEnumerator Start()
        {
            IsRunning = true;
            IsFinished = false;
            FrameCount = 0;
            TotalAudioSamplesWritten = 0;
            Status = "Preparing scenes and audio buffer...";

            // Setup in-memory audio capture buffer (64MB RAM preallocated: zero disk I/O latency!)
            lock (AudioLock)
            {
                memoryAudioStream?.Dispose();
                memoryAudioWriter?.Dispose();
                memoryAudioStream = new MemoryStream(64 * 1024 * 1024);
                memoryAudioWriter = new BinaryWriter(memoryAudioStream);
            }

            HookAudioListener();

            if (SceneManager.GetActiveScene().name != "Level2")
            {
                SceneManager.LoadScene("Level2");
                yield return new WaitForSeconds(1.5f);
                HookAudioListener();
            }

            yield return new WaitForEndOfFrame();
            var t0 = ScreenCapture.CaptureScreenshotAsTexture(1);
            int w = t0.width;
            int h = t0.height;
            Destroy(t0);

            // FFmpeg with exact 16:9 widescreen scaling & padding (zero distortion/stretching!)
            var psi = new ProcessStartInfo
            {
                FileName = @"C:\Program Files\Lenovo\GamingAI\2.2.0105.1301\services\editor\ffmpeg.exe",
                Arguments = $"-y -f rawvideo -pix_fmt rgba -s {w}x{h} -r 30 -i - -vf \"vflip,scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2:color=black\" -c:v h264_nvenc -pix_fmt yuv420p -b:v 6M \"{RawVideo}\"",
                UseShellExecute = false,
                RedirectStandardInput = true,
                CreateNoWindow = true
            };
            ffmpegProc = Process.Start(psi);
            videoStream = ffmpegProc.StandardInput.BaseStream;

            StartCoroutine(CaptureVideoLoop());
            StartCoroutine(CinematicPlaythrough());
        }

        void LateUpdate()
        {
            UpdateListenerTransform();
        }

        void UpdateListenerTransform()
        {
            Camera cam = Camera.main;
            if (cam == null) cam = FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                transform.position = cam.transform.position;
                transform.rotation = cam.transform.rotation;
            }
        }

        void HookAudioListener()
        {
            var myListener = GetComponent<AudioListener>();
            if (myListener == null)
            {
                myListener = gameObject.AddComponent<AudioListener>();
            }
            myListener.enabled = true;

            if (GetComponent<AudioCaptureHook>() == null)
            {
                gameObject.AddComponent<AudioCaptureHook>();
            }

            // Disable other AudioListeners to prevent Unity multi-listener conflicts
            var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            foreach (var al in listeners)
            {
                if (al.gameObject != gameObject)
                {
                    al.enabled = false;
                }
            }
            UpdateListenerTransform();
        }

        IEnumerator CaptureVideoLoop()
        {
            var wait = new WaitForEndOfFrame();
            int sampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
            int channels = 2; // stereo

            while (IsRunning && TotalAudioSamplesWritten == 0)
            {
                yield return wait;
            }

            byte[] lastFrameData = null;

            while (IsRunning)
            {
                yield return wait;

                double audioSeconds = (double)TotalAudioSamplesWritten / (sampleRate * channels);
                int targetFrames = Mathf.Max(0, (int)(audioSeconds * 30.0));

                var tex = ScreenCapture.CaptureScreenshotAsTexture(1);
                if (tex != null)
                {
                    lastFrameData = tex.GetRawTextureData();
                    Destroy(tex);
                }

                if (lastFrameData == null) continue;

                // Sync video frames strictly to audio elapsed time!
                while (FrameCount < targetFrames && IsRunning)
                {
                    try
                    {
                        videoStream.Write(lastFrameData, 0, lastFrameData.Length);
                        FrameCount++;
                    }
                    catch
                    {
                        IsRunning = false;
                        break;
                    }
                }
            }
        }

        // Smooth Mouse Actions
        IEnumerator MoveMouse(Vector2 targetGuiPos, float duration)
        {
            Vector2 start = virtualMousePos;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                virtualMousePos = Vector2.Lerp(start, targetGuiPos, t);
                yield return null;
            }
            virtualMousePos = targetGuiPos;
        }

        IEnumerator VisualClickGui(Vector2 guiPos)
        {
            yield return MoveMouse(guiPos, 0.6f);
            yield return new WaitForSeconds(0.25f);
            isMouseDown = true;
            clickRipplePos = guiPos;
            clickRippleTime = Time.unscaledTime;
            Vector2 screenPos = new Vector2(guiPos.x, Screen.height - guiPos.y);
            GameInput.Click(screenPos);
            GameAudio.Instance?.PlayClick();
            yield return new WaitForSeconds(0.15f);
            isMouseDown = false;
            yield return new WaitForSeconds(0.25f);
        }

        IEnumerator VisualClickWorld(Vector3 worldPos, Camera cam)
        {
            if (cam == null) yield break;
            Vector2 sp = cam.WorldToScreenPoint(worldPos);
            Vector2 guiPos = new Vector2(sp.x, Screen.height - sp.y);
            yield return MoveMouse(guiPos, 0.6f);
            yield return new WaitForSeconds(0.25f);
            isMouseDown = true;
            clickRipplePos = guiPos;
            clickRippleTime = Time.unscaledTime;
            GameInput.Click(sp);
            GameAudio.Instance?.PlayClick();
            yield return new WaitForSeconds(0.15f);
            isMouseDown = false;
            yield return new WaitForSeconds(0.25f);
        }

        IEnumerator VisualClickPoint(string name)
        {
            foreach (var w in FindObjectsByType<Waypoint>(FindObjectsSortMode.None))
            {
                if (w.name == name && w.click != null)
                {
                    yield return VisualClickWorld(w.click.bounds.center, Camera.main);
                    yield break;
                }
            }
        }

        IEnumerator VisualClickL2Node(Level2Game g, Camera cam, string nodeId)
        {
            PathNode n = g.NodeById(nodeId);
            if (n != null && cam != null)
            {
                yield return VisualClickWorld(n.transform.position + Vector3.down * 0.02f, cam);
            }
        }

        IEnumerator DragMouse(Vector2 guiFrom, Vector2 guiTo, float duration, System.Action<float> onProgress)
        {
            yield return MoveMouse(guiFrom, 0.5f);
            yield return new WaitForSeconds(0.2f);
            isMouseDown = true;
            clickRipplePos = guiFrom;
            clickRippleTime = Time.unscaledTime;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                virtualMousePos = Vector2.Lerp(guiFrom, guiTo, t);
                onProgress?.Invoke(t);
                yield return null;
            }
            virtualMousePos = guiTo;
            isMouseDown = false;
            yield return new WaitForSeconds(0.35f);
        }

        IEnumerator CinematicPlaythrough()
        {
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            // ==========================================
            // CHƯƠNG 1: HÀ NỘI HOÀNG HÔN / LEVEL 2
            // ==========================================
            Status = "Game Start: Showing Title & Start Menu...";
            var g = Level2Game.Instance;
            Camera cam2 = g != null ? g.View : Camera.main;
            var hud2 = g != null ? g.Hud : FindAnyObjectByType<Level2Hud>();

            float scaleL2 = Mathf.Max(0.7f, Screen.height / 1080f);
            float hL2 = Screen.height / scaleL2;
            float topYL2 = Mathf.Max(30f, hL2 * 0.10f);

            yield return MoveMouse(center, 0.8f);
            yield return new WaitForSeconds(2.0f);

            // 1. Click Controls Guide button on the Start Menu
            Status = "Opening Controls Guide...";
            Vector2 helpBtnL2 = new Vector2(center.x, (topYL2 + 220f + 66f + 56f + 23f) * scaleL2);
            yield return VisualClickGui(helpBtnL2);
            if (hud2 != null) hud2.ShowControlsModal = true;
            yield return new WaitForSeconds(2.8f);

            // 2. Click Close button on Controls Modal
            float modalHL2 = 390f;
            float modalYL2 = (hL2 - modalHL2) * 0.5f;
            Vector2 closeBtnL2 = new Vector2(center.x, (modalYL2 + modalHL2 - 42f - 14f + 21f) * scaleL2);
            yield return VisualClickGui(closeBtnL2);
            if (hud2 != null) hud2.ShowControlsModal = false;
            yield return new WaitForSeconds(1.2f);

            // 3. Click [ ▶ BẮT ĐẦU CHƠI / START GAME ] button
            Status = "Starting Chapter 1: Hanoi Sunset...";
            Vector2 startBtnL2 = new Vector2(center.x, (topYL2 + 220f + 28f) * scaleL2);
            yield return VisualClickGui(startBtnL2);
            if (g != null && g.State == Level2Game.Phase.Intro) g.BeginPlay();
            yield return new WaitForSeconds(2.0f);

            // Step 1: Walk to lift
            Status = "Chapter 1: Walking onto lift...";
            yield return VisualClickL2Node(g, cam2, "Lift");
            while (g.Player.IsMoving) yield return null;
            yield return new WaitForSeconds(1.8f);

            // Step 2: Drag lift up to rooftop
            Status = "Chapter 1: Dragging lift up to rooftop...";
            Vector3 wBottom = g.LiftTransform.position + Vector3.down * 0.5f;
            Vector2 spBottom = cam2.WorldToScreenPoint(wBottom);
            Vector2 guiLiftBottom = new Vector2(spBottom.x, Screen.height - spBottom.y);

            Vector3 wTop = wBottom + g.Diorama.up * 3.4f;
            Vector2 spTop = cam2.WorldToScreenPoint(wTop);
            Vector2 guiLiftTop = new Vector2(spTop.x, Screen.height - spTop.y);

            yield return DragMouse(guiLiftBottom, guiLiftTop, 2.4f, (t) => { });
            g.SnapLift(Level2Rules.LiftHigh);
            float liftWait = Time.time + 5f;
            while (Mathf.Abs(g.LiftHeight - 3f) > 0.05f && Time.time < liftWait) yield return null;
            yield return new WaitForSeconds(2.0f);

            // Step 3: Walk to first lantern (U2)
            Status = "Chapter 1: Lighting first lantern...";
            yield return VisualClickL2Node(g, cam2, "U2");
            while (g.Player.IsMoving) yield return null;
            yield return new WaitForSeconds(2.6f);

            // Step 4: Turn crimson bridge
            Status = "Chapter 1: Turning crimson bridge across canal...";
            Vector3 crankPos = g.BridgeTransform.parent.TransformPoint(new Vector3(4f, 1.5f, 4f));
            yield return VisualClickWorld(crankPos, cam2);
            g.SnapBridge(0f);
            yield return new WaitForSeconds(2.0f);

            // Step 5: Walk across bridge to second lantern (C2)
            Status = "Chapter 1: Crossing bridge to second lantern...";
            yield return VisualClickL2Node(g, cam2, "C2");
            while (g.Player.IsMoving) yield return null;
            yield return new WaitForSeconds(2.6f);

            // Walk to balcony edge (C4)
            yield return VisualClickL2Node(g, cam2, "C4");
            while (g.Player.IsMoving) yield return null;
            yield return new WaitForSeconds(2.0f);

            // Step 6: Drag rotate world to optical perspective seam (90° -> 0°, dragging right turns world right)
            Status = "Chapter 1: Dragging world to optical perspective seam...";
            Vector2 skyStart = new Vector2(Screen.width * 0.28f, Screen.height * 0.40f);
            Vector2 skyEnd = new Vector2(Screen.width * 0.72f, Screen.height * 0.40f);
            float startWorldYaw = g.WorldYaw;
            yield return DragMouse(skyStart, skyEnd, 2.2f, (t) =>
            {
                g.SnapWorld(Mathf.Lerp(startWorldYaw, Level2Rules.SeamYaw, t));
            });
            g.SnapWorld(Level2Rules.SeamYaw);
            yield return new WaitForSeconds(2.8f);

            // Step 7: Walk across seam to bell tower (T1)
            Status = "Chapter 1: Walking across optical seam to bell tower...";
            yield return VisualClickL2Node(g, cam2, "T1");
            while (g.State == Level2Game.Phase.Play) yield return null;

            // Step 8: Bell rings, 26 lanterns float up into night sky!
            Status = "Chapter 1: Bell ringing & 26 sky lanterns rising...";
            yield return new WaitForSeconds(6.0f);

            // Step 9: Victory Screen "Hà Nội lên đèn"
            Status = "Chapter 1: Hanoi Lights Up (Victory Screen)...";
            yield return new WaitForSeconds(4.0f);

            // Click button to advance to Chapter 2
            Status = "Advancing to Chapter 2...";
            scaleL2 = Mathf.Max(0.7f, Screen.height / 1080f);
            float wL2 = Screen.width / scaleL2;
            hL2 = Screen.height / scaleL2;
            float bwL2 = 300f;
            float gapL2 = 24f;
            float btnX = (wL2 - bwL2 * 3 - gapL2 * 2) * 0.5f + bwL2 + gapL2;
            float btnY = hL2 * 0.29f + 230f;
            Vector2 chap2BtnGui = new Vector2((btnX + bwL2 * 0.5f) * scaleL2, (btnY + 28f) * scaleL2);
            yield return VisualClickGui(chap2BtnGui);
            yield return new WaitForSeconds(0.5f);

            // ==========================================
            // CHUYỂN SANG CHƯƠNG 2: NGÕ PHỐ & TIẾNG LOA / LEVEL 1
            // ==========================================
            Status = "Transitioning to Chapter 2 (Echoes of Old Hanoi)...";
            if (SceneManager.GetActiveScene().name != "Level1")
            {
                SceneManager.LoadScene("Level1");
            }
            while (SceneManager.GetActiveScene().name != "Level1")
                yield return null;
            yield return new WaitForSeconds(1.5f);
            HookAudioListener();

            var dir = FindAnyObjectByType<Level1Director>();
            var player = FindAnyObjectByType<PlayerMovement>();
            var rot = FindAnyObjectByType<BuildingRotator>();

            // Narration: Wait for intro narration to finish comfortably without any mouse clicks
            Status = "Chapter 2: Intro narration...";
            yield return MoveMouse(new Vector2(Screen.width * 0.50f, Screen.height * 0.50f), 0.8f);
            while (dir != null && dir.Phase == Level1Phase.Intro) yield return null;
            yield return new WaitForSeconds(1.2f);

            // =========================================================================
            // BƯỚC 1: BẤM CÁI MÀU VÀNG TRƯỚC (Click Yellow Marker B at the gap edge!)
            // =========================================================================
            Status = "Chapter 2: Clicking yellow marker B (walking to broken gap)...";
            if (dir != null && dir.NodeB != null)
            {
                yield return VisualClickWorld(dir.NodeB.transform.position, Camera.main);
                if (dir.Player != null)
                    dir.Player.TryMoveTo(dir.NodeB);
            }
            while (player != null && player.IsMoving) yield return null;

            // Character is now standing at the yellow circle facing the gap!
            // Wait to admire character at the gap and read dialogue "Có những con đường chỉ mở ra khi ta đổi góc nhìn..."
            yield return new WaitForSeconds(2.2f);

            // =========================================================================
            // BƯỚC 2: XOAY GÓC NHÀ (Rotate house to align optical corridor: 0° -> 315°)
            // =========================================================================
            Status = "Chapter 2: Rotating house to align optical corridor (315°)...";
            Vector2 dragStart = new Vector2(Screen.width * 0.68f, Screen.height * 0.70f);
            Vector2 dragEnd = new Vector2(Screen.width * 0.35f, Screen.height * 0.70f);
            float startYaw = rot != null ? rot.CurrentYaw : StoryProgress.StairYaw;
            float targetYaw = StoryProgress.CorridorYaw; // 315f

            yield return DragMouse(dragStart, dragEnd, 2.8f, (t) =>
            {
                if (rot != null)
                {
                    float current = Mathf.LerpAngle(startYaw, targetYaw, t);
                    rot.transform.rotation = Quaternion.Euler(0f, current, 0f);
                }
            });
            if (rot != null)
                rot.transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
            if (dir != null)
                dir.OnRotateReleased();
            GameAudio.Instance?.PlayAlign();
            yield return new WaitForSeconds(2.0f); // Admire optical connection, red marker C is now visible!

            // =========================================================================
            // BƯỚC 3: MỚI TỚI CÁI ĐỎ (Now click Red Marker C across the optical bridge!)
            // =========================================================================
            Status = "Chapter 2: Walking across aligned optical bridge to Room (Point C)...";
            if (dir != null && dir.NodeC != null)
            {
                yield return VisualClickWorld(dir.NodeC.transform.position, Camera.main);
                if (dir.Player != null)
                    dir.Player.TryMoveTo(dir.NodeC);
            }
            while (player != null && player.IsMoving) yield return null;
            yield return new WaitForSeconds(1.8f);

            // Step 4: Open cabinet & solve puzzle
            Status = "Chapter 2: Opening cabinet...";
            RoomInteractable cab = null;
            foreach (var r in FindObjectsByType<RoomInteractable>(FindObjectsSortMode.None))
                if (r.kind == InteractKind.Cabinet) cab = r;
            if (cab != null)
            {
                yield return VisualClickWorld(cab.transform.position, Camera.main);
                yield return new WaitForSeconds(2.0f);
            }

            Camera pc = GameObject.Find("CabinetCamera")?.GetComponent<Camera>();
            Transform blockA = GameObject.Find("BlockA")?.transform;
            Transform blockB = GameObject.Find("BlockB")?.transform;
            if (pc != null && blockA != null && blockB != null)
            {
                Status = "Chapter 2: Turning puzzle blocks...";
                yield return VisualClickWorld(blockA.position, pc);
                yield return new WaitForSeconds(1.4f);

                for (int i = 0; i < 3; i++)
                {
                    yield return VisualClickWorld(blockB.position, pc);
                    yield return new WaitForSeconds(1.1f);
                }
                yield return new WaitForSeconds(1.5f);

                GameObject tapeGo = GameObject.Find("Cassette");
                if (tapeGo != null)
                {
                    Status = "Chapter 2: Taking cassette tape...";
                    yield return VisualClickWorld(tapeGo.transform.position, pc);
                    yield return new WaitForSeconds(2.5f);
                }
            }

            // Step 5: Walk out through door onto walkway
            Status = "Chapter 2: Walking out door onto upper walkway...";
            if (dir != null && dir.NodeDoor != null)
            {
                yield return VisualClickWorld(dir.NodeDoor.transform.position, Camera.main);
                if (dir.Player != null)
                    dir.Player.TryMoveTo(dir.NodeDoor);
            }
            else
            {
                yield return VisualClickPoint("Door");
            }
            while (player != null && player.IsMoving) yield return null;
            yield return new WaitForSeconds(2.0f);

            // Step 6: Rotate building to face staircase & optical alignment (315° -> 250°, dragging right turns house right)
            Status = "Chapter 2: Rotating house to align staircase...";
            Vector2 rFromStair = new Vector2(Screen.width * 0.35f, Screen.height * 0.72f);
            Vector2 rToStair = new Vector2(Screen.width * 0.65f, Screen.height * 0.72f);
            float curYaw = rot.CurrentYaw;
            float targetStairYaw = StoryProgress.StairYaw; // 250f

            yield return DragMouse(rFromStair, rToStair, 2.2f, (t) =>
            {
                if (rot != null)
                {
                    float y = Mathf.LerpAngle(curYaw, targetStairYaw, t);
                    rot.transform.rotation = Quaternion.Euler(0f, y, 0f);
                }
            });
            if (rot != null)
                rot.transform.rotation = Quaternion.Euler(0f, targetStairYaw, 0f);
            if (dir != null)
                dir.OnRotateReleased();
            GameAudio.Instance?.PlayAlign();
            yield return new WaitForSeconds(2.0f); // Now staircase and dog are facing front & center!

            // Step 7: THE DOG STAIR TRAP!
            // Click staircase to flip 180° upside-down so dog patrols on the underside!
            Status = "Chapter 2: Flipping staircase upside-down to dodge dog...";
            GameObject flight = GameObject.Find("CauThang2") ?? GameObject.Find("CauThang2_Pivot");
            if (flight != null)
            {
                yield return VisualClickWorld(flight.transform.position, Camera.main);
            }
            if (dir != null && !dir.StairFlipped)
            {
                dir.ToggleStairFlip();
            }
            yield return new WaitForSeconds(2.8f); // Admire flipped staircase and upside-down dog patrol!

            // Step 8: Walk up flipped stairs to Point E
            Status = "Chapter 2: Walking up flipped stairs to Point E...";
            if (dir != null && dir.NodeE != null)
            {
                yield return VisualClickWorld(dir.NodeE.transform.position, Camera.main);
                if (dir.Player != null)
                    dir.Player.TryMoveTo(dir.NodeE);
            }
            else
            {
                yield return VisualClickPoint("E");
            }
            int beforeE = dir.story.RoofReached;
            float waitTimeoutE = Time.time + 10f;
            while (dir.story.RoofReached <= beforeE && Time.time < waitTimeoutE)
                yield return null;
            yield return new WaitForSeconds(1.8f);

            // Step 9: Rooftop perspective climbing for remaining points (F, I, L, G, M)
            Status = "Chapter 2: Climbing to rooftop speaker...";
            string[] remainingRoof = { "F", "I", "L", "G", "M" };
            foreach (string p in remainingRoof)
            {
                float tYaw = StoryProgress.YawForRoof(p);
                float cy = rot.CurrentYaw;
                float d = Mathf.DeltaAngle(cy, tYaw);
                // Synchronize mouse drag direction with rotation:
                // d > 0 (counter-clockwise / house turns left) -> mouse drags right to left (0.65 -> 0.35)
                // d < 0 (clockwise / house turns right) -> mouse drags left to right (0.35 -> 0.65)
                float fromX = d > 0 ? Screen.width * 0.65f : Screen.width * 0.35f;
                float toX = d > 0 ? Screen.width * 0.35f : Screen.width * 0.65f;
                Vector2 rFrom = new Vector2(fromX, Screen.height * 0.72f);
                Vector2 rTo = new Vector2(toX, Screen.height * 0.72f);

                yield return DragMouse(rFrom, rTo, 2.0f, (t) =>
                {
                    if (rot != null)
                    {
                        float y = Mathf.LerpAngle(cy, tYaw, t);
                        rot.transform.rotation = Quaternion.Euler(0f, y, 0f);
                    }
                });
                if (rot != null)
                    rot.transform.rotation = Quaternion.Euler(0f, tYaw, 0f);
                if (dir != null)
                    dir.OnRotateReleased();
                yield return new WaitForSeconds(1.2f);

                PathNode roofNode = null;
                switch (p)
                {
                    case "F": roofNode = dir.NodeF; break;
                    case "I": roofNode = dir.NodeI; break;
                    case "L": roofNode = dir.NodeL; break;
                    case "G": roofNode = dir.NodeG; break;
                    case "M": roofNode = dir.NodeM; break;
                }
                if (roofNode != null)
                {
                    yield return VisualClickWorld(roofNode.transform.position, Camera.main);
                    if (dir.Player != null)
                        dir.Player.TryMoveTo(roofNode);
                }
                else
                {
                    yield return VisualClickPoint(p);
                }

                int before = dir.story.RoofReached;
                float waitTimeout = Time.time + 10f;
                while (dir.story.RoofReached <= before && Time.time < waitTimeout)
                    yield return null;
                yield return new WaitForSeconds(1.6f);
            }

            // Step 8: Speaker pole & broadcast audio stinger
            // Hanoi morning broadcast chime plays and pauses for 5.0 seconds!
            Status = "Chapter 2: Activating rooftop loudspeaker (Morning broadcast playing)...";
            yield return VisualClickGui(center + new Vector2(0f, -80f));
            yield return new WaitForSeconds(2.5f);

            var pres = FindAnyObjectByType<Level1Presenter>();
            float endTimeout = Time.time + 16f;
            while (!pres.EndScreen && Time.time < endTimeout) yield return null;

            Status = "Game Complete! Reading ending reflection...";
            yield return new WaitForSeconds(6.0f);

            // ==========================================
            // FINALIZE VIDEO & AUDIO MUXING
            // ==========================================
            Status = "Finalizing video & muxing audio...";
            IsRunning = false;
            yield return new WaitForSeconds(0.5f);

            // 1. Close video stream
            try
            {
                videoStream.Flush();
                videoStream.Close();
                ffmpegProc.WaitForExit(10000);
                ffmpegProc.Dispose();
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError("Error closing ffmpeg video: " + ex);
            }

            // 2. Dump in-memory audio buffer to file (instantaneous & zero dropouts)
            lock (AudioLock)
            {
                try
                {
                    memoryAudioWriter.Flush();
                    File.WriteAllBytes(RawAudio, memoryAudioStream.ToArray());
                    memoryAudioWriter.Close();
                    memoryAudioStream.Close();
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError("Error dumping memory audio: " + ex);
                }
            }

            // 3. Mux video + recorded in-game audio with clean studio limiter (zero clipping, zero crackle!)
            int sampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
            var muxPsi = new ProcessStartInfo
            {
                FileName = @"C:\Program Files\Lenovo\GamingAI\2.2.0105.1301\services\editor\ffmpeg.exe",
                Arguments = $"-y -i \"{RawVideo}\" -f s16le -ar {sampleRate} -ac 2 -i \"{RawAudio}\" -c:v copy -af \"volume=0.88,alimiter=limit=0.92\" -c:a aac -b:a 192k -shortest \"{FinalVideo}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var muxProc = Process.Start(muxPsi);
            muxProc.WaitForExit(20000);
            muxProc.Dispose();

            Status = "Finished! Video saved to " + FinalVideo;
            IsFinished = true;
            UnityEngine.Debug.Log("[CinematicRecorder] " + Status + " total frames: " + FrameCount);
            UnityEditor.EditorApplication.isPlaying = false;
        }

        Vector2 GetPointScreen(string name)
        {
            foreach (var w in FindObjectsByType<Waypoint>(FindObjectsSortMode.None))
            {
                if (w.name == name && w.click != null)
                {
                    Camera c = Camera.main;
                    if (c != null)
                    {
                        Vector2 sp = c.WorldToScreenPoint(w.click.bounds.center);
                        return new Vector2(sp.x, Screen.height - sp.y);
                    }
                }
            }
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        Vector2 GetL2NodeScreen(Level2Game g, Camera cam, string nodeId)
        {
            PathNode n = g.NodeById(nodeId);
            if (n != null && cam != null)
            {
                Vector2 sp = cam.WorldToScreenPoint(n.transform.position + Vector3.down * 0.02f);
                return new Vector2(sp.x, Screen.height - sp.y);
            }
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        public class AudioCaptureHook : MonoBehaviour
        {
            void OnAudioFilterRead(float[] data, int channels)
            {
                if (!IsRunning || memoryAudioWriter == null) return;
                lock (AudioLock)
                {
                    try
                    {
                        for (int i = 0; i < data.Length; i++)
                        {
                            short sample = (short)(Mathf.Clamp(data[i] * 0.90f, -0.95f, 0.95f) * 32767);
                            memoryAudioWriter.Write(sample);
                        }
                        TotalAudioSamplesWritten += data.Length;
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
#endif
