using System.Collections;
using UnityEngine;

namespace MysticCircus
{
    public sealed class EndingSequence : MonoBehaviour
    {
        public Camera View;
        public Transform Entrance;
        public Transform[] Performers;
        public FirstPersonPlayer Player;
        bool triggered;
        float fade;

        void Update()
        {
            if (triggered || View.transform.position.z > -7) return;
            Vector3 towardEntrance = (Entrance.position - View.transform.position).normalized;
            if (Vector3.Dot(View.transform.forward, towardEntrance) > 0.8f) StartCoroutine(Finale());
        }

        IEnumerator Finale()
        {
            triggered = true;
            PrototypeHUD.Notice("They have been waiting for your final bow.");
            yield return new WaitForSeconds(1.5f);
            Player.enabled = false;
            var hud = FindAnyObjectByType<PrototypeHUD>();
            if (hud != null) hud.enabled = false;
            for (float t = 0; t < 2; t += Time.deltaTime)
            {
                foreach (var actor in Performers) actor.localRotation = Quaternion.Euler(Mathf.Sin(t / 2 * Mathf.PI) * 38, 0, 0);
                yield return null;
            }
            for (float t = 0; t < 2; t += Time.deltaTime) { fade = Mathf.Clamp01(t / 2); yield return null; }
            fade = 1;
            // Lets the story continue after the title, for example into a closing film.
            yield return new WaitForSeconds(3);
            StoryDirector.Advance();
        }

        void OnGUI()
        {
            if (fade <= 0) return;
            var previous = GUI.color;
            GUI.color = new Color(0, 0, 0, fade);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            if (fade > 0.95f)
            {
                GUI.color = new Color(0.9f, 0.76f, 0.5f);
                var style = new GUIStyle(GUI.skin.label) { fontSize = 48, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(0, Screen.height / 2 - 60, Screen.width, 120), "MYSTIC CIRCUS", style);
            }
            GUI.color = previous;
        }
    }
}
