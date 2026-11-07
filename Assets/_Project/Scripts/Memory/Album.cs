using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MysticCircus
{
    // 相簿: memory fragments (left page), observations (right page) and the two pair slots, opened with Tab.
    // Everything it knows is a GameSession flag, so it needs no save data of its own:
    //   frag.<id>   a collected fragment, in collection order
    //   obs.<id>    an observed detail; its thumbnail is cached as a PNG next to the save
    //   combo.<id>  a completed pair (plus its trust flag)
    // Persistent service, created with GameSession.
    public sealed class Album : MonoBehaviour
    {
        public const string ObservationPrefix = "obs.";
        const float FlightSeconds = 0.7f;
        const float BounceSeconds = 0.35f;
        const int ObservationsPerPage = 20;

        static Album instance;
        static readonly Dictionary<string, Texture2D> thumbnails = new Dictionary<string, Texture2D>();
        static readonly List<Flight> flights = new List<Flight>();

        public static bool IsOpen { get; private set; }
        public static event Action<MemoryFragment> Collected;
        // Raised after the pair's film has finished, so a level can react once the screen is back.
        public static event Action<PairRecipe> Combined;

        struct Flight { public Vector2 From; public Color Color; public float Start; }

        sealed class Card
        {
            public MemoryFragment Fragment;
            public PairRecipe Recipe;
            public string Id => Recipe != null ? "pair." + Recipe.Id : Fragment.Id;
            public bool Draggable => Recipe == null;
        }

        readonly string[] slots = new string[2];
        string dragging;
        Vector2 dragOffset;
        readonly string[] bouncing = new string[2];
        readonly Rect[] bounceFrom = new Rect[2];
        float bounceStart = -10;
        float evaluateAt = -1;
        int page;
        readonly List<Card> cards = new List<Card>();

        static GameSession Session => GameSession.Instance;
        static string ThumbnailFolder => Path.Combine(Application.persistentDataPath, "album");

        void Awake()
        {
            instance = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsOpen) Close();
        }

        // ---------- Data ----------

        public static bool Has(string fragmentId) => Session != null && Session.HasFlag(MemoryFragment.FlagPrefix + fragmentId);
        public static bool IsCombined(PairRecipe recipe) => recipe != null && Session != null && Session.HasFlag(recipe.ComboFlag);
        public static bool HasObserved(string id) => Session != null && Session.HasFlag(ObservationPrefix + id);

        public static List<MemoryFragment> Fragments
        {
            get
            {
                var list = new List<MemoryFragment>();
                if (Session != null) list.AddRange(MemoryCatalog.Each(Session.Data.Flags, MemoryFragment.FlagPrefix, MemoryCatalog.Find));
                return list;
            }
        }

        public static List<string> Observations
        {
            get
            {
                var list = new List<string>();
                if (Session == null) return list;
                foreach (string flag in Session.Data.Flags)
                    if (flag.StartsWith(ObservationPrefix, StringComparison.Ordinal)) list.Add(flag.Substring(ObservationPrefix.Length));
                return list;
            }
        }

        // Adds a fragment. Returns false if it was already held or is not in the catalog.
        public static bool Collect(string fragmentId, Vector3? from = null)
        {
            var fragment = MemoryCatalog.Find(fragmentId);
            if (fragment == null) { Debug.LogError("Unknown memory fragment: " + fragmentId); return false; }
            if (Session == null || Has(fragmentId)) return false;
            Session.SetFlag(fragment.Flag);
            Sfx.Play(SfxKind.Pickup);
            Fly(from, fragment.Time == MemoryTime.Before ? ShutterHUD.LayerColors[1] : ShutterHUD.LayerColors[2]);
            Collected?.Invoke(fragment);
            return true;
        }

        // Records a looked-at detail. A thumbnail taken later replaces a missing one. True the first time.
        public static bool Observe(string id, Texture2D thumbnail = null, Vector3? from = null)
        {
            if (Session == null || string.IsNullOrEmpty(id)) return false;
            if (thumbnail != null && Thumbnail(id) == null) StoreThumbnail(id, thumbnail);
            if (HasObserved(id)) return false;
            Session.SetFlag(ObservationPrefix + id);
            Fly(from, ShutterHUD.LayerColors[0]);
            return true;
        }

        public static Texture2D Thumbnail(string id)
        {
            if (thumbnails.TryGetValue(id, out var cached)) return cached;
            string path = Path.Combine(ThumbnailFolder, id + ".png");
            if (!GameSession.PersistenceEnabled || !File.Exists(path)) return null;
            var texture = new Texture2D(2, 2) { hideFlags = HideFlags.DontSave };
            if (!texture.LoadImage(File.ReadAllBytes(path))) return null;
            thumbnails[id] = texture;
            return texture;
        }

        static void StoreThumbnail(string id, Texture2D texture)
        {
            thumbnails[id] = texture;
            if (!GameSession.PersistenceEnabled) return;
            try
            {
                Directory.CreateDirectory(ThumbnailFolder);
                File.WriteAllBytes(Path.Combine(ThumbnailFolder, id + ".png"), texture.EncodeToPNG());
            }
            catch (Exception error) { Debug.LogWarning("Could not save album thumbnail: " + error.Message); }
        }

        // What the camera sees right now, graded, as a small picture.
        public static Texture2D Capture(Camera camera, int width = 320, int height = 180)
        {
            if (camera == null) return null;
            var target = RenderTexture.GetTemporary(width, height, 24);
            var previousTarget = camera.targetTexture;
            camera.targetTexture = target;
            camera.Render();
            camera.targetTexture = previousTarget;
            var previousActive = RenderTexture.active;
            RenderTexture.active = target;
            var picture = new Texture2D(width, height, TextureFormat.RGB24, false) { hideFlags = HideFlags.DontSave };
            picture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            picture.Apply();
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(target);
            return picture;
        }

        // Joins two held fragments. On success the flags are written at once, then the pair's film plays.
        public static bool TryCombine(string a, string b)
        {
            var recipe = MemoryCatalog.Match(a, b);
            if (recipe == null || IsCombined(recipe) || !Has(a) || !Has(b)) return false;
            Session.SetFlag(recipe.ComboFlag);
            Session.SetFlag(recipe.TrustFlag);
            if (IsOpen) Close();
            if (instance != null) instance.StartCoroutine(instance.PlayPair(recipe));
            else Combined?.Invoke(recipe);
            return true;
        }

        IEnumerator PlayPair(PairRecipe recipe)
        {
            Sfx.Play(SfxKind.Glass, 0.5f);
            if (!string.IsNullOrEmpty(recipe.Clip)) yield return FilmPlayer.PlayClip(recipe.Clip, false);
            Combined?.Invoke(recipe);
        }

        static void Fly(Vector3? from, Color color)
        {
            flights.Add(new Flight { From = from.HasValue ? GuiKit.FromWorld(from.Value) : GuiKit.Canvas / 2, Color = color, Start = Time.unscaledTime });
        }

        // ---------- Window ----------

        static bool CanOpen => !IsOpen && !InputGate.Blocked && !ScreenFader.Busy && !FilmPlayer.Active && !FilmPlayer.ClipPlaying
            && !RuleSheetView.IsOpen && !TimingQte.Running && FindAnyObjectByType<FirstPersonPlayer>() != null;

        public static void Open()
        {
            if (instance == null || !CanOpen) return;
            IsOpen = true;
            InputGate.Hold();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Sfx.Play(SfxKind.Page, 0.6f);
        }

        public static void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            InputGate.Release();
            if (instance != null)
            {
                instance.slots[0] = instance.slots[1] = null;
                instance.dragging = null;
                instance.evaluateAt = -1;
            }
            if (FindAnyObjectByType<FirstPersonPlayer>() != null) FirstPersonPlayer.LockCursor(true);
            Sfx.Play(SfxKind.Page, 0.4f);
        }

        void Update()
        {
            flights.RemoveAll(f => Time.unscaledTime - f.Start > FlightSeconds);
            var keyboard = Keyboard.current;
            if (!IsOpen)
            {
                if (keyboard != null && keyboard.tabKey.wasPressedThisFrame) Open();
                return;
            }
            if (keyboard != null && (keyboard.tabKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)) { Close(); return; }
            BuildCards();
            if (evaluateAt > 0 && Time.unscaledTime >= evaluateAt) { evaluateAt = -1; Evaluate(); }
            if (Bouncing || evaluateAt > 0) return;
            Vector2 mouse = GuiKit.Mouse;
            if (GuiKit.Clicked) Press(mouse);
            if (GuiKit.Released && dragging != null) Drop(mouse);
        }

        bool Bouncing => Time.unscaledTime - bounceStart < BounceSeconds;

        void Press(Vector2 mouse)
        {
            for (int k = 0; k < 2; k++)
                if (slots[k] != null && SlotRect(k).Contains(mouse))
                {
                    dragging = slots[k];
                    dragOffset = mouse - SlotRect(k).position;
                    slots[k] = null;
                    return;
                }
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (!card.Draggable || InSlot(card.Id) || !CardRect(i).Contains(mouse)) continue;
                dragging = card.Id;
                dragOffset = mouse - CardRect(i).position;
                Sfx.Play(SfxKind.Page, 0.25f);
                return;
            }
            int pages = PageCount;
            if (pages > 1 && ArrowRect(-1).Contains(mouse)) page = (page + pages - 1) % pages;
            if (pages > 1 && ArrowRect(1).Contains(mouse)) page = (page + 1) % pages;
        }

        void Drop(Vector2 mouse)
        {
            var held = new Rect(mouse - dragOffset, CardSize);
            for (int k = 0; k < 2; k++)
            {
                if (!SlotRect(k).Overlaps(held) || !SlotRect(k).Contains(held.center)) continue;
                slots[k] = dragging;
                if (slots[1 - k] == dragging) slots[1 - k] = null;
                Sfx.Play(SfxKind.Page, 0.35f);
                if (slots[0] != null && slots[1] != null) evaluateAt = Time.unscaledTime + 0.3f;
                break;
            }
            dragging = null;
        }

        void Evaluate()
        {
            if (TryCombine(slots[0], slots[1])) return;
            // Not a pair: both drift back to their places with only a soft paper sound, no message.
            for (int k = 0; k < 2; k++) { bouncing[k] = slots[k]; bounceFrom[k] = SlotRect(k); slots[k] = null; }
            bounceStart = Time.unscaledTime;
            Sfx.Play(SfxKind.Page, 0.3f);
        }

        bool InSlot(string id) => slots[0] == id || slots[1] == id || dragging == id || (Bouncing && (bouncing[0] == id || bouncing[1] == id));

        // A completed pair becomes one card where its first fragment was.
        void BuildCards()
        {
            cards.Clear();
            var shown = new HashSet<string>();
            foreach (var fragment in Fragments)
            {
                var recipe = MemoryCatalog.RecipeOf(fragment.Id);
                if (IsCombined(recipe))
                {
                    if (shown.Add("pair." + recipe.Id)) cards.Add(new Card { Recipe = recipe });
                }
                else cards.Add(new Card { Fragment = fragment });
            }
        }

        // ---------- Layout (virtual canvas) ----------

        static Rect Book
        {
            get
            {
                Vector2 c = GuiKit.Canvas;
                return new Rect(c.x / 2 - 550, c.y / 2 - 300, 1100, 600);
            }
        }

        static readonly Vector2 CardSize = new Vector2(116, 100);
        static readonly Vector2 ThumbSize = new Vector2(108, 61);

        static Rect CardRect(int index)
        {
            var book = Book;
            return new Rect(book.x + 28 + index % 4 * (CardSize.x + 10), book.y + 56 + index / 4 * (CardSize.y + 12), CardSize.x, CardSize.y);
        }

        static Rect ThumbRect(int index)
        {
            var book = Book;
            return new Rect(book.center.x + 30 + index % 4 * (ThumbSize.x + 9), book.y + 56 + index / 4 * (ThumbSize.y + 9), ThumbSize.x, ThumbSize.y);
        }

        static Rect SlotRect(int k)
        {
            var book = Book;
            return new Rect(book.center.x - 140 + k * 160, book.yMax - 124, 120, 100);
        }

        static Rect ArrowRect(int direction)
        {
            var book = Book;
            return new Rect(direction < 0 ? book.xMax - 112 : book.xMax - 70, book.yMax - 160, 30, 30);
        }

        static int PageCount => Mathf.Max(1, Mathf.CeilToInt(Observations.Count / (float)ObservationsPerPage));

        // ---------- Drawing ----------

        static readonly Color PaperColor = new Color(0.93f, 0.89f, 0.82f);
        static readonly Color InkColor = new Color(0.25f, 0.22f, 0.26f);

        void OnGUI()
        {
            GuiKit.Begin();
            GUI.depth = -40;
            if (IsOpen) DrawWindow();
            DrawFlights();
        }

        void DrawWindow()
        {
            GuiKit.Fill(new Rect(Vector2.zero, GuiKit.Canvas), new Color(0.05f, 0.04f, 0.06f, 0.72f));
            var book = Book;
            GuiKit.Fill(new Rect(book.x + 6, book.y + 8, book.width, book.height), new Color(0, 0, 0, 0.35f));
            GuiKit.Fill(book, PaperColor);
            GuiKit.Fill(new Rect(book.center.x - 2, book.y + 16, 4, book.height - 32), new Color(0.6f, 0.54f, 0.48f, 0.5f));

            for (int i = 0; i < cards.Count; i++)
            {
                if (InSlot(cards[i].Id)) { Outline(CardRect(i), new Color(InkColor.r, InkColor.g, InkColor.b, 0.18f)); continue; }
                DrawCard(CardRect(i), cards[i], 1);
            }

            var observations = Observations;
            page = Mathf.Clamp(page, 0, PageCount - 1);
            for (int i = 0; i < ObservationsPerPage; i++)
            {
                int index = page * ObservationsPerPage + i;
                if (index >= observations.Count) break;
                DrawThumbnail(ThumbRect(i), observations[index]);
            }
            if (PageCount > 1) { DrawArrow(ArrowRect(-1), -1); DrawArrow(ArrowRect(1), 1); }

            for (int k = 0; k < 2; k++)
            {
                Outline(SlotRect(k), new Color(InkColor.r, InkColor.g, InkColor.b, 0.45f));
                GuiKit.Circle(SlotRect(k).center, 4, new Color(InkColor.r, InkColor.g, InkColor.b, 0.25f));
                if (slots[k] != null) DrawCard(Fit(SlotRect(k)), CardFor(slots[k]), 1);
            }

            if (Bouncing)
            {
                float t = Mathf.SmoothStep(0, 1, (Time.unscaledTime - bounceStart) / BounceSeconds);
                for (int k = 0; k < 2; k++)
                {
                    int index = cards.FindIndex(c => c.Id == bouncing[k]);
                    if (index < 0) continue;
                    var from = Fit(bounceFrom[k]);
                    var to = CardRect(index);
                    DrawCard(new Rect(Vector2.Lerp(from.position, to.position, t), CardSize), cards[index], 1);
                }
            }

            if (dragging != null) DrawCard(new Rect(GuiKit.Mouse - dragOffset, CardSize), CardFor(dragging), 0.92f);
        }

        Card CardFor(string id) => cards.Find(c => c.Id == id) ?? new Card { Fragment = MemoryCatalog.Find(id) };

        static Rect Fit(Rect slot) => new Rect(slot.center - CardSize / 2, CardSize);

        static Color TimeColor(MemoryTime time) => time == MemoryTime.Before ? ShutterHUD.LayerColors[1] : ShutterHUD.LayerColors[2];

        static void DrawCard(Rect r, Card card, float alpha)
        {
            if (card == null || (card.Fragment == null && card.Recipe == null)) return;
            GuiKit.Fill(new Rect(r.x + 3, r.y + 4, r.width, r.height), new Color(0, 0, 0, 0.18f * alpha));
            GuiKit.Fill(r, new Color(0.99f, 0.97f, 0.93f, alpha));
            var ink = new Color(InkColor.r, InkColor.g, InkColor.b, alpha);
            if (card.Recipe != null)
            {
                GuiKit.Fill(new Rect(r.x, r.y, r.width / 2, 6), Faded(TimeColor(MemoryTime.Before), alpha));
                GuiKit.Fill(new Rect(r.center.x, r.y, r.width / 2, 6), Faded(TimeColor(MemoryTime.After), alpha));
                string before = MemoryCatalog.Find(card.Recipe.BeforeId).Title;
                string after = MemoryCatalog.Find(card.Recipe.AfterId).Title;
                GuiKit.Label(new Rect(r.x + 4, r.y + 8, r.width - 8, r.height / 2 - 6), before, GuiKit.Text(14), ink);
                GuiKit.Line(new Vector2(r.x + 18, r.center.y + 2), new Vector2(r.xMax - 18, r.center.y + 2), 1, Faded(InkColor, alpha * 0.5f));
                GuiKit.Label(new Rect(r.x + 4, r.center.y + 4, r.width - 8, r.height / 2 - 6), after, GuiKit.Text(14), ink);
                return;
            }
            GuiKit.Fill(new Rect(r.x, r.y, r.width, 6), Faded(TimeColor(card.Fragment.Time), alpha));
            var art = Art(card.Fragment.Id);
            var picture = new Rect(r.x + 10, r.y + 11, r.width - 20, r.height - 57);
            if (art != null) GuiKit.Draw(picture, art, new Color(1, 1, 1, alpha));
            else GuiKit.Fill(picture, Faded(TimeColor(card.Fragment.Time), alpha * 0.35f));
            GuiKit.Label(new Rect(r.x + 3, r.yMax - 44, r.width - 6, 43), card.Fragment.Title, GuiKit.Text(14), ink);
        }

        static readonly Dictionary<string, Texture2D> artCache = new Dictionary<string, Texture2D>();

        // Optional picture for a fragment card: Resources/Fragments/<id>. Looked up once.
        static Texture2D Art(string id)
        {
            if (!artCache.TryGetValue(id, out var texture)) artCache[id] = texture = Resources.Load<Texture2D>("Fragments/" + id);
            return texture;
        }

        static void DrawThumbnail(Rect r, string id)
        {
            GuiKit.Fill(new Rect(r.x + 2, r.y + 3, r.width, r.height), new Color(0, 0, 0, 0.15f));
            GuiKit.Fill(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), new Color(0.99f, 0.97f, 0.93f));
            var picture = Thumbnail(id);
            if (picture != null) GuiKit.Draw(r, picture, Color.white);
            else
            {
                // Stable muted colour per detail, so a missing picture is still recognisable.
                float hue = (id.GetHashCode() & 0xffff) / 65535f;
                GuiKit.Fill(r, Color.HSVToRGB(hue, 0.18f, 0.82f));
            }
        }

        static void DrawArrow(Rect r, int direction)
        {
            var ink = new Color(InkColor.r, InkColor.g, InkColor.b, 0.6f);
            Vector2 tip = new Vector2(direction < 0 ? r.x + 8 : r.xMax - 8, r.center.y);
            Vector2 back = new Vector2(direction < 0 ? r.xMax - 8 : r.x + 8, 0);
            GuiKit.Line(tip, new Vector2(back.x, r.y + 6), 2, ink);
            GuiKit.Line(tip, new Vector2(back.x, r.yMax - 6), 2, ink);
        }

        static void Outline(Rect r, Color color)
        {
            GuiKit.Fill(new Rect(r.x, r.y, r.width, 1.5f), color);
            GuiKit.Fill(new Rect(r.x, r.yMax - 1.5f, r.width, 1.5f), color);
            GuiKit.Fill(new Rect(r.x, r.y, 1.5f, r.height), color);
            GuiKit.Fill(new Rect(r.xMax - 1.5f, r.y, 1.5f, r.height), color);
        }

        static Color Faded(Color color, float alpha) => new Color(color.r, color.g, color.b, color.a * alpha);

        static void DrawFlights()
        {
            foreach (var flight in flights)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - flight.Start) / FlightSeconds);
                float eased = 1 - (1 - t) * (1 - t);
                Vector2 centre = Vector2.Lerp(flight.From, ShutterHUD.IconRect.center, eased);
                float size = Mathf.Lerp(64, 10, eased);
                var card = new Rect(centre.x - size / 2, centre.y - size * 0.4f, size, size * 0.8f);
                GuiKit.Fill(card, new Color(0.99f, 0.97f, 0.93f, 1 - t * 0.4f));
                GuiKit.Fill(new Rect(card.x, card.y, card.width, Mathf.Max(2, size * 0.08f)), Faded(flight.Color, 1 - t * 0.4f));
            }
        }
    }
}
