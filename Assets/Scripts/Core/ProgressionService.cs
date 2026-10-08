using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Emberfall
{
    public sealed class SaveSlotInfo
    {
        public string Id, DisplayName;
        public HeroClass HeroClass;
        public int Level;
        public DateTime SavedAtUtc;
        public bool CanLoad, RecoveredFromBackup, IsCurrent, DeletionPending;
    }

    /// <summary>A read-only identity snapshot. Creating it never changes files; only an explicit
    /// confirmation should pass it to DeleteSaveSlot. A changed save requires a fresh confirmation.</summary>
    public sealed class SaveDeletionRequest
    {
        public string Id { get; private set; }
        public string DisplayName { get; private set; }
        public DateTime SavedAtUtc { get; private set; }
        public bool WasCurrent { get; private set; }
        internal string Directory, Fingerprint;
        internal SaveDeletionRequest(SaveSlotInfo slot, string directory, string fingerprint)
        {
            Id = slot.Id; DisplayName = slot.DisplayName; SavedAtUtc = slot.SavedAtUtc;
            WasCurrent = slot.IsCurrent; Directory = directory; Fingerprint = fingerprint;
        }
    }

    /// <summary>Owns the character's persistent progression. It has no scene dependencies.</summary>
    public partial class ProgressionService
    {
        public const int MaximumLevel = 100;
        // Limits stop new writes instead of evicting existing characters or equipment.
        public const int MaximumSaveSlots = 64;
        public const int MaximumSaveBytes = 4 * 1024 * 1024;
        private const string DeletionSuffix = ".delete-pending";
        private const string DeletionMarker = "Emberfall confirmed character deletion v1\n";
        private static readonly object StorageGate = new object();
        public const int InventoryCapacity = 256;
        public const int MaximumSavedEquipment = 4098; // Two reserved clear-reward slots; new pickups still stop at 4096.
        public const int MaximumRetainedEquipment = 4096; // Visible overflow; bounded by the save byte limit as well.
        public const int MaximumUpgrade = 10;
        public const int PotionPrice = 20;
        public const int PendingLootCapacity = 24;
        public const int RecoveryLootCapacity = 256;
        public const int MechanicExchangeCost = 12;
        public const int MaximumMasteryRank = MasteryProgressionRules.MaximumRank;
        public const int BuildPresetCount = 2;
        public const int FashionChoiceCost = 30;
        public const int AscensionCost = 24;
        public const int AscensionMilestone = 5;
        public const int VariantCost = 4;
        private static readonly int[] WingHealthPercents = { 3, 5, 8, 12 };
        private static readonly int[] WingArmorPercents = { 2, 3, 5, 8 };
        private static readonly int[] WeaponPercents = { 2, 4, 6, 9 };
        // Absolute probabilities per opened dungeon chest: 22% common, 12% rare,
        // 5% epic, 1% legendary, and 60% without a fashion drop.
        public static Rarity? RollFashionRarity(int roll)
        {
            if (roll < 0 || roll >= 100) throw new ArgumentOutOfRangeException("roll");
            if (roll < 1) return Rarity.Legendary;
            if (roll < 6) return Rarity.Epic;
            if (roll < 18) return Rarity.Rare;
            if (roll < 40) return Rarity.Common;
            return null;
        }

        public static string FashionName(FashionSlot slot,Rarity rarity)
        {return FashionName(slot,rarity,HeroClass.Vanguard);}
        public static string FashionName(FashionSlot slot,Rarity rarity,HeroClass hero)
        {
            int rank=Clamp((int)rarity,0,3);
            if(slot==FashionSlot.Wings)return new[]{"雾羽轻翼","潮光双翼","晶虹幻翼","日冕天翼"}[rank];
            string[][] names={new[]{"灰羽誓锋剑","霜岚仪典剑","星狱断章剑","曜冕天衡剑"},new[]{"萤砂引星杖","霜环奏鸣杖","紫曜织界杖","日冕司辰杖"},new[]{"林露轻歌弓","月潮逐风弓","虹羽巡天弓","九曜破晓弓"},new[]{"苔芽契灵杖","青枝唤魂杖","幽莲归梦杖","万灵祖庭杖"}};
            return names[Clamp((int)hero,0,3)][rank];
        }

        public static string FashionBonus(FashionSlot slot, Rarity rarity)
        {
            int rank = (int)rarity;
            int primary = slot == FashionSlot.Wings ? WingHealthPercents[rank] : WeaponPercents[rank];
            int secondary = slot == FashionSlot.Wings ? WingArmorPercents[rank] : WeaponPercents[rank];
            return slot == FashionSlot.Wings ? "生命 +" + primary + "% · 防御 +" + secondary + "%"
                : "攻击 +" + primary + "% · 暴击几率 ×" + (100 + secondary) + "%";
        }

        public static int WeaponFashionPercent(Rarity rarity) { return WeaponPercents[(int)rarity]; }
        private const int MaximumGold = 999999999;
        private const int MaximumEquipmentStat = 10000;
        private const int MaximumEquipmentHealth = 100000;
        private const string SaveFormat = "emberfall-character";
        private readonly string saveDirectory;
        private string savePath;
        private bool attachedSaveExists, activeSlotDeleted;
        private string lastLoggedSaveError;
        private string currentSlotId = "legacy";
        private readonly System.Random random = new System.Random();
        // Freeze the draw through failed writes and in-process reload/retry. Nothing
        // is granted or presented until the normal durable transaction succeeds.
        private ChestReward pendingChestRoll;
        private string pendingChestRollPath;
        private int pendingChestRollClears, pendingChestRollTier;
        private sealed class PendingChestContext
        {
            internal readonly ChestReward Roll;
            internal readonly int Clears,Tier;
            internal PendingChestContext(ChestReward roll,int clears,int tier){Roll=roll;Clears=clears;Tier=tier;}
        }
        // This process-local context follows staged service replacements, keyed by
        // the exact slot path. It never enters GameProfile or a durable save file.
        private Dictionary<string,PendingChestContext> pendingChestContexts=new Dictionary<string,PendingChestContext>(StringComparer.Ordinal);
        private static ChestReward CopyChestRoll(ChestReward roll)
        {return JsonUtility.FromJson<ChestReward>(JsonUtility.ToJson(roll,true));}
        private void RestorePendingChestRoll()
        {
            if(Profile.pendingFashionChest&&Profile.pendingChestDraw!=null)
                pendingChestContexts[SaveFilePath]=new PendingChestContext(CopyChestRoll(Profile.pendingChestDraw),Profile.clearedRuns,Profile.pendingChestTier);
            PendingChestContext context;
            if(!pendingChestContexts.TryGetValue(SaveFilePath,out context))return;
            if(!Profile.pendingFashionChest||Profile.pendingChestReveal||context.Clears!=Profile.clearedRuns||context.Tier!=Profile.pendingChestTier)
            {pendingChestContexts.Remove(SaveFilePath);pendingChestRoll=null;return;}
            pendingChestRoll=CopyChestRoll(context.Roll);pendingChestRollPath=SaveFilePath;
            pendingChestRollClears=context.Clears;pendingChestRollTier=context.Tier;
        }
        internal void CarryPendingChestContextTo(ProgressionService staged)
        {
            if(staged==null||IsPracticeOnly||staged.IsPracticeOnly||staged.SaveDirectory!=SaveDirectory)return;
            // Copy the map: abandoning a staged candidate cannot clear the live
            // service's pending draw. Entries are immutable private roll snapshots.
            staged.pendingChestContexts=new Dictionary<string,PendingChestContext>(pendingChestContexts,StringComparer.Ordinal);
            staged.RestorePendingChestRoll();
        }
        private readonly HashSet<string> collectedLootIds = new HashSet<string>(StringComparer.Ordinal);

        [Serializable]
        private class SaveFile
        {
            public string format;
            public int version;
            public GameProfile profile;
        }

        public GameProfile Profile { get; private set; }
        public ChestReward LastChestReward { get { return Profile.lastChestReward; } }
        public int RecoveryLootCount { get { return Profile.recoveryLoot.Count; } }
        public bool CanEnterDungeon { get { return RecoveryLootCount == 0 && CanReceiveProtectedLoot; } }
        public event Action Changed;
        public event Action<int> LeveledUp;
        public string LastError { get; private set; }
        public string SaveDirectory { get { return saveDirectory; } }
        public string SaveFilePath { get { return savePath; } }
        public string CurrentSlotId { get { return activeSlotDeleted ? null : currentSlotId; } }
        public bool HasActiveSave { get { return !activeSlotDeleted && attachedSaveExists; } }
        public bool HasSave { get { return DiscoverSlotIds().Count > 0; } }

        public bool IsPracticeOnly { get; private set; }
        public ProgressionService CreatePracticeCopy() { return CreatePracticeSnapshot(Profile); }
        private ProgressionService CreatePracticeSnapshot(GameProfile source)
        { var copy=new ProgressionService(CloneProfile(source));copy.IsPracticeOnly=true;return copy; }
        // A practice service has no storage destination at all. It never routes
        // through the public null-directory constructor (which selects real saves).
        private ProgressionService(GameProfile memoryProfile)
        {saveDirectory=null;savePath=null;currentSlotId=null;Profile=memoryProfile;LastError=string.Empty;}

        public ProgressionService(string saveDirectory = null)
        {
            string directory = string.IsNullOrWhiteSpace(saveDirectory) ? Application.persistentDataPath : saveDirectory;
            this.saveDirectory = Path.GetFullPath(directory);
            SelectSlotPath("legacy");
            Profile = CreateProfile(HeroClass.Vanguard);
            LastError = string.Empty;
        }

        public void NewGame(HeroClass heroClass)
        {
            if(IsPracticeOnly){Fail("试招角色不能重建真实存档。");return;}
            if (!Enum.IsDefined(typeof(HeroClass), heroClass)) heroClass = HeroClass.Vanguard;
            // A deleted character has no autosave destination. An explicit new game
            // gets a fresh ID rather than recycling its deleted filename.
            if (activeSlotDeleted || File.Exists(savePath + DeletionSuffix)) { CreateNewSlot(heroClass); return; }
            CancelChapterRun();
            pendingChestRoll = null;pendingChestContexts.Remove(SaveFilePath);
            Profile = CreateProfile(heroClass);
            collectedLootIds.Clear();
            Commit();
        }

        public bool Load()
        {
            // A newly constructed service still addresses the original legacy file.
            // Once explicitly selected, reload remains local to that selected slot.
            return LoadSlot(currentSlotId);
        }

        public bool LoadSlot(string id)
        {
            if(IsPracticeOnly)return Fail("试招角色不能读写角色存档。");
            string normalized;
            if (!TryNormalizeSlotId(id, out normalized)) return Fail("无效的存档编号。");
            string candidatePath = SlotPath(normalized);
            if (File.Exists(candidatePath + DeletionSuffix)) return Fail("该存档删除未完成，请在存档列表重新确认清理剩余文件。");
            GameProfile loaded;
            string failure;
            bool recovered;
            if (TryReadSlot(candidatePath, out loaded, out failure, out recovered))
            {
                // Migration is written before selecting/publishing the loaded role.
                // A failed save leaves the active role and durable reward flags intact.
                string migrationFailure;
                bool migrationRequired=ChapterProgression.BackfillDifficultyRewards(loaded);
                if(loaded.rewardInventoryRevision<1){loaded.rewardInventoryRevision=1;migrationRequired=true;}
                if(loaded.skillStockVersion<1){SkillStockRules.Normalize(loaded);loaded.skillStockVersion=1;migrationRequired=true;}
                if(loaded.variantKnowledgeRevision<1){loaded.variantKnowledgeRevision=1;migrationRequired=true;}
                if (migrationRequired && !TryWriteProfile(loaded, candidatePath, false, out migrationFailure))
                    return Fail(migrationFailure);
                if (currentSlotId != normalized) collectedLootIds.Clear();
                SelectSlotPath(normalized);
                attachedSaveExists = true;
                CancelChapterRun();
                Profile = loaded;
                LastError = failure;
                RaiseChanged();
                return true;
            }
            return Fail(File.Exists(candidatePath) || File.Exists(candidatePath + ".bak")
                ? "这个存档及其备份均无法读取；其他存档仍可选择。" : "找不到这个存档。");
        }

        public List<SaveSlotInfo> GetSaveSlots()
        {
            var slots = new List<SaveSlotInfo>();
            foreach (string id in DiscoverSlotIds())
            {
                string path = SlotPath(id);
                GameProfile loaded;
                string error;
                bool recovered;
                bool readable = TryReadSlot(path, out loaded, out error, out recovered);
                bool deletionPending = File.Exists(path + DeletionSuffix);
                DateTime written = SafeWriteTime(path);
                DateTime backupWritten = SafeWriteTime(path + ".bak");
                if (backupWritten > written) written = backupWritten;
                slots.Add(new SaveSlotInfo
                {
                    Id = id,
                    DisplayName = deletionPending ? "存档 " + (id == "legacy" ? "legacy" : id.Substring(0, 6)) + " · 删除未完成" : readable ? GameBalance.ClassName(loaded.heroClass) + " · " + loaded.level + "级" + (id == "legacy" ? " · 旧存档" : "") : (id == "legacy" ? "旧存档 · 无法读取" : "存档 " + id.Substring(0, 6) + " · 无法读取"),
                    HeroClass = readable ? loaded.heroClass : HeroClass.Vanguard,
                    Level = readable ? loaded.level : 0,
                    SavedAtUtc = written,
                    CanLoad = readable,
                    RecoveredFromBackup = recovered,
                    IsCurrent = !activeSlotDeleted && string.Equals(currentSlotId, id, StringComparison.Ordinal),
                    DeletionPending = deletionPending
                });
            }
            slots.Sort((a, b) => { int time = b.SavedAtUtc.CompareTo(a.SavedAtUtc); return time != 0 ? time : string.CompareOrdinal(a.Id, b.Id); });
            return slots;
        }

        public bool PrepareSaveDeletion(string id, out SaveDeletionRequest request)
        {
            request = null;
            string normalized;
            if (!TryNormalizeSlotId(id, out normalized)) return Fail("无效的存档编号。");
            lock (StorageGate)
            {
                try
                {
                    string before = SlotFingerprint(SlotPath(normalized));
                    SaveSlotInfo slot = GetSaveSlots().Find(value => value.Id == normalized);
                    if (slot == null) return Fail("找不到这个存档，请刷新列表。");
                    string after = SlotFingerprint(SlotPath(normalized));
                    if (before != after) return Fail("存档正在改变，请刷新后重新选择角色。");
                    request = new SaveDeletionRequest(slot, saveDirectory, after);
                    LastError = string.Empty;
                    return true;
                }
                catch (Exception exception) when (IsStorageException(exception))
                { return Fail("无法准备删除：" + exception.Message); }
            }
        }

        /// <summary>Only call after showing the request's identity and obtaining confirmation.
        /// Normal success removes exactly four known paths; no wildcard or directory deletion.
        /// An interrupted deletion remains visible, cannot load from backup, and requires
        /// another explicit confirmation to finish. Existing unrelated files stay untouched.</summary>
        public bool DeleteSaveSlot(SaveDeletionRequest request)
        {
            if(IsPracticeOnly)return Fail("试招角色不能读写角色存档。");
            if (request == null || !string.Equals(request.Directory, saveDirectory, StringComparison.Ordinal))
                return Fail("删除确认无效，请重新选择存档。");
            string id;
            if (!TryNormalizeSlotId(request.Id, out id)) return Fail("无效的存档编号。");
            lock (StorageGate)
            {
                string primary = SlotPath(id), marker = primary + DeletionSuffix;
                try
                {
                    if (request.Fingerprint == null) { LastError = string.Empty; return true; } // This exact request already completed.
                    if (SlotFingerprint(primary) != request.Fingerprint)
                        return Fail("存档内容已改变，未删除任何文件。请刷新并重新确认角色。");
                    // The durable marker is committed before any save content is removed.
                    // It survives partial failure and blocks both backup recovery and writes.
                    if (!File.Exists(marker))
                    {
                        using (var stream = new FileStream(marker, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        {
                            byte[] bytes = Encoding.UTF8.GetBytes(DeletionMarker);
                            stream.Write(bytes, 0, bytes.Length);
                            stream.Flush(true);
                        }
                    }
                    if (currentSlotId == id)
                    {
                        activeSlotDeleted = true;
                        attachedSaveExists = false;
                        collectedLootIds.Clear();
                    }
                    File.Delete(primary + ".tmp");
                    File.Delete(primary + ".bak");
                    File.Delete(primary);
                    File.Delete(marker);
                    request.Fingerprint = null;
                    LastError = string.Empty;
                    RaiseChanged();
                    return true;
                }
                catch (Exception exception) when (IsStorageException(exception))
                {
                    // Do not rollback by restoring a backup: a confirmed deletion must
                    // never resurrect as a playable character after a partial failure.
                    if (File.Exists(marker) && currentSlotId == id) activeSlotDeleted = true;
                    return Fail("删除未完成：" + exception.Message + " 请刷新并重新确认清理；剩余文件不会作为存档读取。");
                }
            }
        }

        private static bool IsStorageException(Exception exception)
        {
            return exception is IOException || exception is UnauthorizedAccessException ||
                exception is ArgumentException || exception is NotSupportedException;
        }

        private static string SlotFingerprint(string primary)
        {
            var identity = new StringBuilder();
            foreach (string suffix in new[] { "", ".bak", ".tmp", DeletionSuffix })
            {
                string path = primary + suffix;
                if (Directory.Exists(path)) throw new IOException("存档路径被同名目录占用，未删除目录。");
                identity.Append(suffix).Append(':');
                if (!File.Exists(path)) { identity.Append("missing;"); continue; }
                var info = new FileInfo(path);
                if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("存档路径是链接，未修改其目标。");
                identity.Append(info.Length).Append(':').Append(info.LastWriteTimeUtc.Ticks).Append(':');
                // Hash only recognised files, with a bounded read for oversized damaged
                // files. Length/time plus head/tail still identify those for confirmation.
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (SHA256 hash = SHA256.Create())
                {
                    byte[] digest;
                    if (stream.Length <= MaximumSaveBytes) digest = hash.ComputeHash(stream);
                    else
                    {
                        byte[] sample = new byte[8192];
                        int first = stream.Read(sample, 0, 4096);
                        stream.Seek(-4096, SeekOrigin.End);
                        int last = stream.Read(sample, first, 4096);
                        digest = hash.ComputeHash(sample, 0, first + last);
                    }
                    identity.Append(Convert.ToBase64String(digest)).Append(';');
                }
            }
            return identity.ToString();
        }

        public bool CreateNewSlot(HeroClass hero)
        {
            if (!Enum.IsDefined(typeof(HeroClass), hero)) return Fail("无效的职业。");
            return CreateSlot(CreateProfile(hero), true);
        }

        public bool SaveAsNewSlot()
        {
            if (activeSlotDeleted) return Fail("当前角色已删除，请读取其他存档或创建新角色。");
            try
            {
                // Validate/write a deep snapshot, never mutate the current profile
                // while attempting a save that can still fail.
                GameProfile snapshot = Snapshot();
                if (snapshot == null) return Fail("无法创建当前角色的存档快照。");
                return CreateSlot(snapshot, false);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException || exception is NotSupportedException)
            {
                return Fail("另存失败：" + exception.Message);
            }
        }

        private bool CreateSlot(GameProfile candidate, bool newCharacter)
        {
            if(IsPracticeOnly)return Fail("试招角色不能读写角色存档。");
            lock (StorageGate)
            {
                if (DiscoverSlotIds(true).Count >= MaximumSaveSlots)
                    return Fail("已达到 " + MaximumSaveSlots + " 份存档/临时恢复文件上限。请先备份整个存档目录并处理恢复文件，或手动删除不需要的角色；现有文件不会自动清理。");
                string id = Guid.NewGuid().ToString("N");
                string path = SlotPath(id);
                PendingChestContext snapshotDraw = null;
                if (!newCharacter) pendingChestContexts.TryGetValue(SaveFilePath, out snapshotDraw);
                string failure;
                if (!TryWriteProfile(candidate, path, true, out failure)) return Fail(failure);
                // Both primary and backup are now durably written. Only then publish
                // the new active profile/path and notify the UI.
                SelectSlotPath(id);
                attachedSaveExists = true;
                CancelChapterRun();
                Profile = candidate;
                // Save-as copies this same unopened chest, including an in-process
                // failed draw. A fresh character must never inherit that context.
                if (snapshotDraw != null)
                {
                    pendingChestContexts[path] = snapshotDraw;
                    RestorePendingChestRoll();
                }
                if (newCharacter) collectedLootIds.Clear();
                LastError = string.Empty;
                RaiseChanged();
                return true;
            }
        }

        private void SelectSlotPath(string id)
        {
            if (currentSlotId != id) pendingChestRoll = null;
            currentSlotId = id;
            savePath = SlotPath(id);
            activeSlotDeleted = false;
            lastLoggedSaveError = null;
        }

        private string SlotPath(string id)
        {
            return Path.Combine(saveDirectory, id == "legacy" ? "emberfall-save.json" : "emberfall-save-" + id + ".json");
        }

        private static bool TryNormalizeSlotId(string id, out string normalized)
        {
            normalized = null;
            if (id == "legacy") { normalized = id; return true; }
            Guid guid;
            if (id == null || id.Length != 32 || !Guid.TryParseExact(id, "N", out guid)) return false;
            normalized = guid.ToString("N");
            return true;
        }

        private List<string> DiscoverSlotIds(bool includeTemporaryReservations = false)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                if (!Directory.Exists(saveDirectory)) return new List<string>();
                foreach (string file in Directory.EnumerateFiles(saveDirectory, "emberfall-save*.json*", SearchOption.TopDirectoryOnly))
                {
                    string name = Path.GetFileName(file);
                    // A process interrupted before publishing a new primary can
                    // leave only its canonical .tmp path. Reserve capacity for
                    // that identity without exposing it as a playable character
                    // or deleting potentially recoverable progress.
                    if (includeTemporaryReservations && name.EndsWith(".json.tmp", StringComparison.Ordinal))
                        name = name.Substring(0, name.Length - 4);
                    if (name.EndsWith(DeletionSuffix, StringComparison.Ordinal)) name = name.Substring(0, name.Length - DeletionSuffix.Length);
                    if (name.EndsWith(".bak", StringComparison.Ordinal)) name = name.Substring(0, name.Length - 4);
                    if (name == "emberfall-save.json") { ids.Add("legacy"); continue; }
                    const string prefix = "emberfall-save-", suffix = ".json";
                    if (!name.StartsWith(prefix, StringComparison.Ordinal) || !name.EndsWith(suffix, StringComparison.Ordinal)) continue;
                    string raw = name.Substring(prefix.Length, name.Length - prefix.Length - suffix.Length);
                    string id;
                    // Generated filenames are canonical lowercase GuidN. Unknown
                    // files and subdirectories are never save slots. Temporary
                    // writes are counted only by the new-slot capacity guards.
                    if (raw.Length == 32 && TryNormalizeSlotId(raw, out id) && raw == id) ids.Add(id);
                }
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException) { }
            return new List<string>(ids);
        }

        private static DateTime SafeWriteTime(string path)
        {
            try { return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc); }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException)
            { return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc); }
        }

        private static bool TryReadSlot(string primary, out GameProfile profile, out string error, out bool recovered)
        {
            recovered = false;
            profile = null;
            error = "该存档的删除尚未完成，请在存档列表确认清理剩余文件。";
            if (File.Exists(primary + DeletionSuffix)) return false;
            if (TryReadProfile(primary, out profile, out error)) return true;
            if(error=="future format"||IsFrozenRewardReadError(error))return false;
            if (!TryReadProfile(primary + ".bak", out profile, out error)) return false;
            recovered = true;
            error = "主存档无法读取，已恢复上一次备份。" + (string.IsNullOrEmpty(error) ? "" : " " + error);
            return true;
        }

        // Charge spending is persisted before the runtime pays energy/emits a cast.
        // Recharge ticks update memory; normal save captures progress, never wall time.
        public bool CommitSkillStock(HeroClass hero,int count,float remaining,float period)
        {
            if(Profile.heroClass!=hero)return Fail("职业已变化，技能未释放。");
            var candidate=Snapshot();SkillStockRules.Normalize(candidate);candidate.skillStockVersion=1;
            int i=(int)hero;candidate.skillStockCounts[i]=count;candidate.skillStockRemaining[i]=remaining;candidate.skillStockPeriods[i]=period;
            string failure;if(!TryWriteAttachedProfile(candidate,out failure))return Fail(failure);
            Profile=candidate;LastError=string.Empty;return true;
        }
        public void TrackSkillStock(HeroClass hero,int count,float remaining,float period)
        {
            if(Profile.heroClass!=hero)return;
            SkillStockRules.Normalize(Profile);Profile.skillStockVersion=1;
            int i=(int)hero;Profile.skillStockCounts[i]=count;Profile.skillStockRemaining[i]=remaining;Profile.skillStockPeriods[i]=period;
        }

        public void Save()
        {
            string failure;
            var candidate=Snapshot();
            if (TryWriteAttachedProfile(candidate, out failure))
            {
                LastError = string.Empty;
                lastLoggedSaveError = null;
                if(JsonUtility.ToJson(Profile,true)!=JsonUtility.ToJson(candidate,true))Profile=candidate;
            }
            else
            {
                LastError = failure;
                // A full/readonly disk should not grow Player.log every autosave tick.
                if (lastLoggedSaveError != failure) Debug.LogWarning("Emberfall: " + failure);
                lastLoggedSaveError = failure;
            }
        }

        private bool TryWriteAttachedProfile(GameProfile profile, out string failure)
        {
            if(IsPracticeOnly){failure=string.Empty;return true;}
            lock (StorageGate)
            {
                if (activeSlotDeleted || File.Exists(savePath + DeletionSuffix) ||
                    (attachedSaveExists && !File.Exists(savePath) && !File.Exists(savePath + ".bak")))
                {
                    activeSlotDeleted = true;
                    failure = "保存失败：当前存档已删除或移走。请读取其他存档或创建新角色。";
                    return false;
                }
                if (!attachedSaveExists && !File.Exists(savePath) && !File.Exists(savePath + ".bak") &&
                    DiscoverSlotIds(true).Count >= MaximumSaveSlots)
                {
                    failure = "保存失败：已达到 " + MaximumSaveSlots + " 份存档/临时恢复文件上限。请先备份目录并处理恢复文件；未覆盖或清理现有文件。";
                    return false;
                }
                // A first legacy save uses the same collision-safe creation path
                // as a new slot. Existing attached slots never become new again.
                bool createOnly = !attachedSaveExists && !File.Exists(savePath) && !File.Exists(savePath + ".bak");
                bool result = TryWriteProfile(profile, savePath, createOnly, out failure);
                if (result) attachedSaveExists = true;
                return result;
            }
        }

        private static bool TryWriteProfile(GameProfile profile, string primary, bool createOnly, out string failure)
        {
            if(string.IsNullOrEmpty(primary)){failure="试招角色没有持久化目的地。";return false;}
            string backup = primary + ".bak", temporary = primary + ".tmp";
            bool ownsTemporary = false, ownsBackup = false;
            failure = string.Empty;
            try
            {
                // Never follow a temporary-file link (FileMode.Create would
                // truncate its target), or replace linked recovery artifacts.
                // Attribute checks also catch dangling links, which Exists may miss.
                RejectLinkedStoragePath(primary);
                RejectLinkedStoragePath(backup);
                RejectLinkedStoragePath(temporary);
                RejectLinkedStoragePath(primary + DeletionSuffix);
                if (File.Exists(primary + DeletionSuffix)) throw new IOException("该角色正在删除，已停止写入。");
                profile.rewardInventoryRevision=1;
                ValidateProfile(profile);
                Directory.CreateDirectory(Path.GetDirectoryName(primary));
                if (createOnly && (File.Exists(primary) || File.Exists(backup) || File.Exists(temporary) || File.Exists(primary + DeletionSuffix)))
                    throw new IOException("新存档文件名已被占用，请重试。");
                GameProfile pendingWrite;
                string pendingError;
                if (Directory.Exists(temporary)) throw new IOException("临时存档路径被目录占用，未覆盖原文件或备份。");
                if (File.Exists(temporary) && TryReadProfile(temporary, out pendingWrite, out pendingError))
                    throw new IOException("发现可恢复的临时存档，已保留且未覆盖。请先备份整个存档目录，再处理临时存档恢复。");
                // Older readers must reject independent attachment investments
                // and reward receipts instead of silently erasing unknown fields.
                var save = new SaveFile { format = SaveFormat, version = profile.inventory.Count>MaximumRetainedEquipment?6:profile.rewardInventoryRevision>0?5:profile.attachmentRevision>0?4:profile.chestRulesRevision>=2?3:profile.classStateRevision>0?2:1, profile = profile };
                string json = PreserveOptionalReceiptNulls(JsonUtility.ToJson(save, true), profile);
                if (Encoding.UTF8.GetByteCount(json) > MaximumSaveBytes)
                    throw new IOException("存档超过 4 MiB 安全大小，未覆盖原文件或备份。请保留现有文件。");
                // Compare the actual bounded document, never a dirty flag or file
                // timestamp: callers may mutate Profile directly or replace files.
                // An unchanged primary alone is insufficient; saving must also
                // retain a usable recovery backup. All refusal guards run first.
                if (!createOnly)
                {
                    string existingDocument, readError;
                    GameProfile recovery;
                    bool samePrimary = TryReadSaveDocument(primary, out existingDocument, out readError) &&
                        string.Equals(existingDocument, json, StringComparison.Ordinal);
                    bool usableBackup = TryReadProfile(backup, out recovery, out readError);
                    if (samePrimary && usableBackup) return true;
                    bool usablePrimary = samePrimary || TryReadProfile(primary, out recovery, out readError);
                    if(!usablePrimary&&(readError=="future format"||IsFrozenRewardReadError(readError)))throw new IOException("主档含不可回退的奖励记录，原主档和备份均保留。");
                    // Do not expose an uncommitted candidate in a newly repaired
                    // backup or overwrite both damaged originals to manufacture one.
                    if (!usablePrimary && !usableBackup)
                        throw new IOException("主存档与备份均无法验证，当前进度尚未保存。原文件已保留，请先备份整个目录并恢复可用存档。");
                }
                // Flush the complete new document before atomically replacing the old one.
                using (var stream = new FileStream(temporary, createOnly ? FileMode.CreateNew : FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    ownsTemporary = true;
                    using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                    {
                        writer.Write(json);
                        writer.Flush();
                        stream.Flush(true);
                    }
                }
                if (createOnly)
                {
                    using (var backupStream = new FileStream(backup, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        ownsBackup = true;
                        using (var source = new FileStream(temporary, FileMode.Open, FileAccess.Read, FileShare.Read)) source.CopyTo(backupStream);
                        backupStream.Flush(true);
                    }
                    File.Move(temporary, primary);
                }
                else if (File.Exists(primary))
                {
                    GameProfile previous;
                    string error;
                    // A corrupt primary must never replace a usable recovery backup.
                    bool validPrevious = TryReadProfile(primary, out previous, out error);
                    File.Replace(temporary, primary, validPrevious ? backup : null, true);
                }
                else
                {
                    // Non-create-only saves reach this branch only with a usable
                    // recovery backup. There must be no fallible second write
                    // after the candidate has become the durable primary.
                    File.Move(temporary, primary);
                }
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException)
            {
                failure = "保存失败：" + exception.Message;
                // Only a temporary file written by this attempt is disposable. Never
                // sweep the directory or remove an existing recovery backup.
                if (ownsTemporary) DeleteFailedSlotFile(temporary);
                if (createOnly && ownsBackup) DeleteFailedSlotFile(backup);
                return false;
            }
        }

        private static void RejectLinkedStoragePath(string path)
        {
            FileAttributes attributes;
            try { attributes = File.GetAttributes(path); }
            catch (FileNotFoundException) { return; }
            catch (DirectoryNotFoundException) { return; }
            if ((attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("存档路径是链接，未修改该路径或其目标。请保留原文件并检查存档目录。");
        }

        private static void DeleteFailedSlotFile(string path)
        {
            try { File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        public string LevelGrowthDescription()
        {
            switch(Profile.heroClass)
            {
                case HeroClass.Arcanist:return "每级：生命 +15 / 攻击 +3.5 / 防御 +1";
                case HeroClass.Ranger:return "每级：生命 +17 / 攻击 +3.2 / 防御 +1.1";
                case HeroClass.Summoner:return "每级：生命 +13 / 攻击 +2.3 / 防御 +0.75";
                default:return "每级：生命 +20 / 攻击 +3 / 防御 +1.4";
            }
        }

        public StatBlock GetStats()
        {
            int level = Clamp(Profile.level, 1, MaximumLevel);
            float growth = level - 1;
            StatBlock stats;
            switch (Profile.heroClass)
            {
                case HeroClass.Arcanist:
                    stats = new StatBlock { MaxHealth = 125 + 15 * growth, Damage = 24 + 3.5f * growth, Armor = 2 + growth, MoveSpeed = 6.2f, CritChance = .10f };
                    break;
                case HeroClass.Ranger:
                    stats = new StatBlock { MaxHealth = 140 + 17 * growth, Damage = 21 + 3.2f * growth, Armor = 4 + 1.1f * growth, MoveSpeed = 6.6f, CritChance = .14f };
                    break;
                case HeroClass.Summoner:
                    stats = new StatBlock { MaxHealth = 125 + 13 * growth, Damage = 15 + 2.3f * growth, Armor = 3 + .75f * growth, MoveSpeed = 5.5f, CritChance = .10f };
                    break;
                default:
                    stats = new StatBlock { MaxHealth = 170 + 20 * growth, Damage = 20 + 3 * growth, Armor = 7 + 1.4f * growth, MoveSpeed = 6f, CritChance = .08f };
                    break;
            }
            for (int slot = 0; slot < 3; slot++)
            {
                ItemData item = Equipped((ItemSlot)slot);
                if (item == null) continue;
                stats.MaxHealth += item.health;
                stats.Damage += item.attack;
                stats.Armor += item.defense;
            }
            if(Profile.attachments!=null)foreach(var a in Profile.attachments)if(a.mounted&&BuildCatalog.MechanicClass(a.mechanic)==Profile.heroClass)
            {stats.Damage*=1f+.015f*a.upgradeRank;stats.MaxHealth*=1f+.02f*a.upgradeRank;}
            int passiveRank = Profile.skillRanks != null && Profile.skillRanks.Length > 3 ? Clamp(Profile.skillRanks[3], 0, 3) : 0;
            if (passiveRank > 0)
            {
                if (Profile.heroClass == HeroClass.Vanguard)
                {
                    stats.Damage *= 1f + (passiveRank == 1 ? .08f : passiveRank == 2 ? .14f : .22f);
                    stats.Armor += passiveRank == 1 ? 2f : passiveRank == 2 ? 4f : 7f;
                }
                else if (Profile.heroClass == HeroClass.Arcanist)
                {
                    stats.Damage *= 1f + (passiveRank == 1 ? .06f : passiveRank == 2 ? .11f : .18f);
                    stats.MaxHealth *= 1f + (passiveRank == 1 ? .04f : passiveRank == 2 ? .07f : .10f);
                }
                else if (Profile.heroClass == HeroClass.Summoner)
                {
                    stats.Damage *= 1f + (passiveRank == 1 ? .06f : passiveRank == 2 ? .11f : .18f);
                }
                else
                {
                    stats.CritChance = Math.Min(1f, stats.CritChance + (passiveRank == 1 ? .04f : passiveRank == 2 ? .07f : .12f));
                    stats.MoveSpeed *= 1f + (passiveRank == 1 ? .03f : passiveRank == 2 ? .06f : .10f);
                }
            }
            FashionData wings = StrongestFashion(FashionSlot.Wings);
            if (wings != null)
            {
                int rank = (int)wings.rarity;
                stats.MaxHealth *= 1f + WingHealthPercents[rank] / 100f;
                stats.Armor *= 1f + WingArmorPercents[rank] / 100f;
            }
            FashionData weaponFashion = StrongestFashion(FashionSlot.Weapon);
            if (weaponFashion != null)
            {
                float bonus = WeaponPercents[(int)weaponFashion.rarity] / 100f;
                stats.Damage *= 1f + bonus;
                stats.CritChance = Math.Min(1f, stats.CritChance * (1f + bonus));
            }
            if (Profile.masteryRanks != null && Profile.masteryRanks.Length >= 3)
            {
                stats.Damage *= 1f + Clamp(Profile.masteryRanks[0], 0, MaximumMasteryRank) * .003f;
                stats.MaxHealth *= 1f + Clamp(Profile.masteryRanks[1], 0, MaximumMasteryRank) * .005f;
                stats.Armor *= 1f + Clamp(Profile.masteryRanks[2], 0, MaximumMasteryRank) * .0075f;
            }
            return stats;
        }

        public FashionData EquippedFashion(FashionSlot slot)
        {
            string id = slot == FashionSlot.Wings ? Profile.wingsFashionId : Profile.weaponFashionId;
            return Profile.fashions == null ? null : Profile.fashions.Find(value => value != null && value.id == id && value.slot == slot);
        }

        public bool EquipFashion(string id)
        {
            FashionData fashion = Profile.fashions == null ? null : Profile.fashions.Find(value => value != null && value.id == id);
            if (fashion == null) return Fail("尚未获得这件时装。");
            GameProfile candidate=Snapshot();
            if (fashion.slot == FashionSlot.Wings) candidate.wingsFashionId = id;
            else candidate.weaponFashionId = id;
            return CommitCandidate(candidate);
        }

        public bool UnequipFashion(FashionSlot slot)
        {
            GameProfile candidate=Snapshot();
            if (slot == FashionSlot.Wings) candidate.wingsFashionId = null;
            else if (slot == FashionSlot.Weapon) candidate.weaponFashionId = null;
            else return Fail("无效的时装部位。");
            return CommitCandidate(candidate);
        }

        public bool SetSpecialization(ElementalistSpecialization specialization, bool inCamp)
        {
            if (!inCamp) return Fail("只能在营地免费切换专精。");
            if (Profile.heroClass != HeroClass.Arcanist) return Fail("只有元素师可切换冰火专精。");
            if (!Enum.IsDefined(typeof(ElementalistSpecialization), specialization)) return Fail("无效的专精。");
            GameProfile candidate=Snapshot();candidate.specialization=specialization;
            return CommitCandidate(candidate);
        }

        public bool HasMechanic(EquipmentMechanic mechanic)
        {
            // This is queried by combat/companion updates. Keep the closed catalog
            // check allocation-free instead of boxing an enum for reflection.
            switch (mechanic)
            {
                case EquipmentMechanic.FrostEcho:
                case EquipmentMechanic.CinderTrail:
                case EquipmentMechanic.ReturningBlade:
                case EquipmentMechanic.VenomSpread:
                case EquipmentMechanic.TwinSummonResonance: break;
                default: return false;
            }
            if (BuildCatalog.MechanicClass(mechanic) != Profile.heroClass) return false;
            var attachment=Attachment(mechanic);
            if(attachment!=null)return attachment.mounted;
            ItemData equipped = Equipped(BuildCatalog.MechanicSlot(mechanic));
            return equipped != null && equipped.mechanic == mechanic;
        }

        private bool SelectedMechanicB(EquipmentMechanic mechanic)
        {
            if(!HasMechanic(mechanic))return false;
            var attachment=Attachment(mechanic);if(attachment!=null)return attachment.variantUnlocked&&attachment.variant==1;
            ItemData item=Equipped(BuildCatalog.MechanicSlot(mechanic));
            return HasVariant(item)&&item.mechanicVariantUnlocked&&item.mechanicVariant==1;
        }
        /// <summary>Rank effects under the actually worn class-valid mechanism, shared by current and draft previews.</summary>
        public string SkillEffectSummary(int skill,int rank)
        {
            if(skill<0||skill>=GameBalance.SkillCount||rank<1||rank>3)return "未习得";
            HeroClass hero=Profile.heroClass;
            if(hero==HeroClass.Ranger&&skill==0&&HasMechanic(EquipmentMechanic.VenomSpread))
                return BuildCatalog.VenomSkillSummary(rank,SelectedMechanicB(EquipmentMechanic.VenomSpread));
            string text=GameBalance.SkillEvolution(hero,skill,rank);
            if(hero==HeroClass.Arcanist&&skill==0&&HasMechanic(EquipmentMechanic.FrostEcho))
            {
                bool wide=SelectedMechanicB(EquipmentMechanic.FrostEcho);
                text+="\n当前霜回 "+(wide?"B":"A")+" 修正：本阶首击倍率 ×"+BuildCatalog.FrostEchoOpeningMultiplier(wide).ToString("0.##")+"；0.7秒回响伤害 "+(100*BuildCatalog.FrostEchoCoefficient(wide)*CombatBalance.RankPower(rank)).ToString("0.##")+"%攻击；回响范围倍率 ×"+BuildCatalog.FrostEchoRadiusMultiplier(wide).ToString("0.##")+"。追加阶级攻击保留。";
            }
            if(hero==HeroClass.Arcanist&&skill==1&&HasMechanic(EquipmentMechanic.CinderTrail))
            {
                bool concentrated=SelectedMechanicB(EquipmentMechanic.CinderTrail);
                text+="\n当前余烬 "+(concentrated?"B":"A")+" 修正：陨星直伤倍率 ×"+BuildCatalog.CinderDirectMultiplier.ToString("0.##")+"（与专精相乘）；火场半径倍率 ×"+BuildCatalog.CinderTrailRadiusMultiplier(concentrated).ToString("0.##")+"；每跳相对本阶首陨伤害 ×"+BuildCatalog.CinderTrailTickMultiplier(concentrated).ToString("0.####")+"。";
            }
            if(hero==HeroClass.Arcanist&&(skill==0||skill==1||skill==5||skill==9))
                text+="\n当前专精覆盖："+BuildCatalog.SpecializationDescription(Profile.specialization);
            if(hero==HeroClass.Vanguard&&HasMechanic(EquipmentMechanic.ReturningBlade))
            {
                string[] variants=BuildCatalog.MechanicDescription(EquipmentMechanic.ReturningBlade).Split(new[]{"变体B："},StringSplitOptions.None);
                text+="\n同时生效的回刃 "+(SelectedMechanicB(EquipmentMechanic.ReturningBlade)?"B："+variants[1]:"A："+variants[0]);
            }
            if(hero==HeroClass.Summoner&&(skill==2||skill==4||skill==9)&&HasMechanic(EquipmentMechanic.TwinSummonResonance))
                text+="\n数量与继承倍率以上限机制为准："+BuildCatalog.MechanicDescription(EquipmentMechanic.TwinSummonResonance);
            return text;
        }

        public bool HasDiscoveredMechanic(EquipmentMechanic mechanic)
        {
            return Profile.discoveredMechanics != null && Profile.discoveredMechanics.Contains(mechanic);
        }

        public bool SetItemLocked(string id, bool locked)
        {
            GameProfile candidate=Snapshot();
            ItemData item = candidate.inventory.Find(value=>value!=null&&value.id==id);
            if (item == null && candidate.pendingLoot != null) item = candidate.pendingLoot.Find(value => value != null && value.id == id);
            if (item == null && candidate.recoveryLoot != null) item = candidate.recoveryLoot.Find(value => value != null && value.id == id);
            if (item == null) return Fail("找不到这件装备。");
            item.locked = locked;
            return CommitCandidate(candidate);
        }

        // Compatibility shim: old automation can disable but never re-enable removed autosales.
        public bool SetAutoSell(Rarity rarity, bool enabled)
        {
            if(enabled)return Fail("自动出售已移除，请在商人处明确选择要出售的装备。");
            var candidate=Snapshot();candidate.autoSellCommon=candidate.autoSellRare=false;return CommitCandidate(candidate);
        }

        public int BulkSellLowQuality(bool confirmPresetReferences=false)
        {
            if(!confirmPresetReferences&&BulkSalePresetImpact().Length>0){Fail("清理会使这些方案引用缺失："+BulkSalePresetImpact()+"；请确认后再出售。");return 0;}
            GameProfile candidate = Snapshot();
            int sold = 0;
            for (int index = candidate.inventory.Count - 1; index >= 0; index--)
            {
                ItemData item = candidate.inventory[index];
                if (item == null || IsEquipped(candidate, item.id) || IsProtectedLoot(item) || item.rarity > Rarity.Rare) continue;
                candidate.gold = (int)Math.Max(0L, Math.Min(MaximumGold, (long)candidate.gold + SellValue(item)));
                candidate.inventory.RemoveAt(index);
                sold++;
            }
            if (sold == 0)
            {
                Fail("没有可批量出售的普通/稀有装备；穿戴、锁定、机制和强化装备受保护。");
                return 0;
            }
            // Publish inventory and proceeds together, only after the slot write succeeds.
            return CommitCandidate(candidate) ? sold : 0;
        }

        public static bool IsProtectedLoot(ItemData item)
        {
            return item != null && (item.locked || item.mechanic != EquipmentMechanic.None || item.rarity >= Rarity.Epic || item.upgradeLevel > 0);
        }

        public bool CanReceiveProtectedLoot
        {
            get { return Profile.inventory.Count + Profile.pendingLoot.Count + Profile.recoveryLoot.Count < MaximumRetainedEquipment; }
        }

        public static int CombatTrialProgress(GameProfile profile)
        {
            if(profile==null)return 0;
            int count=profile.classTutorialCompleted?1:0;
            foreach(int bit in new[]{1,2,8})if((profile.tutorialMask&bit)!=0)count++;
            return count;
        }

        public bool ClaimPendingLoot(string id) { return TransferLegacyReward(id, false); }
        public int ClaimAllPendingLoot() { return TransferLegacyRewards(false); }

        private static void MigrateOwnedRewards(GameProfile profile)
        {
            var known = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in profile.inventory) known.Add(item.id);
            foreach (var source in new[] { profile.pendingLoot, profile.recoveryLoot })
                if (source != null) foreach (var item in source)
                    if (item != null && known.Add(item.id)) profile.inventory.Add(item);
            if (profile.inventory.Count > MaximumSavedEquipment)
                throw new ArgumentException("装备数量超过存档安全上限；原文件与奖励保留，请先整理行囊。");
            profile.pendingLoot.Clear(); profile.recoveryLoot.Clear();
        }
        private bool TransferLegacyReward(string id, bool recovery)
        {
            var source = recovery ? Profile.recoveryLoot : Profile.pendingLoot;
            if (!source.Exists(item => item != null && item.id == id)) return Fail("奖励已经自动进入行囊，或原奖励不存在。");
            return CommitCandidate(Snapshot());
        }
        private int TransferLegacyRewards(bool recovery)
        {
            var source = recovery ? Profile.recoveryLoot : Profile.pendingLoot;
            int count = source.Count;
            if (count == 0) return 0;
            return CommitCandidate(Snapshot()) ? count : 0;
        }

        /// <summary>Emergency exit/death/quit capture. A single active expedition can
        /// populate this bounded mailbox; entry stays blocked until it is emptied.</summary>
        public bool PreserveGroundLoot(IEnumerable<ItemData> items)
        {
            if (items == null) return Fail("无法读取待保管的地面装备。");
            var known = new HashSet<string>(collectedLootIds, StringComparer.Ordinal);
            foreach (ItemData item in Profile.inventory) known.Add(item.id);
            foreach (ItemData item in Profile.pendingLoot) known.Add(item.id);
            foreach (ItemData item in Profile.recoveryLoot) known.Add(item.id);
            var incoming = new List<ItemData>();
            foreach (ItemData item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.id) || item.id.Length > 80 ||
                    !Enum.IsDefined(typeof(ItemSlot), item.slot) || !Enum.IsDefined(typeof(Rarity), item.rarity) ||
                    !Enum.IsDefined(typeof(EquipmentMechanic), item.mechanic)) return Fail("地面装备数据无效；尚未收取或删除任何装备。");
                if (known.Add(item.id)) incoming.Add(item);
            }
            if (incoming.Count == 0) { LastError = string.Empty; return true; }
            if (incoming.Count > MaximumRetainedEquipment - Profile.inventory.Count - Profile.pendingLoot.Count - Profile.recoveryLoot.Count)
                return Fail("装备保全空间已满，地面物品仍保留；请到商人整理行囊后再离开。");
            GameProfile candidate = CloneProfile(Profile);
            foreach (ItemData item in incoming)
                candidate.inventory.Add(JsonUtility.FromJson<ItemData>(JsonUtility.ToJson(item, true)));
            MigrateOwnedRewards(candidate);
            string failure;
            if (!TryWriteAttachedProfile(candidate, out failure)) return Fail(failure);
            Profile = candidate;
            foreach (ItemData item in incoming) collectedLootIds.Add(item.id);
            LastError = string.Empty;
            RaiseChanged();
            return true;
        }

        // Only the world-transition adapter may release these session receipts.
        // Inventory/mailbox identity checks remain authoritative across worlds.
        // A save, sale, wave change or failed transition is never a boundary.
        internal bool TryRetireWorldLootReceipts(bool transitionCommitted, int pendingGroundLoot,
            bool oldProducersRetired, bool combatEpochRetired)
        {
            if (!transitionCommitted || pendingGroundLoot != 0 || !oldProducersRetired || !combatEpochRetired) return false;
            collectedLootIds.Clear();
            return true;
        }

        public bool ClaimRecoveryLoot(string id) { return TransferLegacyReward(id, true); }
        public int ClaimAllRecoveryLoot() { return TransferLegacyRewards(true); }

        public ItemData CreateMechanicItem(EquipmentMechanic mechanic)
        {
            if (mechanic == EquipmentMechanic.None || !Enum.IsDefined(typeof(EquipmentMechanic), mechanic)) return null;
            int level = EquipmentGenerationLevel(Profile.level);
            ItemSlot slot = BuildCatalog.MechanicSlot(mechanic);
            var item = new ItemData { id = Guid.NewGuid().ToString("N"), name = BuildCatalog.MechanicName(mechanic),
                level = level, rarity = Rarity.Epic, slot = slot, mechanic = mechanic, locked = true };
            SetRolledStats(item);
            EnsureUpgradeBasis(item);
            return item;
        }

        public bool ClaimFirstClearReward(EquipmentMechanic mechanic)
        {
            if (!Profile.pendingFirstClearReward || Profile.firstClearRewardClaimed) return Fail("当前没有首通自选奖励。");
            if (mechanic == EquipmentMechanic.None || !Enum.IsDefined(typeof(EquipmentMechanic), mechanic) ||
                BuildCatalog.MechanicClass(mechanic) != Profile.heroClass) return Fail("请选择本职业的机制装备。");
            return GrantAttachment(mechanic,true);
        }

        public bool ExchangeMechanic(EquipmentMechanic mechanic)
        {
            if (mechanic == EquipmentMechanic.None || !Enum.IsDefined(typeof(EquipmentMechanic), mechanic) ||
                BuildCatalog.MechanicClass(mechanic) != Profile.heroClass) return Fail("只能兑换本职业的机制装备。");
            if (Profile.mechanicMaterials < MechanicExchangeCost) return Fail("需要12枚星烬碎片；遗迹通关按阶数获得3至7枚。");
            return GrantAttachment(mechanic,false);
        }

        public static int MasteryCap(int level)
        { return MasteryProgressionRules.Cap(level); }

        public int MasteryCoreTier(MasteryType mastery)
        {
            return Enum.IsDefined(typeof(MasteryType), mastery) && Profile.masteryCore == (int)mastery
                ? MasteryCoreRules.Tier(Profile.masteryRanks[(int)mastery]) : 0;
        }
        public bool HasMasteryCore(MasteryType mastery) { return MasteryCoreTier(mastery) > 0; }
        public int RefundableSkillRanks
        {
            get { int points = 0; foreach (int rank in Profile.skillRanks) points += Math.Max(0, rank - 1); return points; }
        }
        public bool RefundSkillRanks(bool inCamp)
        {
            if (!inCamp) return Fail("只能在营地退还技能进阶点。");
            if (RefundableSkillRanks == 0) return Fail("没有可退还的2/3阶；已学1阶技能保留。");
            GameProfile candidate = Snapshot();
            for (int i = 0; i < candidate.skillRanks.Length; i++) candidate.skillRanks[i] = Math.Min(1, candidate.skillRanks[i]);
            // Validation recalculates the shared point budget from level and ranks.
            return CommitCandidate(candidate);
        }

        public string MasteryLockReason(MasteryType mastery)
        {
            if (!Enum.IsDefined(typeof(MasteryType), mastery)) return "无效的精通。";
            int cap = MasteryCap(Profile.level);
            if (cap == 0) return MasteryProgressionRules.TierSummary+"；与技能共用点数，营地免费重置。";
            if (Profile.masteryRanks[(int)mastery] >= cap) return "已达当前等级精通上限 " + cap + "；"+MasteryProgressionRules.TierSummary;
            if (Profile.skillPoints < 1) return "需要1点技能点；可在营地重置精通。";
            return string.Empty;
        }

        public bool LearnMastery(MasteryType mastery)
        {
            string reason = MasteryLockReason(mastery);
            if (!string.IsNullOrEmpty(reason)) return Fail(reason);
            GameProfile candidate = Snapshot();
            candidate.masteryRanks[(int)mastery]++;
            return CommitCandidate(candidate);
        }

        public bool SelectMasteryCore(MasteryType mastery, bool inCamp)
        {
            if (!inCamp) return Fail("只能在营地切换精通核心。");
            if (!Enum.IsDefined(typeof(MasteryType), mastery) || Profile.masteryRanks[(int)mastery] < MasteryCoreRules.InitialInvestment)
                return Fail(MasteryProgressionRules.CoreSummary);
            GameProfile candidate = Snapshot(); candidate.masteryCore = (int)mastery;
            return CommitCandidate(candidate);
        }

        public bool ResetMastery(bool inCamp)
        {
            if (!inCamp) return Fail("只能在营地免费重置精通。");
            GameProfile candidate = Snapshot(); candidate.masteryRanks = new int[4]; candidate.masteryCore = -1;
            return CommitCandidate(candidate);
        }

        public int RefundableMasteryPoints
        {
            get { int points = 0; foreach (int rank in Profile.masteryRanks) points += Math.Max(0, rank); return points; }
        }
        public int RefundableBuildPoints { get { return RefundableSkillRanks + RefundableMasteryPoints; } }

        /// <summary>Camp-only detached allocation editor. No writes or Changed events
        /// occur until a single candidate (optionally including A/B) is persisted.</summary>
        public sealed class BuildDraft
        {
            private readonly ProgressionService owner, preview;
            private readonly GameProfile source;
            private readonly string fingerprint, slot;
            private readonly List<GameProfile> history = new List<GameProfile>();
            private bool completed;
            public string Error { get; private set; }
            internal BuildDraft(ProgressionService owner)
            {
                this.owner=owner;source=owner.Profile;slot=owner.CurrentSlotId;
                fingerprint=BuildFingerprint(source);
                preview=new ProgressionService(owner.saveDirectory);preview.Profile=owner.Snapshot();
            }
            private bool Attached { get { return !completed && owner.CurrentSlotId==slot && ReferenceEquals(source,owner.Profile); } }
            public bool IsCurrent { get { return Attached && fingerprint==owner.BuildStateFingerprint(); } }
            public ProgressionService CreatePracticeCopy() { return IsCurrent ? owner.CreatePracticeSnapshot(preview.Profile) : null; }
            public int Points { get { int spent=0;foreach(int rank in preview.Profile.skillRanks)spent+=rank;foreach(int rank in preview.Profile.masteryRanks)spent+=rank;return GameBalance.SkillPointBudget(source.level)-spent; } }
            public int Level { get { return source.level; } }
            public int Core { get { return preview.Profile.masteryCore; } }
            public bool CanUndo { get { return history.Count>0; } }
            public int OriginalSkillRank(int index){return index>=0&&index<GameBalance.SkillCount?source.skillRanks[index]:0;}
            public int OriginalMasteryRank(int index){return index>=0&&index<4?source.masteryRanks[index]:0;}
            public bool SkillChanged(int index){return OriginalSkillRank(index)!=SkillRank(index);}
            public bool MasteryChanged(int index){return OriginalMasteryRank(index)!=MasteryRank(index);}
            private static string CoreLabel(int core,int invested){return core<0?"无核心":BuildCatalog.MasteryName((MasteryType)core)+(MasteryCoreRules.Tier(invested)==2?" · 增强":" · 初阶");}
            public string CoreChangeSummary
            {
                get
                {
                    int oldCore=source.masteryCore;int oldRank=oldCore<0?0:OriginalMasteryRank(oldCore),newRank=Core<0?0:MasteryRank(Core);
                    string result="核心："+CoreLabel(oldCore,oldRank)+" → "+CoreLabel(Core,newRank);
                    if(oldCore>=0&&(Core!=oldCore||MasteryCoreRules.Tier(newRank)<MasteryCoreRules.Tier(oldRank)))result+=" · 失效："+CoreLabel(oldCore,oldRank)+(Core==oldCore?"增强效果":"能力");
                    return result;
                }
            }
            public string ChangeSummary
            {
                get
                {
                    var lines=new List<string>();int spent=0,refunded=0;
                    for(int i=0;i<GameBalance.SkillCount;i++)if(SkillChanged(i)){int delta=SkillRank(i)-OriginalSkillRank(i);spent+=Math.Max(0,delta);refunded+=Math.Max(0,-delta);lines.Add(GameBalance.SkillName(source.heroClass,i)+" "+OriginalSkillRank(i)+"→"+SkillRank(i)+"阶"+(delta<0?"（退阶）":""));}
                    for(int i=0;i<4;i++)if(MasteryChanged(i)){int delta=MasteryRank(i)-OriginalMasteryRank(i);spent+=Math.Max(0,delta);refunded+=Math.Max(0,-delta);lines.Add(BuildCatalog.MasteryName((MasteryType)i)+" "+OriginalMasteryRank(i)+"→"+MasteryRank(i));}
                    return "草稿改动 · 退回 "+refunded+" / 投入 "+spent+"点 · 剩余 "+Points+"点\n"+(lines.Count==0?"技能与精通点数未改变":string.Join("；",lines.ToArray()))+"\n"+CoreChangeSummary;
                }
            }
            public string SkillChangeEffects(int index)
            {
                if(index<0||index>=GameBalance.SkillCount||!SkillChanged(index))return "";
                int before=OriginalSkillRank(index),after=SkillRank(index);
                string text=(after<before?"退阶：撤回原阶效果，按新阶能力结算。":"进阶：按新阶能力结算。")+"\n原 "+before+"阶："+owner.SkillEffectSummary(index,before)+"\n新 "+after+"阶："+preview.SkillEffectSummary(index,after);
                if(!GameBalance.IsPassive(index))text+="\n基础冷却 "+GameBalance.EffectiveCooldown(source.heroClass,index,before).ToString("0.##")+" → "+GameBalance.EffectiveCooldown(source.heroClass,index,after).ToString("0.##")+"秒；"+(GameBalance.SkillEnergyCost(source.heroClass,index)==0?"无需能量":"消耗 "+GameBalance.SkillEnergyCost(source.heroClass,index).ToString("0.##"))+"（不变）";
                return text;
            }

            public int SkillRank(int index) { return index>=0&&index<GameBalance.SkillCount?preview.Profile.skillRanks[index]:0; }
            public int MasteryRank(int index) { return index>=0&&index<4?preview.Profile.masteryRanks[index]:0; }
            public StatBlock Stats { get { return preview.GetStats(); } }
            private bool Reject(string reason) { Error=reason;return false; }
            private void Remember()
            {
                if(history.Count==128)history.RemoveAt(0);
                history.Add(preview.Snapshot());Error=null;
            }
            public string SkillChangeReason(int index,int delta)
            {
                if(!Attached)return "角色资料已变化，请取消并重新打开草稿。";
                if(index<0||index>=GameBalance.SkillCount||(delta!=1&&delta!=-1))return "无效的技能调整。";
                int rank=SkillRank(index);
                if(rank<1)return "先在技能页学习1阶；草稿只调整已学技能的2/3阶。";
                if(delta<0)return rank<=1?"已学1阶保留，不可退还。":null;
                if(rank>=3)return "已达3阶上限。";
                if(source.level<GameBalance.SkillRankRequiredLevel(index,rank+1))return "等级不足，不能提升这一阶。";
                return Points<=0?"共享点数不足，请先退回其他投入。":null;
            }
            public bool ChangeSkill(int index,int delta)
            {
                if(!IsCurrent)return Reject("角色资料已变化，请取消并重新打开草稿。");
                string reason=SkillChangeReason(index,delta);if(reason!=null)return Reject(reason);
                Remember();preview.Profile.skillRanks[index]+=delta;return true;
            }
            public string MasteryChangeReason(int index,int delta)
            {
                if(!Attached)return "角色资料已变化，请取消并重新打开草稿。";
                if(index<0||index>=4||(delta!=1&&delta!=-1))return "无效的精通调整。";
                if(delta<0)return MasteryRank(index)<=0?"没有可退还的投入。":null;
                if(MasteryRank(index)>=MasteryCap(source.level))return "已达当前等级精通上限 "+MasteryCap(source.level)+"。";
                return Points<=0?"共享点数不足，请先退回其他投入。":null;
            }
            public bool ChangeMastery(int index,int delta)
            {
                if(!IsCurrent)return Reject("角色资料已变化，请取消并重新打开草稿。");
                string reason=MasteryChangeReason(index,delta);if(reason!=null)return Reject(reason);
                Remember();preview.Profile.masteryRanks[index]+=delta;
                if(Core==index&&MasteryRank(index)<MasteryCoreRules.InitialInvestment)preview.Profile.masteryCore=-1;
                return true;
            }
            public bool SelectCore(int index)
            {
                if(!IsCurrent)return Reject("角色资料已变化，请重新打开草稿。");
                if(index < -1||index>=4||index>=0&&MasteryRank(index)<MasteryCoreRules.InitialInvestment)return Reject("核心需该方向投入 "+MasteryCoreRules.InitialInvestment+"点。");
                if(index==Core)return true;Remember();preview.Profile.masteryCore=index;return true;
            }
            public string CoreThreshold(int index)
            {
                int rank=MasteryRank(index);
                return rank<MasteryCoreRules.InitialInvestment?"距核心 "+(MasteryCoreRules.InitialInvestment-rank)+"点":rank<MasteryCoreRules.EnhancedInvestment?"核心可选 · 距强化 "+(MasteryCoreRules.EnhancedInvestment-rank)+"点":"强化核心可选";
            }
            public bool Undo()
            {
                if(!IsCurrent||!CanUndo)return false;
                preview.Profile=history[history.Count-1];history.RemoveAt(history.Count-1);Error=null;return true;
            }
            public void Cancel() { completed=true;history.Clear(); }
            public bool Apply(bool inCamp,int saveSlot=-1)
            {
                if(!inCamp)return Reject("只能在营地应用草稿。");
                if(!IsCurrent)return Reject("角色资料已变化，请取消并重新打开草稿。");
                if(saveSlot < -1||saveSlot>=BuildPresetCount)return Reject("无效的方案位置。");
                BuildPreset preset=preview.CaptureBuild();string reason=owner.ValidateBuildPreset(preset);
                if(!string.IsNullOrEmpty(reason))return Reject(reason);
                GameProfile candidate=preview.Snapshot();candidate.skillPoints=Points;
                if(saveSlot>=0){EnsureBuildPresetSlots(candidate);candidate.buildPresets[saveSlot]=preset;}
                if(!owner.CommitCandidate(candidate,true))return Reject(owner.LastError);
                completed=true;history.Clear();Error=null;return true;
            }
        }
        public BuildDraft BeginBuildDraft(bool inCamp)
        {
            if(IsPracticeOnly){Fail("试招角色不能创建持久角色草稿。");return null;}
            if(!inCamp){Fail("只能在营地调整配点草稿。");return null;}
            return new BuildDraft(this);
        }

        /// <summary>One transaction, rather than two independently fallible refunds.
        /// Learned first ranks, prerequisites, equipment and hotbar identity remain.</summary>
        public bool ResetBuild(bool inCamp)
        {
            if (!inCamp) return Fail("只能在营地联合退点。");
            if (RefundableBuildPoints == 0) return Fail("没有可退还的技能进阶或精通投入；已学1阶技能保留。");
            GameProfile candidate = Snapshot();
            for (int i = 0; i < candidate.skillRanks.Length; i++) candidate.skillRanks[i] = Math.Min(1, candidate.skillRanks[i]);
            candidate.masteryRanks = new int[4]; candidate.masteryCore = -1;
            return CommitCandidate(candidate);
        }

        public bool HasBuildPreset(int slot)
        {
            return slot >= 0 && slot < BuildPresetCount && Profile.buildPresets != null &&
                slot < Profile.buildPresets.Length && Profile.buildPresets[slot] != null && Profile.buildPresets[slot].populated;
        }

        private BuildPreset CaptureBuild()
        {
            var preset=new BuildPreset
            {
                version = 1, populated = true, heroClass = Profile.heroClass,
                skillRanks = (int[])Profile.skillRanks.Clone(), masteryRanks = (int[])Profile.masteryRanks.Clone(),
                masteryCore = Profile.masteryCore, specialization = Profile.specialization, summonerRoute = Profile.summonerRoute,
                equippedSkills = (int[])Profile.equippedSkills.Clone(), hotbarKeys = (int[])Profile.hotbarKeys.Clone(), hotbarPage = Profile.hotbarPage,
                weaponId = Profile.weaponId, armorId = Profile.armorId, relicId = Profile.relicId,
                equipmentVariants = new[] { CapturedVariant(Profile.weaponId), CapturedVariant(Profile.armorId), CapturedVariant(Profile.relicId) },
                equipmentMechanicKnownMask = 7,
                equipmentMechanics = new[] { Equipped(ItemSlot.Weapon)?.mechanic??EquipmentMechanic.None, Equipped(ItemSlot.Armor)?.mechanic??EquipmentMechanic.None, Equipped(ItemSlot.Relic)?.mechanic??EquipmentMechanic.None }
            };
            CaptureAttachmentPreset(Profile,preset);return preset;
        }

        private int CapturedVariant(string id)
        {
            ItemData item=FindItem(id);
            return HasVariant(item) ? item.mechanicVariant : -1;
        }
        private static bool HasElementVariant(ItemData item)
        {return item!=null && BuildCatalog.HasMechanicVariant(item.mechanic);}

        public string CurrentBuildSummary() { return DescribeBuild(CaptureBuild()); }
        public string PracticeConfigurationSummary()
        {
            int spent=0;foreach(int rank in Profile.skillRanks)spent+=rank;foreach(int rank in Profile.masteryRanks)spent+=rank;
            string text="等级 "+Profile.level+" · 剩余配点 "+(GameBalance.SkillPointBudget(Profile.level)-spent)+"\n"+CurrentBuildSummary();
            foreach(string id in new[]{Profile.weaponId,Profile.armorId,Profile.relicId})
            {ItemData item=Profile.inventory.Find(x=>x!=null&&x.id==id);if(item!=null)text+="\n"+item.name+" · "+GameBalance.RarityName(item.rarity)+" · 强化 +"+item.upgradeLevel;}
            if(Profile.masteryCore>=0)text+="\n核心阶段："+(MasteryCoreRules.Tier(Profile.masteryRanks[Profile.masteryCore])==2?"增强":"初阶");
            return text;
        }

        public string BuildPresetSummary(int slot)
        { return HasBuildPreset(slot) ? DescribeBuild(Profile.buildPresets[slot]) : "空方案 · 可保存当前技能、精通、专精、快捷栏与穿戴装备"; }

        private string DescribeBuild(BuildPreset preset)
        {
            if (preset.version != 1 || preset.skillRanks == null || preset.skillRanks.Length != GameBalance.SkillCount ||
                preset.masteryRanks == null || preset.masteryRanks.Length != 4 || !Enum.IsDefined(typeof(HeroClass), preset.heroClass))
                return "方案数据无效或版本不兼容；可用当前配装覆盖。";
            int skills = 0, mastery = 0;
            var learned = new List<string>(); var tracks = new List<string>();
            for (int i = 0; i < preset.skillRanks.Length; i++)
            {
                int rank = Clamp(preset.skillRanks[i], 0, 3); skills += rank;
                if (rank > 0) learned.Add(GameBalance.SkillName(preset.heroClass, i) + " " + rank + "阶");
            }
            for (int i = 0; i < preset.masteryRanks.Length; i++)
            {
                int rank = Clamp(preset.masteryRanks[i], 0, MaximumMasteryRank); mastery += rank;
                if (rank > 0) tracks.Add(BuildCatalog.MasteryName((MasteryType)i) + " " + rank + "点");
            }
            string core = preset.masteryCore >= 0 && preset.masteryCore < 4 ? BuildCatalog.MasteryName((MasteryType)preset.masteryCore) : "无核心";
            string classChoice = preset.heroClass == HeroClass.Arcanist ? " · " + BuildCatalog.SpecializationName(preset.specialization) :
                preset.heroClass == HeroClass.Summoner ? (preset.summonerRoute == SummonerRoute.Bonded ? " · 双契" : " · 群契") : "";
            var equipment = new List<string>();
            foreach (string id in new[] { preset.weaponId, preset.armorId, preset.relicId })
            {
                ItemData item = Profile.inventory.Find(value => value != null && value.id == id);
                int slot=equipment.Count;
                string variant=preset.equipmentVariants!=null&&preset.equipmentVariants.Length==3&&preset.equipmentVariants[slot]>=0?" · 变体 "+(preset.equipmentVariants[slot]==0?"A":"B"):"";
                equipment.Add(item == null ? "装备缺失" : item.name+variant);
            }
            return GameBalance.ClassName(preset.heroClass) + classChoice + " · 技能 " + skills + " 点 · 精通 " + mastery + " 点 · " + core +
                "\n技能：" + (learned.Count == 0 ? "尚未学习" : string.Join("、", learned.ToArray())) +
                "\n精通：" + (tracks.Count == 0 ? "尚未投入" : string.Join("、", tracks.ToArray())) +
                "\n装备：" + string.Join(" / ", equipment.ToArray());
        }

        public string BuildPresetLockReason(int slot, bool inCamp)
        {
            if (!inCamp) return "只能在营地应用配装方案。";
            if (slot < 0 || slot >= BuildPresetCount) return "无效的配装方案位置。";
            if (!HasBuildPreset(slot)) return "这个位置还没有保存配装方案。";
            return ValidateBuildPreset(Profile.buildPresets[slot]);
        }

        public bool SaveBuildPreset(int slot, bool inCamp)
        {
            if (!inCamp) return Fail("只能在营地保存配装方案。");
            if (slot < 0 || slot >= BuildPresetCount) return Fail("无效的配装方案位置。");
            BuildPreset preset = CaptureBuild();
            string reason = ValidateBuildPreset(preset);
            if (!string.IsNullOrEmpty(reason)) return Fail(reason);
            GameProfile candidate = Snapshot();
            EnsureBuildPresetSlots(candidate);
            candidate.buildPresets[slot] = preset;
            return CommitCandidate(candidate);
        }

        public string BuildStateFingerprint(){return BuildFingerprint(Profile);}
        private static string BuildFingerprint(GameProfile profile)
        {
            // Recharge progress is runtime state, not an edit to a build/quote.
            var copy=JsonUtility.FromJson<GameProfile>(JsonUtility.ToJson(profile,true));
            copy.skillStockCounts=null;copy.skillStockRemaining=null;copy.skillStockPeriods=null;
            return JsonUtility.ToJson(copy,true);
        }
        public sealed class PresetEquipmentQuote
        {
            internal readonly ProgressionService Owner;internal readonly GameProfile Source;internal readonly string State;internal readonly int Plan,Slot,Variant;internal readonly string ItemId;
            public readonly string Summary,Error;
            internal PresetEquipmentQuote(ProgressionService owner,int plan,int slot,string id,int variant,string summary,string error)
            {Owner=owner;Source=owner.Profile;State=owner.BuildStateFingerprint();Plan=plan;Slot=slot;ItemId=id;Variant=variant;Summary=summary;Error=error;}
        }
        private static string PresetItemId(BuildPreset p,int slot){return slot==0?p.weaponId:slot==1?p.armorId:p.relicId;}
        private EquipmentMechanic? PresetMechanic(BuildPreset p,int slot)
        {
            var item=FindItem(PresetItemId(p,slot));
            if(item!=null&&item.slot==(ItemSlot)slot)return item.mechanic;
            if((p.equipmentMechanicKnownMask&(1<<slot))!=0&&p.equipmentMechanics!=null&&p.equipmentMechanics.Length==3&&Enum.IsDefined(typeof(EquipmentMechanic),p.equipmentMechanics[slot]))return p.equipmentMechanics[slot];
            return null;
        }
        public List<ItemData> PresetReplacementCandidates(int plan,ItemSlot slot)
        {
            var items=new List<ItemData>();if(!HasBuildPreset(plan)||!Enum.IsDefined(typeof(ItemSlot),slot))return items;
            var preset=Profile.buildPresets[plan];var mechanic=PresetMechanic(preset,(int)slot);
            int desired=preset.equipmentVariants!=null&&preset.equipmentVariants.Length==3?preset.equipmentVariants[(int)slot]:-1;
            foreach(var item in Profile.inventory)if(item!=null&&item.slot==slot)items.Add(item);
            items.Sort((a,b)=>{int n=(mechanic.HasValue&&b.mechanic==mechanic.Value).CompareTo(mechanic.HasValue&&a.mechanic==mechanic.Value);if(n!=0)return n;n=(desired!=1||HasVariant(b)).CompareTo(desired!=1||HasVariant(a));if(n!=0)return n;n=(b.level<=Profile.level).CompareTo(a.level<=Profile.level);if(n!=0)return n;n=b.level.CompareTo(a.level);return n!=0?n:string.CompareOrdinal(a.id,b.id);});return items;
        }
        public PresetEquipmentQuote QuotePresetReplacement(int plan,ItemSlot slot,string itemId,int variant=-2)
        {
            if(!HasBuildPreset(plan)||!Enum.IsDefined(typeof(ItemSlot),slot))return null;
            var preset=Profile.buildPresets[plan];if(preset.equipmentVariants!=null&&preset.equipmentVariants.Length!=3||preset.equipmentMechanics!=null&&preset.equipmentMechanics.Length!=3)return null;var item=FindItem(itemId);var old=FindItem(PresetItemId(preset,(int)slot));
            if(variant==-2)variant=preset.equipmentVariants!=null&&preset.equipmentVariants.Length==3?preset.equipmentVariants[(int)slot]:-1;
            if(variant==-1&&item!=null&&HasVariant(item))variant=item.mechanicVariant;
            string error=preset.heroClass!=Profile.heroClass?"方案职业不符。":item==null?"候选装备已不在背包。":item.slot!=slot?"候选部位不符。":item.mechanic!=EquipmentMechanic.None&&BuildCatalog.MechanicClass(item.mechanic)!=Profile.heroClass?"候选机制不适合当前职业。":item.level>Profile.level?"候选装备超过角色等级。":variant < -1||variant>1?"变体数据无效。":variant==1&&!HasVariant(item)?"候选机制尚未学习变体 B，请先学习或明确选择 A。":null;
            if(item!=null&&!HasVariant(item)&&variant==0)variant=-1;
            var before=PreviewEquippedItem(old);var after=PreviewEquippedItem(item);
            var previousMechanic=PresetMechanic(preset,(int)slot);
            string summary="方案 "+(plan==0?"A":"B")+" · "+GameBalance.SlotName(slot)+"："+(old==null?"原引用缺失":old.name)+" → "+(item==null?"候选缺失":item.name)+"\n机制："+(!previousMechanic.HasValue?"未知（旧方案未记录）":BuildCatalog.MechanicName(previousMechanic.Value))+" → "+BuildCatalog.MechanicName(item==null?EquipmentMechanic.None:item.mechanic)+" · 选择 "+(variant==1?"B":variant==0?"A":"无变体")+"\n等级："+(old==null?"未知":old.level.ToString())+" → "+(item==null?"未知":item.level.ToString())+"（角色 "+Profile.level+"）";
            if(after!=null)summary+="\n穿戴属性（继承部位强化）：攻 "+(before==null?"未知":before.attack.ToString())+" → "+after.attack+" / 防 "+(before==null?"未知":before.defense.ToString())+" → "+after.defense+" / 生命 "+(before==null?"未知":before.health.ToString())+" → "+after.health;
            summary+="\n仅更新该方案的此部位引用与选择；不穿戴、不改配点/快捷栏、不改另一方案。";
            return new PresetEquipmentQuote(this,plan,(int)slot,itemId,variant,summary,error);
        }
        public bool ReplacePresetEquipment(PresetEquipmentQuote quote,bool inCamp)
        {
            if(!inCamp||IsPracticeOnly)return Fail("只能在营地修改真实角色方案。");
            if(quote==null||quote.Owner!=this||quote.Source!=Profile||quote.State!=BuildStateFingerprint())return Fail("装备或角色已变化，请重新核对替换预览。");
            if(!string.IsNullOrEmpty(quote.Error))return Fail(quote.Error);
            var fresh=QuotePresetReplacement(quote.Plan,(ItemSlot)quote.Slot,quote.ItemId,quote.Variant);if(fresh==null||!string.IsNullOrEmpty(fresh.Error))return Fail("候选已失效，请重新选择。");
            var candidate=Snapshot();var preset=candidate.buildPresets[quote.Plan];
            if(quote.Slot==0)preset.weaponId=quote.ItemId;else if(quote.Slot==1)preset.armorId=quote.ItemId;else preset.relicId=quote.ItemId;
            if(preset.equipmentVariants==null)preset.equipmentVariants=new[]{-1,-1,-1};
            var capturedMechanics=new EquipmentMechanic[3];int knownMask=0;
            for(int slot=0;slot<3;slot++)
            {var known=PresetMechanic(preset,slot);if(known.HasValue){capturedMechanics[slot]=known.Value;knownMask|=1<<slot;}}
            preset.equipmentMechanics=capturedMechanics;preset.equipmentMechanicKnownMask=knownMask|(1<<quote.Slot);
            preset.equipmentVariants[quote.Slot]=quote.Variant;preset.equipmentMechanics[quote.Slot]=FindItem(quote.ItemId).mechanic;
            return CommitCandidate(candidate);
        }
        public string PresetReferences(string id)
        {
            if(string.IsNullOrEmpty(id))return string.Empty;var names=new List<string>();
            for(int i=0;i<BuildPresetCount;i++)if(HasBuildPreset(i))for(int slot=0;slot<3;slot++)if(PresetItemId(Profile.buildPresets[i],slot)==id){names.Add(i==0?"方案 A":"方案 B");break;}
            if(Profile.classStates!=null)for(int hero=0;hero<Profile.classStates.Length;hero++)
            {
                var state=Profile.classStates[hero];if(hero==(int)Profile.heroClass||state==null||!state.initialized||state.buildPresets==null)continue;
                for(int i=0;i<state.buildPresets.Length;i++)
                {
                    var preset=state.buildPresets[i];if(preset==null||!preset.populated)continue;
                    for(int slot=0;slot<3;slot++)if(PresetItemId(preset,slot)==id){names.Add(GameBalance.ClassName((HeroClass)hero)+" · "+(i==0?"方案 A":"方案 B"));break;}
                }
            }
            return string.Join(" / ",names.ToArray());
        }
        public string BulkSalePresetImpact()
        {
            var names=new List<string>();foreach(var item in Profile.inventory)if(item!=null&&!IsEquipped(Profile,item.id)&&!IsProtectedLoot(item)&&item.rarity<=Rarity.Rare){string refs=PresetReferences(item.id);if(refs.Length>0)names.Add(item.name+"（"+refs+"）");}
            return string.Join("、",names.ToArray());
        }

        public bool ApplyBuildPreset(int slot, bool inCamp)
        {
            string reason = BuildPresetLockReason(slot, inCamp);
            if (!string.IsNullOrEmpty(reason)) return Fail(reason);
            GameProfile candidate = Snapshot();
            BuildPreset preset = candidate.buildPresets[slot];
            // A preset saved before a new first-rank unlock cannot unlearn it or
            // return its point. The combined current cost was checked above.
            for (int i = 0; i < GameBalance.SkillCount; i++)
                candidate.skillRanks[i] = Math.Max(candidate.skillRanks[i] > 0 ? 1 : 0, preset.skillRanks[i]);
            candidate.masteryRanks = (int[])preset.masteryRanks.Clone(); candidate.masteryCore = preset.masteryCore;
            candidate.specialization = preset.specialization; candidate.summonerRoute = preset.summonerRoute;
            candidate.equippedSkills = (int[])preset.equippedSkills.Clone(); candidate.hotbarKeys = (int[])preset.hotbarKeys.Clone();
            candidate.hotbarPage = preset.hotbarPage;
            candidate.weaponId = preset.weaponId; candidate.armorId = preset.armorId; candidate.relicId = preset.relicId;
            if(preset.equipmentVariants!=null)
            {
                string[] ids={preset.weaponId,preset.armorId,preset.relicId};
                for(int i=0;i<3;i++)if(preset.equipmentVariants[i]>=0)
                    {var selected=candidate.inventory.Find(item=>item.id==ids[i]);selected.mechanicVariant=preset.equipmentVariants[i];selected.mechanicVariantUnlocked=true;}
            }
            ApplyAttachmentPreset(candidate,preset);
            return CommitCandidate(candidate);
        }

        private string ValidateBuildPreset(BuildPreset preset)
        {
            if (preset == null || !preset.populated || preset.version != 1 || preset.heroClass != Profile.heroClass)
                return "配装方案无效、职业不符或版本不兼容；未改变当前配装。";
            if (preset.skillRanks == null || preset.skillRanks.Length != GameBalance.SkillCount ||
                preset.masteryRanks == null || preset.masteryRanks.Length != 4)
                return "配装方案的技能或精通数据无效。";
            int spent = 0;
            for (int i = 0; i < GameBalance.SkillCount; i++)
            {
                int rank = preset.skillRanks[i];
                if (rank < 0 || rank > 3 || (rank > 0 && Profile.level < GameBalance.SkillRankRequiredLevel(i, rank)))
                    return "当前等级不足以应用方案中的技能进阶，或技能阶数无效。";
                // Only restore investment in this character's existing unlocks.
                // Grandfathered first ranks in imported saves remain grandfathered.
                if (rank > 0 && Profile.skillRanks[i] < 1) return "方案引用了当前角色尚未学习的技能；请先学习其前置与1阶。";
                spent += Math.Max(Profile.skillRanks[i] > 0 ? 1 : 0, rank);
            }
            foreach (int rank in preset.masteryRanks)
            {
                if (rank < 0 || rank > MasteryCap(Profile.level)) return "当前等级不足以应用方案中的精通投入，或精通点数无效。";
                spent += rank;
            }
            if (spent > GameBalance.SkillPointBudget(Profile.level)) return "当前技能点不足；方案还会保留保存后新学的1阶技能，未退点或改变配装。";
            if (preset.masteryCore < -1 || preset.masteryCore >= 4 ||
                (preset.masteryCore >= 0 && preset.masteryRanks[preset.masteryCore] < MasteryCoreRules.InitialInvestment))
                return "方案核心无效或该方向未投入10点。";
            if (!Enum.IsDefined(typeof(ElementalistSpecialization), preset.specialization) ||
                (Profile.heroClass != HeroClass.Arcanist && preset.specialization != ElementalistSpecialization.None) ||
                !Enum.IsDefined(typeof(SummonerRoute), preset.summonerRoute)) return "方案专精或契约路线无效。";
            if (preset.equippedSkills == null || preset.equippedSkills.Length != GameBalance.HotbarSize * GameBalance.HotbarPages ||
                preset.hotbarKeys == null || preset.hotbarKeys.Length != GameBalance.HotbarSize || preset.hotbarPage < 0 || preset.hotbarPage >= GameBalance.HotbarPages)
                return "方案快捷栏数据无效。";
            for (int page = 0; page < GameBalance.HotbarPages; page++)
            {
                var used = new HashSet<int>();
                for (int slot = 0; slot < GameBalance.HotbarSize; slot++)
                {
                    int entry = preset.equippedSkills[page * GameBalance.HotbarSize + slot];
                    // Locked default placeholders remain mapped; they never grant
                    // an unlock or permit casting an unlearned skill.
                    if (entry == -1) continue;
                    if ((entry != GameBalance.HotbarPotion && (entry < 0 || entry >= GameBalance.SkillCount || GameBalance.IsPassive(entry))) || !used.Add(entry))
                        return "方案快捷栏包含无效、重复或被动技能。";
                }
            }
            var keys = new HashSet<int>();
            foreach (int key in preset.hotbarKeys)
                if (!GameBalance.IsBindableKey(key) || !keys.Add(key)) return "方案快捷键无效或重复。";
            if(preset.mountedAttachments!=null)
            {
                if(preset.mountedAttachments.Length>BuildCatalog.MechanicsFor(Profile.heroClass).Length||preset.attachmentVariants==null||preset.attachmentVariants.Length!=preset.mountedAttachments.Length)return "方案挂件数据无效。";
                var mounted=new HashSet<EquipmentMechanic>();
                for(int i=0;i<preset.mountedAttachments.Length;i++)
                {
                    var mechanic=preset.mountedAttachments[i];var attachment=Attachment(mechanic);int variant=preset.attachmentVariants[i];
                    if(!mounted.Add(mechanic)||BuildCatalog.MechanicClass(mechanic)!=Profile.heroClass||attachment==null||variant<0||variant>1||variant==1&&!attachment.variantUnlocked)return "方案挂件缺失、重复或变体尚未解锁。";
                }
            }
            if(preset.equipmentVariants!=null && preset.equipmentVariants.Length!=3)return "方案变体数据无效。";
            string[] ids = { preset.weaponId, preset.armorId, preset.relicId };
            for (int slot = 0; slot < ids.Length; slot++)
            {
                if (string.IsNullOrEmpty(ids[slot]) || ids[slot].Length > 80) return "方案装备编号无效。";
                ItemData item = Profile.inventory.Find(value => value != null && value.id == ids[slot]);
                if (item == null || item.slot != (ItemSlot)slot || item.level > Profile.level)
                    return "方案装备已不在背包、部位不符或等级不足；请找回装备或重新保存方案。";
                if(preset.equipmentVariants!=null)
                {
                    int variant=preset.equipmentVariants[slot];
                    if(variant < -1 || variant>1 || variant>=0 && !HasVariant(item))
                        return "方案引用了未解锁或无效的装备变体；不会消耗材料或自动解锁。";
                }
            }
            return string.Empty;
        }

        private static void EnsureBuildPresetSlots(GameProfile profile)
        {
            // Added fields in older saves start empty. Reject excess populated
            // slots rather than silently discarding a hand-edited/imported build.
            if (profile.buildPresets != null && profile.buildPresets.Length > BuildPresetCount)
                for (int i = BuildPresetCount; i < profile.buildPresets.Length; i++)
                    if (profile.buildPresets[i] != null && profile.buildPresets[i].populated)
                        throw new ArgumentException("配装方案超过2份安全容量；保留原存档，请从备份恢复。");
            if (profile.buildPresets == null || profile.buildPresets.Length != BuildPresetCount)
            {
                var slots = new BuildPreset[BuildPresetCount];
                if (profile.buildPresets != null) Array.Copy(profile.buildPresets, slots, Math.Min(BuildPresetCount, profile.buildPresets.Length));
                profile.buildPresets = slots;
            }
            // Explicit empty objects keep inline Unity serialization independent
            // of its null-element behavior. The populated bit owns slot identity.
            for (int i = 0; i < BuildPresetCount; i++)
                if (profile.buildPresets[i] == null) profile.buildPresets[i] = new BuildPreset();
        }

        // Active profile fields are authoritative for the current class. Archives are
        // authoritative only for inactive classes. The active entry is a marker, never a
        // second serialized authority that can lag behind direct fields or event callbacks.
        private static ClassBuildState CaptureClassState(GameProfile p)
        {
            var state=new ClassBuildState
            {
                initialized=true,heroClass=p.heroClass,skillRanks=p.skillRanks,masteryRanks=p.masteryRanks,
                masteryCore=p.masteryCore,specialization=p.specialization,summonerRoute=p.summonerRoute,
                equippedSkills=p.equippedSkills,hotbarKeys=p.hotbarKeys,hotbarPage=p.hotbarPage,buildPresets=p.buildPresets,
                tutorialMask=p.tutorialMask,classTutorialCompleted=p.classTutorialCompleted,growthRevision=1,automaticGrowth=p.automaticGrowth,
                progressionGoal=p.progressionGoal,progressionGoalItemId=p.progressionGoalItemId,
                progressionGoalTier=p.progressionGoalTier,progressionGoalMechanic=p.progressionGoalMechanic,
                progressionGoalMinimumRarity=p.progressionGoalMinimumRarity,progressionGoalLevel=p.progressionGoalLevel
            };
            return JsonUtility.FromJson<ClassBuildState>(JsonUtility.ToJson(state,true));
        }
        private static void RestoreClassState(GameProfile p,ClassBuildState state)
        {
            p.skillRanks=state.skillRanks;p.masteryRanks=state.masteryRanks;p.masteryCore=state.masteryCore;
            p.specialization=state.specialization;p.summonerRoute=state.summonerRoute;
            p.equippedSkills=state.equippedSkills;p.hotbarKeys=state.hotbarKeys;p.hotbarPage=state.hotbarPage;p.buildPresets=state.buildPresets;
            p.tutorialMask=state.tutorialMask;p.classTutorialCompleted=state.classTutorialCompleted;
            p.automaticGrowth=state.automaticGrowth;
            p.progressionGoal=state.progressionGoal;p.progressionGoalItemId=state.progressionGoalItemId;
            p.progressionGoalTier=state.progressionGoalTier;p.progressionGoalMechanic=state.progressionGoalMechanic;
            p.progressionGoalMinimumRarity=state.progressionGoalMinimumRarity;p.progressionGoalLevel=state.progressionGoalLevel;
        }
        private static void NormalizeClassStates(GameProfile profile)
        {
            if(profile.classStateRevision<0||profile.classStateRevision>1)throw new ArgumentException("职业存档版本不受支持，请使用较新版本。");
            if(profile.classStateRevision==0)
            {
                profile.classStates=new ClassBuildState[4];
                for(int i=0;i<4;i++)profile.classStates[i]=new ClassBuildState{heroClass=(HeroClass)i};
                profile.classStateRevision=1;
            }
            if(profile.classStates==null||profile.classStates.Length!=4)throw new ArgumentException("职业存档必须包含四个固定位置；原文件已保留。");
            for(int i=0;i<4;i++)
            {
                var state=profile.classStates[i];
                if(state==null||!state.initialized){profile.classStates[i]=new ClassBuildState{heroClass=(HeroClass)i};continue;}
                if(state.version!=1||state.heroClass!=(HeroClass)i)throw new ArgumentException("职业存档版本或位置不符；原文件已保留。");
                if(state.growthRevision<1){state.automaticGrowth=state.progressionGoal==ProgressionGoalKind.None;state.growthRevision=1;}
                if(i==(int)profile.heroClass)continue;
                if(state.skillRanks==null||state.skillRanks.Length!=GameBalance.SkillCount||state.masteryRanks==null||state.masteryRanks.Length!=4)
                    throw new ArgumentException("其他职业的配点结构无效；原文件已保留。");
                int spent=0;
                for(int slot=0;slot<GameBalance.SkillCount;slot++)
                {
                    int rank=state.skillRanks[slot];
                    if(rank<0||rank>3||rank>0&&profile.level<GameBalance.SkillRankRequiredLevel(slot,rank))throw new ArgumentException("其他职业技能阶数无效；原文件已保留。");
                    spent+=rank;
                }
                foreach(int rank in state.masteryRanks){if(rank<0||rank>MasteryCap(profile.level))throw new ArgumentException("其他职业精通投入无效；原文件已保留。");spent+=rank;}
                if(spent>GameBalance.SkillPointBudget(profile.level))throw new ArgumentException("其他职业配点超出共享等级预算；原文件已保留。");
                if(state.buildPresets!=null&&state.buildPresets.Length>BuildPresetCount)throw new ArgumentException("其他职业方案超过安全容量；原文件已保留。");
            }
            profile.classStates[(int)profile.heroClass]=new ClassBuildState{initialized=true,heroClass=profile.heroClass};
        }
        internal sealed class ClassSwitchTransaction
        {
            internal ProgressionService Owner;internal GameProfile Source,Candidate;internal string Fingerprint;
            internal bool Committed,Published;
            public HeroClass Target {get{return Candidate.heroClass;}}
            internal StatBlock PreviewStats {get{return new ProgressionService(Candidate).GetStats();}}
            internal string PreviewSummary {get{return new ProgressionService(Candidate).CurrentBuildSummary();}}
        }
        internal ClassSwitchTransaction PrepareClassSwitch(HeroClass target,bool inCamp)
        {
            if(IsPracticeOnly||!inCamp||!Enum.IsDefined(typeof(HeroClass),target)||target==Profile.heroClass)
            {Fail("请在安全营地选择另一职业。");return null;}
            var candidate=Snapshot();ValidateProfile(candidate);
            candidate.classStates[(int)candidate.heroClass]=CaptureClassState(candidate);
            var state=candidate.classStates[(int)target];
            if(!state.initialized)
            {
                // First visit maps the already-legal shared skill indices and mastery budget.
                // Equipment, economy, world progress, receipts and pending draws stay shared.
                state=CaptureClassState(candidate);state.heroClass=target;
                state.specialization=ElementalistSpecialization.None;state.summonerRoute=SummonerRoute.Bonded;
                state.tutorialMask=0;state.classTutorialCompleted=false;state.buildPresets=new[]{new BuildPreset(),new BuildPreset()};
                state.progressionGoal=ProgressionGoalKind.None;state.progressionGoalItemId=null;state.progressionGoalMechanic=EquipmentMechanic.None;
                state.automaticGrowth=true;state.growthRevision=1;
                state.progressionGoalTier=1;state.progressionGoalLevel=0;state.progressionGoalMinimumRarity=Rarity.Common;
            }
            candidate.heroClass=target;RestoreClassState(candidate,state);ValidateProfile(candidate);
            LastError=string.Empty;
            return new ClassSwitchTransaction{Owner=this,Source=Profile,Candidate=candidate,Fingerprint=BuildStateFingerprint()};
        }
        internal bool CommitClassSwitch(ClassSwitchTransaction transaction,bool inCamp)
        {
            lock(StorageGate)
            {
            if(transaction==null||transaction.Owner!=this||transaction.Committed||!inCamp||IsPracticeOnly||transaction.Source!=Profile||transaction.Fingerprint!=BuildStateFingerprint())
                return Fail("职业切换预览已过期，请重新打开。");
            string failure;
            // Bootstrap the current class using the new envelope before a first switch.
            // The subsequent atomic replacement therefore leaves a v2 backup too: an old
            // client cannot fall back to a v1 backup and erase multiple class archives.
            if(!TryWriteAttachedProfile(Snapshot(),out failure)||!TryWriteAttachedProfile(transaction.Candidate,out failure))return Fail(failure);
            Profile=transaction.Candidate;transaction.Committed=true;LastError=string.Empty;
            // Intentionally no Changed event until the session has installed the prepared hero.
            return true;
            }
        }
        internal void PublishClassSwitch(ClassSwitchTransaction transaction)
        {
            if(transaction==null||transaction.Owner!=this||!transaction.Committed||transaction.Published||transaction.Candidate!=Profile)return;
            transaction.Published=true;bool prior=IsApplyingBuildDraft;IsApplyingBuildDraft=true;
            try{RaiseChanged();}finally{IsApplyingBuildDraft=prior;}
        }

        private GameProfile Snapshot()
        {
            var copy=CloneProfile(Profile);
            NormalizeEmptyChestDraw(copy);
            if(copy.pendingFashionChest&&pendingChestRoll!=null&&pendingChestRollPath==SaveFilePath&&pendingChestRollClears==copy.clearedRuns&&pendingChestRollTier==copy.pendingChestTier)
            {
                copy.pendingChestDraw=CopyChestRoll(pendingChestRoll);copy.chestRulesRevision=2;
                copy.pendingChestRulesRevision=pendingChestRoll.rulesRevision;copy.pendingChestLegacyGoldProtection=pendingChestRoll.legacyGoldProtection;
                if(string.IsNullOrEmpty(copy.pendingChestQualificationId))copy.pendingChestQualificationId=pendingChestRoll.id;
            }
            return copy;
        }
        // True only while publishing a successfully persisted draft candidate.
        // Nested non-draft transactions get their own false scope.
        internal bool IsApplyingBuildDraft { get; private set; }
        private bool CommitCandidate(GameProfile candidate,bool buildDraft=false)
        {
            SkillStockRules.Normalize(Profile);
            candidate.skillStockVersion=Profile.skillStockVersion;
            candidate.skillStockCounts=(int[])Profile.skillStockCounts.Clone();
            candidate.skillStockRemaining=(float[])Profile.skillStockRemaining.Clone();
            candidate.skillStockPeriods=(float[])Profile.skillStockPeriods.Clone();
            MigrateOwnedRewards(candidate);
            string failure;
            if (!TryWriteAttachedProfile(candidate, out failure)) return Fail(failure);
            Profile = candidate; LastError = string.Empty;
            bool previousDraft=IsApplyingBuildDraft;IsApplyingBuildDraft=buildDraft;
            try { RaiseChanged(); } finally { IsApplyingBuildDraft=previousDraft; }
            return true;
        }

        public const int SideEventReceiptLimit=32;
        public bool TryGrantSideEventReward(string receipt) { bool newlyCommitted;return TryGrantSideEventReward(receipt,out newlyCommitted); }
        public bool TryGrantSideEventReward(string receipt,out bool newlyCommitted)
        {
            newlyCommitted=false;Guid parsed;
            if(receipt==null||!Guid.TryParseExact(receipt,"N",out parsed))return Fail("晶核奖励收据无效。");
            receipt=parsed.ToString("N");
            List<string> receipts=NormalizeSideEventReceipts(Profile.sideEventRewardReceipts);
            if(receipts.Contains(receipt)){LastError=string.Empty;return true;}
            GameProfile candidate=Snapshot();candidate.sideEventRewardReceipts=receipts;
            if(receipts.Count==SideEventReceiptLimit)receipts.RemoveAt(0);
            receipts.Add(receipt);candidate.mechanicMaterials=Math.Min(999999,candidate.mechanicMaterials+1);
            if(!CommitCandidate(candidate))return false;
            newlyCommitted=true;return true;
        }
        private static List<string> NormalizeSideEventReceipts(List<string> source)
        {
            var result=new List<string>();if(source==null)return result;
            for(int i=source.Count-1;i>=0&&result.Count<SideEventReceiptLimit;i--)
            {Guid parsed;if(source[i]==null||!Guid.TryParseExact(source[i],"N",out parsed))continue;string id=parsed.ToString("N");if(!result.Contains(id))result.Add(id);}
            result.Reverse();return result;
        }

        public bool SetSummonerRoute(SummonerRoute route, bool inCamp)
        {
            if (!inCamp || Profile.heroClass != HeroClass.Summoner || !Enum.IsDefined(typeof(SummonerRoute), route))
                return Fail("在营地可选择群契或双契伙伴路线。");
            GameProfile candidate = Snapshot(); candidate.summonerRoute = route; return CommitCandidate(candidate);
        }

        public FashionData StrongestFashion(FashionSlot slot)
        {
            FashionData best = null;
            foreach (FashionData fashion in Profile.fashions)
                if (fashion != null && fashion.slot == slot && (best == null || fashion.rarity > best.rarity)) best = fashion;
            return best;
        }

        public bool ChooseLegendaryFashion(FashionSlot slot, bool inCamp)
        {
            if (!inCamp || !Enum.IsDefined(typeof(FashionSlot), slot)) return Fail("请在营地选择有效的时装部位。");
            string id = "fashion-" + (int)slot + "-3";
            if (Profile.fashions.Exists(x => x.id == id)) return Fail("已拥有该部位传说外观，无需兑换。");
            RestorePendingChestRoll();if(pendingChestRoll!=null)return Fail("请先继续开启已冻结的宝箱。");
            if (Profile.fashionThreads < FashionChoiceCost) return Fail("需要30缕星纹；每次开箱+1，重复时装额外增加。");
            GameProfile candidate = Snapshot(); candidate.fashionThreads -= FashionChoiceCost;
            candidate.fashions.Add(new FashionData { id = id, slot = slot, rarity = Rarity.Legendary, name = FashionName(slot, Rarity.Legendary,Profile.heroClass) });
            if(!CommitCandidate(candidate))return false;
            PublishRewardMoment(RewardMomentKind.FashionExchange,fashion:candidate.fashions.Find(f=>f.id==id),threads:-FashionChoiceCost);return true;
        }

        public bool ReforgeMechanic(string id, bool inCamp)
        { return ReforgeMechanic(QuoteReforge(id),inCamp); }

        public string AscensionLockReason(string id, bool inCamp)
        {
            if(!inCamp)return "只能在营地升华机制装备。";
            string reason=MechanicGoalEligibility(id,ProgressionGoalKind.Ascension);if(reason.Length>0)return reason;
            if (HighestAdventureTier < AscensionMilestone) return "任一冒险通关第5阶后开放传说升华。";
            if (Profile.mechanicMaterials < AscensionCost) return "升华需要24枚星烬碎片。";
            return string.Empty;
        }
        public bool AscendMechanic(string id, bool inCamp)
        {
            string reason = AscensionLockReason(id, inCamp);
            if (!string.IsNullOrEmpty(reason)) return Fail(reason);
            GameProfile candidate = Snapshot();
            ItemData item = candidate.inventory.Find(value => value.id == id);
            EnsureUpgradeBasis(item);
            item.rarity = Rarity.Legendary;
            item.baseAttack = (int)Math.Min(MaximumEquipmentStat, ((long)item.baseAttack * 25 + 17) / 18);
            item.baseDefense = (int)Math.Min(MaximumEquipmentStat, ((long)item.baseDefense * 25 + 17) / 18);
            item.baseHealth = (int)Math.Min(MaximumEquipmentHealth, ((long)item.baseHealth * 25 + 17) / 18);
            item.upgradeAnchorLevel = 0;
            item.upgradeAnchorAttack = item.baseAttack; item.upgradeAnchorDefense = item.baseDefense; item.upgradeAnchorHealth = item.baseHealth;
            ApplyUpgradeRank(item, IsEquipped(candidate, id) ? candidate.slotUpgradeRanks[(int)item.slot] : 0);
            var attachment=candidate.attachments.Find(a=>a.mechanic==item.mechanic);if(attachment!=null)attachment.rarity=Rarity.Legendary;
            candidate.mechanicMaterials -= AscensionCost;
            if(!CommitCandidate(candidate))return false;
            PublishRewardMoment(RewardMomentKind.Ascension,item,materials:-AscensionCost);return true;
        }

        public bool ToggleMechanicVariant(string id, bool inCamp)
        {
            string reason=VariantLockReason(id,inCamp);if(reason.Length>0)return Fail(reason);
            GameProfile candidate = Snapshot(); ItemData item = candidate.inventory.Find(x => x.id == id);
            if (!HasVariant(item)) candidate.mechanicMaterials -= VariantCost;
            if(candidate.variantKnowledge==null)candidate.variantKnowledge=new List<EquipmentMechanic>();
            if(!candidate.variantKnowledge.Contains(item.mechanic))candidate.variantKnowledge.Add(item.mechanic);
            candidate.variantKnowledgeRevision=1;
            item.mechanicVariantUnlocked = true; item.mechanicVariant = 1 - item.mechanicVariant;
            var attachment=candidate.attachments.Find(a=>a.mechanic==item.mechanic);if(attachment!=null){attachment.variantUnlocked=true;attachment.variant=item.mechanicVariant;}
            return CommitCandidate(candidate);
        }

        public string ChestOpenCaption {get{return pendingChestRoll!=null||Profile.pendingChestDraw!=null?"继续开启":"开启宝箱";}}
        public const int SingleChestRulesRevision=2,ThreadMaterialCost=6;
        public static string ChestChoiceName(int choice)
        { return choice == 0 ? "兵装" : choice == 1 ? "羽翼" : "补给"; } // Historical revision 1 only.
        public static string DungeonChestRules(int tier,ChestReward savedReward=null)
        {
            int minimum=TierRewardRules.ChestGoldMinimum(tier);
            return (savedReward!=null&&savedReward.rulesRevision<2?"旧版已保存奖励按原回执展示，不追加新箱收益。\n\n":"")+
                "一个通关宝箱，直接开启。金币 "+minimum+"～"+(minimum+40)+" + 1星烬碎片 + 1基础星纹。\n"+
                "额外时装：普通22% · 稀有12% · 史诗5% · 传说1% · 无时装60%。先抽品质，再随机补齐该品质尚缺的兵装或羽翼；两部位齐全才随机重复。\n"+
                "重复时装仍转为原金币与额外星纹。未抽签的旧资格仅此箱保留金币×1.5；已冻结抽签只继续原结果。\n"+
                "营地可手动用6星纹换1碎片，或30星纹自选未有传说；不自动兑换。实际到账受余额上限影响。基础通关碎片独立结算，跳过动画不改变收益。";
        }
        private static void NewChestQualification(GameProfile candidate,int tier,string id)
        {
            candidate.chestRulesRevision=SingleChestRulesRevision;
            candidate.pendingFashionChest=true;candidate.pendingChestTier=TierRewardRules.ClampTier(tier);
            candidate.pendingChestRulesRevision=SingleChestRulesRevision;candidate.pendingChestLegacyGoldProtection=false;
            candidate.pendingChestQualificationId=id;candidate.pendingChestDraw=null;
        }
        public bool PrepareDungeonChest(int tier=1)
        {
            if(Profile.pendingFashionChest&&Profile.clearedRuns<=Profile.materialRewardedClears){LastError=string.Empty;return true;}
            var candidate=Snapshot();
            if(!candidate.pendingFashionChest)NewChestQualification(candidate,tier,Guid.NewGuid().ToString("N"));
            if(candidate.clearedRuns>candidate.materialRewardedClears)
            {candidate.mechanicMaterials=Clamp(candidate.mechanicMaterials+TierRewardRules.ClearMaterials(tier),0,999999);candidate.materialRewardedClears=candidate.clearedRuns;}
            if(!candidate.firstClearRewardClaimed&&candidate.clearedRuns>0)candidate.pendingFirstClearReward=true;
            return CommitCandidate(candidate);
        }
        internal static ChestReward BuildSingleChestRoll(GameProfile profile,int qualityRoll,int slotRoll,int goldRoll,bool protectLegacy,string id)
        {
            if(slotRoll<0||slotRoll>1||goldRoll<0||goldRoll>40)throw new ArgumentOutOfRangeException("roll");
            var rarity=RollFashionRarity(qualityRoll);int gold=TierRewardRules.ChestGoldMinimum(profile.pendingChestTier)+goldRoll;
            var roll=new ChestReward{rulesRevision=2,rewardKind=ChestRewardKind.SingleChest,id=id,choice=-1,gold=protectLegacy?gold*3/2:gold,
                rarityIndex=rarity.HasValue?(int)rarity.Value:-1,materialKind=RewardMaterialKind.StarAshFragment,materials=1,legacyGoldProtection=protectLegacy};
            if(rarity.HasValue)
            {
                bool weapon=profile.fashions.Exists(x=>x.slot==FashionSlot.Weapon&&x.rarity==rarity.Value);
                bool wings=profile.fashions.Exists(x=>x.slot==FashionSlot.Wings&&x.rarity==rarity.Value);
                FashionSlot slot=weapon&&!wings?FashionSlot.Wings:wings&&!weapon?FashionSlot.Weapon:(FashionSlot)slotRoll;
                roll.slotIndex=(int)slot;roll.duplicate=weapon&&wings;roll.name=FashionName(slot,rarity.Value,profile.heroClass);
            }
            return roll;
        }
        // Kept solely for an already-frozen legacy request. Integers 0/1/2 never
        // acquire the meaning of a new reward kind or select a new draw.
        public string OpenDungeonChest(int legacyChoice)
        {
            RestorePendingChestRoll();
            if(pendingChestRoll==null||pendingChestRoll.rulesRevision>=2||pendingChestRoll.choice!=legacyChoice)
            {Fail(pendingChestRoll==null?"请直接开启通关宝箱。":"上次开箱已冻结，请使用继续开启；旧抽签不改变结果。");return null;}
            return OpenDungeonChest();
        }
        public string OpenDungeonChest()
        {
            if(IsPracticeOnly){Fail("试招期间不能开启真实宝箱。");return null;}
            if(!Profile.pendingFashionChest||Profile.pendingChestReveal){Fail("当前没有可开启的宝箱；已保存奖励不会重抽。");return null;}
            RestorePendingChestRoll();
            if(pendingChestRoll!=null&&(pendingChestRollPath!=SaveFilePath||pendingChestRollClears!=Profile.clearedRuns||pendingChestRollTier!=Profile.pendingChestTier))pendingChestRoll=null;
            if(pendingChestRoll==null)
            {
                if(Profile.gold>=MaximumGold&&Profile.fashionThreads>=999999&&Profile.mechanicMaterials>=999999)
                {Fail("金币、星纹与碎片均已满，请先使用资源再开启。");return null;}
                bool legacy=Profile.pendingChestRulesRevision<2;
                pendingChestRoll=BuildSingleChestRoll(Profile,random.Next(100),random.Next(2),random.Next(41),legacy,Guid.NewGuid().ToString("N"));
                pendingChestRollPath=SaveFilePath;pendingChestRollClears=Profile.clearedRuns;pendingChestRollTier=Profile.pendingChestTier;
                pendingChestContexts[SaveFilePath]=new PendingChestContext(CopyChestRoll(pendingChestRoll),pendingChestRollClears,pendingChestRollTier);
            }
            // Persist the exact draw before consuming eligibility or granting anything.
            // A failed first write retains the same process-local roll; a failed grant
            // can also retry after restart from the durable frozen qualification.
            if(Profile.pendingChestDraw==null)
            {
                var frozen=Snapshot();string freezeFailure;
                if(!TryWriteAttachedProfile(frozen,out freezeFailure)){Fail(freezeFailure);return null;}
                Profile=frozen;
            }
            var candidate=Snapshot();var roll=pendingChestRoll;
            var receipt=new ChestReward{rulesRevision=roll.rulesRevision,rewardKind=roll.rewardKind,id=roll.id,choice=roll.choice,gold=roll.gold,
                baseGold=roll.gold,baseThreads=1,materialKind=roll.materialKind,materials=roll.materials,legacyGoldProtection=roll.legacyGoldProtection,name=roll.rulesRevision>=2?"通关资源":"金币"};
            var rarity=roll.Rarity;
            if(rarity.HasValue)
            {
                FashionSlot slot=roll.rulesRevision>=2?(FashionSlot)roll.slotIndex:roll.choice==0?FashionSlot.Weapon:FashionSlot.Wings;
                string id="fashion-"+(int)slot+"-"+(int)rarity.Value;
                bool owned=candidate.fashions.Exists(x=>x.id==id);
                receipt.rarityIndex=(int)rarity.Value;receipt.slotIndex=(int)slot;receipt.name=FashionName(slot,rarity.Value,candidate.heroClass);
                receipt.duplicate=roll.rulesRevision>=2?roll.duplicate:owned;
                if(!receipt.duplicate&&owned){Fail("冻结奖励的收藏状态已改变，请保留存档并恢复原资格；不会重抽。");return null;}
                if(receipt.duplicate)
                {receipt.duplicateGold=new[]{40,100,250,800}[(int)rarity.Value];receipt.duplicateThreads=new[]{1,2,4,8}[(int)rarity.Value];receipt.gold+=receipt.duplicateGold;}
                else candidate.fashions.Add(new FashionData{id=id,slot=slot,rarity=rarity.Value,name=receipt.name});
            }
            candidate.gold=(int)Math.Min(MaximumGold,(long)candidate.gold+receipt.gold);
            candidate.fashionThreads=Clamp(candidate.fashionThreads+receipt.baseThreads+receipt.duplicateThreads,0,999999);
            candidate.mechanicMaterials=Clamp(candidate.mechanicMaterials+receipt.materials,0,999999);
            receipt.hasCurrencyDeltas=true;receipt.goldDelta=candidate.gold-Profile.gold;receipt.threadsDelta=candidate.fashionThreads-Profile.fashionThreads;
            receipt.materialsDelta=candidate.mechanicMaterials-Profile.mechanicMaterials;
            if(receipt.goldDelta==0&&receipt.threadsDelta==0&&receipt.materialsDelta==0&&(!rarity.HasValue||receipt.duplicate))
            {Fail("本次冻结奖励的资源已达上限，请先使用资源；宝箱资格保留。");return null;}
            receipt.summary=(receipt.rulesRevision>=2?"通关宝箱":ChestChoiceName(receipt.choice)+"箱")+"：金币 +"+receipt.goldDelta+" · 星纹 +"+receipt.threadsDelta+
                (receipt.materialKind==RewardMaterialKind.StarAshFragment?" · 星烬碎片 +"+receipt.materialsDelta:"")+(rarity.HasValue?" · "+receipt.name+(receipt.duplicate?"（重复转化）":""):"");
            candidate.pendingFashionChest=false;candidate.pendingChestDraw=null;candidate.lastChestReward=receipt;candidate.pendingChestReveal=true;
            string failure;if(!TryWriteAttachedProfile(candidate,out failure)){Fail(failure);return null;}
            pendingChestRoll=null;pendingChestContexts.Remove(SaveFilePath);Profile=candidate;LastError=string.Empty;RaiseChanged();return receipt.summary;
        }
        public sealed class ThreadMaterialQuote
        {
            internal ProgressionService Owner;internal GameProfile Source;internal string Fingerprint,Slot;
            public string Id {get;internal set;}public long Sequence {get;internal set;}
        }
        public ThreadMaterialQuote QuoteThreadMaterialExchange()
        {return new ThreadMaterialQuote{Owner=this,Source=Profile,Fingerprint=BuildStateFingerprint(),Slot=CurrentSlotId,Id=Guid.NewGuid().ToString("N"),Sequence=Profile.threadMaterialSequence+1};}
        public string ThreadMaterialExchangeLockReason(bool inCamp)
        {return IsPracticeOnly||!inCamp?"只能在营地兑换。":Profile.mechanicMaterials>=999999?"星烬碎片已达上限。":Profile.fashionThreads<ThreadMaterialCost?"需要6星纹。":Profile.threadMaterialSequence==long.MaxValue?"兑换流水已达上限。":string.Empty;}
        public bool ExchangeThreadsForMaterial(ThreadMaterialQuote quote,bool inCamp)
        {
            if(IsPracticeOnly||!inCamp)return Fail("只能在营地兑换。");
            if(quote==null)return Fail("兑换请求无效。");
            if(Profile.lastThreadMaterialReceipt!=null&&Profile.lastThreadMaterialReceipt.id==quote.Id&&Profile.lastThreadMaterialReceipt.sequence==quote.Sequence){LastError=string.Empty;return true;}
            string reason=ThreadMaterialExchangeLockReason(inCamp);if(reason.Length>0)return Fail(reason);
            if(quote.Owner!=this||quote.Source!=Profile||quote.Slot!=CurrentSlotId||quote.Fingerprint!=BuildStateFingerprint()||quote.Sequence!=Profile.threadMaterialSequence+1)return Fail("兑换预览已过期，请重新核对。");
            var candidate=Snapshot();candidate.chestRulesRevision=2;candidate.fashionThreads-=ThreadMaterialCost;candidate.mechanicMaterials++;candidate.threadMaterialSequence=quote.Sequence;
            candidate.lastThreadMaterialReceipt=new MaterialExchangeReceipt{id=quote.Id,sequence=quote.Sequence,materialKind=RewardMaterialKind.StarAshFragment,threadsDelta=-ThreadMaterialCost,materialsDelta=1};
            if(!CommitCandidate(candidate))return false;
            PublishRewardMoment(RewardMomentKind.MaterialExchange,materials:1,threads:-ThreadMaterialCost);return true;
        }

        public bool AcknowledgeChestReward()
        {
            if (!Profile.pendingChestReveal) return Fail("当前没有待展示的宝箱奖励。");
            GameProfile candidate = CloneProfile(Profile);
            candidate.pendingChestReveal = false;
            string failure;
            if (!TryWriteAttachedProfile(candidate, out failure)) return Fail(failure);
            Profile = candidate;
            LastError = string.Empty;
            RaiseChanged();
            return true;
        }

        public bool TravelToHub(int hub)
        {
            int mask=HubTravelRules.UnlockedMask(Profile.unlockedHubMask,Profile.level,Profile.clearedRuns);
            if(!HubTravelRules.IsUnlocked(mask,hub))return Fail("城镇尚未解锁："+HubTravelRules.UnlockHint(hub));
            if(Math.Abs(hub-Profile.currentHub)>1)return Fail("请沿地图逐站旅行，先到相邻城镇。");
            GameProfile candidate=Snapshot();candidate.currentHub=hub;candidate.unlockedHubMask=mask;return CommitCandidate(candidate);
        }

        public int HighestAdventureTier {get{return Clamp(Math.Max(Profile.highestAdventureTier,Profile.bestFloor),0,100);}}
        public int HighestUnlockedAdventureTier {get{return Math.Min(100,HighestAdventureTier+1);}}
        public bool RecordTutorialEvidence(int bit)
        {
            if(bit!=1 && bit!=2 && bit!=8)return Fail("无效的实战事件。");
            if((Profile.tutorialMask&bit)!=0){LastError=string.Empty;return true;}
            GameProfile candidate=Snapshot();candidate.tutorialMask|=bit;return CommitCandidate(candidate);
        }
        public bool RecordClassTutorialEvidence(HeroClass hero)
        {
            if(hero!=Profile.heroClass)return Fail("职业事件不匹配。");
            if(Profile.classTutorialCompleted){LastError=string.Empty;return true;}
            GameProfile candidate=Snapshot();candidate.classTutorialCompleted=true;return CommitCandidate(candidate);
        }
        public bool ClassTutorialUsable
        {
            get
            {
                if(Profile.classTutorialCompleted)return true;
                // Counterattack follows the innate perfect dodge; other lessons need their actual skill.
                int skill=Profile.heroClass==HeroClass.Arcanist?1:Profile.heroClass==HeroClass.Summoner?2:0;
                return Profile.heroClass==HeroClass.Vanguard || Profile.skillRanks[skill]>0;
            }
        }
        public string ClassTutorialText
        {
            get
            {
                switch(Profile.heroClass)
                {
                    case HeroClass.Vanguard:return "完美闪避后，反击命中";
                    case HeroClass.Arcanist:return "碎冰命中，或有效刷新灼烧";
                    case HeroClass.Ranger:return "引爆敌人的三层毒";
                    default:return "指挥伙伴后，让伙伴命中目标";
                }
            }
        }
        public string MechanicGoalEligibility(string id,ProgressionGoalKind kind)
        {
            ItemData item=FindItem(id);
            if(item==null||item.mechanic==EquipmentMechanic.None||!Enum.IsDefined(typeof(EquipmentMechanic),item.mechanic)||
                BuildCatalog.MechanicClass(item.mechanic)!=Profile.heroClass||item.slot!=BuildCatalog.MechanicSlot(item.mechanic))return "请选择背包中的本职业机制装备。";
            if(kind==ProgressionGoalKind.Variant)return HasElementVariant(item)?string.Empty:"这件装备没有机制变体。";
            if(!HasDiscoveredMechanic(item.mechanic))return "请先登记这件装备的机制配方。";
            if(kind==ProgressionGoalKind.Ascension&&item.rarity!=Rarity.Epic)
                return item.rarity==Rarity.Legendary?"已是传说品质，不会重复升华。":"先获取"+BuildCatalog.MechanicName(item.mechanic)+"的史诗装备，再选择升华目标。";
            if(kind==ProgressionGoalKind.Reforge&&item.level>=Profile.level)return "装备已达到当前角色等级。";
            return string.Empty;
        }
        public string ReforgeLockReason(string id,bool inCamp)
        { return ReforgeLockReason(QuoteReforge(id),inCamp); }
        public bool HasVariant(ItemData item)
        {return item!=null&&BuildCatalog.HasMechanicVariant(item.mechanic)&&BuildCatalog.MechanicClass(item.mechanic)==Profile.heroClass&&item.slot==BuildCatalog.MechanicSlot(item.mechanic)&&
            (Profile.variantKnowledge!=null&&Profile.variantKnowledge.Contains(item.mechanic)||Profile.variantKnowledgeRevision<1&&item.mechanicVariantUnlocked);}
        private static void NormalizeVariantKnowledge(GameProfile profile)
        {
            var learned=new List<EquipmentMechanic>();
            if(profile.variantKnowledge!=null)foreach(var mechanic in profile.variantKnowledge)
                if(BuildCatalog.HasMechanicVariant(mechanic)&&!learned.Contains(mechanic))learned.Add(mechanic);
            var items=new List<ItemData>(profile.inventory);items.AddRange(profile.pendingLoot);items.AddRange(profile.recoveryLoot);
            if(profile.variantKnowledgeRevision<1)foreach(var item in items)
                if(item.mechanicVariantUnlocked&&BuildCatalog.HasMechanicVariant(item.mechanic)&&item.slot==BuildCatalog.MechanicSlot(item.mechanic)&&!learned.Contains(item.mechanic))learned.Add(item.mechanic);
            profile.variantKnowledge=learned;
            foreach(var item in items)
            {item.mechanicVariantUnlocked=learned.Contains(item.mechanic);if(!item.mechanicVariantUnlocked)item.mechanicVariant=0;}
        }
        public string VariantLockReason(string id,bool inCamp)
        {
            if(!inCamp)return "只能在营地切换元素机制变体。";
            string reason=MechanicGoalEligibility(id,ProgressionGoalKind.Variant);if(reason.Length>0)return reason;
            return !HasVariant(FindItem(id))&&Profile.mechanicMaterials<VariantCost?"本角色首次学习该机制变体需要4枚碎片，同机制装备之后可免费选择。":string.Empty;
        }
        public bool UnlockMechanicVariant(string id,bool inCamp)
        {
            string reason=VariantLockReason(id,inCamp);if(reason.Length>0)return Fail(reason);
            if(HasVariant(FindItem(id))){LastError=string.Empty;return true;}
            return ToggleMechanicVariant(id,inCamp);
        }
        public bool SelectCoreGoal(EquipmentMechanic mechanic,Rarity minimumRarity=Rarity.Common)
        {
            if(mechanic==EquipmentMechanic.None||!Enum.IsDefined(typeof(EquipmentMechanic),mechanic)||BuildCatalog.MechanicClass(mechanic)!=Profile.heroClass||
                (minimumRarity!=Rarity.Common&&minimumRarity!=Rarity.Epic))return Fail("请选择本职业的具体核心目标。");
            if(Profile.progressionGoal==ProgressionGoalKind.Core&&Profile.progressionGoalMechanic==mechanic&&Profile.progressionGoalMinimumRarity==minimumRarity)
            {LastError=string.Empty;return true;}
            GameProfile candidate=Snapshot();candidate.automaticGrowth=false;candidate.progressionGoal=ProgressionGoalKind.Core;candidate.progressionGoalMechanic=mechanic;
            candidate.progressionGoalMinimumRarity=minimumRarity;candidate.progressionGoalItemId=null;candidate.progressionGoalLevel=0;candidate.progressionGoalTier=1;
            return CommitCandidate(candidate);
        }
        public bool SelectProgressionGoal(ProgressionGoalKind goal,string itemId=null,int tier=0,int targetLevel=0)
        {
            if(!Enum.IsDefined(typeof(ProgressionGoalKind),goal))return Fail("无效目标。");
            bool itemGoal=goal==ProgressionGoalKind.Variant||goal==ProgressionGoalKind.Ascension||goal==ProgressionGoalKind.Reforge;
            if(itemGoal){string reason=MechanicGoalEligibility(itemId,goal);if(reason.Length>0)return Fail(reason);}
            if(goal==ProgressionGoalKind.Tier&&(tier<1||tier>100))return Fail("目标阶数无效。");
            int level=goal==ProgressionGoalKind.Reforge?EquipmentGenerationLevel(targetLevel==0?Profile.level:targetLevel):0;
            if(goal==ProgressionGoalKind.Reforge&&QuoteReforge(itemId,level)==null)return Fail("重铸目标等级无效。");
            if(Profile.progressionGoal==goal&&(goal!=ProgressionGoalKind.Core||Profile.progressionGoalMechanic==EquipmentMechanic.None)&&Profile.progressionGoalItemId==(itemGoal?itemId:null)&&
                (goal!=ProgressionGoalKind.Tier||Profile.progressionGoalTier==tier)&&Profile.progressionGoalLevel==level)
            {LastError=string.Empty;return true;}
            GameProfile candidate=Snapshot();candidate.automaticGrowth=false;candidate.progressionGoal=goal;candidate.progressionGoalItemId=itemGoal?itemId:null;
            candidate.progressionGoalTier=goal==ProgressionGoalKind.Tier?tier:1;candidate.progressionGoalLevel=level;
            candidate.progressionGoalMechanic=itemGoal?FindItem(itemId).mechanic:EquipmentMechanic.None;candidate.progressionGoalMinimumRarity=Rarity.Common;
            return CommitCandidate(candidate);
        }
        private ItemData GoalCoreItem(List<ItemData> items)
        {
            if(items==null)return null;
            foreach(ItemData item in items)if(item!=null&&item.mechanic==Profile.progressionGoalMechanic&&item.rarity>=Profile.progressionGoalMinimumRarity&&
                item.slot==BuildCatalog.MechanicSlot(item.mechanic))return item;
            return null;
        }
        public ProgressionGoalState SelectedProgressionGoal(bool inCamp=false)
        {
            if(Profile.automaticGrowth)return AutomaticGoal(Profile,inCamp);
            var migrated=MigratedAttachmentGoal(inCamp);if(migrated!=null)return migrated;
            var goal=new ProgressionGoalState{Identity=Profile.progressionGoal.ToString(),Title="选择一个成长目标",Step="在营地选择目标"};
            ItemData item=FindItem(Profile.progressionGoalItemId);
            switch(Profile.progressionGoal)
            {
                case ProgressionGoalKind.Core:
                    if(Profile.progressionGoalMechanic==EquipmentMechanic.None)
                    {goal.Identity+="/legacy";goal.Title="首件机制装备（旧目标）";goal.Done=Profile.firstClearRewardClaimed||Profile.discoveredMechanics.Count>0;goal.Step="请选择要追踪的具体核心；旧目标未指定机制";break;}
                    goal.Identity+="/"+(int)Profile.progressionGoalMechanic+"/"+(int)Profile.progressionGoalMinimumRarity;
                    goal.Title="获取"+(Profile.progressionGoalMinimumRarity==Rarity.Epic?"史诗·":"")+BuildCatalog.MechanicName(Profile.progressionGoalMechanic);
                    item=GoalCoreItem(Profile.inventory);
                    if(item!=null)
                    {
                        goal.Done=true;goal.ItemId=item.id;bool equipped=IsEquipped(Profile,item.id);
                        goal.Step=equipped?"目标核心已穿戴":item.level>Profile.level?"角色达到 "+item.level+" 级后可穿戴":"目标核心已入手，可穿戴检查路线";
                        if(!equipped){goal.Action=ProgressionGoalAction.Equip;goal.CanAct=inCamp&&item.level<=Profile.level;}
                        break;
                    }
                    item=GoalCoreItem(Profile.pendingLoot);bool recovery=false;
                    if(item==null){item=GoalCoreItem(Profile.recoveryLoot);recovery=item!=null;}
                    if(item!=null)
                    {goal.ItemId=item.id;goal.Action=recovery?ProgressionGoalAction.ClaimRecovery:ProgressionGoalAction.ClaimPending;goal.CanAct=inCamp&&Profile.inventory.Count<InventoryCapacity;goal.Step=Profile.inventory.Count<InventoryCapacity?"在营地领取指定装备入背包":"先腾出背包位置，再领取目标装备";break;}
                    bool first=Profile.pendingFirstClearReward&&!Profile.firstClearRewardClaimed;
                    goal.MaterialCost=first?0:MechanicExchangeCost;goal.Action=first?ProgressionGoalAction.ClaimCore:ProgressionGoalAction.ExchangeCore;
                    goal.CanAct=inCamp&&(first||Profile.mechanicMaterials>=MechanicExchangeCost);
                    goal.Step=first?"首通自选可领取这件挂件":"在营地定向兑换这件史诗挂件";break;
                case ProgressionGoalKind.Variant:case ProgressionGoalKind.Ascension:case ProgressionGoalKind.Reforge:
                    goal.Identity+="/"+Profile.progressionGoalItemId+(Profile.progressionGoal==ProgressionGoalKind.Reforge?"/"+Profile.progressionGoalLevel:"");
                    goal.ItemId=Profile.progressionGoalItemId;
                    goal.Title=(Profile.progressionGoal==ProgressionGoalKind.Variant?"解锁变体":Profile.progressionGoal==ProgressionGoalKind.Ascension?"传说升华":"重铸至 "+Profile.progressionGoalLevel+" 级")+" · "+(item==null?"原目标装备":item.name);
                    if(item==null){goal.Step="原目标装备不在背包；同名装备不会替代它";break;}
                    goal.Done=Profile.progressionGoal==ProgressionGoalKind.Variant?HasVariant(item):Profile.progressionGoal==ProgressionGoalKind.Ascension?item.rarity==Rarity.Legendary:item.level>=Profile.progressionGoalLevel;
                    if(goal.Done){goal.Step="保留当前目标，可自行选择下一目标";break;}
                    goal.MaterialCost=Profile.progressionGoal==ProgressionGoalKind.Variant?VariantCost:Profile.progressionGoal==ProgressionGoalKind.Ascension?AscensionCost:0;
                    goal.Action=Profile.progressionGoal==ProgressionGoalKind.Variant?ProgressionGoalAction.UnlockVariant:Profile.progressionGoal==ProgressionGoalKind.Ascension?ProgressionGoalAction.Ascend:ProgressionGoalAction.Reforge;
                    if(Profile.progressionGoal==ProgressionGoalKind.Reforge){goal.ReforgeQuote=QuoteReforge(item.id,Profile.progressionGoalLevel);goal.GoldCost=goal.ReforgeQuote==null?0:goal.ReforgeQuote.GoldCost;}
                    string reason=Profile.progressionGoal==ProgressionGoalKind.Variant?VariantLockReason(item.id,inCamp):Profile.progressionGoal==ProgressionGoalKind.Ascension?AscensionLockReason(item.id,inCamp):ReforgeLockReason(goal.ReforgeQuote,inCamp);
                    goal.CanAct=reason.Length==0;goal.Step=goal.CanAct?"营地可执行这件装备的操作":reason;
                    if(Profile.progressionGoal==ProgressionGoalKind.Ascension)goal.RequiredAdventureTier=AscensionMilestone;
                    break;
                case ProgressionGoalKind.SecondPreset:
                    goal.Title="保存第二套配装";goal.Done=HasBuildPreset(0)&&HasBuildPreset(1);goal.Step=goal.Done?"两份方案已保存":"在营地保存方案 A 与 B";goal.Action=ProgressionGoalAction.OpenPresets;goal.CanAct=inCamp;break;
                case ProgressionGoalKind.Tier:
                    goal.Identity+="/"+Profile.progressionGoalTier;goal.Title="通关第 "+Profile.progressionGoalTier+" 阶";goal.Done=HighestAdventureTier>=Profile.progressionGoalTier;goal.Step="任一冒险 · 最高 "+HighestAdventureTier+" 阶";break;
                case ProgressionGoalKind.CombatTrial:
                    goal.Identity+="/"+(int)Profile.heroClass;goal.Title="实战试炼";goal.Step="右上目标查看四项指引 · "+CombatTrialProgress(Profile)+"/4 已完成";goal.Done=CombatTrialProgress(Profile)==4;break;
                case ProgressionGoalKind.ClassTutorial:
                    goal.Identity+="/"+(int)Profile.heroClass;goal.Title="职业练习";goal.Step=ClassTutorialText;goal.Done=Profile.classTutorialCompleted;break;
            }
            goal.Requirements=goal.ResourceRequirements(Profile,HighestAdventureTier);
            if(!goal.Done&&goal.Action!=ProgressionGoalAction.None){goal.Requirements+=(goal.Requirements.Length>0?" · ":"")+(inCamp?"营地已到达":"需返回营地");goal.Step+=(goal.Requirements.Length>0?" · "+goal.Requirements:"");}
            return goal;
        }
        public bool ExecuteProgressionGoal(string expectedActionIdentity,bool inCamp)
        {
            if(Profile.automaticGrowth)return ExecuteAutomaticGoal(expectedActionIdentity,inCamp);
            ProgressionGoalState goal=SelectedProgressionGoal(inCamp);
            if(goal.ActionIdentity!=expectedActionIdentity)return Fail("目标或下一步已变化，请检查当前操作。");
            if(goal.Done&&goal.Action==ProgressionGoalAction.None){LastError=string.Empty;return true;}
            if(!goal.CanAct)return Fail(goal.Step);
            var attachment=Profile.attachments.Find(a=>a.id==goal.ItemId);
            if(attachment!=null)
            {
                if(goal.Action==ProgressionGoalAction.UpgradeAttachment)return UpgradeAttachment(attachment.mechanic,inCamp);
                if(goal.Action==ProgressionGoalAction.UnlockVariant)return ToggleAttachmentVariant(attachment.mechanic,inCamp);
                if(goal.Action==ProgressionGoalAction.Ascend)return AscendAttachment(attachment.mechanic,inCamp);
            }
            switch(goal.Action)
            {
                case ProgressionGoalAction.ClaimCore:return ClaimFirstClearReward(Profile.progressionGoalMechanic);
                case ProgressionGoalAction.ExchangeCore:return ExchangeMechanic(Profile.progressionGoalMechanic);
                case ProgressionGoalAction.ClaimPending:return ClaimPendingLoot(goal.ItemId);
                case ProgressionGoalAction.ClaimRecovery:return ClaimRecoveryLoot(goal.ItemId);
                case ProgressionGoalAction.Equip:return Equip(goal.ItemId);
                case ProgressionGoalAction.UnlockVariant:return UnlockMechanicVariant(goal.ItemId,inCamp);
                case ProgressionGoalAction.Ascend:return AscendMechanic(goal.ItemId,inCamp);
                case ProgressionGoalAction.Reforge:return ReforgeMechanic(goal.ReforgeQuote,inCamp);
                default:return Fail("请打开对应营地入口继续。");
            }
        }
        public string ProgressionGoalStatus(int runMaterials=0,bool inCamp=false)
        {
            ProgressionGoalState goal=SelectedProgressionGoal(inCamp);
            return goal.Title+(goal.Done?" ✓ 已完成":"")+" · "+goal.Step+
                (runMaterials>0?" · 本局 +"+runMaterials+"碎片":"");
        }

        private bool AddAdventureEquipment(GameProfile candidate,string receipt,int mode,int tier)
        {
            int count=AdventureRewardRules.EquipmentCount(mode);
            if(count>MaximumSavedEquipment-candidate.inventory.Count-candidate.pendingLoot.Count-candidate.recoveryLoot.Count)
                return Fail("通关装备保全空间已满；奖励尚未结算，请整理行囊后重试。");
            // Receipt-derived rolls and IDs stay identical across failed writes/retries.
            byte[] seed=Guid.ParseExact(receipt,"N").ToByteArray();uint hash=2166136261;
            foreach(byte value in seed)hash=unchecked((hash^value)*16777619);
            for(int i=0;i<count;i++)
            {
                hash=unchecked((hash^(uint)(mode+2+i))*16777619);
                var item=new ItemData{id=receipt+"-clear-"+i,slot=AdventureRewardRules.EquipmentSlot(mode,i),rarity=AdventureRewardRules.EquipmentRarity(mode,tier,(int)(hash%100)),level=EquipmentGenerationLevel(Profile.level)};
                item.name=new[]{"旅者","苍蓝","星辉","烬王"}[(int)item.rarity]+ItemBaseName(item.slot,candidate.heroClass);
                SetRolledStats(item);EnsureUpgradeBasis(item);candidate.inventory.Add(item);
            }
            return true;
        }

        public bool TryCompleteDungeonRun(string rewardId, int tier, int gold, int experience, bool grantEquipment=false)
        {
            Guid receipt;
            if (rewardId == null || !Guid.TryParseExact(rewardId, "N", out receipt) || tier < 1 || tier > 100 ||
                gold < 0 || gold > 10000 || experience < 0 || experience > 10000) return Fail("遗迹通关奖励无效。");
            rewardId = receipt.ToString("N");
            if (Profile.lastDungeonRewardId == rewardId) { LastError = string.Empty; return true; }
            if (Profile.pendingFashionChest || Profile.pendingChestReveal) return Fail("请先开启并收起已有通关宝箱，再结算下一次遗迹。");
            GameProfile candidate = Snapshot();
            int oldLevel = candidate.level;
            candidate.clearedRuns = Math.Min(999999, candidate.clearedRuns + 1);
            candidate.bestFloor = Math.Max(candidate.bestFloor, tier);
            candidate.highestAdventureTier = Math.Max(candidate.highestAdventureTier,tier);
            candidate.chapterPriorAdventureTier = Math.Max(candidate.chapterPriorAdventureTier,tier);
            candidate.gold = (int)Math.Min(MaximumGold, (long)candidate.gold + gold);
            long xp = (long)candidate.xp + experience;
            while (candidate.level < MaximumLevel && xp >= GameBalance.XpToNext(candidate.level))
            { xp -= GameBalance.XpToNext(candidate.level); candidate.level++; candidate.skillPoints += GameBalance.SkillPointsGainedAtLevel(candidate.level); }
            candidate.xp = candidate.level >= MaximumLevel ? 0 : (int)xp;
            candidate.mechanicMaterials = Math.Min(999999, candidate.mechanicMaterials + TierRewardRules.ClearMaterials(tier));
            candidate.materialRewardedClears = candidate.clearedRuns;
            NewChestQualification(candidate,tier,rewardId);
            candidate.pendingFirstClearReward = !candidate.firstClearRewardClaimed;
            candidate.lastDungeonRewardId = rewardId;
            if(grantEquipment&&!AddAdventureEquipment(candidate,rewardId,-1,tier))return false;
            candidate.lastDungeonRewardDetails=CaptureRewardPresentation(rewardId,Profile,candidate);
            if (!CommitCandidate(candidate)) return false;
            for (int level = oldLevel + 1; level <= candidate.level; level++) RaiseLeveledUp(level);
            return true;
        }

        public bool TryGrantModeReward(string receipt,int gold,int experience,int materials,int completedTier=0,int adventureMode=-2)
        {
            Guid id;if(adventureMode < -2 || adventureMode > 3 || adventureMode>=-1&&completedTier==0 || completedTier<0||completedTier>100||receipt==null||!Guid.TryParseExact(receipt,"N",out id)||gold<0||gold>10000||experience<0||experience>10000||materials<0||materials>10)return Fail("挑战奖励无效。");
            receipt=id.ToString("N");
            if(Profile.lastModeRewardId==receipt){LastError=string.Empty;return true;}
            GameProfile candidate=Snapshot();int oldLevel=candidate.level;
            candidate.gold=(int)Math.Min(MaximumGold,(long)candidate.gold+gold);
            candidate.mechanicMaterials=Math.Min(999999,candidate.mechanicMaterials+materials);
            long xp=(long)candidate.xp+experience;
            while(candidate.level<MaximumLevel&&xp>=GameBalance.XpToNext(candidate.level)){xp-=GameBalance.XpToNext(candidate.level);candidate.level++;candidate.skillPoints += GameBalance.SkillPointsGainedAtLevel(candidate.level);}
            candidate.xp=candidate.level>=MaximumLevel?0:(int)xp;candidate.lastModeRewardId=receipt;
            if(completedTier>0)
            {
                candidate.highestAdventureTier=Math.Max(candidate.highestAdventureTier,completedTier);
                candidate.chapterPriorAdventureTier=Math.Max(candidate.chapterPriorAdventureTier,completedTier);
                candidate.pendingFirstClearReward=!candidate.firstClearRewardClaimed;
            }
            if(adventureMode>=-1&&!AddAdventureEquipment(candidate,receipt,adventureMode,completedTier))return false;
            candidate.lastModeRewardDetails=CaptureRewardPresentation(receipt,Profile,candidate);
            if(!CommitCandidate(candidate))return false;
            for(int level=oldLevel+1;level<=candidate.level;level++)RaiseLeveledUp(level);
            return true;
        }

        /// <summary>
        /// Apply one already-admitted enemy kill to the live character and save its
        /// complete earned reward once. A failed save keeps the reward live: retry
        /// Save(), never this grant. Enemy identity admission belongs to the session.
        /// </summary>
        public void GrantEnemyKillReward(int gold, int experience)
        {
            if (gold < 0 || experience < 0) { Fail("击败敌人奖励无效。"); return; }
            Profile.kills = (int)Math.Min(int.MaxValue, (long)Profile.kills + 1);
            Profile.gold = (int)Math.Max(0L, Math.Min(MaximumGold, (long)Profile.gold + gold));
            int oldLevel = Profile.level;
            if (experience > 0 && Profile.level < MaximumLevel)
            {
                long totalExperience = (long)Profile.xp + experience;
                while (Profile.level < MaximumLevel && totalExperience >= GameBalance.XpToNext(Profile.level))
                {
                    totalExperience -= GameBalance.XpToNext(Profile.level);
                    Profile.level++;
                    Profile.skillPoints += GameBalance.SkillPointsGainedAtLevel(Profile.level);
                }
                Profile.xp = Profile.level == MaximumLevel ? 0 : (int)totalExperience;
            }
            // Capture the earned range before callbacks can mutate the profile.
            int earnedLevel = Profile.level;
            Commit();
            // As with GrantExperience, failure keeps live progress and LastError;
            // Changed runs once before level notifications, all seeing final stats.
            for (int level = oldLevel + 1; level <= earnedLevel; level++)
                RaiseLeveledUp(level);
        }

        public void GrantExperience(int amount)
        {
            if (amount <= 0 || Profile.level >= MaximumLevel) return;
            long experience = (long)Profile.xp + amount;
            var gainedLevels = new List<int>();
            while (Profile.level < MaximumLevel && experience >= GameBalance.XpToNext(Profile.level))
            {
                experience -= GameBalance.XpToNext(Profile.level);
                Profile.level++;
                Profile.skillPoints += GameBalance.SkillPointsGainedAtLevel(Profile.level);
                gainedLevels.Add(Profile.level);
            }
            Profile.xp = Profile.level == MaximumLevel ? 0 : (int)experience;
            Commit();
            // Earned progress remains live after a storage failure; LastError stays visible.
            // Subscribers observe the final in-memory level, including multiple gains.
            foreach (int level in gainedLevels)
                RaiseLeveledUp(level);
        }

        public void AddGold(int amount)
        {
            Profile.gold = (int)Math.Max(0L, Math.Min(MaximumGold, (long)Profile.gold + amount));
            Commit();
        }

        public ItemData CreateLoot(int level, bool boss)
        {
            ItemData item = RollLoot(level, boss);
            CollectLoot(item);
            return item;
        }

        /// <summary>Generate an identified drop without putting it into the bag or saving.</summary>
        public ItemData RollLoot(int level, bool boss, int dungeonTier = 0)
        {
            level = EquipmentGenerationLevel(level);
            int roll = random.Next(100);
            Rarity rarity = TierRewardRules.DropRarity(boss, dungeonTier, roll);
            ItemSlot slot = (ItemSlot)random.Next(3);
            var item = new ItemData
            {
                id = Guid.NewGuid().ToString("N"),
                slot = slot,
                rarity = rarity,
                level = level,
                name = new[] { "旅者", "苍蓝", "星辉", "烬王" }[(int)rarity] + ItemBaseName(slot, Profile.heroClass)
            };
            EquipmentMechanic[] mechanics = BuildCatalog.MechanicsFor(Profile.heroClass);
            if (mechanics.Length > 0 && ((boss && random.Next(100) < TierRewardRules.BossMechanicChance(dungeonTier)) || (!boss && rarity >= Rarity.Epic && random.Next(100) < TierRewardRules.OrdinaryMechanicChance(dungeonTier))))
            {
                item.mechanic = mechanics[random.Next(mechanics.Length)];
                item.slot = BuildCatalog.MechanicSlot(item.mechanic);
                item.name = BuildCatalog.MechanicName(item.mechanic);
                item.locked = true;
            }
            SetRolledStats(item);
            EnsureUpgradeBasis(item);
            return item;
        }

        // New gear advances at level 10, 20, ...; stored items keep their earned stats.
        public static int EquipmentGenerationLevel(int level)
        { level=Clamp(level,1,MaximumLevel);return level<10?1:level/10*10; }

        private static void SetRolledStats(ItemData item)
        {
            float multiplier = new[] { 1f, 1.5f, 2.25f, 3.4f }[(int)item.rarity];
            int level = EquipmentGenerationLevel(item.level);
            item.attack = item.defense = item.health = 0;
            if (item.slot == ItemSlot.Weapon) item.attack = Round((5 + level * 2.5f) * multiplier);
            else if (item.slot == ItemSlot.Armor)
            {
                item.defense = Round((3 + level * 1.2f) * multiplier);
                item.health = Round((10 + level * 4) * multiplier);
            }
            else
            {
                item.attack = Round((2 + level) * multiplier);
                item.health = Round((6 + level * 3) * multiplier);
            }
        }

        // Added only after the attached profile write succeeds, before observers run.
        // Includes auto-sold drops which no longer have an inventory entry.
        internal bool HasCommittedWorldLoot(string itemId) { return collectedLootIds.Contains(itemId); }

        /// <summary>Overflow remains visible and owned. The retained-item safety boundary rejects
        /// acquisition without consuming the drop; the caller must retain it or block departure.</summary>
        public bool CollectLoot(ItemData item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.id) || item.id.Length > 80 || !Enum.IsDefined(typeof(ItemSlot), item.slot) ||
                !Enum.IsDefined(typeof(Rarity), item.rarity) || !Enum.IsDefined(typeof(EquipmentMechanic), item.mechanic))
                return Fail("掉落装备无效。");
            if (collectedLootIds.Contains(item.id) || FindItem(item.id) != null || Profile.pendingLoot.Exists(value => value != null && value.id == item.id) ||
                Profile.recoveryLoot.Exists(value => value != null && value.id == item.id))
                return Fail("这件装备已经拾取。");
            bool overflow = Profile.inventory.Count >= InventoryCapacity;
            if (!CanReceiveProtectedLoot)
                return Fail("装备保全空间已满；物品仍在地上，请到商人整理行囊。");
            var candidate=Snapshot();
            if(item.mechanic!=EquipmentMechanic.None&&Attachment(item.mechanic)==null)
                candidate.attachments.Add(new MechanicAttachment{id=Guid.NewGuid().ToString("N"),legacySourceId=item.id,mechanic=item.mechanic,level=item.level,rarity=item.rarity,variant=item.mechanicVariant,variantUnlocked=item.mechanicVariantUnlocked});
            candidate.inventory.Add(JsonUtility.FromJson<ItemData>(JsonUtility.ToJson(item,true)));
            if(item.mechanic!=EquipmentMechanic.None&&!candidate.discoveredMechanics.Contains(item.mechanic))candidate.discoveredMechanics.Add(item.mechanic);
            string failure;
            if(!TryWriteAttachedProfile(candidate,out failure))return Fail(failure);
            // Preserve the caller's successfully admitted item identity, while failures
            // leave both the world item and the live profile untouched.
            var owned=candidate.inventory.Find(value=>value.id==item.id);
            RestoreUpgradeState(item,owned);item.level=owned.level;item.name=owned.name;item.rarity=owned.rarity;
            item.locked=owned.locked;item.mechanic=owned.mechanic;item.mechanicVariant=owned.mechanicVariant;item.mechanicVariantUnlocked=owned.mechanicVariantUnlocked;
            candidate.inventory[candidate.inventory.IndexOf(owned)]=item;
            Profile=candidate;collectedLootIds.Add(item.id);
            if(IsStrictEquipmentUpgrade(item))PublishRewardMoment(RewardMomentKind.StrictUpgrade,item);
            LastError=string.Empty;RaiseChanged();
            if(overflow)LastError="背包超过常规容量，"+item.name+"已保全在行囊中，可直接查看、穿戴或整理。";
            return true;
        }

        public ItemData Equipped(ItemSlot slot)
        {
            string id = slot == ItemSlot.Weapon ? Profile.weaponId : slot == ItemSlot.Armor ? Profile.armorId : slot == ItemSlot.Relic ? Profile.relicId : null;
            ItemData item = FindItem(id);
            return item != null && item.slot == slot ? item : null;
        }

        public bool Equip(string id)
        {
            ItemData item = FindItem(id);
            if (item == null) return Fail("找不到这件装备。");
            if (item.level > Profile.level) return Fail("需要角色等级 " + item.level + " 才能装备。");
            ItemData previous = Equipped(item.slot);
            ItemData previousState = previous == null ? null : PreviewUpgrade(previous, previous.upgradeLevel);
            ItemData itemState = PreviewUpgrade(item, item.upgradeLevel);
            string previousId = previous == null ? null : previous.id;
            if (previous != null && previous != item) ApplyUpgradeRank(previous, 0);
            EnsureUpgradeBasis(item);
            ApplyUpgradeRank(item, SlotUpgradeRank(item.slot));
            SetEquipped(Profile, item);
            string failure;
            if (!TryWriteAttachedProfile(Profile, out failure))
            {
                RestoreUpgradeState(item, itemState);
                if (previous != null) RestoreUpgradeState(previous, previousState);
                if (item.slot == ItemSlot.Weapon) Profile.weaponId = previousId;
                else if (item.slot == ItemSlot.Armor) Profile.armorId = previousId;
                else Profile.relicId = previousId;
                return Fail(failure);
            }
            LastError = string.Empty;
            RaiseChanged();
            return true;
        }

        public bool Unequip(ItemSlot slot)
        {
            if (!Enum.IsDefined(typeof(ItemSlot), slot)) return Fail("无效的装备槽。");
            GameProfile candidate = Snapshot();
            string id = slot == ItemSlot.Weapon ? candidate.weaponId : slot == ItemSlot.Armor ? candidate.armorId : candidate.relicId;
            var item = candidate.inventory.Find(value => value != null && value.id == id);
            if (item == null) return Fail("这个装备槽是空的。");
            ApplyUpgradeRank(item, 0);
            // Empty string records an intentional empty slot; null remains legacy repair input.
            if (slot == ItemSlot.Weapon) candidate.weaponId = string.Empty;
            else if (slot == ItemSlot.Armor) candidate.armorId = string.Empty;
            else candidate.relicId = string.Empty;
            return CommitCandidate(candidate);
        }

        public int SlotUpgradeRank(ItemSlot slot)
        {
            int index = (int)slot;
            return index < 0 || index > 2 || Profile.slotUpgradeRanks == null || index >= Profile.slotUpgradeRanks.Length
                ? 0 : Clamp(Profile.slotUpgradeRanks[index], 0, MaximumUpgrade);
        }

        /// <summary>Prospective equipped stats at the permanent slot rank. Never
        /// mutates the item, profile or disk, including legacy baseline metadata.</summary>
        public ItemData PreviewEquippedItem(ItemData item)
        {
            return item == null || !Enum.IsDefined(typeof(ItemSlot), item.slot) ? null : PreviewUpgrade(item, SlotUpgradeRank(item.slot));
        }

        public bool Sell(string id,bool confirmPresetReferences=false)
        {
            if(!confirmPresetReferences&&PresetReferences(id).Length>0)return Fail("出售会使 "+PresetReferences(id)+" 缺失此装备；请确认后再出售。");
            ItemData item = FindItem(id);
            if (item == null) return Fail("找不到这件装备。");
            if (IsEquipped(Profile, item.id)) return Fail("请先替换身上的装备，再出售。");
            if (item.locked) return Fail("装备已锁定，请先手动解锁再出售。");
            GameProfile candidate = Snapshot();
            ItemData sale = candidate.inventory.Find(value => value != null && value.id == id);
            candidate.gold = (int)Math.Max(0L, Math.Min(MaximumGold, (long)candidate.gold + SellValue(sale)));
            candidate.inventory.Remove(sale);
            return CommitCandidate(candidate);
        }

        public bool Upgrade(string id)
        {
            ItemData item = FindItem(id);
            if (item == null) return Fail("找不到这件装备。");
            int rank = SlotUpgradeRank(item.slot);
            if (rank >= MaximumUpgrade) return Fail("该部位已达到强化上限 +10；换装会自动继承。");
            int cost = UpgradeCost(item);
            if (Profile.gold < cost) return Fail("金币不足，部位强化需要 " + cost + " 金币。");
            ItemData equipped = Equipped(item.slot);
            ItemData oldEquipped = equipped == null ? null : PreviewUpgrade(equipped, equipped.upgradeLevel);
            int oldGold = Profile.gold;
            Profile.gold -= cost;
            Profile.slotUpgradeRanks[(int)item.slot] = rank + 1;
            if (equipped != null) { EnsureUpgradeBasis(equipped); ApplyUpgradeRank(equipped, rank + 1); }
            string failure;
            if (!TryWriteAttachedProfile(Profile, out failure))
            {
                Profile.gold = oldGold;
                Profile.slotUpgradeRanks[(int)item.slot] = rank;
                if (equipped != null) RestoreUpgradeState(equipped, oldEquipped);
                return Fail(failure);
            }
            LastError = string.Empty;
            RaiseChanged();
            return true;
        }

        /// <summary>Compatibility entry point for old UI callers. Training now
        /// belongs to the slot and is applied automatically when equipment changes.</summary>
        public bool TransferUpgrade(string sourceId, string targetId)
        {
            return Fail("强化等级已绑定装备部位；更换装备会自动继承，无需单独转移。");
        }

        private static void RestoreUpgradeState(ItemData item, ItemData state)
        {
            item.attack = state.attack; item.defense = state.defense; item.health = state.health;
            item.upgradeLevel = state.upgradeLevel; item.upgradeBaseInitialized = state.upgradeBaseInitialized;
            item.balanceRevision = state.balanceRevision;
            item.baseAttack = state.baseAttack; item.baseDefense = state.baseDefense; item.baseHealth = state.baseHealth;
            item.upgradeAnchorLevel = state.upgradeAnchorLevel; item.upgradeAnchorAttack = state.upgradeAnchorAttack;
            item.upgradeAnchorDefense = state.upgradeAnchorDefense; item.upgradeAnchorHealth = state.upgradeAnchorHealth;
        }

        /// <summary>Returns an independent preview; never mutates items, gold, saves or events.</summary>
        public ItemData PreviewUpgrade(ItemData item, int rank)
        {
            if (item == null || rank < 0 || rank > MaximumUpgrade) return null;
            var preview = new ItemData
            {
                id = item.id, name = item.name, slot = item.slot, rarity = item.rarity, level = item.level,
                mechanic = item.mechanic, locked = item.locked,
                mechanicVariant = item.mechanicVariant, mechanicVariantUnlocked = item.mechanicVariantUnlocked, balanceRevision = item.balanceRevision,
                attack = item.attack, defense = item.defense, health = item.health, upgradeLevel = item.upgradeLevel,
                upgradeBaseInitialized = item.upgradeBaseInitialized,
                baseAttack = item.baseAttack, baseDefense = item.baseDefense, baseHealth = item.baseHealth,
                upgradeAnchorLevel = item.upgradeAnchorLevel, upgradeAnchorAttack = item.upgradeAnchorAttack,
                upgradeAnchorDefense = item.upgradeAnchorDefense, upgradeAnchorHealth = item.upgradeAnchorHealth
            };
            EnsureUpgradeBasis(preview);
            ApplyUpgradeRank(preview, rank);
            return preview;
        }

        private static void ApplyUpgradeRank(ItemData item, int rank)
        {
            item.upgradeLevel = Clamp(rank, 0, MaximumUpgrade);
            item.attack = UpgradeValue(item.baseAttack, item.upgradeAnchorAttack, item.upgradeAnchorLevel, item.upgradeLevel, 1, MaximumEquipmentStat);
            item.defense = UpgradeValue(item.baseDefense, item.upgradeAnchorDefense, item.upgradeAnchorLevel, item.upgradeLevel, 1, MaximumEquipmentStat);
            item.health = UpgradeValue(item.baseHealth, item.upgradeAnchorHealth, item.upgradeAnchorLevel, item.upgradeLevel, 2, MaximumEquipmentHealth);
        }

        private static int UpgradeValue(int basis, int anchor, int anchorRank, int rank, int minimumIncrease, int cap)
        {
            // The recorded unenhanced basis is the sole authority after migration.
            // Rank changes always reevaluate the linear curve, never compound caches.
            return CombatBalance.UpgradeValue(basis, rank, minimumIncrease, cap);
        }

        private static int GrowLegacyUpgradeStat(int value, int ranks, int minimumIncrease, int cap)
        {
            if (value <= 0) return 0;
            for (int i = 0; i < ranks; i++)
                value = Math.Min(cap, value + Math.Max(minimumIncrease, Round(value * .12f)));
            return value;
        }

        private static int RecoverUpgradeBase(int value, int rank, int minimumIncrease)
        {
            if (rank <= 0 || value <= 0) return value;
            // The uncapped integer growth function is strictly increasing for
            // positive values. Binary search finds the exact old base when it
            // exists; otherwise use the greatest conservative base below it.
            // Never invert the capped function, whose plateau would invent a base.
            int low = 0, high = value, best = 0;
            while (low <= high)
            {
                int middle = low + (high - low) / 2;
                int grown = GrowLegacyUpgradeStat(middle, rank, minimumIncrease, int.MaxValue);
                if (grown <= value) { best = middle; low = middle + 1; }
                else high = middle - 1;
            }
            return best;
        }

        private static bool ValidUpgradeBasis(int basis, int anchor, int anchorRank, int minimumIncrease, int cap)
        {
            return basis >= 0 && basis <= cap && anchor >= 0 && anchor <= cap &&
                basis == RecoverUpgradeBase(anchor, anchorRank, minimumIncrease);
        }

        private static void EnsureUpgradeBasis(ItemData item)
        {
            item.upgradeLevel = Clamp(item.upgradeLevel, 0, MaximumUpgrade);
            item.attack = Clamp(item.attack, 0, MaximumEquipmentStat);
            item.defense = Clamp(item.defense, 0, MaximumEquipmentStat);
            item.health = Clamp(item.health, 0, MaximumEquipmentHealth);
            bool old = item.balanceRevision < 1;
            bool validOld = item.upgradeBaseInitialized && item.upgradeAnchorLevel >= 0 && item.upgradeAnchorLevel <= MaximumUpgrade &&
                ValidUpgradeBasis(item.baseAttack, item.upgradeAnchorAttack, item.upgradeAnchorLevel, 1, MaximumEquipmentStat) &&
                ValidUpgradeBasis(item.baseDefense, item.upgradeAnchorDefense, item.upgradeAnchorLevel, 1, MaximumEquipmentStat) &&
                ValidUpgradeBasis(item.baseHealth, item.upgradeAnchorHealth, item.upgradeAnchorLevel, 2, MaximumEquipmentHealth);
            bool validNew = item.upgradeBaseInitialized && item.baseAttack >= 0 && item.baseAttack <= MaximumEquipmentStat &&
                item.baseDefense >= 0 && item.baseDefense <= MaximumEquipmentStat && item.baseHealth >= 0 && item.baseHealth <= MaximumEquipmentHealth &&
                item.attack == CombatBalance.UpgradeValue(item.baseAttack, item.upgradeLevel, 1, MaximumEquipmentStat) &&
                item.defense == CombatBalance.UpgradeValue(item.baseDefense, item.upgradeLevel, 1, MaximumEquipmentStat) &&
                item.health == CombatBalance.UpgradeValue(item.baseHealth, item.upgradeLevel, 2, MaximumEquipmentHealth);
            if (!old && validNew) return;
            if (old && validOld) { /* Preserve the saved unenhanced item, not compounded inflation. */ }
            else
            {
                item.baseAttack = old ? RecoverUpgradeBase(item.attack, item.upgradeLevel, 1) : RecoverLinearBase(item.attack, item.upgradeLevel, 1);
                item.baseDefense = old ? RecoverUpgradeBase(item.defense, item.upgradeLevel, 1) : RecoverLinearBase(item.defense, item.upgradeLevel, 1);
                item.baseHealth = old ? RecoverUpgradeBase(item.health, item.upgradeLevel, 2) : RecoverLinearBase(item.health, item.upgradeLevel, 2);
            }
            item.upgradeBaseInitialized = true; item.balanceRevision = 1;
            item.upgradeAnchorLevel = 0;
            item.upgradeAnchorAttack = item.baseAttack; item.upgradeAnchorDefense = item.baseDefense; item.upgradeAnchorHealth = item.baseHealth;
            ApplyUpgradeRank(item, item.upgradeLevel);
        }

        private static int RecoverLinearBase(int value, int rank, int minimumIncrease)
        {
            int low = 0, high = value, best = 0;
            while (low <= high)
            {
                int middle = low + (high - low) / 2;
                if (CombatBalance.UpgradeValue(middle, rank, minimumIncrease, int.MaxValue) <= value) { best = middle; low = middle + 1; }
                else high = middle - 1;
            }
            return best;
        }

        public int UpgradeCost(ItemData item)
        {
            if (item == null || !Enum.IsDefined(typeof(ItemSlot), item.slot)) return 0;
            int rank = SlotUpgradeRank(item.slot);
            if (rank >= MaximumUpgrade) return 0;
            // Every item of a slot trains the same permanent slot rank. Price never
            // depends on a donor's level, rarity, or cached item upgradeLevel.
            int slotPrice = item.slot == ItemSlot.Weapon ? 60 : item.slot == ItemSlot.Armor ? 50 : 45;
            return slotPrice * (rank + 1);
        }

        public int SellValue(ItemData item)
        {
            if (item == null) return 0;
            return (8 + Clamp(item.level, 1, MaximumLevel) * 4) * (Clamp((int)item.rarity, 0, 3) + 1);
        }

        public static float EquipmentScore(ItemData item)
        {
            return item == null ? 0 : item.attack * 5f + item.defense * 3f + item.health * .2f;
        }

        public bool LearnSkill(int slot)
        {
            string reason = SkillLockReason(slot);
            if (!string.IsNullOrEmpty(reason)) return Fail(reason);
            GameProfile candidate=Snapshot();candidate.skillRanks[slot]++;candidate.skillPoints--;
            return CommitCandidate(candidate);
        }

        public string SkillLockReason(int slot)
        {
            if (slot < 0 || slot >= GameBalance.SkillCount) return "无效的技能。";
            if (Profile.skillRanks[slot] >= 3) return "已达到最高等级 3。";
            int nextRank = Profile.skillRanks[slot] + 1;
            int required = GameBalance.SkillRankRequiredLevel(slot, nextRank);
            if (Profile.level < required) return "角色达到 " + required + " 级可学习技能第 " + nextRank + " 阶。";
            if (nextRank == 1 && !PrerequisitesMet(slot)) return "请先点亮前置技能。" + GameBalance.PrerequisiteDescription(Profile.heroClass, slot);
            if (Profile.skillPoints < 1) return "需要 1 点技能点，升级后获得。";
            return string.Empty;
        }

        public bool PrerequisitesMet(int skill)
        {
            if (skill < 0 || skill >= GameBalance.SkillCount) return false;
            foreach (int parent in GameBalance.SkillPrerequisites[skill])
                if (Profile.skillRanks[parent] < 1) return false;
            return true;
        }

        public bool AssignSkill(int hotbarSlot, int skillIndex)
        {
            if (hotbarSlot < 0 || hotbarSlot >= GameBalance.HotbarSize) return Fail("无效的快捷栏位置。");
            if (skillIndex < -1 || skillIndex >= GameBalance.SkillCount) return Fail("无效的技能。");
            if (skillIndex >= 0 && GameBalance.IsPassive(skillIndex)) return Fail("被动技能学习后自动生效，无需装备到快捷栏。");
            if (skillIndex >= 0 && Profile.skillRanks[skillIndex] < 1) return Fail("请先学习这个技能，再装备到快捷栏。");
            if (!HasValidHotbarData()) return Fail("快捷栏数据无效。");
            GameProfile candidate=Snapshot();
            int pageStart = candidate.hotbarPage * GameBalance.HotbarSize;
            int target = pageStart + hotbarSlot;
            if (skillIndex >= 0)
            {
                for (int slot = pageStart; slot < pageStart + GameBalance.HotbarSize; slot++)
                {
                    if (slot == target || candidate.equippedSkills[slot] != skillIndex) continue;
                    candidate.equippedSkills[slot] = candidate.equippedSkills[target];
                    break;
                }
            }
            candidate.equippedSkills[target] = skillIndex;
            return CommitCandidate(candidate);
        }

        public bool AssignConsumable(int hotbarSlot)
        {
            if (hotbarSlot < 0 || hotbarSlot >= GameBalance.HotbarSize) return Fail("无效的快捷栏位置。");
            if (!HasValidHotbarData()) return Fail("快捷栏数据无效。");
            GameProfile candidate=Snapshot();
            int pageStart = candidate.hotbarPage * GameBalance.HotbarSize;
            int target = pageStart + hotbarSlot;
            if (candidate.equippedSkills[target] == GameBalance.HotbarPotion) return Fail("生命药水已经位于这个快捷栏位置。");
            for (int slot = pageStart; slot < pageStart + GameBalance.HotbarSize; slot++)
            {
                if (candidate.equippedSkills[slot] != GameBalance.HotbarPotion) continue;
                int displaced = candidate.equippedSkills[target];
                candidate.equippedSkills[slot] = IsUsableHotbarEntry(displaced) ? displaced : -1;
                break;
            }
            candidate.equippedSkills[target] = GameBalance.HotbarPotion;
            return CommitCandidate(candidate);
        }

        private bool HasValidHotbarData()
        {
            return Profile.hotbarPage >= 0 && Profile.hotbarPage < GameBalance.HotbarPages && Profile.equippedSkills != null &&
                Profile.equippedSkills.Length >= GameBalance.HotbarPages * GameBalance.HotbarSize && Profile.skillRanks != null && Profile.skillRanks.Length >= GameBalance.SkillCount;
        }

        private bool IsUsableHotbarEntry(int entry)
        {
            return entry == GameBalance.HotbarPotion ||
                (entry >= 0 && entry < GameBalance.SkillCount && !GameBalance.IsPassive(entry) && Profile.skillRanks[entry] > 0);
        }

        /// <summary>Move or swap learned active skills and consumables within the selected hotbar page.</summary>
        public bool MoveHotbarSkill(int sourceSlot, int targetSlot)
        {
            if (sourceSlot < 0 || sourceSlot >= GameBalance.HotbarSize || targetSlot < 0 || targetSlot >= GameBalance.HotbarSize)
                return Fail("无效的快捷栏位置。");
            if (sourceSlot == targetSlot) return Fail("已经位于这个快捷栏位置。");
            if (!HasValidHotbarData()) return Fail("快捷栏数据无效。");
            GameProfile candidate=Snapshot();
            int pageStart = candidate.hotbarPage * GameBalance.HotbarSize;
            int source = pageStart + sourceSlot;
            int target = pageStart + targetSlot;
            int skill = candidate.equippedSkills[source];
            if (!IsUsableHotbarEntry(skill)) return Fail("只能拖动已学习的主动技能或可使用物品。");
            int displaced = candidate.equippedSkills[target];
            if (!IsUsableHotbarEntry(displaced)) displaced = -1;
            if (skill == displaced) return Fail("已经位于目标位置。");
            candidate.equippedSkills[target] = skill;
            candidate.equippedSkills[source] = displaced;
            return CommitCandidate(candidate);
        }

        public bool SetHotbarPage(int page)
        {
            if (page < 0 || page >= GameBalance.HotbarPages) return Fail("无效的快捷栏页面。");
            GameProfile candidate=Snapshot();candidate.hotbarPage=page;
            return CommitCandidate(candidate);
        }

        public bool ResetHotbarKeys()
        {
            GameProfile candidate = Snapshot();
            candidate.hotbarKeys = (int[])GameBalance.DefaultHotbarKeys.Clone();
            return CommitCandidate(candidate);
        }

        public bool SetHotbarKey(int slot, int keyCode)
        {
            if (slot < 0 || slot >= GameBalance.HotbarSize) return Fail("无效的快捷栏位置。");
            if (!GameBalance.IsBindableKey(keyCode)) return Fail("请选择字母、数字或 F1–F12；移动、药水、G 对话和面板按键不能绑定。");
            GameProfile candidate=Snapshot();
            int otherSlot = Array.IndexOf(candidate.hotbarKeys, keyCode);
            if (otherSlot >= 0 && otherSlot != slot) candidate.hotbarKeys[otherSlot] = candidate.hotbarKeys[slot];
            candidate.hotbarKeys[slot] = keyCode;
            return CommitCandidate(candidate);
        }

        public bool UsePotion()
        {
            if (Profile.potions <= 0) return Fail("治疗药水已用尽，返回营地购买。");
            GameProfile candidate=Snapshot();candidate.potions--;
            return CommitCandidate(candidate);
        }

        public bool BuyPotion()
        {
            if (Profile.potions >= 99) return Fail("药水已达到携带上限 99。");
            if (Profile.gold < PotionPrice) return Fail("购买药水需要 " + PotionPrice + " 金币。");
            GameProfile candidate = Snapshot();
            candidate.gold = Math.Min(MaximumGold, candidate.gold - PotionPrice);
            candidate.potions = Math.Max(0, candidate.potions) + 1;
            return CommitCandidate(candidate);
        }

        private void Commit()
        {
            Save();
            RaiseChanged();
        }

        public RewardMoment LastRewardMoment {get;private set;}
        private long rewardMomentSequence;
        public bool IsStrictEquipmentUpgrade(ItemData item)
        {
            if(item==null||!Enum.IsDefined(typeof(ItemSlot),item.slot)||item.level<1||item.level>Profile.level)return false;
            if(item.mechanic!=EquipmentMechanic.None&&(!Enum.IsDefined(typeof(EquipmentMechanic),item.mechanic)||BuildCatalog.MechanicClass(item.mechanic)!=Profile.heroClass||BuildCatalog.MechanicSlot(item.mechanic)!=item.slot))return false;
            var current=Equipped(item.slot);if(current==null)return false;
            if(current.mechanic!=EquipmentMechanic.None&&(current.mechanic!=item.mechanic||current.mechanicVariantUnlocked&&(!item.mechanicVariantUnlocked||current.mechanicVariant!=item.mechanicVariant)))return false;
            var before=PreviewEquippedItem(current);var after=PreviewEquippedItem(item);
            return after.attack>=before.attack&&after.defense>=before.defense&&after.health>=before.health&&(after.attack>before.attack||after.defense>before.defense||after.health>before.health);
        }
        private void PublishRewardMoment(RewardMomentKind kind,ItemData item=null,FashionData fashion=null,int gold=0,int materials=0,int threads=0,MechanicAttachment attachment=null)
        {
            if(IsPracticeOnly)return;
            try
            {
                LastRewardMoment=new RewardMoment{Sequence=++rewardMomentSequence,SlotId=CurrentSlotId,HeroClass=Profile.heroClass,Kind=kind,
                    Item=item==null?null:PreviewEquippedItem(item),Fashion=fashion==null?null:JsonUtility.FromJson<FashionData>(JsonUtility.ToJson(fashion,true)),Attachment=attachment==null?null:JsonUtility.FromJson<MechanicAttachment>(JsonUtility.ToJson(attachment,true)),GoldDelta=gold,MaterialsDelta=materials,ThreadsDelta=threads};
            }
            catch(Exception error){Debug.LogWarning("Emberfall: reward presentation snapshot unavailable: "+error);}
        }

        private void RaiseChanged()
        {
            var observers=Changed;if(observers==null)return;
            foreach(Action observer in observers.GetInvocationList())
                try{observer();}catch(Exception error){Debug.LogWarning("Emberfall: change observer failed: "+error);}
        }
        private void RaiseLeveledUp(int level)
        {
            var observers=LeveledUp;if(observers==null)return;
            foreach(Action<int> observer in observers.GetInvocationList())
                try{observer(level);}catch(Exception error){Debug.LogWarning("Emberfall: level observer failed: "+error);}
        }
        private static long RewardExperienceTotal(GameProfile profile)
        {long total=profile.xp;for(int level=1;level<profile.level;level++)total+=GameBalance.XpToNext(level);return total;}
        private static RewardPresentationReceipt CaptureRewardPresentation(string id,GameProfile before,GameProfile after)
        {return new RewardPresentationReceipt{Id=id,Gold=Math.Max(0,after.gold-before.gold),Materials=Math.Max(0,after.mechanicMaterials-before.mechanicMaterials),Experience=(int)Math.Max(0,RewardExperienceTotal(after)-RewardExperienceTotal(before))};}
        public RewardPresentationReceipt GetRewardPresentation(string id)
        {
            if(string.IsNullOrEmpty(id))return null;
            var detail=Profile.lastModeRewardId==id?Profile.lastModeRewardDetails:Profile.lastDungeonRewardId==id?Profile.lastDungeonRewardDetails:Profile.lastChapterRewardId==id?Profile.lastChapterRewardDetails:null;
            return detail!=null&&detail.Id==id&&detail.Gold>=0&&detail.Experience>=0&&detail.Materials>=0?detail:null;
        }
        private bool Fail(string message) { LastError = message; return false; }

        private ItemData FindItem(string id)
        {
            if (string.IsNullOrEmpty(id) || Profile.inventory == null) return null;
            // Do not cache mutable inventory references: load, equip, sale and
            // candidate commits can replace the profile or its contents.
            for (int i = 0; i < Profile.inventory.Count; i++)
            {
                ItemData item = Profile.inventory[i];
                if (item != null && item.id == id) return item;
            }
            return null;
        }

        private static GameProfile CreateProfile(HeroClass heroClass)
        {
            var profile = new GameProfile { heroClass = heroClass,chestRulesRevision=2, chapterDifficultyRewardRevision = 1, variantKnowledgeRevision = 1 };
            SkillStockRules.Normalize(profile);profile.skillStockVersion=1;
            profile.skillRanks[0] = 1;
            for (int slot = 0; slot < 3; slot++) AddStarterItem(profile, (ItemSlot)slot);
            return profile;
        }

        private static void AddStarterItem(GameProfile profile, ItemSlot slot)
        {
            var item = new ItemData
            {
                id = Guid.NewGuid().ToString("N"), name = "初行" + ItemBaseName(slot, profile.heroClass),
                slot = slot, rarity = Rarity.Common, level = 1,
                attack = slot == ItemSlot.Weapon ? 6 : slot == ItemSlot.Relic ? 2 : 0,
                defense = slot == ItemSlot.Armor ? 4 : 0,
                health = slot == ItemSlot.Armor ? 20 : slot == ItemSlot.Relic ? 10 : 0
            };
            EnsureUpgradeBasis(item);
            profile.inventory.Add(item);
            SetEquipped(profile, item);
        }

        private static string ItemBaseName(ItemSlot slot, HeroClass heroClass)
        {
            if (slot == ItemSlot.Armor) return "战衣";
            if (slot == ItemSlot.Relic) return "护符";
            return heroClass == HeroClass.Arcanist ? "法杖" : heroClass == HeroClass.Ranger ? "长弓" : heroClass == HeroClass.Summoner ? "法器" : "长剑";
        }

        private static void SetEquipped(GameProfile profile, ItemData item)
        {
            if (item.slot == ItemSlot.Weapon) profile.weaponId = item.id;
            else if (item.slot == ItemSlot.Armor) profile.armorId = item.id;
            else profile.relicId = item.id;
        }

        private static bool IsEquipped(GameProfile profile, string id)
        {
            return id == profile.weaponId || id == profile.armorId || id == profile.relicId;
        }

        private const string FrozenRewardReadFailure="冻结奖励记录不可安全恢复；原主档与备份保留。";
        // Unity inline serialization materializes null classes as empty objects.
        // Receipt absence is meaningful: an empty draw must never become a frozen reward.
        private static string PreserveOptionalReceiptNulls(string json, GameProfile profile)
        {
            if (profile.pendingChestDraw == null) json = WriteNullReceipt(json, "pendingChestDraw");
            if (profile.lastChestReward == null) json = WriteNullReceipt(json, "lastChestReward");
            if (profile.lastThreadMaterialReceipt == null) json = WriteNullReceipt(json, "lastThreadMaterialReceipt");
            return json;
        }
        private static string WriteNullReceipt(string json, string field)
        {
            // These receipt types contain scalar fields only, with no nested objects.
            return System.Text.RegularExpressions.Regex.Replace(json,
                "(?<!\\\\)\"" + field + "\"\\s*:\\s*\\{(?:[^\"{}]|\"(?:\\\\.|[^\"\\\\])*\")*\\}",
                "\"" + field + "\": null");
        }
        private static void RestoreOptionalReceiptNulls(GameProfile profile, string document)
        {
            if (profile == null) return;
            if (!HasReceiptValue(document, "pendingChestDraw")) profile.pendingChestDraw = null;
            if (!HasReceiptValue(document, "lastChestReward")) profile.lastChestReward = null;
            if (!HasReceiptValue(document, "lastThreadMaterialReceipt")) profile.lastThreadMaterialReceipt = null;
        }
        private static bool HasReceiptValue(string document, string field)
        {
            var match = System.Text.RegularExpressions.Regex.Match(document,
                "(?<!\\\\)\"" + field + "\"\\s*:\\s*(?<token>null|[^ \\t\\r\\n])");
            return match.Success && match.Groups["token"].Value != "null";
        }
        private static GameProfile CloneProfile(GameProfile source)
        {
            string json = PreserveOptionalReceiptNulls(JsonUtility.ToJson(source, true), source);
            var copy = JsonUtility.FromJson<GameProfile>(json);
            RestoreOptionalReceiptNulls(copy, json);
            return copy;
        }
        private static bool IsFrozenRewardReadError(string error)
        {return error!=null&&error.StartsWith(FrozenRewardReadFailure,StringComparison.Ordinal);}
        private static bool HasFrozenRewardDocument(string document)
        {
            var match=System.Text.RegularExpressions.Regex.Match(document??string.Empty,@"(?<!\\)""pendingChestDraw""\s*:\s*(?<token>null|[^ \t\r\n])");
            return match.Success&&match.Groups["token"].Value!="null";
        }
        // Unity serializes a null inline class as this exact empty placeholder.
        // A real frozen roll has an ID and revision; never discard partially populated records.
        private static bool NormalizeEmptyChestDraw(GameProfile profile)
        {
            if (profile == null) return false;
            ChestReward draw = profile.pendingChestDraw;
            if (draw != null && draw.rulesRevision == 0 && !draw.hasCurrencyDeltas &&
                draw.goldDelta == 0 && draw.threadsDelta == 0 && draw.materialsDelta == 0 &&
                (int)draw.rewardKind == 0 && (int)draw.materialKind == 0 && draw.materials == 0 &&
                draw.baseGold == 0 && draw.duplicateGold == 0 && draw.baseThreads == 0 && draw.duplicateThreads == 0 &&
                !draw.legacyGoldProtection && string.IsNullOrEmpty(draw.id) && draw.choice == 0 && draw.gold == 0 &&
                draw.rarityIndex == -1 && draw.slotIndex == -1 && string.IsNullOrEmpty(draw.name) &&
                !draw.duplicate && string.IsNullOrEmpty(draw.summary))
                { profile.pendingChestDraw = null; return true; }
            return false;
        }

        private static bool TryReadProfile(string path, out GameProfile profile, out string error)
        {
            profile = null;
            error = string.Empty;
            bool frozenRecord=false;
            try
            {
                string document;
                if (!TryReadSaveDocument(path, out document, out error, true)) return false;
                frozenRecord=HasFrozenRewardDocument(document);
                // Unity can deserialize scalar/array values into an empty inline class.
                // Only a JSON object can be a compatible historical null placeholder.
                if (frozenRecord && !System.Text.RegularExpressions.Regex.IsMatch(document,
                    @"(?<!\\)""pendingChestDraw""\s*:\s*\{"))
                    throw new ArgumentException("冻结奖励内容不是有效对象；原文件保留。");
                SaveFile data = JsonUtility.FromJson<SaveFile>(document);
                if (data != null) RestoreOptionalReceiptNulls(data.profile, document);
                bool emptyChestDraw = data != null && NormalizeEmptyChestDraw(data.profile);
                if (frozenRecord && !emptyChestDraw && (data == null || data.profile == null || data.profile.pendingChestDraw == null))
                    throw new ArgumentException("冻结奖励内容不是有效对象；原文件保留。");
                frozenRecord=frozenRecord||(data!=null&&data.profile!=null&&data.profile.pendingChestDraw!=null);
                if(data!=null&&data.format==SaveFormat&&(data.version>6||data.profile!=null&&(data.profile.version>1||data.profile.rewardInventoryRevision>1||data.profile.attachmentRevision>1||data.profile.growthRevision>1||data.profile.classStateRevision>1||data.profile.chestRulesRevision>2||data.profile.pendingChestRulesRevision>2||data.profile.pendingChestDraw!=null&&data.profile.pendingChestDraw.rulesRevision>2)))
                {error="future format";return false;}
                if (data == null || data.format != SaveFormat || (data.version != 1 && data.version != 2 && data.version != 3 && data.version != 4 && data.version != 5 && data.version != 6) || data.profile == null || data.profile.version != 1)
                { error = frozenRecord?FrozenRewardReadFailure+" unsupported format":"unsupported format"; return false; }
                bool balanceChanged = HasLegacyEnhancement(data.profile.inventory) || HasLegacyEnhancement(data.profile.pendingLoot) || HasLegacyEnhancement(data.profile.recoveryLoot);
                bool masteryMigrated = data.profile.masteryRevision < 1 && data.profile.masteryRanks != null && Array.Exists(data.profile.masteryRanks, rank => rank > 0);
                int refundedRanks = ValidateProfile(data.profile);
                if (balanceChanged || masteryMigrated) error = "成长规则已更新：部位强化等级与装备基础保留，强化属性按新曲线重算；合法精通投入保留，超出等级或点数预算的部分退回可用点数。";
                if (refundedRanks > 0) error = "部分技能阶级尚未达到新的解锁等级，已调整并返还技能点；角色与装备进度均已保留。";
                profile = data.profile;
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException)
            {
                error = frozenRecord?FrozenRewardReadFailure+" "+exception.Message:exception.Message;
                return false;
            }
        }

        private static bool TryReadSaveDocument(string path, out string document, out string error, bool acceptImportedEncoding = false)
        {
            document = null;
            error = string.Empty;
            try
            {
                // Read through one handle with a fixed allocation bounded by the
                // opened file's length; metadata from an earlier lookup is not proof.
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    long length = stream.Length;
                    if (length <= 0 || length > MaximumSaveBytes) { error = "invalid size"; return false; }
                    var bytes = new byte[(int)length];
                    int offset = 0;
                    while (offset < bytes.Length)
                    {
                        int read = stream.Read(bytes, offset, bytes.Length - offset);
                        if (read == 0) { error = "save changed while reading"; return false; }
                        offset += read;
                    }
                    if (stream.ReadByte() != -1) { error = "save changed while reading"; return false; }
                    if (acceptImportedEncoding)
                    {
                        // Preserve ReadAllText's prior BOM import compatibility,
                        // but only after the complete byte input has been capped.
                        using (var reader = new StreamReader(new MemoryStream(bytes, false), new UTF8Encoding(false, true), true))
                            document = reader.ReadToEnd();
                    }
                    else document = new UTF8Encoding(false, true).GetString(bytes);
                    return true;
                }
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException)
            {
                error = exception.Message;
                return false;
            }
        }

        private static bool HasLegacyEnhancement(List<ItemData> items)
        { return items != null && items.Exists(item => item != null && item.balanceRevision < 1 && item.upgradeLevel > 0); }

        private static int ValidateProfile(GameProfile profile)
        {
            SkillStockRules.Normalize(profile);
            NormalizeEmptyChestDraw(profile);
            int refundedRanks = 0;
            EnsureBuildPresetSlots(profile);
            if (!Enum.IsDefined(typeof(HeroClass), profile.heroClass)) profile.heroClass = HeroClass.Vanguard;
            profile.version = 1;
            ChapterProgression.Normalize(profile);
            profile.level = Clamp(profile.level, 1, MaximumLevel);
            profile.xp = profile.level >= MaximumLevel ? 0 : Clamp(profile.xp, 0, GameBalance.XpToNext(profile.level) - 1);
            profile.gold = Clamp(profile.gold, 0, MaximumGold);
            profile.potions = Clamp(profile.potions, 0, 99);
            profile.kills = Clamp(profile.kills, 0, int.MaxValue);
            profile.clearedRuns = Clamp(profile.clearedRuns, 0, 999999);
            profile.bestFloor = Clamp(profile.bestFloor, 0, 999999);
            profile.highestAdventureTier=Clamp(Math.Max(profile.highestAdventureTier,profile.bestFloor),0,100);
            if(!Enum.IsDefined(typeof(ProgressionGoalKind),profile.progressionGoal))profile.progressionGoal=ProgressionGoalKind.None;
            if(profile.progressionGoalItemId!=null && profile.progressionGoalItemId.Length>80)profile.progressionGoalItemId=null;
            profile.progressionGoalTier=Clamp(profile.progressionGoalTier,1,100);
            profile.unlockedHubMask=HubTravelRules.UnlockedMask(profile.unlockedHubMask,profile.level,profile.clearedRuns);
            profile.currentHub=HubTravelRules.SafeCurrent(profile.currentHub,profile.unlockedHubMask);
            Guid modeReceipt;profile.lastModeRewardId=Guid.TryParseExact(profile.lastModeRewardId,"N",out modeReceipt)?modeReceipt.ToString("N"):null;
            Guid dungeonReceipt;profile.lastDungeonRewardId=Guid.TryParseExact(profile.lastDungeonRewardId,"N",out dungeonReceipt)?dungeonReceipt.ToString("N"):null;
            profile.sideEventRewardReceipts=NormalizeSideEventReceipts(profile.sideEventRewardReceipts);
            profile.tutorialMask = Math.Max(0, profile.tutorialMask) & 15;
            if (profile.heroClass != HeroClass.Arcanist || !Enum.IsDefined(typeof(ElementalistSpecialization), profile.specialization))
                profile.specialization = ElementalistSpecialization.None;
            profile.mechanicMaterials = Clamp(profile.mechanicMaterials, 0, 999999);
            profile.materialRewardedClears = Clamp(profile.materialRewardedClears, 0, profile.clearedRuns);
            profile.pendingFirstClearReward = (profile.clearedRuns > 0 || (profile.chapterCompletedMask&(1<<(int)ChapterNode.StarPlatform))!=0 || profile.chapterPriorAdventureTier>0 || profile.highestAdventureTier>profile.chapterHighestAdventureTier) && !profile.firstClearRewardClaimed;
            profile.pendingChestTier = TierRewardRules.ClampTier(profile.pendingChestTier);
            if(profile.chestRulesRevision<0||profile.chestRulesRevision>2||profile.pendingChestRulesRevision<0||profile.pendingChestRulesRevision>2)throw new ArgumentException("宝箱规则版本不受支持，原文件保留。");
            if(profile.pendingChestDraw!=null)
            {
                var draw=profile.pendingChestDraw;
                if(!profile.pendingFashionChest||string.IsNullOrEmpty(draw.id)||draw.id.Length>80||draw.rulesRevision<1||draw.rulesRevision>2||draw.gold<60||draw.gold>300||draw.rarityIndex< -1||draw.rarityIndex>3||
                    draw.rulesRevision==1&&(draw.choice<0||draw.choice>2)||draw.rulesRevision==2&&(draw.rewardKind!=ChestRewardKind.SingleChest||draw.choice!=-1||draw.materialKind!=RewardMaterialKind.StarAshFragment||draw.materials!=1||draw.rarityIndex>=0&&(draw.slotIndex<0||draw.slotIndex>1)))
                    throw new ArgumentException("冻结宝箱记录无效，未重新抽签；请保留原文件。");
            }
            if(profile.threadMaterialSequence<0||profile.threadMaterialSequence>0&&(profile.lastThreadMaterialReceipt==null||profile.lastThreadMaterialReceipt.sequence!=profile.threadMaterialSequence||string.IsNullOrEmpty(profile.lastThreadMaterialReceipt.id)||profile.lastThreadMaterialReceipt.materialKind!=RewardMaterialKind.StarAshFragment||profile.lastThreadMaterialReceipt.threadsDelta!=-6||profile.lastThreadMaterialReceipt.materialsDelta!=1))
                throw new ArgumentException("星纹兑换流水无效，原文件保留。");
            ChestReward receipt = profile.lastChestReward;
            if (receipt == null || string.IsNullOrWhiteSpace(receipt.id) || receipt.id.Length > 80 ||
                receipt.gold < 60 || receipt.gold > (receipt.rulesRevision>=2?1100:1000) || receipt.rarityIndex < -1 || receipt.rarityIndex > 3 ||
                (receipt.rarityIndex >= 0 && (receipt.slotIndex < 0 || receipt.slotIndex > 1)))
            {
                profile.lastChestReward = null;
                profile.pendingChestReveal = false;
            }
            else
            {
                if(receipt.rulesRevision<2)receipt.choice = Clamp(receipt.choice, 0, 2);
                else if(receipt.rulesRevision!=2||receipt.rewardKind!=ChestRewardKind.SingleChest||receipt.choice!=-1||receipt.materialKind!=RewardMaterialKind.StarAshFragment||receipt.materialsDelta<0||receipt.materialsDelta>1)throw new ArgumentException("宝箱回执类型或增量无效。");
                if (receipt.rarityIndex < 0) { receipt.slotIndex = -1; receipt.duplicate = false; receipt.name = receipt.rulesRevision>=2?"通关资源":"金币"; }
                else receipt.name = FashionName((FashionSlot)receipt.slotIndex, (Rarity)receipt.rarityIndex,profile.heroClass);
                if (string.IsNullOrWhiteSpace(receipt.summary)) receipt.summary = receipt.name + " · " + receipt.gold + " 金币";
                if (receipt.summary.Length > 240) receipt.summary = receipt.summary.Substring(0, 240);
            }
            // Version 1 saves originally held three skills. Preserve their ranks while adding
            // seven unlearned entries; the character, gear, experience and currencies stay intact.
            int[] ranks = new int[GameBalance.SkillCount];
            int remaining = GameBalance.SkillPointBudget(profile.level);
            for (int slot = 0; slot < GameBalance.SkillCount; slot++)
            {
                int rank = profile.skillRanks != null && slot < profile.skillRanks.Length ? Clamp(profile.skillRanks[slot], 0, 3) : 0;
                if (profile.level == 1 && slot == 0) rank = 1;
                int previousRank = rank;
                while (rank > 0 && profile.level < GameBalance.SkillRankRequiredLevel(slot, rank)) rank--;
                refundedRanks += previousRank - rank;
                ranks[slot] = Math.Min(rank, remaining);
                remaining -= ranks[slot];
            }
            profile.skillRanks = ranks;
            int[] mastery = new int[4];
            // Preserve legal legacy investments, including old three-track saves.
            // Only invalid ranks, level caps and the shared lifetime budget constrain migration.
            if (profile.level >= 30)
                for (int track = 0; track < mastery.Length; track++)
                {
                    int oldRank = profile.masteryRanks != null && track < profile.masteryRanks.Length ? profile.masteryRanks[track] : 0;
                    mastery[track] = Math.Min(Clamp(oldRank, 0, MasteryCap(profile.level)), remaining);
                    remaining -= mastery[track];
                }
            profile.masteryRanks = mastery;
            if (profile.masteryCore < 0 || profile.masteryCore >= mastery.Length || mastery[profile.masteryCore] < MasteryCoreRules.InitialInvestment)
                profile.masteryCore = -1;
            profile.masteryRevision = 1;
            profile.autoSellCommon=profile.autoSellRare=false;
            profile.fashionThreads = Clamp(profile.fashionThreads, 0, 999999);
            if (!Enum.IsDefined(typeof(SummonerRoute), profile.summonerRoute)) profile.summonerRoute = SummonerRoute.Bonded;
            // No saved free-point counter is trusted. Levels pay for both skills and
            // bounded mastery, so loading/repeated saves can neither mint nor lose points.
            profile.skillPoints = remaining;
            profile.equippedSkills = RepairLoadout(profile.equippedSkills);
            profile.hotbarKeys = RepairHotbarKeys(profile.hotbarKeys);
            if (profile.hotbarPage < 0 || profile.hotbarPage >= GameBalance.HotbarPages) profile.hotbarPage = 0;
            if (profile.inventory == null) profile.inventory = new List<ItemData>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var items = new List<ItemData>();
            foreach (ItemData item in profile.inventory)
            {
                if (item == null || !Enum.IsDefined(typeof(ItemSlot), item.slot) || !Enum.IsDefined(typeof(Rarity), item.rarity)) continue;
                if (string.IsNullOrWhiteSpace(item.id) || item.id.Length > 80)
                {
                    item.id = Guid.NewGuid().ToString("N");
                }
                if (!ids.Add(item.id)) continue;
                RepairItem(item, profile.heroClass);
                items.Add(item);
            }
            profile.inventory = items;
            var pending = new List<ItemData>();
            if (profile.pendingLoot != null)
            {
                foreach (ItemData item in profile.pendingLoot)
                {
                    if (item == null || !Enum.IsDefined(typeof(ItemSlot), item.slot) || !Enum.IsDefined(typeof(Rarity), item.rarity)) continue;
                    if (string.IsNullOrWhiteSpace(item.id) || item.id.Length > 80) item.id = Guid.NewGuid().ToString("N");
                    if (!ids.Add(item.id)) continue; // A duplicated receipt never creates another copy.
                    RepairItem(item, profile.heroClass);
                    pending.Add(item);
                }
            }
            if (pending.Count > MaximumRetainedEquipment) throw new ArgumentException("待领取栏超过安全容量；保留原存档，请从备份恢复。");
            profile.pendingLoot = pending;
            var recovery = new List<ItemData>();
            if (profile.recoveryLoot != null)
            {
                foreach (ItemData item in profile.recoveryLoot)
                {
                    if (item == null || !Enum.IsDefined(typeof(ItemSlot), item.slot) || !Enum.IsDefined(typeof(Rarity), item.rarity)) continue;
                    if (string.IsNullOrWhiteSpace(item.id) || item.id.Length > 80) item.id = Guid.NewGuid().ToString("N");
                    if (!ids.Add(item.id)) continue;
                    RepairItem(item, profile.heroClass);
                    recovery.Add(item);
                }
            }
            if (recovery.Count > MaximumRetainedEquipment) throw new ArgumentException("临时保管栏超过安全容量；保留原存档，请从备份恢复。");
            profile.recoveryLoot = recovery;
            NormalizeVariantKnowledge(profile);
            var discovered = new List<EquipmentMechanic>();
            if (profile.discoveredMechanics != null)
                foreach (EquipmentMechanic mechanic in profile.discoveredMechanics)
                    if (mechanic != EquipmentMechanic.None && Enum.IsDefined(typeof(EquipmentMechanic), mechanic) && !discovered.Contains(mechanic)) discovered.Add(mechanic);
            foreach (ItemData item in items)
                if (item.mechanic != EquipmentMechanic.None && !discovered.Contains(item.mechanic)) discovered.Add(item.mechanic);
            foreach (ItemData item in pending)
                if (item.mechanic != EquipmentMechanic.None && !discovered.Contains(item.mechanic)) discovered.Add(item.mechanic);
            foreach (ItemData item in recovery)
                if (item.mechanic != EquipmentMechanic.None && !discovered.Contains(item.mechanic)) discovered.Add(item.mechanic);
            profile.discoveredMechanics = discovered;
            profile.progressionGoalLevel=Clamp(profile.progressionGoalLevel,1,MaximumLevel);
            if(profile.progressionGoal!=ProgressionGoalKind.Reforge)profile.progressionGoalLevel=0;
            if(profile.progressionGoalMinimumRarity!=Rarity.Common&&profile.progressionGoalMinimumRarity!=Rarity.Epic)profile.progressionGoalMinimumRarity=Rarity.Common;
            if(!Enum.IsDefined(typeof(EquipmentMechanic),profile.progressionGoalMechanic)||profile.progressionGoalMechanic!=EquipmentMechanic.None&&BuildCatalog.MechanicClass(profile.progressionGoalMechanic)!=profile.heroClass)
                profile.progressionGoalMechanic=EquipmentMechanic.None;

            if (profile.fashions == null) profile.fashions = new List<FashionData>();
            var validFashions = new List<FashionData>();
            var fashionIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (FashionData fashion in profile.fashions)
            {
                if (fashion == null || !Enum.IsDefined(typeof(FashionSlot), fashion.slot) ||
                    !Enum.IsDefined(typeof(Rarity), fashion.rarity)) continue;
                string expectedId = "fashion-" + (int)fashion.slot + "-" + (int)fashion.rarity;
                if (!fashionIds.Add(expectedId)) continue;
                fashion.id = expectedId;
                fashion.name = FashionName(fashion.slot, fashion.rarity,profile.heroClass);
                validFashions.Add(fashion);
            }
            profile.fashions = validFashions;
            if (!validFashions.Exists(value => value.id == profile.wingsFashionId && value.slot == FashionSlot.Wings)) profile.wingsFashionId = null;
            if (!validFashions.Exists(value => value.id == profile.weaponFashionId && value.slot == FashionSlot.Weapon)) profile.weaponFashionId = null;
            // Prefer preserving existing valid equipped gear if a damaged save exceeds the cap.
            for (int slot = 0; slot < 3; slot++)
            {
                ItemSlot itemSlot = (ItemSlot)slot;
                string equippedId = slot == 0 ? profile.weaponId : slot == 1 ? profile.armorId : profile.relicId;
                if (equippedId == string.Empty) continue;
                ItemData equipped = items.Find(item => item.id == equippedId && item.slot == itemSlot && item.level <= profile.level);
                if (equipped == null) equipped = items.Find(item => item.slot == itemSlot && item.level <= profile.level);
                if (equipped == null) AddStarterItem(profile, itemSlot);
                else SetEquipped(profile, equipped);
            }
            InitializeSlotUpgrades(profile);
            MigrateOwnedRewards(profile);
            NormalizeAttachments(profile);
            NormalizeClassStates(profile);
            return refundedRanks;
        }

        private static void InitializeSlotUpgrades(GameProfile profile)
        {
            int[] previous = profile.slotUpgradeRanks;
            bool migrate = !profile.slotUpgradesInitialized || previous == null || previous.Length != 3;
            int[] ranks = new int[3];
            for (int slot = 0; slot < ranks.Length; slot++)
                ranks[slot] = previous != null && slot < previous.Length ? Clamp(previous[slot], 0, MaximumUpgrade) : 0;
            var all = new List<ItemData>(profile.inventory);
            all.AddRange(profile.pendingLoot);
            all.AddRange(profile.recoveryLoot);
            if (migrate)
            {
                foreach (ItemData item in all)
                {
                    ranks[(int)item.slot] = Math.Max(ranks[(int)item.slot], Clamp(item.upgradeLevel, 0, MaximumUpgrade));
                    // Retain intentional protection on once-invested legacy gear;
                    // ordinary future drops do not inherit this lock or sale value.
                    if (item.upgradeLevel > 0) item.locked = true;
                }
            }
            profile.slotUpgradeRanks = ranks;
            profile.slotUpgradesInitialized = true;
            foreach (ItemData item in all)
                ApplyUpgradeRank(item, IsEquipped(profile, item.id) ? ranks[(int)item.slot] : 0);
        }

        private static void RepairItem(ItemData item, HeroClass hero)
        {
            item.level = Clamp(item.level, 1, MaximumLevel);
            item.attack = Clamp(item.attack, 0, MaximumEquipmentStat);
            item.defense = Clamp(item.defense, 0, MaximumEquipmentStat);
            item.health = Clamp(item.health, 0, MaximumEquipmentHealth);
            item.upgradeLevel = Clamp(item.upgradeLevel, 0, MaximumUpgrade);
            if (!Enum.IsDefined(typeof(EquipmentMechanic), item.mechanic) ||
                (item.mechanic != EquipmentMechanic.None && BuildCatalog.MechanicSlot(item.mechanic) != item.slot)) item.mechanic = EquipmentMechanic.None;
            item.mechanicVariant = item.mechanicVariantUnlocked && BuildCatalog.HasMechanicVariant(item.mechanic) ? Clamp(item.mechanicVariant, 0, 1) : 0;
            EnsureUpgradeBasis(item);
            if (string.IsNullOrWhiteSpace(item.name)) item.name = "无名" + ItemBaseName(item.slot, hero);
            if (item.name.Length > 60) item.name = item.name.Substring(0, 60);
        }

        private static int[] RepairLoadout(int[] previous)
        {
            if (previous == null) return GameBalance.DefaultLoadout();
            int[] loadout = new int[GameBalance.HotbarSize * GameBalance.HotbarPages];
            for (int i = 0; i < loadout.Length; i++) loadout[i] = -1;
            bool legacy = previous.Length == 3;
            int[] defaults = GameBalance.DefaultLoadout();
            for (int page = 0; page < GameBalance.HotbarPages; page++)
            {
                var used = new HashSet<int>();
                for (int slot = 0; slot < GameBalance.HotbarSize; slot++)
                {
                    int index = page * GameBalance.HotbarSize + slot;
                    int skill = index < previous.Length ? previous[index] : -1;
                    if (legacy && page == 0 && slot >= 3)
                    {
                        skill = defaults[slot];
                        if (skill >= 0 && used.Contains(skill))
                        {
                            skill = 0;
                            while (skill < GameBalance.SkillCount && (used.Contains(skill) || GameBalance.IsPassive(skill))) skill++;
                        }
                    }
                    // Locked default skills remain mapped; casting still requires a learned rank.
                    if ((skill == GameBalance.HotbarPotion || (skill >= 0 && skill < GameBalance.SkillCount && !GameBalance.IsPassive(skill))) && used.Add(skill)) loadout[index] = skill;
                }
            }
            return loadout;
        }

        private static int[] RepairHotbarKeys(int[] previous)
        {
            int[] keys = new int[GameBalance.HotbarSize];
            var used = new HashSet<int>();
            for (int slot = 0; slot < keys.Length; slot++)
            {
                int key = previous != null && slot < previous.Length ? previous[slot] : 0;
                if (GameBalance.IsBindableKey(key) && used.Add(key)) keys[slot] = key;
            }
            for (int slot = 0; slot < keys.Length; slot++)
            {
                if (keys[slot] != 0) continue;
                int key = GameBalance.DefaultHotbarKeys[slot];
                if (used.Contains(key))
                {
                    for (int candidate = 0; candidate < GameBalance.DefaultHotbarKeys.Length; candidate++)
                        if (!used.Contains(GameBalance.DefaultHotbarKeys[candidate])) { key = GameBalance.DefaultHotbarKeys[candidate]; break; }
                }
                keys[slot] = key;
                used.Add(key);
            }
            return keys;
        }

        private static int Clamp(int value, int minimum, int maximum) { return Math.Max(minimum, Math.Min(maximum, value)); }
        private static int Round(float value) { return (int)Math.Round(value, MidpointRounding.AwayFromZero); }
    }
}
