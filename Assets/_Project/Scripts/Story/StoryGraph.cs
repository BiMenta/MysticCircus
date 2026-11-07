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

    // Text drawn over a film or over the black hold after it (tailSeconds). Times count from the film's start.
    [Serializable]
    public sealed class StoryOverlay
    {
        public const string Typewriter = "typewriter";
        public const string Title = "title";
        // Two lines split by '|', printed over each other slightly out of register.
        public const string Doubled = "doubled";
        public const string Caption = "caption";

        public float at;
        public float until;
        public string kind;
        public string zh;
        public string en;
        // Zero uses the kind's default.
        public float fadeIn;
        public float fadeOut;
        // Typewriter only: seconds per character.
        public float charSeconds;
        // Vertical centre as a fraction of the screen height; zero uses the kind's default.
        public float y;

        public static bool IsKnownKind(string kind) => kind == Typewriter || kind == Title || kind == Doubled || kind == Caption;
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
        // With unskippable: locked the first time, skippable once this film has been seen in any run.
        public bool skippableOnceSeen;
        // No dimming, numbering or countdown bar around choices; for scenes that must look text-free.
        public bool hideChrome;
        // Seconds of black held after the video ends, for overlays; choices appear after it.
        public float tailSeconds;
        public StoryOverlay[] overlays = new StoryOverlay[0];
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
        // A film made only of overlays over black still plays when its video file is absent.
        public bool HasOverlays => (overlays != null && overlays.Length > 0) || tailSeconds > 0;
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
                if (node.tailSeconds < 0) problems.Add(node.id + ": tailSeconds cannot be negative.");
                if (node.overlays != null)
                    foreach (var overlay in node.overlays)
                    {
                        if (!node.IsFilm) { problems.Add(node.id + ": overlays only work on films."); break; }
                        if (!StoryOverlay.IsKnownKind(overlay.kind)) problems.Add(node.id + ": unknown overlay kind: " + overlay.kind);
                        if (overlay.until <= overlay.at) problems.Add(node.id + ": an overlay ends before it starts.");
                        if (string.IsNullOrEmpty(overlay.zh) && string.IsNullOrEmpty(overlay.en)) problems.Add(node.id + ": an overlay has no text.");
                    }
                if (node.HasChoices && (node.defaultChoice < 0 || node.defaultChoice >= node.choices.Length))
                    problems.Add(node.id + ": defaultChoice is out of range.");
            }
            return problems;
        }
    }
}
