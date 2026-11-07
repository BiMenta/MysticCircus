using System;
using System.Collections.Generic;

namespace MysticCircus
{
    // Trust is never shown. It is the number of distinct "trust." flags, so awarding the same moment twice
    // (replaying a room, reloading a save) can never count twice.
    public static class Trust
    {
        public const string Prefix = "trust.";

        public static int Count(IEnumerable<string> flags, string prefix)
        {
            int count = 0;
            foreach (string flag in flags) if (flag.StartsWith(prefix, StringComparison.Ordinal)) count++;
            return count;
        }

        static IEnumerable<string> Flags => GameSession.Instance != null ? GameSession.Instance.Data.Flags : (IEnumerable<string>)Array.Empty<string>();

        public static int Value => Count(Flags, Prefix);
        public static int Combos => Count(Flags, PairRecipe.ComboPrefix);

        // For "followed the performer's hint" moments. The id names the moment, e.g. "u1_follow".
        public static void Award(string id)
        {
            if (GameSession.Instance != null) GameSession.Instance.SetFlag(Prefix + id);
        }
    }

    public enum Ending { E1, E2, E3 }

    public enum FinaleChoice { Bow, CallTheName, SayOwnName }

    // The ending rules from the script, merged into one decision (open questions 2 and 3 in the unit document).
    public static class Endings
    {
        public const int TrustNeeded = 3;
        public const int AllCombos = 5;

        // Without the final pair, only "bow" is offered.
        public static bool BreakoutUnlocked(Func<string, bool> hasFlag) =>
            hasFlag(PairRecipe.ComboPrefix + MemoryCatalog.FinalRecipe);

        public static Ending Resolve(FinaleChoice choice, int trust, int combos)
        {
            if (choice == FinaleChoice.Bow || trust < TrustNeeded) return Ending.E1;
            return combos >= AllCombos ? Ending.E3 : Ending.E2;
        }
    }
}
