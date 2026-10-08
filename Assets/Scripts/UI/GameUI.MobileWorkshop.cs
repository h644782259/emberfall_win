using System;
using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private string mobileWorkshopStatus;
        private bool mobileWorkshopFailed;

        private void MobileWorkshopParagraph(ref float y, float width, string text, Color color, bool draw, bool bold = false, int size = 14)
        {
            y += draw ? DrawMobileParagraph(8, y, width - 16, text, size, color, bold) : MeasureMobileParagraph(text, width - 16, size, bold);
            y += 10;
        }

        private void MobileWorkshopAction(ref float y, float width, string caption, Color color, bool enabled, bool draw, Action action, ButtonRole role = ButtonRole.Action)
        {
            float actionWidth=Mathf.Min(width-16,Mathf.Max(88,Style(TouchFont(12),false).CalcSize(new GUIContent(caption)).x/TouchRatio+24));
            if (draw && QuietAction(TouchRect(8,y,actionWidth,48),caption,enabled)) action();
            y += 58;
        }

        private void MobileWorkshopResult(bool accepted, string message)
        {
            mobileWorkshopFailed = !accepted || !string.IsNullOrEmpty(session.Progression.LastError);
            mobileWorkshopStatus = mobileWorkshopFailed ? (string.IsNullOrEmpty(session.Progression.LastError) ? "当前操作未完成，请重试。" : session.Progression.LastError) : message;
            CancelMobileScroll();
            // The fixed header carries feedback; preserve the current reading/action position.
            Feedback(!mobileWorkshopFailed, mobileWorkshopStatus);
            BlockUITransition();
        }

        private void DrawMobileWorkshopAbilities(ref float y, float width, bool draw)
        {
            var p = session.Progression;
            MobileWorkshopAction(ref y, width, "营地 · 切换职业", jade, true, draw, OpenClassSwitch, ButtonRole.Navigation);
            MobileWorkshopParagraph(ref y, width, "配装方案与免费重置", gold, draw, true, 16);
            MobileWorkshopParagraph(ref y, width, "可返还技能进阶 " + p.RefundableSkillRanks + "点 + 精通 " + p.RefundableMasteryPoints + "点 = " + p.RefundableBuildPoints + "点；保留已学1阶与当前装备。", muted, draw);
            MobileWorkshopAction(ref y, width, "配装方案 A / B · 记录 / 应用", jade, true, draw, OpenBuildPlans, ButtonRole.Navigation);
            MobileWorkshopAction(ref y, width, "免费重置配点 · " + p.RefundableBuildPoints + "点", gold, session.IsInCamp && (p.RefundableBuildPoints > 0 || p.Profile.masteryCore >= 0), draw, () => RequestBuildPlanAction(BuildPlanAction.Reset), ButtonRole.Danger);
            MobileWorkshopParagraph(ref y, width, GameBalance.ClassName(p.Profile.heroClass) + " · 职业能力", gold, draw, true, 16);
            MobileWorkshopParagraph(ref y, width, BuildCatalog.ClassSignatureDescription(p.Profile.heroClass), pale, draw);
            for(int i=0;i<2;i++)
            {
                var info=CampRouteCards.Describe(p.Profile,true,i);
                MobileWorkshopParagraph(ref y,width,info.Name,gold,draw,true,17);
                MobileWorkshopParagraph(ref y,width,info.Loop,pale,draw);
                MobileWorkshopParagraph(ref y,width,info.Requirements,info.Ready?jade:muted,draw);
                MobileWorkshopParagraph(ref y,width,info.Enhancement,muted,draw);
                MobileWorkshopParagraph(ref y,width,"下一步："+info.NextStep,jade,draw);
                int route=i;
                if(info.NextAction!=CampRouteAction.None)MobileWorkshopAction(ref y,width,info.NextStep,gold,session.IsInCamp,draw,()=>FollowCampRouteStep(info,route),CampRouteButtonRole(info));
            }
            if(p.Profile.heroClass==HeroClass.Arcanist)
                MobileWorkshopAction(ref y,width,"恢复均衡专精",jade,session.IsInCamp&&p.Profile.specialization!=ElementalistSpecialization.None,draw,
                    ()=>MobileWorkshopResult(p.SetSpecialization(ElementalistSpecialization.None,session.IsInCamp),"已恢复均衡专精"));
            MobileWorkshopParagraph(ref y, width, "精通与技能共用点数；"+MasteryProgressionRules.TierSummary+"；"+MasteryProgressionRules.CoreSummary, jade, draw);
            for (int i = 0; i < 4; i++)
            {
                var mastery = (MasteryType)i;
                MobileWorkshopParagraph(ref y, width, BuildCatalog.MasteryName(mastery) + " · " + p.Profile.masteryRanks[i] + "/" + ProgressionService.MasteryCap(p.Profile.level), pale, draw, true, 16);
                MobileWorkshopParagraph(ref y, width, BuildCatalog.MasteryDescription(mastery), muted, draw);
                string reason = p.MasteryLockReason(mastery);
                if (!string.IsNullOrEmpty(reason)) MobileWorkshopParagraph(ref y, width, reason, gold, draw);
                MobileWorkshopAction(ref y, width, "投入 1 技能点", jade, string.IsNullOrEmpty(reason), draw,
                    () => MobileWorkshopResult(p.LearnMastery(mastery), "精通已提高"));
                string core = p.HasMasteryCore(mastery) ? (p.MasteryCoreTier(mastery) == 2 ? "增强核心 · 已启用" : "初阶核心 · 已启用") :
                    (p.Profile.masteryRanks[i] >= MasteryCoreRules.EnhancedInvestment ? "切换增强核心" : "启用初阶核心 · 需"+MasteryCoreRules.InitialInvestment+"点");
                MobileWorkshopAction(ref y, width, core, gold, session.IsInCamp && p.Profile.masteryRanks[i] >= MasteryCoreRules.InitialInvestment && !p.HasMasteryCore(mastery), draw, () => MobileWorkshopResult(p.SelectMasteryCore(mastery, session.IsInCamp), "唯一精通核心已切换"), ButtonRole.Primary);
            }
            MobileWorkshopParagraph(ref y, width, "免费退还技能2/3阶投入；保留已学1阶、前置和快捷栏，不重置当前冷却。精通重置会退还精通点并关闭核心。", muted, draw);
            MobileWorkshopAction(ref y, width, "退还技能进阶 · " + p.RefundableSkillRanks + "点", gold, session.IsInCamp && p.RefundableSkillRanks > 0, draw, () => MobileWorkshopResult(p.RefundSkillRanks(session.IsInCamp), "技能进阶点已返还；已学1阶保留"), ButtonRole.Danger);
            bool invested = p.Profile.masteryCore >= 0;
            foreach (int rank in p.Profile.masteryRanks) invested |= rank > 0;
            MobileWorkshopAction(ref y, width, "免费重置精通 / 核心", jade, session.IsInCamp && invested, draw, () => MobileWorkshopResult(p.ResetMastery(session.IsInCamp), "精通点已返还，核心已关闭"), ButtonRole.Danger);
        }

    }
}
