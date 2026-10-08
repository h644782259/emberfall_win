"""Execute full production BeginTouchScroll/EndTouchScroll with engine doubles; not device delivery."""
from pathlib import Path
import os,sys,tempfile,subprocess
r=Path(__file__).resolve().parents[1]
fixture=r'''
using System;using UnityEngine;
namespace UnityEngine{
 public struct Vector2{public float x,y;public Vector2(float a,float b){x=a;y=b;}}
 public struct Rect{public float x,y,width,height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}public float xMax=>x+width;public bool Contains(Vector2 p)=>p.x>=x&&p.x<xMax&&p.y>=y&&p.y<y+height;}
 public enum TouchPhase{Began,Moved,Stationary,Ended,Canceled}public struct Touch{public int fingerId;public TouchPhase phase;public Vector2 position;}
 public static class Input{public static Touch[] touches=new Touch[0];public static int touchCount=>touches.Length;public static Touch GetTouch(int i)=>touches[i];}
 public static class Time{public static int frameCount;}public static class Mathf{public static float Max(float a,float b)=>Math.Max(a,b);}
 public static class GUIUtility{public static int hotControl;}
 public enum EventType{MouseDown,MouseDrag,MouseUp,Repaint,Layout,Used}
 public class Event{public static Event current=new Event();public EventType type;public bool isMouse=>type==EventType.MouseDown||type==EventType.MouseDrag||type==EventType.MouseUp;public void Use(){type=EventType.Used;}}
 public class GUIStyle{public static GUIStyle none=new GUIStyle();}
 public static class GUI{public static bool enabled=true;public static Vector2 BeginScrollView(Rect r,Vector2 p,Rect c,bool h,bool v,GUIStyle a,GUIStyle b)=>p;public static void EndScrollView(){}}
}
namespace Emberfall{
 public static class MobileControls{public static bool Active=true;}
 public sealed partial class GameUI{
 private bool MerchantServiceActive=>true;enum Panel{None,Inventory,Skills}private Panel panel=Panel.Inventory;private float TouchRatio=>1;private GUIStyle scrollBar=new GUIStyle();private Vector2 ScreenToUI(Vector2 p)=>p;private Vector2 pos;
 private static int assertions;private static void Check(bool b,string s){assertions++;if(!b)throw new Exception(s);}
 private void Step(int finger,float y,TouchPhase phase,EventType type,bool second=false){Time.frameCount++;Input.touches=second?new[]{new Touch{fingerId=2,position=new Vector2(20,90),phase=TouchPhase.Began},new Touch{fingerId=finger,position=new Vector2(20,y),phase=phase}}:new[]{new Touch{fingerId=finger,position=new Vector2(20,y),phase=phase}};Event.current.type=type;pos=BeginTouchScroll("bag",new Rect(0,0,200,100),pos,new Rect(0,0,180,500));Check(GUI.enabled,"drag never dims GUI");EndTouchScroll();Check(GUI.enabled,"end restores enabled state");}
 public static void Main(){var u=new GameUI();u.Step(1,70,TouchPhase.Began,EventType.MouseDown);Check(Event.current.type==EventType.MouseDown,"tap down remains available");GUIUtility.hotControl=9;u.Step(1,30,TouchPhase.Moved,EventType.MouseDrag,true);Check(u.pos.y==40&&Event.current.type==EventType.Used&&GUIUtility.hotControl==0,"owned drag moves once and suppresses click despite second finger");u.Step(1,20,TouchPhase.Ended,EventType.MouseUp);Check(Event.current.type==EventType.Used&&u.touchScroll.Finger==-1000,"release suppresses accidental activation and releases owner");u.Step(3,40,TouchPhase.Began,EventType.MouseDown);u.Step(3,40,TouchPhase.Ended,EventType.MouseUp);Check(Event.current.type==EventType.MouseUp,"next normal tap remains functional");u.Step(4,50,TouchPhase.Began,EventType.MouseDown);GUIUtility.hotControl=7;u.Step(4,50,TouchPhase.Canceled,EventType.MouseUp);Check(Event.current.type==EventType.Used&&GUIUtility.hotControl==0&&u.touchScroll.Finger==-1000,"cancel without movement releases hot control and suppresses click");u.Step(5,70,TouchPhase.Began,EventType.MouseDown);u.Step(5,10,TouchPhase.Moved,EventType.Repaint);Check(Event.current.type==EventType.Repaint,"drag repaint stays enabled and unconsumed");Input.touches=new Touch[0];u.ReconcileMobileScroll();Check(u.touchScroll.Finger==-1000&&GUIUtility.hotControl==0,"missing touch cancels capture");Console.WriteLine("PASS "+assertions+" actual scroll method replay assertions (engine doubles, no Unity event delivery)");}
 }
}
'''
with tempfile.TemporaryDirectory(prefix='EmberfallScrollReplay-') as d:
 p=Path(d);(p/'Program.cs').write_text(fixture);sources=[r/'Assets/Scripts/UI/GameUI.TouchScroll.cs',r/'Assets/Scripts/UI/TouchScrollGesture.cs'];(p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup>'+''.join('<Compile Include="'+str(s)+'" />' for s in sources)+'</ItemGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>');os.environ.setdefault('DOTNET_CLI_HOME',str(p/'dotnet-home'));subprocess.run([sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet'),'run','--project',str(p/'Test.csproj'),'--configuration','Release'],check=True)
