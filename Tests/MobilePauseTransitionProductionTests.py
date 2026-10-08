"""Execute actual mobile pause helpers plus ReturnToCamp/EnterDungeon.
Engine distance and persistence/world replacement are explicit managed boundaries.
"""
from pathlib import Path
import tempfile,subprocess,sys,os
root=Path(__file__).resolve().parents[1]
def member(path,sig):
 s=(root/'Assets/Scripts'/path).read_text();a=s.index(sig);b=s.index('{',a)+1;d=1
 while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
ui='\n'.join(member('UI/GameUI.Mobile.cs',x) for x in ['private void LeaveMobilePauseForCamp()','private void LeaveMobilePauseForDungeon()'])
mobile=(root/'Assets/Scripts/UI/GameUI.Mobile.cs').read_text()
assert 'case 4: LeaveMobilePauseForCamp(); break;' in mobile
assert 'case 5: OpenControls(); break;' in mobile
assert 'case 5: LeaveMobilePauseForDungeon(); break;' not in mobile
host='\n'.join(member('Core/GameSession.cs',x) for x in ['public void ReturnToCamp()','public void EnterDungeon()'])
shell='''using System;using System.Collections.Generic;
namespace UnityEngine {public struct Vector3{public float x;public static float Distance(Vector3 a,Vector3 b)=>Math.Abs(a.x-b.x);}public static class Mathf{public static int Clamp(int v,int lo,int hi)=>Math.Max(lo,Math.Min(hi,v));}}
namespace Emberfall{using UnityEngine;
class Transform{public Vector3 position;}
class Actor{public bool IsDead;public int CombatEpoch;public Transform transform=new Transform();}
class EnemyController:Actor{}
class Profile{public bool pendingFashionChest,pendingChestReveal;public int level=2;}
class Progress{public bool CanEnterDungeon=true;public Profile Profile=new Profile();}
partial class GameSession{
 public bool Paused=true,PracticeActive,HasStarted=true,IsDead,InDungeon=true,DungeonCleared,ModeFinished,DungeonSelectionOpen,Near=true,SaveOk=true,Throw,Replace,EndThrows;
 public bool InputBlocked=>Paused;public Actor Player=new Actor();public List<EnemyController> Enemies=new List<EnemyController>();public Progress Progression=new Progress();public int SelectedDungeonTier=1,MaximumDungeonTier=10,Saves;
 public void SetPaused(bool b){Paused=b;}void UpdateTimeScale(){}bool NearPortal(){if(Throw)throw new Exception("fault");return Near;}void Notify(string s){}
 void EndPractice(string s){if(EndThrows)throw new Exception("fault");PracticeActive=false;}
 bool ChangeZone(bool d){Saves++;if(Throw)throw new Exception("fault");if(!SaveOk)return false;if(Replace)Player=new Actor();else Player.CombatEpoch++;InDungeon=d;return true;}
 HOST
}
partial class GameUI{public GameSession session;public GameUI(GameSession s){session=s;}public void Camp()=>LeaveMobilePauseForCamp();public void Dungeon()=>LeaveMobilePauseForDungeon();UI}
class Program{static int count;static void Check(bool v,string why){count++;if(!v)throw new Exception(why);}static void Main(){
 var s=new GameSession();var ui=new GameUI(s);s.Enemies.Add(new EnemyController());ui.Camp();Check(s.Paused&&s.Saves==0,"near enemy preserves pause");
 s.Enemies.Clear();s.SaveOk=false;ui.Camp();ui.Camp();Check(s.Paused&&s.Player.CombatEpoch==0&&s.Saves==2,"failed saves preserve pause");
 s.Throw=true;try{ui.Camp();}catch(Exception){}Check(s.Paused,"camp exception preserves pause");s.Throw=false;s.SaveOk=true;ui.Camp();Check(!s.Paused&&s.Player.CombatEpoch==1,"successful camp resumes");
 s=new GameSession{Replace=true};ui=new GameUI(s);ui.Camp();Check(!s.Paused,"replacement resumes");
 s=new GameSession{PracticeActive=true};ui=new GameUI(s);ui.Camp();Check(!s.Paused&&!s.PracticeActive,"practice ends and resumes");
 s=new GameSession{PracticeActive=true,EndThrows=true};ui=new GameUI(s);try{ui.Camp();}catch(Exception){}Check(s.Paused&&s.PracticeActive,"practice exception preserves pause");
 s=new GameSession();ui=new GameUI(s);ui.Dungeon();Check(s.Paused&&!s.DungeonSelectionOpen,"already dungeon preserves pause");s.InDungeon=false;s.Near=false;ui.Dungeon();Check(s.Paused&&!s.DungeonSelectionOpen,"far portal preserves pause");s.Near=true;s.Throw=true;try{ui.Dungeon();}catch(Exception){}Check(s.Paused,"dungeon exception preserves pause");s.Throw=false;s.Progression.Profile.pendingChestReveal=true;ui.Dungeon();Check(s.Paused,"chest refusal preserves pause");s.Progression.Profile.pendingChestReveal=false;ui.Dungeon();Check(!s.Paused&&s.DungeonSelectionOpen,"selection opens and resumes");ui.Dungeon();Check(s.DungeonSelectionOpen,"repeat preserves selection");
 s=new GameSession{Paused=false,SaveOk=false};ui=new GameUI(s);ui.Camp();Check(!s.Paused,"unpaused refusal remains unpaused");Console.WriteLine("PASS "+count+" actual mobile pause transition assertions");}}
}'''
source=shell.replace('HOST',host).replace('UI}',ui+'}')
with tempfile.TemporaryDirectory(prefix='mobile-pause-transitions-') as tmp:
 p=Path(tmp);cs=p/'Program.cs';cs.write_text(source);proj=p/'Test.csproj';proj.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649</NoWarn></PropertyGroup></Project>');cfg=p/'NuGet.Config';cfg.write_text('<configuration><packageSources><clear/></packageSources></configuration>');dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet';env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');build=[dotnet,'build',str(proj),'--configfile',str(cfg),'-v:q'];run=[dotnet,str(p/'bin/Debug/net8.0/Test.dll')]
 subprocess.run(build,env=env,check=True);subprocess.run(run,env=env,check=True)
 for before,after,label in [('session.ReturnToCamp();','session.SetPaused(false); session.ReturnToCamp();','near enemy preserves pause'),('finally { if(!completed)session.SetPaused(paused); }','finally { }','already dungeon preserves pause')]:
  changed=source.replace(before,after)
  if label.startswith('near'):changed=changed.replace('finally { session.SetPaused(completed?false:paused); }','finally { }')
  assert changed!=source;cs.write_text(changed);subprocess.run(build,env=env,check=True);r=subprocess.run(run,env=env,text=True,capture_output=True);print(r.stdout+r.stderr);assert r.returncode and label in r.stdout+r.stderr;print('PASS compiled old-behavior negative:',label)
