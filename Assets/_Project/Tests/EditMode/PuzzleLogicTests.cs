using System;
using System.Linq;
using NUnit.Framework;

namespace MysticCircus.Tests
{
    public sealed class PuzzleLogicTests
    {
        [Test]
        public void Matcher_CompletesOnExactSequence()
        {
            var matcher = new SequenceMatcher(new[] { 0, 2, 4, 2 });
            Assert.IsTrue(matcher.Feed(0));
            Assert.IsTrue(matcher.Feed(2));
            Assert.IsTrue(matcher.Feed(4));
            Assert.IsFalse(matcher.Complete);
            Assert.IsTrue(matcher.Feed(2));
            Assert.IsTrue(matcher.Complete);
        }

        [Test]
        public void Matcher_WrongInputDropsProgress()
        {
            var matcher = new SequenceMatcher(new[] { 0, 2, 4 });
            matcher.Feed(0);
            Assert.IsFalse(matcher.Feed(3));
            Assert.AreEqual(0, matcher.Step);
        }

        [Test]
        public void Matcher_WrongInputThatRestartsSequenceKeepsThatStep()
        {
            var matcher = new SequenceMatcher(new[] { 0, 2, 4, 2 });
            foreach (int value in new[] { 0, 0, 2, 4, 2 }) matcher.Feed(value);
            Assert.IsTrue(matcher.Complete);
        }

        [Test]
        public void Matcher_KeepsOverlappingPrefixAfterSlip()
        {
            var matcher = new SequenceMatcher(new[] { 1, 2, 1, 3 });
            foreach (int value in new[] { 1, 2, 1, 2, 1, 3 }) matcher.Feed(value);
            Assert.IsTrue(matcher.Complete);
        }

        [Test]
        public void Matcher_ResetAndCompletedInputAreInert()
        {
            var matcher = new SequenceMatcher(new[] { 1 });
            matcher.Feed(1);
            Assert.IsFalse(matcher.Feed(1));
            Assert.AreEqual(1, matcher.Step);
            matcher.Reset();
            Assert.AreEqual(0, matcher.Step);
        }

        [Test]
        public void Opposites_SwapWithinPairs()
        {
            CollectionAssert.AreEqual(new[] { 1, 0, 3, 2 }, PuzzleLogic.Opposites(new[] { 0, 1, 2, 3 }));
        }

        [Test]
        public void Shuffled_IsAPermutation()
        {
            var values = PuzzleLogic.Shuffled(9, new Random(5));
            CollectionAssert.AreEquivalent(Enumerable.Range(0, 9).ToArray(), values);
        }

        [Test]
        public void NoRepeatSequence_NeverRepeatsNeighbours([Values(1, 2, 3)] int seed)
        {
            var sequence = PuzzleLogic.NoRepeatSequence(200, 4, new Random(seed));
            for (int i = 1; i < sequence.Length; i++) Assert.AreNotEqual(sequence[i - 1], sequence[i]);
            Assert.IsTrue(sequence.All(value => value >= 0 && value < 4));
        }

        [Test]
        public void Torque_PositiveWhenLeftHeavier()
        {
            var placement = new[] { PuzzleLogic.Pan.Left, PuzzleLogic.Pan.Right, PuzzleLogic.Pan.Table };
            Assert.AreEqual(2, PuzzleLogic.Torque(placement, new[] { 3, 1, 9 }));
        }

        [Test]
        public void IsBalanced_RequiresEqualSidesAndEmptyTable()
        {
            int[] masses = { 1, 2, 3, 4 };
            var L = PuzzleLogic.Pan.Left;
            var R = PuzzleLogic.Pan.Right;
            var T = PuzzleLogic.Pan.Table;
            Assert.IsTrue(PuzzleLogic.IsBalanced(new[] { L, R, R, L }, masses));
            Assert.IsFalse(PuzzleLogic.IsBalanced(new[] { L, L, R, R }, masses));
            Assert.IsFalse(PuzzleLogic.IsBalanced(new[] { T, T, T, T }, masses));
            // Level (1+2 against 3) but weight 4 is still on the table.
            Assert.IsFalse(PuzzleLogic.IsBalanced(new[] { L, L, R, T }, masses));
        }

        [Test]
        public void Decoys_ExcludeCorrectChoice([Range(0, 2)] int correct)
        {
            var decoys = PuzzleLogic.Decoys(correct, 3, new Random(correct));
            Assert.AreEqual(2, decoys.Length);
            CollectionAssert.DoesNotContain(decoys, correct);
            CollectionAssert.AllItemsAreUnique(decoys);
        }

        [Test]
        public void SameLayout_ComparesEverySlot()
        {
            Assert.IsTrue(PuzzleLogic.SameLayout(new[] { 2, 0, 1 }, new[] { 2, 0, 1 }));
            Assert.IsFalse(PuzzleLogic.SameLayout(new[] { 2, 1, 0 }, new[] { 2, 0, 1 }));
        }
    }
}
