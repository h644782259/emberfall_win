"""Replay the production six-entry selection method with managed GUI/session boundaries."""
from pathlib import Path
import tempfile,subprocess,sys,os
root=Path(__file__).resolve().parents[1]
s=(root/'Assets/Scripts/UI/GameUI.Modes.cs').read_text();a=s.index('  private Vector2 adventureListScroll');b=s.index('  private void DrawMobileModeStatus',a);methods=s[a:b]
fixture=r'''
using System;using System.Collections.Generic;using UnityEngine;
namespace UnityEngine{
 public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>default;}
 public struct Rect{public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}public float xMax=>x+width;public float yMax=>y+height;}
 public struct Color{public Color(float r,float g,float b,float a=1){}}
 public enum TextAnchor{MiddleCenter}public class GUIContent{public string text;public GUIContent(string s){text=s;}public static GUIContent none=new GUIContent("");}
 public static class GUI{public static int Index,Chosen=-1;public static bool Button(Rect r,GUIContent c,object style){return Index++==Chosen;}}
 public static class Mathf{public static int RoundToInt(float v)=>(int)Math.Round(v);public static float Max(float a,float b)=>Math.Max(a,b);}
}
namespace Emberfall{
 public enum ItemSlot{Weapon,Armor,Relic}public enum Rarity{Common,Rare,Epic,Legendary}
 static class MobileControls{public static bool Active=true;}static class UIIconAtlas{public static object Utility(string s)=>s;}
 class Profile{public int level=17;}class Progress{public Profile Profile=new Profile();public object SelectedProgressionGoal()=>null;}
 class Session{public int SelectedArenaMode=-1,SelectedDungeonTier=1,MaximumDungeonTier=100,Entered,Canceled;public bool SelectedChallengeMode;public Progress Progression=new Progress();public void CancelDungeonSelection(){Canceled++;}public void ConfirmDungeonSelection(){Entered++;}}
 static class ProgressionService{public static int EquipmentGenerationLevel(int l)=>l;}
 static class AdventureEntryPresentation{public static string EncounterLine(int m)=>"encounter";public static string GoalFit(Profile p,object g,int m,int t)=>"goal";}
 partial class GameUI{
 float width=568,height=320,TouchRatio=1;Color pale,gold,muted,jade,card;object invisibleButton;List<Rect> blockedRects=new List<Rect>();Session session=new Session();int chapterOpened,canceled,blocked;string Click;int ScrollBodies;List<Rect> Actions=new List<Rect>();
 void Fill(Rect r,Color c){}void Text(Rect r,string t,int n,Color c,bool b=false,bool wrap=false,TextAnchor a=TextAnchor.MiddleCenter){}
 class Metrics{public float CalcHeight(GUIContent c,float w)=>Math.Max(100,c.text.Length*18*18/w);}
 Metrics Style(int n,bool b,bool wrap)=>new Metrics();Vector2 BeginTouchScroll(string key,Rect bounds,Vector2 v,Rect content){ScrollBodies++;return v;}void EndTouchScroll(){}
 void CancelMobileScroll(){canceled++;}void BlockUITransition(){blocked++;}void OpenChapterSelection(){chapterOpened++;}
 bool Button(Rect r,string s,Color c,bool enabled=true){Actions.Add(r);return enabled&&Click==s;}
 bool PrimaryButton(Rect r,string s,Color c){return Button(r,s,c);}bool InventoryPictogramAction(Rect r,string s,object icon){return Button(r,s,jade);}
 void Draw(int chosen=-1,string click=null){GUI.Index=0;GUI.Chosen=chosen;Click=click;Actions.Clear();ScrollBodies=0;DrawArenaSelection();}
 static int count;static void C(bool v,string s){count++;if(!v)throw new Exception(s);}
 public static void Main(){foreach(float u in new[]{.85f,1f,1.2f,1.5f})foreach(var size in new[]{new[]{568f,320f},new[]{681f,323f},new[]{813f,421f},new[]{1024f,768f}}){var ui=new GameUI{width=size[0]*u,height=size[1]*u,TouchRatio=u};for(int i=0;i<6;i++){ui.Draw(i);C(GUI.Index==6&&ui.ScrollBodies==2,"all six entries render before footer without exception");C(i==5?ui.adventureChapterSelected:ui.session.SelectedArenaMode==i-1&&!ui.adventureChapterSelected,"every entry changes correct selection");C(ui.Actions.Count==5,"return tier healing enter remain fixed outside both scrolls");foreach(var r in ui.Actions)C(r.x>=0&&r.y>=0&&r.xMax<=ui.width&&r.yMax<=ui.height&&r.height>=44*u,"footer actions inside logical safe canvas");}ui.Draw(click:"选择章节");C(ui.chapterOpened==1&&ui.session.Entered==0&&ui.session.Canceled==1,"sixth entry routes to chapter only on explicit enter");ui.Draw(0);ui.Draw(click:"进入挑战");C(ui.session.Entered==1,"ordinary entry uses existing confirm service");ui.Draw(click:"−");C(ui.session.SelectedDungeonTier==1,"lower tier bound preserved");ui.session.SelectedDungeonTier=100;ui.Draw(click:"+");C(ui.session.SelectedDungeonTier==100,"unlock upper bound preserved");ui.Draw(click:"普通治疗");C(ui.session.SelectedChallengeMode,"healing restriction toggles existing state");}Console.WriteLine("PASS "+count+" actual six-entry, chapter routing, tier, healing, touch scale and fixed-footer assertions; no Unity rendering");}
 METHODS
 }
}
'''.replace('METHODS',methods)
with tempfile.TemporaryDirectory(prefix='adventure-selection-') as t:
 p=Path(t);(p/'Fixture.cs').write_text(fixture)
 for n in ['UI/AdventureSelectionLayout','UI/MobilePanelLayout','Core/TierRewardBand','Core/TierRewardRules']:(p/(n.split('/')[-1]+'.cs')).write_text((root/'Assets/Scripts'/f'{n}.cs').read_text())
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'Test.csproj')],env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'),check=True)
