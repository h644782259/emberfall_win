#!/usr/bin/env python3
"""Production collection adapter/service with pre/post-commit fault injection."""
from pathlib import Path
import os,sys,tempfile,subprocess
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
s=(root/'Assets/Scripts/Core/GameSession.cs').read_text();a=s.index('public bool TryCollectGroundLoot(string itemId');b=s.index('{',a)+1;n=1
while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
method=s[a:b]
core=['GameTypes','CombatBalance','ProgressionService','HubTravelRules','MasteryCoreRuntime','CastFirstHitReceipt','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Reforge','ReforgeQuote','RoomTactics']
with tempfile.TemporaryDirectory(prefix='loot-callback-') as d:
 p=Path(d)
 for name in core:(p/(name+'.cs')).write_text((root/'Assets/Scripts/Core'/(name+'.cs')).read_text())
 shim=(root/'Tests/ProgressionTests.cs').read_text().replace('public static void LogWarning(object message)','public static readonly System.Collections.Generic.List<string> Warnings=new System.Collections.Generic.List<string>();\n        public static void LogWarning(object message)').replace('Console.WriteLine(message);','Warnings.Add(message.ToString());Console.WriteLine(message);')
 (p/'ProgressionTests.cs').write_text(shim)
 (p/'Boundary.cs').write_text((root/'Tests/DeathLootPersistenceProductionTests.cs').read_text().split('class Program{')[0])
 (p/'Tests.cs').write_text((root/'Tests/GroundLootCallbackRecoveryTests.cs').read_text())
 adapter=p/'Adapter.cs';original='namespace Emberfall{public partial class GameSession{'+method+'}}';adapter.write_text(original)
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');cmd=[dotnet,'run','--project',str(p/'Test.csproj'),'--',str(p/'saves')]
 subprocess.run(cmd,env=env,check=True)
 # Explicit defensive subcase: fault after persistence and before transaction return.
 service=p/'ProgressionService.cs';current=service.read_text()
 def member(text,key):
  a=text.index(key);b=text.index('{',a)+1;depth=1
  while depth:depth+=(text[b]=='{')-(text[b]=='}');b+=1
  return text[a:b]
 service.write_text(current.replace(member(current,'private void RaiseChanged('),'private void RaiseChanged(){if(Changed!=null)Changed();}'))
 result=subprocess.run(cmd[:-1]+[str(p/'isolation-negative')],env=env,capture_output=True,text=True)
 print('NEGATIVE CONTROL: normal observer isolation removed',flush=True);print(result.stdout+result.stderr,flush=True)
 assert result.returncode and 'normal callback fault isolated' in result.stdout+result.stderr
 print('DEFENSIVE SUBCASE: temporary observer-isolation bypass; production source unchanged',flush=True)
 subprocess.run(cmd[:-1]+[str(p/'defense-saves'),'defense'],env=env,check=True)
 assert 'accepted || source.HasCommittedWorldLoot(itemId)' in original
 adapter.write_text(original.replace('accepted || source.HasCommittedWorldLoot(itemId)','accepted'))
 result=subprocess.run(cmd[:-1]+[str(p/'negative-saves'),'defense'],env=env,capture_output=True,text=True);print('NEGATIVE CONTROL: omit committed receipt reconciliation');print(result.stdout+result.stderr);assert result.returncode and 'committed callback failure retires pickup' in result.stdout+result.stderr
 print('PASS: observer isolation and committed receipt reconciliation negative controls rejected')
