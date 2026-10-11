using System.Collections;
using System.Collections.Generic;
using PerspectivePuzzle.Audio;
using PerspectivePuzzle.Movement;
using PerspectivePuzzle.Puzzle;
using PerspectivePuzzle.Visual;
using UnityEngine;

namespace PerspectivePuzzle.Level1
{
    public enum Level1Phase
    {
        Intro,
        ToGap,
        NeedRotate,
        ToRoom,
        InRoom,
        Carrying,
        ToExit,
        Round2,
        NeedStair,
        ToRoof,
        AtPole,
        Ending
    }

    public class Level1Director : MonoBehaviour
    {
        [SerializeField] PlayerMovement player;
        [SerializeField] RotationBoard board;
        [SerializeField] BuildingRotator rotator;
        [SerializeField] Level1Presenter presenter;
        [SerializeField] IsoRig rig;
        [SerializeField] RouteRibbon ribbon;
        [SerializeField] PathNode nodeA;
        [SerializeField] PathNode nodeB;
        [SerializeField] PathNode nodeC;
        [SerializeField] PathNode nodeSpeaker;
        [SerializeField] Waypoint markerB;
        [SerializeField] Waypoint markerC;
        [SerializeField] Waypoint markerSpeaker;
        [SerializeField] RoomInteractable cabinet;
        [SerializeField] Transform hand;
        [SerializeField] Transform roomFocus;
        [SerializeField] List<PathNode> openRoute = new();

        [SerializeField] PathNode nodeDoor;
        [SerializeField] PathNode nodeDog;
        [SerializeField] PathNode nodeSafe;
        [SerializeField] PathNode nodeF;
        [SerializeField] PathNode nodeI;
        [SerializeField] PathNode nodeL;
        [SerializeField] PathNode nodeG;
        [SerializeField] PathNode nodeM;
        [SerializeField] PathNode nodePole;
        [SerializeField] Transform poleAnchor;

        PathNode nodeE;
        static readonly string[] LowerSteps = BuildLowerSteps();
        static readonly string[] UpperSteps = CorridorBridge.UpperTreads;
        static readonly string[] PoleSteps =
        {
            "WFloor", "WBench", "WWall", "C041a", "C041b", "Wmid", "Whip", "S1", "S4", "ShelfW", "SlopeS", "PlaneA"
        };
        // Tang3 detours: the east strip and the middle corridor only meet on the north band.
        static readonly string[] FSteps = { "EF1", "EF2" };
        static readonly string[] LSteps = { "EF2", "EF1" };
        static readonly string[] GSteps = { "LG1", "LipE", "LipW" };
        static readonly Vector3 DogRest = new Vector3(3.60f, 5.34f, 4.80f);

        static string[] BuildLowerSteps()
        {
            var steps = new string[CorridorBridge.LowerTreads.Length + 1];
            steps[0] = "A2";
            for (int i = 0; i < CorridorBridge.LowerTreads.Length; i++)
                steps[i + 1] = CorridorBridge.LowerTreads[i];
            return steps;
        }

        bool dogBusy;
        string routeShown = "";
        Transform dogPatrol;
        float dogWalk;
        // Wooden plank over the Tang3 stairwell crack between LipE and LipW.
        Transform crackPlank;
        float plankOpen;
        bool gLinked;

        public string performanceBase;
        public readonly StoryProgress story = new StoryProgress();
        Level1Phase phase = Level1Phase.Intro;
        float idle;
        bool roomHinted;
        Coroutine captionRoutine;
        bool followRoom;
        float roomOrtho = 5.8f;

        /// <summary>House yaw that shows the cupboard once the upper floors are cut away.</summary>
        public const float RoomViewYaw = 280f;

        void FollowRoom()
        {
            if (!followRoom || rig == null || roomFocus == null)
                return;
            rig.MoveFocus(roomFocus.position, roomOrtho);
        }

        public Level1Phase Phase => phase;
        public bool PuzzleOpen { get; private set; }
        public bool CapturePointer => phase == Level1Phase.Intro || PuzzleOpen;
        public bool AllowRotate => !PuzzleOpen && phase != Level1Phase.Intro && phase != Level1Phase.Ending
            && player != null && !player.IsMoving && !player.MovementLocked;

        public void Bind(
            PlayerMovement mover,
            RotationBoard rotation,
            BuildingRotator building,
            Level1Presenter view,
            IsoRig cameraRig,
            RouteRibbon route,
            PathNode a,
            PathNode b,
            PathNode c,
            PathNode speaker,
            Waypoint markB,
            Waypoint markC,
            Waypoint markSpeaker,
            RoomInteractable cabinetProp,
            Transform carryHand,
            Transform focus,
            List<PathNode> routeNodes)
        {
            player = mover;
            board = rotation;
            rotator = building;
            presenter = view;
            rig = cameraRig;
            ribbon = route;
            nodeA = a;
            nodeB = b;
            nodeC = c;
            nodeSpeaker = speaker;
            markerB = markB;
            markerC = markC;
            markerSpeaker = markSpeaker;
            cabinet = cabinetProp;
            hand = carryHand;
            roomFocus = focus;
            openRoute = routeNodes;
        }

        public void BindRoute(
            PathNode door,
            PathNode dog,
            PathNode safe,
            PathNode f,
            PathNode i,
            PathNode l,
            PathNode g,
            PathNode m,
            PathNode pole,
            Transform anchor)
        {
            nodeDoor = door;
            nodeDog = dog;
            nodeSafe = safe;
            nodeF = f;
            nodeI = i;
            nodeL = l;
            nodeG = g;
            nodeM = m;
            nodePole = pole;
            poleAnchor = anchor;
        }

        void OnEnable()
        {
            if (player != null)
                player.Arrived += OnArrived;
            if (board != null)
                board.AlignmentChanged += OnAlignmentChanged;
        }

