using UnityEngine;

namespace MysticCircus
{
    // Timing rules for film overlays, free of drawing so they can be tested.
    public static class FilmOverlayMath
    {
        public const float DefaultCharSeconds = 0.5f;

        public static float FadeIn(StoryOverlay overlay)
        {
            if (overlay.fadeIn > 0) return overlay.fadeIn;
            switch (overlay.kind)
            {
                case StoryOverlay.Typewriter: return 0;
                case StoryOverlay.Title: return 3;
                case StoryOverlay.Doubled: return 2;
                default: return 1;
            }
        }

        public static float FadeOut(StoryOverlay overlay)
        {
            if (overlay.fadeOut > 0) return overlay.fadeOut;
            return overlay.kind == StoryOverlay.Typewriter ? 1 : 1.5f;
        }

        // 0 outside [at, until); ramps in and out inside it, never overlapping past the middle.
        public static float Alpha(StoryOverlay overlay, float time)
        {
            if (time < overlay.at || time >= overlay.until) return 0;
            float length = overlay.until - overlay.at;
            float fadeIn = Mathf.Min(FadeIn(overlay), length / 2);
            float fadeOut = Mathf.Min(FadeOut(overlay), length / 2);
            float rise = fadeIn > 0 ? (time - overlay.at) / fadeIn : 1;
            float fall = fadeOut > 0 ? (overlay.until - time) / fadeOut : 1;
            return Mathf.Clamp01(Mathf.Min(rise, fall));
        }

        // Characters shown by a typewriter overlay: the first appears at once, then one per charSeconds.
        public static int VisibleCharacters(StoryOverlay overlay, string text, float time)
        {
            if (string.IsNullOrEmpty(text) || time < overlay.at) return 0;
            float step = overlay.charSeconds > 0 ? overlay.charSeconds : DefaultCharSeconds;
            return Mathf.Clamp(Mathf.FloorToInt((time - overlay.at) / step) + 1, 0, text.Length);
        }

        public static float DefaultY(string kind)
        {
            switch (kind)
            {
                case StoryOverlay.Title: return 0.42f;
                case StoryOverlay.Doubled: return 0.56f;
                case StoryOverlay.Caption: return 0.62f;
                default: return 0.5f;
            }
        }
    }
}
