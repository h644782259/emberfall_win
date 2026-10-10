using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private bool CanSwitchFunction
        {
            get{return session!=null&&session.HasStarted&&!session.IsDead&&!session.BackgroundPaused&&!session.PracticeActive&&
                !exitRequest.Open&&!saveFlow.Busy&&!session.RunChoices.AwaitingChoice&&!session.RoomBranchChoiceOpen&&
                !session.DungeonSelectionOpen&&!((session.ModeFinished||session.DungeonCleared)&&!session.FinishedResultDismissed);}
        }
        private Rect dismissedRewardAnchor,dismissedInventoryAnchor;
        private bool suppressRewardHover,suppressInventoryHover;
        private bool CloseTopPopup()
        {
            if(exitRequest.Open){exitRequest.Cancel();exitError=null;BlockUITransition();return true;}
            if(CancelSaveDeletion()||CancelActiveSaveFlow())return true;
            if(rebindingSlot>=0){rebindingSlot=-1;BlockUITransition();return true;}
            if(mobileBindingEditor){mobileBindingEditor=false;bindingDragSource=-1;bindingDragFinger=-1000;BlockUITransition();return true;}
            if(entryRewardPopupVisible||entryRewardSelection!=null)
            {dismissedRewardAnchor=entryRewardAnchor;suppressRewardHover=true;entryRewardPopup=null;entryRewardSelection=entryRewardHoverKey=null;entryRewardPopupVisible=false;BlockUITransition();return true;}
            if(panel==Panel.Inventory&&smithPreviewMechanic!=EquipmentMechanic.None){smithPreviewMechanic=EquipmentMechanic.None;BlockUITransition();return true;}
            if(panel==Panel.Inventory&&smithSocketPicker){smithSocketPicker=false;BlockUITransition();return true;}
            if(inventoryComparisonOpen&&panel==Panel.Inventory)
            {dismissedInventoryAnchor=inventoryPopupAnchor;suppressInventoryHover=true;inventoryComparisonOpen=false;inventoryPopupDismissed=Time.frameCount;CancelMobileScroll();BlockUITransition();return true;}
            if(masteryResetConfirm){masteryResetConfirm=false;BlockUITransition();return true;}
            if(merchantGemSaleConfirmation!=EquipmentMechanic.None){merchantGemSaleConfirmation=EquipmentMechanic.None;BlockUITransition();return true;}
            if(presetSaleOpen){CancelPresetSale();BlockUITransition();return true;}
            if(panel==Panel.Chests&&chestDetails){chestDetails=false;BlockUITransition();return true;}
            return false;
        }
        private void PrepareFunctionSwitch()
        {
            CancelHotbarPointer();CancelMobileCast();CancelMobileScroll();ResetBuildPlanSurface();ClearReforgeSurface();
            if(saveFlow.Open&&!saveFlow.Busy)saveFlow.Cancel();
            pendingSaveDeletion=null;rebindingSlot=-1;mobileBindingEditor=false;bindingDragSource=-1;bindingDragFinger=-1000;
            entryRewardPopup=null;entryRewardSelection=entryRewardHoverKey=null;entryRewardPopupVisible=false;
            inventoryComparisonOpen=false;inventoryFashionOpen=false;mobileInventoryDetail=false;
            smithPreviewMechanic=EquipmentMechanic.None;smithSocketPicker=false;masteryResetConfirm=false;
            merchantGemSaleConfirmation=EquipmentMechanic.None;if(presetSaleOpen)CancelPresetSale();
            merchantExchangeOpen=false;inventoryHubNpc=HubNpcKind.None;
            progressionGoalsOpen=false;classSwitchOpen=false;ResetMobileSkillNavigation();
            controlsReturnPause=saveReturnPause=bindingReturnPause=travelReturnPause=false;saveSelectionFromPause=false;
            panel=Panel.None;session.SetUIBlocking(false);session.SetPaused(false);
        }
        private bool HandleFunctionShortcut()
        {
            if(!CanSwitchFunction||UITransitionBlocked||rebindingSlot>=0)return false;
            if(Input.GetKeyDown(KeyCode.I)){TogglePanel(Panel.Inventory);return true;}
            if(Input.GetKeyDown(KeyCode.K)){TogglePanel(Panel.Skills);return true;}
            if(Input.GetKeyDown(KeyCode.M)){if(panel==Panel.TravelMap)CloseTravelMap();else OpenTravelMap();return true;}
            if(Input.GetKeyDown(KeyCode.J)){if(progressionGoalsOpen)ClosePanel();else OpenProgressionGoals();return true;}
            if(Input.GetKeyDown(KeyCode.P)||Input.GetKeyDown(KeyCode.O))
            {bool merchant=Input.GetKeyDown(KeyCode.P);if(merchant?MerchantServiceActive:SmithServiceActive)ClosePanel();else OpenHubService(merchant?HubNpcKind.Merchant:HubNpcKind.Blacksmith);return true;}
            return false;
        }
        private void DrawFunctionSwitcher(float fullHeight,float reserved)
        {
            bool prior=GUI.enabled;GUI.enabled=prior&&CanSwitchFunction&&!UITransitionBlocked;
            float u=MobileControls.Active?TouchRatio:1,buttonWidth=Mathf.Min(88*u,(width-24*u)/7),total=buttonWidth*7;
            Rect bar=new Rect((width-total)*.5f,fullHeight-reserved+4*u,total,reserved-8*u);blockedRects.Add(bar);Fill(bar,ink);Border(bar,jade*.4f);
            string[] labels={"行囊","技能","成就","商人","铁匠","地图","设置"};
            string[] keys={"I","K","J","P","O","M",""};
            for(int i=0;i<labels.Length;i++)
            {
                Rect hit=new Rect(bar.x+i*buttonWidth+2*u,bar.y+2*u,buttonWidth-4*u,bar.height-4*u);
                bool selected=i==0&&panel==Panel.Inventory&&!MerchantServiceActive&&!SmithServiceActive||i==1&&panel==Panel.Skills||i==2&&progressionGoalsOpen||i==3&&MerchantServiceActive||i==4&&SmithServiceActive||i==5&&panel==Panel.TravelMap||i==6&&session.Paused;
                if(i==3||i==4)GUI.enabled=prior&&CanSwitchFunction&&HubServicesAvailable&&!UITransitionBlocked;
                if(Button(hit,labels[i]+(!MobileControls.Active&&keys[i].Length>0?" ["+keys[i]+"]":""),selected?gold:jade))
                {
                    if(i==0)TogglePanel(Panel.Inventory);else if(i==1)TogglePanel(Panel.Skills);else if(i==2)OpenProgressionGoals();
                    else if(i==3||i==4)OpenHubService(i==3?HubNpcKind.Merchant:HubNpcKind.Blacksmith);else if(i==5)OpenTravelMap();
                    else{PrepareFunctionSwitch();mobilePausePage=0;session.SetPaused(true);BlockUITransition();}
                }
                GUI.enabled=prior&&CanSwitchFunction&&!UITransitionBlocked;
            }
            GUI.enabled=prior;
        }
    }
}
