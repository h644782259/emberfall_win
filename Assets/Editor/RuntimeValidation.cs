using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Emberfall.Editor
{
    /// <summary>Runs real play-mode smoke tests without touching a player's save directory.</summary>
    [InitializeOnLoad]
    public static class RuntimeValidation
    {
        private const string Prefix = "Emberfall.RuntimeValidation.";
        private const string SaveOverride = "Emberfall.ValidationSaveDirectory";
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static IEnumerator routine;
        private static Delay waiting;
        private static bool finishing;
        private static bool validatingPause;

        private sealed class Delay
        {
            private readonly double until;
            private readonly bool scaled;
            public Delay(float seconds, bool useGameTime = true)
            {
                scaled = useGameTime;
                until = (scaled ? Time.timeAsDouble : EditorApplication.timeSinceStartup) + seconds;
            }
            public bool Ready { get { return (scaled ? Time.timeAsDouble : EditorApplication.timeSinceStartup) >= until; } }
        }

        [Serializable]
        private sealed class EditorInfrastructureError
        {
            public string signature;
            public string unityVersion;
            public string message;
            public string stackTrace;
        }

        [Serializable]
        private sealed class Result
        {
            public bool passed;
            public bool runtimePassed;
            public string unityVersion;
            public string resultsDirectory;
            public string isolatedSaveDirectory;
            public int assertions;
            public int consoleErrors;
            public int runtimeErrors;
            public int totalObservedConsoleErrors;
            public int editorInfrastructureErrorCount;
            public EditorInfrastructureError[] editorInfrastructureErrors;
            public int screenshots;
            public float elapsedSeconds;
            public string failure;
            public string captureScope = "Authentic Camera.Render images of the runtime 3D world; OnGUI is exercised but is not included in camera captures.";
        }

        static RuntimeValidation()
        {
            EditorApplication.update += Update;
            Application.logMessageReceived += OnLog;
        }

        /// <summary>CLI: Unity -batchmode -projectPath ... -executeMethod Emberfall.Editor.RuntimeValidation.Run (no -quit / -nographics).</summary>
        [MenuItem("Emberfall/验证真实运行时 Runtime smoke test", false, 40)]
        public static void Run()
        {RunSuite(false);}

        public static void RunNpcInteractions()
        {RunSuite(true);}

        private static void RunSuite(bool npcOnly)
        {
            if (SessionState.GetBool(Prefix + "Active", false)) throw new InvalidOperationException("Runtime validation is already running.");
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop the current play session before running isolated validation.");
            if (!Application.isBatchMode && UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save the edited scene before running runtime validation.");

            string workspace = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string results = Path.Combine(workspace, "Tests", "TestResults", "PlayMode-" + Guid.NewGuid().ToString("N"));
            string saves = Path.Combine(results, "IsolatedSave");
            Directory.CreateDirectory(saves);
            SessionState.SetString(Prefix + "Results", results);
            SessionState.SetString(Prefix + "Save", saves);
            SessionState.SetString(Prefix + "PreviousOverride", SessionState.GetString(SaveOverride, ""));
            SessionState.SetString(SaveOverride, saves);
            SessionState.SetInt(Prefix + "Assertions", 0);
            SessionState.SetInt(Prefix + "Errors", 0);
            SessionState.SetInt(Prefix + "InfrastructureErrors", 0);
            SessionState.SetInt(Prefix + "Screenshots", 0);
            SessionState.SetFloat(Prefix + "Started", (float)EditorApplication.timeSinceStartup);
            SessionState.SetBool(Prefix + "Active", true);
            SessionState.SetBool(Prefix + "NpcOnly", npcOnly);
            finishing = false;
            routine = null;
            waiting = null;
            Append("START Unity " + Application.unityVersion + " | isolated saves: " + saves);
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);
#if UNITY_6000_0_OR_NEWER
            // Unity 6 only creates its missing default Search index while in edit mode.
            // Fully enumerate this public, lazy API before requesting Play; otherwise
            // deferred startup indexing can read an empty database list after Play starts.
            try
            {
                int databaseCount = 0;
                foreach (var database in UnityEditor.Search.SearchService.EnumerateDatabases())
                    databaseCount++;
                Append("EDITOR Search initialized before Play: " + databaseCount + " database(s).");
            }
            catch (Exception exception)
            {
                Finish(false, "Editor Search initialization before Play failed: " + exception);
                return;
            }
#endif
            EditorApplication.isPlaying = true;
        }

        private static void Update()
        {
            if (finishing || !SessionState.GetBool(Prefix + "Active", false)) return;
            try
            {
                double elapsed = EditorApplication.timeSinceStartup - SessionState.GetFloat(Prefix + "Started", 0);
                if (elapsed > 260) throw new TimeoutException("Runtime validation exceeded 260 seconds including play-mode startup.");
                if (EditorApplication.isCompiling || !EditorApplication.isPlaying) return;
                EditorApplication.QueuePlayerLoopUpdate();
                if (GameSession.Instance == null) return;
                if (GameSession.Instance.Paused && !validatingPause && !GroundLootValidation.IsCheckingPause) GameSession.Instance.SetPaused(false);
                if (routine == null)
                {
                    Append("Play mode entered; InitializeOnLoad restored the validation runner after domain reload.");
                    Application.runInBackground = true;
                    MobileControls.ValidationUsesSimulation = true;
                    validatingPause=SessionState.GetBool(Prefix+"NpcOnly",false);
                    routine = validatingPause?HubNpcValidation.Validate(GameSession.Instance,Check,Append,SessionState.GetString(Prefix+"Results","")):Smoke();
                }
                if (waiting != null && !waiting.Ready) return;
                waiting = null;
                if (!routine.MoveNext())
                {
                    Check(SessionState.GetInt(Prefix + "Errors", 0) == 0, "No game/runtime errors or other unclassified console exceptions");
                    Finish(true, null);
                    return;
                }
                waiting = routine.Current as Delay;
            }
            catch (Exception exception)
            {
                Exception failure = exception is TargetInvocationException && exception.InnerException != null ? exception.InnerException : exception;
                Finish(false, failure.ToString());
            }
        }

        private static IEnumerator Smoke()
        {
            GameSession game = GameSession.Instance;
            string savePath = (string)Field(typeof(ProgressionService), "savePath").GetValue(game.Progression);
            string isolated = Path.GetFullPath(SessionState.GetString(Prefix + "Save", "")) + Path.DirectorySeparatorChar;
            Check(Path.GetFullPath(savePath).StartsWith(isolated, StringComparison.OrdinalIgnoreCase), "Save isolation verified before the first write");
            Check(!game.HasStarted && game.Player == null, "Runtime bootstrap creates a title session");
            Check(game.GetComponent<GameUI>() != null && Camera.main != null, "Runtime UI and camera bootstrap");
            GameAudio.Muted = true;
            IEnumerator audioValidation = AudioValidation.Validate(Check, Append);
            while (audioValidation.MoveNext()) yield return audioValidation.Current;
            yield return new Delay(.25f, false);

            IEnumerator saveSlotValidation = SaveSlotRuntimeValidation.Validate(game, Check, Append);
            while (saveSlotValidation.MoveNext()) yield return saveSlotValidation.Current;

            for (int heroIndex = 0; heroIndex < 4; heroIndex++)
            {
                HeroClass hero = (HeroClass)heroIndex;
                Append("HERO " + hero + " — learning, passives, all eight active skills, camera capture");
                game.StartNew(hero);
                game.SetPaused(false);
                Check(game.Player != null && game.Player.HeroClass == hero && game.HasStarted, hero + " initializes correctly");
                int inventoryAssertions = InventoryUIValidation.Validate(game);
                SessionState.SetInt(Prefix + "Assertions", SessionState.GetInt(Prefix + "Assertions", 0) + inventoryAssertions);
                Append("INVENTORY " + hero + " passed " + inventoryAssertions + " classification, sorting and sale checks.");
                FreezeEnemies(game);
                if (heroIndex == 0)
                {
                    validatingPause = true;
                    PersistenceTransitionValidation.Validate(game, Check, Append);
                    IEnumerator feedbackValidation = FeedbackValidation.Validate(game, Check, Append);
                    while (feedbackValidation.MoveNext()) yield return feedbackValidation.Current;
                    IEnumerator mobileValidation = MobileValidation.Validate(game, Check, Append);
                    while (mobileValidation.MoveNext()) yield return mobileValidation.Current;
                    validatingPause = false;
                    IEnumerator cameraValidation = CameraValidation.Validate(game, Check, Append);
                    while (cameraValidation.MoveNext()) yield return cameraValidation.Current;
                    IEnumerator traversalValidation = TraversalValidation.Validate(game, Check, Append);
                    while (traversalValidation.MoveNext()) yield return traversalValidation.Current;
                }
                yield return new Delay(.5f);
                WorldLabelValidation.Validate(ZoneKind.Wilderness, Check);
                CaptureWorld(hero + "-world.png", false);

                game.Progression.GrantExperience(1000000);
                Check(game.Progression.Profile.level == 100, hero + " reaches level 100 through XP progression");
                StatBlock beforePassive = game.Progression.GetStats();
                for (int skill = 0; skill < GameBalance.SkillCount; skill++)
                    for (int rank = game.Progression.Profile.skillRanks[skill] + 1; rank <= 3; rank++)
                        Check(game.Progression.LearnSkill(skill), hero + " learns " + skill + " stage " + rank);
                StatBlock afterPassive = game.Progression.GetStats();
                Check(hero == HeroClass.Ranger ? afterPassive.CritChance > beforePassive.CritChance && afterPassive.MoveSpeed > beforePassive.MoveSpeed : afterPassive.Damage > beforePassive.Damage,
                    hero + " learned offensive passive changes actual stats");
                Check(!game.AssignSkill(0, 3) && !game.AssignSkill(0, 8), hero + " passives cannot enter the hotbar");
                foreach (int skill in game.Progression.Profile.equippedSkills)
                    Check(skill < 0 || !GameBalance.IsPassive(skill), hero + " loadout contains active skills only");
                float energyBeforePassive = game.Player.Energy;
                Cast(game.Player, 3);
                Cast(game.Player, 8);
                Check(Mathf.Approximately(game.Player.Energy, energyBeforePassive) && game.Player.SkillCooldownRemaining(3) == 0 && game.Player.SkillCooldownRemaining(8) == 0,
                    hero + " passive cast attempts consume no energy and create no cooldown");

                GameUI ui = game.GetComponent<GameUI>();
                OpenPanel(ui, "Inventory");
                Check(game.InputBlocked && Mathf.Approximately(Time.timeScale, 0), hero + " inventory pauses combat");
                yield return new Delay(.2f, false);
                Call(ui, "ClosePanel");
                OpenPanel(ui, "Skills");
                Field(typeof(GameUI), "selectedSkill").SetValue(ui, 3);
                Check(game.InputBlocked && Mathf.Approximately(Time.timeScale, 0), hero + " skill panel pauses combat");
                yield return new Delay(.2f, false);
                Field(typeof(GameUI), "selectedSkill").SetValue(ui, 9);
                yield return new Delay(.2f, false);
                Call(ui, "OpenBindings");
                yield return new Delay(.2f, false);
                Call(ui, "ClosePanel");
                Call(ui, "ClosePanel");
                Check(!game.InputBlocked && Mathf.Approximately(Time.timeScale, 1), hero + " modal close restores gameplay");

                if (heroIndex == 0)
                {
                    int oldSecondKey = game.Progression.Profile.hotbarKeys[1];
                    int oldFirstKey = game.Progression.Profile.hotbarKeys[0];
                    Check(game.Progression.SetHotbarKey(0, oldSecondKey), "Custom key assignment accepted");
                    Check(game.Progression.Profile.hotbarKeys[1] == oldFirstKey, "Conflicting custom keys swap slots");
                    Check(!game.Progression.SetHotbarKey(0, (int)KeyCode.W), "Movement key binding rejected");
                    for (int slot = 0; slot < GameBalance.HotbarSize; slot++) game.Progression.SetHotbarKey(slot, GameBalance.DefaultHotbarKeys[slot]);
                }

                IEnumerator combatSystems = ValidateCombatSystems(game, hero);
                while (combatSystems.MoveNext()) yield return combatSystems.Current;

                validatingPause = true;
                game.Player.enabled = false;
                IEnumerator chargeValidation = ChargeValidation.Validate(game, Check, Append);
                while (chargeValidation.MoveNext()) yield return chargeValidation.Current;
                if (hero == HeroClass.Summoner)
                {
                    IEnumerator summonerValidation = SummonerValidation.Validate(game, Check, Append);
                    while (summonerValidation.MoveNext()) yield return summonerValidation.Current;
                }
                game.Player.enabled = true;
                validatingPause = false;

                for (int skill = 0; skill < GameBalance.SkillCount; skill++)
                {
                    if (GameBalance.IsPassive(skill)) continue;
                    PrepareCastFixture(game);
                    SkillRuntime runtime = Runtime(game.Player);
                    runtime.Advance(200f); // Fixture reset between independent casts; never used for the paging assertion.
                    runtime.FillEnergy();
                    SetField(game.Player, "aimPoint", game.Player.transform.position + Vector3.forward * 3);
                    game.Player.transform.rotation = hero == HeroClass.Ranger && skill == 4 ? Quaternion.Euler(0,180,0) : Quaternion.identity;
                    if (hero == HeroClass.Ranger && skill == 4) SetField(game.Player,"aimPoint",game.Player.transform.position+Vector3.back*3);
                    SkillCategory category = GameBalance.GetSkillCategory(hero, skill);
                    if (category == SkillCategory.Healing)
                    {
                        SetField(game.Player, "invulnerability", 0f);
                        game.Player.TakeDamage(game.Player.MaxHealth);
                        Check(game.Player.Health < game.Player.MaxHealth && !game.IsDead, hero + " healing fixture is injured and alive");
                    }
                    float healthBefore = game.Player.Health;
                    EnemyController[] castEnemies = game.Enemies.ToArray();
                    float enemiesBefore = EnemyHealth(castEnemies);
                    Vector3 positionBefore = game.Player.transform.position;
                    float energyBefore = game.Player.Energy;
                    Cast(game.Player, skill);
                    Check(Mathf.Abs(game.Player.Energy - (energyBefore - GameBalance.SkillEnergyCost(hero, skill))) < .02f, hero + " skill " + skill + " spends its configured resource cost");
                    Check(Mathf.Abs(game.Player.SkillCooldownRemaining(skill) - GameBalance.EffectiveCooldown(hero, skill, 3)) < .02f, hero + " skill " + skill + " uses the awakened cooldown");

                    if (skill == 0)
                    {
                        float cooldown = game.Player.SkillCooldownRemaining(skill);
                        game.SetHotbarPage(1);
                        Check(game.AssignSkill(0, skill), hero + " skill can be configured on page two");
                        Check(Mathf.Abs(game.Player.CooldownRemaining(0) - cooldown) < .02f, hero + " page two preserves the existing skill cooldown");
                        game.SetHotbarPage(2);
                        Check(game.AssignSkill(7, skill), hero + " skill can be configured on page three");
                        float beforeRejectedCast = game.Player.Energy;
                        Cast(game.Player, skill);
                        Check(Mathf.Approximately(beforeRejectedCast, game.Player.Energy) && Mathf.Abs(game.Player.CooldownRemaining(7) - cooldown) < .02f,
                            hero + " paging cannot bypass cooldown or double-spend resources");
                        Call(game.Player, "OnBasicAttackHit", game.Player.transform.position);
                        Check(Mathf.Abs(game.Player.Energy - Mathf.Min(game.Player.MaxEnergy, beforeRejectedCast + 8)) < .02f, hero + " a basic attack hit restores eight resource points");
                        game.SetHotbarPage(0);
                        float pausedCooldown = game.Player.SkillCooldownRemaining(skill);
                        validatingPause = true;
                        game.SetPaused(true);
                        yield return new Delay(.25f, false);
                        Check(Mathf.Abs(game.Player.SkillCooldownRemaining(skill) - pausedCooldown) < .02f, hero + " pause freezes active cooldowns");
                        game.SetPaused(false);
                        validatingPause = false;
                    }

                    if (category == SkillCategory.Defense)
                    {
                        SetField(game.Player, "invulnerability", 0f);
                        float unprotected = 100f * CombatBalance.ArmorDamageMultiplier(game.Progression.GetStats().Armor, game.Progression.Profile.level);
                        float before = game.Player.Health;
                        game.Player.TakeDamage(100f);
                        Check(before - game.Player.Health < unprotected * .8f, hero + " defense ability reduces actual incoming damage");
                    }
                    float settle = skill == 9 ? 3.9f : hero == HeroClass.Summoner && skill == 7 ? 2.9f : category == SkillCategory.Healing ? 1.25f : skill == 2 ? 1.15f : 1f;
                    yield return new Delay(settle);
                    Check(!game.IsDead, hero + " remains alive after skill " + skill);
                    Check(game.Player.SkillCooldownRemaining(skill) < GameBalance.EffectiveCooldown(hero, skill, 3), hero + " skill " + skill + " cooldown advances in live frames");
                    if (category == SkillCategory.Healing) Check(game.Player.Health > healthBefore, hero + " healing skill restores real health over time");
                    if (hero == HeroClass.Summoner && (skill == 2 || skill == 4 || skill == 9))
                        Check(SummonedCompanion.Count(game.Player, skill == 9) > 0, "Summoning skill creates its actual combat companion");
                    else if (category == SkillCategory.Damage || category == SkillCategory.Control)
                        Check(EnemyHealth(castEnemies) < enemiesBefore, hero + " skill " + skill + " damages runtime enemies");
                    if (category == SkillCategory.Mobility) Check(Vector3.Distance(game.Player.transform.position, positionBefore) > 1f, hero + " mobility skill moves the actual character");
                    if (skill == 9) CaptureWorld(hero + "-awakened-ultimate.png", false);
                }
            }

            Append("DUNGEON — portal, three real waves, boss, loot, death, respawn and persisted reload");
            game.Player.Teleport(new Vector3(0, 0, 11));
            game.EnterDungeon();
            Check(game.DungeonSelectionOpen && !game.InDungeon && game.InputBlocked && Time.timeScale == 0,
                "Portal opens a blocking tier selector without entering early");
            game.ConfirmDungeonSelection();
            Check(game.InDungeon && !game.DungeonSelectionOpen && game.DungeonWave == 1 && game.Enemies.Count > 0, "Confirming the selector creates dungeon wave one");
            IEnumerator groundLootValidation = GroundLootValidation.Validate(game, Check, Append);
            while (groundLootValidation.MoveNext()) yield return groundLootValidation.Current;
            game.Player.enabled = true;
            WorldLabelValidation.Validate(ZoneKind.Dungeon, Check);
            foreach (DestructibleProp prop in UnityEngine.Object.FindObjectsByType<DestructibleProp>(FindObjectsSortMode.None))
            {
                Renderer[] visuals = prop.GetComponentsInChildren<Renderer>();
                Check(visuals.Length > 0, "Destructible scenery has visible geometry: " + prop.name);
                Bounds bounds = visuals[0].bounds;
                foreach (Renderer visual in visuals) bounds.Encapsulate(visual.bounds);
                Check(bounds.size.x <= prop.Radius * 2 + .3f && bounds.size.z <= prop.Radius * 2 + .3f && bounds.size.y <= 2.5f,
                    "Imported destructible scenery stays at meter scale within its gameplay footprint: " + prop.name);
            }
            CaptureWorld("Dungeon-wave-one.png", false);
            int originalClears = game.Progression.Profile.clearedRuns;
            int originalItems = game.Progression.Profile.inventory.Count;
            int lastWave = 0, blessingCount = 0;
            double dungeonDeadline = EditorApplication.timeSinceStartup + 35;
            while (!game.DungeonCleared)
            {
                if (EditorApplication.timeSinceStartup > dungeonDeadline)
                    throw new TimeoutException("Dungeon validation stalled at wave " + game.DungeonWave + "; enemies=" + game.Enemies.Count + "; choice=" + game.RunChoices.AwaitingChoice);
                game.SetPaused(false);
                if (game.RunChoices.AwaitingChoice)
                {
                    int completedWave = game.DungeonWave;
                    Check(game.Enemies.Count == 0 && game.InputBlocked && Time.timeScale == 0,
                        "Completed wave " + completedWave + " pauses for a blessing after every reinforcement is defeated");
                    Check(game.ConfirmBlessing(0), "A blessing explicitly advances wave " + completedWave);
                    blessingCount++;
                    Check(game.DungeonWave == completedWave + 1 && !game.RunChoices.AwaitingChoice && !game.ConfirmBlessing(0),
                        "Repeated blessing confirmation cannot grant or advance twice");
                }
                if (game.Enemies.Count > 0)
                {
                    Check(game.DungeonWave == lastWave || game.DungeonWave == lastWave + 1,
                        "Enemy batch belongs to the current wave or its immediate successor");
                    if (game.DungeonWave != lastWave) Append("Dungeon advances to distinct wave " + game.DungeonWave);
                    else Append("Dungeon reinforcement batch remains in wave " + game.DungeonWave);
                    lastWave = game.DungeonWave;
                    FreezeEnemies(game);
                    if (lastWave == game.TotalWaves)
                    {
                        bool bossPresent = game.Enemies.Exists(enemy => enemy != null && enemy.IsBoss);
                        Check(bossPresent, "Final wave contains the guardian boss");
                        CaptureWorld("Dungeon-boss.png", false);
                    }
                    EnemyController[] victims = game.Enemies.ToArray();
                    foreach (EnemyController enemy in victims) enemy.TakeDamage(100000000f, Vector3.forward);
                }
                // Modal selection stops game time; the harness must retain an independent deadline.
                yield return new Delay(.15f, false);
            }
            Check(lastWave == 3 && blessingCount == 2 && game.Progression.Profile.clearedRuns == originalClears + 1,
                "Three waves and two explicit blessings award exactly one clear");
            Check(game.CombatEnded && !game.ModeFinished, "Ordinary three-wave completion is a terminal combat state independently of special modes");
            float clearedHealth = game.Player.Health;
            int clearedGold = game.Progression.Profile.gold, clearedKills = game.Progression.Profile.kills;
            SetField(game.Player, "invulnerability", 0f);
            game.Player.TakeDamageFrom(100000000f, "Validation late hostile impact");
            Check(!game.IsDead && !game.Player.IsDead && game.Player.Health == clearedHealth && game.Progression.Profile.gold == clearedGold,
                "A late lethal hostile hit after ordinary clear cannot kill the player or apply a death penalty");
            // A callback arriving after completion must be ignored even if its
            // enemy is still registered. This isolated target grants no fixture loot.
            EnemyController lateEnemy = SpawnFixtureEnemy(game, EnemyKind.Guardian, game.Player.transform.position + Vector3.forward * 5);
            float lateHealth = lateEnemy.Health;
            try
            {
                lateEnemy.TakeDamage(100000000f, Vector3.forward);
                game.OnEnemyKilled(lateEnemy);
                Check(lateEnemy.Health == lateHealth && game.Enemies.Contains(lateEnemy) && game.Progression.Profile.kills == clearedKills &&
                    game.Progression.Profile.gold == clearedGold && game.Progression.Profile.clearedRuns == originalClears + 1,
                    "Ordinary-clear terminal policy rejects stale enemy damage and kill rewards without awarding another clear");
            }
            finally { game.Enemies.Remove(lateEnemy); lateEnemy.gameObject.SetActive(false); UnityEngine.Object.Destroy(lateEnemy.gameObject); }
            Check(game.PendingLootCount > 0, "Dungeon rewards remain visibly on the ground until pickup");
            Check(game.Progression.Profile.pendingFashionChest && game.Progression.OpenDungeonChest() != null,
                "The completed run offers exactly one chest");
            int chestGold = game.Progression.Profile.gold;
            string chestReceipt = game.Progression.LastChestReward.Id;
            Check(game.Progression.Profile.pendingChestReveal && !game.Progression.Profile.pendingFashionChest &&
                game.Progression.OpenDungeonChest() == null && game.Progression.Profile.gold == chestGold,
                "Opening again cannot reroll or grant another reward during the reveal");
            var savedReward = new ProgressionService(game.Progression.SaveDirectory);
            Check(savedReward.LoadSlot(game.Progression.CurrentSlotId) && savedReward.Profile.pendingChestReveal && savedReward.LastChestReward.Id == chestReceipt && savedReward.Profile.gold == chestGold,
                "The saved chest receipt survives reload without granting again");
            Check(game.Progression.AcknowledgeChestReward() && !game.Progression.Profile.pendingChestReveal,
                "Acknowledging the receipt clears only the reveal state");
            Call(game.GetComponent<GameUI>(), "ClosePanel");
            game.ReturnToCamp();
            Check(game.PendingLootCount == 0 && game.Progression.Profile.inventory.Count > originalItems, "Dungeon exit automatically collects remaining equipment exactly once");
            Check(!game.InDungeon && !game.CombatEnded && game.Player.Health == game.Player.MaxHealth, "Dungeon exit clears terminal combat state and heals at camp");
            int inventoryBeforeDeath = game.Progression.Profile.inventory.Count;
            int goldBeforeDeath = game.Progression.Profile.gold;
            SetField(game.Player, "invulnerability", 0f);
            game.Player.TakeDamage(100000000f);
            Check(game.IsDead && game.Player.IsDead && Time.timeScale == 0, "Lethal damage opens death state and stops combat");
            Check(game.Progression.Profile.gold == goldBeforeDeath, "Death preserves earned gold under the current recovery policy");
            yield return new Delay(.25f, false);
            game.Respawn();
            Check(!game.IsDead && !game.Player.IsDead && game.Player.Health == game.Player.MaxHealth && game.Progression.Profile.inventory.Count == inventoryBeforeDeath,
                "Respawn restores health and preserves equipment");
            game.Progression.Save();
            Check(string.IsNullOrEmpty(game.Progression.LastError) && File.Exists(game.Progression.SaveFilePath), "Isolated progress is saved successfully");
            game.QuitToTitle();
            Check(!game.HasStarted && Time.timeScale == 0, "Save-and-title ends active combat");
            yield return new Delay(.2f, false);
            game.ContinueGame();
            Check(game.HasStarted && game.Progression.Profile.level == 100 && game.Progression.Profile.clearedRuns == originalClears + 1,
                "Continue reloads isolated level and dungeon progress");
            for (int skill = 0; skill < GameBalance.SkillCount; skill++) Check(game.Progression.Profile.skillRanks[skill] == 3, "Persisted evolution restored for skill " + skill);
            yield return new Delay(.25f);
            Append("COMPLETE: four heroes, 32 active casts, touch controls and new encounter systems exercised in the Unity player loop.");
        }

        private static IEnumerator ValidateCombatSystems(GameSession game, HeroClass hero)
        {
            PlayerController player = game.Player;
            bool wasEnabled = player.enabled;
            float respawnTimer = (float)Field(typeof(GameSession), "respawnTimer").GetValue(game);
            int firstAssertion = SessionState.GetInt(Prefix + "Assertions", 0);
            player.enabled = false; // Real mouse/key state must not overwrite controlled aiming fixtures.
            SetField(game, "respawnTimer", 10000f);
            try
            {
                Append("COMBAT " + hero + " — projected body selection, attack direction, direct casts and uncommitted ground placement");
                EnemyKind[] kinds = { EnemyKind.Slime, EnemyKind.Wisp, EnemyKind.Guardian };
                for (int i = 0; i < kinds.Length; i++)
                {
                    ResetCombatFixture(game);
                    EnemyController target = SpawnFixtureEnemy(game, kinds[i], player.transform.position + Vector3.forward * 8, i == 2);
                    Vector3 screen = ProjectBody(player, target);
                    Check(screen.z > 0 && Camera.main.pixelRect.Contains(new Vector2(screen.x, screen.y)), hero + " " + kinds[i] + " body projects inside the actual camera");
                    Call(player, "ApplyAim", new Vector2(screen.x, screen.y));
                    Check(player.AimTarget == target && Vector3.Distance(player.AimPoint, target.transform.position) < .01f, hero + " selects " + kinds[i] + " body instead of ground behind it");
                    player.transform.position += Vector3.right * 2;
                    Vector3 expected = (target.transform.position - player.transform.position).normalized;
                    Call(player, "BasicAttack");
                    Check(Vector3.Angle(player.transform.forward, expected) < .1f, hero + " recalculates facing after movement before attacking " + kinds[i]);
                    if (hero != HeroClass.Vanguard)
                    {
                        Component shot = FindBasicProjectile(player);
                        Check(shot != null && Vector3.Angle(ReadVector(shot, "direction"), expected) < .1f, hero + " projectile follows recalculated body direction");
                        Check(Mathf.Abs(ReadFloat(shot, "impactHeight") - ((Vector3)Call(player, "EnemyBodyPoint", target)).y) < .01f && Mathf.Abs(shot.transform.position.y - 1.15f) < .01f,
                            hero + " projectile travels from muzzle height toward " + kinds[i] + " body height");
                    }
                    Call(player, "ApplyAim", new Vector2(Camera.main.pixelRect.xMax - 2, Camera.main.pixelRect.yMax - 2));
                    Check(player.AimTarget == null, hero + " cursor away from all bodies retains free aiming");
                }

                IEnumerator projectiles = ValidateBasicProjectiles(game, hero);
                while (projectiles.MoveNext()) yield return projectiles.Current;
                IEnumerator placement = ValidateSkillPlacement(game, hero);
                while (placement.MoveNext()) yield return placement.Current;
                if (hero == HeroClass.Vanguard)
                {
                    IEnumerator status = ValidateNeutralAndStatusEffects(game);
                    while (status.MoveNext()) yield return status.Current;
                }
            }
            finally
            {
                player.enabled = wasEnabled;
                SetField(game, "respawnTimer", respawnTimer);
                game.SetPaused(false);
                validatingPause = false;
            }
            Append("COMBAT " + hero + " completed " + (SessionState.GetInt(Prefix + "Assertions", 0) - firstAssertion) + " additional assertions.");
        }

        private static IEnumerator ValidateBasicProjectiles(GameSession game, HeroClass hero)
        {
            if (hero == HeroClass.Vanguard) yield break;
            ResetCombatFixture(game);
            PlayerController player = game.Player;
            Vector3 origin = player.transform.position;
            EnemyController target = SpawnFixtureEnemy(game, EnemyKind.Wisp, origin + Vector3.forward * 16);
            Vector3 screen = ProjectBody(player, target);
            Call(player, "ApplyAim", new Vector2(screen.x, screen.y));
            Call(player, "BasicAttack");
            Component shot = FindBasicProjectile(player);
            Check(shot != null && Mathf.Abs(ReadFloat(shot, "lifetime") - 1.15f) < .001f, hero + " basic projectile retains its original 1.15 second lifetime");
            Check((EnemyController)Field(shot.GetType(), "homingTarget").GetValue(shot) == (hero != HeroClass.Ranger ? target : null), hero + " uses the correct mage homing / straight arrow target policy");
            Vector3 initial = ReadVector(shot, "direction");
            if (hero == HeroClass.Ranger) target.transform.position += Vector3.right * .8f;
            yield return new Delay(.25f);
            Check(shot != null, hero + " basic projectile remains alive before its original expiry");
            Vector3 earlyDirection = ReadVector(shot, "direction");
            if (hero == HeroClass.Ranger)
                Check(Vector3.Angle(initial, earlyDirection) <= 12.7f, "Ranger early steering stays inside the 70 degrees/second, .18 second correction budget");
            target.transform.position += Vector3.right * 6;
            yield return new Delay(.12f);
            Check(shot != null, hero + " projectile survives the moving-target steering fixture");
            Vector3 lateDirection = ReadVector(shot, "direction");
            if (hero == HeroClass.Arcanist || hero == HeroClass.Summoner)
            {
                Check(lateDirection.x > earlyDirection.x + .02f && (EnemyController)Field(shot.GetType(), "homingTarget").GetValue(shot) == target,
                    "Mage bolt still follows its original moving target after the ranger correction window has ended");
                EnemyController replacement = SpawnFixtureEnemy(game, EnemyKind.Wisp, origin + new Vector3(-8, 0, 15));
                target.TakeDamage(100000000f, Vector3.zero);
                yield return new Delay(.08f);
                Check(shot != null && (EnemyController)Field(shot.GetType(), "homingTarget").GetValue(shot) != replacement && Vector3.Angle(ReadVector(shot, "direction"), lateDirection) < .1f,
                    "Mage bolt keeps its last direction after target death and never retargets another monster");
            }
            else
                Check(Vector3.Angle(earlyDirection, lateDirection) < .1f, "Ranger arrow stays straight after its brief correction window");
            yield return new Delay(1.2f);
            Check(shot == null, hero + " basic projectile expires without a homing range extension");

            if (hero == HeroClass.Arcanist || hero == HeroClass.Summoner)
            {
                ResetCombatFixture(game);
                origin = player.transform.position;
                EnemyController nearest = SpawnFixtureEnemy(game, EnemyKind.Slime, origin + new Vector3(2, 0, 7));
                EnemyController farther = SpawnFixtureEnemy(game, EnemyKind.Slime, origin + new Vector3(0, 0, 11));
                SpawnFixtureEnemy(game, EnemyKind.Slime, origin - Vector3.forward * 3);
                SpawnFixtureEnemy(game, EnemyKind.Slime, origin + Vector3.right * 8);
                SetField(player, "aimPoint", origin + Vector3.forward * 20);
                Check((EnemyController)Call(player, "MagicConeTarget") == nearest, "Mage fallback selects nearest living monster inside the 14 metre / 35 degree cone");
                Call(player, "BasicAttack");
                Check(player.AimTarget == nearest, "Mage ordinary attack actually acquires the valid cone fallback");
                nearest.gameObject.SetActive(false);
                SetField(player, "aimPoint", origin + Vector3.forward * 20);
                Check((EnemyController)Call(player, "MagicConeTarget") == farther, "Mage fallback ignores inactive closer monsters");
                farther.transform.position = origin + Vector3.forward * 15;
                Check(Call(player, "MagicConeTarget") == null, "Mage fallback cannot lock distant, sideward or rear monsters globally");
            }
        }

        private static IEnumerator ValidateSkillPlacement(GameSession game, HeroClass hero)
        {
            ResetCombatFixture(game);
            PlayerController player = game.Player;
            SkillRuntime runtime = Runtime(player);
            runtime.Advance(200); runtime.FillEnergy();
            SkillTargetingController targeting = player.GetComponent<SkillTargetingController>();
            int skill = hero == HeroClass.Vanguard ? 9 : 1;
            for (int candidate = 0; candidate < GameBalance.SkillCount; candidate++)
            {
                bool expected = hero == HeroClass.Vanguard ? candidate == 9 :
                    hero == HeroClass.Arcanist ? candidate == 1 || candidate == 2 || candidate == 7 || candidate == 9 :
                    hero == HeroClass.Summoner ? candidate == 1 || candidate == 7 || candidate == 9 :
                    candidate == 1 || candidate == 2 || candidate == 5 || candidate == 9;
                Check(SkillTargetingController.RequiresConfirmation(hero, candidate) == expected,
                    hero + " skill " + candidate + " has its intended direct / ground-confirmation policy");
            }
            Check(!SkillTargetingController.RequiresConfirmation(hero, -1) && !SkillTargetingController.RequiresConfirmation(hero, GameBalance.SkillCount),
                hero + " invalid skill identifiers cannot request ground confirmation");
            EnemyController target = SpawnFixtureEnemy(game, EnemyKind.Guardian, player.transform.position + Vector3.forward * 8);
            Vector3 screen = ProjectBody(player, target);
            Call(player, "ApplyAim", new Vector2(screen.x, screen.y));
            float energy = player.Energy;
            Check(targeting != null && targeting.Begin(skill) && targeting.IsTargeting && Mathf.Approximately(player.Energy, energy) && player.SkillCooldownRemaining(skill) == 0,
                hero + " entering skill preview spends no resources and starts no cooldown");
            Check(PlacementVisible(targeting), hero + " an actual ground-confirmation skill displays its preview geometry");
            targeting.SetTarget(player.transform.position + Vector3.right * 1000);
            Check(Vector3.Distance(player.transform.position, targeting.TargetPoint) <= targeting.CurrentPreview.distance + .01f && targeting.TargetPoint.magnitude <= game.ArenaRadius + .01f && WorldTraversal.HasLineOfSight(player.transform.position, targeting.TargetPoint),
                hero + " out-of-range placement respects cast distance, arena and intervening cover");
            targeting.Cancel();
            Check(!targeting.IsTargeting && targeting.CancelledThisFrame && targeting.TickInput() && Mathf.Approximately(player.Energy, energy) && player.SkillCooldownRemaining(skill) == 0,
                hero + " cancelling preserves energy/cooldown and consumes the same-frame ordinary attack input");
            Check(!PlacementVisible(targeting), hero + " cancelling hides all placement geometry");
            Check(targeting.Begin(skill), hero + " cancelled preview can be entered again");
            Vector3 chosen = player.transform.position + new Vector3(-3, 0, 2);
            targeting.SetTarget(chosen);
            Check(targeting.Confirm() && !targeting.IsTargeting && player.AimTarget == null && Vector3.Distance(player.AimPoint, chosen) < .01f,
                hero + " confirmation keeps the precise chosen ground point and clears a prior monster lock");
            if (SkillChargeController.Duration(hero, skill) > 0)
            {
                Check(player.GetComponent<SkillChargeController>().IsCharging && Mathf.Approximately(player.Energy, energy), hero + " confirmed heavy skill begins charging without an early resource charge");
                yield return new Delay(SkillChargeController.Duration(hero, skill) + .08f);
            }
            float spentEnergy = energy - GameBalance.SkillEnergyCost(hero, skill);
            float cooldown = GameBalance.EffectiveCooldown(hero, skill, 3);
            Check(Mathf.Abs(player.Energy - spentEnergy) < .01f && Mathf.Abs(player.SkillCooldownRemaining(skill) - cooldown) < .01f,
                hero + " confirmation consumes the configured cost and cooldown exactly once");
            Check(!targeting.Confirm() && !targeting.Begin(skill) && Mathf.Abs(player.Energy - spentEnergy) < .01f && Mathf.Abs(player.SkillCooldownRemaining(skill) - cooldown) < .01f,
                hero + " duplicate confirmation or cooldown preview cannot double-spend resources");

            // Vanguard has only one ground-confirmation skill. Finish the earlier
            // independent cooldown checks, then explicitly reset this fixture and use
            // a direct cast as the cooldown that teleport/pause must preserve.
            yield return new Delay(.01f);
            runtime.Advance(200); runtime.FillEnergy();
            Check(targeting.Begin(0) && !targeting.IsTargeting && !PlacementVisible(targeting),
                hero + " a ready self/directional skill releases immediately without visible placement");
            float directEnergy = player.MaxEnergy - GameBalance.SkillEnergyCost(hero, 0);
            float directCooldown = GameBalance.EffectiveCooldown(hero, 0, 3);
            Check(Mathf.Abs(player.Energy - directEnergy) < .01f && Mathf.Abs(player.SkillCooldownRemaining(0) - directCooldown) < .01f,
                hero + " direct release consumes its resource and cooldown exactly once");
            Check(!targeting.Confirm() && !targeting.Begin(0) && !targeting.Begin(6) && targeting.TickInput() &&
                !targeting.IsTargeting && !PlacementVisible(targeting) &&
                Mathf.Abs(player.Energy - directEnergy) < .01f && player.SkillCooldownRemaining(6) == 0,
                hero + " direct release rejects duplicate/different same-frame casts and suppresses ordinary attack without spending again");
            yield return new Delay(.01f);
            runtime.FillEnergy();
            Check(targeting.Begin(skill) && targeting.IsTargeting && PlacementVisible(targeting),
                hero + " a ready ground skill may enter preview while the direct skill cools down");
            int learnedHealingRank = game.Progression.Profile.skillRanks[6];
            try
            {
                game.Progression.Profile.skillRanks[6] = 0;
                Check(!targeting.Begin(6) && targeting.IsTargeting && PlacementVisible(targeting) &&
                    targeting.SkillName == GameBalance.SkillName(hero, skill) && Mathf.Approximately(player.Energy, player.MaxEnergy) &&
                    player.SkillCooldownRemaining(6) == 0,
                    hero + " an unlearned direct-cast request cannot spend resources or replace an existing valid ground preview");
            }
            finally { game.Progression.Profile.skillRanks[6] = learnedHealingRank; }
            player.Teleport(player.transform.position + Vector3.right);
            Check(!targeting.IsTargeting && !PlacementVisible(targeting) && !targeting.Confirm() && Mathf.Approximately(player.Energy, player.MaxEnergy) &&
                player.SkillCooldownRemaining(skill) == 0 && Mathf.Abs(player.SkillCooldownRemaining(0) - directCooldown) < .01f,
                hero + " teleport invalidates pending placement while preserving existing independent cooldowns");
            Check(targeting.Begin(skill), hero + " teleport leaves ready ground skills available");
            validatingPause = true;
            game.SetPaused(true);
            yield return new Delay(.12f, false);
            Check(!targeting.IsTargeting && !PlacementVisible(targeting) && Mathf.Approximately(player.Energy, player.MaxEnergy) &&
                player.SkillCooldownRemaining(skill) == 0 && Mathf.Abs(player.SkillCooldownRemaining(0) - directCooldown) < .01f,
                hero + " paused player loop cancels uncommitted placement without spending");
            game.SetPaused(false);
            validatingPause = false;

            IEnumerator direct = ValidateDirectSkills(game, hero);
            while (direct.MoveNext()) yield return direct.Current;
        }

        private static bool PlacementVisible(SkillTargetingController targeting)
        {
            GameObject visuals = (GameObject)Field(typeof(SkillTargetingController), "visuals").GetValue(targeting);
            return visuals != null && visuals.activeInHierarchy;
        }

        private static IEnumerator ValidateDirectSkills(GameSession game, HeroClass hero)
        {
            PlayerController player = game.Player;
            SkillRuntime runtime = Runtime(player);
            SkillTargetingController targeting = player.GetComponent<SkillTargetingController>();
            for (int skill = 0; skill < GameBalance.SkillCount; skill++)
            {
                if (GameBalance.IsPassive(skill) || SkillTargetingController.RequiresConfirmation(hero, skill)) continue;
                ResetCombatFixture(game);
                runtime.Advance(200); runtime.FillEnergy();
                player.transform.rotation = Quaternion.identity;
                SetField(player, "aimPoint", player.transform.position + (hero == HeroClass.Ranger && skill == 4 ? Vector3.back : Vector3.forward) * 6);
                int learnedRank = game.Progression.Profile.skillRanks[skill];
                try
                {
                    game.Progression.Profile.skillRanks[skill] = 0;
                    Check(!targeting.Begin(skill) && !targeting.IsTargeting && !PlacementVisible(targeting) &&
                        Mathf.Approximately(player.Energy, player.MaxEnergy) && player.SkillCooldownRemaining(skill) == 0,
                        hero + " unlearned direct skill " + skill + " cannot release, display a preview, spend or start cooldown");
                }
                finally { game.Progression.Profile.skillRanks[skill] = learnedRank; }
                Check(targeting.Begin(skill) && !targeting.IsTargeting && !PlacementVisible(targeting),
                    hero + " direct skill " + skill + " releases without needing a second click or showing preview geometry");
                if (SkillChargeController.Duration(hero, skill) > 0)
                    yield return new Delay(SkillChargeController.Duration(hero, skill) + .08f);
                float expectedEnergy = player.MaxEnergy - GameBalance.SkillEnergyCost(hero, skill);
                float expectedCooldown = GameBalance.EffectiveCooldown(hero, skill, learnedRank);
                Check(Mathf.Abs(player.Energy - expectedEnergy) < .01f && Mathf.Abs(player.SkillCooldownRemaining(skill) - expectedCooldown) < .01f,
                    hero + " direct skill " + skill + " uses its configured energy and learned cooldown");
                Check(!targeting.Begin(skill) && !targeting.Confirm() && (SkillChargeController.Duration(hero, skill) > 0 || targeting.TickInput()) &&
                    Mathf.Abs(player.Energy - expectedEnergy) < .01f && Mathf.Abs(player.SkillCooldownRemaining(skill) - expectedCooldown) < .01f,
                    hero + " direct skill " + skill + " cannot double-spend through another Begin or Confirm");
                // Wait for a genuine player-loop frame: EditorApplication.update can
                // run more than once while Time.frameCount is unchanged.
                yield return new Delay(.01f);
            }
            ResetCombatFixture(game);
        }

        private static IEnumerator ValidateNeutralAndStatusEffects(GameSession game)
        {
            ResetCombatFixture(game);
            PlayerController player = game.Player;
            EnemyController neutral = SpawnFixtureEnemy(game, EnemyKind.Slime, player.transform.position + Vector3.forward);
            neutral.enabled = true;
            float health = player.Health;
            yield return new Delay(1.05f);
            Check(neutral.Tier == EnemyController.ThreatTier.Normal && !neutral.IsAggro && (int)Field(typeof(EnemyController), "attackNumber").GetValue(neutral) == 0 && Mathf.Approximately(player.Health, health),
                "An ordinary monster remains neutral after a nearby player spends one full second within melee range");
            neutral.transform.position = player.transform.position + Vector3.forward;
            SetField(neutral, "attackCooldown", 0f);
            SetField(player, "invulnerability", 0f);
            neutral.TakeDamage(1, Vector3.zero);
            Check(neutral.IsAggro, "Damage provokes the formerly neutral monster immediately");
            yield return new Delay(.85f);
            Check((int)Field(typeof(EnemyController), "attackNumber").GetValue(neutral) > 0 && player.Health < health,
                "Provoked ordinary monster actually retaliates and deals damage in live player-loop frames");

            ResetCombatFixture(game);
            EnemyController normal = SpawnFixtureEnemy(game, EnemyKind.Guardian, player.transform.position + new Vector3(-4, 0, 8));
            EnemyController boss = SpawnFixtureEnemy(game, EnemyKind.Guardian, player.transform.position + new Vector3(4, 0, 8), true);
            normal.StatusEffects.Slow(3, .6f);
            boss.StatusEffects.Slow(3, .6f);
            Check(Mathf.Abs(normal.StatusEffects.MoveMultiplier - .4f) < .001f && normal.IsAggro,
                "Slow reduces the real movement multiplier and provokes the target");
            Check(boss.StatusEffects.MoveMultiplier > normal.StatusEffects.MoveMultiplier && boss.StatusEffects.MoveMultiplier < 1,
                "Boss slow resistance reduces the applied movement penalty");
            health = normal.Health;
            normal.TakeDamage(10, Vector3.zero);
            float unmarked = health - normal.Health;
            normal.StatusEffects.Mark(3, .2f);
            health = normal.Health;
            normal.TakeDamage(10, Vector3.zero);
            Check(Mathf.Abs((health - normal.Health) - unmarked * 1.2f) < .02f && normal.StatusEffects.DamageMultiplier > 1,
                "Mark increases actual incoming damage by the configured vulnerability");
            normal.StatusEffects.Knockdown(3);
            boss.StatusEffects.Knockdown(3);
            Check(normal.StatusEffects.KnockedDown && normal.IsStunned && !boss.StatusEffects.KnockedDown && !boss.IsStunned,
                "Knockdown controls ordinary enemies while Boss armor rejects raw hard control");
            Check(ReadFloat(boss.StatusEffects, "downTime") < ReadFloat(normal.StatusEffects, "downTime") && ReadFloat(boss, "stunTime") < ReadFloat(normal, "stunTime"),
                "Boss raw knockdown has zero granted duration while ordinary control is bounded");
            normal.enabled = boss.enabled = true;
            yield return new Delay(.85f);
            Check(!boss.StatusEffects.KnockedDown && !boss.IsStunned && normal.StatusEffects.KnockedDown && normal.IsStunned,
                "Boss recovers from resisted control while ordinary enemy remains knocked down in actual frames");
            normal.enabled = boss.enabled = false;
            normal.StatusEffects.Freeze(2);
            Check(normal.IsStunned && normal.StatusEffects.MoveMultiplier < 1 && normal.StatusEffects.Summary.Contains("击倒"),
                "Freeze preserves simultaneous control and slow without erasing an active knockdown");

            EnemyController poisoned = SpawnFixtureEnemy(game, EnemyKind.Guardian, player.transform.position + Vector3.forward * 8);
            health = poisoned.Health;
            for (int i = 0; i < 4; i++) poisoned.StatusEffects.Poison(player, 3, 4);
            Check(poisoned.StatusEffects.PoisonStacks == 3 && Mathf.Approximately(poisoned.Health, health),
                "Poison caps at three stacks and deals no immediate application damage");
            yield return new Delay(.85f);
            Check(poisoned.Health <= health - 12 + .02f && poisoned.StatusEffects.PoisonStacks == 3,
                "Three poison stacks deal their delayed damage in live frames");
            health = poisoned.Health;
            player.Teleport(player.transform.position);
            yield return new Delay(.12f);
            Check(poisoned.StatusEffects.PoisonStacks == 0 && Mathf.Approximately(poisoned.Health, health),
                "Teleport invalidates poison from the previous combat generation without another damage tick");
        }

        private static void ResetCombatFixture(GameSession game)
        {
            ClearFixtureEnemies(game);
            Type projectileType = typeof(PlayerController).Assembly.GetType("Emberfall.CombatProjectile", true);
            foreach (UnityEngine.Object value in UnityEngine.Object.FindObjectsOfType(projectileType))
            {
                Component projectile = (Component)value;
                projectile.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(projectile.gameObject);
            }
            game.Player.Teleport(new Vector3(5, 0, 5));
            game.Player.RefreshStats(true);
            Camera.main.GetComponent<AdventureCamera>().Snap();
            game.SetPaused(false);
        }

        private static void ClearFixtureEnemies(GameSession game)
        {
            EnemyController[] previous = game.Enemies.ToArray();
            game.Enemies.Clear();
            foreach (EnemyController enemy in previous)
            {
                if (enemy == null) continue;
                enemy.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(enemy.gameObject);
            }
        }

        private static EnemyController SpawnFixtureEnemy(GameSession game, EnemyKind kind, Vector3 position, bool boss = false)
        {
            Call(game, "SpawnEnemy", kind, 100, position, boss);
            EnemyController enemy = game.Enemies[game.Enemies.Count - 1];
            enemy.enabled = false;
            return enemy;
        }

        private static Vector3 ProjectBody(PlayerController player, EnemyController enemy)
        {
            return Camera.main.WorldToScreenPoint((Vector3)Call(player, "EnemyBodyPoint", enemy));
        }

        private static Component FindBasicProjectile(PlayerController player)
        {
            Type type = typeof(PlayerController).Assembly.GetType("Emberfall.CombatProjectile", true);
            foreach (UnityEngine.Object value in UnityEngine.Object.FindObjectsOfType(type))
                if (Field(type, "owner").GetValue(value) == (object)player && (bool)Field(type, "basicAttack").GetValue(value)) return (Component)value;
            return null;
        }

        private static float ReadFloat(object target, string name) { return (float)Field(target.GetType(), name).GetValue(target); }
        private static Vector3 ReadVector(object target, string name) { return (Vector3)Field(target.GetType(), name).GetValue(target); }

        private static void PrepareCastFixture(GameSession game)
        {
            EnemyController[] old = game.Enemies.ToArray();
            game.Enemies.Clear();
            foreach (EnemyController enemy in old)
            {
                if (enemy == null) continue;
                enemy.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(enemy.gameObject);
            }
            game.Player.Teleport(new Vector3(5, 0, 5));
            game.Player.RefreshStats(true);
            for (int i = 0; i < 3; i++) Call(game, "SpawnEnemy", EnemyKind.Guardian, 100, game.Player.transform.position + Vector3.forward * (2 + i * 3), false);
            FreezeEnemies(game);
            game.SetPaused(false);
        }

        private static void FreezeEnemies(GameSession game)
        {
            foreach (EnemyController enemy in game.Enemies) if (enemy != null) enemy.enabled = false;
        }

        private static float EnemyHealth(EnemyController[] enemies)
        {
            float total = 0;
            foreach (EnemyController enemy in enemies) if (enemy != null && !enemy.IsDead) total += enemy.Health;
            return total;
        }

        private static float EnemyHealth(GameSession game)
        {
            float health = 0;
            foreach (EnemyController enemy in game.Enemies) if (enemy != null) health += enemy.Health;
            return health;
        }

        private static SkillRuntime Runtime(PlayerController player) { return (SkillRuntime)Field(typeof(PlayerController), "skillRuntime").GetValue(player); }
        private static void Cast(PlayerController player, int skill) { Call(player, "CastSkill", skill); }
        private static FieldInfo Field(Type type, string name)
        {
            FieldInfo result = type.GetField(name, PrivateInstance);
            if (result == null) throw new MissingFieldException(type.FullName, name);
            return result;
        }
        private static void SetField(object target, string name, object value) { Field(target.GetType(), name).SetValue(target, value); }
        private static object Call(object target, string name, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(name, PrivateInstance);
            if (method == null) throw new MissingMethodException(target.GetType().FullName, name);
            return method.Invoke(target, arguments);
        }
        private static void OpenPanel(GameUI ui, string name)
        {
            Type type = typeof(GameUI).GetNestedType("Panel", BindingFlags.NonPublic);
            Call(ui, "TogglePanel", Enum.Parse(type, name));
        }

        private static void CaptureWorld(string filename, bool overview)
        {
            Camera camera = Camera.main;
            Check(camera != null, "Camera exists for " + filename);
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                throw new InvalidOperationException("Graphics device is null. Run validation without -nographics to verify actual rendering.");
            RenderTexture oldTarget = camera.targetTexture;
            RenderTexture oldActive = RenderTexture.active;
            Vector3 oldPosition = camera.transform.position;
            Quaternion oldRotation = camera.transform.rotation;
            float oldAspect = camera.aspect;
            var render = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
            var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            try
            {
                if (overview)
                {
                    camera.transform.position = new Vector3(0, 26, -29);
                    camera.transform.LookAt(new Vector3(0, 0, 1));
                }
                camera.aspect = 1600f / 900;
                camera.targetTexture = render;
                camera.Render();
                RenderTexture.active = render;
                image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                image.Apply();
                byte[] png = image.EncodeToPNG();
                Check(png != null && png.Length > 16000, "3D render produced image data: " + filename);
                File.WriteAllBytes(Path.Combine(SessionState.GetString(Prefix + "Results", ""), filename), png);
                SessionState.SetInt(Prefix + "Screenshots", SessionState.GetInt(Prefix + "Screenshots", 0) + 1);
                Append("CAPTURE " + filename + " — actual runtime Camera.Render, 1600 × 900 (world only)");
            }
            finally
            {
                camera.targetTexture = oldTarget;
                camera.aspect = oldAspect;
                camera.transform.SetPositionAndRotation(oldPosition, oldRotation);
                RenderTexture.active = oldActive;
                render.Release();
                UnityEngine.Object.DestroyImmediate(render);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }

        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("FAIL: " + label);
            SessionState.SetInt(Prefix + "Assertions", SessionState.GetInt(Prefix + "Assertions", 0) + 1);
            Append("PASS " + label);
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (!SessionState.GetBool(Prefix + "Active", false) || (type != LogType.Error && type != LogType.Assert && type != LogType.Exception)) return;
            // Unity 6000.6.3f1 can schedule its first Search index initialization after
            // Play mode starts. The editor then declines to create the default index,
            // but EnumerateAll reads element zero of the empty database list anyway.
            // Classify only the exact observed editor-only startup stack; preserve it
            // in both reports. All project frames and all other errors remain fatal.
            if (IsObservedSearchStartupException(condition, stackTrace, type))
            {
                int index = SessionState.GetInt(Prefix + "InfrastructureErrors", 0);
                var diagnostic = new EditorInfrastructureError
                {
                    signature = "Unity6000.6.3f1.SearchStartup.EmptyDefaultDatabase",
                    unityVersion = Application.unityVersion,
                    message = condition,
                    stackTrace = stackTrace
                };
                SessionState.SetString(Prefix + "InfrastructureError." + index, JsonUtility.ToJson(diagnostic));
                SessionState.SetInt(Prefix + "InfrastructureErrors", index + 1);
                Append("EDITOR_INFRASTRUCTURE_EXCEPTION " + diagnostic.signature + ": " + condition + "\n" + stackTrace);
                return;
            }
            SessionState.SetInt(Prefix + "Errors", SessionState.GetInt(Prefix + "Errors", 0) + 1);
            Append("CONSOLE " + type + ": " + condition + "\n" + stackTrace);
        }

        private static bool IsObservedSearchStartupException(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception || Application.unityVersion != "6000.6.3f1" || string.IsNullOrEmpty(condition) || string.IsNullOrEmpty(stackTrace)) return false;
            return condition.StartsWith("ArgumentOutOfRangeException: Index was out of range.", StringComparison.Ordinal)
                && condition.IndexOf("Parameter name: index", StringComparison.Ordinal) >= 0
                && stackTrace.IndexOf("UnityEditor.Search.SearchDatabase+<EnumerateAll>", StringComparison.Ordinal) >= 0
                && stackTrace.IndexOf("UnityEditor.Search.SearchDatabase.GetDefaultSearchDatabase", StringComparison.Ordinal) >= 0
                && stackTrace.IndexOf("UnityEditor.Search.SearchInit.IndexationOnStartup", StringComparison.Ordinal) >= 0
                && stackTrace.IndexOf("UnityEditor.EditorApplication.Internal_CallDelayFunctions", StringComparison.Ordinal) >= 0
                && stackTrace.IndexOf("Emberfall", StringComparison.OrdinalIgnoreCase) < 0
                && condition.IndexOf("Emberfall", StringComparison.OrdinalIgnoreCase) < 0;
        }

        private static void Append(string message)
        {
            string directory = SessionState.GetString(Prefix + "Results", "");
            if (string.IsNullOrEmpty(directory)) return;
            File.AppendAllText(Path.Combine(directory, "runtime-validation.txt"), DateTime.UtcNow.ToString("O") + " " + message + Environment.NewLine, new UTF8Encoding(false));
        }

        private static void Finish(bool passed, string failure)
        {
            if (finishing) return;
            finishing = true;
            MobileControls.ValidationUsesSimulation = false;
            string results = SessionState.GetString(Prefix + "Results", "");
            int infrastructureCount = SessionState.GetInt(Prefix + "InfrastructureErrors", 0);
            var infrastructure = new EditorInfrastructureError[infrastructureCount];
            for (int i = 0; i < infrastructureCount; i++)
                infrastructure[i] = JsonUtility.FromJson<EditorInfrastructureError>(SessionState.GetString(Prefix + "InfrastructureError." + i, "{}"));
            var result = new Result
            {
                passed = passed,
                runtimePassed = passed,
                unityVersion = Application.unityVersion,
                resultsDirectory = results,
                isolatedSaveDirectory = SessionState.GetString(Prefix + "Save", ""),
                assertions = SessionState.GetInt(Prefix + "Assertions", 0),
                consoleErrors = SessionState.GetInt(Prefix + "Errors", 0),
                runtimeErrors = SessionState.GetInt(Prefix + "Errors", 0),
                totalObservedConsoleErrors = SessionState.GetInt(Prefix + "Errors", 0) + infrastructureCount,
                editorInfrastructureErrorCount = infrastructureCount,
                editorInfrastructureErrors = infrastructure,
                screenshots = SessionState.GetBool(Prefix+"NpcOnly",false)?Directory.GetFiles(results,"npc-*.png").Length:SessionState.GetInt(Prefix + "Screenshots", 0),
                elapsedSeconds = (float)EditorApplication.timeSinceStartup - SessionState.GetFloat(Prefix + "Started", 0),
                failure = failure
            };
            Append((passed ? "RUNTIME SUCCESS" : "RUNTIME FAILED") + " | " + result.assertions + " assertions | " + result.screenshots + " captures | " + result.runtimeErrors + " runtime errors | " + infrastructureCount + " editor infrastructure exceptions (full stacks retained) | " + result.elapsedSeconds.ToString("0.0") + " sec\n" + failure);
            File.WriteAllText(Path.Combine(results, "runtime-validation.json"), JsonUtility.ToJson(result, true), new UTF8Encoding(false));
            SessionState.SetBool(Prefix + "Active", false);
            string previous = SessionState.GetString(Prefix + "PreviousOverride", "");
            if (string.IsNullOrEmpty(previous)) SessionState.EraseString(SaveOverride);
            else SessionState.SetString(SaveOverride, previous);
            SessionState.SetString(Prefix + "LastResults", results);
            routine = null;
            waiting = null;
            if (passed) Debug.Log("Emberfall real play-mode validation PASS: " + results);
            else Debug.LogError("Emberfall real play-mode validation FAILED: " + failure + "\nResults: " + results);
            if (infrastructureCount > 0) Debug.LogWarning("Runtime validation separately recorded " + infrastructureCount + " exact Unity 6000.6.3f1 Search startup exceptions. See editorInfrastructureErrors in the JSON report; these have not been erased or repaired.");
            if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }
    }
}
