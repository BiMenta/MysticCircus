using UnityEngine;

namespace MysticCircus
{
    public sealed class MirrorRoomController : RoomController
    {
        public Transform Mime;
        int gestureStep;
        int toyStep;
        readonly int[] opposite = { 1, 3 };
        readonly int[] toys = { 0, 1, 2 };
        public override string Objective => IsSolved ? base.Objective : gestureStep < 2
            ? "The mime " + (gestureStep == 0 ? "leans LEFT" : "raises his arms") + ". Choose the OPPOSITE action."
            : "Painting: RABBIT > STAR > MOON. Place the three toys in this order.";

        public override void Interact(string action, int value)
        {
            if (IsSolved) return;
            if (action == "gesture" && gestureStep < 2)
            {
                if (value == opposite[gestureStep])
                {
                    gestureStep++;
                    Mime.localRotation = Quaternion.Euler(0, 180, gestureStep == 1 ? 12 : 0);
                    Message = gestureStep == 2 ? "The mime steps aside. Read the painting." : "Correct. Watch his next gesture.";
                }
                else Message = "An opposite gesture, not the same one. Try again.";
            }
            if (action != "toy") return;
            if (gestureStep < 2) { Message = "The mime wants your attention first."; return; }
            if (value == toys[toyStep]) { toyStep++; Message = "Toy placed: " + toyStep + "/3"; }
            else { toyStep = 0; Message = "The order is wrong. Start again with the rabbit."; }
            if (toyStep == toys.Length) Solve("A brass key appears. The exit is unlocked.");
        }
    }
}
