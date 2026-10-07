using System;
using System.Collections.Generic;
using Emberfall;
public static class RoomFailureEvidenceTests
{
 static int checks;static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
 public static string Run()
 {
  foreach(RoomFailureReason reason in new[]{RoomFailureReason.Death,RoomFailureReason.Timeout,RoomFailureReason.Abandoned,RoomFailureReason.GenerationOrPathFailure})
  {
   var session=new GameSession();session.RoomChainRun=new RoomChainState();session.RecordIncomingDamage("魔灵弹幕",42);session.RecordActualHealing(10);session.RoomChainRun.Fail(reason);session.Build(false);
   var snapshot=session.LastRunRecap;var view=new RunRecapPresentation(snapshot);
   Check(snapshot.FailureReason==reason.ToString(),"actual session snapshot must preserve exact room failure reason");
   Check(snapshot.Evidence.Room==1&&snapshot.Evidence.DamageTaken==42&&snapshot.Evidence.HealingReceived==10,"actual damage/heal/room evidence survives snapshot");
   Check(view.HasDamage==(reason==RoomFailureReason.Death),"only death presents prior hit as lethal context");
   if(reason==RoomFailureReason.Abandoned)Check(view.Tip=="","abandonment gives no fabricated death advice");
   if(reason==RoomFailureReason.GenerationOrPathFailure)Check(view.Tip.Contains("重新进入")&&!view.Tip.Contains("闪避"),"path failure has reentry instruction");
   if(reason==RoomFailureReason.Timeout)Check(view.Tip.Contains("当前进度"),"timeout uses actual objective progress");
   session.RoomChainRun.Fail(RoomFailureReason.Abandoned);Check(session.RoomChainRun.Failure==reason,"terminal reason never overwritten by later cleanup");
  }
  for(int mask=1;mask<=3;mask++)
  {
   var generated=new GameSession();generated.runSeed=12345;generated.RoomChainRun=new RoomChainState(12345);generated.RoomChainRun.Fail(RoomFailureReason.GenerationOrPathFailure);
   generated.RoomGenerationFailureDetail="构建异常\n发生位置：印记模型与进度 2\n房间 3 · 守印";
   var evidenceOwner=new object();if((mask&1)!=0)generated.MechanismEvidence.Register(evidenceOwner,1,0);if((mask&2)!=0)generated.MechanismEvidence.Register(evidenceOwner,1,1);
   generated.Build(false);string detail=generated.RoomGenerationFailureDetail;generated.RoomGenerationFailureDetail=null;
   var presentation=new RunRecapPresentation(generated.LastRunRecap);
   Check(presentation.HasGenerationFailure&&presentation.Tip.StartsWith("构建异常")&&!presentation.Tip.Contains("下次"),"generation failure overrides unused ember and frost advice");
   Check(presentation.GenerationFailureDetails==detail&&generated.LastRunRecap.Seed==12345&&!presentation.Tip.Contains("种子")&&!presentation.GenerationFailureDetails.Contains("种子"),"actual recap retains readable generation details and separate seed after live reset");
  }
  var legacy=new GameSession();legacy.modeRewardDetailsUnavailable=true;legacy.Build(true);
  Check(legacy.LastRunRecap.RewardDetailsUnavailable&&new RunRecapPresentation(legacy.LastRunRecap).HasProgress,"real summary preserves visible unknown reward receipt state");
  Check(GameSession.RewardCardHeight(legacy.LastRunRecap)==88,"actual recap card reserves legacy-detail explanation height");
  var partial=new GameSession();partial.RoomChainRun=new RoomChainState(0);for(int i=0;i<6;i++)partial.RoomChainRun.Register(partial.RoomChainRun.Room,i);
  for(int i=0;i<6;i++)partial.RoomChainRun.AdvanceSeal(1,.25f,true,true,false);for(int i=0;i<2;i++)partial.RoomChainRun.AdvanceSeal(0,.25f,true,true,false);
  partial.RoomChainRun.Fail(RoomFailureReason.Abandoned);partial.Build(false);
  Check(partial.LastRunRecap.Evidence.Objective.Contains("A 0.5/3秒 · B 1.5/3秒"),"actual failed result preserves independent partial B-first evidence");
  var death=new GameSession();death.RoomChainRun=new RoomChainState();death.RecordIncomingDamage("近战",22);death.RecordActualHealing(float.NaN);death.RoomChainRun.Fail(RoomFailureReason.Death);death.Build(false);
  Check(new RunRecapPresentation(death.LastRunRecap).Tip.Contains("未记录有效治疗"),"no-healing advice requires real damage and zero valid healing");
  death.RecordActualHealing(8);death.Build(false);Check(!new RunRecapPresentation(death.LastRunRecap).Tip.Contains("未记录有效治疗"),"actual healing changes advice");
  var context=new object();var owner=new object();var a=new object();var b=new object();var side=new SideEventRun(context,owner,3,"test");side.Register(a);side.Register(b);
  Check(side.Contains(a,context,owner,3,true)&&side.Contains(b,context,owner,3,true)&&side.Remaining==2,"both registered crystal enemies are marked");
  side.Defeat(a,context,owner,3,true);Check(!side.Contains(a,context,owner,3,true)&&side.Contains(b,context,owner,3,true)&&side.Remaining==1,"only surviving crystal enemy retains 1 marker");
  Check(!side.Contains(b,new object(),owner,3,true)&&!side.Contains(b,context,owner,4,true)&&!side.Contains(b,context,owner,3,false),"stale context epoch or inactive run hides marker");
  side.Abandon();Check(!side.Contains(b,context,owner,3,true)&&side.Remaining==0,"abandon clears markers");
  return "PASS: "+checks+" production room failure snapshot/actual evidence/side-event membership assertions";
 }
}
namespace Emberfall
{
 public enum EquipmentMechanic { None }
 public enum RunBlessing { None }
 public class Profile {public int mechanicMaterials;public int heroClass;public bool pendingFashionChest,pendingFirstClearReward;}
 public class ProgressionService {public const int MechanicExchangeCost=12;public Profile Profile=new Profile();public bool HasMechanic(EquipmentMechanic m)=>false;}
 public static class BuildCatalog {public static EquipmentMechanic[] MechanicsFor(int hero)=>new EquipmentMechanic[0];public static string MechanicName(EquipmentMechanic m)=>"";}
 public class RunChoices {public RunBlessing[] Active=new RunBlessing[0];public static string Name(RunBlessing b)=>"";}
 public class FakeMode {public int PhaseIndex;public float ObjectiveProgress;public object Failure;}
 public partial class GameSession
 {
  public bool IsDead,ChallengeRun;public bool HasStarted=true,InDungeon=true;public bool SpecialAdventure=>RoomChainRun!=null;
  public int DungeonTier=1,DungeonWave=1,TotalWaves=5,runSeed,recapGoldLost,modeGoldReward,modeXpReward,modeMaterialReward;
  public string ModeName="回廊远征",LastRunSummary,RoomGenerationFailureDetail;public RoomChainState RoomChainRun;public FakeMode ModeRun;
  public ProgressionService Progression=new ProgressionService();public RunChoices RunChoices=new RunChoices();
  public RunRecapSnapshot LastRunRecap;private string lastDamageSource="未记录";private float lastDamageAmount,runDamageTaken,runHealingReceived;
  private Dictionary<string,int> combatActions=new Dictionary<string,int>();public void Build(bool won){BuildRunSummary(won);}
 }
}
namespace UnityEngine { public static class Mathf {public static int RoundToInt(float x)=>(int)Math.Round(x);} }
