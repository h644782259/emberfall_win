using System;
using System.Collections.Generic;
using Emberfall;

public static class EncounterPlanTests
{
    private static int assertions;

    public static string Run()
    {
        assertions = 0;
        var lowCounts = new HashSet<int>();
        var finalCounts = new HashSet<int>();
        var compositions = new HashSet<string>();
        double narrowDistance = 0, wideDistance = 0;
        int narrowMonsters = 0, wideMonsters = 0;
        for (int seed = 0; seed < 160; seed++)
        {
            foreach (int tier in new[] { 1, 3, 11, 1000 })
            {
                for (int wave = 1; wave <= EncounterPlan.FinalWave; wave++)
                {
                    for (int layout = 0; layout < 4; layout++)
                    {
                        List<EncounterSpawn> plan = EncounterPlan.Create(tier, wave, layout, seed);
                        List<EncounterSpawn> again = EncounterPlan.Create(tier, wave, layout, seed);
                        Check(Same(plan, again), "identical inputs reproduce exact spawn order, role, coordinates and boss flag");
                        int bosses = 0;
                        var types = new HashSet<EnemyKind>();
                        foreach (EncounterSpawn spawn in plan)
                        {
                            if (spawn.Boss) bosses++;
                            types.Add(spawn.Kind);
                            Check(Enum.IsDefined(typeof(EnemyKind), spawn.Kind), "all generated enemy roles are valid");
                            Check(!float.IsNaN(spawn.X) && !float.IsNaN(spawn.Z) && !float.IsInfinity(spawn.X) && !float.IsInfinity(spawn.Z), "all intended positions are finite");
                            Check(spawn.X * spawn.X + spawn.Z * spawn.Z <= EncounterPlan.SpawnRadius * EncounterPlan.SpawnRadius + .001f, "spawn lies within bounded dungeon arena");
                            Check(spawn.X * spawn.X + (spawn.Z + 12f) * (spawn.Z + 12f) >= EncounterPlan.EntranceExclusion * EncounterPlan.EntranceExclusion - .001f, "entrance arrival zone stays clear");
                            if (spawn.Boss) Check(spawn.Kind == EnemyKind.Guardian, "only guardian is marked as final boss");
                        }
                        for (int i = 0; i < plan.Count; i++)
                            for (int j = i + 1; j < plan.Count; j++)
                            {
                                float dx = plan[i].X - plan[j].X, dz = plan[i].Z - plan[j].Z;
                                float minimum = plan[i].Boss || plan[j].Boss ? 3.4f : EncounterPlan.MinimumSpacing;
                                Check(dx * dx + dz * dz >= minimum * minimum - .001f, "spawn positions do not stack and keep boss clearance");
                            }
                        Check(types.Count == 4, "each wave mixes all four enemy families");
                        if (wave < EncounterPlan.FinalWave)
                        {
                            Check(plan.Count >= 26 && plan.Count <= EncounterPlan.MaximumWavePopulation && bosses == 0, "ordinary waves scale within 26–32 total enemies, with bounded reinforcements");
                            Check(plan.Exists(value => value.Kind == EnemyKind.Guardian && !value.Boss), "ordinary mixed waves include a real non-boss guardian");
                        }
                        else
                        {
                            Check(plan.Count >= 15 && plan.Count <= 17 && bosses == 1, "final wave has exactly one boss and 14–16 escorts");
                            finalCounts.Add(plan.Count);
                        }
                    }
                }
            }
            List<EncounterSpawn> wide = EncounterPlan.Create(1, 1, 0, seed);
            List<EncounterSpawn> narrow = EncounterPlan.Create(1, 1, 1, seed);
            Check(wide.Count >= 26 && wide.Count <= 29, "first wide wave includes 26–29 enemies across reinforcement groups");
            lowCounts.Add(wide.Count);
            Check(!Same(wide, narrow), "environment layout changes the encounter");
            Check(!Same(wide, EncounterPlan.Create(1, 1, 0, seed + 1)), "changing seed changes positions or composition");
            Check(EncounterPlan.Create(11, 1, 0, seed).Count > wide.Count, "higher tiers measurably increase density within cap");
            int[] kinds = new int[4];
            foreach (EncounterSpawn spawn in wide) { wideDistance += Math.Abs(spawn.X); wideMonsters++; kinds[(int)spawn.Kind]++; }
            foreach (EncounterSpawn spawn in narrow) { narrowDistance += Math.Abs(spawn.X); narrowMonsters++; }
            compositions.Add(string.Join(",", kinds));
        }
        Check(lowCounts.Count == 4 && finalCounts.Count == 3, "seeded group budgets vary across their complete supported ranges");
        Check(compositions.Count >= 12, "tactical archetypes produce varied group compositions");
        Check(narrowDistance / narrowMonsters > wideDistance / wideMonsters + .5, "narrow environments emphasize separated side flanks rather than central clustering");
        Check(Same(EncounterPlan.Create(int.MinValue, int.MinValue, int.MinValue, int.MinValue), EncounterPlan.Create(1, 1, 0, int.MinValue)), "negative boundary input normalizes safely");
        Check(Same(EncounterPlan.Create(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue), EncounterPlan.Create(1000, EncounterPlan.FinalWave, 3, int.MaxValue)), "positive boundary input is capped without overflow");
        return "PASS: " + assertions + " seeded encounter budget, composition, spacing and environment assertions.";
    }

    private static bool Same(List<EncounterSpawn> first, List<EncounterSpawn> second)
    {
        if (first.Count != second.Count) return false;
        for (int i = 0; i < first.Count; i++)
            if (first[i].Kind != second[i].Kind || first[i].Boss != second[i].Boss || first[i].X != second[i].X || first[i].Z != second[i].Z) return false;
        return true;
    }

    private static void Check(bool value, string description)
    {
        assertions++;
        if (!value) throw new Exception("Encounter assertion " + assertions + " failed: " + description);
    }
}
