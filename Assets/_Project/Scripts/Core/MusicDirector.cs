using UnityEngine;

namespace MysticCircus
{
    // Two looping sources that crossfade, surviving scene loads so rooms blend into each other.
    public sealed class MusicDirector : MonoBehaviour
    {
        const float FadeSeconds = 1.8f;
        static MusicDirector instance;
        AudioSource current, previous;
        float blend = 1;
        float trackVolume = 1;
        float previousVolume;
        // Drops the music while a clip plays inside a level, so the clip's own sound is heard.
        float duck = 1;

        void Awake()
        {
            instance = this;
            current = CreateSource();
            previous = CreateSource();
        }

        AudioSource CreateSource()
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0;
            return source;
        }

        public static void Play(AudioClip clip, float volume = 1)
        {
            if (instance == null || clip == null) return;
            instance.Switch(clip, volume);
        }

        // Fades out whatever is playing, for example so a film's own soundtrack plays alone.
        public static void Stop()
        {
            if (instance == null || instance.current.clip == null) return;
            instance.Switch(null, 0);
        }

        void Switch(AudioClip clip, float volume)
        {
            if (current.clip == clip) { trackVolume = volume; return; }
            (current, previous) = (previous, current);
            previousVolume = trackVolume;
            trackVolume = volume;
            current.clip = clip;
            if (clip != null) current.Play();
            blend = 0;
        }

        void Update()
        {
            blend = Mathf.Min(1, blend + Time.unscaledDeltaTime / FadeSeconds);
            duck = Mathf.MoveTowards(duck, FilmPlayer.ClipPlaying ? 0.1f : 1, Time.unscaledDeltaTime * 3);
            current.volume = blend * trackVolume * GameSettings.MusicVolume * duck;
            previous.volume = (1 - blend) * previousVolume * GameSettings.MusicVolume * duck;
            if (blend >= 1 && previous.isPlaying) previous.Stop();
        }
    }
}
