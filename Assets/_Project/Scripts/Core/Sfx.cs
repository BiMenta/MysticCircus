using System;
using UnityEngine;

namespace MysticCircus
{
    public enum SfxKind { Click, Good, Bad, Door, Bell, Laugh, Pickup, Twang, Fanfare, Step, Page, Crank }

    // Every effect is synthesised at startup, so the project needs no SFX assets to sound complete.
    // Replace a case in Build with an imported clip when recorded audio arrives.
    public sealed class Sfx : MonoBehaviour
    {
        const int Rate = 44100;
        const float Tau = Mathf.PI * 2;
        static Sfx instance;
        AudioSource source;
        AudioClip[] clips;
        AudioClip[] notes;
        // C D E F G, chosen so any melody built from them resolves pleasantly.
        public static readonly float[] NoteFrequencies = { 523.25f, 587.33f, 659.25f, 698.46f, 783.99f };

        void Awake()
        {
            instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            var kinds = (SfxKind[])Enum.GetValues(typeof(SfxKind));
            clips = new AudioClip[kinds.Length];
            foreach (var kind in kinds) clips[(int)kind] = Build(kind);
            notes = new AudioClip[NoteFrequencies.Length];
            for (int i = 0; i < notes.Length; i++)
            {
                float f = NoteFrequencies[i];
                notes[i] = Make("Note" + i, 1.2f, t =>
                    (Mathf.Sin(Tau * f * t) + 0.4f * Mathf.Sin(Tau * f * 2.01f * t) + 0.18f * Mathf.Sin(Tau * f * 4.2f * t) * Mathf.Exp(-t * 16)) * Mathf.Exp(-t * 3.6f) * 0.42f);
            }
        }

        public static void Play(SfxKind kind, float volume = 1)
        {
            if (instance == null) return;
            instance.source.PlayOneShot(instance.clips[(int)kind], volume * GameSettings.SfxVolume);
        }

        public static void Note(int index)
        {
            if (instance == null) return;
            instance.source.PlayOneShot(instance.notes[index], GameSettings.SfxVolume);
        }

        static AudioClip Make(string name, float seconds, Func<float, float> wave)
        {
            var samples = new float[Mathf.CeilToInt(seconds * Rate)];
            for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Clamp(wave(i / (float)Rate), -1, 1);
            // Short release so no clip ends on an audible click.
            int tail = Mathf.Min(400, samples.Length);
            for (int i = 0; i < tail; i++) samples[samples.Length - 1 - i] *= i / (float)tail;
            var clip = AudioClip.Create(name, samples.Length, 1, Rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static float Tone(float f, float t, float start, float decay)
        {
            if (t < start) return 0;
            float local = t - start;
            return Mathf.Sin(Tau * f * local) * Mathf.Exp(-local * decay);
        }

        static AudioClip Build(SfxKind kind)
        {
            var noise = new System.Random(17 + (int)kind);
            float Noise() => (float)noise.NextDouble() * 2 - 1;
            switch (kind)
            {
                case SfxKind.Click: return Make("Click", 0.07f, t => Tone(1400, t, 0, 70) * 0.35f + Noise() * Mathf.Exp(-t * 200) * 0.1f);
                case SfxKind.Good: return Make("Good", 0.5f, t => (Tone(659, t, 0, 9) + Tone(988, t, 0.09f, 8)) * 0.3f);
                case SfxKind.Bad: return Make("Bad", 0.4f, t => (Mathf.Sign(Mathf.Sin(Tau * 104 * t)) + Mathf.Sin(Tau * 111 * t)) * Mathf.Exp(-t * 7) * 0.13f);
                case SfxKind.Door:
                    float low = 0;
                    return Make("Door", 0.8f, t =>
                    {
                        low += (Noise() - low) * 0.05f;
                        return low * Mathf.Exp(-t * 6) * 1.6f + Mathf.Sin(Tau * (70 + 40 * t) * t) * Mathf.Exp(-t * 5) * 0.3f;
                    });
                case SfxKind.Bell: return Make("Bell", 2.4f, t => (Tone(622, t, 0, 1.8f) + 0.6f * Tone(1251, t, 0, 2.6f) + 0.35f * Tone(1742, t, 0, 4) + 0.2f * Tone(2533, t, 0, 7)) * 0.3f);
                case SfxKind.Laugh:
                    return Make("Laugh", 1.5f, t =>
                    {
                        float pitch = 380 - t * 130;
                        float gate = Mathf.Pow(Mathf.Max(0, Mathf.Sin(Tau * 4.2f * t)), 2);
                        float saw = 2 * (t * pitch - Mathf.Floor(t * pitch + 0.5f));
                        return (saw * 0.6f + Mathf.Sin(Tau * pitch * 2 * t) * 0.4f) * gate * Mathf.Exp(-t * 0.9f) * 0.34f;
                    });
                case SfxKind.Pickup: return Make("Pickup", 0.8f, t => (Tone(784, t, 0, 7) + Tone(988, t, 0.08f, 7) + Tone(1175, t, 0.16f, 7) + Tone(1568, t, 0.24f, 5)) * 0.26f);
                case SfxKind.Twang: return Make("Twang", 0.9f, t => Mathf.Sin(Tau * (150 + 60 * Mathf.Exp(-t * 12)) * t + Mathf.Sin(Tau * 7 * t) * 2) * Mathf.Exp(-t * 4.5f) * 0.42f);
                case SfxKind.Fanfare: return Make("Fanfare", 1.5f, t => (Tone(523, t, 0, 3) + Tone(659, t, 0.12f, 3) + Tone(784, t, 0.24f, 3) + Tone(1047, t, 0.4f, 2)) * 0.25f);
                case SfxKind.Step:
                    float soft = 0;
                    return Make("Step", 0.13f, t => { soft += (Noise() - soft) * 0.12f; return soft * Mathf.Exp(-t * 34) * 1.3f; });
                case SfxKind.Page: return Make("Page", 0.32f, t => Noise() * Mathf.Sin(Mathf.PI * t / 0.32f) * Mathf.Abs(Mathf.Sin(Tau * 20 * t)) * 0.16f);
                case SfxKind.Crank: return Make("Crank", 0.5f, t => Noise() * Mathf.Pow(Mathf.Max(0, Mathf.Sin(Tau * 11 * t)), 16) * 0.5f);
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }
}
