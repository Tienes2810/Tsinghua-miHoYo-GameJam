using UnityEngine;

namespace PerspectivePuzzle.Level1
{
    public class Level1Presenter : MonoBehaviour
    {
        public string caption;
        public string objective;
        public string hint;
        public bool showRotate;
        public string performance = "";
        public float dim = 1f;
        public float pulse;

        GUIStyle title;
        GUIStyle body;
        GUIStyle small;
        GUIStyle prompt;
        bool stylesReady;

        public void SetCaption(string value)
        {
            caption = value;
        }

        public void SetObjective(string value)
        {
            objective = value;
        }

        public void SetDim(float value)
        {
            dim = Mathf.Clamp01(value);
        }

        void OnGUI()
        {
            EnsureStyles();
            float scale = Mathf.Max(0.75f, Screen.height / 1080f);
            Matrix4x4 saved = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            float height = Screen.height / scale;

            if (dim > 0.01f)
            {
                Color previous = GUI.color;
                GUI.color = new Color(0.04f, 0.045f, 0.05f, dim);
                GUI.DrawTexture(new Rect(0f, 0f, width, height), Texture2D.whiteTexture);
                GUI.color = previous;
            }

            if (!string.IsNullOrEmpty(objective))
            {
                Plate(new Rect(28f, 24f, 820f, 86f));
                GUI.Label(new Rect(44f, 32f, 788f, 24f), "OBJECTIVE", small);
                GUI.Label(new Rect(44f, 54f, 788f, 46f), objective, title);
            }

            if (!string.IsNullOrEmpty(caption))
            {
                float boxHeight = 150f;
                float y = height * 0.58f;
                Plate(new Rect(width * 0.1f, y, width * 0.8f, boxHeight));
                GUI.Label(new Rect(width * 0.12f, y + 18f, width * 0.76f, boxHeight - 36f), caption, body);
            }

            if (showRotate)
            {
                const string rotateText = "Drag left / right to turn the house";
                float boxWidth = Mathf.Min(760f, width - 48f);
                float boxHeight = 64f;
                float x = (width - boxWidth) * 0.5f;
                float y = height - 148f;
                Plate(new Rect(x, y, boxWidth, boxHeight));
                GUI.Label(new Rect(x + 16f, y + 6f, boxWidth - 32f, boxHeight - 12f), rotateText, prompt);
            }

            if (!string.IsNullOrEmpty(hint))
            {
                Plate(new Rect(24f, height - 58f, Mathf.Min(640f, width * 0.48f), 40f));
                GUI.Label(new Rect(36f, height - 54f, Mathf.Min(616f, width * 0.46f), 32f), hint, small);
            }

            if (!string.IsNullOrEmpty(performance))
            {
                Plate(new Rect(width - 560f, 18f, 530f, 32f));
                GUI.Label(new Rect(width - 548f, 20f, 506f, 28f), performance, small);
            }

            GUI.matrix = saved;
        }

        void EnsureStyles()
        {
            if (!stylesReady || title == null || body == null || small == null || prompt == null)
                BuildStyles();
            Paint(title, HudColors.Ink);
            Paint(body, HudColors.Ink);
            Paint(small, HudColors.Ink);
            Paint(prompt, HudColors.Ink);
        }

        void BuildStyles()
        {
            title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };
            body = new GUIStyle(title)
            {
                fontSize = 26,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip
            };
            small = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };
            prompt = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                clipping = TextClipping.Clip
            };
            stylesReady = true;
        }

        static void Paint(GUIStyle style, Color ink)
        {
            style.normal.textColor = ink;
            style.hover.textColor = ink;
            style.active.textColor = ink;
            style.focused.textColor = ink;
            style.onNormal.textColor = ink;
            style.onHover.textColor = ink;
            style.onActive.textColor = ink;
            style.onFocused.textColor = ink;
        }

        static void Plate(Rect rect)
        {
            GUI.color = HudColors.Plate;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
