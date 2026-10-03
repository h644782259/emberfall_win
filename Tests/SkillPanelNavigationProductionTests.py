"""Execute production ClosePanel/TogglePanel with session shells, not a Unity UI test."""
from pathlib import Path
import importlib.util,tempfile,os,subprocess,sys
root=Path(__file__).resolve().parents[1]
def member(file,signature):
 s=(root/'Assets/Scripts/UI'/file).read_text();a=s.index(signature);b=s.index('{',a)+1;depth=1
 while depth:
  depth+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
methods='\n'.join(member(f,s) for f,s in [('GameUI.BuildPlans.cs','private void CancelPresetSale()'),('GameUI.cs','private void ClosePanel()'),('GameUI.cs','private void TogglePanel('),('GameUI.MobileSkillNavigation.cs','private bool CloseMobileSkillDetail()'),('GameUI.MobileSkillNavigation.cs','private void ResetMobileSkillNavigation()')])
shell=r'''
using System;
namespace UnityEngine {public static class Time{public static float unscaledTime;}}
namespace Emberfall {
public static class MobileControls{public static bool Active=true;public static LayoutState Layout=new LayoutState();public class LayoutState{public float Width;}}
public class Session{public bool HasStarted=true,IsDead,Paused,Blocked=true;public Progression Progression=new Progression();public void SetUIBlocking(bool b){Blocked=b;}public void SetPaused(bool b){Paused=b;}}
public class Progression{public Profile Profile=new Profile();}public class Profile{public bool pendingFashionChest,pendingChestReveal;}
public partial class GameUI {
enum Panel{None,Skills,Inventory,SaveSelection,Chests,Fashion,PotionAssignment,Bindings,SaveLocation,Controls}
Panel panel=Panel.Skills,bindingReturnPanel;Session session=new Session();bool presetSaleOpen;bool mobileSkillDetail=true,saveSelectionFromPause,chestDetails,bindingReturnPause,saveReturnPause,controlsReturnPause;int rebindingSlot,blocks,cancels;float chestRevealedAt,ChestDuration=1;bool ChestAnimationDone=true;float listScroll=173,detailScroll=81;
object routeSkillOwner;string routeSkillSlot;bool CloseMobileInventoryDetail()=>false;bool CloseChapterSelection()=>false;bool CloseRouteSkill()=>false;bool CloseProgressionGoalSurface()=>false;bool CloseBuildPlanSurface()=>false;bool CloseTravelMap()=>false;bool CancelSaveDeletion()=>false;bool CancelActiveSaveFlow()=>false;
void CancelMobileScroll(){cancels++;}void BlockUITransition(){blocks++;}void FinishChestReveal(){}void ReturnToInventory(){}
METHODS
public static int Verify(){int n=0;Action<bool,string> check=(b,w)=>{n++;if(!b)throw new Exception(w);};
foreach(float width in new[]{568,667,799,800,1024}){
MobileControls.Layout.Width=width;var ui=new GameUI();ui.ClosePanel();
check(ui.panel==(width<800?Panel.Skills:Panel.None),"first Back routes narrow detail to list, wide tree exits");
check(ui.session.Blocked==(width<800),"detail Back keeps gameplay blocked");
check(ui.listScroll==173&&ui.detailScroll==81,"Back preserves scroll positions");
ui.ClosePanel();check(ui.panel==Panel.None&&!ui.session.Blocked,"second Back exits list and releases gameplay");
ui.mobileSkillDetail=true;ui.TogglePanel(Panel.Skills);check(ui.panel==Panel.Skills&&!ui.mobileSkillDetail&&ui.session.Blocked&&ui.listScroll==173,"reopen enters list retaining scroll");
ui.TogglePanel(Panel.Skills);check(ui.panel==Panel.None&&!ui.session.Blocked,"toggle closes skills");}
MobileControls.Layout.Width=568;var sale=new GameUI{presetSaleOpen=true};sale.ClosePanel();
check(!sale.presetSaleOpen&&sale.panel==Panel.Skills&&sale.mobileSkillDetail&&sale.session.Blocked&&sale.blocks==1,"actual preset cancellation consumes first Back without closing skill detail");
sale.ClosePanel();check(sale.panel==Panel.Skills&&!sale.mobileSkillDetail&&sale.session.Blocked,"next Back returns narrow skill detail to list");
MobileControls.Active=false;var desktop=new GameUI();desktop.ClosePanel();check(desktop.panel==Panel.None&&!desktop.session.Blocked,"desktop ClosePanel unaffected");
return n;}
}}
class Program{static void Main(){Console.WriteLine("PASS: "+Emberfall.GameUI.Verify()+" real ClosePanel/TogglePanel skill navigation checks");}}
'''.replace('METHODS',methods).replace('using System;','using System;using UnityEngine;')
spec=importlib.util.spec_from_file_location('cv',root/'Tools/cloud-validation.py');cv=importlib.util.module_from_spec(spec);spec.loader.exec_module(cv)
with tempfile.TemporaryDirectory(prefix='skill-panel-navigation-') as t:
 out=Path(t);(out/'Replay.cs').write_text(shell)
 project=cv.write_project(out/'project',[out/'Replay.cs',root/'Assets/Scripts/UI/SkillIconPresentation.cs'],program='')
 project.write_text(project.read_text().replace('<OutputType>Library</OutputType>','<OutputType>Exe</OutputType>'))
 config=out/'NuGet.Config';config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 env=dict(os.environ,DOTNET_CLI_HOME=str(out/'cli'),DOTNET_NOLOGO='1');dotnet=sys.argv[1] if len(sys.argv)>1 else 'dotnet'
 subprocess.run([dotnet,'build',str(project),'--configfile',str(config),'-v:q'],check=True,env=env)
 subprocess.run([dotnet,str(project.parent/'bin/Debug/net8.0/Validation.dll')],check=True,env=env)
