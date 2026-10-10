#!/usr/bin/env python3
"""Production-component allocation/geometry tests with a mandatory old-order negative control."""
import os
from pathlib import Path
import subprocess
import tempfile
root=Path(__file__).resolve().parents[1]
dotnet=os.environ.get('DOTNET','dotnet')
source=(root/'Assets/Scripts/Combat/FilledSkillVfx.cs').read_text()
with tempfile.TemporaryDirectory(prefix='emberfall-filled-vfx-') as temp:
    temp=Path(temp)
    for path in ['Assets/Scripts/Core/FilledVfxRecipes.cs','Assets/Scripts/Core/CombatVisualBudget.cs','Assets/Scripts/Combat/CombatVisualLease.cs','Assets/Scripts/Combat/AnchoredImpactMesh.cs','Assets/Scripts/Combat/CoveredAreaParticles.cs','Assets/Scripts/Core/FilledVfxPlacement.cs','Tests/FilledVfxAllocationTests.cs','Tests/FilledVfxRecipeTests.cs','Assets/Scripts/Combat/WeaponVisualLinks.cs','Assets/Scripts/Core/WeaponStructure.cs','Tests/WeaponVisualLinkTests.cs','Assets/Scripts/Combat/ElementalFieldVisual.cs','Tests/ElementalFieldPlacementTests.cs']:
        content=(root/path).read_text()
        if path.endswith('ElementalFieldPlacementTests.cs'):content=content.replace('public static class WorldTraversal{public static int Revision;}','').replace('public enum Element{Fire,Lightning,Poison}', 'public enum Element{Fire,Lightning,Poison,Ice} public static void Burst(PlayerController hero,Vector3 at,float radius,Element element){}')
        (temp/Path(path).name).write_text(content)
    production=temp/'FilledSkillVfx.cs';production.write_text(source)
    (temp/'Program.cs').write_text('System.Console.WriteLine(FilledVfxRecipeTests.Run());System.Console.WriteLine(FilledVfxAllocationTests.Run());System.Console.WriteLine(WeaponVisualLinkTests.Run());System.Console.WriteLine(ElementalFieldPlacementTests.Run());')
    project=temp/'Validation.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>')
    config=temp/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    subprocess.run([dotnet,'restore',str(project),'--configfile',str(config)],check=True)
    command=[dotnet,'run','--project',str(project),'--no-restore','-c','Release']
    subprocess.run(command,check=True)
    # Move actual primary Add calls after ornaments. This restores the defective mobile allocation order.
    start=source.index('        public static void Impact(');end=source.index('        internal static ArrowBatchHandle BeginArrowBatch(',start)
    impact=source[start:end];calls=[]
    for marker in ['fx.Add(rupture,Vector3.up*.07f','fx.Add(main,Vector3.zero','fx.Add(rupture,Vector3.up*.11f']:
        begin=impact.index(marker);finish=impact.index(';',begin)+1;calls.append(impact[begin:finish]);impact=impact[:begin]+impact[finish:]
    close=impact.rfind('        }');impact=impact[:close]+'\n'.join(calls)+'\n'+impact[close:]
    production.write_text(source[:start]+impact+source[end:])
    failed=subprocess.run(command,capture_output=True,text=True)
    if failed.returncode==0 or 'landing base must survive actual allocation' not in failed.stdout+failed.stderr:
        raise AssertionError('Old-order negative control did not fail for the allocation regression: '+failed.stdout+failed.stderr)
    print('PASS: old-order mutation fails actual mobile retained-base allocation; no Unity/GPU execution')

    production.write_text(source)
    bridge=temp/'WeaponVisualLinks.cs';original=bridge.read_text()
    bridge.write_text(original.replace('            transform.position=candidate;', '            simulation.position=candidate; transform.position=candidate;'))
    failed=subprocess.run(command,capture_output=True,text=True)
    if failed.returncode==0 or 'visual convergence never changes trajectory root' not in failed.stdout+failed.stderr:
        raise AssertionError('Logical-root mutation did not fail: '+failed.stdout+failed.stderr)
    print('PASS: visual-writing-logical-root negative control fails the production transform invariant')

    bridge.write_text(original)
    secondary=temp/'ElementalFieldVisual.cs';normal=secondary.read_text()
    legacy="""                if(!onBody){
                    float legacyAngle=i*2.39996f+time*.2f,legacyDistance=radius*(.28f+(i%4)*.19f);
                    Vector3 legacyOrigin=new Vector3(Mathf.Cos(legacyAngle),0,Mathf.Sin(legacyAngle))*legacyDistance;
                    bool legacyVisible=CombatSight.VisualFootprint(transform.position,transform.TransformPoint(legacyOrigin),.55f);
                    if(bubbles!=null)bubbles[i].gameObject.SetActive(legacyVisible);else strands[i].enabled=legacyVisible;
                    if(!legacyVisible)continue;
                }
"""
    secondary.write_text(normal.replace('                if(!onBody&&!placed[i])continue;',legacy+'                if(!onBody&&!placed[i])continue;'))
    failed=subprocess.run(command,capture_output=True,text=True)
    if failed.returncode==0 or 'wall-adjacent field must keep visible secondary pieces' not in failed.stdout+failed.stderr:
        raise AssertionError('Legacy rotating-toggle mutation did not reproduce vanished field: '+failed.stdout+failed.stderr)
    print('PASS: legacy rotating whole-piece coverage toggle fails the actual near-wall field test')
