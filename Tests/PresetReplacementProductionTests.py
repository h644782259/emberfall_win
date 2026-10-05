#!/usr/bin/env python3
"""Production service / record / energy tests plus compiled negative controls; not Unity execution."""
import importlib.util,os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('validation',root/'Tools/cloud-validation.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
with tempfile.TemporaryDirectory(prefix='camp-practice-') as directory:
 p=Path(directory);sources=[root/('Assets/Scripts/Core/'+f+'.cs') for f in ['SkillRuntime','GameTypes','ProgressionService','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','CampPracticeRecord','SafeSaveFlow']]
 sources += [root/'Tests/ProgressionTests.cs',root/'Tests/PresetReplacementTests.cs']
 copies={}
 for name in ['ProgressionService','CombatImpactBatch','CampPracticeRecord','SkillRuntime','SafeSaveFlow']:
  old=root/('Assets/Scripts/Core/'+name+'.cs');new=p/(name+'.cs');new.write_text(old.read_text());sources=[new if f==old else f for f in sources];copies[new]=new.read_text()
 ui=(root/'Assets/Scripts/UI/GameUI.BuildPlans.cs').read_text()
 def member(key):
  a=ui.index(key);b=ui.index('{',a)+1;n=1
  while n:n+=(ui[b]=='{')-(ui[b]=='}');b+=1
  return ui[a:b]
 surface=p/'UI.cs';surface.write_text('using UnityEngine;namespace Emberfall{public partial class GameUI{'+''.join(member(k) for k in ['private static string BuildPlanName(', 'private void RequestBuildPlanAction(','private void BeginPresetReplacement(','private void PreviewPresetReplacement(','private void ConfirmBuildPlanAction()','private bool CloseBuildPlanSurface()','private void RequestPresetSale(','private void ConfirmPresetSale()','private void CancelPresetSale()'])+'}}');sources.append(surface)
 project=m.write_project(p/'project',sources,'using System;class Program{static void Main(string[] args){Console.WriteLine(PresetReplacementTests.Run(args[0]));}}')
 config=p/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1');dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
 def run(args):
  q=subprocess.run([dotnet]+args,env=env,capture_output=True,text=True);print(q.stdout+q.stderr);return q
 assert run(['restore',str(project),'--configfile',str(config),'-v:q']).returncode==0
 assert run(['run','--project',str(project),'--no-restore','--',str(p/'saves')]).returncode==0
 controls=[('ProgressionService','preset.equipmentMechanicKnownMask=knownMask|(1<<quote.Slot);','preset.equipmentMechanicKnownMask=7;','partial legacy repair keeps missing weapon unknown in actual UI'),('ProgressionService','if(!confirmPresetReferences&&PresetReferences(id).Length>0)','if(false)','reference markers and sale require confirmation'),('ProgressionService','preset.equipmentVariants[quote.Slot]=quote.Variant;','preset.equipmentVariants[quote.Slot]=0;','only selected plan slot changes')]
 for i,(name,old,new,message) in enumerate(controls):
  f=p/(name+'.cs');assert old in copies[f];f.write_text(copies[f].replace(old,new));q=run(['build',str(project),'--no-restore','-v:q']);assert q.returncode==0
  q=run([str(project.parent/'bin/Debug/net8.0/Validation.dll'),str(p/('negative-'+str(i)))]);assert q.returncode!=0 and message in q.stdout+q.stderr
  print('PASS compiled negative rejected:',message);f.write_text(copies[f])

 service=p/'ProgressionService.cs';service.write_text(copies[service].replace('||quote.State!=BuildStateFingerprint()',''))
 saved=surface.read_text();surface.write_text(saved.replace('||buildPlanFingerprint!=p.BuildStateFingerprint()',''))
 assert run(['build',str(project),'--no-restore','-v:q']).returncode==0
 q=run([str(project.parent/'bin/Debug/net8.0/Validation.dll'),str(p/'stale-negative')]);assert q.returncode!=0 and 'actual UI rejects in-place upgrade stale quote' in q.stdout+q.stderr
 print('PASS compiled stale UI negative rejected')
