using UnityEngine;

namespace PerspectivePuzzle.Level1
{
    public class Level1Presenter : MonoBehaviour
    {
        public string caption;
        public string objective;
        public string hint;
        public float captionAlpha = 1f;
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

        public bool EndScreen { get; private set; }
        string endTitle;
        string endLine;
        float endShownAt;

        public void ShowEndScreen(string title, string line)
        {
            EndScreen = true;
            endTitle = title;
            endLine = line;
            endShownAt = Time.unscaledTime;
            caption = null;
            hint = null;
            objective = null;
            showRotate = false;
            dim = 1f;
        }

        void Update()
        {
            if (EndScreen && Time.unscaledTime - endShownAt > 1.5f && GameInput.GetKeyDown(KeyCode.Escape))
                Application.Quit();
            if (EndScreen && Time.unscaledTime - endShownAt > 2.4f &&
                (GameInput.GetKeyDown(KeyCode.Return) || GameInput.GetKeyDown(KeyCode.KeypadEnter) || GameInput.GetKeyDown(KeyCode.Space)))
                UnityEngine.SceneManagement.SceneManager.LoadScene("Level2");
        }

        void DrawEnd(float width, float height)
        {
            GUI.color = new Color(0.03f, 0.035f, 0.04f, 1f);
            GUI.DrawTexture(new Rect(0f, 0f, width, height), Texture2D.whiteTexture);
            float a = Mathf.Clamp01((Time.unscaledTime - endShownAt) / 1.2f);
            Color ink = new Color(0.93f, 0.9f, 0.82f, a);
            GUI.color = ink;
            body.normal.textColor = ink;
            GUI.Label(new Rect(width * 0.12f, height * 0.30f, width * 0.76f, 160f), endTitle, body);
            float b = Mathf.Clamp01((Time.unscaledTime - endShownAt - 1.2f) / 1.2f);
            Color soft = new Color(0.93f, 0.78f, 0.45f, b);
            prompt.normal.textColor = soft;
            GUI.color = soft;
            GUI.Label(new Rect(width * 0.12f, height * 0.30f + 168f, width * 0.76f, 50f), endLine, prompt);
            if (b >= 0.8f)
            {
                float btnW = 520f;
                float btnH = 56f;
                Rect btnRect = new Rect((width - btnW) * 0.5f, height * 0.30f + 236f, btnW, btnH);
                Plate(btnRect);
                Color btnInk = new Color(0.96f, 0.82f, 0.35f, 1f);
                prompt.normal.textColor = btnInk;
                if (GUI.Button(btnRect, "CHƯƠNG 2: HÀ NỘI HOÀNG HÔN  →", prompt))
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Level2");
                }

                Color faint = new Color(0.7f, 0.7f, 0.66f, 0.7f);
                small.normal.textColor = faint;
                small.alignment = TextAnchor.MiddleCenter;
                GUI.color = faint;
                GUI.Label(new Rect(0f, height - 60f, width, 30f), "Hoàn thành Chương 1  ·  Enter: Chương 2  ·  Esc: Thoát / Quit", small);
                small.alignment = TextAnchor.MiddleLeft;
            }
            GUI.color = Color.white;
        }

        void OnGUI()
        {
            EnsureStyles();
            float scale = Mathf.Max(0.75f, Screen.height / 1080f);
            Matrix4x4 saved = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            float height = Screen.height / scale;

            if (EndScreen)
            {
                DrawEnd(width, height);
                GUI.matrix = saved;
                return;
            }

            if (dim > 0.01f)
            {
                Color previous = GUI.color;
                GUI.color = new Color(0.04f, 0.045f, 0.05f, dim);
                GUI.DrawTexture(new Rect(0f, 0f, width, height), Texture2D.whiteTexture);
                GUI.color = previous;
            }

            if (!string.IsNullOrEmpty(objective))
            {
                float textH = title.CalcHeight(new GUIContent(objective), 788f);
                float plateH = Mathf.Max(86f, textH + 42f);
                Plate(new Rect(28f, 24f, 820f, plateH));
                GUI.Label(new Rect(44f, 30f, 788f, 22f), "MỤC TIÊU  ·  OBJECTIVE", small);
                GUI.Label(new Rect(44f, 52f, 788f, textH + 8f), objective, title);
            }

            if (!string.IsNullOrEmpty(caption))
            {
                float alpha = Mathf.Clamp01(captionAlpha);
                bool intro = dim > 0.5f;
                float plateWidth = intro ? width * 0.82f : Mathf.Min(width * 0.74f, 1120f);
                float textWidth = plateWidth - 48f;
                float boxHeight = intro ? Mathf.Max(160f, body.CalcHeight(new GUIContent(caption), textWidth) + 36f) : Mathf.Max(64f, body.CalcHeight(new GUIContent(caption), textWidth) + 26f);
                float bottom = showRotate ? height - 160f : height - 76f;
                float y = intro ? height * 0.56f : bottom - boxHeight;
                float x = (width - plateWidth) * 0.5f;
                Rect plate = new Rect(x, y, plateWidth, boxHeight);
                Color wash = HudColors.Plate;
                wash.a *= alpha;
                GUI.color = wash;
                GUI.DrawTexture(plate, Texture2D.whiteTexture);
                GUI.color = new Color(HudColors.Ink.r, HudColors.Ink.g, HudColors.Ink.b, alpha);
                GUI.Label(new Rect(x + 24f, y + 14f, textWidth, boxHeight - 28f), caption, body);
                GUI.color = Color.white;
            }

            if (showRotate)
            {
                const string rotateText = "Kéo ngang để xoay nhà  ·  Drag to turn the house";
                float boxWidth = Mathf.Min(780f, width - 48f);
                float boxHeight = 64f;
                float x = (width - boxWidth) * 0.5f;
                float y = height - 148f;
                Plate(new Rect(x, y, boxWidth, boxHeight));
                GUI.Label(new Rect(x + 16f, y + 6f, boxWidth - 32f, boxHeight - 12f), rotateText, prompt);
            }

            if (!string.IsNullOrEmpty(hint))
            {
                float hintW = Mathf.Min(820f, width * 0.62f);
                Plate(new Rect(24f, height - 58f, hintW, 40f));
                GUI.Label(new Rect(36f, height - 54f, hintW - 24f, 32f), hint, small);
            }

            if (!string.IsNullOrEmpty(performance))
            {
                Plate(new Rect(width - 560f, 18f, 530f, 32f));
                GUI.Label(new Rect(width - 548f, 20f, 506f, 28f), performance, small);
            }

            Rect chapRect = new Rect(width - 274f, 56f, 250f, 34f);
            Plate(chapRect);
            if (GUI.Button(chapRect, "Chương 2 / Chapter 2  →", small))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Level2");
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
