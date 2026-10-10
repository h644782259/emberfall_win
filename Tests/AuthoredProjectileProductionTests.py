#!/usr/bin/env python3
"""Real entry/factory methods and bytes; Unity APIs doubled. Not a GPU/physics run."""
from CastReceiptFixtureSources import include_cast_receipt_source
from pathlib import Path
import os,subprocess,sys,tempfile
root=Path(__file__).resolve().parents[1]
ns={'__file__':str(root/'Tests/AuthoredSpellIntegrationTests.py')};exec((root/'Tests/AuthoredSpellIntegrationTests.py').read_text().split('with tempfile.TemporaryDirectory',1)[0],ns)
s=ns['s']
s=s.replace('public bool IsDead;public int CombatEpoch;','public bool IsDead;public int CombatEpoch;public HeroClass HeroClass;public int NewCastId()=>17;public CastFirstHitReceipt RetainCastReceipt(int id)=>null;')
s=s.replace('public static Material NewGlow()', 'public static GameObject Ring(params object[] a)=>new GameObject("marker");public static Material NewGlow()')
s=s.replace('public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();','public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();public T GetComponentInChildren<T>()where T:Component=>gameObject.GetComponent<T>();')
s=s.replace('public Vector3 right=>','public Vector3 forward=>localRotation.Rotate(Vector3.forward);public Vector3 right=>')
s=s.replace('public bool emitting;public int ClearCount;','public Gradient colorGradient;public Material sharedMaterial;public float time,startWidth,endWidth,minVertexDistance;public int numCapVertices,numCornerVertices;public Color startColor,endColor;public bool emitting;public int ClearCount;')
s=s.replace('public Material(Shader shader){}','public Material(Shader shader){}public void SetColor(string n,Color c){}public void EnableKeyword(string keyword){}public void SetFloat(string n,float v){}')
s=s.replace('public static Vector3 zero=>','public static Vector3 right=>new Vector3(1,0,0);public static Vector3 left=>new Vector3(-1,0,0);public static Vector3 down=>new Vector3(0,-1,0);public static Vector3 back=>new Vector3(0,0,-1);public static Vector3 zero=>')
s=s.replace('public static Quaternion LookRotation(Vector3 forward)=>identity;', 'public static Quaternion LookRotation(Vector3 forward)=>Euler(-(float)Math.Asin(Math.Clamp(forward.normalized.y,-1,1))*180/Mathf.PI,(float)Math.Atan2(forward.x,forward.z)*180/Mathf.PI,0);')
s=s.replace('public struct Color{','public struct Color{public static Color white=>new Color(1,1,1);public static Color operator*(Color c,float f)=>new Color(c.r*f,c.g*f,c.b*f,c.a*f);public static Color Lerp(Color a,Color b,float t)=>new Color(a.r+(b.r-a.r)*t,a.g+(b.g-a.g)*t,a.b+(b.b-a.b)*t,a.a+(b.a-a.a)*t);')
s+='namespace UnityEngine{public class Gradient{public void SetKeys(GradientColorKey[] c,GradientAlphaKey[] a){}}public struct GradientColorKey{public GradientColorKey(Color c,float t){}}public struct GradientAlphaKey{public GradientAlphaKey(float a,float t){}}}'
source=(root/'Assets/Scripts/Combat/CombatEffects.cs').read_text()
def member(src,marker):
 a=src.index(marker);b=src.index('{',a);end=b+1;depth=1
 while depth:depth+=(src[end]=='{')-(src[end]=='}');end+=1
 return src[a:end]
