"""Actual contact branch + save transactions; scene query and Unity serialization are managed doubles."""
from pathlib import Path
import importlib.util,sys,subprocess,tempfile,os
r=Path(__file__).resolve().parents[1];dotnet=sys.argv[1]
spec=importlib.util.spec_from_file_location('cv',r/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
def member(src,key):
 a=src.index(key);b=src.index('{',a)+1;depth=1
 while depth:depth+=(src[b]=='{')-(src[b]=='}');b+=1
 return src[a:b]
with tempfile.TemporaryDirectory(prefix='venom-') as t:
 p=Path(t);config=p/'NuGet.Config';config.write_text('<configuration><packageSources><clear/></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 def run(project,expected=None):
  subprocess.run([dotnet,'build',str(project),'--configfile',str(config),'-v:q'],env=env,check=True)
  out=subprocess.run([dotnet,str(project.parent/'bin/Debug/net8.0/Validation.dll'),str(p/'save')],env=env,capture_output=True,text=True);print(out.stdout+out.stderr)
  if expected:assert out.returncode and expected in out.stdout+out.stderr
  else:out.check_returncode()
 effects=(r/'Assets/Scripts/Combat/CombatEffects.cs').read_text();update=member(effects[effects.index('internal sealed class CombatProjectile'):],'private void Update()');a=update.index('                DestructibleProp hitProp;');b=update.index('\n            }\n            if (terrainHit)',a);branch=update[a:b]
 contact=p/'Contact.cs';contact.write_text('using UnityEngine;namespace Emberfall{internal partial class CombatProjectile{public void Contacts(Vector3 previous){'+branch+'}}}')
 sources=[r/'Assets/Scripts'/f'{x}.cs' for x in ['Core/GameTypes','Core/CombatBalance','Core/SkillDamageBudgets','Core/DestructiblePropRules','Core/LockedImpactMarkPolicy','Combat/CombatDamage','Combat/ProjectileVolleyBudget','Combat/ConcentratedVenomRules']]+[contact,r/'Tests/ConcentratedVenomContactFixture.cs']
 project=cv.write_project(p/'contact',sources,program='');run(project)
 original=contact.read_text();contact.write_text(original.replace(' || concentrated && enemy!=firstIntercept',''));run(project,'front guard intercept independent of list order');contact.write_text(original);print('PASS compiled prior list-order behavior rejected')
 core=['GameTypes','ProgressionService','ProgressionService.Chapter','ChapterProgression','RoomTactics','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Reforge','ReforgeQuote','CombatBalance','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState']
 types=p/'GameTypes.cs';types.write_text((r/'Assets/Scripts/Core/GameTypes.cs').read_text());sources=[r/'Assets/Scripts/Core'/f'{x}.cs' for x in core if x!='GameTypes']+[types,r/'Tests/ProgressionTests.cs',r/'Tests/ConcentratedVenomPersistenceTests.cs'];project=cv.write_project(p/'save-project',sources,program='class Program{static void Main(string[] args){System.Console.WriteLine(ConcentratedVenomPersistenceTests.Run(args[0]));}}');run(project)
 types.write_text(types.read_text().replace(' || mechanic == EquipmentMechanic.VenomSpread; }','; }'));run(project,'firstB unlock four shards');print('PASS compiled previous catalog rejects new variant')
player=(r/'Assets/Scripts/Combat/PlayerController.cs').read_text();venom=(r/'Assets/Scripts/Combat/PlayerController.Venom.cs').read_text()
assert 'bool spread = HasMechanic(EquipmentMechanic.VenomSpread) && !ConcentratedVenom;' in player
assert 'CastConcentratedVenom(rank, range, color, castId);\n                        return;' in player
assert 'concentratedTarget:locked, concentrated:true' in venom and 'blastDamage:' not in venom and 'piercing:' not in venom
print('PASS production dispatch exits before fan/explosion, propagation explicitly excludes B; original energy/CD path retained')
