using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MysticCircus
{
    // 显影: one per level that uses the shutter. Owns the current layer, the Q key, the shutter sound and the white flash.
    // Objects react through Changed (RevealLayerObject, RevealGrade, RevealAudio, RevealVideoSurface), so a level
    // only has to place them. A new scene always starts in Dream.
    public sealed class RevealSystem : MonoBehaviour
    {
        // Flags are run progress: once the shutter has appeared it stays for the rest of the run.
        public const string UnlockFlag = "reveal.unlocked";
        public const string UsedFlag = "reveal.used";

        public static RevealSystem Instance { get; private set; }
        public static RevealLayer Current => Instance != null ? Instance.layer : RevealLayer.Dream;
        public static event Action<RevealLayer> Changed;
        // Fired when the shutter is first given to the player.
        public static event Action Unlocked;

        [Tooltip("Later units start with the shutter already available.")]
        public bool UnlockedOnEnter;
        public float FlashSeconds = 0.15f;

        RevealLayer layer;
        float flashStart = -10;
        int holds;

        public static bool IsUnlocked => GameSession.Instance != null && GameSession.Instance.HasFlag(UnlockFlag);
        public static bool HasBeenUsed => GameSession.Instance != null && GameSession.Instance.HasFlag(UsedFlag);

        // False while anything else owns the screen or a level has paused switching.
        public static bool CanSwitch => Instance != null && IsUnlocked && Instance.holds == 0 && !InputGate.Blocked
            && !ScreenFader.Busy && !FilmPlayer.Active && !FilmPlayer.ClipPlaying;

        void Awake()
        {
            Instance = this;
            layer = RevealLayer.Dream;
            if (GetComponent<ShutterHUD>() == null) gameObject.AddComponent<ShutterHUD>();
        }

        void Start()
        {
            if (UnlockedOnEnter && GameSession.Instance != null) GameSession.Instance.SetFlag(UnlockFlag);
            var player = FindAnyObjectByType<FirstPersonPlayer>();
            var view = player != null ? player.View : Camera.main;
            if (view != null && view.GetComponent<RevealGrade>() == null) view.gameObject.AddComponent<RevealGrade>();
        }

        void OnDestroy()
        {
            // No Changed event here: during a scene unload it would re-activate objects that are being destroyed.
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (GuiKit.KeyPressed(Key.Q)) Cycle();
        }

        // Gives the player the shutter, with a distant click like the one in the prologue. Safe to call again.
        public static void Unlock()
        {
            if (GameSession.Instance == null || IsUnlocked) return;
            GameSession.Instance.SetFlag(UnlockFlag);
            Sfx.Play(SfxKind.Shutter, 0.25f);
            Unlocked?.Invoke();
        }

        // Levels hold switching during moments that must not be interrupted. Pair each Hold with Release.
        public static void Hold() { if (Instance != null) Instance.holds++; }
        public static void Release() { if (Instance != null) Instance.holds = Mathf.Max(0, Instance.holds - 1); }

        // The player's shutter press. False when switching is not allowed right now.
        public static bool Cycle()
        {
            if (!CanSwitch) return false;
            GameSession.Instance.SetFlag(UsedFlag);
            Instance.Apply(RevealLogic.Next(Current), true);
            return true;
        }

        // Scripted change, e.g. returning to Dream for a cutscene. Ignores the player-facing checks.
        public static void Set(RevealLayer target, bool withShutter = false)
        {
            if (Instance == null || Instance.layer == target) return;
            Instance.Apply(target, withShutter);
        }

        void Apply(RevealLayer target, bool withShutter)
        {
            layer = target;
            if (withShutter)
            {
                Sfx.Play(SfxKind.Shutter, 0.7f);
                flashStart = Time.unscaledTime;
            }
            Changed?.Invoke(layer);
        }

        void OnGUI()
        {
            float t = (Time.unscaledTime - flashStart) / FlashSeconds;
            if (t < 0 || t >= 1) return;
            GUI.depth = -90;
            GUI.matrix = Matrix4x4.identity;
            GuiKit.Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(1, 1, 1, 1 - t));
        }
    }
}
