using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Emberfall.Editor
{
    public static class SummonerValidation
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        // Call on the fully learned Summoner in an isolated wilderness fixture.
        public static IEnumerator Validate(GameSession game, Action<bool, string> check, Action<string> log)
        {
            typeof(InventoryUIValidation).GetMethod("RequireIsolatedRuntime", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { game });
            PlayerController player = game.Player;
            check(player.HeroClass == HeroClass.Summoner && !game.InDungeon, "Summoner fixture is a wilderness summoner");
            float previousRespawnTimer = Read<float>(game, "respawnTimer");
            bool playerEnabled = player.enabled;
            SummonerRoute previousRoute = game.Progression.Profile.summonerRoute;
            player.enabled = false; // Isolate automatic foundation upkeep from explicit contract fixtures.
            game.Progression.Profile.summonerRoute = SummonerRoute.Bonded;
            Set(game, "respawnTimer", 600f);
            try
            {
            Clear(game);
            player.Teleport(new Vector3(0, 0, -5));
            player.RefreshStats(true);
            game.SetPaused(false);
            SkillRuntime runtime = Read<SkillRuntime>(player, "skillRuntime");
            int frame = Time.frameCount;
            while (Time.frameCount == frame) yield return null;

            foreach (SummonedCompanion.Kind form in (SummonedCompanion.Kind[])Enum.GetValues(typeof(SummonedCompanion.Kind)))
            {
                Clear(game);
                player.Teleport(new Vector3(0, 0, -5));
                runtime.Advance(200); runtime.FillEnergy();
                EnemyController enemy = Spawn(game, EnemyKind.Guardian, player.transform.position + Vector3.forward * 3);
                Set(player, "aimPoint", enemy.transform.position);
                int skill = form == SummonedCompanion.Kind.Wolf ? 2 : form == SummonedCompanion.Kind.Spirit ? 4 : 9;
                Invoke(player, "CastSkill", skill);
                SummonedCompanion pet = Find(player, form);
                check(pet != null && pet.IsAlive && pet.Owner == player, form + " actual class skill creates a living companion belonging to its caster");
                check(form == SummonedCompanion.Kind.Treant ? !pet.IsPermanent && Mathf.Abs(pet.RemainingLifetime - 16) < .01f
                    : pet.IsPermanent && float.IsPositiveInfinity(pet.RemainingLifetime),
                    form + " bonded wolf/spirit remain permanent while awakened tree keeps finite lifetime");
                float health = enemy.Health;
                double until = Time.timeAsDouble + .8;
                while (Time.timeAsDouble < until) yield return null;
                check(!enemy.IsAggro && enemy.Health == health, form + " does not automatically provoke nearby ordinary wildlife");
                enemy.TakeDamage(1, Vector3.zero, impact: false);
                health = enemy.Health;
                double deadline = EditorApplication.timeSinceStartup + 5;
                while (enemy.Health >= health && EditorApplication.timeSinceStartup < deadline) yield return null;
                check(enemy.Health < health, form + " deals real combat damage after the player provokes an enemy");
                if (form == SummonedCompanion.Kind.Treant)
                    check(enemy.StatusEffects.KnockedDown, "Ancient guardian's actual heavy hit applies knockdown");
                float petHealth = pet.Health;
                float expectedHit = CompanionRules.DamageTaken(25, false, pet.IsRecalling);
                pet.TakeDamage(25);
                check(Mathf.Abs(pet.Health - (petHealth - expectedHit)) < .01f && pet.IsAlive, form + " has real damageable health");
                SummonedCompanion.HealAll(player, .1f);
                check(pet.Health > petHealth - expectedHit && pet.Health <= pet.MaxHealth, form + " receives bounded companion healing");
                pet.transform.position = enemy.transform.position + Vector3.right * .5f;
                check(SummonedCompanion.ThreatTarget(enemy, player.transform.position) == pet, form + " can become the nearby monster's real combat target");
                pet.TakeDamage(pet.MaxHealth * 2);
                check(!pet.IsAlive && SummonedCompanion.Count(player, form == SummonedCompanion.Kind.Treant) == 0, form + " death immediately removes the companion from its active cap");
            }

            Clear(game);
            player.Teleport(new Vector3(0, 0, -5));
            SummonedCompanion foundation = SummonedCompanion.SummonStarter(player, game, 10);
            foundation.TakeDamage(foundation.MaxHealth * .4f);
            float foundationHealth = foundation.Health;
            SummonedCompanion sameWolf = SummonedCompanion.CastContract(player, game, SummonedCompanion.Kind.Wolf, 3, player.transform.position, 10, false);
            check(ReferenceEquals(foundation, sameWolf) && foundation.IsPermanent && foundation.IsStarter &&
                Mathf.Abs(foundation.Health - foundationHealth) < .001f,
                "Learned wolf contract commands the existing permanent foundation without replacing or freely healing it");
            SummonedCompanion bondedSpirit = SummonedCompanion.CastContract(player, game, SummonedCompanion.Kind.Spirit, 3, player.transform.position, 10, false);
            SummonedCompanion temporary = SummonedCompanion.Summon(player, game, SummonedCompanion.Kind.Wolf, 2, player.transform.position, 10);
            player.Teleport(player.transform.position + Vector3.right * 2);
            check(foundation.IsAlive && bondedSpirit.IsAlive && foundation.IsPermanent && bondedSpirit.IsPermanent &&
                WorldTraversal.IsWalkable(foundation.transform.position, .4f) && WorldTraversal.IsWalkable(bondedSpirit.transform.position, .35f) &&
                Mathf.Abs(foundation.Health - foundationHealth) < .001f && !foundation.IsRecalling,
                "Teleport transfers only living permanent partners to legal new ground, preserving health and clearing commands");
            check(!temporary.IsAlive && SummonedCompanion.Count(player) == 2,
                "Teleport still invalidates old temporary pets and excludes them from the new encounter cap");
            game.Progression.Profile.summonerRoute = SummonerRoute.Pack;
            SummonedCompanion.EnforceCapacity(player);
            check(foundation.IsPermanent && !bondedSpirit.IsPermanent && bondedSpirit.RemainingLifetime <= 24,
                "Pack switch keeps permanent foundation but gives spirit its documented upkeep lifetime");
            SummonedCompanion.CastContract(player, game, SummonedCompanion.Kind.Wolf, 3, player.transform.position, 10, true);
            check(SummonedCompanion.Count(player) == 4 && foundation.IsAlive,
                "Awakened pack command fills available four-pet slots without replacing its permanent foundation");
            game.Progression.Profile.summonerRoute = SummonerRoute.Bonded;
            SummonedCompanion.EnforceCapacity(player);
            check(SummonedCompanion.Count(player) == 2 && foundation.IsAlive && bondedSpirit.IsPermanent,
                "Bonded switch removes timed extras and keeps exactly one wolf and one spirit");
            foundation.TakeDamage(foundation.MaxHealth * 20f);
            SummonedCompanion resurrected = SummonedCompanion.CastContract(player, game, SummonedCompanion.Kind.Wolf, 3, player.transform.position, 10, false);
            check(!foundation.IsAlive && resurrected != null && resurrected.IsAlive && !ReferenceEquals(foundation, resurrected) &&
                SummonedCompanion.Count(player) == 2 && bondedSpirit.IsAlive,
                "A dead foundation alone is resummoned; the other living partner is retained");

            IEnumerator contracts = ValidateContractRefreshAndDodge(game, check);
            while (contracts.MoveNext()) yield return contracts.Current;
            IEnumerator captured = ValidateChargedContracts(game, check);
            while (captured.MoveNext()) yield return captured.Current;
            CompanionBuildValidation.Validate(game, check);
            IEnumerator lifecycle = ValidateOwnerLifecycle(game, check);
            while (lifecycle.MoveNext()) yield return lifecycle.Current;

            Clear(game);
            game.Progression.Profile.summonerRoute = SummonerRoute.Pack;
            player.Teleport(new Vector3(0, 0, -5));
            SummonedCompanion oldest = null;
            for (int i = 0; i < 7; i++)
            {
                SummonedCompanion pet = SummonedCompanion.Summon(player, game, i % 2 == 0 ? SummonedCompanion.Kind.Wolf : SummonedCompanion.Kind.Spirit, 3, player.transform.position, 10);
                if (i == 0) oldest = pet;
                check(SummonedCompanion.Count(player) == Math.Min(i + 1, 4), "Ordinary summon population never exceeds four during repeated casts");
            }
            check(!oldest.IsAlive, "Repeated summons replace the oldest ordinary companion");
            SummonedCompanion firstTree = SummonedCompanion.Summon(player, game, SummonedCompanion.Kind.Treant, 3, player.transform.position, 10);
            SummonedCompanion tree = SummonedCompanion.Summon(player, game, SummonedCompanion.Kind.Treant, 3, player.transform.position, 10);
            check(!firstTree.IsAlive && tree.IsAlive && SummonedCompanion.Count(player) == 4 && SummonedCompanion.Count(player, true) == 1,
                "Guardian replacement preserves the separate four ordinary plus one guardian cap");

            foreach (SummonedCompanion pet in UnityEngine.Object.FindObjectsOfType<SummonedCompanion>())
                if (pet.Owner == player) pet.TakeDamage(pet.MaxHealth * .65f);
            tree = Find(player, SummonedCompanion.Kind.Treant);
            float treeBefore = tree.Health;
            typeof(PlayerController).GetProperty("Health").SetValue(player, player.MaxHealth * .35f, null);
            float playerBefore = player.Health;
            runtime.Advance(200); runtime.FillEnergy();
            Invoke(player, "CastSkill", 6);
            double healUntil = Time.timeAsDouble + 1.15;
            while (Time.timeAsDouble < healUntil) yield return null;
            check(tree.Health > treeBefore && player.Health > playerBefore, "Actual rejuvenation ticks heal both the caster and living companions");

            float lifetime = tree.RemainingLifetime;
            game.SetPaused(true);
            try
            {
                double pauseUntil = EditorApplication.timeSinceStartup + .12;
                while (EditorApplication.timeSinceStartup < pauseUntil) yield return null;
                check(Mathf.Abs(tree.RemainingLifetime - lifetime) < .001f, "Companion lifetime freezes while the game is paused");
            }
            finally { game.SetPaused(false); }
            player.Teleport(player.transform.position + Vector3.right);
            frame = Time.frameCount;
            while (Time.frameCount < frame + 2) yield return null;
            check(SummonedCompanion.Count(player) == 0 && SummonedCompanion.Count(player, true) == 0, "Changing encounter epoch dismisses every old temporary companion");
            SummonedCompanion doomed = SummonedCompanion.Summon(player, game, SummonedCompanion.Kind.Wolf, 1, player.transform.position, 10);
            float savedHealth = player.Health;
            try
            {
                typeof(PlayerController).GetProperty("Health").SetValue(player, 0f, null);
                Invoke(doomed, "Update");
                check(!doomed.gameObject.activeSelf && SummonedCompanion.Count(player) == 0, "Owner death immediately dismisses its companion in the live update path");
            }
            finally { typeof(PlayerController).GetProperty("Health").SetValue(player, savedHealth, null); }
            SummonedCompanion expired = SummonedCompanion.Summon(player, game, SummonedCompanion.Kind.Spirit, 1, player.transform.position, 10);
            typeof(SummonedCompanion).GetProperty("RemainingLifetime").SetValue(expired, 0f, null);
            Invoke(expired, "Update");
            check(!expired.gameObject.activeSelf && SummonedCompanion.Count(player) == 0, "Lifetime expiry runs through live dismissal and frees the ordinary summon slot");

            Clear(game);
            EnemyController normal = Spawn(game, EnemyKind.Guardian, new Vector3(-4, 0, 3));
            EnemyController boss = Spawn(game, EnemyKind.Guardian, new Vector3(4, 0, 3), true);
            normal.StatusEffects.Knockup(1f, 1.8f);
            boss.StatusEffects.Knockup(1f, 1.8f);
            check(Read<float>(boss.StatusEffects, "airborneDuration") < Read<float>(normal.StatusEffects, "airborneDuration") && Read<float>(boss.StatusEffects, "airborneHeight") < Read<float>(normal.StatusEffects, "airborneHeight"),
                "Boss resistance reduces both airborne duration and lift height");
            double riseUntil = Time.timeAsDouble + .12;
            while (Time.timeAsDouble < riseUntil) yield return null;
            CombatModel normalModel = normal.GetComponentInChildren<CombatModel>();
            normalModel.Animate(0, 0, false);
            check(normal.StatusEffects.IsAirborne && normal.StatusEffects.AirborneHeight > .15f && normalModel.transform.localPosition.y > .15f,
                "Airborne control raises the actual monster model above the ground");
            float airborneRemaining = Read<float>(normal.StatusEffects, "airborneTime");
            normal.StatusEffects.Knockup(1.2f, 2f);
            check(Read<float>(normal.StatusEffects, "airborneTime") == airborneRemaining, "Repeated hits cannot restart the same airborne arc");

            normal.transform.rotation = Quaternion.identity;
            float before = normal.Health;
            normal.TakeDamage(100, Vector3.back, impact: false);
            float front = before - normal.Health;
            before = normal.Health;
            normal.TakeDamage(100, Vector3.forward, impact: false);
            float behind = before - normal.Health;
            Set(normal, "preparing", true);
            before = normal.Health;
            normal.TakeDamage(100, Vector3.back, impact: false);
            float exposed = before - normal.Health;
            check(Mathf.Abs(front - 65) < .01f && Mathf.Abs(behind - 100) < .01f && Mathf.Abs(exposed - 100) < .01f,
                "Guardian stone armor reduces frontal damage 35%, but rear hits and its windup expose full damage");
            Set(normal, "preparing", false);
            Clear(game);
            player.Teleport(new Vector3(0, 0, -5));
            player.RefreshStats(true);
            runtime.Advance(200); runtime.FillEnergy();
            log("SUMMONER passed owner retirement, non-summoner transition isolation, oldest-first capacity, full-pack refresh, captured charge targets/points, non-disruptive dodge protection and command consumption, real skill summons, neutral wildlife, attacks, health/healing, permanent contracts, route switches, finite packs, 4+1 cap, pause, permanent transfers and temporary epoch/death cleanup, visible airborne resistance and guardian armor.");
            }
            finally
            {
                Set(game, "respawnTimer", previousRespawnTimer);
                game.Progression.Profile.summonerRoute = previousRoute;
                player.enabled = playerEnabled;
            }
        }

        private static IEnumerator ValidateContractRefreshAndDodge(GameSession game, Action<bool, string> check)
        {
            Clear(game);
            PlayerController player = game.Player;
            player.Teleport(new Vector3(0, 0, -5));
            game.Progression.Profile.summonerRoute = SummonerRoute.Pack;
            SummonedCompanion growing = SummonedCompanion.Summon(player, game, SummonedCompanion.Kind.Spirit, 1, player.transform.position, 10);
            growing.TakeDamage(40); float oldHealth = growing.Health, oldMaximum = growing.MaxHealth;
            SummonedCompanion upgraded = SummonedCompanion.CastContract(player, game, SummonedCompanion.Kind.Spirit, 3, player.transform.position, 10, true);
            check(ReferenceEquals(growing, upgraded) && upgraded.MaxHealth > oldMaximum && Mathf.Abs(upgraded.Health - oldHealth) < .001f,
                "A higher-rank contract updates the maximum without multiplying or refilling the living companion's absolute HP");
            Clear(game);
            EnemyController focus = Spawn(game, EnemyKind.Guardian, player.transform.position + Vector3.forward * 6, true);
            SummonedCompanion.CastContract(player, game, SummonedCompanion.Kind.Wolf, 3, focus.transform.position, 10, true, focus);
            SummonedCompanion[] pack = SummonedCompanion.Snapshot(player);
            check(pack.Length == 4 && SummonedCompanion.Count(player) == 4, "Wolf contract fills the ordinary four-member pack");
            foreach (SummonedCompanion pet in pack) { pet.enabled = false; pet.TakeDamage(40); }
            // Seed an already elapsed lifetime only; recast must exercise production
            // refresh and may not change membership, health, or the permanent partner.
            foreach (SummonedCompanion pet in pack)
                if (!pet.IsPermanent) typeof(SummonedCompanion).GetProperty("RemainingLifetime").SetValue(pet, 2f, null);
            float[] health = Array.ConvertAll(pack, pet => pet.Health);
            SummonedCompanion.CastContract(player, game, SummonedCompanion.Kind.Wolf, 3, focus.transform.position, 10, true, focus);
            check(SummonedCompanion.Count(player) == pack.Length, "Full-pack recast creates no replacement or extra companion");
            for (int i = 0; i < pack.Length; i++)
                check(pack[i].IsAlive && Mathf.Abs(pack[i].Health - health[i]) < .001f &&
                    (pack[i].IsPermanent ? float.IsPositiveInfinity(pack[i].RemainingLifetime) : Mathf.Abs(pack[i].RemainingLifetime - CompanionRules.PackLifetime(3)) < .001f),
                    "Full-pack recast retains each living instance and absolute HP while refreshing its own lifetime");
            SummonedCompanion.CastContract(player, game, SummonedCompanion.Kind.Wolf, 3, focus.transform.position, 10, true, focus);
            foreach (SummonedCompanion pet in pack)
                check(pet.IsPermanent || Mathf.Abs(pet.RemainingLifetime - CompanionRules.PackLifetime(3)) < .001f,
                    "Repeated pack refresh does not add lifetimes together");

            Vector3[] positions = Array.ConvertAll(pack, pet => pet.transform.position);
            float[] cooldowns = Array.ConvertAll(pack, pet => Read<float>(pet, "cooldown"));
            SummonedCompanion.OnPerfectDodge(player);
            check(SummonedCompanion.CommandOpportunityRemaining(player) >= GameBalance.EffectiveCooldown(HeroClass.Summoner, 2, 1),
                "Perfect-dodge command opportunity outlasts the actual base wolf cooldown");
            for (int i = 0; i < pack.Length; i++)
                check(!pack[i].IsRecalling && Read<EnemyController>(pack[i], "commandedTarget") == focus &&
                    pack[i].transform.position == positions[i] && Read<float>(pack[i], "cooldown") == cooldowns[i],
                    "Perfect dodge preserves companion command, position, and attack cadence without recall");
            float before = pack[0].Health;
            pack[0].TakeDamage(40);
            check(Mathf.Abs(before - pack[0].Health - 20) < .001f, "Dodge protection halves a companion's direct incoming damage");
            before = pack[0].Health;
            pack[0].TakeDamage(40, true);
            check(Mathf.Abs(before - pack[0].Health - 11) < .001f, "Dodge protection and companion area resistance yield 27.5 percent incoming area damage");
            float opportunity = SummonedCompanion.CommandOpportunityRemaining(player);
            game.SetPaused(true);
            try
            {
                double deadline = EditorApplication.timeSinceStartup + .12;
                while (EditorApplication.timeSinceStartup < deadline) yield return null;
                check(Mathf.Abs(SummonedCompanion.CommandOpportunityRemaining(player) - opportunity) < .001f,
                    "Paused time cannot expire the pending companion command opportunity");
            }
            finally { game.SetPaused(false); }
            SummonedCompanion.CastContract(player, game, SummonedCompanion.Kind.Wolf, 3, focus.transform.position, 10, true, focus);
            check(SummonedCompanion.CommandOpportunityRemaining(player) == 0, "One contract consumes the perfect-dodge opportunity once for the whole pack");
            foreach (SummonedCompanion pet in pack)
                check(Mathf.Abs(Read<float>(pet, "commandMultiplier") - CompanionRules.CommandMultiplier(3) * CompanionRules.EmpoweredCommandMultiplier) < .001f,
                    "Each member receives the same empowered command from the single consumed opportunity");
            SummonedCompanion.CastContract(player, game, SummonedCompanion.Kind.Wolf, 3, focus.transform.position, 10, true, focus);
            foreach (SummonedCompanion pet in pack)
                check(Mathf.Abs(Read<float>(pet, "commandMultiplier") - CompanionRules.CommandMultiplier(3)) < .001f,
                    "The next contract cannot reuse an already consumed dodge opportunity");
            player.Teleport(player.transform.position);
            check(SummonedCompanion.CommandOpportunityRemaining(player) == 0, "An encounter transfer cannot inherit a command opportunity");
        }

        private static IEnumerator ValidateChargedContracts(GameSession game, Action<bool, string> check)
        {
            PlayerController player = game.Player;
            SkillRuntime runtime = Read<SkillRuntime>(player, "skillRuntime");
            SkillTargetingController targeting = player.GetComponent<SkillTargetingController>();
            SkillChargeController charge = player.GetComponent<SkillChargeController>();
            bool simulation = MobileControls.SimulationEnabled;
            MobileControls.SimulationEnabled = false;
            try
            {
                // Spirit is an immediate-entry charge; the tree uses the real
                // ground confirmation path. Neither test calls charge.Begin directly.
                foreach (int skill in new[] { 4, 9 })
                {
                    Clear(game); player.Teleport(new Vector3(0, 0, -5));
                    runtime.Advance(200); runtime.FillEnergy();
                    int frame = Time.frameCount; while (Time.frameCount == frame) yield return null;
                    EnemyController original = Spawn(game, EnemyKind.Guardian, player.transform.position + Vector3.forward * 5);
                    EnemyController later = Spawn(game, EnemyKind.Guardian, player.transform.position + Vector3.left * 3);
                    SetAim(player, original);
                    check(targeting.Begin(skill), "Charged contract enters through the production skill dispatcher");
                    if (targeting.IsTargeting) { targeting.SetTarget(original.transform.position); check(targeting.Confirm(), "Tree contract confirms its selected target point"); }
                    check(charge.IsCharging && charge.TargetEnemy == original, "Charged contract captures the selected living enemy before later aim changes");
                    Vector3 capturedPoint = charge.TargetPoint;
                    SetAim(player, later); Set(player, "focusedEnemy", later); Set(player, "focusTime", 3f);
                    Invoke(charge, "Advance", SkillChargeController.Duration(HeroClass.Summoner, skill));
                    SummonedCompanion pet = Find(player, skill == 4 ? SummonedCompanion.Kind.Spirit : SummonedCompanion.Kind.Treant);
                    check(pet != null && Read<EnemyController>(pet, "commandedTarget") == original && !charge.IsCharging && charge.TargetEnemy == null,
                        "Released companion commands the captured enemy and clears the charge reference");
                    check(Vector3.Distance(player.AimPoint, capturedPoint) < .001f &&
                        Mathf.Abs(player.Energy - (player.MaxEnergy - GameBalance.SkillEnergyCost(HeroClass.Summoner, skill))) < .001f,
                        "Captured contract retains its original point and commits its resource cost once");
                }

                Clear(game); player.Teleport(new Vector3(0, 0, -5));
                runtime.Advance(200); runtime.FillEnergy();
                int nextFrame = Time.frameCount; while (Time.frameCount == nextFrame) yield return null;
                EnemyController disappearing = Spawn(game, EnemyKind.Guardian, player.transform.position + Vector3.forward * 5);
                EnemyController replacement = Spawn(game, EnemyKind.Guardian, player.transform.position + Vector3.left * 3);
                SetAim(player, disappearing);
                check(targeting.Begin(9) && targeting.IsTargeting, "Stale-target fixture enters actual tree placement");
                targeting.SetTarget(disappearing.transform.position); check(targeting.Confirm() && charge.TargetEnemy == disappearing, "Tree charge captures the soon-to-be-stale enemy");
                Vector3 locked = charge.TargetPoint;
                // Simulate a target removed by another attack without awarding a
                // fixture kill; the live charge must reject this inactive reference.
                disappearing.gameObject.SetActive(false); game.Enemies.Remove(disappearing);
                SetAim(player, replacement); Set(player, "focusedEnemy", replacement); Set(player, "focusTime", 3f);
                Invoke(charge, "Advance", SkillChargeController.Duration(HeroClass.Summoner, 9));
                SummonedCompanion tree = Find(player, SummonedCompanion.Kind.Treant);
                check(tree != null && Read<EnemyController>(tree, "commandedTarget") == null && Read<bool>(tree, "hasCommandPoint") &&
                    Vector3.Distance(Read<Vector3>(tree, "commandedPoint"), WorldTraversal.NearestWalkable(locked, .7f)) < .01f,
                    "A stale captured enemy falls back to the captured legal point instead of the newer focus target");
                UnityEngine.Object.Destroy(disappearing.gameObject);
                Clear(game); player.Teleport(player.transform.position);
                runtime.Advance(200); runtime.FillEnergy();
            }
            finally { charge.Cancel(); targeting.Cancel(); MobileControls.SimulationEnabled = simulation; MobileControls.ResetInput(); }
        }

        private static IEnumerator ValidateOwnerLifecycle(GameSession game, Action<bool, string> check)
        {
            // This helper runs only under Validate's isolated-runtime guard. Each
            // temporary controller and pet belongs to this fixture, never a save.
            Clear(game);
            PlayerController player = game.Player;
            player.Teleport(new Vector3(0, 0, -5));
            var ownerStates = (IDictionary)typeof(SummonedCompanion).GetField("bonds", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            var companions = (IList)typeof(SummonedCompanion).GetField("active", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            int initialOwners = ownerStates.Count;
            foreach (HeroClass hero in new[] { HeroClass.Vanguard, HeroClass.Arcanist, HeroClass.Ranger })
            {
                GameObject root = new GameObject("Isolated non-summoner owner lifecycle");
                PlayerController temporary = root.AddComponent<PlayerController>(); temporary.enabled = false;
                try
                {
                    typeof(PlayerController).GetProperty("HeroClass").SetValue(temporary, hero, null);
                    typeof(PlayerController).GetProperty("Health").SetValue(temporary, 1f, null);
                    for (int repeat = 0; repeat < 8; repeat++) temporary.Teleport(player.transform.position);
                    check(!ownerStates.Contains(temporary) && ownerStates.Count == initialOwners,
                        hero + " repeated real teleports never allocate companion owner state");
                }
                finally { UnityEngine.Object.Destroy(root); }
                yield return null;
            }

            GameObject ownedRoot = new GameObject("Isolated retired summoner owner");
            PlayerController retired = ownedRoot.AddComponent<PlayerController>(); retired.enabled = false;
            SummonedCompanion first = null, second = null;
            try
            {
                typeof(PlayerController).GetProperty("HeroClass").SetValue(retired, HeroClass.Summoner, null);
                typeof(PlayerController).GetProperty("Health").SetValue(retired, 100f, null);
                SummonedCompanion.TransferPermanentPartners(retired);
                first = SummonedCompanion.Summon(retired, game, SummonedCompanion.Kind.Wolf, 1, player.transform.position, 10);
                second = SummonedCompanion.Summon(retired, game, SummonedCompanion.Kind.Treant, 1, player.transform.position, 10);
                // Prevent normal Update from cleaning these fixtures first: the
                // owner's real OnDestroy must retire both disabled components.
                first.enabled = second.enabled = false;
                check(ownerStates.Contains(retired) && companions.Contains(first) && companions.Contains(second),
                    "Retirement fixture owns one bond and both companion entries before destruction");
                int destroyFrame = Time.frameCount;
                UnityEngine.Object.Destroy(ownedRoot);
                while (Time.frameCount <= destroyFrame) yield return null;
                check(retired == null && !ReferenceEquals(retired, null) && !ownerStates.Contains(retired),
                    "Actual destroyed Unity owner releases its managed dictionary key");
                check(!companions.Contains(first) && !companions.Contains(second) &&
                    (first == null || !first.gameObject.activeSelf) && (second == null || !second.gameObject.activeSelf),
                    "Owner teardown immediately retires every owned pet even when its Update is disabled");
                typeof(SummonedCompanion).GetMethod("RetireOwner", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { retired });
                check(ownerStates.Count == initialOwners, "Repeated retirement of a destroyed owner is safe and does not retain its key");
            }
            finally
            {
                if (first != null) first.Dismiss(); if (second != null) second.Dismiss();
                if (ownedRoot != null) UnityEngine.Object.Destroy(ownedRoot);
            }

            Clear(game); player.Teleport(player.transform.position);
            SummonerRoute route = game.Progression.Profile.summonerRoute;
            try
            {
                game.Progression.Profile.summonerRoute = SummonerRoute.Pack;
                SummonedCompanion starter = SummonedCompanion.SummonStarter(player, game, 10);
                SummonedCompanion oldestSpirit = SummonedCompanion.Summon(player, game, SummonedCompanion.Kind.Spirit, 1, player.transform.position, 10);
                SummonedCompanion extraWolf = SummonedCompanion.Summon(player, game, SummonedCompanion.Kind.Wolf, 1, player.transform.position, 10);
                SummonedCompanion newerSpirit = SummonedCompanion.Summon(player, game, SummonedCompanion.Kind.Spirit, 1, player.transform.position, 10);
                float oldHealth = oldestSpirit.Health, oldCooldown = Read<float>(oldestSpirit, "cooldown");
                game.Progression.Profile.summonerRoute = SummonerRoute.Bonded;
                for (int repeat = 0; repeat < 32; repeat++) SummonedCompanion.EnforceCapacity(player);
                check(SummonedCompanion.Count(player) == 2 && starter.IsAlive && SummonedCompanion.HasStarter(player) &&
                    oldestSpirit.IsAlive && oldestSpirit.IsPermanent && !extraWolf.IsAlive && !newerSpirit.IsAlive,
                    "Forward removal keeps the oldest spirit and starter without skipping adjacent removals");
                check(oldestSpirit.Health == oldHealth && Read<float>(oldestSpirit, "cooldown") == oldCooldown &&
                    float.IsPositiveInfinity(oldestSpirit.RemainingLifetime),
                    "Repeated capacity checks do not change retained health, attack recovery or permanent lifetime");
            }
            finally { game.Progression.Profile.summonerRoute = route; Clear(game); player.Teleport(player.transform.position); }
        }

        private static void SetAim(PlayerController player, EnemyController enemy)
        {
            typeof(PlayerController).GetProperty("AimTarget").SetValue(player, enemy, null);
            Set(player, "aimPoint", enemy.transform.position);
        }

        private static void Clear(GameSession game)
        {
            foreach (EnemyController enemy in game.Enemies.ToArray())
                if (enemy != null) { enemy.gameObject.SetActive(false); UnityEngine.Object.Destroy(enemy.gameObject); }
            game.Enemies.Clear();
            foreach (SummonedCompanion pet in UnityEngine.Object.FindObjectsOfType<SummonedCompanion>())
                if (pet.Owner == game.Player) pet.Dismiss();
        }
        private static EnemyController Spawn(GameSession game, EnemyKind kind, Vector3 at, bool boss = false)
        {
            Invoke(game, "SpawnEnemy", kind, 1000, at, boss);
            EnemyController enemy = game.Enemies[game.Enemies.Count - 1];
            enemy.enabled = false;
            return enemy;
        }
        private static SummonedCompanion Find(PlayerController owner, SummonedCompanion.Kind kind)
        {
            foreach (SummonedCompanion pet in UnityEngine.Object.FindObjectsOfType<SummonedCompanion>())
                if (pet.Owner == owner && pet.Form == kind && pet.IsAlive) return pet;
            return null;
        }
        private static T Read<T>(object target, string name) { return (T)target.GetType().GetField(name, Private).GetValue(target); }
        private static void Set(object target, string name, object value) { target.GetType().GetField(name, Private).SetValue(target, value); }
        private static object Invoke(object target, string name, params object[] values) { return target.GetType().GetMethod(name, Private).Invoke(target, values); }
    }
}
