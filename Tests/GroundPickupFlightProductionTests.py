from pathlib import Path
import tempfile,subprocess,os
root=Path(__file__).resolve().parents[1]
s=(root/'Assets/Scripts/World/GroundLootPickup.cs').read_text();a=s.index('    internal sealed class GroundPickupFlight');b=s.index('{',a)+1;depth=1
while depth:depth+=(s[b]=='{')-(s[b]=='}');b+=1
production=s[a:b]
program='using System;namespace Emberfall{'+production+r'''
class Program{static int n;static void C(bool b,string m){n++;if(!b)throw new Exception(m);}static void Main(){foreach(int fps in new[]{30,60,120}){var f=new GroundPickupFlight();for(int i=0;i<fps;i++)f.Advance(1f/fps);C(f.Progress==0&&!f.Complete,"drop rests on ground before magnet");for(int i=0;i<fps/3;i++)f.Advance(1f/fps);C(f.Progress>0&&f.Progress<1&&!f.Complete,"visible flight before inventory grant");float progress=f.Progress;f.Advance(0);f.Advance(-1);f.Advance(float.NaN);f.Advance(float.PositiveInfinity);C(f.Progress==progress,"pause and invalid time cannot collect");for(int i=0;i<fps;i++)f.Advance(1f/fps);C(f.Complete&&f.Progress==1,"arrival permits collection");f.Advance(1);C(f.Progress==1,"flight never overshoots");}Console.WriteLine("PASS "+n+" auto-pickup landing/flight assertions");}}}'''
with tempfile.TemporaryDirectory(prefix='pickup-flight-') as d:
 p=Path(d);(p/'Program.cs').write_text(program);(p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 subprocess.run(['/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet','run','--project',str(p/'Test.csproj')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
