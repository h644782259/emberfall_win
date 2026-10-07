#!/usr/bin/env python3
"""Execute actual simulation entry methods; always build/run two old-behavior controls.

The damage recipient and rendering/math APIs are managed substitutes. Production
Update, enemy damage dispatch and WorldTraversal run unchanged. This is not Unity.
"""
from CastReceiptFixtureSources import include_cast_receipt_source
import argparse
import os
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]


def method(source, declaration):
    start = source.index('        private ' + declaration)
    brace = source.index('{', start)
    depth, end = 1, brace + 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[start:end]


def without_simulation_gate(body):
    guard = '\n            if (session.InputBlocked) return;'
    if body.count(guard) != 1 or body.index(guard) > body.index('float dt = Time.deltaTime;'):
        raise AssertionError('negative control requires one exact simulation gate before deltaTime')
    result = body.replace(guard, '', 1)
    # Other guards, including largeBoss and owner/epoch retirement, stay byte-for-byte intact.
    if result + guard == body:
        raise AssertionError('simulation gate must not be a trailing cleanup condition')
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('dotnet_path', nargs='?')
    parser.add_argument('--dotnet')
    args = parser.parse_args()
    args.dotnet = args.dotnet or args.dotnet_path or os.environ.get('DOTNET', 'dotnet')
    paths = {'enemy': 'Assets/Scripts/Combat/EnemyController.cs', 'projectile': 'Assets/Scripts/Combat/CombatEffects.cs'}
    current = {name: (ROOT / path).read_text() for name, path in paths.items()}
    fixture = (ROOT / 'Tests/DestructibleTraversalTests.cs').read_text()
    fixture = 'using System;using UnityEngine;' + fixture[fixture.index('namespace Emberfall'):]
    for declaration in ['public static class CombatFx', 'public static class PlayerUpgradeRules',
                        'public struct Vector3', 'public static class Time', 'public static class Mathf']:
        fixture = fixture.replace(declaration, declaration.replace('class ', 'partial class ').replace('struct ', 'partial struct '))
    cases = [('current', None, None),
             ('old-enemy', 'enemy', 'blocked ordinary windup must stay unchanged'),
             ('old-projectile', 'projectile', 'blocked projectile must keep position and lifetime')]
    with tempfile.TemporaryDirectory(prefix='blocked-combat-update-') as temporary:
        base = Path(temporary)
        for title, old, expected in cases:
            directory = base / title
            directory.mkdir()
            enemy = current['enemy']
            declarations = ['void Update()', 'void ResolveAttack()', 'void DamageTarget(float amount)',
                            'bool RegroupMobileSupport(float dt,float speed)', 'bool InsideImpact(Vector3 point)', 'void FinishAttack()', 'Vector3 WalkForAnimation(Vector3 displacement)']
            body = '\n'.join(without_simulation_gate(method(enemy, name)) if old == 'enemy' and name == 'void Update()' else method(enemy, name) for name in declarations)
            (directory / 'EnemyMethods.cs').write_text('using UnityEngine;namespace Emberfall{public partial class EnemyController{' + body + '}}')
            projectile = current['projectile']
            projectile = projectile[projectile.index('    internal sealed class CombatProjectile'):projectile.index('    internal sealed class CombatArea')]
            (directory / 'ProjectileMethods.cs').write_text('using UnityEngine;namespace Emberfall{public partial class CombatProjectile{' + (without_simulation_gate(method(projectile, 'void Update()')) if old == 'projectile' else method(projectile, 'void Update()')) + '}}')
            (directory / 'ExistingMathSubstitutes.cs').write_text(fixture)
            catalog=(ROOT/'Assets/Scripts/Core/GameTypes.cs').read_text();start=catalog.index('public static float ConcentratedVenomCoefficient(');end=catalog.index('\n',start)
            (directory/'ActualVenomBudget.cs').write_text('namespace Emberfall{public static class BuildCatalog{'+catalog[start:end]+'}}')
            for source in ['Assets/Scripts/Core/CombatImpactBatch.cs','Assets/Scripts/Core/ThreatAdmissionPolicy.cs', 'Assets/Scripts/Core/DestructiblePropRules.cs', 'Assets/Scripts/Combat/ConcentratedVenomRules.cs', 'Assets/Scripts/World/WorldTraversal.cs', 'Assets/Scripts/Combat/EnemyImpactRegion.cs', 'Tests/BlockedCombatUpdateProductionTests.cs']:
                (directory / Path(source).name).write_text((ROOT / source).read_text())
            (directory / 'WorldTraversal.Platforms.cs').write_text((ROOT/'Assets/Scripts/World/WorldTraversal.Platforms.cs').read_text())
            (directory / 'Program.cs').write_text('System.Console.WriteLine(BlockedCombatUpdateProductionTests.Run());')
            (directory / 'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
            project = directory / 'Validation.csproj'
            project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>CS0649;CS0414</NoWarn></PropertyGroup></Project>');include_cast_receipt_source(project)
            subprocess.run([args.dotnet, 'restore', str(project), '--configfile', str(directory / 'NuGet.Config')], check=True, stdout=subprocess.DEVNULL)
            # Both negative controls must compile successfully before a runtime failure is admissible.
            build = subprocess.run([args.dotnet, 'build', str(project), '--no-restore', '-c', 'Release'], text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
            if build.returncode:
                print(build.stdout)
                build.check_returncode()
            result = subprocess.run([args.dotnet, str(directory / 'bin/Release/net8.0/Validation.dll')], text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
            if expected:
                if result.returncode == 0 or ('System.Exception: ' + expected) not in result.stdout:
                    raise RuntimeError(title + ' failed to demonstrate the exact old behavior:\n' + result.stdout)
                print('PASS: ' + title + ' compiled and failed exact runtime assertion: ' + expected)
            else:
                print(result.stdout, end='')
                result.check_returncode()


if __name__ == '__main__':
    main()
