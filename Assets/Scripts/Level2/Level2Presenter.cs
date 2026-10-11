using UnityEngine;
using UnityEngine.SceneManagement;

namespace PerspectivePuzzle.Level2
{
    public class Level2Presenter : MonoBehaviour
    {
        public string caption;
        public string objective = "Thắp sáng 3 ngọn đèn lồng Hoàng Hôn & Lên Tháp Chuông";
        public string hint;
        public float captionAlpha = 1f;
        public bool showRotate;
        public string performance = "";
        public float dim = 1f;
        public int lanternsLit = 0;
        public const int TotalLanterns = 3;

        GUIStyle title;
        GUIStyle body;
        GUIStyle small;
        GUIStyle prompt;
        GUIStyle buttonStyle;
        bool stylesReady;

        public bool EndScreen { get; private set; }
        string endTitle;
        string endLine;
        float endShownAt;

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
            if (EndScreen && Time.unscaledTime - endShownAt > 1.5f && Input.GetKeyDown(KeyCode.Escape))
                Application.Quit();
        }

        void DrawEnd(float width, float height)
        {
            // Sunset deep twilight background
            GUI.color = new Color(0.08f, 0.05f, 0.12f, 1f);
            GUI.DrawTexture(new Rect(0f, 0f, width, height), Texture2D.whiteTexture);

            float a = Mathf.Clamp01((Time.unscaledTime - endShownAt) / 1.2f);
            Color ink = new Color(1f, 0.88f, 0.65f, a);
            GUI.color = ink;
            body.normal.textColor = ink;
            GUI.Label(new Rect(width * 0.12f, height * 0.30f, width * 0.76f, 120f), endTitle, body);

            float b = Mathf.Clamp01((Time.unscaledTime - endShownAt - 1.2f) / 1.2f);
            Color soft = new Color(1f, 0.75f, 0.45f, b);
            prompt.normal.textColor = soft;
            GUI.color = soft;
            GUI.Label(new Rect(width * 0.12f, height * 0.30f + 130f, width * 0.76f, 50f), endLine, prompt);

            if (b >= 0.8f)
            {
                float btnW = 340f;
                float btnH = 50f;
                float centerY = height * 0.30f + 210f;

                // Replay Chapter 2
                Rect btn1 = new Rect((width - btnW) * 0.5f - 180f, centerY, btnW, btnH);
                Plate(btn1, new Color(0.18f, 0.12f, 0.22f, 0.95f));
                Color btnInk1 = new Color(1f, 0.85f, 0.45f, 1f);
                prompt.normal.textColor = btnInk1;
                if (GUI.Button(btn1, "CHƠI LẠI CHƯƠNG 2", prompt))
                {
                    SceneManager.LoadScene("Level2");
                }

                // Return to Chapter 1
                Rect btn2 = new Rect((width - btnW) * 0.5f + 180f, centerY, btnW, btnH);
                Plate(btn2, new Color(0.18f, 0.12f, 0.22f, 0.95f));
                Color btnInk2 = new Color(0.9f, 0.85f, 0.75f, 1f);
                prompt.normal.textColor = btnInk2;
                if (GUI.Button(btn2, "← VỀ CHƯƠNG 1", prompt))
                {
                    SceneManager.LoadScene("Level1");
                }

                Color faint = new Color(0.75f, 0.70f, 0.65f, 0.7f);
                small.normal.textColor = faint;
                small.alignment = TextAnchor.MiddleCenter;
                GUI.color = faint;
                GUI.Label(new Rect(0f, height - 60f, width, 30f), "Chapter 2 Complete  -  Esc to quit", small);
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
                GUI.color = new Color(0.04f, 0.035f, 0.06f, dim);
                GUI.DrawTexture(new Rect(0f, 0f, width, height), Texture2D.whiteTexture);
                GUI.color = previous;
            }

            // Top Bar: Chapter badge & Objective
            if (!string.IsNullOrEmpty(objective))
            {
                Plate(new Rect(28f, 24f, 860f, 96f), new Color(0.14f, 0.10f, 0.18f, 0.92f));
                GUI.Label(new Rect(44f, 30f, 800f, 22f), "CHƯƠNG 2: HÀ NỘI HOÀNG HÔN", small);
                GUI.Label(new Rect(44f, 52f, 800f, 36f), objective, title);

                // Lantern status dots
                string lanternStatus = $"Đèn lồng: {lanternsLit}/{TotalLanterns}";
                for (int i = 0; i < TotalLanterns; i++)
                {
                    lanternStatus += (i < lanternsLit) ? "  [★ SÁNG]" : "  [○ Chưa]";
                }
                GUI.Label(new Rect(44f, 88f, 800f, 24f), lanternStatus, small);
            }

            // Subtitle / Caption Plate
            if (!string.IsNullOrEmpty(caption))
            {
                float alpha = Mathf.Clamp01(captionAlpha);
                bool intro = dim > 0.5f;
                float plateWidth = intro ? width * 0.8f : Mathf.Min(width * 0.72f, 1100f);
                float textWidth = plateWidth - 48f;
                float boxHeight = intro ? 150f : Mathf.Max(56f, body.CalcHeight(new GUIContent(caption), textWidth) + 24f);
                float bottom = showRotate ? height - 160f : height - 76f;
                float y = intro ? height * 0.58f : bottom - boxHeight;
                float x = (width - plateWidth) * 0.5f;
                Rect plate = new Rect(x, y, plateWidth, boxHeight);
                Plate(plate, new Color(0.12f, 0.08f, 0.16f, 0.92f * alpha));

                GUI.color = new Color(1f, 0.92f, 0.75f, alpha);
                body.normal.textColor = GUI.color;
                GUI.Label(new Rect(x + 24f, y + 12f, textWidth, boxHeight - 24f), caption, body);
                GUI.color = Color.white;
            }

            // Drag hint plate
            if (showRotate)
            {
                const string rotateText = "Kéo chuột trái / phải để xoay ngôi nhà và căn góc nhìn";
                float boxWidth = Mathf.Min(780f, width - 48f);
                float boxHeight = 60f;
                float x = (width - boxWidth) * 0.5f;
                float y = height - 148f;
                Plate(new Rect(x, y, boxWidth, boxHeight), new Color(0.14f, 0.10f, 0.18f, 0.92f));
                prompt.normal.textColor = new Color(1f, 0.85f, 0.45f, 1f);
                GUI.Label(new Rect(x + 16f, y + 6f, boxWidth - 32f, boxHeight - 12f), rotateText, prompt);
            }

            // Bottom left hint
            if (!string.IsNullOrEmpty(hint))
            {
                Plate(new Rect(24f, height - 58f, Mathf.Min(660f, width * 0.5f), 40f), new Color(0.14f, 0.10f, 0.18f, 0.90f));
                small.normal.textColor = new Color(1f, 0.90f, 0.65f, 0.95f);
                GUI.Label(new Rect(36f, height - 54f, Mathf.Min(640f, width * 0.48f), 32f), hint, small);
            }

            // Top right: Performance info & Back button
            if (!string.IsNullOrEmpty(performance))
            {
                Plate(new Rect(width - 560f, 18f, 530f, 32f), new Color(0.14f, 0.10f, 0.18f, 0.85f));
                GUI.Label(new Rect(width - 548f, 20f, 506f, 28f), performance, small);
            }

            Rect backRect = new Rect(width - 240f, 56f, 216f, 32f);
            Plate(backRect, new Color(0.16f, 0.12f, 0.20f, 0.90f));
            if (GUI.Button(backRect, "← Quay lại Chương 1", small))
            {
                SceneManager.LoadScene("Level1");
            }

            GUI.matrix = saved;
        }

        void EnsureStyles()
        {
            if (!stylesReady || title == null || body == null || small == null || prompt == null)
                BuildStyles();
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
            title.normal.textColor = new Color(1f, 0.88f, 0.60f, 1f);

            body = new GUIStyle(title)
            {
                fontSize = 24,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip
            };
            body.normal.textColor = new Color(1f, 0.94f, 0.80f, 1f);

            small = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };
            small.normal.textColor = new Color(0.92f, 0.86f, 0.78f, 1f);

            prompt = new GUIStyle(GUI.skin.label)
            {
                fontSize = 21,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                clipping = TextClipping.Clip
            };
            prompt.normal.textColor = new Color(1f, 0.82f, 0.40f, 1f);

            stylesReady = true;
        }

        static void Plate(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