        void Start()
        {
            if (rotator != null)
                rotator.transform.rotation = Quaternion.identity;
            CorridorBridge.Align(nodeA, nodeB, nodeC, nodeSpeaker, nodeDoor, roomFocus, cabinet);
            CacheClimb();
            HideProxies();
            if (board != null)
            {
                board.KeepPair(nodeB, nodeC);
                // The scene still serializes this link at 90. The corridor yaw is measured at 315.
                board.MatchCorridor(nodeB, nodeC, StoryProgress.CorridorYaw);
            }
            FillOpenRoute();
            HideNode(nodeDog);
            HideNode(nodeSafe);
            HideNode(nodeE);
            HideNode(nodeF);
            HideNode(nodeI);
            HideNode(nodeL);
            HideNode(nodeG);
            HideNode(nodeM);
            HideNode(nodePole);
            if (player != null && nodeA != null)
                player.PlaceAt(nodeA);
            if (player != null)
                player.MovementLocked = true;
            if (markerC != null)
                markerC.Show(false);
            if (markerSpeaker != null)
                markerSpeaker.Show(false);
            if (cabinet != null)
                cabinet.SetSpeakerReady(false);
            StyleCassette();
            ResolveHand();

            Transform root = BuildingRoot();
            if (root != null)
                AtmosphericDust.Create(root, new Color(0.95f, 0.82f, 0.55f, 0.6f));
            if (GameAudio.Instance != null)
                GameAudio.Instance.PlayAmbience();

            StartCoroutine(Intro());
        }

        void OnDisable()
        {
            if (player != null)
                player.Arrived -= OnArrived;
            if (board != null)
                board.AlignmentChanged -= OnAlignmentChanged;
        }

        void OnAlignmentChanged(bool opened)
        {
            if (opened && GameAudio.Instance != null)
                GameAudio.Instance.PlayAlign();
        }

        void Update()
        {
            if (presenter != null)
            {
                float fps = 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime);
                presenter.performance = performanceBase + "   " + Mathf.RoundToInt(fps) + " fps";
            }

            ObserveYaw();
            WalkDog();
            TickPlank();

            bool waiting = phase == Level1Phase.NeedRotate || phase == Level1Phase.InRoom
                || phase == Level1Phase.NeedStair || phase == Level1Phase.Round2 || phase == Level1Phase.ToRoof;
            if (!waiting || GameInput.GetMouseButton(0) || GameInput.anyKeyDown)
                idle = 0f;
            else
                idle += Time.deltaTime;

            if ((phase == Level1Phase.NeedRotate || phase == Level1Phase.NeedStair || phase == Level1Phase.Round2
                    || phase == Level1Phase.ToRoof)
                && idle > 12f && presenter != null)
                presenter.hint = "Có lẽ lối đi không nằm ở phía trước... Hãy thử xoay nhà · Perhaps the path isn't ahead";

            WatchDog();
            FollowRoom();

            if (phase == Level1Phase.InRoom && idle > 35f && !roomHinted && cabinet != null && !cabinet.Used)
            {
                roomHinted = true;
                Say("Chiếc tủ nhỏ vẫn còn cất giữ vài món đồ của bà...\nThe cupboard still keeps a few of her things.", 4f);
                if (cabinet.transform != null)
                    StartCoroutine(PulseOnce(cabinet.transform));
            }

            ApplyCorridorGate();
        }

        public static float ReleaseSnapYaw(Level1Phase phase)
        {
            if (phase == Level1Phase.Round2 || phase == Level1Phase.NeedStair || phase == Level1Phase.ToRoof)
                return StoryProgress.StairYaw;
            return StoryProgress.CorridorYaw;
        }

        public bool CorridorGateOpen(float yaw)
        {
            return story.CorridorOpen(yaw);
        }

        public void OnRotateReleased()
        {
            if (rotator == null)
                return;
            float target = ReleaseSnapYaw(phase);
            if (phase == Level1Phase.ToRoof && !string.IsNullOrEmpty(story.NextRoof))
                target = StoryProgress.YawForRoof(story.NextRoof);
            if (Mathf.Abs(Mathf.DeltaAngle(CurrentYaw(), target)) <= 18f)
                rotator.SnapTo(target, 0.28f);
        }

        public void OnClick(Ray ray)
        {
            if (phase == Level1Phase.Intro || phase == Level1Phase.Ending || player == null)
                return;
            if (phase == Level1Phase.AtPole)
            {
                PlaceOnPole();
                return;
            }
            if (!Physics.Raycast(ray, out RaycastHit hit, 800f))
                return;

            ClickRipple.Spawn(hit.point, new Color(0.95f, 0.78f, 0.35f, 0.85f));
            if (GameAudio.Instance != null)
                GameAudio.Instance.PlayClick();

            RoomInteractable prop = hit.collider.GetComponentInParent<RoomInteractable>();
            if (prop != null)
            {
                HandleProp(prop);
                return;
            }

            PathNode node = hit.collider.GetComponentInParent<PathNode>();
            if (node == null)
                return;
            if (phase == Level1Phase.Round2 || phase == Level1Phase.NeedStair || phase == Level1Phase.ToRoof)
            {
                string next = story.NextRoof;
                bool roofClick = next != null && node == RoofNode(next) && story.RoofPointOpen(next, CurrentYaw());
                bool dogClick = node == nodeDog && story.Speaker == SpeakerPhase.Held && !story.StairOpen(CurrentYaw());
                if (!roofClick && !dogClick)
                    return;
            }

            player.TryMoveTo(node);
        }

        void HandleProp(RoomInteractable prop)
        {
            if (phase == Level1Phase.Carrying)
                return;
            if (phase != Level1Phase.InRoom)
            {
                Say("Căn phòng vẫn còn ở quá xa tầm với.\nThe room is still out of reach.", 2.2f);
                return;
            }

            if (prop.kind == InteractKind.Clue)
            {
                if (!story.TryHearClue(prop.line))
                    return;
                prop.Used = true;
                Say(prop.line, 3.2f);
                return;
            }

            if (prop.kind == InteractKind.Cabinet)
            {
                StartCoroutine(OpenCabinet(prop));
                return;
            }

            if (prop.kind == InteractKind.Speaker && (prop.SpeakerReady || story.Speaker == SpeakerPhase.Ready))
                player.TryMoveTo(nodeSpeaker);
        }

