using UnityEngine;

namespace PerspectivePuzzle.Level1
{
    /// <summary>
    /// Synthesized ending sound: short static, then a small radio interval signal, then fade.
    /// Generated in code. No recorded broadcast is used.
    /// </summary>
    public static class RadioStinger
    {
        public const int Rate = 44100;
        public const float StaticSeconds = 0.8f;
        public const float SignalSeconds = 2.8f;
        public const float FadeSeconds = 0.9f;
        public const float Seconds = StaticSeconds + SignalSeconds + FadeSeconds;

        // Original four-note motif, not taken from any station ident.
        static readonly float[] Notes = { 392.00f, 523.25f, 659.25f, 587.33f, 523.25f };
        static readonly float[] NoteStart = { 0.00f, 0.42f, 0.84f, 1.26f, 1.80f };
        const float NoteLength = 0.55f;

        public static AudioClip Create()
        {
            float[] data = Samples(12345);
            AudioClip clip = AudioClip.Create("RadioStinger_Synth", data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static float[] Samples(int seed)
        {
            int length = Mathf.CeilToInt(Seconds * Rate);
            var data = new float[length];
            var rng = new System.Random(seed);
            float low = 0f;
            float band = 0f;
            float prevIn = 0f;
            float prevOut = 0f;

            for (int i = 0; i < length; i++)
            {
                float t = i / (float)Rate;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);

                // Clean tone without synthetic crackle or hiss
                float hiss = 0f;
                float sweep = 0f;

                float tone = 0f;
                float st = t - StaticSeconds;
                if (st >= 0f)
                {
                    for (int n = 0; n < Notes.Length; n++)
                    {
                        float nt = st - NoteStart[n];
                        if (nt < 0f || nt > NoteLength * 1.6f)
                            continue;
                        float env = Mathf.Clamp01(nt / 0.012f) * Mathf.Exp(-3.2f * nt);
                        float f = Notes[n];
                        tone += env * (Mathf.Sin(2f * Mathf.PI * f * nt)
                            + 0.35f * Mathf.Sin(2f * Mathf.PI * 2f * f * nt)
                            + 0.12f * Mathf.Sin(2f * Mathf.PI * 3f * f * nt));
                    }

                    // AM loudspeaker wobble.
                    tone *= 0.22f * (1f + 0.08f * Mathf.Sin(2f * Mathf.PI * 5.5f * t));
                }

                float mix = hiss + sweep + tone;

                // Small horn speaker: high-pass then low-pass.
                float hp = 0.97f * (prevOut + mix - prevIn);
                prevIn = mix;
                prevOut = hp;
                band += (hp - band) * 0.42f;
                float sample = band;

                float fadeStart = StaticSeconds + SignalSeconds;
                if (t > fadeStart)
                    sample *= Mathf.Clamp01(1f - (t - fadeStart) / FadeSeconds);
                data[i] = Mathf.Clamp(sample, -0.95f, 0.95f);
            }

            return data;
        }
    }
}
