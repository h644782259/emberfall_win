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
            const float statusWidth=284;
            if(DrawMobilePanelChrome(layout,HubInventoryTitle,"",canClose:!inventoryComparisonOpen,showNotice:false,headerRightReserve:statusWidth))return;
            {
                float x=layout.Close.X-statusWidth-8,y=layout.Header.Y+4;
                Text(TouchRect(x,y,66,28),"Lv."+profile.level,TouchFont(13),pale,true,false,TextAnchor.MiddleLeft);
                DrawPrice(TouchRect(x+72,y,108,28),profile.gold,false,TouchRatio);
                DrawIcon(TouchRect(x+186,y+4,20,20),UIIconAtlas.Utility("bag"),jade);
                Text(TouchRect(x+212,y,72,28),profile.inventory.Count+"/"+ProgressionService.InventoryCapacity,TouchFont(13),pale,true,false,TextAnchor.MiddleLeft);
            }
            float leftWidth=Mathf.Clamp(layout.Body.Width*.30f,200,260);
            var wear=new MobilePanelLayout.Area(layout.Body.X,layout.Body.Y,leftWidth,layout.Height-layout.Body.Y-12);
            var bag=new MobilePanelLayout.Area(wear.XMax+12,layout.Body.Y,layout.Body.Width-leftWidth-12,wear.Height);
            bool inlineStats=MobileControls.IsIPad&&wear.Height>=480;
            if(inlineStats)
            {
                float previewHeight=Mathf.Min(wear.Width*1.4f+74,wear.Height-344);
                DrawCurrentWear(TouchRect(wear.X,wear.Y,wear.Width,previewHeight),TouchRatio);
                DrawCharacterStats(TouchRect(wear.X,wear.Y+previewHeight+12,wear.Width,wear.Height-previewHeight-12),TouchRatio);
            }
            else
            {
                if(TabButton(TouchRect(wear.X,wear.Y,(wear.Width-6)*.5f,40),"角色",!inventoryStatsVisible))inventoryStatsVisible=false;
                if(TabButton(TouchRect(wear.X+(wear.Width+6)*.5f,wear.Y,(wear.Width-6)*.5f,40),"属性",inventoryStatsVisible))inventoryStatsVisible=true;
                var leftBody=TouchRect(wear.X,wear.Y+44,wear.Width,Mathf.Max(64,wear.Height-44));
                if(inventoryStatsVisible)DrawCharacterStats(leftBody,TouchRatio);else DrawCurrentWear(leftBody,TouchRatio);
            }
            if(inventoryFashionOpen){mobileInventoryTab=3;inventoryFashionOpen=false;inventoryComparisonOpen=false;}
            Rect equipmentTab=MobilePanelRect(new MobilePanelLayout.Area(bag.X,bag.Y,52,44));
            Rect supplyTab=MobilePanelRect(new MobilePanelLayout.Area(bag.X+58,bag.Y,52,44));
            if(QuietAction(equipmentTab,"装备",!inventoryComparisonOpen,null,mobileInventoryTab==0))SelectInventoryTab(0);
            if(QuietAction(supplyTab,"道具",!inventoryComparisonOpen,null,mobileInventoryTab==2))SelectInventoryTab(2);
            if(QuietAction(MobilePanelRect(new MobilePanelLayout.Area(bag.X+116,bag.Y,52,44)),"时装",!inventoryComparisonOpen,null,mobileInventoryTab==3))SelectInventoryTab(3);
            var stats=progression.GetStats();
            float summaryWidth=bag.Width-188;
            if(summaryWidth>=240&&!inlineStats)
            {
                Text(TouchRect(bag.X+188,bag.Y+4,summaryWidth,36),"攻击 "+Mathf.RoundToInt(stats.Damage)+"   防御 "+Mathf.RoundToInt(stats.Armor)+"   生命 "+Mathf.RoundToInt(stats.MaxHealth),TouchFont(12),muted,false,false,TextAnchor.MiddleRight);
            }
            var content=new MobilePanelLayout.Area(bag.X,bag.Y+48,bag.Width,bag.Height-48);
            if(mobileInventoryTab==3){DrawBagFashion(content);return;}
            if(mobileInventoryTab==2)
            {
                DrawBagSupplies(content);
                return;
            }
            var grid=new InventoryGridGeometry(content.Width-InventoryGridGeometry.FilterRailWidth-18,InventoryGridGeometry.MobileCellSize);
            float filledRows=((bagItems.Count+grid.Columns-1)/grid.Columns)*grid.Stride+4;
            float gridHeight=Mathf.Max(176,filledRows);
            bool overview=picked!=null&&bagItems.Count>0&&content.Height-gridHeight>=174;
            var gridArea=overview?new MobilePanelLayout.Area(content.X,content.Y,content.Width,gridHeight):content;
            if(DrawMobileEquipmentGrid(gridArea,false))return;
            if(overview&&!inventoryComparisonOpen)
                DrawInventoryOverview(new MobilePanelLayout.Area(content.X,content.Y+gridHeight+8,content.Width-InventoryGridGeometry.FilterRailWidth,166),picked);

        }

        private void DrawInventoryOverview(MobilePanelLayout.Area area,ItemData item)
        {
            float u=TouchRatio;Rect r=MobilePanelRect(area);
            Surface(r,new Color(.012f,.022f,.034f,1));SurfaceFrame(r,new Color(jade.r,jade.g,jade.b,.18f));
            ItemData preview=MobileEquipmentPreview(item),current=session.Progression.Equipped(item.slot);
            Text(TouchRect(area.X+12,area.Y+8,area.Width-164,28),ItemTitle(preview),TouchFont(16),GameBalance.RarityColor(item.rarity),true,false,TextAnchor.MiddleLeft);
            if(NavigationButton(TouchRect(area.XMax-148,area.Y+4,136,44),"详情",jade))
                OpenInventoryPopup(item.id,r);
            DrawMobileEquipmentScores(area.X+12,area.Y+48,area.Width-24,current,preview);
            Text(TouchRect(area.X+12,area.Y+138,area.Width-24,24),"攻击 "+preview.attack.ToString("0")+"   防御 "+preview.defense.ToString("0")+"   生命 "+preview.health.ToString("0"),TouchFont(13),pale,false,false,TextAnchor.MiddleLeft);
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
            string level = (item.level > session.Progression.Profile.level ? "需 " + item.level + " 级" : "Lv" + item.level) + " · ";
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
            var progression = session.Progression;
            ItemData preview = MobileEquipmentPreview(item), current = progression.Equipped(item.slot);
            float available = width - 16;
            bool worn = IsEquipped(item), eligible = ProgressionAttention.LevelEligible(progression.Profile, item);
            Color rarity = GameBalance.RarityColor(item.rarity);
            y += MobileDetailParagraph(draw, 8, y, available, ItemTitle(preview), 18, eligible ? rarity : muted, true) + 4;
            y += MobileDetailParagraph(draw, 8, y, available, GameBalance.RarityName(item.rarity) + " · " + GameBalance.SlotName(item.slot) + " · " + (eligible ? "Lv" + item.level : "需 " + item.level + " 级") + " · 部位 +" + progression.SlotUpgradeRank(item.slot), 14, eligible ? muted : gold) + 8;
            if (draw) DrawMobileEquipmentScores(8, y, available, current, preview);
            y += MobileCollectionLayout.ScoreHeight + 8;
            y += MobileDetailParagraph(draw, 8, y, available, MobileAttributeLine("攻击", current == null ? 0 : current.attack, preview.attack), 16, pale, true) + 3;
            y += MobileDetailParagraph(draw, 8, y, available, MobileAttributeLine("防御", current == null ? 0 : current.defense, preview.defense), 16, pale, true) + 3;
            y += MobileDetailParagraph(draw, 8, y, available, MobileAttributeLine("生命", current == null ? 0 : current.health, preview.health), 16, pale, true) + 8;
            y += MobileDetailParagraph(draw, 8, y, available, EquipmentComparisonPresentation.Changes(current,item,progression), 15, gold, true) + 8;
            y += MobileDetailParagraph(draw,8,y,available,EquipmentComparisonPresentation.Description(item,progression),14,jade)+8;

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
                Surface(TouchRect(x + half.X, y, half.Width, half.Height), i == 0 ? new Color(.018f, .032f, .048f) : new Color(.02f, .065f, .066f));
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
