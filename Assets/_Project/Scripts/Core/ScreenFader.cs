using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MysticCircus
{
    // Scene changes go through here so fading and input blocking stay in step.
    public sealed class ScreenFader : MonoBehaviour
    {
        static ScreenFader instance;
        float alpha = 1;
        Coroutine opening;
        public static bool Busy { get; private set; }

        void Awake() => instance = this;

        void Start()
        {
            if (!Busy) opening = StartCoroutine(FadeIn());
        }

        public static void Load(string scene)
        {
            if (instance == null) { SceneManager.LoadScene(scene); return; }
            Cover(() => LoadRoutine(scene));
        }

        // Fades to black, runs the work unseen, then fades back. False if a transition is already running.
        public static bool Cover(Func<IEnumerator> whileBlack)
        {
            if (instance == null || Busy) return false;
            instance.StartCoroutine(instance.Transition(whileBlack));
            return true;
        }

        public static IEnumerator LoadRoutine(string scene)
        {
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync(scene);
        }

        IEnumerator Transition(Func<IEnumerator> whileBlack)
        {
            Busy = true;
            if (opening != null) StopCoroutine(opening);
            // Continues from the current darkness, so a transition during the opening fade does not flash.
            for (float t = alpha * 0.6f; t < 0.6f; t += Time.unscaledDeltaTime) { alpha = t / 0.6f; yield return null; }
            alpha = 1;
            yield return whileBlack();
            yield return FadeIn();
            Busy = false;
        }

        IEnumerator FadeIn()
        {
            for (float t = 0; t < 0.7f; t += Time.unscaledDeltaTime) { alpha = 1 - t / 0.7f; yield return null; }
            alpha = 0;
        }

        void OnGUI()
        {
            if (alpha <= 0) return;
            GUI.depth = -100;
            var previous = GUI.color;
            GUI.color = new Color(0, 0, 0, alpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
