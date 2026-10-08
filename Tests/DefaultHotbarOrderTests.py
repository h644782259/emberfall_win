"""Exercise production key migration, without starting Unity."""
from pathlib import Path
import os,re,subprocess,sys,tempfile
r=Path(__file__).resolve().parents[1]
def member(s,key):
 a=s.index(key);b=s.index('{',a)+1;d=1
 while d:d+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
t=(r/'Assets/Scripts/Core/GameTypes.cs').read_text();p=(r/'Assets/Scripts/Core/ProgressionService.cs').read_text()
keys=re.search(r'public static readonly int\[\] DefaultHotbarKeys = .*?;',t).group()
code='using System;using System.Collections.Generic;using System.Linq;class GameBalance{public const int HotbarSize=10;'+keys+member(t,'public static bool IsBindableKey(')+'}class Test{'+member(p,'private static int[] RepairHotbarKeys(')+'''
static void C(bool b,string m){if(!b)throw new Exception(m);}static void Main(){
int[] expected={49,50,51,52,53,122,120,99,118,98};
C(GameBalance.DefaultHotbarKeys.SequenceEqual(expected),"number keys come first");
C(RepairHotbarKeys(null).SequenceEqual(expected),"new save defaults");
C(RepairHotbarKeys(new[]{122,120,99,118,98,49,50,51,52,53}).SequenceEqual(expected),"untouched legacy default migrates");
var custom=new[]{113,120,99,118,98,49,50,51,52,53};C(RepairHotbarKeys(custom).SequenceEqual(custom),"custom bindings preserved");
C(RepairHotbarKeys(expected).SequenceEqual(expected),"migration is stable");
var oldMap=(int[])expected.Clone();oldMap[0]=109;var fixedMap=RepairHotbarKeys(oldMap);C(!fixedMap.Contains(109)&&fixedMap.Distinct().Count()==10,"M reserved for map and replacement remains unique");
Console.WriteLine("PASS default key priority, legacy migration, customization, idempotence and reserved M checks");}}
'''
with tempfile.TemporaryDirectory(prefix='emberfall-key-order-') as tmp:
 p=Path(tmp);(p/'Program.cs').write_text(code);(p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 subprocess.run([sys.argv[1],'run','--project',str(p/'Test.csproj')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
