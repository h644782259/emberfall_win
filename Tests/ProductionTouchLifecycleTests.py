#!/usr/bin/env python3
"""Replay production input methods with engine-value stubs; not Unity input delivery.
Usage: python3 Tests/ProductionTouchLifecycleTests.py /path/to/dotnet
"""
from pathlib import Path
import importlib.util, os, subprocess, sys, tempfile
root=Path(__file__).resolve().parents[1]
def member(file, signature):
    source=(root/'Assets/Scripts'/file).read_text()
    start=source.index(signature); opening=source.index('{',start); end=opening+1; depth=1
    while depth:
        depth+=(source[end]=='{')-(source[end]=='}');end+=1
    return source[start:end]
ui_methods='\n'.join(member(file,sig) for file,sig in [
 ('UI/GameUI.Expedition.cs','public void CancelForegroundInput()'),
 ('UI/GameUI.Expedition.cs','public void CancelBackgroundInput()'),
 ('UI/GameUI.Lifecycle.cs','public bool LifecycleTouchBlocked'),
 ('UI/GameUI.Lifecycle.cs','private void ObserveTouchViewport('),
 ('UI/GameUI.Exit.cs','private bool UITransitionBlocked'),
 ('UI/GameUI.Exit.cs','private void BlockUITransition()'),
 ('UI/GameUI.Exit.cs','private void BlockUITransitionForFinger('),
 ('UI/GameUI.Mobile.cs','public bool MobileDungeonEntranceVisible'),
 ('UI/GameUI.Mobile.cs','private bool CanMobileInteract'),
 ('UI/GameUI.Mobile.cs','public void ActivateMobileInteraction(')])
session_method=member('Core/GameSession.cs','private void SuspendInputs()')
reset_method=member('UI/MobileControls.cs','public static void ResetInput()')
# The route doubles below invoke the actual SuspendInputs implementation. Verify
# the real room-entry/settlement routes still dispatch to that same method.
for path in ['Core/GameSession.RoomChain.cs','Core/GameSession.Modes.cs']:
    assert 'SuspendInputs();' in (root/'Assets/Scripts'/path).read_text()
