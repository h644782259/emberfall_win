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

            DrawSkillTabs(MobilePanelRect(layout.Tabs));
            if(skillSection==1)return;
            bool split = SkillIconPresentation.SideBySide(layout.Width) && mobileSkillDetail;
            bool showList = split || !mobileSkillDetail, showDetail = split || mobileSkillDetail;
            var listArea = split ? new MobilePanelLayout.Area(layout.BodyLeft.X,layout.TabbedBody.Y,layout.BodyLeft.Width,layout.TabbedBody.Height) : layout.TabbedBody;
            var detailArea = split ? new MobilePanelLayout.Area(layout.BodyRight.X,layout.TabbedBody.Y,layout.BodyRight.Width,layout.TabbedBody.Height) : layout.TabbedBody;
            if(!showDetail&&!RouteSkillReturnAvailable)listArea=new MobilePanelLayout.Area(listArea.X,listArea.Y,listArea.Width,layout.Height-listArea.Y-12);
            float u = TouchRatio, listWidth = listArea.Width - 16, detailWidth = detailArea.Width - 16;
            if (showList)
            {
                float listHeight = DrawMobileSkillTree(listWidth, false);
                mobileSkillListScroll = BeginTouchScroll("mobile-skill-list", MobilePanelRect(listArea), mobileSkillListScroll,
                    new Rect(0, 0, listWidth * u, Mathf.Max(listArea.Height, listHeight) * u));
                DrawMobileSkillTree(listWidth, true);
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
            if ((RouteSkillReturnAvailable||showDetail) && NavigationButton(MobilePanelRect(layout.FooterButton(0, showDetail ? 2 : 1)), RouteSkillReturnAvailable?"返回职业路线":!split && mobileSkillDetail ? "返回技能列表" : "关闭详情", jade))
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

        private Rect MobileSkillTreeNode(int skill,float width)
        {
            float step=(width-16)/3f;
            return TouchRect(14+GameBalance.SkillTreeColumn(skill)*step,12+GameBalance.SkillTreeRow(skill)*94,step-12,76);
        }
        private float DrawMobileSkillTree(float width,bool draw)
        {
            if(!draw)return 658;
            var p=session.Progression.Profile;
            for(int skill=0;skill<GameBalance.SkillCount;skill++)
            {
                Rect node=MobileSkillTreeNode(skill,width);
                foreach(int parentIndex in GameBalance.SkillPrerequisites[skill])
                {
                    Rect parent=MobileSkillTreeNode(parentIndex,width);
                    Color link=p.skillRanks[parentIndex]>0?jade:new Color(.2f,.28f,.33f);
                    float bend=node.y-12*TouchRatio;
                    Fill(new Rect(parent.center.x-TouchRatio,parent.yMax,2*TouchRatio,bend-parent.yMax),link);
                    Fill(new Rect(Mathf.Min(parent.center.x,node.center.x),bend,Mathf.Max(2*TouchRatio,Mathf.Abs(parent.center.x-node.center.x)),2*TouchRatio),link);
                    Fill(new Rect(node.center.x-TouchRatio,bend,2*TouchRatio,node.y-bend),link);
                }
            }
            for(int skill=0;skill<GameBalance.SkillCount;skill++)
            {
                int rank=p.skillRanks[skill];bool canLearn=Attention.LearnableSkills.Contains(skill);
                Rect node=MobileSkillTreeNode(skill,width);Color accent=selectedSkill==skill?gold:rank>0?jade:muted;
                // Only the icon has a compact border; text and branch lines float on the tree.
                DrawSkillIdentity(new Rect(node.center.x-18*TouchRatio,node.y+5*TouchRatio,36*TouchRatio,36*TouchRatio),p.heroClass,skill,rank,rank>0||canLearn,48);
                Text(new Rect(node.x+4*TouchRatio,node.y+44*TouchRatio,node.width-8*TouchRatio,18*TouchRatio),GameBalance.SkillName(p.heroClass,skill),TouchFont(12),rank>0||canLearn?pale:muted,true,false,TextAnchor.MiddleCenter);
                string state="Lv."+GameBalance.SkillRequiredLevels[skill]+" · "+(canLearn?rank>0?"可进阶":"可学习":rank>0?GameBalance.SkillRankName(rank):GameBalance.IsPassive(skill)?"被动 · 未学":"未解锁");
                Text(new Rect(node.x+4*TouchRatio,node.y+64*TouchRatio,node.width-8*TouchRatio,16*TouchRatio),state,TouchFont(10),canLearn?gold:accent,false,false,TextAnchor.MiddleCenter);
                Badge(node,canLearn);
                if(MobileSkillRowClicked(node)&&(selectedSkill!=skill||!mobileSkillDetail))
                {selectedSkill=skill;mobileSkillDetail=true;mobileSkillDetailScroll=Vector2.zero;mobileSkillStatus=null;CancelMobileScroll();BlockUITransition();}
            }
            return 658;
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
