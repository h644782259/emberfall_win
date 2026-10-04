"""Actual arena settlement host + reward transaction/state; only engine/UI edges stubbed."""
from pathlib import Path
import os,sys,tempfile,subprocess
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
def member(s,key):
 a=s.index(key);b=s.index('{',a)+1;d=1
 while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
source=(root/'Assets/Scripts/Core/GameSession.Modes.cs').read_text()
methods=''.join(member(source,key) for key in ['private static long TotalEarnedExperience(','private void ApplyRewardPresentation(','private string RewardPresentationText(','public bool TrySettleArenaReward('])
fixture='''using System;using UnityEngine;namespace UnityEngine{public static class Mathf{public static int Max(int a,int b)=>Math.Max(a,b);public static int RoundToInt(float n)=>(int)Math.Round(n);}}
namespace Emberfall{public enum RunBlessing{RiskContract}public sealed partial class GameSession{
public bool ChapterActive;public object RoomChainRun;public ExpeditionModeState ModeRun;public ProgressionService Progression;public string modeReceipt;public int DungeonTier=1,Logs;public bool ThrowBeforeGrant;
int modeGoldReward,modeXpReward,modeMaterialReward;bool modeRewardDetailsUnavailable;public string LastRunSummary,LastLog;
bool TrySettleChapterReward()=>true;bool TrySettleRoomReward()=>true;bool HasBlessing(RunBlessing b){if(ThrowBeforeGrant)throw new InvalidOperationException("precommit blessing lookup fault");return false;}
void Notify(string s){}void LogSystem(string s){Logs++;LastLog=s;}string BuildRunSummary(bool win)=>"summary";
'''+methods+'}}'
core=['SafeSaveFlow','GameTypes','ProgressionService','ProgressionService.Reforge','ReforgeQuote','ProgressionService.Chapter','ChapterProgression','RoomTactics','CombatBalance','HubTravelRules','MasteryCoreRuntime','CastFirstHitReceipt','TierRewardRules','TierRewardBand','ProgressionGoalState','ExpeditionModeState']
with tempfile.TemporaryDirectory(prefix='arena-reward-exceptions-') as tmp:
 p=Path(tmp)
 for name in core:(p/(name+'.cs')).write_text((root/'Assets/Scripts/Core'/(name+'.cs')).read_text())
 for name in ['ProgressionTests','ArenaRewardExceptionTests']:(p/(name+'.cs')).write_text((root/'Tests'/(name+'.cs')).read_text())
 (p/'Host.cs').write_text(fixture);(p/'Program.cs').write_text('System.Console.WriteLine(ArenaRewardExceptionTests.Run(args[0],args.Length>1));')
 shim=p/'ProgressionTests.cs';text=shim.read_text();text=text.replace('public static void LogWarning(object message)', 'public static readonly System.Collections.Generic.List<string> Warnings=new System.Collections.Generic.List<string>();\n        public static void LogWarning(object message)').replace('Console.WriteLine(message);','Warnings.Add(message.ToString());Console.WriteLine(message);');shim.write_text(text)
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 command=[dotnet,'run','--project',str(p/'Test.csproj'),'--',str(p/'save')]
 subprocess.run(command,env=env,check=True)
 # Defensive subcase only: deliberately bypass central isolation in copied temporary
 # production source to simulate a future exception after commit but before return.
 service=p/'ProgressionService.cs';current=service.read_text()
 bypass=current.replace(member(current,'private void RaiseChanged('),'private void RaiseChanged(){if(Changed!=null)Changed();}').replace(member(current,'private void RaiseLeveledUp('),'private void RaiseLeveledUp(int level){if(LeveledUp!=null)LeveledUp(level);}')
 service.write_text(bypass)
 result=subprocess.run(command[:-1]+[str(p/'isolation-negative')],env=env,capture_output=True,text=True)
 print('NEGATIVE CONTROL: normal observer isolation removed',flush=True);print(result.stdout+result.stderr,flush=True)
 assert result.returncode and 'normal isolation continues later subscriber' in result.stdout+result.stderr
 print('DEFENSIVE SUBCASE: temporary observer-isolation bypass; production source unchanged',flush=True)
 subprocess.run(command[:-1]+[str(p/'defense-save'),'defense'],env=env,check=True)
 for index,(old,new,oracle) in enumerate([
  ('finally {rewardRun.CompleteReward(ticket,saved);}','finally {}','committed subscriber exception must not strand reservation'),
  ('saved=verification.LoadSlot(rewardSlot)&&verification.Profile.lastModeRewardId==receipt;','saved=false;','durable receipt reconciles original ticket after callback fault'),
  ('finally {rewardRun.CompleteReward(ticket,saved);}','finally {ModeRun.CompleteReward(ticket,saved);}','callback host replacement settles only reserved original run')]):
  assert old in fixture
  (p/'Host.cs').write_text(fixture.replace(old,new))
  result=subprocess.run(command[:-1]+[str(p/('negative-'+str(index))),'defense'],env=env,capture_output=True,text=True)
  print('NEGATIVE CONTROL:',old,flush=True);print(result.stdout+result.stderr,flush=True)
  assert result.returncode and oracle in result.stdout+result.stderr
 print('PASS: compiled reservation/reconciliation/owner negative controls rejected')
