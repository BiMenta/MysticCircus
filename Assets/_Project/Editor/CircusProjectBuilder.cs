using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MysticCircus.Editor
{
    // All generated content is under _Project. Existing scenes are never overwritten silently.
    public static class CircusProjectBuilder
    {
        const string Root = "Assets/_Project";
        public static readonly string[] SceneNames = {
            "00_Backstage", "01_MirrorRoom", "02_IllusionHall", "03_PuppetStage", "04_ToyRoom", "05_ExteriorFinale"
        };
        static Material wine, gold, teal, cream, dark, red, blue, yellow, glass;
        static Transform environment, props;
        static Font font;

        [MenuItem("Mystic Circus/Generate Missing Prototype Scenes")]
        public static void Generate()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before generating scenes.");
            foreach (string folder in new[] { "Art/Materials", "Art/Models", "Audio/Music", "Audio/SFX", "Prefabs/Characters",
                "Prefabs/Interactables", "Scenes", "Settings", "Scripts/Core", "Scripts/Player", "Scripts/Interaction", "Scripts/Puzzles", "Scripts/UI" })
                Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            wine = Mat("Velvet_Wine", "#532B43"); gold = Mat("Antique_Gold", "#BC9756");
            teal = Mat("Dream_Teal", "#28555D"); cream = Mat("Ivory", "#D5C8AE");
            dark = Mat("Midnight", "#17212D"); red = Mat("Toy_Red", "#BA5267");
            blue = Mat("Toy_Blue", "#5489AD"); yellow = Mat("Toy_Yellow", "#D8B964");
            glass = Mat("Mirror_Placeholder", "#77A8AF");
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            PlayerSettings.companyName = "Mystic Circus";
            PlayerSettings.productName = "Mystic Circus";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.runInBackground = true;
            EditorSettings.serializationMode = SerializationMode.ForceText;

            for (int i = 0; i < SceneNames.Length; i++)
            {
                string path = Root + "/Scenes/" + SceneNames[i] + ".unity";
                if (File.Exists(path)) continue;
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                BaseRoom(i == 5);
                var player = CreatePlayer(i == 5 ? new Vector3(0, 0.12f, -3) : new Vector3(0, 0.12f, -6));
                RoomController room = null;
                if (i == 0) room = Backstage();
                if (i == 1) room = Mirror();
                if (i == 2) room = Illusions();
                if (i == 3) room = PuppetStage();
                if (i == 4) room = Toys();
                if (i == 5) Exterior(player);
                var hud = new GameObject("UI_PrototypeHUD").AddComponent<PrototypeHUD>();
                hud.Room = room; hud.Player = player;
                if (i < 5 && i != 2) Door("Exit", new Vector3(0, 0, 7.4f), room, SceneNames[i + 1], i > 0);
                EditorSceneManager.SaveScene(scene, path);
            }
            var buildScenes = new List<EditorBuildSettingsScene>();
            foreach (string name in SceneNames) buildScenes.Add(new EditorBuildSettingsScene(Root + "/Scenes/" + name + ".unity", true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(Root + "/Scenes/" + SceneNames[0] + ".unity");
            Debug.Log("MYSTIC_CIRCUS_GENERATED: six scenes, materials, player prefab, build scene order.");
        }

        static Material Mat(string name, string hex)
        {
            string path = Root + "/Art/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Standard")) { name = name };
            ColorUtility.TryParseHtmlString(hex, out var color);
            material.color = color;
            material.SetFloat("_Glossiness", 0.23f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static GameObject Group(string name, Transform parent = null)
        {
            var go = new GameObject(name); go.transform.SetParent(parent); return go;
        }

        static GameObject Shape(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name; go.transform.SetParent(parent);
            go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        static GameObject Box(string name, Vector3 position, Vector3 scale, Material material, Transform parent = null) =>
            Shape(name, PrimitiveType.Cube, position, scale, material, parent);

        static void Label(string text, Vector3 position, float size = 0.15f, Transform parent = null)
        {
            var go = Group("Label_" + text.Replace("\n", " "), parent);
            go.transform.localPosition = position;
            var mesh = go.AddComponent<TextMesh>(); mesh.text = text; mesh.font = font;
            mesh.anchor = TextAnchor.MiddleCenter; mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 48; mesh.characterSize = size * 0.6f; mesh.color = cream.color;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }

        static void Lamp(Vector3 position, Color color, float intensity = 2.5f, float range = 12)
        {
            var go = Group("Practical_Light", environment); go.transform.position = position;
            var light = go.AddComponent<Light>(); light.type = LightType.Point;
            light.color = color; light.intensity = intensity; light.range = range;
            Shape("Globe", PrimitiveType.Sphere, position, Vector3.one * 0.22f, gold, environment);
        }

        static void BaseRoom(bool outdoor)
        {
            environment = Group("ENVIRONMENT").transform;
            props = Group("PROPS_AND_INTERACTIONS").transform;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.25f, 0.22f, 0.32f);
            RenderSettings.fog = true; RenderSettings.fogColor = dark.color;
            RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = outdoor ? 0.025f : 0.018f;
            RenderSettings.skybox = null;
            Box("Floor", new Vector3(0, -0.2f, outdoor ? -7 : 0), new Vector3(outdoor ? 40 : 18, 0.4f, outdoor ? 40 : 18), dark, environment);
            if (!outdoor)
            {
                Box("BackWall", new Vector3(0, 3, 8.6f), new Vector3(18, 6, 0.3f), teal, environment);
                Box("EntryWall", new Vector3(0, 3, -8.6f), new Vector3(18, 6, 0.3f), wine, environment);
                Box("LeftWall", new Vector3(-8.6f, 3, 0), new Vector3(0.3f, 6, 18), wine, environment);
                Box("RightWall", new Vector3(8.6f, 3, 0), new Vector3(0.3f, 6, 18), wine, environment);
                Box("VelvetCarpet", new Vector3(0, 0.015f, 0), new Vector3(3.3f, 0.03f, 16), wine, environment);
                for (int side = -1; side <= 1; side += 2)
                {
                    Box("Curtain", new Vector3(side * 7.2f, 3, 7.9f), new Vector3(2.4f, 6, 0.5f), wine, environment);
                    Box("GoldTrim", new Vector3(side * 5.9f, 3, 7.6f), new Vector3(0.13f, 6, 0.15f), gold, environment);
                }
                for (int i = -3; i <= 3; i++) Lamp(new Vector3(i * 2.3f, 4.7f, 6.9f), new Color(1, 0.73f, 0.4f), 1.7f, 9);
            }
            Lamp(new Vector3(-4, 4, -3), new Color(0.46f, 0.7f, 1), 2, 16);
            Lamp(new Vector3(4, 5, 1), new Color(1, 0.65f, 0.4f), 3, 17);
        }

        static FirstPersonPlayer CreatePlayer(Vector3 position)
        {
            var go = Group("PLAYER"); go.transform.position = position;
            var cc = go.AddComponent<CharacterController>(); cc.height = 1.8f; cc.radius = 0.3f;
            cc.center = new Vector3(0, 0.9f, 0); cc.stepOffset = 0.3f;
            var player = go.AddComponent<FirstPersonPlayer>();
            var cameraGo = Group("Main Camera", go.transform); cameraGo.tag = "MainCamera";
            cameraGo.transform.localPosition = new Vector3(0, 1.6f, 0);
            player.View = cameraGo.AddComponent<Camera>(); player.View.fieldOfView = 68;
            player.View.backgroundColor = dark.color; player.View.clearFlags = CameraClearFlags.SolidColor;
            cameraGo.AddComponent<AudioListener>();
            string prefab = Root + "/Prefabs/Characters/Player.prefab";
            if (!File.Exists(prefab)) PrefabUtility.SaveAsPrefabAsset(go, prefab);
            return player;
        }

        static T Controller<T>(string chapter, string brief) where T : RoomController
        {
            var room = Group("ROOM_LOGIC").AddComponent<T>(); room.Chapter = chapter; room.Brief = brief;
            Label(chapter, new Vector3(0, 5.25f, 7.45f), 0.19f, environment);
            return room;
        }

        static GameObject Button(string name, Vector3 position, RoomController room, string action, int value, Material material = null)
        {
            var go = Box(name, position, new Vector3(1.15f, 0.7f, 0.7f), material ?? gold, props);
            var target = go.AddComponent<Interactable>(); target.Prompt = name;
            target.Room = room; target.Action = action; target.Value = value;
            Label(name, position + new Vector3(0, 0.68f, -0.38f), 0.09f, props);
            return go;
        }

        static void Door(string name, Vector3 position, RoomController room, string next, bool locked)
        {
            Box(name + "_Frame", position + new Vector3(0, 1.65f, 0.1f), new Vector3(2.5f, 3.5f, 0.4f), gold, props);
            var door = Box(name, position + new Vector3(0, 1.55f, -0.15f), new Vector3(2.1f, 3.1f, 0.3f), dark, props);
            var target = door.AddComponent<Interactable>(); target.Prompt = "Open " + name;
            target.Room = room; target.NextScene = next; target.RequiresSolved = locked;
            Label(name, position + new Vector3(0, 3.8f, -0.3f), 0.14f, props);
        }

        static Transform Actor(string name, Vector3 position, Material costume, bool hat = false)
        {
            var actor = Group(name, props); actor.transform.position = position;
            Shape("Body", PrimitiveType.Capsule, new Vector3(0, 1.2f, 0), new Vector3(0.65f, 0.7f, 0.5f), costume, actor.transform);
            Shape("Face", PrimitiveType.Sphere, new Vector3(0, 2.2f, 0), Vector3.one * 0.55f, cream, actor.transform);
            for (int side = -1; side <= 1; side += 2)
            {
                Box("Leg", new Vector3(side * 0.19f, 0.38f, 0), new Vector3(0.19f, 0.75f, 0.22f), dark, actor.transform);
                Box("Arm", new Vector3(side * 0.48f, 1.3f, 0), new Vector3(0.18f, 0.8f, 0.2f), costume, actor.transform);
                Shape("Eye", PrimitiveType.Sphere, new Vector3(side * 0.1f, 2.24f, -0.25f), Vector3.one * 0.07f, dark, actor.transform);
            }
            var top = Shape("Hat", PrimitiveType.Cylinder, new Vector3(0, 2.62f, 0), new Vector3(0.48f, 0.23f, 0.48f), dark, actor.transform);
            top.SetActive(hat);
            return actor.transform;
        }

        static RoomController Backstage()
        {
            var room = Controller<WardrobeController>("PROLOGUE / BACKSTAGE", "Try the wardrobe: costume, hat and mask. Learn the controls, then enter the mirror room.");
            var actor = Actor("Wardrobe_Mannequin", new Vector3(-4, 0, 2.5f), wine);
            room.CostumePreview = actor.Find("Body").GetComponent<Renderer>();
            room.HatPreview = actor.Find("Hat").gameObject;
            room.MaskPreview = Shape("Mask", PrimitiveType.Sphere, new Vector3(0, 2.2f, -0.25f), new Vector3(0.43f, 0.25f, 0.14f), gold, actor);
            room.Costumes = new[] { wine, teal, red };
            Box("DressingMirror_Frame", new Vector3(-4, 2.5f, 3.2f), new Vector3(2.8f, 4, 0.2f), gold, props);
            Box("DressingMirror_Glass", new Vector3(-4, 2.5f, 3), new Vector3(2.5f, 3.7f, 0.1f), glass, props);
            Button("COSTUME", new Vector3(-5.5f, 1, 0.5f), room, "costume", 0, wine);
            Button("HAT", new Vector3(-4, 1, 0.5f), room, "hat", 0, dark);
            Button("MASK", new Vector3(-2.5f, 1, 0.5f), room, "mask", 0, cream);
            Box("TravelTrunk", new Vector3(5, 0.6f, 2), new Vector3(2.5f, 1.2f, 1.4f), teal, props);
            Label("WELCOME, STRANGER.\nThe show begins behind the gold door.", new Vector3(4.8f, 3, 3), 0.12f, props);
            return room;
        }

        static RoomController Mirror()
        {
            var room = Controller<MirrorRoomController>("ACT I / THE MIME'S MIRROR", "Observe the mime. Opposites reveal the toy sequence.");
            Box("MirrorFrame", new Vector3(0, 2.8f, 4.3f), new Vector3(5.2f, 4.5f, 0.3f), gold, props);
            Box("MirrorSurface_Placeholder", new Vector3(0, 2.8f, 4.05f), new Vector3(4.8f, 4.1f, 0.1f), glass, props);
            room.Mime = Actor("Mime_Reflection_Placeholder", new Vector3(0, 0.7f, 3.55f), dark);
            room.Mime.localRotation = Quaternion.Euler(0, 0, -12);
            string[] gestures = { "LEAN LEFT", "LEAN RIGHT", "ARMS UP", "ARMS DOWN" };
            for (int i = 0; i < 4; i++) Button(gestures[i], new Vector3(-3 + i * 2, 1, 0.3f), room, "gesture", i, teal);
            Label("PAINTING\nRABBIT > STAR > MOON", new Vector3(-5.2f, 3.8f, 4), 0.12f, props);
            string[] toys = { "RABBIT", "STAR", "MOON" };
            for (int i = 0; i < 3; i++)
            {
                var position = new Vector3(-6 + i * 1.7f, 1, -2);
                Button(toys[i], position, room, "toy", i, i == 1 ? yellow : cream);
                Shape("Toy_" + toys[i], PrimitiveType.Sphere, position + Vector3.up * 0.65f, Vector3.one * 0.4f, gold, props);
            }
            return room;
        }

        static RoomController Illusions()
        {
            var room = Controller<IllusionRoomController>("ACT II / THE CLOWN'S LIE", "Two layouts. Two false doors. One way out.");
            room.Clown = Actor("Clown", new Vector3(-5.5f, 0, 5.4f), red);
            room.InvertedLayout = Group("LAYOUT_Inverted", props);
            room.ChessLayout = Group("LAYOUT_Chess", props);
            var bell = Button("RING INVERTED BELL", new Vector3(0, 2.5f, 2), room, "bell", 0, gold);
            bell.transform.SetParent(room.InvertedLayout.transform, true);
            Shape("BellRope", PrimitiveType.Cylinder, new Vector3(0, 4, 2), new Vector3(0.04f, 1.3f, 0.04f), cream, room.InvertedLayout.transform);
            var chair = Box("UpsideDownTable", new Vector3(3, 4.8f, 3), new Vector3(2, 0.2f, 1.5f), cream, room.InvertedLayout.transform);
            chair.transform.localRotation = Quaternion.Euler(180, 0, 10);
            Button("SWITCH REALITY", new Vector3(-5, 1, -2.4f), room, "layout", 0, teal);
            for (int z = 0; z < 3; z++) for (int x = 0; x < 3; x++)
            {
                int index = z * 3 + x;
                var tile = Box("Tile_" + index, new Vector3(-2 + x * 2, 0.08f, z * 2), new Vector3(1.9f, 0.12f, 1.9f), (x + z) % 2 == 0 ? cream : dark, room.ChessLayout.transform);
                var target = tile.AddComponent<Interactable>(); target.Room = room; target.Action = "tile"; target.Value = index;
                target.Prompt = "Activate " + (char)('A' + x) + (z + 1);
            }
            Label("CHESS CLUE\nA1 > B2 > C3", new Vector3(5.5f, 3, 4), 0.14f, props);
            Door("A", new Vector3(-3.4f, 0, 7.4f), room, "", false);
            Door("B", new Vector3(0, 0, 7.4f), room, SceneNames[3], true);
            Door("C", new Vector3(3.4f, 0, 7.4f), room, "", false);
            foreach (var target in UnityEngine.Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None))
                if (target.name == "A" || target.name == "C") target.Action = "wrongDoor";
            room.ChessLayout.SetActive(false);
            return room;
        }

        static RoomController PuppetStage()
        {
            var room = Controller<PuppetStageController>("ACT III / THE PUPPETEER", "Use the console to balance the scale.");
            Box("Stage", new Vector3(0, 0.2f, 3.7f), new Vector3(11, 0.4f, 5), wine, props);
            Box("ScalePillar", new Vector3(0, 1.5f, 3.8f), new Vector3(0.4f, 2.6f, 0.4f), gold, props);
            room.Beam = Group("ScaleBeam", props).transform; room.Beam.position = new Vector3(0, 2.8f, 3.8f);
            Box("Beam", Vector3.zero, new Vector3(6, 0.18f, 0.3f), gold, room.Beam);
            room.LeftWeights = new GameObject[3]; room.RightWeights = new GameObject[3];
            for (int side = -1; side <= 1; side += 2)
            {
                Box("Suspension", new Vector3(side * 2.5f, -0.6f, 0), new Vector3(0.05f, 1.2f, 0.05f), cream, room.Beam);
                Box("Pan", new Vector3(side * 2.5f, -1.2f, 0), new Vector3(1.6f, 0.12f, 1.3f), gold, room.Beam);
                for (int i = 0; i < 3; i++)
                {
                    var weight = Box("Weight_" + i, new Vector3(side * 2.5f + (i - 1) * 0.4f, -0.9f, 0), Vector3.one * 0.35f, teal, room.Beam);
                    if (side < 0) room.LeftWeights[i] = weight; else room.RightWeights[i] = weight;
                    weight.SetActive(false);
                }
            }
            room.Puppet = Actor("Marionette", new Vector3(-5, 0, 2.6f), cream);
            Actor("Puppeteer", new Vector3(5.5f, 0, 5.5f), teal, true);
            for (int i = -1; i <= 1; i++) Shape("PuppetString", PrimitiveType.Cylinder, new Vector3(-5 + i * 0.3f, 3.7f, 2.6f), new Vector3(0.025f, 1.25f, 0.025f), gold, props);
            Button("LEFT WEIGHT", new Vector3(-2, 1, -1), room, "left", 0, teal);
            Button("RIGHT WEIGHT", new Vector3(0, 1, -1), room, "right", 0, teal);
            Button("OPTIONAL SHOW", new Vector3(3, 1, -1), room, "show", 0, wine);
            return room;
        }

        static RoomController Toys()
        {
            var room = Controller<ToyRoomController>("ACT IV / THE TOY ROOM", "A familiar melody. A tangle of colored strings.");
            var musicBox = Box("MusicBox", new Vector3(-3, 0.65f, 2), new Vector3(5.7f, 1.3f, 1.5f), teal, props);
            room.MusicBox = musicBox.AddComponent<AudioSource>(); room.MusicBox.spatialBlend = 0.5f;
            string[] notes = { "C", "D", "E", "F", "G" };
            for (int i = 0; i < 5; i++) Button(notes[i], new Vector3(-5.4f + i * 1.2f, 1.35f, 1.2f), room, "note", i, cream);
            Label("MUSIC BOX\nC > E > G > E", new Vector3(-3, 3.5f, 3.2f), 0.15f, props);
            Material[] colors = { red, blue, yellow };
            string[] names = { "RED", "BLUE", "YELLOW" };
            for (int i = 0; i < 3; i++)
            {
                var end = new Vector3(2.5f + i * 1.6f, 1, 3);
                Button(names[i] + " SWITCH", end, room, "switch", i, colors[i]);
                var line = Group("String_" + names[i], props).AddComponent<LineRenderer>();
                line.sharedMaterial = colors[i]; line.startWidth = line.endWidth = 0.055f; line.positionCount = 4;
                line.SetPositions(new[] { new Vector3(0, 4.8f, 4), new Vector3(2 + i, 4, 4), end + Vector3.up * 1.5f, end });
                Shape("ToyBlock_" + i, PrimitiveType.Cube, new Vector3(4 + i, 0.6f, -3 + i), Vector3.one, colors[i], props);
            }
            Actor("UnfinishedDoll", new Vector3(5, 0, 6), cream);
            return room;
        }

        static void Exterior(FirstPersonPlayer player)
        {
            player.transform.rotation = Quaternion.Euler(0, 180, 0);
            var entrance = Group("CircusEntrance", environment).transform;
            Box("Tent_Backdrop", new Vector3(0, 4, 4), new Vector3(18, 8, 0.4f), wine, entrance);
            for (int i = -4; i <= 4; i++) Box("TentStripe", new Vector3(i * 2, 4, 3.7f), new Vector3(0.8f, 8, 0.15f), cream, entrance);
            Box("EntranceArch", new Vector3(0, 2, 3.3f), new Vector3(4, 4, 0.2f), dark, entrance);
            Label("MYSTIC CIRCUS", new Vector3(0, 6.1f, 3.1f), 0.27f, entrance);
            Box("Path", new Vector3(0, 0.02f, -10), new Vector3(4, 0.04f, 22), teal, environment);
            var ending = Group("ENDING_SEQUENCE").AddComponent<EndingSequence>();
            ending.View = player.View; ending.Entrance = entrance; ending.Player = player;
            ending.Performers = new[] {
                Actor("Mime", new Vector3(-3.5f, 0, 1), dark),
                Actor("Clown", new Vector3(-1.2f, 0, 1.3f), red),
                Actor("Puppeteer", new Vector3(1.2f, 0, 1.3f), teal),
                Actor("Ringmaster", new Vector3(3.5f, 0, 1), wine, true)
            };
            for (int i = -3; i <= 3; i++) Lamp(new Vector3(i * 2.2f, 5, 2.5f), new Color(1, 0.7f, 0.4f), 1.5f, 10);
        }

        [MenuItem("Mystic Circus/Validate Prototype Scenes")]
        public static void Validate()
        {
            var report = new List<string>();
            for (int i = 0; i < SceneNames.Length; i++)
            {
                var scene = EditorSceneManager.OpenScene(Root + "/Scenes/" + SceneNames[i] + ".unity");
                int missing = 0;
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                        missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                if (missing > 0) throw new Exception(SceneNames[i] + " has missing scripts.");
                if (UnityEngine.Object.FindObjectsByType<FirstPersonPlayer>(FindObjectsSortMode.None).Length != 1) throw new Exception("Expected one player.");
                if (Camera.main == null) throw new Exception("Missing main camera.");
                if (i < 5 && UnityEngine.Object.FindAnyObjectByType<RoomController>() == null) throw new Exception("Missing room logic.");
                report.Add(SceneNames[i] + ": OK (camera, player, room logic, no missing scripts)");
            }
            Directory.CreateDirectory("Docs");
            File.WriteAllLines("Docs/SceneValidation.txt", report);
            EditorSceneManager.OpenScene(Root + "/Scenes/" + SceneNames[0] + ".unity");
            Debug.Log("MYSTIC_CIRCUS_VALIDATION_PASSED: " + string.Join("; ", report));
        }
    }
}
