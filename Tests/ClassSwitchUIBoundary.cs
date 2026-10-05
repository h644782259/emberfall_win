using System;using System.IO;using UnityEngine;
namespace Emberfall{
 public partial class GameSession{
  public string ClassSwitchError;public int SwitchCalls;public Action AfterSwitch;
  public string ClassSwitchLockReason()=>IsInCamp?"":"unsafe";
  // Session/world replacement is covered by ClassSwitchRuntimeTests. This UI boundary
  // uses the real persistence transaction, then publishes an explicit replacement.
  public bool TrySwitchClass(HeroClass hero){SwitchCalls++;var t=Progression.PrepareClassSwitch(hero,IsInCamp);if(!Progression.CommitClassSwitch(t,IsInCamp)){ClassSwitchError=Progression.LastError;return false;}Player=new PlayerController();Player.Bind(this);AfterSwitch?.Invoke();Progression.PublishClassSwitch(t);return true;}
 }
 public sealed partial class GameUI{
  ProgressionService reforgeOwner;object mobileWorkshopService;bool saveSlotsDirty;int inputResets;void CancelForegroundInput(){inputResets++;}void CancelMobileCast(){}void ResetMobileSkillNavigation(){}
  void RebindProgressionNotifications(ProgressionService a,ProgressionService b){ResetBuildPlanSurface();}
  void ClassClick(string caption){click=null;seen=0;DrawClassSwitchSurface();click=caption;clickIndex=0;seen=0;DrawClassSwitchSurface();click=null;}
  static int classChecks;static void C(bool ok,string why){classChecks++;if(!ok)throw new Exception(why);}
  public static void ClassUI(string root){
   foreach(bool mobile in new[]{false,true})foreach(int target in new[]{1,2,3}){
    var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));p.NewGame(HeroClass.Vanguard);p.Profile.level=45;p.Save();MobileControls.Active=mobile;
    var ui=new GameUI{session=new GameSession{Progression=p},panel=Panel.Camp,TouchRatio=mobile?2:1};ui.session.Bind();ui.session.AfterSwitch=ui.OnClassSwitched;
    var before=p.Profile;string disk=File.ReadAllText(p.SaveFilePath);int changed=0;p.Changed+=()=>changed++;
    ui.OpenClassSwitch();ui.ClassClick("当前 · "+GameBalance.ClassName(HeroClass.Vanguard));C(ui.classSwitchPreview==null&&changed==0,"active class button disabled");
    ui.ClassClick("预览 · "+GameBalance.ClassName((HeroClass)target));C(ui.classSwitchPreview!=null&&ReferenceEquals(before,p.Profile)&&File.ReadAllText(p.SaveFilePath)==disk&&changed==0,"preview does not persist or switch");
    ui.ClassClick("取消 · 返回工坊");C(!ui.classSwitchOpen&&ui.session.SwitchCalls==0&&ReferenceEquals(before,p.Profile),"actual cancel does not call runtime");
    ui.OpenClassSwitch();ui.ClassClick("预览 · "+GameBalance.ClassName((HeroClass)target));p.Profile.gold++;ui.ClassClick("重新核对");C(ui.session.SwitchCalls==0&&ui.classSwitchPreview!=null,"stale confirm only refreshes preview");
    Directory.CreateDirectory(p.SaveFilePath+".tmp");ui.ClassClick("确认切换为"+GameBalance.ClassName((HeroClass)target));Directory.Delete(p.SaveFilePath+".tmp");C(ui.classSwitchOpen&&ui.classSwitchMessage!=null&&ReferenceEquals(before,p.Profile)&&changed==0,"failed save stays reviewable without publication");
    ui.OpenBuildPlans();ui.practiceChoicesOpen=true;ui.buildPlanDetails=1;ui.ClassClick("确认切换为"+GameBalance.ClassName((HeroClass)target));C(!ui.classSwitchOpen&&!ui.buildPlansOpen&&!ui.practiceChoicesOpen&&ui.buildPlanDetails==-1&&ui.inputResets==1&&changed==1&&p.Profile.heroClass==(HeroClass)target,"successful UI rebind resets build-plan surface once");
    ui.OpenClassSwitch();ui.panel=Panel.Inventory;ui.ReconcileClassSwitchSurface();ui.panel=Panel.Camp;C(!ui.DrawClassSwitchSurface(),"leaving camp prevents stale modal resurrection");
    ui.OpenClassSwitch();ui.session.Player=new PlayerController();ui.ReconcileClassSwitchSurface();C(!ui.classSwitchOpen,"new player invalidates preview");
    foreach(int guard in new[]{0,1,2,3,4}){ui.buildPlanAction=BuildPlanAction.None;ui.buildPlanChoosing=false;ui.presetSaleOpen=false;ui.allocationDraft=null;ui.reforgeOwner=null;
     if(guard==0)ui.allocationDraft=p.BeginBuildDraft(true);if(guard==1)ui.buildPlanAction=BuildPlanAction.Save;if(guard==2)ui.buildPlanChoosing=true;if(guard==3)ui.presetSaleOpen=true;if(guard==4)ui.reforgeOwner=p;
     ui.OpenClassSwitch();C(!ui.classSwitchOpen,"pending edit/confirmation blocks class entry");}
   }
   Console.WriteLine("PASS "+classChecks+" actual desktop/mobile class UI transaction and navigation assertions; scene and GUI boundaries doubled");
  }
 }
}
