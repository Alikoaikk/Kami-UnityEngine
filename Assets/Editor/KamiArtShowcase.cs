using System.IO;
using System.Linq;
using Kami.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Kami.EditorTools
{
    /// <summary>
    /// Kami > Art > Build Art Showcase Scene: builds Assets/Scenes/ArtShowcase.unity with five small
    /// biome dioramas (bamboo forest, river, mountain, ink cave, finale) along y = 0, and a labelled
    /// catalogue of every sprite folder below them. Press Play to see the materials and flipbooks move.
    /// </summary>
    public static class KamiArtShowcase
    {
        const string ScenePath = "Assets/Scenes/ArtShowcase.unity";
        const string SpriteRoot = "Assets/Art/Sprites/";
        const string MaterialRoot = "Assets/Art/Materials/";

        // Top of the ground tiles (row -1, minus the 10 px torn-edge inset).
        const float Surface = -0.08f;

        static readonly Color Parchment = new Color32(0xF4, 0xEC, 0xDD, 0xFF);
        static readonly Color Ink = new Color32(0x2C, 0x2C, 0x2A, 0xFF);

        static Material paper, paperMid, paperFar, kami, water, ink, wind;

        [MenuItem("Kami/Art/Build Art Showcase Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() || !LoadMaterials())
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddCamera(new Vector3(13f, 4f, -10f), 6.5f);

            BambooForest(0f);
            River(30f);
            Mountain(60f);
            InkCave(90f);
            Finale(108f);
            Catalogue(-12f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Kami: built " + ScenePath + ". Dioramas run along y = 0 (x 0 to 136); the sprite catalogue is below y = -12.");
        }

        // ------------------------------------------------------------------ view scene

        public const string ViewScenePath = "Assets/Scenes/KamiView.unity";

        [MenuItem("Kami/Art/Build View Scene")]
        public static void BuildViewMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() || !LoadMaterials())
                return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ComposeView();
            EditorSceneManager.SaveScene(scene, ViewScenePath);
            Debug.Log("Kami: built " + ViewScenePath + " (view only: a camera and sprites, no scripts).");
        }

        /// <summary>Builds the view scene in the background without touching the scenes that are open.</summary>
        public static bool BuildViewInBackground()
        {
            if (!LoadMaterials())
                return false;
            var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            ComposeView();
            bool saved = EditorSceneManager.SaveScene(scene, ViewScenePath);
            if (previous.IsValid())
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
            if (saved)
                Debug.Log("Kami: built " + ViewScenePath + " (view only). Open it from Assets/Scenes.");
            return saved;
        }

        /// <summary>
        /// One composed 16:9 postcard of Kami's world: bamboo bank, river with the paper boat, torii on the
        /// far bank, mountains and the seal-red sun. Only a camera and SpriteRenderers, no scripts.
        /// </summary>
        static void ComposeView()
        {
            AddCamera(new Vector3(0f, 3.5f, -10f), 6f);

            var sky = Group("Sky");
            Tiled(sky, "Background/mountains_far_snow", 0f, 0.4f, 26f, -100, paper);
            Put(sky, "Background/mountain_peak", -1.5f, 0.9f, -101);
            Put(sky, "Background/sun", 4.6f, 6.1f, -96);
            Tiled(sky, "Background/mountains_mid", 0f, 0.1f, 26f, -90, paper);
            Put(sky, "Background/pagoda", 8.6f, 0.3f, -86, paperMid).transform.localScale = Vector3.one * 0.7f;
            Tiled(sky, "Background/mountains_near", 0f, -0.6f, 26f, -80, paper);
            Put(sky, "Background/cloud_a", -7.2f, 7.7f, -75);
            Put(sky, "Background/cloud_b", 7f, 8.4f, -75);
            Put(sky, "Background/cloud_a", 1.2f, 8.9f, -75).transform.localScale = Vector3.one * 0.8f;
            Put(sky, "Background/birds_flock", -2.6f, 7.9f, -74);

            var forest = Group("Bamboo bank");
            Tiled(forest, "Background/bamboo_far", -6f, -0.2f, 10f, -70, paper);
            Ground(forest, "sand", -11, -1, capLeft: false, capRight: true);
            Put(forest, "Plants/bamboo_tall", -10.1f, Surface, -10);
            Put(forest, "Plants/bamboo_mid", -9.2f, Surface, -11);
            Put(forest, "Plants/bamboo_short", -7.9f, Surface, -12);
            Put(forest, "Plants/bamboo_tall", -4.4f, Surface, -60, paperMid);
            Put(forest, "NPC/npc_sage", -8.6f, Surface, 15);
            Put(forest, "Characters/kami_ninja_throw", -6.1f, Surface, 20, kami);
            Put(forest, "Projectiles/paper_star", -4.6f, 0.95f, 21).transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
            Put(forest, "Enemies/ink_blot", -3.1f, Surface, 18);
            Put(forest, "Props/lantern", -1.8f, Surface, 5);
            Put(forest, "Plants/grass_tuft", -9.6f, Surface, 26);
            Put(forest, "Plants/grass_tuft", -5.2f, Surface, 26);
            Put(forest, "Plants/reeds", -1.3f, Surface, 26);

            var river = Group("River");
            Area(river, water, new Rect(-1.1f, -3f, 7.2f, 2.68f), 25, "River (M_Water)");
            Put(river, "Characters/kami_boat", 1.6f, -0.62f, 20, kami);
            Put(river, "Ambient/koi", 0f, -1.7f, 24);
            Put(river, "Props/lily_pad", 4.1f, -0.36f, 26);
            Put(river, "Plants/lotus", 4.9f, -0.42f, 27);
            Put(river, "Characters/kami_crane", -0.4f, 5f, 20, kami);
            Put(river, "Props/golden_crane", 2.6f, 2.6f, 19);
            Put(river, "FX/sparkle", 3.2f, 3.1f, 30);

            var farBank = Group("Far bank");
            Ground(farBank, "sand", 6, 11, capLeft: true, capRight: false);
            Put(farBank, "Plants/maple", 10.6f, Surface, -12);
            Put(farBank, "Props/torii_gate", 9.1f, Surface, 4);
            Put(farBank, "Props/stone_lantern", 6.8f, Surface, 5);
            Put(farBank, "Plants/reeds", 6.3f, Surface, 26);
            Put(farBank, "Enemies/ink_bat", 6.9f, 4.3f, 18);
            Put(farBank, "Plants/grass_tuft", 7.8f, Surface, 26);
        }

        static bool LoadMaterials()
        {
            paper = LoadMaterial("M_Paper");
            paperMid = LoadMaterial("M_PaperMid");
            paperFar = LoadMaterial("M_PaperFar");
            kami = LoadMaterial("M_Kami");
            water = LoadMaterial("M_Water");
            ink = LoadMaterial("M_Ink");
            wind = LoadMaterial("M_Wind");
            if (paper != null && kami != null)
                return true;
            Debug.LogError("Kami: materials missing in " + MaterialRoot);
            return false;
        }

        static void AddCamera(Vector3 position, float size)
        {
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = size;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Parchment;
            camera.transform.position = position;
        }

        // ------------------------------------------------------------------ dioramas

        static void BambooForest(float ox)
        {
            var root = Group("1 Bamboo Forest (levels 1-4)");
            Backdrop(root, ox, 30f, "Background/mountains_far", sun: true);
            Tiled(root, "Background/bamboo_far", ox + 15f, -1f, 30f, -88, paper);

            Ground(root, "sand", ox + 0, ox + 10, capLeft: false, capRight: true);
            Ground(root, "sand", ox + 14, ox + 30, capLeft: true, capRight: false);
            Area(root, water, new Rect(ox + 9.9f, -3f, 4.2f, 2.68f), 25, "Pond (M_Water)");

            Put(root, "Plants/bamboo_tall", ox + 0.8f, Surface, -10);
            Put(root, "Plants/bamboo_mid", ox + 1.7f, Surface, -11);
            Put(root, "Plants/bamboo_short", ox + 3.4f, Surface, -12);
            Put(root, "Plants/bamboo_tall", ox + 6.5f, Surface, -60, paperMid);
            Put(root, "Plants/bamboo_tall", ox + 21.6f, Surface, -10);
            Put(root, "Plants/bamboo_mid", ox + 22.4f, Surface, -11);
            Put(root, "Plants/bamboo_mid", ox + 17.2f, Surface, -60, paperMid);
            Put(root, "Plants/grass_tuft", ox + 2.6f, Surface, 26);
            Put(root, "Plants/grass_tuft", ox + 8.4f, Surface, 26);
            Put(root, "Plants/grass_tuft", ox + 15.0f, Surface, 26);
            Put(root, "Plants/bush", ox + 28.2f, Surface, -5);
            Put(root, "Props/signpost", ox + 4.4f, Surface, 4);

            var ninja = Hero(root, "Characters/kami_ninja_run_1", ox + 5.6f, Surface, creases: 2);
            Flip(ninja, 8f, "Characters/kami_ninja_run_1", "Characters/kami_ninja_run_2");
            var star = Put(root, "Projectiles/paper_star", ox + 7.0f, 0.95f, 21);
            Motion(star, bob: 0f, sway: 360f, swaySpeed: 4f);
            Flip(Put(root, "Enemies/ink_blot", ox + 8.6f, Surface, 18), 3f, "Enemies/ink_blot", "Enemies/ink_blot_squash");
            Motion(Put(root, "Props/golden_crane", ox + 9.6f, 2.4f, 19), bob: 0.15f);

            Motion(Hero(root, "Characters/kami_boat", ox + 12f, -0.62f, creases: 3), bob: 0.05f, sway: 3f);

            Put(root, "Props/lantern", ox + 15.8f, Surface, 5);
            Put(root, "Props/pressing_stone_active", ox + 17.9f, Surface, 5);
            Put(root, "Props/wind_vent", ox + 20f, Surface, 6);
            Area(root, wind, new Rect(ox + 19.2f, Surface + 0.25f, 1.6f, 6.5f), 24, "Wind column (M_Wind)");
            Motion(Hero(root, "Characters/kami_crane", ox + 20f, 3.3f, creases: 4), bob: 0.25f, bobSpeed: 1.2f);
            Put(root, "Props/torii_gate", ox + 25.4f, Surface, 4);
        }

        static void River(float ox)
        {
            var root = Group("2 River (levels 5-8)");
            Backdrop(root, ox, 30f, "Background/mountains_far", sun: false);
            Put(root, "Background/village_house", ox + 3.5f, -0.4f, -85, paperMid).transform.localScale = Vector3.one * 0.8f;
            Put(root, "Background/pagoda", ox + 24f, -0.6f, -86, paperMid);

            Ground(root, "sand", ox + 0, ox + 7, capLeft: false, capRight: true);
            Ground(root, "sand", ox + 21, ox + 30, capLeft: true, capRight: false);
            Area(root, water, new Rect(ox + 6.9f, -3f, 14.2f, 2.68f), 25, "River (M_Water)");

            Put(root, "Plants/willow", ox + 2.2f, Surface, -10);
            Put(root, "Plants/reeds", ox + 6.3f, Surface, 26);
            Put(root, "Plants/reeds", ox + 21.5f, Surface, 26);
            Put(root, "Plants/maple", ox + 25.6f, Surface, -10);
            Put(root, "NPC/npc_sage", ox + 4.6f, Surface, 15);
            Put(root, "Props/arched_bridge", ox + 11f, -0.5f, 6);
            Put(root, "Props/stone_lantern", ox + 22.8f, Surface, 5);

            Motion(Put(root, "Props/lily_pad", ox + 14.6f, -0.36f, 26), bob: 0.03f);
            Put(root, "Plants/lotus", ox + 16.2f, -0.42f, 27);
            Motion(Put(root, "Props/log", ox + 18.8f, -0.4f, 24), bob: 0.04f, sway: 2f);
            Motion(Put(root, "Ambient/koi", ox + 13f, -1.7f, 24), bob: 0.05f, drift: 2.5f, driftSpeed: 0.6f);
            Flip(Put(root, "Enemies/ink_squid", ox + 17.2f, -1.3f, 24), 1.5f, "Enemies/ink_squid", "Enemies/ink_squid_spit");
            Motion(Hero(root, "Characters/kami_fish", ox + 9.4f, -2f, creases: 5), bob: 0.08f, drift: 1.2f, driftSpeed: 0.8f);
            Hero(root, "Characters/kami_frog", ox + 27.6f, Surface, creases: 1);

            foreach (var (x, y) in new[] { (3.2f, 2.2f), (5.4f, 3.1f), (23.5f, 2.6f), (26.8f, 3.4f) })
                Motion(Put(root, "Ambient/firefly", ox + x, y, 30), bob: 0.2f, bobSpeed: 1f);
            var butterfly = Put(root, "Ambient/butterfly", ox + 24.4f, 2.2f, 30);
            Flip(butterfly, 6f, "Ambient/butterfly", "Ambient/butterfly_flap");
            Motion(butterfly, bob: 0.3f, drift: 1.5f, driftSpeed: 0.7f);
        }

        static void Mountain(float ox)
        {
            var root = Group("3 Mountain (levels 9-12)");
            Backdrop(root, ox, 30f, "Background/mountains_far_snow", sun: true);
            Put(root, "Background/mountain_peak", ox + 15f, -1f, -97);

            Ground(root, "snow", ox + 0, ox + 30, capLeft: false, capRight: false);
            Put(root, "Plants/pine", ox + 3.4f, Surface, -60, paperMid);
            Put(root, "Plants/pine_snow", ox + 1.4f, Surface, -10);
            Put(root, "Plants/pine_snow", ox + 27.6f, Surface, -10);
            Put(root, "Props/rock_large", ox + 5.4f, Surface, 3);
            Put(root, "Props/rock_small", ox + 6.9f, Surface, 4);
            Put(root, "Props/bamboo_spikes", ox + 9.5f, Surface, 6);
            Put(root, "Props/bamboo_spikes", ox + 10.5f, Surface, 6);

            Put(root, "Props/switch_down", ox + 13.5f, Surface, 6);
            Hero(root, "Characters/kami_box", ox + 13.5f, Surface + 0.19f, creases: 6);
            Put(root, "Props/gate", ox + 16f, Surface, 5);
            Put(root, "Props/paper_spring", ox + 18.4f, Surface, 6);
            Flip(Put(root, "Props/fire", ox + 20.6f, Surface, 7), 5f, "Props/fire", "Props/fire_b");
            Put(root, "Props/crumble_ledge", ox + 20f, 2.6f, 6);
            Put(root, "Props/crumble_ledge_cracked", ox + 22.6f, 2.6f, 6);
            Motion(Put(root, "Props/golden_crane", ox + 20f, 3.4f, 19), bob: 0.15f);
            Motion(Put(root, "Props/cloud_platform", ox + 11f, 3.8f, 6), bob: 0.12f, bobSpeed: 0.8f, drift: 1.5f, driftSpeed: 0.5f, face: false);
            Motion(Put(root, "Props/fold_diagram", ox + 11f, 4.8f, 19), bob: 0.15f, sway: 8f);

            Motion(Hero(root, "Characters/kami_plane", ox + 5.5f, 4.6f, creases: 2), bob: 0.1f, bobSpeed: 3f);
            Motion(Hero(root, "Characters/kami_umbrella", ox + 25.2f, 4.2f, creases: 3), bob: 0.2f, bobSpeed: 1f, sway: 6f);

            var bat = Put(root, "Enemies/ink_bat", ox + 8f, 3.4f, 18);
            Flip(bat, 6f, "Enemies/ink_bat", "Enemies/ink_bat_flap");
            Motion(bat, bob: 0.35f, bobSpeed: 2.5f, drift: 1.2f, driftSpeed: 1f);
            var moth = Put(root, "Enemies/paper_moth", ox + 23.5f, 2.2f, 18);
            Flip(moth, 7f, "Enemies/paper_moth", "Enemies/paper_moth_flap");
            Motion(moth, bob: 0.3f, bobSpeed: 3f);
            Flip(Put(root, "Enemies/snip_crab", ox + 26f, Surface, 18), 2.5f, "Enemies/snip_crab", "Enemies/snip_crab_closed");
        }

        static void InkCave(float ox)
        {
            var root = Group("4 Ink Cave (level 10)");
            Tiled(root, "Background/cave_wall", ox + 9f, -1f, 18f, -100, paper);
            Tiled(root, "Background/cave_ceiling", ox + 9f, 9.4f, 18f, -50, paper);

            Ground(root, "stone", ox + 0, ox + 8, capLeft: false, capRight: true);
            Ground(root, "stone", ox + 11, ox + 18, capLeft: true, capRight: false);
            Area(root, ink, new Rect(ox + 7.9f, -3f, 3.2f, 2.5f), 25, "Ink pit (M_Ink)");

            Put(root, "Props/stone_lantern", ox + 1.8f, Surface, 5);
            Put(root, "Props/stone_lantern", ox + 14.2f, Surface, 5);
            foreach (var (x, y) in new[] { (3.6f, 3.4f), (5.8f, 5f), (9.5f, 2.8f), (12.4f, 4.4f) })
                Put(root, "Projectiles/ink_drip", ox + x, y, 22);
            Hero(root, "Characters/kami_umbrella", ox + 5.6f, Surface, creases: 5);
            Hero(root, "Characters/kami_mouse", ox + 12.6f, Surface, creases: 2);
            Motion(Put(root, "Props/seal_key", ox + 9.5f, 1.4f, 19), bob: 0.15f);
            Put(root, "Props/seal_door", ox + 16.8f, Surface, 5);
        }

        static void Finale(float ox)
        {
            var root = Group("5 Finale: The Inkwell (level 13)");
            var sky = Put(root, "Background/ink_sky", ox + 14f, 4.5f, -120);
            sky.transform.localScale = new Vector3(3.8f, 3.6f, 1f);

            Ground(root, "stone", ox + 0, ox + 28, capLeft: false, capRight: false);
            Area(root, ink, new Rect(ox, -3f, 28f, 2.35f), 25, "Rising ink (M_Ink)");

            Put(root, "Props/nobori_banner", ox + 1.4f, Surface, 4);
            Put(root, "Props/nobori_banner", ox + 3f, Surface, 4);
            Hero(root, "Characters/kami_ninja_throw", ox + 6.4f, Surface, creases: 6);
            Flip(Put(root, "Enemies/ink_blot", ox + 9.4f, Surface, 18), 3f, "Enemies/ink_blot", "Enemies/ink_blot_squash");
            Flip(Put(root, "Enemies/ink_blot", ox + 11.2f, Surface, 18), 3.4f, "Enemies/ink_blot", "Enemies/ink_blot_squash");

            Put(root, "Props/inkwell", ox + 20.4f, Surface, 6);
            var segments = new[] { (19.2f, 2.8f), (18.0f, 3.7f), (16.8f, 4.3f), (15.6f, 4.6f) };
            for (int i = 0; i < segments.Length; i++)
            {
                var body = Put(root, "Enemies/ink_serpent_body", ox + segments[i].Item1, segments[i].Item2, 16 + i);
                body.transform.localScale = Vector3.one * (0.8f + i * 0.06f);
            }
            Motion(Put(root, "Enemies/ink_serpent_head", ox + 13.6f, 4.9f, 21), bob: 0.15f, bobSpeed: 1.2f, sway: 4f);
            Put(root, "Props/inkwell_sealed", ox + 25.6f, Surface, 6);
            Motion(Put(root, "FX/sparkle", ox + 24.6f, 2.4f, 30), bob: 0.1f, sway: 20f);
            Motion(Put(root, "FX/sparkle", ox + 26.7f, 2.9f, 30), bob: 0.1f, sway: 20f);
        }

        // ------------------------------------------------------------------ catalogue

        static void Catalogue(float top)
        {
            var root = Group("6 Catalogue (every sprite)");
            float y = top;

            // Kami's paper states, driven by PaperLook on M_Kami.
            var states = Group("Kami states: creases 0/2/4/6/8, stained, tearing", root);
            Label(states, "Kami states (PaperLook): creases 0, 2, 4, 6, 8 (limit) / ink stain / tearing", 0f, y);
            float x = 0f;
            foreach (var (c, stain, tear) in new[] { (0, 0f, 0f), (2, 0f, 0f), (4, 0f, 0f), (6, 0f, 0f), (8, 0f, 0f), (1, 0.6f, 0f), (8, 0f, 0.35f) })
            {
                var sr = Hero(states, "Characters/kami_ninja", x + 0.7f, y - 2.6f, c);
                var look = sr.GetComponent<PaperLook>();
                look.stain = stain;
                look.tear = tear;
                x += 1.8f;
            }
            y -= 4f;

            foreach (var folder in new[] { "Characters", "NPC", "Enemies", "Projectiles", "Props", "Plants", "Ambient", "FX", "Tiles", "UI", "Shapes", "Background" })
                y = CatalogueFolder(root, folder, y);
        }

        static float CatalogueFolder(Transform root, string folder, float top)
        {
            var guids = AssetDatabase.FindAssets("t:Sprite", new[] { SpriteRoot + folder });
            var sprites = guids.Select(g => AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(g)))
                               .Where(s => s != null).OrderBy(s => s.name).ToList();
            if (sprites.Count == 0)
                return top;

            var group = Group(folder, root);
            Label(group, folder, 0f, top);
            const float maxWidth = 64f;
            float x = 0f, rowTop = top - 0.9f, rowHeight = 0f;
            foreach (var sprite in sprites)
            {
                var size = sprite.bounds.size;
                if (x > 0f && x + size.x > maxWidth)
                {
                    x = 0f;
                    rowTop -= rowHeight + 0.9f;
                    rowHeight = 0f;
                }
                var go = new GameObject(sprite.name);
                go.transform.SetParent(group, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sharedMaterial = folder == "Characters" ? kami : paper;
                if (folder == "Characters")
                    go.AddComponent<PaperLook>();
                // Place by bounds so every pivot lines up on the row.
                go.transform.localPosition = new Vector3(x - sprite.bounds.min.x, rowTop - size.y - sprite.bounds.min.y, 0f);
                Label(group, sprite.name, x, rowTop - size.y - 0.12f, 0.5f);
                x += Mathf.Max(size.x, 1.2f) + 0.5f;
                rowHeight = Mathf.Max(rowHeight, size.y + 0.4f);
            }
            return rowTop - rowHeight - 1.6f;
        }

        // ------------------------------------------------------------------ helpers

        static Material LoadMaterial(string name) => AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + name + ".mat");

        static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + path + ".png");
            if (sprite == null)
                Debug.LogWarning("Kami showcase: missing sprite " + path + " (run ArtSource/build_art.py?)");
            return sprite;
        }

        static Transform Group(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null)
                go.transform.SetParent(parent, false);
            return go.transform;
        }

        static SpriteRenderer Put(Transform parent, string sprite, float x, float y, int order, Material material = null)
        {
            var go = new GameObject(Path.GetFileName(sprite));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(sprite);
            sr.sharedMaterial = material != null ? material : paper;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>A Kami shape on M_Kami with a PaperLook showing some creases.</summary>
        static SpriteRenderer Hero(Transform parent, string sprite, float x, float y, int creases)
        {
            var sr = Put(parent, sprite, x, y, 20, kami);
            sr.gameObject.AddComponent<PaperLook>().creases = creases;
            return sr;
        }

        static void Tiled(Transform parent, string sprite, float x, float y, float width, int order, Material material)
        {
            var sr = Put(parent, sprite, x, y, order, material);
            if (sr.sprite == null)
                return;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(width, sr.sprite.bounds.size.y);
        }

        /// <summary>The plain square sprite stretched over a rect, for water / ink / wind materials.</summary>
        static void Area(Transform parent, Material material, Rect rect, int order, string name)
        {
            var sr = Put(parent, "Shapes/square", rect.center.x, rect.center.y, order, material);
            sr.name = name;
            if (sr.sprite == null)
                return;
            var size = sr.sprite.bounds.size;
            sr.transform.localScale = new Vector3(rect.width / size.x, rect.height / size.y, 1f);
        }

        /// <summary>Ground tiles from column x0 to x1 - 1, top row at -1, three rows deep.</summary>
        static void Ground(Transform parent, string set, float x0, float x1, bool capLeft, bool capRight)
        {
            var group = Group($"Ground ({set})", parent);
            int first = Mathf.RoundToInt(x0), last = Mathf.RoundToInt(x1) - 1;
            for (int x = first; x <= last; x++)
            {
                for (int row = 0; row < 3; row++)
                {
                    bool top = row == 0;
                    bool left = capLeft && x == first, right = capRight && x == last;
                    string part = top ? (left ? "top_left" : right ? "top_right" : "top")
                                      : (left ? "left" : right ? "right" : "fill");
                    string tileSet = set == "snow" && !top ? "stone" : set;
                    Put(group, $"Tiles/{tileSet}_{part}", x + 0.5f, -0.5f - row, 0);
                }
            }
        }

        /// <summary>Far / mid / near mountains, sun, clouds and birds behind a 30-unit section.</summary>
        static void Backdrop(Transform parent, float ox, float width, string far, bool sun)
        {
            float centre = ox + width / 2f;
            Tiled(parent, far, centre, -1f, width, -100, paper);
            Tiled(parent, "Background/mountains_mid", centre, -1f, width, -90, paper);
            Tiled(parent, "Background/mountains_near", centre, -1f, width, -80, paper);
            if (sun)
                Put(parent, "Background/sun", ox + 8f, 6.4f, -95);
            Motion(Put(parent, "Background/cloud_a", ox + 4f, 7.4f, -75), bob: 0f, drift: 1f, driftSpeed: 0.15f, face: false);
            Motion(Put(parent, "Background/cloud_b", ox + 19f, 6.8f, -75), bob: 0f, drift: 1f, driftSpeed: 0.12f, face: false);
            Put(parent, "Background/birds_flock", ox + 23f, 8.2f, -74);
        }

        static void Flip(SpriteRenderer sr, float fps, params string[] frames)
        {
            var flipbook = sr.gameObject.AddComponent<SpriteFlipbook>();
            flipbook.frames = frames.Select(LoadSprite).Where(s => s != null).ToArray();
            flipbook.framesPerSecond = fps;
        }

        static void Motion(SpriteRenderer sr, float bob = 0.1f, float bobSpeed = 2f, float sway = 0f, float swaySpeed = 1.5f,
                           float drift = 0f, float driftSpeed = 1f, bool face = true)
        {
            var motion = sr.gameObject.AddComponent<IdleMotion>();
            motion.bobHeight = bob;
            motion.bobSpeed = bobSpeed;
            motion.swayDegrees = sway;
            motion.swaySpeed = swaySpeed;
            motion.driftRange = drift;
            motion.driftSpeed = driftSpeed;
            motion.faceDriftDirection = face;
        }

        static void Label(Transform parent, string text, float x, float y, float scale = 1f)
        {
            var go = new GameObject("Label: " + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, y, 0f);
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.fontSize = 48;
            mesh.characterSize = 0.06f * scale;
            mesh.anchor = TextAnchor.UpperLeft;
            mesh.color = Ink;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = mesh.font.material;
            renderer.sortingOrder = 40;
        }
    }
    /// <summary>Builds Assets/Scenes/KamiView.unity once, the first time these scripts load, if it doesn't exist.</summary>
    [InitializeOnLoad]
    static class KamiViewSceneAutoBuild
    {
        const string DoneKey = "Kami.ViewSceneAutoBuilt";

        static KamiViewSceneAutoBuild()
        {
            if (EditorPrefs.GetBool(DoneKey, false) || File.Exists(KamiArtShowcase.ViewScenePath))
                return;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(KamiArtShowcase.ViewScenePath))
                    return;
                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                {
                    if (string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).path))
                    {
                        Debug.Log("Kami: save your open scene, then use Kami > Art > Build View Scene.");
                        return;
                    }
                }
                if (KamiArtShowcase.BuildViewInBackground())
                    EditorPrefs.SetBool(DoneKey, true);
            };
        }
    }
}
