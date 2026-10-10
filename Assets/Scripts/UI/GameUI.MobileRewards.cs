using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Vector2 mobileFashionScroll, mobileChestScroll, mobileChestArtScroll;
        private string mobileChestError, mobileChestOdds;
        private string mobileFashionStatus;
        private bool mobileFashionFailed;

        private void DrawMobileFashion()
        {
            panel=Panel.Inventory;inventoryFashionOpen=true;DrawMobileInventory();
        }

        private void MobileFashionResult(bool accepted, string message)
        {
            mobileFashionFailed = !accepted;
            mobileFashionStatus = accepted ? message : string.IsNullOrEmpty(session.Progression.LastError) ? "操作未保存，请重试。" : session.Progression.LastError;
            if (!accepted) { CancelMobileScroll(); mobileFashionScroll = Vector2.zero; }
            Feedback(accepted, mobileFashionStatus);
        }

        private float MobileFashionRowHeight(FashionSlot slot, Rarity rarity, float width)
        {
            return Mathf.Max(120, 16 + MeasureMobileParagraph(ProgressionService.FashionName(slot, rarity), width, 16, true) +
                MeasureMobileParagraph(GameBalance.RarityName(rarity) + " · " + ProgressionService.FashionBonus(slot, rarity), width, 14) +
                MeasureMobileParagraph("未收藏 · 来自通关宝箱", width, 14));
        }

        private void DrawMobileChests()
        {
            var progression = session.Progression;
            var savedReward = progression.LastChestReward;
            // Switching profiles or reloading a persisted receipt must restore the
            // saved result, never replay the random grant or keep another slot's UI.
            if (progression.Profile.pendingChestReveal && savedReward != null && chestReceiptId != savedReward.Id)
            { ResetChestReveal(); mobileChestError = null; mobileChestScroll = mobileChestArtScroll = Vector2.zero; }
            if (!progression.Profile.pendingChestReveal && chestRevealResult != null)
            { ResetChestReveal(); mobileChestError = null; mobileChestScroll = mobileChestArtScroll = Vector2.zero; }
            bool revealed = chestRevealResult != null && progression.Profile.pendingChestReveal;
            bool complete = revealed && ChestAnimationDone;
            var reward = revealed ? savedReward : null;
            Color accent = reward != null && reward.Rarity.HasValue ? GameBalance.RarityColor(reward.Rarity.Value) : gold;
            if (revealed && complete && !rewardSoundPlayed)
            {
                rewardSoundPlayed = true;
                GameAudio.Play(reward == null || !reward.Rarity.HasValue || reward.Duplicate ? SoundCue.UI : reward.Rarity.Value == Rarity.Legendary ? SoundCue.Victory : reward.Rarity.Value == Rarity.Epic ? SoundCue.LevelUp : reward.Rarity.Value == Rarity.Rare ? SoundCue.Loot : SoundCue.Cast);
            }
            var layout = revealed||!string.IsNullOrEmpty(progression.LastError)?MobilePanelGeometry():new MobilePanelLayout(MobileControls.Layout.Width,MobileControls.Layout.Height,false);
            string title = revealed ? complete ? "宝箱奖励" : "开启宝箱" : "遗迹馈赠";
            string subtitle = revealed && !complete ? "正在开启宝箱" : "";
            if (DrawMobilePanelChrome(layout, title, subtitle)) return;
            if (revealed) DrawMobileChestResult(layout, reward, accent, complete);
            else if (DrawMobileChestChoices(layout)) return;

            if(!revealed&&!string.IsNullOrEmpty(progression.LastError)&&Button(MobilePanelRect(layout.FooterButton(0,1)),"商人 · 整理容量 / 资源",jade)){OpenChestRecoveryService();return;}
            if (revealed && complete && Button(MobilePanelRect(layout.FooterButton(0,1)),"收下",gold))
            {
                {
                    FinishChestReveal();
                    mobileChestError = panel == Panel.Chests ? progression.LastError : null;
                    if (panel == Panel.Chests) { CancelMobileScroll(); mobileChestScroll = Vector2.zero; }
                }
                BlockUITransition();
            }
        }

        private bool DrawMobileChestChoices(MobilePanelLayout layout)
        {
            float bodyWidth=layout.Body.Width-18;
            float errors=string.IsNullOrEmpty(mobileChestError)?0:MeasureMobileParagraph(mobileChestError,bodyWidth-16,14)+12;
            float cardHeight=Mathf.Max(230,layout.Body.Height-errors-8);
            mobileChestScroll=BeginTouchScroll("mobile-chest-choice",MobilePanelRect(layout.Body),mobileChestScroll,new Rect(0,0,bodyWidth*TouchRatio,(errors+cardHeight+8)*TouchRatio));
            if(errors>0)DrawMobileParagraph(8,0,bodyWidth-16,mobileChestError,14,new Color(1,.55f,.45f));
            int selected=-1,locked=session.Progression.SelectedRewardChest;float cell=(bodyWidth-24)/3;
            for(int choice=0;choice<3;choice++)
            {
                Rect cardRect=TouchRect(4+choice*(cell+8),errors,cell,cardHeight);DrawSingleChestCard(cardRect,TouchRatio);
                if(ChestOpenButton(InteractiveChestArt(cardRect,TouchRatio),TouchRatio,!chestOpening&&(locked<0||locked==choice)&&session.Progression.Profile.pendingFashionChest))selected=choice;
            }
            EndTouchScroll();if(selected<0)return false;
            chestOpening=true;string result;
            try{result=session.Progression.OpenChosenDungeonChest(selected);}finally{chestOpening=false;}
            if(result==null){chestOpening=false;mobileChestError=session.Progression.LastError;mobileChestScroll=Vector2.zero;Feedback(false,"宝箱暂时无法开启");}
            else
            {
                chestRevealOrigin=InteractiveChestArt(TouchRect(layout.Body.X+4+selected*(cell+8),layout.Body.Y+errors-mobileChestScroll.y/TouchRatio,cell,cardHeight),TouchRatio);
                revealedChest=selected;chestRevealResult=result;chestRevealedAt=Time.unscaledTime;chestDetails=false;rewardSoundPlayed=false;
                chestReceiptId=session.Progression.LastChestReward.Id;mobileChestError=null;mobileChestScroll=mobileChestArtScroll=Vector2.zero;GameAudio.Play(SoundCue.Cast);
            }
            BlockUITransition();return true;
        }

        private void DrawMobileChestResult(MobilePanelLayout layout, ChestReward reward, Color accent, bool complete)
        {
            DrawDesktopChestResult(MobilePanelRect(layout.Body),reward,accent);
        }

        private void DrawMobileChestDetails(MobilePanelLayout layout)
        {
            int minimum = TierRewardRules.ChestGoldMinimum(session.Progression.Profile.pendingChestTier);
            string details = session.Progression.ActiveDungeonChestRules;
            float width = layout.Body.Width - 34;
            float total = MeasureMobileParagraph(details, width, 15) + 24;
            mobileChestScroll = BeginTouchScroll("mobile-chest-details", MobilePanelRect(layout.Body), mobileChestScroll,
                new Rect(0, 0, (width + 16) * TouchRatio, Mathf.Max(layout.Body.Height, total) * TouchRatio));
            DrawMobileParagraph(8, 8, width, details, 15, pale);
            EndTouchScroll();
        }

        private string MobileChestOddsText
        {
            get
            {
                if (mobileChestOdds != null) return mobileChestOdds;
                // Read the deterministic production table, never roll a reward.
                int[] counts = new int[4]; int empty = 0;
                for (int roll = 0; roll < 100; roll++) { Rarity? rarity = ProgressionService.RollFashionRarity(roll); if (rarity.HasValue) counts[(int)rarity.Value]++; else empty++; }
                mobileChestOdds = "普通 " + counts[0] + "% · 稀有 " + counts[1] + "%\n史诗 " + counts[2] + "% · 传说 " + counts[3] + "% · 无时装 " + empty + "%";
                return mobileChestOdds;
            }
        }
    }
}
