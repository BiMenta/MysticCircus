using System.Collections.Generic;
using NUnit.Framework;

namespace MysticCircus.Tests
{
    // Rules shared by every unit: layers, the memory catalog, the rule sheet, trust, endings and film overlays.
    public sealed class NarrativeSystemsTests
    {
        [Test]
        public void MirrorSlot_FlipsLeftAndRightAndKeepsTheMiddle()
        {
            Assert.AreEqual(2, PuzzleLogic.MirrorSlot(0, 3));
            Assert.AreEqual(1, PuzzleLogic.MirrorSlot(1, 3));
            Assert.AreEqual(0, PuzzleLogic.MirrorSlot(2, 3));
        }

        [Test]
        public void Reveal_CyclesDreamWarmColdDream()
        {
            Assert.AreEqual(RevealLayer.Warm, RevealLogic.Next(RevealLayer.Dream));
            Assert.AreEqual(RevealLayer.Cold, RevealLogic.Next(RevealLayer.Warm));
            Assert.AreEqual(RevealLayer.Dream, RevealLogic.Next(RevealLayer.Cold));
        }

        [Test]
        public void Catalog_IsConsistent()
        {
            Assert.IsEmpty(MemoryCatalog.Validate());
            Assert.AreEqual(Endings.AllCombos, MemoryCatalog.Recipes.Length, "Four units plus the final pair.");
        }

        [Test]
        public void Match_WorksInEitherOrderAndRejectsNonPairs()
        {
            Assert.AreEqual("u1", MemoryCatalog.Match("u1_ticket", "u1_scratch").Id);
            Assert.AreEqual("u1", MemoryCatalog.Match("u1_scratch", "u1_ticket").Id);
            Assert.IsNull(MemoryCatalog.Match("u1_ticket", "u2_pills"), "Fragments from different units never pair.");
            Assert.IsNull(MemoryCatalog.Match("u1_ticket", "u1_ticket"));
            Assert.IsNull(MemoryCatalog.Match(null, "u1_ticket"));
            Assert.AreEqual("u2", MemoryCatalog.RecipeOf("u2_pills").Id);
        }

        [Test]
        public void RuleSheet_MarksFollowCompletedPairs()
        {
            Assert.IsEmpty(RuleSheet.Marks(_ => false));
            var first = RuleSheet.Marks(flag => flag == "combo.u1");
            Assert.AreEqual(1, first.Count);
            Assert.AreEqual(0, first[0].Rule);
            Assert.AreEqual(RuleMarkKind.NailScratch, first[0].Kind);

            var final = RuleSheet.Marks(flag => flag == "combo.final");
            Assert.IsTrue(final.Exists(m => m.Rule == 2 && m.Kind == RuleMarkKind.ThreePens));
            Assert.IsTrue(final.Exists(m => m.Rule == 2 && m.Kind == RuleMarkKind.CrayonWords));
            Assert.IsTrue(RuleSheet.Marks(flag => flag == "combo.u4").Exists(m => m.Rule == 4), "Unit four strikes out rule five.");
        }

        [Test]
        public void Trust_CountsDistinctPrefixedFlags()
        {
            var flags = new List<string> { "trust.combo_u1", "trust.u1_follow", "combo.u1", "frag.u1_ticket", "trusty" };
            Assert.AreEqual(2, Trust.Count(flags, Trust.Prefix));
            Assert.AreEqual(1, Trust.Count(flags, PairRecipe.ComboPrefix));
        }

        [TestCase(FinaleChoice.Bow, 9, 5, Ending.E1)]
        [TestCase(FinaleChoice.CallTheName, 2, 5, Ending.E1)]
        [TestCase(FinaleChoice.CallTheName, 3, 4, Ending.E2)]
        [TestCase(FinaleChoice.SayOwnName, 3, 4, Ending.E2)]
        [TestCase(FinaleChoice.SayOwnName, 5, 5, Ending.E3)]
        public void Endings_FollowTheScript(FinaleChoice choice, int trust, int combos, Ending expected)
        {
            Assert.AreEqual(expected, Endings.Resolve(choice, trust, combos));
        }

        [Test]
        public void Endings_BreakoutNeedsTheFinalPair()
        {
            Assert.IsFalse(Endings.BreakoutUnlocked(flag => flag == "combo.u1"));
            Assert.IsTrue(Endings.BreakoutUnlocked(flag => flag == "combo.final"));
        }

