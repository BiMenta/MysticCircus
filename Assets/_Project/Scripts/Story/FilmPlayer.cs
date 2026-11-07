using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

namespace MysticCircus
{
    // Plays one film node: video, subtitles, overlays, the black hold after it, skipping, and the choice at the end.
    // Also plays short clips inside a level (PlayClip), such as a pair's reality film, without leaving the level.
    // IMGUI gets no input events in a player while only the Input System is active, so choices are hit-tested by hand.
    public sealed class FilmPlayer : MonoBehaviour
    {
        const float PrepareTimeout = 15;
        static FilmPlayer instance;

        VideoPlayer video;
        Camera backdrop;
        AudioListener listener;
        StoryNode node;
        bool ended;
        bool skipRequested;
        bool choosing;
        int chosen = -1;
        float deadline;
        // Unscaled time the video stopped (or, with no video, the moment the film began); -1 while it plays.
        float endedAt;
        float videoLength;
        GUIStyle subtitleStyle, promptStyle, buttonStyle, overlayStyle;

        VideoPlayer clipVideo;
        Camera clipCamera;
        bool clipEnded;

        public static bool Active => instance != null && instance.node != null;
        public static bool Choosing => Active && instance.choosing;
        public static bool VideoPlaying => Active && instance.video.isPlaying;
        public static bool ClipPlaying { get; private set; }
        // Seconds since the film began, continuing through the black hold after the video.
        public static float Clock => Active ? instance.FilmClock : 0;

        float FilmClock => endedAt < 0 ? (float)video.time : videoLength + (Time.unscaledTime - endedAt);

        bool CanSkip => !node.unskippable || (node.skippableOnceSeen && MetaProgress.HasSeen(node.id));

        void Awake()
        {
            instance = this;
            var stage = new GameObject("FilmStage");
            stage.transform.SetParent(transform, false);
            backdrop = stage.AddComponent<Camera>();
            backdrop.clearFlags = CameraClearFlags.SolidColor;
            backdrop.backgroundColor = Color.black;
            backdrop.enabled = false;
            listener = stage.AddComponent<AudioListener>();
            listener.enabled = false;
            video = CreatePlayer(stage, backdrop);
            video.loopPointReached += _ => ended = true;
            video.errorReceived += (_, message) =>
            {
                // A broken file behaves like a missing one: the story carries on.
                Debug.LogWarning("Film could not play: " + message);
                ended = true;
            };
        }

        static VideoPlayer CreatePlayer(GameObject host, Camera target)
        {
            var player = host.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.isLooping = false;
            player.skipOnDrop = true;
            player.source = VideoSource.Url;
            // Drawn by the camera so the engine handles color space and letterboxing; IMGUI only overlays text.
            player.renderMode = VideoRenderMode.CameraNearPlane;
            player.targetCamera = target;
            player.aspectRatio = VideoAspectRatio.FitInside;
            player.audioOutputMode = VideoAudioOutputMode.Direct;
            return player;
        }

        // Runs behind the fade so the first frame is ready when the screen clears.
        public static IEnumerator Enter(StoryNode film)
        {
            var self = instance;
            self.node = film;
            self.ended = false;
            self.skipRequested = false;
            self.choosing = false;
            self.chosen = -1;
            self.endedAt = -1;
            self.videoLength = 0;
            self.backdrop.enabled = true;
            self.listener.enabled = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = false;

            string path = StoryDirector.FilmPath(film.video);
            if (File.Exists(path))
            {
                self.video.url = path;
                self.video.Prepare();
                float limit = Time.unscaledTime + PrepareTimeout;
                while (!self.video.isPrepared && !self.ended && Time.unscaledTime < limit) yield return null;
            }
            if (self.video.isPrepared)
            {
                for (ushort track = 0; track < self.video.audioTrackCount; track++)
                    self.video.SetDirectAudioVolume(track, GameSettings.MusicVolume);
                self.videoLength = (float)self.video.length;
                self.video.Play();
            }
            else self.ended = true;
            self.StartCoroutine(self.Perform());
        }

