using System;
using System.Collections.Generic;
using Emberfall;

namespace UnityEngine
{
    public struct Color { public Color(float r, float g, float b, float a = 1) { } }
}

public static class SkillRuntimeTests
{
    private static int assertions;
    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new Exception("FAILED: " + message);
    }
    private static bool Near(float a, float b) { return Math.Abs(a - b) < .001f; }

    private static void RejectsArgument(Action operation, string message)
    {
        try { operation(); }
        catch (ArgumentOutOfRangeException) { Check(true, message); return; }
        Check(false, message);
    }

    public static string Run()
    {
        assertions = 0;
        var state = new SkillRuntime(HeroClass.Vanguard);
        Check(Near(state.Energy, 100), "new adventure starts with full energy");
        Check(state.TryConsume(9, 1), "learned ultimate can cast when ready");
        Check(Near(state.Energy, 100) && Near(state.Remaining(9), 42), "Vanguard ultimate is free and starts a 42-second cooldown");
        Check(state.TryConsume(7, 1), "high-tier skill remains usable after a free ultimate");
        Check(state.Remaining(7) > 0 && Near(state.Energy, 62), "other skills still spend their normal energy");
        Check(state.TryConsume(0, 1) && Near(state.Energy, 50), "short-cooldown skill remains usable after ultimate");
        state.RestoreEnergy(1000);
        Check(Near(state.Energy, 100) && !state.TryConsume(0, 1), "energy restoration does not reset skill cooldown");

        var profile = new GameProfile();
        profile.equippedSkills[0] = 9;
        profile.equippedSkills[GameBalance.HotbarSize] = 9;
        profile.hotbarPage = 1;
        profile.hotbarKeys[0] = 113;
        int mappedSkill = profile.equippedSkills[profile.hotbarPage * GameBalance.HotbarSize];
        Check(!state.TryConsume(mappedSkill, 1), "page changes and key rebinding cannot bypass cooldown by skill identity");
        state.Advance(5);
        Check(Near(state.Remaining(0), 0) && Near(state.Remaining(9), 37), "low-tier becomes ready while ultimate still cools");
        Check(state.TryConsume(0, 1), "low-tier reusable during ultimate downtime");
        state.FillEnergy();
        Check(Near(state.Remaining(9), 37), "filling energy keeps cooldown");
        state.Advance(37);
        Check(state.TryConsume(9, 3), "rank-three ultimate usable after full cooldown");
        Check(Near(state.Remaining(9), 36.12f), "rank upgrades reduce cooldown without removing tradeoff");
        Check(Near(state.Energy, 100), "repeated ultimate casts never consume energy");
        var regeneration = new SkillRuntime(HeroClass.Vanguard);
        Check(regeneration.TryConsume(0, 1) && Near(regeneration.Energy, 88), "ordinary skill still costs energy");
        regeneration.Advance(2);
        Check(Near(regeneration.Energy, 96), "passive regeneration is four energy per second");
        regeneration.RestoreEnergy(8);
        Check(Near(regeneration.Energy, 100), "normal attack hit rewards eight energy");

        float energyBefore = state.Energy;
        float cooldownBefore = state.Remaining(9);
        state.Advance(0); state.Advance(-2); state.Advance(float.NaN); state.Advance(float.PositiveInfinity);
        state.RestoreEnergy(-10); state.RestoreEnergy(float.NaN); state.RestoreEnergy(float.PositiveInfinity);
        Check(Near(state.Energy, energyBefore) && Near(state.Remaining(9), cooldownBefore), "paused or invalid deltas cannot alter combat resources");
        Check(!state.TryConsume(-1, 1) && !state.TryConsume(10, 1) && !state.TryConsume(2, 0) && !state.TryConsume(2, 4), "invalid and unlearned casts rejected");
        Check(!state.TryConsume(3, 3) && !state.TryConsume(8, 3), "passives never cast or consume resources");
        Check(Near(state.Remaining(-1), 0), "empty hotbar slot is safe");
        state.Advance(10000);
        Check(Near(state.Energy, 100) && Near(state.Remaining(9), 0), "large legitimate elapsed time clamps both resources");

        Check((int)HeroClass.Vanguard == 0 && (int)HeroClass.Arcanist == 1 && (int)HeroClass.Ranger == 2 && (int)HeroClass.Summoner == 3, "adding Summoner preserves all existing serialized class identities");
        Check(GameBalance.ClassNames.Length == 4 && GameBalance.ClassColors.Length == 4 && GameBalance.ClassDescriptions.Length == 4, "all four classes have selection labels, descriptions and colors");
        for (int hero = 0; hero < GameBalance.ClassNames.Length; hero++)
        {
            var names = new HashSet<string>();
            for (int skill = 0; skill < GameBalance.SkillCount; skill++)
            {
                string name = GameBalance.SkillName((HeroClass)hero, skill);
                Check(!string.IsNullOrWhiteSpace(name) && names.Add(name), "class " + hero + " unique skill " + skill);
                Check(!string.IsNullOrWhiteSpace(GameBalance.SkillDescription((HeroClass)hero, skill)), "skill has a readable effect description");
                for (int rank = 1; rank <= 3; rank++)
                    Check(!string.IsNullOrWhiteSpace(GameBalance.SkillEvolution((HeroClass)hero, skill, rank)), "each class has a readable evolution at every rank");
            }
        }
        // IDs are stable save/hotbar identities. Branches can unlock in a different
        // order, so progression must be checked along prerequisite edges instead.
        Check(GameBalance.SkillPrerequisites.Length == GameBalance.SkillCount, "every skill has a prerequisite entry");
        int roots = 0, activeCount = 0, passiveCount = 0;
        for (int skill = 0; skill < GameBalance.SkillCount; skill++)
        {
            int[] parents = GameBalance.SkillPrerequisites[skill];
            Check(parents != null, "prerequisite entry is present for skill " + skill);
            if (parents.Length == 0) roots++;
            var uniqueParents = new HashSet<int>();
            foreach (int parent in parents)
            {
                Check(parent >= 0 && parent < GameBalance.SkillCount && parent != skill && uniqueParents.Add(parent), "prerequisite is valid, distinct and not self-referential");
                Check(GameBalance.SkillRequiredLevels[parent] < GameBalance.SkillRequiredLevels[skill], "prerequisite unlocks before its child " + parent + " -> " + skill);
                Check(GameBalance.SkillTreeRow(parent) < GameBalance.SkillTreeRow(skill), "tree connector flows down to its child");
            }
            Check(GameBalance.SkillRankRequiredLevel(skill, 1) == GameBalance.SkillRequiredLevels[skill], "initial rank uses its own branch unlock level");
            Check(GameBalance.SkillRankRequiredLevel(skill, 2) == Math.Max(2,GameBalance.SkillRequiredLevels[skill])+8 && GameBalance.SkillRankRequiredLevel(skill,3) == Math.Max(2,GameBalance.SkillRequiredLevels[skill])+18, "rank evolution gated by character level");
            if (GameBalance.IsPassive(skill))
            {
                passiveCount++;
                for (int hero = 0; hero < GameBalance.ClassNames.Length; hero++)
                    Check(Near(GameBalance.SkillEnergyCost((HeroClass)hero, skill), 0) && Near(GameBalance.SkillCooldown((HeroClass)hero, skill), 0), "passive has no manual cast budget for any class");
            }
            else activeCount++;
        }
        Check(roots == 1 && GameBalance.SkillPrerequisites[0].Length == 0, "all branches share the starting skill");
        Check(activeCount == 8 && passiveCount == 2, "each class has eight active skills and two passives");

        var reachable = new HashSet<int> { 0 };
        for (int pass = 0; pass < GameBalance.SkillCount; pass++)
            for (int skill = 0; skill < GameBalance.SkillCount; skill++)
            {
                bool allParentsAvailable = true;
                foreach (int parent in GameBalance.SkillPrerequisites[skill])
                    if (!reachable.Contains(parent)) allParentsAvailable = false;
                if (allParentsAvailable) reachable.Add(skill);
            }
        Check(reachable.Count == GameBalance.SkillCount, "all skills are reachable with no prerequisite cycle or stranded branch");

        for (int hero = 0; hero < GameBalance.ClassNames.Length; hero++)
        for (int skill = 0; skill < GameBalance.SkillCount; skill++)
        {
            if (GameBalance.IsPassive(skill)) continue;
            HeroClass heroClass = (HeroClass)hero;
            Check(skill == 9 ? Near(GameBalance.SkillEnergyCost(heroClass, skill), 0) : GameBalance.SkillEnergyCost(heroClass, skill) > 0 && GameBalance.SkillEnergyCost(heroClass, skill) <= SkillRuntime.MaximumEnergy, "ultimate is free while other active skills have reachable costs");
            Check(skill==SkillStockRules.Skill(heroClass)||GameBalance.EffectiveCooldown(heroClass, skill, 3) < GameBalance.EffectiveCooldown(heroClass, skill, 2) &&
                GameBalance.EffectiveCooldown(heroClass, skill, 2) < GameBalance.EffectiveCooldown(heroClass, skill, 1), "upgrading the same active skill reduces its cooldown");
            for (int rank = 1; rank <= 3; rank++)
            {
                var cast = new SkillRuntime(heroClass);
                float expected = GameBalance.EffectiveCooldown(heroClass, skill, rank);
                if(skill==SkillStockRules.Skill(heroClass))
                {
                    Check(cast.TryConsume(skill,rank)&&cast.Charges(skill)==1,"first stored cast consumes one charge");
                    Check(!cast.TryConsume(skill,rank),"duplicate same-frame stored cast blocked");
                    cast.Advance(.7f);cast.FillEnergy();Check(cast.TryConsume(skill,rank)&&cast.Charges(skill)==0,"second stored cast remains available");
                    Check(Near(cast.RechargeRemaining(skill),expected-.7f),"second cast preserves recharge clock");
                    continue;
                }
                Check(cast.TryConsume(skill, rank), "active skill can cast at rank " + rank);
                Check(cast.HeroClass == heroClass && Near(cast.Remaining(skill), expected) && Near(cast.Energy, 100 - GameBalance.SkillEnergyCost(heroClass, skill)), "cast uses its immutable class, selected rank cooldown and class resource cost");
                cast.FillEnergy();
                Check(!cast.TryConsume(skill, 3) && Near(cast.Remaining(skill), expected), "changing rank cannot reset an already-running cooldown");
                cast.Advance(expected - .01f);
                Check(!cast.TryConsume(skill, rank), "cast stays blocked until the whole cooldown elapses");
                cast.Advance(.02f);
                cast.FillEnergy();
                Check(cast.TryConsume(skill, rank), "cast becomes available after its cooldown elapses");
            }
        }
        CheckRoleBudgets();
        CheckCombatRotations();
        RejectsArgument(() => new SkillRuntime((HeroClass)(-1)), "negative class rejected when constructing combat state");
        RejectsArgument(() => new SkillRuntime((HeroClass)4), "unknown class cannot silently use another class budget");
        RejectsArgument(() => GameBalance.SkillCooldown(HeroClass.Vanguard, 10), "out-of-range skill budget rejected");
        RejectsArgument(() => GameBalance.SkillEnergyCost((HeroClass)4, 0), "unknown class resource budget rejected");
        Check(typeof(SkillRuntime).GetProperty("HeroClass").GetSetMethod(true) == null, "runtime class cannot be swapped to bypass per-skill cooldowns");
        int[] layout = GameBalance.DefaultLoadout();
        Check(layout.Length == 30, "three independent ten-slot pages");
        int[] active = { 0, 1, 2, 4, 5, 6, 7, 9 };
        for (int slot = 0; slot < 8; slot++) Check(layout[slot] == active[slot] && !GameBalance.IsPassive(layout[slot]), "first page maps eight active skills in stable catalog order");
        for (int slot = 8; slot < 30; slot++) Check(layout[slot] == -1, "remaining slots and extra pages initially empty");
        var keys = new HashSet<int>();
        foreach (int key in GameBalance.DefaultHotbarKeys) Check(GameBalance.IsBindableKey(key) && keys.Add(key), "default key is valid and unique");
        foreach (int key in new[] { 97, 100, 102, 104, 105, 106, 107, 115, 116, 119, 9, 27, 32, 91, 93 })
            Check(!GameBalance.IsBindableKey(key), "navigation and utility controls reserved");
        Check(GameBalance.KeyName(122) == "Z" && GameBalance.KeyName(49) == "1" && GameBalance.KeyName(293) == "F12", "custom key names readable");
        return "PASS: " + assertions + " skill combat, catalog, paging, and binding assertions.";
    }

    private static void CheckRoleBudgets()
    {
        // These are mechanic constraints, not a requirement for ever-longer
        // cooldowns on deeper tree nodes. They include the strongest rank.
        Check(GameBalance.SkillCooldown(HeroClass.Vanguard, 5) < GameBalance.SkillCooldown(HeroClass.Vanguard, 2), "lane dash cycles faster than the following persistent bladestorm");
        Check(GameBalance.SkillEnergyCost(HeroClass.Vanguard, 5) < GameBalance.SkillEnergyCost(HeroClass.Vanguard, 2), "narrow dash reserves energy for sustained area damage");
        Check(GameBalance.SkillCooldown(HeroClass.Arcanist, 4) < GameBalance.SkillCooldown(HeroClass.Arcanist, 1), "advanced chain lightning cycles faster than the earlier meteor burst");
        Check(GameBalance.SkillCooldown(HeroClass.Ranger, 4) < GameBalance.SkillCooldown(HeroClass.Ranger, 2), "retreat is available sooner than persistent arrow rain");
        Check(GameBalance.SkillCooldown(HeroClass.Summoner, 4) < GameBalance.SkillCooldown(HeroClass.Summoner, 2), "ranged spirit can be refreshed sooner than the earlier melee summon");
        foreach (HeroClass hero in (HeroClass[])Enum.GetValues(typeof(HeroClass)))
        {
            Check(GameBalance.EffectiveCooldown(hero, 6, 3) > 5.2f * 4, "strongest heal protection covers less than a quarter of its reuse cycle: " + hero);
            Check(Near(GameBalance.SkillEnergyCost(hero, 9), 0), "ultimate has no energy cost: " + hero);
            Check(GameBalance.EffectiveCooldown(hero, 9, 1) > 0, "free ultimate retains its cooldown: " + hero);
            var empty = new SkillRuntime(hero).CopyForClass(hero, 0, 0);
            Check(empty.TryConsume(9, 1) && Near(empty.Energy, 0), "ultimate casts with no energy: " + hero);
            Check(!empty.TryConsume(9, 1), "ultimate cannot bypass cooldown: " + hero);
            Check(!empty.TryConsume(0, 1), "ordinary skill cannot cast with no energy: " + hero);
        }
        Check(GameBalance.EffectiveCooldown(HeroClass.Vanguard, 4, 3) > 20 && GameBalance.EffectiveCooldown(HeroClass.Arcanist, 5, 3) > 20 && GameBalance.EffectiveCooldown(HeroClass.Summoner, 5, 3) > 20, "ten-second shields leave a longer exposed interval than their protected interval");
        Check(GameBalance.EffectiveCooldown(HeroClass.Arcanist, 0, 3) > 2.75f * 2, "double frost nova cannot permanently freeze one enemy by itself");
        Check(GameBalance.EffectiveCooldown(HeroClass.Vanguard, 7, 3) > 2.5f * 3, "multi-segment airborne control leaves recovery time");
        Check(GameBalance.EffectiveCooldown(HeroClass.Ranger, 4, 3) > 7f + 3f, "retreat's seven-second damage and mobility buff has a real gap");
        Check(GameBalance.EffectiveCooldown(HeroClass.Ranger, 5, 3) > 10.45f, "poison field plus lingering poison does not run indefinitely from one cast");
        Check(GameBalance.EffectiveCooldown(HeroClass.Summoner, 9, 3) > 16f * 2, "ancient guardian cannot have permanent uptime");
    }

    private static void CheckCombatRotations()
    {
        var warrior = new SkillRuntime(HeroClass.Vanguard);
        Check(warrior.TryConsume(5, 1) && warrior.TryConsume(1, 1) && warrior.TryConsume(2, 1), "warrior can engage, knock down and start a bladestorm");
        Check(Near(warrior.Energy, 22) && !warrior.TryConsume(4, 1), "aggressive warrior opening must earn energy before shielding");
        warrior.RestoreEnergy(8);
        Check(warrior.TryConsume(4, 1) && Near(warrior.Energy, 0), "one landed basic attack funds the emergency shield exactly");
        warrior.Advance(9);
        Check(warrior.TryConsume(1, 1) && !warrior.TryConsume(5, 1) && warrior.Remaining(2) > 0, "knockdown returns first while dash and persistent damage keep independent clocks");

        var mage = new SkillRuntime(HeroClass.Arcanist);
        Check(mage.TryConsume(4, 1) && mage.TryConsume(1, 1) && mage.TryConsume(2, 1) && Near(mage.Energy, 10), "mage can combine chain, meteor and storm at a substantial resource cost");
        Check(!mage.TryConsume(0, 1) && Near(mage.Remaining(0), 0), "offensive sequence temporarily postpones defensive nova without starting its clock");
        mage.RestoreEnergy(8);
        Check(mage.TryConsume(0, 1) && Near(mage.Energy, 0), "one landed attack restores exactly enough for the emergency nova");

        var ranger = new SkillRuntime(HeroClass.Ranger);
        Check(ranger.TryConsume(4, 3) && ranger.TryConsume(7, 3) && ranger.TryConsume(5, 3) && Near(ranger.Energy, 6), "ranger combines retreat buff, marked volley and poison with little energy remaining");
        ranger.Advance(7);
        Check(ranger.TryConsume(4, 3) && ranger.Charges(4)==0 && Near(ranger.Remaining(4), 7f), "second vault spends stored charge without resetting fourteen-second recovery");
        ranger.Advance(7.01f);
        Check(ranger.TryConsume(4, 3) && ranger.Remaining(5) > 0, "ranger regains mobility before both sustained offensive skills");

        var summoner = new SkillRuntime(HeroClass.Summoner);
        Check(summoner.TryConsume(2, 1) && summoner.TryConsume(4, 1) && summoner.TryConsume(1, 1) && Near(summoner.Energy, 14), "summoner can establish wolf, spirit and a control field");
        Check(summoner.TryConsume(9, 1) && Near(summoner.Energy, 14), "guardian can be summoned without spending energy");
        Check(!summoner.TryConsume(9, 1), "guardian still respects its cooldown");
        Check(!summoner.TryConsume(0, 1) && Near(summoner.Remaining(0), 0), "ready ordinary skill still respects the shared class energy pool");
    }
}
