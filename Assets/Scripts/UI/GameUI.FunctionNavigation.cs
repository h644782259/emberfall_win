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
            ReleaseFashionSmithPreview();
            smithPreviewMechanic=EquipmentMechanic.None;smithSocketPicker=false;masteryResetConfirm=false;
            merchantGemSaleConfirmation=EquipmentMechanic.None;
            merchantExchangeOpen=false;inventoryHubNpc=HubNpcKind.None;
            progressionGoalsOpen=false;classSwitchOpen=false;ResetMobileSkillNavigation();
            controlsReturnPause=saveReturnPause=bindingReturnPause=travelReturnPause=false;saveSelectionFromPause=false;
            panel=Panel.None;session.SetUIBlocking(false);session.SetPaused(false);
        }
        private bool HandleFunctionShortcut()
        {
            if(!CanSwitchFunction||MobileControls.Active&&UITransitionBlocked||rebindingSlot>=0)return false;
            if(Input.GetKeyDown(KeyCode.I)){TogglePanel(Panel.Inventory);return true;}
            if(Input.GetKeyDown(KeyCode.K)){TogglePanel(Panel.Skills);return true;}
            if(Input.GetKeyDown(KeyCode.M)){if(panel==Panel.TravelMap)CloseTravelMap();else OpenTravelMap();return true;}
            if(Input.GetKeyDown(KeyCode.J)){if(progressionGoalsOpen)ClosePanel();else OpenProgressionGoals();return true;}
            if(Input.GetKeyDown(KeyCode.P)||Input.GetKeyDown(KeyCode.O))
            {bool merchant=Input.GetKeyDown(KeyCode.P);if(merchant?MerchantServiceActive:SmithServiceActive)ClosePanel();else OpenHubService(merchant?HubNpcKind.Merchant:HubNpcKind.Blacksmith);return true;}
            return false;
        }
    }
}
