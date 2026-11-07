using System.Collections.Generic;
using UnityEngine;

namespace MysticCircus
{
    // True planar reflection for the Built-in pipeline. Attach to a Quad using the PlanarMirror shader.
    // The mirror camera also sees the MirrorOnly layer, so a mirror can show things that are not in the room.
    [RequireComponent(typeof(Renderer))]
    public sealed class PlanarMirror : MonoBehaviour
    {
        public int MaxTextureHeight = 1024;
        // Shown in this mirror only: every other mirror hides them while it renders. Put them on the MirrorOnly
        // layer too, or the player sees them directly. Changeable at runtime, e.g. a performer moving between mirrors.
        public List<GameObject> OnlyHere = new List<GameObject>();
        static readonly List<PlanarMirror> mirrors = new List<PlanarMirror>();
        static readonly List<Renderer> hidden = new List<Renderer>();
        Camera reflectionCamera;
        RenderTexture target;
        Material material;
        static bool rendering;

        // A Quad's visible face looks along local -Z.
        Vector3 Normal => -transform.forward;

        void Awake()
        {
            var cameraObject = new GameObject("MirrorCamera") { hideFlags = HideFlags.HideAndDontSave };
            cameraObject.transform.SetParent(transform, false);
            reflectionCamera = cameraObject.AddComponent<Camera>();
            reflectionCamera.enabled = false;
            material = GetComponent<Renderer>().material;
        }

        void OnEnable() => mirrors.Add(this);
        void OnDisable() => mirrors.Remove(this);

        void OnWillRenderObject()
        {
            var source = Camera.current;
            if (rendering || source == null || source.cameraType != CameraType.Game || source == reflectionCamera) return;
            rendering = true;
            try { Render(source); }
            finally { rendering = false; }
        }

        void Render(Camera source)
        {
            EnsureTarget(source);
            Vector3 point = transform.position;
            Vector3 normal = Normal;
            var plane = new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(normal, point));

            reflectionCamera.CopyFrom(source);
            reflectionCamera.cullingMask = source.cullingMask | (1 << CircusLayers.MirrorOnly);
            reflectionCamera.targetTexture = target;
            reflectionCamera.worldToCameraMatrix = source.worldToCameraMatrix * ReflectionMatrix(plane);
            // Oblique near plane so nothing behind the glass leaks into the reflection.
            reflectionCamera.projectionMatrix = source.CalculateObliqueMatrix(CameraSpacePlane(reflectionCamera, point, normal));
            reflectionCamera.transform.position = source.transform.position - 2 * Vector3.Dot(normal, source.transform.position - point) * normal;

            HideOtherMirrorsExclusives();
            bool culling = GL.invertCulling;
            GL.invertCulling = !culling;
            try { reflectionCamera.Render(); }
            finally
            {
                GL.invertCulling = culling;
                foreach (var part in hidden) if (part != null) part.forceRenderingOff = false;
                hidden.Clear();
            }
            material.SetTexture("_ReflectionTex", target);
        }

        void HideOtherMirrorsExclusives()
        {
            foreach (var mirror in mirrors)
            {
                if (mirror == this) continue;
                foreach (var owner in mirror.OnlyHere)
                {
                    if (owner == null || OnlyHere.Contains(owner)) continue;
                    foreach (var part in owner.GetComponentsInChildren<Renderer>())
                        if (!part.forceRenderingOff) { part.forceRenderingOff = true; hidden.Add(part); }
                }
            }
        }

        void EnsureTarget(Camera source)
        {
            int height = Mathf.Min(source.pixelHeight, MaxTextureHeight);
            int width = Mathf.Max(1, Mathf.RoundToInt(height * source.aspect));
            if (target != null && target.width == width && target.height == height) return;
            if (target != null) { target.Release(); Destroy(target); }
            target = new RenderTexture(width, height, 24) { name = "MirrorTarget", hideFlags = HideFlags.DontSave, antiAliasing = 2 };
        }

        static Vector4 CameraSpacePlane(Camera camera, Vector3 point, Vector3 normal)
        {
            const float clipOffset = 0.02f;
            Matrix4x4 view = camera.worldToCameraMatrix;
            Vector3 cameraPoint = view.MultiplyPoint(point + normal * clipOffset);
            Vector3 cameraNormal = view.MultiplyVector(normal).normalized;
            return new Vector4(cameraNormal.x, cameraNormal.y, cameraNormal.z, -Vector3.Dot(cameraPoint, cameraNormal));
        }

        static Matrix4x4 ReflectionMatrix(Vector4 p)
        {
            var m = Matrix4x4.identity;
            m.m00 = 1 - 2 * p.x * p.x; m.m01 = -2 * p.x * p.y; m.m02 = -2 * p.x * p.z; m.m03 = -2 * p.x * p.w;
            m.m10 = -2 * p.y * p.x; m.m11 = 1 - 2 * p.y * p.y; m.m12 = -2 * p.y * p.z; m.m13 = -2 * p.y * p.w;
            m.m20 = -2 * p.z * p.x; m.m21 = -2 * p.z * p.y; m.m22 = 1 - 2 * p.z * p.z; m.m23 = -2 * p.z * p.w;
            return m;
        }

        void OnDestroy()
        {
            if (target != null) { target.Release(); Destroy(target); }
            if (material != null) Destroy(material);
            if (reflectionCamera != null) Destroy(reflectionCamera.gameObject);
        }
    }
}