        IEnumerator OpenCabinet(RoomInteractable prop)
        {
            if (prop != null && prop.Used)
                yield break;

            player.MovementLocked = true;
            PuzzleOpen = true;
            bool taken = false;
            CabinetView view = CabinetView.Open(() => taken = true);
            while (view != null && view.IsOpen)
                yield return null;
            PuzzleOpen = false;
            if (!taken)
            {
                player.MovementLocked = false;
                yield break;
            }

            if (prop != null)
                prop.Used = true;
            story.Speaker = SpeakerPhase.Ready;
            if (cabinet != null)
                cabinet.SetSpeakerReady(true);
            player.MovementLocked = false;
            Pickup();
        }

        void OnArrived(PathNode node)
        {
            if (phase == Level1Phase.ToGap && node == nodeB)
                StartCoroutine(ReachedGap());
            else if ((phase == Level1Phase.ToRoom || phase == Level1Phase.NeedRotate) && node == nodeC)
                StartCoroutine(EnteredRoom());
            else if (phase == Level1Phase.InRoom && node == nodeSpeaker)
                Pickup();
            else if ((phase == Level1Phase.ToExit || phase == Level1Phase.Carrying) && node == nodeDoor)
                EnterRound2();
            else if (node == nodeDog && story.Speaker == SpeakerPhase.Held && !story.StairOpen(CurrentYaw()))
                StartCoroutine(DogFall());
            else if (node == nodeM || node == nodePole)
                FinishRoof(node);
            else if (node == nodeE || node == nodeF || node == nodeI || node == nodeL || node == nodeG)
                FinishRoof(node);
        }

        void ApplyCorridorGate()
        {
            bool open = nodeB != null && nodeC != null && nodeB.IsLinked(nodeC);
            if (ribbon != null)
            {
                if (RoundTwo())
                    ribbon.DrawRoundTwo(RoundTwoLeg());
                else
                    ribbon.Draw(openRoute, nodeB, nodeC, open);
            }
            if (markerC != null)
                markerC.Show(open && !RoundTwo() && phase != Level1Phase.Intro && phase != Level1Phase.Ending);
        }

        readonly List<PathNode> blueLeg = new();

        bool RoundTwo()
        {
            return phase == Level1Phase.ToExit || phase == Level1Phase.Carrying || Round2Stair()
                || phase == Level1Phase.AtPole;
        }

        /// <summary>
        /// Blue leg: linked nodes from the player to the target that is open right now.
        /// </summary>
        List<PathNode> RoundTwoLeg()
        {
            blueLeg.Clear();
            if (player == null || player.CurrentNode == null)
                return blueLeg;
            PathNode target = null;
            if (phase == Level1Phase.ToExit || phase == Level1Phase.Carrying)
                target = nodeDoor;
            else if (Round2Stair())
            {
                string next = story.NextRoof;
                PathNode roof = next != null ? RoofNode(next) : null;
                if (roof != null && story.RoofPointOpen(next, CurrentYaw()))
                    target = roof;
                else if (DogTrapLive(CurrentYaw()) && !dogBusy)
                    target = nodeDog;
            }
            if (target != null)
                NodePath.TryFind(player.CurrentNode, target, blueLeg);
            return blueLeg;
        }

        IEnumerator Intro()
        {
            phase = Level1Phase.Intro;
            while (presenter != null && presenter.ShowStartMenu)
            {
                yield return null;
            }

            if (presenter != null)
            {
                presenter.SetDim(0.94f);
                presenter.SetObjective("Tìm cuộn băng cassette của Bà\nFind Grandma's cassette tape");
                presenter.hint = "Click chuột để bỏ qua · Click to skip";
            }

            string[] lines =
            {
                "Ngôi nhà này sắp sửa biến mất vào quá khứ...\nThis home is about to disappear.",
                "Nhưng bà — cựu phát thanh viên Hà Nội xưa — vẫn muốn nghe lại một thanh âm quen thuộc.\nGrandma, a former radio broadcaster from old Hanoi, still longs to hear one familiar voice.",
                "Cuộn băng cassette của bà vẫn còn đâu đó trong căn nhà này.\nGrandma's cassette tape is still somewhere inside this home.",
                "Hãy tìm cuộn băng và mang lên cột loa trên tầng mái.\nFind it. Take it to the loudspeaker post on the rooftop."
            };

            for (int i = 0; i < lines.Length; i++)
            {
                if (presenter != null)
                    presenter.SetCaption(lines[i]);
                float t = 0f;
                const float hold = 2.6f;
                while (t < hold)
                {
                    if (GameInput.GetMouseButtonDown(0))
                        break;
                    float edge = 0.35f;
                    float alpha = t < edge ? t / edge : hold - t < edge ? (hold - t) / edge : 1f;
                    if (presenter != null)
                        presenter.captionAlpha = alpha;
                    t += Time.deltaTime;
                    yield return null;
                }
            }

            if (presenter != null)
            {
                presenter.captionAlpha = 1f;
                presenter.SetCaption(null);
            }
            float dim = presenter != null ? presenter.dim : 0f;
            while (dim > 0f)
            {
                dim -= Time.deltaTime * 1.15f;
                if (presenter != null)
                    presenter.SetDim(dim);
                yield return null;
            }

            phase = Level1Phase.ToGap;
            if (player != null)
                player.MovementLocked = false;
            if (presenter != null)
            {
                presenter.SetObjective("Tìm cuộn băng cassette của Bà và mang lên cột loa trên mái\nFind Grandma's cassette tape and bring it to the rooftop loudspeaker");
                presenter.hint = "Kéo để xoay nhà, click điểm vàng để đi · Drag to turn, click to walk";
                presenter.showRotate = false;
            }
        }

