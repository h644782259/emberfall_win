from pathlib import Path
import importlib.util,tempfile,subprocess,os,sys
root=Path(__file__).resolve().parents[1];spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
with tempfile.TemporaryDirectory(prefix='class-enemy-kill-') as d:
 p=Path(d);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['SkillRuntime','GameTypes','ProgressionService','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+n+'.cs') for n in names]+[root/'Tests/ProgressionTests.cs',root/'Tests/SingleChestTests.cs']
 service=p/'Service.cs';original=(root/'Assets/Scripts/Core/ProgressionService.cs').read_text()
 seam='private bool TryWriteAttachedProfile(GameProfile profile, out string failure)\n        {'
 assert seam in original
 service.write_text(original.replace(seam,seam+'\n            if(profile.pendingChestReveal&&SingleChestFaults.GrantOnce){SingleChestFaults.GrantOnce=false;failure="injected grant write failure";return false;}',1))
 sources=[service if f==root/'Assets/Scripts/Core/ProgressionService.cs' else f for f in sources]
 project=cv.write_project(p/'project',sources,'using System;class Program{static void Main(string[] a){Console.WriteLine(SingleChestTests.Run(a[0]));}}')
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
