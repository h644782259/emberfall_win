"""Replay compact mastery production drawing and dispatch with explicit service/GUI boundaries."""
from pathlib import Path
import os,sys,tempfile,subprocess
root=Path(__file__).resolve().parents[1]
s=(root/'Assets/Scripts/UI/GameUI.SkillIntegration.cs').read_text();a=s.index('        private int selectedMastery');methods=s[s.index('        private string SkillMasterySummary()'):s.rfind('    }')]
methods+=s[s.index('        private float DrawSpecializationChoices('):s.index('        private bool DrawSkillSubsurface()')]
g=(root/'Assets/Scripts/Core/GameTypes.cs').read_text();ga=g.index('public static class MasteryProgressionRules');gb=g.index('{',ga)+1;depth=1
while depth:depth+=(g[gb]=='{')-(g[gb]=='}');gb+=1
actual_rules=g[ga:gb]
fixture=r'''
using System;using System.Collections.Generic;using UnityEngine;
namespace UnityEngine{
 public struct Vector2{public float x,y;}public struct Rect{public float x,y,width,height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}public float xMax=>x+width;public float yMax=>y+height;public Vector2 center=>new Vector2{x=x+width/2,y=y+height/2};public bool Contains(Vector2 p)=>p.x>=x&&p.x<xMax&&p.y>=y&&p.y<yMax;}
 public static class Time{public static float unscaledTime;}public struct Color{public Color(float r,float g,float b,float a=1){}public static Color white=>default;public static Color operator*(Color c,float f)=>c;}
 public class GUIContent{public string text;public GUIContent(string s){text=s;}public static GUIContent none=new GUIContent("");}public enum TextAnchor{MiddleCenter}
 public static class GUI{public static int Target=-1,Index;public static bool Button(Rect r,GUIContent c,object style)=>Index++==Target;}
 public static class Mathf{public static int RoundToInt(float n)=>(int)Math.Round(n);public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Clamp(int n,int a,int b)=>Math.Max(a,Math.Min(b,n));}
}
namespace Emberfall{
 enum HeroClass{Vanguard,Arcanist,Ranger,Summoner}enum MasteryType{Offense,Vitality,Guard,Technique}enum ElementalistSpecialization{None,Shatter,Burn}enum SummonerRoute{Bonded,Pack}
 class Profile{public HeroClass heroClass=HeroClass.Arcanist;public int level=80,skillPoints=20,masteryCore=-1;public int[] masteryRanks={0,1,10,20};public SummonerRoute summonerRoute;public ElementalistSpecialization specialization=ElementalistSpecialization.Shatter;}
 class ProgressionService{public string CurrentSlotId="slot",LastError;public Profile Profile=new Profile();public int RefundableBuildPoints=15,RefundableSkillRanks=3,Learned=-1,Core=-1,Refunds,Resets,Specialization;public static int MasteryCap(int l)=>MasteryProgressionRules.Cap(l);public bool HasBuildPreset(int i)=>i==1;public string MasteryLockReason(MasteryType m)=>MasteryCap(Profile.level)==0?"未解锁":Profile.masteryRanks[(int)m]>=MasteryCap(Profile.level)?"满级":Profile.skillPoints==0?"缺点数":"";public bool HasMasteryCore(MasteryType m)=>Profile.masteryCore==(int)m;public int MasteryCoreTier(MasteryType m)=>Profile.masteryRanks[(int)m]>=MasteryProgressionRules.EnhancedInvestment?2:1;public bool LearnMastery(MasteryType m){if(MasteryLockReason(m)!="")return false;Learned=(int)m;Profile.masteryRanks[Learned]++;Profile.skillPoints--;return true;}public bool SelectMasteryCore(MasteryType m,bool camp){Core=(int)m;Profile.masteryCore=Core;return camp;}public bool RefundSkillRanks(bool camp){Refunds++;return camp;}public bool ResetMastery(bool camp){Resets++;return camp;}public bool SetSummonerRoute(SummonerRoute m,bool camp){Profile.summonerRoute=m;return true;}public bool SetSpecialization(ElementalistSpecialization m,bool camp){Specialization++;Profile.specialization=m;return true;}}
 class Session{public bool IsInCamp=true;public ProgressionService Progression=new ProgressionService();}
 static class GameBalance{public static string ClassName(HeroClass c)=>c.ToString();}
 static class BuildCatalog{public static string ClassSignatureDescription(HeroClass c)=>"signature";public static string MasteryName(MasteryType m)=>"破敌精通";public static string MasteryDescription(MasteryType m)=>"Each point changes real attributes; the selected core has actual cooldown and threshold. "+m;}
 static class MasteryCoreRules{public const int InitialInvestment=MasteryProgressionRules.InitialInvestment,EnhancedInvestment=MasteryProgressionRules.EnhancedInvestment;}static class MobileControls{public static bool Active=true;}
 static class UIIconAtlas{public static object Utility(string s)=>s;public static object SkillGlyph(HeroClass c,int i)=>i;public static object Mastery(MasteryType m)=>m;}
 enum CampRouteAction{None,Skill}class Route{public string Name="Route",Loop="Loop",Requirements="Requirement",Enhancement="Enhancement",NextStep="Step";public bool Ready=true;public CampRouteAction NextAction=CampRouteAction.Skill;}
 static class CampRouteCards{public static Route Describe(Profile p,bool mobile,int i)=>new Route();}
 partial class GameUI{
 bool masteryResetConfirm;Session session=new Session();Color pale,jade,gold,muted,card;Vector2 Mouse;object invisibleButton;string tooltip;int Opens,RouteCalls,Requests,RequestSlot,ResultCalls;enum BuildPlanAction{Save,Apply,Reset}BuildPlanAction Requested;
 int SelectedMarkers;string Click;int Ordinal=1,Seen;float Scale=1;List<Rect> Targets=new List<Rect>();List<bool> Enables=new List<bool>();List<string> Captions=new List<string>();
 bool InventoryPictogramAction(Rect r,string s,object icon,bool enabled=true,bool selected=false,bool throttle=false){Targets.Add(r);Enables.Add(enabled);Captions.Add(s);if(s!=Click)return false;Seen++;return enabled&&Seen==Ordinal;}
 bool Button(Rect r,string s,Color c,bool enabled=true)=>InventoryPictogramAction(r,s,null,enabled);
 bool PrimaryButton(Rect r,string s,Color c,bool enabled=true)=>Button(r,s,c,enabled);
 void GoalParagraph(ref float y,float w,float u,string text,int size,Color c,bool bold,bool draw){y+=44;}
 void Rule(float x,float y,float w,Color c){}
 bool TabButton(Rect r,string s,bool selected)=>InventoryPictogramAction(r,s,null,true,selected);
 void OpenClassSwitch(){Opens++;}void RequestBuildPlanAction(BuildPlanAction a,int slot=0){Requested=a;RequestSlot=slot;Requests++;}void MobileWorkshopResult(bool ok,string msg){ResultCalls++;}void CancelMobileScroll(){}void BlockUITransition(){}void FollowCampRouteStep(Route r,int index){RouteCalls++;}
 void Text(Rect r,string s,int n,Color c,bool bold=false,bool wrap=false,TextAnchor align=TextAnchor.MiddleCenter){}void DrawIcon(Rect r,object icon,Color c){if(icon is string v&&v=="confirm")SelectedMarkers++;}void Border(Rect r,Color c){}void Fill(Rect r,Color c){}
 class Metrics{public int Font;public float Scale;public float CalcHeight(GUIContent c,float w)=>Math.Max(1,(float)Math.Ceiling(c.text.Length*Font*Scale/w))*Font*Scale*1.2f;}Metrics Style(int n,bool bold,bool wrap)=>new Metrics{Font=n,Scale=Scale};
 void Draw(float width=520,float u=1,string click=null,int ordinal=1,int node=-1){Click=click;Ordinal=ordinal;Seen=0;GUI.Index=0;GUI.Target=node;SelectedMarkers=0;Targets.Clear();Enables.Clear();Captions.Clear();float measured=DrawSkillDevelopmentContent(width,u,false);float actual=DrawSkillDevelopmentContent(width,u,true);C(Math.Abs(measured-actual)<.02,"measured content equals actual draw bounds");}
 static int count;static void C(bool b,string s){count++;if(!b)throw new Exception(s);}
 public static void Main(){
 foreach(float width in new[]{340f,520f,700f,900f})foreach(float u in new[]{.85f,1f,1.5f}){
 var ui=new GameUI();ui.Draw(width,u);C(!ui.Captions.Contains("配点管理")&&ui.Captions.FindAll(v=>v=="重置精通").Count==1,"reset is directly exposed once");
 C(ui.Captions.FindAll(v=>v.Contains("核心已启用")||v=="启用核心"||v.StartsWith("投入 ")).Count==4,"each mastery card owns one core selector");
 int reset=ui.Captions.IndexOf("重置精通");var rect=ui.Targets[reset];C(rect.x>=0&&rect.xMax<=width*u+.01&&rect.height>=44*u,"reset stays in bounds with touch height");
 C(ui.session.Progression.Resets==0,"rendering does not reset");ui.Draw(width,u,click:"重置精通");C(ui.session.Progression.Resets==0&&ui.masteryResetConfirm,"first reset click only requests confirmation");ui.masteryResetConfirm=false;
 ui.session.Progression.Profile.skillPoints=0;ui.session.IsInCamp=false;ui.Draw(width,u,click:"重置精通");C(ui.session.Progression.Resets==0&&ui.masteryResetConfirm,"reset is always available and still requests confirmation");
 }
 Console.WriteLine("PASS "+count+" direct mastery reset geometry and dispatch assertions; managed GUI boundary");}
 static string BuildModeName(ElementalistSpecialization m)=>m==ElementalistSpecialization.None?"均衡":m==ElementalistSpecialization.Shatter?"碎冰":"灼燃";
 METHODS
 }
}
'''.replace('METHODS',methods).replace('enum HeroClass{',actual_rules+'\nenum HeroClass{')
with tempfile.TemporaryDirectory(prefix='skill-development-') as t:
 p=Path(t);(p/'Fixture.cs').write_text(fixture);(p/'Layout.cs').write_text((root/'Assets/Scripts/UI/MobilePanelLayout.cs').read_text());(p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>');subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'Test.csproj')],env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'),check=True)
