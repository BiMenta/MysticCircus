using UnityEngine;

namespace MysticCircus
{
    // Shows this object only in the ticked layers. Hiding deactivates it, so its colliders and interactions go too.
    // Leave the object active in the scene: an object that starts inactive never registers.
    public sealed class RevealLayerObject : MonoBehaviour
    {
        public bool InDream = true;
        public bool InWarm;
        public bool InCold;

        public bool VisibleIn(RevealLayer layer) =>
            layer == RevealLayer.Dream ? InDream : layer == RevealLayer.Warm ? InWarm : InCold;

        void Awake()
        {
            RevealSystem.Changed += Apply;
            Apply(RevealSystem.Current);
        }

        // Again in Start, for objects built in code whose layer ticks are set after AddComponent.
        void Start() => Apply(RevealSystem.Current);

        void OnDestroy() => RevealSystem.Changed -= Apply;

        void Apply(RevealLayer layer)
        {
            if (this == null) return;
            gameObject.SetActive(VisibleIn(layer) && !Taken());
        }

        // A collected pickup must stay gone in every layer, including after it reloads.
        bool Taken()
        {
            var session = GameSession.Instance;
            if (session == null) return false;
            var pickup = GetComponent<FragmentPickup>();
            if (pickup != null && pickup.Collected) return true;
            var interactable = GetComponent<Interactable>();
            return interactable != null && !string.IsNullOrEmpty(interactable.CollectibleId) && session.HasFlag(interactable.CollectibleId);
        }
    }
}
