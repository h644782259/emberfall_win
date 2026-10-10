"""Run the actual popup priority and function-switch methods with managed UI/session shells."""
from pathlib import Path
import os,sys,tempfile,subprocess
root=Path(__file__).resolve().parents[1]
def member(file,key):
 s=(root/file).read_text();a=s.index(key);b=s.index('{',a)+1;depth=1
 while depth:depth+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
nav='Assets/Scripts/UI/GameUI.FunctionNavigation.cs'
methods='\n'.join(member(nav,key) for key in ['private bool CanSwitchFunction','private bool CloseTopPopup()','private void PrepareFunctionSwitch()','private bool HandleFunctionShortcut()'])
methods+='\n'+member('Assets/Scripts/UI/GameUI.cs','private void TogglePanel(')
code=r"""using System;
namespace UnityEngine{public struct Rect{} public static class Time{public static int frameCount=7;}public enum KeyCode{I,K,M,J,P,O}public static class Input{public static KeyCode Key;public static bool GetKeyDown(KeyCode k)=>k==Key;}}
namespace Emberfall{using UnityEngine;
enum EquipmentMechanic{None,Test}enum HubNpcKind{None,Merchant,Blacksmith}
class Flow{public bool Open,Busy;public void Cancel(){Open=false;}}
class Choices{public bool AwaitingChoice;}
class Session{public bool HasStarted=true,IsDead,BackgroundPaused,PracticeActive,RoomBranchChoiceOpen,DungeonSelectionOpen,ModeFinished,DungeonCleared,FinishedResultDismissed,Paused,Blocked;public Choices RunChoices=new Choices();public void SetUIBlocking(bool v){Blocked=v;}public void SetPaused(bool v){Paused=v;}}
class GameUI{
enum Panel{None,Inventory,Skills,Camp,TravelMap,Controls,Chests}
Session session=new Session();Panel panel=Panel.Inventory;Flow exitRequest=new Flow(),saveFlow=new Flow();string exitError;object pendingSaveDeletion;
bool UITransitionBlocked,HubServicesAvailable=true;
bool mobileBindingEditor,entryRewardPopupVisible,suppressRewardHover,suppressInventoryHover,inventoryComparisonOpen,masteryResetConfirm,presetSaleOpen,chestDetails,smithSocketPicker;
Rect dismissedRewardAnchor,entryRewardAnchor,dismissedInventoryAnchor,inventoryPopupAnchor;
object entryRewardPopup;string entryRewardSelection,entryRewardHoverKey;
int rebindingSlot=-1,bindingDragSource,bindingDragFinger,inventoryPopupDismissed,skillSection;
EquipmentMechanic smithPreviewMechanic,merchantGemSaleConfirmation;
bool merchantShopOpen,smithShopOpen,merchantExchangeOpen,inventoryFashionOpen,mobileInventoryDetail,progressionGoalsOpen,classSwitchOpen;
bool controlsReturnPause,saveReturnPause,bindingReturnPause,travelReturnPause,saveSelectionFromPause;
HubNpcKind inventoryHubNpc;bool MerchantServiceActive=>merchantShopOpen||inventoryHubNpc==HubNpcKind.Merchant;bool SmithServiceActive=>smithShopOpen||inventoryHubNpc==HubNpcKind.Blacksmith;
void BlockUITransition(){}void CancelMobileScroll(){}void CancelHotbarPointer(){}void CancelMobileCast(){}void ResetBuildPlanSurface(){}void ClearReforgeSurface(){}void ResetMobileSkillNavigation(){}
bool CancelSaveDeletion(){if(pendingSaveDeletion==null)return false;pendingSaveDeletion=null;return true;}
bool CancelActiveSaveFlow(){if(!saveFlow.Open||saveFlow.Busy)return false;saveFlow.Cancel();return true;}
void CancelPresetSale(){presetSaleOpen=false;}
void ClosePanel(){if(!CloseTopPopup()){panel=Panel.None;session.SetUIBlocking(false);}}
void CloseTravelMap(){panel=Panel.None;session.SetUIBlocking(false);}
void OpenTravelMap(){PrepareFunctionSwitch();panel=Panel.TravelMap;session.SetUIBlocking(true);}
void OpenProgressionGoals(){PrepareFunctionSwitch();panel=Panel.Camp;progressionGoalsOpen=true;session.SetUIBlocking(true);}
void OpenHubService(HubNpcKind k){if(!HubServicesAvailable)return;PrepareFunctionSwitch();inventoryHubNpc=k;merchantShopOpen=k==HubNpcKind.Merchant;smithShopOpen=k==HubNpcKind.Blacksmith;panel=Panel.Inventory;session.SetUIBlocking(true);}
"""+methods+r"""
static int checks;static void C(bool ok,string msg){checks++;if(!ok)throw new Exception(msg);}
public static void Run(){
var ui=new GameUI{smithPreviewMechanic=EquipmentMechanic.Test,smithSocketPicker=true};ui.session.Blocked=true;
C(ui.CloseTopPopup()&&ui.smithPreviewMechanic==EquipmentMechanic.None&&ui.smithSocketPicker&&ui.panel==Panel.Inventory&&ui.session.Blocked,"first back closes gem preview only");
C(ui.CloseTopPopup()&&!ui.smithSocketPicker&&ui.panel==Panel.Inventory&&ui.session.Blocked,"second back closes picker only");C(!ui.CloseTopPopup(),"no hidden extra leaf");
ui=new GameUI{entryRewardPopupVisible=true,inventoryComparisonOpen=true};C(ui.CloseTopPopup()&&!ui.entryRewardPopupVisible&&ui.inventoryComparisonOpen,"top reward before inventory detail");C(ui.CloseTopPopup()&&!ui.inventoryComparisonOpen&&ui.panel==Panel.Inventory&&ui.suppressInventoryHover,"detail closed without closing bag or reopening hover");
ui=new GameUI{masteryResetConfirm=true,panel=Panel.Skills};C(ui.CloseTopPopup()&&!ui.masteryResetConfirm&&ui.panel==Panel.Skills,"mastery confirmation only");
ui=new GameUI{mobileBindingEditor=true};ui.session.Paused=true;C(ui.CloseTopPopup()&&!ui.mobileBindingEditor&&ui.session.Paused,"binding editor returns to settings");
ui=new GameUI{panel=Panel.Chests,chestDetails=true};C(ui.CloseTopPopup()&&!ui.chestDetails&&ui.panel==Panel.Chests,"chest detail only");
foreach(Panel from in new[]{Panel.Inventory,Panel.Skills,Panel.Camp,Panel.TravelMap,Panel.Controls})foreach(KeyCode key in new[]{KeyCode.I,KeyCode.K,KeyCode.M,KeyCode.J,KeyCode.P,KeyCode.O}){
 ui=new GameUI{panel=from,inventoryComparisonOpen=true,smithSocketPicker=true,entryRewardSelection="merchant:gem"};ui.session.Blocked=true;Input.Key=key;C(ui.HandleFunctionShortcut(),"shortcut accepted from function");
 if((key==KeyCode.I&&from==Panel.Inventory)||(key==KeyCode.K&&from==Panel.Skills)||(key==KeyCode.M&&from==Panel.TravelMap))continue;
 C(!ui.inventoryComparisonOpen&&!ui.smithSocketPicker&&ui.entryRewardSelection==null&&ui.session.Blocked,"switch resets nested UI, keeps gameplay blocked");
 C(key==KeyCode.I?ui.panel==Panel.Inventory:key==KeyCode.K?ui.panel==Panel.Skills:key==KeyCode.M?ui.panel==Panel.TravelMap:key==KeyCode.J?ui.progressionGoalsOpen:key==KeyCode.P?ui.MerchantServiceActive:ui.SmithServiceActive,"correct destination");}
ui=new GameUI{panel=Panel.Controls};ui.session.Paused=true;Input.Key=KeyCode.K;C(ui.HandleFunctionShortcut()&&ui.panel==Panel.Skills&&!ui.session.Paused&&ui.session.Blocked,"settings to skills");
ui.session.IsDead=true;Input.Key=KeyCode.I;C(!ui.HandleFunctionShortcut(),"death cannot bypass");ui.session.IsDead=false;ui.session.RunChoices.AwaitingChoice=true;C(!ui.HandleFunctionShortcut(),"mandatory choice cannot bypass");
Console.WriteLine("PASS "+checks+" production popup priority and function-switch assertions");}}
class Program{static void Main(){GameUI.Run();}}}
"""
with tempfile.TemporaryDirectory(prefix='ui-navigation-') as d:
 p=Path(d);(p/'Program.cs').write_text(code);(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>');(p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>')
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else os.environ.get('DOTNET','dotnet'),'run','--project',str(p/'Test.csproj'),'-c','Release'],check=True)
