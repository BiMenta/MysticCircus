using UnityEngine;

namespace MysticCircus
{
    // A memory fragment lying in the world. Using it adds it to the album and hides it for good.
    // Pair with RevealLayerObject to make it exist only in its layer.
    [RequireComponent(typeof(Interactable))]
    public sealed class FragmentPickup : MonoBehaviour
    {
        public string FragmentId;

        public bool Collected => Album.Has(FragmentId);

        void Awake()
        {
            if (Collected) { gameObject.SetActive(false); return; }
            GetComponent<Interactable>().Used.AddListener(Take);
        }

        public void Take()
        {
            if (Collected) return;
            Album.Collect(FragmentId, transform.position);
            gameObject.SetActive(false);
        }
    }
}
