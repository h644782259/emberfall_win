#!/usr/bin/env python3
"""Actual practice GUI emits bounded top-band controls and frozen A/B presentation."""
from pathlib import Path
import os,sys,tempfile,subprocess
root=Path(__file__).resolve().parents[1]
def member(s,key):
 a=s.index(key);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
with tempfile.TemporaryDirectory(prefix='practice-hud-') as d:
 p=Path(d)
 for rel in ['Core/CombatImpactBatch','Core/CampPracticeRecord','UI/GameUI.Practice','UI/PracticeResultPresentation','UI/PracticeHudLayout','UI/MobilePanelLayout','UI/MobileControlLayout','Core/HudLogicalScale','Combat/MobileSkillPolicy']:(p/(Path(rel).name+'.cs')).write_text((root/'Assets/Scripts'/(rel+'.cs')).read_text())
 ui=(root/'Assets/Scripts/UI/GameUI.cs').read_text();mobile=(root/'Assets/Scripts/UI/GameUI.Mobile.cs').read_text()
 matrix=next(line.strip() for line in member(ui,'private void OnGUI()').splitlines() if 'GUI.matrix = Matrix4x4.TRS' in line)
 draft=(root/'Assets/Scripts/UI/GameUI.BuildDraft.cs').read_text();plans=(root/'Assets/Scripts/UI/GameUI.BuildPlans.cs').read_text()
 (p/'SharedUI.cs').write_text('using UnityEngine;namespace Emberfall{public sealed partial class GameUI{'+member(draft,'private void DraftButton(')+member(plans,'private static Rect BuildPlanRect(')+member(plans,'private void BuildPlanParagraph(')+member(mobile,'private float TouchRatio')+member(mobile,'private Rect TouchRect(MobileControlLayout.Area')+member(ui,'private void RefreshLayout()')+'public void PixelFrame(){RefreshLayout();'+matrix+'Labels.Clear();Buttons.Clear();PixelButtons.Clear();DrawPracticeCombatHUD();DrawPracticeOverlay();GUI.matrix=Matrix4x4.identity;} } }')
 (p/'Test.cs').write_text((root/'Tests/PracticeHudProductionTests.cs').read_text())
 project=p/'Test.csproj';project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NoWarn>0649;0414;0169</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1');sdk=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
 def run(args):
  q=subprocess.run([sdk]+args,env=env,text=True,capture_output=True);print(q.stdout+q.stderr);return q
 assert run(['run','--project',str(project)]).returncode==0
 for name,old,new,message in [('PracticeResultPresentation','造成扣血的施法 / 施放','有效 / 施放','skill ratio explicitly names actual health loss casts'),('GameUI.Practice','治疗看有效治疗；伙伴伤害计入总伤害','治疗与伙伴','actual result GUI explains supportive cast accounting without false failure'),('PracticeHudLayout','buttonHeight=Math.Max(48,48/pixelsPerUnit)','buttonHeight=48','physical practice actions remain at least 48 pixels after production transforms'),('CampPracticeRecord','(string[])skillNames.Clone()','skillNames','record freezes incoming skill labels'),('CampPracticeRecord','&&RulesVersion==other.RulesVersion','', 'changed target rules cannot compare'),('PracticeResultPresentation','Number(r.DamagePerSecond)','Number(r.ActualDamage/r.Duration)','DPS uses actual completion time not configured cap')]:
  path=p/(name+'.cs');original=path.read_text();assert old in original;path.write_text(original.replace(old,new));assert run(['build',str(project),'--no-restore','-v:q']).returncode==0
  q=run([str(p/'bin/Debug/net8.0/Test.dll')]);assert q.returncode!=0 and message in q.stdout+q.stderr;print('PASS compiled negative:',message);path.write_text(original)
