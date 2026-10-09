#!/usr/bin/env python3
from pathlib import Path
import importlib.util,tempfile,subprocess,os,sys
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
def member(file,key):
 s=(root/file).read_text();a=s.index(key);b=s.index('{',a)+1;depth=1
 while depth:depth+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
with tempfile.TemporaryDirectory(prefix='single-chest-ui-') as d:
 p=Path(d);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 names=['SkillRuntime','GameTypes','ProgressionService','CombatBalance','SkillDamageBudgets','HubTravelRules','MasteryCoreRuntime','TierRewardRules','TierRewardBand','ProgressionGoalState','ChapterProgression','ProgressionService.Chapter','ProgressionService.Attachments','ProgressionService.AutomaticGrowth','ProgressionService.Reforge','ReforgeQuote','RoomTactics','CombatImpactBatch','SafeSaveFlow']
 sources=[root/('Assets/Scripts/Core/'+n+'.cs') for n in names]+[root/'Tests/ProgressionTests.cs',root/'Tests/SingleChestUIBoundary.cs',root/'Assets/Scripts/UI/ChestRevealPresentation.cs',root/'Assets/Scripts/UI/MobilePanelLayout.cs',root/'Assets/Scripts/UI/GameUI.RewardVisuals.cs']
 methods=[]
 for name,keys in [('GameUI.Smith.cs',['private string SmithVariantName(','private string SmithVariantDescription(','private void SelectSmithVariant(']),('GameUI.RunRecap.cs',['private bool SettlementRewardsPending','private void RefreshSettlementChest()','private void CollectSettlementRewards(']),('GameUI.Rewards.cs',['private float ChestDuration','private bool ChestAnimationDone','private void ResetChestReveal()','private void DrawChests()','private static bool CanTrialChestReward(','private void FinishChestReveal()','private bool ChestDismissed','private void DismissChestPanel()','private void EnsurePendingChestPanel()','private bool PendingChestReturnVisible','private Rect PendingChestReturnRect()','private void DrawPendingChestReturn()']),('GameUI.MobileRewards.cs',['private void DrawMobileChests()','private bool DrawMobileChestChoices(']),('GameUI.Expedition.cs',['public void OpenDungeonExit()']),('GameUI.ChestChoices.cs',['private Rect ChestChoiceArt(','private void DrawSingleChestCard('])]:
  methods.extend(member('Assets/Scripts/UI/'+name,key) for key in keys)
 file=p/'Methods.cs';file.write_text('using UnityEngine;namespace Emberfall{public sealed partial class GameUI{'+''.join(methods)+'}}');sources.append(file)
 project=cv.write_project(p/'project',sources,'class Program{static void Main(string[] a){Emberfall.GameUI.Run(a[0]);}}')
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))

 original=file.read_text();sdk=sys.argv[1] if len(sys.argv)>1 else 'dotnet';env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 controls=[('choice<3','choice<2','three equivalent chest choices'),('session.LogSystem(chestRevealResult);','session.Progression.Profile.gold++;session.LogSystem(chestRevealResult);','one normal acknowledgement retains receipt and amounts')]
 for index,(before,after,expected) in enumerate(controls):
  assert before in original;file.write_text(original.replace(before,after,1))
  build=subprocess.run([sdk,'build',str(project),'--no-restore','-v:q'],env=env,capture_output=True,text=True);assert build.returncode==0,build.stdout+build.stderr
  run=subprocess.run([sdk,str(project.parent/'bin/Debug/net8.0/Validation.dll'),str(p/('negative-'+str(index)))],env=env,capture_output=True,text=True)
  assert run.returncode!=0 and expected in run.stdout+run.stderr,run.stdout+run.stderr
  print('PASS compiled three-choice UI negative: '+expected)
