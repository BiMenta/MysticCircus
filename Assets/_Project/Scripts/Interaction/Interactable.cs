using UnityEngine;
using UnityEngine.Events;

namespace MysticCircus
{
    // Single entry point for anything the player can use. Precedence: collectible, scene exit, room action.
    public sealed class Interactable : MonoBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_Color");
        const float FocusBrighten = 1.3f;

        public string Prompt = "Interact";
        public RoomController Room;
        public string Action;
        public int Value;
        public string NextScene;
        public bool RequiresSolved;
        // Non-empty: this object is a pickup, saved as a GameSession flag and hidden once taken.
        public string CollectibleId;
        // Runs after a successful use, for wiring content without a new script.
        // Created here as well as by serialization, so components added from code can listen to it.
        public UnityEvent Used = new UnityEvent();

        Renderer[] parts;
        Color[] baseColors;
        MaterialPropertyBlock block;
        bool focused;
        Color glow = Color.black;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            parts = GetComponentsInChildren<Renderer>(true);
            baseColors = new Color[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                var material = parts[i].sharedMaterial;
                baseColors[i] = material != null && material.HasProperty(ColorId) ? material.color : Color.white;
            }
            if (!string.IsNullOrEmpty(CollectibleId) && GameSession.Instance != null && GameSession.Instance.HasFlag(CollectibleId))
                gameObject.SetActive(false);
        }

        public void Use()
        {
            if (!string.IsNullOrEmpty(CollectibleId))
            {
                GameSession.Instance.SetFlag(CollectibleId);
                Sfx.Play(SfxKind.Pickup);
                gameObject.SetActive(false);
                Used?.Invoke();
                return;
            }
            if (!string.IsNullOrEmpty(NextScene))
            {
                if (RequiresSolved && (Room == null || !Room.IsSolved))
                {
                    Sfx.Play(SfxKind.Bad, 0.6f);
                    PrototypeHUD.Notice(L.T("门锁着，先完成这个房间的谜题。", "The door is locked. Finish this room's puzzles."));
                    return;
                }
                Sfx.Play(SfxKind.Door);
                Used?.Invoke();
                // The story decides where an exit leads; NextScene covers scenes outside the story.
                if (!StoryDirector.Advance()) ScreenFader.Load(NextScene);
                return;
            }
            if (Room != null) Room.Interact(Action, Value);
            Used?.Invoke();
        }

        public void SetFocused(bool value)
        {
            focused = value;
            Refresh();
        }

        // Persistent tint used by puzzles to show state, such as a lit key or a thrown switch.
        public void SetGlow(Color color)
        {
            glow = color;
            Refresh();
        }

        // Tints through a property block, so it works on any material and never edits shared assets.
        void Refresh()
        {
            if (parts == null) return;
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (part == null || part is LineRenderer || part.GetComponent<TextMesh>() != null) continue;
                Color color = baseColors[i] + glow;
                if (focused) color *= FocusBrighten;
                color.a = baseColors[i].a;
                block.SetColor(ColorId, color);
                part.SetPropertyBlock(block);
            }
        }
    }
}
