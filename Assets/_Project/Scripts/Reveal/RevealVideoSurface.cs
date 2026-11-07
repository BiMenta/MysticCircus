using System.IO;
using UnityEngine;
using UnityEngine.Video;

namespace MysticCircus
{
    // Replaces a surface with a looping video in chosen layers, e.g. the central mirror becoming a memory.
    // The frame around it stays, so the shot keeps the same composition in every layer.
    // A layer with no file keeps the surface's normal material; a missing file shows the fallback colour.
    [RequireComponent(typeof(Renderer))]
    public sealed class RevealVideoSurface : MonoBehaviour
    {
        [Tooltip("Paths inside StreamingAssets/Films. Empty keeps the normal surface in that layer.")]
        public string DreamVideo, WarmVideo, ColdVideo;
        public Color WarmFallback = new Color(1f, 0.82f, 0.76f);
        public Color ColdFallback = new Color(0.58f, 0.64f, 0.7f);
        public Color DreamFallback = Color.white;
        [Tooltip("Switched off while a video shows, such as the PlanarMirror that would keep rendering reflections.")]
        public Behaviour[] PauseWhilePlaying = new Behaviour[0];
        public bool PlaySound;
        public int TextureWidth = 1280, TextureHeight = 720;

        Renderer surface;
        Material normal, screen;
        VideoPlayer player;
        RenderTexture frames;

        public bool ShowingVideo { get; private set; }

        void Start()
        {
            // Start, not Awake: a PlanarMirror makes its own material instance in Awake, and that is the one to restore.
            surface = GetComponent<Renderer>();
            normal = surface.sharedMaterial;
            RevealSystem.Changed += Apply;
            Apply(RevealSystem.Current);
        }

        void OnDestroy()
        {
            RevealSystem.Changed -= Apply;
            if (frames != null) { frames.Release(); Destroy(frames); }
            if (screen != null) Destroy(screen);
        }

        string FileFor(RevealLayer layer) => layer == RevealLayer.Warm ? WarmVideo : layer == RevealLayer.Cold ? ColdVideo : DreamVideo;
        Color FallbackFor(RevealLayer layer) => layer == RevealLayer.Warm ? WarmFallback : layer == RevealLayer.Cold ? ColdFallback : DreamFallback;

        void Apply(RevealLayer layer)
        {
            if (this == null || surface == null) return;
            string file = FileFor(layer);
            if (string.IsNullOrEmpty(file))
            {
                if (player != null) player.Stop();
                surface.sharedMaterial = normal;
                foreach (var behaviour in PauseWhilePlaying) if (behaviour != null) behaviour.enabled = true;
                ShowingVideo = false;
                return;
            }

            Prepare();
            foreach (var behaviour in PauseWhilePlaying) if (behaviour != null) behaviour.enabled = false;
            surface.sharedMaterial = screen;
            ShowingVideo = true;
            // The fallback shows until the first frame arrives, and stays if the file is missing.
            screen.mainTexture = Texture2D.whiteTexture;
            screen.color = FallbackFor(layer);
            string path = StoryDirector.FilmPath(file);
            player.Stop();
            if (!File.Exists(path)) { Debug.LogWarning("Reveal video missing, showing a flat colour: " + path); return; }
            player.url = path;
            player.Play();
        }

        void Prepare()
        {
            if (player != null) return;
            var shader = Resources.Load<Shader>("RevealVideo");
            screen = new Material(shader != null ? shader : Shader.Find("Unlit/Texture")) { hideFlags = HideFlags.DontSave };
            frames = new RenderTexture(TextureWidth, TextureHeight, 0) { name = "RevealVideo", hideFlags = HideFlags.DontSave };
            player = gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.isLooping = true;
            player.source = VideoSource.Url;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = frames;
            player.audioOutputMode = PlaySound ? VideoAudioOutputMode.Direct : VideoAudioOutputMode.None;
            player.prepareCompleted += _ =>
            {
                if (!ShowingVideo) return;
                screen.mainTexture = frames;
                screen.color = Color.white;
            };
            player.errorReceived += (_, message) => Debug.LogWarning("Reveal video could not play: " + message);
        }
    }
}
