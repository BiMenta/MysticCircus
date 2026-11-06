using System;
using System.Collections.Generic;

namespace MysticCircus
{
    // Engine-free puzzle rules, covered by EditMode tests. Room controllers only add presentation.
    public sealed class SequenceMatcher
    {
        readonly int[] target;
        readonly List<int> history = new List<int>();
        public int Step { get; private set; }
        public int Length => target.Length;
        public bool Complete => Step == target.Length;

        public SequenceMatcher(int[] target) { this.target = target; }

        // Returns whether the input advanced the sequence. A wrong input keeps any progress it also
        // satisfies: for C-E-G-E, the slip "C C E G E" still solves, because the second C restarts it.
        public bool Feed(int value)
        {
            if (Complete) return false;
            int before = Step;
            history.Add(value);
            Step = LongestPrefixMatchingSuffix();
            return Step > before;
        }

        public void Reset() { history.Clear(); Step = 0; }

        int LongestPrefixMatchingSuffix()
        {
            for (int length = Math.Min(target.Length, history.Count); length > 0; length--)
            {
                bool match = true;
                for (int i = 0; i < length && match; i++)
                    match = target[i] == history[history.Count - length + i];
                if (match) return length;
            }
            return 0;
        }
    }

    public static class PuzzleLogic
    {
        public enum Pan { Table = 0, Left = 1, Right = 2 }

        // Options paired as 0/1, 2/3, ... each answer the other, as with mirrored gestures.
        public static int Opposite(int option) => option ^ 1;

        public static int[] Opposites(int[] options)
        {
            var answer = new int[options.Length];
            for (int i = 0; i < options.Length; i++) answer[i] = Opposite(options[i]);
            return answer;
        }

        public static int[] Shuffled(int count, Random random)
        {
            var values = new int[count];
            for (int i = 0; i < count; i++) values[i] = i;
            for (int i = count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
            return values;
        }

        // Each value differs from the last, so a demonstrated sequence always visibly changes.
        public static int[] NoRepeatSequence(int length, int alphabet, Random random)
        {
            var sequence = new int[length];
            for (int i = 0; i < length; i++)
            {
                int value;
                do value = random.Next(alphabet); while (i > 0 && value == sequence[i - 1]);
                sequence[i] = value;
            }
            return sequence;
        }

        // Left minus right. Positive means the left pan is heavier.
        public static int Torque(Pan[] placement, int[] masses)
        {
            int torque = 0;
            for (int i = 0; i < placement.Length; i++)
            {
                if (placement[i] == Pan.Left) torque += masses[i];
                if (placement[i] == Pan.Right) torque -= masses[i];
            }
            return torque;
        }

        // Every weight must be on a pan, so a level beam with weights left over does not count.
        public static bool IsBalanced(Pan[] placement, int[] masses)
        {
            foreach (var pan in placement) if (pan == Pan.Table) return false;
            return Torque(placement, masses) == 0;
        }

        public static bool SameLayout(int[] actual, int[] wanted)
        {
            for (int i = 0; i < wanted.Length; i++) if (actual[i] != wanted[i]) return false;
            return true;
        }

        // Every wrong choice in shuffled order, for a character who only ever points at false options.
        public static int[] Decoys(int correct, int optionCount, Random random)
        {
            var order = Shuffled(optionCount, random);
            var decoys = new List<int>();
            foreach (int option in order) if (option != correct) decoys.Add(option);
            return decoys.ToArray();
        }
    }
}