        [Test]
        public void Overlay_FadesInHoldsAndFadesOut()
        {
            var title = new StoryOverlay { at = 10, until = 20, kind = StoryOverlay.Title };
            Assert.AreEqual(0, FilmOverlayMath.Alpha(title, 9.9f));
            Assert.AreEqual(0.5f, FilmOverlayMath.Alpha(title, 11.5f), 0.001f, "Titles fade in over three seconds.");
            Assert.AreEqual(1, FilmOverlayMath.Alpha(title, 15));
            Assert.AreEqual(0, FilmOverlayMath.Alpha(title, 20));
            var shortOne = new StoryOverlay { at = 0, until = 2, kind = StoryOverlay.Title };
            Assert.AreEqual(1, FilmOverlayMath.Alpha(shortOne, 1), 0.001f, "Fades shrink to fit a short overlay.");
        }

        [Test]
        public void Typewriter_RevealsOneCharacterPerStep()
        {
            var line = new StoryOverlay { at = 46, until = 55, kind = StoryOverlay.Typewriter };
            const string text = "你醒了。";
            Assert.AreEqual(0, FilmOverlayMath.VisibleCharacters(line, text, 45.9f));
            Assert.AreEqual(1, FilmOverlayMath.VisibleCharacters(line, text, 46));
            Assert.AreEqual(2, FilmOverlayMath.VisibleCharacters(line, text, 46.5f));
            Assert.AreEqual(4, FilmOverlayMath.VisibleCharacters(line, text, 50));
            Assert.AreEqual(1, FilmOverlayMath.Alpha(line, 46), "Typing starts fully visible.");
        }

        [Test]
        public void TimingQte_AcceptsOnlyTheWindow()
        {
            Assert.IsTrue(TimingQte.IsHit(1.0f, 1.0f, 0.15f));
            Assert.IsTrue(TimingQte.IsHit(0.9f, 1.0f, 0.15f));
            Assert.IsFalse(TimingQte.IsHit(0.7f, 1.0f, 0.15f));
            Assert.IsFalse(TimingQte.IsHit(1.2f, 1.0f, 0.15f));
        }

        [Test]
        public void MirrorDelay_StretchRampsHoldsAndRecovers()
        {
            Assert.AreEqual(0, DelayedMirrorBody.StretchWeight(-1, 0.3f, 1, 2));
            Assert.AreEqual(0.5f, DelayedMirrorBody.StretchWeight(0.15f, 0.3f, 1, 2), 0.001f);
            Assert.AreEqual(1, DelayedMirrorBody.StretchWeight(1, 0.3f, 1, 2));
            Assert.AreEqual(0.5f, DelayedMirrorBody.StretchWeight(2.3f, 0.3f, 1, 2), 0.001f);
            Assert.AreEqual(0, DelayedMirrorBody.StretchWeight(10, 0.3f, 1, 2));
        }

        [TestCase(@"{""start"":""a"",""nodes"":[{""id"":""a"",""type"":""film"",""video"":""v"",""overlays"":[{""at"":1,""until"":2,""kind"":""banner"",""zh"":""x""}]}]}", "unknown overlay kind")]
        [TestCase(@"{""start"":""a"",""nodes"":[{""id"":""a"",""type"":""film"",""video"":""v"",""overlays"":[{""at"":3,""until"":2,""kind"":""title"",""zh"":""x""}]}]}", "ends before it starts")]
        [TestCase(@"{""start"":""a"",""nodes"":[{""id"":""a"",""type"":""film"",""video"":""v"",""overlays"":[{""at"":1,""until"":2,""kind"":""title""}]}]}", "no text")]
        [TestCase(@"{""start"":""a"",""nodes"":[{""id"":""a"",""type"":""film"",""video"":""v"",""tailSeconds"":-1}]}", "tailSeconds")]
        public void Validate_ReportsOverlayMistakes(string json, string expected)
        {
            var problems = StoryGraph.FromJson(json).Validate();
            Assert.IsTrue(problems.Exists(p => p.Contains(expected)), string.Join("; ", problems));
        }

        [Test]
        public void Json_ReadsOverlayFields()
        {
            var node = StoryGraph.FromJson(@"{""start"":""a"",""nodes"":[{""id"":""a"",""type"":""film"",""video"":""v"",""hideChrome"":true,""tailSeconds"":34,
                ""overlays"":[{""at"":58,""until"":78,""kind"":""doubled"",""zh"":""第 47 场|第 47 天""}]}]}").Find("a");
            Assert.IsTrue(node.hideChrome);
            Assert.AreEqual(34, node.tailSeconds);
            Assert.IsTrue(node.HasOverlays);
            Assert.AreEqual("doubled", node.overlays[0].kind);
        }
    }
}
