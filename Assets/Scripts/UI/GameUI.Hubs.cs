using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private bool travelReturnPause;
        private string travelError;
        private HubNpcKind inventoryHubNpc;

        private bool HubServicesAvailable {get{return session!=null&&session.HasStarted&&!session.IsDead&&!session.InDungeon&&!session.PracticeActive;}}

        private void OpenHubService(HubNpcKind kind)
        {
            if(UITransitionBlocked||!HubServicesAvailable||session.InputBlocked)return;
            if(kind!=HubNpcKind.Merchant&&kind!=HubNpcKind.Blacksmith)return;
            CancelHotbarPointer();
            inventoryHubNpc = kind;
            panel=Panel.Inventory;
            hubServiceScroll=Vector2.zero;inventoryFilter=-1;mobileInventoryNpcRequest=HubNpcKind.None;
            if(kind==HubNpcKind.Blacksmith)smithPreviewMechanic=EquipmentMechanic.None;
            if(kind==HubNpcKind.Merchant){bool first=session.Progression.Profile.pendingFirstClearReward&&!session.Progression.Profile.firstClearRewardClaimed;if(first)merchantGemRarity=Rarity.Epic;SelectMerchantMode(first?1:0,true);}
            if(kind==HubNpcKind.Blacksmith)selectedItem=session.Progression.Profile.weaponId;
            session.SetUIBlocking(true);
            BlockUITransition();
        }

        private string HubNpcServiceSubtitle(string fallback) {return fallback;}

        private string HubInventoryTitle
        {
            get
            {
                if (!HubServicesAvailable) return "行囊";
                return inventoryHubNpc == HubNpcKind.Merchant ? "商人 · 行囊与补给" :
                    inventoryHubNpc == HubNpcKind.Blacksmith ? "铁匠 · 装备与强化" : "行囊";
            }
        }

        private string HubInventoryHint
        {
            get
            {
                if (!HubServicesAvailable) return "";
                return inventoryHubNpc == HubNpcKind.Merchant ? "下方购买药剂 · 背包右侧出售闲置装备" :
                    inventoryHubNpc == HubNpcKind.Blacksmith ? "右侧强化装备部位 · 换装继承部位等级" : "";
            }
        }

        private void OpenTravelMap()
        {
            if (UITransitionBlocked || session == null || !session.HasStarted || session.IsDead || exitRequest.Open || saveFlow.Open) return;
            travelReturnPause = session.Paused;
            travelError = null;travelMapTab=0;
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

        private int travelMapTab;
        private void DrawTravelMap()
        {
            float u=MobileControls.Active?TouchRatio:1.35f;
            float w=Mathf.Min(700,width/u-24),h=Mathf.Min(450,height/u-24);
            Rect r=new Rect((width-w*u)*.5f,(height-h*u)*.5f,w*u,h*u);
            blockedRects.Add(r);Box(r,jade);
            Text(new Rect(r.x+16*u,r.y+10*u,r.width-76*u,30*u),"地图 · "+session.ZoneName,Mathf.RoundToInt(20*u),pale,true);
            Rect close=new Rect(r.xMax-52*u,r.y+6*u,44*u,44*u);

            if(PopupCloseButton(close,!UITransitionBlocked)){CloseTravelMap();return;}
            if(PauseSidebarTab(new Rect(r.x+16*u,r.y+50*u,120*u,44*u),"当前地图",travelMapTab==0,u))travelMapTab=0;
            if(PauseSidebarTab(new Rect(r.x+144*u,r.y+50*u,120*u,44*u),"城镇旅行",travelMapTab==1,u))travelMapTab=1;
            Rect body=new Rect(r.x+16*u,r.y+102*u,r.width-32*u,r.height-114*u);
            if(travelMapTab==0){DrawExpandedMap(body,u);return;}
            string hint=!string.IsNullOrEmpty(travelError)?travelError:!session.CanOpenTravelMap?"挑战中或正在战斗，暂不可旅行。":"";
            Text(new Rect(body.x,body.y,body.width,28*u),hint,Mathf.RoundToInt(11*u),string.IsNullOrEmpty(travelError)?muted:gold,false,true);
            var profile=session.Progression.Profile;int mask=HubTravelRules.UnlockedMask(profile.unlockedHubMask,profile.level,profile.clearedRuns);
            float cardWidth=(body.width-16*u)/3;
            for(int hub=0;hub<HubTravelRules.Count;hub++)
            {
                bool unlocked=HubTravelRules.IsUnlocked(mask,hub),current=hub==session.CurrentHub;
                Rect tile=new Rect(body.x+hub*(cardWidth+8*u),body.y+32*u,cardWidth,body.height-32*u);
                Fill(tile,card);Border(tile,current?gold:jade);
                Text(new Rect(tile.x+6*u,tile.y+8*u,tile.width-12*u,26*u),HubTravelRules.Name(hub),Mathf.RoundToInt(14*u),unlocked?pale:muted,true,false,TextAnchor.MiddleCenter);
                Text(new Rect(tile.x+8*u,tile.y+38*u,tile.width-16*u,36*u),unlocked?"":HubTravelRules.UnlockHint(hub),Mathf.RoundToInt(11*u),muted,false,true,TextAnchor.MiddleCenter);
                DrawTownPostcard(new Rect(tile.x+8*u,tile.y+78*u,tile.width-16*u,Mathf.Max(32*u,tile.height-138*u)),hub,unlocked);
                if(Button(new Rect(tile.x+6*u,tile.yMax-50*u,tile.width-12*u,44*u),current?"当前城镇":unlocked?"前往":"尚未解锁",jade,unlocked&&!current&&session.CanOpenTravelMap&&!UITransitionBlocked))
                {
                    if(session.TravelToHub(hub)){travelReturnPause=false;CloseTravelMap();return;}
                    travelError=string.IsNullOrEmpty(session.Progression.LastError)?"旅行未完成，请安全返回营地后重试。":session.Progression.LastError;
                }
            }
        }

        private void DrawTownPostcard(Rect area,int hub,bool unlocked)
        {
            GUI.BeginGroup(area);
            float w=area.width,h=area.height;
            Color sky=hub==0?new Color(.10f,.24f,.28f):hub==1?new Color(.37f,.16f,.13f):new Color(.07f,.10f,.25f);
            Color light=hub==0?new Color(.65f,.83f,.63f):hub==1?new Color(1,.66f,.32f):new Color(.65f,.75f,1);
            for(int y=0;y<48;y++)Fill(new Rect(0,y*h/48,w,h/48+1),Color.Lerp(sky,light*.62f,y/48f));
            DrawIcon(new Rect(w*.72f,h*.12f,w*.15f,w*.15f),UIIconAtlas.ControlDisc(),light);
            for(int layer=0;layer<3;layer++)for(int x=0;x<80;x++)
            {
                float ridge=(.48f+layer*.12f+Mathf.Sin(x*.10f+hub+layer)*.075f+Mathf.Sin(x*.24f+layer)*.035f)*h;
                Fill(new Rect(x*w/80,ridge,w/80+1,h-ridge),Color.Lerp(sky,new Color(.025f,.07f,.09f),layer*.32f));
            }
            if(hub==0)
            {
                for(int i=0;i<7;i++)
                {
                    float x=(.04f+i*.145f)*w,baseY=h*(.78f+(i%2)*.13f),th=h*(.27f+(i%3)*.07f);
                    Fill(new Rect(x-2,baseY-th*.35f,4,th*.45f),new Color(.30f,.21f,.13f));
                    for(int row=0;row<24;row++){float span=(row/24f)*w*.12f;Fill(new Rect(x-span*.5f,baseY-th+row*th/24,span,th/24+1),new Color(.10f,.28f,.21f));}
                }
                for(int i=0;i<2;i++)
                {
                    float x=w*(.3f+i*.31f),baseY=h*.86f;
                    for(int row=0;row<28;row++){float span=row/28f*w*.31f;Fill(new Rect(x-span*.5f,baseY-h*.28f+row*h*.28f/28,span,h*.28f/28+1),i==0?new Color(.78f,.64f,.39f):new Color(.42f,.59f,.54f));}
                    Fill(new Rect(x-4,baseY-h*.11f,8,h*.11f),sky);
                }
            }
            else if(hub==1)
            {
                for(int i=0;i<4;i++)
                {
                    float x=w*(.12f+i*.22f),bh=h*(.20f+(i%2)*.10f),bw=w*.20f;
                    Fill(new Rect(x,h*.83f-bh,bw,bh),new Color(.40f,.22f,.15f));
                    Fill(new Rect(x-3,h*.83f-bh-5,bw+6,7),new Color(.67f,.35f,.20f));
                    Fill(new Rect(x+bw*.35f,h*.83f-bh*.7f,bw*.3f,bh*.3f),light);
                }
                Fill(new Rect(w*.69f,h*.35f,w*.04f,h*.31f),new Color(.30f,.20f,.16f));
                for(int i=0;i<3;i++)DrawIcon(new Rect(w*.66f+i*w*.018f,h*(.19f+i*.05f),w*.11f,w*.11f),UIIconAtlas.ControlDisc(),new Color(.45f,.32f,.27f,.55f));
            }
            else
            {
                for(int i=0;i<5;i++)
                {
                    float x=w*(.12f+i*.18f),bh=h*(i==2?.58f:.28f+(i%2)*.12f),bw=w*.13f;
                    Fill(new Rect(x,h*.86f-bh,bw,bh),new Color(.29f,.36f,.48f));
                    for(int row=0;row<14;row++){float span=row/14f*bw*1.4f;Fill(new Rect(x+bw*.5f-span*.5f,h*.86f-bh-h*.12f+row*h*.12f/14,span,h*.12f/14+1),new Color(.35f,.48f,.72f));}
                    Fill(new Rect(x+bw*.4f,h*.86f-bh*.7f,bw*.2f,bh*.4f),light);
                }
                for(int i=0;i<13;i++)Fill(new Rect((.06f+Mathf.Repeat(i*.273f,.88f))*w,(.06f+Mathf.Repeat(i*.113f,.33f))*h,2,2),light);
            }
            Fill(new Rect(0,h*.91f,w,h*.09f),new Color(.025f,.055f,.07f));
            if(!unlocked)Fill(new Rect(0,0,w,h),new Color(.04f,.05f,.06f,.6f));
            GUI.EndGroup();Border(area,unlocked?jade*.4f:muted*.3f);
        }

        private void DrawExpandedMap(Rect body,float u)
        {
            float mapWidth=body.width-132*u,size=Mathf.Min(mapWidth,body.height);
            Rect map=new Rect(body.x+(mapWidth-size)*.5f,body.y+(body.height-size)*.5f,size,size);
            Fill(map,ink);DrawMinimapTerrain(map);Border(map,jade);
            Color portal=new Color(.4f,.65f,1),camp=new Color(.9f,.9f,.85f),ordinary=new Color(.54f,.77f,.5f),aggro=new Color(1,.58f,.35f),boss=new Color(1,.32f,.3f);
            if(!session.InDungeon){MapDot(map,new Vector3(0,0,11),portal,7*u);MapDot(map,new Vector3(0,0,-10),camp,6*u);}
            else if(session.DungeonReturnAvailable)MapDot(map,session.DungeonReturnPosition,portal,7*u);
            foreach(var enemy in session.Enemies)if(enemy!=null&&!enemy.IsDead)
                MapDot(map,enemy.transform.position,enemy.IsBoss?boss:enemy.Tier==EnemyController.ThreatTier.Elite?gold:enemy.IsAggro?aggro:ordinary,enemy.IsBoss?7*u:4*u);
            if(session.Player!=null)MapDot(map,session.Player.transform.position,jade,7*u);
            string[] names={"你的位置","入口 / 出口","营地","普通敌人","战斗中","精英","首领"};Color[] colors={jade,portal,camp,ordinary,aggro,gold,boss};
            float x=body.xMax-116*u;
            Text(new Rect(x,body.y,116*u,22*u),"图例",Mathf.RoundToInt(13*u),pale,true);
            for(int i=0;i<names.Length;i++)
            {
                float y=body.y+(27+i*18)*u;Fill(new Rect(x,y+5*u,8*u,8*u),colors[i]);
                Text(new Rect(x+16*u,y,100*u,18*u),names[i],Mathf.RoundToInt(11*u),pale);
            }
        }
    }
}
