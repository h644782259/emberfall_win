using System;
namespace Emberfall
{
    public enum CollectionPreviewAction { Idle, Attack, Cast, Move }
    public enum CollectionPreviewComposition { Full, Weapon, Back }
    // Physical pixel request, quantized to avoid native texture churn on tiny GUI reflows.
    public readonly struct CollectionPreviewSurface : IEquatable<CollectionPreviewSurface>
    {
        public readonly int Width,Height,Samples;
        public CollectionPreviewSurface(float width,float height,bool mobile)
        {
            if(float.IsNaN(width)||float.IsInfinity(width)||width<=0)width=384;
            if(float.IsNaN(height)||float.IsInfinity(height)||height<=0)height=480;
            int cap=mobile?1024:1536;float scale=Math.Min(1,cap/Math.Max(width,height));
            Width=Math.Min(cap,Math.Max(64,(int)Math.Ceiling(width*scale/16)*16));
            Height=Math.Min(cap,Math.Max(64,(int)Math.Ceiling(height*scale/16)*16));Samples=mobile?2:4;
        }
        public bool Equals(CollectionPreviewSurface other){return Width==other.Width&&Height==other.Height&&Samples==other.Samples;}
    }
    public static class CollectionPreviewFraming
    {
        public static float DefaultYaw(CollectionPreviewComposition mode){return mode==CollectionPreviewComposition.Back?160:20;}
        public static float Size(float halfWidth,float halfHeight,float halfDepth,float aspect,CollectionPreviewComposition mode)
        {
            if(float.IsNaN(aspect)||float.IsInfinity(aspect)||aspect<=0)aspect=.8f;
            // Camera has an 8 degree downward pitch. Include projected depth.
            float vertical=Math.Abs(halfHeight)*.9903f+Math.Abs(halfDepth)*.1392f;
            return Math.Max(mode==CollectionPreviewComposition.Full?.8f:.4f,Math.Max(vertical,Math.Abs(halfWidth)/aspect)*1.16f);
        }
    }
    public sealed class CollectionPreviewMotion
    {
        private int frame=-1,bucket=-1;
        private float time,actionTime,orbitYaw;
        public float Time {get{return time;}}
        public float OrbitYaw {get{return orbitYaw;}}
        public CollectionPreviewAction Action {get;private set;}
        public float Progress {get{return Action==CollectionPreviewAction.Idle?1:Math.Min(1,actionTime/(Action==CollectionPreviewAction.Attack?.95f:Action==CollectionPreviewAction.Move?1.4f:1.25f));}}
        public void Play(CollectionPreviewAction value)
        {if((int)value<0||(int)value>3)return;Action=value;actionTime=0;}

        public float BreathScale {get{return 1f+.004f*(float)Math.Sin(time*Math.PI*.5);}}
        public float RingYaw {get{return time*12%360;}}
        public bool Advance(float dt,int currentFrame)
        {
            if(currentFrame==frame)return false;frame=currentFrame;
            if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0)return false;
            float step=Math.Min(dt,.1f);time=(time+step)%120;orbitYaw=(orbitYaw+step*16)%360;
            if(Action!=CollectionPreviewAction.Idle){actionTime+=step;if(Progress>=1){Action=CollectionPreviewAction.Idle;actionTime=0;}}

            int next=(int)(time*20);if(next==bucket)return false;bucket=next;return true;
        }
    }
}