        public static void Stop()
        {
            if (instance == null) return;
            instance.StopAllCoroutines();
            instance.video.Stop();
            instance.node = null;
            instance.choosing = false;
            instance.backdrop.enabled = false;
            instance.listener.enabled = false;
            instance.EndClip();
        }

        public static void Skip()
        {
            if (Active && instance.CanSkip) instance.skipRequested = true;
        }

        public static void Choose(int index)
        {
            if (Choosing && index >= 0 && index < instance.node.choices.Length) instance.chosen = index;
        }

        IEnumerator Perform()
        {
            while (ScreenFader.Busy) yield return null;
            while (!ended && !skipRequested) yield return null;
            if (endedAt < 0) endedAt = Time.unscaledTime;
            // The black hold that carries overlays; skipping skips it too.
            while (!skipRequested && Time.unscaledTime - endedAt < node.tailSeconds) yield return null;
            if (video.isPlaying) video.Pause();
            MetaProgress.MarkSeen(node.id);

            if (!node.HasChoices)
            {
                StoryDirector.FilmFinished(StoryDirector.NextOf(node));
                yield break;
            }

            choosing = true;
            Cursor.visible = true;
            deadline = node.choiceSeconds > 0 ? Time.unscaledTime + node.choiceSeconds : float.PositiveInfinity;
            while (chosen < 0)
            {
                if (Time.unscaledTime >= deadline) chosen = node.defaultChoice;
                yield return null;
            }
            choosing = false;
            Cursor.visible = false;
            Sfx.Play(SfxKind.Click);
            var choice = node.choices[chosen];
            if (!string.IsNullOrEmpty(choice.setFlag)) GameSession.Instance.SetFlag(choice.setFlag);
            StoryDirector.FilmFinished(choice.next);
        }

        // ---------- Clips inside a level ----------

        // Plays a file from StreamingAssets/Films full screen over the level, then returns. Input is held meanwhile.
        // A missing file is skipped with a warning, so content can be wired before the film exists.
        public static IEnumerator PlayClip(string video, bool skippable = true)
        {
            if (instance == null || ClipPlaying || string.IsNullOrEmpty(video)) yield break;
            // Runs on this persistent object, so the clip still ends cleanly if the caller is destroyed.
            instance.StartCoroutine(instance.ClipRoutine(video, skippable));
            while (ClipPlaying) yield return null;
        }

        IEnumerator ClipRoutine(string file, bool skippable)
        {
            string path = Path.IsPathRooted(file) ? file : StoryDirector.FilmPath(file);
            if (!File.Exists(path)) { Debug.LogWarning("Clip has no video file: " + path); yield break; }
            ClipPlaying = true;
            InputGate.Hold();
            if (clipVideo == null)
            {
                var stage = new GameObject("ClipStage");
                stage.transform.SetParent(transform, false);
                clipCamera = stage.AddComponent<Camera>();
                clipCamera.clearFlags = CameraClearFlags.SolidColor;
                clipCamera.backgroundColor = Color.black;
                clipCamera.cullingMask = 0;
                // Above every level camera.
                clipCamera.depth = 100;
                clipCamera.enabled = false;
                clipVideo = CreatePlayer(stage, clipCamera);
                clipVideo.loopPointReached += _ => clipEnded = true;
                clipVideo.errorReceived += (_, message) => { Debug.LogWarning("Clip could not play: " + message); clipEnded = true; };
            }
            clipEnded = false;
            clipVideo.url = path;
            clipVideo.Prepare();
            float limit = Time.unscaledTime + PrepareTimeout;
            while (!clipVideo.isPrepared && !clipEnded && Time.unscaledTime < limit) yield return null;
            if (clipVideo.isPrepared)
            {
                for (ushort track = 0; track < clipVideo.audioTrackCount; track++)
                    clipVideo.SetDirectAudioVolume(track, GameSettings.MusicVolume);
                clipCamera.enabled = true;
                clipVideo.Play();
                while (!clipEnded)
                {
                    var keyboard = Keyboard.current;
                    if (skippable && keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)) break;
                    yield return null;
                }
            }
            EndClip();
        }

