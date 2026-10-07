#!/usr/bin/env python3
"""Compile actual runtime and GroundLootValidation in separate assemblies.
Only UnityEditor timing/session APIs are stubbed; no runtime types are replaced.
This is an accessibility regression test, not a Unity Editor import test.
"""
from pathlib import Path
import importlib.util,os,subprocess,sys,tempfile
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
with tempfile.TemporaryDirectory(prefix='emberfall-editor-boundary-') as tmp:
 p=Path(tmp);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
 refs=list(cv.unity_references(False).glob('*.dll'))
 def project(folder,sources,name,references):
  proj=cv.write_project(p/folder,sources,references=references,defines='UNITY_STANDALONE_WIN;UNITY_EDITOR')
  proj.write_text(proj.read_text().replace('<PropertyGroup>','<PropertyGroup><AssemblyName>'+name+'</AssemblyName>',1));return proj
 def build(proj):
  result=subprocess.run([sys.argv[1],'build',str(proj),'-c','Release','-v','minimal'],env=env,text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
  print(result.stdout,flush=True);return result
 shim=p/'UnityEditorShim.cs';shim.write_text('namespace UnityEditor { public static class EditorApplication { public static double timeSinceStartup => 0; public static bool isPlaying { get; set; } } public static class SessionState { public static bool GetBool(string key,bool value)=>value; public static string GetString(string key,string value)=>value; } }')
 shimProject=project('unity-editor-shim',[shim],'UnityEditorBoundaryShim',[]);assert build(shimProject).returncode==0
 shimDll=p/'unity-editor-shim/bin/Release/net8.0/UnityEditorBoundaryShim.dll';refs.append(shimDll)
 runtime=project('runtime',sorted((root/'Assets/Scripts').rglob('*.cs')),'EmberfallRuntime',refs)
 assert build(runtime).returncode==0,'actual runtime compile'
 dll=p/'runtime/bin/Release/net8.0/EmberfallRuntime.dll';assert dll.is_file()
 actual=root/'Assets/Editor/GroundLootValidation.cs'
 good=project('editor',[actual],'EmberfallEditorBoundary',[*refs,dll]);assert build(good).returncode==0,'separate editor assembly compile'
 old=p/'GroundLootValidation.Old.cs';text=actual.read_text();assert text.count('ReadCombatEpoch(game.Player)')==2
 old.write_text(text.replace('ReadCombatEpoch(game.Player)','game.Player.CombatEpoch'))
 bad=project('negative',[old],'EmberfallEditorNegative',[*refs,dll]);result=build(bad)
 assert result.returncode!=0 and 'CombatEpoch' in result.stdout and ('CS1061' in result.stdout or 'CS0122' in result.stdout),'old direct access must fail'
 print('PASS separate actual runtime/editor assemblies; old direct access negative control fails; UnityEditor APIs explicitly stubbed, no Unity import claim')
