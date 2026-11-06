using UnityEngine;

namespace MysticCircus
{
    public sealed class WardrobeController : RoomController
    {
        public Renderer CostumePreview;
        public GameObject HatPreview;
        public GameObject MaskPreview;
        public Material[] Costumes;

        void Start() { IsSolved = true; Refresh(); }
        public override string Objective => Brief;

        public override void Interact(string action, int value)
        {
            var session = GameSession.Instance;
            if (action == "costume") session.Costume = (session.Costume + 1) % Costumes.Length;
            if (action == "hat") session.Hat = 1 - session.Hat;
            if (action == "mask") session.Mask = 1 - session.Mask;
            Refresh();
            Message = "Your look is ready. Step through the gold door when you are ready.";
        }

        void Refresh()
        {
            var session = GameSession.Instance;
            CostumePreview.sharedMaterial = Costumes[session.Costume % Costumes.Length];
            HatPreview.SetActive(session.Hat == 1);
            MaskPreview.SetActive(session.Mask == 1);
        }
    }
}
