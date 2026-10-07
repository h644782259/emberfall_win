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
 sources=[root/('Assets/Scripts/Core/'+n+'.cs') for n in names]+[root/'Tests/ProgressionTests.cs',root/'Tests/RewardPolishUIBoundary.cs',root/'Assets/Scripts/UI/GameUI.RewardMoments.cs',root/'Assets/Scripts/UI/ChestRevealPresentation.cs',root/'Assets/Scripts/UI/MobilePanelLayout.cs',root/'Assets/Scripts/UI/GameUI.RewardVisuals.cs']
 methods=[]
 for name,keys in [('GameUI.Rewards.cs',['private void DrawChestRevealTransition(', 'private float ChestDuration','private bool ChestAnimationDone','private void ResetChestReveal()','private void DrawChests()','private static bool CanTrialChestReward(','private void FinishChestReveal()','private string[] ChestSectionCopy(','private float ChestSectionsHeight(','private void DrawChestSections(']),('GameUI.MobileRewards.cs',['private void DrawMobileChests()','private bool DrawMobileChestChoices(','private void DrawMobileChestResult(']),('GameUI.CollectionPreview.cs',['private bool DrawChestRewardModel(']),('GameUI.ChestChoices.cs',['private Rect ChestChoiceArt(','private void DrawSingleChestCard('])]:
  methods.extend(member('Assets/Scripts/UI/'+name,key) for key in keys)
 methods.append('')
 color=p/'ColorScale.cs';color.write_text('namespace UnityEngine{public partial struct Color{public static Color operator *(Color c,float f)=>c;}}');sources.append(color)
 colorFixture=p/'ProgressionBoundary.cs';colorFixture.write_text((root/'Tests/ProgressionTests.cs').read_text().replace('public struct Color','public partial struct Color'));sources=[colorFixture if f==root/'Tests/ProgressionTests.cs' else f for f in sources]
 helper=p/'Receipt.cs';helper.write_text('using System;namespace Emberfall{public static class EquipmentComparisonPresentation{'+member('Assets/Scripts/UI/EquipmentComparisonPresentation.cs','public static FashionData Trial(')+member('Assets/Scripts/UI/EquipmentComparisonPresentation.cs','public static FashionData Receipt(')+'}}');sources.append(helper)
 file=p/'Methods.cs';file.write_text('using UnityEngine;namespace Emberfall{public sealed partial class GameUI{'+''.join(methods)+'}}');sources.append(file)
 project=cv.write_project(p/'project',sources,'class Program{static void Main(string[] a){Emberfall.GameUI.Run(a[0]);}}')
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(project),'--',str(p/'saves')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))


 original=file.read_text();moment=p/'Moment.cs';moment.write_text((root/'Assets/Scripts/UI/GameUI.RewardMoments.cs').read_text());momentOriginal=moment.read_text()
 sources=[moment if f==root/'Assets/Scripts/UI/GameUI.RewardMoments.cs' else f for f in sources]
 project=cv.write_project(p/'controls',sources,'class Program{static void Main(string[] a){Emberfall.GameUI.Run(a[0]);}}')
 sdk=sys.argv[1] if len(sys.argv)>1 else 'dotnet';env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1')
 controls=[
  (file,'?300:144','?0:0','actual compact mobile result reserves readable model height'),
  (moment,'Event.current.Use();','','skip consumes pointer before underlying panel'),
  (file,'||reward.Duplicate?SoundCue.UI','?SoundCue.UI','actual desktop/mobile first acquisition and duplicate completion use distinct audio')]
 for index,(target,before,after,expected) in enumerate(controls):
  file.write_text(original);moment.write_text(momentOriginal);baseline=target.read_text();assert before in baseline;target.write_text(baseline.replace(before,after,1))
  build=subprocess.run([sdk,'build',str(project),'-v:q'],env=env,capture_output=True,text=True);assert build.returncode==0,build.stdout+build.stderr
  run=subprocess.run([sdk,str(project.parent/'bin/Debug/net8.0/Validation.dll'),str(p/('negative-'+str(index)))],env=env,capture_output=True,text=True)
  assert run.returncode!=0 and expected in run.stdout+run.stderr,run.stdout+run.stderr
  print('PASS compiled reward-polish UI negative: '+expected)
