using System;
using UnityEngine;
namespace Emberfall
{
    internal sealed partial class LargeBossRig
    {
        private readonly GameObject[] powerChannels=new GameObject[3];
        private readonly Vector3[] shutdownPetalPositions=new Vector3[4],shutdownLegPositions=new Vector3[4];
        private readonly Quaternion[] shutdownPetalRotations=new Quaternion[4],shutdownLegRotations=new Quaternion[4];
        private Vector3 shutdownBody,shutdownCore;
        private Quaternion shutdownFirstRing,shutdownSecondRing;
        private bool shuttingDown;
        private void BuildPowerChannels(Func<Color,VisualSurface,Material> material)
        {
            var casing=material(new Color(.31f,.29f,.25f),VisualSurface.Metal);
            var energized=material(new Color(.17f,.70f,.78f),VisualSurface.Crystal);
            for(int i=0;i<3;i++)
            {
                var rail=Joint(body,"Anchor-indexed power rail "+i,Vector3.zero);rail.localRotation=Quaternion.Euler(0,i*120,0);
                Part(rail,"Metal power channel casing",PrimitiveType.Cube,new Vector3(0,.48f,.63f),new Vector3(.15f,.68f,.12f),casing);
                powerChannels[i]=Part(rail,"Live anchor channel "+i,PrimitiveType.Cube,new Vector3(0,.48f,.705f),new Vector3(.055f,.58f,.035f),energized).gameObject;
                powerChannels[i].SetActive(false);
            }
        }
        private void LateUpdate()
        {if(!shuttingDown)SetPowerAnchorMask(encounter!=null&&encounter.State!=null?encounter.State.LiveAnchorMask:0);}
        internal void SetPowerAnchorMask(int mask)
        {for(int i=0;i<3;i++)if(powerChannels[i]!=null)powerChannels[i].SetActive((mask&(1<<i))!=0);}
        internal void BeginShutdown()
        {
            shuttingDown=true;SetPowerAnchorMask(0);if(beamEmitter!=null)beamEmitter.gameObject.SetActive(false);
            shutdownBody=body.localPosition;shutdownCore=core.localScale;shutdownFirstRing=firstRing.localRotation;shutdownSecondRing=secondRing.localRotation;
            for(int i=0;i<4;i++){shutdownPetalPositions[i]=petals[i].localPosition;shutdownPetalRotations[i]=petals[i].localRotation;shutdownLegPositions[i]=legs[i].localPosition;shutdownLegRotations[i]=legs[i].localRotation;}
        }
        internal void SampleShutdown(float progress)
        {
            float t=Mathf.Clamp01(progress),drop=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.16f)/.42f)),spread=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.12f)/.66f));
            body.localPosition=shutdownBody+Vector3.down*(.75f*drop);
            core.localScale=shutdownCore*Mathf.Max(.03f,1-t*1.2f);
            firstRing.localRotation=shutdownFirstRing*Quaternion.Euler(28*drop,45*drop,0);
            secondRing.localRotation=shutdownSecondRing*Quaternion.Euler(-32*drop,-30*drop,18*drop);
            for(int i=0;i<4;i++)
            {
                Vector3 radial=Quaternion.Euler(0,i*90,0)*Vector3.forward;
                petals[i].localPosition=shutdownPetalPositions[i]+radial*(.42f*spread)+Vector3.down*(.32f*drop);
                petals[i].localRotation=shutdownPetalRotations[i]*Quaternion.Euler(-48*spread,0,0);
                legs[i].localPosition=shutdownLegPositions[i]+Vector3.down*(.13f*drop);
                legs[i].localRotation=shutdownLegRotations[i]*Quaternion.Euler(-22*drop,0,7*drop);
            }
        }
    }
    public sealed partial class CombatModel
    {
        public bool TryBeginLargeBossShutdown()
        {if(largeBossRig==null)return false;LargeBossShutdownVisual.Detach(this,largeBossRig,GameSession.Instance);return true;}
    }
}
