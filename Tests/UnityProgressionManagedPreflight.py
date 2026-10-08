#!/usr/bin/env python3
"""Run the entire Unity validation entry with a managed engine/JSON boundary. This is NOT Unity or real JsonUtility validation. Usage: python Tests/UnityProgressionManagedPreflight.py DOTNET OUTPUT_DIRECTORY."""
from pathlib import Path
import importlib.util,sys,subprocess,os
r=Path(__file__).resolve().parents[1];out=Path(sys.argv[2]).resolve();out.mkdir(parents=True,exist_ok=True)
spec=importlib.util.spec_from_file_location('cv',r/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
fake=(r/'Tests/ProgressionTests.cs').read_text().replace('public static string persistentDataPath;','public static string persistentDataPath;public static string dataPath;public static string unityVersion="managed-boundary";public static string version="0.4.1";').replace('public static void LogWarning(object message)', 'public static void Log(object message){Console.WriteLine(message);}public static void LogError(object message){Console.WriteLine(message);}public static void LogWarning(object message)')
fake=fake.replace('bool pretty)', 'bool pretty=false)')
(out/'Boundary.cs').write_text(fake);(out/'EditorBoundary.cs').write_text('namespace UnityEditor{public class MenuItem:System.Attribute{public MenuItem(string name){}}}')
names=['GameTypes','ProgressionService','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','SkillRuntime','CombatImpactBatch','SafeSaveFlow']
source=[r/('Assets/Scripts/Core/'+n+'.cs') for n in names]+[out/'Boundary.cs',out/'EditorBoundary.cs',r/'Assets/Editor/ProgressionValidation.cs']
p=out/'project'
if p.exists():import shutil;shutil.rmtree(p)
project=cv.write_project(p,source,'class Program{static void Main(){UnityEngine.Application.dataPath="'+str(out)+'/Assets";System.IO.Directory.CreateDirectory(UnityEngine.Application.dataPath);Emberfall.Editor.ProgressionValidation.ValidateFull();}}')
env=dict(os.environ,DOTNET_NOLOGO='1',DOTNET_CLI_HOME=str(out/'cli'));q=subprocess.run([sys.argv[1],'run','--project',str(project)],env=env,capture_output=True,text=True);(out/'run.log').write_text(q.stdout+q.stderr);print(q.stdout+q.stderr);sys.exit(q.returncode)
