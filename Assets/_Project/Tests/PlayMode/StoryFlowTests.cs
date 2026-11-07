using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MysticCircus.Tests
{
    public sealed class StoryFlowTests
    {
        // TEST_FILM is a fixture, so replacing the game's real films cannot break these tests; its color is known.
        // nothing.mp4 stands for a slot with no file yet.
        const string Story = @"{
            ""start"": ""real"",
            ""nodes"": [
                { ""id"": ""real"", ""type"": ""film"", ""video"": ""TEST_FILM"", ""next"": ""mirror"" },
                { ""id"": ""mirror"", ""type"": ""level"", ""scene"": ""01_MirrorRoom"", ""next"": ""empty_slot"" },
                { ""id"": ""empty_slot"", ""type"": ""film"", ""video"": ""nothing.mp4"", ""next"": ""illusion"" },
                { ""id"": ""illusion"", ""type"": ""level"", ""scene"": ""02_IllusionHall"", ""next"": ""fork"" },
                { ""id"": ""fork"", ""type"": ""film"", ""video"": ""nothing.mp4"", ""choiceSeconds"": 0.2, ""defaultChoice"": 1,
                  ""choices"": [
                    { ""next"": ""mirror"", ""setFlag"": ""went_mirror"" },
                    { ""next"": ""routed"", ""setFlag"": ""went_routed"" } ] },
                { ""id"": ""routed"", ""type"": ""level"", ""scene"": ""03_PuppetStage"", ""next"": ""mirror"",
                  ""routes"": [ { ""ifFlag"": ""went_routed"", ""next"": ""illusion"" } ] },
                { ""id"": ""last"", ""type"": ""level"", ""scene"": ""04_ToyRoom"" }
            ]
        }";

        [OneTimeSetUp]
        public void DisablePersistence() => GameSession.PersistenceEnabled = false;

        [UnitySetUp]
        public IEnumerator UseTestStory()
        {
            while (ScreenFader.Busy) yield return null;
            FilmPlayer.Stop();
            GameSession.Instance.Data = new SaveData();
            StoryDirector.UseGraph(StoryGraph.FromJson(Story.Replace("TEST_FILM", Application.dataPath + "/_Project/Tests/PlayMode/Fixtures/test_film.mp4")));
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            while (ScreenFader.Busy) yield return null;
            FilmPlayer.Stop();
            StoryDirector.UseGraph(null);
        }

        static IEnumerator Settle()
        {
            yield return null;
            while (ScreenFader.Busy) yield return null;
        }

        static string ActiveScene => SceneManager.GetActiveScene().name;

        [UnityTest]
        public IEnumerator Film_PlaysThenSkipLeadsToNextLevel()
        {
            Assume.That(SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null, "Requires a graphics device.");
            Assert.IsTrue(StoryDirector.Go("real"));
            yield return Settle();
            Assert.IsTrue(FilmPlayer.Active);
            Assert.IsTrue(FilmPlayer.VideoPlaying, "The fixture mp4 should decode and play.");
            Assert.AreEqual("real", GameSession.Instance.Data.Node);
            Assert.IsNull(Object.FindAnyObjectByType<RoomController>(), "The previous level must be unloaded during a film.");
            FilmPlayer.Skip();
            yield return null;
            yield return Settle();
            Assert.IsFalse(FilmPlayer.Active);
            Assert.AreEqual("01_MirrorRoom", ActiveScene);
            Assert.AreEqual("mirror", StoryDirector.Current.id);
        }

        // A level-free frame sampled at a true 16:9. Guards against drawing the film black, washed out or letterboxed.
        [UnityTest]
        public IEnumerator Film_DrawsTrueColorsEdgeToEdge()
        {
            Assume.That(SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null, "Requires a graphics device.");
            StoryDirector.Go("real");
            yield return Settle();
            // FilmPlayer also keeps a second player for in-level clips, whose camera is off during a film.
            Camera camera = null;
            foreach (var player in Object.FindObjectsByType<UnityEngine.Video.VideoPlayer>(FindObjectsSortMode.None))
                if (player.targetCamera != null && player.targetCamera.enabled) camera = player.targetCamera;
            Assert.IsNotNull(camera, "A film should have an active camera.");
            Assert.AreEqual("FilmStage", camera.name);
            var target = new RenderTexture(1280, 720, 24);
            camera.targetTexture = target;
            yield return new WaitForSecondsRealtime(0.5f);

            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var frame = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            frame.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            RenderTexture.active = previous;
            camera.targetTexture = null;
            target.Release();

            var expected = new Color32(40, 85, 93, 255);
            foreach (var point in new[] { new Vector2Int(5, 360), new Vector2Int(1275, 360), new Vector2Int(640, 5), new Vector2Int(640, 715) })
            {
                Color32 actual = frame.GetPixel(point.x, point.y);
                Assert.Less(Mathf.Abs(actual.r - expected.r) + Mathf.Abs(actual.g - expected.g) + Mathf.Abs(actual.b - expected.b), 15,
                    "Pixel " + point + " was " + actual);
            }
        }

        [UnityTest]
        public IEnumerator DoorExit_FollowsStoryAndEmptyFilmSlotIsSkipped()
        {
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no video file"));
            StoryDirector.Go("mirror");
            yield return Settle();
            var door = new GameObject().AddComponent<Interactable>();
            door.NextScene = "05_ExteriorFinale";
            door.Use();
            yield return Settle();
            Assert.AreEqual("02_IllusionHall", ActiveScene, "The story outranks the door's own NextScene.");
            Assert.AreEqual("illusion", StoryDirector.Current.id);
        }

        [UnityTest]
        public IEnumerator Choice_ClickRecordsFlagAndFollowsBranch()
        {
            StoryDirector.Go("fork");
            yield return Settle();
            yield return null;
            Assert.IsTrue(FilmPlayer.Choosing, "A film with no video still offers its choices.");
            FilmPlayer.Choose(0);
            yield return null;
            yield return Settle();
            Assert.IsTrue(GameSession.Instance.HasFlag("went_mirror"));
            Assert.IsFalse(GameSession.Instance.HasFlag("went_routed"));
            Assert.AreEqual("01_MirrorRoom", ActiveScene);
        }

        [UnityTest]
        public IEnumerator Choice_TimeoutTakesDefaultAndFlagRoutesLater()
        {
            StoryDirector.Go("fork");
            yield return Settle();
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Settle();
            Assert.IsTrue(GameSession.Instance.HasFlag("went_routed"));
            Assert.AreEqual("routed", StoryDirector.Current.id);
            Assert.IsTrue(StoryDirector.Advance());
            yield return Settle();
            Assert.AreEqual("illusion", StoryDirector.Current.id, "The route should beat the plain next.");
        }

        [UnityTest]
        public IEnumerator LevelOpenedDirectly_JoinsTheStory()
        {
            ScreenFader.Load("03_PuppetStage");
            yield return Settle();
            Assert.AreEqual("routed", StoryDirector.Current.id);
            Assert.AreEqual("routed", GameSession.Instance.Data.Node);
        }

        [UnityTest]
        public IEnumerator LastLevel_HasNothingToAdvanceTo()
        {
            StoryDirector.Go("last");
            yield return Settle();
            Assert.IsFalse(StoryDirector.Advance());
            Assert.AreEqual("04_ToyRoom", ActiveScene);
        }

        [UnityTest]
        public IEnumerator BootScene_StartsTheStory()
        {
            ScreenFader.Load(StoryDirector.BootScene);
            // Boot starts the story when it loads, while its own transition is still running; that request must not be lost.
            yield return Settle();
            yield return Settle();
            Assert.AreEqual("real", StoryDirector.Current != null ? StoryDirector.Current.id : null);
        }

        [Test]
        public void CanResume_OnlyAcceptsNodesInTheStory()
        {
            Assert.IsFalse(StoryDirector.CanResume("not_a_node"));
            Assert.IsFalse(StoryDirector.CanResume(null));
            Assert.IsTrue(StoryDirector.CanResume("illusion"));
        }
    }
}
