#!/usr/bin/env python3
"""Compile actual persistence service and actual desktop/mobile draft UI, not copied logic."""
import importlib.util,os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('validation',root/'Tools/cloud-validation.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
with tempfile.TemporaryDirectory(prefix='camp-draft-') as directory:
 p=Path(directory);sources=[root/('Assets/Scripts/Core/'+f+'.cs') for f in ['SkillRuntime','GameTypes','ProgressionService','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics']]
 sources += [root/('Assets/Scripts/UI/'+f+'.cs') for f in ['GameUI.BuildPlans','GameUI.BuildDraft','MobilePanelLayout']]
 sources += [root/('Tests/'+f+'.cs') for f in ['ProgressionTests','CampBuildDraftTests','CampBuildDraftUIBoundary','BuildPresetTests']]
 # Extend this isolated Unity API double for the production wrapped-button color expression.
 boundary=p/'ProgressionBoundary.cs';boundary.write_text((root/'Tests/ProgressionTests.cs').read_text().replace('public struct Color {', 'public struct Color { public static Color operator *(Color c,float f)=>c;'))
 sources=[boundary if f==root/'Tests/ProgressionTests.cs' else f for f in sources]

 def method(source,signature):
  a=source.index(signature);b=source.index('{',a)+1;depth=1
  while depth:depth+=(source[b]=='{')-(source[b]=='}');b+=1
  return source[a:b]
 runtime=p/'Runtime.cs';runtime.write_text('namespace Emberfall{public partial class PlayerController{'+method((root/'Assets/Scripts/Combat/PlayerController.cs').read_text(),'public void RefreshStats(bool heal)')+'}public partial class GameSession{'+method((root/'Assets/Scripts/Core/GameSession.cs').read_text(),'private void OnProgressChanged()')+'}}')
 runtime.write_text('using UnityEngine;'+runtime.read_text());sources.append(runtime)
 service=p/'ProgressionService.cs';service.write_text((root/'Assets/Scripts/Core/ProgressionService.cs').read_text());sources=[service if f==root/'Assets/Scripts/Core/ProgressionService.cs' else f for f in sources]
 project=m.write_project(p/'project',sources,'using System;class Program{static void Main(string[] args){Console.WriteLine(CampBuildDraftTests.Run(args[0]));Console.WriteLine(BuildPresetTests.Run(args[0]));}}')
 config=p/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1');dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
 for args in [['restore',str(project),'--configfile',str(config),'-v:q'],['run','--project',str(project),'--no-restore','--',str(p/'saves')]]:
  q=subprocess.run([dotnet]+args,env=env,capture_output=True,text=True);print(q.stdout,q.stderr);assert q.returncode==0
 # Every negative control must compile and fail its intended behavioral assertion.
 originals={runtime:runtime.read_text(),service:service.read_text()}
 controls=[
  (runtime,'Mathf.Min(Health, MaxHealth) : Mathf.Clamp(Health, 1, MaxHealth)','Mathf.Clamp(Health, 1, MaxHealth) : Mathf.Clamp(Health, 1, MaxHealth)','draft no-op preserves fractional HP and dead zero exactly'),
  (service,'owner.CommitCandidate(candidate,true)','owner.CommitCandidate(candidate,false)','draft no-op preserves fractional HP and dead zero exactly'),
  (runtime,': Mathf.Clamp(Health, 1, MaxHealth)',': Mathf.Min(Health, MaxHealth)','non-draft legacy refresh floor is unchanged'),
  (service,'finally { IsApplyingBuildDraft=previousDraft; }','finally { }','draft no-op preserves fractional HP and dead zero exactly')]
 for index,(file,old,new,message) in enumerate(controls):
  assert old in originals[file];file.write_text(originals[file].replace(old,new))
  q=subprocess.run([dotnet,'build',str(project),'--no-restore','-v:q'],env=env,capture_output=True,text=True);assert q.returncode==0,q.stdout+q.stderr
  q=subprocess.run([dotnet,str(project.parent/'bin/Debug/net8.0/Validation.dll'),str(p/('negative-'+str(index)))],env=env,capture_output=True,text=True)
  assert q.returncode!=0 and message in q.stdout+q.stderr,q.stdout+q.stderr
  print('PASS compiled negative control rejected:',message)
  file.write_text(originals[file])
# Explicit runtime preservation boundary: successful commits use the existing non-refill refresh path.
s=(root/'Assets/Scripts/Core/GameSession.cs').read_text();a=s.index('private void OnProgressChanged()');b=s.index('private void OnLevelUp',a);assert 'Player.RefreshStats(false)' in s[a:b] and 'RefreshStats(true)' not in s[a:b]
print('PASS existing post-commit session path remains non-refill (source guard; Unity runtime not executed)')
