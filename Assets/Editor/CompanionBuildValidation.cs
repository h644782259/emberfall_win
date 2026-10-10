using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Emberfall.Editor
{
    /// <summary>Prepared isolated engine fixture, not evidence of engine execution.</summary>
    public static class CompanionBuildValidation
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        public static void Validate(GameSession game, Action<bool, string> check)
        {
            typeof(InventoryUIValidation).GetMethod("RequireIsolatedRuntime", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { game });
            if (game.Player == null || game.Player.HeroClass != HeroClass.Summoner || game.InDungeon || game.PendingLootCount != 0)
                throw new InvalidOperationException("Companion build fixture requires the isolated living summoner camp without pending drops.");
            ProgressionService p = game.Progression; PlayerController player = game.Player;
            GameProfile original = p.Profile; string error = p.LastError, path = p.SaveFilePath;
            if (File.Exists(path + ".tmp") || Directory.Exists(path + ".tmp"))
                throw new InvalidOperationException("Companion fixture refuses pre-existing temporary save data.");
            byte[] primary = File.ReadAllBytes(path), backup = File.ReadAllBytes(path + ".bak");
            DateTime primaryTime = File.GetLastWriteTimeUtc(path), backupTime = File.GetLastWriteTimeUtc(path + ".bak");
            object fingerprint = Field(typeof(GameSession), "equipmentFingerprint").GetValue(game);
            object oldRuntime = Field(typeof(PlayerController), "skillRuntime").GetValue(player);
            bool paused = game.Paused, enabled = player.enabled; float oldHealth = player.Health;
            Vector3 position = player.transform.position;
            bool ownsTemporary = false;
            try
            {
                foreach (SummonedCompanion pet in SummonedCompanion.Snapshot(player)) pet.Dismiss();
                var profile = JsonUtility.FromJson<GameProfile>(JsonUtility.ToJson(original));
                profile.level = 100; profile.masteryRanks = new int[4]; profile.masteryCore = -1;
                for (int skill = 0; skill < profile.skillRanks.Length; skill++) profile.skillRanks[skill] = 3;
                profile.summonerRoute = SummonerRoute.Bonded; profile.relicId = "";
                Set(p, "Profile", profile); p.Save(); player.RefreshStats(false);
                check(string.IsNullOrEmpty(p.LastError), "Companion fixture commits its isolated high-rank build");
                player.enabled = false; game.SetPaused(true);
                var runtime = new SkillRuntime(HeroClass.Summoner);
                check(runtime.TryConsume(4, 3), "Build fixture seeds a real Spirit skill cooldown");
                Field(typeof(PlayerController), "skillRuntime").SetValue(player, runtime);
                float skillCooldown = runtime.Remaining(4), energy = runtime.Energy;
                SummonedCompanion wolf = SummonedCompanion.SummonStarter(player, game, 10);
                SummonedCompanion spirit = SummonedCompanion.CastContract(player, game, SummonedCompanion.Kind.Spirit, 3, position, 10, false);
                wolf.enabled = spirit.enabled = false;
                spirit.TakeDamage(spirit.MaxHealth * .6f); float health = spirit.Health;
                SummonedCompanion.OnPerfectDodge(player);
                SummonedCompanion.CastContract(player, game, SummonedCompanion.Kind.Spirit, 3, position, 10, false);
                SummonedCompanion.OnPerfectDodge(player);
                Field(typeof(SummonedCompanion), "cooldown").SetValue(spirit, 1.25f);
                float commandTime = Read<float>(spirit, "commandTime"), opportunity = SummonedCompanion.CommandOpportunityRemaining(player);
                check(p.RefundSkillRanks(true) && spirit.Rank == 1 && wolf.Rank == 1 && spirit.IsAlive && spirit.IsPermanent,
                    "Real paused refund immediately updates the existing permanent wolf and Spirit to rank one");
                CheckPower(spirit, p, 1, check);
                check(spirit.Health == health && Read<float>(spirit, "cooldown") == 1.25f && Read<float>(spirit, "commandTime") == commandTime &&
                    Mathf.Abs(Read<float>(spirit, "commandMultiplier") - CompanionRules.ActiveCommandMultiplier(1, true)) < .001f &&
                    SummonedCompanion.CommandOpportunityRemaining(player) == opportunity,
                    "Refund preserves absolute HP, attack recovery, remaining command time, paid empowerment and unspent dodge token");
                check(p.LearnSkill(4) && p.LearnSkill(4) && spirit.Rank == 3 && spirit.Health == health,
                    "Real rank purchases immediately strengthen the living Spirit without free healing");
                check(p.ResetBuild(true) && spirit.Rank == 1 && spirit.Health == health,
                    "Combined build reset also reconciles its permanent companion in the same paused call");
                check(runtime.Remaining(4) == skillCooldown && runtime.Energy == energy && SummonedCompanion.CommandOpportunityRemaining(player) == opportunity,
                    "All live build operations preserve actual hero skill cooldown/energy and the unspent command token");

                check(p.SetSummonerRoute(SummonerRoute.Pack, true) && !spirit.IsPermanent,
                    "Paused route change immediately makes the Spirit timed");
                SummonedCompanion extraA = SummonedCompanion.Summon(player, game, SummonedCompanion.Kind.Wolf, 1, position, 10);
                SummonedCompanion extraB = SummonedCompanion.Summon(player, game, SummonedCompanion.Kind.Wolf, 1, position, 10);
                extraA.enabled = extraB.enabled = false;
                check(SummonedCompanion.Count(player) == 4, "Gear fixture starts with four living pack members");
                ItemData twin = new ItemData { id = "companion-build-" + Guid.NewGuid().ToString("N"), name = "Twin fixture", slot = ItemSlot.Relic,
                    level = 1, rarity = Rarity.Epic, health = 200, mechanic = EquipmentMechanic.TwinSummonResonance };
                p.Profile.inventory.Add(twin);
                check(p.Equip(twin.id) && SummonedCompanion.Count(player) == 2 && wolf.IsAlive && spirit.IsAlive && !extraA.IsAlive && !extraB.IsAlive,
                    "Paused equipment commit immediately enforces the two-pet cap with existing eviction priority");
                check(spirit.Health == health && Read<float>(spirit, "cooldown") == 1.25f,
                    "Higher-health mechanic equipment cannot multiply current companion HP or reset attack recovery");
                check(p.SetSummonerRoute(SummonerRoute.Bonded,true)&&spirit.IsPermanent,
                    "Paused route change promotes the same timed Spirit without changing its health");
                player.Teleport(position);
                check(spirit.IsAlive && spirit.IsPermanent && spirit.Rank == 1 && wolf.IsAlive && runtime.Remaining(4) == skillCooldown,
                    "Immediate transfer before unpausing retains the just-bonded Spirit and skill cooldown");
                check(SummonedCompanion.CommandOpportunityRemaining(player) == 0, "New encounter still clears old command opportunities by existing policy");

                p.Profile.skillRanks[4]=3;p.Save();
                File.Copy(path, path + ".tmp"); ownsTemporary = true;
                float beforeFailure = spirit.Health;
                check(!p.RefundSkillRanks(true) && spirit.Rank == 3 && spirit.Health == beforeFailure,
                    "Blocked refund cannot publish a lower companion rank before its profile commit");
                File.Delete(path + ".tmp"); ownsTemporary = false;
                p.Profile.skillRanks[4] = 1; // Exercise transfer's defensive reconciliation without an event.
                player.Teleport(position);
                check(spirit.IsAlive && spirit.Rank == 1 && spirit.Health <= beforeFailure,
                    "Transfer independently reconciles stale permanent rank even if a direct profile edit emitted no change event");
            }
            finally
            {
                if (ownsTemporary && File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
                foreach (SummonedCompanion pet in SummonedCompanion.Snapshot(player)) pet.Dismiss();
                Set(p, "Profile", original); Set(p, "LastError", error);
                Field(typeof(PlayerController), "skillRuntime").SetValue(player, oldRuntime);
                Field(typeof(GameSession), "equipmentFingerprint").SetValue(game, fingerprint);
                player.RefreshStats(false); Set(player, "Health", oldHealth);
                player.transform.position = position; player.enabled = enabled; game.SetPaused(paused);
                File.WriteAllBytes(path, primary); File.WriteAllBytes(path + ".bak", backup);
                File.SetLastWriteTimeUtc(path, primaryTime); File.SetLastWriteTimeUtc(path + ".bak", backupTime);
            }
        }
        private static void CheckPower(SummonedCompanion spirit, ProgressionService p, int rank, Action<bool, string> check)
        {
            StatBlock stats = p.GetStats();
            check(Mathf.Abs(Read<float>(spirit, "damage") - stats.Damage * CompanionRules.RankPower(rank)) < .001f &&
                Mathf.Abs(spirit.MaxHealth - stats.MaxHealth * CompanionRules.HealthFraction(1, rank, false)) < .001f,
                "Living Spirit damage and maximum HP match the newly committed learned rank");
        }
        private static FieldInfo Field(Type type, string name)
        { return type.GetField(name, Hidden) ?? throw new MissingFieldException(type.FullName, name); }
        private static T Read<T>(object target, string name) { return (T)Field(target.GetType(), name).GetValue(target); }
        private static void Set(object target, string name, object value)
        { target.GetType().GetProperty(name).GetSetMethod(true).Invoke(target, new[] { value }); }
    }
}
