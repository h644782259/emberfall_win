"""Actual chapter UI partial + progression persistence; engine/host shells are explicit.
No Unity event delivery, font rendering, device touch or GameSession host behavior is claimed.
"""
from pathlib import Path
import importlib.util,os,subprocess,sys,tempfile
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
def member(file,signature):
 s=(root/'Assets/Scripts/UI'/file).read_text();a=s.index(signature);b=s.index('{',a)+1;depth=1
 while depth:depth+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
shell=r'''
using System;using System.IO;using System.Collections.Generic;using UnityEngine;
namespace UnityEngine {
 public struct Vector2{public float x,y;public Vector2(float a,float b){x=a;y=b;}public static Vector2 zero=>new Vector2();}
 public struct Rect{public float x,y,width,height;public Rect(float a,float b,float w,float h){x=a;y=b;width=w;height=h;}public float yMax=>y+height;public float xMax=>x+width;}
 public enum TextAnchor{MiddleLeft}public static class Time{public static float unscaledTime;}
 public static class Mathf{public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static int Clamp(int x,int a,int b)=>Math.Max(a,Math.Min(b,x));public static int RoundToInt(float x)=>(int)Math.Round(x);}
 public class GUIContent{public string text;public GUIContent(string s){text=s;}}
 public class GUIStyle{public static int Measurements;public float CalcHeight(GUIContent c,float width){Measurements++;return 20*(1+c.text.Length/Math.Max(1,(int)(width/10)));}}
}
namespace Emberfall {
 public static class MobileControls{public static bool Active=true;}
 public sealed class RunStub{public bool Failed;}
 public sealed class SessionStub {
  public ProgressionService Progression;public bool Paused,BackgroundPaused,IsDead,HasStarted=true,Blocked,AllowConfirm=true,ChapterFinished,ChapterRewardPending;
  public bool OpenChapterSelectionAllowed=>HasStarted&&!Paused&&!BackgroundPaused&&!IsDead&&!ChapterFinished;
  public ChapterNode SelectedChapterNode,ActiveChapterNode;public ChapterDifficulty SelectedChapterDifficulty;public int SelectedChapterTier=1;public int SelectedChapterTactic=-1;public string SelectedChapterLineupPreview=>"";public bool SelectedChapterLimitedHealing;
  public bool CanRetryChapter=>ChapterFinished&&ChapterRun.Failed;public int RetryCalls;public bool RetryFailedChapter(){RetryCalls++;return true;}
  public bool ChapterResultReady=true;public ChapterResultSnapshot ChapterResult;public void ContinueChapterResult(){ChapterResultReady=true;}public void Respawn(){ReturnCalls++;}
  public RunStub ChapterRun=new RunStub();public ChapterRunReceipt Receipt;public int ChapterRewardMaterials=>Receipt==null?0:Receipt.Materials;public int ConfirmCalls,ReturnCalls;
  public bool ConfirmChapterEnter(){ConfirmCalls++;if(!AllowConfirm||!Progression.TryBeginChapterNode(SelectedChapterNode,SelectedChapterDifficulty,SelectedChapterTier,out Receipt))return false;for(int room=0;room<ChapterDefinition.RoomCount(Receipt.Node);room++)for(int i=0;i<(Receipt.Node==ChapterNode.StarPlatform?3:6);i++)if(!Progression.RegisterChapterEnemy(Receipt,room,i,Receipt.Node==ChapterNode.StarPlatform&&i==0))throw new Exception("UI host double must register actual completion budget");ChapterResult=new ChapterResultSnapshot(Receipt.Node,Receipt.Difficulty,Receipt.Tier,Progression.Profile.potions,false,0,0,0,0,false,null,null,0,0);return true;}
  public bool TrySettleChapterReward(){bool beforePending=Progression.Profile.pendingFirstClearReward;int before=Progression.Profile.mechanicMaterials;bool ok=Progression.TryCompleteChapterNode(Receipt);if(ok){ChapterRewardPending=false;ChapterResult.RecordSaved(Progression.Profile.mechanicMaterials-before,true,-1,-1,0,0,Progression.ChapterCompletionExperience,!beforePending&&Progression.Profile.pendingFirstClearReward);}return ok;}
  public bool LeaveSucceeds=true;public void ReturnToCamp(){ReturnCalls++;if(LeaveSucceeds)ChapterFinished=false;}public void SetUIBlocking(bool b){Blocked=b;}public void SetPaused(bool b){Paused=b;}
 }
 public sealed partial class GameUI {
  // This chapter navigation fixture never opens the inventory preset-sale dialog.
  bool presetSaleOpen=>false;void CancelPresetSale(){throw new InvalidOperationException("chapter-only fixture entered preset-sale cancellation");}
  enum Panel{None,Chapter,Camp,Inventory,Skills,Chests,Fashion,PotionAssignment,Bindings,SaveLocation,SaveSelection,Controls,TravelMap}
  int ordinaryDeaths;void DrawDeath(){ordinaryDeaths++;}void ReplayDeadSurface() DEAD_DISPATCH
  Panel panel,bindingReturnPanel;SessionStub session;int campTab,rebindingSlot,blocks,cancels;
  bool opaqueFrame;bool UITransitionBlocked=false,saveSelectionFromPause,chestDetails,bindingReturnPause,saveReturnPause,controlsReturnPause;float chestRevealedAt;const float ChestDuration=1;bool ChestAnimationDone=>true;
  float width=568,height=320,TouchRatio=1;Color gold=new Color(),jade=new Color(),pale=new Color(),muted=new Color();string click;bool insideScroll;Rect viewport,content;
  List<(string text,Rect rect,bool scroll,bool enabled)> buttons=new List<(string,Rect,bool,bool)>();List<string> texts=new List<string>();
  void CancelHotbarPointer(){}void CancelMobileScroll(){cancels++;}void BlockUITransition(){blocks++;}
  void Fill(Rect r,Color c){if(r.width==width&&r.height==height)opaqueFrame=true;}void Text(Rect r,string s,int size,Color c,bool bold=false,bool wrap=false,TextAnchor anchor=TextAnchor.MiddleLeft){texts.Add(s);}
  bool Button(Rect r,string s,Color c,bool enabled=true){buttons.Add((s,r,insideScroll,enabled));if(enabled&&click!=null&&s.StartsWith(click)){click=null;return true;}return false;}
  GUIStyle Style(int n,bool b,bool w)=>new GUIStyle();MobilePanelLayout MobilePanelGeometry()=>new MobilePanelLayout(width/TouchRatio,height/TouchRatio);
  Vector2 observedResultScroll;Vector2 BeginTouchScroll(string key,Rect body,Vector2 p,Rect full){if(key=="chapter-result")observedResultScroll=p;insideScroll=true;viewport=body;content=full;return p;}void EndTouchScroll(){insideScroll=false;}
  bool CloseMobileInventoryDetail()=>false;bool CloseMobileSkillDetail()=>false;bool CloseRouteSkill()=>false;bool CloseProgressionGoalSurface()=>false;bool CloseClassSwitchSurface()=>false;bool CloseBuildPlanSurface()=>false;bool CloseTravelMap()=>false;bool CancelSaveDeletion()=>false;bool CancelActiveSaveFlow()=>false;
  void FinishChestReveal(){}void ReturnToInventory(){panel=Panel.Inventory;}
  CLOSE
  public static int Verify(string root){int n=0;Action<bool,string> check=(ok,why)=>{n++;if(!ok)throw new Exception(why);};
   var p=new ProgressionService(Path.Combine(root,"ui"));check(p.CreateNewSlot(HeroClass.Arcanist),"create persisted profile");p.Profile.highestAdventureTier=12;
   var ui=new GameUI{session=new SessionStub{Progression=p}};ui.session.SelectedChapterTier=7;
   string state=JsonUtility.ToJson(p.Profile,true),disk=File.ReadAllText(p.SaveFilePath);int events=0;p.Changed+=()=>events++;
   check(ui.OpenChapterSelection()&&ui.panel==Panel.Chapter&&ui.session.Blocked,"open actual chapter UI blocks combat");
   ui.ClosePanel();check(ui.panel==Panel.None&&ui.chapterSelectionOwner==null&&!ui.session.Blocked&&ui.cancels==2,"chapter Back must clear owner and cancel input without entering combat");
   check(ui.session.ConfirmCalls==0,"Back never enters chapter");
   check(ui.OpenChapterSelection(),"reopen chapter");
   check(!ui.SelectChapterNode(ChapterNode.Redrock)&&!ui.SelectChapterDifficulty(ChapterDifficulty.Heroic),"locked node/difficulty cannot be selected");
   ui.session.SelectedChapterDifficulty=ChapterDifficulty.Heroic;check(!ui.ConfirmSelectedChapter()&&ui.session.ConfirmCalls==0,"stale locked difficulty cannot reach host");
   ui.session.SelectedChapterDifficulty=ChapterDifficulty.Normal;
   p.Profile.chapterCompletedMask=1;p.Profile.chapterHighestDifficulties[0]=1;
   int tier=ui.session.SelectedChapterTier;check(ui.SelectChapterDifficulty(ChapterDifficulty.Hard)&&ui.session.SelectedChapterTier==tier,"difficulty selection never changes tier");
   ui.ChangeChapterTier(1);check(ui.session.SelectedChapterDifficulty==ChapterDifficulty.Hard&&ui.session.SelectedChapterTier==tier+1,"tier adjustment never changes difficulty");
   check(ui.SelectChapterNode(ChapterNode.Redrock)&&ui.session.SelectedChapterDifficulty==ChapterDifficulty.Normal,"node change resets only difficulty to valid normal");
   ui.session.AllowConfirm=false;check(!ui.ConfirmSelectedChapter()&&ui.panel==Panel.Chapter&&ui.session.Blocked&&!string.IsNullOrEmpty(ui.chapterEntryError),"host rejection retains selection and retry surface");
   foreach(int interruption in new[]{0,1,2}){
    ui.session.BackgroundPaused=interruption==0;ui.session.Paused=interruption==1;ui.session.IsDead=interruption==2;int calls=ui.session.ConfirmCalls;
    check(!ui.ConfirmSelectedChapter()&&ui.session.ConfirmCalls==calls,"interrupted UI cannot dispatch entry");
    ui.session.BackgroundPaused=ui.session.Paused=ui.session.IsDead=false;
   }
   // Read-only UI interactions above did not persist story/tier or touch the saved file.
   check(events==0&&File.ReadAllText(p.SaveFilePath)==disk,"browsing/selecting/rejected entry never writes progression");
   foreach(float logicalWidth in new[]{568,667,800,1024})foreach(float ratio in new[]{1f,1.8f,3f}){
    ui.width=logicalWidth*ratio;ui.height=320*ratio;ui.TouchRatio=ratio;ui.buttons.Clear();ui.texts.Clear();int measured=GUIStyle.Measurements;ui.DrawChapterSelection();
    check(GUIStyle.Measurements>measured&&ui.content.height>=ui.viewport.height,"body uses measured scroll content");
    int footer=0,nodeButtons=0;foreach(var b in ui.buttons){check(b.rect.height>=48*ratio-.01f,"all chapter choices keep 48-unit touch height");bool nodeCard=b.text.StartsWith("林庭")||b.text.StartsWith("赤岩")||b.text.StartsWith("星台");if(nodeCard){nodeButtons++;check(!b.scroll&&b.rect.yMax+35*ratio<=ui.viewport.y+.01f,"FIXED_NODES must remain above scrolling details and show completion badges");}else if(!b.scroll){footer++;check(b.rect.y>=ui.viewport.yMax&&b.rect.x>=0&&b.rect.xMax<=ui.width&&b.rect.yMax<=ui.height,"footer stays below body and inside viewport");}}
    check(footer==3&&nodeButtons==3,"three nodes and all fixed navigation actions remain reachable");
    check(ui.texts.Contains("最高通关 · 普通")&&ui.texts.Contains("尚未通关"),"completion shown independently from current selected node");
    check(!ui.texts.Contains(ChapterEntryPresentation.Story(ui.session.SelectedChapterNode)),"story collapsed while goal mechanism and reward remain visible");
    check(ui.buttons.Exists(b=>b.text.StartsWith("星台")&&!b.enabled)&&ui.buttons.Exists(b=>b.text.StartsWith("英雄")&&!b.enabled),"locked node and heroic render disabled using shared core eligibility");
   }
   ui.click="展开故事线索";ui.DrawChapterSelection();ui.texts.Clear();ui.DrawChapterSelection();check(ui.chapterStoryExpanded&&ui.texts.Contains(ChapterEntryPresentation.Story(ui.session.SelectedChapterNode)),"story expands inside measured body without changing entry");
   ui.SelectChapterNode(ChapterNode.ForestCourt);check(!ui.chapterStoryExpanded,"node selection resets optional story only");ui.SelectChapterNode(ChapterNode.Redrock);
   MobileControls.Active=false;ui.width=1600;ui.height=900;ui.buttons.Clear();ui.DrawChapterSelection();
   var centered=ui.ChapterRect(ui.ChapterPanelGeometry().Body,1);check(centered.x==336&&centered.y==188,"CENTERED_CHAPTER desktop content centered in wide viewport");
   MobileControls.Active=true;ui.width=568;ui.height=320;ui.TouchRatio=1;
   string preview=ChapterEntryPresentation.Preview(p.Profile,ChapterNode.ForestCourt,ChapterDifficulty.Normal,1,false);check(preview.Contains("本节点不授予资格")&&!preview.Contains("可领取共享一次"),"forest preview must not promise first-core eligibility");
   check(ChapterEntryPresentation.Preview(p.Profile,ChapterNode.Redrock,ChapterDifficulty.Normal,1,false).Contains("本节点不授予资格"),"redrock preview requires entire chapter");
   check(ChapterEntryPresentation.Preview(p.Profile,ChapterNode.StarPlatform,ChapterDifficulty.Normal,1,false).Contains("星台通关完成整章，可领取共享一次首通核心"),"star preview identifies actual entitlement trigger");
   p.Profile.highestAdventureTier=0;p.Profile.chapterPriorAdventureTier=0;ui.session.SelectedChapterTier=1;
   ui.session.AllowConfirm=true;check(ui.ConfirmSelectedChapter()&&ui.panel==Panel.None&&!ui.session.Blocked,"successful actual core Begin closes entry once");
   // Real filesystem rejection and actual core Complete, reached through production result retry method.
   ui.session.ChapterFinished=true;ui.session.ChapterRewardPending=true;ui.session.ActiveChapterNode=ChapterNode.Redrock;
   ui.chapterScroll=new Vector2(0,99);ui.DrawChapterResult();check(ui.observedResultScroll.y==0,"new result starts at top independently from entry");ui.chapterResultScroll=new Vector2(0,47);
   Directory.CreateDirectory(p.SaveFilePath+".tmp");check(!ui.RetryChapterSettlement()&&ui.session.ChapterRewardPending,"save failure preserves pending receipt");
   ui.texts.Clear();ui.buttons.Clear();ui.DrawChapterResult();check(ui.observedResultScroll.y==47,"pending failed-save retry preserves result reading position");check(ui.buttons.Exists(b=>b.text=="重试保存结算"&&b.enabled&&!b.scroll),"save failure keeps reachable fixed retry action");
   check(ui.texts.Exists(t=>t.Contains("结算待保存"))&&ui.texts.Exists(t=>t.Contains(p.LastError)),"result visibly distinguishes unsaved progress and actual error");
   int capturedMaterials=ui.session.Receipt.Materials;Directory.Delete(p.SaveFilePath+".tmp");check(ui.RetryChapterSettlement()&&!ui.session.ChapterRewardPending,"same receipt retries through actual UI method and real save");
   ui.texts.Clear();ui.DrawChapterResult();check(ui.observedResultScroll.y==47,"successful real settlement retry preserves result reading position");check(ui.texts.Exists(t=>t.Contains("奖励已保存 · +"+capturedMaterials+" 碎片")),"result displays original receipt amount including captured first-clear bonus");
   check(!p.Profile.pendingFirstClearReward&&!ui.session.ChapterResult.FirstCoreAvailable,"actual Redrock UI settlement cannot unlock shared first core");
   check(capturedMaterials==ChapterProgression.MaterialReward(ui.session.Receipt.Node,ui.session.Receipt.Tier)+1,"first-clear receipt retains bonus after completion mask changed");
   int after=events;check(!ui.RetryChapterSettlement()&&events==after,"completed UI retry cannot grant again");
   ui.session.ChapterResultReady=false;ui.opaqueFrame=false;ui.texts.Clear();ui.buttons.Clear();ui.DrawChapterResult();
   check(!ui.opaqueFrame&&ui.buttons.Exists(b=>b.text=="继续 · 查看结果")&&ui.texts.Exists(t=>t.Contains("奖励已保存")),"BOSS_EXIT_OVERLAY must preserve battlefield with saved badge and explicit continue");
   ui.click="继续 · 查看结果";ui.DrawChapterResult();check(ui.session.ChapterResultReady,"explicit continue only switches presentation state");
   ui.session.ChapterResultReady=true;
   var failure=new ChapterResultSnapshot(ChapterNode.ForestCourt,ChapterDifficulty.Hard,7,4,true,0,0,1,2,true,"spawn #4 unreachable","guardian",17,7);
   string evidence=ChapterEntryPresentation.Result(failure);check(evidence.Contains("困难")&&evidence.Contains("第 7 阶")&&evidence.Contains("携带药剂 4")&&evidence.Contains("限疗规则")&&evidence.Contains("spawn #4 unreachable")&&evidence.Contains("guardian"),"failure UI uses separated attempt identity healing and concrete evidence");
   ui.session.IsDead=true;ui.session.ChapterFinished=false;ui.ReplayDeadSurface();check(ui.ordinaryDeaths==1,"ordinary nonchapter death still dispatches original death screen");
   var previousResult=ui.session.ChapterResult;ui.session.ChapterFinished=true;ui.session.ChapterRun.Failed=true;ui.session.ChapterResult=failure;ui.texts.Clear();ui.ReplayDeadSurface();check(ui.ordinaryDeaths==1&&ui.texts.Exists(t=>t.Contains("spawn #4 unreachable")),"chapter death dispatches retained failure evidence instead of ordinary death screen");check(ui.buttons.Exists(b=>b.text=="原条件重试"&&b.enabled)&&ui.buttons.Exists(b=>b.text=="返回营地"),"failed chapter shows retry and camp actions");ui.click="原条件重试";ui.DrawChapterResult();check(ui.session.RetryCalls==1,"retry button dispatches host once");ui.session.IsDead=false;ui.session.ChapterRun.Failed=false;ui.session.ChapterResult=previousResult;
   var repeat=new ChapterResultSnapshot(ChapterNode.ForestCourt,ChapterDifficulty.Normal,1,3,false,1,2,3,3,false,null,null,0,14);repeat.RecordSaved(1,false,-1,-1,2,2,140);
   string repeatText=ChapterEntryPresentation.Result(repeat);check(repeatText.Contains("+1 碎片")&&!repeatText.Contains(ChapterDefinition.Get(ChapterNode.ForestCourt).Outcome)&&!repeatText.Contains("新节点"),"repeat completion displays actual gain without invented first-clear reveal");
   ui.ReturnFromChapter();check(ui.session.ReturnCalls==1,"result return delegates to host guarded leave path");
   ui.session.ChapterFinished=true;ui.session.LeaveSucceeds=false;var selectedBefore=ui.session.SelectedChapterNode;int confirmsBefore=ui.session.ConfirmCalls;
   check(!ui.ReturnAndSelectNextChapter()&&ui.session.ChapterFinished&&ui.session.SelectedChapterNode==selectedBefore,"next-node save rejection keeps terminal view and prior selection");
   ui.session.LeaveSucceeds=true;check(ui.ReturnAndSelectNextChapter()&&ui.panel==Panel.Chapter&&ui.session.SelectedChapterNode==ChapterNode.StarPlatform&&ui.session.ConfirmCalls==confirmsBefore,"next node returns to camp selection without auto entering combat");
   ui.session.ChapterFinished=false;check(ui.OpenChapterSelection(),"open new selection after completed run");
   check(p.LoadSlot(p.CurrentSlotId),"replace profile through real slot load");int callsBefore=ui.session.ConfirmCalls;
   check(!ui.ConfirmSelectedChapter()&&ui.session.ConfirmCalls==callsBefore,"same character reloaded profile cannot use stale UI owner");
   ui.DrawChapterSelection();check(ui.panel==Panel.None&&ui.chapterSelectionOwner==null&&!ui.session.Blocked,"drawing stale selection closes safely without entry");
   var starProgression=new ProgressionService(Path.Combine(root,"star-ui"));check(starProgression.CreateNewSlot(HeroClass.Vanguard),"fresh star UI profile");starProgression.Profile.chapterCompletedMask=3;starProgression.Profile.chapterHighestDifficulties=new[]{1,1,0};starProgression.Save();
   var starUI=new GameUI{session=new SessionStub{Progression=starProgression,SelectedChapterNode=ChapterNode.StarPlatform,ActiveChapterNode=ChapterNode.StarPlatform}};check(starUI.OpenChapterSelection()&&starUI.ConfirmSelectedChapter(),"actual star UI confirmation starts eligible receipt");starUI.session.ChapterFinished=true;starUI.session.ChapterRewardPending=true;check(starUI.RetryChapterSettlement()&&starProgression.Profile.pendingFirstClearReward&&starUI.session.ChapterResult.FirstCoreAvailable,"actual Star UI settlement enables shared core after save");starUI.chapterScroll=new Vector2(0,99);starUI.DrawChapterResult();check(starUI.observedResultScroll.y==0,"new result starts at top independently from entry");starUI.chapterResultScroll=new Vector2(0,47);starUI.RetryChapterSettlement();starUI.DrawChapterResult();check(starUI.observedResultScroll.y==47,"same result retains scroll through save retry");starUI.session.ChapterRun=new RunStub();starUI.DrawChapterResult();check(starUI.observedResultScroll.y==0,"different run resets result scroll");check(starUI.texts.Exists(t=>t.Contains("首通核心已可领取（共享一次）")),"saved Star UI reveals actual new shared entitlement");
   int starMaterials=starProgression.Profile.mechanicMaterials;check(!starUI.RetryChapterSettlement()&&starProgression.Profile.mechanicMaterials==starMaterials,"Star UI saved retry cannot duplicate reward");
   return n;
  }
 }
}
class Program{static void Main(string[] args){Console.WriteLine("PASS: "+Emberfall.GameUI.Verify(args[0])+" chapter UI/core replay assertions");}}
'''
core=['RunChoices','RunChoices.Rooms','RunChoices.Chapter','GameTypes','ProgressionService','ProgressionService.Reforge','ReforgeQuote','ProgressionService.Chapter','ChapterProgression','ChapterResultSnapshot','RoomTactics','CombatBalance','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState']
dispatch=member('GameUI.cs','else if (session.IsDead)');shell=shell.replace('DEAD_DISPATCH',dispatch[dispatch.index('{'):])
close=member('GameUI.cs','private void ClosePanel()');hook='if(CloseChapterSelection())return;'
assert hook in close,'chapter ClosePanel hook must be integrated before replay'
files=[root/'Assets/Scripts/Combat/EnemyControlPolicy.cs']+[root/'Assets/Scripts/Core'/f'{name}.cs' for name in core]+[root/'Assets/Scripts/UI/GameUI.Chapter.cs',root/'Assets/Scripts/UI/ChapterEntryPresentation.cs',root/'Assets/Scripts/UI/MobilePanelLayout.cs',root/'Tests/ProgressionTests.cs']
with tempfile.TemporaryDirectory(prefix='chapter-entry-') as folder:
 out=Path(folder);config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli'),DOTNET_NOLOGO='1')
 for legacy in [False,True]:
  p=cv.write_project(out/('legacy' if legacy else 'positive'),files,program=shell.replace('CLOSE',close.replace(hook,'') if legacy else close))
  subprocess.run([dotnet,'build',str(p),'--configfile',str(config),'-v:q'],env=env,check=True)
  run=[dotnet,str(p.parent/'bin/Debug/net8.0/Validation.dll'),str(out/('legacy-saves' if legacy else 'saves'))]
  if not legacy:subprocess.run(run,env=env,check=True)
  else:
   result=subprocess.run(run,env=env,capture_output=True,text=True);error=result.stdout+result.stderr
   expected='Unhandled exception. System.Exception: chapter Back must clear owner and cancel input without entering combat'
   if result.returncode==0 or not error.splitlines() or error.splitlines()[0]!=expected or any('Exception:' in line for line in error.splitlines()[1:]):raise RuntimeError('legacy hook removal did not fail intended assertion: '+error)
   print('PASS: old chapter Back hook negative control compiled and failed specified navigation assertion')

 # The old opaque-first result must reach the production presentation oracle.
 chapter=root/'Assets/Scripts/UI/GameUI.Chapter.cs'
 current=chapter.read_text();assert current.count('if(!session.ChapterResultReady)')==1
 mutated=out/'OldOpaque.cs';mutated.write_text(current.replace('if(!session.ChapterResultReady)','if(false)'))
 project=cv.write_project(out/'old-opaque',[mutated if f==chapter else f for f in files],program=shell.replace('CLOSE',close))
 subprocess.run([dotnet,'build',str(project),'--configfile',str(config),'-v:q'],env=env,check=True,stdout=subprocess.DEVNULL)
 result=subprocess.run([dotnet,str(project.parent/'bin/Debug/net8.0/Validation.dll'),str(out/'opaque-saves')],env=env,capture_output=True,text=True)
 assert result.returncode and 'System.Exception: BOSS_EXIT_OVERLAY' in result.stdout+result.stderr,result.stdout+result.stderr
 print('PASS: old opaque-first result compiled and failed exact boss-exit UI assertion')

 # Force the prior origin-aligned desktop placement; compile before checking the precise oracle.
 mutated=out/'OldOrigin.cs';mutated.write_text(current.replace('return new Rect(x+area.X*u,y+area.Y*u,area.Width*u,area.Height*u);','return new Rect(area.X*u,area.Y*u,area.Width*u,area.Height*u);'))
 project=cv.write_project(out/'old-origin',[mutated if f==chapter else f for f in files],program=shell.replace('CLOSE',close))
 subprocess.run([dotnet,'build',str(project),'--configfile',str(config),'-v:q'],env=env,check=True,stdout=subprocess.DEVNULL)
 result=subprocess.run([dotnet,str(project.parent/'bin/Debug/net8.0/Validation.dll'),str(out/'origin-saves')],env=env,capture_output=True,text=True)
 assert result.returncode and 'System.Exception: CENTERED_CHAPTER' in result.stdout+result.stderr,result.stdout+result.stderr
 print('PASS: old origin-aligned desktop compiled and failed exact centering oracle')

 # Recreate the old node-in-scroll ownership using the production cards unchanged.
 insertion='            float cardWidth=(layout.Body.Width-16)/3;'
 old_scroll=current.replace(insertion,'            BeginTouchScroll("legacy-node-scroll",ChapterRect(layout.Body,u),chapterScroll,new Rect(0,0,layout.Body.Width*u,600*u));\n'+insertion)
 old_scroll=old_scroll.replace('            Rect body=ChapterRect(new MobilePanelLayout.Area(layout.Body.X,layout.Body.Y+88','            EndTouchScroll();\n            Rect body=ChapterRect(new MobilePanelLayout.Area(layout.Body.X,layout.Body.Y+88')
 mutated=out/'OldScrollingNodes.cs';mutated.write_text(old_scroll)
 project=cv.write_project(out/'old-scrolling-nodes',[mutated if f==chapter else f for f in files],program=shell.replace('CLOSE',close))
 subprocess.run([dotnet,'build',str(project),'--configfile',str(config),'-v:q'],env=env,check=True,stdout=subprocess.DEVNULL)
 result=subprocess.run([dotnet,str(project.parent/'bin/Debug/net8.0/Validation.dll'),str(out/'scroll-saves')],env=env,capture_output=True,text=True)
 assert result.returncode and 'System.Exception: FIXED_NODES' in result.stdout+result.stderr,result.stdout+result.stderr
 print('PASS: old node-scroll ownership compiled and failed exact fixed-node oracle')

 # Restore shared entry/result scroll; the actual result view must fail its top-of-new-run oracle.
 mutated=out/'OldSharedResultScroll.cs';mutated.write_text(current.replace('chapterResultScroll=BeginTouchScroll("chapter-result",ChapterRect(layout.Body,u),chapterResultScroll','chapterScroll=BeginTouchScroll("chapter-result",ChapterRect(layout.Body,u),chapterScroll'))
 project=cv.write_project(out/'old-shared-result',[mutated if f==chapter else f for f in files],program=shell.replace('CLOSE',close))
 subprocess.run([dotnet,'build',str(project),'--configfile',str(config),'-v:q'],env=env,check=True,stdout=subprocess.DEVNULL)
 result=subprocess.run([dotnet,str(project.parent/'bin/Debug/net8.0/Validation.dll'),str(out/'shared-scroll-saves')],env=env,capture_output=True,text=True)
 assert result.returncode and 'new result starts at top independently from entry' in result.stdout+result.stderr,result.stdout+result.stderr
 print('PASS: compiled shared entry/result scroll fails exact new-run scroll oracle')

 # Reset-on-retry must fail while the real receipt is still pending and filesystem writes fail.
 mutated=out/'ResetRetryScroll.cs';mutated.write_text(current.replace('bool saved=session.TrySettleChapterReward();','chapterResultScroll=Vector2.zero;bool saved=session.TrySettleChapterReward();'))
 project=cv.write_project(out/'reset-retry-scroll',[mutated if f==chapter else f for f in files],program=shell.replace('CLOSE',close))
 subprocess.run([dotnet,'build',str(project),'--configfile',str(config),'-v:q'],env=env,check=True,stdout=subprocess.DEVNULL)
 result=subprocess.run([dotnet,str(project.parent/'bin/Debug/net8.0/Validation.dll'),str(out/'retry-scroll-saves')],env=env,capture_output=True,text=True)
 assert result.returncode and 'pending failed-save retry preserves result reading position' in result.stdout+result.stderr,result.stdout+result.stderr
 print('PASS: compiled retry-reset control fails actual pending write-failure reading-position assertion')
