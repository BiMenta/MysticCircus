using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MysticCircus
{
    [Serializable]
    public sealed class SaveData
    {
        public string Scene;
        public int Costume;
        public int Hat;
        public int Mask;
        public int HintsUsed;
        public float Seconds;
        // Content-defined progress such as collected items or seen cutscenes, keyed by string id.
        public List<string> Flags = new List<string>();
    }

    // Persistent run state plus the services every scene relies on. Rooms keep their own puzzle state,
    // so a checkpoint is simply "the room you last entered".
    public sealed class GameSession : MonoBehaviour
    {
        public const string FirstScene = "00_Backstage";

        public static GameSession Instance { get; private set; }
        // Tests switch this off so a test run never touches the player's real save.
        public static bool PersistenceEnabled = true;
        // Scenes that never count as a checkpoint or as play time, such as a future title screen.
        public static readonly HashSet<string> NonGameplayScenes = new HashSet<string>();
        public SaveData Data = new SaveData();

        public int Costume { get => Data.Costume; set => Data.Costume = value; }
        public int Hat { get => Data.Hat; set => Data.Hat = value; }
        public int Mask { get => Data.Mask; set => Data.Mask = value; }

        static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");
        public static bool HasSave => PersistenceEnabled && File.Exists(SavePath);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            Application.targetFrameRate = 60;
            GameSettings.Load();
            InputGate.Clear();
            if (Instance != null) return;
            var root = new GameObject("GameSession");
            Instance = root.AddComponent<GameSession>();
            root.AddComponent<ScreenFader>();
            root.AddComponent<MusicDirector>();
            root.AddComponent<Sfx>();
            DontDestroyOnLoad(root);
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

        void Update()
        {
            if (Time.timeScale > 0 && !NonGameplayScenes.Contains(SceneManager.GetActiveScene().name))
                Data.Seconds += Time.unscaledDeltaTime;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // The test runner's bootstrap scene loads before tests can disable saving; it must not overwrite a real save.
            if (scene.name.StartsWith("InitTestScene") || NonGameplayScenes.Contains(scene.name)) return;
            Data.Scene = scene.name;
            Save();
        }

        public bool HasFlag(string id) => Data.Flags.Contains(id);

        public void SetFlag(string id)
        {
            if (HasFlag(id)) return;
            Data.Flags.Add(id);
            Save();
        }

        public void NewGame(string scene = FirstScene)
        {
            ClearSave();
            Data = new SaveData();
            ScreenFader.Load(scene);
        }

        public void Continue()
        {
            if (!HasSave) return;
            try { Data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath)) ?? new SaveData(); }
            catch (Exception error)
            {
                // A damaged save must not trap the player: fall back to a fresh run.
                Debug.LogWarning("Ignoring unreadable save: " + error.Message);
                Data = new SaveData();
            }
            ScreenFader.Load(Application.CanStreamedLevelBeLoaded(Data.Scene ?? "") ? Data.Scene : FirstScene);
        }

        public void Save()
        {
            if (!PersistenceEnabled) return;
            try { File.WriteAllText(SavePath, JsonUtility.ToJson(Data)); }
            catch (Exception error) { Debug.LogWarning("Could not write save: " + error.Message); }
        }

        public void ClearSave()
        {
            if (PersistenceEnabled && File.Exists(SavePath)) File.Delete(SavePath);
        }

        void OnApplicationQuit() => Save();
    }
}
