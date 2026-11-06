using UnityEngine;

namespace MysticCircus
{
    // Declares which track a scene wants; the persistent MusicDirector does the rest.
    public sealed class SceneAmbience : MonoBehaviour
    {
        public AudioClip Music;
        [Range(0, 1)] public float Volume = 0.8f;
        void Start() => MusicDirector.Play(Music, Volume);
    }
}