        void EndClip()
        {
            if (clipVideo != null) clipVideo.Stop();
            if (clipCamera != null) clipCamera.enabled = false;
            if (!ClipPlaying) return;
            ClipPlaying = false;
            InputGate.Release();
        }

        // ---------- Input and drawing ----------

        void Update()
        {
            if (!Active || ScreenFader.Busy) return;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (choosing)
            {
                if (keyboard != null)
                {
                    if (keyboard.digit1Key.wasPressedThisFrame) Choose(0);
                    if (keyboard.digit2Key.wasPressedThisFrame) Choose(1);
                    if (keyboard.digit3Key.wasPressedThisFrame) Choose(2);
                    if (keyboard.digit4Key.wasPressedThisFrame) Choose(3);
                }
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    int hit = ChoiceAtMouse();
                    if (hit >= 0) Choose(hit);
                }
            }
            else if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)) Skip();
        }

        static float Scale => Mathf.Min(Screen.width / 1280f, Screen.height / 720f);

        Rect ChoiceRect(int index)
        {
            float width = 640 * Scale, height = 52 * Scale, gap = 12 * Scale;
            float top = Screen.height - (node.choices.Length - index) * (height + gap) - 40 * Scale;
            return new Rect((Screen.width - width) / 2, top, width, height);
        }

        int ChoiceAtMouse()
        {
            if (Mouse.current == null) return -1;
            Vector2 position = Mouse.current.position.ReadValue();
            position.y = Screen.height - position.y;
            for (int i = 0; i < node.choices.Length; i++)
                if (ChoiceRect(i).Contains(position)) return i;
            return -1;
        }

        void OnGUI()
        {
            if (!Active) return;
            GUI.depth = -50;
            if (subtitleStyle == null)
            {
                subtitleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerCenter, wordWrap = true };
                promptStyle = new GUIStyle(subtitleStyle) { alignment = TextAnchor.MiddleCenter };
                buttonStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter };
                overlayStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
            }
            int subtitleSize = Mathf.RoundToInt(26 * Scale);
            subtitleStyle.fontSize = subtitleSize;
            promptStyle.fontSize = subtitleSize;
            buttonStyle.fontSize = Mathf.RoundToInt(22 * Scale);

            DrawOverlays();
            if (choosing) DrawChoices();
            else DrawSubtitle();
        }

        void DrawSubtitle()
        {
            if (node.subtitles == null) return;
            foreach (var line in node.subtitles)
            {
                if (video.time < line.from || video.time >= line.to) continue;
                var area = new Rect(Screen.width * 0.1f, Screen.height * 0.72f, Screen.width * 0.8f, Screen.height * 0.22f);
                Outlined(area, L.T(line.zh, line.en), subtitleStyle);
                return;
            }
        }

        void DrawOverlays()
        {
            if (node.overlays == null) return;
            float clock = FilmClock;
            foreach (var overlay in node.overlays)
            {
                float alpha = FilmOverlayMath.Alpha(overlay, clock);
                if (alpha <= 0) continue;
                string text = L.T(overlay.zh, overlay.en) ?? "";
                float y = Screen.height * (overlay.y > 0 ? overlay.y : FilmOverlayMath.DefaultY(overlay.kind));
                var area = new Rect(0, y - 60 * Scale, Screen.width, 120 * Scale);
                var cream = new Color(0.96f, 0.93f, 0.88f, alpha);
                switch (overlay.kind)
                {
                    case StoryOverlay.Typewriter:
                        overlayStyle.fontSize = Mathf.RoundToInt(40 * Scale);
                        overlayStyle.fontStyle = FontStyle.Normal;
                        // Measured on the full line so the text does not slide while it types.
                        string shown = text.Substring(0, FilmOverlayMath.VisibleCharacters(overlay, text, clock));
                        float full = overlayStyle.CalcSize(new GUIContent(text)).x;
                        var left = new Rect((Screen.width - full) / 2, area.y, full + 40 * Scale, area.height);
                        overlayStyle.alignment = TextAnchor.MiddleLeft;
                        Tinted(left, shown, cream);
                        overlayStyle.alignment = TextAnchor.MiddleCenter;
                        break;
                    case StoryOverlay.Title:
                        overlayStyle.fontSize = Mathf.RoundToInt(64 * Scale);
                        overlayStyle.fontStyle = FontStyle.Bold;
                        Tinted(area, text, cream);
                        break;
                    case StoryOverlay.Doubled:
                        // Two lines in the same place, each 60% opaque, drifting a few pixels apart.
                        overlayStyle.fontSize = Mathf.RoundToInt(30 * Scale);
                        overlayStyle.fontStyle = FontStyle.Normal;
                        string[] lines = text.Split('|');
                        float drift = (3 + Mathf.Sin(clock * 0.7f)) * Scale;
                        var faint = new Color(cream.r, cream.g, cream.b, alpha * 0.6f);
                        Tinted(new Rect(area.x - drift, area.y + Mathf.Sin(clock * 0.45f) * Scale, area.width, area.height), lines[0], faint);
                        if (lines.Length > 1) Tinted(new Rect(area.x + drift, area.y - Mathf.Sin(clock * 0.45f) * Scale, area.width, area.height), lines[1], faint);
                        break;
                    default:
                        overlayStyle.fontSize = Mathf.RoundToInt(30 * Scale);
                        overlayStyle.fontStyle = FontStyle.Normal;
                        Tinted(area, text, cream);
                        break;
                }
            }
        }

        void Tinted(Rect area, string text, Color color)
        {
            overlayStyle.normal.textColor = color;
            GUI.Label(area, text, overlayStyle);
        }

        void DrawChoices()
        {
            bool plain = node.hideChrome;
            if (!plain)
            {
                GUI.color = new Color(0, 0, 0, 0.55f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            string prompt = L.T(node.promptZh, node.promptEn);
            float top = ChoiceRect(0).y;
            if (!string.IsNullOrEmpty(prompt))
                Outlined(new Rect(Screen.width * 0.1f, top - 90 * Scale, Screen.width * 0.8f, 70 * Scale), prompt, promptStyle);

            int hover = ChoiceAtMouse();
            for (int i = 0; i < node.choices.Length; i++)
            {
                var choice = node.choices[i];
                string label = L.T(choice.zh, choice.en);
                var highlight = new Color(0.91f, 0.77f, 0.5f);
                if (plain)
                {
                    var tint = i == hover ? highlight : new Color(0.96f, 0.93f, 0.88f, 0.85f);
                    var color = promptStyle.normal.textColor;
                    promptStyle.normal.textColor = tint;
                    GUI.Label(ChoiceRect(i), label, promptStyle);
                    promptStyle.normal.textColor = color;
                    continue;
                }
                GUI.color = i == hover ? highlight : Color.white;
                GUI.Box(ChoiceRect(i), (i + 1) + ".  " + label, buttonStyle);
            }
            GUI.color = Color.white;

            if (!plain && !float.IsInfinity(deadline))
            {
                float left = Mathf.Clamp01((deadline - Time.unscaledTime) / node.choiceSeconds);
                GUI.color = new Color(0.91f, 0.77f, 0.5f);
                GUI.DrawTexture(new Rect((Screen.width - 640 * Scale) / 2, top - 14 * Scale, 640 * Scale * left, 4 * Scale), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }

        static void Outlined(Rect area, string text, GUIStyle style)
        {
            var color = style.normal.textColor;
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(area.x + 2, area.y + 2, area.width, area.height), text, style);
            style.normal.textColor = Color.white;
            GUI.Label(area, text, style);
            style.normal.textColor = color;
        }
    }
}
