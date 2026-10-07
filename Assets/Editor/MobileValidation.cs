using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Emberfall.Editor
{
    public static class MobileValidation
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        public static IEnumerator Validate(GameSession game, Action<bool, string> check, Action<string> log)
        {
            // Reuse the exact save-isolation boundary used by the inventory fixture.
            typeof(InventoryUIValidation).GetMethod("RequireIsolatedRuntime", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { game });
            MobileControls controls = game.GetComponent<MobileControls>();
            check(controls != null, "Mobile controls are installed by runtime bootstrap");
            GameUI ui = game.GetComponent<GameUI>();
            GameProfile originalProfile = game.Progression.Profile;
            bool originalSimulation = MobileControls.SimulationEnabled;
            bool originalEnabled = game.Player.enabled;
            Vector3 originalPosition = game.Player.transform.position;
            Quaternion originalRotation = game.Player.transform.rotation;
            SkillRuntime originalRuntime = (SkillRuntime)Field(typeof(PlayerController), "skillRuntime").GetValue(game.Player);
            var fixtureRuntime = new SkillRuntime(game.Player.HeroClass);
            float originalRespawn = (float)Field(typeof(GameSession), "respawnTimer").GetValue(game);
            EnemyController[] originalEnemies = game.Enemies.ToArray();
            bool[] activeEnemies = Array.ConvertAll(originalEnemies, enemy => enemy != null && enemy.gameObject.activeSelf);
            EnemyController fixtureEnemy = null;
            SkillTargetingController targeting = game.Player.GetComponent<SkillTargetingController>();
            SkillChargeController charge = game.Player.GetComponent<SkillChargeController>();
            string originalError = game.Progression.LastError;
            object notification = Field(typeof(GameSession), "notification").GetValue(game);
            object notificationUntil = Field(typeof(GameSession), "notificationUntil").GetValue(game);
            string save = game.Progression.SaveFilePath;
            string[] paths = { save, save + ".bak", save + ".tmp" };
            byte[][] bytes = new byte[paths.Length][];
            for (int i = 0; i < paths.Length; i++) bytes[i] = File.Exists(paths[i]) ? File.ReadAllBytes(paths[i]) : null;
            try
            {
                log?.Invoke("MOBILE INPUT — multi-touch, ten direct slots, one-tap autoaim, safe cancellation and safe-area bounds");
                MobileControls.SimulationEnabled = true;
                MobileControls.ResetInput();
                Field(typeof(PlayerController), "skillRuntime").SetValue(game.Player, fixtureRuntime);
                Field(typeof(GameSession), "respawnTimer").SetValue(game, 600f);
                game.Player.enabled = false;
                game.SetPaused(false);
                game.SetUIBlocking(false);
                foreach (object tick in Wait(.12f)) yield return tick;
                Vector2 move = controls.ControlScreenPoint("move");
                Vector2 attack = controls.ControlScreenPoint("attack");
                Vector2 dodge = controls.ControlScreenPoint("dodge");
                Vector2 potion = controls.ControlScreenPoint("potion");
                Rect safe = MobileControls.SafeArea;
                check(safe.Contains(move) && safe.Contains(attack) && safe.Contains(dodge) && safe.Contains(potion), "Touch control centers stay inside the screen safe area");
                check(controls.ProcessPointer(11, TouchPhase.Began, move), "Left joystick captures its own finger");
                controls.ProcessPointer(11, TouchPhase.Moved, move + Vector2.right * 200);
                controls.ProcessPointer(12, TouchPhase.Began, attack);
                check(MobileControls.Move.x > .9f && MobileControls.AttackHeld, "Two fingers can move and hold attack simultaneously");
                controls.ProcessPointer(12, TouchPhase.Ended, attack);
                check(!MobileControls.AttackHeld && MobileControls.Move.x > .9f, "Releasing attack preserves the independent movement finger");
                controls.ProcessPointer(11, TouchPhase.Canceled, move);
                check(MobileControls.Move == Vector2.zero, "Cancelled movement clears the joystick without a stuck direction");
                controls.ProcessPointer(21, TouchPhase.Began, dodge);
                check(MobileControls.ConsumeDodge() && !MobileControls.ConsumeDodge(), "One dodge touch is consumed exactly once");
                controls.ProcessPointer(21, TouchPhase.Ended, dodge);
                controls.ProcessPointer(22, TouchPhase.Began, potion);
                check(MobileControls.ConsumePotion() && !MobileControls.ConsumePotion(), "One potion touch is consumed exactly once");
                controls.ProcessPointer(22, TouchPhase.Ended, potion);
                controls.ProcessPointer(23, TouchPhase.Began, attack);
                controls.ProcessPointer(24, TouchPhase.Began, dodge);
                game.SetPaused(true);
                Call(controls, "Update");
                check(!MobileControls.AttackHeld && MobileControls.Move == Vector2.zero && !MobileControls.ConsumeDodge(), "Blocking gameplay clears held and queued touch commands");
                game.SetPaused(false);
                controls.ProcessPointer(23, TouchPhase.Stationary, attack);
                check(!MobileControls.AttackHeld, "A stale pre-pause finger cannot restart attack after resume");

                Vector3 beforeMove = game.Player.transform.position;
                controls.ProcessPointer(31, TouchPhase.Began, move);
                controls.ProcessPointer(31, TouchPhase.Moved, move + Vector2.right * 200);
                game.Player.enabled = true;
                foreach (object tick in Wait(.16f)) yield return tick;
                game.Player.enabled = false;
                controls.ProcessPointer(31, TouchPhase.Ended, move);
                check(game.Player.transform.position.x > beforeMove.x + .15f, "PlayerController actually moves from the virtual joystick");

                GameProfile testProfile = JsonUtility.FromJson<GameProfile>(JsonUtility.ToJson(originalProfile));
                testProfile.level = 100;
                testProfile.hotbarPage = 2;
                for (int i = 0; i < testProfile.equippedSkills.Length; i++) testProfile.equippedSkills[i] = -1;
                Array.Clear(testProfile.skillRanks, 0, testProfile.skillRanks.Length);
                testProfile.skillRanks[0] = testProfile.skillRanks[1] = testProfile.skillRanks[3] = testProfile.skillRanks[8] = testProfile.skillRanks[9] = 1;
                // Deliberately incompatible desktop assignments prove that the
                // mobile buttons address skill identities, not loadout slots/pages.
                testProfile.equippedSkills[0] = 1;
                testProfile.equippedSkills[1] = 0;
                Set(game.Progression, "Profile", testProfile);
                game.Player.RefreshStats(false);
                // Batch mode need not repaint Game View. Use the same layout
                // method as runtime Update, touch input and OnGUI, not test rectangles.
                Call(ui, "RefreshLayout");
                Rect[] slots = (Rect[])Field(typeof(GameUI), "hotbarSlots").GetValue(ui);
                float scale = (float)Field(typeof(GameUI), "scale").GetValue(ui);
                Vector2 offset = (Vector2)Field(typeof(GameUI), "guiOffset").GetValue(ui);
                Func<Vector2, Vector2> screen = point => new Vector2(point.x * scale + offset.x, Screen.height - point.y * scale - offset.y);
                float ratio=MobileControls.Layout.Scale/scale;
                check(slots.Length == 10 && Mathf.Abs(slots[0].width-48*ratio)<.1f && slots[9].width>0, "Mobile HUD exposes all ten skills with 48-unit targets");
                for (int i = 0; i < 10; i++)
                {
                    check(safe.Contains(screen(slots[i].min)) && safe.Contains(screen(slots[i].max-Vector2.one*.01f)), "Skill target stays in safe area");
                    for(int j=i+1;j<10;j++)check(!slots[i].Overlaps(slots[j]), "Active skill targets never overlap");
                }
                Vector2 first=screen(slots[0].center),second=screen(slots[1].center);
                check(controls.ProcessPointer(41,TouchPhase.Began,first), "Skill finger captured independently");
                controls.ProcessPointer(41,TouchPhase.Moved,second);controls.ProcessPointer(41,TouchPhase.Canceled,second);
                check(game.Progression.Profile.equippedSkills[0]==1&&game.Progression.Profile.equippedSkills[1]==0&&!MobileControls.AttackHeld,"Combat drag/cancel never accidentally rearranges skills or starts attack");
                controls.ProcessPointer(42,TouchPhase.Began,attack);controls.ProcessPointer(42,TouchPhase.Began,dodge);
                check(!MobileControls.ConsumeDodge(),"Repeated Began cannot change a captured finger role");
                controls.ProcessPointer(42,TouchPhase.Ended,attack);
                for(int i=0;i<MobileSkillPolicy.ButtonCount;i++)check(MobileSkillPolicy.SkillAtButton(i)==i,"Each direct button retains its stable skill identity");
                foreach (EnemyController enemy in originalEnemies) if (enemy != null) enemy.gameObject.SetActive(false);
                game.Player.Teleport(new Vector3(0, 0, -5));
                game.Player.transform.rotation = Quaternion.identity;
                typeof(GameSession).GetMethod("SpawnEnemy", Hidden).Invoke(game, new object[] { EnemyKind.Guardian, 100, game.Player.transform.position + Vector3.forward * 4, false });
                fixtureEnemy = game.Enemies[game.Enemies.Count - 1]; fixtureEnemy.enabled = false;
                check(WorldTraversal.HasLineOfSight(game.Player.transform.position, fixtureEnemy.transform.position), "Autoaim fixture target is genuinely visible in the authored world");
                float energy = game.Player.Energy;
                foreach (int inactive in new[] { 3, 8, 7 })
                {
                    Vector2 point = screen(slots[inactive].center);
                    controls.ProcessPointer(50 + inactive, TouchPhase.Began, point);
                    controls.ProcessPointer(50 + inactive, TouchPhase.Ended, point);
                }
                check(!charge.IsCharging && !targeting.IsTargeting && game.Player.Energy == energy && game.Player.SkillCooldownRemaining(7) == 0,
                    "Learned passive and unlearned active slots never cast or spend resources");
                controls.ProcessPointer(60, TouchPhase.Began, first);
                controls.ProcessPointer(60, TouchPhase.Ended, second);
                check(game.Player.Energy == energy && game.Player.SkillCooldownRemaining(0) == 0,
                    "Releasing a skill finger over a different slot cancels instead of changing the selected skill");
                int frame = Time.frameCount; while (Time.frameCount == frame) yield return null;
                controls.ProcessPointer(61, TouchPhase.Began, second);
                check(game.Player.Energy == energy, "Skill press does not spend before the tap is released");
                controls.ProcessPointer(61, TouchPhase.Ended, second);
                float committedEnergy = energy - GameBalance.SkillEnergyCost(game.Player.HeroClass, 1);
                check(!targeting.IsTargeting && game.Player.AimTarget == fixtureEnemy && Mathf.Abs(game.Player.Energy - committedEnergy) < .001f &&
                    game.Player.SkillCooldownRemaining(1) > 0 && game.Player.SkillCooldownRemaining(0) == 0,
                    "One mobile tap autoaims and casts directional skill one independently of desktop page/slot assignments");
                controls.ProcessPointer(61, TouchPhase.Ended, second);
                check(Mathf.Abs(game.Player.Energy - committedEnergy) < .001f, "Duplicate touch release cannot commit a second cast");

                fixtureRuntime.Advance(200); fixtureRuntime.FillEnergy();
                frame = Time.frameCount; while (Time.frameCount == frame) yield return null;
                Vector2 ultimate = screen(slots[9].center);
                controls.ProcessPointer(62, TouchPhase.Began, ultimate);
                controls.ProcessPointer(62, TouchPhase.Ended, ultimate);
                check(charge.IsCharging && !targeting.IsTargeting && game.Player.Energy == game.Player.MaxEnergy &&
                    Vector3.Distance(charge.TargetPoint, fixtureEnemy.transform.position) < .01f,
                    "A single ultimate tap autoaims and starts charging with no ground confirmation step or early spend");
                charge.Cancel();
                check(!charge.IsCharging && game.Player.Energy == game.Player.MaxEnergy && game.Player.SkillCooldownRemaining(9) == 0,
                    "Cancelling the mobile charge preserves its uncommitted budget");
                frame = Time.frameCount; while (Time.frameCount == frame) yield return null;
                controls.ProcessPointer(63, TouchPhase.Began, ultimate);
                controls.ProcessPointer(63, TouchPhase.Ended, ultimate);
                typeof(SkillChargeController).GetMethod("Advance", Hidden).Invoke(charge, new object[] { SkillChargeController.Duration(game.Player.HeroClass, 9) });
                check(!charge.IsCharging && !targeting.IsTargeting && game.Player.SkillCooldownRemaining(9) > 0 &&
                    Mathf.Abs(game.Player.Energy - (game.Player.MaxEnergy - GameBalance.SkillEnergyCost(game.Player.HeroClass, 9))) < .001f,
                    "The mobile charge completes through the real controller and spends exactly once");
                fixtureEnemy.gameObject.SetActive(false);
                fixtureRuntime.Advance(200); fixtureRuntime.FillEnergy();
                frame = Time.frameCount; while (Time.frameCount == frame) yield return null;
                game.Player.transform.rotation = Quaternion.identity;
                controls.ProcessPointer(64, TouchPhase.Began, ultimate);
                controls.ProcessPointer(64, TouchPhase.Ended, ultimate);
                Vector3 expectedFallback = Vector3.ClampMagnitude(game.Player.transform.position + game.Player.transform.forward * 8f, game.ArenaRadius - .65f);
                check(WorldTraversal.HasLineOfSight(game.Player.transform.position, expectedFallback), "Forward-fallback fixture has an unobstructed eight-meter line");
                check(charge.IsCharging && game.Player.AimTarget == null && Vector3.Distance(charge.TargetPoint, expectedFallback) < .01f,
                    "With no living visible target, one-tap ultimate uses its legal forward fallback");
                charge.Cancel();
                MobileControls.SimulationEnabled = false;
                MobileControls.ResetInput();
#if !UNITY_IOS && !UNITY_ANDROID
                check(!MobileControls.Active && MobileControls.Move == Vector2.zero && !MobileControls.AttackHeld, "Desktop mode leaves virtual input inactive after simulation");
#endif
            }
            finally
            {
                MobileControls.ResetInput();
                MobileControls.SimulationEnabled = originalSimulation;
                charge.Cancel(); targeting.Cancel(); ui.CancelMobileCast();
                Call(ui, "CancelHotbarPointer");
                if (fixtureEnemy != null) { game.Enemies.Remove(fixtureEnemy); fixtureEnemy.gameObject.SetActive(false); UnityEngine.Object.Destroy(fixtureEnemy.gameObject); }
                for (int i = 0; i < originalEnemies.Length; i++)
                    if (originalEnemies[i] != null) originalEnemies[i].gameObject.SetActive(activeEnemies[i]);
                Field(typeof(GameSession), "respawnTimer").SetValue(game, originalRespawn);
                Field(typeof(PlayerController), "skillRuntime").SetValue(game.Player, originalRuntime);
                Set(game.Progression, "Profile", originalProfile);
                Set(game.Progression, "LastError", originalError);
                game.SetPaused(false);
                game.SetUIBlocking(false);
                game.Player.Teleport(originalPosition);
                game.Player.transform.rotation = originalRotation;
                game.Player.RefreshStats(false);
                game.Player.enabled = originalEnabled;
                Field(typeof(GameSession), "notification").SetValue(game, notification);
                Field(typeof(GameSession), "notificationUntil").SetValue(game, notificationUntil);
                for (int i = 0; i < paths.Length; i++)
                {
                    if (bytes[i] != null) File.WriteAllBytes(paths[i], bytes[i]);
                    else if (File.Exists(paths[i])) File.Delete(paths[i]);
                }
            }
        }
        private static IEnumerable<object> Wait(float seconds)
        {
            double until = Time.timeAsDouble + seconds;
            double deadline = EditorApplication.timeSinceStartup + 5;
            while (Time.timeAsDouble < until)
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Mobile input simulation received no runtime updates.");
                yield return null;
            }
        }
        private static FieldInfo Field(Type type, string name) { return type.GetField(name, Hidden); }
        private static void Call(object owner, string name) { owner.GetType().GetMethod(name, Hidden).Invoke(owner, null); }
        private static void Set(object owner, string name, object value) { owner.GetType().GetProperty(name).GetSetMethod(true).Invoke(owner, new[] { value }); }
    }
}
