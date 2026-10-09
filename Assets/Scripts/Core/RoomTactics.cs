using System;
namespace Emberfall
{
    public enum RoomObjective { Purify, Hunt, Escape, Rest, Boss }
    // Explicit integer mixing keeps plans independent of Unity's global RNG and runtime Random implementation.
    public static class RoomTactics
    {
        public static int Positive(int seed) { return (int)((uint)seed & 0x7fffffff); }
        public static int Opening(int seed) { return Positive(seed) % 3; }
        public static int Terrain(int seed, int room) { return (Positive(seed) / 3 % 3 + room) % 3; }
        public static int NextSeed(int seed, int previous, int beforePrevious=-1)
        {
            int value = Positive(seed);
            for (int i = 0; i < 9; i++)
            {
                if ((previous < 0 || (Opening(value) != Opening(previous) && Terrain(value,0) != Terrain(previous,0))) &&
                    (beforePrevious < 0 || (Opening(value) != Opening(beforePrevious) && Terrain(value,0) != Terrain(beforePrevious,0)))) return value;
                value = value == int.MaxValue ? 0 : value + 1;
            }
            return value;
        }
        public static RoomObjective Objective(int seed, int room)
        { return room == 3 ? RoomObjective.Hunt : room == 4 ? RoomObjective.Boss : (RoomObjective)((Opening(seed) + room) % 3); }
        public static int Mirror(int seed) { return Positive(seed) / 9 % 2 == 0 ? 1 : -1; }
        public static int EventRoom(int seed) { return Positive(seed) / 18 % 3; }
        public static string Name(RoomObjective objective)
        { return new[] { "双印净化", "截断供能", "北门突围", "星泉休憩", "王座封印" }[(int)objective]; }
    }
}
