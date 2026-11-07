using System;
using System.Collections.Generic;

namespace MysticCircus
{
    // 「前」 is before the abduction, 「后」 after it. A pair always joins one of each.
    public enum MemoryTime { Before, After }

    public enum RuleMarkKind { NailScratch, RedScribble, Stitched, ThreePens, CrayonWords }

    public readonly struct RuleMark
    {
        public readonly int Rule;
        public readonly RuleMarkKind Kind;
        public RuleMark(int rule, RuleMarkKind kind) { Rule = rule; Kind = kind; }
    }

    public sealed class MemoryFragment
    {
        public const string FlagPrefix = "frag.";
        public readonly string Id;
        public readonly MemoryTime Time;
        // Where it can be found. Content may still place it elsewhere; this is for the album and checks.
        public readonly RevealLayer Layer;
        readonly string titleZh, titleEn;

        public MemoryFragment(string id, MemoryTime time, RevealLayer layer, string zh, string en)
        {
            Id = id; Time = time; Layer = layer; titleZh = zh; titleEn = en;
        }

        public string Flag => FlagPrefix + Id;
        public string Title => L.T(titleZh, titleEn);
    }

    public sealed class PairRecipe
    {
        public const string ComboPrefix = "combo.";
        public readonly string Id;
        public readonly string BeforeId, AfterId;
        // Path inside StreamingAssets/Films played full screen on success; null plays nothing.
        public readonly string Clip;
        public readonly RuleMark[] Marks;

        public PairRecipe(string id, string before, string after, string clip, params RuleMark[] marks)
        {
            Id = id; BeforeId = before; AfterId = after; Clip = clip; Marks = marks;
        }

        public string ComboFlag => ComboPrefix + Id;
        public string TrustFlag => Trust.Prefix + "combo_" + Id;
        public bool Uses(string fragmentId) => fragmentId == BeforeId || fragmentId == AfterId;
    }

    // Every memory fragment and pair in the game. Data lives in code so tests can check it and no asset can drift from it.
    public static class MemoryCatalog
    {
        public const string FinalRecipe = "final";

        public static readonly MemoryFragment[] Fragments =
        {
            new MemoryFragment("u1_ticket", MemoryTime.Before, RevealLayer.Warm, "半张票根", "Half a ticket stub"),
            new MemoryFragment("u1_scratch", MemoryTime.After, RevealLayer.Cold, "两道划痕", "Two scratches"),
            new MemoryFragment("u2_dice", MemoryTime.Before, RevealLayer.Warm, "木骰子", "Wooden dice"),
            new MemoryFragment("u2_pills", MemoryTime.After, RevealLayer.Cold, "白色药片", "White pills"),
            new MemoryFragment("u3_family", MemoryTime.Before, RevealLayer.Warm, "缺一个位置的全家福", "A family photo with a gap"),
            new MemoryFragment("u3_back", MemoryTime.After, RevealLayer.Cold, "照片背面", "The back of the photo"),
            new MemoryFragment("u4_knot", MemoryTime.Before, RevealLayer.Warm, "绳结手偶", "Knotted hand puppet"),
            new MemoryFragment("u4_tag", MemoryTime.After, RevealLayer.Cold, "编号牌与缝字线", "Number tag and stitched thread"),
            new MemoryFragment("final_circles", MemoryTime.Before, RevealLayer.Dream, "三个圆的画", "Drawing of three circles"),
            new MemoryFragment("final_strokes", MemoryTime.After, RevealLayer.Dream, "三色划痕", "Strokes in three colors"),
        };

        public static readonly PairRecipe[] Recipes =
        {
            new PairRecipe("u1", "u1_ticket", "u1_scratch", "u1/1-5.mp4", new RuleMark(0, RuleMarkKind.NailScratch)),
            new PairRecipe("u2", "u2_dice", "u2_pills", "u2/2-5.mp4", new RuleMark(1, RuleMarkKind.RedScribble)),
            new PairRecipe("u3", "u3_family", "u3_back", "u3/3-5.mp4", new RuleMark(2, RuleMarkKind.Stitched)),
            new PairRecipe("u4", "u4_knot", "u4_tag", "u4/4-5.mp4", new RuleMark(4, RuleMarkKind.ThreePens)),
            new PairRecipe(FinalRecipe, "final_circles", "final_strokes", null,
                new RuleMark(2, RuleMarkKind.ThreePens), new RuleMark(2, RuleMarkKind.CrayonWords)),
        };

        public static MemoryFragment Find(string id)
        {
            foreach (var fragment in Fragments) if (fragment.Id == id) return fragment;
            return null;
        }

        public static PairRecipe FindRecipe(string id)
        {
            foreach (var recipe in Recipes) if (recipe.Id == id) return recipe;
            return null;
        }

        // The pair these two make, in either order, or null.
        public static PairRecipe Match(string a, string b)
        {
            if (a == null || b == null || a == b) return null;
            foreach (var recipe in Recipes)
                if ((recipe.BeforeId == a && recipe.AfterId == b) || (recipe.BeforeId == b && recipe.AfterId == a)) return recipe;
            return null;
        }

        public static PairRecipe RecipeOf(string fragmentId)
        {
            foreach (var recipe in Recipes) if (recipe.Uses(fragmentId)) return recipe;
            return null;
        }

        // Structural mistakes in the tables above, for the EditMode test.
        public static List<string> Validate()
        {
            var problems = new List<string>();
            var ids = new HashSet<string>();
            foreach (var fragment in Fragments)
                if (!ids.Add(fragment.Id)) problems.Add("Duplicate fragment: " + fragment.Id);
            var used = new HashSet<string>();
            foreach (var recipe in Recipes)
            {
                var before = Find(recipe.BeforeId);
                var after = Find(recipe.AfterId);
                if (before == null || after == null) { problems.Add(recipe.Id + " names a missing fragment."); continue; }
                if (before.Time != MemoryTime.Before) problems.Add(recipe.Id + ": " + before.Id + " is not a Before fragment.");
                if (after.Time != MemoryTime.After) problems.Add(recipe.Id + ": " + after.Id + " is not an After fragment.");
                if (!used.Add(before.Id) || !used.Add(after.Id)) problems.Add(recipe.Id + " reuses a fragment.");
                foreach (var mark in recipe.Marks)
                    if (mark.Rule < 0 || mark.Rule >= RuleSheet.Count) problems.Add(recipe.Id + " marks a rule that does not exist.");
            }
            return problems;
        }

        internal static IEnumerable<T> Each<T>(IEnumerable<string> flags, string prefix, Func<string, T> find) where T : class
        {
            foreach (string flag in flags)
            {
                if (!flag.StartsWith(prefix, StringComparison.Ordinal)) continue;
                var item = find(flag.Substring(prefix.Length));
                if (item != null) yield return item;
            }
        }
    }
}
