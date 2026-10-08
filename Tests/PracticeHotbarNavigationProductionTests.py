"""Actual practice HUD -> DrawHotbar and CompleteHotbarPointer -> TogglePanel.
Reuses desktop draw boundary; no GUI event delivery/Unity claim.
"""
import ast,os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1]
def member(s,sig):
 a=s.index(sig);b=s.index('{',a)+1;d=1
 while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
fixture=ast.parse((root/'Tests/DesktopOpportunityHotbarProductionTests.py').read_text());shell=next(ast.literal_eval(n.value) for n in fixture.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='shell' for t in n.targets))
shell=shell.replace('bool MerchantServiceActive=>true;public enum Panel{Inventory,Skills}','bool MerchantServiceActive=>true;public enum Panel{None,Inventory,Skills}')
shell=shell.replace('public SkillChargeController Charge=', 'public SkillTargetingController Targeting=new SkillTargetingController();public SkillChargeController Charge=').replace('=>Charge as T;','=>typeof(T)==typeof(SkillTargetingController)?Targeting as T:Charge as T;')
shell=shell.replace(' public class Progression{',' public class SkillTargetingController{public int Casts;public bool Begin(int s){Casts++;return true;}}\n public static class GameAudio{public static void Play(SoundCue c){}}public enum SoundCue{UI}\n public class Progression{')
shell=shell.replace('public bool HasStarted=true;', 'public bool Paused,IsDead;public bool CanChangeLoadout=>HasStarted&&!IsDead;public void SetUIBlocking(bool b){InputBlocked=b;}public int Moves,Assigns,Potions;public bool MoveHotbarSkill(int a,int b){Moves++;return true;}public bool AssignSkill(int a,int b){Assigns++;return true;}public void UseHotbarConsumable(){Potions++;}public bool HasStarted=true;')
shell=shell.replace('void TogglePanel(Panel p){dispatches++;}','')
shell=shell.replace('GameSession session=new GameSession();','Panel panel=Panel.None;int hotbarPointerSkill,hotbarPointerPage,selectedSkill;void ResetMobileSkillNavigation(){}void CancelHotbarPointer(){hotbarPointerSlot=-1;}void DrawMobileHotbar(){}void DrawCompanionCommands(){}void DrawChargeProgress(){}void DrawTargetingHint(){}GameSession session=new GameSession();')
old=member(shell,'public static void Run()')
new='''public static void Run(){var v=new GameUI();var p=v.session.Progression.Profile;Array.Fill(p.equippedSkills,-1);Array.Fill(p.skillRanks,1);p.potions=3;for(int i=0;i<10;i++)v.hotbarSlots[i]=new Rect(i*54,0,48,46);v.session.PracticeActive=true;Event.current.type=EventType.MouseDown;Event.current.button=1;
foreach(int skill in new[]{-1,0,GameBalance.HotbarPotion}){p.equippedSkills[0]=skill;v.Mouse=new Vector2(10,10);v.ReplayPracticeRoute();Check(v.panel==Panel.None&&!v.session.InputBlocked,"practice right-click cannot open hidden panel");}
Event.current.type=EventType.Repaint;p.equippedSkills[0]=-1;v.Begin(0,-1,false,false);v.CompleteHotbarPointer(0);Check(v.panel==Panel.None&&!v.session.InputBlocked,"practice empty slot cannot open hidden panel");
p.equippedSkills[0]=0;v.Begin(0,0,false,false);v.CompleteHotbarPointer(0);Check(v.session.Player.Targeting.Casts==1,"practice learned slot still casts");v.Begin(0,0,true,false);v.CompleteHotbarPointer(1);Check(v.session.Moves==0,"practice cannot drag frozen loadout");
p.equippedSkills[0]=GameBalance.HotbarPotion;v.Begin(0,GameBalance.HotbarPotion,false,false);v.CompleteHotbarPointer(0);Check(v.session.Potions==1,"practice consumable route retained");
v.session.PracticeActive=false;p.equippedSkills[0]=-1;v.Begin(0,-1,false,false);v.CompleteHotbarPointer(0);Check(v.panel==Panel.Skills&&v.session.InputBlocked,"normal empty slot opens skills");v.panel=Panel.None;v.session.InputBlocked=false;Event.current.type=EventType.MouseDown;Event.current.button=1;p.equippedSkills[0]=GameBalance.HotbarPotion;v.DrawHotbar();Check(v.panel==Panel.Inventory&&v.session.InputBlocked,"normal potion right click opens inventory");Console.WriteLine("PASS "+checks+" actual practice hotbar navigation assertions");}
void Begin(int slot,int skill,bool drag,bool config){hotbarPointerSlot=slot;hotbarPointerSkill=skill;hotbarPointerPage=session.Progression.Profile.hotbarPage;hotbarDragging=drag;hotbarPointerConfiguring=config;}'''
shell=shell.replace(old,new)
ui=(root/'Assets/Scripts/UI/GameUI.cs').read_text();practice=(root/'Assets/Scripts/UI/GameUI.Practice.cs').read_text();helper=(root/'Assets/Scripts/UI/GameUI.CombatOpportunities.cs').read_text()
body=''.join(member(ui,x) for x in ['private void DrawHotbar(','private void CompleteHotbarPointer(','private void TogglePanel(','private static int SkillAtSlot(','private static int LearnedSkillAtSlot('])+member(practice,'private void DrawPracticeCombatHUD()')+''.join(member(helper,x) for x in ['private string DesktopSkillOpportunityCaption(','private string DesktopBasicOpportunityCaption('])
# Execute the exact early-return practice routing block; only GUI state objects are boundaries.
on_gui=member(ui,'private void OnGUI()')
route=member(on_gui,'if(session.PracticeActive)')
body+='void ReplayPracticeRoute(){var oldMatrix=GUI.matrix;var oldColor=GUI.color;var oldContentColor=GUI.contentColor;bool oldEnabled=GUI.enabled;'+route+'throw new System.Exception("practice route fell through");}void ClearRewardMoment(){}void DrawPracticeOverlay(){}'
shell=shell.replace('public static bool enabled=true;', 'public static object matrix,color,contentColor;public static bool enabled=true;')
with tempfile.TemporaryDirectory(prefix='practice-hotbar-') as tmp:
 p=Path(tmp)
 for f in ['Core/GameTypes.cs','Core/CombatBalance.cs','Core/CombatOpportunityState.cs']:(p/Path(f).name).write_text((root/'Assets/Scripts'/f).read_text())
 (p/'Shell.cs').write_text(shell);m=p/'Methods.cs';original='using UnityEngine;namespace Emberfall{public partial class GameUI{'+body+'}}';m.write_text(original);proj=p/'Test.csproj';proj.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');cfg=p/'NuGet.Config';cfg.write_text('<configuration><packageSources><clear/></packageSources></configuration>');sdk=sys.argv[1] if len(sys.argv)>1 else 'dotnet';env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');build=[sdk,'build',str(proj),'--configfile',str(cfg),'-v:q'];run=[sdk,str(p/'bin/Debug/net8.0/Test.dll')]
 subprocess.run(build,env=env,check=True);subprocess.run(run,env=env,check=True)
 for before,after,label in [('!session.PracticeActive && !mobile && hover','!mobile && hover','practice right-click cannot open hidden panel'),('if (session.PracticeActive && (configuring || dragged || skill < 0 && skill != GameBalance.HotbarPotion)) return;','','practice empty slot cannot open hidden panel')]:
  assert before in original;m.write_text(original.replace(before,after));subprocess.run(build,env=env,check=True);r=subprocess.run(run,env=env,text=True,capture_output=True);print(r.stdout+r.stderr);assert r.returncode and label in r.stdout+r.stderr;print('PASS compiled old-behavior negative:',label)
