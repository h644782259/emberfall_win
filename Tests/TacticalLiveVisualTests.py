#!/usr/bin/env python3
import os,sys,tempfile,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1];dotnet=sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet')
source=(root/'Assets/Scripts/Combat/EnemyController.cs').read_text()
start=source.index('            if (session == null || !AdventureResultPolicy.AcceptsDamage',source.index('public void TakeDamage('));end=source.index('\n            if (CombatReviewEvents.Enabled)',start)
damage=source[start:end]
prop=source[source.index('public bool GuardArmorClosed'):source.index('\n',source.index('public bool GuardArmorClosed'))]
def member(text,signature):
 a=text.index(signature);b=text.index('{',a)+1;depth=1
 while depth:depth+=(text[b]=='{')-(text[b]=='}');b+=1
 return text[a:b]
late=member(source,'private void LateUpdate()')
with tempfile.TemporaryDirectory(prefix='live-tactics-') as directory:
 path=Path(directory)
 for name in ['Combat/TacticalEnemyVisual','Combat/TacticalCaptureVisual','Combat/GuardArmorVisual','Core/GuardArmorRules','Core/CombatImpactBatch','Core/CampPracticeRecord']:(path/(name.split('/')[-1]+'.cs')).write_text((root/'Assets/Scripts'/(name+'.cs')).read_text())
 (path/'AttachmentFallback.cs').write_text('namespace Emberfall{internal class TacticalAttachmentArt{internal static TacticalAttachmentArt Create(EnemyController e)=>new TacticalAttachmentArt();internal bool Set(int i,bool a)=>false;internal void Hide(){}internal void Destroy(){}}}')
 (path/'Fixture.cs').write_text((root/'Tests/TacticalLiveVisualTests.cs').read_text())
 unity=(root/'Tests/FilledVfxAllocationTests.cs').read_text().split('namespace UnityEngine',1)[1]
 # Reuse the existing explicit managed Unity substitutes, not any combat implementation.
 unity=unity.replace('public Vector3 right=>','public Vector3 forward=>localRotation.Rotate(new Vector3(0,0,1));public Vector3 right=>').replace('public float sqrMagnitude=>','public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;public float sqrMagnitude=>')
 unity=unity.replace('public struct Color{','public struct Color{public static Color white=>new Color(1,1,1);').replace('public float sqrMagnitude=>','public static Vector3 operator-(Vector3 a)=>new Vector3(-a.x,-a.y,-a.z);public float sqrMagnitude=>')
 unity=unity.replace('public int positionCount;','public int positionCount{get=>Positions.Length;set=>Array.Resize(ref Positions,value);}') .replace('public readonly Vector3[] Positions=new Vector3[4];','public Vector3[] Positions=new Vector3[0];')
 (path/'Unity.cs').write_text('using System;using System.Linq;using System.Reflection;using System.Collections.Generic;namespace UnityEngine'+unity)
 (path/'Enemy.cs').write_text('using UnityEngine;namespace Emberfall{public partial class EnemyController{'+prop+late+'public void TakeDamagePrefix(float amount,Vector3 direction,System.Action<float> actualHealthLoss=null,int practiceCastId=0){'+damage+'}}}')
 (path/'Program.cs').write_text('System.Console.WriteLine(TacticalLiveVisualTests.Run());')
 project=path/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');config=path/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 subprocess.run([dotnet,'restore',str(project),'--configfile',str(config)],check=True)
 command=[dotnet,'run','--project',str(project),'--no-restore'];subprocess.run(command,check=True)
 visual=path/'TacticalEnemyVisual.cs';original=visual.read_text();visual.write_text(original.replace('supported=next;ward.enabled=next||Time.time<severedUntil;','if(supported&&!next)return; supported=next;ward.enabled=next||Time.time<severedUntil;'))
 result=subprocess.run(command,capture_output=True,text=True)
 assert result.returncode and 'support removal immediately extinguishes supported color' in result.stdout+result.stderr,result.stdout+result.stderr
 visual.write_text(original)
 guard=path/'GuardArmorVisual.cs';original=guard.read_text();guard.write_text(original.replace('bool closed=enemy.GuardArmorClosed;','bool closed=true;'))
 result=subprocess.run(command,capture_output=True,text=True)
 assert result.returncode and 'windup opens real weakpoint state' in result.stdout+result.stderr,result.stdout+result.stderr
 guard.write_text(original)
 enemy=path/'Enemy.cs';original=enemy.read_text();enemy.write_text(original.replace('private void LateUpdate()\n        {','private void LateUpdate()\n        {\n            if(session.IsRoomContesting(this)&&roomContestMarker==null)roomContestMarker=WorldBuilder.MakeRoomContestMarker(transform,NavigationRadius);'))
 result=subprocess.run(command,capture_output=True,text=True)
 assert result.returncode and 'enemy update never allocates the redundant legacy contest rings' in result.stdout+result.stderr,result.stdout+result.stderr
 enemy.write_text(original)
 capture=path/'TacticalCaptureVisual.cs';original=capture.read_text()
 for old,expected in [('local','two seals render independent local progress'),('zero','zero-progress contested seal still shows visible flag')]:
  mutation=original.replace('session.TryGetChapterSeal(sealIndex,','session.TryGetChapterSeal(0,') if old=='local' else original.replace('contestedFlag.enabled=contested&&!complete;','contestedFlag.enabled=contested&&!complete&&fraction>0;')
  assert mutation!=original
  capture.write_text(mutation);result=subprocess.run(command,capture_output=True,text=True)
  assert result.returncode and expected in result.stdout+result.stderr,result.stdout+result.stderr
 capture.write_text(original)
 mutation=original.replace('ownerMode==OwnerMode.RoomSeal?session.TryGetRoomSeal(', 'ownerMode==OwnerMode.RoomSeal?session.TryGetChapterSeal(')
 assert mutation!=original
 capture.write_text(mutation);result=subprocess.run(command,capture_output=True,text=True)
 assert result.returncode and 'room owner mode renders stable identities and independent pressure without chapter adapter' in result.stdout+result.stderr,result.stdout+result.stderr
 capture.write_text(original)
 mutation=original.replace('&&object.ReferenceEquals(session.Player,roomOwner)', '')
 assert mutation!=original
 capture.write_text(mutation);result=subprocess.run(command,capture_output=True,text=True)
 assert result.returncode and 'replacement player same epoch cannot revive old room visuals' in result.stdout+result.stderr,result.stdout+result.stderr
 capture.write_text(original)
 print('PASS: compiled stale support/closed armor/duplicate ring/shared seal/zero contention/wrong room adapter/owner controls rejected')
for file in ['GameSession.RoomTactics.cs','GameSession.Chapter.cs']:
 body=(root/'Assets/Scripts/Core'/file).read_text();assert 'CombatFx.Ring(' not in body
assert 'guardArmorVisual=GuardArmorVisual.Attach(this)' in source
print('PASS: tactical heartbeat rings removed and guard initialization connected; managed engine substitutes, no GPU acceptance')
