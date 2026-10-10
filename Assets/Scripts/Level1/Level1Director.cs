using System.Collections;
using System.Collections.Generic;
using PerspectivePuzzle.Movement;
using PerspectivePuzzle.Puzzle;
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
        static readonly string[] LowerSteps =
        {
            "A2", "Tread1", "L2", "L3", "L4", "L5", "L6", "L7", "Tread2", "L8", "L9", "Tread3", "L11"
        };
        static readonly string[] UpperSteps =
        {
            "Up1", "Up2", "Up3", "Up4", "Up5", "Up6", "Up7", "Up8", "Up9", "Up10"
        };
        static readonly string[] PoleSteps =
        {
            "LipE", "LipW", "WestA", "WestB", "Wlip", "Wmid", "Whip", "S1", "S2", "S3", "S4", "ShelfW", "SlopeS", "PlaneA", "PlaneB", "RoofA", "RoofB"
        };

        bool dogBusy;
        string routeShown = "";
        Transform dogStair;
        Quaternion stairBaseRot;
        Vector3 stairBasePos;
        Vector3 stairCenterLocal;
        Vector3 stairCenterRoot;
        bool stairReady;
        Transform dogPatrol;
        Vector3[] dogLocal;
        float dogWalk;

        public string performanceBase;
        public readonly StoryProgress story = new StoryProgress();
        Level1Phase phase = Level1Phase.Intro;
        float idle;
        bool roomHinted;
        Coroutine captionRoutine;

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
        }

        void Start()
        {
            if (rotator != null)
                rotator.transform.rotation = Quaternion.identity;
            CorridorBridge.Align(nodeA, nodeB, nodeC, nodeSpeaker, nodeDoor, roomFocus, cabinet);
            StairSpan.Ensure(rotator != null ? rotator.transform : null);
            CacheClimb();
            if (board != null)
                board.KeepPair(nodeB, nodeC);
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
            StartCoroutine(Intro());
        }

        void OnDisable()
        {
            if (player != null)
                player.Arrived -= OnArrived;
        }

        void Update()
        {
            if (presenter != null)
            {
                float fps = 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime);
                presenter.performance = performanceBase + "   " + Mathf.RoundToInt(fps) + " fps";
            }

            ObserveYaw();
            PoseDogStair();

            bool waiting = phase == Level1Phase.NeedRotate || phase == Level1Phase.InRoom
                || phase == Level1Phase.NeedStair || phase == Level1Phase.Round2 || phase == Level1Phase.ToRoof;
            if (!waiting || Input.GetMouseButton(0) || Input.anyKeyDown)
                idle = 0f;
            else
                idle += Time.deltaTime;

            if ((phase == Level1Phase.NeedRotate || phase == Level1Phase.NeedStair || phase == Level1Phase.Round2
                    || phase == Level1Phase.ToRoof)
                && idle > 12f && presenter != null)
                presenter.hint = "Perhaps the path isn't ahead.";

            WatchDog();

            if (phase == Level1Phase.InRoom && idle > 35f && !roomHinted && cabinet != null && !cabinet.Used)
            {
                roomHinted = true;
                Say("The cupboard still keeps a few of her things.", 3.2f);
                if (cabinet.transform != null)
                    cabinet.transform.localScale *= 1.04f;
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
                Say("The room is still out of reach.", 1.8f);
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
            if (markerSpeaker != null)
                markerSpeaker.Show(true);
            story.Speaker = SpeakerPhase.Ready;
            if (cabinet != null)
                cabinet.SetSpeakerReady(true);
            if (nodeC != null && nodeSpeaker != null)
                nodeC.Link(nodeSpeaker);
            Say("There it is.", 1.6f);
            player.MovementLocked = false;
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
                ribbon.Draw(openRoute, nodeB, nodeC, open);
            if (markerC != null)
                markerC.Show(open && phase != Level1Phase.Intro && phase != Level1Phase.Ending);
        }

        IEnumerator Intro()
        {
            phase = Level1Phase.Intro;
            if (presenter != null)
            {
                presenter.SetDim(0.94f);
                presenter.SetObjective("Find Grandma's cassette tape.");
                presenter.hint = "Click to skip a line";
            }

            string[] lines =
            {
                "This home is about to disappear.",
                "But Grandma-a former radio broadcaster from old Hanoi-still wants to hear one thing… before it all comes to an end.",
                "Grandma's cassette tape is still somewhere inside this home.",
                "Find it. Take it to the loudspeaker post on the rooftop."
            };

            for (int i = 0; i < lines.Length; i++)
            {
                if (presenter != null)
                    presenter.SetCaption(lines[i]);
                float t = 0f;
                const float hold = 2.6f;
                while (t < hold)
                {
                    if (Input.GetMouseButtonDown(0))
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
                presenter.SetObjective("Find Grandma's cassette tape. Bring it to the loudspeaker post on the rooftop.");
                presenter.hint = "Click the gold point. Drag to turn the house.";
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
                if (presenter != null)
                {
                    presenter.SetCaption("Some paths only appear when we see this place from a different perspective.");
                    presenter.showRotate = true;
                    presenter.hint = "Drag left / right to turn the house.";
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
            if (rig != null && roomFocus != null)
                rig.MoveFocus(roomFocus.position, rig.ortho * 0.72f);

            yield return new WaitForSeconds(0.75f);
            if (presenter != null)
            {
                presenter.SetCaption("Grandma used to have a cassette tape. It must still be somewhere around here.");
                presenter.SetObjective("Find Grandma's cassette tape.");
            }

            float t = 0f;
            while (t < 3f)
            {
                if (Input.GetMouseButtonDown(0))
                    break;
                t += Time.deltaTime;
                yield return null;
            }

            if (presenter != null)
                presenter.SetCaption(null);
            player.MovementLocked = false;
            idle = 0f;
        }

        void Pickup()
        {
            RoomInteractable speaker = cabinet != null ? cabinet.speaker : null;
            if (speaker == null || hand == null)
                return;

            speaker.transform.SetParent(hand, true);
            speaker.transform.localPosition = Vector3.zero;
            speaker.transform.localScale = Vector3.one * 0.35f;
            Collider col = speaker.GetComponent<Collider>();
            if (col != null)
                col.enabled = false;

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
            if (nodeDoor != null && nodeDog != null)
                nodeDoor.Unlink(nodeDog);
            ShowNode(nodeDoor, true);
            HideNode(nodeDog);
            if (presenter != null)
            {
                presenter.SetCaption("If you remember… Grandma used to keep it at the highest place in the building.");
                presenter.SetObjective("Return the cassette tape to where it belongs.");
                presenter.hint = "Click the way out of the room.";
                presenter.showRotate = false;
            }
        }

        void EnterRound2()
        {
            phase = Level1Phase.Round2;
            if (presenter != null)
            {
                presenter.SetCaption(null);
                presenter.hint = "The side stair is not safe yet. Drag to turn the house.";
                presenter.showRotate = true;
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
            ClipActor actor = player != null ? player.GetComponent<ClipActor>() : null;
            if (actor != null)
                actor.PlayFall();
            story.DogFail();
            yield return new WaitForSeconds(1.4f);
            if (player != null && nodeA != null)
                player.PlaceAt(nodeA);
            if (presenter != null)
            {
                presenter.SetCaption("Có lẽ con đường vừa rồi không dành cho chúng ta.");
                presenter.hint = "Perhaps the path isn't ahead.";
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
            if (nodeA == null)
                yield break;
            Transform root = nodeA.transform;
            while (root.parent != null)
                root = root.parent;
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "DogRunner";
            go.transform.SetParent(root, false);
            go.transform.localScale = new Vector3(0.42f, 0.22f, 0.62f);
            Renderer rend = go.GetComponent<Renderer>();
            if (rend != null)
                rend.material.color = new Color(0.42f, 0.28f, 0.16f);
            Collider col = go.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
            Vector3[] pts =
            {
                new Vector3(0.75f, 1.36f, -0.10f),
                new Vector3(0.75f, 2.02f, 0.99f),
                new Vector3(0.75f, 2.60f, 1.95f),
                new Vector3(0.75f, 3.19f, 2.92f),
                new Vector3(0.75f, 3.49f, 3.42f),
                new Vector3(0.75f, 4.10f, 4.42f),
                new Vector3(0.75f, 4.70f, 5.41f),
                new Vector3(0.75f, 5.00f, 5.90f)
            };
            for (int i = 1; i < pts.Length; i++)
            {
                Vector3 from = root.TransformPoint(pts[i - 1]);
                Vector3 to = root.TransformPoint(pts[i]);
                float t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime / 0.26f;
                    if (go == null)
                        yield break;
                    go.transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(t));
                    yield return null;
                }
            }

            if (go != null)
                Destroy(go);
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
                presenter.hint = "Drag to turn the house.";
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
                presenter.SetCaption("Back then, standing here was enough for the whole neighborhood to hear.");
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

        IEnumerator EndingRoutine()
        {
            phase = Level1Phase.Ending;
            if (player != null)
                player.MovementLocked = true;
            RoomInteractable speaker = cabinet != null ? cabinet.speaker : null;
            Transform mount = nodePole != null ? nodePole.transform : poleAnchor;
            if (speaker != null)
                MountOnPole(speaker.transform, mount);

            PlayStinger();
            if (presenter != null)
            {
                presenter.showRotate = false;
                presenter.SetCaption("Some things have disappeared from this home. But memories still know the way back.");
                presenter.SetObjective("Return the cassette tape to where it belongs.");
            }

            if (rig != null)
                rig.MoveFocus(rig.lookAt, rig.ortho * 1.35f);
            yield return new WaitForSeconds(2.6f);
            if (presenter != null)
                presenter.SetDim(1f);
        }

        void PlayStinger()
        {
            AudioSource source = GetComponent<AudioSource>();
            if (source == null)
                source = gameObject.AddComponent<AudioSource>();
            int rate = 44100;
            int length = rate / 2;
            AudioClip clip = AudioClip.Create("HanoiStinger", length, 1, rate, false);
            float[] data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)rate;
                data[i] = Mathf.Sin(2f * Mathf.PI * 440f * t) * Mathf.Exp(-3f * t) * 0.2f;
            }
            clip.SetData(data, 0);
            source.PlayOneShot(clip);
        }

        float CurrentYaw()
        {
            return board != null ? board.CurrentYaw : 0f;
        }

        bool Round2Stair()
        {
            return phase == Level1Phase.Round2 || phase == Level1Phase.NeedStair || phase == Level1Phase.ToRoof;
        }

        void PoseDogStair()
        {
            if (!CacheDogStair())
                return;

            bool flip = Round2Stair() && !dogBusy && story.StairOpen(CurrentYaw());
            Transform root = BuildingRoot();
            dogStair.localPosition = stairBasePos;
            dogStair.localRotation = flip ? stairBaseRot * Quaternion.Euler(0f, 0f, 180f) : stairBaseRot;
            if (root != null)
            {
                Vector3 want = root.TransformPoint(stairCenterRoot);
                Vector3 now = dogStair.TransformPoint(stairCenterLocal);
                dogStair.position += want - now;
            }

            WalkDog(flip);
        }

        bool CacheDogStair()
        {
            if (stairReady)
                return dogStair != null;
            stairReady = true;
            GameObject flight = GameObject.Find("Plane.002");
            if (flight == null)
                return false;
            dogStair = flight.transform;
            stairBasePos = dogStair.localPosition;
            stairBaseRot = dogStair.localRotation;
            MeshFilter filter = flight.GetComponent<MeshFilter>();
            stairCenterLocal = filter != null && filter.sharedMesh != null
                ? filter.sharedMesh.bounds.center
                : Vector3.zero;
            Transform root = BuildingRoot();
            stairCenterRoot = root != null
                ? root.InverseTransformPoint(dogStair.TransformPoint(stairCenterLocal))
                : stairCenterLocal;
            Vector3[] tops =
            {
                new Vector3(0.75f, 1.36f, -0.10f),
                new Vector3(0.75f, 2.02f, 0.99f),
                new Vector3(0.75f, 2.60f, 1.95f),
                new Vector3(0.75f, 3.19f, 2.92f),
                new Vector3(0.75f, 3.49f, 3.42f),
                new Vector3(0.75f, 4.10f, 4.42f),
                new Vector3(0.75f, 4.70f, 5.41f),
                new Vector3(0.75f, 5.00f, 5.90f)
            };
            dogLocal = new Vector3[tops.Length];
            for (int i = 0; i < tops.Length; i++)
            {
                Vector3 world = root != null ? root.TransformPoint(tops[i]) : tops[i];
                dogLocal[i] = dogStair.InverseTransformPoint(world);
            }

            return true;
        }

        void WalkDog(bool under)
        {
            bool show = Round2Stair() || phase == Level1Phase.ToExit;
            if (!show || dogLocal == null)
            {
                if (dogPatrol != null)
                    dogPatrol.gameObject.SetActive(false);
                return;
            }

            if (dogPatrol == null)
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "DogPatrol";
                body.transform.SetParent(dogStair, false);
                body.transform.localScale = new Vector3(0.38f, 0.16f, 0.55f);
                Renderer rend = body.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.shader = Shader.Find("Unlit/Color");
                    rend.material.color = new Color(0.45f, 0.28f, 0.16f);
                }

                Collider col = body.GetComponent<Collider>();
                if (col != null)
                    Destroy(col);
                dogPatrol = body.transform;
            }

            dogPatrol.gameObject.SetActive(true);
            dogWalk += Time.deltaTime * 0.9f;
            float span = (dogLocal.Length - 1) * 2f;
            float loop = dogWalk % span;
            float along = loop <= dogLocal.Length - 1 ? loop : span - loop;
            int i0 = Mathf.Clamp(Mathf.FloorToInt(along), 0, dogLocal.Length - 1);
            int i1 = Mathf.Min(i0 + 1, dogLocal.Length - 1);
            float blend = along - i0;
            Vector3 onTread = Vector3.Lerp(dogLocal[i0], dogLocal[i1], blend);
            dogPatrol.localPosition = under ? onTread : onTread + Vector3.up * 0.22f;
            dogPatrol.localRotation = Quaternion.identity;
        }

        Transform BuildingRoot()
        {
            if (nodeA == null)
                return dogStair != null ? dogStair.root : null;
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
            bool climbing = held && (phase == Level1Phase.Round2 || phase == Level1Phase.NeedStair || phase == Level1Phase.ToRoof);
            bool trap = climbing && !story.StairOpen(yaw);
            PathNode tread = FindClimb("Tread2");
            if (tread != null && nodeDog != null)
            {
                if (trap)
                    tread.Link(nodeDog);
                else
                    tread.Unlink(nodeDog);
            }

            ShowNode(nodeDog, trap && !dogBusy);
            HideRoofMarks();
            UnlinkRoof();
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
                    presenter.showRotate = true;
                    if (openPoint)
                        presenter.hint = "Click the gold point.";
                    else if (!trap)
                        presenter.hint = "Drag to turn the house.";
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
            AddRoute(FindClimb("Landing"));
            AddRoute(FindClimb("NearMid"));
            AddRoute(nodeB);
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
            PathNode stair = FindClimb("Stair");
            UnlinkChain(ClimbChain(stair, UpperSteps, nodeE));
            UnlinkChain(nodeE, nodeF, nodeI, nodeL, nodeG);
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
            PathNode stair = FindClimb("Stair");
            if (point == "E")
                LinkChain(ClimbChain(stair, UpperSteps, nodeE));
            else if (point == "F")
                LinkChain(nodeE, nodeF);
            else if (point == "I")
                LinkChain(nodeF, nodeI);
            else if (point == "L")
                LinkChain(nodeI, nodeL);
            else if (point == "G")
                LinkChain(nodeL, nodeG);
            else if (point == "M")
                LinkChain(PoleChain());
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
