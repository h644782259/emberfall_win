using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    /// <summary>Small imported paintings shared by all presentations of an icon.</summary>
    public static class AuthoredIconArt
    {
        private static readonly Dictionary<string, Texture2D> loaded = new Dictionary<string, Texture2D>();
        private static readonly HashSet<Texture2D> paintings = new HashSet<Texture2D>();
        private static readonly Dictionary<Texture2D, string> keys = new Dictionary<Texture2D, string>();
        private static readonly string[] heroes = { "vanguard", "arcanist", "ranger", "summoner" };

        public static Texture2D Load(string key)
        {
            Texture2D result;
            if (!loaded.TryGetValue(key, out result))
            {
                result = Resources.Load<Texture2D>("IconArt/" + key);
                loaded[key] = result;
                if (result != null) { paintings.Add(result); keys[result] = key; }
            }
            return result;
        }

        public static Texture2D Skill(HeroClass hero, int skill)
        {
            return skill >= 0 && skill < GameBalance.SkillCount ? Load("skills-" + heroes[(int)hero] + "/" + skill.ToString("D2")) : null;
        }

        public static Texture2D Equipment(ItemSlot slot, HeroClass hero, Rarity rarity)
        {
            string family = slot == ItemSlot.Weapon ? "weapon" : slot == ItemSlot.Armor ? "armor" : "relic";
            return Load("equipment-" + family + "/" + ((int)hero * 4 + Mathf.Clamp((int)rarity, 0, 3)).ToString("D2"));
        }

        public static Texture2D Fashion(FashionSlot slot, int appearanceTier, HeroClass hero)
        {
            int family = slot == FashionSlot.Wings ? 0 : hero == HeroClass.Vanguard ? 1 : hero == HeroClass.Ranger ? 2 : 3;
            return Load("fashion/" + (family * 4 + Mathf.Clamp(appearanceTier, 0, 3)).ToString("D2"));
        }

        public static Texture2D BasicAttack(HeroClass hero)
        {
            if (hero == HeroClass.Vanguard) return Load("resources/10") ?? UIIconAtlas.Utility("attack");
            return Load("actions/" + ((int)hero - 1).ToString("D2")) ?? UIIconAtlas.Skill(hero, 0);
        }

        public static Texture2D Utility(string key)
        {
            switch (key)
            {
                case "jump": return Load("actions/03");
                case "shop": return Load("toolbar/00");
                case "achievement": return Load("toolbar/01");
                case "potion": return Load("resources/00");
                case "coin": return Load("resources/01");
                case "shard": return Load("resources/02");
                case "gem": return Load("resources/03");
                case "bag": case "inventory": return Load("resources/06");
                case "core": return Load("resources/07");
                case "smith": return Load("resources/08");
                case "portal": return Load("resources/09");
                case "attack": return Load("resources/10");
                case "dodge": case "blink": return Load("resources/11");
                case "skills": return Load("resources/12");
                case "codex": return Load("resources/14");
                default: return null;
            }
        }

        public static bool IsPainting(Texture2D texture) { return texture != null && paintings.Contains(texture); }

        public static Texture2D ForTint(Texture2D texture, Color requested)
        {
            string key;
            // Existing gem callers provide quality through their tint. Resolve it
            // to authored variants before removing tint from the material art.
            if (texture != null && keys.TryGetValue(texture, out key) && key == "resources/03")
            {
                for (int rank = 0; rank < 4; rank++)
                {
                    Color quality = GameBalance.RarityColor((Rarity)rank);
                    if (Mathf.Abs(requested.r-quality.r) < .01f && Mathf.Abs(requested.g-quality.g) < .01f && Mathf.Abs(requested.b-quality.b) < .01f)
                        return Load("gems/" + rank.ToString("D2")) ?? texture;
                }
            }
            return texture;
        }

        public static Texture2D Cooldown(Texture2D texture)
        {
            string key;
            return texture != null && keys.TryGetValue(texture, out key) ? Load(key + "-cooldown") ?? texture : texture;
        }

        public static Color DisplayTint(Texture2D texture, Color requested)
        {
            if (!IsPainting(texture)) return requested;
            // Rarity/class colors belong to frames. Multiplying a painting by those
            // colors destroys material contrast; only dim states attenuate the art.
            float brightness = Mathf.Max(requested.r, Mathf.Max(requested.g, requested.b));
            brightness = brightness >= .6f ? 1f : brightness;
            return new Color(brightness, brightness, brightness, requested.a);
        }
    }
}
