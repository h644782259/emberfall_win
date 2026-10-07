using System;
using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private readonly Vector2[] mobileWorkshopScroll = new Vector2[4];
        private ProgressionService mobileWorkshopService;
        private string mobileWorkshopStatus;
        private bool mobileWorkshopFailed;

        private void DrawMobileCampWorkshop()
        {
            var p = session.Progression;
            if (mobileWorkshopService != p)
            {
                mobileWorkshopService = p;
                for (int i = 0; i < mobileWorkshopScroll.Length; i++) mobileWorkshopScroll[i] = Vector2.zero;
                mobileWorkshopStatus = null;
            }
            var layout = MobilePanelGeometry();
            campTab = Mathf.Clamp(campTab, 0, 3);
            if (DrawMobilePanelChrome(layout, "营地工坊", CurrentProgressionGoalStatus())) return;
            string[] tabs = { "战技", "机制图鉴", "待领取", "实战试炼" };
            for (int i = 0; i < tabs.Length; i++)
            {
                Rect tab = MobilePanelRect(layout.Tab(i, tabs.Length));
                if (TabButton(tab, tabs[i], campTab == i) && campTab != i)
                { campTab = i; mobileWorkshopStatus = null; CancelMobileScroll(); BlockUITransition(); }
                Badge(tab, i == 1 ? Attention.FirstClearClaimable : i == 2 && Attention.LootClaimable);
            }
            float contentWidth = layout.TabbedBody.Width - 16;
            float contentHeight = DrawMobileWorkshopContent(contentWidth, false);
            mobileWorkshopScroll[campTab] = BeginTouchScroll("mobile-workshop-" + campTab, MobilePanelRect(layout.TabbedBody),
                mobileWorkshopScroll[campTab], new Rect(0, 0, contentWidth * TouchRatio, Mathf.Max(contentHeight, layout.TabbedBody.Height) * TouchRatio));
            DrawMobileWorkshopContent(contentWidth, true);
            EndTouchScroll();
            if (NavigationButton(MobilePanelRect(layout.FooterButton(0, 4)), "返回冒险", jade))
            { ClosePanel(); BlockUITransition(); return; }
            if (NavigationButton(MobilePanelRect(layout.FooterButton(1, 4)), "技能树", jade))
            { panel = Panel.Skills; CancelMobileScroll(); BlockUITransition(); return; }
            if (NavigationButton(MobilePanelRect(layout.FooterButton(2, 4)), "行囊", jade))
            { panel = Panel.Inventory; CancelMobileScroll(); BlockUITransition(); }
            if (NavigationButton(MobilePanelRect(layout.FooterButton(3, 4)), "目标", jade))OpenProgressionGoals();
        }

        private float DrawMobileWorkshopContent(float width, bool draw)
        {
            float y = 8;
            if (campTab == 0) DrawMobileWorkshopAbilities(ref y, width, draw);
            else if (campTab == 1) DrawMobileWorkshopMechanics(ref y, width, draw);
            else if (campTab == 2) DrawMobileWorkshopLoot(ref y, width, draw);
            else DrawMobileWorkshopTutorial(ref y, width, draw);
            return y + 8;
        }

        private void MobileWorkshopParagraph(ref float y, float width, string text, Color color, bool draw, bool bold = false, int size = 14)
        {
            y += draw ? DrawMobileParagraph(8, y, width - 16, text, size, color, bold) : MeasureMobileParagraph(text, width - 16, size, bold);
            y += 10;
        }

        private void MobileWorkshopAction(ref float y, float width, string caption, Color color, bool enabled, bool draw, Action action, ButtonRole role = ButtonRole.Action)
        {
            if (draw && DrawButton(TouchRect(8, y, width - 16, 48), caption, role, enabled)) action();
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

        private void DrawMobileWorkshopMechanics(ref float y, float width, bool draw)
        {
            var p = session.Progression;
            MobileWorkshopParagraph(ref y, width, "星烬碎片 " + p.Profile.mechanicMaterials + (p.Profile.pendingFirstClearReward ? " · 可领取首通自选" : " · 定向兑换12碎片"), gold, draw, true, 16);
            foreach (EquipmentMechanic mechanic in BuildCatalog.MechanicsFor(p.Profile.heroClass))
            {
                MobileWorkshopParagraph(ref y, width, BuildCatalog.MechanicName(mechanic), pale, draw, true, 16);
                MobileWorkshopParagraph(ref y, width, BuildCatalog.MechanicDescription(mechanic), pale, draw);
                MobileWorkshopParagraph(ref y, width, BuildCatalog.MechanicSource(mechanic), muted, draw);
                bool first = p.Profile.pendingFirstClearReward;
                MobileWorkshopAction(ref y, width, first ? "首通自选 · 领取这件装备" : "兑换这件装备 · 12碎片", gold,
                    session.IsInCamp && (first || p.Profile.mechanicMaterials >= ProgressionService.MechanicExchangeCost), draw,
                    () => MobileWorkshopResult(first ? p.ClaimFirstClearReward(mechanic) : p.ExchangeMechanic(mechanic), "机制装备已领取"));
                ItemData item = p.Equipped(BuildCatalog.MechanicSlot(mechanic));
                if (item == null || item.mechanic != mechanic)
                { MobileWorkshopParagraph(ref y, width, "穿戴这件机制装备后，可在此重铸、切换元素变体或升华。", muted, draw); continue; }
                string id = item.id;
                MobileWorkshopParagraph(ref y, width, "当前穿戴：" + ItemTitle(item) + " · " + GameBalance.RarityName(item.rarity) + " · Lv." + item.level + "\n编号：" + id, jade, draw);
                MobileWorkshopParagraph(ref y, width, "重铸保留装备身份、机制和部位强化；可以分段成长。", muted, draw);
                MobileWorkshopAction(ref y, width, "选择重铸档位", jade, session.IsInCamp&&p.QuoteReforge(id)!=null, draw, ()=>OpenReforgeSurface(id), ButtonRole.Navigation);
                if (BuildCatalog.HasMechanicVariant(mechanic))
                {
                    MobileWorkshopParagraph(ref y, width, "当前变体 " + (item.mechanicVariant == 0 ? "A" : "B") + "；首次解锁4碎片，此后免费切换互斥效果。", muted, draw);
                    MobileWorkshopAction(ref y, width, p.HasVariant(item) ? "切换到变体 " + (item.mechanicVariant == 0 ? "B" : "A") : "解锁变体 B · 4碎片", jade,
                        string.IsNullOrEmpty(p.VariantLockReason(id,session.IsInCamp)), draw,
                        () => MobileWorkshopResult(p.ToggleMechanicVariant(id, session.IsInCamp), "装备变体已切换"));
                }
                string reason = p.AscensionLockReason(id, session.IsInCamp);
                MobileWorkshopParagraph(ref y, width, string.IsNullOrEmpty(reason) ? "史诗升华为传说：保留编号、等级、机制变体和部位强化；基础属性按25/18提升，不随机重抽。" : reason, gold, draw);
                MobileWorkshopAction(ref y, width, item.rarity == Rarity.Legendary ? "已是传说品质" : "升华当前装备 · 24碎片", gold, string.IsNullOrEmpty(reason), draw,
                    () => MobileWorkshopResult(p.AscendMechanic(id, session.IsInCamp), "机制装备已升华为传说；身份与变体保留"));
            }
        }

        private void DrawMobileWorkshopLoot(ref float y, float width, bool draw)
        {
            var p = session.Progression;
            MobileWorkshopParagraph(ref y, width, "背包 " + p.Profile.inventory.Count + "/" + ProgressionService.InventoryCapacity + " · 待领取 " + p.Profile.pendingLoot.Count +
                " · 恢复栏 " + p.RecoveryLootCount, pale, draw, true, 16);
            MobileWorkshopParagraph(ref y, width, "自动出售只处理之后拾取的对应低品质装备；穿戴、锁定、机制及已强化装备受保护。领取装备绕过自动出售。", muted, draw);
            MobileWorkshopAction(ref y, width, "普通自动出售：" + (p.Profile.autoSellCommon ? "开" : "关"), jade, true, draw, () => MobileWorkshopResult(p.SetAutoSell(Rarity.Common, !p.Profile.autoSellCommon), "普通装备自动出售设置已更新"), p.Profile.autoSellCommon?ButtonRole.ActiveToggle:ButtonRole.Toggle);
            MobileWorkshopAction(ref y, width, "稀有自动出售：" + (p.Profile.autoSellRare ? "开" : "关"), jade, true, draw, () => MobileWorkshopResult(p.SetAutoSell(Rarity.Rare, !p.Profile.autoSellRare), "稀有装备自动出售设置已更新"), p.Profile.autoSellRare?ButtonRole.ActiveToggle:ButtonRole.Toggle);
            MobileWorkshopAction(ref y, width, "批量出售背包低品质装备", gold, true, draw, () =>
            {
                RequestPresetSale(null,true);
            }, ButtonRole.Danger);
            var mailbox = new List<ItemData>(p.Profile.pendingLoot);
            mailbox.AddRange(p.Profile.recoveryLoot);
            MobileWorkshopAction(ref y, width, "领取所有可放入背包的装备", gold, mailbox.Count > 0 && p.Profile.inventory.Count < ProgressionService.InventoryCapacity, draw, ClaimMobileWorkshopLoot, ButtonRole.Primary);
            if (mailbox.Count == 0) MobileWorkshopParagraph(ref y, width, "没有待领取或保管的装备。", muted, draw);
            foreach (ItemData item in mailbox)
            {
                string id = item.id;
                bool recovery = p.Profile.recoveryLoot.Exists(value => value.id == id);
                MobileWorkshopParagraph(ref y, width, ItemTitle(item) + " · " + GameBalance.RarityName(item.rarity) + " · Lv." + item.level +
                    (recovery ? " · 恢复栏" : " · 待领取") + (item.locked ? " · 已锁定" : "") + "\n编号：" + id, GameBalance.RarityColor(item.rarity), draw, true);
                MobileWorkshopAction(ref y, width, "领取这件装备", jade, p.Profile.inventory.Count < ProgressionService.InventoryCapacity, draw,
                    () => MobileWorkshopResult(recovery ? p.ClaimRecoveryLoot(id) : p.ClaimPendingLoot(id), "已领取 " + item.name));
            }
        }

        private void ClaimMobileWorkshopLoot()
        {
            var p = session.Progression;
            int claimed = 0;
            if (p.Profile.pendingLoot.Count > 0 && p.Profile.inventory.Count < ProgressionService.InventoryCapacity)
            {
                claimed = p.ClaimAllPendingLoot();
                if (claimed == 0 && !string.IsNullOrEmpty(p.LastError)) { MobileWorkshopResult(false, ""); return; }
            }
            if (p.Profile.recoveryLoot.Count > 0 && p.Profile.inventory.Count < ProgressionService.InventoryCapacity)
            {
                int recovered = p.ClaimAllRecoveryLoot();
                if (recovered == 0 && !string.IsNullOrEmpty(p.LastError)) { MobileWorkshopResult(false, ""); return; }
                claimed += recovered;
            }
            MobileWorkshopResult(claimed > 0, "已领取 " + claimed + " 件；放不下的装备继续保管");
        }

        private void DrawMobileWorkshopTutorial(ref float y, float width, bool draw)
        {
            string[] actions = { "普攻命中，回复能量", "躲过一次即将命中的预警攻击", session.Progression.ClassTutorialText, "在行囊换上一件装备" };
            for (int i = 0; i < actions.Length; i++)
            {
                if(i==2&&!session.ClassTutorialVisible)continue;
                bool done = i==2?session.Progression.Profile.classTutorialCompleted:(session.Progression.Profile.tutorialMask & (1 << i)) != 0;
                MobileWorkshopParagraph(ref y, width, (done ? "✓ 已完成 · " : "○ 待完成 · ") + actions[i], done ? jade : pale, draw, true, 16);
            }
            if(session.Progression.HighestAdventureTier>0||session.Progression.Profile.clearedRuns>0)
            {
                MobileWorkshopParagraph(ref y,width,"首通整备 · 领取核心、检查路线，再保存配装",jade,draw);
                MobileWorkshopAction(ref y, width, "机制与核心", gold, true, draw, ()=>{campTab=1;CancelMobileScroll();BlockUITransition();}, ButtonRole.Navigation);
                MobileWorkshopAction(ref y, width, "职业路线", jade, true, draw, ()=>{campTab=0;CancelMobileScroll();BlockUITransition();}, ButtonRole.Navigation);
                MobileWorkshopAction(ref y, width, "配装方案", jade, true, draw, OpenBuildPlans, ButtonRole.Navigation);
                MobileWorkshopAction(ref y, width, "选择下一目标", jade, true, draw, OpenProgressionGoals, ButtonRole.Navigation);
            }
        }
    }
}
