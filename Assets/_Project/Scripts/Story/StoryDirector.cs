using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MysticCircus
{
    // Walks the story graph. Levels and films only say "I am finished"; this decides what comes next,
    // so changing the running order never means editing a scene.
    public sealed class StoryDirector : MonoBehaviour
    {
        public const string BootScene = "Boot";
        const string FilmStage = "FilmStage";

        public static StoryGraph Graph { get; private set; }
        public static StoryNode Current { get; private set; }

        public static string StoryPath => Path.Combine(Application.streamingAssetsPath, "Story", "story.json");
        public static string FilmPath(string video) => Path.Combine(Application.streamingAssetsPath, "Films", video);

        void Awake()
        {
            Current = null;
            Graph = LoadFromDisk();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

        static StoryGraph LoadFromDisk()
        {
            if (!File.Exists(StoryPath)) { Debug.LogWarning("No story file; door exits use their NextScene. " + StoryPath); return null; }
            try
            {
                var graph = StoryGraph.FromJson(File.ReadAllText(StoryPath));
                foreach (string problem in graph.Validate()) Debug.LogError("Story: " + problem);
                return graph;
            }
            catch (System.Exception error)
            {
                // A typo in the story file must not stop the game from launching.
                Debug.LogError("Could not read story file: " + error.Message);
                return null;
            }
        }

        // Tests supply their own graph; null falls back to each door's NextScene.
        public static void UseGraph(StoryGraph graph)
        {
            Graph = graph;
            Current = null;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == BootScene) { StartCoroutine(BeginWhenClear()); return; }
            if (Graph == null || (Current != null && Current.IsLevel && Current.scene == scene.name)) return;
            // Reached by the debug menu or opened directly in the editor: pick up the story from this level.
            FilmPlayer.Stop();
            var level = Graph.FindLevel(scene.name);
            if (level != null) Record(level);
            else Current = null;
        }

        // Boot may arrive through a transition that is still fading in, which would refuse the request.
        IEnumerator BeginWhenClear()
        {
            while (ScreenFader.Busy) yield return null;
            Begin();
        }

        public static void Begin()
        {
            if (Graph == null) ScreenFader.Load(GameSession.FirstScene);
            else Go(Graph.start);
        }

        public static bool CanResume(string id) => Graph != null && !string.IsNullOrEmpty(id) && Graph.Find(id) != null;

        public static bool Go(string id) => Go(id, 0);

        static bool Go(string id, int skipped)
        {
            if (Graph == null || ScreenFader.Busy) return false;
            var node = Graph.Find(id);
            if (node == null) { Debug.LogError("Story has no node: " + id); return false; }

            if (node.IsFilm && !node.HasChoices && !File.Exists(FilmPath(node.video)))
            {
                // An empty slot must never block the game while films are still being made.
                Debug.LogWarning("Skipping film with no video file: " + node.video);
                if (skipped > Graph.nodes.Length) { Debug.LogError("Story loops through films with no video."); return false; }
                string next = NextOf(node);
                if (string.IsNullOrEmpty(next)) { Record(node); Finish(); return true; }
                return Go(next, skipped + 1);
            }

            bool started = ScreenFader.Cover(() => node.IsFilm ? EnterFilm(node) : EnterLevel(node));
            if (started) Record(node);
            return started;
        }

        // Called when the current level is finished. False means the story has nothing to say here,
        // either because this scene is not part of it or because it is the last node.
        public static bool Advance()
        {
            if (Graph == null || Current == null || ScreenFader.Busy) return false;
            if (Current.IsLevel && Current.scene != SceneManager.GetActiveScene().name) return false;
            string next = NextOf(Current);
            return !string.IsNullOrEmpty(next) && Go(next);
        }

        // A film ended. Without a next node the story is over, so a new run starts.
        public static void FilmFinished(string next)
        {
            if (string.IsNullOrEmpty(next)) Finish();
            else Go(next);
        }

        public static string NextOf(StoryNode node) => StoryGraph.ResolveNext(node, GameSession.Instance.HasFlag);

        static void Finish() => GameSession.Instance.NewGame();

        static void Record(StoryNode node)
        {
            Current = node;
            GameSession.Instance.Data.Node = node.id;
            GameSession.Instance.Save();
        }

        static IEnumerator EnterLevel(StoryNode node)
        {
            FilmPlayer.Stop();
            yield return ScreenFader.LoadRoutine(node.scene);
        }

        static IEnumerator EnterFilm(StoryNode node)
        {
            FilmPlayer.Stop();
            // Replaces the level with an empty scene so it stops running and releases its memory during the film.
            var previous = SceneManager.GetActiveScene();
            if (previous.name != FilmStage)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene(FilmStage));
                yield return SceneManager.UnloadSceneAsync(previous);
            }
            MusicDirector.Stop();
            yield return FilmPlayer.Enter(node);
        }
    }
}
