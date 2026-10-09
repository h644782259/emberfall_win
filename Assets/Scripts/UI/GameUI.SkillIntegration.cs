using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private int skillSection;
        private Vector2 skillDevelopmentScroll;
        private void DrawSkillTabs(Rect rect)
        {
            float u=MobileControls.Active?TouchRatio:1,gap=8*u,w=(rect.width-gap)/2;
            for(int i=0;i<2;i++)
            {
                Rect hit=new Rect(rect.x+i*(w+gap),rect.y,w,rect.height);
                bool selected=skillSection==i;
                Text(hit,i==0?"战技":"职业精通",Mathf.RoundToInt(14*u),selected?gold:muted,selected,false,TextAnchor.MiddleCenter);
                if(selected)Fill(new Rect(hit.x+8*u,hit.yMax-5*u,hit.width-16*u,2*u),gold);
                if(GUI.Button(hit,GUIContent.none,invisibleButton)&&!selected){skillSection=i;CancelMobileScroll();}
            }
        }
        private float DrawSpecializationChoices(float width,float u,float y,bool draw)
        {
            var p=session.Progression;
            bool elemental=p.Profile.heroClass==HeroClass.Arcanist;
            if(!elemental&&p.Profile.heroClass!=HeroClass.Summoner)return y;
            string[] names=elemental?new[]{"均衡","碎冰","灼燃"}:new[]{"强契","群契"};
            string[] descriptions=elemental?new[]{
                "均衡：保留冰霜控制与陨星直接伤害；消耗霜痕时追加 50% 基础伤害。",
                "碎冰：陨星直接伤害降低 15%，碎冰追加 100% 基础伤害；霜环延长霜痕，大招积霜后引爆。",
                "灼燃：普攻与陨星附加灼烧，新星改为减速；霜焰行者提供机动火区，大招偏持续灼烧。"
            }:new[]{"强契：每次召唤一只，召唤物伤害提高20%；全部限时存在。","群契：灵狼每次召唤两只，召唤物伤害降低20%；全部限时存在。"};
            int selected=elemental?(int)p.Profile.specialization:(int)p.Profile.summonerRoute;
            float descriptionHeight=0;
            foreach(string text in descriptions)descriptionHeight=Mathf.Max(descriptionHeight,Style(Mathf.RoundToInt(13*u),false,true).CalcHeight(new GUIContent(text),(width-8)*u)/u);
            if(draw)
            {
                Text(new Rect(4*u,y*u,(width-8)*u,26*u),elemental?"专精":"契约模式",Mathf.RoundToInt(16*u),pale,true);
                float optionWidth=(width-(names.Length-1)*8)/names.Length;
                for(int i=0;i<names.Length;i++)
                {
                    Rect hit=new Rect(i*(optionWidth+8)*u,(y+30)*u,optionWidth*u,44*u);
                    if(TabButton(hit,names[i],selected==i)&&selected!=i)
                    {
                        bool saved=elemental?p.SetSpecialization((ElementalistSpecialization)i,session.IsInCamp):p.SetSummonerRoute((SummonerRoute)i,session.IsInCamp);
                        MobileWorkshopResult(saved,names[i]+"已选择");
                        if(saved)selected=i;
                    }
                }
                Text(new Rect(4*u,(y+82)*u,(width-8)*u,descriptionHeight*u),descriptions[Mathf.Clamp(selected,0,names.Length-1)],Mathf.RoundToInt(13*u),pale,false,true);
            }
            return y+82+descriptionHeight+12;
        }
        private bool DrawSkillSubsurface()
        {return DrawClassSwitchSurface()||DrawBuildPlanSurface();}
        private void DrawSkillDevelopment()
        {
            var p=session.Progression;
            if(MobileControls.Active)
            {
                var l=MobilePanelGeometry();
                if(DrawMobilePanelChrome(l,"技能 · 职业与精通","技能点 "+p.Profile.skillPoints+" · 共用配点",headerRightReserve:188))return;
                DrawSkillTabs(TouchRect(l.Close.X-188,8,176,44));
                if(skillSection==0)return;
                var body=new MobilePanelLayout.Area(l.Body.X,l.Body.Y,l.Body.Width,l.Height-l.Body.Y-12);
                float mobileWidth=body.Width-16,h=8;DrawMobileWorkshopAbilities(ref h,mobileWidth,false);
                skillDevelopmentScroll=BeginTouchScroll("skill-development",MobilePanelRect(body),skillDevelopmentScroll,new Rect(0,0,mobileWidth*TouchRatio,Mathf.Max(body.Height,h)*TouchRatio));
                h=8;DrawMobileWorkshopAbilities(ref h,mobileWidth,true);EndTouchScroll();return;
            }
            Rect w=Modal(980,660,"技能 · 职业与精通","可用技能点 "+p.Profile.skillPoints);
            if(PopupCloseButton(new Rect(w.xMax-69,w.y+20,44,32)))ClosePanel();
            DrawSkillTabs(new Rect(w.xMax-269,w.y+20,176,36));
            if(skillSection==0)return;
            Rect desktopBody=new Rect(w.x+26,w.y+82,w.width-52,w.height-96);float available=desktopBody.width-18;
            float desktopHeight=DrawSkillDevelopmentContent(available,1,false);
            skillDevelopmentScroll=BeginTouchScroll("skill-development",desktopBody,skillDevelopmentScroll,new Rect(0,0,available,Mathf.Max(desktopBody.height,desktopHeight)));
            DrawSkillDevelopmentContent(available,1,true);EndTouchScroll();
        }
        private string SkillMasterySummary()
        {var p=session.Progression;return "可用点数 "+p.Profile.skillPoints+" · "+MasteryProgressionRules.TierSummary+" · "+MasteryProgressionRules.CoreSummary;}
        private string MasteryNodeDescription(MasteryType mastery)
        {
            string description=BuildCatalog.MasteryDescription(mastery);
            int tier=description.IndexOf(MasteryProgressionRules.TierSummary,System.StringComparison.Ordinal);
            if(tier>=0)description=description.Substring(0,tier);
            return description+"\n"+SkillMasterySummary();
        }
        private int selectedMastery;
        private float DrawSkillDevelopmentContent(float width,float u,bool draw)
        {
            var p=session.Progression;var layout=new SkillDevelopmentLayout(width);float y=8;
            if(!draw)
            {
                var m=(MasteryType)Mathf.Clamp(selectedMastery,0,3);
                string measuredHint=BuildCatalog.MasteryName(m)+" · "+(string.IsNullOrEmpty(p.MasteryLockReason(m))?"投入消耗 1 技能点":p.MasteryLockReason(m))+"\n核心需 "+MasteryCoreRules.InitialInvestment+" 点投入，"+MasteryCoreRules.EnhancedInvestment+" 点增强；唯一核心。";
                float hintHeight=Style(Mathf.RoundToInt(13*u),false,true).CalcHeight(new GUIContent(measuredHint),(width-12)*u)/u;
                float descriptionHeight=Style(Mathf.RoundToInt(12*u),false,true).CalcHeight(new GUIContent(MasteryNodeDescription(m)),(width-12)*u)/u;
                return DrawSpecializationChoices(width,u,244+hintHeight+8+descriptionHeight+12,false);
            }
            Rect tools=new Rect(0,y*u,width*u,48*u);
            Rect role=new Rect(0,tools.y,layout.ClassWidth*u,48*u);
            if(InventoryPictogramAction(role,GameBalance.ClassName(p.Profile.heroClass),UIIconAtlas.SkillGlyph(p.Profile.heroClass,0),true))OpenClassSwitch();
            if(role.Contains(Mouse))tooltip="切换职业 · "+BuildCatalog.ClassSignatureDescription(p.Profile.heroClass);
            for(int i=0;i<2;i++)
            {
                int slot=i;float x=layout.ClassWidth+8+i*(layout.PlanWidth+8);
                Text(new Rect(x*u,tools.y,24*u,48*u),i==0?"A":"B",Mathf.RoundToInt(15*u),p.HasBuildPreset(i)?gold:muted,true,false,TextAnchor.MiddleCenter);
                float action=(layout.PlanWidth-24)/2;
                if(InventoryPictogramAction(new Rect((x+24)*u,tools.y,action*u,48*u),"记录",UIIconAtlas.Utility("save"),session.IsInCamp))RequestBuildPlanAction(BuildPlanAction.Save,slot);
                if(InventoryPictogramAction(new Rect((x+24+action)*u,tools.y,action*u,48*u),"应用",UIIconAtlas.Utility("apply"),session.IsInCamp&&p.HasBuildPreset(slot)))RequestBuildPlanAction(BuildPlanAction.Apply,slot);
            }
            Rect reset=new Rect((width-layout.ResetWidth)*u,tools.y,layout.ResetWidth*u,48*u);
            if(InventoryPictogramAction(reset,"返还 "+p.RefundableBuildPoints,UIIconAtlas.Utility("reset"),session.IsInCamp&&(p.RefundableBuildPoints>0||p.Profile.masteryCore>=0)))RequestBuildPlanAction(BuildPlanAction.Reset);
            if(reset.Contains(Mouse))tooltip="免费重置配点 · 退还进阶与精通；保留已学1阶、快捷栏和装备。先确认再提交。";
            y=64;
            for(int i=0;i<4;i++)
            {
                var mastery=(MasteryType)i;int rank=p.Profile.masteryRanks[i],cap=ProgressionService.MasteryCap(p.Profile.level);string reason=p.MasteryLockReason(mastery);
                var area=layout.MasteryNode(i);Rect node=new Rect(area.X*u,area.Y*u,area.Width*u,area.Height*u);
                bool active=selectedMastery==i,learnable=string.IsNullOrEmpty(reason);Color ink=active?gold:learnable?jade:muted;
                Rect icon=new Rect(node.center.x-15*u,node.y,30*u,30*u);DrawIcon(icon,UIIconAtlas.Mastery(mastery),rank>0||learnable?Color.white:muted);Border(icon,ink);
                if(active)DrawIcon(new Rect(icon.x-6*u,icon.y-2*u,12*u,12*u),UIIconAtlas.Utility("confirm"),gold);
                if(p.HasMasteryCore(mastery))DrawIcon(new Rect(icon.xMax-8*u,icon.y-2*u,14*u,14*u),UIIconAtlas.Utility("core"),gold);
                Text(new Rect(node.x,node.y+31*u,node.width,22*u),BuildCatalog.MasteryName(mastery).Replace("精通",""),Mathf.RoundToInt(13*u),active?gold:pale,true,false,TextAnchor.MiddleCenter);
                Text(new Rect(node.x,node.y+53*u,node.width,17*u),cap==0?"Lv."+MasteryProgressionRules.FirstUnlockLevel+" 解锁":rank+" / "+cap,Mathf.RoundToInt(11*u),muted,false,false,TextAnchor.MiddleCenter);
                Fill(new Rect(node.x+8*u,node.y+70*u,node.width-16*u,u),muted*.25f);Fill(new Rect(node.x+8*u,node.y+70*u,(node.width-16*u)*Mathf.Min(1,rank/(float)Mathf.Max(1,cap)),u),jade);
                Rect select=new Rect(node.x,node.y,node.width,72*u);
                if(GUI.Button(select,GUIContent.none,invisibleButton)){selectedMastery=i;CancelMobileScroll();}
                if(select.Contains(Mouse))tooltip=BuildCatalog.MasteryDescription(mastery)+(string.IsNullOrEmpty(reason)?"":"\n"+reason);
                string state=cap==0?"未解锁":rank>=cap?"已满":learnable?"1 点":p.Profile.skillPoints<=0?"缺点数":"未解锁";
                if(InventoryPictogramAction(new Rect(node.x,node.y+72*u,node.width,44*u),state,UIIconAtlas.Utility(cap>0&&rank>=cap?"confirm":learnable?"upgrade":"lock"),learnable,false,true))
                {selectedMastery=i;MobileWorkshopResult(p.LearnMastery(mastery),"精通已提高");}
            }
            y=188;selectedMastery=Mathf.Clamp(selectedMastery,0,3);var selected=(MasteryType)selectedMastery;
            bool hasCore=p.HasMasteryCore(selected);int selectedRank=p.Profile.masteryRanks[selectedMastery];
            string core=hasCore?(p.MasteryCoreTier(selected)==2?"增强核心":"初阶核心"):selectedRank>=MasteryCoreRules.EnhancedInvestment?"启用增强":"启用核心";
            float third=(width-16)/3;
            if(InventoryPictogramAction(new Rect(0,y*u,third*u,48*u),core,UIIconAtlas.Utility("core"),session.IsInCamp&&selectedRank>=MasteryCoreRules.InitialInvestment&&!hasCore,hasCore,true))MobileWorkshopResult(p.SelectMasteryCore(selected, session.IsInCamp),"唯一精通核心已切换");
            if(InventoryPictogramAction(new Rect((third+8)*u,y*u,third*u,48*u),"退进阶",UIIconAtlas.Utility("reset"),session.IsInCamp&&p.RefundableSkillRanks>0,false,true))MobileWorkshopResult(p.RefundSkillRanks(session.IsInCamp),"技能进阶点已返还；已学1阶保留");
            bool invested=p.Profile.masteryCore>=0;foreach(int rank in p.Profile.masteryRanks)invested|=rank>0;
            if(InventoryPictogramAction(new Rect((third+8)*2*u,y*u,third*u,48*u),"退精通",UIIconAtlas.Utility("reset"),session.IsInCamp&&invested,false,true))MobileWorkshopResult(p.ResetMastery(session.IsInCamp),"精通点已返还，核心已关闭");
            y+=56;
            string hint=BuildCatalog.MasteryName(selected)+" · "+(string.IsNullOrEmpty(p.MasteryLockReason(selected))?"投入消耗 1 技能点":p.MasteryLockReason(selected))+"\n核心需 "+MasteryCoreRules.InitialInvestment+" 点投入，"+MasteryCoreRules.EnhancedInvestment+" 点增强；唯一核心。";
            float h=Style(Mathf.RoundToInt(13*u),false,true).CalcHeight(new GUIContent(hint),(width-12)*u)/u;
            Text(new Rect(4*u,y*u,(width-12)*u,h*u),hint,Mathf.RoundToInt(13*u),pale,false,true);y+=h+8;
            string description=MasteryNodeDescription(selected);float dh=Style(Mathf.RoundToInt(12*u),false,true).CalcHeight(new GUIContent(description),(width-12)*u)/u;
            Text(new Rect(4*u,y*u,(width-12)*u,dh*u),description,Mathf.RoundToInt(12*u),muted,false,true);y+=dh+12;
            return DrawSpecializationChoices(width,u,y,draw);
        }
    }
}
