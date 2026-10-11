using System.Collections;
using System.Collections.Generic;
using PerspectivePuzzle.Audio;
using PerspectivePuzzle.Level1;
using PerspectivePuzzle.Movement;
using PerspectivePuzzle.Puzzle;
using PerspectivePuzzle.Visual;
using UnityEngine;

namespace PerspectivePuzzle.Level2
{
    public enum Level2Phase
    {
        Intro,
        ToLantern1,
        RotateBridge,
        CrossBridge,
        RotateTower,
        AscendTower,
        Ending
    }

    public class Level2Director : MonoBehaviour
    {
        [SerializeField] PlayerMovement player;
        [SerializeField] RotationBoard board;
        [SerializeField] BuildingRotator rotator;
        [SerializeField] Level2Presenter presenter;
        [SerializeField] IsoRig rig;
        [SerializeField] RouteRibbon ribbon;

        [Header("Key Path Nodes")]
        [SerializeField] PathNode nodeA;
        [SerializeField] PathNode nodeB;
        [SerializeField] PathNode nodeC;
        [SerializeField] PathNode nodeBridge;
        [SerializeField] PathNode nodeF;
        [SerializeField] PathNode nodeRoof;
        [SerializeField] PathNode nodePole;

        [Header("Lanterns")]
        [SerializeField] LanternProp lantern1;
        [SerializeField] LanternProp lantern2;
        [SerializeField] LanternProp lantern3;

        [Header("Visual Elements")]
        [SerializeField] GameObject bridgeVisual;
        [SerializeField] Transform bellProp;

        public const float BridgeYaw = 135f;
        public const float TowerYaw = 225f;
        public const float Tolerance = 14f;

        Level2Phase phase = Level2Phase.Intro;
        Coroutine captionRoutine;
        bool bridgeWasOpen;
        bool towerWasOpen;
        float idleTime;

        public Level2Phase Phase => phase;
        public bool CapturePointer => phase == Level2Phase.Intro || phase == Level2Phase.Ending;
        public bool AllowRotate => phase != Level2Phase.Intro && phase != Level2Phase.Ending
            && player != null && !player.IsMoving && !player.MovementLocked;

        float CurrentYaw => rotator != null ? rotator.CurrentYaw : 0f;

        public void Bind(
            PlayerMovement mover,
            RotationBoard rotation,
            BuildingRotator buildingRotator,
            Level2Presenter view,
            IsoRig cameraRig,
            RouteRibbon routeRibbon,
            PathNode a,
            PathNode b,
            PathNode c,
            PathNode bridgeNode,
            PathNode f,
            PathNode roof,
            PathNode pole,
            LanternProp l1,
            LanternProp l2,
            LanternProp l3,
            GameObject bridgeObj,
            Transform bell)
        {
            player = mover;
            board = rotation;
            rotator = buildingRotator;
            presenter = view;
            rig = cameraRig;
            ribbon = routeRibbon;
            nodeA = a;
            nodeB = b;
            nodeC = c;
            nodeBridge = bridgeNode;
            nodeF = f;
            nodeRoof = roof;
            nodePole = pole;
            lantern1 = l1;
            lantern2 = l2;
            lantern3 = l3;
            bridgeVisual = bridgeObj;
            bellProp = bell;
        }

        void OnEnable()
        {
            if (player != null)
                player.Arrived += OnArrived;
        }

        void OnDisable()
        {
            if (player != null)
                player.Arrived -= OnArrived;
        }

        void Start()
        {
            if (rotator != null)
                rotator.transform.rotation = Quaternion.identity;

            // Spawn sunset atmospheric particles
            Transform root = BuildingRoot();
            if (root != null)
                AtmosphericDust.Create(root, new Color(1.0f, 0.72f, 0.38f, 0.75f));

            if (GameAudio.Instance != null)
                GameAudio.Instance.PlayAmbience();

            if (player != null && nodeA != null)
                player.PlaceAt(nodeA);
            if (player != null)
                player.MovementLocked = true;

            // Initialize lanterns
            if (lantern1 != null) lantern1.SetLit(false);
            if (lantern2 != null) lantern2.SetLit(false);
            if (lantern3 != null) lantern3.SetLit(false);

            // Setup initial path links
            if (nodeA != null && nodeB != null)
                nodeA.Link(nodeB);
            if (nodeB != null && nodeC != null)
                nodeB.Link(nodeC);

            // Initially unlink perspective bridges
            if (nodeC != null && nodeBridge != null)
                nodeC.Unlink(nodeBridge);
            if (nodeBridge != null && nodeF != null)
                nodeBridge.Unlink(nodeF);
            if (nodeF != null && nodeRoof != null)
                nodeF.Link(nodeRoof);
            if (nodeRoof != null && nodePole != null)
                nodeRoof.Unlink(nodePole);

            StartCoroutine(IntroRoutine());
        }

        Transform BuildingRoot()
        {
            if (nodeA == null)
                return rotator != null ? rotator.transform : null;
            Transform root = nodeA.transform;
            while (root.parent != null)
                root = root.parent;
            return root;
        }

