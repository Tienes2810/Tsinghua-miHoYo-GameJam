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

            var camGo = new GameObject("CabinetCamera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.position = stage.position + new Vector3(0f, 0.35f, -3.4f);
            camGo.transform.LookAt(stage.position + Vector3.up * 0.7f);
            puzzleCamera = camGo.AddComponent<Camera>();
            puzzleCamera.clearFlags = CameraClearFlags.SolidColor;
            puzzleCamera.backgroundColor = new Color(0.16f, 0.13f, 0.11f);
            puzzleCamera.nearClipPlane = 0.05f;
            puzzleCamera.farClipPlane = 40f;
            puzzleCamera.fieldOfView = 32f;

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
            return block;
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

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                Close(false);
                return;
            }

            if (!Input.GetMouseButtonDown(0) || puzzleCamera == null || spinT < 1f)
                return;
            if (!Physics.Raycast(puzzleCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 30f))
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
            spinning = block;
            spinFrom = block.localRotation;
            turns = CabinetBlocks.Normalize(turns + 1);
            spinTo = Quaternion.Euler(0f, turns * 90f, 0f);
            spinT = 0f;
        }

        void OnGUI()
        {
            if (!IsOpen)
                return;
            const string line = "Turn each block by 90°. The cassette is reachable in only one arrangement. Right click to step back.";
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
