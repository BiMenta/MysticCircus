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
        public static bool Busy { get; private set; }

        void Awake() => instance = this;
        void Start() => StartCoroutine(FadeIn());

        public static void Load(string scene)
        {
            if (instance == null) { SceneManager.LoadScene(scene); return; }
            if (!Busy) instance.StartCoroutine(instance.Transition(scene));
        }

        IEnumerator Transition(string scene)
        {
            Busy = true;
            for (float t = 0; t < 0.6f; t += Time.unscaledDeltaTime) { alpha = t / 0.6f; yield return null; }
            alpha = 1;
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync(scene);
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
