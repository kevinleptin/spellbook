using UnityEditor;
using UnityEngine;

namespace Spellbook.EditorTools
{
    /// <summary>
    /// 资源导入规则:Resources 下的贴图统一导为 Sprite(UI 用),
    /// 边框类贴图设置九宫格切片。规则在导入时自动生效,无需手动配置。
    /// </summary>
    public class AssetImportPost : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            // 九宫格边框:面板与磁贴框(Kenney Fantasy UI Borders 为 16px 网格)
            var name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            if (name.StartsWith("panel_") || name == "tile_frame")
            {
                importer.spriteBorder = new Vector4(16f, 16f, 16f, 16f);
            }
        }

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/Resources/Audio/")) return;

            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;
            // 音乐流式加载,短音效解压进内存
            var isMusic = assetPath.Contains("music");
            settings.loadType = isMusic
                ? AudioClipLoadType.Streaming
                : AudioClipLoadType.DecompressOnLoad;
            importer.defaultSampleSettings = settings;
        }
    }
}
