"""Exercise level and dungeon milestone receipts against the actual save service."""
from pathlib import Path
import importlib.util,os,subprocess,tempfile
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
program=r'''using System;using Emberfall;
class Program{
 static int n;static void C(bool ok,string m){n++;if(!ok)throw new Exception(m);}static bool Near(float a,float b){return Math.Abs(a-b)<.001f;}
 static void Main(string[] args){
 for(int rank=1;rank<=3;rank++){
 var r=new RelicGemRuntime();r.Configure(0,rank,0);C(r.SkillCast(0).Energy==0&&r.SkillCast(0).Energy==0,"same skill cannot build distinct chain");C(r.SkillCast(1).Energy==8+4*rank,"alternating skills restore energy");C(r.SkillCast(2).Energy==0,"chain cooldown prevents rapid repeat");r.Advance(8);r.SkillCast(0);r.Advance(6.1f);C(r.SkillCast(1).Energy==0,"expired chain cannot trigger");
 r=new RelicGemRuntime();r.Configure(0,rank,1);C(r.SkillCast(0).Energy==0,"echo requires ultimate");r.SkillCast(9);C(r.SkillCast(3).Energy==0,"passive is not successful cast");for(int i=0;i<3;i++)C(r.SkillCast(0).Energy==6+2*rank,"ultimate arms three refunds");C(r.SkillCast(0).Energy==0,"fourth refund forbidden");r.SkillCast(9);r.Advance(8.1f);C(r.SkillCast(0).Energy==0,"echo expires");
 r=new RelicGemRuntime();r.Configure(1,rank,0);r.SkillCast(0);r.SkillCast(1);C(Near(r.SkillCast(2).Cooldown,.5f+.5f*rank),"three-skill cooldown chain");C(r.SkillCast(4).Cooldown==0,"cooldown chain bounded");r.Configure(1,rank,1);C(r.SkillCast(0).OtherCooldown==0&&r.SkillCast(9).OtherCooldown==1+rank,"ultimate variant only reduces other skills");
 r=new RelicGemRuntime();r.Configure(2,rank,0);C(r.SkillCast(0).Burst==0,"strike requires dodge");r.PerfectDodge();C(Near(r.SkillCast(0).Burst,.4f+.3f*rank)&&r.SkillCast(1).Burst==0,"one followup burst only");r.Advance(6);r.PerfectDodge();r.Advance(4.1f);C(r.SkillCast(0).Burst==0,"expired dodge followup discarded");
 r.Configure(2,rank,1);C(!r.SkillCast(0).ResetDodge&&r.SkillCast(1).ResetDodge,"two skills refresh dodge");C(Near(r.PerfectDodge().Burst,.6f+.4f*rank)&&r.PerfectDodge().Burst==0,"perfect dodge consumes counter once");
 r.Reset();C(r.PerfectDodge().Burst==0,"world transition retires counter");r.Configure(2,0,1);r.SkillCast(0);C(!r.SkillCast(1).ResetDodge,"unmounted or unascended disabled");
 }
 var combined=new RelicGemRuntime();for(int i=0;i<3;i++)combined.Configure(i,3,0);combined.PerfectDodge();C(combined.SkillCast(0).Burst>0,"three sockets coexist: dodge strike");C(combined.SkillCast(1).Energy>0,"three sockets coexist: energy chain");C(combined.SkillCast(2).Cooldown>0,"three sockets coexist: cooldown chain");
 var names=new System.Collections.Generic.HashSet<string>();for(int i=0;i<3;i++)for(int v=0;v<2;v++){var gem=(EquipmentMechanic)((int)EquipmentMechanic.RelicPower+i);C(names.Add(BuildCatalog.GemFormName(gem,v)),"all six relic forms have distinct identities");C(!BuildCatalog.GemFormDescription(gem,v,1).Contains("回复速度"),"relic descriptions no longer reuse passive recovery");}
 foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass))){var skills=new SkillRuntime(hero);skills.TryConsume(0,1);C(skills.TryConsume(9,1),"ultimate cast");float ultimate=skills.Remaining(9),other=skills.Remaining(0);skills.ReduceNonUltimateCooldowns(2);C(Near(skills.Remaining(9),ultimate)&&skills.Remaining(0)<=other,"other cooldown reduction never touches ultimate");}
 Console.WriteLine("PASS "+n+" relic combo and lifecycle assertions");}}
'''
with tempfile.TemporaryDirectory(prefix='achievement-milestones-') as tmp:
 p=Path(tmp);names=['SkillRuntime','GameTypes','ProgressionService','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Trading','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+name+'.cs') for name in names]+[root/'Tests/ProgressionTests.cs'];project=cv.write_project(p/'project',sources,program)
 sdk='/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet'
 subprocess.run([sdk,'run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
