using System.Globalization;
namespace Emberfall
{
    // One snapshot per physical ring; never infer identity from aggregate Seals/Progress.
    public sealed class ChapterSealPresentation
    {
        public readonly float Seconds;
        public readonly bool Occupied, Contested, Complete;
        public readonly string Label;
        public ChapterSealPresentation(int index,float seconds,bool occupied,bool contested,bool complete,bool paused)
        {
            Seconds=seconds;Occupied=occupied;Complete=complete;Contested=!complete&&contested;
            Label=(occupied?"▶ ":"")+(index==0?"A":"B")+" · "+seconds.ToString("0.0",CultureInfo.InvariantCulture)+"/2秒 · "+
                (complete?"完成":Contested?"争夺":paused?"暂停":occupied?"累计中":"待占领");
        }
    }
}
