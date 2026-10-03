#!/usr/bin/env python3
"""Compile actual service/equipment/draft summaries and production venom budget, with behavior-negative mutations."""
import importlib.util,os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('v',root/'Tools/cloud-validation.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
with tempfile.TemporaryDirectory(prefix='draft-variant-effects-') as directory:
 p=Path(directory);names=['GameTypes','ProgressionService','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics']
 sources=[root/('Assets/Scripts/Core/'+f+'.cs') for f in names]+[root/'Assets/Scripts/Combat/ConcentratedVenomRules.cs',root/'Tests/ProgressionTests.cs',root/'Tests/BuildDraftVariantEffectsTests.cs']
 service=p/'ProgressionService.cs';original=(root/'Assets/Scripts/Core/ProgressionService.cs').read_text();service.write_text(original);sources=[service if f.name=='ProgressionService.cs' else f for f in sources]
 project=m.write_project(p/'project',sources,'using System;class Program{static void Main(string[] args){Console.WriteLine(BuildDraftVariantEffectsTests.Run(args[0]));}}')
 config=p/'NuGet.Config';config.write_text('<configuration><packageSources><clear/></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');dn=sys.argv[1]
 def run(args):
  q=subprocess.run([dn]+args,env=env,capture_output=True,text=True);print(q.stdout+q.stderr);return q
 assert run(['restore',str(project),'--configfile',str(config),'-v:q']).returncode==0
 assert run(['run','--project',str(project),'--no-restore','--',str(p/'saves')]).returncode==0
 for index,(old,new,expected) in enumerate([
  ('owner.SkillEffectSummary(index,before)','GameBalance.SkillEvolution(source.heroClass,index,before)','before uses actual equipped current mechanism'),
  ('preview.SkillEffectSummary(index,after)','GameBalance.SkillEvolution(source.heroClass,index,after)','B both before and after replace fan descriptions'),
  ('HasVariant(item)&&item.mechanicVariantUnlocked&&item.mechanicVariant==1','HasVariant(item)&&item.mechanicVariant==1','locked forged B cannot override preview')]):
  assert old in original;service.write_text(original.replace(old,new));assert run(['build',str(project),'--no-restore','-v:q']).returncode==0
  q=run([str(project.parent/'bin/Debug/net8.0/Validation.dll'),str(p/('negative-'+str(index)))]);assert q.returncode!=0 and expected in q.stdout+q.stderr;print('PASS compiled behavioral mutation rejected:',expected);service.write_text(original)
