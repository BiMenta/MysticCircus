using UnityEngine;

namespace MysticCircus
{
    // Applies the wardrobe choices saved in GameSession to any body.
    public sealed class ActorLook : MonoBehaviour
    {
        public Renderer[] CostumeRenderers;
        public GameObject Hat;
        public GameObject Mask;
        public Material[] Costumes;

        void Start() => Apply();

        public void Apply()
        {
            var session = GameSession.Instance;
            if (session == null) return;
            if (Costumes != null && Costumes.Length > 0 && CostumeRenderers != null)
                foreach (var part in CostumeRenderers) part.sharedMaterial = Costumes[Mathf.Abs(session.Costume) % Costumes.Length];
            if (Hat != null) Hat.SetActive(session.Hat == 1);
            if (Mask != null) Mask.SetActive(session.Mask == 1);
        }
    }
}