        IEnumerator ReachedGap()
        {
            player.MovementLocked = true;
            if (presenter != null)
                presenter.showRotate = false;
            yield return new WaitForSeconds(1f);

            bool open = nodeB != null && nodeC != null && nodeB.IsLinked(nodeC);
            if (!open)
            {
                phase = Level1Phase.NeedRotate;
                Say("Có những con đường chỉ mở ra khi ta đổi góc nhìn.\nSome paths only appear when we see this place from a different perspective.", 4.5f);
                if (presenter != null)
                {
                    presenter.showRotate = true;
                    presenter.hint = "Kéo ngang để xoay nhà · Drag left / right to turn the house";
                }
            }
            else
            {
                phase = Level1Phase.ToRoom;
            }

            player.MovementLocked = false;
        }

        IEnumerator EnteredRoom()
        {
            phase = Level1Phase.InRoom;
            player.MovementLocked = true;
            if (presenter != null)
                presenter.showRotate = false;
            ShowUpperFloors(false);
            // At the corridor yaw the room sits under the Tang3 slab and the cupboard faces away.
            // Turn the house so the cupboard faces the camera.
            if (rotator != null)
                rotator.SnapTo(RoomViewYaw, 0.7f);
            if (rig != null)
                roomOrtho = rig.HomeOrtho * 0.72f;
            followRoom = true;

            yield return new WaitForSeconds(0.75f);
            if (presenter != null)
            {
                presenter.SetCaption("Bà từng có một cuộn băng cassette. Chắc vẫn còn quanh quẩn đâu đây...\nGrandma used to have a cassette tape. It must still be somewhere around here.");
                presenter.SetObjective("Tìm cuộn băng cassette trong phòng\nFind Grandma's cassette tape in the room");
            }

            float t = 0f;
            while (t < 3f)
            {
                if (GameInput.GetMouseButtonDown(0))
                    break;
                t += Time.deltaTime;
                yield return null;
            }

            if (presenter != null)
            {
                presenter.SetCaption(null);
                presenter.hint = "Khám phá các đồ vật trong phòng · Tap things in the room";
            }
            player.MovementLocked = false;
            idle = 0f;
        }

        Transform ResolveHand()
        {
            if (hand != null)
                return hand;
            if (player != null)
            {
                foreach (var t in player.GetComponentsInChildren<Transform>())
                {
                    if (t.name.EndsWith("RightHand") || t.name.Contains("RightHand"))
                    {
                        hand = t;
                        return hand;
                    }
                }
            }
            return null;
        }

        void Pickup()
        {
            Transform carryHand = ResolveHand();
            RoomInteractable speaker = cabinet != null ? cabinet.speaker : null;
            if (speaker != null)
            {
                Transform parent = carryHand != null ? carryHand : (player != null ? player.transform : null);
                if (parent != null)
                {
                    speaker.transform.SetParent(parent, true);
                    speaker.transform.localPosition = carryHand != null ? Vector3.zero : new Vector3(0.2f, 0.8f, 0.2f);
                    speaker.transform.localScale = Vector3.one * 0.35f;
                }
                Collider col = speaker.GetComponent<Collider>();
                if (col != null)
                    col.enabled = false;
            }

            if (!story.TryPickup())
                return;

            ClipActor actor = player != null ? player.GetComponent<ClipActor>() : null;
            if (actor != null)
                actor.Carrying = true;

            phase = Level1Phase.ToExit;
            if (markerSpeaker != null)
                markerSpeaker.Show(false);
            if (nodeC != null && nodeDoor != null)
                nodeC.Link(nodeDoor);
            // Round 2 walks the real Tang2 walkway: door, stair head, landing.
            PathNode hall = FindClimb("Hall");
            if (nodeDoor != null && hall != null)
                nodeDoor.Link(hall);
            if (nodeB != null && hall != null)
                nodeB.Link(hall);
            if (nodeDoor != null && nodeDog != null)
                nodeDoor.Unlink(nodeDog);
            ShowNode(nodeDoor, true);
            HideNode(nodeDog);
            if (presenter != null)
            {
                presenter.SetCaption("Nếu bạn còn nhớ... bà thường cất nó ở nơi cao nhất của toà nhà.\nIf you remember… Grandma used to keep it at the highest place in the building.");
                presenter.SetObjective("Đưa cuộn băng về đúng vị trí trên tầng mái\nReturn the cassette tape to the rooftop loudspeaker");
                presenter.hint = "Bước ra khỏi phòng · Click the way out of the room";
                presenter.showRotate = false;
            }
        }

        void EnterRound2()
        {
            phase = Level1Phase.Round2;
            HideNode(nodeDoor);
            followRoom = false;
            ShowUpperFloors(true);
            if (rig != null)
                rig.MoveFocus(rig.HomeFocus, rig.HomeOrtho);
            if (presenter != null)
            {
                presenter.SetCaption(null);
                presenter.hint = "Lên cầu thang hướng về sân thượng · Take the stair up to the roof";
                presenter.showRotate = false;
            }
        }

        IEnumerator DogFall()
        {
            if (dogBusy)
                yield break;
            dogBusy = true;
            phase = Level1Phase.NeedStair;
            if (player != null)
            {
                player.Halt();
                player.MovementLocked = true;
            }

            StartCoroutine(RunDog());
            if (GameAudio.Instance != null)
                GameAudio.Instance.PlayDogBark();
            ClipActor actor = player != null ? player.GetComponent<ClipActor>() : null;
            if (actor != null)
                actor.PlayFall();
            story.DogFail();
            yield return new WaitForSeconds(1.4f);
            if (player != null && nodeA != null)
                player.PlaceAt(nodeA);
            Say("Có lẽ con đường vừa rồi không dành cho chúng ta...\nPerhaps that path wasn't meant for us.", 3.5f);
            if (presenter != null)
            {
                presenter.hint = "Xoay nhà để tìm lối đi an toàn tránh chó · Rotate to find a safe path";
                presenter.showRotate = true;
            }
            yield return new WaitForSeconds(1.6f);
            if (player != null)
                player.MovementLocked = false;
            phase = Level1Phase.Round2;
            dogBusy = false;
            routeShown = "";
        }

