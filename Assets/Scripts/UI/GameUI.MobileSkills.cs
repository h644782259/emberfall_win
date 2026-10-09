using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Vector2 mobileSkillListScroll, mobileSkillDetailScroll;
        private string mobileSkillsSlot;
        private ProgressionService mobileSkillsService;
        private string mobileSkillStatus;
        private bool mobileSkillStatusFailed;

        private void ReconcileMobileSkillOwner()
        {
            var progression=session.Progression;
            if (mobileSkillsService != progression || mobileSkillsSlot != progression.CurrentSlotId)
            {
                mobileSkillsService = progression; mobileSkillsSlot = progression.CurrentSlotId;
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
                "Lv." + profile.level + " · 等级自动解锁",showNotice:false,headerRightReserve:276)) return;

            DrawSkillTabs(TouchRect(layout.Close.X-276,8,264,44));
            if(skillSection!=0)return;
            float u=TouchRatio,bodyHeight=RouteSkillReturnAvailable?layout.Body.Height:layout.Height-layout.Body.Y-12;
            float leftWidth=layout.Body.Width*.44f;
            var listArea=new MobilePanelLayout.Area(layout.Body.X,layout.Body.Y,leftWidth,bodyHeight);
            float listWidth=listArea.Width-16;
            float listHeight=DrawMobileSkillTree(listWidth,false);
            mobileSkillListScroll=BeginTouchScroll("mobile-skill-list",MobilePanelRect(listArea),mobileSkillListScroll,new Rect(0,0,listWidth*u,Mathf.Max(listArea.Height,listHeight)*u));
            DrawMobileSkillTree(listWidth,true);EndTouchScroll();
            if(RouteSkillReturnAvailable&&NavigationButton(MobilePanelRect(layout.FooterButton(0,1)),"返回职业路线",jade)){ClosePanel();BlockUITransition();return;}
            Rect r=TouchRect(listArea.XMax+12,listArea.Y,layout.Body.Width-leftWidth-12,bodyHeight);
            Fill(r,new Color(.025f,.055f,.075f,.99f));Border(r,jade*.5f);
            Text(new Rect(r.x+10*u,r.y+4*u,r.width-20*u,36*u),GameBalance.SkillName(profile.heroClass,selectedSkill)+" · "+profile.skillRanks[selectedSkill]+"/3",TouchFont(16),pale,true,false,TextAnchor.MiddleLeft);
            Rect detailArea=new Rect(r.x+6*u,r.y+44*u,r.width-12*u,Mathf.Max(32*u,r.height-100*u));float detailWidth=detailArea.width/u-16;
            float detailHeight=DrawMobileSkillDescription(detailWidth,false);
            mobileSkillDetailScroll=BeginTouchScroll("mobile-skill-detail",detailArea,mobileSkillDetailScroll,new Rect(0,0,detailWidth*u,Mathf.Max(detailArea.height,detailHeight*u)));
            DrawMobileSkillDescription(detailWidth,true);EndTouchScroll();
            int rank = progression.Profile.skillRanks[selectedSkill];
            string reason = progression.SkillLockReason(selectedSkill);
            Text(new Rect(r.x+8*u,r.yMax-48*u,r.width-16*u,40*u),reason,TouchFont(14),gold,true,false,TextAnchor.MiddleCenter);

        }

        private Rect MobileSkillTreeNode(int skill,float width)
        {
            float step=width/3;
            return TouchRect(4+GameBalance.SkillTreeColumn(skill)*step,8+GameBalance.SkillTreeRow(skill)*108,step-8,82);
        }
        private float DrawMobileSkillTree(float width,bool draw)
        {
            if(!draw)return 7*108+8;
            var p=session.Progression.Profile;
            for(int child=0;child<GameBalance.SkillCount;child++)foreach(int parent in GameBalance.SkillPrerequisites[child])
            {
                Rect a=MobileSkillTreeNode(parent,width),b=MobileSkillTreeNode(child,width);
                Color c=p.skillRanks[parent]>0?jade:muted*.45f;float mid=b.y-12*TouchRatio;
                Fill(new Rect(a.center.x-TouchRatio,a.yMax,2*TouchRatio,Mathf.Max(0,mid-a.yMax)),c);
                Fill(new Rect(Mathf.Min(a.center.x,b.center.x),mid,Mathf.Max(2*TouchRatio,Mathf.Abs(a.center.x-b.center.x)),2*TouchRatio),c);
                Fill(new Rect(b.center.x-TouchRatio,mid,2*TouchRatio,b.y-mid),c);
            }
            for(int skill=0;skill<GameBalance.SkillCount;skill++)
            {
                int rank=p.skillRanks[skill];bool canLearn=Attention.LearnableSkills.Contains(skill);
                Rect node=MobileSkillTreeNode(skill,width);Color accent=selectedSkill==skill?gold:rank>0?jade:muted;
                // Branch positions and connectors use the shared skill prerequisite graph.
                Rect icon=new Rect(node.center.x-18*TouchRatio,node.y+5*TouchRatio,36*TouchRatio,36*TouchRatio);
                DrawSkillIdentity(icon,p.heroClass,skill,rank,rank>0||canLearn,48);
                if(selectedSkill==skill)DrawIcon(new Rect(node.center.x+20*TouchRatio,node.y+2*TouchRatio,12*TouchRatio,12*TouchRatio),UIIconAtlas.Utility("confirm"),gold);
                Text(new Rect(node.x+4*TouchRatio,node.y+44*TouchRatio,node.width-8*TouchRatio,22*TouchRatio),GameBalance.SkillName(p.heroClass,skill),TouchFont(10),rank>0||canLearn?pale:muted,true,false,TextAnchor.MiddleCenter);
                string state="Lv."+GameBalance.SkillRequiredLevels[skill]+" · "+(canLearn?rank>0?"可进阶":"可学习":rank>0?GameBalance.SkillRankName(rank):GameBalance.IsPassive(skill)?"被动 · 未学":"未解锁");
                Text(new Rect(node.x+4*TouchRatio,node.y+64*TouchRatio,node.width-8*TouchRatio,16*TouchRatio),state,TouchFont(10),canLearn?gold:accent,false,false,TextAnchor.MiddleCenter);
                Badge(icon,canLearn);
                if(MobileSkillRowClicked(node)&&selectedSkill!=skill)
                {selectedSkill=skill;mobileSkillDetailScroll=Vector2.zero;mobileSkillStatus=null;CancelMobileScroll();}
            }
            return 7*108+8;
        }

        private float DrawMobileSkillDescription(float width, bool draw)
        {
            var p = session.Progression.Profile;
            int skill = selectedSkill, rank = p.skillRanks[skill];
            float y = 8;
            if (!string.IsNullOrEmpty(mobileSkillStatus))
                MobileSkillParagraph(ref y, width, mobileSkillStatus, 14, mobileSkillStatusFailed ? gold : jade, true, draw);
            MobileSkillParagraph(ref y, width, (GameBalance.IsPassive(skill) ? "被动" : "主动") + " · " +
                GameBalance.CategoryName(GameBalance.GetSkillCategory(p.heroClass, skill)), 13, jade, true, draw);
            MobileSkillParagraph(ref y, width, SkillTooltip(p, skill, rank), 14, pale, false, draw);
            MobileSkillParagraph(ref y,width,session.Progression.SkillLockReason(skill),14,gold,true,draw);
            for (int stage = 1; stage <= 3; stage++)
            {
                MobileSkillParagraph(ref y, width, GameBalance.SkillRankName(stage) + " · Lv." + GameBalance.SkillRankRequiredLevel(skill, stage) +
                    (stage == rank ? " · 当前" : stage < rank ? " · 已学习" : ""), 16, stage <= rank ? jade : pale, true, draw);
                string evolution = skill == 2 && p.heroClass != HeroClass.Summoner ? SkillBudgetHint(p.heroClass, skill, stage) : GameBalance.SkillEvolution(p.heroClass, skill, stage);
                string venom=BuildCatalog.VenomSkillOverride(p,skill,stage);if(venom.Length>0)evolution=venom;
                MobileSkillParagraph(ref y, width, evolution, 14, muted, false, draw);
            }
            return y;
        }

        private void MobileSkillParagraph(ref float y, float width, string text, int size, Color color, bool bold, bool draw)
        {
            y += draw ? DrawMobileParagraph(8, y, width - 16, text, size, color, bold) : MeasureMobileParagraph(text, width - 16, size, bold);
            y += 10;
        }
    }
}
