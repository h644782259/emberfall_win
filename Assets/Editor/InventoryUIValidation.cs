using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Emberfall.Editor
{
    /// <summary>Behavior checks invoked by RuntimeValidation after a hero starts. Never starts Play mode.</summary>
    public static class InventoryUIValidation
    {
        private const string Prefix = "Emberfall.RuntimeValidation.";
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        public static int Validate(GameSession game)
        {
            RequireIsolatedRuntime(game);
            var validation = new Cases(game);
            try
            {
                validation.Run();
                return validation.Assertions;
            }
            finally
            {
                validation.Restore();
            }
        }

        private static void RequireIsolatedRuntime(GameSession game)
        {
            if (!Application.isPlaying || !EditorApplication.isPlaying || !SessionState.GetBool(Prefix + "Active", false))
                throw new InvalidOperationException("Inventory UI validation is only available during RuntimeValidation Play mode.");
            if (game == null || game != GameSession.Instance || !game.HasStarted || game.Progression == null || game.GetComponent<GameUI>() == null)
                throw new InvalidOperationException("Inventory UI validation requires the active RuntimeValidation hero and UI.");

            string saved = SessionState.GetString(Prefix + "Save", "");
            string overridden = SessionState.GetString("Emberfall.ValidationSaveDirectory", "");
            string results = SessionState.GetString(Prefix + "Results", "");
            if (string.IsNullOrWhiteSpace(saved) || string.IsNullOrWhiteSpace(overridden) || string.IsNullOrWhiteSpace(results))
                throw new InvalidOperationException("RuntimeValidation did not provide its isolated save paths.");
            string save = Full(saved);
            string result = Full(results);
            string expectedRoot = Full(Path.Combine(Application.dataPath, "..", "Tests", "TestResults"));
            string runName = Path.GetFileName(result);
            Guid runId;
            if (!Same(Path.GetDirectoryName(result), expectedRoot) || !runName.StartsWith("PlayMode-", StringComparison.Ordinal) ||
                !Guid.TryParseExact(runName.Substring("PlayMode-".Length), "N", out runId) ||
                !Same(save, Path.Combine(result, "IsolatedSave")) || !Same(save, overridden) ||
                !Same(game.Progression.SaveDirectory, save) || !IsSlotFileInDirectory(game.Progression.SaveFilePath, save))
                throw new InvalidOperationException("Inventory UI validation refused a save path outside this RuntimeValidation run.");
        }

        private static bool IsSlotFileInDirectory(string path, string directory)
        {
            if (!Same(Path.GetDirectoryName(Full(path)), directory)) return false;
            string name = Path.GetFileName(path);
            if (name == "emberfall-save.json") return true;
            const string prefix = "emberfall-save-", suffix = ".json";
            if (!name.StartsWith(prefix, StringComparison.Ordinal) || !name.EndsWith(suffix, StringComparison.Ordinal) || name.Length != prefix.Length + 32 + suffix.Length) return false;
            Guid id;
            string token = name.Substring(prefix.Length, 32);
            return Guid.TryParseExact(token, "N", out id) && token == id.ToString("N");
        }

        private static string Full(string path) { return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
        private static bool Same(string a, string b) { return string.Equals(Full(a), Full(b), StringComparison.OrdinalIgnoreCase); }
        private static FieldInfo Field(Type type, string name)
        {
            FieldInfo field = type.GetField(name, PrivateInstance);
            if (field == null) throw new MissingFieldException(type.FullName, name);
            return field;
        }
        private static object Call(object target, string name, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(name, PrivateInstance);
            if (method == null) throw new MissingMethodException(target.GetType().FullName, name);
            ParameterInfo[] parameters = method.GetParameters();
            if (arguments.Length < parameters.Length)
            {
                int supplied = arguments.Length;
                Array.Resize(ref arguments, parameters.Length);
                for (int i = supplied; i < parameters.Length; i++)
                {
                    if (!parameters[i].IsOptional) throw new TargetParameterCountException(name);
                    arguments[i] = parameters[i].DefaultValue;
                }
            }
            ItemData temporaryCandidate = null;
            GameProfile candidateProfile = null;
            if (name == "IsEquipmentUpgrade" && arguments[0] is ItemData)
            {
                var gameUI = (GameUI)target;
                candidateProfile = ((GameSession)Field(typeof(GameUI), "session").GetValue(gameUI)).Progression.Profile;
                var candidate = (ItemData)arguments[0];
                if (!candidateProfile.inventory.Contains(candidate))
                { temporaryCandidate = candidate; candidateProfile.inventory.Add(candidate); }
                gameUI.GetType().GetMethod("InvalidateAttention", PrivateInstance).Invoke(gameUI, null);
            }
            try { return method.Invoke(target, arguments); }
            catch (TargetInvocationException exception)
            {
                throw new InvalidOperationException("Inventory UI call failed: " + name, exception.InnerException ?? exception);
            }
            finally
            {
                if (temporaryCandidate != null) candidateProfile.inventory.Remove(temporaryCandidate);
                if (candidateProfile != null) target.GetType().GetMethod("InvalidateAttention", PrivateInstance).Invoke(target, null);
            }
        }
        private static void SetProperty(object target, string name, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            MethodInfo setter = property == null ? null : property.GetSetMethod(true);
            if (setter == null) throw new MissingMemberException(target.GetType().FullName, name);
            setter.Invoke(target, new[] { value });
        }

        private sealed class SavedFile
        {
            private readonly string path;
            private readonly byte[] bytes;
            public SavedFile(string value) { path = value; bytes = File.Exists(path) ? File.ReadAllBytes(path) : null; }
            public void Restore()
            {
                if (bytes != null) File.WriteAllBytes(path, bytes);
                else if (File.Exists(path)) File.Delete(path);
            }
        }

        private sealed class Cases
        {
            private readonly GameSession game;
            private readonly GameUI ui;
            private readonly ProgressionService progression;
            private readonly GameProfile originalProfile;
            private readonly string originalError;
            private readonly object originalNotification;
            private readonly object originalNotificationUntil;
            private readonly bool originalUIBlocking;
            private readonly float originalTimeScale;
            private readonly object originalPlayerStats;
            private readonly float originalHealth;
            private readonly float originalMaxHealth;
            private readonly Dictionary<string, object> originalFields = new Dictionary<string, object>();
            private readonly ItemData[] originalBag;
            private readonly ItemData[] equipped = new ItemData[3];
            private readonly SavedFile[] files;
            public int Assertions { get; private set; }
            private GameProfile Profile { get { return progression.Profile; } }
            private List<ItemData> Bag { get { return (List<ItemData>)Get("bagItems"); } }

            public Cases(GameSession session)
            {
                game = session;
                ui = game.GetComponent<GameUI>();
                progression = game.Progression;
                originalProfile = progression.Profile;
                originalError = progression.LastError;
                originalNotification = Field(typeof(GameSession), "notification").GetValue(game);
                originalNotificationUntil = Field(typeof(GameSession), "notificationUntil").GetValue(game);
                originalUIBlocking = (bool)Field(typeof(GameSession), "uiBlocking").GetValue(game);
                originalTimeScale = Time.timeScale;
                originalPlayerStats = Field(typeof(PlayerController), "stats").GetValue(game.Player);
                originalHealth = game.Player.Health;
                originalMaxHealth = game.Player.MaxHealth;
                foreach (string name in new[] { "inventoryFilter", "inventorySort", "inventoryScroll", "selectedItem", "unequippedCount", "panel", "hotbarPointerSlot", "hotbarPointerPage", "hotbarPointerSkill", "hotbarPointerConfiguring", "hotbarDragging", "hotbarPointerOrigin", "hotbarPointerControl", "hotbarReleaseFrame", "suppressHotbarMouse", "scale" })
                    originalFields[name] = Get(name);
                originalBag = Bag.ToArray();
                for (int i = 0; i < equipped.Length; i++)
                {
                    ItemData item = progression.Equipped((ItemSlot)i);
                    if (item == null) throw new InvalidOperationException("Inventory UI validation requires all three starter equipment slots.");
                    equipped[i] = Clone(item);
                }
                files = new[] { new SavedFile(progression.SaveFilePath), new SavedFile(progression.SaveFilePath + ".bak"), new SavedFile(progression.SaveFilePath + ".tmp") };
            }

            public void Run()
            {
                // The original profile object and all of its item references remain untouched.
                SetProperty(progression, "Profile", JsonUtility.FromJson<GameProfile>(JsonUtility.ToJson(originalProfile)));
                ValidateScoreAndOrdering();
                ValidateFiltersAndSelection();
                ValidateUpgradeTags();
                ValidateSalesAndScrolling();
                ValidateUpgradeReordering();
                ValidateSlotUpgradeUI();
                ValidateGoldCapAndEmptyInventory();
                ValidateHotbarMappingAndDragging();
            }

            private void ValidateHotbarMappingAndDragging()
            {
                Profile.level = 40;
                Profile.hotbarPage = 0;
                for (int i = 0; i < Profile.equippedSkills.Length; i++) Profile.equippedSkills[i] = -1;
                for (int i = 0; i < Profile.skillRanks.Length; i++) Profile.skillRanks[i] = 0;
                Profile.equippedSkills[0] = 0;
                Profile.equippedSkills[1] = 1;
                Profile.equippedSkills[2] = 3;
                Profile.skillRanks[0] = 1;
                Profile.skillRanks[3] = 1;
                MethodInfo learned = typeof(GameUI).GetMethod("LearnedSkillAtSlot", BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo label = typeof(GameUI).GetMethod("SlotSkillName", BindingFlags.Static | BindingFlags.NonPublic);
                Check((int)learned.Invoke(null, new object[] { Profile, 0 }) == 0, "mapping exposes learned active skill");
                Check((int)learned.Invoke(null, new object[] { Profile, 1 }) == -1 && (string)label.Invoke(null, new object[] { Profile, 1 }) == "未配置", "reserved unlearned preset hides its name and icon");
                Check((int)learned.Invoke(null, new object[] { Profile, 2 }) == -1 && (int)learned.Invoke(null, new object[] { Profile, -1 }) == -1, "mapping rejects passive and invalid slots");
                Profile.skillRanks[1] = 1;
                Set("panel", Enum.Parse(Field(typeof(GameUI), "panel").FieldType, "None"));
                Set("scale", 1f);
                game.SetUIBlocking(false);
                float energy = game.Player.Energy;
                float cooldown = game.Player.SkillCooldownRemaining(0);
                Call(ui, "BeginHotbarPointer", 0, Vector2.zero, false);
                Check((int)Get("hotbarPointerSlot") == 0 && !(bool)Get("hotbarDragging") && game.Player.Energy == energy && game.Player.SkillCooldownRemaining(0) == cooldown, "mouse down captures without casting");
                Call(ui, "ContinueHotbarPointer", new Vector2(5, 0));
                Check(!(bool)Get("hotbarDragging"), "less than six screen pixels remains a click candidate");
                Call(ui, "ContinueHotbarPointer", new Vector2(7, 0));
                Check((bool)Get("hotbarDragging") && ui.IsPointerOverUI, "drag threshold captures pointer even outside the HUD");
                Call(ui, "CompleteHotbarPointer", 1);
                Check(Profile.equippedSkills[0] == 1 && Profile.equippedSkills[1] == 0, "HUD drag swaps occupied learned slots");
                Check(game.Player.Energy == energy && game.Player.SkillCooldownRemaining(0) == cooldown, "drag swap neither casts nor resets cooldown");
                Call(ui, "BeginHotbarPointer", 1, Vector2.zero, false);
                Call(ui, "ContinueHotbarPointer", new Vector2(9, 0));
                Call(ui, "CompleteHotbarPointer", 4);
                Check(Profile.equippedSkills[1] == -1 && Profile.equippedSkills[4] == 0, "drag to empty slot moves skill");
                string beforeCancel = JsonUtility.ToJson(Profile);
                Call(ui, "BeginHotbarPointer", 4, Vector2.zero, false);
                Call(ui, "ContinueHotbarPointer", new Vector2(12, 0));
                Call(ui, "CompleteHotbarPointer", -1);
                Check(JsonUtility.ToJson(Profile) == beforeCancel && (int)Get("hotbarPointerSlot") == -1, "outside release cancels without a loadout mutation");
                Check(ui.IsPointerOverUI, "release frame remains blocked from world basic attacks");
                Call(ui, "BeginHotbarPointer", 4, Vector2.zero, false);
                Call(ui, "ContinueHotbarPointer", new Vector2(12, 0));
                Profile.hotbarPage = 1;
                Call(ui, "CompleteHotbarPointer", 5);
                Check(Profile.equippedSkills[4] == 0 && Profile.equippedSkills[15] == -1, "changing page invalidates an in-flight drag");
                Check((string)label.Invoke(null, new object[] { Profile, 4 }) == "未配置", "mapping follows current page independently");
                Profile.hotbarPage = 0;
                Set("panel", Enum.Parse(Field(typeof(GameUI), "panel").FieldType, "Skills"));
                game.SetUIBlocking(true);
                Call(ui, "BeginHotbarPointer", 4, Vector2.zero, true);
                Call(ui, "ContinueHotbarPointer", new Vector2(12, 0));
                Call(ui, "CompleteHotbarPointer", 5);
                Check(Profile.equippedSkills[4] == -1 && Profile.equippedSkills[5] == 0, "skill detail uses same drag move behavior while combat is paused");
                Check(progression.Load() && Profile.hotbarPage == 0 && Profile.equippedSkills[5] == 0, "drag arrangement persists through actual save/load");
                Set("panel", Enum.Parse(Field(typeof(GameUI), "panel").FieldType, "None"));
                game.SetUIBlocking(false);
                Profile.potions = 0;
                Check(progression.AssignConsumable(9), "empty potion supply can still be configured in hotbar");
                Check((int)learned.Invoke(null, new object[] { Profile, 9 }) == GameBalance.HotbarPotion && (string)label.Invoke(null, new object[] { Profile, 9 }) == "生命药剂", "potion mapping keeps its icon/name when quantity is zero");
                Call(ui, "BeginHotbarPointer", 9, Vector2.zero, false);
                Call(ui, "ContinueHotbarPointer", new Vector2(12, 0));
                Check((bool)Get("hotbarDragging"), "potion is a draggable hotbar item despite its negative identifier");
                Call(ui, "CompleteHotbarPointer", 5);
                Check(Profile.equippedSkills[5] == GameBalance.HotbarPotion && Profile.equippedSkills[9] == 0, "potion drag swaps with an occupied learned skill");
                Call(ui, "BeginHotbarPointer", 5, Vector2.zero, false);
                Call(ui, "ContinueHotbarPointer", new Vector2(12, 0));
                Call(ui, "CompleteHotbarPointer", 4);
                Check(Profile.equippedSkills[4] == GameBalance.HotbarPotion && Profile.equippedSkills[5] == -1 && Profile.potions == 0, "moving a potion to an empty slot does not consume it");
                Check(progression.Load() && Profile.equippedSkills[4] == GameBalance.HotbarPotion, "potion HUD arrangement persists through actual save/load");
                Set("panel", Enum.Parse(Field(typeof(GameUI), "panel").FieldType, "PotionAssignment"));
                game.SetUIBlocking(true);
                Call(ui, "ClosePanel");
                Check(Get("panel").ToString() == "Inventory" && game.InputBlocked, "closing potion slot picker returns to inventory and keeps combat paused");
            }

            private void ValidateScoreAndOrdering()
            {
                ItemData score = Item("score", ItemSlot.Weapon, 1, Rarity.Common, 2, 4, 30);
                score.upgradeLevel = 8;
                Check(Mathf.Approximately(ProgressionService.EquipmentScore(score), 28), "score weights actual attack, defense and health exactly once");
                score.upgradeLevel = 1;
                Check(Mathf.Approximately(ProgressionService.EquipmentScore(score), 28), "upgrade counter does not double-count already upgraded stats");
                ResetItems(
                    Item("weapon-strong", ItemSlot.Weapon, 5, Rarity.Common, 100),
                    Item("armor-level", ItemSlot.Armor, 90, Rarity.Rare, 0, 10, 50),
                    Item("relic-legend", ItemSlot.Relic, 10, Rarity.Legendary, 1),
                    Item("tie-b", ItemSlot.Weapon, 20, Rarity.Epic, 10),
                    Item("tie-a", ItemSlot.Weapon, 20, Rarity.Epic, 10),
                    Item("weapon-level", ItemSlot.Weapon, 25, Rarity.Rare, 10),
                    Item("armor-rare", ItemSlot.Armor, 20, Rarity.Rare, 10),
                    Item("armor-legend", ItemSlot.Armor, 20, Rarity.Legendary, 10));
                Set("inventorySort", 0);
                Rebuild();
                Order("score descending with level, rarity and ordinal-ID ties", "weapon-strong", "weapon-level", "armor-legend", "tie-a", "tie-b", "armor-rare", "armor-level", "relic-legend");
                Profile.inventory.Reverse();
                Rebuild();
                Order("identical sort survives opposite source-list order", "weapon-strong", "weapon-level", "armor-legend", "tie-a", "tie-b", "armor-rare", "armor-level", "relic-legend");
                Set("inventorySort", 1);
                Rebuild();
                Order("level descending with rarity and stable-ID ties", "armor-level", "weapon-level", "armor-legend", "tie-a", "tie-b", "armor-rare", "relic-legend", "weapon-strong");
                Set("inventorySort", 2);
                Rebuild();
                Order("rarity descending with level and stable-ID ties", "armor-legend", "relic-legend", "tie-a", "tie-b", "armor-level", "weapon-level", "armor-rare", "weapon-strong");
            }

            private void ValidateFiltersAndSelection()
            {
                Set("inventorySort", 0);
                Set("inventoryFilter", -1);
                Rebuild();
                Check(Bag.Count == 8 && (int)Get("unequippedCount") == 8, "all filter contains every unequipped item exactly once");
                foreach (ItemData item in equipped) Check(!Bag.Exists(value => value.id == item.id), "equipped slot excluded from sale list: " + item.slot);
                Set("inventoryFilter", 0);
                Rebuild();
                Order("weapon filter", "weapon-strong", "weapon-level", "tie-a", "tie-b");
                Set("selectedItem", "tie-a");
                Set("inventoryFilter", 1);
                Rebuild();
                Order("armor filter", "armor-legend", "armor-rare", "armor-level");
                ItemData selected = Resolve();
                Check(selected != null && selected.id == "armor-legend", "hidden previous selection resolves to the first visible item");
                Check((int)Get("unequippedCount") == 8, "filtered count does not alter total unequipped count");
                Set("selectedItem", equipped[0].id);
                Check(Resolve().id == equipped[0].id, "visible equipped selection remains inspectable while filtering bag items");
                int count = Profile.inventory.Count;
                int gold = Profile.gold;
                Call(ui, "SellInventoryItem", "tie-a");
                Check(Profile.inventory.Count == count && Profile.gold == gold, "sale entry rejects items outside current filter");
                Bag.Add(Profile.inventory.Find(value => value.id == equipped[0].id));
                Call(ui, "SellInventoryItem", equipped[0].id);
                Check(Profile.inventory.Count == count && Profile.gold == gold, "sale entry protects equipped items even in a stale list");
                Set("inventoryFilter", 2);
                Rebuild();
                Order("relic filter", "relic-legend");
                Set("selectedItem", "missing-item");
                Check(Resolve().id == "relic-legend", "missing selection resolves to a valid filtered item");
            }

            private void ValidateUpgradeTags()
            {
                ResetItems();
                ItemData weapon = Profile.inventory.Find(item => item.id == Profile.weaponId);
                ItemData armor = Profile.inventory.Find(item => item.id == Profile.armorId);
                ItemData relic = Profile.inventory.Find(item => item.id == Profile.relicId);
                weapon.attack = 20; weapon.defense = 0; weapon.health = 0;
                armor.attack = 0; armor.defense = 1; armor.health = 0;
                relic.attack = 100; relic.defense = 0; relic.health = 0;
                ItemData betterWeapon = Item("tag-better", ItemSlot.Weapon, 1, Rarity.Common, 21);
                Check((bool)Call(ui, "IsEquipmentUpgrade", betterWeapon), "upgrade tag appears for higher actual score in the same equipment slot");
                Check(!(bool)Call(ui, "IsEquipmentUpgrade", Item("tag-equal", ItemSlot.Weapon, 1, Rarity.Legendary, 20)), "equal same-slot score never receives an upgrade tag, even at higher rarity");
                Check(!(bool)Call(ui, "IsEquipmentUpgrade", Item("tag-lower", ItemSlot.Weapon, 1, Rarity.Legendary, 10)), "lower weapon score is not compared against weaker armor");
                Check((bool)Call(ui, "IsEquipmentUpgrade", Item("tag-armor", ItemSlot.Armor, 1, Rarity.Common, 10)), "better armor is compared only with armor, not the stronger weapon or relic");
                Check(!(bool)Call(ui, "IsEquipmentUpgrade", weapon) && !(bool)Call(ui, "IsEquipmentUpgrade", new object[] { null }), "equipped and null items never receive upgrade tags");
                Profile.relicId = null;
                Check((bool)Call(ui, "IsEquipmentUpgrade", Item("tag-empty", ItemSlot.Relic, 1, Rarity.Common, 1)), "an empty equipment slot is identified as an upgrade opportunity");
                int level = Profile.level;
                try
                {
                    Profile.level = 1;
                    betterWeapon.level = 20;
                    Check(!(bool)Call(ui, "IsEquipmentUpgrade", betterWeapon), "level-locked equipment does not receive an actionable upgrade badge");
                    string hint = (string)Call(ui, "EquipmentUpgradeHint", betterWeapon);
                    Check(hint.Contains("20") && hint.Contains("等级不足"), "higher-level upgrade hint states required level and current restriction");
                }
                finally { Profile.level = level; }
                // No commit occurs while testing temporary equipment stats; the next case resets all three slots.
            }

            private void ValidateSalesAndScrolling()
            {
                var items = new List<ItemData>();
                for (int i = 0; i < 10; i++) items.Add(Item("sale-" + i.ToString("00"), ItemSlot.Weapon, 5, Rarity.Common, 100 - i * 5));
                items.Add(Item("kept-armor", ItemSlot.Armor, 1, Rarity.Legendary, 0, 400));
                ResetItems(items.ToArray());
                Set("inventoryFilter", 0);
                Set("inventorySort", 2);
                Rebuild();
                Set("selectedItem", "sale-03");
                Set("inventoryScroll", new Vector2(0, 99999));
                int gold = Profile.gold;
                int price = progression.SellValue(Bag[3]);
                Call(ui, "SellInventoryItem", "sale-03");
                Check(Profile.gold == gold + price && !Profile.inventory.Exists(item => item.id == "sale-03"), "UI sale removes item and grants exact quoted gold");
                Check(game.Notification.Contains("sale-03") && game.Notification.Contains("+" + price + " 金币"), "sale feedback names item and actual gold gain");
                Check(string.IsNullOrEmpty(progression.LastError), "UI sale writes its isolated save without errors");
                Check((int)Get("inventoryFilter") == 0 && (int)Get("inventorySort") == 2, "sale preserves non-default filter and sort");
                Check((string)Get("selectedItem") == "sale-04", "selling selected middle row selects its following neighbor");
                Order("sale rebuilds visible sorted list", "sale-00", "sale-01", "sale-02", "sale-04", "sale-05", "sale-06", "sale-07", "sale-08", "sale-09");
                // The current bag viewport is 330 units tall, with 76-unit rows and 4 units of padding.
                Check(Mathf.Approximately(((Vector2)Get("inventoryScroll")).y, 358), "sale clamps excessive scrolling to the shortened list");
                Call(ui, "SellInventoryItem", "sale-00");
                Check((string)Get("selectedItem") == "sale-04", "selling another row preserves selected item identity");
                Set("selectedItem", "sale-09");
                Call(ui, "SellInventoryItem", "sale-09");
                Check((string)Get("selectedItem") == "sale-08", "selling final row selects the previous neighbor without out-of-range access");
                while (Bag.Count > 0)
                {
                    string id = Bag[0].id;
                    Set("selectedItem", id);
                    Call(ui, "SellInventoryItem", id);
                }
                Check(Profile.inventory.Count == 4 && Profile.inventory.Exists(item => item.id == "kept-armor"), "selling filtered items preserves equipment and other categories");
                Check((int)Get("inventoryFilter") == 0 && (int)Get("inventorySort") == 2, "empty filtered bag retains view preferences");
                Check((string)Get("selectedItem") == equipped[0].id && Resolve().id == equipped[0].id, "empty filtered bag falls back to valid equipped detail");
                Check(Mathf.Approximately(((Vector2)Get("inventoryScroll")).y, 0), "empty filtered bag resets vertical scroll to zero");
                gold = Profile.gold;
                Call(ui, "SellInventoryItem", "sale-03");
                Call(ui, "SellInventoryItem", "unknown");
                Check(Profile.gold == gold && Profile.inventory.Count == 4, "repeated and unknown sale IDs cannot grant gold twice");
            }

            private void ValidateUpgradeReordering()
            {
                ResetItems(Item("leader", ItemSlot.Weapon, 1, Rarity.Common, 21), Item("upgrade-me", ItemSlot.Armor, 1, Rarity.Common, 0, 34));
                Set("selectedItem", "upgrade-me");
                Order("score order before enhancement", "leader", "upgrade-me");
                Check(progression.Upgrade("upgrade-me"), "fixture enhancement succeeds through production economy");
                Rebuild();
                Order("score order updates using prospective slot enhancement", "upgrade-me", "leader");
                Check(Resolve().id == "upgrade-me", "enhancement reordering preserves item selection");
                Check(Resolve().upgradeLevel == 0 && progression.SlotUpgradeRank(ItemSlot.Armor) == 1, "upgrading a bag selection trains its slot without mutating the bag item");
            }

            private void ValidateSlotUpgradeUI()
            {
                ItemData worn = Item("slot-worn", ItemSlot.Armor, 1, Rarity.Common, 0, 20, 100);
                ItemData target = Item("slot-target", ItemSlot.Armor, 1, Rarity.Rare, 0, 25, 200);
                ItemData weaker = Item("slot-weaker", ItemSlot.Armor, 1, Rarity.Common, 0, 10, 80);
                ResetItems(worn, target, weaker);
                Profile.armorId = worn.id;
                Profile.inventory.RemoveAll(item => item.id == equipped[1].id);
                for (int i = 0; i < 5; i++) Check(progression.Upgrade(worn.id), "prepare persistent armor slot through real upgrade " + (i + 1));
                Set("inventoryFilter", 1);
                Set("inventorySort", 0);
                Set("selectedItem", target.id);
                Call(ui, "ReturnToInventory");
                int gold = Profile.gold;
                int itemCount = Profile.inventory.Count;
                string targetBefore = JsonUtility.ToJson(target);
                ItemData preview = (ItemData)Call(ui, "EquipmentPreview", target);
                ItemData expected = progression.PreviewUpgrade(target, 5);
                Check(progression.SlotUpgradeRank(ItemSlot.Armor) == 5 && preview.upgradeLevel == 5 &&
                    preview.attack == expected.attack && preview.defense == expected.defense && preview.health == expected.health,
                    "candidate detail uses its own attributes at the persistent slot rank");
                Check(JsonUtility.ToJson(target) == targetBefore && Profile.gold == gold, "comparison preview does not change source equipment or gold");
                Check(ProgressionService.EquipmentScore(preview) > ProgressionService.EquipmentScore(worn) && (bool)Call(ui, "IsEquipmentUpgrade", target),
                    "upgrade badge compares the candidate after inheriting the persistent slot rank");
                Check(!(bool)Call(ui, "IsEquipmentUpgrade", weaker), "inferior baseline stays inferior after inheriting the same rank");
                ItemData staleRank = progression.PreviewUpgrade(Item("stale-item-rank", ItemSlot.Armor, 1, Rarity.Common, 0, 14, 80), 9);
                string staleBefore = JsonUtility.ToJson(staleRank);
                Check(ProgressionService.EquipmentScore(staleRank) > ProgressionService.EquipmentScore(worn) &&
                    !(bool)Call(ui, "IsEquipmentUpgrade", staleRank) && (int)Call(ui, "CompareInventoryItems", target, staleRank) < 0,
                    "stale per-item rank cannot inflate upgrade badges or prospective score ordering");
                Check(JsonUtility.ToJson(staleRank) == staleBefore, "badge and score sorting previews leave legacy candidate metadata untouched");
                Check(!(bool)Call(ui, "IsEquipmentUpgrade", Item("equal-slot-baseline", ItemSlot.Armor, 1, Rarity.Legendary, 0, 20, 100)),
                    "equal post-inheritance score never gains an upgrade badge from rarity alone");
                Check(Mathf.Approximately((float)Call(ui, "EquipmentPreviewScore", target), ProgressionService.EquipmentScore(preview)),
                    "score sorting and detail share the same prospective enhancement basis");
                Order("same-slot candidates sort by prospective score", target.id, weaker.id);
                Check(Get("panel").ToString() == "Inventory" && game.InputBlocked, "slot upgrades stay in the paused inventory without a transfer workflow");
                Check(Array.IndexOf(Enum.GetNames(Field(typeof(GameUI), "panel").FieldType), "UpgradeTransfer") < 0 &&
                    typeof(GameUI).GetMethod("OpenUpgradeTransfer", PrivateInstance) == null,
                    "manual transfer panel and entry point are removed");

                float healthBefore = game.Player.MaxHealth * .5f;
                SetProperty(game.Player, "Health", healthBefore);
                float maximumBefore = game.Player.MaxHealth;
                Check(progression.Equip(target.id), "equipping replacement succeeds without a separate inheritance action");
                worn = Profile.inventory.Find(item => item.id == "slot-worn");
                target = Profile.inventory.Find(item => item.id == "slot-target");
                weaker = Profile.inventory.Find(item => item.id == "slot-weaker");
                Rebuild();
                Check(target.upgradeLevel == 5 && target.defense == preview.defense && target.health == preview.health && worn.upgradeLevel == 0,
                    "equipped replacement matches the preview and outgoing gear returns to its own baseline");
                Check(Profile.gold == gold && Profile.inventory.Count == itemCount && progression.SlotUpgradeRank(ItemSlot.Armor) == 5,
                    "automatic inheritance is free and preserves the slot rank and item count");
                Check((int)Get("inventoryFilter") == 1 && (int)Get("inventorySort") == 0 && Resolve().id == target.id,
                    "automatic inheritance preserves filter, sort and selected equipment");
                Check(game.Player.MaxHealth > maximumBefore && Mathf.Approximately(game.Player.MaxHealth, progression.GetStats().MaxHealth) && Mathf.Approximately(game.Player.Health, healthBefore),
                    "stronger inherited equipment raises maximum health without healing");
                SetProperty(game.Player, "Health", game.Player.MaxHealth - 1);
                float injuredHealth = game.Player.Health;
                Check(progression.Equip(worn.id), "previous equipment can be re-equipped with the persistent rank");
                worn = Profile.inventory.Find(item => item.id == "slot-worn");
                target = Profile.inventory.Find(item => item.id == "slot-target");
                weaker = Profile.inventory.Find(item => item.id == "slot-weaker");
                Check(worn.upgradeLevel == 5 && target.upgradeLevel == 0 && progression.SlotUpgradeRank(ItemSlot.Armor) == 5 &&
                    Mathf.Approximately(game.Player.Health, Mathf.Min(injuredHealth, game.Player.MaxHealth)),
                    "swapping back keeps enhancement and only clamps health downward");
                Check(progression.Equip(target.id), "replacement can be equipped repeatedly");
                target = Profile.inventory.Find(item => item.id == "slot-target");
                string repeatBefore = JsonUtility.ToJson(target);
                Check(progression.Equip(target.id) && JsonUtility.ToJson(Profile.inventory.Find(item => item.id == target.id)) == repeatBefore && Profile.gold == gold,
                    "repeated equip cannot compound inherited attributes or charge gold");
                worn = Profile.inventory.Find(item => item.id == "slot-worn");
                target = Profile.inventory.Find(item => item.id == "slot-target");
                weaker = Profile.inventory.Find(item => item.id == "slot-weaker");

                int cost = progression.UpgradeCost(weaker);
                Check(cost == progression.UpgradeCost(target), "upgrade price depends on slot rank rather than selected item rank");
                Check(progression.Upgrade(weaker.id) && progression.SlotUpgradeRank(ItemSlot.Armor) == 6 && target.upgradeLevel == 6 && weaker.upgradeLevel == 0 && Profile.gold == gold - cost,
                    "upgrading any selected bag item trains its slot and improves the worn item immediately");
                Check(progression.SlotUpgradeRank(ItemSlot.Weapon) == 0 && progression.SlotUpgradeRank(ItemSlot.Relic) == 0,
                    "slot training leaves other equipment slots unchanged");
                Rebuild();
                Call(ui, "SellInventoryItem", worn.id);
                Check(!Profile.inventory.Exists(item => item.id == worn.id) && progression.SlotUpgradeRank(ItemSlot.Armor) == 6 && target.upgradeLevel == 6,
                    "selling old equipment cannot remove the slot's accumulated reinforcement");
                for (int rank = 6; rank < ProgressionService.MaximumUpgrade; rank++) Check(progression.Upgrade(weaker.id), "train slot to cap " + (rank + 1));
                gold = Profile.gold;
                Check(progression.UpgradeCost(weaker) == 0 && !progression.Upgrade(weaker.id) && Profile.gold == gold &&
                    ((ItemData)Call(ui, "EquipmentPreview", weaker)).upgradeLevel == ProgressionService.MaximumUpgrade,
                    "bag selection uses persistent slot cap for cost, action and preview");
            }

            private void ValidateGoldCapAndEmptyInventory()
            {
                ResetItems(Item("cap-item", ItemSlot.Weapon, 1, Rarity.Common, 10));
                Profile.gold = 999999998;
                Call(ui, "SellInventoryItem", "cap-item");
                Check(Profile.gold == 999999999 && game.Notification.Contains("+1 金币"), "gold-cap sale reports actual credited amount rather than full price");
                Profile.inventory.Clear();
                Profile.weaponId = Profile.armorId = Profile.relicId = null;
                Set("selectedItem", "missing-item");
                Rebuild();
                Check(Bag.Count == 0 && Resolve() == null && Get("selectedItem") == null, "fully empty inventory resolves a null detail without indexing errors");
            }

            private void ResetItems(params ItemData[] items)
            {
                Profile.inventory = new List<ItemData>();
                Profile.slotUpgradeRanks = new int[3];
                foreach (ItemData item in equipped) Profile.inventory.Add(progression.PreviewUpgrade(item, 0));
                foreach (ItemData item in items) Profile.inventory.Add(item);
                Profile.weaponId = equipped[0].id;
                Profile.armorId = equipped[1].id;
                Profile.relicId = equipped[2].id;
                Profile.gold = 5000;
                Set("inventoryFilter", -1);
                Set("inventorySort", 0);
                Set("inventoryScroll", Vector2.zero);
                Set("selectedItem", null);
                Rebuild();
            }

            private static ItemData Item(string id, ItemSlot slot, int level, Rarity rarity, int attack, int defense = 0, int health = 0)
            {
                return new ItemData { id = id, name = id, slot = slot, level = level, rarity = rarity, attack = attack, defense = defense, health = health };
            }
            private static ItemData Clone(ItemData item) { return JsonUtility.FromJson<ItemData>(JsonUtility.ToJson(item)); }
            private object Get(string name) { return Field(typeof(GameUI), name).GetValue(ui); }
            private void Set(string name, object value) { Field(typeof(GameUI), name).SetValue(ui, value); }
            private void Rebuild() { Call(ui, "RebuildBagItems"); }
            private ItemData Resolve() { return (ItemData)Call(ui, "ResolveSelectedItem"); }
            private void Order(string label, params string[] expected)
            {
                var actual = new string[Bag.Count];
                for (int i = 0; i < Bag.Count; i++) actual[i] = Bag[i].id;
                Check(string.Join(",", actual) == string.Join(",", expected), label + " | actual: " + string.Join(",", actual));
            }
            private void Check(bool condition, string label)
            {
                if (!condition) throw new InvalidOperationException("Inventory UI validation failed: " + label);
                Assertions++;
            }

            public void Restore()
            {
                SetProperty(progression, "Profile", originalProfile);
                SetProperty(progression, "LastError", originalError);
                foreach (KeyValuePair<string, object> pair in originalFields) Set(pair.Key, pair.Value);
                Bag.Clear();
                Bag.AddRange(originalBag);
                Field(typeof(PlayerController), "stats").SetValue(game.Player, originalPlayerStats);
                SetProperty(game.Player, "MaxHealth", originalMaxHealth);
                SetProperty(game.Player, "Health", originalHealth);
                game.SetUIBlocking(originalUIBlocking);
                Time.timeScale = originalTimeScale;
                Field(typeof(GameSession), "notification").SetValue(game, originalNotification);
                Field(typeof(GameSession), "notificationUntil").SetValue(game, originalNotificationUntil);
                foreach (SavedFile file in files) file.Restore();
            }
        }
    }
}
