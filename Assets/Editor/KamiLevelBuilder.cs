using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kami.Gameplay;
using Kami.Visual;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace Kami.EditorTools
{
    /// <summary>
    /// Kami > Levels > Build All Levels: creates the game data (ShapeData x10, LevelData, tiles) if missing,
    /// then turns each LevelData layout into Assets/Scenes/Levels/Level_XX.unity and adds it to Build Settings.
    /// Existing data assets are never overwritten, so tune them freely; scenes ARE rebuilt from the layout.
    ///
    /// Layout legend (one character per tile, top row first, y = 0 is the bottom row):
    ///   #  ground (auto edges)      =  one-way bamboo bridge   ~  water        I  ink pit
    ///   S  Kami's start             P  pressing stone          B  Ink Blot     G  torii (goal)
    ///   W  wind vent + column       ^  bamboo spikes           F  fire         c  golden crane
    ///   X  floor switch             D  stone gate (3 tall)     K  crate        s  Paper Sage (hint)
    ///   decor: T bamboo  t grass  b bush  w willow  m maple  p pine  r rock  L lantern  l stone lantern  n banner
    /// </summary>
    public static class KamiLevelBuilder
    {
        const string ShapeRoot = "Assets/Data/Shapes";
        const string LevelRoot = "Assets/Data/Levels";
        const string TileRoot = "Assets/Art/Tiles";
        const string SceneRoot = "Assets/Scenes/Levels";
        const string SpriteRoot = "Assets/Art/Sprites/";
        const string MaterialRoot = "Assets/Art/Materials/";

        static readonly Color Parchment = new Color32(0xF4, 0xEC, 0xDD, 0xFF);

        static Material paper, paperMid, kamiMaterial, water, ink, wind;
        static Dictionary<string, Tile> tiles;

        [MenuItem("Kami/Levels/Build All Levels", priority = 0)]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() || !LoadMaterials())
                return;

            var shapes = EnsureShapes();
            var levels = EnsureLevels(shapes);
            tiles = EnsureTiles();

            var built = new List<EditorBuildSettingsScene>();
            foreach (var level in levels.OrderBy(l => l.number))
                built.Add(new EditorBuildSettingsScene(BuildLevel(level), true));
            EditorBuildSettings.scenes = built
                .Concat(EditorBuildSettings.scenes.Where(s => built.All(b => b.path != s.path)))
                .ToArray();

            EditorSceneManager.OpenScene(built[0].path);
            Debug.Log($"Kami: built {built.Count} levels into {SceneRoot}. Press Play!");
        }

        [MenuItem("Kami/Levels/Rebuild Open Level", priority = 1)]
        public static void RebuildOpen()
        {
            string name = EditorSceneManager.GetActiveScene().name;
            var level = AssetDatabase.FindAssets("t:LevelData", new[] { LevelRoot })
                .Select(g => AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault(l => l != null && l.SceneName == name);
            if (level == null)
            {
                Debug.LogWarning("Kami: the open scene isn't a level built from LevelData. Use Build All Levels.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() || !LoadMaterials())
                return;
            EnsureShapes();
            tiles = EnsureTiles();
            EditorSceneManager.OpenScene(BuildLevel(level));
        }

        // ------------------------------------------------------------------ data

        static Dictionary<ShapeId, ShapeData> EnsureShapes()
        {
            Directory.CreateDirectory(ShapeRoot);
            var shapes = new Dictionary<ShapeId, ShapeData>
            {
                [ShapeId.Ninja] = Shape(ShapeId.Ninja, "Ninja", "Paper stars", "Can't clear tall walls", s =>
                {
                    s.landSpeed = 6f; s.jumpHeight = 2.3f; s.colliderSize = new Vector2(0.6f, 1.5f);
                    s.ability = ShapeAbility.PaperStar; s.abilityCooldown = 0.35f;
                    s.runFrames = new[] { LoadSprite("Characters/kami_ninja_run_1"), LoadSprite("Characters/kami_ninja_run_2") };
                    s.jumpSprite = LoadSprite("Characters/kami_ninja_jump");
                    s.fallSprite = LoadSprite("Characters/kami_ninja_fall");
                    s.hurtSprite = LoadSprite("Characters/kami_ninja_hurt");
                    s.abilitySprite = LoadSprite("Characters/kami_ninja_throw");
                }),
                [ShapeId.Frog] = Shape(ShapeId.Frog, "Frog", "Leaps 5-tile walls", "Slow", s =>
                {
                    s.landSpeed = 3.5f; s.jumpHeight = 5.3f; s.colliderSize = new Vector2(0.95f, 0.7f);
                }),
                [ShapeId.Crane] = Shape(ShapeId.Crane, "Crane", "Rides wind, glides", "Can't jump", s =>
                {
                    s.landSpeed = 5f; s.jumpHeight = 0f; s.glideFallSpeed = 1.5f; s.ridesWind = true;
                    s.colliderSize = new Vector2(1f, 0.85f);
                }),
                [ShapeId.Boat] = Shape(ShapeId.Boat, "Boat", "Floats", "Almost stuck on land", s =>
                {
                    s.landSpeed = 1.5f; s.waterSpeed = 5f; s.jumpHeight = 0f; s.floats = true;
                    s.colliderSize = new Vector2(1.3f, 1f);
                }),
                [ShapeId.Mouse] = Shape(ShapeId.Mouse, "Mouse", "Fits 1-tile tunnels", "Tiny jump", s =>
                {
                    s.landSpeed = 7f; s.jumpHeight = 1.1f; s.colliderSize = new Vector2(0.8f, 0.55f);
                }),
                [ShapeId.Box] = Shape(ShapeId.Box, "Box", "Holds switches, blots bounce off", "Sinks", s =>
                {
                    s.landSpeed = 2f; s.jumpHeight = 0.5f; s.heavy = true; s.blotsBounceOff = true;
                    s.colliderSize = new Vector2(0.95f, 0.95f);
                }),
                [ShapeId.Fish] = Shape(ShapeId.Fish, "Fish", "Swims 6 s under water", "Flops on land", s =>
                {
                    s.landSpeed = 1f; s.waterSpeed = 6f; s.jumpHeight = 0f; s.swims = true; s.swimSeconds = 6f;
                    s.colliderSize = new Vector2(1f, 0.55f);
                }),
                [ShapeId.Fan] = Shape(ShapeId.Fan, "Fan", "4-tile gust", "Can't stop big fires", s =>
                {
                    s.landSpeed = 4f; s.jumpHeight = 1.5f; s.ability = ShapeAbility.FanGust; s.abilityCooldown = 0.6f;
                    s.colliderSize = new Vector2(1f, 0.8f);
                }),
                [ShapeId.Umbrella] = Shape(ShapeId.Umbrella, "Umbrella", "Blocks drips, falls slowly", "Blown by wind", s =>
                {
                    s.landSpeed = 4f; s.jumpHeight = 1f; s.glideFallSpeed = 1f; s.blocksDrips = true; s.blownByWind = true;
                    s.colliderSize = new Vector2(0.9f, 1.4f);
                }),
                [ShapeId.Plane] = Shape(ShapeId.Plane, "Plane", "10-tile dash", "Can't steer", s =>
                {
                    s.landSpeed = 3f; s.jumpHeight = 0f; s.glideFallSpeed = 2f; s.ability = ShapeAbility.PlaneDash;
                    s.abilityCooldown = 1f; s.colliderSize = new Vector2(1.4f, 0.45f);
                }),
            };
            AssetDatabase.SaveAssets();
            return shapes;
        }

        static ShapeData Shape(ShapeId id, string name, string strength, string weakness, Action<ShapeData> setup)
        {
            string path = $"{ShapeRoot}/Shape_{id}.asset";
            var data = AssetDatabase.LoadAssetAtPath<ShapeData>(path);
            if (data != null)
                return data;
            data = ScriptableObject.CreateInstance<ShapeData>();
            data.id = id;
            data.displayName = name;
            data.strength = strength;
            data.weakness = weakness;
            data.sprite = LoadSprite($"Characters/kami_{id.ToString().ToLowerInvariant()}");
            setup(data);
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        static List<LevelData> EnsureLevels(Dictionary<ShapeId, ShapeData> s)
        {
            Directory.CreateDirectory(LevelRoot);
            var levels = new List<LevelData>
            {
                Level(1, "First steps", Biome.BambooForest, "Pop the blots, climb the tall walls", 8, 2,
                      new[] { s[ShapeId.Ninja], s[ShapeId.Frog], null, null }, new[] { s[ShapeId.Frog] }, null,
                      "Every fold leaves a crease, and too many tear you apart.\nF throws a paper star at the blots. " +
                      "Press 2 to fold into the frog and leap the tall walls. Fold wisely!",
                      "Level_02", Layouts.Level1),
                Level(2, "Water and wind", Biome.River, "Cross the ink pit and the river", 8, 4,
                      new[] { s[ShapeId.Ninja], s[ShapeId.Frog], s[ShapeId.Crane], s[ShapeId.Boat] },
                      new[] { s[ShapeId.Frog], s[ShapeId.Crane], s[ShapeId.Boat] }, s[ShapeId.Mouse],
                      "Paper soaks in water: only the boat floats.\nStand in the wind as the crane (3) to fly over the ink. " +
                      "Fold into the boat (4) at the river.",
                      "", Layouts.Level2),
            };
            AssetDatabase.SaveAssets();
            return levels;
        }

        static LevelData Level(int number, string title, Biome biome, string goal, int limit, int par, ShapeData[] belt,
                               ShapeData[] pool, ShapeData unlocks, string hint, string next, string layout)
        {
            string path = $"{LevelRoot}/Level_{number:00}.asset";
            var data = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (data != null)
                return data;
            data = ScriptableObject.CreateInstance<LevelData>();
            data.number = number;
            data.title = title;
            data.biome = biome;
            data.goalText = goal;
            data.creaseLimit = limit;
            data.parFolds = par;
            data.startingBelt = belt;
            data.swapPool = pool;
            data.unlocks = unlocks;
            data.sageHint = hint;
            data.nextScene = next;
            data.layout = layout.Trim('\n');
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        static Dictionary<string, Tile> EnsureTiles()
        {
            Directory.CreateDirectory(TileRoot);
            var result = new Dictionary<string, Tile>();
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { SpriteRoot + "Tiles" }))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
                if (sprite == null)
                    continue;
                string path = $"{TileRoot}/{sprite.name}.asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = sprite;
                    tile.colliderType = sprite.name.StartsWith("bridge") ? Tile.ColliderType.None : Tile.ColliderType.Grid;
                    AssetDatabase.CreateAsset(tile, path);
                }
                result[sprite.name] = tile;
            }
            AssetDatabase.SaveAssets();
            return result;
        }

        static PhysicsMaterial2D Slippery()
        {
            const string path = "Assets/Data/KamiSlippery.physicsMaterial2D";
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (material != null)
                return material;
            material = new PhysicsMaterial2D("KamiSlippery") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // ------------------------------------------------------------------ scene

        static string BuildLevel(LevelData level)
        {
            Directory.CreateDirectory(SceneRoot);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var map = new Map(level.layout);

            var world = new GameObject("World").transform;
            BuildGround(map, level.biome, world);
            BuildBridges(map, world);
            BuildLiquids(map, world, '~', water, "Water");
            BuildLiquids(map, world, 'I', ink, "Ink pit");

            Transform start = null;
            var switches = new List<FloorSwitch>();
            var gates = new List<Gate>();
            var objects = new GameObject("Objects").transform;
            var decor = new GameObject("Decor").transform;
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var foot = new Vector2(x + 0.5f, y);
                    switch (map[x, y])
                    {
                        case 'S': start = new GameObject("Start").transform; start.position = foot; start.SetParent(objects); break;
                        case 'P': Stone(objects, foot); break;
                        case 'B': Blot(objects, foot); break;
                        case 'G': Goal(objects, foot); break;
                        case 'W': Wind(map, objects, x, y); break;
                        case '^': HazardSprite(objects, "Props/bamboo_spikes", foot, new Vector2(0.9f, 0.45f), "Snagged on bamboo spikes!", false); break;
                        case 'F': HazardSprite(objects, "Props/fire", foot, new Vector2(0.6f, 0.8f), "Burned! Paper and fire don't mix.", true); break;
                        case 'c': Crane(objects, foot + Vector2.up * 0.5f); break;
                        case 's': Sage(objects, foot); break;
                        case 'X': switches.Add(Switch(objects, foot)); break;
                        case 'D': gates.Add(GateAt(objects, foot)); break;
                        case 'K': Crate(objects, foot); break;
                        case 'T': Decor(decor, (x % 3) switch { 0 => "Plants/bamboo_tall", 1 => "Plants/bamboo_mid", _ => "Plants/bamboo_short" }, foot, -10); break;
                        case 't': Decor(decor, "Plants/grass_tuft", foot, 26); break;
                        case 'b': Decor(decor, "Plants/bush", foot, -5); break;
                        case 'w': Decor(decor, "Plants/willow", foot, -10); break;
                        case 'm': Decor(decor, "Plants/maple", foot, -10); break;
                        case 'p': Decor(decor, level.biome == Biome.Mountain ? "Plants/pine_snow" : "Plants/pine", foot, -10); break;
                        case 'r': Decor(decor, "Props/rock_small", foot, 3); break;
                        case 'L': Decor(decor, "Props/lantern", foot, 5); break;
                        case 'l': Decor(decor, "Props/stone_lantern", foot, 5); break;
                        case 'n': Decor(decor, "Props/nobori_banner", foot, 4); break;
                    }
                }
            }
            foreach (var gate in gates)
                Set(gate, "switches", switches.Cast<Object>().ToArray());

            BuildBackdrop(level.biome, map.Width);

            var kami = BuildKami(start != null ? (Vector2)start.position : new Vector2(2.5f, 3f));
            var camera = BuildCamera(kami.transform, map);
            var hud = BuildHud();

            var manager = new GameObject("Level Manager").AddComponent<LevelManager>();
            Set(manager, "level", level);
            Set(manager, "kami", kami);
            Set(manager, "hud", hud);
            Set(manager, "cameraFollow", camera);
            Set(manager, "startPoint", start);

            string path = $"{SceneRoot}/{level.SceneName}.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        static void BuildGround(Map map, Biome biome, Transform parent)
        {
            var grid = new GameObject("Grid", typeof(Grid));
            grid.transform.SetParent(parent);
            var go = new GameObject("Ground", typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(grid.transform);
            var tilemap = go.GetComponent<Tilemap>();
            var renderer = go.GetComponent<TilemapRenderer>();
            renderer.sharedMaterial = paper;
            renderer.sortingOrder = 0;

            string set = biome is Biome.BambooForest or Biome.River ? "sand" : "stone";
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    if (map[x, y] != '#')
                        continue;
                    bool top = !map.IsGround(x, y + 1);
                    bool bottom = y > 0 && !map.IsGround(x, y - 1);
                    bool left = x > 0 && !map.IsGround(x - 1, y);
                    bool right = x < map.Width - 1 && !map.IsGround(x + 1, y);
                    string part = top ? (left ? (right ? "single" : "top_left") : right ? "top_right" : "top")
                                : bottom ? (left ? "bottom_left" : right ? "bottom_right" : "bottom")
                                : left ? "left" : right ? "right" : "fill";
                    string tileSet = biome == Biome.Mountain && top ? "snow" : set;
                    if (!tiles.TryGetValue($"{tileSet}_{part}", out var tile))
                        tiles.TryGetValue($"{set}_{part}", out tile);
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }

            var collider = go.AddComponent<TilemapCollider2D>();
            var composite = go.AddComponent<CompositeCollider2D>();
            go.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            collider.compositeOperation = Collider2D.CompositeOperation.Merge;
        }

        static void BuildBridges(Map map, Transform parent)
        {
            var root = new GameObject("Bridges").transform;
            root.SetParent(parent);
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    if (map[x, y] != '=' || map[x - 1, y] == '=')
                        continue;
                    int end = x;
                    while (map[end + 1, y] == '=')
                        end++;
                    var run = new GameObject($"Bridge {x}-{end}").transform;
                    run.SetParent(root);
                    for (int i = x; i <= end; i++)
                    {
                        string piece = i == x ? "bridge_left" : i == end ? "bridge_right" : "bridge_mid";
                        Decor(run, "Tiles/" + piece, new Vector2(i + 0.5f, y + 0.5f), 2);
                    }
                    var platform = new GameObject("Platform", typeof(BoxCollider2D), typeof(PlatformEffector2D));
                    platform.transform.SetParent(run);
                    platform.transform.position = new Vector2((x + end + 1) * 0.5f, y + 1f - 0.16f);
                    var box = platform.GetComponent<BoxCollider2D>();
                    box.size = new Vector2(end - x + 1, 0.2f);
                    box.usedByEffector = true;
                    platform.GetComponent<PlatformEffector2D>().surfaceArc = 160f;
                }
            }
        }

        static void BuildLiquids(Map map, Transform parent, char cell, Material material, string name)
        {
            var visited = new HashSet<Vector2Int>();
            for (int y = map.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    if (map[x, y] != cell || visited.Contains(new Vector2Int(x, y)))
                        continue;
                    int x1 = x;
                    while (map[x1 + 1, y] == cell)
                        x1++;
                    int y0 = y;
                    while (y0 > 0 && Enumerable.Range(x, x1 - x + 1).All(i => map[i, y0 - 1] == cell))
                        y0--;
                    for (int i = x; i <= x1; i++)
                        for (int j = y0; j <= y; j++)
                            visited.Add(new Vector2Int(i, j));

                    float top = y + 1f - (cell == '~' ? 0.25f : 0.2f);
                    float bottom = y0 > 0 ? y0 : -3f;
                    var rect = Rect.MinMaxRect(x - 0.08f, bottom, x1 + 1.08f, top);
                    var body = new GameObject(name);
                    body.transform.SetParent(parent);
                    body.transform.position = rect.center;
                    var trigger = body.AddComponent<BoxCollider2D>();
                    trigger.isTrigger = true;
                    trigger.size = rect.size;
                    if (cell == '~')
                        body.AddComponent<WaterVolume>();
                    else
                        Set(body.AddComponent<Hazard>(), "reason", "Swallowed by the living ink!");

                    var surface = Decor(body.transform, "Shapes/square", rect.center, 25, material);
                    surface.name = "Surface";
                    var size = surface.sprite.bounds.size;
                    surface.transform.localScale = new Vector3(rect.width / size.x, rect.height / size.y, 1f);
                }
            }
        }

        // ------------------------------------------------------------------ objects

        static void Stone(Transform parent, Vector2 foot)
        {
            var sr = Decor(parent, "Props/pressing_stone", foot, 5);
            sr.name = "Pressing Stone";
            var box = sr.gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.4f, 1.4f);
            box.offset = new Vector2(0f, 0.7f);
            var stone = sr.gameObject.AddComponent<PressingStone>();
            Set(stone, "spriteRenderer", sr);
            Set(stone, "inactiveSprite", LoadSprite("Props/pressing_stone"));
            Set(stone, "activeSprite", LoadSprite("Props/pressing_stone_active"));
            Set(stone, "sparkle", LoadSprite("FX/sparkle"));
            Set(stone, "fxMaterial", paper);
        }

        static void Blot(Transform parent, Vector2 foot)
        {
            var sr = Decor(parent, "Enemies/ink_blot", foot, 18);
            sr.name = "Ink Blot";
            var go = sr.gameObject;
            var flipbook = go.AddComponent<SpriteFlipbook>();
            flipbook.frames = new[] { LoadSprite("Enemies/ink_blot"), LoadSprite("Enemies/ink_blot_squash") };
            flipbook.framesPerSecond = 3f;
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.85f, 0.75f);
            box.offset = new Vector2(0f, 0.38f);
            var blot = go.AddComponent<InkBlot>();
            Set(blot, "splatSprite", LoadSprite("FX/ink_splat"));
            Set(blot, "fxMaterial", paper);
        }

        static void Goal(Transform parent, Vector2 foot)
        {
            var sr = Decor(parent, "Props/torii_gate", foot, 4);
            sr.name = "Torii (goal)";
            var trigger = new GameObject("Goal", typeof(BoxCollider2D), typeof(LevelGoal));
            trigger.transform.SetParent(sr.transform, false);
            var box = trigger.GetComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1f, 3f);
            box.offset = new Vector2(0f, 1.5f);
        }

        static void Wind(Map map, Transform parent, int x, int y)
        {
            var vent = Decor(parent, "Props/wind_vent", new Vector2(x + 0.5f, y), 6);
            vent.name = "Wind";
            int height = 1;
            while (height < 9 && map[x, y + height] != '#')
                height++;
            var column = new GameObject("Column", typeof(BoxCollider2D), typeof(WindColumn));
            column.transform.SetParent(vent.transform, false);
            var box = column.GetComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1.5f, height);
            box.offset = new Vector2(0f, height * 0.5f + 0.2f);
            var look = Decor(column.transform, "Shapes/square", new Vector2(x + 0.5f, y + 0.2f + height * 0.5f), 24, wind);
            var size = look.sprite.bounds.size;
            look.transform.localScale = new Vector3(1.6f / size.x, height / size.y, 1f);
        }

        static void HazardSprite(Transform parent, string sprite, Vector2 foot, Vector2 size, string reason, bool fire)
        {
            var sr = Decor(parent, sprite, foot, 7);
            if (fire)
            {
                var flipbook = sr.gameObject.AddComponent<SpriteFlipbook>();
                flipbook.frames = new[] { LoadSprite("Props/fire"), LoadSprite("Props/fire_b") };
                flipbook.framesPerSecond = 6f;
            }
            var box = sr.gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = size;
            box.offset = new Vector2(0f, size.y * 0.5f + 0.05f);
            var hazard = sr.gameObject.AddComponent<Hazard>();
            Set(hazard, "reason", reason);
            Set(hazard, "extinguishable", fire);
            Set(hazard, "puffSprite", LoadSprite("FX/dust"));
            Set(hazard, "puffMaterial", paper);
        }

        static void Crane(Transform parent, Vector2 centre)
        {
            var sr = Decor(parent, "Props/golden_crane", centre, 19);
            sr.name = "Golden Crane";
            sr.gameObject.AddComponent<CircleCollider2D>().radius = 0.4f;
            var pickup = sr.gameObject.AddComponent<Collectible>();
            Set(pickup, "sparkle", LoadSprite("FX/sparkle"));
            Set(pickup, "fxMaterial", paper);
            var motion = sr.gameObject.AddComponent<IdleMotion>();
            motion.bobHeight = 0.12f;
        }

        static void Sage(Transform parent, Vector2 foot)
        {
            var sr = Decor(parent, "NPC/npc_sage", foot, 15);
            sr.name = "Paper Sage";
            var box = sr.gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(4f, 2.5f);
            box.offset = new Vector2(0f, 1.25f);
            sr.gameObject.AddComponent<SageHint>();
        }

        static FloorSwitch Switch(Transform parent, Vector2 foot)
        {
            var sr = Decor(parent, "Props/switch", foot, 6);
            sr.name = "Floor Switch";
            var box = sr.gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.9f, 0.5f);
            box.offset = new Vector2(0f, 0.25f);
            var floorSwitch = sr.gameObject.AddComponent<FloorSwitch>();
            Set(floorSwitch, "spriteRenderer", sr);
            Set(floorSwitch, "upSprite", LoadSprite("Props/switch"));
            Set(floorSwitch, "downSprite", LoadSprite("Props/switch_down"));
            return floorSwitch;
        }

        static Gate GateAt(Transform parent, Vector2 foot)
        {
            var sr = Decor(parent, "Props/gate", foot, 5);
            sr.name = "Stone Gate";
            var box = sr.gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.8f, 3f);
            box.offset = new Vector2(0f, 1.5f);
            sr.gameObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            return sr.gameObject.AddComponent<Gate>();
        }

        static void Crate(Transform parent, Vector2 foot)
        {
            var sr = Decor(parent, "Props/crate", foot, 6);
            sr.name = "Crate";
            var body = sr.gameObject.AddComponent<Rigidbody2D>();
            body.mass = 4f;
            body.freezeRotation = true;
            var box = sr.gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.92f, 0.92f);
            box.offset = new Vector2(0f, 0.46f);
        }

        // ------------------------------------------------------------------ Kami, camera, HUD, backdrop

        static KamiController BuildKami(Vector2 start)
        {
            var go = new GameObject("Kami");
            go.transform.position = start;
            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;
            var box = go.AddComponent<BoxCollider2D>();
            box.sharedMaterial = Slippery();
            box.size = new Vector2(0.6f, 1.5f);
            box.offset = new Vector2(0f, 0.75f);
            go.AddComponent<CreaseCounter>();
            go.AddComponent<FoldBelt>();
            var kami = go.AddComponent<KamiController>();

            var visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(go.transform, false);
            var sr = visualGo.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Characters/kami_ninja");
            sr.sharedMaterial = kamiMaterial;
            sr.sortingOrder = 20;
            visualGo.AddComponent<PaperLook>();
            var visual = visualGo.AddComponent<KamiVisual>();
            Set(visual, "kami", kami);
            Set(visual, "fxMaterial", paper);
            Set(visual, "foldPuff", LoadSprite("FX/fold_puff"));
            Set(visual, "tearBurst", LoadSprite("FX/tear_burst"));
            Set(visual, "paperScrap", LoadSprite("FX/paper_scrap"));
            Set(visual, "dust", LoadSprite("FX/dust"));

            Set(kami, "visual", visual);
            Set(kami, "paperStarSprite", LoadSprite("Projectiles/paper_star"));
            Set(kami, "paperStarMaterial", paper);
            Set(kami, "gustSprite", LoadSprite("FX/gust"));
            return kami;
        }

        static CameraFollow BuildCamera(Transform target, Map map)
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            var camera = go.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Parchment;
            go.transform.position = new Vector3(target.position.x, target.position.y + 2f, -10f);
            var follow = go.AddComponent<CameraFollow>();
            Set(follow, "target", target);
            Set(follow, "bounds", new Rect(0f, -1f, map.Width, map.Height + 3f));
            return follow;
        }

        static KamiHUD BuildHud()
        {
            var hud = new GameObject("HUD", typeof(RectTransform)).AddComponent<KamiHUD>();
            hud.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hud.sprites = new KamiHUD.Sprites
            {
                hudBox = LoadSprite("UI/ui_hud_box"),
                slot = LoadSprite("UI/ui_slot"),
                slotSelected = LoadSprite("UI/ui_slot_selected"),
                creaseEmpty = LoadSprite("UI/ui_crease_empty"),
                creaseFull = LoadSprite("UI/ui_crease_full"),
                creaseWarn = LoadSprite("UI/ui_crease_warn"),
                creaseWarnFull = LoadSprite("UI/ui_crease_warn_full"),
                panel = LoadSprite("UI/ui_panel"),
                dialog = LoadSprite("UI/ui_dialog_box"),
                starFull = LoadSprite("UI/ui_star_full"),
                starEmpty = LoadSprite("UI/ui_star_empty"),
                crane = LoadSprite("Props/golden_crane"),
            };
            return hud;
        }

        static void BuildBackdrop(Biome biome, int width)
        {
            var root = new GameObject("Backdrop").transform;
            float centre = width * 0.5f, span = width + 50f;

            void Layer(string sprite, float y, int order, float followX, float followY, bool tiled = true, float x = float.NaN,
                       Material material = null)
            {
                var holder = new GameObject(Path.GetFileName(sprite)).transform;
                holder.SetParent(root);
                var parallax = holder.gameObject.AddComponent<ParallaxLayer>();
                parallax.followX = followX;
                parallax.followY = followY;
                var sr = Decor(holder, sprite, new Vector2(float.IsNaN(x) ? centre : x, y), order, material);
                if (tiled && sr.sprite != null)
                {
                    sr.drawMode = SpriteDrawMode.Tiled;
                    sr.size = new Vector2(span, sr.sprite.bounds.size.y);
                }
            }

            void Cloud(string sprite, float x, float y)
            {
                Layer(sprite, y, -75, 0.9f, 0.7f, false, x);
                var cloud = root.GetChild(root.childCount - 1).GetChild(0).gameObject.AddComponent<IdleMotion>();
                cloud.bobHeight = 0f;
                cloud.driftRange = 1.5f;
                cloud.driftSpeed = 0.15f;
                cloud.faceDriftDirection = false;
            }

            switch (biome)
            {
                case Biome.BambooForest:
                case Biome.River:
                    Layer("Background/mountains_far", 1.5f, -100, 0.85f, 0.6f);
                    Layer("Background/sun", 9.5f, -95, 0.95f, 0.8f, false, 14f);
                    Layer("Background/mountains_mid", 1.2f, -90, 0.7f, 0.5f);
                    if (biome == Biome.BambooForest)
                        Layer("Background/bamboo_far", 2f, -88, 0.45f, 0.3f);
                    else
                    {
                        Layer("Background/mountains_near", 1f, -80, 0.55f, 0.4f);
                        Layer("Background/pagoda", 1.6f, -85, 0.6f, 0.45f, false, width * 0.7f, paperMid);
                        Layer("Background/village_house", 2f, -84, 0.5f, 0.4f, false, 9f, paperMid);
                    }
                    break;
                case Biome.Mountain:
                    Layer("Background/mountains_far_snow", 1.5f, -100, 0.85f, 0.6f);
                    Layer("Background/mountain_peak", 1.8f, -101, 0.9f, 0.7f, false, width * 0.4f);
                    Layer("Background/mountains_mid", 1.2f, -90, 0.7f, 0.5f);
                    Layer("Background/mountains_near", 1f, -80, 0.55f, 0.4f);
                    break;
                case Biome.Cave:
                    Layer("Background/cave_wall", -1f, -100, 0.3f, 0.2f);
                    break;
                case Biome.Finale:
                    Layer("Background/ink_sky", 6f, -120, 1f, 1f, false);
                    root.GetChild(root.childCount - 1).GetChild(0).localScale = new Vector3(4f, 4f, 1f);
                    break;
            }
            if (biome != Biome.Cave && biome != Biome.Finale)
            {
                Cloud("Background/cloud_a", 8f, 10.5f);
                Cloud("Background/cloud_b", 26f, 11.2f);
                Cloud("Background/cloud_a", 44f, 10.8f);
                Layer("Background/birds_flock", 11.6f, -74, 0.92f, 0.75f, false, 34f);
            }
        }

        // ------------------------------------------------------------------ helpers

        static bool LoadMaterials()
        {
            paper = AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + "M_Paper.mat");
            paperMid = AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + "M_PaperMid.mat");
            kamiMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + "M_Kami.mat");
            water = AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + "M_Water.mat");
            ink = AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + "M_Ink.mat");
            wind = AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + "M_Wind.mat");
            if (paper != null && kamiMaterial != null && water != null)
                return true;
            Debug.LogError("Kami: materials missing in " + MaterialRoot);
            return false;
        }

        static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + path + ".png");
            if (sprite == null)
                Debug.LogWarning("Kami: missing sprite " + path);
            return sprite;
        }

        static SpriteRenderer Decor(Transform parent, string sprite, Vector2 position, int order, Material material = null)
        {
            var go = new GameObject(Path.GetFileName(sprite));
            go.transform.SetParent(parent);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(sprite);
            sr.sharedMaterial = material != null ? material : paper;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>Sets a (possibly private) serialized field, like dragging it in the Inspector.</summary>
        static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"Kami builder: no serialized field '{field}' on {target.GetType().Name}");
                return;
            }
            switch (value)
            {
                case null: property.objectReferenceValue = null; break;
                case Object[] array:
                    property.arraySize = array.Length;
                    for (int i = 0; i < array.Length; i++)
                        property.GetArrayElementAtIndex(i).objectReferenceValue = array[i];
                    break;
                case Object reference: property.objectReferenceValue = reference; break;
                case float f: property.floatValue = f; break;
                case int i: property.intValue = i; break;
                case bool b: property.boolValue = b; break;
                case string s: property.stringValue = s; break;
                case Vector2 v: property.vector2Value = v; break;
                case Rect r: property.rectValue = r; break;
                default: Debug.LogError($"Kami builder: can't set {field} to {value.GetType().Name}"); return;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>The layout text as a grid with y = 0 at the bottom. Outside the map reads as empty.</summary>
        sealed class Map
        {
            readonly string[] rows;
            public int Width { get; }
            public int Height { get; }

            public Map(string layout)
            {
                rows = (layout ?? "").Replace("\r", "").Split('\n').Where(r => r.Length > 0).ToArray();
                Height = rows.Length;
                Width = rows.Length == 0 ? 0 : rows.Max(r => r.Length);
            }

            public char this[int x, int y]
            {
                get
                {
                    if (y < 0 || y >= Height || x < 0)
                        return '.';
                    var row = rows[Height - 1 - y];
                    return x < row.Length ? row[x] : '.';
                }
            }

            /// <summary>Ground continues past the left, right and bottom edges, so the map edges get no torn edge.</summary>
            public bool IsGround(int x, int y) => y < 0 || this[Mathf.Clamp(x, 0, Width - 1), y] == '#';
        }

        /// <summary>Starting layouts. They are copied into each LevelData once; edit them there.</summary>
        static class Layouts
        {
            public const string Level1 = @"
................................................................
................................................................
................................................................
...................c............................................
...............................................c................
..................###...........................................
..................###.............####.......====...............
..................###.............####..........................
.TS..s.T.t..B..T..###..ct..B..T...####.t.P.T......L.t.T.b.G...T.
################################################################
################################################################
################################################################
";

            public const string Level2 = @"
................................................................
................................................................
................................................................
................................................................
...........................c....................................
................................................................
................................................................
..............###......................c........................
..............###...............................................
..............###...............................................
.wS.s...r.B.t.###..t.W...........P.tl...............mB...t..G.b.
#######################........#######~~~~~~~~~~~~##############
#######################IIIIIIII#######~~~~~~~~~~~~##############
#######################IIIIIIII#######~~~~~~~~~~~~##############
";
        }
    }
}
