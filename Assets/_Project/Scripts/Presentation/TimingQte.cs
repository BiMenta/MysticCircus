using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MysticCircus
{
    // Wordless timing prompt: a ring closes onto a fixed ring; press Space, E or click as they meet.
    // Used for the dice throw, the photo-booth shutter and similar moments. Failure only reports false,
    // so the caller decides what to reset, in keeping with "no death, no 'you chose wrong'".
    // Persistent service, created with GameSession.
    public sealed class TimingQte : MonoBehaviour
    {
        const float StartRadius = 150, TargetRadius = 42;
        static TimingQte instance;

        public static bool Running { get; private set; }

        float start, duration, window;
        Vector2? anchor;
        Action<bool> done;
        // After the press: a short burst (hit) or a sag (miss) before the prompt disappears.
        float resultAt = -1;
        bool hit;

        void Awake() => instance = this;

        // Seconds for the ring to reach the target; window is the half-width of the accepted moment, in seconds.
        // anchor is a canvas point; null centres it.
        public static bool Begin(float seconds, float window, Action<bool> result, Vector2? anchor = null)
        {
            if (instance == null || Running) return false;
            Running = true;
            InputGate.Hold();
            instance.start = Time.unscaledTime;
            instance.duration = Mathf.Max(0.2f, seconds);
            instance.window = Mathf.Max(0.02f, window);
            instance.anchor = anchor;
            instance.done = result;
            instance.resultAt = -1;
            return true;
        }

        // For coroutines: yield return TimingQte.Run(1.2f, 0.15f, ok => ...).
        public static IEnumerator Run(float seconds, float window, Action<bool> result, Vector2? anchor = null)
        {
            bool finished = false;
            if (!Begin(seconds, window, ok => { finished = true; result?.Invoke(ok); }, anchor)) yield break;
            while (!finished) yield return null;
        }

        // The ring's lateness at time t: negative before the target moment, positive after.
        public static float Offset(float elapsed, float seconds) => elapsed - seconds;
        public static bool IsHit(float elapsed, float seconds, float window) => Mathf.Abs(Offset(elapsed, seconds)) <= window;

        void Update()
        {
            if (!Running) return;
            float elapsed = Time.unscaledTime - start;
            if (resultAt > 0)
            {
                if (Time.unscaledTime - resultAt > 0.45f) Finish();
                return;
            }
            var keyboard = Keyboard.current;
            bool pressed = GuiKit.Clicked || (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame));
            if (pressed) Resolve(IsHit(elapsed, duration, window));
            else if (Offset(elapsed, duration) > window) Resolve(false);
        }

        void Resolve(bool success)
        {
            hit = success;
            resultAt = Time.unscaledTime;
            Sfx.Play(success ? SfxKind.Tap : SfxKind.Ripple, success ? 0.8f : 0.4f);
        }

        void Finish()
        {
            Running = false;
            InputGate.Release();
            var callback = done;
            done = null;
            callback?.Invoke(hit);
        }

        void OnGUI()
        {
            if (!Running) return;
            GUI.depth = -60;
            GuiKit.Begin();
            Vector2 centre = anchor ?? GuiKit.Canvas / 2;
            var cream = new Color(0.98f, 0.95f, 0.9f, 0.9f);
            if (resultAt > 0)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - resultAt) / 0.45f);
                if (hit) GuiKit.Circle(centre, TargetRadius * (1 + t * 0.8f), new Color(1, 1, 1, 0.9f * (1 - t)), true);
                else GuiKit.Circle(centre + new Vector2(0, t * 12), TargetRadius * (1 - t * 0.2f), new Color(0.5f, 0.5f, 0.55f, 0.8f * (1 - t)), true);
                return;
            }
            float k = Mathf.Clamp01((Time.unscaledTime - start) / duration);
            float radius = Mathf.Lerp(StartRadius, TargetRadius, k);
            GuiKit.Circle(centre, TargetRadius, cream, true);
            GuiKit.Circle(centre, 3, cream);
            GuiKit.Circle(centre, radius, new Color(1, 1, 1, 0.35f + 0.55f * k), true);
        }
    }
}
