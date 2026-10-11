using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private int skillSection;
        private bool masteryResetConfirm;
        private Vector2 skillDevelopmentScroll;
        private void DrawSkillTabs(Rect rect)
        {
            float u=MobileControls.Active?TouchRatio:1,gap=8*u,w=(rect.width-2*gap)/3;
            for(int i=0;i<3;i++)
            {
                Rect hit=new Rect(rect.x+i*(w+gap),rect.y,w,rect.height);
                bool selected=skillSection==i;
                Text(hit,i==0?"战技":i==1?"职业精通":"切换职业",Mathf.RoundToInt(14*u),selected?gold:muted,selected,false,TextAnchor.MiddleCenter);
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
        {
            if(!masteryResetConfirm)return DrawClassSwitchSurface()||DrawBuildPlanSurface();
            float u=MobileControls.Active?TouchRatio:1;
            Rect r=Modal(Mathf.Min(460*u,width-24*u),Mathf.Min(240*u,height-24*u),"重置职业精通","");
            Text(new Rect(r.x+24*u,r.y+100*u,r.width-48*u,40*u),"确认重置所有精通加点并返还精通点？",Mathf.RoundToInt(14*u),pale,false,true);
            if(Button(new Rect(r.x+24*u,r.yMax-60*u,(r.width-60*u)/2,40*u),"取消",jade))masteryResetConfirm=false;
            if(Button(new Rect(r.center.x+6*u,r.yMax-60*u,(r.width-60*u)/2,40*u),"确认重置",gold))
            {bool saved=session.Progression.ResetMastery(true);masteryChangeNotice=saved?"职业精通已重置，精通点已返还":session.Progression.LastError;if(saved)masteryResetConfirm=false;}
            if(PopupCloseButton(new Rect(r.xMax-52*u,r.y+12*u,40*u,36*u)))masteryResetConfirm=false;
            return true;
        }
        private void DrawSkillDevelopment()
        {
            var p=session.Progression;
            if(MobileControls.Active)
            {
                var l=MobilePanelGeometry();
                if(DrawMobilePanelChrome(l,"职业精通","",showNotice:false,headerRightReserve:276))return;
                DrawSkillTabs(TouchRect(l.Close.X-276,8,264,44));
                if(skillSection!=1)return;
                var body=new MobilePanelLayout.Area(l.Body.X,l.Body.Y,l.Body.Width,l.Height-l.Body.Y-12);
                float mobileWidth=body.Width-16,h=8;DrawMobileWorkshopAbilities(ref h,mobileWidth,false);
                skillDevelopmentScroll=BeginTouchScroll("skill-development",MobilePanelRect(body),skillDevelopmentScroll,new Rect(0,0,mobileWidth*TouchRatio,Mathf.Max(body.Height,h)*TouchRatio));
                h=8;DrawMobileWorkshopAbilities(ref h,mobileWidth,true);EndTouchScroll();return;
            }
            Rect w=Modal(1160,660,"职业精通","");
            if(PopupCloseButton(new Rect(w.xMax-69,w.y+20,44,32)))ClosePanel();
            DrawSkillTabs(new Rect(w.x+330,w.y+20,264,36));
            if(skillSection!=1)return;
            Rect desktopBody=new Rect(w.x+26,w.y+108,w.width-52,w.height-122);float available=desktopBody.width-18;
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
        private string masteryChangeNotice,masteryNoticeSlot;
        private float masteryChangedAt;
        private int masteryChangedNode=-1;
        private string MasteryBonusText(int index,int rank)
        {
            float amount=rank*(index==0?.3f:index==1?.5f:index==2?.75f:.1f);
            return new[]{"攻击","生命","护甲","普攻回能"}[index]+" +"+amount.ToString("0.##")+(index==3?"":"%");
        }
        private string MasteryCoreText(MasteryType mastery)
        {
            string text=BuildCatalog.MasteryDescription(mastery);
            int end=text.IndexOf(MasteryProgressionRules.TierSummary,System.StringComparison.Ordinal);
            if(end>=0)text=text.Substring(0,end);
            int start=text.IndexOf("核心：",System.StringComparison.Ordinal);
            return start>=0?text.Substring(start+3):text;
        }
        private float DrawSkillDevelopmentContent(float width,float u,bool draw)
        {
            var p=session.Progression;bool compact=MobileControls.Active;float y=compact?4:8;
            if(masteryNoticeSlot!=p.CurrentSlotId){masteryNoticeSlot=p.CurrentSlotId;masteryChangeNotice=null;masteryChangedNode=-1;}
            selectedMastery=Mathf.Clamp(selectedMastery,0,3);
            if(draw)
            {
                Text(new Rect(4*u,y*u,(width-132)*u,44*u),"可用精通点 "+p.Profile.skillPoints,Mathf.RoundToInt((compact?13:16)*u),gold,true);
                if(DrawButton(new Rect((width-116)*u,(y+5)*u,112*u,32*u),"重置精通",ButtonRole.Action,fontSize:Mathf.RoundToInt(11*u)))masteryResetConfirm=true;
            }
            y+=compact?44:52;
            int columns=compact||width>=760?4:2,cap=ProgressionService.MasteryCap(p.Profile.level);
            float tileWidth=(width-(columns-1)*10)/columns,coreHeight=0;
            for(int i=0;i<4;i++)coreHeight=Mathf.Max(coreHeight,Style(Mathf.RoundToInt((compact?11:12)*u),false,true).CalcHeight(new GUIContent(MasteryCoreText((MasteryType)i)),(tileWidth-(compact?16:24))*u)/u);
            float tileHeight=(compact?168:202)+coreHeight;
            for(int i=0;i<4;i++)
            {
                var mastery=(MasteryType)i;int rank=p.Profile.masteryRanks[i];
                string reason=p.MasteryLockReason(mastery);bool learnable=string.IsNullOrEmpty(reason);
                Rect tile=new Rect((i%columns)*(tileWidth+10)*u,(y+i/columns*(tileHeight+10))*u,tileWidth*u,tileHeight*u);
                if(!draw)continue;
                bool selected=selectedMastery==i,flashing=masteryChangedNode==i&&Time.unscaledTime-masteryChangedAt<1.2f;
                Fill(tile,flashing?new Color(.07f,.23f,.17f):card);Border(tile,p.HasMasteryCore(mastery)?gold:jade*.4f);
                DrawIcon(new Rect(tile.x+(compact?8:10)*u,tile.y+8*u,(compact?22:28)*u,(compact?22:28)*u),UIIconAtlas.Mastery(mastery),pale);
                Text(new Rect(tile.x+(compact?36:46)*u,tile.y+(compact?4:8)*u,tile.width-(compact?100:110)*u,28*u),BuildCatalog.MasteryName(mastery),Mathf.RoundToInt((compact?12:14)*u),pale,true);
                Text(new Rect(tile.xMax-56*u,tile.y+5*u,48*u,24*u),"投入 "+rank,Mathf.RoundToInt(10*u),muted,false,false,TextAnchor.MiddleRight);
                Text(new Rect(tile.x+12*u,tile.y+(compact?32:42)*u,tile.width-24*u,24*u),"当前  "+MasteryBonusText(i,rank),Mathf.RoundToInt(13*u),pale,true);
                Text(new Rect(tile.x+12*u,tile.y+(compact?54:68)*u,tile.width-24*u,24*u),rank<cap?"下一点  "+MasteryBonusText(i,rank+1):cap==0?"Lv"+MasteryProgressionRules.FirstUnlockLevel+" 解锁":"已达当前等级上限",Mathf.RoundToInt(12*u),rank<cap?jade:muted);
                if(GUI.Button(new Rect(tile.x,tile.y,tile.width,(compact?72:98)*u),GUIContent.none,invisibleButton))selectedMastery=i;
                string action=learnable?"+ 1点":cap==0?"尚未解锁":rank>=cap?"当前已满":"精通点不足";
                if(PrimaryButton(new Rect(tile.x+10*u,tile.y+(compact?76:102)*u,tile.width-20*u,36*u),action,jade,learnable))
                {
                    selectedMastery=i;bool saved=p.LearnMastery(mastery);
                    masteryChangeNotice=saved?BuildCatalog.MasteryName(mastery)+"："+MasteryBonusText(i,rank)+" → "+MasteryBonusText(i,p.Profile.masteryRanks[i])+"（消耗 1 精通点）":p.LastError;
                    if(saved){masteryChangedNode=i;masteryChangedAt=Time.unscaledTime;}
                }
                bool active=p.HasMasteryCore(mastery);
                Text(new Rect(tile.x+(compact?8:12)*u,tile.y+(compact?120:150)*u,tile.width-(compact?16:24)*u,coreHeight*u),MasteryCoreText(mastery),Mathf.RoundToInt((compact?11:12)*u),active?gold:pale,false,true);
                string coreAction=active?"✓ 核心已启用":rank<MasteryCoreRules.InitialInvestment?"投入 "+MasteryCoreRules.InitialInvestment+" 点解锁":"启用核心";
                if(Button(new Rect(tile.x+10*u,tile.yMax-(compact?40:44)*u,tile.width-20*u,36*u),coreAction,active?gold:jade,session.IsInCamp&&rank>=MasteryCoreRules.InitialInvestment&&!active))
                {bool saved=p.SelectMasteryCore(mastery,true);masteryChangeNotice=saved?"已启用 "+BuildCatalog.MasteryName(mastery)+"核心":p.LastError;}
            }
            y+=((4+columns-1)/columns)*(tileHeight+10);
            string notice=string.IsNullOrEmpty(masteryChangeNotice)?"":masteryChangeNotice;
            float noticeHeight=string.IsNullOrEmpty(notice)?0:Style(Mathf.RoundToInt(13*u),false,true).CalcHeight(new GUIContent(notice),(width-24)*u)/u+16;
            if(draw&&noticeHeight>0){Fill(new Rect(0,y*u,width*u,noticeHeight*u),new Color(.045f,.13f,.12f));Text(new Rect(12*u,(y+8)*u,(width-24)*u,(noticeHeight-16)*u),notice,Mathf.RoundToInt(13*u),jade,false,true);}
            y+=noticeHeight+12;
            y=DrawSpecializationChoices(width,u,y+8,draw)+16;
            return y+8;
        }
    }
}
