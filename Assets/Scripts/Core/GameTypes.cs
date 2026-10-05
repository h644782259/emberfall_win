using System;
using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    // Written atomically with the matching reward identity. Legacy saves may omit it.
    [Serializable]
    public sealed class RewardPresentationReceipt
    {
        public string Id;
        public int Gold,Experience,Materials;
        public bool FirstCompletion,FirstCoreAvailable;
        public int UnlockedNode=-1,UnlockedDifficulty=-1,SharedBefore,SharedAfter;
    }
    public static class MasteryProgressionRules
    {
        public const int MaximumRank=35,InitialInvestment=10,EnhancedInvestment=20;
        public static int Cap(int level)
        {return level<30?0:level<35?5:level<50?10:level<65?15:level<80?20:level<95?30:MaximumRank;}
        public static readonly string TierSummary=BuildTierSummary();
        public static readonly string CoreSummary=InitialInvestment+"点初阶 / "+EnhancedInvestment+"点增强 · 仅启用一个核心";
        private static string BuildTierSummary()
        {
            string levels="",caps="";int previous=Cap(0);
            for(int level=1;level<=100;level++)
            {int cap=Cap(level);if(cap==previous)continue;levels+=(levels.Length>0?"/":"")+level;caps+=(caps.Length>0?"/":"")+cap;previous=cap;}
            return levels+"级 → 上限"+caps+"点";
        }
    }
    public enum HeroClass { Vanguard, Arcanist, Ranger, Summoner }
    public enum ItemSlot { Weapon, Armor, Relic }
    public enum Rarity { Common, Rare, Epic, Legendary }
    public enum FashionSlot { Wings, Weapon }
    public enum ElementalistSpecialization { None, Shatter, Burn }
    public enum EquipmentMechanic { None, FrostEcho, CinderTrail, ReturningBlade, VenomSpread, TwinSummonResonance }
    public enum SummonerRoute { Bonded, Pack }
    public enum MasteryType { Offense, Vitality, Guard, Technique }

    /// <summary>One source of truth for the build codex, acquisition routes and combat contracts.</summary>
    public static class BuildCatalog
    {
        public static EquipmentMechanic[] MechanicsFor(HeroClass hero)
        {
            switch (hero)
            {
                case HeroClass.Arcanist: return new[] { EquipmentMechanic.FrostEcho, EquipmentMechanic.CinderTrail };
                case HeroClass.Vanguard: return new[] { EquipmentMechanic.ReturningBlade };
                case HeroClass.Ranger: return new[] { EquipmentMechanic.VenomSpread };
                case HeroClass.Summoner: return new[] { EquipmentMechanic.TwinSummonResonance };
                default: return new EquipmentMechanic[0];
            }
        }

        public static HeroClass MechanicClass(EquipmentMechanic mechanic)
        {
            switch (mechanic)
            {
                case EquipmentMechanic.FrostEcho:
                case EquipmentMechanic.CinderTrail: return HeroClass.Arcanist;
                case EquipmentMechanic.VenomSpread: return HeroClass.Ranger;
                case EquipmentMechanic.TwinSummonResonance: return HeroClass.Summoner;
                default: return HeroClass.Vanguard;
            }
        }

        public static ItemSlot MechanicSlot(EquipmentMechanic mechanic)
        {
            return mechanic == EquipmentMechanic.CinderTrail || mechanic == EquipmentMechanic.ReturningBlade ? ItemSlot.Weapon : ItemSlot.Relic;
        }

        // Shared with combat dispatch and allocation previews; do not duplicate selected-variant budgets.
        public static float ConcentratedVenomCoefficient(int rank){return rank<=1?2.4f:rank==2?3.6f:4.8f;}
        public static float FrostEchoOpeningMultiplier(bool wide){return wide?.65f:.8f;}
        public static float FrostEchoCoefficient(bool wide){return wide?.45f:.6f;}
        public static float FrostEchoRadiusMultiplier(bool wide){return wide?1.35f:1f;}
        public static float CinderTrailRadiusMultiplier(bool concentrated){return concentrated?.7f:1f;}
        public static float CinderTrailTickMultiplier(bool concentrated){return concentrated?1f/7f:.1f;}
        public const float CinderDirectMultiplier=.8f;

        public static string VenomModifier(bool concentrated){return concentrated?"单发 · 收束毒矢":"扇形 · 毒种传播";}
        public static string VenomSkillSummary(int rank,bool concentrated)
        {
            string identity=GameBalance.SkillName(HeroClass.Ranger,0)+" · "+VenomModifier(concentrated)+"\n";
            if(concentrated)return identity+"一发窄幅实体毒矢，直伤 "+(100*ConcentratedVenomCoefficient(rank)).ToString("0.##")+"%攻击；仅首个实际拦截目标，受墙体和前排阻挡，固定目标意图不保证送达。取消扇形、多目标爆炸与毒传播；保留普通三毒引爆，独立且一次。能量/冷却不变。";
            return identity+GameBalance.SkillEvolution(HeroClass.Ranger,0,rank)+"\n变体A：毒爆加成 -20%；每2秒向附近最多2个合法目标传播1层毒素。";
        }
        public static bool ConcentratedVenomEquipped(GameProfile profile)
        {
            if(profile==null||profile.heroClass!=HeroClass.Ranger||profile.inventory==null)return false;
            var item=profile.inventory.Find(x=>x.id==profile.relicId);
            return item!=null&&item.slot==ItemSlot.Relic&&item.mechanic==EquipmentMechanic.VenomSpread&&item.mechanicVariantUnlocked&&item.mechanicVariant==1;
        }
        public static string VenomSkillOverride(GameProfile profile,int skill,int rank)
        {
            if(profile==null||profile.heroClass!=HeroClass.Ranger||skill!=0||profile.inventory==null)return "";
            var item=profile.inventory.Find(x=>x.id==profile.relicId);
            if(item==null||item.slot!=ItemSlot.Relic||item.mechanic!=EquipmentMechanic.VenomSpread)return "";
            return VenomSkillSummary(Math.Max(1,Math.Min(3,rank)),item.mechanicVariantUnlocked&&item.mechanicVariant==1);
        }

        public static bool HasMechanicVariant(EquipmentMechanic mechanic)
        { return mechanic == EquipmentMechanic.FrostEcho || mechanic == EquipmentMechanic.CinderTrail || mechanic == EquipmentMechanic.ReturningBlade || mechanic == EquipmentMechanic.VenomSpread; }

        public static string MechanicName(EquipmentMechanic mechanic)
        {
            switch (mechanic)
            {
                case EquipmentMechanic.FrostEcho: return "霜回护符";
                case EquipmentMechanic.CinderTrail: return "余烬法杖";
                case EquipmentMechanic.ReturningBlade: return "回刃长剑";
                case EquipmentMechanic.VenomSpread: return "蔓毒护符";
                case EquipmentMechanic.TwinSummonResonance: return "双契共鸣";
                default: return "无机制";
            }
        }

        public static string MechanicDescription(EquipmentMechanic mechanic)
        {
            switch (mechanic)
            {
                case EquipmentMechanic.FrostEcho: return "冰霜新星首击伤害 -20%；0.7秒后回响造成40%基础伤害并再次施加冰霜控制（灼燃专精仍只减速）。变体：范围 +35%、回响伤害降低。";
                case EquipmentMechanic.CinderTrail: return "陨星直接伤害 -20%；落点留下2秒火场，总计40%基础伤害。每次施法每目标仅反应一次。变体：火场半径 -30%、每跳伤害提高。";
                case EquipmentMechanic.ReturningBlade: return "普攻伤害 -8%；每1.5秒回刃弹向4米内另一个目标，造成110%基础伤害；回收后强化下一刀，击杀再弹65%。变体B：放弃全部弹射与回收强化，取消普攻-8%；真实完美闪避的反击窗口延至3秒，下次175%反击改窄刺，可沿合法地面前进最多2米。";
                case EquipmentMechanic.VenomSpread: return "变体A：毒素引爆加成伤害 -20%；每2秒向附近最多2个目标传播1层毒素。变体B：扇形箭改为一发窄幅实体毒矢，直伤240%/360%/480%攻击，仅命中首个拦截目标；无扇形、爆炸和传播，保留普通三毒引爆，能量与冷却不变。";
                case EquipmentMechanic.TwinSummonResonance: return "普通召唤上限改为2；伙伴伤害 +60%、生命 +20%。不同类型伙伴1.5秒内实际命中同一集火目标时共鸣追加35%基础伤害，冷却3秒。";
                default: return "装备机制只在穿戴且职业匹配时生效。";
            }
        }

        public static string MechanicSource(EquipmentMechanic mechanic)
        {
            if (mechanic == EquipmentMechanic.None || !Enum.IsDefined(typeof(EquipmentMechanic), mechanic)) return "无";
            return "本职业首通自选；遗迹首领机制概率25%至37%、普通史诗/传说机制概率12%至20%，随实际阶数提升；12碎片定向兑换。通关1/5/10/20/40阶每次获3/4/5/6/7碎片；通关第5阶后24碎片将已知史诗机制升华为传说。";
        }

        public static string SpecializationName(ElementalistSpecialization specialization)
        {
            return specialization == ElementalistSpecialization.Shatter ? "碎冰" : specialization == ElementalistSpecialization.Burn ? "灼燃" : "均衡";
        }

        public static string SpecializationDescription(ElementalistSpecialization specialization)
        {
            if (specialization == ElementalistSpecialization.Shatter) return "普攻积霜，新星冻结，陨星首个满足条件的命中碎冰；陨星直伤 -15%、碎冰 +100%。护盾延长霜痕，终极法术先积霜后引爆。";
            if (specialization == ElementalistSpecialization.Burn) return "普攻余烬、新星减速留人、陨星直伤 -20%并灼烧90%。护盾减伤降至30%但机动留火区；终极法术降低直伤换持续灼烧。";
            return "保留原有冰霜控制；陨星消耗冰霜印记，碎冰追加50%基础伤害。专精仅改变元素师，营地免费切换。";
        }

        public static string ClassSignatureDescription(HeroClass hero)
        {
            if (hero == HeroClass.Vanguard) return "完美闪避真正避开攻击时回复12能量；2秒内下一次普攻造成175%伤害。";
            if (hero == HeroClass.Arcanist) return "对同一目标3次普攻触发霜触；学新星后转为3秒霜痕，灼燃转为余烬。完美闪避强化下次命中的霜痕或余烬，3秒不叠加。";
            if (hero == HeroClass.Ranger) return "普攻叠加最多3层毒素；扇形箭消耗3层引爆，每个目标每次施法仅触发一次。";
            return "基础灵狼永久陪伴；契约升级伙伴，存活时按键发出指令，死亡后可重召。营地选择双契常驻或群契限时路线；完美闪避召回并强化下次指令。";
        }

        public static string MasteryName(MasteryType mastery)
        {
            return new[] { "破敌精通", "生命精通", "坚壁精通", "技巧精通" }[(int)mastery];
        }
        public static string MasteryDescription(MasteryType mastery)
        {
            return new[] { "每点攻击 +0.3%；核心：技能命中后6秒内下一次普攻追加一次非暴击攻击，初阶60% / 增强100%攻击伤害；冷却6 / 4秒。",
                "每点生命 +0.5%；核心：受到实际伤害后生命低于50%且存活时，恢复3% / 5%最大生命；冷却12 / 10秒，不会复活。",
                "每点护甲 +0.75%；核心：完美闪避获得15% / 25%减伤，持续2 / 3秒；冷却8 / 6秒。",
                "每点普攻回能 +0.1；核心：技能实际消耗累计60 / 45能量，回复8 / 12能量并减冷却0.4 / 0.7秒；冷却8 / 6秒，冷却中不累计。" }[(int)mastery]
                + MasteryProgressionRules.TierSummary+"；"+MasteryProgressionRules.CoreSummary+"。营地可退还技能2/3阶或重置精通。";
        }
        public const string DamageRules = "暴击：普攻与直接技能每次施法掷骰，延迟命中继承结果；持续伤害、反应和伙伴不暴击。伙伴继承当前攻击与生命及契约阶数，数量与指令影响输出。";

    }

    /// <summary>Durable chest receipt. Unity serializes the fields; nullable view properties
    /// let the reveal UI distinguish a gold-only reward without unsupported nullable fields.</summary>
    public enum RewardMaterialKind { None, StarAshFragment }
    public enum ChestRewardKind { Legacy, SingleChest }
    [Serializable]
    public class MaterialExchangeReceipt
    { public string id;public long sequence;public RewardMaterialKind materialKind;public int threadsDelta,materialsDelta; }
    [Serializable]
    public class ChestReward
    {
        // Zero is the historical identical-chest schema; never infer it from choice.
        public int rulesRevision;
        public bool hasCurrencyDeltas;
        public int goldDelta, threadsDelta,materialsDelta;
        public ChestRewardKind rewardKind;public RewardMaterialKind materialKind;
        public int materials,baseGold,duplicateGold,baseThreads,duplicateThreads;public bool legacyGoldProtection;
        public string id;
        public int choice;
        public int gold;
        public int rarityIndex = -1;
        public int slotIndex = -1;
        public string name;
        public bool duplicate;
        public string summary;
        public string Id { get { return id; } }
        public int Gold { get { return gold; } }
        public Rarity? Rarity { get { return rarityIndex < 0 ? (Emberfall.Rarity?)null : (Emberfall.Rarity)rarityIndex; } }
        public FashionSlot? Slot { get { return slotIndex < 0 ? (FashionSlot?)null : (FashionSlot)slotIndex; } }
        public string Name { get { return name; } }
        public bool Duplicate { get { return duplicate; } }
    }

    [Serializable]
    public class FashionData
    {
        public string id;
        public FashionSlot slot;
        public Rarity rarity;
        public string name;
    }
    public enum EnemyKind { Slime, Goblin, Wisp, Guardian }
    public enum ZoneKind { Wilderness, Dungeon }
    public enum SkillCategory { Damage, Control, Mobility, Buff, Defense, Healing }

    [Serializable]
    public class ItemData
    {
        public string id;
        public string name;
        public ItemSlot slot;
        public Rarity rarity;
        public int level;
        public int attack;
        public int defense;
        public int health;
        public int upgradeLevel;
        public EquipmentMechanic mechanic;
        public bool locked;
        public int mechanicVariant;
        public bool mechanicVariantUnlocked;
        public int balanceRevision;
        // Persistent origins make changing a rank reversible without repeatedly
        // rounding already-upgraded attributes. Legacy saves initialize these once.
        public bool upgradeBaseInitialized;
        public int baseAttack;
        public int baseDefense;
        public int baseHealth;
        public int upgradeAnchorLevel;
        public int upgradeAnchorAttack;
        public int upgradeAnchorDefense;
        public int upgradeAnchorHealth;
    }

    /// <summary>Two bounded, character-local build descriptions. Item IDs are references
    /// to owned equipment, never copies of inventory or currency.</summary>
    [Serializable]
    public class BuildPreset
    {
        public int version = 1;
        public bool populated;
        public HeroClass heroClass;
        public int[] skillRanks;
        public int[] masteryRanks;
        public int masteryCore = -1;
        public ElementalistSpecialization specialization;
        public SummonerRoute summonerRoute;
        public int[] equippedSkills;
        public int[] hotbarKeys;
        public int hotbarPage;
        public string weaponId, armorId, relicId;
        // Null in legacy presets: preserve current item variants. -1 means no unlocked variant captured.
        public int[] equipmentVariants;
        public EquipmentMechanic[] equipmentMechanics;
        // A zero enum entry is not evidence: each slot explicitly records whether its mechanism is known.
        public int equipmentMechanicKnownMask;
    }

    /// <summary>Fixed class-local allocations. Inventory, equipment identity and rewards live only on GameProfile.</summary>
    [Serializable]
    public sealed class ClassBuildState
    {
        public int version=1;
        public bool initialized;
        public HeroClass heroClass;
        public int[] skillRanks,masteryRanks,equippedSkills,hotbarKeys;
        public int masteryCore=-1,hotbarPage,tutorialMask;
        public ElementalistSpecialization specialization;
        public SummonerRoute summonerRoute;
        public BuildPreset[] buildPresets;
        public bool classTutorialCompleted;
        public ProgressionGoalKind progressionGoal;
        public string progressionGoalItemId;
        public int progressionGoalTier,progressionGoalLevel;
        public EquipmentMechanic progressionGoalMechanic;
        public Rarity progressionGoalMinimumRarity;
    }

    public enum ProgressionGoalKind { None, Core, Variant, Ascension, SecondPreset, Tier, Reforge, ClassTutorial }

    [Serializable]
    public class GameProfile
    {
        public int classStateRevision;
        public ClassBuildState[] classStates;
        public int version = 1;
        public HeroClass heroClass;
        public int level = 1;
        public int xp;
        public int gold = 60;
        public int potions = 5;
        public int skillPoints;
        public int tutorialMask;
        public bool classTutorialCompleted;
        public ProgressionGoalKind progressionGoal;
        public string progressionGoalItemId;
        public int progressionGoalTier;
        public EquipmentMechanic progressionGoalMechanic;
        public Rarity progressionGoalMinimumRarity;
        public int progressionGoalLevel;
        public int highestAdventureTier;
        public int chapterMasteryMask;
        public int[] chapterMasteryTiers = new int[4];
        public int chapterRevision;
        public int chapterCompletedMask;
        public int chapterFirstRewardMask;
        // Six independent node/difficulty first rewards; revision gates legacy backfill.
        public int chapterDifficultyRewardMask;
        public int chapterDifficultyRewardRevision;
        public int[] chapterHighestDifficulties = new int[3];
        public long chapterRewardSequence;
        public string lastChapterRewardId;
        public int chapterHighestAdventureTier;
        public int chapterPriorAdventureTier;
        public ElementalistSpecialization specialization;
        public int[] masteryRanks = new int[4];
        public int masteryCore = -1;
        public int masteryRevision;
        public SummonerRoute summonerRoute;
        public BuildPreset[] buildPresets = { new BuildPreset(), new BuildPreset() };
        public int fashionThreads;
        // Slot training is the sole enhancement authority. Item upgradeLevel is
        // only the current equipped-stat cache; legacy anchors preserve item bases.
        public int[] slotUpgradeRanks = new int[3];
        public bool slotUpgradesInitialized;
        public int mechanicMaterials;
        public int variantKnowledgeRevision;
        public List<EquipmentMechanic> variantKnowledge = new List<EquipmentMechanic>();
        public int materialRewardedClears;
        public bool firstClearRewardClaimed;
        public bool pendingFirstClearReward;
        public List<EquipmentMechanic> discoveredMechanics = new List<EquipmentMechanic>();
        public List<ItemData> pendingLoot = new List<ItemData>();
        public List<ItemData> recoveryLoot = new List<ItemData>();
        public bool autoSellCommon;
        public bool autoSellRare;
        public int[] skillRanks = new int[GameBalance.SkillCount];
        public int[] equippedSkills = GameBalance.DefaultLoadout();
        public int hotbarPage;
        public int[] hotbarKeys = (int[])GameBalance.DefaultHotbarKeys.Clone();
        public int kills;
        public int clearedRuns;
        public int bestFloor;
        public string lastModeRewardId;
        public string lastDungeonRewardId;
        public RewardPresentationReceipt lastModeRewardDetails,lastDungeonRewardDetails,lastChapterRewardDetails;
        public List<string> sideEventRewardReceipts = new List<string>();
        public int currentHub;
        public int unlockedHubMask=1;
        public List<ItemData> inventory = new List<ItemData>();
        public string weaponId;
        public string armorId;
        public string relicId;
        public List<FashionData> fashions = new List<FashionData>();
        public string wingsFashionId;
        public string weaponFashionId;
        public int chestRulesRevision,pendingChestRulesRevision;public bool pendingChestLegacyGoldProtection;
        public string pendingChestQualificationId;public ChestReward pendingChestDraw;
        public long threadMaterialSequence;public MaterialExchangeReceipt lastThreadMaterialReceipt;
        public bool pendingFashionChest;
        public int pendingChestTier = 1;
        public ChestReward lastChestReward;
        public bool pendingChestReveal;
    }

    public struct StatBlock
    {
        public float MaxHealth;
        public float Damage;
        public float Armor;
        public float MoveSpeed;
        public float CritChance;
    }

    public static class GameBalance
    {
        public const int SkillCount = 10;
        public const int HotbarSize = 10;
        public const int HotbarPotion = -2;
        public const int HotbarPages = 3;
        public static readonly int[] DefaultHotbarKeys = { 122, 120, 99, 118, 98, 49, 50, 51, 52, 53 };
        public static readonly string[] ClassNames = { "剑卫", "元素师", "游侠", "唤灵师" };
        public static readonly string[] ClassDescriptions = {
            "挥剑近战 · 旋风斩击 · 坚韧生存",
            "连击霜印 · 冰霜控制 · 陨星爆发",
            "灵巧射击 · 散射箭雨 · 灵活游走",
            "契约召唤 · 灵狼星灵 · 协同作战"
        };
        public static readonly Color[] ClassColors = {
            new Color(1f, .65f, .26f), new Color(.4f, .7f, 1f), new Color(.38f, .88f, .65f), new Color(.83f, .63f, 1f)
        };
        public static readonly int[] SkillRequiredLevels = { 1, 4, 6, 4, 10, 6, 13, 20, 13, 30 };
        public static readonly int[][] SkillPrerequisites = {
            new int[0], new[] { 0 }, new[] { 1 }, new[] { 0 }, new[] { 2 },
            new[] { 3 }, new[] { 5 }, new[] { 4 }, new[] { 5 }, new[] { 7, 6 }
        };
        public static int SkillTreeRow(int skill) { return new[] { 0, 1, 2, 1, 3, 2, 4, 5, 4, 6 }[skill]; }
        public static float SkillTreeColumn(int skill) { return new[] { 1f, 0f, 0f, 2f, 0f, 2f, 1f, 0f, 2f, 1f }[skill]; }
        public static string PrerequisiteDescription(HeroClass hero, int skill)
        {
            int[] parents = SkillPrerequisites[skill];
            if (parents.Length == 0) return "起始技能 · 无前置";
            string result = "前置：";
            for (int i = 0; i < parents.Length; i++) result += (i == 0 ? "" : " + ") + SkillName(hero, parents[i]);
            return result;
        }
        // Budgets reflect damage coverage, control uptime and protection, rather
        // than position in the skill tree. IDs stay stable for existing saves.
        // Dash and chain attacks cycle faster than persistent fields; shields
        // leave an exposed interval even at rank three. Summons have a hard cap.
        private static readonly float[,] ClassSkillCooldowns = {
            { 5f, 9f, 18f, 0f, 28f, 11f, 32f, 16f, 0f, 42f },
            { 10f, 11f, 20f, 0f, 8f, 26f, 34f, 22f, 0f, 45f },
            { 6f, 12f, 19f, 0f, 12f, 18f, 32f, 16f, 0f, 40f },
            { 7f, 16f, 14f, 0f, 12f, 28f, 36f, 21f, 0f, 46f }
        };
        private static readonly float[,] ClassSkillEnergyCosts = {
            { 12f, 20f, 34f, 0f, 30f, 24f, 28f, 38f, 0f, 64f },
            { 18f, 28f, 38f, 0f, 24f, 28f, 30f, 42f, 0f, 68f },
            { 14f, 20f, 34f, 0f, 22f, 30f, 28f, 42f, 0f, 64f },
            { 16f, 28f, 30f, 0f, 28f, 32f, 36f, 40f, 0f, 70f }
        };
        private static readonly string[,] SkillNames = {
            { "旋风斩", "裂地冲击", "剑刃风暴", "剑术精研", "圣盾反击", "破军突进", "生命战旗", "大地崩裂", "不屈意志", "终焉裁决" },
            { "冰霜新星", "陨星术", "奥术风暴", "奥能亲和", "雷霆锁链", "冰晶护体", "奥术回流", "虚空漩涡", "法力屏障", "天灾终章" },
            { "扇形箭", "震荡陷阱", "天幕箭雨", "弱点洞察", "逐风步", "毒蔓牢笼", "森林祈愿", "幻影连射", "灵风庇佑", "万箭归星" },
            { "灵能冲击", "荆棘牢笼", "灵狼契约", "灵魂共鸣", "星灵契约", "灵魂护盾", "回春共鸣", "引力印记", "灵体庇护", "远古树灵" }
        };
        private static readonly string[,] SkillDescriptions = {
            {
                "旋转斩击周围敌人。低消耗、短冷却，觉醒后牵引收束。", "向前方重击，击退并击倒敌人；可打断带青色符号的首领预警，首领打断后5秒免疫再次打断。", "连续释放剑气，切割周围的敌人。",
                "被动：永久提高攻击与防御，学习后自动生效。", "展开护盾减轻伤害，并以圣光反击周围敌人。", "向前突进并连续斩击，撕开敌阵。",
                "树立生命战旗，持续恢复生命；升阶获得防护与回复能量。", "沿前方逐段引爆地脉，将普通/精英敌人击飞；首领免疫浮空，仍受到伤害。", "被动：濒危时自动触发减伤防护；触发后有独立内置冷却。",
                "巨剑裁决与多段剑阵爆发，终结大范围敌群。集中消耗战意，适合聚怪后的爆发。"
            },
            {
                "冻结普通/精英敌人，解冻后暂时减速40%；首领保留霜痕，并可打断其带青色符号的预警。", "在瞄准地点降下陨星，造成范围爆发；主冲击可打断带青色符号的首领预警。", "在瞄准地点制造持续的奥术风暴。",
                "被动：永久提高法术攻击与生命，学习后自动生效。", "雷霆在敌人间跳跃，逐个造成伤害并短暂眩晕。", "碎冰/均衡护盾维持霜痕；灼燃护盾减伤30%，移速+20%并留下火区。",
                "回收奥术之力，持续恢复生命；升阶获得防护与回复能量。", "创造虚空漩涡，将敌人吸向中心并反复撕裂。", "被动：受伤时自动生成法力屏障并回复少量能量，具有内置冷却。",
                "多重星环汇聚，陨星与雷霆引爆整片战场。高奥能消耗，兼顾范围伤害与控制。"
            },
            {
                "向前方发射多支穿透箭矢。短冷却，适合清理敌群。", "在瞄准地点引爆陷阱，伤害并眩晕敌人；主爆炸可打断带青色符号的首领预警。", "向瞄准地点持续倾泻箭雨。",
                "被动：永久提高暴击几率和移动速度，学习后自动生效。", "后撤脱离危险，同时获得机动增益并释放追击箭矢。", "毒蔓使敌人减速并叠加中毒，最多3层；离开毒区后毒伤仍会持续。",
                "召唤自然之力持续恢复生命；升阶获得防护与回复能量。", "锁定选区内的一名敌人持续追射，并使其受到的伤害提高12%至20%。目标死亡后不自动转锁。", "被动：受伤时自动短暂无敌并提高移动速度，具有内置冷却。",
                "星弓展开，巨量光羽与箭雨汇聚于目标。集中消耗专注，适合敌群密集时使用。"
            },
            {
                "向前方释放灵能冲击，造成扇形伤害并将敌人轰开；可打断带青色符号的首领预警。", "在目标地点生长荆棘，使敌人减速并持续中毒；首领减速效果降低。", "升级常驻灵狼；存活时命令扑击并强化3秒，死亡时重召。群契路线追加限时伙伴；普通上限4、双契装备上限2。",
                "被动：提高自身攻击，召唤物继承强化后的攻击。学习后永久生效。", "双契路线召唤常驻星灵；存活时命令集火强化3秒。群契路线星灵限时存在，遵守普通召唤上限。", "灵魂护盾持续6秒，受到的伤害减少35%；升阶延长保护并增强减伤。",
                "5秒内治疗自身与召唤物，升阶获得防护与回复能量。", "在目标地点施加引力印记，持续聚拢敌人，结束时击飞普通/精英敌人；首领免疫浮空，牵引衰减。", "被动：受伤时触发灵体庇护，短暂减伤并回复能量，具有独立内置冷却。",
                "召唤一只远古树灵守卫，持续12秒，使用范围重击并击倒敌人；最多存在1只树灵。"
            }
        };
        private static readonly string[,] ReinforcedEffects = {
            { "追加一段旋斩；伤害+30%，范围+15%。", "追加震荡，延长眩晕；伤害+30%，范围+15%。", "剑刃风暴持续3秒；整段伤害单独预算，范围+15%。", "", "反击护盾持续8秒，减伤65%，反击范围扩大。", "冲锋末端追加爆发；伤害+30%，路径扩大。", "", "裂地增加至7段，逐段击飞；伤害+30%，范围+15%。", "", "裁决增加至7段；伤害+30%，范围+15%。" },
            { "释放双重新星；伤害+30%，范围+15%。", "双陨星连续落下；伤害+30%，范围+15%。", "风暴脉冲加快；整段伤害单独预算，范围+15%。", "", "连锁最多8个目标；伤害+30%，范围+15%。", "护体持续8秒；碎冰/均衡减伤45%，脉冲延长霜痕；灼燃减伤30%并机动留火区。", "", "漩涡攻击增加至11次；伤害+30%，范围+15%。", "", "天灾增加至7段；伤害+30%，范围+15%。" },
            { "增加至7支箭矢；伤害+30%，作用范围+15%。", "陷阱追加余震；伤害+30%，范围+15%。", "箭雨持续5秒；整段伤害单独预算，范围+15%。", "", "后撤射出4支穿透箭，获得6秒机动与普攻增益。", "毒区持续更久；伤害+30%，范围+15%。", "", "连续射出17支穿透箭；伤害+30%，射程扩大。", "", "箭幕增加至10段；伤害+30%，范围+15%。" },
            { "灵能冲击伤害+30%，范围+15%，击退敌人。", "荆棘伤害+30%，范围+15%，延长减速与中毒覆盖。", "常驻灵狼攻击+30%，指令强化45%；群契增援持续12秒。", "", "星灵攻击+30%，指令强化45%；双契常驻，群契持续21秒。", "护盾持续8秒，减伤45%。", "", "引力印记伤害+30%，范围+15%，结束时击飞。", "", "树灵持续14秒，重击伤害+30%，范围扩大。" }
        };
        private static readonly string[,] AwakenedEffects = {
            { "三段旋斩并牵引收束；伤害+60%，范围+35%。", "追加终结冲击与强化眩晕；伤害+60%，范围+35%。", "持续3.6秒，牵引敌人并追加收尾爆发。", "", "反击护盾持续10秒，减伤70%，多层圣光反击。", "冲锋终点爆发，起点追加残影冲击。", "", "8段裂地逐段击飞，并追加末端爆破；伤害+60%，范围+35%。", "", "8段裁决，终击留下2秒剑气领域。" },
            { "双重新星后追加8枚冰片；同目标冰片递减并设上限，扩大范围与冻结。", "双陨星后留下2秒灼烧领域。", "高速脉冲聚拢敌人，并追加收束爆发。", "", "连锁最多10个目标，每个节点追加溅射。", "护体持续10秒；碎冰/均衡减伤55%，每秒维持霜痕；灼燃减伤30%并机动留火区。", "", "13次漩涡攻击，更强吸附并追加终结爆发。", "", "8段天灾，终击留下聚怪元素领域。" },
            { "9支穿透箭，命中追加爆裂；同目标后续命中递减，伤害有上限。", "引爆前吸附敌人，随后追加余震。", "持续6秒箭雨，并在结束时追加爆发。", "", "5支追踪箭与残影陷阱，获得7秒机动与普攻增益。", "毒区吸附敌人，并在结束时追加爆裂。", "", "连续射出20支追踪穿透箭，强化覆盖能力。", "", "11段箭幕，终爆追加12支放射箭。" },
            { "灵能冲击伤害+60%，范围+35%，将敌群轰开。", "荆棘伤害+60%，范围+35%，强化毒伤与区域控制。", "常驻灵狼攻击+60%，指令强化55%；群契最多3只增援持续14秒。", "", "星灵攻击+60%，指令强化55%；双契常驻，群契持续24秒。", "护盾持续10秒，减伤55%。", "", "引力印记伤害+60%，范围+35%，聚怪后以击飞结束。", "", "树灵持续16秒，重击伤害+60%，范围扩大。" }
        };
        public static string ClassName(HeroClass value) { return ClassNames[(int)value]; }
        public static Color ClassColor(HeroClass value) { return ClassColors[(int)value]; }
        public static string SkillName(HeroClass value, int slot) { return SkillNames[(int)value, slot]; }
        public static string SkillDescription(HeroClass value, int slot) { return SkillDescriptions[(int)value, slot] + "\n" + BuildCatalog.DamageRules; }
        public static string EnergyName(HeroClass value) { return new[] { "战意", "奥能", "专注", "灵力" }[(int)value]; }
        public static bool IsPassive(int skill) { return skill == 3 || skill == 8; }
        public static int SkillPointBudget(int level) { return Math.Max(1, level - 1); }
        public static int SkillPointsGainedAtLevel(int level) { return level <= 1 ? 0 : SkillPointBudget(level) - SkillPointBudget(level - 1); }
        public static int SkillRankRequiredLevel(int skill, int rank) { return rank <= 1 ? SkillRequiredLevels[skill] : Math.Max(2, SkillRequiredLevels[skill]) + (rank == 2 ? 8 : 18); }
        public static float SkillRangeMultiplier(int rank) { return rank <= 1 ? 1f : rank == 2 ? 1.15f : 1.35f; }
        public static string SkillRankName(int rank) { return rank <= 0 ? "未习得" : rank == 1 ? "初习" : rank == 2 ? "强化" : "觉醒"; }
        public static SkillCategory GetSkillCategory(HeroClass hero, int skill)
        {
            if (skill == 3) return SkillCategory.Buff;
            if (skill == 8) return SkillCategory.Defense;
            if (skill == 6) return SkillCategory.Healing;
            if (hero == HeroClass.Vanguard)
            {
                if (skill == 1 || skill == 7) return SkillCategory.Control;
                if (skill == 4) return SkillCategory.Defense;
                if (skill == 5) return SkillCategory.Mobility;
            }
            else if (hero == HeroClass.Arcanist)
            {
                if (skill == 0 || skill == 7) return SkillCategory.Control;
                if (skill == 5) return SkillCategory.Defense;
            }
            else if (hero == HeroClass.Ranger)
            {
                if (skill == 1 || skill == 5) return SkillCategory.Control;
                if (skill == 4) return SkillCategory.Mobility;
            }
            else if (hero == HeroClass.Summoner)
            {
                if (skill == 1 || skill == 7) return SkillCategory.Control;
                if (skill == 5) return SkillCategory.Defense;
            }
            return SkillCategory.Damage;
        }
        public static string CategoryName(SkillCategory category) { return new[] { "输出", "控制", "位移", "增益", "防御", "治疗" }[(int)category]; }
        public static string SkillEvolution(HeroClass hero, int skill, int rank)
        {
            int stage = Math.Max(1, Math.Min(3, rank)) - 1;
            if (skill == 3)
            {
                if (hero == HeroClass.Vanguard) return new[] { "攻击 +8%，防御 +2。永久生效。", "攻击 +14%，防御 +4。", "攻击 +22%，防御 +7。" }[stage];
                if (hero == HeroClass.Arcanist) return new[] { "攻击 +6%，最大生命 +4%。", "攻击 +11%，最大生命 +7%。", "攻击 +18%，最大生命 +10%。" }[stage];
                if (hero == HeroClass.Summoner) return new[] { "攻击 +6%，召唤物继承自身攻击。永久生效。", "攻击 +11%，召唤物继承强化后的攻击。", "攻击 +18%，召唤物继承强化后的攻击。" }[stage];
                return new[] { "暴击 +4%，移动速度 +3%。", "暴击 +7%，移动速度 +6%。", "暴击 +12%，移动速度 +10%。" }[stage];
            }
            if (skill == 8)
            {
                if (hero == HeroClass.Vanguard) return new[] { "濒危时减伤30%，持续3秒；内置冷却45秒。", "减伤40%，持续4秒；内置冷却38秒。", "减伤50%/5秒并自动反击；内置冷却30秒。" }[stage];
                if (hero == HeroClass.Arcanist) return new[] { "受伤后减伤40%/3秒，回复4能量；冷却45秒。", "减伤50%/4秒，回复6能量；冷却38秒。", "减伤60%/5秒，回复8能量并释放冰环；冷却30秒。" }[stage];
                if (hero == HeroClass.Summoner) return new[] { "受伤后减伤35%/3秒，回复4灵力；冷却45秒。", "减伤45%/4秒，回复6灵力；冷却38秒。", "减伤55%/5秒，回复8灵力并震慑周围敌人1.5秒；冷却30秒。" }[stage];
                return new[] { "受伤后无敌0.25秒、移速+15%/3秒；冷却45秒。", "无敌0.4秒、移速+20%/4秒；冷却38秒。", "无敌0.55秒、移速+25%/5秒，回复8能量；冷却30秒。" }[stage];
            }
            if (skill == 6)
            {
                string limited = " 限疗模式：5秒内自身恢复" + new[] { "60%", "70%", "80%" }[stage] + "最大生命，消耗1次治疗充能。";
                if (hero == HeroClass.Summoner) return new[] { "5秒内为自身与召唤物恢复30%最大生命。", "5秒为自身与召唤物恢复42%生命，自身获得18%减伤。", "5秒为自身与召唤物恢复55%生命，自身获得25%减伤并回复8灵力。" }[stage] + limited + "召唤物治疗量不变。";
                return new[] { "5秒内恢复30%最大生命，不造成伤害。", "5秒恢复42%生命，获得18%减伤。", "5秒恢复55%生命，获得25%减伤并回复8能量。" }[stage] + limited;
            }
            if (stage == 0) return SkillDescription(hero, skill);
            return stage == 1 ? ReinforcedEffects[(int)hero,skill] : AwakenedEffects[(int)hero,skill];
        }
        public static float SkillCooldown(HeroClass hero, int skill)
        {
            ValidateSkillBudget(hero, skill);
            return ClassSkillCooldowns[(int)hero, skill];
        }
        public static float SkillEnergyCost(HeroClass hero, int skill)
        {
            ValidateSkillBudget(hero, skill);
            return ClassSkillEnergyCosts[(int)hero, skill];
        }
        public static float EffectiveCooldown(HeroClass hero, int skill, int rank)
        {
            return SkillCooldown(hero, skill) * (1f - (Math.Max(1, Math.Min(3, rank)) - 1) * .07f);
        }
        private static void ValidateSkillBudget(HeroClass hero, int skill)
        {
            if ((int)hero < 0 || (int)hero >= ClassSkillCooldowns.GetLength(0)) throw new ArgumentOutOfRangeException(nameof(hero));
            if (skill < 0 || skill >= SkillCount) throw new ArgumentOutOfRangeException(nameof(skill));
        }
        public static int[] DefaultLoadout()
        {
            int[] result = new int[HotbarSize * HotbarPages];
            for (int i = 0; i < result.Length; i++) result[i] = -1;
            int slot = 0;
            for (int skill = 0; skill < SkillCount; skill++) if (!IsPassive(skill)) result[slot++] = skill;
            return result;
        }
        public static bool IsBindableKey(int key)
        {
            if (key == 97 || key == 100 || key == 102 || key == 104 || key == 105 || key == 106 || key == 107 || key == 115 || key == 116 || key == 119) return false;
            return (key >= 97 && key <= 122) || (key >= 48 && key <= 57) || (key >= 282 && key <= 293);
        }
        public static string KeyName(int key)
        {
            if (key >= 97 && key <= 122) return ((char)(key - 32)).ToString();
            if (key >= 48 && key <= 57) return ((char)key).ToString();
            if (key >= 282 && key <= 293) return "F" + (key - 281);
            return "未绑定";
        }
        public static string SlotName(ItemSlot slot) { return new[] { "武器", "护甲", "饰品" }[(int)slot]; }
        public static string RarityName(Rarity rarity) { return new[] { "普通", "稀有", "史诗", "传说" }[(int)rarity]; }
        public static Color RarityColor(Rarity rarity) {
            return new[] { new Color(.76f,.8f,.84f), new Color(.35f,.65f,1f), new Color(.77f,.43f,1f), new Color(1f,.72f,.26f) }[(int)rarity];
        }
        public static int XpToNext(int level) { return 60 + (level - 1) * 30; }
    }
}
