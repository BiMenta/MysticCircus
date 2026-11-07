using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MysticCircus.Editor
{
    // Keeps Build Settings in step with the story file and reports content mistakes before play.
    public static class StoryTools
    {
        const string SceneRoot = "Assets/_Project/Scenes/";

        static string ScenePath(string scene) => SceneRoot + scene + ".unity";

        public static StoryGraph ReadStory() => StoryGraph.FromJson(File.ReadAllText(StoryDirector.StoryPath));

        [MenuItem("Mystic Circus/Sync Build Settings From Story")]
        public static void SyncBuildSettings()
        {
            var ordered = new List<string> { StoryDirector.BootScene };
            foreach (var node in ReadStory().nodes)
                if (node.IsLevel && !ordered.Contains(node.scene)) ordered.Add(node.scene);
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (string scene in ordered)
            {
                if (File.Exists(ScenePath(scene))) scenes.Add(new EditorBuildSettingsScene(ScenePath(scene), true));
                else Debug.LogError("Story uses a scene that does not exist: " + ScenePath(scene));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("Build Settings now lists: " + string.Join(", ", ordered));
        }

        // Everything wrong with the story and the files it names, empty when ready to play.
        public static List<string> Check()
        {
            var story = ReadStory();
            var problems = story.Validate();
            var built = new HashSet<string>();
            foreach (var scene in EditorBuildSettings.scenes) if (scene.enabled) built.Add(scene.path);
            foreach (var node in story.nodes)
            {
                if (node.IsLevel && !built.Contains(ScenePath(node.scene)))
                    problems.Add(node.id + ": scene is not in Build Settings: " + node.scene);
                if (node.IsFilm && !File.Exists(StoryDirector.FilmPath(node.video)))
                    problems.Add(node.id + ": video file is missing: " + node.video);
            }
            return problems;
        }

        [MenuItem("Mystic Circus/Validate Story")]
        public static void Validate()
        {
            var problems = Check();
            foreach (string problem in problems) Debug.LogError("Story: " + problem);
            if (problems.Count == 0) Debug.Log("MYSTIC_CIRCUS_STORY_OK");
        }
    }
}
