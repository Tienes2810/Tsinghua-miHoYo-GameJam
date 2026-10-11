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

            GUI.matrix = saved;
        }

        void DrawIntro(Level2Game g, float w, float h)
        {
            Color prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.92f);
            GUI.DrawTexture(new Rect(0, h * 0.4f, w, h * 0.6f), vignette, ScaleMode.StretchToFill);
            GUI.color = prev;

            float a = Mathf.Clamp01(Time.time / 1.2f);
            Paint(smallStyle, new Color(GoldInk.r, GoldInk.g, GoldInk.b, a));
            smallStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(0, h * 0.6f, w, 30), "CHƯƠNG 2  ·  CHAPTER 2", smallStyle);
            Paint(bigStyle, new Color(Ink.r, Ink.g, Ink.b, a));
            GUI.Label(new Rect(0, h * 0.63f, w, 90), "Hà Nội Hoàng Hôn", bigStyle);
            Paint(titleStyle, new Color(Ink.r, Ink.g, Ink.b, a * 0.9f));
            titleStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(0, h * 0.63f + 86, w, 34), "Hanoi at Dusk", titleStyle);
            Paint(bodyStyle, new Color(Ink.r, Ink.g, Ink.b, a * 0.85f));
            GUI.Label(new Rect(w * 0.15f, h * 0.63f + 124, w * 0.70f, 70),
                "Thang máy đỏ, nhịp cầu xoay, và góc nhìn ảo ảnh đưa lối về phố cổ.\nRide the red lift, turn the bridge, and find the impossible path through the old quarter.", bodyStyle);
            float blink = 0.55f + 0.45f * Mathf.Sin(Time.time * 3f);
            Paint(titleStyle, new Color(GoldInk.r, GoldInk.g, GoldInk.b, a * blink));
            GUI.Label(new Rect(0, h - 70, w, 36), "Nhấn bất kỳ để bắt đầu  ·  Click anywhere to begin", titleStyle);
            titleStyle.alignment = TextAnchor.MiddleLeft;
            smallStyle.alignment = TextAnchor.MiddleLeft;
            DrawTopButtons(g, w, false);
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
                GUI.Label(new Rect(42, 26, pw - 30, 22), "CHƯƠNG 2 · HÀ NỘI HOÀNG HÔN / CHAPTER 2", smallStyle);
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
            if (Button(new Rect(w - 294f, y, 270f, 40f), "← Chương 1 / Chapter 1", Plate))
                g.BackToChapter1();
            if (withRestart && Button(new Rect(w - 294f, y + 48f, 270f, 40f), "Chơi lại / Restart  (R)", Plate))
                g.Restart();
        }

        void DrawEnd(Level2Game g, float w, float h)
        {
            float t = Time.time - g.EndAt;
            float a = Mathf.Clamp01(t / 1.2f);
            Fill(new Rect(0, 0, w, h), new Color(0.03f, 0.03f, 0.08f, 0.5f * a));
            Paint(smallStyle, new Color(GoldInk.r, GoldInk.g, GoldInk.b, a));
            smallStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(0, h * 0.25f, w, 30), "CHƯƠNG 2 HOÀN THÀNH  ·  CHAPTER 2 COMPLETE", smallStyle);
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
                if (Button(new Rect(x + bw + gap, y, bw, 56f), "← Chương 1 / Chapter 1  (C)", Plate))
                    g.BackToChapter1();
                if (Button(new Rect(x + (bw + gap) * 2, y, bw, 56f), "Thoát / Quit  (Esc)", Plate))
                    Application.Quit();
            }
        }
    }
}
