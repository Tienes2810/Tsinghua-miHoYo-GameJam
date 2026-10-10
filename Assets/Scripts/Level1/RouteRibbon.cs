using System.Collections.Generic;
using PerspectivePuzzle.Movement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PerspectivePuzzle.Level1
{
    public class RouteRibbon : MonoBehaviour
    {
        [SerializeField] float width = 0.12f;
        LineRenderer openLine;
        LineRenderer lockedLine;

        void Awake()
        {
            if (openLine == null || lockedLine == null)
                Build(width);
        }

        public void Build(float width)
        {
            this.width = width;
            openLine = Create("OpenRoute", new Color(0.86f, 0.24f, 0.18f, 0.95f), width);
            lockedLine = Create("LockedRoute", new Color(0.86f, 0.24f, 0.18f, 0.28f), width * 0.65f);
        }

        public void Draw(IList<PathNode> always, PathNode gapFrom, PathNode gapTo, bool gapOpen)
        {
            if (openLine == null || lockedLine == null)
                Build(width);
            DrawLine(openLine, always);
            if (gapFrom == null || gapTo == null || lockedLine == null)
            {
                if (lockedLine != null)
                    lockedLine.positionCount = 0;
                return;
            }

            Vector3 from = gapFrom.transform.position + Vector3.up * 0.05f;
            Vector3 to = gapTo.transform.position + Vector3.up * 0.05f;
            if (gapOpen && openLine.positionCount > 0)
            {
                int count = openLine.positionCount;
                openLine.positionCount = count + 2;
                openLine.SetPosition(count, from);
                openLine.SetPosition(count + 1, to);
                lockedLine.positionCount = 0;
                return;
            }

            lockedLine.positionCount = 2;
            lockedLine.SetPosition(0, from);
            lockedLine.SetPosition(1, to);
        }

        static void DrawLine(LineRenderer line, IList<PathNode> nodes)
        {
            if (line == null)
                return;
            if (nodes == null || nodes.Count == 0)
            {
                line.positionCount = 0;
                return;
            }

            line.positionCount = nodes.Count;
            for (int i = 0; i < nodes.Count; i++)
                line.SetPosition(i, nodes[i].transform.position + Vector3.up * 0.05f);
        }

        LineRenderer Create(string name, Color color, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.widthMultiplier = width;
            line.positionCount = 0;
            line.numCapVertices = 4;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            if (shader != null)
            {
                line.material = new Material(shader);
                line.startColor = color;
                line.endColor = color;
            }

            return line;
        }
    }
}
