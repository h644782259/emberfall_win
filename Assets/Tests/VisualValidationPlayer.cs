#if EMBERFALL_VISUAL_VALIDATION
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Emberfall
{
    /// <summary>Only compiled into the dedicated validation player. Captures real rendered IMGUI.</summary>
    public sealed partial class VisualValidationPlayer : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private const float TimeoutSeconds = 230f;
        private static string outputDirectory;
        private static string saveDirectory;
        private static string setupError;
        private GameSession session;
        private GameUI ui;
        private Result result;
        private float started;
        private bool finished;
        private bool expectedPause;
        private int requestedWidth;
        private int requestedHeight;
        private bool movingPreview;
        private EnemyController hitPreview;

        [Serializable]
        private sealed class Screenshot
        {
            public string name;
            public string file;
            public int requestedWidth;
            public int requestedHeight;
            public int actualWidth;
            public int actualHeight;
            public int bytes;
            public int distinctSampledColors;
        }

        [Serializable]
        private sealed class Result
        {
            public string status = "RUNNING";
            public string unityVersion;
            public string outputDirectory;
            public string isolatedSaveDirectory;
            public string captureScope = "Actual player backbuffer after WaitForEndOfFrame, including IMGUI; player input and enemy AI disabled only in this validation process.";
            public int assertions;
            public int consoleErrors;
            public float elapsedSeconds;
            public string failure = "";
            public List<string> errors = new List<string>();
            public List<Screenshot> screenshots = new List<Screenshot>();
        }

        public static string SaveDirectory
        {
            get
            {
                ConfigurePaths();
                return saveDirectory;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            try
            {
                ConfigurePaths();
                Application.runInBackground = true;
                GameAudio.Muted = true;
                var runner = new GameObject("Isolated Full-frame Visual Validation");
                DontDestroyOnLoad(runner);
                runner.AddComponent<VisualValidationPlayer>();
            }
            catch (Exception exception)
            {
                setupError = exception.ToString();
                Debug.LogError("Visual validation startup failed: " + setupError);
                Application.Quit(1);
            }
        }

        private static void ConfigurePaths()
        {
            if (!string.IsNullOrEmpty(setupError)) throw new InvalidOperationException(setupError);
            if (!string.IsNullOrEmpty(saveDirectory)) return;
            string argument = null;
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; i++)
                if (arguments[i] == "--visual-validation-root") { argument = arguments[i + 1]; break; }
            if (string.IsNullOrWhiteSpace(argument) || !Path.IsPathRooted(argument))
                throw new InvalidOperationException("The test player requires --visual-validation-root with an absolute isolated output directory.");
            outputDirectory = Path.GetFullPath(argument);
            string candidate = Path.GetFullPath(Path.Combine(outputDirectory, "Saves"));
            string defaultDirectory = Path.GetFullPath(Application.persistentDataPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string releaseDirectory = Path.Combine(Path.GetDirectoryName(defaultDirectory), "Emberfall");
            if (candidate.Equals(defaultDirectory, StringComparison.OrdinalIgnoreCase) || candidate.StartsWith(defaultDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Visual validation must never use the default save directory.");
            if (candidate.Equals(releaseDirectory, StringComparison.OrdinalIgnoreCase) || candidate.StartsWith(releaseDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Visual validation must never use the release player's save directory.");
            // Reject accidental reuse rather than replacing a previous test character.
            if (Directory.Exists(candidate) && (Directory.GetFiles(candidate, "emberfall-save*.json").Length > 0 || Directory.GetFiles(candidate, "emberfall-save*.json.bak").Length > 0))
                throw new InvalidOperationException("Choose a fresh visual-validation output directory; this one already contains a test save.");
            Directory.CreateDirectory(outputDirectory);
            Directory.CreateDirectory(candidate);
            saveDirectory = candidate;
        }

        private void Awake()
        {
            started = Time.realtimeSinceStartup;
            result = new Result
            {
                unityVersion = Application.unityVersion,
                outputDirectory = outputDirectory,
                isolatedSaveDirectory = SaveDirectory
            };
            Application.logMessageReceived += OnLog;
        }

        private void Start() { StartCoroutine(Guarded(Run())); }

        private void Update()
        {
            if (!finished && Time.realtimeSinceStartup - started > TimeoutSeconds)
                Finish("Visual validation exceeded its 180-second watchdog.");
        }

        private void LateUpdate()
        {
            Application.runInBackground = true;
            if (session == null || !session.HasStarted || finished) return;
            if (session.Player != null) session.Player.enabled = false;
            if (session.Player != null)
            {
                CombatModel model = session.Player.GetComponentInChildren<CombatModel>();
                if (movingPreview) session.Player.transform.position += Vector3.right * Time.deltaTime * 4f;
                if (model != null)
                {
                    model.SetLocomotion(session.Player.transform.InverseTransformDirection(movingPreview?Vector3.right*Time.deltaTime*4f:Vector3.zero),Time.deltaTime,4f,movingPreview);
                    model.Animate(movingPreview ? 1 : 0, 0, false);
                }
                var charge = session.Player.GetComponent<SkillChargeController>();
                if (charge != null && charge.IsCharging && model != null) model.AnimateCharge(charge.Progress);
            }
            foreach (EnemyController enemy in session.Enemies) if (enemy != null) enemy.enabled = false;
            if (hitPreview != null) hitPreview.GetComponentInChildren<CombatModel>().Animate(0,0,true);
            // Focus-loss pauses only this test player; keep the requested screenshot state stable.
            if (session.Paused != expectedPause) session.SetPaused(expectedPause);
        }

        private IEnumerator Guarded(IEnumerator first)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(first);
            while (stack.Count > 0 && !finished)
            {
                object yielded = null;
                bool hasNext = false;
                Exception failure = null;
                try
                {
                    hasNext = stack.Peek().MoveNext();
                    if (hasNext) yielded = stack.Peek().Current;
                }
                catch (Exception exception) { failure = exception; }
                if (failure != null) { Finish(failure.ToString()); yield break; }
                if (!hasNext) { stack.Pop(); continue; }
                IEnumerator nested = yielded as IEnumerator;
                if (nested != null) stack.Push(nested);
                else yield return yielded;
            }
            if (!finished) Finish(null);
        }

        private IEnumerator Run()
        {
            // The player loop can start before Unity's splash overlay is gone;
            // screenshots during that transition contain a black backbuffer.
            float splashDeadline = Time.realtimeSinceStartup + 12;
            while (!UnityEngine.Rendering.SplashScreen.isFinished && Time.realtimeSinceStartup < splashDeadline)
                yield return null;
            Check(UnityEngine.Rendering.SplashScreen.isFinished, "Startup splash has finished before framebuffer capture");
            yield return new WaitForSecondsRealtime(1f);
            float deadline = Time.realtimeSinceStartup + 20;
            while (GameSession.Instance == null && Time.realtimeSinceStartup < deadline) yield return null;
            session = GameSession.Instance;
            Check(session != null, "GameSession bootstrapped");
            ui = session.GetComponent<GameUI>();
            Check(ui != null, "Runtime IMGUI component exists");
            Check(Path.GetFullPath(session.Progression.SaveDirectory) == SaveDirectory, "Test player uses its isolated save directory");
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--windows-handoff")>=0){yield return VerifyWindowsHandoff();yield break;}
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--chapter-ui")>=0){yield return VerifyChapterUI();yield break;}
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--adventure-typography")>=0){yield return VerifyAdventureTypography();yield break;}
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--button-styles")>=0){yield return VerifyButtonStyles();yield break;}
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--user-fixes")>=0){yield return VerifyUserFixes();yield break;}
            int[] widths = { 1280, 1600, 1920, 1600 };
            int[] heights = { 720, 900, 1080, 900 };
            for (int hero = 0; hero < 4; hero++)
            {
                if (session.HasStarted) { ResetPanels(); session.QuitToTitle(); }
                SetField("selectedClass", (HeroClass)hero);
                SetField("saveSlotsDirty", true);
                yield return SetResolution(widths[hero], heights[hero]);
                string label = ((HeroClass)hero).ToString().ToLowerInvariant();
                yield return Capture(label + "-title");
                Invoke("StartSelectedHero");
                Check(session.HasStarted && session.Player.HeroClass == (HeroClass)hero, "Selected hero starts: " + label);
                session.Player.enabled = false;
                foreach (EnemyController enemy in session.Enemies) if (enemy != null) enemy.enabled = false;
                yield return Capture(label + "-hud-initial");
                int experience = 0;
                for (int level = 1; level < 50; level++) experience += GameBalance.XpToNext(level);
                session.Progression.GrantExperience(experience);
                for (int skill = 0; skill < GameBalance.SkillCount; skill++)
                    while(session.Progression.Profile.skillRanks[skill]<3) Check(session.Progression.LearnSkill(skill), "Learn skill " + skill + " next rank");
                for (int item = 0; item < 10; item++) session.Progression.CreateLoot(45 + item % 6, item % 3 == 0);
                session.Progression.Upgrade(session.Progression.Profile.weaponId);
                // Let actual level-up floating text expire, while input and enemy AI remain disabled.
                yield return new WaitForSecondsRealtime(1.7f);
                yield return Capture(label + "-hud-learned");
                if (hero == 0)
                {
                    session.Progression.Profile.potions = 7;
                    Check(session.Progression.AssignConsumable(9), "Configure visible potion supply on the desktop hotbar");
                    yield return Capture("hud-potion-hotbar");
                    AdventureCamera camera = Camera.main.GetComponent<AdventureCamera>();
                    Check(camera != null, "Orbit camera is available for rotated low-angle capture");
                    FieldInfo yaw = typeof(AdventureCamera).GetField("yaw", PrivateInstance);
                    FieldInfo pitch = typeof(AdventureCamera).GetField("pitch", PrivateInstance);
                    FieldInfo distance = typeof(AdventureCamera).GetField("distance", PrivateInstance);
                    object priorYaw = yaw.GetValue(camera), priorPitch = pitch.GetValue(camera), priorDistance = distance.GetValue(camera);
                    yaw.SetValue(camera, 58f); pitch.SetValue(camera, 27f); distance.SetValue(camera, 14f);
                    camera.Snap();
                    yield return Capture("camera-orbit-low-angle");
                    pitch.SetValue(camera, AdventureCamera.MinimumPitch);
                    camera.Snap();
                    Check(camera.transform.forward.y > 0f, "Lowest orbit angle looks upward");
                    yield return Capture("camera-look-up");
                    yaw.SetValue(camera, priorYaw); pitch.SetValue(camera, priorPitch); distance.SetValue(camera, priorDistance);
                    camera.Snap();
                }
                SkillTargetingController placement = session.Player.GetComponent<SkillTargetingController>();
                int placementSkill = (HeroClass)hero == HeroClass.Vanguard ? 9 : 1;
                Check(SkillTargetingController.RequiresConfirmation((HeroClass)hero, placementSkill) && placement.Begin(placementSkill) && placement.IsTargeting,
                    "A learned ground skill enters placement preview: " + label);
                placement.SetTarget(session.Player.transform.position + new Vector3(3, 0, 6));
                yield return Capture(label + "-skill-placement");
                placement.Cancel();
                typeof(PlayerController).GetMethod("CastSkill", PrivateInstance).Invoke(session.Player, new object[] { 9 });
                yield return Capture(label + "-ultimate-windup", .08f);
                yield return Capture(label + "-ultimate-release", .25f);
                yield return Capture(label + "-ultimate-impact", .55f);
                yield return new WaitForSecondsRealtime(5f);
                OpenPanel("Skills");
                SetField("selectedSkill", 9);
                SetField("skillScroll", new Vector2(0, 1000));
                yield return Capture(label + "-skills-awakened");
                SetField("selectedSkill", 3);
                SetField("skillScroll", Vector2.zero);
                yield return Capture(label + "-skills-passive");
                ResetPanels();
                OpenPanel("Inventory");
                yield return Capture(label + "-inventory");
                if (hero == 0)
                {
                    session.Progression.Profile.fashions.Add(new FashionData {
                        id = "fashion-0-3", slot = FashionSlot.Wings, rarity = Rarity.Legendary, name = "烬王之翼"
                    });
                    session.Progression.Profile.fashions.Add(new FashionData {
                        id = "fashion-1-2", slot = FashionSlot.Weapon, rarity = Rarity.Epic, name = "苍穹兵装"
                    });
                    Check(session.Progression.EquipFashion("fashion-0-3") && session.Progression.EquipFashion("fashion-1-2"),
                        "Both fashion slots equip without a level gate");
                    ResetPanels();
                    yield return Capture("vanguard-fashion-world");
                    OpenPanel("Fashion");
                    yield return Capture("vanguard-fashion-collection");
                    ResetPanels();
                    OpenPanel("Inventory");
                }
                ItemData previousWeapon = session.Progression.Equipped(ItemSlot.Weapon);
                var replacementWeapon = new ItemData {
                    id = "visual-slot-target-" + hero, name = "部位强化试炼武器",
                    slot = ItemSlot.Weapon, rarity = Rarity.Epic, level = 10, attack = 40
                };
                session.Progression.Profile.inventory.Add(replacementWeapon);
                session.Progression.Profile.gold = 100000;
                while (session.Progression.SlotUpgradeRank(ItemSlot.Weapon) < 3)
                    Check(session.Progression.Upgrade(previousWeapon.id), "Prepare permanent weapon-slot reinforcement");
                Check(session.Progression.Upgrade(replacementWeapon.id) && session.Progression.SlotUpgradeRank(ItemSlot.Weapon) == 4 &&
                    previousWeapon.upgradeLevel == 4 && replacementWeapon.upgradeLevel == 0,
                    "Reinforcing a bag selection trains the slot and updates the currently worn weapon");
                SetField("inventoryFilter", 0);
                SetField("inventorySort", 0);
                SetField("selectedItem", replacementWeapon.id);
                Invoke("RebuildBagItems");
                string candidateBefore = JsonUtility.ToJson(replacementWeapon);
                ItemData replacementPreview = (ItemData)typeof(GameUI).GetMethod("EquipmentPreview", PrivateInstance).Invoke(ui, new object[] { replacementWeapon });
                Check(replacementPreview.upgradeLevel == 4 && replacementPreview.attack > replacementWeapon.attack &&
                    JsonUtility.ToJson(replacementWeapon) == candidateBefore,
                    "Inventory comparison previews automatic slot inheritance without changing the bag item");
                Check(GetField("panel").ToString() == "Inventory", "Automatic inheritance preview stays in the inventory");
                yield return Capture(label + "-slot-reinforcement-preview");
                Check(JsonUtility.ToJson(replacementWeapon) == candidateBefore, "Rendering the slot-enhanced preview leaves candidate attributes unchanged");
                int goldBeforeEquip = session.Progression.Profile.gold;
                int inventoryCountBeforeEquip = session.Progression.Profile.inventory.Count;
                Check(session.Progression.Equip(replacementWeapon.id) && replacementWeapon.upgradeLevel == 4 && previousWeapon.upgradeLevel == 0 &&
                    replacementWeapon.attack == replacementPreview.attack && replacementWeapon.defense == replacementPreview.defense && replacementWeapon.health == replacementPreview.health,
                    "Equipping automatically matches the slot-enhanced preview and restores outgoing gear to its baseline");
                Check(session.Progression.Equip(previousWeapon.id) && previousWeapon.upgradeLevel == 4 && replacementWeapon.upgradeLevel == 0 &&
                    previousWeapon.attack != replacementPreview.attack,
                    "Swapping back retains the same rank while each weapon uses its own baseline attributes");
                Check(session.Progression.Equip(replacementWeapon.id) && session.Progression.Equip(replacementWeapon.id) &&
                    replacementWeapon.attack == replacementPreview.attack && session.Progression.SlotUpgradeRank(ItemSlot.Weapon) == 4 &&
                    session.Progression.Profile.gold == goldBeforeEquip && session.Progression.Profile.inventory.Count == inventoryCountBeforeEquip,
                    "Repeated automatic inheritance is free, preserves items and cannot compound reinforcement");
                Check(GetField("panel").ToString() == "Inventory" && (string)GetField("selectedItem") == replacementWeapon.id,
                    "Automatic inheritance keeps the selected item visible in the inventory");
                yield return Capture(label + "-slot-reinforcement-result");
                ResetPanels();
                OpenPanel("Skills");
                Invoke("OpenBindings");
                SetField("rebindingSlot", -1);
                yield return Capture(label + "-key-bindings");
                SetField("rebindingSlot", 0);
                yield return Capture(label + "-key-binding-prompt");
                ResetPanels();
                OpenPanel("Controls");
                yield return Capture(label + "-controls-keyboard");
                ResetPanels();
                expectedPause = true;
                session.SetPaused(true);
                yield return Capture(label + "-pause");
                ResetPanels();
                OpenPanel("SaveLocation");
                yield return Capture(label + "-save-location");
                ResetPanels();
                session.Player.Teleport(new Vector3(0,0,11));
                session.EnterDungeon();
                Check(session.DungeonSelectionOpen && !session.InDungeon && session.InputBlocked,
                    "Portal displays a blocking dungeon selector before entry");
                yield return Capture(label + "-dungeon-selection");
                session.ConfirmDungeonSelection();
                Check(session.InDungeon && !session.DungeonSelectionOpen && session.DungeonWave == 1,
                    "Confirming dungeon selection enters the first wave");
                session.Player.enabled = false;
                foreach(var enemy in session.Enemies) if(enemy!=null) enemy.enabled=false;
                for(int i=0;i<3;i++) session.SpawnGroundLoot(new ItemData {
                    id="visual-ground-"+hero+"-"+i, name=new[]{"星辉法器","古树护甲","星辰遗物"}[i],
                    level=50, rarity=(Rarity)(i+1), slot=(ItemSlot)i, attack=60
                }, new Vector3(-3+i*3,0,1));
                yield return Capture(label + "-dungeon-ground-loot");
                if (hero == 0)
                {
                    session.Progression.PrepareDungeonChest();
                    yield return null;
                    Check(GetField("panel").ToString() == "Chests", "A pending clear opens the three-chest choice panel");
                    yield return Capture("dungeon-chest-choice");
                    Check(session.Progression.OpenDungeonChest(1) != null && !session.Progression.Profile.pendingFashionChest && session.Progression.Profile.pendingChestReveal,
                        "Opening one visual-validation chest saves its receipt before presentation");
                    string receiptId = session.Progression.LastChestReward.Id;
                    int receiptGold = session.Progression.Profile.gold;
                    var savedReward = new ProgressionService(session.Progression.SaveDirectory);
                    Check(savedReward.Load() && savedReward.Profile.pendingChestReveal && savedReward.LastChestReward.Id == receiptId && savedReward.Profile.gold == receiptGold,
                        "Reload preserves the same pending reward without granting it again");
                    Invoke("ResetChestReveal");
                    yield return Capture("dungeon-chest-receipt-restored");
                    ResetPanels();
                    Check(!session.Progression.Profile.pendingChestReveal && GetField("panel").ToString() == "None" && session.Progression.Profile.gold == receiptGold,
                        "Closing a restored receipt acknowledges it without regranting currency");
                }
                if (hero == 1 && session.Enemies.Count > 1)
                {
                    EnemyController visualEnemy = session.Enemies[0];
                    visualEnemy.transform.position = session.Player.transform.position + Vector3.forward * 3f;
                    Vector3 effectAt = visualEnemy.transform.position;
                    CombatArea.Spawn(session.Player, session, effectAt, 2f, .1f, 0f, 0f, 1.1f, .35f,
                        new Color(1f, .5f, .2f));
                    yield return Capture("elemental-fire-on-enemy", .35f);
                    yield return Capture("elemental-burning-body", 1.1f);
                    yield return new WaitForSecondsRealtime(2.4f);
                    CombatArea.Spawn(session.Player, session, effectAt, 2f, .1f, 0f, 0f, 1.1f, .3f,
                        new Color(.65f, .5f, 1f));
                    yield return Capture("elemental-lightning", .2f);
                    yield return new WaitForSecondsRealtime(1.2f);
                    CombatArea.Spawn(session.Player, session, effectAt, 2f, .1f, 0f, 0f, 1.1f, .35f,
                        new Color(.7f, 1f, .59f));
                    visualEnemy.StatusEffects.Poison(session.Player, 2f, .1f);
                    yield return Capture("elemental-poison-bubbles", .35f);
                    yield return new WaitForSecondsRealtime(1.15f);
                    visualEnemy.TakeDamage(visualEnemy.Health + 1f, Vector3.forward);
                    Check(!session.Enemies.Contains(visualEnemy) && visualEnemy != null,
                        "Enemy leaves combat immediately but its body remains visible");
                    yield return Capture("enemy-death-fall", .3f);
                    yield return Capture("enemy-death-rest", 1.1f);
                    yield return Capture("enemy-death-dissolve", 1.35f);
                    yield return new WaitForSecondsRealtime(.55f);
                    Check(visualEnemy == null, "Enemy body evaporates after the death animation");
                }
                if(hero==3)
                {
                    SummonedCompanion.Summon(session.Player,session,SummonedCompanion.Kind.Wolf,3,session.Player.transform.position+new Vector3(-2,0,2),50);
                    SummonedCompanion.Summon(session.Player,session,SummonedCompanion.Kind.Spirit,3,session.Player.transform.position+new Vector3(2,0,2),50);
                    SummonedCompanion.Summon(session.Player,session,SummonedCompanion.Kind.Treant,3,session.Player.transform.position+new Vector3(0,0,4),50);
                    yield return Capture("summoner-companions",.12f);
                    movingPreview=true;
                    for(int frame=0;frame<3;frame++) yield return Capture("dungeon-moving-"+frame,.12f);
                    movingPreview=false;
                    hitPreview=session.Enemies[0];
                    hitPreview.transform.position=session.Player.transform.position+Vector3.forward*3;
                    hitPreview.TakeDamage(10,Vector3.forward,.1f);
                    yield return Capture("enemy-impact",.035f);
                    hitPreview=null;
                    SkillRuntime runtime=(SkillRuntime)typeof(PlayerController).GetField("skillRuntime",PrivateInstance).GetValue(session.Player);
                    runtime.Advance(200); runtime.FillEnergy();
                    Check(session.Player.GetComponent<SkillTargetingController>().Begin(4),"Summoner starts visible charge");
                    yield return Capture("summoner-charging",.12f);
                    session.Player.GetComponent<SkillChargeController>().Cancel();
                    MobileControls.SimulationEnabled=true;
                    Check(session.Progression.AssignConsumable(9), "Mobile hotbar also displays an assigned potion");
                    yield return SetResolution(1920,900);
                    yield return Capture("mobile-landscape-hud");
                    OpenPanel("Skills");
                    yield return Capture("mobile-landscape-skills");
                    ResetPanels();
                    MobileControls.SimulationEnabled=false;
                    MobileControls.ResetInput();
                }
                session.ReturnToCamp();
            }
            ResetPanels();
            Check(session.SaveAsNewSlot(), "Pause save-as creates an independent copy before save selection");
            session.QuitToTitle();
            Invoke("OpenSaveSelection");
            Check(GetField("panel").ToString() == "SaveSelection" && session.Progression.GetSaveSlots().Count >= 5, "Title offers multiple independent character saves");
            yield return Capture("save-selection");
            // These files live exclusively in the validation player's isolated directory.
            Check(Path.GetFullPath(session.Progression.SaveDirectory) == SaveDirectory, "Corrupt-save visual fixtures remain isolated");
            Check(session.Progression.SaveAsNewSlot(), "Create an isolated backup-recovery fixture");
            File.WriteAllText(session.Progression.SaveFilePath, "{ invalid visual validation primary }");
            Check(session.Progression.SaveAsNewSlot(), "Create an isolated unreadable-save fixture");
            File.WriteAllText(session.Progression.SaveFilePath, "{ invalid visual validation primary }");
            File.WriteAllText(session.Progression.SaveFilePath + ".bak", "{ invalid visual validation backup }");
            Invoke("OpenSaveSelection");
            List<SaveSlotInfo> saveChoices = session.Progression.GetSaveSlots();
            Check(saveChoices.Exists(slot => slot.CanLoad && slot.RecoveredFromBackup) && saveChoices.Exists(slot => !slot.CanLoad), "Save selection displays recoverable and unreadable slots distinctly");
            yield return Capture("save-selection-recovery");
            Check(result.screenshots.Count >= 88, "Full-frame captures include four heroes, fashion, chest choice, encounters, mobile, saves and camera orbit");
        }

        private IEnumerator VerifyChapterUI()
        {
            SetField("selectedClass",HeroClass.Arcanist);
            Invoke("StartSelectedHero");
            session.Player.enabled=false;
            foreach(var enemy in session.Enemies)if(enemy!=null)enemy.enabled=false;
            MobileControls.SimulationEnabled=false;
            yield return SetResolution(1280,720);
            Vector3 npc=GameSession.HubNpcPosition(2);
            session.Player.Teleport(npc+Vector3.back*2.5f);
            Check(session.NearbyHubNpc==HubNpcKind.Exchange,"Exchange prompt appears within ground interaction radius");
            session.Player.transform.position=npc+new Vector3(0,1.65f,-2.5f);
            Check(session.NearbyHubNpc==HubNpcKind.Exchange,"Jump height cannot hide exchange prompt");
            session.Player.transform.position=npc+Vector3.back*2.9f;
            Check(session.NearbyHubNpc==HubNpcKind.Exchange,"Prompt stays visible in exit buffer");
            session.Player.transform.position=npc+Vector3.back*3.2f;
            Check(session.NearbyHubNpc==HubNpcKind.None,"Leaving exit buffer clears prompt");
            session.Player.transform.position=npc+Vector3.back*2.8f;
            Check(session.NearbyHubNpc==HubNpcKind.None,"Outside entry radius cannot acquire NPC");
            session.Player.transform.position=npc+Vector3.back*2.5f;
            yield return Capture("stargazer-hud");
            var rects=(List<Rect>)GetField("blockedRects");
            float hudWidth=(float)GetField("width"),hudHeight=(float)GetField("height");
            Check(rects.Exists(r=>r.Contains(new Vector2(hudWidth-120,hudHeight-133))),"Exchange button blocks combat pointer input");
            Invoke("OpenNearbyHubNpc");
            Check(GetField("panel").ToString()=="HubDialogue","Exchange first greets the player with a visible world dialogue");
            yield return Capture("stargazer-dialogue");
            Invoke("OpenHubNpcService");
            Check(GetField("panel").ToString()=="Chapter","Exchange opens chapter selection");
            yield return Capture("chapter-desktop-first");
            Invoke("SetChapterLimitedHealing",true);
            yield return Capture("chapter-desktop-limited");
            Invoke("SetChapterLimitedHealing",false);
            Invoke("OpenChapterExchange");
            Check(GetField("panel").ToString()=="Camp","Mechanism exchange remains reachable");
            ResetPanels();
            var profile=session.Progression.Profile;
            profile.chapterCompletedMask=3;
            profile.chapterHighestDifficulties=new[]{2,1,0};
            profile.highestAdventureTier=12;
            session.SelectedChapterNode=ChapterNode.Redrock;
            Invoke("OpenChapterSelection");
            Invoke("SelectChapterDifficulty",ChapterDifficulty.Hard);
            yield return SetResolution(1600,900);
            yield return Capture("chapter-desktop-hard");
            SetField("chapterRulesExpanded",true);
            yield return Capture("chapter-desktop-rules");
            SetField("chapterRulesExpanded",false);
            MobileControls.SimulationEnabled=true;
            yield return SetResolution(1280,720);
            yield return Capture("chapter-mobile");
            MobileControls.SimulationEnabled=false;
            ResetPanels();
        }

        private IEnumerator VerifyAdventureTypography()
        {
            yield return SetResolution(1920,1080);
            SetField("selectedClass",HeroClass.Arcanist);Invoke("StartSelectedHero");
            session.Player.enabled=false;
            session.Progression.GrantExperience(40000);
            session.Progression.Profile.highestAdventureTier=14;
            var goalWeapon=session.Progression.Profile.inventory.Find(item=>item.id==session.Progression.Profile.weaponId);
            goalWeapon.mechanic=EquipmentMechanic.CinderTrail;goalWeapon.name="余烬法杖";
            session.Progression.Profile.mechanicMaterials=2;
            Check(session.Progression.SelectProgressionGoal(ProgressionGoalKind.Variant,goalWeapon.id),"Select equipment goal for wrapped description");
            session.Player.Teleport(new Vector3(0,0,11));session.EnterDungeon();
            Check(session.DungeonSelectionOpen,"Adventure selection is visible");
            session.SelectedDungeonTier=14;session.SelectedChallengeMode=true;
            yield return Capture("adventure-desktop");
            yield return SetResolution(1280,720);
            session.SelectedArenaMode=2;
            yield return Capture("adventure-desktop-compact");
            MobileControls.SimulationEnabled=true;
            yield return Capture("adventure-touch");
            yield return SetResolution(568,320);
            var controls=MobileControls.Layout;
            var layout=new AdventureSelectionLayout(controls.Width,controls.Height);
            Check(layout.FooterY+48<=controls.Height,"Touch footer stays within safe canvas");
            Check(layout.EntryEncounter(4).YMax<=layout.Entry(4).YMax-3.9f,"Last encounter line clears the card border");
            yield return Capture("adventure-touch-small");
            MobileControls.SimulationEnabled=false;session.CancelDungeonSelection();
        }

        private IEnumerator VerifyButtonStyles()
        {
            yield return SetResolution(1920,1080);
            SetField("selectedClass", HeroClass.Arcanist);
            Invoke("StartSelectedHero");
            session.Player.enabled=false;
            foreach(var enemy in session.Enemies)if(enemy!=null)enemy.enabled=false;
            session.Progression.GrantExperience(40000);
            var profile=session.Progression.Profile;
            profile.gold=100000;
            for(int i=0;i<6;i++)session.Progression.CreateLoot(profile.level,true);
            session.ReturnToCamp();session.Player.enabled=false;
            OpenPanel("Camp");
            SetField("campTab",0);
            yield return Capture("buttons-workshop-actions");
            profile.skillPoints=0;
            yield return Capture("buttons-workshop-disabled");
            for(int tab=1;tab<4;tab++)
            {
                SetField("campTab",tab);
                yield return Capture("buttons-workshop-tab-"+tab);
            }
            ResetPanels();OpenPanel("Inventory");
            yield return Capture("buttons-inventory");
            ResetPanels();expectedPause=true;session.SetPaused(true);
            yield return Capture("buttons-pause");
            ResetPanels();
            MobileControls.SimulationEnabled=true;
            yield return SetResolution(1280,720);
            OpenPanel("Camp");SetField("campTab",0);
            yield return Capture("buttons-mobile-workshop");
            SetField("campTab",2);
            yield return Capture("buttons-mobile-toggles");
            ResetPanels();expectedPause=true;session.SetPaused(true);
            yield return Capture("buttons-mobile-pause");
            ResetPanels();MobileControls.SimulationEnabled=false;
        }

        private IEnumerator VerifyUserFixes()
        {
            yield return SetResolution(1280,720);Invoke("StartSelectedHero");session.Player.enabled=false;
            session.Progression.GrantExperience(40000);
            foreach(var enemy in session.Enemies)if(enemy!=null)enemy.enabled=false;
            session.Progression.Profile.gold=100000;
            for(int i=0;i<6;i++)session.Progression.CollectLoot(session.Progression.RollLoot(50,true,1));
            for(int npc=0;npc<2;npc++)
            {
                ResetPanels();session.Player.Teleport(GameSession.HubNpcPosition(npc));yield return new WaitForSecondsRealtime(.5f);
                Invoke("OpenNearbyHubNpc");yield return new WaitForSecondsRealtime(.5f);Invoke("OpenHubNpcService");
                Check(GetField("panel").ToString()=="Inventory","NPC opens independent service page");
                Check(session.ActiveHubNpc==(npc==0?HubNpcKind.Merchant:HubNpcKind.Blacksmith),"Distinct NPC service context");
                yield return Capture(npc==0?"fix-merchant-service":"fix-blacksmith-service",.6f);
            }
            ResetPanels();
            var player=session.Player;Vector3 bank=new Vector3(7,0,-4.5f);player.Teleport(bank);
            var hidden=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(PlayerController).GetField("traversalFrame",hidden).SetValue(player,-1);
            typeof(PlayerController).GetField("jumpInput",hidden).SetValue(player,Vector3.forward);
            Check((bool)typeof(PlayerController).GetMethod("TryJump",hidden).Invoke(player,null),"Moving jump begins");
            typeof(PlayerController).GetMethod("AdvanceJump",hidden).Invoke(player,new object[]{.55f});
            Check(player.transform.position.z>0&&WorldTraversal.IsWalkable(player.transform.position),"Moving jump lands across river");
            typeof(PlayerController).GetField("jumpInput",hidden).SetValue(player,Vector3.zero);
            for(int i=0;i<12;i++)session.Progression.CollectLoot(session.Progression.RollLoot(50,true,1));
            OpenPanel("Inventory");yield return Capture("fix-inventory-badges");ResetPanels();
            player.Teleport(new Vector3(0,0,11));session.EnterDungeon();
            Check(session.SelectedDungeonTier==session.MaximumDungeonTier,"Selection defaults to highest tier");
            yield return Capture("fix-dungeon-selection");session.SelectedDungeonTier=1;session.ConfirmDungeonSelection();
            session.Player.enabled=false;foreach(var enemy in session.Enemies)if(enemy!=null)enemy.enabled=false;
            var camera=Camera.main.GetComponent<AdventureCamera>();
            foreach(var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {var bounds=renderer.bounds;if(bounds.size.y>10||bounds.size.x>45||bounds.size.z>45)Check(false,"Oversized scene geometry: "+renderer.name+" "+bounds);}
            foreach(float angle in new[]{48f,15f,-18f})
            {typeof(AdventureCamera).GetField("pitch",hidden).SetValue(camera,angle);camera.Snap();yield return Capture("fix-sanctum-angle-"+angle);}
            session.Progression.PrepareDungeonChest();yield return null;
            Check(session.Progression.OpenDungeonChest()!=null,"Chest commits receipt");Invoke("ResetChestReveal");yield return Capture("fix-chest-sections",2f);
        }

        private IEnumerator SetResolution(int width, int height)
        {
            requestedWidth = width;
            requestedHeight = height;
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(.7f);
            float deadline = Time.realtimeSinceStartup + 8;
            while ((Screen.width != width || Screen.height != height) && Time.realtimeSinceStartup < deadline) yield return null;
            yield return new WaitForEndOfFrame();
            Check(Screen.width == width && Screen.height == height, "Requested resolution available: " + width + "x" + height + "; actual=" + Screen.width + "x" + Screen.height);
        }

        private IEnumerator Capture(string name, float settle = .2f)
        {
            yield return new WaitForSecondsRealtime(settle);
            yield return new WaitForEndOfFrame();
            Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                Check(image != null && image.width == Screen.width && image.height == Screen.height, "Full player framebuffer captured: " + name);
                Color32[] pixels = image.GetPixels32();
                var colors = new HashSet<uint>();
                int stride = Math.Max(1, pixels.Length / 4096);
                for (int i = 0; i < pixels.Length; i += stride)
                {
                    Color32 color = pixels[i];
                    colors.Add(((uint)color.r << 16) | ((uint)color.g << 8) | color.b);
                }
                byte[] png = image.EncodeToPNG();
                string file = name + "-" + image.width + "x" + image.height + ".png";
                File.WriteAllBytes(Path.Combine(outputDirectory, file), png);
                Check(colors.Count >= 24, "Screenshot contains rendered content rather than a blank frame: " + name + "; sampled colors=" + colors.Count + "; diagnostic PNG=" + file);
                Check(png != null && png.Length > 8192, "Nonempty screenshot PNG encoded: " + name);
                result.screenshots.Add(new Screenshot
                {
                    name = name, file = file,
                    requestedWidth = requestedWidth, requestedHeight = requestedHeight,
                    actualWidth = image.width, actualHeight = image.height,
                    bytes = png.Length, distinctSampledColors = colors.Count
                });
                WriteReport();
            }
            finally { if (image != null) Destroy(image); }
        }

        private void OpenPanel(string name)
        {
            Type type = typeof(GameUI).GetNestedType("Panel", BindingFlags.NonPublic);
            if (type == null) throw new MissingMemberException("GameUI.Panel");
            Invoke("TogglePanel", Enum.Parse(type, name));
            Check(GetField("panel").ToString() == name, "UI panel opened: " + name);
        }

        private void ResetPanels()
        {
            SetField("rebindingSlot", -1);
            for (int i = 0; i < 4 && GetField("panel").ToString() != "None"; i++) Invoke("ClosePanel");
            expectedPause = false;
            session.SetPaused(false);
            session.SetUIBlocking(false);
        }

        private object GetField(string name)
        {
            FieldInfo field = typeof(GameUI).GetField(name, PrivateInstance);
            if (field == null) throw new MissingFieldException(typeof(GameUI).Name, name);
            return field.GetValue(ui);
        }
        private void SetField(string name, object value)
        {
            FieldInfo field = typeof(GameUI).GetField(name, PrivateInstance);
            if (field == null) throw new MissingFieldException(typeof(GameUI).Name, name);
            field.SetValue(ui, value);
        }
        private void Invoke(string name, params object[] arguments)
        {
            MethodInfo method = typeof(GameUI).GetMethod(name, PrivateInstance);
            if (method == null) throw new MissingMethodException(typeof(GameUI).Name, name);
            method.Invoke(ui, arguments);
        }
        private void Check(bool condition, string message)
        {
            result.assertions++;
            if (!condition) throw new InvalidOperationException(message);
        }
        private void OnLog(string message, string stack, LogType type)
        {
            if (finished || (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
            result.consoleErrors++;
            if (result.errors.Count < 32) result.errors.Add(message + "\n" + stack);
        }
        private void WriteReport()
        {
            result.elapsedSeconds = Time.realtimeSinceStartup - started;
            File.WriteAllText(Path.Combine(outputDirectory, "visual-validation-report.json"), JsonUtility.ToJson(result, true), new UTF8Encoding(false));
        }
        private void Finish(string failure)
        {
            if (finished) return;
            finished = true;
            result.failure = failure ?? (result.consoleErrors > 0 ? "Player logged " + result.consoleErrors + " error(s); inspect errors in report." : "");
            result.status = string.IsNullOrEmpty(result.failure) ? "PASS" : "FAIL";
            int exitCode = result.status == "PASS" ? 0 : 1;
            try { WriteReport(); }
            catch (Exception exception) { Debug.LogError("Cannot write visual validation report: " + exception); exitCode = 1; }
            Debug.Log("Visual validation " + result.status + ": " + result.screenshots.Count + " actual player screenshots. Output: " + outputDirectory);
            Application.Quit(exitCode);
        }
        private void OnDestroy() { Application.logMessageReceived -= OnLog; }
    }
}
#endif
