using UnityEngine;

namespace MysticCircus
{
    // The shutter in the lower right: a camera icon and three dots for the current layer, no text.
    // It breathes until the player first uses it. Clicking it works whenever the cursor is free.
    public sealed class ShutterHUD : MonoBehaviour
    {
        static readonly Color Paper = new Color(0.96f, 0.92f, 0.86f);
        static readonly Color Ink = new Color(0.22f, 0.2f, 0.24f);
        public static readonly Color[] LayerColors = { new Color(0.97f, 0.95f, 0.98f), new Color(1f, 0.72f, 0.7f), new Color(0.62f, 0.7f, 0.8f) };

        // Canvas position of the icon, also the target that collected items fly into.
        public static Rect IconRect
        {
            get
            {
                Vector2 canvas = GuiKit.Canvas;
                return new Rect(canvas.x - 118, canvas.y - 112, 76, 54);
            }
        }

        float shownAt = -1;

        static bool Hidden => FilmPlayer.Active || FilmPlayer.ClipPlaying || RuleSheetView.IsOpen;

        void Update()
        {
            if (!RevealSystem.IsUnlocked || Hidden) return;
            if (Cursor.lockState != CursorLockMode.Locked && GuiKit.Clicked && IconRect.Contains(GuiKit.Mouse)) RevealSystem.Cycle();
        }

        void OnGUI()
        {
            if (!RevealSystem.IsUnlocked || Hidden) { shownAt = -1; return; }
            if (shownAt < 0) shownAt = Time.unscaledTime;
            GUI.depth = -20;
            GuiKit.Begin();
            float alpha = Mathf.Clamp01((Time.unscaledTime - shownAt) / 0.8f);
            if (!RevealSystem.HasBeenUsed) alpha *= 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.2f));
            if (!RevealSystem.CanSwitch) alpha *= 0.5f;
            DrawCamera(IconRect, alpha);

            Rect icon = IconRect;
            for (int i = 0; i < RevealLogic.LayerCount; i++)
            {
                bool current = (int)RevealSystem.Current == i;
                var centre = new Vector2(icon.center.x + (i - 1) * 16, icon.yMax + 14);
                var color = LayerColors[i];
                color.a = alpha * (current ? 1 : 0.35f);
                GuiKit.Circle(centre, current ? 5 : 3.5f, color);
            }
        }

        static void DrawCamera(Rect r, float alpha)
        {
            var paper = Paper; paper.a = alpha * 0.92f;
            var ink = Ink; ink.a = alpha;
            GuiKit.Fill(new Rect(r.x + 10, r.y - 7, 18, 9), paper);
            GuiKit.Fill(r, paper);
            GuiKit.Fill(new Rect(r.x + 4, r.y + 4, r.width - 8, r.height - 8), new Color(ink.r, ink.g, ink.b, alpha * 0.12f));
            GuiKit.Circle(r.center, 19, ink);
            GuiKit.Circle(r.center, 13, paper);
            var lens = LayerColors[(int)RevealSystem.Current]; lens.a = alpha;
            GuiKit.Circle(r.center, 9, lens);
            GuiKit.Circle(new Vector2(r.xMax - 11, r.y + 10), 3, ink);
        }
    }
}
