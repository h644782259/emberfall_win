"""Replay production quantity pointer handling; Unity screenshots are separate evidence."""
from pathlib import Path
import tempfile, subprocess, os, sys
root=Path(__file__).resolve().parents[1]
s=(root/'Assets/Scripts/UI/GameUI.Merchant.cs').read_text()
a=s.index('        private int MerchantQuantityMaximum()');b=s.index('        private void DrawMerchantQuantity(float u)',a)
methods=s[a:b]
fixture=r"""
using System;using UnityEngine;
namespace UnityEngine {
public struct Vector2{public float x,y;public Vector2(float a,float b){x=a;y=b;}}
public struct Rect{public float x,y,width,height;public float xMax=>x+width;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}public bool Contains(Vector2 p)=>p.x>=x&&p.x<xMax&&p.y>=y&&p.y<y+height;}
public enum EventType{MouseDown,MouseUp,Repaint,Used}public class Event{public static Event current=new Event();public EventType type;public int button;public Vector2 mousePosition;public void Use(){type=EventType.Used;}}
public static class Mathf{public static int Max(int a,int b)=>Math.Max(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static int Clamp(int n,int a,int b)=>Math.Clamp(n,a,b);}
public static class GUI{public static bool enabled=true;public static void FocusControl(string n){}}
public static class GUIUtility{public static int keyboardControl,hotControl;}
}
namespace Emberfall {
class Profile{public int gold=3803,potions=3,refinementStones,affixReforgeStones;}
class ProgressionService{public const int PotionPrice=20,RefinementStonePrice=100,AffixReforgeStonePrice=100;public Profile Profile=new Profile();}
class Session{public ProgressionService Progression=new ProgressionService();}
class GameUI{Session session=new Session();int merchantQuantityKind=0,merchantQuantity=1;string merchantQuantityText="1";float width=693,height=320;
METHODS
static int checks;static void C(bool value,string name){checks++;if(!value)throw new Exception(name);}
void Press(int action,float u=1){var r=MerchantQuantityBox(u);float x=action==1?r.xMax-38*u:r.x+38*u;float y=r.y+(action==2?136:88)*u;Event.current.type=EventType.MouseDown;Event.current.button=0;Event.current.mousePosition=new Vector2(x,y);GUIUtility.keyboardControl=12;GUIUtility.hotControl=9;HandleMerchantQuantityPointer(u);C(Event.current.type==EventType.Used,"overlay owns stepper press");C(GUIUtility.keyboardControl==0&&GUIUtility.hotControl==0,"stepper clears stale field focus");C(merchantQuantityText==merchantQuantity.ToString(),"value and editable text stay synchronized");}
static void Main(){foreach(float u in new[]{.75f,1f,1.5f,2f}){var q=new GameUI();q.Press(1,u);C(q.merchantQuantity==2,"plus increments");q.Press(0,u);C(q.merchantQuantity==1,"minus decrements");q.Press(0,u);C(q.merchantQuantity==1,"minimum is one");q.Press(2,u);C(q.merchantQuantity==96,"max obeys remaining capacity");q.Press(1,u);C(q.merchantQuantity==96,"plus cannot overflow");q.Press(0,u);C(q.merchantQuantity==95,"minus after max");q.session.Progression.Profile.gold=41;q.Press(2,u);C(q.merchantQuantity==2,"max obeys affordability");C(q.merchantQuantity*ProgressionService.PotionPrice==40,"total cost tracks quantity");GUI.enabled=false;q.PressDisabled(u);GUI.enabled=true;q.merchantQuantityKind=-1;q.PressDisabled(u);}
Console.WriteLine("PASS "+checks+" production quantity pointer and boundary assertions");}
void PressDisabled(float u){int before=merchantQuantity;Event.current.type=EventType.MouseDown;HandleMerchantQuantityPointer(u);C(merchantQuantity==before&&Event.current.type==EventType.MouseDown,"disabled or closed modal cannot mutate");}
}}
""".replace('METHODS',methods)
with tempfile.TemporaryDirectory(prefix='EmberfallQuantity-') as d:
 p=Path(d);(p/'Program.cs').write_text(fixture);(p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(p/'home'),DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1');subprocess.run([sys.argv[1],'run','--project',str(p/'Test.csproj'),'--configuration','Release'],env=env,check=True)
