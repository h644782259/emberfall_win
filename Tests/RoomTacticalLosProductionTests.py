#!/usr/bin/env python3
"""Actual session query methods and WorldTraversal, with test-only LOS entry counter.

Managed object/math substitutes do not establish Unity play or frame-time results.
Both eager-argument old behaviors must compile then fail their exact count check.
"""
import argparse
import os
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]


def method(source, declaration):
    start = source.index('        ' + declaration)
    brace = source.index('{', start)
    depth, end = 1, brace + 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[start:end]


def once(source, old, new):
    assert source.count(old) == 1, 'production seam must occur exactly once: ' + old
    return source.replace(old, new, 1)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('dotnet_path', nargs='?')
    parser.add_argument('--dotnet')
    args = parser.parse_args()
    dotnet = args.dotnet or args.dotnet_path or os.environ.get('DOTNET', 'dotnet')
    source = (ROOT / 'Assets/Scripts/Core/GameSession.RoomTactics.cs').read_text()
    methods = '\n'.join(method(source, d) for d in ['public bool IsRoomContesting(EnemyController enemy)', 'private static bool LiveRoomEnemy(EnemyController enemy)', 'public float RoomSupportMultiplier(EnemyController enemy)'])
    traversal = (ROOT / 'Assets/Scripts/World/WorldTraversal.cs').read_text()
    traversal = once(traversal, 'public static bool HasLineOfSight(Vector3 from, Vector3 to) { return ClearSegment(from, to, .04f, true); }', 'public static int TestLineOfSightCalls; public static bool HasLineOfSight(Vector3 from, Vector3 to) { TestLineOfSightCalls++; return ClearSegment(from, to, .04f, true); }')
    fixture = (ROOT / 'Tests/DestructibleTraversalTests.cs').read_text()
    fixture = 'using System;using UnityEngine;' + fixture[fixture.index('namespace Emberfall'):]
    cases = [('current', None), ('old-contest', 'far contest candidates must skip LOS'), ('old-support', 'far support candidates must skip LOS')]
    with tempfile.TemporaryDirectory(prefix='room-los-production-') as temporary:
        for title, expected in cases:
            body = methods
            if title == 'old-contest':
                body = once(body, 'sqrMagnitude,enemy.NavigationRadius,true)&&\n                WorldTraversal.HasLineOfSight(enemy.transform.position,RoomObjectivePoint);', 'sqrMagnitude,enemy.NavigationRadius,\n                WorldTraversal.HasLineOfSight(enemy.transform.position,RoomObjectivePoint));')
            elif title == 'old-support':
                body = once(body, 'sqrMagnitude,true)&&\n                WorldTraversal.HasLineOfSight(enemy.transform.position,roomSupplier.transform.position) ? .7f : 1;', 'sqrMagnitude,\n                WorldTraversal.HasLineOfSight(enemy.transform.position,roomSupplier.transform.position)) ? .7f : 1;')
            directory = Path(temporary) / title
            directory.mkdir()
            (directory / 'SessionMethods.cs').write_text('using UnityEngine;namespace Emberfall{public partial class GameSession{' + body + '}}')
            (directory / 'WorldTraversal.cs').write_text(traversal)
            (directory / 'WorldTraversal.Platforms.cs').write_text((ROOT/'Assets/Scripts/World/WorldTraversal.Platforms.cs').read_text())
            (directory / 'ExistingMathSubstitutes.cs').write_text(fixture)
            for source in ['Assets/Scripts/Core/RoomTacticalRegion.cs', 'Tests/RoomTacticalLosProductionTests.cs']:
                (directory / Path(source).name).write_text((ROOT / source).read_text())
            (directory / 'Program.cs').write_text('System.Console.WriteLine(RoomTacticalLosProductionTests.Run());')
            (directory / 'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
            project = directory / 'Validation.csproj'
            project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
            subprocess.run([dotnet, 'restore', str(project), '--configfile', str(directory / 'NuGet.Config')], check=True, stdout=subprocess.DEVNULL)
            build = subprocess.run([dotnet, 'build', str(project), '--no-restore', '-c', 'Release'], text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
            if build.returncode:
                print(build.stdout)
                build.check_returncode()
            result = subprocess.run([dotnet, str(directory / 'bin/Release/net8.0/Validation.dll')], text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
            if expected:
                if result.returncode == 0 or ('System.Exception: ' + expected) not in result.stdout:
                    raise RuntimeError(title + ' did not fail the exact runtime assertion:\n' + result.stdout)
                print('PASS: ' + title + ' compiled and failed exact runtime assertion: ' + expected)
            else:
                print(result.stdout, end='')
                result.check_returncode()


if __name__ == '__main__':
    main()
