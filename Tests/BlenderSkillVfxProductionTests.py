#!/usr/bin/env python3
"""Execute production authored VFX + strict decoder + lease with real checked-in meshes.
Unity substitutes model transforms/lifecycle only: this is not engine/GPU validation.
"""
import os
from pathlib import Path
import subprocess
import tempfile
root = Path(__file__).resolve().parents[1]
dotnet = os.environ.get('DOTNET', 'dotnet')
# Reuse the established managed scene implementation, without its unrelated test cases.
fixture = (root/'Tests/FilledVfxAllocationTests.cs').read_text()
fixture = 'using System;\nusing System.Collections.Generic;\nusing System.Linq;\nusing System.Reflection;\nusing UnityEngine;\n' + fixture[fixture.index('namespace Emberfall\n{'):]
fixture = fixture.replace('public static int FootprintCalls;', 'public static int FootprintCalls;public static float LastFootprint;')
fixture = fixture.replace('float radius){FootprintCalls++;return', 'float radius){LastFootprint=radius;FootprintCalls++;return')
fixture = fixture.replace('public Vector3 right=>', 'public Vector3 forward=>localRotation.Rotate(Vector3.forward);\n        public Vector3 right=>')
fixture = fixture.replace('public static Quaternion LookRotation(Vector3 forward)=>identity;', 'public static Quaternion LookRotation(Vector3 forward)=>Euler(0,(float)Math.Atan2(forward.x,forward.z)*180/(float)Math.PI,0);')
fixture = fixture.replace('public Vector3[] vertices;', 'public Vector3[] normals;public Vector3[] vertices;')
fixture = fixture.replace('public enum PrimitiveType{Sphere}', 'public enum PrimitiveType{Sphere,Capsule,Cube,Cylinder}')
fixture = fixture.replace('public sealed class Shader:Object{', 'public sealed class Shader:Object{public bool isSupported=true;')
fixture = fixture.replace('public static class Resources{public static T Load<T>(string name)where T:new()=>new T();}', '''public sealed class TextAsset:Object{public byte[] bytes;}
    public static class Resources
    {
        public static readonly Dictionary<string,Object> Values=new Dictionary<string,Object>();
        public static T Load<T>(string name)where T:class=>Values.TryGetValue(name,out var value)?value as T:null;
    }''')
fixture=fixture.replace('components.Add(c);return c;', 'components.Add(c);if(activeSelf)c.GetType().GetMethod("Awake",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)?.Invoke(c,null);return c;')
with tempfile.TemporaryDirectory(prefix='emberfall-blender-vfx-') as directory:
    temp = Path(directory)
    for path in ['Assets/Scripts/Combat/BlenderSkillVfx.cs','Assets/Scripts/Combat/AuthoredActorMeshes.cs','Assets/Scripts/Combat/CombatVisualLease.cs','Assets/Scripts/Core/CombatVisualBudget.cs','Tests/BlenderSkillVfxProductionTests.cs']:
        (temp/Path(path).name).write_text((root/path).read_text())
    (temp/'Fixture.cs').write_text(fixture)
    (temp/'Program.cs').write_text('System.Console.WriteLine(BlenderSkillVfxProductionTests.Run(System.Environment.GetCommandLineArgs()[1]));')
    project = temp/'Validation.csproj'
    project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>')
    config = temp/'NuGet.Config'
    config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    subprocess.run([dotnet,'restore',str(project),'--configfile',str(config)],check=True)
    command = [dotnet,'run','--project',str(project),'--no-restore','-c','Release','--',str(root)]
    subprocess.run(command,check=True)
    # Negative controls exercise assertions against modified production code, not source-string tests.
    production = temp/'BlenderSkillVfx.cs'
    source = production.read_text()
    controls = [
        ('age-zero', 'fx.Sample();return true;', 'return true;', 'age-zero geometry initialized'),
        ('epoch', 'owner.CombatEpoch!=epoch||', '', 'epoch retires'),
        ('footprint', '(groundShock?4.8f:3.4f)*range', '(groundShock?1f:1f)*range', 'animated vertex contained'),
    ]
    for name, before, after, expected in controls:
        if before not in source: raise AssertionError('Negative control target missing: '+name)
        production.write_text(source.replace(before,after))
        failed = subprocess.run(command,capture_output=True,text=True)
        if failed.returncode == 0 or expected not in failed.stdout+failed.stderr:
            raise AssertionError('Negative control did not fail as expected: '+name+'\n'+failed.stdout+failed.stderr)
        print('PASS: production '+name+' mutation rejected ('+expected+')',flush=True)
        production.write_text(source)

    decoder = temp/'AuthoredActorMeshes.cs'
    original = decoder.read_text()
    decoder.write_text(original.replace('if(n<.9f||n>1.1f)return null;', ''))
    failed = subprocess.run(command,capture_output=True,text=True)
    if failed.returncode == 0 or 'strict malformed resource rejected' not in failed.stdout+failed.stderr:
        raise AssertionError('Decoder normal validation mutation was not caught: '+failed.stdout+failed.stderr)
    print('PASS: production decoder zero-normal acceptance mutation rejected',flush=True)
