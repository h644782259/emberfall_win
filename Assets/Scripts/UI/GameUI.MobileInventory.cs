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
            PrepareInventoryPopupInput();
            var progression = session.Progression;
            var profile = progression.Profile;
            if (mobileInventoryProfile != progression.CurrentSlotId || mobileInventoryStatusOwner != session.Player)
            {
                mobileInventoryProfile = progression.CurrentSlotId; mobileInventoryStatusOwner = session.Player;
                mobileInventoryStatus = mobileSupplyStatus = null;
                inventoryComparisonOpen=false;inventoryPopupItem=null;mobileInventoryDetail = false; mobileInventoryTab = 0;
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
            if(DrawMobilePanelChrome(layout,HubInventoryTitle,"",showNotice:false))return;
            {
                float y=layout.Header.Y+31;
                Text(TouchRect(layout.Header.X,y,40,20),"Lv."+profile.level,TouchFont(12),pale,true);
                DrawPrice(TouchRect(layout.Header.X+46,y,90,20),profile.gold,false,TouchRatio);
                DrawIcon(TouchRect(layout.Header.X+144,y+1,18,18),UIIconAtlas.Utility("bag"),jade);
                Text(TouchRect(layout.Header.X+166,y,90,20),profile.inventory.Count+"/"+ProgressionService.InventoryCapacity,TouchFont(12),pale,true);
                if(!string.IsNullOrEmpty(session.Notification))
                    Text(TouchRect(layout.Header.X+264,y,Mathf.Max(0,layout.Header.Width-264),20),PlatformText(session.Notification),TouchFont(12),gold,false,false,TextAnchor.MiddleLeft);
            }
            float leftWidth=Mathf.Clamp(layout.Body.Width*.28f,156,232);
            var wear=new MobilePanelLayout.Area(layout.Body.X,layout.Body.Y,leftWidth,layout.Height-layout.Body.Y-12);
            var bag=new MobilePanelLayout.Area(wear.XMax+12,layout.Body.Y,layout.Body.Width-leftWidth-12,wear.Height);
            DrawCurrentWear(MobilePanelRect(wear),TouchRatio);
            if(inventoryFashionOpen){mobileInventoryTab=3;inventoryFashionOpen=false;inventoryComparisonOpen=false;}
            Rect equipmentTab=MobilePanelRect(new MobilePanelLayout.Area(bag.X,bag.Y,52,44));
            Rect supplyTab=MobilePanelRect(new MobilePanelLayout.Area(bag.X+58,bag.Y,52,44));
            if(QuietAction(equipmentTab,"装备",true,null,mobileInventoryTab==0))SelectInventoryTab(0);
            if(QuietAction(supplyTab,"补给",true,null,mobileInventoryTab==2))SelectInventoryTab(2);
            if(QuietAction(MobilePanelRect(new MobilePanelLayout.Area(bag.X+116,bag.Y,52,44)),"时装",true,null,mobileInventoryTab==3))SelectInventoryTab(3);
            var content=new MobilePanelLayout.Area(bag.X,bag.Y+48,bag.Width,bag.Height-48);
            if(mobileInventoryTab==3){DrawBagFashion(content);return;}
            if(mobileInventoryTab==2)
            {
                DrawBagSupplies(content);
                return;
            }
            if(DrawMobileEquipmentGrid(content,false))return;

        }

        private string MobileInventoryFilterLabel { get { return inventoryFilter < 0 ? "全部" : GameBalance.SlotName((ItemSlot)inventoryFilter); } }
        private string MobileInventorySortLabel { get { return inventorySort == 1 ? "等级" : inventorySort == 2 ? "品质" : "评分"; } }


        private float MobileInventoryRowHeight(ItemData item, float width)
        {
            if (item == null) return 64;
            if (mobileRowWidth != width || mobileRowRatio != TouchRatio)
            { mobileRowWidth = width; mobileRowRatio = TouchRatio; mobileRowHeights.Clear(); }
            float height;
            if (mobileRowHeights.TryGetValue(item.id, out height)) return height;
            string level = (item.level > session.Progression.Profile.level ? "需 " + item.level + " 级" : "Lv." + item.level) + " · ";
            string locked = item.locked ? " · 已锁" : "";
            height = 16 + MeasureMobileParagraph(ItemTitle(MobileEquipmentPreview(item)), width - 60, 16, true) +
                Mathf.Max(MeasureMobileParagraph(level + GameBalance.SlotName(item.slot) + locked, width - 26, 14), MeasureMobileParagraph(level + "穿戴中" + locked, width - 26, 14)) +
                MeasureMobileParagraph("评分 " + MobileEquipmentScore(item).ToString("0.#") + (IsEquipmentUpgrade(item) ? "  ↑ 可提升" : ""), width - 26, 14, true);
            if(item.mechanic!=EquipmentMechanic.None)height+=MeasureMobileParagraph(MechanicBadgePresentation.Title(item,session.Progression.Profile.heroClass),width-26,13,true);
            mobileRowHeights[item.id] = height;
            return height;
        }

        private bool DrawMobileInventoryDetail(MobilePanelLayout.Area viewport, ItemData item)
        {
            if (mobileDetailItem != (item == null ? null : item.id)) { mobileDetailItem = item == null ? null : item.id; mobileInventoryDetailScroll = Vector2.zero; }
            float contentWidth = viewport.Width - 18;
            int ignored;
            float contentHeight = MobileItemDetailContent(item, contentWidth, false, out ignored);
            mobileInventoryDetailScroll = BeginTouchScroll("mobile-inventory-detail", MobilePanelRect(viewport), mobileInventoryDetailScroll,
                new Rect(0, 0, contentWidth * TouchRatio, Mathf.Max(viewport.Height, contentHeight) * TouchRatio));
            int action;
            MobileItemDetailContent(item, contentWidth, true, out action);
            EndTouchScroll();
            return false;
        }

        private float MobileItemDetailContent(ItemData item, float width, bool draw, out int action)
        {
            action = 0; float y = 8;
            string status = string.IsNullOrEmpty(mobileInventoryStatus) ? session.Progression.LastError : mobileInventoryStatus;
            if (!string.IsNullOrEmpty(status))
                y += MobileDetailParagraph(draw, 8, y, width - 16, status, 14,
                    mobileInventoryFailed || string.IsNullOrEmpty(mobileInventoryStatus) ? gold : jade, true) + 12;
            if (item == null) return y + MobileDetailParagraph(draw, 8, y, width - 16, "选择装备查看比较与操作", 16, muted) + 16;
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
            y += MobileDetailParagraph(draw, 8, y, available, "换装继承部位强化", 14, muted) + 8;
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

    }
}
