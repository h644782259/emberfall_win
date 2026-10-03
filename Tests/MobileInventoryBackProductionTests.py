"""Execute actual ClosePanel and mandatory old-entry negative control by default.
--legacy runs only the intentionally failing old-entry replay for debugging.
Engine/session shells do not validate Unity event delivery or actual touch rendering.
"""
from pathlib import Path
import importlib.util,os,subprocess,sys,tempfile
root=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
def method(file,signature):
 source=(root/'Assets/Scripts/UI'/file).read_text();start=source.index(signature);end=source.index('{',start)+1;depth=1
 while depth:
  depth+=(source[end]=='{')-(source[end]=='}');end+=1
 return source[start:end]
close=method('GameUI.cs','private void ClosePanel()')+'\n'+method('GameUI.BuildPlans.cs','private void CancelPresetSale()')
hook='if(CloseMobileInventoryDetail())return;'
assert close.count(hook)==1,'production entry must contain exactly one inventory detail hook'
assert 'ClosePanel(); return;' in method('GameUI.MobileInventory.cs','private void DrawMobileEquipmentActions(')
assert 'else ClosePanel();' in method('GameUI.MobilePanels.cs','private bool DrawMobilePanelChrome(')
assert 'else if (panel != Panel.None) ClosePanel();' in method('GameUI.cs','private void Update()')
shell=r'''
using System;
namespace UnityEngine {public static class Time{public static float unscaledTime;}}
namespace Emberfall {
 public static class MobileControls {public static bool Active=true;public sealed class Bounds{public float Width=568;}public static Bounds Layout=new Bounds();}
 public sealed class ProfileStub {public bool pendingFashionChest,pendingChestReveal;}
 public sealed class ProgressionStub {public ProfileStub Profile=new ProfileStub();}
 public sealed class SessionStub {public bool Paused,HasStarted=true,Blocked=true;public ProgressionStub Progression=new ProgressionStub();public void SetUIBlocking(bool value){Blocked=value;}public void SetPaused(bool value){Paused=value;}}
 public sealed partial class GameUI {
  enum Panel{None,Inventory,Skills,Camp,TravelMap,SaveSelection,Chests,Fashion,PotionAssignment,Bindings,SaveLocation,Controls}
  Panel panel=Panel.Inventory,bindingReturnPanel;SessionStub session=new SessionStub();
  bool presetSaleOpen;bool mobileInventoryDetail=true,saveSelectionFromPause,chestDetails,bindingReturnPause,saveReturnPause,controlsReturnPause;
  int mobileInventoryTab,rebindingSlot,blocks,cancels;float mobileInventoryListScroll=173,mobileInventoryDetailScroll=81,chestRevealedAt;
  const float ChestDuration=1;bool ChestAnimationDone=>true;
  bool CloseMobileSkillDetail()=>false;bool CloseChapterSelection()=>false;bool CloseRouteSkill()=>false;bool CloseProgressionGoalSurface()=>false;bool CloseBuildPlanSurface()=>false;bool CloseTravelMap()=>false;bool CancelSaveDeletion()=>false;bool CancelActiveSaveFlow()=>false;
  void BlockUITransition(){blocks++;}void CancelMobileScroll(){cancels++;}void FinishChestReveal(){}void ReturnToInventory(){panel=Panel.Inventory;}
  CLOSE
  public static void Main(){int n=0;Action<bool,string> check=(ok,why)=>{n++;if(!ok)throw new Exception(why);};
   foreach(int tab in new[]{0,1})foreach(float width in new[]{568,667,799}){
    MobileControls.Active=true;MobileControls.Layout.Width=width;var ui=new GameUI{mobileInventoryTab=tab};
    ui.ClosePanel();check(ui.panel==Panel.Inventory&&!ui.mobileInventoryDetail&&ui.session.Blocked,"first Back must return to equipment list and keep combat blocked");
    check(ui.mobileInventoryListScroll==173&&ui.mobileInventoryDetailScroll==81&&ui.mobileInventoryTab==tab,"Back preserves list tab and scroll anchors");
    check(ui.blocks==1&&ui.cancels==1,"consumed navigation cancels drag and latches initiating release");
    ui.ClosePanel();check(ui.panel==Panel.None&&!ui.session.Blocked,"second Back exits inventory and releases blocking");
    ui.panel=Panel.Inventory;ui.session.Blocked=true;check(!ui.mobileInventoryDetail&&ui.mobileInventoryListScroll==173,"same-character reopen keeps list state and anchor");
   }
   for(int tab=0;tab<3;tab++)foreach(float width in new[]{568,800,1024})foreach(bool detail in new[]{false,true})foreach(bool mobile in new[]{false,true}){
    MobileControls.Active=mobile;MobileControls.Layout.Width=width;var ui=new GameUI{mobileInventoryTab=tab,mobileInventoryDetail=detail};
    bool intercepted=mobile&&width<800&&tab!=2&&detail;ui.ClosePanel();
    check((ui.panel==Panel.Inventory)==intercepted,"only visible compact detail owns Back");check(ui.session.Blocked==intercepted,"wide/supply/list/desktop retain outer close behavior");
   }
   MobileControls.Active=true;MobileControls.Layout.Width=568;
   var sale=new GameUI{presetSaleOpen=true};sale.ClosePanel();
   check(!sale.presetSaleOpen&&sale.panel==Panel.Inventory&&sale.mobileInventoryDetail&&sale.session.Blocked&&sale.blocks==1,"preset cancellation owns Back before inventory detail and retains blocking");
   sale.ClosePanel();check(sale.panel==Panel.Inventory&&!sale.mobileInventoryDetail&&sale.session.Blocked,"next Back returns equipment detail to list");
   var other=new GameUI{panel=Panel.Skills};other.ClosePanel();check(other.panel==Panel.None&&other.blocks==0,"stale inventory flag cannot intercept skill close");
   var paused=new GameUI();paused.session.Paused=true;paused.ClosePanel();check(paused.panel==Panel.None&&paused.session.Paused,"paused overlay retains previous outer behavior");
   Console.WriteLine("PASS: "+n+" production inventory ClosePanel assertions");
  }
 }
}
'''.replace('Time.unscaledTime','UnityEngine.Time.unscaledTime')
dotnet=next((x for x in sys.argv[1:] if not x.startswith('--')),'dotnet')
legacy_only='--legacy' in sys.argv
for legacy in ([True] if legacy_only else [False,True]):
 with tempfile.TemporaryDirectory(prefix='inventory-back-') as tmp:
  source=shell.replace('CLOSE',close.replace(hook,'') if legacy else close).replace('Time.unscaledTime','UnityEngine.Time.unscaledTime')
  out=Path(tmp);p=cv.write_project(out/'project',[root/'Assets/Scripts/UI/GameUI.MobileInventoryNavigation.cs',root/'Assets/Scripts/UI/MobileCollectionLayout.cs',root/'Assets/Scripts/UI/MobilePanelLayout.cs'],program=source)
  config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>');env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli'),DOTNET_NOLOGO='1')
  # A compilation error is never accepted as the expected old-behavior failure.
  subprocess.run([dotnet,'build',str(p),'--configfile',str(config),'-v:q'],env=env,check=True)
  command=[dotnet,str(p.parent/'bin/Debug/net8.0/Validation.dll')]
  if not legacy or legacy_only:
   subprocess.run(command,env=env,check=True)
  else:
   result=subprocess.run(command,env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True)
   expected='Unhandled exception. System.Exception: first Back must return to equipment list and keep combat blocked'
   lines=result.stdout.splitlines()
   if result.returncode==0 or not lines or lines[0]!=expected or any('Exception:' in line for line in lines[1:]):
    raise RuntimeError('Old-entry replay did not fail at the required blocking assertion: exit '+str(result.returncode)+'\n'+result.stdout)
   print('PASS: mandatory legacy negative control compiled, then failed precisely at first-Back combat-blocking assertion')
