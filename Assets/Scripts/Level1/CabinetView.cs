using System;
using UnityEngine;

namespace PerspectivePuzzle.Level1
{
    /// <summary>
    /// Two cabinet blocks. Each click turns one block by 90 degrees.
    /// The cassette can be taken only at the solved pair of turns.
    /// </summary>
    public static class CabinetBlocks
    {
        public const int SolutionA = 1;
        public const int SolutionB = 3;

        public static int Normalize(int turns)
        {
            int value = turns % 4;
            return value < 0 ? value + 4 : value;
        }

        public static bool Solved(int blockA, int blockB)
        {
            return Normalize(blockA) == SolutionA && Normalize(blockB) == SolutionB;
        }
    }

    public class CabinetView : MonoBehaviour
    {
        public static CabinetView Current { get; private set; }

        public bool IsOpen { get; private set; }

        Action onTaken;
        Camera puzzleCamera;
        Camera hiddenCamera;
        Transform stage;
        Transform blockA;
        Transform blockB;
        Transform tape;
        Transform spinning;
        Quaternion spinFrom;
        Quaternion spinTo;
        float spinT = 1f;
        int turnsA;
        int turnsB;

        public static CabinetView Open(Action taken)
        {
            if (Current != null)
                Current.Close(false);

            var go = new GameObject("CabinetView");
            var view = go.AddComponent<CabinetView>();
            view.onTaken = taken;
            view.Build();
            Current = view;
            view.IsOpen = true;
            return view;
        }

        void Build()
        {
            stage = new GameObject("CabinetStage").transform;
            stage.SetParent(transform, false);
            stage.position = new Vector3(420f, 30f, 420f);

            blockA = Block("BlockA", new Vector3(-0.72f, 0.85f, 0f), new Color(0.45f, 0.28f, 0.16f));
            blockB = Block("BlockB", new Vector3(0.72f, 0.85f, 0f), new Color(0.34f, 0.22f, 0.13f));
            tape = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            tape.name = "Cassette";
            tape.SetParent(stage, false);
            tape.localPosition = new Vector3(0f, 0.72f, 0.62f);
            tape.localScale = new Vector3(0.34f, 0.08f, 0.22f);
            Paint(tape.GetComponent<Renderer>(), new Color(0.08f, 0.08f, 0.09f));
            tape.gameObject.SetActive(false);

            BuildFrame();

            // Blocks face the camera (+z is the door side), seen from a raised 3/4 angle so
            // the player can read which way each door points.
            var camGo = new GameObject("CabinetCamera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.position = stage.position + new Vector3(0f, 3.1f, 6.2f);
            camGo.transform.LookAt(stage.position + Vector3.up * 0.75f);
            puzzleCamera = camGo.AddComponent<Camera>();
            puzzleCamera.clearFlags = CameraClearFlags.SolidColor;
            puzzleCamera.backgroundColor = new Color(0.16f, 0.13f, 0.11f);
            puzzleCamera.nearClipPlane = 0.05f;
            puzzleCamera.farClipPlane = 40f;
            puzzleCamera.fieldOfView = 34f;

            var keyGo = new GameObject("CabinetKey");
            keyGo.transform.SetParent(transform, false);
            keyGo.transform.position = stage.position + new Vector3(1.5f, 3f, 3f);
            var key = keyGo.AddComponent<Light>();
            key.type = LightType.Point;
            key.range = 9f;
            key.intensity = 1.6f;
            key.color = new Color(1f, 0.86f, 0.66f);

            Camera main = Camera.main;
            if (main != null && main != puzzleCamera)
            {
                hiddenCamera = main;
                hiddenCamera.enabled = false;
            }
        }

        Transform Block(string name, Vector3 local, Color color)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            block.name = name;
            block.SetParent(stage, false);
            block.localPosition = local;
            block.localScale = new Vector3(1.15f, 1.7f, 0.72f);
            Paint(block.GetComponent<Renderer>(), color);
            var door = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            door.name = "Door";
            door.SetParent(block, false);
            door.localPosition = new Vector3(0f, 0f, 0.52f);
            door.localScale = new Vector3(0.72f, 0.78f, 0.06f);
            Paint(door.GetComponent<Renderer>(), new Color(0.62f, 0.5f, 0.32f));
            var knob = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            knob.name = "Knob";
            knob.SetParent(door, false);
            knob.localPosition = new Vector3(0.32f, 0f, 1.2f);
            knob.localScale = new Vector3(0.14f, 0.1f, 1.6f);
            Paint(knob.GetComponent<Renderer>(), new Color(0.86f, 0.68f, 0.3f));
            return block;
        }

