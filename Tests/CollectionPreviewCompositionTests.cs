using System;
using Emberfall;
public static class CollectionPreviewCompositionTests
{
    public static string Run()
    {
        int n=0;Action<bool,string> check=(ok,why)=>{n++;if(!ok)throw new Exception(why);};
        foreach(bool mobile in new[]{false,true})foreach(float w in new[]{1,64,180,300,512,768,1200,4096,float.NaN,float.PositiveInfinity})
        foreach(float h in new[]{1,64,200,480,768,2400,float.NaN})
        {
            var s=new CollectionPreviewSurface(w,h,mobile);int cap=mobile?1024:1536;
            check(s.Width>=64&&s.Height>=64&&s.Width<=cap&&s.Height<=cap,"bounded physical surface");
            check(s.Width%16==0&&s.Height%16==0,"quantized native allocation");
            check(s.Samples==(mobile?2:4),"bounded requested sample count");
            check(s.Equals(new CollectionPreviewSurface(w,h,mobile)),"stable request does not require reallocation");
        }
        check(new CollectionPreviewSurface(301,402,true).Equals(new CollectionPreviewSurface(303,404,true)),"small within-bucket layout jitter keeps surface");
        check(!new CollectionPreviewSurface(301,402,true).Equals(new CollectionPreviewSurface(350,402,true)),"material resize changes surface key");
        foreach(float aspect in new[]{.4f,.8f,1,1.8f,3})foreach(var mode in new[]{CollectionPreviewComposition.Full,CollectionPreviewComposition.Weapon,CollectionPreviewComposition.Back})
        {
            float size=CollectionPreviewFraming.Size(1.7f,2.1f,.8f,aspect,mode);
            check(size*aspect>=1.7f&&size>=2.1f*.9903f+.8f*.1392f,"projected bounds fit each aspect and composition");
        }
        check(CollectionPreviewFraming.DefaultYaw(CollectionPreviewComposition.Back)==160&&CollectionPreviewFraming.DefaultYaw(CollectionPreviewComposition.Weapon)==20,"back and held-weapon default views differ");
        var motion=new CollectionPreviewMotion();int dirty=0;
        for(int frame=0;frame<7200;frame++)
        {
            if(motion.Advance(1f/60,frame))dirty++;
            check(!motion.Advance(1f/60,frame),"multiple GUI passes cannot advance same frame twice");
            check(motion.BreathScale>=.996f&&motion.BreathScale<=1.0041f&&motion.RingYaw>=0&&motion.RingYaw<360,"presentation motion remains bounded");
        }
        check(dirty>=2390&&dirty<=2410,"native render invalidation stays about20Hz over two minutes");
        var spike=new CollectionPreviewMotion();spike.Advance(20,0);check(spike.RingYaw<=1.201f,"resume spike is capped rather than snapping rotation");
        float prior=spike.RingYaw;spike.Advance(float.NaN,1);spike.Advance(-1,2);check(spike.RingYaw==prior,"invalid timing cannot poison model state");
        return "PASS: "+n+" collection surface/composition/motion rules (not rendered UI or native lifecycle)";
    }
}
