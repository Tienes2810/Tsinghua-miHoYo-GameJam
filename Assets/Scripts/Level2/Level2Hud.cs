using System.Collections.Generic;
using PerspectivePuzzle.Level1;
using UnityEngine;

namespace PerspectivePuzzle.Level2
{
    /// <summary>
    /// IMGUI overlay for Chapter 2: intro card, objective, lantern pips, captions, toasts,
    /// hint plate, top-right buttons and the end screen. Real mouse clicks on these buttons are
    /// handled by IMGUI; every button also has a keyboard shortcut read through GameInput.
    /// </summary>
    public class Level2Hud : MonoBehaviour
    {
        static readonly Color Plate = HudColors.Plate;
        static readonly Color Ink = HudColors.Ink;
        static readonly Color GoldInk = new(1f, 0.84f, 0.45f, 1f);

        GUIStyle titleStyle;
        GUIStyle bodyStyle;
        GUIStyle smallStyle;
        GUIStyle buttonStyle;
        GUIStyle bigStyle;
        Texture2D dot;
        Texture2D vignette;
        readonly List<Rect> uiRects = new();
        float scale = 1f;

        Level2Game Game => Level2Game.Instance;

        /// <summary>True when a screen point (pixels, origin bottom-left) is over a HUD button.</summary>
        public bool OverUi(Vector2 screen)
        {
            Vector2 gui = new(screen.x / scale, (Screen.height - screen.y) / scale);
            for (int i = 0; i < uiRects.Count; i++)
                if (uiRects[i].Contains(gui))
                    return true;
            return false;
        }

        public bool ShowControlsModal;

        void Update()
        {
            if (GameInput.GetKeyDown(KeyCode.F11) || ((GameInput.GetKey(KeyCode.LeftAlt) || GameInput.GetKey(KeyCode.RightAlt)) && GameInput.GetKeyDown(KeyCode.Return)))
            {
                ScreenUtils.ToggleFullscreen();
            }

            if (GameInput.GetKeyDown(KeyCode.H))
            {
                ShowControlsModal = !ShowControlsModal;
                PerspectivePuzzle.Audio.GameAudio.Instance?.PlayClick();
            }

            if (ShowControlsModal && GameInput.GetKeyDown(KeyCode.Escape))
            {
                ShowControlsModal = false;
                PerspectivePuzzle.Audio.GameAudio.Instance?.PlayClick();
            }
        }

