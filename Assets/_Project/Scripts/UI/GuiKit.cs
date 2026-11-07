using UnityEngine;
using UnityEngine.InputSystem;

namespace MysticCircus
{
    // Shared IMGUI helpers. Every screen is laid out on a 1280×720 virtual canvas and scaled to fit.
    // IMGUI gets no mouse events while only the Input System is active, so hit tests use Mouse.current directly.
    // Colors here are written as they should appear. The project renders in linear space, where IMGUI would encode
    // them a second time and wash them out, so everything drawn through this class is converted first.
    public static class GuiKit
    {
        public const float Width = 1280, Height = 720;
        static Texture2D disc, ring;
        static GUIStyle text;

        public static float Scale => Mathf.Min(Screen.width / Width, Screen.height / Height);
        // Size of the visible canvas in virtual units; wider than 1280 on ultra-wide screens.
        public static Vector2 Canvas => new Vector2(Screen.width / Scale, Screen.height / Scale);

        public static void Begin() => GUI.matrix = Matrix4x4.Scale(Vector3.one * Scale);

        public static Vector2 Mouse
        {
            get
            {
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse == null) return new Vector2(-1, -1);
                Vector2 position = mouse.position.ReadValue();
                return new Vector2(position.x, Screen.height - position.y) / Scale;
            }
        }

        public static bool Clicked => UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
        public static bool Released => UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasReleasedThisFrame;

        // World point to canvas, or the screen centre when it is behind the camera.
        public static Vector2 FromWorld(Vector3 world)
        {
            var camera = Camera.main;
            if (camera == null) return Canvas / 2;
            Vector3 screen = camera.WorldToScreenPoint(world);
            if (screen.z <= 0) return Canvas / 2;
            return new Vector2(screen.x, Screen.height - screen.y) / Scale;
        }

        public static Texture2D Disc => disc != null ? disc : disc = Round(64, 0);
        public static Texture2D Ring => ring != null ? ring : ring = Round(128, 0.82f);

        // Anti-aliased disc, or a ring when inner > 0 (fraction of the radius).
        static Texture2D Round(int size, float inner)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            float radius = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius)) / radius;
                float a = Mathf.Clamp01((1 - d) * radius);
                if (inner > 0) a *= Mathf.Clamp01((d - inner) * radius);
                texture.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            texture.Apply();
            return texture;
        }

        // .linear keeps alpha unchanged.
        static Color Display(Color color) => QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;

        public static void Fill(Rect area, Color color)
        {
            var previous = GUI.color;
            GUI.color = Display(color);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        public static void Draw(Rect area, Texture texture, Color color)
        {
            var previous = GUI.color;
            GUI.color = Display(color);
            GUI.DrawTexture(area, texture, ScaleMode.StretchToFill);
            GUI.color = previous;
        }

        public static void Circle(Vector2 centre, float radius, Color color, bool hollow = false) =>
            Draw(new Rect(centre.x - radius, centre.y - radius, radius * 2, radius * 2), hollow ? Ring : Disc, color);

        // A straight stroke in canvas space, rotated about its start.
        public static void Line(Vector2 from, Vector2 to, float width, Color color)
        {
            var matrix = GUI.matrix;
            float angle = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;
            GUI.matrix = matrix * Matrix4x4.TRS(from, Quaternion.Euler(0, 0, angle), Vector3.one) * Matrix4x4.TRS(-from, Quaternion.identity, Vector3.one);
            Fill(new Rect(from.x, from.y - width / 2, Vector2.Distance(from, to), width), color);
            GUI.matrix = matrix;
        }

        public static GUIStyle Text(int size, TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            if (text == null) text = new GUIStyle(GUI.skin.label) { wordWrap = true, richText = false };
            text.fontSize = size;
            text.alignment = anchor;
            text.fontStyle = style;
            return text;
        }

        public static void Label(Rect area, string content, GUIStyle style, Color color, bool shadow = false)
        {
            var previous = style.normal.textColor;
            if (shadow)
            {
                style.normal.textColor = new Color(0, 0, 0, color.a * 0.8f);
                GUI.Label(new Rect(area.x + 2, area.y + 2, area.width, area.height), content, style);
            }
            style.normal.textColor = Display(color);
            GUI.Label(area, content, style);
            style.normal.textColor = previous;
        }

        public static bool KeyPressed(Key key) => Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
    }
}
