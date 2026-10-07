#!/usr/bin/env python3
"""Full enemy telegraph/region plus actual traversal and original hit predicate; managed rendering substitutes."""
import argparse,os,subprocess,tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def method(source,signature):
    start=source.index(signature);brace=source.index('{',start);depth=1;end=brace+1
    while depth:depth+=(source[end]=='{')-(source[end]=='}');end+=1
    return source[start:end]
def main():
    parser=argparse.ArgumentParser();parser.add_argument('dotnet_path',nargs='?');args=parser.parse_args();dotnet=args.dotnet_path or os.environ.get('DOTNET','dotnet')
    math=(ROOT/'Tests/DestructibleTraversalTests.cs').read_text();math='using System;using UnityEngine;'+math[math.index('namespace Emberfall'):]
    for name in ['public static class PlayerUpgradeRules','public struct Vector3','public static class Mathf']:math=math.replace(name,name.replace('class ','partial class ').replace('struct ','partial struct '))
    predicate=method((ROOT/'Assets/Scripts/Combat/PlayerUpgradeRules.cs').read_text(),'public static bool IsInsideArea(')
    with tempfile.TemporaryDirectory(prefix='enemy-impact-contour-') as temporary:
        for legacy in [False,True]:
            folder=Path(temporary)/('legacy' if legacy else 'current');folder.mkdir();(folder/'Math.cs').write_text(math);(folder/'Predicate.cs').write_text('using System;namespace Emberfall{public static partial class PlayerUpgradeRules{'+predicate+'}}')
            for path in ['Assets/Scripts/Combat/EnemyImpactRegion.cs','Assets/Scripts/Combat/EnemyAttackTelegraph.cs','Assets/Scripts/World/WorldTraversal.cs','Assets/Scripts/World/WorldTraversal.Platforms.cs','Tests/EnemyImpactContourTests.cs']:
                source=(ROOT/path).read_text()
                if legacy and path.endswith('EnemyImpactRegion.cs'):
                    start=source.index('        internal static List<Vector3> Outline(');source=source[:start]+source[start:].replace('Contains(attacker,center,','CircleOnly(center,');source=source.replace('        internal static List<Vector3> Outline(', '        private static bool CircleOnly(Vector3 center,Vector3 point,float radius){var d=CombatFx.Flat(point-center);return PlayerUpgradeRules.IsInsideArea(d.x,d.z,radius,true);}\n        internal static List<Vector3> Outline(')
                (folder/Path(path).name).write_text(source)
            (folder/'Program.cs').write_text('System.Console.WriteLine(EnemyImpactContourTests.Run());');config=folder/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>');project=folder/'Validation.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
            build=subprocess.run([dotnet,'build',str(project),'--configfile',str(config),'-v:q'],text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
            if build.returncode:print(build.stdout);build.check_returncode()
            result=subprocess.run([dotnet,str(folder/'bin/Debug/net8.0/Validation.dll')],text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
            if legacy:
                if result.returncode==0 or 'System.Exception: enemy warning must not cover the wall-hidden safe lobe' not in result.stdout:raise RuntimeError('wrong full-circle negative result:\n'+result.stdout)
                print('PASS: full-circle old-behavior control compiled and failed safe-lobe assertion')
            else:print(result.stdout,end='');result.check_returncode()
if __name__=='__main__':main()
