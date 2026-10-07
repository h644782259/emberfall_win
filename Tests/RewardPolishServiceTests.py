from pathlib import Path
import importlib.util,tempfile,subprocess,os,sys
root=Path(__file__).resolve().parents[1];spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
with tempfile.TemporaryDirectory(prefix='class-enemy-kill-') as d:
 p=Path(d);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['SkillRuntime','GameTypes','ProgressionService','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+n+'.cs') for n in names]+[root/'Tests/ProgressionTests.cs',root/'Tests/RewardPolishServiceTests.cs',root/'Assets/Scripts/UI/ChestRevealPresentation.cs']
 service=p/'Service.cs';original=(root/'Assets/Scripts/Core/ProgressionService.cs').read_text()
 seam='private bool TryWriteAttachedProfile(GameProfile profile, out string failure)\n        {'
 assert seam in original
 service.write_text(original.replace(seam,seam+'\n            if(profile.pendingChestReveal&&RewardPolishFaults.GrantOnce){RewardPolishFaults.GrantOnce=false;failure="injected grant write failure";return false;}',1))
 sources=[service if f==root/'Assets/Scripts/Core/ProgressionService.cs' else f for f in sources]
 project=cv.write_project(p/'project',sources,'using System;class Program{static void Main(string[] a){Console.WriteLine(RewardPolishServiceTests.Run(a[0]));}}')
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))

 original=service.read_text();sdk=sys.argv[1] if len(sys.argv)>1 else 'dotnet';env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 controls=[
  ('if(error=="future format"||IsFrozenRewardReadError(error))return false;','if(error=="future format")return false;','nested frozen corruption must not recover undrawn backup: slot'),
  ('if(!usablePrimary&&(readError=="future format"||IsFrozenRewardReadError(readError)))throw new IOException("主档含不可回退的奖励记录，原主档和备份均保留。");','','failed load/save preserves both protected documents:'),
  ('after.health>=before.health&&','true&&','higher weighted score with health loss is not lossless')]
 for index,(before,after,expected) in enumerate(controls):
  assert before in original;service.write_text(original.replace(before,after,1))
  build=subprocess.run([sdk,'build',str(project),'--no-restore','-v:q'],env=env,capture_output=True,text=True);assert build.returncode==0,build.stdout+build.stderr
  run=subprocess.run([sdk,str(project.parent/'bin/Debug/net8.0/Validation.dll'),str(p/('negative-'+str(index)))],env=env,capture_output=True,text=True)
  assert run.returncode!=0 and expected in run.stdout+run.stderr,run.stdout+run.stderr
  print('PASS compiled reward-polish service negative: '+expected)
