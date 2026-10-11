using System;
using System.Collections.Generic;

namespace Emberfall
{
    public struct EncounterSpawn
    {
        public EnemyKind Kind;
        public float X, Z;
        public bool Boss;
    }

    /// <summary>Seeded, scene-independent dungeon population. Coordinates are intentions;
    /// the runtime resolves obstacles, navigation and the current player's safe radius.</summary>
    public static class EncounterPlan
    {
        public const int MaximumSimultaneous = 14;
        public const int MaximumWavePopulation = 32;
        public const int FinalWave = 5;
        public const float MinimumSpacing = 1.8f;
        public const float EntranceExclusion = 7f;
        public const float SpawnRadius = 24f;

        private struct Generator
        {
            private uint state;
            public Generator(int tier, int wave, int layout, int seed)
            {
                unchecked { state = (uint)seed ^ ((uint)tier * 2654435761u) ^ ((uint)wave * 2246822519u) ^ ((uint)layout * 3266489917u); }
                if (state == 0) state = 0xA341316Cu;
            }
            private uint NextUInt()
            {
                state ^= state << 13; state ^= state >> 17; state ^= state << 5;
                return state;
            }
            public int Next(int maximum) { return (int)(NextUInt() % (uint)maximum); }
            public float Range(float minimum, float maximum)
            {
                return minimum + (maximum - minimum) * ((NextUInt() & 0xFFFFFFu) / 16777216f);
            }
        }

        public static List<EncounterSpawn> Create(int tier, int wave, int layout, int seed)
        {
            tier = Math.Max(1, Math.Min(1000, tier));
            wave = Math.Max(1, Math.Min(FinalWave, wave));
            // Layout IDs are intentionally normalized: negative/large migrated values
            // select one of the authored environments without integer overflow.
            layout = ((layout % 4) + 4) % 4;
            var random = new Generator(tier, wave, layout, seed);
            int archetype = random.Next(4);
            bool narrow = (layout & 1) != 0;
            bool final = wave == FinalWave;
            int tierBonus = Math.Min(5, (tier - 1) / 2);
            int ordinary = final ? 14 + random.Next(3)
                : Math.Max(26, Math.Min(MaximumWavePopulation, 26 + random.Next(4) + tierBonus + (wave > 1 ? 1 : 0) - (narrow ? 1 : 0)));
            var result = new List<EncounterSpawn>(ordinary + (final ? 1 : 0));
            if (final)
            {
                float bossX = random.Range(-2.2f, 2.2f), bossZ = random.Range(15.2f, 18.3f);
                result.Add(new EncounterSpawn { Kind = EnemyKind.Guardian, X = bossX, Z = bossZ, Boss = true });
            }
            int group = 0, inGroup = 0, groupSize = 0;
            int mirror = random.Next(2) == 0 ? -1 : 1;
            for (int index = 0; index < ordinary; index++)
            {
                if (inGroup >= groupSize)
                {
                    group++;
                    groupSize = narrow ? 1 + random.Next(2) : 2 + random.Next(3);
                    inGroup = 0;
                }
                float anchorX, anchorZ;
                Anchor(archetype, group - 1, narrow, final, mirror, out anchorX, out anchorZ);
                anchorX*=1.65f;anchorZ*=1.65f;
                EnemyKind kind;
                if (final && index < 3) kind = new[] { EnemyKind.Goblin, EnemyKind.Wisp, EnemyKind.Slime }[index];
                else if (!final && index < 4) kind = new[] { EnemyKind.Guardian, EnemyKind.Wisp, EnemyKind.Goblin, EnemyKind.Slime }[index];
                else kind = RollKind(archetype, ref random);
                float x = 0, z = 0;
                bool found = false;
                float spread = narrow ? 2.2f : 4.0f;
                for (int attempt = 0; attempt < 64; attempt++)
                {
                    x = anchorX + random.Range(-spread, spread);
                    z = anchorZ + random.Range(-spread, spread);
                    if (Safe(x, z, result)) { found = true; break; }
                }
                if (!found)
                {
                    // A finite lattice is a deterministic safety net, not a discarded
                    // monster or an overlap. Its 2.25m cells exceed our separation bound.
                    int offset = random.Next(221);
                    for (int cell = 0; cell < 221; cell++)
                    {
                        int candidate = (cell + offset) % 221;
                        x = (candidate % 17 - 8) * 2.5f;
                        z = -8f + (candidate / 17) * 2.5f;
                        if (Safe(x, z, result)) { found = true; break; }
                    }
                }
                if (!found) throw new InvalidOperationException("Encounter placement safety lattice exhausted.");
                result.Add(new EncounterSpawn { Kind = kind, X = x, Z = z });
                inGroup++;
            }
            return result;
        }

        private static EnemyKind RollKind(int archetype, ref Generator random)
        {
            int roll = random.Next(100);
            // Shield battery, fast pincer, ranged crossfire, and heavy siege differ
            // in composition as well as pack shapes, while every wave stays mixed.
            if (archetype == 0) return roll < 35 ? EnemyKind.Guardian : roll < 70 ? EnemyKind.Wisp : roll < 85 ? EnemyKind.Goblin : EnemyKind.Slime;
            if (archetype == 1) return roll < 50 ? EnemyKind.Goblin : roll < 75 ? EnemyKind.Slime : roll < 90 ? EnemyKind.Wisp : EnemyKind.Guardian;
            if (archetype == 2) return roll < 50 ? EnemyKind.Wisp : roll < 70 ? EnemyKind.Slime : roll < 90 ? EnemyKind.Goblin : EnemyKind.Guardian;
            return roll < 45 ? EnemyKind.Guardian : roll < 75 ? EnemyKind.Slime : roll < 90 ? EnemyKind.Goblin : EnemyKind.Wisp;
        }

        private static void Anchor(int archetype, int group, bool narrow, bool final, int mirror, out float x, out float z)
        {
            if (narrow)
            {
                // Short, alternating side groups leave the center lane readable.
                x = ((group & 1) == 0 ? -8f : 8f) * mirror;
                z = (group / 2 % 3) * 4.2f - .4f;
                if (final) z += 1.2f;
                return;
            }
            int point = group % 4;
            if (archetype == 0) { x = new[] { -7f, 7f, 0f, -10f }[point]; z = new[] { 7.5f, 7.5f, 2.5f, .5f }[point]; }
            else if (archetype == 1) { x = new[] { -10f, 10f, 0f, 0f }[point]; z = new[] { .8f, .8f, 8.5f, 2.5f }[point]; }
            else if (archetype == 2) { x = new[] { -9f, 9f, -4f, 4f }[point]; z = new[] { 7f, 7f, .5f, .5f }[point]; }
            else { x = new[] { -4.5f, 4.5f, 0f, -10f }[point]; z = new[] { 4f, 4f, 10f, 1f }[point]; }
            x *= mirror;
            if (final) z += .5f;
        }

        private static bool Safe(float x, float z, List<EncounterSpawn> previous)
        {
            if (x * x + z * z > SpawnRadius * SpawnRadius) return false;
            if (x * x + (z + 12f) * (z + 12f) < EntranceExclusion * EntranceExclusion) return false;
            foreach (EncounterSpawn spawn in previous)
            {
                float dx = x - spawn.X, dz = z - spawn.Z;
                float spacing = spawn.Boss ? 3.4f : MinimumSpacing;
                if (dx * dx + dz * dz < spacing * spacing) return false;
            }
            return true;
        }
    }
}