        IEnumerator IntroRoutine()
        {
            phase = Level2Phase.Intro;
            if (presenter != null)
            {
                presenter.SetDim(1f);
                presenter.lanternsLit = 0;
            }

            yield return new WaitForSeconds(0.6f);

            // Smooth fade in
            float fade = 1f;
            while (fade > 0f)
            {
                fade -= Time.deltaTime / 1.1f;
                if (presenter != null)
                    presenter.SetDim(Mathf.Max(0f, fade));
                yield return null;
            }

            Say("Hoàng hôn buông sắc ấm lên từng mái ngói phố cổ...", 3.2f);
            yield return new WaitForSeconds(3.0f);

            Say("Hãy bước tới Ban công Đông để thắp sáng ngọn đèn lồng thứ nhất.", 3.5f);
            if (presenter != null)
            {
                presenter.objective = "Bước tới Ban công Đông thắp Đèn Lồng 1";
                presenter.hint = "Nhấn chuột vào Ban công Đông để di chuyển";
                presenter.showRotate = false;
            }

            if (player != null)
                player.MovementLocked = false;
            phase = Level2Phase.ToLantern1;
        }

        void Update()
        {
            if (presenter != null)
            {
                float fps = 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime);
                presenter.performance = $"Chương 2  |  {Mathf.RoundToInt(fps)} fps";
            }

            ObservePerspective();

            // Idle hint counter
            if (Input.GetMouseButton(0) || Input.anyKeyDown)
                idleTime = 0f;
            else
                idleTime += Time.deltaTime;

            if (idleTime > 10f && presenter != null)
            {
                if (phase == Level2Phase.RotateBridge)
                    presenter.hint = "Gợi ý: Xoay nhà về góc 135° để căn chỉnh nhịp cầu";
                else if (phase == Level2Phase.RotateTower)
                    presenter.hint = "Gợi ý: Xoay nhà về góc 225° để mở đường lên Tháp Chuông";
            }
        }

        void ObservePerspective()
        {
            if (rotator == null)
                return;

            float yaw = rotator.CurrentYaw;
            bool bridgeOpen = Mathf.Abs(Mathf.DeltaAngle(yaw, BridgeYaw)) <= Tolerance;
            bool towerOpen = Mathf.Abs(Mathf.DeltaAngle(yaw, TowerYaw)) <= Tolerance;

            // Spatial bridge connection
            if (bridgeOpen != bridgeWasOpen)
            {
                bridgeWasOpen = bridgeOpen;
                if (bridgeOpen)
                {
                    if (nodeC != null && nodeBridge != null)
                        nodeC.Link(nodeBridge);
                    if (nodeBridge != null && nodeF != null)
                        nodeBridge.Link(nodeF);
                    if (GameAudio.Instance != null)
                        GameAudio.Instance.PlayAlign();
                }
                else
                {
                    if (nodeC != null && nodeBridge != null)
                        nodeC.Unlink(nodeBridge);
                    if (nodeBridge != null && nodeF != null)
                        nodeBridge.Unlink(nodeF);
                }
            }

            // Tower ascent connection
            if (towerOpen != towerWasOpen)
            {
                towerWasOpen = towerOpen;
                if (towerOpen)
                {
                    if (nodeRoof != null && nodePole != null)
                        nodeRoof.Link(nodePole);
                    if (GameAudio.Instance != null)
                        GameAudio.Instance.PlayAlign();
                }
                else
                {
                    if (nodeRoof != null && nodePole != null)
                        nodeRoof.Unlink(nodePole);
                }
            }

            // Update presenter rotate banner
            if (presenter != null)
            {
                if (phase == Level2Phase.RotateBridge)
                    presenter.showRotate = !bridgeOpen;
                else if (phase == Level2Phase.RotateTower)
                    presenter.showRotate = !towerOpen;
            }
        }

        public void OnRotateReleased()
        {
            if (rotator == null)
                return;

            float current = CurrentYaw;
            if (phase == Level2Phase.RotateBridge || phase == Level2Phase.CrossBridge)
            {
                if (Mathf.Abs(Mathf.DeltaAngle(current, BridgeYaw)) <= 20f)
                    rotator.SnapTo(BridgeYaw, 0.28f);
            }
            else if (phase == Level2Phase.RotateTower || phase == Level2Phase.AscendTower)
            {
                if (Mathf.Abs(Mathf.DeltaAngle(current, TowerYaw)) <= 20f)
                    rotator.SnapTo(TowerYaw, 0.28f);
            }
        }

