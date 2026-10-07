using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Emberfall.Editor
{
    /// <summary>Isolated Play-mode checks using actual pickup Update ticks and scene transitions.</summary>
    public static class GroundLootValidation
    {
        private const string Prefix = "Emberfall.RuntimeValidation.";
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        public static bool IsCheckingPause { get; private set; }

        public static IEnumerator Validate(GameSession game, Action<bool, string> check, Action<string> log)
        {
            RequireIsolatedRuntime(game);
            if (check == null) throw new ArgumentNullException(nameof(check));
            var fixture = new Fixture(game);
            try
            {
                fixture.Prepare();
                log?.Invoke("GROUND LOOT — actual landing, range, blocked input and scene-exit settlement");
                ProgressionService progression = game.Progression;
                int before = progression.Profile.inventory.Count;
                ItemData first = fixture.Roll();
                check(progression.Profile.inventory.Count == before && game.PendingLootCount == 0, "RollLoot creates data without inventory or scene collection");
                GroundLootPickup pickup = game.SpawnGroundLoot(first, game.Player.transform.position);
                check(pickup != null && game.PendingLootCount == 1 && !pickup.ReadyToCollect && !fixture.Contains(first), "Fresh gear lands visibly with protection and remains outside inventory");
                check(pickup.GetComponentsInChildren<Renderer>().Length >= 4, "Ground gear has actual model, rarity glow and label renderers");
                check(game.SpawnGroundLoot(first, game.Player.transform.position) == pickup && game.PendingLootCount == 1, "Repeated spawning of one item retains a single pickup");
                foreach (object tick in Wait(.1f)) yield return tick;
                check(pickup != null && !pickup.ReadyToCollect && !fixture.Contains(first), "Pickup cannot happen during the first 0.6 seconds even at player feet");
                foreach (object tick in Wait(.8f)) yield return tick;
                check(fixture.Contains(first) && game.PendingLootCount == 0, "Nearby landed gear enters the actual bag through Update");
                check(!game.TryCollectGroundLoot(first.id) && game.SpawnGroundLoot(first, Vector3.zero) == null && progression.Profile.inventory.Count == before + 1, "Repeated collection and respawning cannot duplicate collected gear");

                ItemData distant = fixture.Roll();
                Vector3 distantPoint = game.Player.transform.position + Vector3.right * 6;
                GroundLootPickup farPickup = game.SpawnGroundLoot(distant, distantPoint);
                foreach (object tick in Wait(.8f)) yield return tick;
                check(farPickup != null && farPickup.ReadyToCollect && !fixture.Contains(distant) && game.PendingLootCount == 1, "Landed gear outside pickup range stays on the ground");
                game.Player.Teleport(distantPoint + Vector3.left * 2.1f);
                foreach (object tick in Wait(.15f)) yield return tick;
                check(!fixture.Contains(distant), "A player 2.1 metres away does not collect gear");
                game.Player.Teleport(distantPoint + Vector3.left * 1.8f);
                foreach (object tick in Wait(.2f)) yield return tick;
                check(fixture.Contains(distant) && game.PendingLootCount == 0, "A player inside two metres automatically collects gear");

                ItemData blocked = fixture.Roll();
                Vector3 blockedPoint = game.Player.transform.position + Vector3.left * 5;
                game.SpawnGroundLoot(blocked, blockedPoint);
                foreach (object tick in Wait(.7f)) yield return tick;
                game.SetUIBlocking(true);
                game.Player.Teleport(blockedPoint);
                foreach (object tick in Wait(.2f, false)) yield return tick;
                check(!fixture.Contains(blocked) && game.PendingLootCount == 1, "An open blocking panel prevents nearby automatic pickup");
                IsCheckingPause = true;
                game.SetPaused(true);
                game.SetUIBlocking(false);
                foreach (object tick in Wait(.2f, false)) yield return tick;
                check(game.Paused && !fixture.Contains(blocked), "A real pause prevents nearby pickup until resumed");
                game.SetPaused(false);
                IsCheckingPause = false;
                SetProperty(game, "IsDead", true);
                game.SetUIBlocking(false);
                foreach (object tick in Wait(.2f, false)) yield return tick;
                check(!fixture.Contains(blocked), "A dead session cannot collect nearby gear");
                SetProperty(game, "IsDead", false);
                game.SetUIBlocking(false);
                foreach (object tick in Wait(.2f)) yield return tick;
                check(fixture.Contains(blocked) && game.PendingLootCount == 0, "Unblocking living gameplay resumes automatic pickup");

                ItemData camp = fixture.Roll();
                game.SpawnGroundLoot(camp, new Vector3(-10, 0, -7));
                check(game.PendingLootCount == 1 && !fixture.Contains(camp), "An uncollected distant drop remains pending before camp return");
                game.ReturnToCamp();
                check(!game.InDungeon && fixture.Contains(camp) && game.PendingLootCount == 0, "Returning to camp settles all pending gear before old scene cleanup");
                check(ReceiptCount(game, false) == 0 && ReceiptCount(game, true) == 0,
                    "Successful camp transition releases both old-world receipt sets after settlement");
                IEnumerator wilderness = ValidateWildernessRetention(game, fixture, check);
                while (wilderness.MoveNext()) yield return wilderness.Current;
                fixture.EnterFreshDungeon();
                ValidateRoomReceiptBoundary(game, fixture, check);
                fixture.EnterFreshDungeon();
                ItemData title = fixture.Roll();
                game.SpawnGroundLoot(title, new Vector3(10, 0, -6));
                game.QuitToTitle();
                check(!game.HasStarted && game.PendingLootCount == 0 && fixture.Contains(title), "Returning to title collects distant and protected gear before destroying the player");
                game.ContinueGame();
                progression = game.Progression;
                check(game.HasStarted && fixture.Contains(title) && fixture.Contains(camp), "Scene-exit pickups survive the actual persisted reload");
                fixture.EnterFreshDungeon();
                ItemData settled = fixture.Roll();
                game.SpawnGroundLoot(settled, new Vector3(10, 0, 0));
                int beforeSettle = progression.Profile.inventory.Count;
                game.CollectRemainingDungeonLoot();
                game.CollectRemainingDungeonLoot();
                check(fixture.Contains(settled) && game.PendingLootCount == 0 && progression.Profile.inventory.Count == beforeSettle + 1, "Repeated explicit exit settlement is idempotent");
                log?.Invoke("GROUND LOOT complete; restoring profile and isolated save bytes, then preparing a fresh first wave.");
            }
            finally
            {
                IsCheckingPause = false;
                fixture.Restore();
            }
        }

        private static int ReceiptCount(GameSession game, bool progression)
        {
            object owner = progression ? (object)game.Progression : game;
            return ((HashSet<string>)Field(owner.GetType(), progression ? "collectedLootIds" : "collectedGroundLoot").GetValue(owner)).Count;
        }

        private static IEnumerator ValidateWildernessRetention(GameSession game, Fixture fixture, Action<bool, string> check)
        {
            ProgressionService progression = game.Progression;
            foreach (EnemyController enemy in game.Enemies) if (enemy != null) enemy.enabled = false;
            MethodInfo deliver = typeof(GameSession).GetMethod("DeliverEnemyLoot", PrivateInstance);
            MethodInfo spawn = typeof(GameSession).GetMethod("SpawnWildernessEnemy", PrivateInstance);
            MethodInfo automaticCollect = typeof(GameSession).GetMethod("TryCollectGroundLoot", PrivateInstance, null, new[] { typeof(GroundLootPickup) }, null);
            string temporary = progression.SaveFilePath + ".tmp";
            if (File.Exists(temporary) || Directory.Exists(temporary)) throw new InvalidOperationException("Wilderness fixture refuses existing temporary save data.");
            ItemData retained = fixture.Roll(); Vector3 point = game.Player.transform.position + Vector3.right * 6;
            int bag = progression.Profile.inventory.Count, population = game.Enemies.Count;
            byte[] primary = File.ReadAllBytes(progression.SaveFilePath), backup = File.ReadAllBytes(progression.SaveFilePath + ".bak");
            bool ownsTemporary = false;
            try
            {
                File.Copy(progression.SaveFilePath, temporary); ownsTemporary = true;
                deliver.Invoke(game, new object[] { retained, point });
                GroundLootPickup pending = game.SpawnGroundLoot(retained, point);
                check(!game.InDungeon && pending != null && game.PendingLootCount == 1 && !fixture.Contains(retained) && progression.Profile.inventory.Count == bag,
                    "Rejected wilderness collection retains the exact rolled ID as one visible session-owned pickup");
                spawn.Invoke(game, null);
                check(game.Enemies.Count == population, "A retained wilderness drop prevents replacement enemy producers while existing enemies remain");
                game.Player.Teleport(pending.transform.position);
                foreach (object tick in Wait(.85f)) yield return tick;
                check(game.PendingLootCount == 1 && !fixture.Contains(retained) && ReadPickupRetry(pending) > 0,
                    "Actual wilderness pickup Update retains failed data and backs off disk retries");
                check(Convert.ToBase64String(File.ReadAllBytes(progression.SaveFilePath)) == Convert.ToBase64String(primary) &&
                    Convert.ToBase64String(File.ReadAllBytes(progression.SaveFilePath + ".bak")) == Convert.ToBase64String(backup),
                    "Blocked wilderness pickup leaves primary and backup bytes unchanged");
                File.Delete(temporary); ownsTemporary = false;
                foreach (object tick in Wait(1.25f)) yield return tick;
                check(fixture.Contains(retained) && game.PendingLootCount == 0 && progression.Profile.inventory.Count == bag + 1,
                    "After recovery the same wilderness identity is collected exactly once through real Update");
                check(!(bool)automaticCollect.Invoke(game, new object[] { pending }) && !game.TryCollectGroundLoot(retained.id),
                    "A retired wilderness pickup cannot collect again through either adapter");

                GameProfile beforeCrowding = JsonUtility.FromJson<GameProfile>(JsonUtility.ToJson(progression.Profile));
                try
                {
                    while (progression.Profile.inventory.Count < ProgressionService.InventoryCapacity) progression.Profile.inventory.Add(fixture.Roll());
                    while (progression.Profile.pendingLoot.Count < ProgressionService.PendingLootCapacity) progression.Profile.pendingLoot.Add(fixture.Roll());
                    progression.Save();
                    ItemData protectedDrop = fixture.Roll(); protectedDrop.locked = true;
                    point = game.Player.transform.position + Vector3.right * 4;
                    deliver.Invoke(game, new object[] { protectedDrop, point });
                    GroundLootPickup protectedPickup = game.SpawnGroundLoot(protectedDrop, point);
                    check(protectedPickup != null && game.PendingLootCount == 1 && !fixture.Contains(protectedDrop),
                        "Full bag plus protected mailbox retains a valuable wilderness item instead of discarding or selling it");
                    progression.Profile.inventory.RemoveAt(progression.Profile.inventory.Count - 1); progression.Save();
                    check(game.TryCollectGroundLoot(protectedDrop.id) && fixture.Contains(protectedDrop) && game.PendingLootCount == 0 &&
                        !game.TryCollectGroundLoot(protectedDrop.id), "Freeing one bag slot admits the retained protected identity exactly once");
                }
                finally { SetProperty(progression, "Profile", beforeCrowding); progression.Save(); game.Player.RefreshStats(false); }
            }
            finally { if (ownsTemporary && File.Exists(temporary)) File.Delete(temporary); }
        }

        private static float ReadPickupRetry(GroundLootPickup pickup)
        { return (float)Field(typeof(GroundLootPickup), "retryTime").GetValue(pickup); }

        private static void ValidateRoomReceiptBoundary(GameSession game, Fixture fixture, Action<bool, string> check)
        {
            var run = new RoomChainState(); RoomChainPlan first = run.Room;
            for (int i = 0; i < first.EnemyCount; i++) { run.Register(first, i); run.Defeat(first, i); }
            for(int tick=0;tick<24;tick++)run.Advance(.25f,true,true,false);
            SetProperty(game, "RoomChainRun", run);
            ItemData item = fixture.Roll();
            game.SpawnGroundLoot(item, game.Player.transform.position);
            check(game.TryCollectGroundLoot(item.id) && ReceiptCount(game, false) > 0 && ReceiptCount(game, true) > 0,
                "Room boundary fixture starts with both real collected receipt sets");
            GameObject world = (GameObject)Field(typeof(GameSession), "world").GetValue(game);
            EnemyController[] enemies = game.Enemies.ToArray();
            SkillRuntime runtime = (SkillRuntime)Field(typeof(PlayerController), "skillRuntime").GetValue(game.Player);
            runtime.Advance(200); runtime.FillEnergy(); runtime.TryConsume(0, 1);
            float cooldown = runtime.Remaining(0); int epoch = ReadCombatEpoch(game.Player);
            game.Player.transform.position = new Vector3(0, 0, 14);
            check(game.EnterNextRoom() && game.RoomChainRun.Room.Index == 1 && !world.activeSelf && ReadCombatEpoch(game.Player) != epoch &&
                ReceiptCount(game, false) == 0 && ReceiptCount(game, true) == 0 && runtime.Remaining(0) == cooldown,
                "Successful real room travel retires both receipt sets after old world/epoch and preserves skill cooldown");
            int kills = game.Progression.Profile.kills;
            foreach (EnemyController enemy in enemies) game.OnEnemyKilled(enemy);
            check(game.Progression.Profile.kills == kills && game.PendingLootCount == 0,
                "Late old-room enemy callbacks cannot recreate drops or rewards after receipt retirement");
            typeof(GameSession).GetMethod("ChangeZone", PrivateInstance).Invoke(game, new object[] { false });
        }

        private static IEnumerable<object> Wait(float seconds, bool scaled = true)
        {
            double start = scaled ? Time.timeAsDouble : EditorApplication.timeSinceStartup;
            double deadline = EditorApplication.timeSinceStartup + 6;
            while ((scaled ? Time.timeAsDouble : EditorApplication.timeSinceStartup) - start < seconds)
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Ground-loot validation did not receive game-time updates.");
                yield return null;
            }
        }

        private static void RequireIsolatedRuntime(GameSession game)
        {
            if (!Application.isPlaying || !EditorApplication.isPlaying || !SessionState.GetBool(Prefix + "Active", false) ||
                game == null || game != GameSession.Instance || !game.HasStarted || !game.InDungeon || game.InputBlocked || game.DungeonWave != 1 || game.PendingLootCount != 0)
                throw new InvalidOperationException("Ground-loot validation requires the isolated runner in an active fresh dungeon first wave with no pending drops.");
            string result = SessionState.GetString(Prefix + "Results", "");
            string save = SessionState.GetString(Prefix + "Save", "");
            string overridden = SessionState.GetString("Emberfall.ValidationSaveDirectory", "");
            if (string.IsNullOrWhiteSpace(result) || string.IsNullOrWhiteSpace(save) || string.IsNullOrWhiteSpace(overridden))
                throw new InvalidOperationException("Ground-loot validation refused missing isolated paths.");
            string expectedRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tests", "TestResults"));
            string runName = Path.GetFileName(Full(result));
            Guid guid;
            if (!Same(Path.GetDirectoryName(Full(result)), expectedRoot) || !runName.StartsWith("PlayMode-", StringComparison.Ordinal) ||
                !Guid.TryParseExact(runName.Substring("PlayMode-".Length), "N", out guid) || !Same(save, Path.Combine(result, "IsolatedSave")) ||
                !Same(save, overridden) || !Same(save, game.Progression.SaveDirectory) || !IsSlotFileInDirectory(game.Progression.SaveFilePath, save))
                throw new InvalidOperationException("Ground-loot validation refused a save path outside its isolated runner.");
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
        private static int ReadCombatEpoch(PlayerController player)
        {
            PropertyInfo property = typeof(PlayerController).GetProperty("CombatEpoch", PrivateInstance);
            if (property == null) throw new MissingMemberException(typeof(PlayerController).FullName, "CombatEpoch");
            return (int)property.GetValue(player);
        }
        private static void SetProperty(object owner, string name, object value)
        {
            PropertyInfo property = owner.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            MethodInfo setter = property == null ? null : property.GetSetMethod(true);
            if (setter == null) throw new MissingMemberException(owner.GetType().FullName, name);
            setter.Invoke(owner, new[] { value });
        }

        private sealed class Fixture
        {
            private readonly GameSession game;
            private readonly GameProfile originalProfile;
            private readonly string originalError;
            private readonly object notification;
            private readonly object notificationUntil;
            private readonly bool playerEnabled;
            private readonly string[] filePaths;
            private readonly byte[][] fileBytes;
            private readonly HashSet<string> sessionCollected;
            private readonly HashSet<string> progressionCollected;
            private readonly string[] originalSessionCollected;
            private readonly string[] originalProgressionCollected;

            public Fixture(GameSession session)
            {
                game = session;
                originalProfile = game.Progression.Profile;
                originalError = game.Progression.LastError;
                notification = Field(typeof(GameSession), "notification").GetValue(game);
                notificationUntil = Field(typeof(GameSession), "notificationUntil").GetValue(game);
                playerEnabled = game.Player.enabled;
                string path = game.Progression.SaveFilePath;
                filePaths = new[] { path, path + ".bak", path + ".tmp" };
                fileBytes = new byte[filePaths.Length][];
                for (int i = 0; i < filePaths.Length; i++) fileBytes[i] = File.Exists(filePaths[i]) ? File.ReadAllBytes(filePaths[i]) : null;
                sessionCollected = (HashSet<string>)Field(typeof(GameSession), "collectedGroundLoot").GetValue(game);
                progressionCollected = (HashSet<string>)Field(typeof(ProgressionService), "collectedLootIds").GetValue(game.Progression);
                originalSessionCollected = new List<string>(sessionCollected).ToArray();
                originalProgressionCollected = new List<string>(progressionCollected).ToArray();
            }

            public void Prepare()
            {
                GameProfile profile = JsonUtility.FromJson<GameProfile>(JsonUtility.ToJson(originalProfile));
                profile.level = Math.Max(2, profile.level);
                profile.inventory.RemoveAll(item => item.id != profile.weaponId && item.id != profile.armorId && item.id != profile.relicId);
                SetProperty(game.Progression, "Profile", profile);
                FreezeFixture();
            }

            private void FreezeFixture()
            {
                game.SetUIBlocking(false);
                game.SetPaused(false);
                game.Player.enabled = false;
                game.Player.Teleport(new Vector3(0, 0, -12));
                for (int i = 0; i < game.Enemies.Count; i++)
                {
                    EnemyController enemy = game.Enemies[i];
                    if (enemy == null) continue;
                    enemy.enabled = false;
                    enemy.transform.position = new Vector3((i - 2) * 1.5f, 0, 12);
                }
            }

            public ItemData Roll()
            {
                ItemData item = game.Progression.RollLoot(2, true);
                item.id = "ground-validation-" + Guid.NewGuid().ToString("N");
                return item;
            }
            public bool Contains(ItemData item) { return game.Progression.Profile.inventory.Exists(value => value != null && value.id == item.id); }
            public void EnterFreshDungeon()
            {
                game.Player.Teleport(new Vector3(0, 0, 11));
                game.EnterDungeon();
                if (!game.DungeonSelectionOpen) throw new InvalidOperationException("Ground-loot fixture could not open the dungeon selector.");
                game.ConfirmDungeonSelection();
                if (!game.InDungeon || game.DungeonSelectionOpen || game.DungeonWave != 1) throw new InvalidOperationException("Ground-loot fixture could not re-enter a fresh dungeon.");
                FreezeFixture();
            }

            public void Restore()
            {
                try
                {
                    game.CollectRemainingDungeonLoot();
                    SetProperty(game, "IsDead", false);
                    SetProperty(game.Progression, "Profile", originalProfile);
                    if (!game.HasStarted) typeof(GameSession).GetMethod("BeginAdventure", PrivateInstance).Invoke(game, null);
                    typeof(GameSession).GetMethod("ChangeZone", PrivateInstance).Invoke(game, new object[] { true });
                    game.SetPaused(false);
                    game.SetUIBlocking(false);
                    foreach (EnemyController enemy in game.Enemies) if (enemy != null) enemy.enabled = false;
                    game.Player.enabled = playerEnabled;
                }
                finally
                {
                    SetProperty(game.Progression, "Profile", originalProfile);
                    sessionCollected.Clear(); sessionCollected.UnionWith(originalSessionCollected);
                    var currentReceipts = (HashSet<string>)Field(typeof(ProgressionService), "collectedLootIds").GetValue(game.Progression);
                    currentReceipts.Clear(); currentReceipts.UnionWith(originalProgressionCollected);
                    SetProperty(game.Progression, "LastError", originalError);
                    Field(typeof(GameSession), "notification").SetValue(game, notification);
                    Field(typeof(GameSession), "notificationUntil").SetValue(game, notificationUntil);
                    for (int i = 0; i < filePaths.Length; i++)
                    {
                        if (fileBytes[i] != null) File.WriteAllBytes(filePaths[i], fileBytes[i]);
                        else if (File.Exists(filePaths[i])) File.Delete(filePaths[i]);
                    }
                }
            }
        }
    }
}
