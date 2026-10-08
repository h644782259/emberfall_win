using System;
namespace Emberfall
{
    // Title-only sizing. Never change the shared combat or phone density.
    public sealed class MobileTitleLayout
    {
        public readonly float Zoom, X, Y;
        public static bool IsIPad(string model)
        { return !string.IsNullOrEmpty(model) && model.StartsWith("iPad", StringComparison.OrdinalIgnoreCase); }
        public MobileTitleLayout(float safeWidth, float safeHeight, bool ipad)
        {
            Zoom = ipad ? Math.Max(.1f, Math.Min(1.75f, Math.Min((safeWidth-48f)/528f, (safeHeight-48f)/313f))) : 1f;
            X = (safeWidth-528f*Zoom)*.5f;
            Y = (safeHeight-(ipad?313f:300f)*Zoom)*.5f;
        }
        public MobileControlLayout.Area Rect(float x,float y,float width,float height)
        { return new MobileControlLayout.Area(X+x*Zoom,Y+y*Zoom,width*Zoom,height*Zoom); }
    }
}
