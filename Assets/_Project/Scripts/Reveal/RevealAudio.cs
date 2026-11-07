using System;
using UnityEngine;

namespace MysticCircus
{
    // Treats one looping music source per layer: the accordion "from the next room" in Dream, an old tape in Warm,
    // a fainter, noisier tape in Cold. Filters cross-fade, so a single source serves all layers.
    // While this plays, the shared MusicDirector track is faded out.
    [RequireComponent(typeof(AudioSource))]
    public sealed class RevealAudio : MonoBehaviour
    {
        [Serializable]
        public struct Treatment
        {
            [Range(0, 1)] public float Volume;
            public float LowPass;
            public float HighPass;
            [Range(0, 1)] public float Distortion;
            [Range(0, 1)] public float Reverb;
            [Range(0, 1)] public float Hiss;
            // Pitch wander, as on a worn tape.
            public float Wobble;

            public static Treatment Lerp(Treatment a, Treatment b, float t) => new Treatment
            {
                Volume = Mathf.Lerp(a.Volume, b.Volume, t),
                LowPass = Mathf.Lerp(a.LowPass, b.LowPass, t),
                HighPass = Mathf.Lerp(a.HighPass, b.HighPass, t),
                Distortion = Mathf.Lerp(a.Distortion, b.Distortion, t),
                Reverb = Mathf.Lerp(a.Reverb, b.Reverb, t),
                Hiss = Mathf.Lerp(a.Hiss, b.Hiss, t),
                Wobble = Mathf.Lerp(a.Wobble, b.Wobble, t),
            };
        }

        public Treatment Dream = new Treatment { Volume = 0.6f, LowPass = 1500, HighPass = 10, Distortion = 0, Reverb = 1, Hiss = 0, Wobble = 0 };
        public Treatment Warm = new Treatment { Volume = 0.6f, LowPass = 3800, HighPass = 350, Distortion = 0.25f, Reverb = 0.3f, Hiss = 0.1f, Wobble = 0.012f };
        public Treatment Cold = new Treatment { Volume = 0.35f, LowPass = 2200, HighPass = 450, Distortion = 0.35f, Reverb = 0.5f, Hiss = 0.25f, Wobble = 0.02f };
        // Nearly untreated, for moments like "the accordion picks the beat up again".
        public Treatment Clear = new Treatment { Volume = 0.75f, LowPass = 12000, HighPass = 10, Distortion = 0, Reverb = 0.2f, Hiss = 0, Wobble = 0 };
        public float BlendSeconds = 0.3f;
        public bool StopSharedMusic = true;

        AudioSource source, hiss;
        AudioLowPassFilter lowPass;
        AudioHighPassFilter highPass;
        AudioDistortionFilter distortion;
        AudioReverbFilter reverb;
        Treatment now, from, to;
        float progress = 1;
        float duck = 1;
        bool cleared;

        public static Treatment For(RevealAudio audio, RevealLayer layer) =>
            layer == RevealLayer.Warm ? audio.Warm : layer == RevealLayer.Cold ? audio.Cold : audio.Dream;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = true;
            source.spatialBlend = 0;
            lowPass = Need<AudioLowPassFilter>();
            highPass = Need<AudioHighPassFilter>();
            distortion = Need<AudioDistortionFilter>();
            reverb = Need<AudioReverbFilter>();
            reverb.reverbPreset = AudioReverbPreset.User;
            reverb.decayTime = 2.2f;
            hiss = new GameObject("TapeHiss").AddComponent<AudioSource>();
            hiss.transform.SetParent(transform, false);
            hiss.clip = HissClip();
            hiss.loop = true;
            hiss.spatialBlend = 0;
            hiss.volume = 0;
            now = to = For(this, RevealSystem.Current);
            RevealSystem.Changed += OnLayer;
        }

        // Not "??": a missing component is a fake null in the editor.
        T Need<T>() where T : Component
        {
            var existing = GetComponent<T>();
            return existing != null ? existing : gameObject.AddComponent<T>();
        }

        void Start()
        {
            if (StopSharedMusic) MusicDirector.Stop();
            if (source.clip != null && !source.isPlaying) source.Play();
            hiss.Play();
            Apply();
        }

        void OnDestroy() => RevealSystem.Changed -= OnLayer;

        void OnLayer(RevealLayer layer)
        {
            if (!cleared) BlendTo(For(this, layer));
        }

        void BlendTo(Treatment target)
        {
            from = now;
            to = target;
            progress = 0;
        }

        // Restarts the track from the top with the dry treatment; Resume hands it back to the layer.
        public void PickUpTheBeat()
        {
            cleared = true;
            source.time = 0;
            if (!source.isPlaying) source.Play();
            BlendTo(Clear);
        }

        public void Resume()
        {
            cleared = false;
            BlendTo(For(this, RevealSystem.Current));
        }

        void Update()
        {
            if (progress < 1)
            {
                progress = BlendSeconds <= 0 ? 1 : Mathf.Min(1, progress + Time.unscaledDeltaTime / BlendSeconds);
                now = Treatment.Lerp(from, to, progress);
            }
            duck = Mathf.MoveTowards(duck, FilmPlayer.ClipPlaying ? 0 : 1, Time.unscaledDeltaTime * 3);
            Apply();
        }

        void Apply()
        {
            float level = GameSettings.MusicVolume * duck;
            source.volume = now.Volume * level;
            source.pitch = 1 + Mathf.Sin(Time.time * 0.73f) * now.Wobble + Mathf.Sin(Time.time * 2.9f) * now.Wobble * 0.3f;
            lowPass.cutoffFrequency = now.LowPass;
            highPass.cutoffFrequency = now.HighPass;
            distortion.distortionLevel = now.Distortion * 0.5f;
            reverb.dryLevel = 0;
            reverb.room = Mathf.Lerp(-10000, -600, now.Reverb);
            reverb.reverbLevel = Mathf.Lerp(-10000, 0, now.Reverb);
            hiss.volume = now.Hiss * level;
        }

        // Soft tape noise with slow flutter, made once so the project needs no noise asset.
        static AudioClip HissClip()
        {
            const int rate = 22050;
            var samples = new float[rate * 2];
            var random = new System.Random(47);
            float low = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                low += ((float)random.NextDouble() * 2 - 1 - low) * 0.35f;
                samples[i] = low * 0.4f * (0.85f + 0.15f * Mathf.Sin(i * 0.0021f));
            }
            var clip = AudioClip.Create("TapeHiss", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
