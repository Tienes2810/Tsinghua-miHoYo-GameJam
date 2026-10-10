using UnityEngine;

namespace PerspectivePuzzle.Level1
{
    /// <summary>
    /// One ink/plate pair for every on-screen line. Tests read these fields.
    /// </summary>
    public static class HudColors
    {
        public static readonly Color Plate = new Color(0.32f, 0.24f, 0.18f, 0.94f);
        public static readonly Color Ink = new Color(0.98f, 0.96f, 0.90f, 1f);

        public static float Luminance(Color color)
        {
            return (0.2126f * color.r) + (0.7152f * color.g) + (0.0722f * color.b);
        }

        public static bool Readable(Color ink, Color plate)
        {
            bool blackOnBlack = Luminance(ink) < 0.2f && Luminance(plate) < 0.2f;
            return !blackOnBlack && Mathf.Abs(Luminance(ink) - Luminance(plate)) >= 0.45f;
        }
    }
}
