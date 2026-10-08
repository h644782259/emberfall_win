"""Execute production mobile HUD methods with managed drawing boundaries, not Unity rendering."""
from pathlib import Path
import subprocess,tempfile,sys,os
root=Path(__file__).resolve().parents[1]
def method(s,key):
 a=s.index(key);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
mobile=(root/'Assets/Scripts/UI/GameUI.Mobile.cs').read_text()
assert 'DrawMobileVitals(l);' in method(mobile,'private void DrawMobileHUD(')
assert 'DrawMobilePassiveIdentities' not in mobile
assert 'Box(goal' not in mobile and 'blockedRects.Add(goal)' not in mobile
feedback=(root/'Assets/Scripts/UI/GameUI.MobileFeedback.cs').read_text()
rules=(root/'Assets/Scripts/Core/GameTypes.cs').read_text()
modes=(root/'Assets/Scripts/UI/GameUI.Modes.cs').read_text()
assert 'Box(r' not in method(modes,'private void DrawMobileModeStatus(')
with tempfile.TemporaryDirectory(prefix='mobile-hud-') as tmp:
 p=Path(tmp)
 for rel in ['UI/MobileControlLayout','Combat/MobileSkillPolicy']:(p/(Path(rel).name+'.cs')).write_text((root/'Assets/Scripts'/(rel+'.cs')).read_text())
 (p/'Methods.cs').write_text('using UnityEngine;namespace Emberfall{partial class GameUI{'+''.join(method(mobile,k) for k in ['private void DrawMobileHotbar(','private void DrawMobileVitals(','private void DrawMobileObjectiveText('])+method(feedback,'private void DrawSkillStock(')+'}public static class SkillStockRules{'+method(rules,'public static int Skill(HeroClass hero)')+'}}')
 (p/'Fixture.cs').write_text(r"""
using System;using System.Collections.Generic;using UnityEngine;
namespace UnityEngine{
 public struct Vector2{public float x,y;public Vector2(float a,float b){x=a;y=b;}}
 public struct Rect{public float x,y,width,height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}public float xMax=>x+width;public float yMax=>y+height;public Vector2 center=>new Vector2(x+width/2,y+height/2);}
 public struct Color{public Color(float r,float g,float b,float a=1){}public static Color white=>new Color();public static Color operator*(Color c,float n)=>c;}
 public static class Mathf{public const float Deg2Rad=(float)(Math.PI/180);public static int RoundToInt(float x)=>(int)Math.Round(x);public static float Cos(float x)=>(float)Math.Cos(x);public static float Clamp01(float x)=>Math.Clamp(x,0,1);public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static float Sin(float x)=>(float)Math.Sin(x);public static int CeilToInt(float x)=>(int)Math.Ceiling(x);}
 public static class Time{public static float unscaledTime;}
 public enum TextAnchor{MiddleCenter}
 public class GUIContent{public string text;public GUIContent(string s){text=s;}public static GUIContent none=new GUIContent("");}
 public class GUIStyle{public int size;public Vector2 CalcSize(GUIContent c)=>new Vector2(c.text.Length*size,size+2);public float CalcHeight(GUIContent c,float w)=>(float)Math.Ceiling(c.text.Length*size/w)*(size+2);}
 public static class GUI{public static bool Click;public static List<Rect> Hits=new List<Rect>();public static bool Button(Rect r,GUIContent c,GUIStyle s){Hits.Add(r);return Click;}}
}
namespace Emberfall{
 public enum HeroClass{Vanguard,Arcanist,Ranger,Summoner}
 public class GameProfile{public int[] skillRanks={1,1,1,1,1,1,1,1,1,1};public HeroClass heroClass;}
 public class Progression{public GameProfile Profile=new GameProfile();}
 public class Player{public int Stock=2;public int SkillCharges(int s)=>Stock;public float SkillRechargeRemaining(int s)=>Stock==2?0:6;public float SkillRechargePeriod(int s)=>12;public float Health=70,MaxHealth=100,Energy=40,MaxEnergy=80;public bool IsSkillAvailable(int n)=>true;}
 public class Session{public Player Player=new Player();public Progression Progression=new Progression();}
 public static class MobileControls{public static MobileControlLayout Layout;}
 public static class EffectPreferences{public static float TouchOpacity=>1;}
 public static class UIIconAtlas{public static int ControlDisc()=>-1;public static int Skill(HeroClass c,int i,int size)=>i;public static Color SkillColor(HeroClass c,int i)=>new Color();}
 partial class GameUI{
 Session session=new Session();float controlOpacity=1;float TouchRatio=>MobileControls.Layout.Scale;MobileSkillTap mobileTap=new MobileSkillTap();Rect[] hotbarSlots=new Rect[8];List<Rect> blockedRects=new List<Rect>();Color jade,pale,gold,muted;GUIStyle invisibleButton=new GUIStyle();
 List<int> icons=new List<int>(),availability=new List<int>();List<(Rect,float)> bars=new List<(Rect,float)>();List<string> labels=new List<string>();int maps;List<Rect> fills=new List<Rect>();void Fill(Rect r,Color c){fills.Add(r);}
 Rect TouchRect(MobileControlLayout.Area r)=>new Rect(r.X*TouchRatio,r.Y*TouchRatio,r.Width*TouchRatio,r.Height*TouchRatio);int TouchFont(float f)=>(int)Math.Round(f*TouchRatio);Rect MobileVisualRect(Rect r)=>r;
 void DrawIcon(Rect r,int token,Color c){if(token>=0)icons.Add(token);}void DrawMobileSkillAvailability(Rect r,int i){availability.Add(i);DrawSkillStock(r,i,TouchRatio);}void Text(Rect r,string s,int f,Color c,bool bold=false,bool wrap=false,TextAnchor anchor=TextAnchor.MiddleCenter){labels.Add(s);}void Bar(Rect r,float v,Color c){bars.Add((r,v));}
 string PlatformText(string s)=>s;GUIStyle Style(int size,bool bold,bool wrap)=>new GUIStyle{size=size};void OpenTravelMap(){maps++;}
 static int assertions;static void C(bool b,string s){assertions++;if(!b)throw new Exception(s);}
 public static void Main(){
 foreach(var d in new[]{(568f,320f,163f),(1440f,650f,320f),(2048f,1536f,264f)})foreach(int preset in new[]{-1,0,1})foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass))){
 var l=MobileControls.Layout=new MobileControlLayout(d.Item1,d.Item2,d.Item3,preset);var ui=new GameUI();ui.session.Progression.Profile.heroClass=hero;for(int i=0;i<8;i++)ui.hotbarSlots[i]=ui.TouchRect(l.Skills[i]);ui.DrawMobileHotbar();ui.DrawMobileVitals(l);
 int[] expected={0,1,2,4,5,6,7,9};C(ui.icons.Count==8&&ui.availability.Count==8,"eight active skill identities remain");for(int i=0;i<8;i++)C(ui.icons[i]==expected[i]&&ui.availability[i]==expected[i],"unchanged active skill mapping");C(ui.blockedRects.Count==9,"only active buttons and compact vitals own HUD input");C(!ui.labels.Contains("被动")&&!ui.labels.Contains("未学"),"passive badges hidden");C(ui.bars.Count==2&&Math.Abs(ui.bars[0].Item2-.7f)<.001&&Math.Abs(ui.bars[1].Item2-.5f)<.001,"health and energy retain live values");C(Math.Abs(ui.bars[0].Item1.x-l.PlayerHealth.X*l.Scale)<.01&&Math.Abs(ui.bars[1].Item1.y-l.PlayerEnergy.Y*l.Scale)<.01,"draw uses bottom vitals geometry");
 foreach(int count in new[]{2,1,0}){ui.session.Player.Stock=count;ui.labels.Clear();ui.fills.Clear();var rect=ui.hotbarSlots[0];ui.DrawSkillStock(rect,SkillStockRules.Skill(hero),l.Scale);C(ui.labels.Contains(count+"/2"),"stock badge displays exact count");C(ui.fills.Count==(count==2?1:33),"thin recovery ring only while bank is missing uses");foreach(var fill in ui.fills)C(fill.x>=rect.x-.01&&fill.y>=rect.y-.01&&fill.xMax<=rect.xMax+.01&&fill.yMax<=rect.yMax+.01,"stock visual stays inside existing button");}
 ui.blockedRects.Clear();GUI.Hits.Clear();GUI.Click=true;Rect bounds=ui.TouchRect(l.AdventureStatus);float y=bounds.y;ui.DrawMobileObjectiveText(bounds,ref y,"目标",11,ui.gold,true,true);ui.DrawMobileObjectiveText(bounds,ref y,"进度 2 / 3",10,ui.pale);
 C(ui.blockedRects.Count==1&&GUI.Hits.Count==1&&ui.maps==1,"only title locates through actual button");C(GUI.Hits[0].width<bounds.width&&GUI.Hits[0].height<bounds.height/2,"title hitbox measures text instead of full objective slot");C(y<bounds.y+bounds.height,"short goal has no blank reserved drawing");int before=ui.labels.Count;ui.DrawMobileObjectiveText(bounds,ref y,"",10,ui.pale);C(ui.labels.Count==before,"empty detail draws nothing");
 }
 Console.WriteLine("PASS "+assertions+" production HUD drawing, active identity, vital and text-hitbox assertions (managed GUI boundary)");}
 }
}
""")
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'Test.csproj')],env=env,check=True)
