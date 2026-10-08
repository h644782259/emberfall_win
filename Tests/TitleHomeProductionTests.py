"""Replay actual home selection and actions with managed UI boundaries; no device rendering."""
from pathlib import Path
import os, subprocess, sys, tempfile
root=Path(__file__).resolve().parents[1]
s=(root/'Assets/Scripts/UI/GameUI.cs').read_text()
def member(key):
 a=s.index(key);b=s.index('{',a)+1;d=1
 while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
methods='\n'.join(member(k) for k in ['private SaveSlotInfo RecentAdventureSlot()', 'private void ContinueRecentAdventure()', 'private void DrawAdventureHome()'])
fixture=r'''
using System;using System.Collections.Generic;
struct Rect{public float x,y,width,height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}}
struct Color{public Color(float a,float b,float c,float d=1){}}
enum TextAnchor{MiddleCenter}enum HeroClass{A,B,C,D}
static class Mathf{public static float Min(float a,float b)=>Math.Min(a,b);public static int RoundToInt(float a)=>(int)Math.Round(a);}
static class SystemInfo{public static string deviceModel="iPhone";}
static class GameBalance{public static Color ClassColor(HeroClass h)=>new Color();}
static class MobileControls{public static bool Active;public static LayoutData Layout=new LayoutData();}class LayoutData{public float Width=568,Height=320;}
class SaveSlotInfo{public string Id,DisplayName="战士 · 10级";public bool CanLoad=true,DeletionPending;public DateTime SavedAtUtc;public HeroClass HeroClass;}
class Session{public bool HasStarted;public string Notification;public Progression Progression=new Progression();}class Progression{public string LastError;}
class GameUI{
List<SaveSlotInfo> saveSlots=new List<SaveSlotInfo>();Session session=new Session();bool titleCreatingHero,saveSelectionFromPause,fail;string selectedSaveId,saveSelectionError,click,loaded;int opened,blocks,refreshes;float width=1280,height=720;Color pale,muted,gold;enum Panel{None,SaveSelection}Panel panel;enum ButtonRole{Primary,Navigation}struct Vector2{public static Vector2 zero=>new Vector2();}Vector2 saveSelectionScroll;
List<(Rect rect,string label,ButtonRole role,bool enabled)> buttons=new List<(Rect,string,ButtonRole,bool)>();
void RefreshSaveSlots(){refreshes++;}void OpenSaveSelection(){opened++;panel=Panel.SaveSelection;}void BlockUITransition(){blocks++;}void ContinueSelectedSave(){loaded=selectedSaveId;session.HasStarted=!fail;if(fail)saveSelectionError="failed";}void RequestExit(bool a){}
Rect TouchRect(float a,float b,float c,float d)=>new Rect(a,b,c,d);int TouchFont(float f)=>(int)Math.Round(f);void Fill(Rect r,Color c){}void Text(Rect r,string t,int f,Color c,bool bold=false,bool wrap=false,TextAnchor anchor=default){}void DrawCrest(Rect r,HeroClass h,Color c){}
bool DrawButton(Rect r,string label,ButtonRole role,bool enabled=true,int fontSize=0){buttons.Add((r,label,role,enabled));return enabled&&click==label;}bool NavigationButton(Rect r,string label,Color c)=>false;
METHODS
static int checks;static void C(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
static GameUI Make(){var u=new GameUI();u.saveSlots.Add(new SaveSlotInfo{Id="old",SavedAtUtc=new DateTime(2026,1,1)});u.saveSlots.Add(new SaveSlotInfo{Id="latest",SavedAtUtc=new DateTime(2026,10,8)});u.saveSlots.Add(new SaveSlotInfo{Id="corrupt",CanLoad=false,SavedAtUtc=new DateTime(2026,10,9)});u.saveSlots.Add(new SaveSlotInfo{Id="deleted",DeletionPending=true,SavedAtUtc=new DateTime(2026,10,10)});return u;}
static void Main(){
var u=Make();C(u.RecentAdventureSlot().Id=="latest","latest readable nondeleted save, independent of list order");u.click="继续冒险";u.DrawAdventureHome();C(u.loaded=="latest"&&u.opened==0&&u.session.HasStarted&&u.refreshes==1,"continue loads directly after refreshing disk list");
u=Make();u.fail=true;u.click="继续冒险";u.DrawAdventureHome();C(u.panel==Panel.SaveSelection&&u.saveSelectionError=="failed"&&u.loaded=="latest","failure keeps error visible without falling back to another save");
u=Make();u.click="其他存档";u.DrawAdventureHome();C(u.opened==1&&u.loaded==null,"other saves opens selection without loading");
u=Make();u.click="新的角色";u.DrawAdventureHome();C(u.titleCreatingHero&&u.loaded==null,"new character only opens class selection");
u=new GameUI();u.click="创建角色";u.DrawAdventureHome();C(u.titleCreatingHero&&u.loaded==null,"first launch has usable create action");
foreach(bool mobile in new[]{false,true})foreach(var size in new[]{(568f,320f),(844f,390f),(1024f,768f),(1280f,720f)})foreach(string model in new[]{"iPhone","iPad13,1"}){
MobileControls.Active=mobile;SystemInfo.deviceModel=model;MobileControls.Layout.Width=size.Item1;MobileControls.Layout.Height=size.Item2;u=Make();u.width=size.Item1;u.height=size.Item2;u.DrawAdventureHome();C(u.buttons.Count==3,"one primary and two secondary actions");var primary=u.buttons[0];C(primary.role==ButtonRole.Primary&&primary.label=="继续冒险","continue has primary styling");foreach(var b in u.buttons)C(b.rect.x>=0&&b.rect.y>=0&&b.rect.x+b.rect.width<=size.Item1&&b.rect.y+b.rect.height<=size.Item2,"actions inside viewport");C(primary.rect.width>u.buttons[1].rect.width&&primary.rect.height>u.buttons[1].rect.height,"primary visually larger");C(u.buttons[1].rect.x+u.buttons[1].rect.width<u.buttons[2].rect.x,"secondary actions separated");
}
Console.WriteLine("PASS "+checks+" home hierarchy, viewport, direct resume, empty save and failure checks (managed boundaries)");}}
'''
with tempfile.TemporaryDirectory(prefix='emberfall-home-') as tmp:
 p=Path(tmp);(p/'Program.cs').write_text(fixture.replace('METHODS',methods));(p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'Test.csproj')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1'))
