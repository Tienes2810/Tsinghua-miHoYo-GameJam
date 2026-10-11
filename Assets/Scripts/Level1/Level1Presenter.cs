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

        public bool ShowStartMenu = true;
        public bool ShowControlsModal;

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
            bool isAlt = GameInput.GetKey(KeyCode.LeftAlt) || GameInput.GetKey(KeyCode.RightAlt);
            if (GameInput.GetKeyDown(KeyCode.F11) || (isAlt && GameInput.GetKeyDown(KeyCode.Return)))
            {
                ScreenUtils.ToggleFullscreen();
            }

            if (GameInput.GetKeyDown(KeyCode.H))
            {
                ShowControlsModal = !ShowControlsModal;
                PerspectivePuzzle.Audio.GameAudio.Instance?.PlayClick();
            }

            if (ShowControlsModal)
            {
                if (GameInput.GetKeyDown(KeyCode.Escape))
                {
                    ShowControlsModal = false;
                    PerspectivePuzzle.Audio.GameAudio.Instance?.PlayClick();
                }
                return;
            }

            if (ShowStartMenu)
            {
                if (!isAlt && (GameInput.GetKeyDown(KeyCode.Return) || GameInput.GetKeyDown(KeyCode.KeypadEnter) || GameInput.GetKeyDown(KeyCode.Space)))
                {
                    ShowStartMenu = false;
                    PerspectivePuzzle.Audio.GameAudio.Instance?.PlayClick();
                }
                return;
            }

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
                if (GUI.Button(btnRect, "↺  CHƠI LẠI TỪ CHƯƠNG 1 (HOÀNG HÔN HÀ NỘI)", prompt))
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Level2");
                }

                Color faint = new Color(0.7f, 0.7f, 0.66f, 0.7f);
                small.normal.textColor = faint;
                small.alignment = TextAnchor.MiddleCenter;
                GUI.color = faint;
                GUI.Label(new Rect(0f, height - 60f, width, 30f), "Hoàn thành toàn bộ trò chơi  ·  Enter: Chơi lại từ đầu  ·  Esc: Thoát / Quit", small);
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

            if (ShowStartMenu)
            {
                DrawStartMenu(width, height);
                if (ShowControlsModal)
                {
                    DrawControlsModal(width, height);
                }
                GUI.matrix = saved;
                return;
            }

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

            // Top-right buttons
            Rect helpBtnRect = new Rect(width - 834f, 56f, 230f, 34f);
            Plate(helpBtnRect);
            if (GUI.Button(helpBtnRect, "📖 Hướng dẫn / Guide (H)", small))
            {
                ShowControlsModal = !ShowControlsModal;
                PerspectivePuzzle.Audio.GameAudio.Instance?.PlayClick();
            }

            Rect fsRect = new Rect(width - 594f, 56f, 310f, 34f);
            Plate(fsRect);
            string fsLabel = ScreenUtils.IsFullscreen ? "🗗 Cửa sổ / Window (F11)" : "⛶ Toàn màn hình / Fullscreen (F11)";
            if (GUI.Button(fsRect, fsLabel, small))
            {
                ScreenUtils.ToggleFullscreen();
                PerspectivePuzzle.Audio.GameAudio.Instance?.PlayClick();
            }

            Rect chapRect = new Rect(width - 274f, 56f, 250f, 34f);
            Plate(chapRect);
            if (GUI.Button(chapRect, "← Chương 1 / Chapter 1", small))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Level2");
            }

            if (ShowControlsModal)
            {
                DrawControlsModal(width, height);
            }

            GUI.matrix = saved;
        }

        void DrawStartMenu(float width, float height)
        {
            // Dark elegant background
            GUI.color = new Color(0.04f, 0.045f, 0.055f, 0.94f);
            GUI.DrawTexture(new Rect(0f, 0f, width, height), Texture2D.whiteTexture);

            float centerX = width * 0.5f;
            float topY = Mathf.Max(30f, height * 0.12f);

            // Header: Jam info
            Color gold = new Color(1f, 0.82f, 0.35f, 1f);
            small.alignment = TextAnchor.MiddleCenter;
            small.normal.textColor = gold;
            GUI.color = gold;
            GUI.Label(new Rect(0f, topY, width, 28f), "TSINGHUA UNIVERSITY  ×  MIHOYO GAME JAM 2026", small);

            // Title
            GUIStyle bigTitle = new GUIStyle(title)
            {
                fontSize = 44,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            Color ink = new Color(0.96f, 0.95f, 0.92f, 1f);
            bigTitle.normal.textColor = ink;
            GUI.color = ink;
            GUI.Label(new Rect(0f, topY + 36f, width, 58f), "HỒI ỨC PHỐ CŨ", bigTitle);

            // Subtitle
            Color soft = new Color(0.85f, 0.85f, 0.80f, 0.85f);
            small.normal.textColor = soft;
            GUI.color = soft;
            GUI.Label(new Rect(0f, topY + 98f, width, 28f), "CHƯƠNG 2: NGÕ PHỐ & TIẾNG LOA · CHAPTER 2: ALLEYWAYS & LOUDSPEAKER", small);

            // Gold divider
            GUI.color = new Color(1f, 0.82f, 0.35f, 0.45f);
            GUI.DrawTexture(new Rect(centerX - 180f, topY + 134f, 360f, 2f), Texture2D.whiteTexture);

            // 1. Start Button
            float btnW = 460f;
            float btnH = 58f;
            float curY = topY + 160f;
            Rect startRect = new Rect(centerX - btnW * 0.5f, curY, btnW, btnH);
            Plate(startRect);
            prompt.alignment = TextAnchor.MiddleCenter;
            prompt.normal.textColor = gold;
            GUI.color = gold;
            if (GUI.Button(startRect, "▶  BẮT ĐẦU CHƠI  /  START GAME", prompt))
            {
                ShowStartMenu = false;
                PerspectivePuzzle.Audio.GameAudio.Instance?.PlayClick();
            }

            // 2. Fullscreen Button
            curY += 70f;
            Rect fsRect = new Rect(centerX - btnW * 0.5f, curY, btnW, 48f);
            Plate(fsRect);
            string fsLabel = ScreenUtils.IsFullscreen ? "🗗  CỬA SỔ  /  WINDOWED (F11)" : "⛶  TOÀN MÀN HÌNH  /  FULLSCREEN (F11)";
            Color fsColor = new Color(0.94f, 0.94f, 0.94f, 1f);
            small.alignment = TextAnchor.MiddleCenter;
            small.normal.textColor = fsColor;
            GUI.color = fsColor;
            if (GUI.Button(fsRect, fsLabel, small))
            {
                ScreenUtils.ToggleFullscreen();
                PerspectivePuzzle.Audio.GameAudio.Instance?.PlayClick();
            }

            // 3. Controls Guide Button (Interactive & Clickable!)
            curY += 58f;
            Rect helpRect = new Rect(centerX - btnW * 0.5f, curY, btnW, 48f);
            Plate(helpRect);
            Color guideBtnColor = new Color(1f, 0.84f, 0.45f, 1f);
            small.normal.textColor = guideBtnColor;
            GUI.color = guideBtnColor;
            if (GUI.Button(helpRect, "📖  HƯỚNG DẪN  ·  CONTROLS GUIDE (H)", small))
            {
                ShowControlsModal = true;
                PerspectivePuzzle.Audio.GameAudio.Instance?.PlayClick();
            }

            // Bottom controls strip (also clickable to open full guide)
            float infoW = Mathf.Min(820f, width - 60f);
            float infoH = 68f;
            Rect infoRect = new Rect(centerX - infoW * 0.5f, height - infoH - 24f, infoW, infoH);
            Plate(infoRect);

            if (GUI.Button(infoRect, GUIContent.none, GUIStyle.none))
            {
                ShowControlsModal = true;
                PerspectivePuzzle.Audio.GameAudio.Instance?.PlayClick();
            }

            small.alignment = TextAnchor.MiddleLeft;
            Color guideHeader = new Color(1f, 0.84f, 0.45f, 0.95f);
            small.normal.textColor = guideHeader;
            GUI.color = guideHeader;
            GUI.Label(new Rect(infoRect.x + 20f, infoRect.y + 8f, infoW - 40f, 22f), "HƯỚNG DẪN ĐIỀU KHIỂN  ·  CONTROLS (Click xem chi tiết / Full guide)", small);

            Color guideBody = new Color(0.88f, 0.88f, 0.85f, 0.85f);
            small.normal.textColor = guideBody;
            GUI.color = guideBody;
            string guideText = "• Click: Đi / Walk  ·  Kéo / Drag: Xoay góc nhìn / Rotate  ·  F11: Toàn màn hình / Fullscreen\n" +
                               "• Nhấn phím H hoặc click ô này để mở hướng dẫn chi tiết / Press H for controls";
            GUI.Label(new Rect(infoRect.x + 20f, infoRect.y + 30f, infoW - 40f, 32f), guideText, small);

            GUI.color = Color.white;
        }

        void DrawControlsModal(float width, float height)
        {
            // Dim background behind modal
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0f, 0f, width, height), Texture2D.whiteTexture);

            float modalW = Mathf.Min(780f, width - 60f);
            float modalH = 350f;
            Rect modalRect = new Rect((width - modalW) * 0.5f, (height - modalH) * 0.5f, modalW, modalH);

            Plate(modalRect);

            // Modal Header
            Color gold = new Color(1f, 0.82f, 0.35f, 1f);
            small.alignment = TextAnchor.MiddleCenter;
            small.normal.textColor = gold;
            GUI.color = gold;
            GUI.Label(new Rect(modalRect.x, modalRect.y + 16f, modalW, 28f), "📖  HƯỚNG DẪN ĐIỀU KHIỂN  ·  CONTROLS GUIDE", small);

            // Divider
            GUI.color = new Color(1f, 0.82f, 0.35f, 0.35f);
            GUI.DrawTexture(new Rect(modalRect.x + 36f, modalRect.y + 48f, modalW - 72f, 1f), Texture2D.whiteTexture);

            // Instructions text
            Color soft = new Color(0.92f, 0.92f, 0.90f, 1f);
            small.normal.textColor = soft;
            GUI.color = soft;
            small.alignment = TextAnchor.MiddleLeft;
            string guide =
                "• Chuột trái (Left Click): Click điểm vàng hoặc sàn để di chuyển nhân vật (Walk)\n" +
                "• Kéo chuột (Drag): Xoay góc nhìn kiến trúc để nối các lối đi đánh lừa thị giác (Perspective shift)\n" +
                "• Phím F11 / Alt+Enter: Bật/Tắt toàn màn hình · Toggle Fullscreen\n" +
                "• Phím R: Khởi động lại khi muốn chơi lại từ đầu · Restart\n" +
                "• Phím H / Esc: Đóng hoặc mở lại bảng hướng dẫn này · Toggle Guide";
            GUI.Label(new Rect(modalRect.x + 36f, modalRect.y + 64f, modalW - 72f, 190f), guide, small);

            // Close button
            float closeW = 280f;
            float closeH = 46f;
            Rect closeRect = new Rect(modalRect.x + (modalW - closeW) * 0.5f, modalRect.y + modalH - 60f, closeW, closeH);
            Plate(closeRect);
            prompt.alignment = TextAnchor.MiddleCenter;
            prompt.normal.textColor = gold;
            GUI.color = gold;
            if (GUI.Button(closeRect, "✕  ĐÃ HIỂU  /  GOT IT", prompt))
            {
                ShowControlsModal = false;
                PerspectivePuzzle.Audio.GameAudio.Instance?.PlayClick();
            }

            GUI.color = Color.white;
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
