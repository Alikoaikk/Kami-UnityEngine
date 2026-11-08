using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kami.Gameplay
{
    /// <summary>
    /// HUD and in-level screens (GDD section 7), built at runtime from the UI kit sprites:
    /// crease meter (top left, last two marks red), level name (top centre, fades after 3 s),
    /// folds vs par and golden cranes (top right), fold belt on 1-4 (bottom centre), toasts,
    /// Sage hints, the torn message, and the pause and level-complete panels.
    /// </summary>
    public class KamiHUD : MonoBehaviour
    {
        [System.Serializable]
        public class Sprites
        {
            public Sprite hudBox, slot, slotSelected, creaseEmpty, creaseFull, creaseWarn, creaseWarnFull;
            public Sprite panel, dialog, starFull, starEmpty, crane;
        }

        public Sprites sprites = new();
        public Font font;
        [SerializeField] Color ink = new Color32(0x2C, 0x2C, 0x2A, 0xFF);
        [SerializeField] Color red = new Color32(0xA3, 0x2D, 0x2D, 0xFF);
        [SerializeField] Color paper = new Color32(0xF4, 0xEC, 0xDD, 0xFF);

        LevelManager manager;
        KamiController kami;
        RectTransform root;

        RectTransform creaseBox;
        readonly List<Image> creaseMarks = new();
        readonly Image[] slotFrames = new Image[FoldBelt.Size];
        readonly Image[] slotIcons = new Image[FoldBelt.Size];
        Text foldsText, cranesText, titleText, subtitleText, toastText, hintText, tornText, tornReason, beltHint, inkText;
        CanvasGroup titleGroup, toastGroup, hintGroup, tornGroup;
        GameObject pausePanel, completePanel;
        Image[] starImages;
        Text completeStats, unlockText;
        Image unlockIcon;
        float titleTimer = 3f, toastTimer;

        public void Bind(LevelManager levelManager)
        {
            manager = levelManager;
            kami = levelManager.Kami;
            Build();
            kami.Creases.Changed += Refresh;
            kami.Belt.Changed += Refresh;
            kami.ShapeChanged += OnShapeChanged;
            kami.BeltEditingChanged += Refresh;
            kami.Notice += Toast;
            Refresh();
        }

        void OnDestroy()
        {
            if (kami == null)
                return;
            kami.Creases.Changed -= Refresh;
            kami.Belt.Changed -= Refresh;
            kami.ShapeChanged -= OnShapeChanged;
            kami.BeltEditingChanged -= Refresh;
            kami.Notice -= Toast;
        }

        void OnShapeChanged(ShapeData _) => Refresh();

        // ------------------------------------------------------------------ updates

        public void Refresh()
        {
            if (kami == null)
                return;
            var creases = kami.Creases;
            if (creaseMarks.Count != creases.Limit)
                BuildCreaseMarks(creases.Limit);
            for (int i = 0; i < creaseMarks.Count; i++)
            {
                bool used = i < creases.Creases, warn = i >= creases.Limit - 2;
                creaseMarks[i].sprite = used ? (warn ? sprites.creaseWarnFull : sprites.creaseFull)
                                             : (warn ? sprites.creaseWarn : sprites.creaseEmpty);
            }

            foldsText.text = $"Folds {creases.Folds} / par {manager.Level.parFolds}";
            cranesText.text = manager.CranesTotal > 0 ? $"Golden cranes {manager.CranesFound} / {manager.CranesTotal}" : "";

            for (int i = 0; i < FoldBelt.Size; i++)
            {
                var shape = kami.Belt[i];
                slotFrames[i].sprite = shape != null && shape == kami.Shape ? sprites.slotSelected : sprites.slot;
                slotIcons[i].enabled = shape != null;
                slotIcons[i].sprite = shape != null ? shape.sprite : null;
            }
        }

        void Update()
        {
            if (kami == null)
                return;
            float dt = Time.unscaledDeltaTime;

            titleTimer -= dt;
            titleGroup.alpha = Mathf.Clamp01(titleTimer / 0.6f);
            toastTimer -= dt;
            toastGroup.alpha = Mathf.Clamp01(toastTimer / 0.4f);

            bool stained = kami.StainTimeLeft > 0f;
            inkText.text = stained ? $"Ink! No folding for {kami.StainTimeLeft:0.0} s" : "";
            for (int i = 0; i < FoldBelt.Size; i++)
                slotIcons[i].color = stained ? new Color(0.35f, 0.35f, 0.35f, 0.8f) : Color.white;

            beltHint.text = kami.BeltEditing ? "Change belt: press 2, 3 or 4   ·   Tab when done"
                          : kami.StoneHere != null && !kami.IsTorn ? "Tab: change belt" : "";
        }

        public void Toast(string message)
        {
            toastText.text = message;
            toastTimer = 2.4f;
        }

        public void ShowHint(string hint)
        {
            hintGroup.alpha = string.IsNullOrEmpty(hint) ? 0f : 1f;
            hintText.text = hint ?? "";
        }

        public void ShowTorn(string reason)
        {
            tornGroup.alpha = reason == null ? 0f : 1f;
            tornReason.text = reason ?? "";
        }

        public void ShowPause(bool paused) => pausePanel.SetActive(paused);

        public void ShowComplete(int stars, int folds)
        {
            completePanel.SetActive(true);
            for (int i = 0; i < starImages.Length; i++)
                starImages[i].sprite = i < stars ? sprites.starFull : sprites.starEmpty;
            var level = manager.Level;
            completeStats.text = $"Folds {folds} / par {level.parFolds}" +
                                 (manager.CranesTotal > 0 ? $"\nGolden cranes {manager.CranesFound} / {manager.CranesTotal}" : "");
            bool unlock = level.unlocks != null;
            unlockText.text = unlock ? $"New fold unlocked: {level.unlocks.displayName}!" : "";
            unlockIcon.enabled = unlock;
            if (unlock)
                unlockIcon.sprite = level.unlocks.sprite;
        }

        // ------------------------------------------------------------------ layout

        void Build()
        {
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (!TryGetComponent(out Canvas canvas))
                canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            if (!TryGetComponent(out CanvasScaler scaler))
                scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root = (RectTransform)transform;

            var topLeft = new Vector2(0f, 1f);
            var topRight = new Vector2(1f, 1f);
            var top = new Vector2(0.5f, 1f);
            var bottom = new Vector2(0.5f, 0f);
            var centre = new Vector2(0.5f, 0.5f);

            // Crease meter.
            creaseBox = MakeImage("Creases", root, sprites.hudBox, topLeft, topLeft, new Vector2(32f, -28f), new Vector2(420f, 80f), true).rectTransform;
            Label("Label", creaseBox, "Creases", 26, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                  new Vector2(24f, 0f), new Vector2(120f, 40f), ink);

            // Level name.
            var title = MakeImage("Title", root, sprites.hudBox, top, top, new Vector2(0f, -28f), new Vector2(640f, 96f), true);
            titleGroup = title.gameObject.AddComponent<CanvasGroup>();
            var level = manager.Level;
            titleText = Label("Name", title.rectTransform, $"Level {level.number} · {level.title}", 32, TextAnchor.UpperCenter,
                              top, top, new Vector2(0f, -12f), new Vector2(620f, 44f), ink);
            titleText.fontStyle = FontStyle.Bold;
            subtitleText = Label("Goal", title.rectTransform, level.goalText, 21, TextAnchor.UpperCenter, top, top,
                                 new Vector2(0f, -56f), new Vector2(620f, 30f), red);

            // Folds and cranes.
            var folds = MakeImage("Folds", root, sprites.hudBox, topRight, topRight, new Vector2(-32f, -28f), new Vector2(300f, 80f), true);
            foldsText = Label("Text", folds.rectTransform, "", 28, TextAnchor.MiddleCenter, centre, centre, Vector2.zero,
                              new Vector2(280f, 60f), ink);
            cranesText = Label("Cranes", root, "", 22, TextAnchor.UpperRight, topRight, topRight, new Vector2(-40f, -116f),
                               new Vector2(400f, 34f), new Color32(0xC9, 0x8A, 0x1E, 0xFF));

            // Fold belt.
            var belt = MakeRect("Belt", root, bottom, bottom, new Vector2(0f, 24f), new Vector2(4 * 128f, 124f));
            for (int i = 0; i < FoldBelt.Size; i++)
            {
                slotFrames[i] = MakeImage($"Slot {i + 1}", belt, sprites.slot, new Vector2(0f, 0.5f), centre,
                                      new Vector2(64f + i * 128f, 0f), new Vector2(112f, 120f), false);
                slotIcons[i] = MakeImage("Icon", slotFrames[i].rectTransform, null, centre, centre, new Vector2(2f, -6f),
                                     new Vector2(80f, 80f), false);
                slotIcons[i].preserveAspect = true;
                Label("Key", slotFrames[i].rectTransform, (i + 1).ToString(), 22, TextAnchor.MiddleCenter, centre, centre,
                      new Vector2(-34f, 44f), new Vector2(30f, 30f), ink).fontStyle = FontStyle.Bold;
            }
            beltHint = Label("Belt hint", root, "", 24, TextAnchor.MiddleCenter, bottom, bottom, new Vector2(0f, 156f),
                             new Vector2(900f, 34f), ink);
            inkText = Label("Ink", root, "", 24, TextAnchor.MiddleCenter, bottom, bottom, new Vector2(0f, 190f),
                            new Vector2(900f, 34f), red);

            // Toast.
            toastText = Label("Toast", root, "", 30, TextAnchor.MiddleCenter, top, top, new Vector2(0f, -150f),
                              new Vector2(1300f, 50f), ink);
            toastText.gameObject.AddComponent<Shadow>().effectColor = paper;
            toastGroup = toastText.gameObject.AddComponent<CanvasGroup>();

            // Sage hint.
            var hint = MakeImage("Hint", root, sprites.dialog, Vector2.zero, Vector2.zero, new Vector2(32f, 170f), new Vector2(640f, 176f), true);
            hintGroup = hint.gameObject.AddComponent<CanvasGroup>();
            hintText = Label("Text", hint.rectTransform, "", 23, TextAnchor.UpperLeft, topLeft, topLeft, new Vector2(30f, -30f),
                             new Vector2(580f, 110f), ink);
            ShowHint(null);

            // Torn.
            var torn = MakeRect("Torn", root, centre, centre, new Vector2(0f, 80f), new Vector2(1200f, 200f));
            tornGroup = torn.gameObject.AddComponent<CanvasGroup>();
            tornText = Label("Title", torn, "Torn!", 84, TextAnchor.MiddleCenter, top, top, Vector2.zero, new Vector2(1200f, 110f), red);
            tornText.fontStyle = FontStyle.Bold;
            tornText.gameObject.AddComponent<Shadow>().effectColor = paper;
            tornReason = Label("Reason", torn, "", 30, TextAnchor.MiddleCenter, top, top, new Vector2(0f, -112f),
                               new Vector2(1200f, 50f), ink);
            ShowTorn(null);

            // Pause.
            pausePanel = MakeImage("Pause", root, sprites.panel, centre, centre, Vector2.zero, new Vector2(680f, 440f), true).gameObject;
            var pause = (RectTransform)pausePanel.transform;
            Label("Title", pause, "Paused", 52, TextAnchor.MiddleCenter, top, top, new Vector2(0f, -86f), new Vector2(600f, 70f), ink)
                .fontStyle = FontStyle.Bold;
            Label("Help", pause, "Esc  resume\nR  restart from the last stone\n\nA / D move  ·  W / Space jump\n1-4 fold  ·  F ability  ·  Tab belt at a stone",
                  24, TextAnchor.UpperCenter, top, top, new Vector2(0f, -170f), new Vector2(600f, 220f), ink);
            pausePanel.SetActive(false);

            // Level complete.
            completePanel = MakeImage("Complete", root, sprites.panel, centre, centre, Vector2.zero, new Vector2(760f, 560f), true).gameObject;
            var done = (RectTransform)completePanel.transform;
            Label("Title", done, "Level complete!", 50, TextAnchor.MiddleCenter, top, top, new Vector2(0f, -88f),
                  new Vector2(680f, 70f), ink).fontStyle = FontStyle.Bold;
            starImages = new Image[3];
            for (int i = 0; i < 3; i++)
                starImages[i] = MakeImage($"Star {i + 1}", done, sprites.starEmpty, top, centre, new Vector2((i - 1) * 110f, -190f),
                                      new Vector2(96f, 96f), false);
            completeStats = Label("Stats", done, "", 28, TextAnchor.UpperCenter, top, top, new Vector2(0f, -250f),
                                  new Vector2(680f, 80f), ink);
            unlockIcon = MakeImage("Unlock icon", done, null, top, centre, new Vector2(-210f, -370f), new Vector2(80f, 80f), false);
            unlockIcon.preserveAspect = true;
            unlockText = Label("Unlock", done, "", 28, TextAnchor.MiddleLeft, top, top, new Vector2(40f, -350f),
                               new Vector2(440f, 50f), red);
            Label("Next", done, "Enter: next level", 26, TextAnchor.MiddleCenter, Vector2.right * 0.5f, Vector2.right * 0.5f,
                  new Vector2(0f, 70f), new Vector2(680f, 40f), ink);
            completePanel.SetActive(false);
        }

        void BuildCreaseMarks(int limit)
        {
            foreach (var mark in creaseMarks)
                Destroy(mark.gameObject);
            creaseMarks.Clear();
            for (int i = 0; i < limit; i++)
                creaseMarks.Add(MakeImage($"Mark {i + 1}", creaseBox, sprites.creaseEmpty, new Vector2(0f, 0.5f),
                                      new Vector2(0f, 0.5f), new Vector2(150f + i * 34f, 0f), new Vector2(28f, 44f), false));
            creaseBox.sizeDelta = new Vector2(174f + limit * 34f, 80f);
        }

        RectTransform MakeRect(string name, RectTransform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        Image MakeImage(string name, RectTransform parent, Sprite sprite, Vector2 anchor, Vector2 pivot, Vector2 position,
                    Vector2 size, bool sliced)
        {
            var image = MakeRect(name, parent, anchor, pivot, position, size).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            if (sliced)
            {
                image.type = UnityEngine.UI.Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1.4f;
            }
            return image;
        }

        Text Label(string name, RectTransform parent, string text, int size, TextAnchor alignment, Vector2 anchor,
                   Vector2 pivot, Vector2 position, Vector2 box, Color colour)
        {
            var label = MakeRect(name, parent, anchor, pivot, position, box).gameObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = colour;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }
    }
}
