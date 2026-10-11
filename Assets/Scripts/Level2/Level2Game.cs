using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using PerspectivePuzzle.Audio;
using PerspectivePuzzle.Level1;
using PerspectivePuzzle.Movement;
using PerspectivePuzzle.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PerspectivePuzzle.Level2
{
    /// <summary>
    /// Chapter 2 - "Hà Nội Hoàng Hôn / Hanoi at Dusk".
    /// A Monument-Valley style diorama built entirely at runtime: a red lift, a turning red bridge
    /// and a city you can spin until an impossible seam joins the villa roof to the lake pagoda.
    /// All pointer and key input goes through <see cref="GameInput"/>.
    /// </summary>
    public partial class Level2Game : MonoBehaviour
    {
        [Header("Wired by the editor builder")]
        public Camera view;
        public Light sun;
        public GameObject bodyPrefab;
        public AnimationClip idleClip;
        public AnimationClip walkClip;
        public AnimationClip carryClip;
        public Material litTemplate;
        public Material vertexTemplate;
        public Material glowTemplate;

        public enum Phase { Intro, Play, Ending, End }

        public static Level2Game Instance { get; private set; }

        public Phase State { get; private set; } = Phase.Intro;
        public float WorldYaw { get; private set; }
        public float BridgeYaw { get; private set; }
        public float LiftHeight { get; private set; }
        public int LanternsLit { get; private set; }
        public bool SeamOpen => c4 != null && c4.IsLinked(t0);
        public PlayerMovement Player => player;
        public Camera View => view;
        public string Objective { get; private set; } = "";
        public string Caption { get; private set; }
        public float CaptionAt { get; private set; } = -99f;
        public float CaptionFor { get; private set; } = 5f;
        public string Toast { get; private set; }
        public float ToastAt { get; private set; } = -99f;
        public bool HintActive { get; private set; }
        public float EndAt { get; private set; }
        public float StartedAt { get; private set; }
        public int Moves { get; private set; }
        public bool Busy => player.IsMoving || liftTween != null || bridgeTween != null || worldTween != null;
        public Transform LiftTransform => liftT;
        public Transform BridgeTransform => bridgeT;
        public Transform Diorama => root;
        public Level2Hud Hud { get; private set; }

        static readonly Vector3 Center = new(0.6f, 0f, 3.0f);
        const float PlayerHeight = 0.95f;
        const float ClickSlop = 9f;

        L2Kit kit;
        Transform root;
        Transform layout;
        Transform liftT;
        Transform bridgeT;
        Transform crankT;
        Transform rope;
        PlayerMovement player;
        PathNode s0, s1, s2, liftNode, u0, u1, u2, bNorth, bPivot, bSouth, c0, c1, c2, c3, c4, t0, t1;
        readonly Dictionary<string, PathNode> nodes = new();
        readonly Lantern[] lanterns = new Lantern[3];
        readonly List<Renderer>[] windowGroups = { new(), new(), new() };
        readonly List<Renderer> fairyLights = new();
        Material liftMat;
        Material bridgeMat;
        Material windowDark;
        Material windowWarm;
        Material fairyOff;
        Material fairyOn;
        Transform bell;
        Transform marker;

        Tween liftTween;
        Tween bridgeTween;
        Tween worldTween;

        PathNode lastNode;
        float lastActionAt;
        float objectiveChangedAt;
        float stepTimer;
        Transform hintTarget;
        string lastObjective;

        // camera
        Vector3 focus;
        Vector3 focusTarget;
        float baseSize = 7.4f;
        float sizeTarget = 7.4f;
        float sizeScale = 1f;
        Vector3 focusVelocity;
        float sizeVelocity;
        float night;
        float nightTarget;

        void Awake()
        {
            Instance = this;
            DOTween.Init();
            kit = new L2Kit(litTemplate, vertexTemplate, glowTemplate);
            Hud = GetComponent<Level2Hud>();
            if (Hud == null)
                Hud = gameObject.AddComponent<Level2Hud>();

            SetupCamera();
            BuildSky();
            BuildWorld();
            BuildPlayer();
            WorldYaw = Level2Rules.StartWorldYaw;
            BridgeYaw = Level2Rules.StartBridgeYaw;
            LiftHeight = Level2Rules.LiftLow;
            ApplyWorld();
            ApplyBridge();
            ApplyLift();
            RelinkAll();
            lastActionAt = Time.time;
            ApplyMood(0f);
            focus = focusTarget = new Vector3(0f, -1.2f, 0f);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            DOTween.Kill(this);
        }

        void Start()
        {
            GameAudio.Instance?.PlayAmbience();
            UpdateObjective();
        }

        // =====================================================================
        // Frame loop
        // =====================================================================

        void Update()
        {
            float t = Time.time;
            HandleInput();
            TrackPlayer();
            UpdateObjective();
            AnimateWorld(t);

            if (State == Phase.Intro)
            {
                WorldYaw = Level2Rules.StartWorldYaw + Mathf.Sin(t * 0.5f) * 12f;
                ApplyWorld();
            }

            HintActive = State == Phase.Play && !Busy && t - lastActionAt > 11f;
            UpdateMarker(t);

            night = Mathf.MoveTowards(night, nightTarget, Time.deltaTime * 0.25f);
            ApplyMood(night);
        }

        void LateUpdate()
        {
            float aspect = Mathf.Max(0.5f, view.aspect);
            // keep the whole diorama in frame on narrow windows
            float need = Mathf.Max(baseSize, 7.6f / aspect * 1.35f);
            sizeTarget = need * sizeScale;
            focus = Vector3.SmoothDamp(focus, focusTarget, ref focusVelocity, 0.6f);
            view.orthographicSize = Mathf.SmoothDamp(view.orthographicSize, sizeTarget, ref sizeVelocity, 0.6f);
            Quaternion rot = Quaternion.Euler(Level2Rules.CameraPitch, Level2Rules.CameraYaw, 0f);
            view.transform.SetPositionAndRotation(focus - rot * Vector3.forward * 60f, rot);
            LayoutSky(aspect);
        }

        // =====================================================================
        // Input
        // =====================================================================

        enum Drag { None, World, Lift, Bridge, Blocked }

        bool pressing;
        Vector2 pressPos;
        L2Pick pressPick;
        Drag drag;
        float pressWorldYaw;
        float pressBridgeYaw;
        float pressLift;
        float pressAngle;
        float liftPixelsPerUnit = 80f;
        bool bridgeAngular;

        void HandleInput()
        {
            Vector2 m = GameInput.mousePosition;

            if (State == Phase.Intro)
            {
                if (Time.time > 0.6f && (GameInput.GetMouseButtonDown(0) || GameInput.anyKeyDown) && !Hud.OverUi(m))
                    BeginPlay();
                return;
            }

            if (State == Phase.End)
            {
                if (Time.time - EndAt < 1.2f)
                    return;
                if (GameInput.GetKeyDown(KeyCode.R) || GameInput.GetKeyDown(KeyCode.Return) || GameInput.GetKeyDown(KeyCode.KeypadEnter))
                    Restart();
                else if (GameInput.GetKeyDown(KeyCode.C) || GameInput.GetKeyDown(KeyCode.Backspace))
                    BackToChapter1();
                else if (GameInput.GetKeyDown(KeyCode.Escape))
                    Application.Quit();
                return;
            }

            if (State != Phase.Play)
                return;

            if (GameInput.GetKeyDown(KeyCode.R))
            {
                Restart();
                return;
            }

            if (GameInput.GetKeyDown(KeyCode.Q) || GameInput.GetKeyDown(KeyCode.LeftArrow))
                TurnWorld(90f);
            else if (GameInput.GetKeyDown(KeyCode.E) || GameInput.GetKeyDown(KeyCode.RightArrow))
                TurnWorld(-90f);

            if (GameInput.GetMouseButtonDown(0))
            {
                if (Hud.OverUi(m))
                {
                    pressing = false;
                    return;
                }

                pressing = true;
                pressPos = m;
                drag = Drag.None;
                pressPick = PickAt(m);
                pressWorldYaw = WorldYaw;
                pressBridgeYaw = BridgeYaw;
                pressLift = LiftHeight;
                lastActionAt = Time.time;
            }

            if (pressing && GameInput.GetMouseButton(0))
            {
                if (drag == Drag.None && (m - pressPos).magnitude > ClickSlop)
                    BeginDrag();
                ContinueDrag(m);
            }

            if (pressing && GameInput.GetMouseButtonUp(0))
            {
                pressing = false;
                if (drag == Drag.None)
                    ClickAt(m);
                else
                    EndDrag();
                drag = Drag.None;
                lastActionAt = Time.time;
            }
        }

        L2Pick PickAt(Vector2 screen)
        {
            Physics.SyncTransforms();
            Ray ray = view.ScreenPointToRay(screen);
            if (Physics.Raycast(ray, out RaycastHit hit, 500f))
                return hit.collider.GetComponentInParent<L2Pick>();
            return null;
        }

        void BeginDrag()
        {
            PickGroup group = pressPick != null ? pressPick.group : PickGroup.World;
            if (group == PickGroup.Lift)
            {
                if (player.IsMoving || liftTween != null || worldTween != null)
                {
                    drag = Drag.Blocked;
                    ShowToast(player.IsMoving ? "Đợi nhân vật đi xong · Wait until walk is finished" : "Xin chờ một chút · Wait a moment...");
                    return;
                }

                drag = Drag.Lift;
                Vector3 a = view.WorldToScreenPoint(liftT.position);
                Vector3 b = view.WorldToScreenPoint(liftT.position + root.up);
                liftPixelsPerUnit = Mathf.Max(20f, b.y - a.y);
                if (player.CurrentNode == liftNode)
                    player.transform.SetParent(liftT, true);
                UnlinkLift();
                return;
            }

            if (group == PickGroup.Bridge)
            {
                if (player.IsMoving || bridgeTween != null || worldTween != null)
                {
                    drag = Drag.Blocked;
                    ShowToast("Đợi nhân vật đi xong · Wait until walk is finished");
                    return;
                }

                if (PlayerOnBridge())
                {
                    drag = Drag.Blocked;
                    ShowToast("Bước ra khỏi cầu trước khi xoay · Step off bridge before turning");
                    GameAudio.Instance?.PlayClick(0.6f);
                    return;
                }

                drag = Drag.Bridge;
                Vector2 pivot = view.WorldToScreenPoint(bridgeT.position);
                Vector2 rel = pressPos - pivot;
                bridgeAngular = rel.magnitude > 24f;
                pressAngle = Mathf.Atan2(rel.y, rel.x) * Mathf.Rad2Deg;
                UnlinkBridge();
                return;
            }

            if (player.IsMoving || worldTween != null || liftTween != null || bridgeTween != null)
            {
                drag = Drag.Blocked;
                ShowToast("Đợi nhân vật đi xong · Wait until walk is finished");
                return;
            }

            drag = Drag.World;
            UnlinkSeam();
        }

        void ContinueDrag(Vector2 m)
        {
            switch (drag)
            {
                case Drag.Lift:
                    SetLift(Mathf.Clamp(pressLift + (m.y - pressPos.y) / liftPixelsPerUnit, Level2Rules.LiftLow, Level2Rules.LiftHigh));
                    break;
                case Drag.Bridge:
                    if (bridgeAngular)
                    {
                        Vector2 pivot = view.WorldToScreenPoint(bridgeT.position);
                        Vector2 rel = m - pivot;
                        if (rel.magnitude > 12f)
                        {
                            float angle = Mathf.Atan2(rel.y, rel.x) * Mathf.Rad2Deg;
                            BridgeYaw = pressBridgeYaw - Mathf.DeltaAngle(pressAngle, angle);
                        }
                    }
                    else
                    {
                        BridgeYaw = pressBridgeYaw - (m.x - pressPos.x) * 0.5f;
                    }
                    ApplyBridge();
                    break;
                case Drag.World:
                    WorldYaw = pressWorldYaw + Level2Rules.DragYawSign * Level2Rules.DegreesPerPixel * (m.x - pressPos.x);
                    ApplyWorld();
                    break;
            }
        }

        void EndDrag()
        {
            switch (drag)
            {
                case Drag.Lift:
                    SnapLift(Level2Rules.SnapLift(LiftHeight + (LiftHeight - pressLift) * 0.15f));
                    break;
                case Drag.Bridge:
                    SnapBridge(Level2Rules.SnapQuarter(BridgeYaw));
                    break;
                case Drag.World:
                    SnapWorld(Level2Rules.SnapQuarter(WorldYaw));
                    break;
            }
        }

        void ClickAt(Vector2 m)
        {
            L2Pick pick = PickAt(m);
            if (pick == null)
            {
                Ray ray = view.ScreenPointToRay(m);
                if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float d))
                    ClickRipple.Spawn(ray.GetPoint(d), new Color(1f, 1f, 1f, 0.35f), 0.3f, 0.35f);
                return;
            }

            if (pick.node != null)
            {
                WalkTo(pick.node);
                return;
            }

            if (pick.group == PickGroup.Lift)
            {
                if (Busy)
                {
                    ShowToast("Đợi nhân vật đi xong · Wait until walk is finished");
                    return;
                }
                if (player.CurrentNode == liftNode)
                    player.transform.SetParent(liftT, true);
                UnlinkLift();
                SnapLift(Level2Rules.LiftAt(LiftHeight, Level2Rules.LiftHigh) ? Level2Rules.LiftLow : Level2Rules.LiftHigh);
            }
            else if (pick.group == PickGroup.Bridge)
            {
                if (Busy)
                {
                    ShowToast("Đợi nhân vật đi xong · Wait until walk is finished");
                    return;
                }
                if (PlayerOnBridge())
                {
                    ShowToast("Bước ra khỏi cầu trước khi xoay · Step off bridge before turning");
                    return;
                }
                UnlinkBridge();
                SnapBridge(Level2Rules.SnapQuarter(BridgeYaw) - 90f);
            }
        }

        public bool WalkTo(PathNode target)
        {
            if (State != Phase.Play || target == null)
                return false;
            if (liftTween != null || bridgeTween != null || worldTween != null)
            {
                ShowToast("Xin chờ một chút · Wait a moment...");
                return false;
            }

            if (player.CurrentNode == target)
            {
                ClickRipple.Spawn(target.transform.position, new Color(1f, 0.85f, 0.45f, 0.9f));
                return true;
            }

            bool ok = player.TryMoveTo(target);
            if (ok)
            {
                Moves++;
                ClickRipple.Spawn(target.transform.position, new Color(1f, 0.85f, 0.45f, 0.95f), 0.5f, 0.5f);
                GameAudio.Instance?.PlayClick();
            }
            else
            {
                ClickRipple.Spawn(target.transform.position, new Color(1f, 0.35f, 0.3f, 0.9f), 0.4f, 0.4f);
                GameAudio.Instance?.PlayClick(0.55f);
                ShowToast("Chưa có đường tới đây: hãy xoay đổi góc nhìn · No path yet - change the view");
            }

            return ok;
        }

        bool PlayerOnBridge()
        {
            PathNode n = player.CurrentNode;
            return n == bNorth || n == bPivot || n == bSouth;
        }

        // =====================================================================
        // Mechanisms
        // =====================================================================

        void SetLift(float h)
        {
            LiftHeight = h;
            ApplyLift();
        }

        void ApplyLift()
        {
            liftT.localPosition = new Vector3(4f, LiftHeight, -1f);
            if (rope != null)
            {
                float top = 3.72f;
                float len = Mathf.Max(0.05f, top - (LiftHeight + 0.02f));
                rope.localPosition = new Vector3(4f, LiftHeight + len * 0.5f, -1f);
                rope.localScale = new Vector3(0.035f, len, 0.035f);
            }
        }

        void SnapLift(float target)
        {
            liftTween?.Kill();
            float distance = Mathf.Abs(target - LiftHeight);
            liftTween = DOTween.To(() => LiftHeight, SetLift, target, Mathf.Max(0.2f, distance * 0.35f))
                .SetEase(Ease.InOutSine).SetTarget(this)
                .OnComplete(() =>
                {
                    liftTween = null;
                    RelinkLift();
                    GameAudio.Instance?.PlaySwitch();
                    ClickRipple.Spawn(liftT.position, new Color(1f, 0.55f, 0.4f, 0.9f), 0.65f, 0.5f);
                });
        }

        void ApplyBridge()
        {
            bridgeT.localRotation = Quaternion.Euler(0f, BridgeYaw, 0f);
            if (crankT != null)
                crankT.localRotation = Quaternion.Euler(0f, BridgeYaw, 0f);
        }

        void SnapBridge(float target)
        {
            bridgeTween?.Kill();
            float distance = Mathf.Abs(Mathf.DeltaAngle(BridgeYaw, target));
            bridgeTween = DOTween.To(() => BridgeYaw, v => { BridgeYaw = v; ApplyBridge(); }, target, Mathf.Max(0.18f, distance / 260f))
                .SetEase(Ease.OutBack).SetTarget(this)
                .OnComplete(() =>
                {
                    BridgeYaw = Level2Rules.Wrap(target);
                    ApplyBridge();
                    bridgeTween = null;
                    bool connected = RelinkBridge();
                    if (connected)
                    {
                        GameAudio.Instance?.PlayAlign();
                        ClickRipple.Spawn(bridgeT.position, new Color(1f, 0.8f, 0.4f, 1f), 1.6f, 0.7f);
                    }
                    else
                    {
                        GameAudio.Instance?.PlayClick(0.8f);
                    }
                });
        }

        void ApplyWorld()
        {
            root.localRotation = Quaternion.Euler(0f, WorldYaw, 0f);
        }

        void TurnWorld(float delta)
        {
            if (player.IsMoving || worldTween != null || liftTween != null || bridgeTween != null)
            {
                ShowToast("Đợi nhân vật đi xong · Wait until walk is finished");
                return;
            }
            lastActionAt = Time.time;
            UnlinkSeam();
            SnapWorld(Level2Rules.SnapQuarter(WorldYaw + delta));
        }

        void SnapWorld(float target)
        {
            worldTween?.Kill();
            float distance = Mathf.Abs(Mathf.DeltaAngle(WorldYaw, target));
            worldTween = DOTween.To(() => WorldYaw, v => { WorldYaw = v; ApplyWorld(); }, target, Mathf.Max(0.2f, distance / 220f))
                .SetEase(Ease.OutCubic).SetTarget(this)
                .OnComplete(() =>
                {
                    WorldYaw = Level2Rules.Wrap(target);
                    ApplyWorld();
                    worldTween = null;
                    bool wasOpen = SeamOpen;
                    RelinkSeam();
                    if (SeamOpen && !wasOpen)
                    {
                        GameAudio.Instance?.PlayAlign();
                        ClickRipple.Spawn(c4.transform.position, new Color(1f, 0.85f, 0.45f, 1f), 1.4f, 0.8f);
                        ClickRipple.Spawn(t0.transform.position, new Color(1f, 0.85f, 0.45f, 1f), 1.4f, 0.8f);
                        if (LanternsLit >= 2)
                            ShowCaption("From exactly the right angle, the old pagoda was only one step away.", 5f);
                    }
                    else
                    {
                        GameAudio.Instance?.PlayDoor();
                    }
                });
        }

        void UnlinkLift()
        {
            liftNode.Unlink(s2);
            liftNode.Unlink(u0);
        }

        void RelinkLift()
        {
            UnlinkLift();
            if (liftTween != null)
                return;
            if (Level2Rules.LiftAt(LiftHeight, Level2Rules.LiftLow))
                liftNode.Link(s2);
            else if (Level2Rules.LiftAt(LiftHeight, Level2Rules.LiftHigh))
                liftNode.Link(u0);
        }

        void UnlinkBridge()
        {
            bNorth.Unlink(u2);
            bNorth.Unlink(c0);
            bSouth.Unlink(u2);
            bSouth.Unlink(c0);
        }

        bool RelinkBridge()
        {
            UnlinkBridge();
            if (bridgeTween != null || !Level2Rules.BridgeNorthSouth(BridgeYaw))
                return false;
            bool any = false;
            foreach (PathNode arm in new[] { bNorth, bSouth })
            {
                Vector3 p = layout.InverseTransformPoint(arm.transform.position);
                if (Vector3.Distance(p, new Vector3(4f, 3f, 3f)) < 0.1f) { arm.Link(u2); any = true; }
                if (Vector3.Distance(p, new Vector3(4f, 3f, 5f)) < 0.1f) { arm.Link(c0); any = true; }
            }
            return any;
        }

        void UnlinkSeam()
        {
            c4.Unlink(t0);
        }

        void RelinkSeam()
        {
            UnlinkSeam();
            if (worldTween == null && Level2Rules.SeamAligned(WorldYaw))
                c4.Link(t0);
        }

        void RelinkAll()
        {
            RelinkLift();
            RelinkBridge();
            RelinkSeam();
        }

        // =====================================================================
        // Progression
        // =====================================================================

        void BeginPlay()
        {
            State = Phase.Play;
            StartedAt = Time.time;
            lastActionAt = Time.time;
            focusTarget = Vector3.up * 1.6f;
            GameAudio.Instance?.PlayButton();
            worldTween?.Kill();
            worldTween = DOTween.To(() => WorldYaw, v => { WorldYaw = v; ApplyWorld(); }, Level2Rules.StartWorldYaw, 0.6f)
                .SetEase(Ease.OutCubic).SetTarget(this)
                .OnComplete(() =>
                {
                    WorldYaw = Level2Rules.StartWorldYaw;
                    ApplyWorld();
                    worldTween = null;
                    RelinkSeam();
                });
            ShowCaption("Mỗi mùa Trung Thu, Bà luôn thắp ba ngọn đèn để con cháu tìm được lối về. Hãy thắp sáng lại những ngọn đèn ấy.\nEvery Mid-Autumn, Grandma lit three lanterns so the family could find its way home. Light them again.", 7.5f);
        }

        void TrackPlayer()
        {
            PathNode now = player.CurrentNode;
            if (!player.IsMoving && now != null)
            {
                Transform wanted = now == liftNode ? liftT : layout;
                if (player.transform.parent != wanted)
                    player.transform.SetParent(wanted, true);
            }

            if (player.IsMoving)
            {
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f)
                {
                    stepTimer = 0.3f;
                    GameAudio.Instance?.PlayStep();
                }
            }

            if (now == lastNode)
                return;
            lastNode = now;
            lastActionAt = Time.time;
            if (State != Phase.Play)
                return;

            if (now == u2)
                LightLantern(0, "Ngọn đèn thứ nhất — soi bóng những mái ngói nơi ta từng thả diều tới khi phố lên đèn.\nThe first lantern — for the rooftops where we flew kites until dusk.");
            else if (now == c2)
                LightLantern(1, "Ngọn đèn thứ hai — soi nhịp cầu son bà qua mỗi sớm đi chợ về.\nThe second — for the crimson bridge she crossed each morning.");
            else if (now == t1)
                StartCoroutine(Ending());
        }

        void LightLantern(int index, string memory)
        {
            Lantern lantern = lanterns[index];
            if (lantern == null || lantern.lit)
                return;
            lantern.Ignite(kit);
            LanternsLit++;
            nightTarget = Mathf.Min(0.75f, LanternsLit * 0.3f);
            foreach (Renderer r in windowGroups[index])
                if (r != null)
                    r.sharedMaterial = windowWarm;
            GameAudio.Instance?.PlaySuccess();
            ClickRipple.Spawn(lantern.shade.position + Vector3.down * 0.3f, new Color(1f, 0.7f, 0.35f, 1f), 1.2f, 0.9f);
            ShowCaption(memory, 6f);
        }

        IEnumerator Ending()
        {
            State = Phase.Ending;
            LightLantern(2, "Ngọn đèn thứ ba — soi sáng cho bất kỳ ai vẫn đang trên đường trở về nhà.\nThe third — for whoever is still on the way home.");
            nightTarget = 1f;
            foreach (Renderer r in fairyLights)
                if (r != null)
                    r.sharedMaterial = fairyOn;
            for (int i = 0; i < 3; i++)
                foreach (Renderer r in windowGroups[i])
                    if (r != null)
                        r.sharedMaterial = windowWarm;
            focusTarget = t1.transform.position + Vector3.up * 0.6f;
            sizeScale = 0.72f;
            if (bell != null)
            {
                bell.DOLocalRotate(new Vector3(0f, 0f, 22f), 0.35f).SetEase(Ease.OutSine).SetLoops(8, LoopType.Yoyo).SetTarget(this)
                    .OnComplete(() => bell.localRotation = Quaternion.identity);
            }
            GameAudio.Instance?.PlayDoor();
            SpawnSkyLanterns(26);
            yield return new WaitForSeconds(3.2f);
            ShowCaption("Hà Nội lại rực rỡ lên đèn. Đâu đó tiếng loa phường rè rè: \"Cơm chín rồi, về nhà thôi!\"\nHanoi lights up once more. A loudspeaker crackles: \"Dinner's ready — come home!\"", 6.5f);
            focusTarget = Vector3.up * 1.6f;
            sizeScale = 1.05f;
            yield return new WaitForSeconds(4.2f);
            State = Phase.End;
            EndAt = Time.time;
            GameAudio.Instance?.PlaySuccess();
        }

        public void Restart()
        {
            DOTween.Kill(this);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void BackToChapter1()
        {
            DOTween.Kill(this);
            SceneManager.LoadScene("Level1");
        }

        void ShowCaption(string text, float seconds)
        {
            Caption = text;
            CaptionAt = Time.time;
            CaptionFor = seconds;
        }

        void ShowToast(string text)
        {
            Toast = text;
            ToastAt = Time.time;
        }

        void UpdateObjective()
        {
            if (player == null)
                return;
            PathNode n = player.CurrentNode;
            string text;
            Transform target;
            if (State == Phase.Intro)
            {
                text = "";
                target = null;
            }
            else if (State != Phase.Play)
            {
                text = "Ba ngọn đèn đã được thắp sáng · The lanterns are lit.";
                target = null;
            }
            else if (n == s0 || n == s1 || n == s2)
            {
                bool low = Level2Rules.LiftAt(LiftHeight, Level2Rules.LiftLow);
                text = low ? "Bước lên thang máy đỏ · Walk onto the red lift" : "Hạ thang máy xuống: kéo hoặc click · Lower the lift";
                target = low ? liftNode.transform : liftT;
            }
            else if (n == liftNode)
            {
                bool high = Level2Rules.LiftAt(LiftHeight, Level2Rules.LiftHigh);
                text = high ? "Bước ra mái nhà ống · Step out onto the rooftop" : "Kéo thang máy lên mái nhà (hoặc click) · Raise the lift";
                target = high ? u0.transform : liftT;
            }
            else if (n == u0 || n == u1 || n == u2)
            {
                if (!lanterns[0].lit)
                {
                    text = "Thắp ngọn đèn lồng thứ nhất ở cuối mái · Light the 1st lantern";
                    target = u2.transform;
                }
                else if (!u2.IsLinked(bNorth) && !u2.IsLinked(bSouth))
                {
                    text = "Xoay cầu đỏ: kéo hoặc click tay quay · Turn the red bridge";
                    target = bridgeT;
                }
                else
                {
                    text = "Đi qua cầu đỏ sang biệt thự · Cross the bridge to the villa";
                    target = c2.transform;
                }
            }
            else if (n == bNorth || n == bPivot || n == bSouth)
            {
                text = c0.IsLinked(bNorth) || c0.IsLinked(bSouth) ? "Bước tiếp sang mái biệt thự · Walk on to the villa roof" : "Lùi lại và xoay cầu hướng về biệt thự · Turn bridge toward villa";
                target = c2.transform;
            }
            else if (n == c0 || n == c1 || n == c2 || n == c3 || n == c4)
            {
                if (!lanterns[1].lit)
                {
                    text = "Thắp ngọn đèn lồng thứ hai trên ban công · Light the 2nd lantern";
                    target = c2.transform;
                }
                else if (!SeamOpen)
                {
                    text = "Đường cụt? Kéo trời hoặc bấm Q/E xoay phố · Turn city with Q/E";
                    target = c4.transform;
                }
                else
                {
                    text = "Ảo giác nối lối sang tháp! Đi về phía chuông · Walk to the bell";
                    target = t1.transform;
                }
            }
            else if (n == t0 || n == t1)
            {
                text = "Thỉnh chuông dưới mái tháp · Ring the bell under the pagoda";
                target = t1.transform;
            }
            else
            {
                text = "";
                target = null;
            }

            Objective = text;
            hintTarget = target;
            if (text != lastObjective)
            {
                lastObjective = text;
                objectiveChangedAt = Time.time;
            }
        }

        void UpdateMarker(float t)
        {
            if (marker == null)
                return;
            bool fresh = State == Phase.Play && Time.time - objectiveChangedAt < 3.5f && Time.time - StartedAt > 2f;
            bool show = hintTarget != null && (HintActive || fresh) && !player.IsMoving;
            marker.gameObject.SetActive(show);
            if (show)
            {
                marker.position = hintTarget.position + Vector3.up * (1.15f + Mathf.Sin(t * 3f) * 0.12f);
                marker.rotation = Quaternion.Euler(0f, t * 90f, 0f) * Quaternion.Euler(45f, 0f, 45f);
                float s = HintActive ? 0.24f + Mathf.Sin(t * 6f) * 0.03f : 0.2f;
                marker.localScale = Vector3.one * s;
            }

            float pulse = HintActive ? 0.5f + 0.5f * Mathf.Sin(t * 5f) : 0f;
            Color lacquer = new Color(0.78f, 0.13f, 0.12f);
            Color hot = new Color(1f, 0.5f, 0.32f);
            bool liftHint = hintTarget == liftT;
            bool bridgeHint = hintTarget == bridgeT;
            liftMat.color = Color.Lerp(lacquer, hot, liftHint ? pulse : 0f);
            bridgeMat.color = Color.Lerp(lacquer, hot, bridgeHint ? pulse : 0f);
        }
    }
}
