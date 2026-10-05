#!/usr/bin/env python3
"""Actual class-state persistence and immutable previous-reader rejection. No Unity execution."""
import importlib.util,os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('validation',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
sdk=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
core=['SkillRuntime','GameTypes','ProgressionService','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
with tempfile.TemporaryDirectory(prefix='class-switch-tests-') as directory:
 p=Path(directory);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
 sources=[root/('Assets/Scripts/Core/'+f+'.cs') for f in core]+[root/'Tests/ProgressionTests.cs',root/'Tests/ClassSwitchProductionTests.cs']
 service=p/'CurrentService.cs';original=(root/'Assets/Scripts/Core/ProgressionService.cs').read_text();service.write_text(original);sources=[service if f==root/'Assets/Scripts/Core/ProgressionService.cs' else f for f in sources]
 project=cv.write_project(p/'current',sources,'using System;class Program{static void Main(string[] args){Console.WriteLine(ClassSwitchProductionTests.Run(args[0]));}}')
 subprocess.run([sdk,'run','--project',str(project),'--',str(p/'saves')],env=env,check=True)
 mutation='profile.classStates[(int)profile.heroClass]=new ClassBuildState{initialized=true,heroClass=profile.heroClass};'
 assert mutation in original
 service.write_text(original.replace(mutation,'profile.classStates[(int)profile.heroClass]=CaptureClassState(profile);'))
 build=subprocess.run([sdk,'build',str(project),'--no-restore','-v:q'],env=env,capture_output=True,text=True);assert build.returncode==0,build.stdout+build.stderr
 run=subprocess.run([sdk,str(project.parent/'bin/Debug/net8.0/Validation.dll'),str(p/'negative-active-cache')],env=env,capture_output=True,text=True)
 assert run.returncode!=0 and 'active class fields have one authority after callback and save' in run.stdout+run.stderr,run.stdout+run.stderr
 print('PASS compiled active-class mirror regression rejected; callback/save authority assertion unchanged')
 service.write_text(original)
 # Fixed immutable UI-only parent revisions, never the current working tree.
 refs=('56a095307d9ad1ff46aff0839502d6caaea3ff5b','57e8b56a8f8f7e6ccaf042a5b8bfb085f6b68f23')
 ref=next(x for x in refs if subprocess.run(['git','cat-file','-e',x+':Assets/Scripts/Core/ProgressionService.cs'],cwd=root,stderr=subprocess.DEVNULL).returncode==0)
 old=[]
 for name in core:
  path=p/(name+'.cs');path.write_text(subprocess.check_output(['git','show',ref+':Assets/Scripts/Core/'+name+'.cs'],cwd=root,text=True));old.append(path)
 old_fixture=p/'OldProgressionTests.cs';old_fixture.write_text(subprocess.check_output(['git','show',ref+':Tests/ProgressionTests.cs'],cwd=root,text=True));old.append(old_fixture)
 program='''using System;using System.IO;using Emberfall;class Program{static void Main(string[] args){var p=new ProgressionService(args[0]);string before=File.ReadAllText(p.SaveFilePath),backup=File.ReadAllText(p.SaveFilePath+".bak");if(p.Load())throw new Exception("old reader must reject class archive");p.Save();if(File.ReadAllText(p.SaveFilePath)!=before||File.ReadAllText(p.SaveFilePath+".bak")!=backup)throw new Exception("old writer silently erased class archive");Console.WriteLine("PASS immutable pre-class reader and writer refuse both v2 documents");}}'''
 project=cv.write_project(p/'old',old,program)
 directory=(p/'saves/old-reader-directory.txt').read_text()
 subprocess.run([sdk,'run','--project',str(project),'--',directory],env=env,check=True)
