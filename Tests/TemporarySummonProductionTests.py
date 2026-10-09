#!/usr/bin/env python3
"""Run actual summon recast/auto-target methods and lifetime rules with managed engine boundaries."""
from pathlib import Path
import ast,subprocess,tempfile,sys,os
root=Path(__file__).resolve().parents[1]
source=(root/'Assets/Scripts/Combat/SummonedCompanion.cs').read_text()
def member(mark):
 a=source.index(mark);b=source.index('{',a)+1;depth=1
 while depth:
  depth+=(source[b]=='{')-(source[b]=='}');b+=1
 return source[a:b]
tree=ast.parse((root/'Tests/CompanionIntentProductionTests.py').read_text())
for node in tree.body:
 if isinstance(node,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='shell' for t in node.targets):
  shell=node.value.func.value.value;break
shell=shell.split('  public static int Verify()')[0]
shell=shell.replace('private bool pathClear;','private bool pathClear=true;')
shell=shell.replace('IsStarter=k==Kind.Wolf','IsStarter=false')
shell=shell.replace('active.Add(pet);return pet;','pet.transform.position=at;pet.RemainingLifetime=CompanionRules.ContractLifetime((int)k,r,permanent);active.Add(pet);return pet;')
shell=shell.replace('public enum SummonerRoute','public enum CompanionRetirementReason{Replaced} public enum SummonerRoute')
shell=shell.replace('  METHODS','  private void Dismiss(CompanionRetirementReason reason){IsAlive=false;active.Remove(this);}'+'\n  METHODS')
markers=['private sealed class BondState','private static BondState State(', 'public static SummonedCompanion[] Snapshot(', 'private struct PackPathProbe','public static SummonedCompanion CastContract(', 'private EnemyController AcquireTarget()', 'internal bool AdvanceLifetime(']
shell=shell.replace('METHODS',chr(10).join(member(x) for x in markers))
shell+=r"""
 public static int Verify(){
  int n=0;Action<bool,string> check=(v,m)=>{n++;if(!v)throw new Exception(m);};
  var owner=new PlayerController();var game=new GameSession{Player=owner};GameSession.Instance=game;
  check(Snapshot(owner).Length==0,"no summon before a skill cast");
  var wolf=CastContract(owner,game,Kind.Wolf,1,Vector3.zero,10,false);
  check(!wolf.IsPermanent&&!wolf.IsStarter&&wolf.RemainingLifetime==14,"wolf is a timed summon");
  var tower=CastContract(owner,game,Kind.Spirit,2,new Vector3(6,0,0),10,false);
  check(tower.RemainingLifetime==14&&!tower.IsPermanent,"turret is timed");
  for(int i=0;i<100;i++)CastContract(owner,game,Kind.Spirit,2,new Vector3(6,0,0),10,false);
  check(Snapshot(owner).Length==2&&!tower.IsAlive,"recasts replace same form without accumulating summons");
  var ultimate=CastContract(owner,game,Kind.Treant,3,Vector3.zero,10,false);
  check(ultimate.RemainingLifetime==16&&!ultimate.IsPermanent,"ultimate creates a timed treant");
  CastContract(owner,game,Kind.Wolf,2,Vector3.zero,10,true);
  check(Array.FindAll(Snapshot(owner),p=>p.Form==Kind.Wolf).Length==2,"pack mode creates two temporary wolves");
  var near=new EnemyController();near.transform.position=new Vector3(2,0,0);
  var far=new EnemyController();far.transform.position=new Vector3(9,0,0);
  game.Enemies.Add(far);game.Enemies.Add(near);
  check(ultimate.AcquireTarget()==near,"autonomous summon selects nearest reachable enemy");
  near.IsDead=true;check(ultimate.AcquireTarget()==far,"autonomous retarget after death");
  for(int form=0;form<3;form++)for(int rank=1;rank<=3;rank++){
   check(!float.IsInfinity(CompanionRules.ContractLifetime(form,rank,true)),"legacy permanent flag cannot produce infinite lifetime");
   check(!CompanionRules.PermanentPartner(true,form,false),"no permanent route");
  }
  check(ultimate.AdvanceLifetime(0)&&ultimate.RemainingLifetime==16,"pause does not age summon");
  check(ultimate.AdvanceLifetime(15.9f)&&!ultimate.AdvanceLifetime(.2f)&&ultimate.RemainingLifetime==0,"summon expires exactly after its timed life");
  return n;
 }
}}
class Program {static void Main(){Console.WriteLine(Emberfall.SummonedCompanion.Verify()+" production summon checks passed");}}
"""
with tempfile.TemporaryDirectory(prefix='temporary-summons-') as work:
 p=Path(work);(p/'Replay.cs').write_text(shell)
 for name in ['CompanionRules.cs','CompanionDirective.cs']:(p/name).write_text((root/'Assets/Scripts/Combat'/name).read_text())
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType></PropertyGroup></Project>')
 (p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 subprocess.run([sys.argv[1],'run','--project',str(p/'Test.csproj')],env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'),check=True)
assert 'permanent=false;foundation=false;' in member('public static SummonedCompanion Summon(')
assert 'SummonStarter(this' not in (root/'Assets/Scripts/Combat/PlayerController.cs').read_text()
assert 'bool moving = Form!=Kind.Spirit' in member('private void Update()')
assert 'CompanionCommandsVisible {get{return false;}}' in (root/'Assets/Scripts/UI/GameUI.CombatOpportunities.cs').read_text()
print('PASS no automatic starter, fixed turret, hidden command controls')
