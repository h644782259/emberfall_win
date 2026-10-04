"""Real companion clocks; query purity/boundaries only, not Unity suspension."""
from pathlib import Path
import os, subprocess, sys, tempfile
root = Path(__file__).resolve().parents[1]
program = r'''
using System;
using Emberfall;
class Program {
 static int n;
 static void Check(bool value,string reason){n++;if(!value)throw new Exception(reason);}
 static void Main(){
  var target=new object();var other=new object();
  var marks=new CompanionCooperationTracker<object>();
  Check(!marks.HasPending(0)&&!marks.HasPending(10),"empty tracker does not block class");
  Check(!marks.RegisterHit(target,0,10),"first form only opens a mark");
  for(int i=0;i<100;i++)Check(marks.HasPending(11.5f),"mark remains pending at inclusive cooperation boundary");
  Check(marks.RegisterHit(target,1,11.5f),"repeated observation cannot consume or clear original partner mark");
  Check(marks.HasPending(14.499f)&&!marks.HasPending(14.5f),"cooldown blocks only until exact original deadline");
  Check(!marks.RegisterHit(other,0,15)&&!marks.HasPending(16.501f),"expired mark no longer blocks practice");
  Check(!marks.RegisterHit(other,1,16.501f),"expired partner hit never revived by observation");
  Check(marks.HasPending(16.501f),"new first form opens its own live window");
  marks.Clear();Check(!marks.HasPending(16.501f),"real reset clears pending state");
  var cooldown=new CompanionCooperationTracker<object>();
  Check(!cooldown.RegisterHit(target,0,20)&&cooldown.RegisterHit(target,1,20),"two forms produce real cooldown");
  for(int i=0;i<100;i++)Check(cooldown.HasPending(21),"queries preserve unexpired cooldown");
  Check(!cooldown.HasPending(23),"queries never extend original cooldown");
  Check(!cooldown.RegisterHit(target,0,23)&&cooldown.RegisterHit(target,1,23),"original cooldown boundary still permits real next cooperation");
  var command=new CompanionCommandOpportunity();command.Grant(30);
  for(int i=0;i<100;i++)Check(command.Remaining(31)==15&&command.IsProtected(31),"command observation preserves 16s grant and 3s protection");
  Check(command.TryConsume(31)&&command.Remaining(31)==0&&command.IsProtected(31),"consumed command still leaves original protection");
  Check(!command.IsProtected(33)&&!command.TryConsume(33),"neither protection nor consumed opportunity restored");
  Console.WriteLine("PASS "+n+" actual companion clock purity/boundary assertions (not Unity)");
 }
}
'''
with tempfile.TemporaryDirectory(prefix='companion-practice-clock-') as directory:
    p=Path(directory)
    (p/'Program.cs').write_text(program)
    (p/'CompanionRules.cs').write_text((root/'Assets/Scripts/Combat/CompanionRules.cs').read_text())
    (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
    (p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
    env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
    subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'Test.csproj')],env=env,check=True)
