"""Actual save-as/reward service regression; isolated managed JSON/filesystem boundary."""
from pathlib import Path
import os,sys,tempfile,subprocess
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
core=['SafeSaveFlow','GameTypes','ProgressionService','ProgressionService.Reforge','ReforgeQuote','ProgressionService.Chapter','ChapterProgression','RoomTactics','CombatBalance','HubTravelRules','MasteryCoreRuntime','CastFirstHitReceipt','TierRewardRules','TierRewardBand','ProgressionGoalState']
with tempfile.TemporaryDirectory(prefix='chest-saveas-retry-') as tmp:
 p=Path(tmp)
 for name in core:(p/(name+'.cs')).write_text((root/'Assets/Scripts/Core'/(name+'.cs')).read_text())
 for name in ['ProgressionTests','ChestSnapshotRetryTests']:(p/(name+'.cs')).write_text((root/'Tests'/(name+'.cs')).read_text())
 (p/'Program.cs').write_text('System.Console.WriteLine(ChestSnapshotRetryTests.Run(args[0]));')
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 command=[dotnet,'run','--project',str(p/'Test.csproj'),'--',str(p/'saves')]
 subprocess.run(command,env=env,check=True)
 source=p/'ProgressionService.cs';original=source.read_text()
 seam='if (!newCharacter) pendingChestContexts.TryGetValue(SaveFilePath, out snapshotDraw);'
 assert original.count(seam)==1
 durable='GameProfile snapshot = Snapshot();'
 assert original.count(durable)==1
 source.write_text(original.replace(seam,'/* regression: copied role loses process draw */').replace(durable,'GameProfile snapshot = JsonUtility.FromJson<GameProfile>(JsonUtility.ToJson(Profile, true));'))
 result=subprocess.run(command[:-1]+[str(p/'negative-saves')],env=env,capture_output=True,text=True)
 print('NEGATIVE CONTROL: drop both durable and process save-as pending draw inheritance',flush=True)
 print(result.stdout+result.stderr,flush=True)
 assert result.returncode and 'save-as snapshot must preserve failed draw identity' in result.stdout+result.stderr
 print('PASS: compiled lost snapshot draw negative control rejected')
