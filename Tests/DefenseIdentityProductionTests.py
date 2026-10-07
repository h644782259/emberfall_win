#!/usr/bin/env python3
"""Actual guard/passive blocks -> nonpooled anchor -> real loaded pooled mesh, with deferred Unity destruction doubles. Args: dotnet [evidence.json]. Default evidence is temporary."""
from CastReceiptFixtureSources import include_cast_receipt_source
from pathlib import Path
import tempfile,subprocess,os,sys
root=Path(__file__).resolve().parents[1]
ns={'__file__':str(root/'Tests/AuthoredSpellIntegrationTests.py')};exec((root/'Tests/AuthoredSpellIntegrationTests.py').read_text().split('with tempfile.TemporaryDirectory',1)[0],ns);stubs=ns['s']
stubs=stubs.replace('public sealed class PlayerController:MonoBehaviour','public sealed partial class PlayerController:MonoBehaviour')
stubs=stubs.replace('public bool HasStarted,ModeFinished,InputBlocked;','public bool HasStarted,ModeFinished,InputBlocked;public Progression Progression=new Progression();public void SpawnMechanismText(Vector3 at,string text,Color c){}')
stubs=stubs.replace('public static void Destroy(Object value){','public static readonly List<Object> Pending=new List<Object>();public static void Destroy(Object value){if(value!=null)Pending.Add(value);}public static void Flush(){var values=Pending.ToArray();Pending.Clear();foreach(var o in values)DestroyNow(o);}private static void DestroyNow(Object value){')
stubs=stubs.replace('foreach(var child in GameObject.All.Where(x=>x.transform.parent==go.transform).ToArray())Destroy(child);','foreach(var child in GameObject.All.Where(x=>x.transform.parent==go.transform).ToArray())DestroyNow(child);')
# Match Unity hierarchy disable callbacks for lease release before end-of-frame Destroy.
stubs=stubs.replace('public void SetActive(bool value){if(activeSelf==value)return;activeSelf=value;Call(value?"OnEnable":"OnDisable");}', 'public void SetActive(bool value){if(activeSelf==value)return;var descendants=GameObject.All.Where(o=>o!=this&&Descends(o.transform,transform)&&o.activeInHierarchy).ToArray();activeSelf=value;Call(value?"OnEnable":"OnDisable");if(!value)foreach(var o in descendants)o.Call("OnDisable");}private static bool Descends(Transform t,Transform ancestor){for(var p=t.parent;p!=null;p=p.parent)if(p==ancestor)return true;return false;}')
def member(s,key):
 a=s.index(key);i=s.index('{',a)+1;n=1
 while n:n+=(s[i]=='{')-(s[i]=='}');i+=1
 return s[a:i]
def block(s,key):
 t=member(s,key);return t[t.index('{'):]
player=(root/'Assets/Scripts/Combat/PlayerController.cs').read_text()
guardkeys=['if (slot == 5)','if (HeroClass == HeroClass.Vanguard && slot == 4)','else if (HeroClass == HeroClass.Arcanist && slot == 5)']
cast=player[player.index('private void CastSkillCore'):]
guards=''.join('internal void Guard'+str(i)+'(int rank,float power,float range,Color color,int castId)'+block(cast,k) for i,k in enumerate(guardkeys))
fixture='''using System;using UnityEngine;namespace Emberfall {
public static class FlameRide{public static void Spawn(PlayerController p,GameSession g,float d,float a,int cast){}}
public enum HeroClass{Vanguard,Arcanist,Ranger,Summoner}public enum ElementalistSpecialization{None,Burn}
public class Profile{public int[] skillRanks=new int[10];}public class Progression{public Profile Profile=new Profile();}
public static class GameBalance{public static float SkillRangeMultiplier(int r)=>1+(r-1)*.15f;public static Color ClassColor(HeroClass h)=>new Color(1,.8f,.3f,1);public static string SkillName(HeroClass h,int slot)=>"passive";}
public static class SkillDamageBudgets{public const float BasicEnergyOnHit=1;}
public class SkillRuntime{public float Energy;public void RestoreEnergy(float n){Energy+=n;}}
public sealed partial class PlayerController {
private GameSession session=>GameSession.Instance;internal float guardTime,guardPower,guardReduction,guardRadius,burnStrideTime,guardPulseTimer,passiveTime,passiveCooldown,passiveReduction,passiveSpeed,invulnerability;
// Cast ownership is an explicit neutral boundary in these guard presentation tests.
private CastFirstHitReceipt guardCastReceipt;private void HoldCastReceipt(ref CastFirstHitReceipt receipt,int id){}
internal int guardRank,guardCastId;internal HeroClass HeroClass;internal ElementalistSpecialization Specialization;internal float Health=10,MaxHealth=100,CombatAttack=100;internal SkillRuntime skillRuntime=new SkillRuntime();internal int AreaHits,Controls;
private void HitArea(Vector3 p,float radius,float damage,float knock,float stun){AreaHits++;}private void ControlArea(Vector3 p,float radius,float duration){Controls++;}
GUARDS
PASSIVE
internal void TriggerPassive(){TryDefensePassive();}
}}
'''.replace('GUARDS',guards).replace('PASSIVE',member(player,'private void TryDefensePassive('))
evidence_path=Path(sys.argv[2]).expanduser().resolve() if len(sys.argv)>2 else None
with tempfile.TemporaryDirectory(prefix='defense-identity-') as d:
 p=Path(d);(p/'Stubs.cs').write_text(stubs);(p/'Player.cs').write_text(fixture);(p/'Test.cs').write_text((root/'Tests/DefenseIdentityProductionTests.cs').read_text())
 for f in ['Core/FilledVfxRecipes','Core/FilledVfxPlacement','Core/CombatVisualBudget','Combat/CombatVisualLease','Combat/AnchoredImpactMesh','Combat/FilledSkillVfx','Combat/AuthoredActorMeshes','Combat/AuthoredSpellBases','Combat/AdvancedSkillVfx']:(p/(Path(f).name+'.cs')).write_text((root/('Assets/Scripts/'+f+'.cs')).read_text())
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>');include_cast_receipt_source(p/'Test.csproj');(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 cmd=[sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'Test.csproj'),'--',str(root/'Assets/Resources'),str(evidence_path if evidence_path is not None else p/'Runtime-Samples.json')]
 env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 subprocess.run(cmd,env=env,check=True)
 for name,old,new,expected in [('missing-identity','detail,true,4','detail,true,0','actual defense selects loaded ProtectionCage'),('lost-body-envelope','detail,true,4,true','detail,true,4,false','persistent protection has body envelope'),('stale-state','if(stateActive!=null&&!stateActive())','if(false)','state cancellation hides hierarchy before deferred destruction')]:
  path=p/'AdvancedSkillVfx.cs';before=path.read_text();assert old in before;path.write_text(before.replace(old,new));result=subprocess.run(cmd,env=env,text=True,capture_output=True);path.write_text(before)
  assert result.returncode!=0 and expected in result.stdout+result.stderr,result.stdout+result.stderr
  print('PASS compiled negative control',name)
