using System;
using UnityEngine;

namespace MysticCircus
{
    // Player preferences. Independent from the save file so starting a new game keeps them.
    public static class GameSettings
    {
        public static bool Chinese = true;
        public static float MusicVolume = 0.7f;
        public static float SfxVolume = 0.9f;
        public static float Sensitivity = 2f;
        public static event Action Changed;

        public static void Load()
        {
            Chinese = PlayerPrefs.GetInt("lang_zh", 1) == 1;
            MusicVolume = PlayerPrefs.GetFloat("music", 0.7f);
            SfxVolume = PlayerPrefs.GetFloat("sfx", 0.9f);
            Sensitivity = PlayerPrefs.GetFloat("sensitivity", 2f);
        }

        public static void Save()
        {
            PlayerPrefs.SetInt("lang_zh", Chinese ? 1 : 0);
            PlayerPrefs.SetFloat("music", MusicVolume);
            PlayerPrefs.SetFloat("sfx", SfxVolume);
            PlayerPrefs.SetFloat("sensitivity", Sensitivity);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }

    // Inline two-language text: the sentence stays beside the code that uses it.
    public static class L
    {
        public static string T(string zh, string en) => GameSettings.Chinese ? zh : en;
    }

    public static class CircusLayers
    {
        // Rendered by mirror cameras only; the player's camera excludes it.
        public const int MirrorOnly = 8;
    }

    // Menus and cutscenes hold this to stop player control. Counted so overlapping holders are safe.
    public static class InputGate
    {
        static int holds;
        public static bool Blocked => holds > 0;
        public static void Hold() => holds++;
        public static void Release() => holds = Mathf.Max(0, holds - 1);
        public static void Clear() => holds = 0;
    }
}