        public void OnClick(Ray ray)
        {
            if (phase == Level2Phase.Intro || phase == Level2Phase.Ending || player == null)
                return;

            if (!Physics.Raycast(ray, out RaycastHit hit, 800f))
                return;

            ClickRipple.Spawn(hit.point, new Color(1.0f, 0.78f, 0.35f, 0.85f));
            if (GameAudio.Instance != null)
                GameAudio.Instance.PlayClick();

            PathNode targetNode = hit.collider.GetComponentInParent<PathNode>();
            if (targetNode != null)
            {
                player.TryMoveTo(targetNode);
                return;
            }

            // Check if lantern directly clicked
            LanternProp lantern = hit.collider.GetComponentInParent<LanternProp>();
            if (lantern != null)
            {
                if (lantern == lantern1 && nodeB != null)
                    player.TryMoveTo(nodeB);
                else if (lantern == lantern2 && nodeF != null)
                    player.TryMoveTo(nodeF);
                else if (lantern == lantern3 && nodePole != null)
                    player.TryMoveTo(nodePole);
            }
        }

        void OnArrived(PathNode node)
        {
            if (node == null)
                return;

            // Arrived at Lantern 1 (Node B)
            if ((phase == Level2Phase.ToLantern1 || phase == Level2Phase.RotateBridge) && (node == nodeB || node == nodeC))
            {
                if (lantern1 != null && !lantern1.IsLit)
                {
                    lantern1.LightUp();
                    if (presenter != null)
                    {
                        presenter.lanternsLit = 1;
                        presenter.objective = "Căn chỉnh Nhịp Cầu Không Gian (góc 135°)";
                        presenter.hint = "Kéo chuột xoay nhà đến góc 135° để nhịp cầu liền mạch";
                        presenter.showRotate = true;
                    }
                    Say("Ngọn đèn thứ nhất đã rực sáng! Nhịp cầu không gian đang đợi bạn căn chỉnh...", 3.6f);
                    phase = Level2Phase.RotateBridge;
                }
            }

            // Arrived at Lantern 2 (Node F via Bridge)
            if ((phase == Level2Phase.RotateBridge || phase == Level2Phase.CrossBridge || phase == Level2Phase.RotateTower) && node == nodeF)
            {
                if (lantern2 != null && !lantern2.IsLit)
                {
                    lantern2.LightUp();
                    if (presenter != null)
                    {
                        presenter.lanternsLit = 2;
                        presenter.objective = "Mở đường lên Đỉnh Tháp Chuông (góc 225°)";
                        presenter.hint = "Kéo chuột xoay nhà đến góc 225° để mở đường lên mái thượng";
                        presenter.showRotate = true;
                    }
                    Say("Ngọn đèn thứ hai đã sáng trên Ban công Tây! Giờ hãy mở đường lên Tháp Chuông.", 3.6f);
                    phase = Level2Phase.RotateTower;
                }
            }

            // Arrived at Summit Bell Tower (Node Pole)
            if ((phase == Level2Phase.RotateTower || phase == Level2Phase.AscendTower) && node == nodePole)
            {
                if (lantern3 != null && !lantern3.IsLit)
                {
                    StartCoroutine(EndingSequence());
                }
            }
        }

        IEnumerator EndingSequence()
        {
            phase = Level2Phase.Ending;
            if (player != null)
            {
                player.Halt();
                player.MovementLocked = true;
            }

            if (lantern3 != null)
                lantern3.LightUp();

            if (presenter != null)
            {
                presenter.lanternsLit = 3;
                presenter.showRotate = false;
                presenter.hint = null;
                presenter.SetCaption(null);
            }

            // Ring bell animation / effect
            if (bellProp != null)
            {
                float ringTime = 0f;
                Vector3 origRot = bellProp.localEulerAngles;
                while (ringTime < 1.8f)
                {
                    ringTime += Time.deltaTime;
                    float swing = Mathf.Sin(ringTime * 14f) * 18f * Mathf.Exp(-ringTime * 1.5f);
                    bellProp.localEulerAngles = origRot + new Vector3(0f, 0f, swing);
                    yield return null;
                }
                bellProp.localEulerAngles = origRot;
            }

            if (GameAudio.Instance != null)
                GameAudio.Instance.PlaySuccess();

            Say("Tiếng chuông chiều thanh bình ngân vang khắp phố phường Hà Nội...", 3.5f);
            yield return new WaitForSeconds(3.2f);

            // Grand Victory Screen
            if (presenter != null)
            {
                presenter.ShowEndScreen(
                    "CHƯƠNG 2: HÀ NỘI HOÀNG HÔN HOÀN THÀNH",
                    "Ánh đèn lồng ấm áp thắp sáng ký ức. Hà Nội trong bạn luôn bình yên và tươi đẹp."
                );
            }
        }

        void Say(string line, float seconds)
        {
            if (captionRoutine != null)
                StopCoroutine(captionRoutine);
            captionRoutine = StartCoroutine(CaptionFor(line, seconds));
        }

        IEnumerator CaptionFor(string line, float seconds)
        {
            if (presenter != null)
                presenter.SetCaption(line);
            float t = 0f;
            while (t < seconds)
            {
                if (Input.GetMouseButtonDown(0))
                    break;
                t += Time.deltaTime;
                yield return null;
            }

            if (presenter != null && presenter.caption == line)
                presenter.SetCaption(null);
        }
    }
}
