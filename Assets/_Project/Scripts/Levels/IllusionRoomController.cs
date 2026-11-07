using UnityEngine;

namespace MysticCircus
{
    public sealed class IllusionRoomController : RoomController
    {
        public GameObject InvertedLayout;
        public GameObject ChessLayout;
        public Transform Clown;
        bool chess;
        bool bell;
        int tileStep;
        readonly int[] tiles = { 0, 4, 8 };
        public override string Objective => IsSolved ? "The clown lied twice. Doors A and C are excluded. Choose B." :
            "Ring the inverted bell; switch the layout; activate A1 > B2 > C3. Remember which doors the clown points at.";

        public override void Interact(string action, int value)
        {
            if (action == "layout")
            {
                chess = !chess;
                InvertedLayout.SetActive(!chess);
                ChessLayout.SetActive(chess);
                Message = chess ? "Chess layout. Plaque: A1 > B2 > C3." : "Inverted layout. Find the low-hanging bell.";
            }
            if (action == "bell" && !chess && !bell)
            {
                bell = true;
                Clown.localRotation = Quaternion.Euler(0, 220, 0);
                Message = "The clown points at Door A. Remember: he lies.";
            }
            if (action == "tile" && chess && tileStep < 3)
            {
                if (value == tiles[tileStep]) tileStep++;
                else tileStep = 0;
                Message = tileStep == 3 ? "The clown points at Door C. Another lie." : "Chess sequence: " + tileStep + "/3";
                if (tileStep == 3) Clown.localRotation = Quaternion.Euler(0, 140, 0);
            }
            if (action == "wrongDoor") Message = "The clown pointed here. This is a false exit.";
            if (bell && tileStep == 3) Solve("Excluded doors: A and C. The remaining exit is B.");
        }
    }
}
