using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameSession
    {
        private int previousRoomSeed=-1, beforePreviousRoomSeed=-1;
        private readonly DeferredRoomChoice pendingRoomChoice=new DeferredRoomChoice();
        private bool RoomCaptureActive {get{return RoomChainRun!=null&&!RoomChainRun.Finished&&!RoomChainRun.DoorUnlocked&&roomObjectiveMarker!=null;}}
        private bool RoomPurifyLive {get{return RoomChainRun!=null&&!RoomChainRun.Finished&&RoomChainRun.Room.Objective==RoomObjective.Purify;}}
        private Vector3 RoomSealPoint(int index){int side=RoomTactics.Mirror(RoomRunSeed);return index==0?new Vector3(-side*8,0,-6):new Vector3(side*8,0,9);}
        private bool RoomSealInside(int index){return Player!=null&&RoomTacticalRegion.ContainsPlayer(CombatFx.Flat(Player.transform.position-RoomSealPoint(index)).sqrMagnitude);}
        private bool IsRoomContestingPoint(EnemyController enemy,Vector3 point)
        {return LiveRoomEnemy(enemy)&&RoomTacticalRegion.Contests(CombatFx.Flat(enemy.transform.position-point).sqrMagnitude,enemy.NavigationRadius,true)&&WorldTraversal.HasLineOfSight(enemy.transform.position,point);}
        private bool RoomSealContested(int index)
        {if(!RoomPurifyLive||RoomChainRun.SealComplete(index))return false;foreach(var enemy in Enemies)if(IsRoomContestingPoint(enemy,RoomSealPoint(index)))return true;return false;}
        public bool TryGetRoomSeal(int index,out float fraction,out bool contested,out bool complete)
        {fraction=0;contested=complete=false;if(!RoomPurifyLive||index<0||index>=2)return false;fraction=RoomChainRun.SealProgress(index)/2f;complete=RoomChainRun.SealComplete(index);contested=RoomSealContested(index);return true;}
        public ChapterSealPresentation RoomSealView(int index)
        {float fraction;bool contested,complete;if(!TryGetRoomSeal(index,out fraction,out contested,out complete))return null;return new ChapterSealPresentation(index,RoomChainRun.SealProgress(index),RoomSealInside(index),contested,complete,InputBlocked);}
        public Vector3 RoomNextObjectivePoint
        {get{if(!RoomPurifyLive)return new Vector3(0,0,14);int best=-1;float distance=float.PositiveInfinity;for(int i=0;i<2;i++)if(!RoomChainRun.SealComplete(i)){float d=Player==null?i:(Player.transform.position-RoomSealPoint(i)).sqrMagnitude;if(d<distance){distance=d;best=i;}}return best<0?new Vector3(0,0,14):RoomSealPoint(best);}}
        public bool RoomCaptureInside {get{return RoomCaptureActive&&Player!=null&&(RoomPurifyLive?(!RoomChainRun.SealComplete(0)&&RoomSealInside(0)||!RoomChainRun.SealComplete(1)&&RoomSealInside(1)):RoomTacticalRegion.ContainsPlayer(CombatFx.Flat(Player.transform.position-RoomObjectivePoint).sqrMagnitude));}}
        public bool RoomCaptureContested {get{return RoomContestantCount>0;}}
        public int RoomContestantCount
        {get{int count=0;if(RoomCaptureActive)foreach(var enemy in Enemies)if(IsRoomContesting(enemy))count++;return count;}}
        public bool IsRoomContesting(EnemyController enemy)
        {
            if(!RoomCaptureActive)return false;
            if(RoomPurifyLive)return (!RoomChainRun.SealComplete(0)&&RoomSealInside(0)&&IsRoomContestingPoint(enemy,RoomSealPoint(0)))||(!RoomChainRun.SealComplete(1)&&RoomSealInside(1)&&IsRoomContestingPoint(enemy,RoomSealPoint(1)));
            return LiveRoomEnemy(enemy)&&RoomTacticalRegion.Contests(
                CombatFx.Flat(enemy.transform.position-RoomObjectivePoint).sqrMagnitude,enemy.NavigationRadius,true)&&
                WorldTraversal.HasLineOfSight(enemy.transform.position,RoomObjectivePoint);
        }
        private static bool LiveRoomEnemy(EnemyController enemy)
        {return enemy!=null&&!enemy.IsDead&&enemy.isActiveAndEnabled&&enemy.gameObject.activeInHierarchy;}
        private GameObject roomObjectiveMarker;
        private readonly GameObject[] roomSealMarkers=new GameObject[2];
        private EnemyController roomSupplier;
        public bool RoomSupplyActive {get{return RoomChainRun!=null&&!RoomChainRun.Finished&&LiveRoomEnemy(roomSupplier);}}
        public bool IsRoomSupplier(EnemyController enemy){return RoomSupplyActive&&enemy==roomSupplier;}
        public int RoomRunSeed {get{return RoomChainRun==null?0:RoomChainRun.Room.Seed;}}
        private Vector3 RoomObjectivePoint
        {
            get
            {
                if(RoomChainRun.Room.Objective==RoomObjective.Escape)return new Vector3(0,0,11);
                return RoomNextObjectivePoint;
            }
        }
        private string TacticalObjectiveStatus
        {
            get
            {
                var run=RoomChainRun;
                if(run.DoorUnlocked)return "北门已开 · 可撤离或留下获取击杀收益";
                if(run.Room.Interlude)return "选择星泉祝福";
                if(run.Room.Boss)return "击败首领与护卫";
                if(run.Room.Objective==RoomObjective.Hunt)return "击败金环供能魔灵 · "+RoomSupport.Hint;
                if(run.Room.Objective==RoomObjective.Purify)return "净化 "+run.Seals+"/2 · "+RoomSealView(0).Label+" · "+RoomSealView(1).Label+" · "+RoomSupport.Hint;
                return "突围 · "+"站入光环 "+run.Progress.ToString("0.0")+"/"+(run.Room.Objective==RoomObjective.Purify?"3":"4")+"秒 · "+(RoomCaptureContested?"争夺 "+RoomContestantCount+" 敌 · 进度保留":"进入2.4米金环累计")+" · "+RoomSupport.Hint;
            }
        }
        private void BuildRoomObjective()
        {
            roomSupplier=null;roomObjectiveMarker=null;roomSealMarkers[0]=roomSealMarkers[1]=null;
            RoomGenerationStage("北门路线");var plan=RoomChainRun.Room;
            if(plan.Interlude||plan.Boss)return;
            if(!WorldTraversal.IsWalkable(new Vector3(0,0,14),.65f)||!WorldTraversal.CanReach(TacticalRoomGeometry.Entrance,new Vector3(0,0,14),.65f))
            {FailRoomGeneration("北门路线不可达");return;}
            if(plan.Objective==RoomObjective.Hunt)return;
            RoomGenerationStage("目标路线");Vector3 first=RoomObjectivePoint;
            Vector3 second=new Vector3(RoomTactics.Mirror(RoomRunSeed)*8,0,9);
            if(!WorldTraversal.CanReach(TacticalRoomGeometry.Entrance,first,.65f)||!WorldTraversal.CanReach(first,second,.65f))
            {FailRoomGeneration("目标路线不可达");return;}
            if(plan.Objective==RoomObjective.Purify)
            {
                for(int i=0;i<2;i++)
                {
                    RoomGenerationStage("印记路线 "+(i+1));Vector3 point=RoomSealPoint(i);
                    if(!WorldTraversal.CanReach(TacticalRoomGeometry.Entrance,point,.65f)){FailRoomGeneration("印记路线不可达");return;}
                    RoomGenerationStage("印记模型与进度 "+(i+1));var marker=WorldBuilder.MakeRoomObjective(point,true);marker.transform.SetParent(world.transform,true);roomSealMarkers[i]=marker;TacticalCaptureVisual.AttachRoomSeal(marker,this,i);
                }
                roomObjectiveMarker=roomSealMarkers[0];return;
            }
            RoomGenerationStage("突围目标模型与进度");roomObjectiveMarker=WorldBuilder.MakeRoomObjective(first);
            roomObjectiveMarker.name="Room objective: stand within 2.4m";
            roomObjectiveMarker.transform.SetParent(world.transform,true);
            TacticalCaptureVisual.Attach(roomObjectiveMarker,this);
        }
        private void TickRoomTactics()
        {
            if(RoomChainRun==null||RoomChainRun.Finished||InputBlocked||Player==null)return;
            if(TryOpenPendingRoomChoice())return;
            if(!RoomChainRun.DoorUnlocked && roomObjectiveMarker!=null)
            {
                if(RoomPurifyLive)
                {for(int i=0;i<2;i++)RoomChainRun.AdvanceSeal(i,Time.deltaTime,true,RoomSealInside(i),RoomSealContested(i));}
                else
                {RoomChainRun.Advance(Time.deltaTime,true,RoomCaptureInside,RoomCaptureContested);roomObjectiveMarker.transform.position=RoomObjectivePoint;}
                if(RoomChainRun.DoorUnlocked){if(!RoomPurifyLive)roomObjectiveMarker.SetActive(false);OpenRoomGate();}
            }

        }
        public bool TryGetTacticalCapture(out float fraction,out bool contested)
        {
            fraction=0;contested=false;
            if(ChapterActive&&!ChapterFinished&&chapterObjective!=null&&!ChapterRun.DoorUnlocked)
            {float required=ChapterRun.Objective==RoomObjective.Purify?2f:2.5f;int total=ChapterRun.Objective==RoomObjective.Purify?2:1;fraction=(ChapterRun.Seals+ChapterRun.Progress/required)/total;foreach(var enemy in Enemies)if(IsChapterContesting(enemy)){contested=true;break;}return true;}
            if(!RoomCaptureActive)return false;
            fraction=RoomChainRun.CaptureFraction;contested=RoomCaptureContested;return true;
        }
        public float RoomSupportMultiplier(EnemyController enemy)
        {
            if(PracticeActive)return PracticeSupportMultiplier(enemy);
            if(!RoomSupplyActive||!LiveRoomEnemy(enemy)||enemy==roomSupplier||enemy.IsBoss)return 1;
            return RoomTacticalRegion.ReceivesSupport(CombatFx.Flat(enemy.transform.position-roomSupplier.transform.position).sqrMagnitude,true)&&
                WorldTraversal.HasLineOfSight(enemy.transform.position,roomSupplier.transform.position) ? .7f : 1;
        }
        public RoomSupportSnapshot RoomSupport
        {
            get
            {
                int count=0;foreach(var enemy in Enemies)if(RoomSupportMultiplier(enemy)<1)count++;
                var target=Player==null?null:Player.AimTarget;
                RoomTargetSupport relation=!LiveRoomEnemy(target)?RoomTargetSupport.None:target==roomSupplier?RoomTargetSupport.Supplier:
                    target.IsBoss?RoomTargetSupport.Exempt:RoomSupportMultiplier(target)<1?RoomTargetSupport.Supported:RoomTargetSupport.Severed;
                return new RoomSupportSnapshot(RoomSupplyActive,count,relation);
            }
        }
        public RoomObjectivePresentation RoomObjectiveView
        {get{return RoomObjectivePresentation.Create(RoomChainRun,RoomCaptureInside,RoomContestantCount,InputBlocked,RoomSupport);}}
        private bool TryOpenPendingRoomChoice()
        {
            bool valid=HasStarted&&InDungeon&&!IsDead&&Player!=null&&RoomChainRun!=null&&!RoomChainRun.Finished&&
                RoomChainRun.Room.Index==0&&RoomChainRun.DoorUnlocked&&RunChoices.CompletedWave==0;
            if(!pendingRoomChoice.TryClaim(RoomChainRun==null?null:RoomChainRun.Room,Player==null?-1:Player.CombatEpoch,
                Time.frameCount,valid,!InputBlocked))return false;
            RunChoices.PrepareRoomChoice(1,Progression.Profile,MobileControls.Active,runSeed);
            UpdateTimeScale();return RunChoices.AwaitingChoice;
        }
        private void OpenRoomGate()
        {
            if(RoomChainRun!=null&&RoomChainRun.Room.Index==0&&RunChoices.CompletedWave==0&&Player!=null)
                pendingRoomChoice.Request(RoomChainRun.Room,Player.CombatEpoch,Time.frameCount);
            if(roomExitMarker!=null)roomExitMarker.SetActive(true);
            Notify(pendingRoomChoice.Pending?"首房完成 · 选择本局打法":"目标完成 · 北门已开；撤离会放弃剩余敌人的击杀收益");
        }
    }
}
