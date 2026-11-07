using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MysticCircus.Tests
{
    // The shared narrative systems running in the engine: reveal layers, memory pickups and pairs, film overlays,
    // the timing prompt, mirror-exclusive objects and the delayed reflection.
    public sealed class NarrativeFlowTests
    {
        sealed class CoroutineHost : MonoBehaviour { }

        [OneTimeSetUp]
        public void DisablePersistence() => GameSession.PersistenceEnabled = false;

        [UnitySetUp]
        public IEnumerator Reset()
        {
            while (ScreenFader.Busy) yield return null;
            FilmPlayer.Stop();
            if (Album.IsOpen) Album.Close();
            GameSession.Instance.Data = new SaveData();
            MetaProgress.Reset();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            while (ScreenFader.Busy) yield return null;
            FilmPlayer.Stop();
            StoryDirector.UseGraph(null);
            InputGate.Clear();
        }

        static RevealSystem NewRevealSystem() => new GameObject("Reveal").AddComponent<RevealSystem>();

        [UnityTest]
        public IEnumerator Reveal_SwitchesLayersOnlyOnceUnlocked()
        {
            var reveal = NewRevealSystem();
            var warmOnly = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var layer = warmOnly.AddComponent<RevealLayerObject>();
            layer.InDream = false;
            layer.InWarm = true;
            yield return null;
            Assert.IsFalse(warmOnly.activeSelf, "A warm-only object is hidden in the dream.");

            Assert.IsFalse(RevealSystem.Cycle(), "No switching before the shutter is given.");
            RevealSystem.Unlock();
            Assert.IsTrue(RevealSystem.Cycle());
            Assert.AreEqual(RevealLayer.Warm, RevealSystem.Current);
            Assert.IsTrue(warmOnly.activeSelf);
            Assert.IsTrue(GameSession.Instance.HasFlag(RevealSystem.UsedFlag));

            RevealSystem.Hold();
            Assert.IsFalse(RevealSystem.Cycle(), "A level can pause switching.");
            RevealSystem.Release();
            Assert.IsTrue(RevealSystem.Cycle());
            Assert.AreEqual(RevealLayer.Cold, RevealSystem.Current);
            Assert.IsFalse(warmOnly.activeSelf);
            Assert.IsTrue(RevealSystem.Cycle());
            Assert.AreEqual(RevealLayer.Dream, RevealSystem.Current);

            Object.Destroy(warmOnly);
            Object.Destroy(reveal.gameObject);
            yield return null;
            Assert.AreEqual(RevealLayer.Dream, RevealSystem.Current);
        }

        [UnityTest]
        public IEnumerator FragmentPickup_StaysGoneInEveryLayer()
        {
            var reveal = NewRevealSystem();
            RevealSystem.Unlock();
            var ticket = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var layer = ticket.AddComponent<RevealLayerObject>();
            layer.InDream = layer.InWarm = layer.InCold = true;
            ticket.AddComponent<Interactable>();
            ticket.AddComponent<FragmentPickup>().FragmentId = "u1_ticket";
            yield return null;

            ticket.GetComponent<Interactable>().Use();
            Assert.IsTrue(Album.Has("u1_ticket"));
            Assert.IsFalse(ticket.activeSelf);
            RevealSystem.Cycle();
            Assert.IsFalse(ticket.activeSelf, "Changing layer must not bring a collected fragment back.");

            Object.Destroy(ticket);
            Object.Destroy(reveal.gameObject);
        }

        [UnityTest]
        public IEnumerator Pair_RecordsComboAndTrustOnce()
        {
            PairRecipe combined = null;
            System.Action<PairRecipe> listen = recipe => combined = recipe;
            Album.Combined += listen;
            try
            {
                Assert.IsTrue(Album.Collect("u1_ticket"));
                Assert.IsFalse(Album.Collect("u1_ticket"), "A fragment is collected once.");
                Assert.IsFalse(Album.TryCombine("u1_ticket", "u1_scratch"), "Both halves must be held.");
                Assert.IsTrue(Album.Collect("u1_scratch"));
                Assert.AreEqual(2, Album.Fragments.Count);

                Assert.IsTrue(Album.TryCombine("u1_scratch", "u1_ticket"));
                Assert.IsTrue(GameSession.Instance.HasFlag("combo.u1"));
                Assert.AreEqual(1, Trust.Value);
                Assert.AreEqual(1, Trust.Combos);
                Assert.IsFalse(Album.TryCombine("u1_ticket", "u1_scratch"), "A pair completes once.");
                Assert.AreEqual(1, Trust.Value);

                // The pair's film may or may not exist yet; either way the event follows it.
                if (FilmPlayer.ClipPlaying) FilmPlayer.Stop();
                for (int i = 0; i < 5 && combined == null; i++) yield return null;
                Assert.AreEqual("u1", combined != null ? combined.Id : null);
                Assert.IsFalse(InputGate.Blocked, "Input must be free again after the pair's film.");
                Assert.AreEqual(RuleMarkKind.NailScratch, RuleSheet.Marks(GameSession.Instance.HasFlag)[0].Kind);
            }
            finally { Album.Combined -= listen; }
        }

        [Test]
        public void Observe_RecordsOnce()
        {
            Assert.IsTrue(Album.Observe("test_detail"));
            Assert.IsFalse(Album.Observe("test_detail"));
            CollectionAssert.AreEqual(new[] { "test_detail" }, Album.Observations);
        }

        const string OverlayStory = @"{
            ""start"": ""title"",
            ""nodes"": [
                { ""id"": ""title"", ""type"": ""film"", ""video"": ""nothing.mp4"", ""tailSeconds"": 0.4, ""next"": ""mirror"",
                  ""overlays"": [ { ""at"": 0, ""until"": 0.4, ""kind"": ""typewriter"", ""zh"": ""你醒了。"", ""en"": ""You're awake."" } ] },
                { ""id"": ""locked"", ""type"": ""film"", ""video"": ""nothing.mp4"", ""tailSeconds"": 30, ""unskippable"": true, ""skippableOnceSeen"": true, ""next"": ""mirror"",
                  ""overlays"": [ { ""at"": 0, ""until"": 30, ""kind"": ""caption"", ""zh"": ""等"", ""en"": ""Wait"" } ] },
                { ""id"": ""mirror"", ""type"": ""level"", ""scene"": ""01_MirrorRoom"" }
            ]
        }";

        static IEnumerator Settle()
        {
            yield return null;
            while (ScreenFader.Busy) yield return null;
        }

        [UnityTest]
        public IEnumerator OverlayFilm_PlaysOverBlackWithoutAVideoAndIsRemembered()
        {
            StoryDirector.UseGraph(StoryGraph.FromJson(OverlayStory));
            Assert.IsTrue(StoryDirector.Go("title"));
            yield return Settle();
            Assert.IsTrue(FilmPlayer.Active, "A film with overlays is not skipped just because its video is missing.");
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Settle();
            Assert.AreEqual("01_MirrorRoom", SceneManager.GetActiveScene().name);
            Assert.IsTrue(MetaProgress.HasSeen("title"));
        }

        [UnityTest]
        public IEnumerator SkippableOnceSeen_LocksOnlyTheFirstViewing()
        {
            StoryDirector.UseGraph(StoryGraph.FromJson(OverlayStory));
            StoryDirector.Go("locked");
            yield return Settle();
            FilmPlayer.Skip();
            yield return Settle();
            Assert.IsTrue(FilmPlayer.Active, "The first viewing cannot be skipped.");
            MetaProgress.MarkSeen("locked");
            FilmPlayer.Skip();
            yield return null;
            yield return Settle();
            Assert.IsFalse(FilmPlayer.Active);
            Assert.AreEqual("01_MirrorRoom", SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator TimingQte_ReportsAMissWhenNothingIsPressed()
        {
            bool? result = null;
            Assert.IsTrue(TimingQte.Begin(0.2f, 0.05f, ok => result = ok));
            Assert.IsTrue(InputGate.Blocked);
            Assert.IsFalse(TimingQte.Begin(1, 1, _ => { }), "Only one prompt at a time.");
            yield return new WaitForSecondsRealtime(1f);
            Assert.AreEqual(false, result);
            Assert.IsFalse(TimingQte.Running);
            Assert.IsFalse(InputGate.Blocked);
        }

        [UnityTest]
        public IEnumerator DelayedMirrorBody_TrailsThePlayer()
        {
            var host = new GameObject("Player");
            host.SetActive(false);
            host.AddComponent<CharacterController>();
            var player = host.AddComponent<FirstPersonPlayer>();
            player.View = new GameObject("View").AddComponent<Camera>();
            player.View.transform.SetParent(host.transform, false);
            host.SetActive(true);
            var body = new GameObject("Reflection").AddComponent<DelayedMirrorBody>();
            body.Player = player;
            body.Delay = 0.3f;
            yield return new WaitForSeconds(0.4f);

            player.Warp(new Vector3(5, 0, 0), Quaternion.identity);
            yield return new WaitForSeconds(0.1f);
            Assert.Less(body.transform.position.x, 1, "The reflection has not caught up yet.");
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(5, body.transform.position.x, 0.01f);

            Object.Destroy(body.gameObject);
            Object.Destroy(host);
        }

        [UnityTest]
        public IEnumerator Mirror_OnlyHereHidesAnObjectFromOtherMirrors()
        {
            Assume.That(SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null, "Requires a graphics device.");
            foreach (var other in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) other.enabled = false;
            var origin = new Vector3(200, 100, 100);
            var view = new GameObject("View").AddComponent<Camera>();
            view.transform.position = origin;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = Color.black;
            view.cullingMask = ~(1 << CircusLayers.MirrorOnly);
            view.targetTexture = new RenderTexture(64, 64, 24);

            var shader = Shader.Find("MysticCircus/PlanarMirror");
            PlanarMirror NewMirror(Vector3 position)
            {
                var glass = GameObject.CreatePrimitive(PrimitiveType.Quad);
                glass.transform.position = position;
                glass.transform.localScale = Vector3.one * 8;
                glass.GetComponent<Renderer>().sharedMaterial = new Material(shader);
                return glass.AddComponent<PlanarMirror>();
            }
            var watched = NewMirror(origin + Vector3.forward * 3);
            var elsewhere = NewMirror(origin + Vector3.right * 40);

            var ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ghost.layer = CircusLayers.MirrorOnly;
            ghost.transform.position = origin + Vector3.forward * 1.5f;
            ghost.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/Color")) { color = Color.red };
            elsewhere.OnlyHere.Add(ghost);
            yield return null;

            view.Render();
            Assert.Less(CenterRed(view), 0.1f, "The ghost belongs to the other mirror.");
            Assert.IsFalse(ghost.GetComponent<Renderer>().forceRenderingOff, "Hiding is undone after each reflection.");

            elsewhere.OnlyHere.Clear();
            watched.OnlyHere.Add(ghost);
            view.Render();
            Assert.Greater(CenterRed(view), 0.4f, "Moved to the watched mirror, the ghost shows.");

            Object.Destroy(watched.gameObject);
            Object.Destroy(elsewhere.gameObject);
            Object.Destroy(ghost);
            Object.Destroy(view.gameObject);
        }

        [UnityTest]
        public IEnumerator RevealGrade_ChangesTheRenderedPicturePerLayer()
        {
            Assume.That(SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null, "Requires a graphics device.");
            var reveal = NewRevealSystem();
            var cameraObject = new GameObject("GradedCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.5f, 0.5f, 0.5f);
            camera.cullingMask = 0;
            camera.targetTexture = new RenderTexture(64, 64, 24);
            var grade = cameraObject.AddComponent<RevealGrade>();
            Assume.That(grade.enabled, "The grade shader must be available.");
            grade.BlendSeconds = 0;
            yield return null;

            Color Sample()
            {
                camera.Render();
                var previous = RenderTexture.active;
                RenderTexture.active = camera.targetTexture;
                var reader = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                reader.ReadPixels(new Rect(32, 32, 1, 1), 0, 0);
                RenderTexture.active = previous;
                return reader.GetPixel(0, 0);
            }

            var dream = Sample();
            RevealSystem.Set(RevealLayer.Cold);
            yield return null;
            var cold = Sample();
            RevealSystem.Set(RevealLayer.Warm);
            yield return null;
            var warm = Sample();

            // Read back in linear light: mid grey, untouched, would total about 0.64.
            Assert.Greater(dream.r + dream.g + dream.b, 0.85f, "The dream look is lifted and overexposed.");
            Assert.Greater(cold.b, cold.r, "The cold layer leans blue.");
            Assert.Greater(warm.r, warm.b, "The warm layer leans pink.");
            Assert.Greater(Mathf.Abs(warm.r - cold.r) + Mathf.Abs(warm.b - cold.b), 0.1f, "The layers must look different.");

            camera.targetTexture.Release();
            Object.Destroy(cameraObject);
            Object.Destroy(reveal.gameObject);
        }

        [UnityTest]
        public IEnumerator InputGate_StaysBlockedForTheFrameOfARelease()
        {
            InputGate.Hold();
            InputGate.Release();
            Assert.IsTrue(InputGate.Blocked, "The press that closed a window must not reach the player.");
            yield return null;
            Assert.IsFalse(InputGate.Blocked);
        }

        [UnityTest]
        public IEnumerator RuleSheet_OpensAndClosesWithoutLeavingInputHeld()
        {
            RuleSheetView.Open();
            Assert.IsTrue(RuleSheetView.IsOpen);
            Assert.IsTrue(InputGate.Blocked);
            RuleSheetView.Close();
            yield return null;
            Assert.IsFalse(RuleSheetView.IsOpen);
            Assert.IsFalse(InputGate.Blocked);
        }

        [UnityTest]
        public IEnumerator Reveal_SceneUnloadInAnotherLayerRunsCleanly()
        {
            NewRevealSystem();
            RevealSystem.Unlock();
            var dreamOnly = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dreamOnly.AddComponent<RevealLayerObject>();
            var audioHost = new GameObject("Accordion");
            audioHost.AddComponent<AudioSource>();
            audioHost.AddComponent<RevealAudio>();
            yield return null;
            Assert.IsTrue(RevealSystem.Cycle());
            Assert.IsFalse(dreamOnly.activeSelf);
            yield return null;

            // Any error logged while these unload fails the test.
            ScreenFader.Load("01_MirrorRoom");
            yield return Settle();
            Assert.AreEqual(RevealLayer.Dream, RevealSystem.Current);
            Assert.IsNull(RevealSystem.Instance);
        }

        static string FixtureFilm => Application.dataPath + "/_Project/Tests/PlayMode/Fixtures/test_film.mp4";

        [UnityTest]
        public IEnumerator PlayClip_HoldsInputWhilePlayingThenReturnsToTheLevel()
        {
            Assume.That(SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null, "Requires a graphics device.");
            bool finished = false;
            var host = new GameObject("ClipHost").AddComponent<CoroutineHost>();
            host.StartCoroutine(Watch());
            IEnumerator Watch()
            {
                yield return FilmPlayer.PlayClip(FixtureFilm);
                finished = true;
            }
            yield return null;
            Assert.IsTrue(FilmPlayer.ClipPlaying);
            Assert.IsTrue(InputGate.Blocked);
            Assert.IsFalse(FilmPlayer.Active, "A clip is not a story film: the level stays loaded.");
            for (float limit = Time.unscaledTime + 30; !finished && Time.unscaledTime < limit;) yield return null;
            Assert.IsTrue(finished, "The clip should reach its end.");
            Assert.IsFalse(FilmPlayer.ClipPlaying);
            yield return null;
            Assert.IsFalse(InputGate.Blocked);
            Object.Destroy(host.gameObject);
        }

        [UnityTest]
        public IEnumerator PlayClip_MissingFileEndsAtOnceWithoutHoldingInput()
        {
            yield return FilmPlayer.PlayClip("no_such_clip.mp4");
            Assert.IsFalse(FilmPlayer.ClipPlaying);
            Assert.IsFalse(InputGate.Blocked);
        }

        [UnityTest]
        public IEnumerator RevealVideoSurface_ShowsVideoInItsLayerAndRestoresTheMirror()
        {
            Assume.That(SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null, "Requires a graphics device.");
            var reveal = NewRevealSystem();
            RevealSystem.Unlock();
            var glass = GameObject.CreatePrimitive(PrimitiveType.Quad);
            glass.transform.position = new Vector3(300, 100, 100);
            glass.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("MysticCircus/PlanarMirror"));
            var mirror = glass.AddComponent<PlanarMirror>();
            var surface = glass.AddComponent<RevealVideoSurface>();
            surface.WarmVideo = FixtureFilm;
            surface.ColdVideo = "no_such_clip.mp4";
            surface.PauseWhilePlaying = new Behaviour[] { mirror };
            yield return null;
            var normal = glass.GetComponent<Renderer>().sharedMaterial;
            Assert.IsFalse(surface.ShowingVideo);

            RevealSystem.Cycle();
            Assert.IsTrue(surface.ShowingVideo);
            Assert.IsFalse(mirror.enabled, "The reflection pauses while the video shows.");
            for (float limit = Time.unscaledTime + 10; !(glass.GetComponent<Renderer>().sharedMaterial.mainTexture is RenderTexture) && Time.unscaledTime < limit;) yield return null;
            Assert.IsInstanceOf<RenderTexture>(glass.GetComponent<Renderer>().sharedMaterial.mainTexture, "The first frame should reach the surface.");

            RevealSystem.Cycle();
            Assert.IsTrue(surface.ShowingVideo, "A missing file still replaces the surface, with a flat colour.");
            Assert.AreSame(Texture2D.whiteTexture, glass.GetComponent<Renderer>().sharedMaterial.mainTexture);

            RevealSystem.Cycle();
            Assert.IsFalse(surface.ShowingVideo);
            Assert.IsTrue(mirror.enabled);
            Assert.AreSame(normal, glass.GetComponent<Renderer>().sharedMaterial);

            Object.Destroy(glass);
            Object.Destroy(reveal.gameObject);
        }

        static FirstPersonPlayer NewPlayer()
        {
            var host = new GameObject("Player");
            host.SetActive(false);
            host.AddComponent<CharacterController>();
            var player = host.AddComponent<FirstPersonPlayer>();
            player.View = new GameObject("View").AddComponent<Camera>();
            player.View.transform.SetParent(host.transform, false);
            player.View.fieldOfView = 60;
            host.SetActive(true);
            return player;
        }

        [UnityTest]
        public IEnumerator Inspectable_PushesInThenFreesThePlayerAndFilesTheDetail()
        {
            var player = NewPlayer();
            var detail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var use = detail.AddComponent<Interactable>();
            var inspect = detail.AddComponent<Inspectable>();
            inspect.Id = "test_close_up";
            inspect.Seconds = 0.3f;
            yield return null;

            use.Use();
            Assert.IsTrue(player.Frozen, "The player holds still during the close-up.");
            yield return new WaitForSeconds(0.2f);
            Assert.Less(player.View.fieldOfView, 60f, "The camera pushes in.");
            yield return new WaitForSeconds(0.8f);
            Assert.IsFalse(player.Frozen);
            Assert.AreEqual(60f, player.View.fieldOfView, 0.01f);
            Assert.IsTrue(Album.HasObserved("test_close_up"));

            Object.Destroy(detail);
            Object.Destroy(player.gameObject);
        }

        [UnityTest]
        public IEnumerator Inspectable_HiddenMidCloseUpDoesNotTrapThePlayer()
        {
            var player = NewPlayer();
            var detail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var use = detail.AddComponent<Interactable>();
            detail.AddComponent<Inspectable>().Id = "test_interrupted";
            yield return null;

            use.Use();
            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(player.Frozen);
            detail.SetActive(false);
            Assert.IsFalse(player.Frozen, "A close-up cut short must hand control back.");
            Assert.AreEqual(60f, player.View.fieldOfView, 0.01f);

            Object.Destroy(detail);
            Object.Destroy(player.gameObject);
        }

        [UnityTest]
        public IEnumerator Apparition_AppearsSweepsAndVanishesAgain()
        {
            var host = new GameObject("Glimpse");
            host.SetActive(false);
            var figure = new GameObject("Sleeve");
            figure.transform.SetParent(host.transform, false);
            var glimpse = host.AddComponent<Apparition>();
            glimpse.Figure = figure.transform;
            glimpse.From = Vector3.zero;
            glimpse.To = Vector3.right * 2;
            glimpse.SweepSeconds = 0.2f;
            glimpse.MinGap = glimpse.MaxGap = 0.1f;
            host.SetActive(true);
            Assert.IsFalse(figure.activeSelf, "Nothing shows between sweeps.");

            for (float limit = Time.time + 3; !figure.activeSelf && Time.time < limit;) yield return null;
            Assert.IsTrue(figure.activeSelf, "A sweep should begin after the gap.");
            for (float limit = Time.time + 3; figure.activeSelf && Time.time < limit;) yield return null;
            Assert.IsFalse(figure.activeSelf, "The figure vanishes when the sweep ends.");
            Assert.AreEqual(2, figure.transform.localPosition.x, 0.01f);

            Object.Destroy(host);
        }

        static float CenterRed(Camera camera)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = camera.targetTexture;
            var reader = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            reader.ReadPixels(new Rect(camera.targetTexture.width / 2, camera.targetTexture.height / 2, 1, 1), 0, 0);
            RenderTexture.active = previous;
            return reader.GetPixel(0, 0).r;
        }
    }
}
