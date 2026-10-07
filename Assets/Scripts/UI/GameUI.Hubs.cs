using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private bool travelReturnPause;
        private string travelError;
        private HubNpcKind inventoryHubNpc;

        private static string HubNpcLabel(HubNpcKind kind)
        {
            return kind == HubNpcKind.Merchant ? "商人 · 药剂 / 出售" :
                kind == HubNpcKind.Blacksmith ? "铁匠 · 部位强化" :
                kind == HubNpcKind.Exchange ? "观星员 · 星路 / 兑换" : "营地工坊";
        }

        private static string HubNpcMobileLabel(HubNpcKind kind)
        { return kind == HubNpcKind.Merchant ? "商人交易" : kind == HubNpcKind.Blacksmith ? "铁匠强化" : "星路 / 兑换"; }

        private void OpenNearbyHubNpc()
        {
            if (UITransitionBlocked || session == null || session.InputBlocked || panel != Panel.None) return;
            HubNpcKind kind = session.NearbyHubNpc;
            if (kind == HubNpcKind.None) return;
            CancelHotbarPointer();
            inventoryHubNpc = kind;
            panel=Panel.HubDialogue;
            session.SetUIBlocking(true);
            session.BeginHubNpcConversation(kind);
            BlockUITransition();
        }

        private void OpenHubNpcService()
        {
            if(UITransitionBlocked||session==null||session.Paused||session.BackgroundPaused||panel!=Panel.HubDialogue)return;
            HubNpcKind kind=session.ActiveHubNpc;
            if(kind==HubNpcKind.None){ClosePanel();return;}
            if (kind == HubNpcKind.Exchange)
            {
                OpenChapterSelection();
                return;
            }
            else
            {
                panel = Panel.Inventory;
                hubServiceScroll=Vector2.zero;inventoryFilter=-1;
                mobileInventoryNpcRequest=HubNpcKind.None;
                if (kind == HubNpcKind.Blacksmith) selectedItem = session.Progression.Profile.weaponId;
            }
            session.SetUIBlocking(true);
            BlockUITransition();
        }

        private void HandleHubNpcShortcut(bool pressed)
        {
            if(!pressed||MobileControls.Active)return;
            if(panel==Panel.HubDialogue)OpenHubNpcService();else OpenNearbyHubNpc();
        }

        private void ReconcileHubNpcConversation()
        {
            if(session.ActiveHubNpc==HubNpcKind.None)
            {if(panel==Panel.HubDialogue)ClosePanel();return;}
            bool service=session.ActiveHubNpc==HubNpcKind.Exchange?
                panel==Panel.Chapter||panel==Panel.Camp:panel==Panel.Inventory;
            if(panel!=Panel.HubDialogue&&!service)session.EndHubNpcConversation();
        }

        private void DrawHubNpcDialogue()
        {
            HubNpcKind kind=session.ActiveHubNpc;
            if(kind==HubNpcKind.None)return;
            float u=MobileControls.Active?TouchRatio:1;
            float w=Mathf.Min(width-32*u,620*u),h=148*u;
            Rect r=new Rect((width-w)*.5f,height-h-18*u,w,h);
            blockedRects.Add(r);
            Box(r,gold);
            Text(new Rect(r.x+18*u,r.y+12*u,w-82*u,25*u),HubNpcLabel(kind),Mathf.RoundToInt(18*u),gold,true);
            Text(new Rect(r.x+18*u,r.y+43*u,w-36*u,42*u),HubNpcGreeting.Text(kind),Mathf.RoundToInt(15*u),pale,false,true);
            string action=kind==HubNpcKind.Merchant?"查看补给 / 出售":kind==HubNpcKind.Blacksmith?"打磨 / 强化装备":"查看星路 / 兑换";
            if(NavigationButton(new Rect(r.x+18*u,r.y+96*u,w-138*u,38*u),(MobileControls.Active?"":"[G] ")+action,jade))OpenHubNpcService();
            if(NavigationButton(new Rect(r.xMax-106*u,r.y+96*u,88*u,38*u),"告辞",muted))ClosePanel();
        }

        private string HubNpcServiceSubtitle(string fallback)
        {return session.ActiveHubNpc==HubNpcKind.None?fallback:HubNpcGreeting.Text(session.ActiveHubNpc);}

        private string HubInventoryTitle
        {
            get
            {
                if (session.InDungeon || session.NearbyHubNpc != inventoryHubNpc) return "行囊与装备";
                return inventoryHubNpc == HubNpcKind.Merchant ? "商人 · 行囊与补给" :
                    inventoryHubNpc == HubNpcKind.Blacksmith ? "铁匠 · 装备与强化" : "行囊与装备";
            }
        }

        private string HubInventoryHint
        {
            get
            {
                if (session.InDungeon || session.NearbyHubNpc != inventoryHubNpc) return "";
                return inventoryHubNpc == HubNpcKind.Merchant ? "下方购买药剂 · 背包右侧出售闲置装备" :
                    inventoryHubNpc == HubNpcKind.Blacksmith ? "右侧强化装备部位 · 换装继承部位等级" : "";
            }
        }

        private void OpenTravelMap()
        {
            if (UITransitionBlocked || session == null || !session.HasStarted || session.IsDead || exitRequest.Open || saveFlow.Open) return;
            travelReturnPause = session.Paused;
            travelError = null;
            CancelHotbarPointer();
            panel = Panel.TravelMap;
            session.SetUIBlocking(true);
            session.SetPaused(false);
            BlockUITransition();
        }

        private bool CloseTravelMap()
        {
            if (panel != Panel.TravelMap) return false;
            panel = Panel.None;
            session.SetUIBlocking(false);
            session.SetPaused(travelReturnPause);
            travelReturnPause = false;
            BlockUITransition();
            return true;
        }

        private void DrawHubActions(float x, float y)
        {
            Rect travel = new Rect(x, y, 222, 40);
            blockedRects.Add(travel);
            if (NavigationButton(travel, "城镇旅行地图", jade)) OpenTravelMap();
            HubNpcKind nearby = session.NearbyHubNpc;
            if (nearby == HubNpcKind.None) return;
            Rect interaction = new Rect(x, y - 50, 222, 42);
            blockedRects.Add(interaction);
            if (NavigationButton(interaction, "[G] " + HubNpcLabel(nearby), gold)) OpenNearbyHubNpc();
        }

        private void DrawTravelMap()
        {
            bool mobile = MobileControls.Active;
            float u = mobile ? TouchRatio : 1.35f;
            float w = 520 * u, h = 306 * u;
            Rect r = new Rect((width - w) * .5f, (height - h) * .5f, w, h);
            Fill(new Rect(0, 0, width, height), new Color(.01f, .025f, .045f, .96f));
            Box(r, jade);
            Text(new Rect(r.x + 16*u, r.y + 10*u, 488*u, 28*u), "城镇旅行 · " + HubTravelRules.Name(session.CurrentHub),
                Mathf.RoundToInt(21*u), pale, true);
            string hint = !string.IsNullOrEmpty(travelError) ? travelError : !session.CanOpenTravelMap ?
                "挑战中或附近有敌人时不能旅行，请先安全返回营地。" : "免费旅行 · 商人、铁匠、兑换员提供相同服务 · 装备与货币保留";
            Text(new Rect(r.x + 16*u, r.y + 40*u, 488*u, 30*u), hint, Mathf.RoundToInt(11*u),
                !string.IsNullOrEmpty(travelError) ? gold : muted, false, true);
            GameProfile profile = session.Progression.Profile;
            int mask = HubTravelRules.UnlockedMask(profile.unlockedHubMask, profile.level, profile.clearedRuns);
            // The connecting road is a travel diagram, not a claim about world distances.
            Fill(new Rect(r.x + 60*u, r.y + 95*u, 400*u, 2*u), jade * .5f);
            for (int hub = 0; hub < HubTravelRules.Count; hub++)
            {
                bool unlocked = HubTravelRules.IsUnlocked(mask, hub), current = hub == session.CurrentHub;
                Color tint = hub == 0 ? jade : hub == 1 ? new Color(.95f,.52f,.32f) : new Color(.56f,.66f,1);
                float x = r.x + (16 + hub * 166)*u;
                Rect cardRect = new Rect(x, r.y + 76*u, 156*u, 164*u);
                Fill(cardRect, card); Border(cardRect, unlocked ? tint : muted*.4f);
                Fill(new Rect(x + 68*u, r.y + 86*u, 20*u, 20*u), current ? gold : unlocked ? tint : muted*.4f);
                Text(new Rect(x + 6*u, r.y + 114*u, 144*u, 25*u), HubTravelRules.Name(hub), Mathf.RoundToInt(16*u), unlocked ? pale : muted, true, false, TextAnchor.MiddleCenter);
                Text(new Rect(x + 7*u, r.y + 143*u, 142*u, 31*u), unlocked ? "商人 / 铁匠 / 兑换员" : HubTravelRules.UnlockHint(hub),
                    Mathf.RoundToInt(11*u), muted, false, true, TextAnchor.MiddleCenter);
                if (NavigationButton(new Rect(x + 8*u, r.y + 183*u, 140*u, 48*u), current ? "当前城镇" : unlocked ? "前往" : "尚未解锁", tint, unlocked && !current && session.CanOpenTravelMap && !UITransitionBlocked))
                {
                    if (session.TravelToHub(hub))
                    {
                        travelReturnPause = false;
                        CloseTravelMap();
                        return;
                    }
                    travelError = string.IsNullOrEmpty(session.Progression.LastError) ? "旅行未完成，请确认已安全返回营地后重试。" : session.Progression.LastError;
                    BlockUITransition();
                }
            }
            if (NavigationButton(new Rect(r.x + 16*u, r.y + 250*u, 488*u, 48*u), travelReturnPause ? "返回暂停菜单" : "返回冒险", jade)) CloseTravelMap();
        }
    }
}
