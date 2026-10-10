#!/usr/bin/env python3
"""Run production secondary-field refresh with explicit managed engine substitutes."""
import os
import sys
from pathlib import Path
import subprocess
import tempfile
root=Path(__file__).resolve().parents[1]
dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
with tempfile.TemporaryDirectory(prefix='emberfall-field-continuity-') as directory:
    temp=Path(directory)
    files=['Assets/Scripts/Combat/ElementalFieldVisual.cs','Assets/Scripts/Core/FilledVfxPlacement.cs',
        'Assets/Scripts/Combat/FilledSkillVfx.cs','Assets/Scripts/Core/FilledVfxRecipes.cs',
        'Assets/Scripts/Combat/CombatVisualLease.cs','Assets/Scripts/Core/CombatVisualBudget.cs',
        'Assets/Scripts/Combat/AnchoredImpactMesh.cs',
        'Assets/Scripts/Combat/CoveredAreaParticles.cs','Assets/Scripts/Combat/WeaponVisualLinks.cs',
        'Assets/Scripts/Core/WeaponStructure.cs','Tests/FilledVfxAllocationTests.cs',
        'Tests/WeaponVisualLinkTests.cs','Tests/ElementalFieldPlacementTests.cs','Tests/ElementalFieldContinuityTests.cs']
    for path in files:(temp/Path(path).name).write_text((root/path).read_text())
    # Reconcile shared managed substitutes without editing gameplay sources.
    placement=temp/'ElementalFieldPlacementTests.cs'
    placement.write_text(placement.read_text().replace('    public static class WorldTraversal{public static int Revision;}','').replace('public enum Element{Fire,Lightning,Poison}', 'public enum Element{Fire,Ice,Lightning,Poison}public static void Burst(PlayerController hero,UnityEngine.Vector3 at,float radius,Element element){}'))
    fixture=temp/'FilledVfxAllocationTests.cs'
    stub=fixture.read_text().replace('public static class Mathf\n    {','public static class Mathf\n    { public const float Deg2Rad=PI/180f;')
    stub=stub.replace('public sealed class Mesh:Object{','public struct Bounds{public Vector3 extents;public Vector3 size=>extents*2;public Vector3 min=>extents*-1;}public sealed class Mesh:Object{public Bounds bounds;')
    fixture.write_text(stub)
    (temp/'Program.cs').write_text('System.Console.WriteLine(ElementalFieldContinuityTests.Run());')
    project=temp/'Validation.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>')
    config=temp/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    subprocess.run([dotnet,'restore',str(project),'--configfile',str(config)],check=True)
    command=[dotnet,'run','--project',str(project),'--no-restore','-c','Release']
    subprocess.run(command,check=True)
    component=temp/'ElementalFieldVisual.cs';source=component.read_text()
    begin=source.index('                // Retain a still-valid')
    end=source.index('                float angle=',begin)
    component.write_text(source[:begin]+source[end:])
    failed=subprocess.run(command,capture_output=True,text=True)
    if failed.returncode==0 or 'removing cover must not teleport already valid secondary geometry' not in failed.stdout+failed.stderr:
        raise AssertionError('Old replan-every-piece control did not reproduce continuity regression: '+failed.stdout+failed.stderr)
    print('PASS: old unconditional replan fails the actual rendered-point continuity assertion')
