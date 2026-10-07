using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Vector2 mobileSkillListScroll, mobileSkillDetailScroll;
        private bool mobileSkillDetail;
        private string mobileSkillsSlot;
        private ProgressionService mobileSkillsService;
        private string mobileSkillStatus;
        private bool mobileSkillStatusFailed;

        private void ReconcileMobileSkillOwner()
        {
            var progression=session.Progression;
            if (mobileSkillsService != progression || mobileSkillsSlot != progression.CurrentSlotId)
            {
                mobileSkillsService = progression; mobileSkillsSlot = progression.CurrentSlotId; mobileSkillDetail = false;
                mobileSkillListScroll = mobileSkillDetailScroll = Vector2.zero;
                mobileSkillStatus = null;
            }
        }
        private void DrawMobileSkills()
        {
            var progression = session.Progression;
            ReconcileMobileSkillOwner();
            GameProfile profile = progression.Profile;
            selectedSkill = Mathf.Clamp(selectedSkill, 0, GameBalance.SkillCount - 1);
            var layout = MobilePanelGeometry();
            if (DrawMobilePanelChrome(layout, GameBalance.ClassName(profile.heroClass) + " · 技能",
                "Lv." + profile.level + " · 可用技能点 " + profile.skillPoints + " · 上下滑动查看全部10项")) return;

            bool split = SkillIconPresentation.SideBySide(layout.Width);
            bool showList = split || !mobileSkillDetail, showDetail = split || mobileSkillDetail;
            var listArea = split ? layout.BodyLeft : layout.Body;
            var detailArea = split ? layout.BodyRight : layout.Body;
            float u = TouchRatio, listWidth = listArea.Width - 16, detailWidth = detailArea.Width - 16;
            if (showList)
            {
                float listHeight = DrawMobileSkillRows(listWidth, false);
                mobileSkillListScroll = BeginTouchScroll("mobile-skill-list", MobilePanelRect(listArea), mobileSkillListScroll,
                    new Rect(0, 0, listWidth * u, Mathf.Max(listArea.Height, listHeight) * u));
                DrawMobileSkillRows(listWidth, true);
                EndTouchScroll();
            }
            if (showDetail)
            {
                float detailHeight = DrawMobileSkillDescription(detailWidth, false);
                mobileSkillDetailScroll = BeginTouchScroll("mobile-skill-detail", MobilePanelRect(detailArea), mobileSkillDetailScroll,
                    new Rect(0, 0, detailWidth * u, Mathf.Max(detailArea.Height, detailHeight) * u));
                DrawMobileSkillDescription(detailWidth, true);
                EndTouchScroll();
            }
            if (NavigationButton(MobilePanelRect(layout.FooterButton(0, showDetail ? 2 : 1)), RouteSkillReturnAvailable?"返回职业路线":!split && mobileSkillDetail ? "返回技能列表" : "返回冒险", jade))
            { ClosePanel(); BlockUITransition(); return; }
            if (!showDetail) return;
            int rank = progression.Profile.skillRanks[selectedSkill];
            string reason = progression.SkillLockReason(selectedSkill);
            string caption = rank >= 3 ? "已完全觉醒" : (rank == 0 ? "学习初习" : "进阶" + GameBalance.SkillRankName(rank + 1)) + " · 1点";
            Rect learn = MobilePanelRect(layout.FooterButton(1, 2));
            if (PrimaryButton(learn, caption, gold, string.IsNullOrEmpty(reason)))
            {
                bool saved = progression.LearnSkill(selectedSkill) && string.IsNullOrEmpty(progression.LastError);
                mobileSkillStatusFailed = !saved;
                mobileSkillStatus = saved ? GameBalance.SkillName(progression.Profile.heroClass, selectedSkill) + "已达到" +
                    GameBalance.SkillRankName(progression.Profile.skillRanks[selectedSkill]) : progression.LastError;
                CancelMobileScroll();
                mobileSkillDetailScroll = Vector2.zero;
                Feedback(saved, mobileSkillStatus);
                BlockUITransition();
            }
            Badge(learn, Attention.LearnableSkills.Contains(selectedSkill));
        }

        private float DrawMobileSkillRows(float width, bool draw)
        {
            var p = session.Progression.Profile;
            float y = 4;
            for (int skill = 0; skill < GameBalance.SkillCount; skill++)
            {
                int rank = p.skillRanks[skill];
                string name = (skill + 1) + ". " + GameBalance.SkillName(p.heroClass, skill);
                string state = GameBalance.SkillRankName(rank) + " · " + (GameBalance.IsPassive(skill) ? "被动" : "主动") +
                    "\nLv." + GameBalance.SkillRequiredLevels[skill] + (Attention.LearnableSkills.Contains(skill) ? " · 可学习 / 进阶" : "");
                float nameHeight = MeasureMobileParagraph(name, width - 78, 15, true);
                float stateHeight = MeasureMobileParagraph(state, width - 78, 14);
                float h = Mathf.Max(48, nameHeight + stateHeight + 24);
                if (draw)
                {
                    Rect row = TouchRect(0, y, width, h);
                    Fill(row, selectedSkill == skill ? new Color(.10f, .20f, .23f) : card);
                    Border(row, selectedSkill == skill ? gold : jade * .4f);
                    DrawSkillIdentity(TouchRect(8, y + 10, 48, 48), p.heroClass, skill, rank, rank > 0, 48);
                    DrawMobileParagraph(66, y + 8, width - 78, name, 15, pale, true);
                    DrawMobileParagraph(66, y + 12 + nameHeight, width - 78, state, 14, rank > 0 ? jade : muted);
                    Badge(row, Attention.LearnableSkills.Contains(skill));
                    if (MobileSkillRowClicked(row) && (selectedSkill != skill || !mobileSkillDetail))
                    {
                        selectedSkill = skill; mobileSkillDetail = true;
                        mobileSkillDetailScroll = Vector2.zero;
                        mobileSkillStatus = null;
                        CancelMobileScroll();
                        BlockUITransition();
                    }
                }
                y += h + 8;
            }
            return y;
        }

        private float DrawMobileSkillDescription(float width, bool draw)
        {
            var p = session.Progression.Profile;
            int skill = selectedSkill, rank = p.skillRanks[skill];
            float y = 8;
            string name = GameBalance.SkillName(p.heroClass, skill);
            float headerHeight = Mathf.Max(56, MeasureMobileParagraph(name, width - 80, 18, true) + 8);
            if (draw)
            {
                DrawSkillIdentity(TouchRect(8, y, 48, 48), p.heroClass, skill, rank, rank > 0, 48);
                DrawMobileParagraph(68, y + 4, width - 80, name, 18, pale, true);
            }
            y += headerHeight;
            if (!string.IsNullOrEmpty(mobileSkillStatus))
                MobileSkillParagraph(ref y, width, mobileSkillStatus, 14, mobileSkillStatusFailed ? gold : jade, true, draw);
            MobileSkillParagraph(ref y, width, (GameBalance.IsPassive(skill) ? "被动" : "主动") + " · " +
                GameBalance.CategoryName(GameBalance.GetSkillCategory(p.heroClass, skill)), 16, jade, true, draw);
            MobileSkillParagraph(ref y, width, SkillTooltip(p, skill, rank), 14, pale, false, draw);
            MobileSkillParagraph(ref y, width, "学习前置：" + GameBalance.PrerequisiteDescription(p.heroClass, skill), 14,
                session.Progression.PrerequisitesMet(skill) ? muted : gold, false, draw);
            string reason = session.Progression.SkillLockReason(skill);
            MobileSkillParagraph(ref y, width, string.IsNullOrEmpty(reason) ? "可学习下一阶：消耗1技能点" : reason, 14, gold, true, draw);
            for (int stage = 1; stage <= 3; stage++)
            {
                MobileSkillParagraph(ref y, width, GameBalance.SkillRankName(stage) + " · Lv." + GameBalance.SkillRankRequiredLevel(skill, stage) +
                    (stage == rank ? " · 当前" : stage < rank ? " · 已学习" : ""), 16, stage <= rank ? jade : pale, true, draw);
                string evolution = skill == 2 && p.heroClass != HeroClass.Summoner ? SkillBudgetHint(p.heroClass, skill, stage) : GameBalance.SkillEvolution(p.heroClass, skill, stage);
                string venom=BuildCatalog.VenomSkillOverride(p,skill,stage);if(venom.Length>0)evolution=venom;
                MobileSkillParagraph(ref y, width, evolution, 14, muted, false, draw);
            }
            MobileSkillParagraph(ref y, width, GameBalance.IsPassive(skill) ? "被动学习后自动生效，无须施放。" :
                "学会后直接显示在战斗界面；轻点自动瞄准施放，无须配置或翻页。", 14, jade, false, draw);
            return y;
        }

        private void MobileSkillParagraph(ref float y, float width, string text, int size, Color color, bool bold, bool draw)
        {
            y += draw ? DrawMobileParagraph(8, y, width - 16, text, size, color, bold) : MeasureMobileParagraph(text, width - 16, size, bold);
            y += 10;
        }
    }
}
