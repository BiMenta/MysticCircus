using System;
using System.Collections.Generic;

namespace MysticCircus
{
    // 《表演守则》: the same sheet hangs at every unit's entrance. Its marks come only from completed pairs,
    // so every copy shows the same progress without storing anything of its own.
    public static class RuleSheet
    {
        static readonly string[] Zh = { "一、不许说话。", "二、哭要笑。", "三、不许回头。", "四、谢幕前不许叫真名。", "五、不按规则演就会被打。" };
        static readonly string[] En =
        {
            "1. No talking.", "2. If you cry, smile.", "3. Never look back.",
            "4. Never say your real name before the curtain call.", "5. Break the rules and you will be punished.",
        };

        public static int Count => Zh.Length;
        public static string Title => L.T("表演守则", "Rules of the Show");
        public static string Rule(int index) => L.T(Zh[index], En[index]);

        public static List<RuleMark> Marks(Func<string, bool> hasFlag)
        {
            var marks = new List<RuleMark>();
            foreach (var recipe in MemoryCatalog.Recipes)
                if (hasFlag(recipe.ComboFlag)) marks.AddRange(recipe.Marks);
            return marks;
        }
    }
}
