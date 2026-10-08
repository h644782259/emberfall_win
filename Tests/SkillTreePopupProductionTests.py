"""Replay the actual same-page skill draw orchestration with explicit GUI/service boundaries."""
from pathlib import Path
import tempfile,subprocess,os,sys
root=Path(__file__).resolve().parents[1]
s=(root/'Assets/Scripts/UI/GameUI.MobileSkills.cs').read_text()
def member(sig):
 a=s.index(sig);b=s.index('{',a)+1;depth=1
 while depth:depth+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
methods=member('private void DrawMobileSkills()')
fixture=r'''
using System;using System.Collections.Generic;using UnityEngine;
namespace UnityEngine{
 public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>default;}
 public struct Rect{public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}public float xMax=>x+width;public float yMax=>y+height;public bool Contains(Vector2 p)=>p.x>=x&&p.x<xMax&&p.y>=y&&p.y<yMax;}
 public struct Color{public Color(float r,float g,float b,float a=1){}public static Color operator*(Color c,float f)=>c;}
 public enum EventType{Repaint,MouseDown,Used}public class Event{public static Event current=new Event();public EventType type;public void Use(){type=EventType.Used;}}
 public static class GUI{public static bool enabled=true;}public static class Mathf{public static float Max(float a,float b)=>Math.Max(a,b);public static int Clamp(int n,int a,int b)=>Math.Max(a,Math.Min(b,n));}
}
namespace Emberfall{
 class GameProfile{public int heroClass,level=20,skillPoints=10;public int[] skillRanks=new int[10];}
 class Progress{public GameProfile Profile=new GameProfile();public int Learned=-1;public bool Success=true;public string LastError;public string SkillLockReason(int i)=>Profile.skillRanks[i]>=3?"cap":Profile.skillPoints==0?"points":"";public bool LearnSkill(int i){if(!Success){LastError="write failed";return false;}Learned=i;Profile.skillRanks[i]++;Profile.skillPoints--;LastError="";return true;}}
 class Session{public Progress Progression=new Progress();}
 static class GameBalance{public const int SkillCount=10;public static string ClassName(int n)=>"Class";public static string SkillName(int hero,int skill)=>"Skill"+skill;public static string SkillRankName(int rank)=>"Rank"+rank;}
 static class UIIconAtlas{public static object Utility(string s)=>s;}
 class AttentionState{public HashSet<int> LearnableSkills=new HashSet<int>{0,1,2};}
 partial class GameUI{
 Session session=new Session();int selectedSkill,skillSection,treeDraws,detailDraws,closed,blocks;bool mobileSkillDetail,mobileSkillStatusFailed,RouteSkillReturnAvailable;string mobileSkillStatus;float TouchRatio=1,W=568,H=320;Vector2 mobileSkillListScroll=new Vector2(0,173),mobileSkillDetailScroll,Mouse;Rect mobileSkillPopupRect;Color jade,pale,gold;AttentionState Attention=new AttentionState();List<string> Scrolls=new List<string>();List<Rect> ScrollRects=new List<Rect>();Rect LearnTarget;bool ClickLearn,ClickClose,ClickRoute;
 void ReconcileMobileSkillOwner(){}MobilePanelLayout MobilePanelGeometry()=>new MobilePanelLayout(W,H);bool DrawMobilePanelChrome(MobilePanelLayout l,string title,string info)=>false;void DrawSkillTabs(Rect r){}Rect MobilePanelRect(MobilePanelLayout.Area a)=>new Rect(a.X*TouchRatio,a.Y*TouchRatio,a.Width*TouchRatio,a.Height*TouchRatio);
 float DrawMobileSkillTree(float width,bool draw){if(draw)treeDraws++;return 658;}Rect MobileSkillTreeNode(int skill,float width)=>new Rect((skill%3)*120*TouchRatio,(skill/3)*94*TouchRatio,100*TouchRatio,76*TouchRatio);
 Vector2 BeginTouchScroll(string s,Rect bounds,Vector2 value,Rect content){Scrolls.Add(s);ScrollRects.Add(bounds);return value;}void EndTouchScroll(){}
 bool NavigationButton(Rect r,string s,Color c)=>ClickRoute;void ClosePanel(){closed++;}void BlockUITransition(){blocks++;}void CancelMobileScroll(){}
 void Fill(Rect r,Color c){}void Border(Rect r,Color c){}void DrawIcon(Rect r,object icon,Color c){}void Text(Rect r,string s,int n,Color c,bool bold,bool wrap){}int TouchFont(int n)=>n;
 bool QuietAction(Rect r,string s,bool enabled,string hint)=>ClickClose;float DrawMobileSkillDescription(float width,bool draw){if(draw)detailDraws++;return 900;}
 bool InventoryPictogramAction(Rect r,string s,object icon,bool enabled,bool selected,bool throttle){LearnTarget=r;return enabled&&GUI.enabled&&ClickLearn;}void Feedback(bool saved,string s){}void Badge(Rect r,bool on){}
 void Draw(){treeDraws=detailDraws=0;Scrolls.Clear();ScrollRects.Clear();GUI.enabled=true;Event.current.type=EventType.Repaint;DrawMobileSkills();}
 static int n;static void C(bool b,string s){n++;if(!b)throw new Exception(s);}
 public static void Main(){foreach(float u in new[]{.85f,1f,1.5f})foreach(var size in new[]{new[]{568f,320f},new[]{681f,323f},new[]{813f,421f},new[]{1024f,768f}})for(int i=0;i<10;i++){var ui=new GameUI{W=size[0],H=size[1],TouchRatio=u,selectedSkill=i,mobileSkillDetail=true};ui.Draw();C(ui.treeDraws==1&&ui.detailDraws==1&&ui.Scrolls.Count==2,"tree remains drawn while selected node hint is open");var p=ui.mobileSkillPopupRect;C(p.x>=0&&p.y>=0&&p.xMax<=ui.W*u&&p.yMax<=ui.H*u,"popup bounded for every node and scroll anchor");C(ui.LearnTarget.height==48*u&&ui.LearnTarget.yMax<=p.yMax&&ui.ScrollRects[1].yMax<=ui.LearnTarget.y,"learn stays outside scrolling hint");C(ui.mobileSkillListScroll.y==173,"opening hint preserves tree scroll");ui.ClickClose=true;ui.Draw();C(!ui.mobileSkillDetail&&ui.closed==0,"hint close keeps skill tree open");}
 var learn=new GameUI{selectedSkill=2,mobileSkillDetail=true,ClickLearn=true};learn.Draw();C(learn.session.Progression.Learned==2&&learn.session.Progression.Profile.skillPoints==9&&!learn.mobileSkillStatusFailed,"selected node upgrade calls real dispatch and feedback");learn.session.Progression.Success=false;learn.Draw();C(learn.session.Progression.Profile.skillPoints==9&&learn.mobileSkillStatusFailed&&learn.mobileSkillStatus=="write failed","failed save does not spend and shows actual error");learn.session.Progression.Profile.skillRanks[2]=3;learn.session.Progression.Success=true;learn.Draw();C(learn.session.Progression.Profile.skillPoints==9,"capped skill disables upgrade");
 var outside=new GameUI{mobileSkillDetail=true};outside.Draw();outside.Mouse=new Vector2(1,1);Event.current.type=EventType.MouseDown;outside.DrawMobileSkills();C(!outside.mobileSkillDetail&&Event.current.type==EventType.Used,"outside dismissal consumes input to prevent tree click-through");
 var route=new GameUI{mobileSkillDetail=true,RouteSkillReturnAvailable=true,ClickRoute=true};route.Draw();C(route.closed==1,"distinct route return remains visible and uses existing navigation");
 Console.WriteLine("PASS "+n+" actual same-page skill-tree, node popup, learn/error/cap, outside-dismiss and route-return assertions; managed boundaries");}
 METHODS
 }
}
'''.replace('METHODS',methods)
with tempfile.TemporaryDirectory(prefix='skill-tree-popup-') as t:
 p=Path(t);(p/'Fixture.cs').write_text(fixture);(p/'Layout.cs').write_text((root/'Assets/Scripts/UI/MobilePanelLayout.cs').read_text());(p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>');subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'Test.csproj')],env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'),check=True)
