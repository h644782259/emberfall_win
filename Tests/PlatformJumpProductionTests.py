from pathlib import Path
import os,sys,tempfile,subprocess,shutil
root=Path(__file__).resolve().parents[1];p=Path(tempfile.mkdtemp(prefix='platform-jump-check-'))
s=(root/'Tests/DestructibleTraversalTests.cs').read_text();s='using System;using UnityEngine;'+s[s.index('namespace Emberfall'):];s=s.replace('public static Vector3 zero=>','public static Vector3 up=>new Vector3(0,1,0);public static Vector3 zero=>').replace('public const float PI=', 'public static float Clamp01(float x)=>Clamp(x,0,1);public const float PI=')
(p/'Fixture.cs').write_text(s)
(p/'World.cs').write_text((root/'Assets/Scripts/World/WorldTraversal.cs').read_text())
platforms=root/'Assets/Scripts/World/WorldTraversal.Platforms.cs'
if platforms.exists():(p/'Platforms.cs').write_text(platforms.read_text())
(p/'Sight.cs').write_text((root/'Assets/Scripts/Combat/CombatSight.cs').read_text())
(p/'Rules.cs').write_text((root/'Assets/Scripts/Core/CombatSightRules.cs').read_text())
(p/'Program.cs').write_text('''using System;using Emberfall;using UnityEngine;
class Program{static int checks;static void C(bool x,string why){checks++;if(!x)throw new Exception(why);}static void Main(){
Vector3 target;
foreach(float h in new[]{.65f,.95f}){
WorldTraversal.Reset(ZoneKind.Wilderness);WorldTraversal.AddPlatform(Vector3.zero,new Vector2(2.2f,1.8f),h);
var from=new Vector3(0,0,-1.351f);C(WorldTraversal.CanStand(from),"start beside platform");
C(WorldTraversal.TryResolvePlatformJump(from,new Vector3(0,0,1),2,.45f,out target),"jump up from touching side");C(target.y==h,"land on top");C(WorldTraversal.TryResolvePlatformJump(from,new Vector3(0,0,1),6,.45f,out target)&&target.y==h,"normal jump prefers reachable platform");
var top=new Vector3(0,h,0);var aim=CombatSight.GroundPoint(top,new Vector3(0,0,5));C(aim.z>4.9f,"cast from raised platform to ground");
C(WorldTraversal.TryResolvePlatformJump(top,new Vector3(0,0,1),4,.45f,out target)&&target.y==0,"jump down from platform");
WorldTraversal.AddBox(new Vector3(0,0,3),new Vector2(4,1));aim=CombatSight.GroundPoint(top,new Vector3(0,0,6));C(aim.z<2.6f,"raised caster cannot cast through wall");
}
WorldTraversal.Reset(ZoneKind.Wilderness);WorldTraversal.SetRiver(new[]{new Vector3(-15,0,0),new Vector3(15,0,0)},2.4f,new Rect(-1,-3,2,6));
var bank=new Vector3(6,0,-2.86f);C(WorldTraversal.TryResolvePlatformJump(bank,new Vector3(0,0,1),6,.45f,out target),"cross actual camp river width");C(target.z>2.85f&&WorldTraversal.CanStand(target),"land dry on far bank");
WorldTraversal.Reset(ZoneKind.Wilderness);WorldTraversal.AddBox(new Vector3(0,0,1),new Vector2(4,1));
C(!WorldTraversal.TryResolvePlatformJump(new Vector3(0,0,-1),new Vector3(0,0,1),6,.45f,out target)||target.z<.1f,"solid walls still block jump");
Console.WriteLine(checks+" production jump and raised-cast assertions passed");}}
''')
(p/'Check.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')

try:
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else "dotnet","run","--project",str(p/"Check.csproj")],check=True)
finally:
 shutil.rmtree(p)