        void BuildFrame()
        {
            // Plain tu che body around the two blocks: base, top and back panel.
            Color wood = new Color(0.26f, 0.16f, 0.1f);
            Slab("Base", new Vector3(0f, -0.08f, 0f), new Vector3(3.2f, 0.16f, 1.2f), wood);
            Slab("Top", new Vector3(0f, 1.78f, 0f), new Vector3(3.2f, 0.12f, 1.2f), wood);
            Slab("Back", new Vector3(0f, 0.85f, -0.62f), new Vector3(3.2f, 1.86f, 0.08f), wood * 0.8f);
            Slab("Floor", new Vector3(0f, -0.2f, 0.6f), new Vector3(7f, 0.08f, 5f), new Color(0.55f, 0.38f, 0.22f));
        }

        void Slab(string name, Vector3 local, Vector3 size, Color color)
        {
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            slab.name = name;
            slab.SetParent(stage, false);
            slab.localPosition = local;
            slab.localScale = size;
            Destroy(slab.GetComponent<Collider>());
            Paint(slab.GetComponent<Renderer>(), color);
        }

        void Update()
        {
            if (!IsOpen)
                return;

            if (spinT < 1f && spinning != null)
            {
                spinT = Mathf.Min(1f, spinT + Time.deltaTime / 0.22f);
                spinning.localRotation = Quaternion.Slerp(spinFrom, spinTo, spinT);
            }

            bool solved = CabinetBlocks.Solved(turnsA, turnsB) && spinT >= 1f;
            if (tape != null && tape.gameObject.activeSelf != solved)
                tape.gameObject.SetActive(solved);

            if (GameInput.GetKeyDown(KeyCode.Escape) || GameInput.GetMouseButtonDown(1))
            {
                Close(false);
                return;
            }

            if (!GameInput.GetMouseButtonDown(0) || puzzleCamera == null)
                return;
            if (!Physics.Raycast(puzzleCamera.ScreenPointToRay(GameInput.mousePosition), out RaycastHit hit, 30f))
                return;

            if (tape != null && solved && hit.transform.IsChildOf(tape))
            {
                Close(true);
                return;
            }

            if (hit.transform == blockA || hit.transform.IsChildOf(blockA))
                Spin(blockA, ref turnsA);
            else if (hit.transform == blockB || hit.transform.IsChildOf(blockB))
                Spin(blockB, ref turnsB);
        }

        void Spin(Transform block, ref int turns)
        {
            if (spinning != null && spinT < 1f)
                spinning.localRotation = spinTo;
            spinning = block;
            spinFrom = block.localRotation;
            turns = CabinetBlocks.Normalize(turns + 1);
            if (PerspectivePuzzle.Audio.GameAudio.Instance != null)
                PerspectivePuzzle.Audio.GameAudio.Instance.PlaySwitch();
            spinTo = Quaternion.Euler(0f, turns * 90f, 0f);
            spinT = 0f;
        }

        public void SpinA() => Spin(blockA, ref turnsA);
        public void SpinB() => Spin(blockB, ref turnsB);
        public bool IsTapeVisible => tape != null && tape.gameObject.activeSelf;
        public int TurnsA => turnsA;
        public int TurnsB => turnsB;

        public bool TakeTape()
        {
            if (tape != null && CabinetBlocks.Solved(turnsA, turnsB))
            {
                Close(true);
                return true;
            }
            return false;
        }

        public void Cancel() => Close(false);

        void OnGUI()
        {
            if (!IsOpen)
                return;
            const string line = "Click a block to turn it 90°. Open both doors toward each other to reach what is inside. Right click / Esc to step back.";
            float width = Mathf.Min(920f, Screen.width - 40f);
            Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height - 90f, width, 56f);
            GUI.color = new Color(0.93f, 0.9f, 0.82f, 0.94f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(0.12f, 0.08f, 0.05f, 1f);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 8f, rect.width - 32f, rect.height - 16f), line);
            GUI.color = Color.white;
        }

        public void Close(bool taken)
        {
            if (!IsOpen && Current != this)
                return;
            IsOpen = false;
            if (hiddenCamera != null)
                hiddenCamera.enabled = true;
            if (Current == this)
                Current = null;
            if (taken)
                onTaken?.Invoke();
            Destroy(gameObject);
        }

        static void Paint(Renderer renderer, Color color)
        {
            if (renderer == null)
                return;
            renderer.material.color = color;
        }
    }
}
