using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Kami.EditorTools
{
    /// <summary>
    /// Import settings for the sprites that ArtSource/build_art.py renders into Assets/Art/Sprites.
    /// 128 px = 1 tile = 1 unit. Pivots, mesh type and 9-slice borders come from the folder and file name.
    /// Settings are applied once per texture (marked in userData), so later Inspector tweaks stick;
    /// Kami > Art > Reapply Sprite Import Settings applies them again.
    /// </summary>
    public class KamiSpriteImporter : AssetPostprocessor
    {
        public const string Root = "Assets/Art/Sprites/";
        public const float PixelsPerUnit = 128f;
        const string Marker = "kami-art-v1";

        static readonly HashSet<string> CentreFolders = new() { "Projectiles", "FX", "Ambient", "UI", "Shapes", "Tiles" };

        // Full Rect: Kami's tear effect needs the padding; tiled backgrounds, tiles and 9-slices need it too.
        static readonly HashSet<string> FullRectFolders = new() { "Characters", "Background", "Tiles", "Shapes", "UI" };

        // File-name prefix -> pivot, where it differs from the folder default.
        static readonly (string prefix, SpriteAlignment pivot)[] PivotOverrides =
        {
            ("ink_bat", SpriteAlignment.Center),
            ("paper_moth", SpriteAlignment.Center),
            ("ink_serpent_head", SpriteAlignment.Center),
            ("ink_serpent_body", SpriteAlignment.Center),
            ("ink_serpent_tail", SpriteAlignment.LeftCenter),
            ("seal_key", SpriteAlignment.Center),
            ("glue_pot", SpriteAlignment.Center),
            ("golden_crane", SpriteAlignment.Center),
            ("fold_diagram", SpriteAlignment.Center),
            ("log", SpriteAlignment.Center),
            ("lily_pad", SpriteAlignment.Center),
            ("crumble_ledge", SpriteAlignment.TopCenter),
            ("cloud_platform", SpriteAlignment.TopCenter),
            ("wind_chime", SpriteAlignment.TopCenter),
            ("cave_ceiling", SpriteAlignment.TopCenter),
            ("sun", SpriteAlignment.Center),
            ("cloud_a", SpriteAlignment.Center),
            ("cloud_b", SpriteAlignment.Center),
            ("birds_flock", SpriteAlignment.Center),
            ("ink_sky", SpriteAlignment.Center),
            ("gust", SpriteAlignment.LeftCenter),
            ("splash", SpriteAlignment.BottomCenter),
            ("dust", SpriteAlignment.BottomCenter),
        };

        // 9-slice borders in px: left, bottom, right, top.
        static readonly Dictionary<string, Vector4> Borders = new()
        {
            { "ui_panel", new Vector4(40, 64, 40, 64) },
            { "ui_dialog_box", new Vector4(104, 44, 32, 32) },
            { "ui_button", new Vector4(28, 28, 28, 28) },
            { "ui_button_pressed", new Vector4(28, 28, 28, 28) },
            { "ui_hud_box", new Vector4(24, 24, 24, 24) },
            { "ui_keycap", new Vector4(16, 16, 16, 16) },
        };

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root) || assetImporter.userData == Marker)
                return;

            var importer = (TextureImporter)assetImporter;
            Apply(importer, assetPath);
            importer.userData = Marker;
        }

        static void Apply(TextureImporter importer, string path)
        {
            string relative = path.Substring(Root.Length);
            string folder = relative.Contains("/") ? relative.Substring(0, relative.IndexOf('/')) : "";
            string file = System.IO.Path.GetFileNameWithoutExtension(path);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = 2048;

            var pivot = CentreFolders.Contains(folder) ? SpriteAlignment.Center : SpriteAlignment.BottomCenter;
            foreach (var (prefix, alignment) in PivotOverrides)
            {
                if (file.StartsWith(prefix))
                {
                    pivot = alignment;
                    break;
                }
            }

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)pivot;
            settings.spriteMeshType = FullRectFolders.Contains(folder) ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
            settings.spriteExtrude = 1;
            if (Borders.TryGetValue(file, out var border))
                settings.spriteBorder = border;
            importer.SetTextureSettings(settings);
        }

        [MenuItem("Kami/Art/Reapply Sprite Import Settings")]
        static void Reapply() => Reimport(onlyUnmarked: false);

        /// <summary>Picks up sprites imported before this script existed (or copied in with old .meta files).</summary>
        internal static void ReimportUnmarked() => Reimport(onlyUnmarked: true);

        static void Reimport(bool onlyUnmarked)
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Root.TrimEnd('/') });
            int count = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var guid in guids)
                {
                    if (AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) is not TextureImporter importer)
                        continue;
                    if (onlyUnmarked && importer.userData == Marker)
                        continue;
                    importer.userData = "";
                    importer.SaveAndReimport();
                    count++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            if (count > 0 || !onlyUnmarked)
                Debug.Log($"Kami: re-imported {count} sprites with Kami import settings.");
        }
    }

    [InitializeOnLoad]
    static class KamiSpriteImportCheck
    {
        const string SessionKey = "Kami.SpriteImportCheck";

        static KamiSpriteImportCheck()
        {
            if (SessionState.GetBool(SessionKey, false))
                return;
            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += KamiSpriteImporter.ReimportUnmarked;
        }
    }
}
