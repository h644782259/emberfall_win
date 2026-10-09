"""Exercise desktop pointer routing and double-click item use from production methods."""
from pathlib import Path
import os,subprocess,sys,tempfile
root=Path(__file__).resolve().parents[1]
source=(root/'Assets/Scripts/UI/GameUI.InventoryGrid.cs').read_text()
def member(key):
 a=source.index(key);b=source.index('{',a)+1;n=1
 while n:n+=(source[b]=='{')-(source[b]=='}');b+=1
 return source[a:b]
methods='\n'.join(member(k) for k in ['private void PrepareInventoryPopupInput()','private void OpenInventoryPopup(','private bool DesktopInventoryGesture(','private void DesktopInventoryClick(','private Rect DesktopInventorySheet('])
fixture=r'''
using System;using System.Collections.Generic;
class Time{public static int frameCount;public static float unscaledTime;}
class MobileControls{public static bool Active;}
enum EventType{Repaint,MouseDown,ScrollWheel,Used}
class Event{public static Event current=new Event();public EventType type;public Vector2 mousePosition;public void Use(){type=EventType.Used;}}
struct Vector2{public float x,y;public static Vector2 zero=>default;}
struct Rect{public float x,y,width,height;public Rect(float a,float b,float w,float h){x=a;y=b;width=w;height=h;}public float xMax=>x+width;public float yMax=>y+height;public bool Contains(Vector2 p)=>p.x>=x&&p.x<xMax&&p.y>=y&&p.y<yMax;}
class Mathf{public static float Max(float a,float b)=>Math.Max(a,b);public static float Clamp(float v,float a,float b)=>Math.Max(a,Math.Min(b,v));}
class Item{public string id;}
class Profile{public List<Item> inventory=new List<Item>();}
class Progression{public Profile Profile=new Profile();public string Equipped,Fashion;public int Equips,Fashions;public bool Equip(string id){Equipped=id;Equips++;return true;}public bool EquipFashion(string id){Fashion=id;Fashions++;return true;}}
class Session{public Progression Progression=new Progression();public int Potions;public void DrinkPotion(){Potions++;}}
class UI{
 Session session=new Session();bool inventoryComparisonOpen,inventoryPopupCompare;Rect inventoryPopupAnchor,inventoryPopupRect;string inventoryPopupItem,selectedItem,inventoryLastClick;float inventoryLastClickAt=-10,width=1280,height=720;int inventoryPopupOpened,inventoryPopupDismissed,blocks;Vector2 Mouse,inventoryComparisonScroll;
 void CancelMobileScroll(){}void BlockUITransition(){blocks++;}void ReviewEquipment(Item i){}void MobileFashionResult(bool b,string s){}void MobileInventoryResult(bool b,string s){}
 METHODS
 static int n;static void C(bool b,string s){n++;if(!b)throw new Exception(s);}
 public static void Main(){
 var u=new UI();u.session.Progression.Profile.inventory.Add(new Item{id="gear"});var r=new Rect(40,50,44,44);Event.current.mousePosition=new Vector2{x=60,y=60};Event.current.type=EventType.Repaint;
 C(u.DesktopInventoryGesture(r,r,"gear")&&u.inventoryComparisonOpen&&u.inventoryPopupCompare,"desktop hover opens equipment comparison");C(u.session.Progression.Equips==0,"hover never equips");
 u.DesktopInventoryClick("gear");C(u.session.Progression.Equips==0,"first click never equips");Time.unscaledTime=.2f;u.DesktopInventoryClick("gear");C(u.session.Progression.Equips==1&&u.session.Progression.Equipped=="gear","double click equips selected identity once");u.DesktopInventoryClick("gear");C(u.session.Progression.Equips==1,"third click starts another pair");
 Time.unscaledTime=1;u.DesktopInventoryClick("@potion");Time.unscaledTime=1.2f;u.DesktopInventoryClick("@potion");C(u.session.Potions==1,"potion double click uses once");
 Time.unscaledTime=2;u.DesktopInventoryClick("@fashion:wings");Time.unscaledTime=2.2f;u.DesktopInventoryClick("@fashion:wings");C(u.session.Progression.Fashions==1&&u.session.Progression.Fashion=="wings","fashion double click equips selected appearance");
 Time.unscaledTime=3;u.DesktopInventoryClick("gear");Time.unscaledTime=3.1f;u.DesktopInventoryClick("@potion");C(u.session.Potions==1&&u.session.Progression.Equips==1,"different item clicks do not count as a double click");
 u.Mouse=new Vector2{x=60,y=60};Event.current.type=EventType.MouseDown;u.PrepareInventoryPopupInput();C(Event.current.type==EventType.MouseDown&&u.inventoryComparisonOpen&&u.blocks==0,"desktop popup never consumes inventory click");
 u.Mouse=new Vector2{x=1100,y=650};u.PrepareInventoryPopupInput();C(!u.inventoryComparisonOpen,"leaving anchor and sheet closes hover detail");
 MobileControls.Active=true;Event.current.type=EventType.Repaint;C(!u.DesktopInventoryGesture(r,r,"gear")&&!u.inventoryComparisonOpen,"mobile does not open hover details");u.OpenInventoryPopup("gear",r);Event.current.type=EventType.MouseDown;u.PrepareInventoryPopupInput();C(Event.current.type==EventType.Used&&!u.inventoryComparisonOpen&&u.blocks==1,"mobile outside touch still dismisses without click through");
 foreach(float x in new[]{20f,640f,1200f}){u.inventoryPopupAnchor=new Rect(x,680,44,44);var sheet=u.DesktopInventorySheet(380,480);C(sheet.x>=0&&sheet.y>=0&&sheet.xMax<=1280&&sheet.yMax<=720,"hover sheet remains in viewport");}
 Console.WriteLine("PASS "+n+" desktop/mobile inventory pointer and double-click assertions");}
}
'''.replace('METHODS',methods)
with tempfile.TemporaryDirectory(prefix='inventory-gesture-') as tmp:
 p=Path(tmp);(p/'Fixture.cs').write_text(fixture);(p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 subprocess.run([sys.argv[1],'run','--project',str(p/'Test.csproj')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
