using System;
using UnityEngine;

namespace MysticCircus
{
    // Base for every room's rules. Derive, implement Interact, and call Solve when the room is finished.
    public abstract class RoomController : MonoBehaviour
    {
        public string Chapter;
        [TextArea] public string Brief;
        // Optional light that switches on when the room is solved, marking the exit.
        public Light ExitLamp;
        public bool IsSolved { get; protected set; }
        public string Message { get; protected set; }
        public event Action Solved;
        public abstract void Interact(string action, int value);
        public virtual string Objective => IsSolved ? "The exit is open. Follow the gold door." : Brief;
        // Hints for the current phase, mildest first. Returning a different list starts the count again.
        public virtual string[] Hints => Array.Empty<string>();

        string hintPhase;
        int hintsShown;

        // Next hint for the current phase, or null if there are none. Repeats the last one once exhausted.
        public string NextHint()
        {
            string[] hints = Hints;
            if (hints.Length == 0) return null;
            if (hintPhase != hints[0]) { hintPhase = hints[0]; hintsShown = 0; }
            if (hintsShown < hints.Length)
            {
                hintsShown++;
                if (GameSession.Instance != null) GameSession.Instance.Data.HintsUsed++;
            }
            return hints[hintsShown - 1];
        }

        protected void Say(string zh, string en) => Message = L.T(zh, en);

        protected void Solve(string message)
        {
            if (IsSolved) return;
            IsSolved = true;
            Message = message;
            if (ExitLamp != null) ExitLamp.enabled = true;
            Sfx.Play(SfxKind.Fanfare);
            Solved?.Invoke();
        }

        protected void Solve(string zh, string en) => Solve(L.T(zh, en));
    }
}
