#!/usr/bin/env python3
"""Execute production disclosure navigation methods with managed session boundaries, not Unity."""
from pathlib import Path
import subprocess, tempfile, sys, os
root=Path(__file__).resolve().parents[3]
for entry in ['GameUI.Expedition.cs','GameUI.MobileWorkshop.cs']:
 assert 'RequestBuildPlanAction(BuildPlanAction.Reset)' in (root/'Assets/Scripts/UI'/entry).read_text(),entry
source=(root/'Assets/Scripts/UI/GameUI.BuildPlans.cs').read_text()
def method(key):
 a=source.index(key);b=source.index('{',a)+1;depth=1
 while depth:
  depth+=(source[b]=='{')-(source[b]=='}');b+=1
 return source[a:b]
methods='\n'.join(method(k) for k in ['private void OpenBuildPlans()', 'private void RequestBuildPlanAction(', 'private bool CloseBuildPlanSurface()', 'private void ResetBuildPlanSurface()', 'private void ReconcileBuildPlanSurface()'])
shell='''
using System;
class Vector2 {public static Vector2 zero=new Vector2();}
class Progression {public string CurrentSlotId="character-one";public object Profile=new object();public string BuildStateFingerprint()=>"state";public string BuildPresetSummary(int slot)=>"plan";public string CurrentBuildSummary()=>"current";public string BuildPresetLockReason(int slot,bool camp)=>null;}
class Session {public Progression Progression=new Progression();public object Player=new object();public bool PracticeActive,IsInCamp=true;public object Result=new object(),Baseline=new object();}
class GameUI {
 enum Panel {Camp,None} enum BuildPlanAction {None,Save,Apply,Replace,Reset}
 Session session=new Session();Panel panel=Panel.Camp;BuildPlanAction buildPlanAction;
 bool buildPlansOpen,buildPlanChoosing,practiceChoicesOpen;int buildPlanDetails=-1,blocks,cancels,draftCancels,buildPlanSlot;string buildPlanFingerprint;
 object buildPlanReplacement,buildPlanOwner,buildPlanSource,buildPlanHero,allocationDraft;
 string buildPlanCharacterId,buildPlanPreview,buildPlanError;Vector2 buildPlanScroll;
 void CancelMobileScroll(){cancels++;} void BlockUITransition(){blocks++;}
 void CancelAllocationDraft(){if(allocationDraft!=null)draftCancels++;allocationDraft=null;}
 METHODS
 static int n;static void Check(bool v,string label){n++;if(!v)throw new Exception(label);}
 static void Main(){
 foreach(var action in new[]{BuildPlanAction.Save,BuildPlanAction.Apply,BuildPlanAction.Replace,BuildPlanAction.Reset}){
  var ui=new GameUI();ui.OpenBuildPlans();var result=ui.session.Result;var baseline=ui.session.Baseline;
  Check(ui.buildPlansOpen&&!ui.practiceChoicesOpen,"open defaults collapsed");
  ui.practiceChoicesOpen=true;ui.buildPlanAction=action;
  Check(ui.CloseBuildPlanSurface()&&ui.buildPlansOpen&&ui.buildPlanAction==BuildPlanAction.None,"cancel confirmation returns to plan list");
  Check(ui.practiceChoicesOpen&&ReferenceEquals(result,ui.session.Result)&&ReferenceEquals(baseline,ui.session.Baseline),"cancel preserves disclosure and session records");
  Check(ui.CloseBuildPlanSurface()&&!ui.buildPlansOpen,"next Back closes plan page");
  ui.OpenBuildPlans();Check(!ui.practiceChoicesOpen,"reopen collapses optional practice");
  ui.buildPlanChoosing=true;Check(ui.CloseBuildPlanSurface()&&ui.buildPlansOpen&&!ui.buildPlanChoosing,"Back from repair chooser returns to plans");
  ui.allocationDraft=new object();Check(ui.CloseBuildPlanSurface()&&ui.buildPlansOpen&&ui.allocationDraft==null&&ui.draftCancels==1,"Back cancels only draft before outer page");
  Check(ui.blocks==6&&ui.cancels==6,"navigation cancels scroll and latches initiating input");
  ui.practiceChoicesOpen=true;ui.session.PracticeActive=true;ui.panel=Panel.None;ui.ReconcileBuildPlanSurface();Check(ui.buildPlansOpen&&ui.practiceChoicesOpen,"temporary practice panel does not reset page");
  ui.session.PracticeActive=false;ui.panel=Panel.Camp;ui.ReconcileBuildPlanSurface();Check(ui.buildPlansOpen&&ui.practiceChoicesOpen,"practice return retains expanded result access");
  ui.session.Progression.CurrentSlotId="character-two";ui.ReconcileBuildPlanSurface();Check(!ui.buildPlansOpen&&!ui.practiceChoicesOpen,"character change clears transient UI ownership");
 }
 {
  var ui=new GameUI();var result=ui.session.Result;var baseline=ui.session.Baseline;
  ui.OpenBuildPlans();ui.practiceChoicesOpen=true;ui.buildPlanDetails=1;
  ui.RequestBuildPlanAction(BuildPlanAction.Save);ui.CloseBuildPlanSurface();
  Check(ui.practiceChoicesOpen&&ui.buildPlanDetails==1,"internal confirmation cancellation preserves disclosure");
  ui.CloseBuildPlanSurface();
  Check(!ui.practiceChoicesOpen&&ui.buildPlanDetails==-1,"outer closure clears disclosures");
  ui.practiceChoicesOpen=true;ui.buildPlanDetails=1; // stale external state must also be reconciled
  ui.RequestBuildPlanAction(BuildPlanAction.Reset);ui.CloseBuildPlanSurface();
  Check(ui.buildPlansOpen&&!ui.practiceChoicesOpen&&ui.buildPlanDetails==-1,"external free-reset shortcut reopens collapsed");
  ui.practiceChoicesOpen=true;ui.buildPlanDetails=0;ui.ResetBuildPlanSurface();ui.session.Player=new object();
  ui.RequestBuildPlanAction(BuildPlanAction.Reset);ui.CloseBuildPlanSurface();
  Check(!ui.practiceChoicesOpen&&ui.buildPlanDetails==-1,"post-class-rebind direct shortcut is collapsed");
  Check(ReferenceEquals(result,ui.session.Result)&&ReferenceEquals(baseline,ui.session.Baseline),"entry and rebind retain practice evidence");
 }
 Console.WriteLine("PASS "+n+" production navigation assertions; managed boundaries, not Unity events");}
}
'''.replace('METHODS',methods)
with tempfile.TemporaryDirectory(prefix='loadout-navigation-') as tmp:
 p=Path(tmp);(p/'Program.cs').write_text(shell);(p/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 (p/'Replay.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>')
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'Replay.csproj')],check=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))

 # Each compiled mutation must fail its behavioral assertion, not compilation.
 for name,before,after,expected in [
  ('outer-close','else {buildPlansOpen=false;buildPlanDetails=-1;practiceChoicesOpen=false;}','else buildPlansOpen=false;','outer closure clears disclosures'),
  ('external-entry','if(!buildPlansOpen){buildPlanDetails=-1;practiceChoicesOpen=false;}','','external free-reset shortcut reopens collapsed')]:
  assert before in shell,name
  (p/'Program.cs').write_text(shell.replace(before,after,1))
  result=subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'Replay.csproj')],stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'))
  assert result.returncode!=0 and 'error CS' not in result.stdout and expected in result.stdout,result.stdout
  print('PASS compiled negative '+name+': '+expected)
