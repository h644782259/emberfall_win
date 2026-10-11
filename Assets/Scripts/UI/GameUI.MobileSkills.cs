using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private float mobileSkillTreeStep=108;
        private bool CompactSkillTree => mobileSkillTreeStep<86;
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
                "人物等级达成后技能自动解锁",showNotice:false,headerRightReserve:276)) return;

            DrawSkillTabs(TouchRect(layout.Close.X-276,8,264,44));
            if(skillSection!=0)return;
            float u=TouchRatio,bodyHeight=RouteSkillReturnAvailable?layout.Body.Height:layout.Height-layout.Body.Y-12;
            float leftWidth=layout.Body.Width*.44f;
            var listArea=new MobilePanelLayout.Area(layout.Body.X,layout.Body.Y,leftWidth,bodyHeight);
            float listWidth=listArea.Width-16;
            mobileSkillTreeStep=Mathf.Clamp((bodyHeight-8)/7,44,108);
            float listHeight=DrawMobileSkillTree(listWidth,false);
            mobileSkillListScroll=BeginTouchScroll("mobile-skill-list",MobilePanelRect(listArea),mobileSkillListScroll,new Rect(0,0,listWidth*u,Mathf.Max(listArea.Height,listHeight)*u));
            DrawMobileSkillTree(listWidth,true);EndTouchScroll();
            if(RouteSkillReturnAvailable&&NavigationButton(MobilePanelRect(layout.FooterButton(0,1)),"返回职业路线",jade)){ClosePanel();BlockUITransition();return;}
            Rect r=TouchRect(listArea.XMax+12,listArea.Y,layout.Body.Width-leftWidth-12,bodyHeight);
            Surface(r,new Color(.012f,.024f,.038f,.99f));SurfaceFrame(r,new Color(jade.r,jade.g,jade.b,.28f));
            float tagsWidth=132*u;
            Text(new Rect(r.x+10*u,r.y+4*u,r.width-tagsWidth-26*u,36*u),GameBalance.SkillName(profile.heroClass,selectedSkill),TouchFont(16),pale,true,false,TextAnchor.MiddleLeft);
            SkillStateTag(new Rect(r.xMax-tagsWidth-10*u,r.y+10*u,56*u,24*u),GameBalance.IsPassive(selectedSkill)?"被动":"主动",jade,TouchFont(12));
            SkillStateTag(new Rect(r.xMax-82*u,r.y+10*u,72*u,24*u),GameBalance.CategoryName(GameBalance.GetSkillCategory(profile.heroClass,selectedSkill)),gold,TouchFont(12));
            int rank = progression.Profile.skillRanks[selectedSkill];
            string reason = rank >= 3 ? "" : progression.SkillLockReason(selectedSkill);
            Rect detailArea=new Rect(r.x+6*u,r.y+44*u,r.width-12*u,Mathf.Max(32*u,r.height-(string.IsNullOrEmpty(reason)?52:100)*u));float detailWidth=detailArea.width/u-16;
            float detailHeight=DrawMobileSkillDescription(detailWidth,false);
            bool detailOverflow=detailHeight*u>detailArea.height+.5f;
            if(detailOverflow)mobileSkillDetailScroll=BeginTouchScroll("mobile-skill-detail",detailArea,mobileSkillDetailScroll,new Rect(0,0,detailWidth*u,detailHeight*u));
            else{mobileSkillDetailScroll=Vector2.zero;GUI.BeginGroup(detailArea);}
            DrawMobileSkillDescription(detailWidth,true);if(detailOverflow)EndTouchScroll();else GUI.EndGroup();
            Text(new Rect(r.x+8*u,r.yMax-48*u,r.width-16*u,40*u),reason,TouchFont(14),gold,true,false,TextAnchor.MiddleCenter);

        }

        private Rect MobileSkillTreeNode(int skill,float width)
        {
            float step=width/3;
            return TouchRect(4+GameBalance.SkillTreeColumn(skill)*step,8+GameBalance.SkillTreeRow(skill)*mobileSkillTreeStep,step-8,CompactSkillTree?40:82);
        }
        private float DrawMobileSkillTree(float width,bool draw)
        {
            if(!draw)return 7*mobileSkillTreeStep+8;
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
                if(CompactSkillTree)
                {
                    Surface(node,new Color(accent.r*.025f,accent.g*.035f,accent.b*.045f,.98f));
                    SurfaceFrame(node,new Color(accent.r,accent.g,accent.b,selectedSkill==skill?.8f:.22f));
                    Rect compactIcon=new Rect(node.x+3*TouchRatio,node.y+4*TouchRatio,30*TouchRatio,30*TouchRatio);
                    DrawSkillIdentity(compactIcon,p.heroClass,skill,rank,rank>0||canLearn,48);
                    Text(new Rect(node.x+37*TouchRatio,node.y+2*TouchRatio,node.width-40*TouchRatio,20*TouchRatio),GameBalance.SkillName(p.heroClass,skill),TouchFont(10),rank>0?pale:muted,true,false,TextAnchor.MiddleLeft);
                    Text(new Rect(node.x+37*TouchRatio,node.y+22*TouchRatio,node.width-40*TouchRatio,16*TouchRatio),"Lv."+GameBalance.SkillRequiredLevels[skill]+" · "+(rank>0?GameBalance.SkillRankName(rank):"锁定"),TouchFont(8),accent,false,false,TextAnchor.MiddleLeft);
                    Badge(compactIcon,canLearn);
                    if(MobileSkillRowClicked(new Rect(node.x,node.y-2*TouchRatio,node.width,44*TouchRatio))&&selectedSkill!=skill){selectedSkill=skill;mobileSkillDetailScroll=Vector2.zero;mobileSkillStatus=null;CancelMobileScroll();}
                    continue;
                }
                Rect icon=new Rect(node.center.x-18*TouchRatio,node.y+5*TouchRatio,36*TouchRatio,36*TouchRatio);
                DrawSkillIdentity(icon,p.heroClass,skill,rank,rank>0||canLearn,48);
                if(selectedSkill==skill)DrawIcon(new Rect(node.center.x+20*TouchRatio,node.y+2*TouchRatio,12*TouchRatio,12*TouchRatio),UIIconAtlas.Utility("confirm"),gold);
                Text(new Rect(node.x+4*TouchRatio,node.y+44*TouchRatio,node.width-8*TouchRatio,22*TouchRatio),GameBalance.SkillName(p.heroClass,skill),TouchFont(10),rank>0||canLearn?pale:muted,true,false,TextAnchor.MiddleCenter);
                SkillStateTag(new Rect(node.x+3*TouchRatio,node.y+64*TouchRatio,(node.width-10*TouchRatio)*.45f,18*TouchRatio),"Lv."+GameBalance.SkillRequiredLevels[skill],muted,TouchFont(9));
                SkillStateTag(new Rect(node.x+node.width*.46f,node.y+64*TouchRatio,node.width*.54f-3*TouchRatio,18*TouchRatio),rank>0?GameBalance.SkillRankName(rank):"未解锁",accent,TouchFont(9));
                Badge(icon,canLearn);
                if(MobileSkillRowClicked(node)&&selectedSkill!=skill)
                {selectedSkill=skill;mobileSkillDetailScroll=Vector2.zero;mobileSkillStatus=null;CancelMobileScroll();}
            }
            return 7*mobileSkillTreeStep+8;
        }

        private float DrawMobileSkillDescription(float width, bool draw)
        {
            var p = session.Progression.Profile;
            int skill = selectedSkill, rank = p.skillRanks[skill];
            float y = 8;
            if (!string.IsNullOrEmpty(mobileSkillStatus))
                MobileSkillParagraph(ref y, width, mobileSkillStatus, 14, mobileSkillStatusFailed ? gold : jade, true, draw);
            string description=BuildCatalog.VenomSkillOverride(p,skill,rank);
            if(description.Length==0)description=GameBalance.SkillDescription(p.heroClass,skill);
            // The initial rank already contains the base description. Show only a distinct summary.
            string initial = GameBalance.SkillEvolution(p.heroClass,skill,1);
            if(!string.Equals(description,initial,System.StringComparison.Ordinal))
                MobileSkillParagraph(ref y,width,description,14,pale,false,draw);
            for (int stage = 1; stage <= 3; stage++)
            {
                if(draw)
                {
                    Rect header=TouchRect(8,y,width-16,30);
                    Fill(header,stage==rank?new Color(.028f,.10f,.088f):new Color(.018f,.036f,.054f));
                    if(stage==rank){Border(header,gold);Fill(new Rect(header.x,header.y,3*TouchRatio,header.height),gold);}
                    Text(TouchRect(14,y,48,30),GameBalance.SkillRankName(stage),TouchFont(14),stage<=rank?jade:pale,true);
                    SkillStateTag(TouchRect(66,y+4,52,22),"Lv."+GameBalance.SkillRankRequiredLevel(skill,stage),muted,TouchFont(10));
                    if(stage==rank)DrawIcon(TouchRect(width-42,y+4,22,22),UIIconAtlas.Utility("confirm"),gold);
                }
                y+=38;
                string evolution = skill == 2 && p.heroClass != HeroClass.Summoner ? SkillBudgetHint(p.heroClass, skill, stage) : GameBalance.SkillEvolution(p.heroClass, skill, stage);
                string venom=BuildCatalog.VenomSkillOverride(p,skill,stage);if(venom.Length>0)evolution=venom;
                MobileSkillParagraph(ref y, width, evolution, 14, muted, false, draw);
            }
            return y;
        }

        private void SkillStateTag(Rect rect,string label,Color accent,int size)
        {
            Surface(rect,new Color(accent.r,accent.g,accent.b,.09f));
            SurfaceFrame(rect,new Color(accent.r,accent.g,accent.b,.38f));
            Text(rect,label,size,accent,true,false,TextAnchor.MiddleCenter);
        }

        private void MobileSkillParagraph(ref float y, float width, string text, int size, Color color, bool bold, bool draw)
        {
            y += draw ? DrawMobileParagraph(8, y, width - 16, text, size, color, bold) : MeasureMobileParagraph(text, width - 16, size, bold);
            y += 10;
        }
    }
}