shell=r'''
using System;using System.Collections.Generic;using UnityEngine;
namespace UnityEngine {
 public enum KeyCode {Space}
 public static class Time {public static float unscaledTime;}
 public static class Screen {public static float width=568,height=320,dpi=160;public static int orientation=1;}
 public static class GUIUtility {public static int keyboardControl,hotControl;}
 public struct Rect {public float x,y,width,height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}}
 public struct Vector2 {public static Vector2 zero=>new Vector2();}
 public struct Touch {public int fingerId;}
 public static class Input {
  public static int[] Fingers=new int[0]; public static int touchCount=>Fingers.Length;
  public static Touch GetTouch(int i)=>new Touch{fingerId=Fingers[i]};
  public static bool GetMouseButton(int i)=>false;public static bool GetMouseButtonUp(int i)=>false;
 }
}
namespace Emberfall {
 public enum HubNpcKind {None}
 public class SkillTargetingController {public void Cancel(){}}
 public class SkillChargeController {public void Cancel(){}}
 public class PlayerStub {public T GetComponent<T>() where T:class=>null;}
 public class ProgressionStub{public bool CanEnterDungeon=true;}
 public sealed class GameSession {public ProgressionStub Progression=new ProgressionStub();
  // This replay exercises ordinary adventure touch lifecycle, never practice.
  public bool PracticeActive=>false;
  public GameUI ui;public bool BackgroundPaused;public PlayerStub Player;
  public bool InputBlocked,DungeonSelectionOpen,IsNearDungeonEntrance;public bool NearDungeonReturn;public bool NearChapterExit;public bool NearRoomExit=true,SideEventAvailable,IsInCamp,InDungeon;public HubNpcKind NearbyHubNpc;
  public void EnterNextRoom(){SuspendInputs();}
  public bool EnterNextChapterRoom(){SuspendInputs();return true;}
  public void StartSideEvent(){}public void ReturnToCamp(){}public int DungeonEntries;public void EnterDungeon(){DungeonEntries++;}public void SetUIBlocking(bool b){}
  public void SuspendForTest(){SuspendInputs();}
  SESSION_METHOD
 }
 public sealed class MobileControls {
  private static MobileControls instance=new MobileControls();
  public static bool Active=true;public static Rect SafeArea=>new Rect(0,0,568,320);
  public class LayoutStub {public float Scale=1;}public static LayoutStub Layout=new LayoutStub();
  public static Vector2 Move;public static bool AttackHeld;private static bool dodge,potion,jump;private static readonly HashSet<KeyCode> keyboardDodgeHeld=new HashSet<KeyCode>();
  private Dictionary<int,int> fingers=new Dictionary<int,int>();private int moveFinger;private bool hasJoystickOrigin;private GameUI ui;
  private object worldPointerOwner,worldPointerTarget;
  private class Gesture {public void Cancel(){}}private Gesture cameraGesture=new Gesture();
  public static void OwnJoystick(GameUI owner){instance.ui=owner;instance.fingers[11]=1;instance.moveFinger=11;instance.hasJoystickOrigin=true;AttackHeld=true;}
  public static bool Cleared=>instance.fingers.Count==0&&!AttackHeld&&!dodge&&!potion&&!jump&&instance.moveFinger==-1000&&!instance.hasJoystickOrigin;
  RESET_METHOD
 }
 public sealed class GameUI {private float lastBlessingClick=-100;
  private GameSession session;private int rebindingSlot;private bool MerchantServiceActive=>true;enum Panel{None,Camp,Skills}private Panel panel;
  private readonly TouchReleaseLatch lifecycleRelease=new TouchReleaseLatch(),uiTransition=new TouchReleaseLatch();
  private readonly TouchViewportState touchViewport=new TouchViewportState();
  private void CancelHotbarPointer(){}public void CancelMobileCast(){}private void OpenNearbyHubNpc(){}
  UI_METHODS
  public bool UIBlocked=>UITransitionBlocked;
  public bool OwnershipCleared=>rebindingSlot==-1&&panel==Panel.None;
  public void Viewport()=>ObserveTouchViewport(MobileControls.SafeArea);
  public static GameUI Create(out GameSession session){session=new GameSession();var ui=new GameUI{session=session};session.ui=ui;return ui;}
 }
}
class Program {
 static int count;
 static void Check(bool ok,string message){count++;if(!ok)throw new Exception(message);}
 static void Main(){
  foreach(float elapsed in new[]{1f,110f}) {
   Emberfall.MobileControls.Active=true;
   Time.unscaledTime=0;Input.Fingers=new[]{11};
   var ui=Emberfall.GameUI.Create(out var session);ui.Viewport();Emberfall.MobileControls.OwnJoystick(ui);
   Input.Fingers=new[]{11,12};ui.ActivateMobileInteraction(12);
   Check(Emberfall.MobileControls.Cleared&&ui.OwnershipCleared,"room entry retains existing movement-owner and UI reset");
   Time.unscaledTime=elapsed;Check(ui.UIBlocked,"initiating finger still blocks its UI transition");
   Input.Fingers=new[]{11,13};
   Check(!ui.UIBlocked,"released finger12 frees UI even with joystick11 and new control13");
   Check(!ui.LifecycleTouchBlocked,"foreground room transition does not lock new right-hand controls");
   foreach(bool viewport in new[]{false,true}) {
    Time.unscaledTime=0;Input.Fingers=new[]{11,12};
    ui=Emberfall.GameUI.Create(out session);ui.Viewport();
    if(viewport){Screen.orientation++;ui.Viewport();}
    else {session.BackgroundPaused=true;session.SuspendForTest();session.BackgroundPaused=false;}
    Time.unscaledTime=elapsed;Input.Fingers=new[]{11,13};
    Check(ui.LifecycleTouchBlocked,"background/viewport must still block replacement control13 while11 remains");
    Input.Fingers=new[]{11};Check(ui.LifecycleTouchBlocked,"releasing13 alone cannot remove background protection");
    Input.Fingers=new int[0];Check(!ui.LifecycleTouchBlocked,"all released clears lifecycle quarantine");
    Input.Fingers=new[]{13};Check(!ui.LifecycleTouchBlocked,"new input works after observed full release");
   }
   Emberfall.MobileControls.Active=false;
   Time.unscaledTime=0;Input.Fingers=new int[0];ui=Emberfall.GameUI.Create(out session);
   session.BackgroundPaused=true;session.SuspendForTest();session.BackgroundPaused=false;
   Time.unscaledTime=elapsed;Check(!ui.LifecycleTouchBlocked,"desktop no-touch resume cannot remain latched");
  }
  Input.Fingers=new int[0];Time.unscaledTime=200;var entrance=Emberfall.GameUI.Create(out var entrySession);entrySession.NearRoomExit=false;entrySession.IsNearDungeonEntrance=true;
  Check(entrance.MobileDungeonEntranceVisible,"near eligible dungeon entrance becomes visible");entrySession.Progression.CanEnterDungeon=false;Check(!entrance.MobileDungeonEntranceVisible,"locked entrance has no hidden top action");entrySession.Progression.CanEnterDungeon=true;entrySession.InputBlocked=true;Check(!entrance.MobileDungeonEntranceVisible,"blocked UI hides entrance");entrySession.InputBlocked=false;entrySession.IsNearDungeonEntrance=false;Check(!entrance.MobileDungeonEntranceVisible,"moving away hides entrance");entrySession.IsNearDungeonEntrance=true;entrance.ActivateMobileInteraction(8);Check(entrySession.DungeonEntries==1,"top entrance dispatches existing dungeon entry exactly once");
  Console.WriteLine("PASS: "+count+" production multi-pointer lifecycle assertions (11/12/13 at 1s and 110s)");
 }
}
'''.replace('UI_METHODS',ui_methods).replace('SESSION_METHOD',session_method).replace('RESET_METHOD',reset_method)
spec=importlib.util.spec_from_file_location('validation',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
with tempfile.TemporaryDirectory(prefix='emberfall-touch-replay-') as folder:
    out=Path(folder);(out/'Replay.cs').write_text(shell)
    project=cv.write_project(out/'project',[out/'Replay.cs',root/'Assets/Scripts/UI/TouchReleaseLatch.cs',root/'Assets/Scripts/UI/TouchViewportState.cs'],program='')
    # Empty program still produces an executable; Main lives in Replay.cs.
    text=project.read_text().replace('<OutputType>Library</OutputType>','<OutputType>Exe</OutputType>');project.write_text(text)
    config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    env=os.environ.copy();env.update(DOTNET_CLI_HOME=str(out/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
    dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
    subprocess.run([dotnet,'build',str(project),'--configfile',str(config),'-v:q'],check=True,env=env)
    subprocess.run([dotnet,str(project.parent/'bin/Debug/net8.0/Validation.dll')],check=True,env=env)
