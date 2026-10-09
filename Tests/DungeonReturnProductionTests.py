from pathlib import Path
import importlib.util,os,subprocess,tempfile
root=Path(__file__).resolve().parents[1]
def member(text,signature):
 a=text.index(signature);b=text.index('{',a);depth=1;e=b+1
 while depth:
  if text[e]=='{':depth+=1
  elif text[e]=='}':depth-=1
  e+=1
 return text[a:e]
exp=(root/'Assets/Scripts/Core/GameSession.Expedition.cs').read_text();session=(root/'Assets/Scripts/Core/GameSession.cs').read_text()
actual='\n'.join(member(exp,s) for s in ['public bool FinishedResultDismissed','public void DismissFinishedResult()','public bool DungeonReturnAvailable','public bool NearDungeonReturn'])+'\n'+member(session,'public bool InputBlocked')
fixture='''using System;
struct Vector3{public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static float Distance(Vector3 a,Vector3 b){return (float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y)+(a.z-b.z)*(a.z-b.z));}}
class Transform{public Vector3 position;}
class PlayerController{public int CombatEpoch;public Transform transform=new Transform();}
class Flag{public bool BackgroundPaused,Finished,AwaitingChoice;}
class Session{
 public bool HasStarted=true,InDungeon=true,DungeonCleared,ModeFinished,IsDead,Paused,uiBlocking,PracticeActive,RoomBranchChoiceOpen,DungeonSelectionOpen;
 public PlayerController Player=new PlayerController(),dismissedResultOwner;public int dismissedResultEpoch=-1;public Vector3 dungeonReturnPosition=new Vector3(0,0,-16);
 public Flag pauseState=new Flag(),PracticeRecord=new Flag(),RunChoices=new Flag();
 public void SetUIBlocking(bool value){uiBlocking=value;}public void UpdateTimeScale(){}
'''+actual+'''}
class Program{static int n;static void Check(bool x,string why){n++;if(!x)throw new Exception(why);}static void Main(){var s=new Session();Check(!s.DungeonReturnAvailable,"no exit during combat");s.DungeonCleared=true;Check(s.DungeonReturnAvailable,"normal clear exit");Check(!s.NearDungeonReturn,"not globally interactable");s.Player.transform.position=new Vector3(0,0,-15);Check(s.NearDungeonReturn,"near exit");s.DungeonCleared=false;s.ModeFinished=true;Check(s.DungeonReturnAvailable&&s.InputBlocked,"mode result blocks initially");s.DismissFinishedResult();Check(!s.InputBlocked&&s.NearDungeonReturn,"dismiss restores movement and exit interaction");s.Paused=true;Check(s.InputBlocked,"pause still blocks");s.Paused=false;s.Player.CombatEpoch++;Check(s.InputBlocked&&!s.FinishedResultDismissed,"next run cannot inherit dismissal");s.IsDead=true;Check(!s.DungeonReturnAvailable,"dead cannot use exit");Console.WriteLine(n+" return portal checks passed");}}
'''
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
with tempfile.TemporaryDirectory(prefix='return-portal-test-') as directory:
 p=Path(directory);proj=m.write_project(p/'project',[],fixture);env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'))
 result=subprocess.run(['/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(proj)],env=env,capture_output=True,text=True);print(result.stdout,result.stderr);raise SystemExit(result.returncode)
