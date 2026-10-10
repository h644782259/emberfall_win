using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Emberfall.Editor
{
    public sealed class IconArtImport : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/IconArt/", StringComparison.Ordinal)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.maxTextureSize = 128;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }

        public static void Validate()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            int count = 0;
            foreach (string path in Directory.GetFiles("Assets/Resources/IconArt", "*.png", SearchOption.AllDirectories))
            {
                string key = path.Substring("Assets/Resources/IconArt/".Length).Replace(".png", "");
                Texture2D texture = AuthoredIconArt.Load(key);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (texture == null || texture.width != 128 || texture.height != 128 || texture.isReadable ||
                    !importer.alphaIsTransparency || importer.mipmapEnabled || texture.wrapMode != TextureWrapMode.Clamp)
                    throw new InvalidOperationException("Invalid icon import: " + path);
                count++;
            }
            if (count != 170) throw new InvalidOperationException("Expected 130 paintings and 40 cooldown states, got " + count);
            for (int hero = 0; hero < 4; hero++) for (int skill = 0; skill < GameBalance.SkillCount; skill++)
            {
                Texture2D painting = UIIconAtlas.Skill((HeroClass)hero, skill);
                if (!AuthoredIconArt.IsPainting(painting) || UIIconAtlas.SkillGlyph((HeroClass)hero, skill) != painting ||
                    AuthoredIconArt.Cooldown(painting) == painting)
                    throw new InvalidOperationException("Skill identity/cooldown mapping: " + hero + "/" + skill);
            }
            for (int slot = 0; slot < 3; slot++) for (int hero = 0; hero < 4; hero++)
            {
                Texture2D previous = null;
                for (int rarity = 0; rarity < 4; rarity++)
                {
                    Texture2D painting = UIIconAtlas.EquipmentCardIcon((ItemSlot)slot, 1, (Rarity)rarity, (HeroClass)hero);
                    if (!AuthoredIconArt.IsPainting(painting) || painting == previous)
                        throw new InvalidOperationException("Equipment rarity mapping: " + slot + "/" + hero + "/" + rarity);
                    previous = painting;
                }
            }
            for (int rank = 0; rank < 4; rank++)
                if (AuthoredIconArt.ForTint(UIIconAtlas.Utility("gem"), GameBalance.RarityColor((Rarity)rank)) != AuthoredIconArt.Load("gems/" + rank.ToString("D2")))
                    throw new InvalidOperationException("Gem rarity mapping: " + rank);
            Debug.Log("IconArt PASS: 170 imported 128px RGBA textures; 40 shared skill identities/cooldown states, 48 equipment and 4 gem rarity variants.");
        }
    }
}