        void EnsureStyles()
        {
            if (titleStyle != null)
                return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, wordWrap = true, alignment = TextAnchor.MiddleLeft };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, wordWrap = true, alignment = TextAnchor.MiddleCenter };
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, alignment = TextAnchor.MiddleLeft };
            buttonStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            bigStyle = new GUIStyle(GUI.skin.label) { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            dot = MakeDot(64);
            vignette = MakeVignette();
        }

        static Texture2D MakeDot(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                    float a = Mathf.Clamp01((1f - d) * 6f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            return tex;
        }

        static Texture2D MakeVignette()
        {
            const int h = 64;
            var tex = new Texture2D(1, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                tex.SetPixel(0, y, new Color(0f, 0f, 0f, Mathf.Pow(1f - t, 2f)));
            }
            tex.Apply();
            return tex;
        }

        static void Paint(GUIStyle s, Color c)
        {
            s.normal.textColor = c;
            s.hover.textColor = c;
            s.active.textColor = c;
        }

        void Fill(Rect r, Color c)
        {
            Color prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        bool Button(Rect r, string label, Color plate)
        {
            uiRects.Add(r);
            bool hover = r.Contains(Event.current.mousePosition);
            Fill(r, hover ? Color.Lerp(plate, Color.white, 0.12f) : plate);
            Fill(new Rect(r.x, r.yMax - 3f, r.width, 3f), GoldInk * new Color(1, 1, 1, 0.8f));
            Paint(buttonStyle, Ink);
            return GUI.Button(r, label, buttonStyle);
        }

        void OnGUI()
        {
            Level2Game g = Game;
            if (g == null)
                return;
            EnsureStyles();
            uiRects.Clear();
            scale = Mathf.Max(0.7f, Screen.height / 1080f);
            Matrix4x4 saved = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            float w = Screen.width / scale;
            float h = Screen.height / scale;

            switch (g.State)
            {
                case Level2Game.Phase.Intro:
                    DrawIntro(g, w, h);
                    break;
                case Level2Game.Phase.End:
                    DrawEnd(g, w, h);
                    break;
                default:
                    DrawPlay(g, w, h);
                    break;
            }

            if (ShowControlsModal)
            {
                DrawControlsModal(w, h);
            }

            GUI.matrix = saved;
        }

        void DrawIntro(Level2Game g, float w, float h)
        {
            // Dark vignette background over the sunset diorama
            Color prev = GUI.color;
            GUI.color = new Color(0.04f, 0.045f, 0.055f, 0.94f);
            GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
            GUI.color = prev;

            float centerX = w * 0.5f;
            float topY = Mathf.Max(30f, h * 0.10f);

            // Jam Header
            Paint(smallStyle, GoldInk);
            smallStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(0, topY, w, 28), "TSINGHUA UNIVERSITY  ×  MIHOYO GAME JAM 2026", smallStyle);

            // Title
            Paint(bigStyle, Ink);
            GUI.Label(new Rect(0, topY + 34, w, 70), "HỒI ỨC PHỐ CŨ", bigStyle);

            // Subtitle
            Paint(smallStyle, new Color(0.85f, 0.85f, 0.80f, 0.9f));
            GUI.Label(new Rect(0, topY + 104, w, 28), "CHƯƠNG 1: HÀ NỘI HOÀNG HÔN  ·  CHAPTER 1: HANOI AT DUSK", smallStyle);

            // Gold divider
            Fill(new Rect(centerX - 180f, topY + 138f, 360f, 2f), GoldInk * new Color(1, 1, 1, 0.5f));

            // Story Description
            Paint(bodyStyle, new Color(Ink.r, Ink.g, Ink.b, 0.9f));
            GUI.Label(new Rect(centerX - 380f, topY + 150f, 760f, 54f),
                "Thang máy đỏ, nhịp cầu xoay, và góc nhìn ảo ảnh đưa lối về phố cổ.\nRide the red lift, turn the bridge, and find the impossible path through the old quarter.", bodyStyle);

            // 1. Start Game Button
            float btnW = 460f;
            float btnH = 56f;
            float curY = topY + 220f;
            Rect startRect = new Rect(centerX - btnW * 0.5f, curY, btnW, btnH);
            if (Button(startRect, "▶  BẮT ĐẦU CHƠI  /  START GAME", new Color(0.55f, 0.16f, 0.12f, 0.95f)))
            {
                g.BeginPlay();
            }

            // 2. Fullscreen Button
            curY += 66f;
            Rect fsRect = new Rect(centerX - btnW * 0.5f, curY, btnW, 46f);
            string fsLabel = ScreenUtils.IsFullscreen ? "🗗  CỬA SỔ  /  WINDOWED (F11)" : "⛶  TOÀN MÀN HÌNH  /  FULLSCREEN (F11)";
            if (Button(fsRect, fsLabel, Plate))
            {
                ScreenUtils.ToggleFullscreen();
                PerspectivePuzzle.Audio.GameAudio.Instance?.PlayClick();
            }

            // 3. Controls Guide Button
            curY += 56f;
            Rect helpRect = new Rect(centerX - btnW * 0.5f, curY, btnW, 46f);
            if (Button(helpRect, "📖  HƯỚNG DẪN  ·  CONTROLS GUIDE (H)", Plate))
            {
                ShowControlsModal = true;
                PerspectivePuzzle.Audio.GameAudio.Instance?.PlayClick();
            }

            // 4. Chapter 2 shortcut button
            curY += 56f;
            Rect chapRect = new Rect(centerX - btnW * 0.5f, curY, btnW, 40f);
            if (Button(chapRect, "Sang Chương 2 / Chapter 2 →", Plate))
            {
                g.GoToChapter2();
            }

            // Bottom controls strip
            float infoW = Mathf.Min(840f, w - 60f);
            float infoH = 68f;
            Rect infoRect = new Rect(centerX - infoW * 0.5f, h - infoH - 24f, infoW, infoH);
            Fill(infoRect, Plate);
            Fill(new Rect(infoRect.x, infoRect.yMax - 3f, infoRect.width, 3f), GoldInk * new Color(1, 1, 1, 0.8f));

            if (GUI.Button(infoRect, GUIContent.none, GUIStyle.none))
            {
                ShowControlsModal = true;
                PerspectivePuzzle.Audio.GameAudio.Instance?.PlayClick();
            }

            smallStyle.alignment = TextAnchor.MiddleLeft;
            Paint(smallStyle, GoldInk);
            GUI.Label(new Rect(infoRect.x + 20f, infoRect.y + 8f, infoW - 40f, 22f), "HƯỚNG DẪN ĐIỀU KHIỂN  ·  CONTROLS (Click xem chi tiết / Full guide)", smallStyle);

            Paint(smallStyle, new Color(0.88f, 0.88f, 0.85f, 0.90f));
            string guideSummary = "• Click: Đi / Walk  ·  Kéo / Drag: Xoay phố, thang máy & cầu / Rotate diorama & lifts  ·  F11: Toàn màn hình\n" +
                                  "• Nhấn phím H hoặc click ô này để mở bảng hướng dẫn chi tiết / Press H for full guide";
            GUI.Label(new Rect(infoRect.x + 20f, infoRect.y + 30f, infoW - 40f, 32f), guideSummary, smallStyle);
        }

        void DrawControlsModal(float w, float h)
        {
            // Dim background behind modal
            Color prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.78f);
            GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
            GUI.color = prev;

            float modalW = Mathf.Min(820f, w - 60f);
            float modalH = 390f;
            Rect modalRect = new Rect((w - modalW) * 0.5f, (h - modalH) * 0.5f, modalW, modalH);

            Fill(modalRect, Plate);
            Fill(new Rect(modalRect.x, modalRect.yMax - 4f, modalRect.width, 4f), GoldInk);

            // Modal Header
            Paint(titleStyle, GoldInk);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(modalRect.x, modalRect.y + 16f, modalW, 28f), "📖  HƯỚNG DẪN ĐIỀU KHIỂN  ·  CONTROLS GUIDE", titleStyle);

            // Modal Body Rows
            float rowY = modalRect.y + 54f;
            float rowH = 46f;
            float padX = modalRect.x + 28f;
            float contentW = modalW - 56f;

            string[,] rows = {
                { "🖱️  ĐI LẠI  /  WALK", "Click chuột trái vào các ô vuông hoặc điểm đường để nhân vật di chuyển.\nLeft-click reachable path nodes to walk." },
                { "🔄  XOAY GÓC NHÌN  /  ROTATE", "Kéo chuột ngang trên nền trời (hoặc phím Q / E) để xoay phố cổ.\nDrag horizontally on the sky or press Q / E to rotate the diorama." },
                { "🏗️  THANG MÁY & CẦU  /  LIFTS", "Kéo hoặc click các cơ cấu màu đỏ để nâng hạ thang máy hoặc xoay nhịp cầu.\nDrag or click red mechanisms to move the lift or rotate the bridge." },
                { "📐  ẢO ẢNH  /  PERSPECTIVE", "Xoay góc nhìn đến khi các con đường rời rạc nối liền mạch nhau.\nRotate the view until impossible optical paths seamlessly align!" },
                { "⌨️  PHÍM TẮT  /  SHORTCUTS", "F11: Toàn màn hình (Fullscreen)  ·  R: Chơi lại (Restart)  ·  H: Bật/Tắt hướng dẫn" }
            };

            smallStyle.alignment = TextAnchor.MiddleLeft;
            for (int i = 0; i < 5; i++)
            {
                Fill(new Rect(padX, rowY + i * (rowH + 6f), contentW, rowH), new Color(0f, 0f, 0f, 0.25f));
                Paint(smallStyle, GoldInk);
                GUI.Label(new Rect(padX + 10f, rowY + i * (rowH + 6f) + 4f, 230f, 38f), rows[i, 0], smallStyle);

                Paint(smallStyle, new Color(0.92f, 0.92f, 0.90f, 0.95f));
                GUI.Label(new Rect(padX + 246f, rowY + i * (rowH + 6f) + 4f, contentW - 256f, 38f), rows[i, 1], smallStyle);
            }

            // Close button
            float closeW = 260f;
            float closeH = 42f;
            Rect closeRect = new Rect(modalRect.x + (modalW - closeW) * 0.5f, modalRect.y + modalH - closeH - 14f, closeW, closeH);
            if (Button(closeRect, "×  ĐÓNG  /  CLOSE (Esc)", new Color(0.55f, 0.16f, 0.12f, 0.95f)))
            {
                ShowControlsModal = false;
                PerspectivePuzzle.Audio.GameAudio.Instance?.PlayClick();
            }
        }

        void DrawPlay(Level2Game g, float w, float h)
        {
            // objective plate
            if (!string.IsNullOrEmpty(g.Objective))
            {
                float pw = Mathf.Min(780f, w * 0.60f);
                float ph = Mathf.Max(76f, titleStyle.CalcHeight(new GUIContent(g.Objective), pw - 32f) + 38f);
                Fill(new Rect(24, 22, pw, ph), Plate);
                Fill(new Rect(24, 22, 4, ph), GoldInk);
                Paint(smallStyle, GoldInk);
                GUI.Label(new Rect(42, 26, pw - 30, 22), "CHƯƠNG 1 · HÀ NỘI HOÀNG HÔN / CHAPTER 1: HANOI SUNSET", smallStyle);
                Paint(titleStyle, Ink);
                GUI.Label(new Rect(42, 46, pw - 32, ph - 30), g.Objective, titleStyle);
            }

            // lantern pips
            {
                float px = w - 304f;
                Rect plate = new(px, 22, 280, 52);
                Fill(plate, Plate);
                Paint(smallStyle, Ink);
                GUI.Label(new Rect(px + 14, 22, 110, 52), "Đèn lồng · Lanterns", smallStyle);
                for (int i = 0; i < 3; i++)
                {
                    bool lit = i < g.LanternsLit;
                    Color c = lit ? new Color(1f, 0.62f, 0.3f, 1f) : new Color(0.55f, 0.3f, 0.28f, 0.8f);
                    Color prev = GUI.color;
                    if (lit)
                    {
                        GUI.color = new Color(1f, 0.7f, 0.35f, 0.35f + 0.15f * Mathf.Sin(Time.time * 4f + i));
                        GUI.DrawTexture(new Rect(px + 108 + i * 38 - 6, 30, 36, 36), dot);
                    }
                    GUI.color = c;
                    GUI.DrawTexture(new Rect(px + 108 + i * 38, 36, 24, 24), dot);
                    GUI.color = prev;
                }
            }

            DrawTopButtons(g, w, true);

            // caption
            float since = Time.time - g.CaptionAt;
            if (!string.IsNullOrEmpty(g.Caption) && since < g.CaptionFor)
            {
                float alpha = Mathf.Clamp01(since / 0.5f) * Mathf.Clamp01((g.CaptionFor - since) / 0.8f);
                float cw = Mathf.Min(880f, w * 0.58f);
                float ch = bodyStyle.CalcHeight(new GUIContent(g.Caption), cw - 48f) + 28f;
                Rect r = new((w - cw) * 0.5f, Mathf.Max(132f, h * 0.17f), cw, ch);
                Fill(r, new Color(Plate.r, Plate.g, Plate.b, Plate.a * alpha));
                Paint(bodyStyle, new Color(Ink.r, Ink.g, Ink.b, alpha));
                bodyStyle.fontStyle = FontStyle.Italic;
                GUI.Label(new Rect(r.x + 24, r.y + 12, r.width - 48, r.height - 24), g.Caption, bodyStyle);
                bodyStyle.fontStyle = FontStyle.Normal;
            }

            // toast
            float ts = Time.time - g.ToastAt;
            if (!string.IsNullOrEmpty(g.Toast) && ts < 2.2f)
            {
                float alpha = Mathf.Clamp01((2.2f - ts) / 0.4f);
                Vector2 size = buttonStyle.CalcSize(new GUIContent(g.Toast));
                Rect r = new((w - size.x - 40f) * 0.5f, h - 120f, size.x + 40f, 44f);
                Fill(r, new Color(0.45f, 0.14f, 0.12f, 0.9f * alpha));
                Paint(buttonStyle, new Color(1f, 0.95f, 0.9f, alpha));
                GUI.Label(r, g.Toast, buttonStyle);
            }

            // controls strip
            if (g.State == Level2Game.Phase.Play)
            {
                string controls = g.HintActive
                    ? "GỢI Ý: Theo dấu kim cương sáng · Phần đỏ có thể kéo/click | HINT: Follow the glowing diamond"
                    : "Click ô để đi  ·  Kéo/click thang máy & cầu  ·  Kéo trời hoặc Q/E xoay phố  ·  R: Chơi lại (Restart)";
                float cw = Mathf.Min(1180f, w - 48f);
                Rect r = new((w - cw) * 0.5f, h - 58f, cw, 38f);
                Fill(r, g.HintActive ? new Color(0.55f, 0.36f, 0.1f, 0.92f) : new Color(Plate.r, Plate.g, Plate.b, 0.8f));
                Paint(smallStyle, Ink);
                smallStyle.alignment = TextAnchor.MiddleCenter;
                GUI.Label(r, controls, smallStyle);
                smallStyle.alignment = TextAnchor.MiddleLeft;
            }
        }

        void DrawTopButtons(Level2Game g, float w, bool withRestart)
        {
            float y = withRestart ? 84f : 22f;
            float btnW = 310f;
            if (Button(new Rect(w - (btnW + 24f), y, btnW, 38f), "Tiếp tục: Chương 2 → / Chapter 2 →", Plate))
                g.GoToChapter2();
            if (withRestart && Button(new Rect(w - (btnW + 24f), y + 44f, btnW, 38f), "Chơi lại / Restart  (R)", Plate))
                g.Restart();
            if (Button(new Rect(w - (btnW + 24f), y + (withRestart ? 88f : 44f), btnW, 38f), "📖 Hướng dẫn / Guide (H)", Plate))
            {
                ShowControlsModal = !ShowControlsModal;
                PerspectivePuzzle.Audio.GameAudio.Instance?.PlayClick();
            }
            string fsLabel = ScreenUtils.IsFullscreen ? "🗗 Cửa sổ / Window (F11)" : "⛶ Toàn màn hình / Fullscreen (F11)";
            if (Button(new Rect(w - (btnW + 24f), y + (withRestart ? 132f : 88f), btnW, 38f), fsLabel, Plate))
                ScreenUtils.ToggleFullscreen();
        }

        void DrawEnd(Level2Game g, float w, float h)
        {
            float t = Time.time - g.EndAt;
            float a = Mathf.Clamp01(t / 1.2f);
            Fill(new Rect(0, 0, w, h), new Color(0.03f, 0.03f, 0.08f, 0.5f * a));
            Paint(smallStyle, new Color(GoldInk.r, GoldInk.g, GoldInk.b, a));
            smallStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(0, h * 0.25f, w, 30), "CHƯƠNG 1 HOÀN THÀNH  ·  CHAPTER 1 COMPLETE", smallStyle);
            Paint(bigStyle, new Color(Ink.r, Ink.g, Ink.b, a));
            GUI.Label(new Rect(0, h * 0.28f, w, 90), "Hà Nội lên đèn", bigStyle);
            Paint(bodyStyle, new Color(Ink.r, Ink.g, Ink.b, a * 0.9f));
            GUI.Label(new Rect(w * 0.12f, h * 0.28f + 96, w * 0.76f, 80),
                "Ba ngọn đèn đã thắp, chuông đã ngân. Phố cổ nhớ lối trở về.\nThree lanterns lit, one bell rung. The old quarter remembers the way home.", bodyStyle);
            float elapsed = Mathf.Max(0f, g.EndAt - g.StartedAt);
            int mins = Mathf.FloorToInt(elapsed / 60f);
            int secs = Mathf.FloorToInt(elapsed % 60f);
            Paint(smallStyle, new Color(Ink.r, Ink.g, Ink.b, a * 0.75f));
            GUI.Label(new Rect(0, h * 0.28f + 176, w, 28), $"Thời gian / Time: {mins}:{secs:00}   ·   Số bước / Steps: {g.Moves}", smallStyle);
            smallStyle.alignment = TextAnchor.MiddleLeft;

            if (t > 1.2f)
            {
                float bw = 300f;
                float gap = 24f;
                float x = (w - bw * 3 - gap * 2) * 0.5f;
                float y = h * 0.29f + 230f;
                if (Button(new Rect(x, y, bw, 56f), "Chơi lại / Replay  (R)", new Color(0.55f, 0.16f, 0.12f, 0.95f)))
                    g.Restart();
                if (Button(new Rect(x + bw + gap, y, bw, 56f), "Sang Chương 2 / Chapter 2  (C)", Plate))
                    g.GoToChapter2();
                if (Button(new Rect(x + (bw + gap) * 2, y, bw, 56f), "Thoát / Quit  (Esc)", Plate))
                    Application.Quit();
            }
        }
    }
}
