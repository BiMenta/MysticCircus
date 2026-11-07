using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MysticCircus
{
    // Full-screen reading view of 《表演守则》. The printed rules are UI text (so they switch language);
    // every scratch, scribble and stitch is drawn on top from RuleSheet.Marks. E, Esc or a click closes it.
    // Persistent service, created with GameSession.
    public sealed class RuleSheetView : MonoBehaviour
    {
        static RuleSheetView instance;
        public static bool IsOpen { get; private set; }
        int openedFrame;

        static readonly Color Paper = new Color(0.9f, 0.86f, 0.76f);
        static readonly Color Print = new Color(0.2f, 0.18f, 0.2f);
        static readonly Color Nail = new Color(0.35f, 0.3f, 0.32f, 0.85f);
        static readonly Color RedPen = new Color(0.78f, 0.1f, 0.12f, 0.9f);
        static readonly Color[] Threads = { new Color(0.85f, 0.25f, 0.35f), new Color(0.25f, 0.5f, 0.85f), new Color(0.95f, 0.75f, 0.2f) };
        // Mother (pencil grey), father (red pen), brother (blue crayon).
        static readonly Color[] ThreePens = { new Color(0.3f, 0.3f, 0.34f, 0.9f), new Color(0.78f, 0.1f, 0.12f, 0.9f), new Color(0.2f, 0.35f, 0.8f, 0.85f) };
        static readonly Color Crayon = new Color(0.85f, 0.35f, 0.2f, 0.95f);

        void Awake()
        {
            instance = this;
            SceneManager.sceneLoaded += (_, __) => { if (IsOpen) Close(); };
        }

        public static void Open()
        {
            if (instance == null || IsOpen || Album.IsOpen || FilmPlayer.Active || FilmPlayer.ClipPlaying) return;
            IsOpen = true;
            instance.openedFrame = Time.frameCount;
            InputGate.Hold();
            Sfx.Play(SfxKind.Page, 0.7f);
        }

        public static void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            InputGate.Release();
            Sfx.Play(SfxKind.Page, 0.4f);
        }

        void Update()
        {
            // The E press that opened the sheet must not also close it.
            if (!IsOpen || Time.frameCount <= openedFrame) return;
            var keyboard = Keyboard.current;
            bool close = keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame);
            if (close || GuiKit.Clicked) Close();
        }

        void OnGUI()
        {
            if (!IsOpen) return;
            GUI.depth = -40;
            GuiKit.Begin();
            Vector2 canvas = GuiKit.Canvas;
            GuiKit.Fill(new Rect(Vector2.zero, canvas), new Color(0.03f, 0.03f, 0.04f, 0.8f));
            var sheet = new Rect(canvas.x / 2 - 300, canvas.y / 2 - 320, 600, 640);
            GuiKit.Fill(new Rect(sheet.x + 8, sheet.y + 10, sheet.width, sheet.height), new Color(0, 0, 0, 0.4f));
            GuiKit.Fill(sheet, Paper);
            // Foxing and a fold, so the sheet reads as old paper rather than a panel.
            GuiKit.Fill(new Rect(sheet.x, sheet.center.y - 1, sheet.width, 2), new Color(0.6f, 0.55f, 0.45f, 0.25f));
            var random = new System.Random(7);
            for (int i = 0; i < 14; i++)
                GuiKit.Circle(new Vector2(sheet.x + (float)random.NextDouble() * sheet.width, sheet.y + (float)random.NextDouble() * sheet.height),
                    3 + (float)random.NextDouble() * 9, new Color(0.62f, 0.5f, 0.32f, 0.12f));

            GuiKit.Label(new Rect(sheet.x, sheet.y + 40, sheet.width, 50), RuleSheet.Title, GuiKit.Text(34, TextAnchor.MiddleCenter, FontStyle.Bold), Print);
            for (int i = 0; i < RuleSheet.Count; i++)
                GuiKit.Label(LineRect(sheet, i), RuleSheet.Rule(i), GuiKit.Text(24, TextAnchor.MiddleLeft), Print);

            if (GameSession.Instance != null)
                foreach (var mark in RuleSheet.Marks(GameSession.Instance.HasFlag)) DrawMark(LineRect(sheet, mark.Rule), mark);
        }

        static Rect LineRect(Rect sheet, int rule) => new Rect(sheet.x + 60, sheet.y + 130 + rule * 88, sheet.width - 120, 60);

        static void DrawMark(Rect line, RuleMark mark)
        {
            // Seeded per rule and kind so the marks look hand-made but never change between frames.
            var random = new System.Random(mark.Rule * 31 + (int)mark.Kind * 7 + 3);
            float Jitter(float range) => ((float)random.NextDouble() * 2 - 1) * range;
            float y = line.center.y;
            switch (mark.Kind)
            {
                case RuleMarkKind.NailScratch:
                    // A long, thin fingernail scratch beside the line: two near-parallel strokes.
                    for (int s = 0; s < 2; s++)
                    {
                        float x = line.xMax - 40 + s * 7;
                        GuiKit.Line(new Vector2(x, y - 34), new Vector2(x + 18, y + 34), 2.4f, Nail);
                    }
                    break;
                case RuleMarkKind.RedScribble:
                    // Pressed hard: dense zigzags across the whole line.
                    var at = new Vector2(line.x - 6, y);
                    for (int s = 0; s < 26; s++)
                    {
                        var next = new Vector2(line.x + (s + 1) * (line.width * 0.75f / 26), y + (s % 2 == 0 ? -14 : 14) + Jitter(5));
                        GuiKit.Line(at, next, 3.2f, RedPen);
                        at = next;
                    }
                    break;
                case RuleMarkKind.Stitched:
                    // Stitched out one cross at a time, the thread colour changing along the way.
                    for (int s = 0; s < 14; s++)
                    {
                        float x = line.x + 8 + s * 24;
                        var thread = Threads[s % Threads.Length];
                        GuiKit.Line(new Vector2(x, y - 10), new Vector2(x + 16, y + 10), 2.4f, thread);
                        GuiKit.Line(new Vector2(x, y + 10), new Vector2(x + 16, y - 10), 2.4f, thread);
                    }
                    break;
                case RuleMarkKind.ThreePens:
                    for (int p = 0; p < ThreePens.Length; p++)
                    {
                        float offset = (p - 1) * 7;
                        GuiKit.Line(new Vector2(line.x - 8, y + offset + Jitter(3)), new Vector2(line.x + line.width * 0.8f, y + offset + Jitter(6)), 2.6f, ThreePens[p]);
                    }
                    break;
                case RuleMarkKind.CrayonWords:
                    string[] words = { L.T("叫", "Call"), L.T("大声叫", "Call out loud"), L.T("叫妈妈", "Call Mom") };
                    var style = GuiKit.Text(20, TextAnchor.MiddleLeft, FontStyle.Bold);
                    // Laid end to end by measured width, so the three never overlap in either language.
                    float wordX = line.x + 190;
                    for (int w = 0; w < words.Length; w++)
                    {
                        float width = style.CalcSize(new GUIContent(words[w])).x;
                        GuiKit.Label(new Rect(wordX, y + 24 + w * 7 + Jitter(2), width + 4, 30), words[w], style, Crayon);
                        wordX += width + 16;
                    }
                    break;
            }
        }
    }
}
