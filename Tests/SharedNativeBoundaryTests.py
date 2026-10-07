"""Compile actual small method/property bodies; explicit host fields, not engine execution."""
from pathlib import Path
import importlib.util,tempfile,subprocess,os,sys
root=Path(__file__).resolve().parents[1];spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
def member(s,key):
 a=s.index(key);b=s.index('{',a)+1;depth=1
 while depth:depth+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
audio=member((root/'Assets/Scripts/Core/GameAudio.cs').read_text(),'private void ResetThrottle()')
mobile=(root/'Assets/Scripts/UI/MobileControls.cs').read_text();mobile=mobile[mobile.index('public static bool SimulationEnabled'):mobile.index('public static Vector2 Move')]
program='''using System;class Host {float lastImpact=42;float[] lastPlayed; AUDIO
public void Check(){ResetThrottle();if(!float.IsNegativeInfinity(lastImpact))throw new Exception("null voices still reset impact");lastImpact=42;lastPlayed=new[]{1f,2f};ResetThrottle();if(!float.IsNegativeInfinity(lastImpact)||Array.Exists(lastPlayed,x=>!float.IsNegativeInfinity(x)))throw new Exception("all throttle reset");}}
class Mobile { MOBILE }
class Program {static void Main(){new Host().Check();Mobile.SimulationEnabled=false;
#if UNITY_ANDROID || UNITY_IOS
if(!Mobile.Active)throw new Exception("platform default");
#else
if(Mobile.Active)throw new Exception("desktop default");
#endif
#if UNITY_EDITOR
Mobile.ValidationUsesSimulation=true;if(Mobile.Active)throw new Exception("editor desktop simulation");Mobile.SimulationEnabled=true;if(!Mobile.Active)throw new Exception("editor mobile simulation");Mobile.ValidationUsesSimulation=false;
#else
if(typeof(Mobile).GetProperty("ValidationUsesSimulation")!=null)throw new Exception("editor switch leaked");
#endif
Console.WriteLine("PASS throttle reset and platform/editor simulation boundary");}}
'''.replace('AUDIO',audio).replace('MOBILE',mobile)
with tempfile.TemporaryDirectory(prefix='shared-native-host-') as d:
 p=Path(d);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 for i,defines in enumerate(['UNITY_STANDALONE_WIN','UNITY_ANDROID','UNITY_IOS','UNITY_STANDALONE_WIN;UNITY_EDITOR','UNITY_ANDROID;UNITY_EDITOR','UNITY_IOS;UNITY_EDITOR']):
  proj=cv.write_project(p/str(i),[],program,defines=defines);subprocess.run([sys.argv[1],'run','--project',str(proj)],env=env,check=True)
 # Ensure moving native allocation to Awake really removed component field initializers.
 for name in ['BlenderSkillVfx','FilledSkillVfx']:
  s=(root/('Assets/Scripts/Combat/'+name+'.cs')).read_text();assert 'private MaterialPropertyBlock block;' in s and 'void Awake' in s
 runner=(root/'Assets/Editor/RuntimeValidation.cs').read_text();assert 'MobileControls.ValidationUsesSimulation = true;' in runner and 'MobileControls.ValidationUsesSimulation = false;' in runner
 print('PASS explicit source boundary contracts; full Editor runner not executed')
