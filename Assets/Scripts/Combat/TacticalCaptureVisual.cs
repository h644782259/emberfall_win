using UnityEngine;
namespace Emberfall
{
    public sealed class TacticalCaptureVisual:MonoBehaviour
    {
        private LineRenderer completedRing;
        private enum OwnerMode { Generic, ChapterSeal, RoomSeal }
        private OwnerMode ownerMode;
        private PlayerController roomOwner;private RoomChainState roomRun;private object roomIdentity;
        private GameSession session;private LineRenderer[] segments;private LineRenderer contestedFlag,direction,identity;private Material material;private int epoch,sealIndex=-1;
        public static void Attach(GameObject objective,GameSession session){Create(objective,session,-1,OwnerMode.Generic);}
        public static void AttachChapter(GameObject objective,GameSession session,int index){Create(objective,session,index,OwnerMode.ChapterSeal);}
        public static void AttachRoomSeal(GameObject objective,GameSession session,int index){Create(objective,session,index,OwnerMode.RoomSeal);}
        private static void Create(GameObject objective,GameSession session,int index,OwnerMode mode)
        {
            var root=new GameObject("Segmented capture progress");root.transform.SetParent(objective.transform,false);
            var visual=root.AddComponent<TacticalCaptureVisual>();visual.session=session;visual.ownerMode=mode;visual.sealIndex=index;visual.epoch=session.Player.CombatEpoch;visual.material=CombatFx.NewGlow();visual.segments=new LineRenderer[12];
            if(mode==OwnerMode.RoomSeal){visual.roomOwner=session.Player;visual.roomRun=session.RoomChainRun;visual.roomIdentity=session.RoomChainRun==null?null:session.RoomChainRun.Room;}
            for(int i=0;i<12;i++)
            {
                var line=visual.Line("Capture segment "+i,3,.1f);
                for(int j=0;j<3;j++){float a=(i+(j*.4f+.1f))*Mathf.PI/6;line.SetPosition(j,new Vector3(Mathf.Sin(a)*2.6f,.1f,Mathf.Cos(a)*2.6f));}
                visual.segments[i]=line;
            }
            visual.completedRing=visual.Line("Completed capture halo",64,.13f);visual.completedRing.loop=true;
            for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;visual.completedRing.SetPosition(i,new Vector3(Mathf.Sin(a)*2.6f,.12f,Mathf.Cos(a)*2.6f));}
            visual.completedRing.startColor=visual.completedRing.endColor=new Color(.3f,1,.65f,1);visual.completedRing.enabled=false;
            visual.contestedFlag=visual.Line("Capture contested flag",4,.12f);
            Vector3[] flag={new Vector3(-.28f,.12f,-2.85f),new Vector3(0,.12f,-3.15f),new Vector3(.28f,.12f,-2.85f),new Vector3(0,.12f,-3.15f)};
            for(int i=0;i<4;i++)visual.contestedFlag.SetPosition(i,flag[i]);
            visual.direction=visual.Line("Capture next direction",3,.09f);
            if(index>=0)
            {
                // Ground-plane letters remain attached to the indexed physical ring.
                Vector3[] glyph=index==0?new[]{new Vector3(-.3f,.13f,-.35f),new Vector3(0,.13f,.4f),new Vector3(.3f,.13f,-.35f),new Vector3(.17f,.13f,-.02f),new Vector3(-.17f,.13f,-.02f)}:
                    new[]{new Vector3(-.25f,.13f,-.35f),new Vector3(-.25f,.13f,.4f),new Vector3(.14f,.13f,.4f),new Vector3(.3f,.13f,.22f),new Vector3(.14f,.13f,.04f),new Vector3(-.25f,.13f,.04f),new Vector3(.14f,.13f,.04f),new Vector3(.3f,.13f,-.16f),new Vector3(.14f,.13f,-.35f),new Vector3(-.25f,.13f,-.35f)};
                visual.identity=visual.Line(index==0?"Seal A":"Seal B",glyph.Length,.07f);
                for(int i=0;i<glyph.Length;i++)visual.identity.SetPosition(i,glyph[i]);
                visual.identity.startColor=visual.identity.endColor=new Color(.8f,.9f,.85f);
            }
            visual.Refresh();
        }
        private LineRenderer Line(string name,int points,float width)
        {var go=new GameObject(name);go.transform.SetParent(transform,false);var line=go.AddComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=false;line.positionCount=points;line.widthMultiplier=width;return line;}
        private void LateUpdate(){Refresh();}
        private void Refresh()
        {
            float fraction=0;bool contested=false,complete=false;
            bool valid=session!=null&&session.Player!=null&&session.Player.CombatEpoch==epoch;
            if(ownerMode==OwnerMode.RoomSeal)valid=valid&&roomIdentity!=null&&object.ReferenceEquals(session.Player,roomOwner)&&object.ReferenceEquals(session.RoomChainRun,roomRun)&&object.ReferenceEquals(session.RoomChainRun.Room,roomIdentity);
            bool active=valid&&(ownerMode==OwnerMode.RoomSeal?session.TryGetRoomSeal(sealIndex,out fraction,out contested,out complete):ownerMode==OwnerMode.ChapterSeal?session.TryGetChapterSeal(sealIndex,out fraction,out contested,out complete):session.TryGetTacticalCapture(out fraction,out contested));
            if(valid&&ownerMode==OwnerMode.Generic&&(session.RoomChainRun!=null&&session.RoomChainRun.DoorUnlocked||session.ChapterActive&&session.ChapterRun.DoorUnlocked)){active=true;complete=true;}
            if(!active){Hide();return;}
            if(identity!=null)identity.enabled=true;
            completedRing.enabled=complete;
            for(int i=0;i<segments.Length;i++)
            {
                segments[i].enabled=!complete;
                Color tint=complete?new Color(.16f,.25f,.23f):fraction*12>=i+1?(contested?new Color(1,.45f,.18f):new Color(.3f,1,.65f)):new Color(.22f,.28f,.32f);
                segments[i].startColor=segments[i].endColor=tint;
            }
            // Contention at zero progress still has an explicit visible flag.
            contestedFlag.enabled=contested&&!complete;contestedFlag.startColor=contestedFlag.endColor=new Color(1,.4f,.18f);
            direction.enabled=sealIndex>=0&&complete;
            if(direction.enabled)
            {
                Vector3 delta=CombatFx.Flat((ownerMode==OwnerMode.RoomSeal?session.RoomNextObjectivePoint:session.ChapterNextObjectivePoint)-transform.position);
                if(delta.sqrMagnitude<.01f){direction.enabled=false;return;}
                Vector3 forward=delta.normalized,right=Vector3.Cross(Vector3.up,forward),tip=forward*3.05f+Vector3.up*.12f;
                direction.SetPosition(0,tip-forward*.45f+right*.28f);direction.SetPosition(1,tip);direction.SetPosition(2,tip-forward*.45f-right*.28f);
                direction.startColor=direction.endColor=new Color(.35f,.7f,.75f);
            }
        }
        private void Hide(){if(completedRing!=null)completedRing.enabled=false;if(identity!=null)identity.enabled=false;if(segments!=null)foreach(var line in segments)if(line!=null)line.enabled=false;if(contestedFlag!=null)contestedFlag.enabled=false;if(direction!=null)direction.enabled=false;}
        private void OnDisable(){Hide();}
        private void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