s+='namespace Emberfall{public static class BuildCatalog{'+member((root/'Assets/Scripts/Core/GameTypes.cs').read_text(),'public static float ConcentratedVenomCoefficient(')+'}}'
header=source[source.index('    internal sealed class CombatProjectile'):source.index('        public static void Friendly')]
methods=['public static void Friendly','internal static bool CanLaunchFromMuzzle','public static void BasicShot','public static void Hostile','private static CombatProjectile Make','private void OnDisable','private void OnDestroy','private void BindVisualOrigin','private void AlignBodyFlight']
projectile='using System.Collections.Generic;using UnityEngine;namespace Emberfall{'+header+'\n'.join(member(source[source.index("    internal sealed class CombatProjectile"):],m) for m in methods)+'}}'
# The simulation method is byte-identical to the 2026-10-10 pre-art baseline.
# Refresh the old oracle for already-landed gameplay changes, retaining exclusions below.
import hashlib
simulation=member(source[source.index('internal sealed class CombatProjectile'):],'private void Update()')
steering=member(simulation,'            if(concentrated && age<=')
selection=member(simulation,'                if(concentrated)')
legacy=simulation.replace(steering+'\n','').replace('                EnemyController firstIntercept = null;\n','').replace(selection+'\n','').replace(' || concentrated && enemy!=firstIntercept','')
# Practice attribution adds metadata to the accepted direct-hit call only. Assert
# its exact guarded identity, then retain the pre-art gameplay byte oracle.
practice_identity=',practiceCastId:!basicAttack&&companionSource==null?castId:0'
assert legacy.count(practice_identity)==1,'practice attribution must occur once on the actual accepted hit'
legacy=legacy.replace(practice_identity,'',1)
visual_contact='                    if(concentrated&&enemy.Health<healthBefore)VenomSkillVfx.Contact(owner,owner.EnemyBodyPoint(enemy),false);\n'
assert legacy.count(visual_contact)==1
legacy=legacy.replace(visual_contact,'',1)
assert legacy.count('if(!concentrated)CombatFx.Ring(hitPosition, .7f, color, .2f);')==1
legacy=legacy.replace('if(!concentrated)CombatFx.Ring(hitPosition, .7f, color, .2f);','CombatFx.Ring(hitPosition, .7f, color, .2f);',1)
# Only the reviewed synchronous settlement wrapper is excluded; every simulation byte stays checked.
opening='\n            CombatImpactBatch.BeginAction();\n            try\n            {'
closing='\n\n            }\n            finally { CombatImpactBatch.EndAction(); }'
assert legacy.count(opening)==1 and legacy.count(closing)==1
legacy=legacy.replace(opening,'',1).replace(closing,'',1)
assert hashlib.sha256(legacy.encode()).hexdigest()=='8586f0c38e93ec6be535e4d568dec10356e6b1f45c84b12abf6c0500c4973eb0','non-variant simulation changed outside reviewed venom opt-in blocks'
print('PASS original projectile Update SHA preserved after excluding only explicit B-only steering/selection/filter.')
# Execute exactly the changed area cosmetic setup and age gate; gameplay scheduling is not duplicated.
area=source[source.index('internal sealed class CombatArea'):]
start=area.index('            if(visual==SkillVisualRecipe.Neutral');end=area.index('\n        private void Update()')
setup=area[start:end].rsplit('        }',1)[0]
trapgate='            if(trapCore!=null && age>=delay){Destroy(trapCore);trapCore=null;}'
assert trapgate in area
meteor=member(area,'            if (fallingOrb != null)')
area_fixture='''using UnityEngine;namespace Emberfall{internal sealed class AreaCosmetics:MonoBehaviour{internal GameObject fallingOrb,trapCore;internal Material orbMaterial;internal float age,delay;internal void Setup(PlayerController player,SkillVisualRecipe visual,int statusSkill,float startup,float size,Color tint,bool fallingMeteor){var area=this;var obj=gameObject;delay=startup;'''+setup+'''}internal void Advance(float elapsed){age=elapsed;'''+trapgate+meteor+''' }private void OnDestroy(){if(orbMaterial!=null)Destroy(orbMaterial);}}}'''
s=s.replace('public void SetParent(Transform value,bool worldPositionStays)', 'public void Rotate(float x,float y,float z,Space space){localRotation=localRotation*Quaternion.Euler(x,y,z);}public void SetParent(Transform value,bool worldPositionStays)').replace('public enum PrimitiveType','public enum Space{Self}public enum PrimitiveType')
with tempfile.TemporaryDirectory(prefix='projectile-art-') as d:
 p=Path(d);(p/'Stubs.cs').write_text(s);(p/'Projectile.cs').write_text(projectile);(p/'Area.cs').write_text(area_fixture)
 for f in ['Core/CombatImpactBatch','Combat/ConcentratedVenomRules','Combat/AuthoredProjectileMeshes','Combat/AuthoredActorMeshes','Combat/VisualMeshRecipes','Combat/AnchoredImpactMesh','Combat/CombatVisualLease','Core/CombatVisualBudget']:(p/(Path(f).name+'.cs')).write_text((root/('Assets/Scripts/'+f+'.cs')).read_text())
 (p/'Test.cs').write_text((root/'Tests/AuthoredProjectileProductionTests.cs').read_text());(p/'test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');include_cast_receipt_source(p/'test.csproj');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 command=[sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'test.csproj'),'--',str(root/'Assets/Resources'),str(root/'ArtSource/BlenderProjectiles/factory-samples.json')];env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 subprocess.run(command,env=env,check=True)
 for file,old,new,reason in [('AuthoredProjectileMeshes.cs','if(!Enabled)return null;','if(true)return null;','real resource identity'),('Projectile.cs','false,"HostileBolt"','false,"CasterBolt"','hostile identity')]:
  target=p/file;before=target.read_text();assert old in before;target.write_text(before.replace(old,new));r=subprocess.run(command,env=env,capture_output=True,text=True);target.write_text(before);assert r.returncode!=0 and reason in r.stdout+r.stderr,(reason,r.stdout,r.stderr);print('PASS negative control',reason)
