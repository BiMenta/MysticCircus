using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

namespace MysticCircus
{
    // Plays one film node: video, subtitles, skipping, and the choice at the end.
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
        GUIStyle subtitleStyle, promptStyle, buttonStyle;

        public static bool Active => instance != null && instance.node != null;
        public static bool Choosing => Active && instance.choosing;
        public static bool VideoPlaying => Active && instance.video.isPlaying;

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
            video = stage.AddComponent<VideoPlayer>();
            video.playOnAwake = false;
            video.isLooping = false;
            video.skipOnDrop = true;
            video.source = VideoSource.Url;
            // Drawn by the camera so the engine handles color space and letterboxing; IMGUI only overlays text.
            video.renderMode = VideoRenderMode.CameraNearPlane;
            video.targetCamera = backdrop;
            video.aspectRatio = VideoAspectRatio.FitInside;
            video.audioOutputMode = VideoAudioOutputMode.Direct;
            video.loopPointReached += _ => ended = true;
            video.errorReceived += (_, message) =>
            {
                // A broken file behaves like a missing one: the story carries on.
                Debug.LogWarning("Film could not play: " + message);
                ended = true;
            };
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
        }

        public static void Skip()
        {
            if (Active && !instance.node.unskippable) instance.skipRequested = true;
        }

        public static void Choose(int index)
        {
            if (Choosing && index >= 0 && index < instance.node.choices.Length) instance.chosen = index;
        }

        IEnumerator Perform()
        {
            while (ScreenFader.Busy) yield return null;
            while (!ended && !skipRequested) yield return null;

            if (!node.HasChoices)
            {
                StoryDirector.FilmFinished(StoryDirector.NextOf(node));
                yield break;
            }

            if (video.isPlaying) video.Pause();
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
            }
            int subtitleSize = Mathf.RoundToInt(26 * Scale);
            subtitleStyle.fontSize = subtitleSize;
            promptStyle.fontSize = subtitleSize;
            buttonStyle.fontSize = Mathf.RoundToInt(22 * Scale);

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

        void DrawChoices()
        {
            GUI.color = new Color(0, 0, 0, 0.55f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            string prompt = L.T(node.promptZh, node.promptEn);
            float top = ChoiceRect(0).y;
            if (!string.IsNullOrEmpty(prompt))
                Outlined(new Rect(Screen.width * 0.1f, top - 90 * Scale, Screen.width * 0.8f, 70 * Scale), prompt, promptStyle);

            int hover = ChoiceAtMouse();
            for (int i = 0; i < node.choices.Length; i++)
            {
                var choice = node.choices[i];
                GUI.color = i == hover ? new Color(0.91f, 0.77f, 0.5f) : Color.white;
                GUI.Box(ChoiceRect(i), (i + 1) + ".  " + L.T(choice.zh, choice.en), buttonStyle);
            }
            GUI.color = Color.white;

            if (!float.IsInfinity(deadline))
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