        IEnumerator RunDog()
        {
            Transform root = BuildingRoot();
            if (root == null)
                yield break;
            if (dogPatrol != null)
                dogPatrol.gameObject.SetActive(false);
            Transform go = MakeDog("DogRunner", root);
            // Down CauThang2 toward the player on the trap tread.
            int[] treads = { 8, 7, 6, 5, 4, 3 };
            for (int i = 1; i < treads.Length; i++)
            {
                Vector3 from = CorridorBridge.UpperTread(treads[i - 1]) - Vector3.up * 0.04f;
                Vector3 to = CorridorBridge.UpperTread(treads[i]) - Vector3.up * 0.04f;
                float t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime / 0.14f;
                    if (go == null)
                        yield break;
                    go.localPosition = Vector3.Lerp(from, to, Mathf.Clamp01(t));
                    go.localRotation = Quaternion.LookRotation(to - from - Vector3.up * (to.y - from.y), Vector3.up);
                    yield return null;
                }
            }

            yield return new WaitForSeconds(0.6f);
            if (go != null)
                Destroy(go.gameObject);
        }

        /// <summary>
        /// Low brown dog from a few primitives. No colliders, so it never pushes the player or eats clicks.
        /// Pivot is at the paws.
        /// </summary>
        static Transform MakeDog(string name, Transform parent)
        {
            var dog = new GameObject(name).transform;
            dog.SetParent(parent, false);
            var brown = new Color(0.47f, 0.30f, 0.17f);
            var dark = new Color(0.25f, 0.16f, 0.09f);
            Part(dog, PrimitiveType.Capsule, new Vector3(0f, 0.28f, 0f), new Vector3(90f, 0f, 0f), new Vector3(0.22f, 0.26f, 0.22f), brown, true);
            Part(dog, PrimitiveType.Sphere, new Vector3(0f, 0.40f, 0.30f), Vector3.zero, new Vector3(0.20f, 0.19f, 0.22f), brown, false);
            Part(dog, PrimitiveType.Sphere, new Vector3(0f, 0.37f, 0.42f), Vector3.zero, new Vector3(0.09f, 0.08f, 0.10f), dark, false);
            Part(dog, PrimitiveType.Cube, new Vector3(-0.07f, 0.50f, 0.27f), new Vector3(0f, 0f, 20f), new Vector3(0.05f, 0.09f, 0.04f), dark, false);
            Part(dog, PrimitiveType.Cube, new Vector3(0.07f, 0.50f, 0.27f), new Vector3(0f, 0f, -20f), new Vector3(0.05f, 0.09f, 0.04f), dark, false);
            Part(dog, PrimitiveType.Cube, new Vector3(0f, 0.36f, -0.28f), new Vector3(-40f, 0f, 0f), new Vector3(0.04f, 0.04f, 0.16f), brown, false);
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -0.07f : 0.07f;
                float z = i < 2 ? 0.13f : -0.13f;
                Part(dog, PrimitiveType.Cube, new Vector3(x, 0.09f, z), Vector3.zero, new Vector3(0.05f, 0.18f, 0.05f), dark, false);
            }
            return dog;
        }

        static void Part(Transform dog, PrimitiveType shape, Vector3 pos, Vector3 euler, Vector3 scale, Color color, bool body)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = body ? "Body" : shape.ToString();
            go.transform.SetParent(dog, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
            Collider col = go.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
            Renderer rend = go.GetComponent<Renderer>();
            if (rend != null)
                rend.material.color = color;
        }

        void FinishRoof(PathNode node)
        {
            if (node == null || story.Speaker != SpeakerPhase.Held)
                return;
            string name = node == nodePole ? "M" : node.name;
            if (name == "M")
            {
                if (!story.ReachRoof("M"))
                    return;
                ArriveAtRoof();
                return;
            }

            if (!story.ReachRoof(name))
                return;
            phase = Level1Phase.ToRoof;
            routeShown = "";
            if (presenter != null)
            {
                presenter.showRotate = true;
                presenter.hint = "Kéo ngang để xoay nhà · Drag to turn the house";
            }
        }

        void ArriveAtRoof()
        {
            if (story.Speaker != SpeakerPhase.Held)
                return;
            phase = Level1Phase.AtPole;
            if (player != null)
                player.MovementLocked = true;
            if (presenter != null)
                presenter.SetCaption("Ngày xưa, đứng từ đây là cả khu phố đều có thể lắng nghe tiếng loa của bà...\nBack then, standing here was enough for the whole neighborhood to hear.");
            ShowNode(nodePole, true);
        }

        public static void MountOnPole(Transform speaker, Transform pole)
        {
            if (speaker == null || pole == null)
                return;
            speaker.SetParent(pole, false);
            Vector3 scale = pole.lossyScale;
            const float size = 0.35f;
            speaker.localPosition = new Vector3(0f, DivideScale(CorridorBridge.SpeakerRest.y, scale.y), 0f);
            speaker.localRotation = Quaternion.identity;
            speaker.localScale = new Vector3(
                DivideScale(size, scale.x),
                DivideScale(size, scale.y),
                DivideScale(size, scale.z));
        }

        static float DivideScale(float value, float scale)
        {
            return Mathf.Abs(scale) < 0.0001f ? value : value / scale;
        }

        void StyleCassette()
        {
            RoomInteractable item = cabinet != null ? cabinet.speaker : null;
            if (item == null)
                return;
            item.gameObject.name = "Cassette";
            item.transform.localScale = new Vector3(0.28f, 0.05f, 0.18f);
            Renderer rend = item.GetComponent<Renderer>();
            if (rend != null)
                rend.material.color = new Color(0.1f, 0.09f, 0.08f);
        }

        public void PlaceOnPole()
        {
            if (phase != Level1Phase.AtPole || !story.TryPlace())
                return;
            StartCoroutine(EndingRoutine());
        }

        public const string FinalLine = "Có những điều đã rời xa ngôi nhà này. Nhưng ký ức luôn biết lối trở về.\nSome things have disappeared from this home. But memories still know the way back.";

        IEnumerator EndingRoutine()
        {
            phase = Level1Phase.Ending;
            if (player != null)
            {
                player.Halt();
                player.MovementLocked = true;
            }
            if (presenter != null)
            {
                presenter.showRotate = false;
                presenter.hint = null;
                presenter.SetCaption(null);
            }

            RoomInteractable speaker = cabinet != null ? cabinet.speaker : null;
            Transform mount = nodePole != null ? nodePole.transform : poleAnchor;
            if (speaker != null && mount != null)
            {
                // Short hand-to-pole move. No reach clip exists.
                Vector3 from = speaker.transform.position;
                MountOnPole(speaker.transform, mount);
                Vector3 rest = speaker.transform.localPosition;
                speaker.transform.position = from;
                float t = 0f;
                Vector3 startLocal = speaker.transform.localPosition;
                while (t < 1f)
                {
                    t = Mathf.Min(1f, t + Time.deltaTime / 0.55f);
                    float e = t * t * (3f - 2f * t);
                    speaker.transform.localPosition = Vector3.Lerp(startLocal, rest, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * 0.25f;
                    yield return null;
                }
            }

            ClipActor actor = player != null ? player.GetComponent<ClipActor>() : null;
            if (actor != null)
                actor.Carrying = false;

            float clipLength = PlayStinger();
            if (rig != null)
                rig.MoveFocus(rig.lookAt, rig.ortho * 1.35f);
            yield return new WaitForSeconds(Mathf.Min(1.2f, clipLength));
            if (presenter != null)
                presenter.SetCaption(FinalLine);
            yield return new WaitForSeconds(Mathf.Max(1.6f, clipLength - 1.2f));

            float dim = presenter != null ? presenter.dim : 0f;
            while (presenter != null && dim < 1f)
            {
                dim = Mathf.Min(1f, dim + Time.deltaTime / 1.4f);
                presenter.SetDim(dim);
                yield return null;
            }

            if (presenter != null)
                presenter.ShowEndScreen(FinalLine, "Cảm ơn bạn đã mang tiếng nói của bà trở về  ·  Thank you for bringing her voice home.");
        }

        IEnumerator PulseOnce(Transform target)
        {
            Vector3 baseScale = target.localScale;
            float t = 0f;
            while (t < 1f && target != null)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / 0.6f);
                target.localScale = baseScale * (1f + 0.06f * Mathf.Sin(t * Mathf.PI));
                yield return null;
            }

            if (target != null)
                target.localScale = baseScale;
        }

        float PlayStinger()
        {
            AudioSource source = GetComponent<AudioSource>();
            if (source == null)
                source = gameObject.AddComponent<AudioSource>();
            source.spatialBlend = 0f;
            AudioClip clip = RadioStinger.Create();
            source.PlayOneShot(clip);
            return clip.length;
        }

        float CurrentYaw()
        {
            return board != null ? board.CurrentYaw : 0f;
        }

        bool Round2Stair()
        {
            return phase == Level1Phase.Round2 || phase == Level1Phase.NeedStair || phase == Level1Phase.ToRoof;
        }

        /// <summary>
        /// The trap is live only on the way to E, while the stair yaw is not open.
        /// </summary>
        bool DogTrapLive(float yaw)
        {
            bool held = story.Speaker == SpeakerPhase.Held;
            return held && Round2Stair() && story.RoofReached == 0 && !story.StairOpen(yaw);
        }

        void WalkDog()
        {
            bool show = Round2Stair() || phase == Level1Phase.ToExit;
            Transform root = BuildingRoot();
            if (!show || root == null || dogBusy)
            {
                if (dogPatrol != null && !dogBusy)
                    dogPatrol.gameObject.SetActive(false);
                return;
            }

            if (dogPatrol == null)
                dogPatrol = MakeDog("DogPatrol", root);
            dogPatrol.gameObject.SetActive(true);

            Vector3 target;
            Vector3 face;
            bool safe = story.StairOpen(CurrentYaw()) || story.RoofReached > 0;
            if (safe)
            {
                // Dog Trigger of the old route is off. The dog stays in the house, away from the stair.
                target = DogRest;
                face = Vector3.back;
            }
            else
            {
                // Pace CauThang2 treads 1..8.
                dogWalk += Time.deltaTime * 1.2f;
                const float span = 14f;
                float loop = dogWalk % span;
                float along = 1f + (loop <= 7f ? loop : span - loop);
                int i0 = Mathf.Clamp(Mathf.FloorToInt(along), 1, 8);
                int i1 = Mathf.Min(i0 + 1, 8);
                Vector3 a = CorridorBridge.UpperTread(i0) - Vector3.up * 0.04f;
                Vector3 b = CorridorBridge.UpperTread(i1) - Vector3.up * 0.04f;
                target = Vector3.Lerp(a, b, along - i0);
                face = loop <= 7f ? Vector3.back : Vector3.forward;
            }

            // The patrol target already advances tread by tread each frame. Switching between the
            // stair patrol and the room rest is a jump, never a glide across the stairwell void.
            dogPatrol.localPosition = target;
            dogPatrol.localRotation = Quaternion.LookRotation(face, Vector3.up);
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

        void ObserveYaw()
        {
            if (board == null || player != null && player.IsMoving)
                return;
            float yaw = board.CurrentYaw;
            story.Observe(yaw);
            bool held = story.Speaker == SpeakerPhase.Held;
            bool climbing = held && Round2Stair();
            bool trap = DogTrapLive(yaw);

            HideRoofMarks();
            UnlinkRoof();
            PathNode hall = FindClimb("Hall");
            if (trap && !dogBusy)
                LinkChain(hall, FindClimb("Up1"), FindClimb("Up2"), FindClimb("Up3"), nodeDog);
            ShowNode(nodeDog, trap && !dogBusy);
            string next = story.NextRoof;
            bool openPoint = climbing && next != null && story.RoofPointOpen(next, yaw);
            if (openPoint)
            {
                LinkRoof(next);
                ShowNode(RoofNode(next), true);
                if (phase == Level1Phase.Round2 || phase == Level1Phase.NeedStair)
                    phase = Level1Phase.ToRoof;
            }

            string shown = trap ? "dog" : openPoint ? next : "";
            if (climbing && shown != routeShown)
            {
                routeShown = shown;
                if (presenter != null)
                {
                    presenter.showRotate = !openPoint;
                    if (openPoint)
                        presenter.hint = "Click điểm vàng để bước tiếp · Click the gold point to advance";
                    else if (trap && story.RoofReached == 0 && phase == Level1Phase.Round2)
                        presenter.hint = "Lên cầu thang hướng về sân thượng · Take the stair up to the roof";
                    else
                        presenter.hint = "Kéo để xoay nhà · Drag to turn the house";
                }
            }
        }

        void WatchDog()
        {
            if (dogBusy || player == null || nodeDog == null || player.CurrentNode != nodeDog)
                return;
            if (story.Speaker != SpeakerPhase.Held || story.StairOpen(CurrentYaw()))
                return;
            if (phase != Level1Phase.Round2 && phase != Level1Phase.NeedStair && phase != Level1Phase.ToRoof
                && phase != Level1Phase.ToExit)
                return;
            StartCoroutine(DogFall());
        }

        void CacheClimb()
        {
            nodeE = FindClimb("E");
        }

        PathNode FindClimb(string name)
        {
            if (nodeA == null)
                return null;
            Transform child = nodeA.transform.parent != null ? nodeA.transform.parent.Find(name) : null;
            return child != null ? child.GetComponent<PathNode>() : null;
        }

        void FillOpenRoute()
        {
            if (openRoute == null)
                return;
            openRoute.Clear();
            AddRoute(nodeA);
            for (int i = 0; i < LowerSteps.Length; i++)
                AddRoute(FindClimb(LowerSteps[i]));
            AddRoute(FindClimb("Stair"));
            AddRoute(nodeB);
        }

        readonly List<Renderer> upperFloors = new();

        /// <summary>
        /// Cutaway for the Tầng 2 room: the room ceiling is the Tầng 3 slab (bottom ~7.05), so from every yaw
        /// the TuBuffet sits under it. While the player is inside, everything wholly above 7.0 is hidden.
        /// </summary>
        void ShowUpperFloors(bool visible)
        {
            Transform root = BuildingRoot();
            Transform house = root != null ? root.Find("House") : null;
            if (house == null)
                return;
            if (!visible)
            {
                upperFloors.Clear();
                float cut = root.position.y + 7.0f;
                Renderer[] all = house.GetComponentsInChildren<Renderer>();
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i].enabled && all[i].bounds.min.y >= cut)
                    {
                        all[i].enabled = false;
                        upperFloors.Add(all[i]);
                    }
                }
                return;
            }

            for (int i = 0; i < upperFloors.Count; i++)
            {
                if (upperFloors[i] != null)
                    upperFloors[i].enabled = true;
            }
            upperFloors.Clear();
        }

        /// <summary>
        /// Room click targets are invisible boxes over the real TuBuffet, table and fan of the mesh.
        /// </summary>
        void HideProxies()
        {
            Transform root = BuildingRoot();
            if (root == null)
                return;
            string[] names = { "Table", "Shelf", "Cabinet" };
            for (int i = 0; i < names.Length; i++)
            {
                Transform prop = root.Find(names[i]);
                if (prop == null)
                    continue;
                Renderer view = prop.GetComponent<Renderer>();
                if (view != null)
                    view.enabled = false;
                for (int c = 0; c < prop.childCount; c++)
                {
                    Transform child = prop.GetChild(c);
                    if (child.GetComponent<RoomInteractable>() != null)
                        continue;
                    Renderer piece = child.GetComponent<Renderer>();
                    if (piece != null)
                        piece.enabled = false;
                }
            }

            // TuBuffet is 0.74 x 1.30 x 2.10 m (x, y, z). Fit the cabinet's click box to it.
            if (cabinet != null)
            {
                BoxCollider box = cabinet.GetComponent<BoxCollider>();
                Vector3 scale = cabinet.transform.lossyScale;
                if (box != null && Mathf.Abs(scale.x) > 0.001f && Mathf.Abs(scale.y) > 0.001f && Mathf.Abs(scale.z) > 0.001f)
                {
                    cabinet.transform.rotation = root.rotation;
                    box.center = Vector3.zero;
                    box.size = new Vector3(0.80f / scale.x, 1.30f / scale.y, 2.10f / scale.z);
                }
            }
        }

        void AddRoute(PathNode node)
        {
            if (node != null)
                openRoute.Add(node);
        }

        PathNode RoofNode(string point)
        {
            if (point == "E")
                return nodeE;
            if (point == "F")
                return nodeF;
            if (point == "I")
                return nodeI;
            if (point == "L")
                return nodeL;
            if (point == "G")
                return nodeG;
            if (point == "M")
                return nodeM;
            return null;
        }

        void HideRoofMarks()
        {
            HideNode(nodeSafe);
            HideNode(nodeE);
            HideNode(nodeF);
            HideNode(nodeI);
            HideNode(nodeL);
            HideNode(nodeG);
            HideNode(nodeM);
            if (phase != Level1Phase.AtPole)
                HideNode(nodePole);
        }

        void UnlinkRoof()
        {
            PathNode hall = FindClimb("Hall");
            UnlinkChain(ClimbChain(hall, UpperSteps, nodeE));
            if (nodeDog != null)
            {
                PathNode up3 = FindClimb("Up3");
                if (up3 != null)
                    up3.Unlink(nodeDog);
            }
            UnlinkChain(ClimbChain(nodeE, FSteps, nodeF));
            UnlinkChain(nodeF, nodeI);
            UnlinkChain(ClimbChain(nodeI, LSteps, nodeL));
            UnlinkChain(ClimbChain(nodeL, GSteps, nodeG));
            gLinked = false;
            UnlinkChain(PoleChain());
            if (nodeDoor != null)
            {
                nodeDoor.Unlink(nodeDog);
                nodeDoor.Unlink(nodeSafe);
                nodeDoor.Unlink(nodeE);
            }
        }

        void LinkRoof(string point)
        {
            PathNode hall = FindClimb("Hall");
            if (point == "E")
                LinkChain(ClimbChain(hall, UpperSteps, nodeE));
            else if (point == "F")
                LinkChain(ClimbChain(nodeE, FSteps, nodeF));
            else if (point == "I")
                LinkChain(nodeF, nodeI);
            else if (point == "L")
                LinkChain(ClimbChain(nodeI, LSteps, nodeL));
            else if (point == "G")
            {
                LinkChain(ClimbChain(nodeL, GSteps, nodeG));
                gLinked = true;
            }
            else if (point == "M")
                LinkChain(PoleChain());
        }

        // LipE / LipW sit on either side of a 1.5 m stairwell crack on Tang3 (see CorridorBridge.SeatGuide).
        static readonly Vector3 PlankFrom = new Vector3(-0.10f, 8.05f, -3.04f);
        static readonly Vector3 PlankTo = new Vector3(-2.30f, 8.05f, -2.46f);

        /// <summary>
        /// A wooden plank slides out over the crack when the route to G opens, and stays once G is reached.
        /// Without it the LipE-LipW step walks over empty air.
        /// </summary>
        void TickPlank()
        {
            bool want = story.RoofReached >= 5 || (story.RoofReached == 4 && gLinked);
            if (crackPlank == null)
            {
                if (!want)
                    return;
                crackPlank = BuildPlank(BuildingRoot());
                if (crackPlank == null)
                    return;
            }

            float before = plankOpen;
            plankOpen = Mathf.MoveTowards(plankOpen, want ? 1f : 0f, Time.deltaTime / 0.45f);
            if (before < 0.01f && plankOpen >= 0.01f && GameAudio.Instance != null)
                GameAudio.Instance.PlayAlign();
            float e = plankOpen * plankOpen * (3f - 2f * plankOpen);
            crackPlank.gameObject.SetActive(plankOpen > 0.001f);
            crackPlank.localScale = new Vector3(1f, 1f, Mathf.Max(0.001f, e));
        }

        static Transform BuildPlank(Transform root)
        {
            if (root == null)
                return null;
            Vector3 span = PlankTo - PlankFrom;
            span.y = 0f;
            float length = span.magnitude;

            // Pivot at the LipE end, so the plank grows from the side the player arrives on.
            var pivot = new GameObject("CrackPlank").transform;
            pivot.SetParent(root, false);
            pivot.localPosition = PlankFrom;
            pivot.localRotation = Quaternion.LookRotation(span.normalized, Vector3.up);

            var wood = new Color(0.52f, 0.34f, 0.19f);
            var dark = new Color(0.33f, 0.21f, 0.12f);
            PlankPart(pivot, new Vector3(0f, 0.015f, length * 0.5f), new Vector3(0.62f, 0.05f, length), wood);
            // Two battens underneath and a seam line on top read as boards nailed together.
            PlankPart(pivot, new Vector3(0f, -0.03f, length * 0.22f), new Vector3(0.66f, 0.04f, 0.08f), dark);
            PlankPart(pivot, new Vector3(0f, -0.03f, length * 0.78f), new Vector3(0.66f, 0.04f, 0.08f), dark);
            PlankPart(pivot, new Vector3(0f, 0.042f, length * 0.5f), new Vector3(0.015f, 0.006f, length * 0.98f), dark);
            pivot.localScale = new Vector3(1f, 1f, 0.001f);
            pivot.gameObject.SetActive(false);
            return pivot;
        }

        static void PlankPart(Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Board";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            Collider col = go.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
            Renderer rend = go.GetComponent<Renderer>();
            if (rend != null)
                rend.material.color = color;
        }

        PathNode[] PoleChain()
        {
            return ClimbChain(nodeG, PoleSteps, nodeM);
        }

        PathNode[] ClimbChain(PathNode head, string[] names, PathNode tail)
        {
            var nodes = new PathNode[names.Length + 2];
            nodes[0] = head;
            for (int i = 0; i < names.Length; i++)
                nodes[i + 1] = FindClimb(names[i]);
            nodes[nodes.Length - 1] = tail;
            return nodes;
        }

        static void ShowNode(PathNode node, bool visible)
        {
            if (node == null)
                return;
            Waypoint way = node.GetComponent<Waypoint>();
            if (way != null)
                way.Show(visible);
        }

        static void HideNode(PathNode node)
        {
            ShowNode(node, false);
        }

        static void LinkChain(params PathNode[] nodes)
        {
            for (int i = 0; i < nodes.Length - 1; i++)
            {
                if (nodes[i] != null && nodes[i + 1] != null)
                    nodes[i].Link(nodes[i + 1]);
            }
        }

        static void UnlinkChain(params PathNode[] nodes)
        {
            for (int i = 0; i < nodes.Length - 1; i++)
            {
                if (nodes[i] != null && nodes[i + 1] != null)
                    nodes[i].Unlink(nodes[i + 1]);
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
                if (GameInput.GetMouseButtonDown(0))
                    break;
                t += Time.deltaTime;
                yield return null;
            }

            if (presenter != null && presenter.caption == line)
                presenter.SetCaption(null);
        }
    }
}
