using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private int mobileInventoryTab;
        private HubNpcKind mobileInventoryNpcRequest;
        private bool mobileInventoryDetail;
        private Vector2 mobileInventoryListScroll, mobileInventoryDetailScroll, mobileSupplyScroll;
        private string mobileInventoryProfile, mobileDetailItem;
        private string mobileInventoryStatus, mobileSupplyStatus;
        private bool mobileInventoryFailed, mobileSupplyFailed;
        private PlayerController mobileInventoryStatusOwner;
        private GameProfile mobileBagProfile;
        private int mobileBagCount = -1, mobileBagLevel, mobileBagFilter, mobileBagSort, mobileWeaponRank, mobileArmorRank, mobileRelicRank;
        private string mobileWeaponId, mobileArmorId, mobileRelicId;
        private readonly System.Collections.Generic.Dictionary<string, ItemData> mobileEquipmentPreviews = new System.Collections.Generic.Dictionary<string, ItemData>();
        private readonly System.Collections.Generic.Dictionary<string, float> mobileRowHeights = new System.Collections.Generic.Dictionary<string, float>();
        private float mobileRowWidth, mobileRowRatio;

        private void EnsureMobileBagItems()
        {
            var p = session.Progression.Profile;
            int weapon = session.Progression.SlotUpgradeRank(ItemSlot.Weapon), armor = session.Progression.SlotUpgradeRank(ItemSlot.Armor), relic = session.Progression.SlotUpgradeRank(ItemSlot.Relic);
            if (mobileBagProfile == p && mobileBagCount == p.inventory.Count && mobileBagLevel == p.level &&
                mobileBagFilter == inventoryFilter && mobileBagSort == inventorySort && mobileWeaponRank == weapon && mobileArmorRank == armor && mobileRelicRank == relic &&
                mobileWeaponId == p.weaponId && mobileArmorId == p.armorId && mobileRelicId == p.relicId) return;
            RebuildBagItems();
            mobileEquipmentPreviews.Clear(); mobileRowHeights.Clear();
            mobileBagProfile = p; mobileBagCount = p.inventory.Count; mobileBagLevel = p.level;
            mobileBagFilter = inventoryFilter; mobileBagSort = inventorySort;
            mobileWeaponRank = weapon; mobileArmorRank = armor; mobileRelicRank = relic;
            mobileWeaponId = p.weaponId; mobileArmorId = p.armorId; mobileRelicId = p.relicId;
        }
        private ItemData MobileEquipmentPreview(ItemData item)
        {
            if (item == null) return null;
            ItemData preview;
            if (!mobileEquipmentPreviews.TryGetValue(item.id, out preview))
            { preview = EquipmentPreview(item); mobileEquipmentPreviews[item.id] = preview; }
            return preview;
        }
        private float MobileEquipmentScore(ItemData item) { return ProgressionService.EquipmentScore(MobileEquipmentPreview(item)); }

        private void DrawMobileInventory()
        {
            var progression = session.Progression;
            var profile = progression.Profile;
            if (mobileInventoryProfile != progression.CurrentSlotId || mobileInventoryStatusOwner != session.Player)
            {
                mobileInventoryProfile = progression.CurrentSlotId; mobileInventoryStatusOwner = session.Player;
                mobileInventoryStatus = mobileSupplyStatus = null;
                mobileInventoryDetail = false; mobileInventoryTab = 0;
                mobileInventoryListScroll = mobileInventoryDetailScroll = mobileSupplyScroll = Vector2.zero;
            }
            // Apply the explicit NPC destination after a new-profile reset, so a
            // compact phone never opens a hidden equipped item in the bag list.
            if(mobileInventoryNpcRequest!=HubNpcKind.None)
            {
                bool smith=mobileInventoryNpcRequest==HubNpcKind.Blacksmith;
                mobileInventoryTab=smith?1:2;mobileInventoryDetail=smith&&!string.IsNullOrEmpty(selectedItem);
                mobileInventoryStatus=mobileSupplyStatus=null;
                mobileInventoryListScroll=mobileInventoryDetailScroll=mobileSupplyScroll=Vector2.zero;
                mobileInventoryNpcRequest=HubNpcKind.None;
            }
            EnsureMobileBagItems();
            ItemData picked = ResolveSelectedItem();
            var layout = MobilePanelGeometry();
            bool wide = MobileCollectionLayout.SideBySideInventory(layout.Width);
            bool detail = mobileInventoryDetail && !wide && mobileInventoryTab != 2;
            if (DrawMobilePanelChrome(layout, detail ? "装备详情" : HubInventoryTitle,
                "Lv." + profile.level + "  ·  " + Money(profile.gold) + " 金币  ·  " + profile.inventory.Count + "/" + ProgressionService.InventoryCapacity)) return;
            if (detail)
            {
                if (DrawMobileInventoryDetail(layout.Body, picked)) return;
                DrawMobileEquipmentActions(layout, picked, true);
                return;
            }
            string[] tabs = { "背包", "穿戴", "补给", "收藏" };
            for (int i = 0; i < tabs.Length; i++)
                if (Button(MobilePanelRect(layout.Tab(i, tabs.Length)), tabs[i], mobileInventoryTab == i ? gold : jade))
                {
                    if (i == 3) { panel = Panel.Fashion; BlockUITransition(); return; }
                    mobileInventoryTab = i; mobileInventoryDetail = false; mobileInventoryListScroll = Vector2.zero;
                    BlockUITransition(); return;
                }
            if (mobileInventoryTab == 2)
            {
                DrawMobileSupplies(layout);
                return;
            }
            if (DrawMobileInventoryList(wide ? layout.Left : layout.TabbedBody, wide)) return;
            picked = ResolveSelectedItem();
            if (wide)
            {
                if (DrawMobileInventoryDetail(layout.Right, picked)) return;
                DrawMobileEquipmentActions(layout, picked, false);
            }
            else
            {
                if (Button(MobilePanelRect(layout.FooterButton(0, 3)), "分类 · " + MobileInventoryFilterLabel, jade, mobileInventoryTab == 0))
                { CycleMobileInventoryFilter(); return; }
                if (Button(MobilePanelRect(layout.FooterButton(1, 3)), "排序 · " + MobileInventorySortLabel, jade, mobileInventoryTab == 0))
                { CycleMobileInventorySort(); return; }
                Rect catalog = MobilePanelRect(layout.FooterButton(2, 3));
                bool openCatalog = Button(catalog, "图鉴 / 待领", gold);
                Badge(catalog, Attention.Rewards);
                if (openCatalog) { panel = Panel.Camp; campTab = 1; BlockUITransition(); }
            }
        }

        private string MobileInventoryFilterLabel { get { return inventoryFilter < 0 ? "全部" : GameBalance.SlotName((ItemSlot)inventoryFilter); } }
        private string MobileInventorySortLabel { get { return inventorySort == 1 ? "等级" : inventorySort == 2 ? "品质" : "评分"; } }
        private void CycleMobileInventoryFilter()
        { inventoryFilter = inventoryFilter >= 2 ? -1 : inventoryFilter + 1; mobileInventoryListScroll = Vector2.zero; BlockUITransition(); }
        private void CycleMobileInventorySort()
        { inventorySort = (inventorySort + 1) % 3; mobileInventoryListScroll = Vector2.zero; BlockUITransition(); }

        private bool DrawMobileInventoryList(MobilePanelLayout.Area viewport, bool wide)
        {
            float contentWidth = viewport.Width - 18, y = 8;
            bool equipped = mobileInventoryTab == 1;
            if (wide && !equipped) y += 56;
            int count = equipped ? 3 : bagItems.Count;
            for (int i = 0; i < count; i++)
            {
                ItemData item = equipped ? session.Progression.Equipped((ItemSlot)i) : bagItems[i];
                y += MobileInventoryRowHeight(item, contentWidth) + 8;
            }
            if (count == 0) y += 80;
            float u = TouchRatio;
            mobileInventoryListScroll = BeginTouchScroll("mobile-inventory-list", MobilePanelRect(viewport), mobileInventoryListScroll,
                new Rect(0, 0, contentWidth * u, Mathf.Max(viewport.Height, y) * u));
            y = 8; string selected = null; int filterAction = 0;
            if (wide && !equipped)
            {
                if (Button(MobilePanelRect(MobileCollectionLayout.Split(contentWidth, y, 0, 2)), MobileInventoryFilterLabel, jade)) filterAction = 1;
                if (Button(MobilePanelRect(MobileCollectionLayout.Split(contentWidth, y, 1, 2)), MobileInventorySortLabel, jade)) filterAction = 2;
                y += 56;
            }
            for (int i = 0; i < count; i++)
            {
                ItemData item = equipped ? session.Progression.Equipped((ItemSlot)i) : bagItems[i];
                float rowHeight = MobileInventoryRowHeight(item, contentWidth);
                float visibleTop = mobileInventoryListScroll.y / u;
                if (y + rowHeight < visibleTop || y > visibleTop + viewport.Height) { y += rowHeight + 8; continue; }
                Rect row = TouchRect(0, y, contentWidth, rowHeight);
                Color rarity = item == null ? muted : GameBalance.RarityColor(item.rarity);
                bool levelLocked = item != null && !ProgressionAttention.LevelEligible(session.Progression.Profile, item);
                Color availableRarity=levelLocked?Color.Lerp(rarity,new Color(.30f,.35f,.4f),.68f):rarity;
                if (Button(row, "", selectedItem == (item == null ? null : item.id) ? jade : availableRarity, item != null)) selected = item.id;
                Fill(TouchRect(0, y, 3, rowHeight), availableRarity);
                float at = y + 8;
                at += DrawMobileParagraph(10, at, contentWidth - 26, item == null ? GameBalance.SlotName((ItemSlot)i) + " · 空槽" : ItemTitle(MobileEquipmentPreview(item)), 16,
                    availableRarity, true);
                if (item != null)
                {
                    string status = levelLocked ? "需 " + item.level + " 级" : "Lv." + item.level;
                    at += DrawMobileParagraph(10, at, contentWidth - 26, status + " · " + (equipped ? "穿戴中" : GameBalance.SlotName(item.slot)) + (item.locked ? " · 已锁" : ""), 14, levelLocked ? gold : muted);
                    if(item.mechanic!=EquipmentMechanic.None)at+=DrawMobileParagraph(10,at,contentWidth-26,MechanicBadgePresentation.Title(item,session.Progression.Profile.heroClass),13,levelLocked?muted:gold,true);
                    DrawMobileParagraph(10, at, contentWidth - 26, "评分 " + MobileEquipmentScore(item).ToString("0.#") + (IsEquipmentUpgrade(item) ? "  ↑ 可提升" : ""), 14, levelLocked?muted:pale, true);
                    Badge(new Rect(row.xMax - 14 * u, row.y + 10 * u, 8 * u, 8 * u), IsEquipmentUpgrade(item) && !reviewedEquipment.Contains(item.id));
                }
                y += rowHeight + 8;
            }
            if (count == 0) DrawMobileParagraph(8, y + 8, contentWidth - 16, "这个分类暂无闲置装备", 16, muted);
            EndTouchScroll();
            if (filterAction != 0) { if (filterAction == 1) CycleMobileInventoryFilter(); else CycleMobileInventorySort(); return true; }
            if (selected != null)
            {
                selectedItem = selected; mobileInventoryDetail = true; mobileInventoryDetailScroll = Vector2.zero;
                mobileInventoryStatus = null;
                ReviewEquipment(session.Progression.Profile.inventory.Find(item => item != null && item.id == selected));
                BlockUITransition(); return true;
            }
            return false;
        }

        private float MobileInventoryRowHeight(ItemData item, float width)
        {
            if (item == null) return 64;
            if (mobileRowWidth != width || mobileRowRatio != TouchRatio)
            { mobileRowWidth = width; mobileRowRatio = TouchRatio; mobileRowHeights.Clear(); }
            float height;
            if (mobileRowHeights.TryGetValue(item.id, out height)) return height;
            string level = (item.level > session.Progression.Profile.level ? "需 " + item.level + " 级" : "Lv." + item.level) + " · ";
            string locked = item.locked ? " · 已锁" : "";
            height = 16 + MeasureMobileParagraph(ItemTitle(MobileEquipmentPreview(item)), width - 26, 16, true) +
                Mathf.Max(MeasureMobileParagraph(level + GameBalance.SlotName(item.slot) + locked, width - 26, 14), MeasureMobileParagraph(level + "穿戴中" + locked, width - 26, 14)) +
                MeasureMobileParagraph("评分 " + MobileEquipmentScore(item).ToString("0.#") + (IsEquipmentUpgrade(item) ? "  ↑ 可提升" : ""), width - 26, 14, true);
            if(item.mechanic!=EquipmentMechanic.None)height+=MeasureMobileParagraph(MechanicBadgePresentation.Title(item,session.Progression.Profile.heroClass),width-26,13,true);
            mobileRowHeights[item.id] = height;
            return height;
        }

        private bool DrawMobileInventoryDetail(MobilePanelLayout.Area viewport, ItemData item)
        {
            if (mobileDetailItem != (item == null ? null : item.id)) { mobileDetailItem = item == null ? null : item.id; mobileInventoryDetailScroll = Vector2.zero; }
            if(item!=null&&equipmentAppearanceOpen){DrawMobileEquipmentAppearance(MobilePanelRect(viewport),item,TouchRatio);return false;}
            float contentWidth = viewport.Width - 18;
            int ignored;
            float contentHeight = MobileItemDetailContent(item, contentWidth, false, out ignored);
            mobileInventoryDetailScroll = BeginTouchScroll("mobile-inventory-detail", MobilePanelRect(viewport), mobileInventoryDetailScroll,
                new Rect(0, 0, contentWidth * TouchRatio, Mathf.Max(viewport.Height, contentHeight) * TouchRatio));
            int action;
            MobileItemDetailContent(item, contentWidth, true, out action);
            EndTouchScroll();
            if (item == null || action == 0) return false;
            string id = item.id;
            if (action == 1)
            {
                bool locked = !item.locked;
                MobileInventoryResult(session.Progression.SetItemLocked(id, locked), locked ? "装备已锁定" : "装备已解锁");
            }
            else
            {
                SellInventoryItem(id);
                if(presetSaleOpen)return true;
                bool sold = !session.Progression.Profile.inventory.Exists(value => value != null && value.id == id);
                MobileInventoryResult(sold, "已出售 " + item.name, false, false);
            }
            BlockUITransition(); return true;
        }

        private float MobileItemDetailContent(ItemData item, float width, bool draw, out int action)
        {
            action = 0; float y = 8;
            string status = string.IsNullOrEmpty(mobileInventoryStatus) ? session.Progression.LastError : mobileInventoryStatus;
            if (!string.IsNullOrEmpty(status))
                y += MobileDetailParagraph(draw, 8, y, width - 16, status, 14,
                    mobileInventoryFailed || string.IsNullOrEmpty(mobileInventoryStatus) ? gold : jade, true) + 12;
            if (item == null) return y + MobileDetailParagraph(draw, 8, y, width - 16, "选择装备查看比较与操作", 16, muted) + 16;
            if(draw&&Button(TouchRect(8,y,width-16,48),"外观比较",jade)){equipmentAppearanceOpen=true;collectionOwner=session.Player;BlockUITransition();}
            y+=56;
            var progression = session.Progression;
            ItemData preview = MobileEquipmentPreview(item), current = progression.Equipped(item.slot);
            float available = width - 16;
            bool worn = IsEquipped(item), eligible = ProgressionAttention.LevelEligible(progression.Profile, item);
            Color rarity = GameBalance.RarityColor(item.rarity);
            y += MobileDetailParagraph(draw, 8, y, available, ItemTitle(preview)+" · "+progression.PresetReferences(item.id), 18, eligible ? rarity : muted, true) + 4;
            y += MobileDetailParagraph(draw, 8, y, available, GameBalance.RarityName(item.rarity) + " · " + GameBalance.SlotName(item.slot) + " · " + (eligible ? "Lv." + item.level : "需 " + item.level + " 级") + " · 部位 +" + progression.SlotUpgradeRank(item.slot), 14, eligible ? muted : gold) + 8;
            if (draw) DrawMobileEquipmentScores(8, y, available, current, preview);
            y += MobileCollectionLayout.ScoreHeight + 8;
            y += MobileDetailParagraph(draw, 8, y, available, MobileAttributeLine("攻击", current == null ? 0 : current.attack, preview.attack), 16, pale, true) + 3;
            y += MobileDetailParagraph(draw, 8, y, available, MobileAttributeLine("防御", current == null ? 0 : current.defense, preview.defense), 16, pale, true) + 3;
            y += MobileDetailParagraph(draw, 8, y, available, MobileAttributeLine("生命", current == null ? 0 : current.health, preview.health), 16, pale, true) + 8;
            y += MobileDetailParagraph(draw, 8, y, available, "评分不含机制价值 · 换装自动继承部位强化", 14, muted) + 8;
            y += MobileDetailParagraph(draw, 8, y, available, EquipmentComparisonPresentation.Changes(current,item,progression.Profile.heroClass), 15, gold, true) + 8;
            if(item.mechanic!=EquipmentMechanic.None)
            {
                y += MobileDetailParagraph(draw,8,y,available,MechanicBadgePresentation.Title(item,progression.Profile.heroClass),16,gold,true)+4;
                y += MobileDetailParagraph(draw,8,y,available,"收益 · "+MechanicBadgePresentation.Benefit(item,progression.Profile.heroClass),14,jade)+4;
                y += MobileDetailParagraph(draw,8,y,available,"代价 · "+MechanicBadgePresentation.Cost(item,progression.Profile.heroClass),14,gold)+8;
            }
            y += MobileDetailParagraph(draw, 8, y, available, "换装后机制：" + EquipmentComparisonPresentation.Description(item,progression.Profile.heroClass), 14, item.mechanic == EquipmentMechanic.None ? muted : gold) + 8;
            if (!worn && !EquipmentComparisonPresentation.SameMechanism(current,item,progression.Profile.heroClass))
                y += MobileDetailParagraph(draw, 8, y, available, "当前机制：" + EquipmentComparisonPresentation.Description(current,progression.Profile.heroClass), 14, muted) + 8;
            if (draw)
            {
                var lockArea = MobileCollectionLayout.Split(available, y, 0, 2);
                var saleArea = MobileCollectionLayout.Split(available, y, 1, 2);
                if (Button(TouchRect(8 + lockArea.X, y, lockArea.Width, lockArea.Height), item.locked ? "解锁装备" : "锁定装备", item.locked ? gold : jade)) action = 1;
                if (Button(TouchRect(8 + saleArea.X, y, saleArea.Width, saleArea.Height), worn ? "穿戴中不可售" : item.locked ? "已锁定不可售" : "出售 · " + progression.SellValue(item) + " 金", gold, !worn && !item.locked)) action = 2;
            }
            y += 56;
            y += MobileDetailParagraph(draw, 8, y, available, "强化绑定部位；同一强化等级的加成按每件装备自身基础属性计算。", 14, jade) + 8;
            return y;
        }

        private float MobileDetailParagraph(bool draw, float x, float y, float width, string value, int size, Color color, bool bold = false)
        { return draw ? DrawMobileParagraph(x, y, width, value, size, color, bold) : MeasureMobileParagraph(value, width, size, bold); }
        private static string MobileAttributeLine(string label, int current, int next)
        { int delta = next - current; return label + "  " + current + " → " + next + "   " + (delta == 0 ? "±0" : (delta > 0 ? "+" : "") + delta); }
        private static string MobileMechanicSummary(ItemData item)
        {
            if (item == null || item.mechanic == EquipmentMechanic.None) return "无特殊机制";
            string variant = BuildCatalog.HasMechanicVariant(item.mechanic) ? " · 当前变体 " + (item.mechanicVariant == 1 ? "B" : "A") : "";
            return BuildCatalog.MechanicName(item.mechanic) + variant + "\n" + BuildCatalog.MechanicDescription(item.mechanic);
        }
        private void DrawMobileEquipmentScores(float x, float y, float width, ItemData current, ItemData candidate)
        {
            float currentScore = ProgressionService.EquipmentScore(current), nextScore = ProgressionService.EquipmentScore(candidate), delta = nextScore - currentScore;
            for (int i = 0; i < 2; i++)
            {
                var half = MobileCollectionLayout.ScoreCard(width, i);
                Fill(TouchRect(x + half.X, y, half.Width, half.Height), i == 0 ? new Color(.035f, .075f, .1f) : new Color(.065f, .115f, .14f));
                Text(TouchRect(x + half.X + 8, y + 7, half.Width - 16, 21), i == 0 ? current == null ? "当前 · 空槽" : "当前评分" : "换装后评分", TouchFont(14), i == 0 ? muted : jade);
                Text(TouchRect(x + half.X + 8, y + 29, half.Width - 16, 30), (i == 0 ? currentScore : nextScore).ToString("0.#"), TouchFont(22), pale, true);
                if (i == 1) Text(TouchRect(x + half.X + 8, y + 59, half.Width - 16, 21), delta == 0 ? "±0" : (delta > 0 ? "+" : "") + delta.ToString("0.#"), TouchFont(14), delta < 0 ? new Color(1, .48f, .42f) : delta > 0 ? jade : muted, true);
            }
        }

        private void DrawMobileEquipmentActions(MobilePanelLayout layout, ItemData item, bool back)
        {
            int count = back ? 3 : 2, first = back ? 1 : 0;
            if (back && Button(MobilePanelRect(layout.FooterButton(0, count)), "返回列表", jade))
            { ClosePanel(); return; }
            bool canEquip = item != null && !IsEquipped(item) && ProgressionAttention.LevelEligible(session.Progression.Profile, item);
            string equip = item == null ? "选择装备" : IsEquipped(item) ? "已穿戴" : canEquip ? "穿戴" : "需要 " + item.level + " 级";
            if (Button(MobilePanelRect(layout.FooterButton(first, count)), equip, jade, canEquip, null, true))
            { MobileInventoryResult(session.Progression.Equip(item.id), "已装备 " + item.name); BlockUITransition(); return; }
            int rank = item == null ? 0 : session.Progression.SlotUpgradeRank(item.slot), cost = item == null ? 0 : session.Progression.UpgradeCost(item);
            bool capped = rank >= ProgressionService.MaximumUpgrade;
            if (Button(MobilePanelRect(layout.FooterButton(first + 1, count)), capped ? "部位已满级" : "强化 · " + cost + " 金", gold, item != null && !capped && session.Progression.Profile.gold >= cost))
            { MobileInventoryResult(session.Progression.Upgrade(item.id), GameBalance.SlotName(item.slot) + "部位强化 +" + (rank + 1)); BlockUITransition(); }
        }

        private void MobileInventoryResult(bool accepted, string message, bool supplies = false, bool notify = true)
        {
            string status = accepted ? message : string.IsNullOrEmpty(session.Progression.LastError) ? "操作未保存，请重试。" : session.Progression.LastError;
            if (supplies) { mobileSupplyStatus = status; mobileSupplyFailed = !accepted; }
            else { mobileInventoryStatus = status; mobileInventoryFailed = !accepted; }
            if (!accepted)
            {
                CancelMobileScroll();
                if (supplies) mobileSupplyScroll = Vector2.zero;
                else mobileInventoryDetailScroll = Vector2.zero;
            }
            if (notify) Feedback(accepted, status);
        }

        private void DrawMobileSupplies(MobilePanelLayout layout)
        {
            var p = session.Progression.Profile; var stats = session.Progression.GetStats();
            string summary = "生命药剂 × " + p.potions + "\n恢复 50% 最大生命" + (session.ChallengeRun && session.InDungeon ? "\n本局治疗充能 " + session.HealingCharges + "/3" : "");
            string attributes = "攻击 " + Mathf.RoundToInt(stats.Damage) + "   防御 " + Mathf.RoundToInt(stats.Armor) + "\n生命上限 " + Mathf.RoundToInt(stats.MaxHealth) + "   暴击 " + (stats.CritChance * 100).ToString("0.#") + "%\n当前护甲减伤 " + ((1 - CombatBalance.ArmorDamageMultiplier(stats.Armor, p.level)) * 100).ToString("0.#") + "%";
            float w = layout.TabbedBody.Width - 34;
            string status = string.IsNullOrEmpty(mobileSupplyStatus) ? session.Progression.LastError : mobileSupplyStatus;
            float statusHeight = string.IsNullOrEmpty(status) ? 0 : MeasureMobileParagraph(status, w, 14, true) + 12;
            float content = 32 + statusHeight + MeasureMobileParagraph(summary, w, 16, true) + MeasureMobileParagraph(attributes, w, 16);
            mobileSupplyScroll = BeginTouchScroll("mobile-inventory-supplies", MobilePanelRect(layout.TabbedBody), mobileSupplyScroll,
                new Rect(0, 0, (w + 16) * TouchRatio, Mathf.Max(layout.TabbedBody.Height, content) * TouchRatio));
            float y = 8;
            if (statusHeight > 0) y += DrawMobileParagraph(8, y, w, status, 14,
                mobileSupplyFailed || string.IsNullOrEmpty(mobileSupplyStatus) ? gold : jade, true) + 12;
            y += DrawMobileParagraph(8, y, w, summary, 16, pale, true) + 12;
            DrawMobileParagraph(8, y, w, attributes, 16, muted);
            EndTouchScroll();
            if (Button(MobilePanelRect(layout.FooterButton(0, 2)), "购买药剂 · " + ProgressionService.PotionPrice + " 金", gold, p.gold >= ProgressionService.PotionPrice))
            { MobileInventoryResult(session.Progression.BuyPotion(), "已购买生命药剂", true); BlockUITransition(); return; }
            Rect rewards = MobilePanelRect(layout.FooterButton(1, 2));
            bool openRewards = Button(rewards, "机制图鉴 / 待领取", jade);
            Badge(rewards, Attention.Rewards);
            if (openRewards) { panel = Panel.Camp; campTab = 1; BlockUITransition(); }
        }
    }
}
