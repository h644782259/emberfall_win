#!/usr/bin/env python3
"""Whole production plan surface including real platform scaling, cards, disclosures and fixed footer."""
from pathlib import Path
import importlib.util,tempfile,os,sys,subprocess
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
def member(s,key):
 a=s.index(key);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
with tempfile.TemporaryDirectory(prefix='whole-plan-geometry-') as d:
 p=Path(d);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 sources=[root/('Assets/Scripts/Core/'+f+'.cs') for f in ['SkillRuntime','GameTypes','ProgressionService','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','CampPracticeRecord','HudLogicalScale']]
 sources += [root/('Assets/Scripts/UI/'+f+'.cs') for f in ['GameUI.BuildPlans','GameUI.BuildDraft','GameUI.Practice','PracticeResultPresentation','PracticeHudLayout','MobilePanelLayout','MobileControlLayout']]+[root/'Assets/Scripts/Combat/MobileSkillPolicy.cs',root/'Tests/BuildPlanPageGeometryBoundary.cs']
 progression=p/'Progression.cs';progression.write_text((root/'Tests/ProgressionTests.cs').read_text().replace('public struct Color {','public struct Color {public static Color operator *(Color c,float f)=>c;'));sources.append(progression)
 s=(root/'Tests/CampBuildDraftUIBoundary.cs').read_text()
 s='using System.Linq;'+s
 s=s.replace('public struct Vector2 {public static Vector2 zero=>new Vector2();}','public struct Vector2 {public float x,y;public Vector2(float a,float b){x=a;y=b;}public static Vector2 zero=>new Vector2();}')
 s=s.replace('public float x,y,width,height;', 'public float x,y,width,height;public float xMax=>x+width;public float yMax=>y+height;')
 s=s.replace('public static bool enabled=true;', 'public static bool enabled=true;public static Matrix4x4 matrix=Matrix4x4.identity;')
 s=s.replace('public class GUIContent {public GUIContent(string s){}}public class GUIStyle {public float CalcHeight(GUIContent c,float w)=>24;}', 'public class GUIContent {public string text;public GUIContent(string s){text=s;}}public class GUIStyle {public int size;public float CalcHeight(GUIContent c,float w)=>c.text.Split(\'\\n\').Sum(line=>System.Math.Max(1,(int)System.Math.Ceiling(line.Length*size/System.Math.Max(1,w))))*(size+2);}')
 s=s.replace('public static bool Active;', 'public static bool Active;public static Rect SafeArea;public static MobileControlLayout Layout;')
 s=s.replace('public float Health=37,MaxHealth=100;', 'public float Energy=>skillRuntime.Energy;public float Health=37,MaxHealth=100;')
 s=s.replace('public bool PracticeActive;', 'public bool PracticeActive;public CampPracticeRecord PracticeRecord,PreviousPracticeRecord;public bool BeginPractice(CampPracticeScenario s,int seconds,ProgressionService.BuildDraft d)=>false;public bool PinPracticeBaseline()=>false;public bool StartPractice()=>false;public bool RestartPractice()=>false;public void EndPractice(string reason){}')
 s=s.replace('bool practiceChoicesOpen;int TouchFont(int size)=>(int)(size*TouchRatio);','')
 s=s.replace('private void DrawPracticeChoices(ref float y,float width,float unit,bool draw,bool enabled,ProgressionService.BuildDraft draft){}','')
 s=s.replace('enum Panel{Camp,Inventory}', 'enum Panel{Camp,Inventory,None}')
 s=s.replace('float width=1000,height=700,TouchRatio=1;', 'float width=1000,height=700;')
 s=s.replace('TouchRatio=mobile?2:1,','')
 s=s.replace('labels.Add(s);if(click==null', 'labels.Add(s);GeometryButton(r,s);if(click==null')
 s=s.replace('void Box(Rect r,Color c,bool b){}GUIStyle Style(int s,bool b,bool w)=>new GUIStyle();','void Box(Rect r,Color c,bool b){geometryFrame=r;}GUIStyle Style(int s,bool b,bool w)=>new GUIStyle{size=s};')
 s=s.replace('Vector2 BeginTouchScroll(string s,Rect r,Vector2 v,Rect body)=>v;void EndTouchScroll(){}','Vector2 BeginTouchScroll(string s,Rect r,Vector2 v,Rect body){geometryScrolling=true;geometryView=r;geometryContent=body;return v;}void EndTouchScroll(){geometryScrolling=false;}')
 boundary=p/'Boundary.cs';boundary.write_text(s);sources.append(boundary)
 player=(root/'Assets/Scripts/Combat/PlayerController.cs').read_text();session=(root/'Assets/Scripts/Core/GameSession.cs').read_text();ui=(root/'Assets/Scripts/UI/GameUI.cs').read_text();mobile=(root/'Assets/Scripts/UI/GameUI.Mobile.cs').read_text()
 assert 'MobileControls.Active && rect.height>=44*TouchRatio ? TouchFont(15) : 15' in member(ui,'private bool Button('),'button font policy changed; update explicit boundary'
 matrix=next(line.strip() for line in member(ui,'private void OnGUI()').splitlines() if 'GUI.matrix = Matrix4x4.TRS' in line)
 runtime=p/'Methods.cs';runtime.write_text('using UnityEngine;namespace Emberfall{public partial class PlayerController{'+member(player,'public void RefreshStats(')+'}public partial class GameSession{'+member(session,'private void OnProgressChanged()')+'}public sealed partial class GameUI{'+member(ui,'private void RefreshLayout()')+member(mobile,'private float TouchRatio')+member(mobile,'private Rect TouchRect(MobileControlLayout.Area')+member(mobile,'private int TouchFont(')+'void GeometryFrame(){RefreshLayout();'+matrix+'geometryButtons.Clear();click=null;DrawBuildPlanSurface();}}}');sources.append(runtime)
 project=cv.write_project(p/'project',sources,'using Emberfall;class Program{static void Main(string[] args){var p=new ProgressionService(args[0]);p.NewGame(HeroClass.Summoner);GameUI.Geometry(p);}}')
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
