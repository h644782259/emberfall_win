using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private int selectedBlessing = -1, campTab;
        private Vector2 pendingScroll;
        private bool systemHistory;
        public void CancelForegroundInput()
        {
            CancelHotbarPointer(); rebindingSlot = -1;
            // IMGUI buttons have their own hotControl, unrelated to our hotbar.
            GUIUtility.hotControl=0;GUIUtility.keyboardControl=0;
            BlockUITransition();
        }
        public void CancelBackgroundInput()
        {
            CancelForegroundInput();
            // Only focus/suspension and viewport invalidation require every
            // pointer to lift. Room transitions retain their initiating finger.
            lifecycleRelease.Block(Time.unscaledTime);
        }

        private void DrawDungeonSelection(){DrawArenaSelection();}

        private Vector2 branchScroll;
        private void DrawRoomBranchChoice()
        {
            float u=MobileControls.Active?TouchRatio:1;
            Rect w=Modal(Mathf.Min(width-24,900*u),Mathf.Min(height-24,520*u),"第三房 · 选择侧廊","只进入所选路线；第四房星泉汇合，第五房首领。无额外材料奖励。");
            Rect viewport=new Rect(w.x+20*u,w.y+105*u,w.width-40*u,Mathf.Max(60*u,w.height-175*u));
            float cw=(viewport.width-16*u)*.5f;
            branchScroll=BeginTouchScroll("room-branch",viewport,branchScroll,new Rect(0,0,viewport.width,280*u));
            for(int i=0;i<2;i++)
            {
                RoomBranch branch=i==0?RoomBranch.Seal:RoomBranch.Supply;
                Rect c=new Rect(i*(cw+16*u),0,cw,270*u);Fill(c,card);Border(c,jade);
                Text(new Rect(c.x+12*u,c.y+12*u,c.width-24*u,196*u),GameSession.RoomBranchDescription(branch),Mathf.RoundToInt(16*u),pale,false,true);
                if(Button(new Rect(c.x+12*u,c.y+218*u,c.width-24*u,42*u),"进入这条侧廊",gold))
                {session.ConfirmRoomBranch(branch);BlockUITransition();}
            }
            EndTouchScroll();
            if(Button(new Rect(w.x+20*u,w.yMax-58*u,w.width-40*u,40*u),"返回第二房 · 暂不选择",jade))
            {session.CancelRoomBranchChoice();BlockUITransition();}
        }

        private void DrawBlessingChoice()
        {
            if(MobileControls.Active){DrawMobileBlessingChoice();return;}
            RunBlessing[] offer = session.RunChoices.Offer;
            Rect w = Modal(1000, 470, "星烬祝福", BlessingSubtitle(false));
            for (int i=0;i<offer.Length;i++)
            {
                Rect cardRect = new Rect(w.x+28+i*322,w.y+117,300,240);
                bool chosen = i == selectedBlessing;
                Fill(cardRect,chosen ? new Color(.12f,.2f,.21f):card); Border(cardRect,chosen?gold:jade*.5f);
                Text(new Rect(cardRect.x+20,cardRect.y+23,260,28),RunChoices.Name(offer[i]),23,chosen?gold:pale,true);
                bool compatible = RunChoices.IsCompatible(offer[i],session.Progression.Profile.heroClass,RunChoices.UsableRanks(session.Progression.Profile,false));
                Text(new Rect(cardRect.x+20,cardRect.y+63,260,21),RunChoices.Association(offer[i],session.Progression.Profile,false),12,compatible?jade:muted);
                Text(new Rect(cardRect.x+20,cardRect.y+102,260,93),RunChoices.Description(offer[i]),16,pale,false,true);
                if(Button(new Rect(cardRect.x+20,cardRect.y+192,260,32),chosen?"已选择":"选择",chosen?gold:jade))selectedBlessing=i;
            }
            if(Button(new Rect(w.x+310,w.y+392,380,46),session.RoomChainRun!=null?"确认祝福并继续":"确认并进入下一波",gold,selectedBlessing>=0&&selectedBlessing<offer.Length,null,true))
            { if(session.ConfirmBlessing(selectedBlessing))selectedBlessing=-1; }
        }

        private void DrawCampWorkshop()
        {
            if(DrawReforgeSurface())return;
            if(DrawProgressionGoalSurface())return;
            if(DrawBuildPlanSurface())return;
            if(MobileControls.Active){DrawMobileCampWorkshop();return;}
            Rect w=Modal(980,620,"营地工坊",CurrentProgressionGoalStatus());
            if(Button(new Rect(w.xMax-255,w.y+20,170,36),"成长目标",jade))OpenProgressionGoals();
            if(Button(new Rect(w.xMax-69,w.y+20,44,32),"×",jade))ClosePanel();
            string[] tabs={"战技","机制图鉴","待领取","实战试炼"};
            for(int i=0;i<tabs.Length;i++){Rect tabRect=new Rect(w.x+26+i*233,w.y+110,220,36);if(Button(tabRect,tabs[i],campTab==i?gold:jade))campTab=i;Badge(tabRect,i==1?Attention.FirstClearClaimable:i==2?Attention.LootClaimable:false);}
            ProgressionService p=session.Progression;
            if(campTab==0)
            {
                Text(new Rect(w.x+32,w.y+167,880,30),GameBalance.ClassName(p.Profile.heroClass)+" · 职业能力",23,gold,true);
                string[] signatures={"真正躲过攻击后，2秒内下一次普攻反击。","冰霜新星 → 陨星，消耗霜印碎冰。","普攻积累三层毒，以扇形箭引爆。","幼狼从开场协战；普攻让伙伴短时集火。"};
                Text(new Rect(w.x+32,w.y+208,880,38),signatures[(int)p.Profile.heroClass],18,pale,false,true);
                if(new Rect(w.x+32,w.y+167,880,81).Contains(Mouse))tooltip=BuildCatalog.ClassSignatureDescription(p.Profile.heroClass);
                for(int i=0;i<2;i++)
                {
                    int route=i;var info=CampRouteCards.Describe(p.Profile,false,i);
                    Rect c=new Rect(w.x+32+i*458,w.y+254,430,174);Fill(c,card);
                    Text(new Rect(c.x+14,c.y+10,402,25),info.Name,19,gold,true);
                    Text(new Rect(c.x+14,c.y+39,402,36),info.Loop,14,pale,false,true);
                    Text(new Rect(c.x+14,c.y+79,402,50),info.Requirements+(info.Ready?"\n"+info.Enhancement:""),12,info.Ready?jade:muted,false,true);
                    if(info.NextAction==CampRouteAction.None)Text(new Rect(c.x+14,c.y+137,402,25),info.NextStep,12,muted);
                    else if(Button(new Rect(c.x+14,c.y+137,402,28),info.NextStep,gold,session.IsInCamp))FollowCampRouteStep(info,route);
                }
                for(int i=0;i<4;i++)
                {
                    MasteryType mastery=(MasteryType)i;Rect c=new Rect(w.x+32+i*229,w.y+435,214,134);
                    Text(new Rect(c.x,c.y,c.width,25),BuildCatalog.MasteryName(mastery)+"  "+p.Profile.masteryRanks[i]+"/"+ProgressionService.MasteryCap(p.Profile.level),17,pale,true);
                    string reason=p.MasteryLockReason(mastery);
                    if(Button(new Rect(c.x,c.y+39,c.width,35),"投入 1 点",jade,string.IsNullOrEmpty(reason),string.IsNullOrEmpty(reason)?BuildCatalog.MasteryDescription(mastery):reason))Feedback(p.LearnMastery(mastery),"精通已提高");
                    if(Button(new Rect(c.x,c.y+82,c.width,32),p.HasMasteryCore(mastery)?(p.MasteryCoreTier(mastery)==2?"增强核心 ✓":"初阶核心 ✓"):"启用核心 · "+MasteryCoreRules.InitialInvestment+"点",gold,session.IsInCamp&&p.Profile.masteryRanks[i]>=MasteryCoreRules.InitialInvestment&&!p.HasMasteryCore(mastery),BuildCatalog.MasteryDescription(mastery)))Feedback(p.SelectMasteryCore(mastery,session.IsInCamp),"已切换唯一精通核心");
                }
                Text(new Rect(w.x+32,w.y+552,884,22),"可用点数 "+p.Profile.skillPoints+" · "+MasteryProgressionRules.TierSummary+" · "+MasteryProgressionRules.CoreSummary,12,muted);
                if(Button(new Rect(w.x+32,w.y+580,435,30),"免费重置配点 · "+p.RefundableBuildPoints+"点",muted,session.IsInCamp&&(p.RefundableBuildPoints>0||p.Profile.masteryCore>=0),"先核对技能进阶与精通返还点数；保留已学1阶、快捷栏和装备。"))RequestBuildPlanAction(BuildPlanAction.Reset);
                if(Button(new Rect(w.x+485,w.y+580,461,30),"配装方案 · 记录 / 应用两套",jade))OpenBuildPlans();
            }
            else if(campTab==1)
            {
                Text(new Rect(w.x+32,w.y+168,880,28),"星烬碎片  "+p.Profile.mechanicMaterials+" / 12",22,gold,true);
                EquipmentMechanic[] all=BuildCatalog.MechanicsFor(p.Profile.heroClass);
                for(int i=0;i<all.Length;i++)
                {
                    EquipmentMechanic mechanic=all[i];Rect c=new Rect(w.x+32,w.y+217+i*162,884,147);Fill(c,card);
                    Text(new Rect(c.x+18,c.y+13,620,29),BuildCatalog.MechanicName(mechanic),21,pale,true);
                    Text(new Rect(c.x+18,c.y+53,610,78),BuildCatalog.MechanicDescription(mechanic),15,muted,false,true);
                    bool first=p.Profile.pendingFirstClearReward;
                    if(Button(new Rect(c.x+660,c.y+9,205,36),first?"首通选取":"兑换 · 12 碎片",gold,session.IsInCamp&&(first||p.Profile.mechanicMaterials>=12),BuildCatalog.MechanicSource(mechanic),true))
                        Feedback(first?p.ClaimFirstClearReward(mechanic):p.ExchangeMechanic(mechanic),"机制装备已领取");
                    ItemData equipped=p.Equipped(BuildCatalog.MechanicSlot(mechanic));
                    if(equipped!=null&&equipped.mechanic==mechanic)
                    {
                        if(Button(new Rect(c.x+660,c.y+55,99,36),"重铸档位",jade,session.IsInCamp&&p.QuoteReforge(equipped.id)!=null,"选择提升5级、金币可达或追平等级"))OpenReforgeSurface(equipped.id);
                        if(BuildCatalog.HasMechanicVariant(mechanic)&&Button(new Rect(c.x+766,c.y+55,99,36),p.HasVariant(equipped)?(equipped.mechanicVariant==0?"变体 A":"变体 B"):"变体 · 4",jade,string.IsNullOrEmpty(p.VariantLockReason(equipped.id,session.IsInCamp)),"本角色首次学习4碎片，同机制装备免费选已学变体"))Feedback(p.ToggleMechanicVariant(equipped.id,session.IsInCamp),"装备变体已切换");
                        string ascension = p.AscensionLockReason(equipped.id,session.IsInCamp);
                        if(Button(new Rect(c.x+660,c.y+101,205,36),equipped.rarity==Rarity.Legendary?"已是传说品质":"传说升华 · 24碎片",gold,string.IsNullOrEmpty(ascension),string.IsNullOrEmpty(ascension)?"保留物品编号、等级、机制变体和部位强化；基础属性按25/18提升，无随机重抽。":ascension))Feedback(p.AscendMechanic(equipped.id,session.IsInCamp),"机制装备已升华为传说；身份、变体与部位强化保留");
                    }
                    if(c.Contains(Mouse)&&Mouse.x<c.x+648)tooltip=BuildCatalog.MechanicSource(mechanic);
                }
                Text(new Rect(w.x+32,w.y+566,884,30),"穿戴已知机制装备：金币按成长等级报价重铸 · 机制变体首次4碎片 · 通关第5阶后24碎片史诗升华传说",14,jade);
            }
            else if(campTab==2)
            {
                if(Button(new Rect(w.x+32,w.y+165,278,38),"普通自动卖："+(p.Profile.autoSellCommon?"开":"关"),jade))p.SetAutoSell(Rarity.Common,!p.Profile.autoSellCommon);
                if(Button(new Rect(w.x+322,w.y+165,278,38),"稀有自动卖："+(p.Profile.autoSellRare?"开":"关"),jade))p.SetAutoSell(Rarity.Rare,!p.Profile.autoSellRare);
                if(Button(new Rect(w.x+612,w.y+165,304,38),"批量出售低品质",gold,true,"穿戴、锁定及机制装备受保护")){RequestPresetSale(null,true);}
                Text(new Rect(w.x+32,w.y+216,884,24),"待领取 "+p.Profile.pendingLoot.Count+"/24 · 恢复栏 "+p.RecoveryLootCount+" · 锁定、穿戴和机制装备受保护",14,muted);
                var mailbox=new System.Collections.Generic.List<ItemData>(p.Profile.pendingLoot); mailbox.AddRange(p.Profile.recoveryLoot);
                Rect viewport=new Rect(w.x+32,w.y+254,884,280);
                pendingScroll=BeginTouchScroll("rewards",viewport,pendingScroll,new Rect(0,0,865,Mathf.Max(280,mailbox.Count*58)));
                for(int i=0;i<mailbox.Count;i++)
                { ItemData item=mailbox[i];Text(new Rect(12,i*58+8,660,28),item.name,18,GameBalance.RarityColor(item.rarity),true);
                  if(Button(new Rect(702,i*58+4,145,38),"领取",jade,p.Profile.inventory.Count<ProgressionService.InventoryCapacity)) { Feedback(p.Profile.recoveryLoot.Exists(x=>x.id==item.id)?p.ClaimRecoveryLoot(item.id):p.ClaimPendingLoot(item.id),"已领取 "+item.name);break; } }
                EndTouchScroll();
                if(Button(new Rect(w.x+32,w.y+554,884,39),"领取可放入背包的装备",gold))Feedback(true,"领取 "+(p.ClaimAllPendingLoot()+p.ClaimAllRecoveryLoot())+" 件");
            }
            else
            {
                string[] actions={"普攻命中，回复能量","躲过一次即将命中的预警攻击",p.ClassTutorialText,"在行囊换上一件装备"};
                float line=w.y+174;
                for(int i=0;i<actions.Length;i++)
                {
                    if(i==2&&!session.ClassTutorialVisible)continue;
                    bool done=i==2?p.Profile.classTutorialCompleted:(p.Profile.tutorialMask&(1<<i))!=0;
                    Text(new Rect(w.x+42,line,850,38),(done?"✓ ":"○ ")+actions[i],20,done?jade:pale,true);line+=56;
                }
                if(p.HighestAdventureTier>0||p.Profile.clearedRuns>0)
                {
                    Text(new Rect(w.x+42,w.y+435,850,40),"首通整备 · 领取核心、检查路线，再保存一套配装",16,jade);
                    if(Button(new Rect(w.x+42,w.y+493,200,48),"机制与核心",gold))campTab=1;
                    if(Button(new Rect(w.x+256,w.y+493,200,48),"职业路线",jade))campTab=0;
                    if(Button(new Rect(w.x+470,w.y+493,200,48),"配装方案",jade))OpenBuildPlans();
                    if(Button(new Rect(w.x+684,w.y+493,200,48),"选择下一目标",jade))OpenProgressionGoals();
                }
            }
        }

        private void DrawExpeditionHUD()
        {
            if(session.NearChapterExit){Rect next=new Rect((width-300)*.5f,height-225,300,48);blockedRects.Add(next);if(Button(next,"沿星路前进",gold))session.EnterNextChapterRoom();}
            else if(session.NearRoomExit){Rect next=new Rect((width-300)*.5f,height-225,300,48);blockedRects.Add(next);if(Button(next,"北门已开启 · 进入下一间",gold))session.EnterNextRoom();}
            if(session.IsInCamp)
            { Rect r=new Rect(16,AdventureSelectionLayout.WorkshopY(height,session.SystemMessages.Count,systemHistory),212,36);blockedRects.Add(r);if(Button(r,"营地工坊",jade)) {panel=Panel.Camp;session.SetUIBlocking(true);} }
            if(session.SideEventAvailable)
            { Rect r=new Rect((width-410)*.5f,height-260,410,48);blockedRects.Add(r);
              if(Button(r,"晶核支线：2敌 · 全灭1碎片+补给",gold,true,"额外一名遗迹守卫与一名魔灵；全部击败才获得1碎片和补给。未完成可放弃，不阻挡已开启的北门。"))session.StartSideEvent(); }

            DrawSystemLog();
        }
        private void DrawSystemLog()
        {
            if(panel!=Panel.None || session.InputBlocked)return;
            var messages=session.SystemMessages;
            if(messages.Count==0)return;
            int show=systemHistory?Mathf.Min(8,messages.Count):Mathf.Min(3,messages.Count);
            float panelHeight=AdventureSelectionLayout.LogHeight(messages.Count,systemHistory);
            float bottom=height-(MobileControls.Active?220:16);
            Rect r=new Rect(16,bottom-panelHeight,344,panelHeight);
            Fill(r,new Color(.025f,.045f,.065f,.88f));
            Text(new Rect(r.x+10,r.y+6,228,20),"系统信息",12,jade,true);
            if(Button(new Rect(r.xMax-68,r.y+3,59,25),systemHistory?"收起":"记录",muted))systemHistory=!systemHistory;
            for(int i=0;i<show;i++)
            {
                Rect row=new Rect(r.x+10,r.y+31+i*39,r.width-20,38);
                Text(row,messages[messages.Count-show+i].Text,13,pale,false,true);
                if(row.Contains(Mouse))tooltip=messages[messages.Count-show+i].Text;
            }
            blockedRects.Add(r);
        }

        private void DrawRunSummary()
        {
            if(DrawStructuredRunRecap(false))ClosePanel();
        }

        private void DrawAccessibilityStrip(Rect r)
        {
            if(Button(new Rect(r.x,r.y,145,35),"字号 "+EffectPreferences.CombatTextScale.ToString("0.00")+"×",jade))
                EffectPreferences.CombatTextScale=EffectPreferences.CombatTextScale>=1.79f?1f:Mathf.Min(1.8f,EffectPreferences.CombatTextScale+.25f);
            if(Button(new Rect(r.x+154,r.y,145,35),EffectPreferences.CameraShake?"镜头震动：开":"镜头震动：关",jade))EffectPreferences.CameraShake=!EffectPreferences.CameraShake;
            if(Button(new Rect(r.x+308,r.y,145,35),EffectPreferences.ReducedEffects?"低动态效果":"完整效果",jade))EffectPreferences.EffectsScale=EffectPreferences.ReducedEffects?1f:.3f;
        }
    }
}
