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
            var layout = MobilePanelGeometry();
            string title = chestDetails ? "奖励规则" : revealed ? complete ? "宝箱奖励" : "开启宝箱" : "遗迹馈赠";
            string subtitle = chestDetails ? (session.Progression.Profile.pendingChestReveal && session.Progression.LastChestReward != null && session.Progression.LastChestReward.rulesRevision == 0 ? "旧版奖励 · 新开箱规则见下" : "单宝箱 · 金币 / 碎片 / 星纹") : revealed ? complete ? ChestRevealPresentation.Outcome(reward) : "正在揭晓已保存的奖励" : "直接开启 · 奖励先保存";
            if (DrawMobilePanelChrome(layout, title, subtitle, true, true)) return;
            if (chestDetails) DrawMobileChestDetails(layout);
            else if (revealed) DrawMobileChestResult(layout, reward, accent, complete);
            else if (DrawMobileChestChoices(layout)) return;

            bool firstTrial=complete&&!chestDetails&&CanTrialChestReward(reward);int footerCount=firstTrial?3:2;
            if(firstTrial&&PrimaryButton(MobilePanelRect(layout.FooterButton(1,3)), "收下并查看时装", jade)){AcceptChestForTrial();return;}
            if (NavigationButton(MobilePanelRect(layout.FooterButton(0, footerCount)), chestDetails ? "返回宝箱" : "概率 / 规则", jade))
            { chestDetails = !chestDetails; mobileChestScroll = Vector2.zero; BlockUITransition(); return; }
            if (DrawButton(MobilePanelRect(layout.FooterButton(footerCount-1, footerCount)), revealed ? complete ? "收下" : "跳过动画" : "返回", revealed ? ButtonRole.Primary : ButtonRole.Navigation, !chestDetails))
            {
                if(!revealed){ClosePanel();BlockUITransition();return;}
                if (!complete) chestRevealedAt = Time.unscaledTime - ChestDuration;
                else
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
            float disclosure=MeasureMobileParagraph(ChestRevealPresentation.ChoiceDisclosure,bodyWidth-16,14)+12;
            float errors=string.IsNullOrEmpty(mobileChestError)?0:MeasureMobileParagraph(mobileChestError,bodyWidth-16,14)+12;
            float cardHeight=Mathf.Max(230,layout.Body.Height-disclosure-errors-8);
            mobileChestScroll=BeginTouchScroll("mobile-chest-choice",MobilePanelRect(layout.Body),mobileChestScroll,new Rect(0,0,bodyWidth*TouchRatio,(disclosure+errors+cardHeight+8)*TouchRatio));
            DrawMobileParagraph(8,0,bodyWidth-16,ChestRevealPresentation.ChoiceDisclosure,14,pale);
            if(errors>0)DrawMobileParagraph(8,disclosure,bodyWidth-16,mobileChestError,14,new Color(1,.55f,.45f));
            Rect cardRect=TouchRect(8,disclosure+errors,bodyWidth-16,cardHeight);
            DrawSingleChestCard(cardRect,TouchRatio);
            bool open=PrimaryButton(TouchRect(20,disclosure+errors+cardHeight-60,bodyWidth-40,48), session.Progression.ChestOpenCaption, gold, !chestOpening&&session.Progression.Profile.pendingFashionChest&&!session.Progression.Profile.pendingChestReveal);
            EndTouchScroll();if(!open)return false;
            chestOpening=true;string result=session.Progression.OpenDungeonChest();
            if(result==null){chestOpening=false;mobileChestError=session.Progression.LastError;mobileChestScroll=Vector2.zero;Feedback(false,"宝箱暂时无法开启");}
            else
            {
                chestRevealOrigin=ChestChoiceArt(TouchRect(layout.Body.X+8,layout.Body.Y+disclosure+errors-mobileChestScroll.y/TouchRatio,bodyWidth-16,cardHeight),TouchRatio);
                revealedChest=0;chestRevealResult=result;chestRevealedAt=Time.unscaledTime;chestDetails=false;rewardSoundPlayed=false;
                chestReceiptId=session.Progression.LastChestReward.Id;mobileChestError=null;mobileChestScroll=mobileChestArtScroll=Vector2.zero;GameAudio.Play(SoundCue.Cast);
            }
            BlockUITransition();return true;
        }

        private void DrawMobileChestResult(MobilePanelLayout layout, ChestReward reward, Color accent, bool complete)
        {
            float progress = ChestRevealPresentation.Progress(Time.unscaledTime - chestRevealedAt,ChestDuration);
            if(!complete){DrawChestRevealTransition(MobilePanelRect(layout.Body),reward,TouchRect(layout.BodyLeft.X+8,layout.BodyLeft.Y+8,layout.BodyLeft.Width-16,layout.BodyLeft.Height-16));return;}
            var art = layout.BodyLeft;
            Fill(MobilePanelRect(art), new Color(.055f, .08f, .11f)); Border(MobilePanelRect(art), accent);
            float artHeight=Mathf.Max(art.Height-16,reward!=null&&reward.Rarity.HasValue?300:144);
            mobileChestArtScroll=BeginTouchScroll("mobile-chest-art",MobilePanelRect(art),mobileChestArtScroll,new Rect(0,0,(art.Width-8)*TouchRatio,(artHeight+16)*TouchRatio));
            DrawChestCommittedReward(TouchRect(8,8,art.Width-24,artHeight),reward,accent);
            EndTouchScroll();
            if (progress > .35f)
            {
                Rect clip = TouchRect(art.X + 8, art.Y + 8, art.Width - 16, art.Height - 16);
                GUI.BeginGroup(clip);
                DrawRewardRadiance(new Rect(0, 0, clip.width, clip.height), accent, progress);
                GUI.EndGroup();
            }
            string result = ChestRevealPresentation.ResultWithCollection(reward,session.Progression.Profile);
            var viewport = layout.BodyRight;
            float width = viewport.Width - 34;
            string error = string.IsNullOrEmpty(mobileChestError) ? session.Progression.LastError : mobileChestError;
            float total = ChestSectionsHeight(reward,width*TouchRatio,TouchRatio)/TouchRatio + MeasureMobileParagraph(error, width, 14)+24;
            mobileChestScroll = BeginTouchScroll("mobile-chest-result", MobilePanelRect(viewport), mobileChestScroll,
                new Rect(0, 0, (width + 16) * TouchRatio, Mathf.Max(viewport.Height, total) * TouchRatio));
            float y = 8;
            if (!string.IsNullOrEmpty(error)) y += DrawMobileParagraph(8, y, width, error, 14, new Color(1, .55f, .45f)) + 8;
            DrawChestSections(TouchRect(8,y,width,total-y),reward,accent,TouchRatio);
            EndTouchScroll();
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
