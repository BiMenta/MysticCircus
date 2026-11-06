using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace MysticCircus
{
    public sealed class PrototypeHUD : MonoBehaviour
    {
        public RoomController Room;
        public FirstPersonPlayer Player;
        static string notice;
        static float noticeUntil;
        GUIStyle title, body, small;

        public static void Notice(string message)
        {
            notice = message;
            noticeUntil = Time.unscaledTime + 3;
        }

        void Update()
        {
            if (Room == null || Keyboard.current == null || !Keyboard.current.hKey.wasPressedThisFrame) return;
            string hint = Room.NextHint();
            if (hint != null) Notice(hint);
        }

        void OnGUI()
        {
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 27, fontStyle = FontStyle.Bold };
                title.normal.textColor = new Color(0.91f, 0.77f, 0.5f);
                body = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
                small = new GUIStyle(body) { fontSize = 13 };
            }
            var scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            float width = Screen.width / scale, height = Screen.height / scale;
            GUI.Box(new Rect(22, 20, 560, 160), "");
            GUI.Label(new Rect(40, 30, 520, 40), Room != null ? Room.Chapter : "MYSTIC CIRCUS", title);
            GUI.Label(new Rect(40, 77, 520, 75), Room != null ? Room.Objective : "Walk outside, then look back at the entrance.", body);
            GUI.Label(new Rect(40, 152, 520, 25), "GREYBOX PROTOTYPE / v0.1", small);
            GUI.Label(new Rect(width / 2 - 5, height / 2 - 12, 24, 24), "+", body);
            if (Player != null && Player.Focus != null)
            {
                GUI.Box(new Rect(width / 2 - 250, height - 135, 500, 40), "");
                GUI.Label(new Rect(width / 2 - 235, height - 130, 470, 35), "[E] " + Player.Focus.Prompt, body);
            }
            string feedback = Time.unscaledTime < noticeUntil ? notice : Room != null ? Room.Message : "";
            GUI.Label(new Rect(40, height - 92, width - 80, 40), feedback ?? "", body);
            GUI.Label(new Rect(40, height - 40, width - 80, 30), "WASD move  /  Mouse look  /  E interact  /  Esc release cursor  /  F1 room guide", small);
            if (Keyboard.current != null && Keyboard.current.f1Key.isPressed)
            {
                GUI.Box(new Rect(width - 240, 20, 220, 260), "ROOM GUIDE / DEBUG");
                for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    string path = SceneUtility.GetScenePathByBuildIndex(i);
                    string name = System.IO.Path.GetFileNameWithoutExtension(path);
                    if (GUI.Button(new Rect(width - 225, 55 + i * 33, 190, 28), name)) SceneManager.LoadScene(i);
                }
            }
        }
    }
}
