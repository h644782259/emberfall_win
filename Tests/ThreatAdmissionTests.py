"""Production policy plus narrow integration contracts; no Unity/device execution."""
from CastReceiptFixtureSources import include_cast_receipt_source
from pathlib import Path
import os,subprocess,tempfile
root=Path(__file__).resolve().parents[1]
controller=(root/'Assets/Scripts/Combat/EnemyController.cs').read_text()
chapter=(root/'Assets/Scripts/Core/GameSession.Chapter.cs').read_text()
assert 'ready && BeginAttack()' in controller
assert 'if (!AdmitThreatAttack()) return false;' in controller
assert 'threatAdmission.Withdraw(threatMember)' in controller
assert 'threatAdmission.LaunchVolley(threatMember,2)' in controller
assert 'chapterThreatAdmission!=null&&index<4&&!boss' in chapter
assert 'ThreatAdmissionPolicy.IsPilot(ChapterActive,(int)ActiveChapterNode,(int)ActiveChapterDifficulty,ChapterRoomIndex)' in chapter
assert 'chapterThreatAdmission.Advance(Time.deltaTime)' in chapter
assert 'projectile.lifetime = 3f;' in (root/'Assets/Scripts/Combat/CombatEffects.cs').read_text()
with tempfile.TemporaryDirectory(prefix='threat-admission-') as tmp:
    p=Path(tmp)
    for rel in ['Assets/Scripts/Core/ThreatAdmissionPolicy.cs','Tests/ThreatAdmissionTests.cs']:
        (p/Path(rel).name).write_bytes((root/rel).read_bytes())
    (p/'Program.cs').write_text('System.Console.WriteLine(ThreatAdmissionTests.Run());')
    (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>');include_cast_receipt_source(p/'Test.csproj')
    (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    env=os.environ.copy();env.update(DOTNET_CLI_HOME=str(p/'home'),DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_GENERATE_ASPNET_CERTIFICATE='false')
    subprocess.run([os.environ.get('DOTNET','dotnet'),'run','--project',str(p/'Test.csproj'),'--configfile',str(p/'NuGet.Config')],check=True,env=env)
print('Threat admission integration contracts PASS')
