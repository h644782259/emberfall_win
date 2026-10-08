using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {
        private readonly MobileSkillTap mobileTap=new MobileSkillTap();
        private int mobileCastFinger {get{return mobileTap.Finger;}}
        private float TouchRatio { get { return MobileControls.Layout.Scale/scale; } }
        private Rect TouchRect(MobileControlLayout.Area a) { float r=TouchRatio;return new Rect(a.X*r,a.Y*r,a.Width*r,a.Height*r); }
        private Rect TouchRect(float x,float y,float w,float h) { return TouchRect(new MobileControlLayout.Area(x,y,w,h)); }
        private int TouchFont(float size) { return Mathf.RoundToInt(size*TouchRatio*EffectPreferences.InterfaceTextScale); }
        private bool MobileButton(MobileControlLayout.Area area,string label,Color color)
        { Rect r=TouchRect(area);blockedRects.Add(r);return Button(r,label,color); }
        private bool MobileIcon(MobileControlLayout.Area area,string icon,Color color)
        {
            Rect r=TouchRect(area);blockedRects.Add(r);
            float size=28*TouchRatio;DrawIcon(new Rect(r.center.x-size*.5f,r.center.y-size*.5f,size,size),UIIconAtlas.Utility(icon),color);
            Badge(r,icon=="inventory"?NewEquipmentAttention||Attention.LootPending:icon=="skills"?Attention.Skills:false);
            return GUI.Button(r,GUIContent.none,invisibleButton);
        }

        private int mobileSkillPage, mobileSkillPageFrame=-1;
        public bool MobileSkillVisible(int skill)
        {for(int button=0;button<MobileSkillPolicy.ButtonCount;button++)if(BoundMobileSkill(button,mobileSkillPage)==skill)return true;return false;}
        public void CancelMobileCast(){mobileTap.Cancel();}
        private bool BeginMobileCast(int finger,Vector2 screen)
        {
            if(session.InputBlocked||panel!=Panel.None||session.Player==null)return false;
            Vector2 p=ScreenToUI(screen);
            if(TouchRect(MobileControls.Layout.SkillPage).Contains(p)){if(mobileSkillPageFrame!=Time.frameCount){CancelMobileCast();mobileSkillPage=(mobileSkillPage+1)%MobileSkillPolicy.PageCount;mobileSkillPageFrame=Time.frameCount;}return true;}
            if(mobileTap.Active)return false;
            for(int i=0;i<MobileSkillPolicy.ButtonCount;i++)
            {
                if(!hotbarSlots[i].Contains(p))continue;
                int skill=BoundMobileSkill(i,mobileSkillPage);
                if(skill<0)return false;
                if(session.Progression.Profile.skillRanks[skill]<=0||!MobileSkillPolicy.IsActiveSkill(skill))return true;
                mobileTap.Begin(finger,skill);return true;
            }
            return false;
        }
        private void ContinueMobileCast(int finger,Vector2 screen,bool ended,bool cancelled)
        {
            if(finger!=mobileTap.Finger)return;
            if(cancelled||!MobileControls.SafeArea.Contains(screen)||session.InputBlocked||panel!=Panel.None||session.Player==null)
            {CancelMobileCast();return;}
            if(!ended)return;
            bool inside=false;Vector2 point=ScreenToUI(screen);
            for(int i=0;i<MobileSkillPolicy.ButtonCount;i++)if(hotbarSlots[i].Contains(point)&&BoundMobileSkill(i,mobileSkillPage)==mobileTap.Skill)inside=true;
            int skill;
            if(mobileTap.Release(finger,inside,false,out skill))
            {var targeting=session.Player.GetComponent<SkillTargetingController>();
                if(targeting!=null&&!targeting.Begin(skill))
                {if(string.IsNullOrEmpty(session.ControlFailure("skill"+skill)))session.ReportControlFailure("skill"+skill,"暂不可用");}}
        }
        public bool MobileDungeonEntranceVisible {get{return session!=null&&!session.PracticeActive&&!session.InDungeon&&!session.InputBlocked&&!session.DungeonSelectionOpen&&!session.NearChapterExit&&!session.NearRoomExit&&!session.SideEventAvailable&&session.NearbyHubNpc==HubNpcKind.None&&session.IsNearDungeonEntrance&&session.Progression.CanEnterDungeon;}}
        public MobileControlLayout.Area MobileInteractionArea
        {
            get
            {
                var layout=MobileControls.Layout;
                if(MobileDungeonEntranceVisible)return layout.DungeonEntrance;
                if(session!=null&&session.NearbyHubNpc!=HubNpcKind.None&&!session.NearChapterExit&&!session.NearRoomExit&&!session.SideEventAvailable)
                    return new MobileControlLayout.Area(Mathf.Max(layout.Width*.5f-48,254),layout.Height*.62f-22,96,44);
                return layout.Interact;
            }
        }
        public bool MobileInteractionVisible {get{return session!=null&&!session.PracticeActive&&(session.NearChapterExit||session.NearRoomExit||session.SideEventAvailable||session.NearbyHubNpc!=HubNpcKind.None||MobileDungeonEntranceVisible);}}
        private bool CanMobileInteract {get{return session!=null&&!session.PracticeActive&&!session.InputBlocked&&!session.DungeonSelectionOpen&&(session.NearChapterExit||session.NearRoomExit||session.SideEventAvailable||session.NearbyHubNpc!=HubNpcKind.None||session.IsInCamp||session.InDungeon||session.IsNearDungeonEntrance);}}
        public void ActivateMobileInteraction(int triggeringFinger=TouchReleaseLatch.AnyPointer)
        {
            if(!CanMobileInteract||UITransitionBlocked)return;
            try
            {
                if(MobileDungeonEntranceVisible)session.EnterDungeon();
                else if(session.NearChapterExit)session.EnterNextChapterRoom();
                else if(session.NearRoomExit)session.EnterNextRoom();
                else if(session.SideEventAvailable)session.StartSideEvent();
                else if(session.NearbyHubNpc!=HubNpcKind.None)OpenNearbyHubNpc();
                else if(session.IsInCamp){panel=Panel.Camp;session.SetUIBlocking(true);}
                else if(session.InDungeon)session.ReturnToCamp();
                else session.EnterDungeon();
            }
            finally{BlockUITransitionForFinger(triggeringFinger);}
        }
        private void LeaveMobilePauseForCamp()
        {
            bool paused=session.Paused, practice=session.PracticeActive, completed=false;
            var player=session.Player;
            int epoch=player==null?0:player.CombatEpoch;
            try
            {
                session.ReturnToCamp();
                completed=session.Player!=player || player!=null&&player.CombatEpoch!=epoch || practice&&!session.PracticeActive;
            }
            finally { session.SetPaused(completed?false:paused); }
        }
        private void LeaveMobilePauseForDungeon()
        {
            bool paused=session.Paused, completed=false;
            try
            {
                session.SetPaused(false);
                session.EnterDungeon();
                completed=session.DungeonSelectionOpen;
            }
            finally { if(!completed)session.SetPaused(paused); }
        }
        private void DrawMobileHUD()
        {
            var l=MobileControls.Layout;GameProfile p=session.Progression.Profile;
            DrawMobileVitals(l);
            if(MobileIcon(l.Inventory,"inventory",jade))TogglePanel(Panel.Inventory);
            if(MobileIcon(l.SkillsMenu,"skills",p.skillPoints>0?gold:jade))TogglePanel(Panel.Skills);
            if(MobileIcon(l.Menu,"settings",pale))session.SetPaused(true);
            if(MobileIcon(l.Catalog,"confirm",gold))OpenProgressionGoals();
            Badge(TouchRect(l.Catalog),Attention.Rewards);
            Rect map=TouchRect(l.Map);blockedRects.Add(map);Box(map,jade,false);DrawMinimapTerrain(map);
            if(!session.InDungeon){MapDot(map,new Vector3(0,0,11),jade,4*TouchRatio);for(int npc=0;npc<3;npc++)MapDot(map,GameSession.HubNpcPosition(npc),gold,3*TouchRatio);}
            else MapDot(map,new Vector3(0,0,-16),jade,4*TouchRatio);
            if(session.Player!=null)MapDot(map,session.Player.transform.position,jade,4*TouchRatio);
            foreach(var enemy in session.Enemies)if(enemy!=null&&!enemy.IsDead)MapDot(map,enemy.transform.position,enemy.IsBoss?gold:new Color(1,.4f,.3f),2*TouchRatio);
            if(GUI.Button(map,GUIContent.none,invisibleButton))OpenTravelMap();
            string growthTitle,growthStep;
            if(string.IsNullOrEmpty(session.Notification))
            {
                if(session.ChapterActive||session.SpecialAdventure)DrawMobileModeStatus(TouchRect(l.AdventureStatus));
                else if(!session.IsNearDungeonEntrance&&TryGrowthHudHint(out growthTitle,out growthStep))
                {
                    Rect goal=TouchRect(l.AdventureStatus);float y=goal.y;
                    DrawMobileObjectiveText(goal,ref y,growthTitle,11,gold,true,true);
                    DrawMobileObjectiveText(goal,ref y,growthStep,10,pale);
                }
            }
            if(session.InDungeon&&!session.SpecialAdventure)
            {blockedRects.Add(TouchRect(l.EncounterText));Text(TouchRect(l.EncounterText),session.DungeonCleared?"遗迹肃清":"第 "+session.DungeonWave+" / "+session.TotalWaves+" 波",TouchFont(12),pale,true,false,TextAnchor.MiddleCenter);}
            DrawMobileHotbar();
            DrawCompanionCommands();

            string interaction=MobileDungeonEntranceVisible?"进入副本":session.NearChapterExit?"沿星路前进":session.NearRoomExit?"进入下一间":session.SideEventAvailable?"晶核挑战":session.NearbyHubNpc!=HubNpcKind.None?HubNpcMobileLabel(session.NearbyHubNpc):session.IsInCamp?"营地工坊":session.InDungeon?"返回营地":session.IsNearDungeonEntrance?"进入副本":"靠近入口";
            if(MobileInteractionVisible)
            {
            Rect interact=TouchRect(MobileInteractionArea);blockedRects.Add(interact);
            // One pointer owner handles real touches and simulated/attached mice.
            // This is presentation only: a second IMGUI Button here would dispatch
            // again after a room transition changed the context on pointer release.
            if(session.NearbyHubNpc!=HubNpcKind.None&&!session.NearChapterExit&&!session.NearRoomExit&&!session.SideEventAvailable)
            {
                DrawIcon(interact,UIIconAtlas.NpcDialogCapsule(),new Color(.025f,.075f,.085f,.92f));
                DrawIcon(interact,UIIconAtlas.NpcDialogCapsule(true),CanMobileInteract?gold:muted);
            }
            else Box(interact,CanMobileInteract?gold:muted,false);
            Text(interact,interaction,TouchFont(11),CanMobileInteract?gold:muted,true,true,TextAnchor.MiddleCenter);

            }
            EnemyController boss=null;foreach(var e in session.Enemies)if(e!=null&&e.IsBoss&&!e.IsDead){boss=e;break;}
            if(boss!=null){blockedRects.Add(TouchRect(l.BossHealth));Bar(TouchRect(l.BossHealth),boss.Health/Mathf.Max(1,boss.MaxHealth),new Color(.93f,.34f,.29f));}
            var targeting=session.Player==null?null:session.Player.GetComponent<SkillTargetingController>();
            var charge=session.Player==null?null:session.Player.GetComponent<SkillChargeController>();
            if(charge!=null&&charge.IsCharging)Bar(TouchRect(26,l.Height-12,128,5),charge.Progress,gold);
        }
        private void DrawMobileHotbar()
        {
            float priorOpacity=controlOpacity;controlOpacity=EffectPreferences.TouchOpacity;
            var l=MobileControls.Layout;GameProfile p=session.Progression.Profile;
            for(int i=0;i<MobileSkillPolicy.ButtonCount;i++)
            {
                int skill=BoundMobileSkill(i,mobileSkillPage);if(skill<0)continue;
                Rect hit=hotbarSlots[i];blockedRects.Add(hit);Rect r=MobileVisualRect(hit);
                bool ready=session.Player!=null&&session.Player.IsSkillAvailable(skill);
                bool pressed=mobileTap.Skill==skill&&mobileTap.Active;
                DrawMobileControlSurface(r,ready,pressed);
                float iconSize=Mathf.Min(r.width,r.height)*.76f;
                Rect icon=new Rect(r.center.x-iconSize*.5f,r.center.y-iconSize*.5f,iconSize,iconSize);
                // Floating transparent glyph: the entire identity carries availability.
                Color skillColor=UIIconAtlas.SkillColor(p.heroClass,skill);
                Color tint=ready?(pressed?Color.Lerp(skillColor,Color.white,.25f):skillColor):new Color(.38f,.42f,.46f,.58f);
                DrawIcon(icon,UIIconAtlas.SkillGlyph(p.heroClass,skill,48),tint);
                if(skill==9)Text(new Rect(r.x,r.yMax-13*TouchRatio,r.width,12*TouchRatio),"终极",TouchFont(9),ready?new Color(.3f,1f,.72f):muted,true,false,TextAnchor.MiddleCenter);
                DrawMobileSkillAvailability(r,skill);
            }
            Rect pageHit=TouchRect(l.SkillPage);blockedRects.Add(pageHit);
            DrawIcon(new Rect(pageHit.center.x-12*TouchRatio,pageHit.center.y-12*TouchRatio,24*TouchRatio,24*TouchRatio),UIIconAtlas.SkillPageArrow(),Color.white);
            controlOpacity=priorOpacity;
        }
        private void DrawMobileControlSurface(Rect r,bool ready,bool pressed)
        {
            DrawIcon(r,UIIconAtlas.ControlDisc(),new Color(.75f,.87f,.92f,pressed&&ready?.10f:.045f));
            if(ready)DrawIcon(r,UIIconAtlas.ControlRing(true),new Color(.32f,.88f,1f,.12f));
            DrawIcon(r,UIIconAtlas.ControlRing(),ready?new Color(.35f,1f,.76f,.85f):new Color(.8f,.88f,.94f,.48f));
        }
        private void DrawMobileVitals(MobileControlLayout layout)
        {
            float hp=session.Player==null?0:session.Player.Health,max=session.Player==null?1:session.Player.MaxHealth;
            blockedRects.Add(TouchRect(layout.PlayerStatus));
            Bar(TouchRect(layout.PlayerHealth),hp/Mathf.Max(1,max),new Color(.86f,.16f,.19f));
            Text(TouchRect(layout.PlayerHealth),Mathf.CeilToInt(hp)+" / "+Mathf.CeilToInt(max),TouchFont(9),pale,true,false,TextAnchor.MiddleCenter);
            Bar(TouchRect(layout.PlayerEnergy),session.Player==null?0:session.Player.Energy/Mathf.Max(1,session.Player.MaxEnergy),new Color(.35f,.63f,1));
        }
        // Only the measured title is interactive. Text and the unused objective slot
        // never claim battlefield input; a small shadow works over bright terrain.
        private void DrawMobileObjectiveText(Rect bounds,ref float y,string value,int size,Color tint,bool bold=false,bool locate=false)
        {
            if(string.IsNullOrEmpty(value))return;
            string content=PlatformText(value);var style=Style(TouchFont(size),bold,true);
            float w=Mathf.Min(bounds.width,style.CalcSize(new GUIContent(content)).x+2*TouchRatio);
            float h=style.CalcHeight(new GUIContent(content),w);
            Rect line=new Rect(bounds.x,y,w,h);
            Text(new Rect(line.x+TouchRatio,line.y+TouchRatio,line.width,line.height),content,TouchFont(size),new Color(0,0,0,.9f),bold,true);
            Text(line,content,TouchFont(size),tint,bold,true);
            if(locate){blockedRects.Add(line);if(GUI.Button(line,GUIContent.none,invisibleButton))OpenTravelMap();}
            y+=h+2*TouchRatio;
        }
        private string mobileNoticeDetail;
        private Vector2 mobileNoticeScroll;
        private void DrawMobileBattleNotice()
        {
            var area=MobileControls.Layout.Notice;
            Rect r=TouchRect(area);blockedRects.Add(r);
            Box(r,gold,false);
            Text(TouchRect(area.X+7,area.Y+6,area.Width-14,area.Height-30),PlatformText(session.Notification),TouchFont(12),pale,false,true);
            Text(TouchRect(area.X+7,area.Y+area.Height-21,area.Width-14,16),"轻触查看完整提示",TouchFont(10),gold,false,false,TextAnchor.MiddleCenter);
            if(GUI.Button(r,GUIContent.none,invisibleButton))
            {
                mobileNoticeDetail=PlatformText(session.Notification);mobileNoticeScroll=Vector2.zero;
                panel=Panel.Notice;session.SetUIBlocking(true);CancelMobileScroll();BlockUITransition();
            }
        }
        private void DrawMobileNotice()
        {
            var layout=DrawMobileDialogChrome("冒险提示",gold);
            float contentWidth=layout.Body.Width-18;
            float contentHeight=MeasureMobileParagraph(mobileNoticeDetail,contentWidth-16,16)+16;
            mobileNoticeScroll=BeginTouchScroll("mobile-notice",MobilePanelRect(new MobilePanelLayout.Area(layout.Body.X,layout.Body.Y,layout.Body.Width,layout.Body.Height+56)),mobileNoticeScroll,
                new Rect(0,0,contentWidth*TouchRatio,Mathf.Max(layout.Body.Height+56,contentHeight)*TouchRatio));
            DrawMobileParagraph(8,8,contentWidth-16,mobileNoticeDetail,16,pale);
            EndTouchScroll();
        }
        private void DrawMobileTitle()
        {
            if(saveSlotsDirty)RefreshSaveSlots();var l=MobileControls.Layout;
            Fill(new Rect(0,0,width,height),new Color(.018f,.029f,.048f,1));
            float x=(l.Width-528)/2,y=(l.Height-300)/2;
            Text(TouchRect(x,y,528,30),"星烬纪元",TouchFont(25),pale,true);
            Text(TouchRect(x,y+31,528,16),"初选职业可在安全营地自由切换",TouchFont(11),muted);
            for(int i=0;i<4;i++)
            {
                var hero=(HeroClass)i;Color tint=GameBalance.ClassColor(hero);Rect r=TouchRect(x+i*134,y+50,126,166);
                Fill(r,new Color(.055f,.09f,.13f));Border(r,selectedClass==hero?gold:tint*.45f,selectedClass==hero?2:1);
                DrawCrest(TouchRect(x+i*134+26,y+68,74,78),hero,tint);
                Text(TouchRect(x+i*134+5,y+166,116,31),GameBalance.ClassName(hero),TouchFont(18),pale,true,false,TextAnchor.MiddleCenter);
                if(GUI.Button(r,GUIContent.none,invisibleButton))selectedClass=hero;
            }
            if(NavigationButton(TouchRect(x,y+237,254,52), "选择角色存档", jade, saveSlots.Count>0))OpenSaveSelection();
            if(PrimaryButton(TouchRect(x+274,y+237,254,52), "新建冒险", gold))StartSelectedHero();
            if(!string.IsNullOrEmpty(session.Progression.LastError))Text(TouchRect(x,y+291,528,22),session.Progression.LastError,TouchFont(11),gold);
        }
        private void DrawMobileSaveSelection()
        {
            var l=MobileControls.Layout;float x=(l.Width-520)/2,y=12;
            Fill(new Rect(0,0,width,height),new Color(.018f,.029f,.048f,1));
            Text(TouchRect(x,y,520,30),"角色存档  ·  "+saveSlots.Count,TouchFont(21),pale,true);
            Rect viewport=TouchRect(x,y+42,520,l.Height-135);float ratio=TouchRatio;
            if(mobileSaveSelectionIssue!=saveSelectionError)
            {mobileSaveSelectionIssue=saveSelectionError;if(!string.IsNullOrEmpty(saveSelectionError)){CancelMobileScroll();saveSelectionScroll=Vector2.zero;}}
            float issueHeight=string.IsNullOrEmpty(saveSelectionError)?0:MeasureMobileParagraph(saveSelectionError,478,14,true)+16;
            saveSelectionScroll=BeginTouchScroll("mobile-saves",viewport,saveSelectionScroll,new Rect(0,0,500*ratio,Mathf.Max(viewport.height,(issueHeight+saveSlots.Count*68)*ratio)));
            if(issueHeight>0)DrawMobileParagraph(10,6,478,saveSelectionError,14,gold,true);
            for(int i=0;i<saveSlots.Count;i++)
            {
                var slot=saveSlots[i];Rect r=new Rect(0,(issueHeight+i*68)*ratio,498*ratio,60*ratio);
                Fill(r,card);Border(r,selectedSaveId==slot.Id?gold:jade*.35f);
                Text(new Rect(12*ratio,r.y+7*ratio,320*ratio,23*ratio),slot.DisplayName,TouchFont(16),pale,true);
                string id=slot.Id=="legacy"?"旧存档":slot.Id.Substring(0,8);
                Text(new Rect(12*ratio,r.y+34*ratio,470*ratio,19*ratio),id+"  ·  "+(slot.SavedAtUtc==System.DateTime.MinValue?"时间未知":slot.SavedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"))+(slot.DeletionPending?"  删除未完成":""),TouchFont(12),muted);
                if(GUI.Button(r,GUIContent.none,invisibleButton)){selectedSaveId=slot.Id;saveSelectionError=null;}
            }
            EndTouchScroll();
            float bottom=l.Height-62;
            if(NavigationButton(TouchRect(x,bottom,110,48), "返回", jade))ClosePanel();
            if(Button(TouchRect(x+122,bottom,100,48),"刷新",muted))RefreshSaveSlots();
            DrawDeleteSaveButton(TouchRect(x+234,bottom,130,48));
            if(PrimaryButton(TouchRect(x+376,bottom,144,48), "读取角色", gold, saveSlots.Exists(a=>a.Id==selectedSaveId&&a.CanLoad)))ContinueSelectedSave();
        }
        private string mobileSaveSelectionIssue;
        private void DrawMobileGuide()
        {
            var l=MobileControls.Layout;float x=(l.Width-510)/2,y=(l.Height-300)/2;
            Fill(new Rect(0,0,width,height),new Color(.018f,.029f,.048f,1));
            Text(TouchRect(x,y,510,30),"触屏操作",TouchFont(22),pale,true);
            string[] tips={"左侧拖动移动 · 右下按住普攻，可同时操作", "主动技能分两页，每页最多4个；大招固定，被动自动生效", "轻点技能自动瞄准并施放，无须圈选或二次确认", "点敌人固定目标；点战场空白取消，恢复自动瞄准", "蓄力自动完成；点取消或闪避可中断", "灰色技能尚未学会；到技能学习后直接可用"};
            for(int i=0;i<tips.Length;i++)Text(TouchRect(x,y+43+i*32,510,28),tips[i],TouchFont(14),i==2?jade:pale);
            if(controlsReturnPause&&NavigationButton(TouchRect(x,y+250,510,48), "返回设置", jade))ClosePanel();
        }
        private bool PauseSidebarTab(Rect hit,string caption,bool selected,float u)
        {
            if(selected)
            {
                Fill(hit,new Color(jade.r,jade.g,jade.b,.10f));
                Fill(new Rect(hit.x,hit.y+8*u,3*u,hit.height-16*u),gold);
            }
            Text(new Rect(hit.x+14*u,hit.y,hit.width-20*u,hit.height),caption,Mathf.RoundToInt(14*u),selected?gold:muted,selected,false,TextAnchor.MiddleLeft);
            bool clicked=GUI.Button(hit,GUIContent.none,invisibleButton);
            if(clicked)GameAudio.Play(SoundCue.UI);
            return clicked;
        }
        private int mobilePausePage;
        private readonly Vector2[] mobilePauseScroll = new Vector2[4];
        private void DrawMobilePause()
        {
            if(mobileBindingEditor){DrawMobileBindingEditor();return;}
            var layout = MobileControls.Layout;
            float panelWidth=Mathf.Min(720,layout.Width-24),x=(layout.Width-panelWidth)*.5f,y=12;
            float titleWidth=panelWidth-52;
            float headerHeight=Mathf.Max(44,Style(TouchFont(23),true).CalcHeight(new GUIContent("设置"),titleWidth*TouchRatio)/TouchRatio+8);
            Fill(new Rect(0, 0, width, height), new Color(.012f, .025f, .04f, .94f));
            Text(TouchRect(x,y,titleWidth,headerHeight), "设置", TouchFont(23), pale, true);
            Rect close=TouchRect(x+panelWidth-44,y,44,44);
            DrawIcon(new Rect(close.center.x-9*TouchRatio,close.center.y-9*TouchRatio,18*TouchRatio,18*TouchRatio),UIIconAtlas.Utility("cancel"),jade);
            if(QuietAction(close,"",true,"关闭设置")){session.SetPaused(false);BlockUITransition();return;}
            string[] tabs = { "冒险", "声音与画面", "按键设置", "存档" };
            int[] tabOrder={0,3,1,2};
            float sidebarWidth=120,bodyY=y+headerHeight+8;
            float bodyHeight=Mathf.Max(48,layout.Height-bodyY-12);
            Fill(TouchRect(x,bodyY,sidebarWidth,bodyHeight),new Color(.025f,.05f,.065f,.65f));
            for (int i=0;i<tabs.Length;i++)
                if (PauseSidebarTab(TouchRect(x,bodyY+i*52,sidebarWidth,48),tabs[tabOrder[i]],mobilePausePage==tabOrder[i],TouchRatio) && mobilePausePage!=tabOrder[i])
                { mobilePausePage=tabOrder[i]; BlockUITransition(); }
            float contentX=x+sidebarWidth+16,bodyWidth=panelWidth-sidebarWidth-16,contentWidth=bodyWidth-18;
            string notice=string.IsNullOrEmpty(session.Notification)?"":PlatformText(session.Notification);
            float noticeHeight=string.IsNullOrEmpty(notice)?0:Mathf.Max(32,Style(TouchFont(11),false,true).CalcHeight(new GUIContent(notice),contentWidth*TouchRatio)/TouchRatio+8);
            float contentHeight=mobilePausePage==0?174+noticeHeight:mobilePausePage==3?174+noticeHeight:mobilePausePage==2?174:116;
            mobilePauseScroll[mobilePausePage]=BeginTouchScroll("mobile-pause-"+mobilePausePage,TouchRect(contentX,bodyY,bodyWidth,bodyHeight),mobilePauseScroll[mobilePausePage],new Rect(0,0,contentWidth*TouchRatio,Mathf.Max(bodyHeight,contentHeight)*TouchRatio));
            try { DrawMobilePauseBody(contentWidth,notice,noticeHeight); }
            finally { EndTouchScroll(); }
        }
        private void DrawMobilePauseBody(float contentWidth,string notice,float noticeHeight)
        {
            if(mobilePausePage==2){DrawMobileControlPreferences(0,0,contentWidth);if(NavigationButton(TouchRect(0,116,contentWidth,48),"操作指南",jade))OpenControls();return;}
            if(mobilePausePage==3)
            {
                if(PrimaryButton(TouchRect(0,0,contentWidth,48),"保存",gold))RequestManualSave();
                if(NavigationButton(TouchRect(0,58,contentWidth,48),"读取存档",jade))OpenSaveSelection();
                if(NavigationButton(TouchRect(0,116,contentWidth,48),"存档位置 / 迁移",jade))
                {saveReturnPause=true;panel=Panel.SaveLocation;session.SetUIBlocking(true);session.SetPaused(false);}
            if (!string.IsNullOrEmpty(notice))
                Text(TouchRect(0,174,contentWidth,noticeHeight),notice,TouchFont(11),gold,false,true,TextAnchor.MiddleCenter);
                return;
            }
            if (mobilePausePage == 1)
            {
                float column=(contentWidth-12)*.5f;
                string[] extra = { "声音：" + (GameAudio.Muted ? "关" : "开"), "飘字：" + (EffectPreferences.CombatTextScale > 1.5f ? "大" : "标准"),
                    "镜头反馈：" + (EffectPreferences.CameraShake ? "开" : "关"), "特效：" + (EffectPreferences.ReducedEffects ? "精简" : "完整") };
                for (int i = 0; i < extra.Length; i++)
                    if (Button(TouchRect((i%2)*(column+12),(i/2)*58,column,48),extra[i],jade))
                    {
                        if (i == 0) GameAudio.Muted = !GameAudio.Muted;
                        else if (i == 1) EffectPreferences.CombatTextScale = EffectPreferences.CombatTextScale > 1.5f ? 1.25f : 1.8f;
                        else if (i == 2) EffectPreferences.CameraShake = !EffectPreferences.CameraShake;
                        else if (i == 3) EffectPreferences.EffectsScale = EffectPreferences.ReducedEffects ? 1f : .35f;
                    }
                return;
            }
            if(NavigationButton(TouchRect(0,0,contentWidth,48),"营地 / 撤离",jade))LeaveMobilePauseForCamp();
            if(DangerButton(TouchRect(0,58,contentWidth,48),"返回主菜单",muted))RequestExit(true);
            if (!string.IsNullOrEmpty(notice))
                Text(TouchRect(0,174,contentWidth,noticeHeight),notice,TouchFont(11),gold,false,true,TextAnchor.MiddleCenter);
        }
    }
}
