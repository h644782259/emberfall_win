using System;
namespace Emberfall
{
    public static class CombatTextLayout
    {
        public struct Box
        {
            public float X,Y,Width,Height;
            public Box(float x,float y,float width,float height){X=x;Y=y;Width=width;Height=height;}
            public bool Overlaps(Box other){return X<other.X+other.Width&&X+Width>other.X&&Y<other.Y+other.Height&&Y+Height>other.Y;}
        }
        public const int MechanismLimit=4, CandidateCount=12;
        public static float PixelHeight(float scale,float density,bool critical)
        {return 21f*Math.Max(1,Math.Min(1.8f,scale))*Math.Max(.25f,Math.Min(4,density))*(critical?1.28f*1.14f:1f);}
        public static Box Candidate(float x,float y,float width,float height,int slot)
        {
            float pad=Math.Max(4,height*.14f);width=Math.Max(1,width)+pad*2;height=Math.Max(1,height)+pad*2;
            // Above and to either side of the silhouette; spacing derives from
            // the complete measured glyph box, including the peak critical pop.
            float side=slot%2==0?-1:1;
            return slot==0?new Box(x-width*.5f,y+4,width,height):new Box(x+side*(width*.5f+pad)-width*.5f,y+4+((slot+1)/2)*(height+pad),width,height);
        }
        public static bool Fits(Box box,float width,float height)
        {return box.X>=0&&box.Y>=0&&box.X+box.Width<=width&&box.Y+box.Height<=height;}
    }
}
