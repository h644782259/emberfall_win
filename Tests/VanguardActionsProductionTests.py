#!/usr/bin/env python3
"""Real binary loader and CombatModel additive adapter, managed engine doubles."""
from pathlib import Path
import tempfile, subprocess, sys, os
root=Path(__file__).resolve().parents[1]
fixture=r'''
using System;using System.IO;using UnityEngine;using Emberfall;
namespace UnityEngine {
 public struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;} public static Vector3 zero=>new Vector3();public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>new Vector3(a.x+(b.x-a.x)*t,a.y+(b.y-a.y)*t,a.z+(b.z-a.z)*t);}
 public struct Quaternion {public Vector3 e;public static Quaternion Euler(Vector3 a)=>new Quaternion{e=a};public static Quaternion operator*(Quaternion a,Quaternion b)=>Euler(new Vector3(a.e.x+b.e.x,a.e.y+b.e.y,a.e.z+b.e.z));}
 public class Transform {public Quaternion localRotation;}
 public class TextAsset {public byte[] bytes;}
 public static class Resources {public static byte[] Bytes;public static T Load<T>(string n) where T:class=>Bytes==null?null:new TextAsset{bytes=Bytes} as T;}
 public static class Time {public static float time;}
 public static class Mathf {public const float PI=(float)Math.PI;public static float Abs(float f)=>Math.Abs(f);public static float Max(float a,float b)=>Math.Max(a,b);public static float Clamp01(float f)=>Math.Max(0,Math.Min(1,f));public static float Repeat(float t,float n)=>t-(float)Math.Floor(t/n)*n;}
}
namespace Emberfall {
 enum HeroClass {Vanguard,Ranger} static class AuthoredActorMeshes {internal static bool Enabled=true;}
 class Loc {public float Phase,Speed,Side,Forward,JumpTuck,Landing;}
 class Motion {public float Turn;}
 public sealed partial class CombatModel {
 HeroClass heroClass;Transform pelvis=new Transform(),spine=new Transform(),headRig=new Transform(),leftArm=new Transform(),rightArm=new Transform(),leftKnee=new Transform(),rightKnee=new Transform(),cloak=new Transform(),swordRig=new Transform();
 bool pilotOwnerDead,dying,isolatedPreview,pilotAirborne,actionBasic;float previewTime;int actionSkill;Loc locomotion=new Loc();Motion visualMotion=new Motion();
 public static void Run(Action<bool,string> check) {
 var m=new CombatModel();m.ConfigureVanguardArt();check(m.vanguardArt!=null,"complete library selected");
 var body=m.spine;var sword=m.swordRig;
 for(int state=0;state<13;state++) {
  foreach(var j in m.vanguardArtBones)j.localRotation=new Quaternion();
  m.locomotion=new Loc();m.visualMotion.Turn=0;m.pilotAirborne=false;Time.time=2;m.vanguardHurtStart=-10;m.vanguardHurt=false;
  bool act=false,hurt=false;m.actionSkill=0;m.actionBasic=false;
  if(state==0)Time.time=1;
  if(state>=1&&state<=4){m.locomotion.Phase=Mathf.PI*.5f;m.locomotion.Speed=1;m.locomotion.Forward=state==1?1:state==2?-1:0;m.locomotion.Side=state==3?-1:state==4?1:0;}
  if(state==5||state==6)m.visualMotion.Turn=state==5?-1:1;
  if(state==7||state==8){act=true;m.actionBasic=state==7;}
  if(state==9){m.pilotAirborne=true;m.locomotion.JumpTuck=1;}
  if(state==10)m.locomotion.Landing=1;
  if(state==11)hurt=true;
  if(state==12){m.pilotOwnerDead=true;m.SampleVanguardDeath();Time.time+=.35f;m.SampleVanguardDeath();}else m.ApplyAuthoredVanguardPose(act,.3f,hurt);
  float sum=0;foreach(var j in m.vanguardArtBones)sum+=Math.Abs(j.localRotation.e.x)+Math.Abs(j.localRotation.e.y)+Math.Abs(j.localRotation.e.z);
  check(sum>.1f,"actual adapter samples pose "+state);check(m.spine==body&&m.swordRig==sword,"body/equipment identity stable "+state);
 }
 var before=m.spine.localRotation.e;m.ApplyAuthoredVanguardPose(true,.3f,true);check(m.spine.localRotation.e.x==before.x,"death prevents live pose writer");
 Resources.Bytes=null;var fallback=new CombatModel();fallback.ConfigureVanguardArt();check(fallback.vanguardArt==null,"missing complete library chooses old rig");fallback.ApplyAuthoredVanguardPose(true,.3f,true);check(fallback.spine.localRotation.e.x==0,"fallback has no partial offsets");
 }
}
}
class Entry {static void Main(string[] args){int n=0;Action<bool,string> c=(ok,s)=>{n++;if(!ok)throw new Exception(s);};var bytes=File.ReadAllBytes(args[0]);Resources.Bytes=bytes;var lib=VanguardActionLibrary.Decode(bytes);c(lib!=null,"actual resource loaded");
 for(int clip=0;clip<13;clip++)for(int bone=0;bone<9;bone++)for(int s=0;s<=128;s++){var v=lib.Sample((VanguardArtPose)clip,bone,s/128f);c(!float.IsNaN(v.x)&&Math.Abs(v.x)<=40&&Math.Abs(v.y)<=40&&Math.Abs(v.z)<=40,"finite bounded sample");}
 foreach(int length in new[]{0,16,bytes.Length-1,bytes.Length+1}){var bad=new byte[length];Array.Copy(bytes,bad,Math.Min(length,bytes.Length));c(VanguardActionLibrary.Decode(bad)==null,"length rejected");}
 foreach(int offset in new[]{0,4,8,12}){var bad=(byte[])bytes.Clone();bad[offset]^=127;c(VanguardActionLibrary.Decode(bad)==null,"schema rejected");}
 foreach(float badValue in new[]{float.NaN,float.PositiveInfinity,41f}){var bad=(byte[])bytes.Clone();Array.Copy(BitConverter.GetBytes(badValue),0,bad,16,4);c(VanguardActionLibrary.Decode(bad)==null,"unsafe value rejected");}
 CombatModel.Run(c);Console.WriteLine("PASS "+n+" Vanguard real resource/adapter checks; managed doubles, not Unity");}}
'''
with tempfile.TemporaryDirectory(prefix='vanguard-actions-') as d:
 p=Path(d)
 for name in ['VanguardActionLibrary','CombatModel.VanguardArt']:(p/(name+'.cs')).write_text((root/'Assets/Scripts/Combat'/ (name+'.cs')).read_text())
 (p/'Fixture.cs').write_text(fixture)
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649;0169</NoWarn></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'Test.csproj'),'--',str(root/'Assets/Resources/VanguardActions/Swordguard.bytes')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli')))
 # Compiled negative controls exercise actual loader/adapter, not copied algorithms.
 adapter=p/'CombatModel.VanguardArt.cs';original=adapter.read_text()
 for label,old,new in [('missing-load','vanguardArt=VanguardActionLibrary.Load();','vanguardArt=null;'),('skip-motion','if(vanguardArt==null||pilotOwnerDead||dying)return;','if(true)return;'),('dead-writer','if(vanguardArt==null||pilotOwnerDead||dying)return;','if(vanguardArt==null)return;')]:
  assert old in original
  adapter.write_text(original.replace(old,new,1))
  result=subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'Test.csproj'),'--',str(root/'Assets/Resources/VanguardActions/Swordguard.bytes')],capture_output=True,text=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli')))
  assert result.returncode!=0 and 'Unhandled exception. System.Exception' in result.stderr,(label,result.stdout,result.stderr)
  print('PASS compiled negative control:',label)
 adapter.write_text(original)
model=(root/'Assets/Scripts/Combat/CombatModel.cs').read_text()
assert model.index('ApplyAuthoredVanguardPose(acting,t,hurt);') < model.index('ApplyVisualRecovery(dt);')
assert 'model.ConfigureVanguardArt();' in model and 'SampleVanguardDeath();' in model
print('PASS real factory, AnimateHero and LateUpdate call sites; recovery has final ownership')
