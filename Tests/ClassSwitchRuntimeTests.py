#!/usr/bin/env python3
"""Compile actual session/player switching methods against explicit managed Unity boundaries."""
import importlib.util,os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
def member(text,key):
 a=text.index(key);b=text.index('{',a)+1;n=1
 while n:n+=(text[b]=='{')-(text[b]=='}');b+=1
 return text[a:b]
with tempfile.TemporaryDirectory(prefix='class-runtime-') as d:
 p=Path(d);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 core=['SkillRuntime','GameTypes','ProgressionService','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow','CastFirstHitReceipt']
 sources=[root/('Assets/Scripts/Core/'+f+'.cs') for f in core]
 sources += [root/'Assets/Scripts/Core/GameSession.ClassSwitch.cs',root/'Assets/Scripts/Combat/PlayerController.ClassSwitch.cs',root/'Assets/Scripts/Combat/PlayerController.CastReceipts.cs',root/'Assets/Scripts/Combat/PlayerUpgradeRules.cs',root/'Tests/ClassSwitchRuntimeBoundary.cs']
 boundary=p/'ProgressionBoundary.cs';boundary.write_text((root/'Tests/ProgressionTests.cs').read_text().replace('{ throw new InvalidOperationException("Unexpected production exception logged at Unity boundary", exception); }', '{ if(exception.Message=="fixture visual preparation failure"){Console.WriteLine("EXPECTED injected model preparation failure");return;}throw new InvalidOperationException("Unexpected production exception logged at Unity boundary", exception); }'));sources.append(boundary)
 text=(root/'Assets/Scripts/Combat/PlayerController.cs').read_text();methods=p/'PlayerInitialization.cs';methods.write_text('using UnityEngine;namespace Emberfall{public sealed partial class PlayerController{'+member(text,'public void Initialize(')+member(text,'private void BindSkillStock(')+member(text,'public void RefreshStats(')+'}}');sources.append(methods)
 copies={}
 for name in ['GameSession.ClassSwitch','PlayerController.ClassSwitch','SkillRuntime','ProgressionService']:
  old=next(f for f in sources if f.name==name+'.cs');new=p/(name+'.cs');original=old.read_text()
  if name=='ProgressionService':
   seam='private bool TryWriteAttachedProfile(GameProfile profile, out string failure)\n        {'
   assert original.count(seam)==1
   original=original.replace(seam,seam+'\n            if(ClassSwitchPersistenceFaults.Armed&&++ClassSwitchPersistenceFaults.Attempts==2){failure="injected second target save failure";return false;}')
  new.write_text(original);copies[new]=new.read_text();sources=[new if f==old else f for f in sources]
 project=cv.write_project(p/'project',sources);source=project.read_text().replace('<OutputType>Library</OutputType>','<OutputType>Exe</OutputType>');project.write_text(source)
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');sdk=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
 subprocess.run([sdk,'run','--project',str(project),'--',str(p/'saves')],env=env,check=True)

 controls=[
  ('GameSession.ClassSwitch','if(!transaction.Committed)UnityEngine.Random.state=randomBefore;','/* regression: failed preparation advances combat RNG */','failed staged model restores consumed combat RNG'),
  ('GameSession.ClassSwitch','if(!transaction.Committed)UnityEngine.Random.state=randomBefore;','UnityEngine.Random.state=randomBefore;','successful staged model keeps random consumption'),
  ('GameSession.ClassSwitch','Player=prepared;','Progression.PublishClassSwitch(transaction);Player=prepared;','mixed owner publication'),
  ('PlayerController.ClassSwitch','Health=Mathf.Clamp01(shared.HealthFraction)*MaxHealth;','Health=MaxHealth;','actual player install preserves health ratio energy and cooldowns'),
  ('SkillRuntime','copy.Energy=float.IsNaN(currentEnergy)||float.IsInfinity(currentEnergy)?0:Math.Max(0,Math.Min(MaximumEnergy,currentEnergy));','copy.Energy=MaximumEnergy;','actual player install preserves health ratio energy and cooldowns')]
 for index,(name,before,after,expected) in enumerate(controls):
  file=p/(name+'.cs');assert before in copies[file];file.write_text(copies[file].replace(before,after,1))
  build=subprocess.run([sdk,'build',str(project),'--no-restore','-v:q'],env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True);assert build.returncode==0,build.stdout
  result=subprocess.run([sdk,str(project.parent/'bin/Debug/net8.0/Validation.dll'),str(p/('negative-'+str(index)))],env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True)
  assert result.returncode!=0 and expected in result.stdout,result.stdout
  print('PASS compiled class runtime negative: '+expected);file.write_text(copies[file])
