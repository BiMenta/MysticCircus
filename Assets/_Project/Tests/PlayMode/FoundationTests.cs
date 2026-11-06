using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MysticCircus.Tests
{
    public sealed class FoundationTests
    {
        sealed class HintRoom : RoomController
        {
            public string Phase = "a";
            public override void Interact(string action, int value) => Solve(action);
            public override string[] Hints => Phase == "a" ? new[] { "a1", "a2" } : new[] { "b1" };
        }

        [OneTimeSetUp]
        public void DisablePersistence() => GameSession.PersistenceEnabled = false;

        [UnitySetUp]
        public IEnumerator ResetRun()
        {
            GameSession.Instance.Data = new SaveData();
            yield return null;
        }

        [Test]
        public void Bootstrap_CreatesPersistentServices()
        {
            var session = GameSession.Instance;
            Assert.IsNotNull(session);
            Assert.IsNotNull(session.GetComponent<ScreenFader>());
            Assert.IsNotNull(session.GetComponent<MusicDirector>());
            Assert.IsNotNull(session.GetComponent<Sfx>());
        }

        [UnityTest]
        public IEnumerator Fader_LoadsSceneAndSessionRecordsCheckpoint()
        {
            ScreenFader.Load("01_MirrorRoom");
            Assert.IsTrue(ScreenFader.Busy);
            while (ScreenFader.Busy) yield return null;
            Assert.AreEqual("01_MirrorRoom", SceneManager.GetActiveScene().name);
            Assert.AreEqual("01_MirrorRoom", GameSession.Instance.Data.Scene);
            Assert.IsNotNull(Object.FindAnyObjectByType<RoomController>());
            Assert.IsNotNull(Object.FindAnyObjectByType<FirstPersonPlayer>().Hand);
        }

        // Any error or exception logged while a scene runs fails this test automatically.
        [UnityTest]
        public IEnumerator EveryBuildScene_LoadsAndRunsCleanly()
        {
            Assert.Greater(SceneManager.sceneCountInBuildSettings, 0);
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
                // The runner adds its own bootstrap scene to this list; reloading it would disturb the run.
                if (name.StartsWith("InitTestScene")) continue;
                ScreenFader.Load(name);
                while (ScreenFader.Busy) yield return null;
                Assert.AreEqual(name, SceneManager.GetActiveScene().name);
                for (int frame = 0; frame < 10; frame++) yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Collectible_SetsFlagAndStaysGoneOnReload()
        {
            var first = GameObject.CreatePrimitive(PrimitiveType.Cube).AddComponent<Interactable>();
            first.CollectibleId = "test_item";
            yield return null;
            first.Use();
            Assert.IsTrue(GameSession.Instance.HasFlag("test_item"));
            Assert.IsFalse(first.gameObject.activeSelf);

            var host = new GameObject();
            host.SetActive(false);
            var second = host.AddComponent<Interactable>();
            second.CollectibleId = "test_item";
            host.SetActive(true);
            Assert.IsFalse(host.activeSelf, "A collected item must not reappear when its room reloads.");
            Object.Destroy(first.gameObject);
            Object.Destroy(host);
        }

        [UnityTest]
        public IEnumerator Interactable_LockedExitStaysUntilSolved()
        {
            var room = new GameObject().AddComponent<HintRoom>();
            var door = GameObject.CreatePrimitive(PrimitiveType.Cube).AddComponent<Interactable>();
            door.Room = room;
            door.NextScene = "01_MirrorRoom";
            door.RequiresSolved = true;
            yield return null;
            door.Use();
            Assert.IsFalse(ScreenFader.Busy);
            room.Interact("done", 0);
            Assert.IsTrue(room.IsSolved);
            door.Use();
            Assert.IsTrue(ScreenFader.Busy);
            while (ScreenFader.Busy) yield return null;
        }

        [Test]
        public void Hints_AdvanceCountOnceAndRestartWithPhase()
        {
            var room = new GameObject().AddComponent<HintRoom>();
            Assert.AreEqual("a1", room.NextHint());
            Assert.AreEqual("a2", room.NextHint());
            Assert.AreEqual("a2", room.NextHint());
            Assert.AreEqual(2, GameSession.Instance.Data.HintsUsed);
            room.Phase = "b";
            Assert.AreEqual("b1", room.NextHint());
            Assert.AreEqual(3, GameSession.Instance.Data.HintsUsed);
            Object.Destroy(room.gameObject);
        }

        [UnityTest]
        public IEnumerator PuppetRig_BowsTowardTheModelsFront()
        {
            var root = new GameObject();
            var torso = new GameObject().transform;
            torso.SetParent(root.transform, false);
            var head = new GameObject().transform;
            head.SetParent(torso, false);
            head.localPosition = Vector3.up;
            var rig = root.AddComponent<PuppetRig>();
            rig.Torso = torso;
            rig.Speed = 1000;
            rig.Pose(bowAngle: 45);
            yield return null;
            yield return null;
            Assert.Less(head.position.z, -0.5f, "The front is -Z, so a bow must move the head toward -Z.");
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator Mirror_ShowsMirrorOnlyLayerThatTheCameraCannotSeeDirectly()
        {
            Assume.That(SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null, "Requires a graphics device.");
            foreach (var other in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) other.enabled = false;

            var viewObject = new GameObject("View");
            viewObject.transform.position = new Vector3(100, 100, 100);
            var view = viewObject.AddComponent<Camera>();
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = Color.black;
            view.cullingMask = ~(1 << CircusLayers.MirrorOnly);
            view.targetTexture = new RenderTexture(64, 64, 24);

            var glass = GameObject.CreatePrimitive(PrimitiveType.Quad);
            glass.transform.position = viewObject.transform.position + Vector3.forward * 3;
            glass.transform.localScale = Vector3.one * 8;
            var shader = Shader.Find("MysticCircus/PlanarMirror");
            Assert.IsNotNull(shader);
            glass.GetComponent<Renderer>().sharedMaterial = new Material(shader);
            glass.AddComponent<PlanarMirror>();

            var ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ghost.layer = CircusLayers.MirrorOnly;
            ghost.transform.position = viewObject.transform.position + Vector3.forward * 1.5f;
            ghost.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/Color")) { color = Color.red };
            yield return null;

            view.Render();
            Assert.Greater(CenterPixel(view).r, 0.4f, "The mirror should reflect the MirrorOnly object.");

            ghost.SetActive(false);
            view.Render();
            Assert.Less(CenterPixel(view).r, 0.1f, "Without the object the mirror should show the empty background.");

            Object.Destroy(glass);
            Object.Destroy(ghost);
            Object.Destroy(viewObject);
        }

        static Color CenterPixel(Camera camera)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = camera.targetTexture;
            var reader = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            reader.ReadPixels(new Rect(camera.targetTexture.width / 2, camera.targetTexture.height / 2, 1, 1), 0, 0);
            RenderTexture.active = previous;
            return reader.GetPixel(0, 0);
        }
    }
}
