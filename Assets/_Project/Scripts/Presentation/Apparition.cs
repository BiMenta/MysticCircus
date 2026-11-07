using UnityEngine;

namespace MysticCircus
{
    // Something glimpsed, never met: every MinGap–MaxGap seconds Figure sweeps from From to To and vanishes again.
    // For the diamond-patterned sleeve passing a doorway (F4). Put a RevealLayerObject on this object to
    // limit it to a layer; deactivating this object stops it.
    public sealed class Apparition : MonoBehaviour
    {
        public Transform Figure;
        [Tooltip("Local positions of Figure at the start and end of a sweep.")]
        public Vector3 From, To;
        public float SweepSeconds = 0.6f;
        public float MinGap = 8, MaxGap = 12;
        [Tooltip("Optional sound at each sweep. Leave the bell silent on purpose.")]
        public AudioSource Sound;

        float nextAt;
        float sweepStart = -1;

        void OnEnable()
        {
            sweepStart = -1;
            nextAt = Time.time + Random.Range(MinGap * 0.3f, MinGap);
            if (Figure != null) Figure.gameObject.SetActive(false);
        }

        void Update()
        {
            if (Figure == null) return;
            if (sweepStart < 0)
            {
                if (Time.time < nextAt) return;
                sweepStart = Time.time;
                Figure.gameObject.SetActive(true);
                if (Sound != null) Sound.Play();
            }
            float t = (Time.time - sweepStart) / Mathf.Max(0.05f, SweepSeconds);
            Figure.localPosition = Vector3.Lerp(From, To, Mathf.SmoothStep(0, 1, t));
            if (t < 1) return;
            Figure.gameObject.SetActive(false);
            sweepStart = -1;
            nextAt = Time.time + Random.Range(MinGap, MaxGap);
        }
    }
}
