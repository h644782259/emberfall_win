#!/usr/bin/env python3
"""Real knockdown overlay + actual CombatModel.Animate + status LateUpdate.
Managed transform fixture; controller clocks and Unity rendering are not simulated.
"""
import os
from pathlib import Path
import subprocess
import tempfile
root=Path(__file__).resolve().parents[1]
dotnet=os.environ.get('DOTNET','dotnet')
def member(source,signature):
    start=source.index(signature);end=source.index('{',start)+1;depth=1
    while depth:
        if source[end]=='{':depth+=1
        elif source[end]=='}':depth-=1
        end+=1
    return source[start:end]
source=(root/'Assets/Scripts/Combat/CombatModel.cs').read_text()
status=(root/'Assets/Scripts/Combat/EnemyStatusEffects.cs').read_text()
f=(root/'Tests/FilledVfxAllocationTests.cs').read_text()
f='using System;using System.Linq;using System.Collections.Generic;using System.Reflection;using UnityEngine;\n'+f[f.index('namespace Emberfall\n{'):]
f=f.replace('public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();','public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();public T GetComponentInChildren<T>()where T:Component=>GameObject.All.Where(o=>o.transform.parent==transform).Select(o=>o.GetComponent<T>()).FirstOrDefault(c=>c!=null);')
f=f.replace('public Vector3 right=>','public Vector3 InverseTransformDirection(Vector3 v)=>localRotation.InverseRotate(v);public void Rotate(float x,float y,float z,Space space){localRotation=localRotation*Quaternion.Euler(x,y,z);}\npublic Vector3 right=>')
f=f.replace('public enum PrimitiveType{Sphere}','public enum PrimitiveType{Sphere}public enum Space{Self}')
f=f.replace('public static Quaternion identity=>','public static Quaternion Slerp(Quaternion a,Quaternion b,float t)=>new Quaternion{value=System.Numerics.Quaternion.Slerp(a.value,b.value,Mathf.Clamp01(t))};public static float Angle(Quaternion a,Quaternion b)=>(float)(2*Math.Acos(Math.Min(1,Math.Abs(System.Numerics.Quaternion.Dot(a.value,b.value))))*180/Math.PI);public static Quaternion identity=>')
f=f.replace('public const float PI=', 'public static float Exp(float x)=>(float)Math.Exp(x);public static float SmoothStep(float a,float b,float t){t=Clamp01(t);return a+(b-a)*t*t*(3-2*t);}public static float MoveTowards(float a,float b,float d)=>Math.Abs(b-a)<=d?b:a+Math.Sign(b-a)*d;public const float PI=')
with tempfile.TemporaryDirectory(prefix='enemy-knockdown-production-') as directory:
    p=Path(directory)
    for path in ['Assets/Scripts/Combat/CombatModel.Knockdown.cs','Assets/Scripts/Core/LocomotionPoseState.cs','Assets/Scripts/Core/ThreatAdmissionPolicy.cs','Assets/Scripts/Core/CombatImpactBatch.cs','Assets/Scripts/Core/CampPracticeRecord.cs','Tests/EnemyKnockdownProductionTests.cs','Tests/EnemyKnockdownDeathTests.cs']:
        (p/Path(path).name).write_text((root/path).read_text())
    f=f.replace('public sealed class Material:Object{','public sealed class Material:Object{public void SetFloat(string n,float v){}public void SetInt(string n,int v){}public void DisableKeyword(string n){}public void EnableKeyword(string n){}')
    f=f.replace('public enum ShadowCastingMode{Off}', 'public enum ShadowCastingMode{Off}public enum BlendMode{SrcAlpha,OneMinusSrcAlpha}')
    (p/'Fixture.cs').write_text(f)
    # This fixture exercises ordinary stunned enemies, never the separate practice path.
    f=f.replace('public bool HasStarted,ModeFinished,InputBlocked;', 'public bool HasStarted,ModeFinished,InputBlocked,Paused,IsDead,InDungeon;public bool PracticeActive=>false;public bool MovePracticeTarget(EnemyController enemy)=>throw new System.InvalidOperationException("ordinary knockdown replay cannot move a practice target");public CampPracticeRecord PracticeRecord=>throw new System.InvalidOperationException("ordinary knockdown replay cannot record practice");')
    (p/'Fixture.cs').write_text(f)
    controller=(root/'Assets/Scripts/Combat/EnemyController.cs').read_text()
    dissolve=(root/'Assets/Scripts/Combat/EnemyDeathDissolve.cs').read_text()
    initialize=member(dissolve,'public void Initialize(')
    initialize=initialize[:initialize.index('            ash =')]+ '}' # Capture/BeginDeath are actual; particle creation is out of scope.
    update=member(controller,'private void Update(')
    update=update[:update.index('            if (RegroupMobileSupport')]+ 'throw new System.Exception("death-order fixture only supports the real stunned Update path");}'
    (p/'DeathMethods.cs').write_text('using UnityEngine;namespace Emberfall{public sealed partial class CombatModel{'+member(source,'public void BeginDeath(')+'}public sealed partial class EnemyController{'+member(controller,'internal void BeginDeath(')+member(controller,'private void AnimateModel(')+update+'}internal partial class EnemyDeathDissolve{'+initialize+'}}')
    (p/'Model.cs').write_text('using UnityEngine;namespace Emberfall{public sealed partial class CombatModel{'+''.join(member(source,s) for s in ['public void Animate(','public void Recoil(','private void ApplyRecoil('])+'}public partial class EnemyStatusEffects{'+member(status,'private void LateUpdate(')+'}}')
    (p/'Program.cs').write_text('System.Console.WriteLine(EnemyKnockdownProductionTests.Run());System.Console.WriteLine(EnemyKnockdownDeathTests.Run());')
    project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NoWarn>0649;0169</NoWarn></PropertyGroup></Project>')
    config=p/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
    subprocess.run([dotnet,'restore',str(project),'--configfile',str(config),'-v:q'],env=env,check=True)
    command=[dotnet,'run','--project',str(project),'--no-restore']
    subprocess.run(command,env=env,check=True)
    overlay=p/'CombatModel.Knockdown.cs';original=overlay.read_text()
    for label,before,after,expected in [
        ('snap','Weight=Mathf.MoveTowards(Weight,target,delta/(heavy?.18f:.13f));','Weight=target;','fall has intermediate pose'),
        ('pause','if(delta<=0 || float.IsNaN(delta) || float.IsInfinity(delta))return;','if(delta<=0)delta=.016f;','paused pose stays exact'),
        ('death','if (dying || enemyOwner.IsDead) return true;','','death owns final pose'),
        ('airborne','float target=airborne ||','float target=false ||','airborne supersedes ground pose'),
        ('restore','            RestoreKnockdownBase();','', 'paused pose stays exact'),
        ('stale-display-cache','knockdownDisplayed=false;return true;', 'return true;', 'death capture keeps last complete displayed pose'),
    ]:
        assert before in original,label
        overlay.write_text(original.replace(before,after))
        result=subprocess.run(command,env=env,capture_output=True,text=True)
        if result.returncode==0 or expected not in result.stdout+result.stderr:raise AssertionError(label+' mutation not caught as expected\n'+result.stdout+result.stderr)
        print('PASS: production '+label+' mutation rejected ('+expected+')',flush=True)
        overlay.write_text(original)

    model=p/'DeathMethods.cs';original=model.read_text()
    for label,before,after in [
        ('missing-display-handoff','            RestoreKnockdownForDeath();',''),
        ('capture-before-finalize','            model.BeginDeath(); // Finalize the displayed pose before capturing death ownership.',''),
    ]:
        assert before in original
        changed=original.replace(before,after)
        if label=='capture-before-finalize':changed=changed.replace('startScale = visual.localScale;', 'startScale = visual.localScale;model.BeginDeath();')
        model.write_text(changed)
        failure=subprocess.run(command,env=env,capture_output=True,text=True)
        if failure.returncode==0 or 'death capture keeps last complete displayed pose' not in failure.stdout+failure.stderr:raise AssertionError(label+' mutation did not fail correctly\n'+failure.stdout+failure.stderr)
        print('PASS: compiled '+label+' mutation rejected at real controller/death capture ordering',flush=True)
        model.write_text(original)
