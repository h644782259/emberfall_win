#!/usr/bin/env python3
"""Execute the entire production UI Update with narrow engine/session shells.
No Unity GUI/input delivery or persistence I/O is simulated.
"""
from pathlib import Path
import importlib.util, os, subprocess, sys, tempfile
root=Path(__file__).resolve().parents[1]
def member(file,signature):
    source=(root/'Assets/Scripts'/file).read_text();start=source.index(signature)
    end=source.index('{',start)+1;depth=1
    while depth:
        depth+=(source[end]=='{')-(source[end]=='}');end+=1
    return source[start:end]
update=member('UI/GameUI.cs','private void Update()')
allowed=member('UI/GameUI.Lifecycle.cs','public bool GameplayBackAllowed')
rewards=(root/'Assets/Scripts/UI/GameUI.MobileRewards.cs').read_text()
chrome=member('UI/GameUI.MobilePanels.cs','private bool DrawMobilePanelChrome(')
assert 'DrawMobilePanelChrome(layout, title, subtitle, true, true)' in rewards
assert 'if(pauseInstead)session.SetPaused(true);else ClosePanel();' in chrome
shell=r'''
using System;using UnityEngine;
namespace UnityEngine {
 public enum KeyCode { Escape,I,K }
 public static class Time {public static int frameCount;}
 public static class Input {public static bool GetKeyDown(KeyCode key)=>key==KeyCode.Escape;public static bool GetMouseButton(int b)=>false;}
}
namespace Emberfall {
 public static class MobileControls {public static bool Active=true;}
 public static class GameBalance {public static bool IsPassive(int skill)=>false;}
 public sealed class SkillChargeController {public bool IsCharging,CancelledThisFrame;public void Cancel(){}}
 public sealed class SkillTargetingController {public bool IsTargeting,CancelledThisFrame;public void Cancel(){}}
 public sealed class PlayerStub {public T GetComponent<T>() where T:class=>null;}
 public sealed class ProfileStub {public int hotbarPage;public bool pendingFashionChest,pendingChestReveal;public readonly object RewardReceipt=new object();}
 public sealed class ProgressionStub {public ProfileStub Profile=new ProfileStub();}
 public sealed class ChoiceStub {public bool AwaitingChoice;}
 public sealed class SessionStub {
  public bool PracticeActive=>false;public void EndPractice(string reason){throw new System.InvalidOperationException("ordinary chest replay cannot exit practice");}
  public bool BackgroundPaused,Paused=true,HasStarted=true,IsDead,DungeonSelectionOpen,Blocked=true;
  // Room-choice session behavior is an explicit boundary; Update routing is production.
  public bool RoomBranchChoiceOpen;public int BranchCancels;public void CancelRoomBranchChoice(){RoomBranchChoiceOpen=false;BranchCancels++;}
  public bool InputBlocked=>Paused||Blocked;public ProgressionStub Progression=new ProgressionStub();
  public ChoiceStub RunChoices=new ChoiceStub();public PlayerStub Player;
  public void SetPaused(bool value){Paused=value;}public void SetUIBlocking(bool value){Blocked=value;}
  public void CancelDungeonSelection(){DungeonSelectionOpen=false;}
 }
 public sealed class ExitStub {public bool Open;public void Cancel(){Open=false;}}
 public sealed partial class GameUI {
  private enum Panel {None,Chapter,Controls,Chests,Skills,SaveSelection,SaveLocation,Bindings,PotionAssignment,Camp,TravelMap,Notice,Inventory}
  private Panel panel=Panel.Chests;private SessionStub session=new SessionStub();
  private int mobilePausePage,transitionBlocks,backConsumedFrame=-1,rebindingSlot=-1;
  private int mobileCastFinger=-1000,hotbarPointerSlot=-1,hotbarPointerPage=0,selectedSkill=0,closeCalls,resetCalls;
  private bool suppressHotbarMouse,hotbarPointerConfiguring=false,chestDetails;
  private bool PauseUtilityVisible=>false;private bool AndroidBackExitEnabled=>true;
  private ExitStub exitRequest=new ExitStub();private string exitError;
  private void RefreshLayout(){}private void ReconcileMobileScroll(){}private void ReconcileCollectionPreview(){}
  private void ReconcileClassSwitchSurface(){}private void ReconcileBuildPlanSurface(){}private void ReconcileProgressionGoalSurface(){}
  private void CancelMobileCast(){mobileCastFinger=-1000;}private void CancelHotbarPointer(){hotbarPointerSlot=-1;}
  private void BlockUITransition(){transitionBlocks++;}private void ClosePanel(){closeCalls++;panel=Panel.None;}
  private void ResetChestReveal(){resetCalls++;}private void RequestExit(bool b){exitRequest.Open=true;}
  private void TogglePanel(Panel value){panel=value;}
  ALLOWED
  UPDATE
  public static int Verify(){
   int n=0;Action<bool,string> check=(ok,message)=>{n++;if(!ok)throw new Exception(message);};
   for(int reveal=0;reveal<2;reveal++)for(int details=0;details<2;details++)for(int page=1;page<=2;page++){
    var ui=new GameUI{mobilePausePage=page,chestDetails=details!=0};
    var p=ui.session.Progression.Profile;p.pendingFashionChest=reveal==0;p.pendingChestReveal=reveal!=0;
    object receipt=p.RewardReceipt;
    Time.frameCount++;ui.Update();
    check(ui.mobilePausePage==0&&ui.session.Paused,"Back from chest-owned pause subpage returns to pause root");
    check(ui.panel==Panel.Chests&&ui.chestDetails==(details!=0),"chest context and details survive");
    check(ui.closeCalls==0&&ui.resetCalls==0,"return does not close or reset reward UI");
    check(p.pendingFashionChest==(reveal==0)&&p.pendingChestReveal==(reveal!=0)&&ReferenceEquals(receipt,p.RewardReceipt),"pending choice/reveal and receipt remain untouched");
    check(ui.backConsumedFrame==Time.frameCount&&!ui.GameplayBackAllowed,"same Back stays consumed");
    check(ui.transitionBlocks==1,"subpage return adds one release gate");
    Time.frameCount++;ui.Update();
    check(!ui.session.Paused&&ui.panel==Panel.Chests&&ui.mobilePausePage==0,"next Back from root returns to chest");
    check(ui.closeCalls==0&&ui.resetCalls==0&&ReferenceEquals(receipt,p.RewardReceipt),"return to chest does not grant, acknowledge or replace receipt");
    ui.session.SetPaused(true);
    check(ui.mobilePausePage==0&&ui.panel==Panel.Chests,"reopening chest menu starts at retained pause root");
    Time.frameCount++;ui.Update();
    check(!ui.session.Paused&&ui.panel==Panel.Chests&&ReferenceEquals(receipt,p.RewardReceipt),"reopened root returns directly to same chest receipt");
   }
   var dialog=new GameUI{mobilePausePage=2};dialog.exitRequest.Open=true;Time.frameCount++;dialog.Update();
   check(!dialog.exitRequest.Open&&dialog.exitError==null&&dialog.mobilePausePage==2&&dialog.session.Paused,"exit confirmation still owns first Back");
   check(dialog.closeCalls==0&&dialog.resetCalls==0,"exit cancellation does not alter chest context");
   Time.frameCount++;dialog.Update();
   check(dialog.mobilePausePage==0&&dialog.session.Paused&&dialog.panel==Panel.Chests,"after cancelling exit, next Back returns only the subpage");
   var branch=new GameUI();branch.session.Paused=false;branch.session.RoomBranchChoiceOpen=true;
   Time.frameCount++;branch.Update();
   check(!branch.session.RoomBranchChoiceOpen&&branch.session.BranchCancels==1,"room branch owns Back before chest navigation");
   check(branch.panel==Panel.Chests&&branch.closeCalls==0&&branch.resetCalls==0&&branch.transitionBlocks==1,"branch cancel preserves chest context and gates release");
   return n;
  }
 }
}
class Program {static void Main(){Console.WriteLine("PASS: "+Emberfall.GameUI.Verify()+" production Update chest pause assertions");}}
'''.replace('UPDATE',update).replace('ALLOWED',allowed)
spec=importlib.util.spec_from_file_location('validation',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
with tempfile.TemporaryDirectory(prefix='emberfall-chest-back-') as folder:
    out=Path(folder);(out/'Replay.cs').write_text(shell)
    project=cv.write_project(out/'project',[out/'Replay.cs',root/'Assets/Scripts/UI/GameUI.PauseNavigation.cs'],program='')
    project.write_text(project.read_text().replace('<OutputType>Library</OutputType>','<OutputType>Exe</OutputType>'))
    config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    env=os.environ.copy();env.update(DOTNET_CLI_HOME=str(out/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
    dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
    subprocess.run([dotnet,'build',str(project),'--configfile',str(config),'-v:q'],check=True,env=env)
    subprocess.run([dotnet,str(project.parent/'bin/Debug/net8.0/Validation.dll')],check=True,env=env)
