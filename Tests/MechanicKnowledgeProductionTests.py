#!/usr/bin/env python3
"""Production service / record / energy tests plus compiled negative controls; not Unity execution."""
import importlib.util,os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('validation',root/'Tools/cloud-validation.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
with tempfile.TemporaryDirectory(prefix='camp-practice-') as directory:
 p=Path(directory);sources=[root/('Assets/Scripts/Core/'+f+'.cs') for f in ['SkillRuntime','GameTypes','ProgressionService','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','CampPracticeRecord','SafeSaveFlow']]
 sources += [root/'Tests/ProgressionTests.cs',root/'Tests/MechanicKnowledgeTests.cs']
 copies={}
 for name in ['ProgressionService','CombatImpactBatch','CampPracticeRecord','SkillRuntime','SafeSaveFlow']:
  old=root/('Assets/Scripts/Core/'+name+'.cs');new=p/(name+'.cs');new.write_text(old.read_text());sources=[new if f==old else f for f in sources];copies[new]=new.read_text()
 project=m.write_project(p/'project',sources,'using System;class Program{static void Main(string[] args){Console.WriteLine(MechanicKnowledgeTests.Run(args[0]));}}')
 config=p/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1');dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
 def run(args):
  q=subprocess.run([dotnet]+args,env=env,capture_output=True,text=True);print(q.stdout+q.stderr);return q
 assert run(['restore',str(project),'--configfile',str(config),'-v:q']).returncode==0
 assert run(['run','--project',str(project),'--no-restore','--',str(p/'saves')]).returncode==0
 controls=[('ProgressionService','if(!candidate.variantKnowledge.Contains(item.mechanic))candidate.variantKnowledge.Add(item.mechanic);','','four shards buys character mechanism knowledge'),('ProgressionService','migrationRequired && !TryWriteProfile(loaded, candidatePath, false, out migrationFailure)','false && !TryWriteProfile(loaded, candidatePath, false, out migrationFailure)','legacy migration write failure never publishes knowledge')]
 for i,(name,old,new,message) in enumerate(controls):
  f=p/(name+'.cs');assert old in copies[f];f.write_text(copies[f].replace(old,new));q=run(['build',str(project),'--no-restore','-v:q']);assert q.returncode==0
  q=run([str(project.parent/'bin/Debug/net8.0/Validation.dll'),str(p/('negative-'+str(i)))]);assert q.returncode!=0 and message in q.stdout+q.stderr
  print('PASS compiled negative rejected:',message);f.write_text(copies[f])
