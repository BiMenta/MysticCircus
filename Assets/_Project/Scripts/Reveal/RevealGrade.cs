using System;
using UnityEngine;

namespace MysticCircus
{
    // Full-screen grade for the Built-in pipeline. RevealSystem adds it to the player camera; it eases between looks
    // whenever the layer changes. Override forces a look regardless of layer, e.g. the white reality look.
    [RequireComponent(typeof(Camera))]
    public sealed class RevealGrade : MonoBehaviour
    {
        [Serializable]
        public struct Look
        {
            public float Exposure;
            public Color Tint;
            public float Saturation;
            // Blur mixed over the image, brightest parts first: the dreamy glow.
            public float SoftFocus;
            public float Grain;
            public float Aberration;
            public float Vignette;
            // Raises blacks toward white: the washed-out pastel of the dream.
            public float Lift;

            public static Look Lerp(Look a, Look b, float t) => new Look
            {
                Exposure = Mathf.Lerp(a.Exposure, b.Exposure, t),
                Tint = Color.Lerp(a.Tint, b.Tint, t),
                Saturation = Mathf.Lerp(a.Saturation, b.Saturation, t),
                SoftFocus = Mathf.Lerp(a.SoftFocus, b.SoftFocus, t),
                Grain = Mathf.Lerp(a.Grain, b.Grain, t),
                Aberration = Mathf.Lerp(a.Aberration, b.Aberration, t),
                Vignette = Mathf.Lerp(a.Vignette, b.Vignette, t),
                Lift = Mathf.Lerp(a.Lift, b.Lift, t),
            };
        }

        // 梦界: pastel, overexposed, soft.
        public static readonly Look Dream = new Look { Exposure = 1.35f, Tint = new Color(1.04f, 0.96f, 1.02f), Saturation = 0.82f, SoftFocus = 0.5f, Grain = 0.012f, Aberration = 0.0015f, Vignette = 0.2f, Lift = 0.14f };
        // 被拐前: warm pink, overexposed, fine grain.
        public static readonly Look Warm = new Look { Exposure = 1.4f, Tint = new Color(1.14f, 0.95f, 0.88f), Saturation = 0.95f, SoftFocus = 0.35f, Grain = 0.025f, Aberration = 0.001f, Vignette = 0.25f, Lift = 0.1f };
        // 被拐后: faded and cold, heavy noise.
        public static readonly Look Cold = new Look { Exposure = 0.95f, Tint = new Color(0.86f, 0.94f, 1.06f), Saturation = 0.42f, SoftFocus = 0.1f, Grain = 0.06f, Aberration = 0.003f, Vignette = 0.38f, Lift = 0.05f };
        // 现实层: pure white overexposure, only light and hands.
        public static readonly Look White = new Look { Exposure = 3f, Tint = Color.white, Saturation = 0.3f, SoftFocus = 0.7f, Grain = 0.015f, Aberration = 0.001f, Vignette = 0.1f, Lift = 0.45f };

        public static Look For(RevealLayer layer) => layer == RevealLayer.Warm ? Warm : layer == RevealLayer.Cold ? Cold : Dream;

        public float BlendSeconds = 0.15f;
        Look current, from, to;
        Look? forced;
        float progress = 1;
        Material material;

        const int GradePass = 0, BlurPass = 1;
        static readonly int BlurId = Shader.PropertyToID("_BlurTex");
        static readonly int ExposureId = Shader.PropertyToID("_Exposure"), TintId = Shader.PropertyToID("_Tint"),
            SaturationId = Shader.PropertyToID("_Saturation"), SoftId = Shader.PropertyToID("_Soft"), GrainId = Shader.PropertyToID("_Grain"),
            AberrationId = Shader.PropertyToID("_Aberration"), VignetteId = Shader.PropertyToID("_Vignette"), LiftId = Shader.PropertyToID("_Lift");

        public Look Current => current;

        void Awake()
        {
            var shader = Resources.Load<Shader>("RevealGrade");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("RevealGrade shader is unavailable; the screen will not be graded.");
                enabled = false;
                return;
            }
            material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            current = to = For(RevealSystem.Current);
            RevealSystem.Changed += OnLayer;
        }

        void OnDestroy()
        {
            RevealSystem.Changed -= OnLayer;
            if (material != null) Destroy(material);
        }

        void OnLayer(RevealLayer layer) => BlendTo(forced ?? For(layer));

        // Null hands control back to the layer.
        public void Override(Look? look)
        {
            forced = look;
            BlendTo(look ?? For(RevealSystem.Current));
        }

        void BlendTo(Look target)
        {
            from = current;
            to = target;
            progress = 0;
        }

        void Update()
        {
            if (progress >= 1) return;
            progress = BlendSeconds <= 0 ? 1 : Mathf.Min(1, progress + Time.unscaledDeltaTime / BlendSeconds);
            current = Look.Lerp(from, to, progress);
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (material == null) { Graphics.Blit(source, destination); return; }
            material.SetFloat(ExposureId, current.Exposure);
            material.SetColor(TintId, current.Tint);
            material.SetFloat(SaturationId, current.Saturation);
            material.SetFloat(SoftId, current.SoftFocus);
            material.SetFloat(GrainId, current.Grain);
            material.SetFloat(AberrationId, current.Aberration);
            material.SetFloat(VignetteId, current.Vignette);
            material.SetFloat(LiftId, current.Lift);
            // Down to an eighth and back to a quarter: a smooth blur for little cost. Sparse taps at full size read as ghosting.
            var half = RenderTexture.GetTemporary(source.width / 2, source.height / 2, 0, source.format);
            var quarter = RenderTexture.GetTemporary(source.width / 4, source.height / 4, 0, source.format);
            var eighth = RenderTexture.GetTemporary(source.width / 8, source.height / 8, 0, source.format);
            Graphics.Blit(source, half, material, BlurPass);
            Graphics.Blit(half, quarter, material, BlurPass);
            Graphics.Blit(quarter, eighth, material, BlurPass);
            Graphics.Blit(eighth, quarter, material, BlurPass);
            material.SetTexture(BlurId, quarter);
            // The pass must be named: without it Blit runs every pass in the shader.
            Graphics.Blit(source, destination, material, GradePass);
            RenderTexture.ReleaseTemporary(half);
            RenderTexture.ReleaseTemporary(quarter);
            RenderTexture.ReleaseTemporary(eighth);
        }
    }
}
