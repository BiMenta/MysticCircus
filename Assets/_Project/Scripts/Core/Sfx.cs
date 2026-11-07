using System;
using UnityEngine;

namespace MysticCircus
{
    // Append new kinds at the end: scenes and tests refer to them by number.
    public enum SfxKind { Click, Good, Bad, Door, Bell, Laugh, Pickup, Twang, Fanfare, Step, Page, Crank, Shutter, Heartbeat, Glass, Tap, Slide, Mechanism, Ripple }

    // Every effect is synthesised at startup, so the project needs no SFX assets to sound complete.
    // A recorded clip at Resources/Sfx/<Kind> (e.g. Resources/Sfx/Shutter.wav) replaces the synthesised one.
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
            foreach (var kind in kinds)
            {
                var recorded = Resources.Load<AudioClip>("Sfx/" + kind);
                clips[(int)kind] = recorded != null ? recorded : Build(kind);
            }
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
                // Placeholders below: the plan asks for recorded shutter and heartbeat sounds.
                case SfxKind.Shutter:
                    return Make("Shutter", 0.24f, t =>
                        Noise() * Mathf.Exp(-t * 110) * 0.45f
                        + (t > 0.075f ? Noise() * Mathf.Exp(-(t - 0.075f) * 150) * 0.35f : 0)
                        + Tone(190, t, 0, 45) * 0.25f);
                case SfxKind.Heartbeat:
                    // Monitor beep, not a pulse: a short sine that cuts off.
                    return Make("Heartbeat", 0.32f, t => Mathf.Sin(Tau * 980 * t) * (t < 0.11f ? 1 : Mathf.Exp(-(t - 0.11f) * 60)) * 0.22f);
                case SfxKind.Glass:
                    return Make("Glass", 2.2f, t => (Tone(1318, t, 0, 1.6f) + 0.7f * Tone(1322, t, 0, 1.7f) + 0.4f * Tone(1975, t, 0, 2.4f) + 0.2f * Tone(2637, t, 0, 3.5f)) * 0.16f);
                case SfxKind.Tap:
                    return Make("Tap", 0.09f, t => Tone(2300, t, 0, 70) * 0.3f + Noise() * Mathf.Exp(-t * 320) * 0.18f);
                case SfxKind.Slide:
                    float rub = 0;
                    return Make("Slide", 0.7f, t => { rub += (Noise() - rub) * 0.08f; return rub * Mathf.Sin(Mathf.PI * t / 0.7f) * 0.9f; });
                case SfxKind.Mechanism:
                    float rumble = 0;
                    return Make("Mechanism", 1f, t =>
                    {
                        rumble += (Noise() - rumble) * 0.03f;
                        return Noise() * Mathf.Pow(Mathf.Max(0, Mathf.Sin(Tau * 9 * t)), 24) * 0.45f + rumble * Mathf.Exp(-t * 2.5f) * 1.2f;
                    });
                case SfxKind.Ripple:
                    return Make("Ripple", 1.3f, t => Mathf.Sin(Tau * (900 - 260 * t) * t + Mathf.Sin(Tau * 6 * t) * 3) * Mathf.Exp(-t * 3) * 0.16f);
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }
}
