#!/usr/bin/env python3
"""Actual DrawHotbar + slot resolution + desktop captions, managed draw/host recorders.
The player's opportunity query is an observation boundary; actual query/expiry rules
are replayed separately by ShatterAvailabilityTests/BurnFinaleProductionTests.
"""
import os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
def member(s,sig):
 a=s.index(sig);b=s.index('{',a)+1;depth=1
 while depth:depth+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
ui=(root/'Assets/Scripts/UI/GameUI.cs').read_text();helper=(root/'Assets/Scripts/UI/GameUI.CombatOpportunities.cs').read_text()
body=''.join(member(ui,sig) for sig in ('private void DrawHotbar(', 'private static int SkillAtSlot(', 'private static int LearnedSkillAtSlot('))
body+=''.join(member(helper,sig) for sig in ('private string DesktopSkillOpportunityCaption(', 'private string DesktopBasicOpportunityCaption('))
shell=r'''
using System;using System.Collections.Generic;using System.Linq;using UnityEngine;
namespace UnityEngine {
 public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 public struct Rect{public float x,y,width,height;public float xMax=>x+width;public float yMax=>y+height;public Vector2 center=>new Vector2(x+width/2,y+height/2);public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}public bool Contains(Vector2 p)=>p.x>=x&&p.x<xMax&&p.y>=y&&p.y<yMax;}
 public struct Color{public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color white=>new Color(1,1,1);}
 public enum TextAnchor{MiddleCenter,MiddleRight}
 public static class Mathf{public static float Max(float a,float b)=>Math.Max(a,b);public static float Clamp01(float n)=>Math.Max(0,Math.Min(1,n));}
 public static class GUI{public static bool enabled=true;}
 public enum EventType{Repaint,MouseDown}
 public class Event{public static Event current=new Event();public EventType type;public int button;public void Use(){}}
}
namespace Emberfall {
 public static class UIIconAtlas{public static object Skill(HeroClass hero,int skill,int size)=>skill;public static Color SkillColor(HeroClass hero,int skill)=>new Color();}
 public static class MobileControls{public static bool Active;}
 public class SkillChargeController{public bool IsCharging,ConsumedThisFrame;public int SkillIndex;}
 public class PlayerController{
  public bool Available=true;public bool IsSkillAvailable(int skill)=>Available&&skill>=0&&skill!=3&&skill!=8;public float Energy=100;public bool IsDead,MasteryComboReady;public CombatOpportunityState SkillWindow,BasicWindow;public CombatOpportunityState SkillOpportunityWindow(int skill){if(skill<0||skill==3||skill==8)return default;if(!Queries.Contains(skill)&&skill>=0&&skill!=3&&skill!=8)Queries.Add(skill);return SkillWindow.Window?SkillWindow:Observations.TryGetValue(skill,out var state)?state:default;}public CombatOpportunityState BasicOpportunityWindow(bool mastery=false)=>mastery&&MasteryComboReady?new CombatOpportunityState(CombatOpportunityKind.MasteryCombo,6):BasicWindow;public float[] Cooldowns=new float[10];public List<int> Queries=new List<int>();public int BasicQueries;
  public Dictionary<int,CombatOpportunityState> Observations=new Dictionary<int,CombatOpportunityState>();public CombatOpportunityState Basic;
  public SkillChargeController Charge=new SkillChargeController();public T GetComponent<T>()where T:class=>Charge as T;
  public float CooldownRemaining(int slot)=>Cooldowns[slot];
  public CombatOpportunityState SkillOpportunity(int skill){Queries.Add(skill);return Observations.TryGetValue(skill,out var state)?state:default;}
  public CombatOpportunityState BasicOpportunity(){BasicQueries++;return Basic;}
 }
 public class Progression{public GameProfile Profile=new GameProfile();}
 public class GameSession{public Progression Progression=new Progression();public PlayerController Player=new PlayerController();public bool InputBlocked,ChallengeRun,InDungeon,PracticeActive;public bool HasStarted=true;public int HealingCharges=2;public Dictionary<string,string> Failures=new Dictionary<string,string>();public string ControlFailure(string key)=>Failures.TryGetValue(key,out var text)?text:"";}
 public partial class GameUI {
  public enum Panel{Inventory,Skills}
  GameSession session=new GameSession();Rect hotbarBounds=new Rect(0,0,282,129);Rect[] hotbarSlots=new Rect[10];List<Rect> blockedRects=new List<Rect>();bool hotbarDragging,hotbarPointerConfiguring;int hotbarPointerSlot;Vector2 Mouse=new Vector2(-100,-100);Color jade=new Color(),pale=new Color(),muted=new Color(),gold=new Color(),card=new Color();string tooltip;
  struct Label{public Rect Rect;public string Value;public Label(Rect r,string s){Rect=r;Value=s;}}
  List<Label> labels=new List<Label>();List<int> identities=new List<int>();int utilityIcons,dispatches;List<Rect> emphasis=new List<Rect>();
  void Box(Rect r,Color c){}void Fill(Rect r,Color c){}void Border(Rect r,Color c,float width=1){if(width==2)emphasis.Add(r);}void Text(Rect r,string text,int size,Color c,bool bold=false,bool wrap=false,TextAnchor anchor=TextAnchor.MiddleCenter){labels.Add(new Label(r,text));}
  void DrawIcon(Rect r,object icon,Color tint){if(icon is int skill)identities.Add(skill);else utilityIcons++;Check(hotbarSlots.Any(slot=>r.x==slot.x+1&&r.y==slot.y+1&&r.width==slot.width-2&&r.height==slot.height-2),"glyph fills slot interior regardless of transient status");}object HotbarIcon(GameProfile p,int skill)=>null;
  string PotionTooltip(GameProfile p)=>"potion";string SkillTooltip(GameProfile p,int skill,int rank)=>"skill";void TogglePanel(Panel p){dispatches++;}void SelectSkill(int skill){dispatches++;}void HandleHotbarPointer(Rect[] slots,bool configuring){}
  static int checks;static void Check(bool okay,string why){checks++;if(!okay)throw new Exception(why);}
  void Draw(){labels.Clear();identities.Clear();utilityIcons=0;emphasis.Clear();session.Player.Queries.Clear();session.Player.BasicQueries=0;DrawHotbar();}
  bool WindowAt(int slot,string text)=>labels.Any(x=>x.Value==text&&x.Rect.x==hotbarSlots[slot].x&&x.Rect.y==hotbarSlots[slot].y-12);
  bool Has(string text)=>labels.Any(x=>x.Value==text);bool At(int slot,string text)=>labels.Any(x=>x.Value==text&&x.Rect.x>=hotbarSlots[slot].x&&x.Rect.x<hotbarSlots[slot].xMax&&x.Rect.y>=hotbarSlots[slot].y&&x.Rect.y<hotbarSlots[slot].yMax);
  public static void Run(){var view=new GameUI();var p=view.session.Progression.Profile;var hero=view.session.Player;Array.Fill(p.equippedSkills,-1);Array.Fill(p.skillRanks,1);p.heroClass=HeroClass.Arcanist;p.potions=3;
   for(int i=0;i<10;i++)view.hotbarSlots[i]=new Rect(10+(i%5)*53,27+(1-i/5)*50,48,46);
   p.equippedSkills[0]=1;p.equippedSkills[2]=GameBalance.HotbarPotion;p.equippedSkills[3]=3;hero.Observations[1]=new CombatOpportunityState(CombatOpportunityKind.Shatter,1.25f);
   view.Draw();Check(view.WindowAt(0,"碎冰 1.3")&&hero.Queries.SequenceEqual(new[]{1}),"actual remapped meteor slot reads skill identity not slot index");
   Check(!view.At(0,"碎冰 1.3")&&view.labels.Count(x=>x.Value=="碎冰 1.3")==1,"ready window caption renders once outside the slot");
   Check(view.emphasis.Count==1&&view.emphasis[0].x==view.hotbarSlots[0].x&&view.identities.SequenceEqual(new[]{1}),"readiness border and skill icon stay on remapped actual slot");
   Check(view.utilityIcons==1&&view.At(2,"×3")&&!hero.Queries.Contains(-2)&&!hero.Queries.Contains(3),"potion and passive/empty slots never query actionable skill identity");
   Check(Enumerable.Range(0,10).All(i=>view.At(i,GameBalance.KeyName(p.hotbarKeys[i]))),"opportunity preserves all ten configured key labels");
   Check(view.WindowAt(0,"碎冰 1.3")&&!view.At(0,"碎冰 1.3"),"opportunity clock stays in dedicated outer row");
   hero.Observations[1]=default;view.Draw();Check(!view.Has("碎冰 1.3")&&view.emphasis.Count==1,"expired opportunity leaves available skill steadily highlighted");
   hero.Observations[1]=new CombatOpportunityState(CombatOpportunityKind.Shatter,2);hero.Cooldowns[0]=1.5f;view.Draw();Check(view.At(0,"1.5")&&!view.At(0,"碎冰 2.0"),"slot cooldown overlay wins before opportunity query");
   hero.Cooldowns[0]=0;hero.Energy=0;view.Draw();Check(view.At(0,"缺能")&&!view.At(0,"碎冰 2.0"),"energy shortage wins before opportunity query");hero.Energy=100;
   hero.Charge.IsCharging=true;hero.Charge.SkillIndex=1;view.Draw();Check(view.At(0,"蓄力")&&!view.At(0,"碎冰 2.0"),"captured mapped charge remains charging rather than future opportunity");
   hero.Charge.SkillIndex=4;view.Draw();Check(!view.At(0,"碎冰 2.0"),"another captured charge suppresses new-action promise");hero.Charge.IsCharging=false;hero.Charge.ConsumedThisFrame=true;view.Draw();Check(!view.At(0,"碎冰 2.0"),"frame-consumed charge suppresses opportunity");hero.Charge.ConsumedThisFrame=false;
   view.session.Failures["skill1"]="目标被遮挡";view.Draw();Check(view.At(0,"目标被遮挡")&&!view.At(0,"碎冰 2.0"),"mapped skill rejection takes priority without losing icon or key");Check(view.identities.Contains(1)&&view.At(0,GameBalance.KeyName(p.hotbarKeys[0])),"failure retains mapped icon and hotkey");view.session.Failures.Clear();
   view.session.InputBlocked=true;view.Draw();Check(hero.BasicQueries==0&&!view.At(0,"碎冰 2.0"),"blocked UI does not publish active opportunities");view.session.InputBlocked=false;
   hero.IsDead=true;view.Draw();Check(hero.BasicQueries==0,"death hides opportunities before querying host");hero.IsDead=false;
   p.skillRanks[1]=0;view.Draw();Check(hero.Queries.Count==0&&!view.identities.Contains(1),"unlearned remapped skill remains empty");p.skillRanks[1]=1;
   p.heroClass=HeroClass.Summoner;p.equippedSkills[0]=-1;p.equippedSkills[7]=4;hero.Observations[4]=new CombatOpportunityState(CombatOpportunityKind.EmpoweredContract,7.5f);view.Draw();Check(view.WindowAt(7,"强化 7.5")&&!view.At(7,"强化 7.5")&&hero.Queries.SequenceEqual(new[]{4}),"actual remapped contract reads skill four in slot seven");
   hero.SkillWindow=new CombatOpportunityState(CombatOpportunityKind.EmpoweredContract,7.5f,blockReason:"无目标");view.Draw();Check(view.WindowAt(7,"强化 7.5")&&!view.At(7,"强化 7.5")&&view.At(7,"无目标")&&view.emphasis.Count==1,"summoner no target keeps clock and block reason while empty-ground cast remains ready");
   hero.SkillWindow=new CombatOpportunityState(CombatOpportunityKind.EmpoweredContract,6.2f);view.Draw();Check(view.WindowAt(7,"强化 6.2")&&!view.At(7,"强化 6.2")&&!view.At(7,"无目标")&&view.emphasis.Count==1,"summoner legal target restores executable emphasis and current time");hero.SkillWindow=default;
   p.heroClass=HeroClass.Vanguard;hero.Basic=new CombatOpportunityState(CombatOpportunityKind.Counter,.8f);view.Draw();Check(view.Has("左键普攻 · 反击 0.8"),"desktop basic control names left click and actual counter window");hero.Basic=default;view.Draw();Check(!view.Has("技能快捷栏")&&!view.Has("左键普攻 · 反击 0.8"),"expired counter leaves no redundant hotbar title");
   hero.MasteryComboReady=true;view.Draw();Check(view.Has("左键普攻 · 连击 6.0"),"actual core readiness appears near basic action");hero.MasteryComboReady=false;
   hero.Basic=new CombatOpportunityState(CombatOpportunityKind.Counter,1);view.session.Failures["attack"]="距离不足";view.Draw();Check(view.Has("左键普攻 · 距离不足")&&hero.BasicQueries==0,"basic rejection takes priority over opportunity");
   hero.SkillWindow=new CombatOpportunityState(CombatOpportunityKind.Shatter,.7f,blockReason:"缺能");hero.Energy=0;view.Draw();Check(view.Has("碎冰 0.7")&&view.Has("缺能"),"desktop separate window survives shortage caption");hero.SkillWindow=default;hero.Energy=100;
   hero.Available=false;view.Draw();Check(view.emphasis.Count==0,"authoritative unavailable state removes readiness border");hero.Available=true;
   Check(view.dispatches==0,"drawing opportunity never dispatches any cast or panel");
   Console.WriteLine("PASS: "+checks+" real desktop DrawHotbar remapping/priority/input-state observations (managed drawing, not Unity)");
  }
 }
}
class Program{static void Main(){Emberfall.GameUI.Run();}}
'''
with tempfile.TemporaryDirectory(prefix='desktop-opportunity-') as t:
 p=Path(t)
 for f in ('Core/GameTypes.cs','Core/CombatBalance.cs','Core/CombatOpportunityState.cs'):(p/Path(f).name).write_text((root/'Assets/Scripts'/f).read_text())
 (p/'Shell.cs').write_text(shell);method=p/'Method.cs';original='using UnityEngine;namespace Emberfall{public partial class GameUI{'+body+'}}';method.write_text(original)
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');config=p/'NuGet.Config';config.write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 def build():
  result=subprocess.run([dotnet,'build',str(project),'--configfile',str(config),'-v:q'],text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
  if result.returncode:print(result.stdout);result.check_returncode()
 command=[dotnet,str(p/'bin/Debug/net8.0/Test.dll')];build();subprocess.run(command,check=True)
 for before,after,oracle in [
  ('return opportunity.Window?opportunity.BlockReason:"";','return actionable?opportunity.Caption:"";','ready window caption renders once outside the slot'),
  ('if(window.Window)Text','if(false)Text','actual remapped meteor slot reads skill identity not slot index'),
  ('slot.width-2,slot.height-2','slot.width-12,slot.height-12','glyph fills slot interior regardless of transient status'),
  ('session.Player.IsSkillAvailable(skill)','false','readiness border and skill icon stay on remapped actual slot'),
  ('session.Player.IsSkillAvailable(skill)','true','authoritative unavailable state removes readiness border'),
  ('DesktopSkillOpportunityCaption(skill,locked,lacksEnergy,cooldown,out actionable)','DesktopSkillOpportunityCaption(slotIndex,locked,lacksEnergy,cooldown,out actionable)','actual remapped meteor slot reads skill identity not slot index'),
  ('DesktopBasicOpportunityCaption();','"";','desktop basic control names left click and actual counter window')]:
  assert before in original;method.write_text(original.replace(before,after));build();result=subprocess.run(command,text=True,capture_output=True)
  assert result.returncode and 'System.Exception: '+oracle in result.stdout+result.stderr,result.stdout+result.stderr
  print('PASS: compiled old/miswired desktop behavior fails exact assertion: '+oracle)
