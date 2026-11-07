using System.Collections;
using UnityEngine;

namespace MysticCircus
{
    // 「不对的细节」: using this pushes the camera in slowly, then files what was seen on the album's observation page.
    // Works alongside a room action on the same Interactable (for example a toy that is also picked up).
    [RequireComponent(typeof(Interactable))]
    public sealed class Inspectable : MonoBehaviour
    {
        [Tooltip("Observation id, unique across the game, e.g. u1_frame_scratch.")]
        public string Id;
        public float Seconds = 1.5f;
        public float ZoomFieldOfView = 38;

        bool busy;
        FirstPersonPlayer viewer;
        float wide;

        void Awake() => GetComponent<Interactable>().Used.AddListener(Inspect);

        public void Inspect()
        {
            if (busy || string.IsNullOrEmpty(Id)) return;
            var player = FindAnyObjectByType<FirstPersonPlayer>();
            if (player == null || player.View == null) { Album.Observe(Id); return; }
            StartCoroutine(CloseUp(player));
        }

        IEnumerator CloseUp(FirstPersonPlayer player)
        {
            busy = true;
            viewer = player;
            player.Frozen = true;
            RevealSystem.Hold();
            var view = player.View;
            wide = view.fieldOfView;
            float pushIn = Seconds * 0.7f;
            for (float t = 0; t < pushIn; t += Time.deltaTime)
            {
                view.fieldOfView = Mathf.Lerp(wide, ZoomFieldOfView, Mathf.SmoothStep(0, 1, t / pushIn));
                yield return null;
            }
            view.fieldOfView = ZoomFieldOfView;
            var picture = Album.HasObserved(Id) && Album.Thumbnail(Id) != null ? null : Album.Capture(view);
            yield return new WaitForSeconds(Seconds * 0.3f);
            for (float t = 0; t < 0.4f; t += Time.deltaTime)
            {
                view.fieldOfView = Mathf.Lerp(ZoomFieldOfView, wide, Mathf.SmoothStep(0, 1, t / 0.4f));
                yield return null;
            }
            Finish();
            Album.Observe(Id, picture, transform.position);
        }

        void Finish()
        {
            if (!busy) return;
            busy = false;
            RevealSystem.Release();
            if (viewer == null) return;
            viewer.View.fieldOfView = wide;
            viewer.Frozen = false;
        }

        // If the object is hidden or unloaded mid close-up, the player must not stay frozen.
        void OnDisable() => Finish();
    }
}
