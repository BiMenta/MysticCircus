using System;
using System.Collections.Generic;
using UnityEngine;

namespace MysticCircus
{
    // The whole game's running order, read from StreamingAssets/Story/story.json.
    // Field names are the JSON keys, so they stay lowercase. Rules here do not touch scenes or video and are covered by EditMode tests.
    [Serializable]
    public sealed class StorySubtitle
    {
        public float from;
        public float to;
        public string zh;
        public string en;
    }

    [Serializable]
    public sealed class StoryChoice
    {
        public string zh;
        public string en;
        public string next;
        // Optional progress flag recorded when this choice is taken; later nodes can route on it.
        public string setFlag;
    }

    [Serializable]
    public sealed class StoryRoute
    {
        public string ifFlag;
        public string next;
    }

    [Serializable]
    public sealed class StoryNode
    {
        public const string Level = "level";
        public const string Film = "film";

        public string id;
        public string type;
        // Level nodes: the scene name.
        public string scene;
        // Film nodes: file name inside StreamingAssets/Films.
        public string video;
        public bool unskippable;
        public StorySubtitle[] subtitles = new StorySubtitle[0];
        public string promptZh;
        public string promptEn;
        public StoryChoice[] choices = new StoryChoice[0];
        // Seconds to decide before defaultChoice is taken. Zero waits forever.
        public float choiceSeconds;
        public int defaultChoice;
        // First route whose flag is set wins; otherwise next. Empty next ends the story.
        public StoryRoute[] routes = new StoryRoute[0];
        public string next;

        public bool IsFilm => type == Film;
        public bool IsLevel => type == Level;
        public bool HasChoices => choices != null && choices.Length > 0;
    }

    [Serializable]
    public sealed class StoryGraph
    {
        public string start;
        public StoryNode[] nodes = new StoryNode[0];

        public static StoryGraph FromJson(string json) => JsonUtility.FromJson<StoryGraph>(json);

        public StoryNode Find(string id)
        {
            foreach (var node in nodes) if (node.id == id) return node;
            return null;
        }

        // The node that plays a scene, so opening a level directly still knows where it sits in the story.
        public StoryNode FindLevel(string scene)
        {
            foreach (var node in nodes) if (node.IsLevel && node.scene == scene) return node;
            return null;
        }

        public static string ResolveNext(StoryNode node, Func<string, bool> hasFlag)
        {
            if (node.routes != null)
                foreach (var route in node.routes)
                    if (hasFlag(route.ifFlag)) return route.next;
            return node.next;
        }

        // Every structural mistake at once, so a content edit can be fixed in one pass.
        public List<string> Validate()
        {
            var problems = new List<string>();
            var ids = new HashSet<string>();
            foreach (var node in nodes)
                if (string.IsNullOrEmpty(node.id)) problems.Add("A node has no id.");
                else if (!ids.Add(node.id)) problems.Add("Duplicate node id: " + node.id);

            if (string.IsNullOrEmpty(start) || !ids.Contains(start)) problems.Add("start points to an unknown node: " + start);

            void CheckTarget(string owner, string target, bool required)
            {
                if (string.IsNullOrEmpty(target)) { if (required) problems.Add(owner + " has no next node."); }
                else if (!ids.Contains(target)) problems.Add(owner + " points to an unknown node: " + target);
            }

            foreach (var node in nodes)
            {
                if (!node.IsLevel && !node.IsFilm) problems.Add(node.id + ": type must be level or film.");
                if (node.IsLevel && string.IsNullOrEmpty(node.scene)) problems.Add(node.id + ": level needs a scene.");
                if (node.IsFilm && string.IsNullOrEmpty(node.video)) problems.Add(node.id + ": film needs a video.");
                if (node.IsLevel && node.HasChoices) problems.Add(node.id + ": choices only work on films.");
                CheckTarget(node.id, node.next, false);
                foreach (var route in node.routes)
                {
                    if (string.IsNullOrEmpty(route.ifFlag)) problems.Add(node.id + ": a route has no ifFlag.");
                    CheckTarget(node.id + " route", route.next, true);
                }
                foreach (var choice in node.choices) CheckTarget(node.id + " choice", choice.next, true);
                if (node.HasChoices && (node.defaultChoice < 0 || node.defaultChoice >= node.choices.Length))
                    problems.Add(node.id + ": defaultChoice is out of range.");
            }
            return problems;
        }
    }
}
