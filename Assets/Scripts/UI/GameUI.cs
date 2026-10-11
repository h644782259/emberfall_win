using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    /// <summary>Resolution-independent runtime interface; no scene or package dependencies.</summary>
    public sealed partial class GameUI : MonoBehaviour
    {
        private enum Panel { None, DungeonExit, Inventory, Skills, Bindings, SaveLocation, Controls, SaveSelection, PotionAssignment, Fashion, Chests, HubUtility, Summary, TravelMap, Notice, Chapter, HubDialogue }
        private GameSession session;
        private Panel panel;
        private HeroClass selectedClass;
        private string selectedItem;
        private readonly List<SaveSlotInfo> saveSlots = new List<SaveSlotInfo>();
        private bool saveSlotsDirty = true;
        private string selectedSaveId;
        private string saveSelectionError;
        private Vector2 saveSelectionScroll;
        private Vector2 inventoryScroll;
        private int inventoryFilter = -1;
        private int inventorySort=0;
        private int unequippedCount;
        private Vector2 skillScroll;
        private int selectedSkill;
        private int rebindingSlot = -1;
        private Panel bindingReturnPanel;
        private bool bindingReturnPause;
        private bool saveReturnPause;
        private bool controlsReturnPause;
        private int hotbarPointerSlot = -1;
        private int hotbarPointerPage = -1;
        private int hotbarPointerSkill = -1;
        private bool hotbarPointerConfiguring;
        private bool hotbarDragging;
        private Vector2 hotbarPointerOrigin;
        private int hotbarPointerControl;
        private int hotbarReleaseFrame = -1;
        private bool suppressHotbarMouse;
        private int hotbarTouchFinger = -1000;
        private Vector2 hotbarTouchPosition;
        private readonly Rect[] hotbarSlots = new Rect[GameBalance.HotbarSize];
        private Rect hotbarBounds;
        private readonly Rect[] detailSlots = new Rect[GameBalance.HotbarSize];
        private Font font;
        private float scale = 1f;
        private float width = 1280f;
        private float height = 720f;
        private Vector2 guiOffset;
        private readonly Dictionary<int, GUIStyle> styles = new Dictionary<int, GUIStyle>();
        private readonly List<Rect> blockedRects = new List<Rect>();
        private readonly List<ItemData> bagItems = new List<ItemData>();
        private GUIStyle invisibleButton;
        private GUIStyle scrollBar;
        private GUIStyle scrollThumb;
        private Texture2D thumbTexture;
        private Texture2D trackTexture;
        private readonly Texture2D[] crestTextures = new Texture2D[4];
        private string tooltip;
        private Rect tooltipAnchor;
        private string tooltipAnchorText;
        private string tooltipKey;
        private readonly Color ink = new Color(.006f, .010f, .019f, 1f);
        private readonly Color card = new Color(.016f, .027f, .045f, .99f);
        private readonly Color jade = new Color(.32f, .91f, .77f);
        private readonly Color gold = new Color(1f, .76f, .37f);
        private readonly Color muted = new Color(.48f, .58f, .69f);
        private readonly Color pale = new Color(.97f, .985f, 1f);

        public bool IsPointerOverUI
        {
            get
            {
                if (session == null) return false;
                if(mobileCastFinger!=-1000)return true;
                if (hotbarPointerSlot >= 0 || suppressHotbarMouse || Time.frameCount <= hotbarReleaseFrame) return true;
                if (!session.HasStarted || session.InputBlocked || panel != Panel.None) return true;
                if (MobileControls.IsScreenPointOverControls(Input.mousePosition)) return true;
                Vector2 mouse = Mouse;
                for (int i = 0; i < blockedRects.Count; i++)
                    if (blockedRects[i].Contains(mouse)) return true;
                return false;
            }
        }

        private Vector2 Mouse { get { return hotbarTouchFinger != -1000 ? hotbarTouchPosition : ScreenToUI(Input.mousePosition); } }
        private Vector2 ScreenToUI(Vector2 point) { return (new Vector2(point.x, Screen.height - point.y) - guiOffset) / scale; }
        public bool IsScreenPointOverUI(Vector2 point)
        {
            if (session == null || !session.HasStarted || session.InputBlocked || panel != Panel.None) return true;
            if (MobileControls.IsScreenPointOverControls(point)) return true;
            return IsScreenPointOverHUD(point);
        }
        // UI priority without calling back into movement-zone hit testing.
        public bool IsScreenPointOverHUD(Vector2 point)
        {
            if (session == null || !session.HasStarted || session.InputBlocked || panel != Panel.None) return true;
            Vector2 position = ScreenToUI(point);
            foreach (Rect rect in blockedRects) if (rect.Contains(position)) return true;
            return false;
        }
        public bool TryBeginTouchSkill(int finger, Vector2 point)
        {
            if (!MobileControls.Active || hotbarPointerSlot >= 0 || session == null || !session.HasStarted || session.Paused || session.IsDead) return false;
            RefreshLayout();
            bool configuring = false; // Mobile skills are direct actions; desktop loadout paging stays desktop-only.
            if(!configuring)return BeginMobileCast(finger,point);
            if (!configuring && session.InputBlocked) return false;
            if (panel != Panel.None && !configuring) return false;
            Rect[] slots = configuring ? detailSlots : hotbarSlots;
            Vector2 position = ScreenToUI(point);
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].Contains(position)) continue;
                BeginHotbarPointer(i, position, configuring);
                hotbarTouchFinger = finger;
                hotbarTouchPosition = position;
                return true;
            }
            return false;
        }
        public void UpdateTouchSkill(int finger, Vector2 point, bool ended, bool cancelled)
        {
            if(finger==mobileCastFinger){ContinueMobileCast(finger,point,ended,cancelled);return;}
            if (finger != hotbarTouchFinger || hotbarPointerSlot < 0) return;
            Vector2 position = ScreenToUI(point);
            hotbarTouchPosition = position;
            ContinueHotbarPointer(position);
            if (!ended) return;
            if (cancelled) { CancelHotbarPointer(); return; }
            Rect[] slots = hotbarPointerConfiguring ? detailSlots : hotbarSlots;
            int target = -1;
            for (int i = 0; i < slots.Length; i++) if (slots[i].Contains(position)) { target = i; break; }
            CompleteHotbarPointer(target);
        }

        public void Initialize(GameSession gameSession)
        {
            session = gameSession;
            BindLootNotices();
            var targetFeedback=GetComponent<CombatTargetFeedback>();
            if(targetFeedback==null)targetFeedback=gameObject.AddComponent<CombatTargetFeedback>();
            targetFeedback.Initialize(session);
            session.Progression.Changed+=InvalidateAttention;
            font = GameFont.Shared;
        }

        // Input and rendering share geometry, including before the first repaint
        // or immediately after a safe-area/orientation change.
        private void RefreshLayout()
        {
            Rect safe = MobileControls.SafeArea;
            ObserveTouchViewport(safe);
            scale = HudLogicalScale.For(safe.width, safe.height) * (MobileControls.Active ? MobileControls.Layout.UiZoom : 1f);
            width = safe.width / scale;
            height = safe.height / scale;
            guiOffset = new Vector2(safe.x, Screen.height - safe.yMax);
            bool mobile = MobileControls.Active;
            if(mobile)
            {
                var touch=MobileControls.Layout;
                hotbarBounds=new Rect(0,0,0,0);
                for(int i=0;i<hotbarSlots.Length;i++)hotbarSlots[i]=i<MobileSkillPolicy.ButtonCount?TouchRect(touch.Skills[i]):new Rect(-10000,-10000,0,0);
                return;
            }
            float barWidth = mobile ? 362 : 282;
            hotbarBounds = new Rect((width - barWidth) * .5f, height - (mobile ? 177 : 143), barWidth, mobile ? 165 : 131);
            for (int slot = 0; slot < hotbarSlots.Length; slot++)
                hotbarSlots[slot] = new Rect(hotbarBounds.x + 10 + (slot % 5) * (mobile ? 69 : 53),
                    hotbarBounds.y + (mobile ? 27 : 17) + (slot / 5) * (mobile ? 65 : 62), mobile ? 64 : 48, mobile ? 61 : 46);
        }

        private bool PauseUtilityVisible {get{return panel==Panel.Controls&&controlsReturnPause||panel==Panel.SaveLocation&&saveReturnPause||panel==Panel.Bindings&&(bindingReturnPause||bindingReturnPanel==Panel.Controls&&controlsReturnPause)||panel==Panel.TravelMap&&travelReturnPause;}}

        private void Update()
        {
            if (session == null) return;
            if (session.BackgroundPaused) { ReleaseCollectionModel(); return; }
            RefreshLayout();
            ReconcileTitleBackdrop();
            ReconcileMobileScroll();
            ReconcileCollectionPreview();
            if (session == null || session.BackgroundPaused) return;
            if(session.PracticeActive){if(Input.GetKeyDown(KeyCode.Escape))session.EndPractice("主动离开 · 记录提前结束");return;}
            ReconcileBuildPlanSurface();ReconcileClassSwitchSurface();
            ReconcileProgressionGoalSurface();
            bool gameplayBackAllowed=GameplayBackAllowed;
            if(Input.GetKeyDown(KeyCode.Escape))backConsumedFrame=Time.frameCount;
            if(Input.GetKeyDown(KeyCode.Escape)&&CloseTopPopup())return;
            if(session.RoomBranchChoiceOpen&&!session.Paused){if(Input.GetKeyDown(KeyCode.Escape)){session.CancelRoomBranchChoice();BlockUITransition();}return;}
            if(exitRequest.Open){if(Input.GetKeyDown(KeyCode.Escape)){exitRequest.Cancel();exitError=null;BlockUITransition();}return;}
            if(mobileCastFinger!=-1000&&(session.InputBlocked||panel!=Panel.None))CancelMobileCast();
            if (suppressHotbarMouse && !Input.GetMouseButton(0)) suppressHotbarMouse = false;
            if (hotbarPointerSlot >= 0 && (!session.HasStarted || session.Paused || session.IsDead ||
                session.Progression.Profile.hotbarPage != hotbarPointerPage ||
                (hotbarPointerConfiguring ? panel != Panel.Skills || GameBalance.IsPassive(selectedSkill) : panel != Panel.None))) CancelHotbarPointer();
            if (!session.HasStarted)
            {
                ClearRewardMoment();
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    if(panel==Panel.SaveSelection)ClosePanel();
                    else if(AndroidBackExitEnabled&&panel==Panel.None)RequestExit(false);
                }
                return;
            }
            if (session.IsDead && !session.Paused && !PauseUtilityVisible)
            {
                if (panel != Panel.None)
                {
                    panel = Panel.None;
                    rebindingSlot = -1;
                    session.SetUIBlocking(false);
                }
                if(Input.GetKeyDown(KeyCode.Escape))session.SetPaused(true);
                return;
            }
            if(!session.Paused&&(session.ModeFinished||session.DungeonCleared)&&!session.FinishedResultDismissed)
            {if(session.FinishedResultReady&&Input.GetKeyDown(KeyCode.Escape))CloseSettlement();return;}
            if(masteryResetConfirm&&panel==Panel.Skills&&Input.GetKeyDown(KeyCode.Escape)){masteryResetConfirm=false;return;}
            EnsurePendingChestPanel();
            if ((session.DungeonSelectionOpen || session.RunChoices.AwaitingChoice) && !session.Paused && !PauseUtilityVisible)
            {if(Input.GetKeyDown(KeyCode.Escape)){if(session.DungeonSelectionOpen)session.CancelDungeonSelection();else session.SetPaused(true);}return;}
            if (rebindingSlot >= 0) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (ReturnToMobilePauseRoot()) return;
                if (hotbarPointerSlot >= 0) { CancelHotbarPointer(); return; }
                SkillChargeController charge = session.Player == null ? null : session.Player.GetComponent<SkillChargeController>();
                if (gameplayBackAllowed && charge != null && (charge.IsCharging || charge.CancelledThisFrame))
                {
                    charge.Cancel();
                    return;
                }
                SkillTargetingController targeting = session.Player == null ? null : session.Player.GetComponent<SkillTargetingController>();
                if (gameplayBackAllowed && targeting != null && (targeting.IsTargeting || targeting.CancelledThisFrame))
                {
                    targeting.Cancel();
                    return;
                }
                if(panel==Panel.Summary){CloseSettlement();}
                else if (panel == Panel.Chests) { session.SetPaused(false); ClosePanel(); BlockUITransition(); }
                else if (panel != Panel.None) ClosePanel();
                else session.SetPaused(!session.Paused);
                return;
            }
            if(HandleFunctionShortcut())return;


        }
        private void OnDestroy()
        {
            ReleaseInterfaceSurfaces();
            ReleaseFashionSmithPreview();
            ReleaseLootNotices();
            if(session!=null&&session.Progression!=null)session.Progression.Changed-=InvalidateAttention;
            if(attentionDot!=null)Destroy(attentionDot);
            ClearRewardMoment();
            ReleaseTitleBackdrop();
            ReleaseChestTextures();
            ReleaseCollectionPreview();
            ReleaseTerrainMaps();
            if (thumbTexture != null) Destroy(thumbTexture);
            if (trackTexture != null) Destroy(trackTexture);
            for (int i = 0; i < crestTextures.Length; i++) if (crestTextures[i] != null) Destroy(crestTextures[i]);
            UIIconAtlas.Clear();
            GameFont.Release(ref font);
        }

        private void OnGUI()
        {
            if (session == null || session.Progression == null || session.BackgroundPaused) return;
            RefreshLayout();
#if UNITY_EDITOR
            if(MobileControls.ValidationUsesSimulation)MobileControls.ValidationRenderedViewport=new Vector2(Screen.width,Screen.height);
#endif
            if (!session.HasStarted) DrawTitleBackdrop();
            if (font == null) font = GameFont.Shared;
            if (invisibleButton == null) BuildStyles();
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            Color oldContentColor = GUI.contentColor;
            bool oldEnabled = GUI.enabled;
            GUI.matrix = Matrix4x4.TRS(guiOffset, Quaternion.identity, new Vector3(scale, scale, 1));
            GUI.color = Color.white;
            GUI.contentColor = Color.white;
            GUI.enabled = !session.BackgroundPaused && !LifecycleTouchBlocked && MerchantServiceLayout.StablePanelEvent(UITransitionBlocked,true,Event.current.type==EventType.Repaint||Event.current.type==EventType.Layout);
            blockedRects.Clear();
            tooltip = null;tooltipAnchorText=null;tooltipKey=null;
            bool rewardOverlayEnabled=GUI.enabled;
            bool rewardOverlayPointer=entryRewardPopupVisible&&entryRewardPopupRect.Contains(Mouse);
            BeginEntryRewardPopup();
            // Block input under the popup while keeping Repaint/Layout enabled:
            // Unity applies a disabled tint to textures when GUI.enabled is false.
            if(rewardOverlayPointer&&Event.current.type!=EventType.Repaint&&Event.current.type!=EventType.Layout)GUI.enabled=false;
            if(exitRequest.Open)
            {
                ClearRewardMoment();DrawExitConfirmation();GUI.matrix=oldMatrix;GUI.color=oldColor;GUI.contentColor=oldContentColor;GUI.enabled=oldEnabled;return;
            }
            if(session.PracticeActive)
            { ClearRewardMoment();DrawPracticeCombatHUD();DrawPracticeOverlay();GUI.matrix=oldMatrix;GUI.color=oldColor;GUI.contentColor=oldContentColor;GUI.enabled=oldEnabled;return; }
            HandleBindingInput();

            if (!session.HasStarted)
            {
                ClearRewardMoment();
                if (panel == Panel.SaveSelection) DrawSaveSelection();
                else DrawTitle();
            }
            else
            {
                EnsurePendingChestPanel();
                if(PendingChestReturnVisible)blockedRects.Add(PendingChestReturnRect());
                PrepareLootNotices();
                PrepareRewardMoment();
                bool priorEnabled = GUI.enabled;
                GUI.enabled = priorEnabled && panel == Panel.None && !session.InputBlocked;
                DrawHUD();
                GUI.enabled = priorEnabled;
                if (panel == Panel.None && !session.Paused && !session.IsDead) DrawTargetingHint();
                if (session.Paused) DrawPause();
                else if(PauseUtilityVisible)
                {if(panel==Panel.Controls)DrawControls();else if(panel==Panel.Bindings)DrawBindings();else if(panel==Panel.SaveLocation)DrawSaveLocation();else DrawTravelMap();}
                else if (panel == Panel.Chests) DrawChests();
                else if (panel == Panel.DungeonExit) DrawDungeonExit();
                else if (chestRecoveryService && panel == Panel.Inventory) DrawInventory();
                else if (session.IsDead) DrawDeath();
                else if (session.DungeonSelectionOpen) DrawDungeonSelection();
                else if (session.RoomBranchChoiceOpen) DrawRoomBranchChoice();
                else if (session.RunChoices.AwaitingChoice) DrawBlessingChoice();
                else if((session.ModeFinished||session.DungeonCleared)&&!session.FinishedResultDismissed){if(session.FinishedResultReady){if(DrawStructuredRunRecap(false))CloseSettlement();}else DrawVictoryTransition();}
                else if (panel == Panel.Inventory) DrawInventory();
                else if (panel == Panel.Skills) DrawSkills();
                else if (panel == Panel.Bindings) DrawBindings();
                else if (panel == Panel.SaveLocation) DrawSaveLocation();
                else if (panel == Panel.Controls) DrawControls();
                else if (panel == Panel.PotionAssignment) DrawPotionAssignment();
                else if (panel == Panel.Fashion) DrawFashion();
                else if (panel == Panel.HubUtility) DrawHubUtility();
                else if (panel == Panel.Summary) DrawRunSummary();
                else if (panel == Panel.TravelMap) DrawTravelMap();
                else if (panel == Panel.Notice) DrawMobileNotice();
                else if (panel == Panel.Chapter) DrawChapterSelection();
                DrawNotification();
                DrawRewardMoment();
                DrawPendingChestReturn();
                DrawLootNotices();
            }
            if (hotbarDragging && hotbarPointerSkill != -1)
            {
                tooltip = null;
                DrawIcon(new Rect(Mouse.x + 11, Mouse.y + 11, 36, 36), HotbarIcon(session.Progression.Profile, hotbarPointerSkill), Color.white);
            }
            if(panel==Panel.Skills||MobileControls.Active&&panel==Panel.Inventory&&inventoryComparisonOpen)tooltip=null;
            DrawTooltip();
            GUI.enabled=rewardOverlayEnabled;
            DrawEntryRewardPopup();
            DrawExitConfirmation();
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
            GUI.contentColor = oldContentColor;
            GUI.enabled = oldEnabled;
        }

        private void BuildStyles()
        {
            invisibleButton = new GUIStyle { font = font, alignment = TextAnchor.MiddleCenter };
            invisibleButton.normal.textColor = Color.clear;
            invisibleButton.hover.textColor = Color.clear;
            invisibleButton.active.textColor = Color.clear;
            trackTexture = SolidTexture(new Color(.1f, .17f, .22f));
            thumbTexture = SolidTexture(new Color(.27f, .48f, .5f));
            scrollBar = new GUIStyle(GUI.skin.verticalScrollbar) { fixedWidth = 7 };
            scrollBar.normal.background = trackTexture;
            scrollThumb = new GUIStyle(GUI.skin.verticalScrollbarThumb);
            scrollThumb.normal.background = thumbTexture;
            scrollThumb.hover.background = thumbTexture;
            scrollThumb.active.background = thumbTexture;
        }

        private static Texture2D SolidTexture(Color color)
        {
            var texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private float interfaceStyleScale=-1;
        private GUIStyle Style(int size, bool bold = false, bool wrap = false, TextAnchor align = TextAnchor.UpperLeft)
        {
            float scale=EffectPreferences.InterfaceTextScale;
            if(interfaceStyleScale!=scale){styles.Clear();interfaceStyleScale=scale;}
            size=Mathf.Max(1,Mathf.RoundToInt(size*scale));
            int key = size + (bold ? 100 : 0) + (wrap ? 200 : 0) + (int)align * 1000;
            GUIStyle result;
            if (styles.TryGetValue(key, out result)) return result;
            result = new GUIStyle { font = font, fontSize = size, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal, wordWrap = wrap, alignment = align, clipping = TextClipping.Clip };
            result.normal.textColor = Color.white;
            styles.Add(key, result);
            return result;
        }

        private void Text(Rect rect, string value, int size, Color color, bool bold = false, bool wrap = false, TextAnchor align = TextAnchor.UpperLeft)
        {
            Color previous = GUI.contentColor;
            GUIStyle style = Style(size, bold, wrap, align);
            Color previousTextColor = style.normal.textColor;
            // Keep the global tint neutral; the style owns the intended text color.
            GUI.contentColor = Color.white;
            color.a*=controlOpacity;style.normal.textColor = color;
            GUI.Label(rect, value ?? "", style);
            style.normal.textColor = previousTextColor;
            GUI.contentColor = previous;
        }

        private static string PlatformText(string value)
        {
            if (!MobileControls.Active || string.IsNullOrEmpty(value)) return value;
            return value.Replace("WASD 移动，鼠标瞄准", "拖动左侧摇杆移动，点击技能或按住攻击")
                .Replace("按 Shift 闪现", "点击闪现按钮")
                .Replace("按 K ", "打开技能").Replace("按 I ", "打开行囊")
                .Replace("按 T ", "点击传送按钮").Replace("按 H ", "点击回营按钮")
                .Replace("按 F ", "点击药剂按钮")
                .Replace(" · I", "").Replace(" · K", "").Replace(" · H", "").Replace(" · T", "");
        }

        private static string SkillTooltip(GameProfile profile, int skill, int rank)
        {
            HeroClass hero = profile.heroClass;
            string result = GameBalance.SkillName(hero, skill) + " · " + GameBalance.SkillRankName(rank) + "\n" + GameBalance.SkillDescription(hero, skill);
            string venom=BuildCatalog.VenomSkillOverride(profile,skill,rank);
            if(venom.Length>0)result=venom;
            if (GameBalance.IsPassive(skill)) return result;
            float cost = GameBalance.SkillEnergyCost(hero, skill);
            result += (skill==SkillStockRules.Skill(hero)?"\n储存2次 · 每 "+SkillStockRules.Seconds(hero)+" 秒逐次回复 ·":"\n冷却 " + GameBalance.EffectiveCooldown(hero, skill, rank).ToString("0.#")) +
                (cost == 0 ? " 秒 · 无需能量" : (skill==SkillStockRules.Skill(hero)?" 每次消耗 ":" 秒 · 消耗 ") + cost.ToString("0") + " " + GameBalance.EnergyName(hero));
            string budget=BuildCatalog.ConcentratedVenomEquipped(profile)&&hero==HeroClass.Ranger&&skill==0?"":SkillBudgetHint(hero,skill,rank);if(budget.Length>0)result+="\n"+budget;
            float charge = SkillChargeController.Duration(hero, skill);
            if (charge > 0) result += "\n蓄力 " + charge.ToString("0.##") + " 秒";
            return result;
        }

        private static string SkillBudgetHint(HeroClass hero,int skill,int rank)
        {
            if(skill==2&&hero!=HeroClass.Summoner){var field=SkillDamageBudgets.EarlyField(hero,rank);return field.Duration.ToString("0.#")+"秒 · 全命中 "+field.TotalCoefficient.ToString("0.##")+"×攻击（非暴击，含收尾）";}
            if(hero==HeroClass.Ranger&&skill==0)return "同目标连中递减 · 箭矢与爆裂合计上限 "+SkillDamageBudgets.FanTargetCap(rank).ToString("0.#")+"×攻击；既有毒层引爆另计";
            if(hero==HeroClass.Arcanist&&skill==0&&rank>=3)return "同目标冰片额外伤害上限1.8×攻击；不同目标独立计算";
            return "";
        }

        private static float controlOpacity=1f;
        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            color.a*=controlOpacity;GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void Border(Rect rect, Color color, float thickness = 1f)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.y, thickness, rect.height), color);
            Fill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private void Box(Rect rect, Color accent, bool shadow = true)
        {
            if (shadow) Surface(new Rect(rect.x + 4, rect.y + 6, rect.width, rect.height), new Color(0, 0, 0, .38f));
            Surface(rect, ink);
            SurfaceFrame(rect, new Color(accent.r, accent.g, accent.b, .28f));
            float cap=Mathf.Min(28,rect.width*.12f);
            Fill(new Rect(rect.x+10,rect.y,cap,2),new Color(accent.r,accent.g,accent.b,.72f));
            Fill(new Rect(rect.xMax-cap-10,rect.yMax-2,cap,2),new Color(accent.r,accent.g,accent.b,.45f));
        }

        private bool Button(Rect rect, string caption, Color accent, bool enabled = true, string hint = null, bool primary = false)
        {
            return DrawButton(rect, caption, primary ? ButtonRole.Primary : ButtonRole.Action, enabled, hint);
        }

        private void Rule(float x, float y, float length, Color color)
        {
            Fill(new Rect(x, y, length, 1), new Color(color.r, color.g, color.b, .25f));
        }

        private bool titleCreatingHero;

        private SaveSlotInfo RecentAdventureSlot()
        {
            SaveSlotInfo recent=null;
            foreach(var slot in saveSlots)
                if(slot.CanLoad&&!slot.DeletionPending&&(recent==null||slot.SavedAtUtc>recent.SavedAtUtc))recent=slot;
            return recent;
        }

        private void ContinueRecentAdventure()
        {
            if(saveSlotsDirty)RefreshSaveSlots();
            var recent=RecentAdventureSlot();
            if(recent==null){OpenSaveSelection();return;}
            selectedSaveId=recent.Id;
            saveSelectionError=null;
            ContinueSelectedSave();
            if(!session.HasStarted)
            {
                // Keep load failures visible and let the player choose another character.
                saveSelectionFromPause=false;saveSelectionScroll=Vector2.zero;
                panel=Panel.SaveSelection;BlockUITransition();
            }
        }

        private void DrawAdventureHome()
        {
            bool mobile=MobileControls.Active;
            float availableWidth=mobile?MobileControls.Layout.Width:width;
            float availableHeight=mobile?MobileControls.Layout.Height:height;
            float maxZoom=mobile?(SystemInfo.deviceModel!=null&&SystemInfo.deviceModel.StartsWith("iPad")?1.75f:1f):1.5f;
            float zoom=Mathf.Min(maxZoom,Mathf.Min((availableWidth-32)/528f,(availableHeight-24)/324f));
            float x=(availableWidth-528*zoom)*.5f,y=(availableHeight-324*zoom)*.5f;
            Rect HomeRect(float rx,float ry,float rw,float rh) => mobile?TouchRect(x+rx*zoom,y+ry*zoom,rw*zoom,rh*zoom):new Rect(x+rx*zoom,y+ry*zoom,rw*zoom,rh*zoom);
            int HomeFont(float size) => mobile?TouchFont(size*zoom):Mathf.RoundToInt(size*zoom);
            // The viewport backdrop is drawn before the safe-area transform.
            Text(HomeRect(0,4,528,42),"星烬纪元",HomeFont(32),pale,true,false,TextAnchor.MiddleCenter);
            SaveSlotInfo recent=RecentAdventureSlot();
            if(recent!=null)
            {
                DrawCrest(HomeRect(120,68,50,52),recent.HeroClass,GameBalance.ClassColor(recent.HeroClass));
                Text(HomeRect(184,67,260,28),recent.DisplayName,HomeFont(16),pale,true);
                string saved=recent.SavedAtUtc==System.DateTime.MinValue?"最近的冒险":recent.SavedAtUtc.ToLocalTime().ToString("MM-dd HH:mm")+" 保存";
                Text(HomeRect(184,98,260,20),saved,HomeFont(11),muted);
            }
            else Text(HomeRect(0,74,528,38),"选择你的角色，开启第一段冒险",HomeFont(15),muted,false,false,TextAnchor.MiddleCenter);
            if(DrawButton(HomeRect(88,141,352,62),recent!=null?"继续冒险":"创建角色",ButtonRole.Primary,fontSize:HomeFont(21)))
            {
                if(recent!=null)ContinueRecentAdventure();
                else {titleCreatingHero=true;BlockUITransition();}
                return;
            }
            if(saveSlots.Count>0&&DrawButton(HomeRect(recent!=null?88:180,215,168,44),"其他存档",ButtonRole.Navigation,fontSize:HomeFont(14)))
            {OpenSaveSelection();return;}
            if(recent!=null&&DrawButton(HomeRect(272,215,168,44),"新的角色",ButtonRole.Navigation,fontSize:HomeFont(14)))
            {titleCreatingHero=true;BlockUITransition();return;}
#if !UNITY_IOS && !UNITY_ANDROID
            if(!mobile&&NavigationButton(new Rect(width-148,height-60,120,38),"退出游戏",muted))RequestExit(false);
#endif
            string message=!string.IsNullOrEmpty(session.Notification)?session.Notification:session.Progression.LastError;
            if(!string.IsNullOrEmpty(message))Text(HomeRect(0,296,528,24),message,HomeFont(11),gold,false,true,TextAnchor.MiddleCenter);
        }

        private void DrawTitle()
        {
            if(MobileControls.Active){DrawMobileTitle();return;}
            if (saveSlotsDirty) RefreshSaveSlots();
            if(!titleCreatingHero){DrawAdventureHome();return;}
            // The title is a static, opaque composition; world geometry cannot leak through it.
            // Full-screen backdrop remains visible behind character cards.
            float x = (width - 1040) * .5f;
            float y = (height - 438) * .5f;

            Fill(new Rect(x, y, 1040, 438), new Color(.048f, .074f, .112f, .72f));
            Border(new Rect(x, y, 1040, 438), new Color(.22f, .33f, .43f, 1f));
            Text(new Rect(x + 32, y + 24, 976, 43), "选择职业", 32, Color.white, true);
            Text(new Rect(x+280,y+36,720,24),"初选职业可在安全营地自由切换 · 同一角色共享成长",14,muted);
            Fill(new Rect(x + 32, y + 81, 976, 1), new Color(.2f, .3f, .39f, 1f));
            string[] roles = { "近战 · 范围斩击 · 耐久", "远程 · 控制 · 法术爆发", "远程 · 灵活 · 群体射击", "召唤 · 协同 · 灵兽守护" };
            for (int i = 0; i < 4; i++)
            {
                HeroClass hero = (HeroClass)i;
                Color accent = GameBalance.ClassColor(hero);
                Rect choice = new Rect(x + 32 + i * 247, y + 104, 235, 228);
                bool selected = selectedClass == hero;
                Fill(choice, selected ? Color.Lerp(new Color(.063f, .096f, .143f, 1f), accent, .12f) : new Color(.063f, .096f, .143f, 1f));
                Border(choice, selected ? accent : new Color(.22f, .32f, .42f, 1f), selected ? 2 : 1);
                Fill(new Rect(choice.x, choice.y, choice.width, 3), selected ? accent : new Color(.28f, .39f, .5f, 1f));
                Text(new Rect(choice.x + 17, choice.y + 16, 201, 19), selected ? "已选择" : "", 11, accent, true, false, TextAnchor.MiddleRight);
                DrawCrest(new Rect(choice.x + 74, choice.y + 40, 87, 90), hero, accent);
                Text(new Rect(choice.x + 17, choice.y + 145, 201, 36), GameBalance.ClassName(hero), 27, Color.white, true, false, TextAnchor.MiddleCenter);
                Text(new Rect(choice.x + 17, choice.y + 193, 201, 21), roles[i], 12, pale, false, false, TextAnchor.MiddleCenter);
                if (GUI.Button(choice, GUIContent.none, invisibleButton)) { selectedClass = hero; GameAudio.Play(SoundCue.UI); }
            }
            if (NavigationButton(new Rect(x + 314, y + 359, 186, 50), "返回首页", jade)) {titleCreatingHero=false;BlockUITransition();return;}
            if (PrimaryButton(new Rect(x + 520, y + 359, 206, 50), "开始冒险", gold, true, "以" + GameBalance.ClassName(selectedClass) + "创建独立存档，保留已有角色。", true)) StartSelectedHero();
#if !UNITY_IOS && !UNITY_ANDROID
            if(DangerButton(new Rect(x+800,y+359,206,50), "退出游戏", muted))RequestExit(false);
#endif
            string titleMessage = !string.IsNullOrEmpty(session.Notification) ? session.Notification : session.Progression.LastError;
            if (!string.IsNullOrEmpty(titleMessage))
            {
                float toastHeight = Mathf.Clamp(Style(13, false, true).CalcHeight(new GUIContent(titleMessage), 620) + 14, 36, 82);
                Rect toast = new Rect((width - 648) * .5f, height - toastHeight - 12, 648, toastHeight);
                Box(toast, gold, false);
                Text(new Rect(toast.x + 14, toast.y + 7, toast.width - 28, toast.height - 14), titleMessage, 13, pale, false, true, TextAnchor.MiddleCenter);
            }
        }

        private void RefreshSaveSlots()
        {
            saveSlots.Clear();
            List<SaveSlotInfo> available = session.Progression.GetSaveSlots();
            if (available != null) saveSlots.AddRange(available);
            if (!saveSlots.Exists(slot => slot.Id == selectedSaveId))
            {
                SaveSlotInfo preferred = saveSlots.Find(slot => slot.IsCurrent && slot.CanLoad) ?? saveSlots.Find(slot => slot.CanLoad) ?? saveSlots.Find(slot => true);
                selectedSaveId = preferred == null ? null : preferred.Id;
            }
            saveSlotsDirty = false;
        }

        private void OpenSaveSelection()
        {
            if (saveFlow.Open) return;
            saveSelectionFromPause = session.HasStarted;
            if (session.HasStarted) { session.SetPaused(true); session.SetUIBlocking(true); }
            RefreshSaveSlots();
            saveSelectionError = null;
            saveSelectionScroll = Vector2.zero;
            panel = Panel.SaveSelection;
            BlockUITransition();
        }

        private void ContinueSelectedSave()
        {
            SaveSlotInfo selected = saveSlots.Find(slot => slot.Id == selectedSaveId && slot.CanLoad);
            if (selected == null) return;
            if (session.HasStarted) { RequestLoadSelectedSave(selected); return; }
            if (!session.ContinueGame(selected.Id))
            {
                saveSelectionError = string.IsNullOrEmpty(session.SaveLoadError) ? "无法读取该存档，请返回后重新打开存档页重试。" : session.SaveLoadError;
                RefreshSaveSlots();
                return;
            }
            CompleteSaveLoadUI();
        }

        private void DrawSaveSelection()
        {
            if(DrawSaveFlowConfirmation())return;
            if(DrawSaveDeletionConfirmation())return;
            if(MobileControls.Active){DrawMobileSaveSelection();return;}
            if(!session.HasStarted)Fill(new Rect(0, 0, width, height), new Color(.018f, .029f, .048f, 1f));
            Rect w = Modal(900, 570, "选择存档", "");
            Text(new Rect(w.x + 620, w.y + 29, 186, 23), saveSlots.Count + " 份存档", 13, muted, false, false, TextAnchor.MiddleRight);
            if (PopupCloseButton(new Rect(w.xMax - 69, w.y + 20, 44, 32))) ClosePanel();
            Rect viewport = new Rect(w.x + 24, w.y + 113, 852, 347);
            float contentHeight = Mathf.Max(viewport.height, saveSlots.Count * 84);
            saveSelectionScroll = BeginTouchScroll("save-list",viewport,saveSelectionScroll,new Rect(0,0,837,contentHeight));
            if (saveSlots.Count == 0) Text(new Rect(20, 120, 797, 36), "暂无存档", 20, muted, true, false, TextAnchor.MiddleCenter);
            for (int i = 0; i < saveSlots.Count; i++)
            {
                SaveSlotInfo slot = saveSlots[i];
                Rect row = new Rect(0, i * 84, 833, 74);
                bool selected = slot.Id == selectedSaveId;
                Color accent = slot.CanLoad ? GameBalance.ClassColor(slot.HeroClass) : muted;
                Fill(row, selected ? new Color(.10f, .19f, .23f) : card);
                Border(row, selected ? gold : new Color(accent.r, accent.g, accent.b, .25f));
                if (slot.CanLoad) DrawCrest(new Rect(row.x + 12, row.y + 9, 52, 56), slot.HeroClass, accent);
                else DrawIcon(new Rect(row.x + 21, row.y + 22, 32, 32), UIIconAtlas.Utility("inventory"), muted);
                Text(new Rect(row.x + 81, row.y + 12, 307, 25), slot.DisplayName, 17, slot.CanLoad ? pale : muted, true);
                Text(new Rect(row.x + 81, row.y + 43, 307, 20), slot.CanLoad ? GameBalance.ClassName(slot.HeroClass)+" · Lv"+slot.Level : "存档暂时无法读取", 13, accent);
                string saved = slot.SavedAtUtc == System.DateTime.MinValue ? "保存时间未知" : slot.SavedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                Text(new Rect(row.x + 399, row.y + 17, 232, 21), saved, 13, muted);
                string state = slot.DeletionPending ? "删除未完成" : !slot.CanLoad ? "无法读取" : slot.RecoveredFromBackup ? "可从备份恢复" : slot.IsCurrent ? "当前存档" : "";
                Text(new Rect(row.x + 637, row.y + 13, 176, 22), state, 12, slot.RecoveredFromBackup ? gold : muted, false, false, TextAnchor.MiddleRight);
                if (selected) Text(new Rect(row.x + 637, row.y + 43, 176, 20), "已选择", 12, gold, true, false, TextAnchor.MiddleRight);
                bool prior = GUI.enabled;
                GUI.enabled = prior;
                if (GUI.Button(row, GUIContent.none, invisibleButton)) { selectedSaveId = slot.Id; saveSelectionError = null; GameAudio.Play(SoundCue.UI); }
                GUI.enabled = prior;
            }
            EndTouchScroll();
            if (!string.IsNullOrEmpty(saveSelectionError)) Text(new Rect(w.x + 27, w.y + 469, 846, 26), saveSelectionError, 12, gold, false, true);
            SaveSlotInfo selectedSlot = saveSlots.Find(slot => slot.Id == selectedSaveId && slot.CanLoad);
            if (NavigationButton(new Rect(w.x + 24, w.y + 506, 268, 40), "返回", jade)) ClosePanel();
            DrawDeleteSaveButton(new Rect(w.x + 312, w.y + 506, 268, 40));
            if (Button(new Rect(w.x + 600, w.y + 506, 276, 40), selectedSlot != null && selectedSlot.RecoveredFromBackup ? "从备份读取" : "读取存档", gold, selectedSlot != null, null, true)) ContinueSelectedSave();
        }

        private void StartSelectedHero()
        {
            saveSlotsDirty = true;
            panel = Panel.None;
            selectedItem = null;
            inventoryScroll = Vector2.zero;
            selectedSkill = 0;
            skillScroll = Vector2.zero;
            rebindingSlot = -1;
            session.StartNew(selectedClass);
            if(session.HasStarted)titleCreatingHero=false;
        }

        private void DrawCrest(Rect r, HeroClass hero, Color color)
        {
            int index = (int)hero;
            if (crestTextures[index] == null) crestTextures[index] = CreateCrest(hero, color);
            Color previous = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(r, crestTextures[index], ScaleMode.ScaleToFit, true);
            GUI.color = previous;
        }

        private static Texture2D CreateCrest(HeroClass hero, Color color)
        {
            const int size = 256;
            var pixels = new Color[size * size];
            Color dim = new Color(color.r, color.g, color.b, .46f);
            CrestStroke(pixels, size, 64, 9, 119, 64, dim, 1.5f);
            CrestStroke(pixels, size, 119, 64, 64, 119, dim, 1.5f);
            CrestStroke(pixels, size, 64, 119, 9, 64, dim, 1.5f);
            CrestStroke(pixels, size, 9, 64, 64, 9, dim, 1.5f);
            if (hero == HeroClass.Vanguard)
            {
                CrestStroke(pixels, size, 64, 24, 54, 41, color, 3.5f);
                CrestStroke(pixels, size, 54, 41, 58, 76, color, 3.5f);
                CrestStroke(pixels, size, 64, 24, 74, 41, color, 3.5f);
                CrestStroke(pixels, size, 74, 41, 70, 76, color, 3.5f);
                CrestStroke(pixels, size, 64, 32, 64, 75, Color.white, 2.5f);
                CrestStroke(pixels, size, 43, 78, 85, 78, color, 5);
                CrestStroke(pixels, size, 64, 79, 64, 99, color, 6);
                CrestStroke(pixels, size, 57, 102, 71, 102, color, 4);
            }
            else if (hero == HeroClass.Arcanist)
            {
                CrestStroke(pixels, size, 64, 23, 85, 64, color, 4);
                CrestStroke(pixels, size, 85, 64, 64, 105, color, 4);
                CrestStroke(pixels, size, 64, 105, 43, 64, color, 4);
                CrestStroke(pixels, size, 43, 64, 64, 23, color, 4);
                CrestStroke(pixels, size, 33, 64, 95, 64, color, 2.5f);
                CrestStroke(pixels, size, 64, 39, 64, 89, dim, 2.5f);
                CrestStroke(pixels, size, 64, 64, 64, 64, Color.white, 10);
            }
            else if (hero == HeroClass.Summoner)
            {
                CrestStroke(pixels, size, 45, 58, 35, 36, color, 5);
                CrestStroke(pixels, size, 35, 36, 58, 48, color, 5);
                CrestStroke(pixels, size, 83, 58, 93, 36, color, 5);
                CrestStroke(pixels, size, 93, 36, 70, 48, color, 5);
                CrestStroke(pixels, size, 45, 58, 45, 84, color, 4);
                CrestStroke(pixels, size, 45, 84, 64, 99, color, 4);
                CrestStroke(pixels, size, 64, 99, 83, 84, color, 4);
                CrestStroke(pixels, size, 83, 84, 83, 58, color, 4);
                CrestStroke(pixels, size, 51, 68, 55, 68, Color.white, 6);
                CrestStroke(pixels, size, 73, 68, 77, 68, Color.white, 6);
                CrestStroke(pixels, size, 64, 82, 64, 87, Color.white, 5);
                CrestStroke(pixels, size, 64, 18, 64, 35, dim, 4);
                CrestStroke(pixels, size, 55, 26, 73, 26, dim, 4);
            }
            else
            {
                CrestStroke(pixels, size, 80, 25, 57, 37, color, 4);
                CrestStroke(pixels, size, 57, 37, 46, 64, color, 4);
                CrestStroke(pixels, size, 46, 64, 57, 91, color, 4);
                CrestStroke(pixels, size, 57, 91, 80, 103, color, 4);
                CrestStroke(pixels, size, 80, 25, 69, 64, dim, 2.5f);
                CrestStroke(pixels, size, 69, 64, 80, 103, dim, 2.5f);
                CrestStroke(pixels, size, 31, 64, 103, 64, Color.white, 3);
                CrestStroke(pixels, size, 91, 53, 103, 64, color, 4);
                CrestStroke(pixels, size, 103, 64, 91, 75, color, 4);
                CrestStroke(pixels, size, 31, 55, 41, 64, color, 3);
                CrestStroke(pixels, size, 31, 73, 41, 64, color, 3);
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false)
            {
                name = "Class crest " + hero,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        // Bake antialiased strokes once. Drawing the finished texture never changes GUI.matrix.
        private static void CrestStroke(Color[] pixels, int size, float ax, float ay, float bx, float by, Color color, float thickness)
        {
            float factor = size / 128f;
            Vector2 a = new Vector2(ax, ay) * factor;
            Vector2 b = new Vector2(bx, by) * factor;
            Vector2 segment = b - a;
            float lengthSquared = segment.sqrMagnitude;
            float radius = thickness * factor * .5f;
            int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - radius - 1));
            int x1 = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + radius + 1));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - radius - 1));
            int y1 = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + radius + 1));
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                Vector2 point = new Vector2(x + .5f, y + .5f);
                float t = lengthSquared < .001f ? 0 : Mathf.Clamp01(Vector2.Dot(point - a, segment) / lengthSquared);
                float alpha = color.a * Mathf.Clamp01(radius + .75f - Vector2.Distance(point, a + segment * t));
                if (alpha <= 0) continue;
                int index = (size - 1 - y) * size + x;
                Color previous = pixels[index];
                float outAlpha = alpha + previous.a * (1 - alpha);
                pixels[index] = new Color((color.r * alpha + previous.r * previous.a * (1 - alpha)) / outAlpha,
                    (color.g * alpha + previous.g * previous.a * (1 - alpha)) / outAlpha,
                    (color.b * alpha + previous.b * previous.a * (1 - alpha)) / outAlpha, outAlpha);
            }
        }

        private void DrawHudVital(Rect rect,float fraction,Color tint,string label)
        {
            Fill(rect,new Color(.012f,.025f,.04f,1));
            Color fill=Color.Lerp(new Color(.012f,.025f,.04f),tint,.72f);fill.a=1;
            float inset=Mathf.Max(1,rect.height*.06f);
            Fill(new Rect(rect.x+inset,rect.y+inset,(rect.width-2*inset)*Mathf.Clamp01(fraction),rect.height-2*inset),fill);
            Border(rect,new Color(.40f,.53f,.61f),inset);
            var textStyle=Style(MobileControls.Active?TouchFont(11):12,true,false,TextAnchor.MiddleCenter);
            Color prior=textStyle.normal.textColor;
            textStyle.normal.textColor=new Color(0,0,0,1);
            float stroke=MobileControls.Active?TouchRatio:1;
            GUI.Label(new Rect(rect.x+stroke,rect.y+stroke,rect.width,rect.height),label,textStyle);
            textStyle.normal.textColor=Color.white;GUI.Label(rect,label,textStyle);
            textStyle.normal.textColor=prior;
        }
        private void DrawScreenExperience()
        {
            var p=session.Progression.Profile;float u=MobileControls.Active?TouchRatio:1;
            bool capped=p.level>=ProgressionService.MaximumLevel;
            // GUI coordinates start at the safe-area top; the XP rail ends at the physical screen bottom.
            float railHeight=18*u,railY=(Screen.height-guiOffset.y)/scale-railHeight;
            Rect rail=new Rect(-guiOffset.x/scale,railY,Screen.width/scale,railHeight);
            Bar(rail,capped?1:p.xp/(float)GameBalance.XpToNext(p.level),gold);
            var labelStyle=new GUIStyle(Style(Mathf.RoundToInt(11*u),true,false,TextAnchor.MiddleLeft));
            labelStyle.padding=new RectOffset(0,0,0,0);labelStyle.normal.textColor=Color.white;
            Fill(new Rect(0,railY,width,railHeight),new Color(.012f,.025f,.04f,.62f));
            GUI.Label(new Rect(8*u,railY,width-16*u,railHeight),"Lv."+p.level+"   "+(capped?"满级":p.xp+" / "+GameBalance.XpToNext(p.level)),labelStyle);

        }
        private void DrawHUD()
        {
            DrawComboCounter();
            DrawScreenExperience();
            if(MobileControls.Active){DrawMobileHUD();return;}
            GameProfile p = session.Progression.Profile;
            Color accent = GameBalance.ClassColor(p.heroClass);
            Rect playerRect = new Rect(16, 16, 240, 88);
            blockedRects.Add(playerRect);
            Box(playerRect, accent);
            Fill(new Rect(16, 16, 2, 88), accent);
            Text(new Rect(28, 24, 139, 22), GameBalance.ClassName(p.heroClass), 16, pale, true);
            DrawPrice(new Rect(169,26,74,20),p.gold,false,1);
            float hp = session.Player == null ? 0 : session.Player.Health;
            float maxHp = session.Player == null ? 1 : session.Player.MaxHealth;
            DrawHudVital(new Rect(28,48,216,23),hp/Mathf.Max(1,maxHp),new Color(.20f,.95f,.30f),Mathf.CeilToInt(hp)+" / "+Mathf.CeilToInt(maxHp));
            float energy=session.Player==null?0:session.Player.Energy;
            float maxEnergy=session.Player==null?100:session.Player.MaxEnergy;
            DrawHudVital(new Rect(28,75,216,21),energy/Mathf.Max(1,maxEnergy),new Color(.28f,.57f,.91f),Mathf.FloorToInt(energy)+" / "+Mathf.RoundToInt(maxEnergy));
            string objectiveText = session.SpecialAdventure?session.ModeName:session.InDungeon
                ? session.DungeonCleared ? "沉星遗迹已通关" : "击败本轮敌人"
                : p.level < 2 ? "击败原野怪物，升至 2 级"
                : p.skillRanks[0] == 0 ? "学习首个职业技能"
                : "前往北方的沉星遗迹";
            string objectiveProgress = session.SpecialAdventure?session.ModeObjectiveStatus:session.InDungeon
                ? session.DungeonCleared ? "靠近返营传送点离开" : "第 " + session.DungeonWave + " / " + session.TotalWaves + " 波 · 剩余 " + session.Enemies.Count + " 个敌人"
                : p.level < 2 ? "经验 " + p.xp + " / " + GameBalance.XpToNext(p.level)
                : p.skillRanks[0] == 0 ? "可用精通点 " + p.skillPoints + " · K"
                : "收集装备，进入传送门 · T";
            if(session.ChapterActive){objectiveText=ChapterDefinition.Get(session.ActiveChapterNode).Name;objectiveProgress=session.ChapterObjectiveStatus;}
            string growthTitle,growthDetail;bool hasGrowth=TryGrowthHudHint(out growthTitle,out growthDetail);
            if(!session.ChapterActive&&hasGrowth){objectiveText=growthTitle;objectiveProgress=growthDetail;}
            bool showObjective=session.InDungeon||session.ChapterActive||session.SpecialAdventure||hasGrowth||p.level<2||p.skillRanks[0]==0;
            var chapterFirstSeal=session.ChapterSealView(0);
            var roomFirstSeal=session.ChapterActive?null:session.RoomSealView(0);
            if(chapterFirstSeal!=null&&session.ChapterRun.DoorUnlocked)objectiveText="双印完成 · 前往出口";
            if(roomFirstSeal!=null&&session.RoomChainRun.DoorUnlocked)objectiveText="双印完成 · 前往北门";
            float bodyHeight=Mathf.Max(24,Style(15,true,true).CalcHeight(new GUIContent(objectiveText),255));
            string progressText=PlatformText(objectiveProgress);
            float progressHeight=Mathf.Max(18,Style(12,false,true).CalcHeight(new GUIContent(progressText),255));
            bool showSeals=chapterFirstSeal!=null;
            bool showRoomSeals=roomFirstSeal!=null;
            if(showSeals)progressHeight=44;
            else if(showRoomSeals)progressHeight=66;
            bool showCharge=session.InDungeon&&session.ChallengeRun;
            string chargeText="治疗充能  "+session.HealingCharges+" / 3";
            float chargeHeight=showCharge?Mathf.Max(23,Style(17,true,true).CalcHeight(new GUIContent(chargeText),255)):0;
            var measured=new ObjectiveCardLayout(17,bodyHeight,progressHeight,chargeHeight);
            if(showObjective)
            {
            Rect objective=new Rect(16,116,282,measured.Height);blockedRects.Add(objective);Box(objective,jade,false);
            Fill(new Rect(objective.x,objective.y,3,objective.height),jade);
            Text(new Rect(objective.x+13,objective.y+measured.HeadingY,255,17),"当前目标",11,jade,true);
            Text(new Rect(objective.x+13,objective.y+measured.BodyY,255,bodyHeight),objectiveText,15,pale,true,true);
            if(showSeals)DrawChapterSeals(new Rect(objective.x+13,objective.y+measured.ProgressY,255,44),1,chapterFirstSeal,session.ChapterSealView(1));
            else if(showRoomSeals)DrawRoomSeals(new Rect(objective.x+13,objective.y+measured.ProgressY,255,66),1,false,roomFirstSeal,session.RoomSealView(1),session.RoomObjectiveView);
            else Text(new Rect(objective.x+13,objective.y+measured.ProgressY,255,progressHeight),progressText,12,muted,false,true);
            if(showCharge)Text(new Rect(objective.x+13,objective.y+measured.ChargeY,255,chargeHeight),chargeText,17,gold,true,true);
            }
            DrawMinimap();
            DrawHotbar();
            DrawCompanionCommands();
            Text(new Rect(hotbarBounds.x-170,hotbarBounds.y-22,622,18),CurrentCombatOpportunity(),12,gold,true,false,TextAnchor.MiddleCenter);
            Text(new Rect(hotbarBounds.x-170,hotbarBounds.y-43,622,18),CurrentCombatResult(),11,pale,true,false,TextAnchor.MiddleCenter);
            DrawChargeProgress();
            DrawDungeonStatus();
            DrawEdgeActions();
            DrawExpeditionHUD();
            DrawTownActivityEntry();
            EnemyController target = session.Player == null ? null : session.Player.AimTarget;
            if (target != null && !target.IsDead)
            {
                bool neutral = target.Tier == EnemyController.ThreatTier.Normal && !target.IsAggro;
                string state = neutral ? "中立" : target.Tier == EnemyController.ThreatTier.Normal ? "反击中" : "主动敌人";
                Color tint = neutral ? jade : target.Tier == EnemyController.ThreatTier.Elite ? gold : new Color(1, .55f, .45f);
                string effects = target.StatusEffects == null ? "" : target.StatusEffects.Summary;
                Rect targetInfo = new Rect((width - 560) * .5f, 68, 560, 19);
                blockedRects.Add(targetInfo);
                Text(targetInfo, target.DisplayName + " · " + state + (string.IsNullOrEmpty(effects) ? "" : " · " + effects), 11, tint, true, false, TextAnchor.MiddleCenter);
                if (targetInfo.Contains(Mouse) && GUI.enabled) tooltip = target.DisplayName + "\n" + target.TraitDescription;
            }
        }

        private void DrawTargetingHint()
        {
            if(MobileControls.Active)return;
            SkillTargetingController targeting = session.Player == null ? null : session.Player.GetComponent<SkillTargetingController>();
            if (targeting == null || !targeting.IsTargeting) return;
            Rect strip = new Rect((width - 490) * .5f, height - (MobileControls.Active ? 235 : 197), 490, 43);
            blockedRects.Add(strip);
            Box(strip, jade, false);
            Fill(new Rect(strip.x, strip.y, 3, strip.height), jade);
            Text(new Rect(strip.x + 10, strip.y + 5, strip.width - 20, 17), "准备施放 · " + targeting.SkillName, 12, jade, true, false, TextAnchor.MiddleCenter);
            Text(new Rect(strip.x + 10, strip.y + 25, strip.width - 20, 14), MobileControls.Active ? "点选地面并松开，或点击确认按钮 · 点击取消按钮取消" : targeting.Hint, 10, pale, false, false, TextAnchor.MiddleCenter);
        }

        private void DrawChargeProgress()
        {
            SkillChargeController charge = session.Player == null ? null : session.Player.GetComponent<SkillChargeController>();
            if (charge == null || !charge.IsCharging || charge.SkillIndex < 0) return;
            GameProfile profile = session.Progression.Profile;
            Rect strip = new Rect((width - 226) * .5f, height - (MobileControls.Active ? 224 : 186), 226, 35);
            blockedRects.Add(strip);
            Box(strip, GameBalance.ClassColor(profile.heroClass), false);
            DrawIcon(new Rect(strip.x + 5, strip.y + 4, 27, 27), UIIconAtlas.Skill(profile.heroClass, charge.SkillIndex), Color.white);
            Text(new Rect(strip.x + 40, strip.y + 3, 178, 15), GameBalance.SkillName(profile.heroClass, charge.SkillIndex), 11, pale, true);
            Bar(new Rect(strip.x + 40, strip.y + 23, 175, 5), charge.Progress, GameBalance.ClassColor(profile.heroClass));
            if (strip.Contains(Mouse)) tooltip = MobileControls.Active ? "蓄力中 · 移动减速\n点击闪现或取消按钮中断蓄力。" : "蓄力中 · 移动减速\nShift 闪现 / 右键 / Esc 取消。";
        }

        private void Bar(Rect rect, float fraction, Color color)
        {
            Fill(rect, new Color(.11f, .15f, .18f));
            float filled = rect.width * Mathf.Clamp01(fraction);
            Fill(new Rect(rect.x, rect.y, filled, rect.height), color);
            Fill(new Rect(rect.x, rect.y, filled, Mathf.Min(2, rect.height)), new Color(1, 1, 1, .2f));
        }

        private Rect DesktopMinimapRect { get { return new Rect(width-166,70,150,154); } }
        private void DrawMinimap()
        {
            float x = width - 166;
            Rect map = DesktopMinimapRect;
            blockedRects.Add(map);
            Box(map, jade);
            Text(new Rect(x + 8, 77, 134, 19), session.ZoneName, 11, pale, true, false, TextAnchor.MiddleCenter);
            Rect field = new Rect(x + 11, 103, 128, 109);
            Fill(field, new Color(.055f, .11f, .14f));
            for (int i = 1; i < 4; i++)
            {
                Fill(new Rect(field.x + field.width * i / 4, field.y, 1, field.height), new Color(.15f, .23f, .25f, .4f));
                Fill(new Rect(field.x, field.y + field.height * i / 4, field.width, 1), new Color(.15f, .23f, .25f, .4f));
            }
            DrawMinimapTerrain(field);
            if (!session.InDungeon)
            {
                MapDot(field, new Vector3(0, 0, -10), gold, 7);
                MapDot(field, new Vector3(0, 0, 11), jade, 7);
                for (int npc = 0; npc < 2; npc++) MapDot(field, GameSession.HubNpcPosition(npc), gold, 4);
            }
            for (int i = 0; i < session.Enemies.Count; i++)
            {
                EnemyController enemy = session.Enemies[i];
                if (enemy != null && !enemy.IsDead)
                {
                    Color dot = enemy.Tier == EnemyController.ThreatTier.Boss ? new Color(1, .32f, .3f) : enemy.Tier == EnemyController.ThreatTier.Elite ? gold : enemy.IsAggro ? new Color(1, .58f, .35f) : new Color(.54f, .77f, .5f);
                    MapDot(field, enemy.transform.position, dot, enemy.IsBoss ? 6 : 3);
                }
            }
            if (session.Player != null) MapDot(field, session.Player.transform.position, jade, 6);
            if (GUI.Button(map, GUIContent.none, invisibleButton)) OpenTravelMap();
        }

        private void MapDot(Rect map, Vector3 position, Color color, float size)
        {
            map=new Rect(map.x+3,map.y+3,map.width-6,map.height-6);
            float radius = Mathf.Max(1f, session.ArenaRadius);
            float x = map.x + map.width * Mathf.InverseLerp(-radius, radius, position.x);
            float y = map.yMax - map.height * Mathf.InverseLerp(-radius, radius, position.z);
            DrawMapMarker(new Rect(x-size*.5f-1,y-size*.5f-1,size+2,size+2),ink);
            DrawMapMarker(new Rect(x-size*.5f,y-size*.5f,size,size),color);
        }

        private void DrawDungeonStatus()
        {
            if (!session.InDungeon || session.ChapterFinished) return;
            if(session.DungeonCleared)return;
            EnemyController boss = null;
            for (int i = 0; i < session.Enemies.Count; i++)
            {
                EnemyController enemy = session.Enemies[i];
                if (enemy != null && enemy.IsBoss && !enemy.IsDead) { boss = enemy; break; }
            }
            if (boss == null) return;
            float bossWidth=Mathf.Clamp(width-860,160,462);
            Rect bossBar = new Rect((width-bossWidth)*.5f,8,bossWidth,58);
            blockedRects.Add(bossBar);
            Box(bossBar, new Color(1f, .4f, .32f));
            Text(new Rect(bossBar.x + 14, bossBar.y + 8, bossBar.width-28, 25), boss.DisplayName, 16, gold, true, false, TextAnchor.MiddleCenter);
            Bar(new Rect(bossBar.x + 17, bossBar.y + 40, bossBar.width-34, 8), boss.Health / Mathf.Max(1, boss.MaxHealth), new Color(.89f, .33f, .28f));
        }

        private void DrawHotbar()
        {
            GameProfile p = session.Progression.Profile;
            bool mobile = MobileControls.Active;
            Rect bar = hotbarBounds;
            if (!mobile && session.Player != null && session.Player.DodgeCooldown > .01f)
            {
                Rect cooldown = new Rect(bar.xMax-46,bar.y-58,44,44);
                Fill(cooldown,ink);Border(cooldown,jade);
                DrawIcon(new Rect(cooldown.x+4,cooldown.y+4,36,36),UIIconAtlas.Utility("blink"),new Color(1,1,1,.4f));
                Text(cooldown,session.Player.DodgeCooldown.ToString("0.0"),18,Color.white,true,false,TextAnchor.MiddleCenter);
            }

            float x = bar.x;
            float y = bar.y;
            blockedRects.Add(bar);
            Box(bar, jade);
            string basicCaption=mobile?"":DesktopBasicOpportunityCaption();
            if(!string.IsNullOrEmpty(basicCaption))Text(new Rect(x+10,y-19,bar.width-20,18),basicCaption,10,pale,true,false,TextAnchor.MiddleCenter);
            for (int slotIndex = 0; slotIndex < GameBalance.HotbarSize; slotIndex++)
            {
                int skill = LearnedSkillAtSlot(p, slotIndex);
                bool potion = skill == GameBalance.HotbarPotion;
                bool empty = skill == -1;
                int rank = skill < 0 ? 0 : p.skillRanks[skill];
                bool locked = empty || potion && (session.ChallengeRun && session.InDungeon ? session.HealingCharges : p.potions) <= 0;
                float cost = skill < 0 ? 0 : GameBalance.SkillEnergyCost(p.heroClass, skill);
                bool lacksEnergy = !locked && session.Player != null && session.Player.Energy < cost;
                float cooldown = skill < 0 || session.Player == null ? 0 : session.Player.CooldownRemaining(slotIndex);
                bool actionable=false;
                string actionCaption=mobile?"":DesktopSkillOpportunityCaption(skill,locked,lacksEnergy,cooldown,out actionable);
                Rect slot = hotbarSlots[slotIndex];
                // Dedicated clock row is never replaced by cooldown/failure/result captions.
                var window=mobile||locked||session.Player==null?default(CombatOpportunityState):session.Player.SkillOpportunityWindow(skill);
                if(window.Window)Text(new Rect(slot.x,slot.y-12,slot.width,11),window.Caption,8,window.Actionable?jade:muted,true,false,TextAnchor.MiddleCenter);

                Color accent = empty ? muted : potion ? gold : UIIconAtlas.SkillColor(p.heroClass, skill);
                Fill(slot, locked ? new Color(.04f, .06f, .085f) : card);
                bool ready=session.Player!=null&&(potion?!session.InputBlocked&&!session.Player.IsDead&&!locked&&session.Player.Health<session.Player.MaxHealth-.5f:skill>=0&&session.Player.IsSkillAvailable(skill));
                if (!empty)
                {
                    // The slot owns its only frame; the glyph uses the whole interior.
                    Rect identity=new Rect(slot.x+1,slot.y+1,slot.width-2,slot.height-2);
                    Color tint=!ready?new Color(.5f,.55f,.6f,.85f):Color.white;
                    if(potion)DrawIcon(identity,HotbarIcon(p,skill),tint);
                    else DrawRecoveringSkill(identity,UIIconAtlas.Skill(p.heroClass,skill,48),tint,skill,ready);
                }
                else Text(new Rect(slot.x, slot.y + 9, slot.width, 32), "+", 20, new Color(.34f, .44f, .53f), false, false, TextAnchor.MiddleCenter);
                if (cooldown > .01f)
                {
                    Text(new Rect(slot.x, slot.y + 12, slot.width, 29), cooldown.ToString(cooldown >= 10 ? "0" : "0.0"), 15, pale, true, false, TextAnchor.MiddleCenter);
                }
                DrawSkillStock(slot,skill,mobile?TouchRatio:1f);
                string key = GameBalance.KeyName(p.hotbarKeys[slotIndex]);
                if (!mobile)
                {
                    Fill(new Rect(slot.x + 2, slot.y + 2, Mathf.Max(14, key.Length * 7 + 4), 13), new Color(.015f, .025f, .04f, .93f));
                    Text(new Rect(slot.x + 4, slot.y + 1, 39, 15), key, 9, locked ? muted : pale, true);
                }
                if (lacksEnergy) Fill(new Rect(slot.x + 2, slot.yMax - 3, slot.width - 4, 2), new Color(.45f, .64f, 1f));
                if (potion)
                {
                    string count = "×"+(session.ChallengeRun && session.InDungeon ? session.HealingCharges : p.potions).ToString();
                    Vector2 countSize=Style(11,true).CalcSize(new GUIContent(count));
                    float countWidth=Mathf.Max(24,countSize.x+6),countHeight=Mathf.Max(22,countSize.y+4);
                    Rect countBadge=new Rect(slot.xMax-countWidth-2,slot.yMax-countHeight-2,countWidth,countHeight);
                    Fill(countBadge,new Color(.015f,.025f,.04f,.94f));
                    Text(countBadge,count,11,locked?muted:pale,true,false,TextAnchor.MiddleCenter);
                }

                if(actionCaption.Length>0)
                {
                    Rect caption=new Rect(slot.x+2,slot.yMax-12,slot.width-4,11);
                    Fill(caption,new Color(.025f,.055f,.06f,.96f));
                    Text(caption,actionCaption,8,actionable?jade:gold,true,false,TextAnchor.MiddleCenter);
                }
                bool hover = slot.Contains(Mouse);
                bool selected=hover&&GUI.enabled||hotbarDragging&&!hotbarPointerConfiguring&&(slotIndex==hotbarPointerSlot||hover);
                Border(slot,selected?gold:ready?jade:new Color(accent.r,accent.g,accent.b,locked?.23f:.55f),selected||ready?2:1);
                if (hover && GUI.enabled)
                {
                    tooltip = potion ? PotionTooltip(p) : !mobile&&skill>=0 ? SkillTooltip(p,skill,rank) : null;
                    if(!string.IsNullOrEmpty(tooltip)){tooltipAnchor=slot;tooltipAnchorText=tooltip;}
                }
                if (!session.PracticeActive && !mobile && hover && GUI.enabled && Event.current.type == EventType.MouseDown && Event.current.button == 1)
                {
                    Event.current.Use();
                    if (potion) TogglePanel(Panel.Inventory);
                    else { if (!empty) SelectSkill(skill); TogglePanel(Panel.Skills); }
                }
            }
            HandleHotbarPointer(hotbarSlots, false);
        }

        private void HandleHotbarPointer(Rect[] slots, bool configuring)
        {
            if (MobileControls.Active) return;
            int control = GUIUtility.GetControlID(configuring ? 192702 : 192701, FocusType.Passive);
            if (!GUI.enabled) return;
            Event input = Event.current;
            int hovered = -1;
            for (int i = 0; i < slots.Length; i++) if (slots[i].Contains(input.mousePosition)) { hovered = i; break; }
            if (input.type == EventType.MouseDown && input.button == 0 && hovered >= 0)
            {
                BeginHotbarPointer(hovered, input.mousePosition, configuring);
                hotbarPointerControl = control;
                GUIUtility.hotControl = control;
                input.Use();
                return;
            }
            if (hotbarPointerSlot < 0 || hotbarPointerConfiguring != configuring) return;
            if (input.type == EventType.MouseDrag && input.button == 0)
            {
                ContinueHotbarPointer(input.mousePosition);
                input.Use();
            }
            else if (input.type == EventType.MouseUp && input.button == 0)
            {
                ContinueHotbarPointer(input.mousePosition);
                CompleteHotbarPointer(hovered);
                input.Use();
            }
            else if (input.type == EventType.KeyDown && input.keyCode == KeyCode.Escape)
            {
                CancelHotbarPointer();
                input.Use();
            }
        }

        private void BeginHotbarPointer(int slot, Vector2 position, bool configuring)
        {
            if (slot < 0 || slot >= GameBalance.HotbarSize || !session.CanChangeLoadout) return;
            hotbarPointerSlot = slot;
            hotbarPointerPage = session.Progression.Profile.hotbarPage;
            hotbarPointerSkill = LearnedSkillAtSlot(session.Progression.Profile, slot);
            hotbarPointerConfiguring = configuring;
            hotbarPointerOrigin = position;
            hotbarDragging = false;
        }

        private void ContinueHotbarPointer(Vector2 position)
        {
            if (hotbarPointerSlot >= 0 && hotbarPointerSkill != -1 &&
                (position - hotbarPointerOrigin).sqrMagnitude * scale * scale >= 36f) hotbarDragging = true;
        }

        private void CompleteHotbarPointer(int targetSlot)
        {
            int source = hotbarPointerSlot;
            int skill = hotbarPointerSkill;
            bool dragged = hotbarDragging;
            bool configuring = hotbarPointerConfiguring;
            bool valid = source >= 0 && targetSlot >= 0 && targetSlot < GameBalance.HotbarSize &&
                session.CanChangeLoadout && !session.Paused && !session.IsDead &&
                (configuring ? panel == Panel.Skills : panel == Panel.None) && session.Progression.Profile.hotbarPage == hotbarPointerPage &&
                LearnedSkillAtSlot(session.Progression.Profile, source) == skill;
            CancelHotbarPointer();
            if (!valid) return;
            if (session.PracticeActive && (configuring || dragged || skill < 0 && skill != GameBalance.HotbarPotion)) return;
            if (dragged)
            {
                if (source != targetSlot && session.MoveHotbarSkill(source, targetSlot)) GameAudio.Play(SoundCue.UI);
                return;
            }
            if (source != targetSlot) return;
            if (configuring)
            {
                GameProfile profile = session.Progression.Profile;
                if (profile.skillRanks[selectedSkill] <= 0 || GameBalance.IsPassive(selectedSkill)) return;
                if (session.AssignSkill(source, skill == selectedSkill ? -1 : selectedSkill)) GameAudio.Play(SoundCue.UI);
            }
            else if (skill == GameBalance.HotbarPotion) session.UseHotbarConsumable();
            else if (skill < 0) { TogglePanel(Panel.Skills); GameAudio.Play(SoundCue.UI); }
            else
            {
                SkillTargetingController targeting = session.Player == null ? null : session.Player.GetComponent<SkillTargetingController>();
                if (targeting != null) targeting.Begin(skill);
            }
        }

        private void CancelHotbarPointer()
        {
            CancelMobileCast();CancelMobileScroll();
            if (hotbarPointerControl != 0 && GUIUtility.hotControl == hotbarPointerControl) GUIUtility.hotControl = 0;
            hotbarReleaseFrame = Time.frameCount;
            suppressHotbarMouse = Input.GetMouseButton(0);
            hotbarPointerSlot = hotbarPointerPage = hotbarPointerSkill = -1;
            hotbarPointerControl = 0;
            hotbarTouchFinger = -1000;
            hotbarDragging = false;
        }

        private static void DrawIcon(Rect r, Texture2D texture, Color tint)
        {
            if (texture == null) return;
            texture = AuthoredIconArt.ForTint(texture, tint);
            texture=UIIconAtlas.ForDisplay(texture,Mathf.Max(r.width,r.height)*Mathf.Abs(GUI.matrix.lossyScale.x));
            Color previous = GUI.color;
            tint = AuthoredIconArt.DisplayTint(texture, tint);
            tint.a*=controlOpacity;GUI.color = tint;
            GUI.DrawTexture(r, texture, ScaleMode.ScaleToFit, true);
            GUI.color = previous;
        }

        private bool IconButton(Rect r, string icon, string key, string hint, Color accent, string badge = null)
        {
            blockedRects.Add(r);
            bool hover = r.Contains(Mouse) && GUI.enabled;
            Fill(r, hover ? new Color(.11f, .18f, .21f) : ink);
            Border(r, new Color(accent.r, accent.g, accent.b, hover ? .9f : .35f));
            DrawIcon(new Rect(r.x + 7, r.y + 8, r.width - 14, r.height - 13), UIIconAtlas.Utility(icon), Color.white);
            Badge(r,icon=="inventory"?NewEquipmentAttention||Attention.LootPending:icon=="skills"?Attention.Skills:icon=="camp"?Attention.Rewards:icon=="confirm"?session.Progression.ClaimableAchievements>0:false);
            if (!MobileControls.Active) Text(new Rect(r.x + 3, r.y + 1, r.width - 6, 12), key, 8, pale, true);
            if (!string.IsNullOrEmpty(badge))
            {
                Rect label = new Rect(r.xMax - 21, r.yMax - 15, 20, 14);
                Fill(label, new Color(.06f, .08f, .10f));
                Text(label, badge, 9, gold, true, false, TextAnchor.MiddleCenter);
            }
            if (hover){tooltip=PlatformText(hint);tooltipAnchor=r;tooltipAnchorText=tooltip;tooltipKey=key;}
            bool clicked = GUI.Button(r, GUIContent.none, invisibleButton);
            if (clicked) GameAudio.Play(SoundCue.UI);
            return clicked;
        }

        private void DrawEdgeActions()
        {
            GameProfile p = session.Progression.Profile;
            float x = width - 330;
            float y = 18;
            if(HubServicesAvailable)
            {
                if(IconButton(new Rect(x + 46,y,38,38),"shop","P","商店",jade))OpenHubService(HubNpcKind.Merchant);
                if(IconButton(new Rect(x + 92,y,38,38),"smith","O","铁匠",jade))OpenHubService(HubNpcKind.Blacksmith);
            }
            if(IconButton(new Rect(x + 138,y,38,38),"confirm","J","成就",jade))OpenProgressionGoals();

            if (IconButton(new Rect(x + 230, y, 38, 38), "inventory", "I", "行囊", jade))
                TogglePanel(Panel.Inventory);
            if (IconButton(new Rect(x + 184, y, 38, 38), "skills", "K", "技能", jade, p.skillPoints > 0 ? "+" + p.skillPoints : null))
                TogglePanel(Panel.Skills);
            if (IconButton(new Rect(x, y, 38, 38), "camp", "H", "回到起点", jade))
                session.ReturnToOrigin();
            if (IconButton(new Rect(x + 276, y, 38, 38), "settings", "Esc", "设置", jade)) session.SetPaused(true);
        }

        private static int SkillAtSlot(GameProfile profile, int slot)
        {
            if (profile == null || slot < 0 || slot >= GameBalance.HotbarSize) return -1;
            int index = profile.hotbarPage * GameBalance.HotbarSize + slot;
            if (profile.equippedSkills == null || index < 0 || index >= profile.equippedSkills.Length) return -1;
            int skill = profile.equippedSkills[index];
            return skill == GameBalance.HotbarPotion || skill >= 0 && skill < GameBalance.SkillCount && !GameBalance.IsPassive(skill) ? skill : -1;
        }

        private static int LearnedSkillAtSlot(GameProfile profile, int slot)
        {
            int skill = SkillAtSlot(profile, slot);
            if (skill == GameBalance.HotbarPotion) return skill;
            return skill >= 0 && profile.skillRanks != null && skill < profile.skillRanks.Length && profile.skillRanks[skill] > 0 ? skill : -1;
        }

        private static string SlotSkillName(GameProfile profile, int slot)
        {
            int skill = LearnedSkillAtSlot(profile, slot);
            return skill == GameBalance.HotbarPotion ? "生命药剂" : skill < 0 ? "未配置" : GameBalance.SkillName(profile.heroClass, skill);
        }

        private void DrawSlotIdentity(Rect r, GameProfile profile, int slot, Color tint)
        {
            int skill = LearnedSkillAtSlot(profile, slot);
            float iconSize = Mathf.Min(24, r.height);
            if (skill != -1) DrawIcon(new Rect(r.x, r.y + (r.height - iconSize) * .5f, iconSize, iconSize), HotbarIcon(profile, skill), Color.white);
            Text(new Rect(r.x + (skill == -1 ? 0 : iconSize + 4), r.y, r.width - (skill == -1 ? 0 : iconSize + 4), r.height), SlotSkillName(profile, slot), 11, tint, false, false, TextAnchor.MiddleLeft);
        }

        private static Texture2D HotbarIcon(GameProfile profile, int entry)
        {
            return entry == GameBalance.HotbarPotion ? UIIconAtlas.Utility("potion") : UIIconAtlas.Skill(profile.heroClass, entry);
        }

        private string PotionTooltip(GameProfile profile)
        {
            return "恢复 50% 最大生命\n" + (session.ChallengeRun && session.InDungeon ? "治疗充能 " + session.HealingCharges + "/3" : "数量 " + profile.potions);
        }

        private static int AssignedSlot(GameProfile profile, int skill)
        {
            for (int i = 0; i < GameBalance.HotbarSize; i++) if (SkillAtSlot(profile, i) == skill) return i;
            return -1;
        }

        private void ChangePage(int direction)
        {
            if (hotbarPointerSlot >= 0) CancelHotbarPointer();
            int page = (session.Progression.Profile.hotbarPage + direction + GameBalance.HotbarPages) % GameBalance.HotbarPages;
            session.SetHotbarPage(page);
        }

        private void SelectSkill(int skill)
        {
            selectedSkill = Mathf.Clamp(skill, 0, GameBalance.SkillCount - 1);
            skillScroll.y = Mathf.Clamp(GameBalance.SkillTreeRow(selectedSkill) * 102 - 150, 0, 738 - 468);
        }

        private Rect Modal(float modalWidth, float modalHeight, string title, string subtitle)
        {

            Rect window = new Rect((width - modalWidth) * .5f, (height - modalHeight) * .5f, modalWidth, modalHeight);
            Box(window, jade);
            Fill(new Rect(window.x, window.y, 4, window.height), jade);
            Text(new Rect(window.x + 24, window.y + 19, modalWidth - 105, 35), title, 27, pale, true);
            Text(new Rect(window.x + 24, window.y + 60, modalWidth - 100, 23), subtitle, 13, muted);
            Rule(window.x + 24, window.y + 94, modalWidth - 48, jade);
            return window;
        }

        private void DrawInventory()
        {
            if(MerchantServiceActive||SmithServiceActive){DrawHubEquipmentService();return;}
            if(MobileControls.Active){DrawMobileInventory();return;}
            PrepareInventoryPopupInput();
            ProgressionService progression = session.Progression;
            GameProfile p = progression.Profile;
            RebuildBagItems();
            ItemData picked = ResolveSelectedItem();
            Rect w = Modal(1160, 638, HubInventoryTitle, "");
            Text(new Rect(w.x + 24, w.y + 59, 238, 31), HubInventoryHint, 11, muted, false, true);
            Text(new Rect(w.x + 530, w.y + 55, 600, 31), HubNpcServiceSubtitle(""), 13, pale, false, true);
            if (PopupCloseButton(new Rect(w.xMax - 69, w.y + 20, 44, 32))) ClosePanel();
            DrawPrice(new Rect(w.x + 921, w.y + 28, 136, 30),p.gold,false,1);
            float left = w.x + 24;
            DrawCurrentWear(new Rect(left,w.y+100,232,232),1);
            StatBlock stats = progression.GetStats();
            Rule(left, w.y + 337, 232, jade);
            Text(new Rect(left, w.y + 346, 232, 22), "角色属性", 15, jade, true);
            DrawCharacterStats(new Rect(left,w.y+374,232,230),1);

            float middle = w.x + 272;
            Rect bagArea=new Rect(middle,w.y+144,864,462);
            if(inventoryFashionOpen){mobileInventoryTab=3;inventoryFashionOpen=false;inventoryComparisonOpen=false;}
            if(QuietAction(new Rect(middle,w.y+108,64,32),"装备",true,null,mobileInventoryTab==0))SelectInventoryTab(0);
            if(QuietAction(new Rect(middle+76,w.y+108,64,32),"道具",true,null,mobileInventoryTab==2))SelectInventoryTab(2);
            if(QuietAction(new Rect(middle+152,w.y+108,64,32),"时装",true,null,mobileInventoryTab==3))SelectInventoryTab(3);
            if(mobileInventoryTab==3){DrawBagFashion(InventoryArea(bagArea));return;}
            if(mobileInventoryTab==2){DrawBagSupplies(InventoryArea(bagArea));return;}

            Text(new Rect(middle + 690, w.y + 117, 144, 17), "总容量 " + p.inventory.Count + " / " + ProgressionService.InventoryCapacity, 11, muted, false, false, TextAnchor.MiddleRight);
            Rect viewport=bagArea;
            DrawEquipmentIconGrid(viewport,ref inventoryScroll,1);
        }

        private void DrawFashion()
        {
            panel=Panel.Inventory;inventoryFashionOpen=true;DrawInventory();
        }

        private void DrawPotionAssignment()
        {
            GameProfile p = session.Progression.Profile;
            Rect w = Modal(820, 366, "生命药剂", "选择十格快捷栏中的位置");
            if (PopupCloseButton(new Rect(w.xMax - 69, w.y + 20, 44, 32))) ClosePanel();
            for (int slot = 0; slot < GameBalance.HotbarSize; slot++)
            {
                int entry = LearnedSkillAtSlot(p, slot);
                bool current = entry == GameBalance.HotbarPotion;
                Rect tile = new Rect(w.x + 24 + slot % 5 * 156, w.y + 114 + (slot / 5) * 76, 148, 64);
                Fill(tile, current ? new Color(.13f, .2f, .2f) : card);
                Border(tile, current ? gold : new Color(jade.r, jade.g, jade.b, .4f));
                Text(new Rect(tile.x + 9, tile.y + 5, tile.width - 18, 19), MobileControls.Active ? "位置 " + (slot + 1) : GameBalance.KeyName(p.hotbarKeys[slot]), 13, pale, true);
                DrawSlotIdentity(new Rect(tile.x + 9, tile.y + 30, tile.width - 18, 25), p, slot, pale);
                if (tile.Contains(Mouse)) tooltip = current ? "从此槽移除生命药剂" : "放入此槽";
                if (GUI.Button(tile, GUIContent.none, invisibleButton) && session.CanChangeLoadout)
                {
                    bool changed = current ? session.AssignSkill(slot, -1) : session.Progression.AssignConsumable(slot);
                    Feedback(changed, current ? "已移除药剂快捷栏" : "生命药剂已放入快捷栏");
                    if (changed) ClosePanel();
                }
            }
            if (NavigationButton(new Rect(w.xMax - 244, w.y + 289, 220, 42), "返回行囊", jade)) ClosePanel();
        }

        private void RebuildBagItems()
        {
            bagItems.Clear();
            unequippedCount = 0;
            List<ItemData> inventory = session.Progression.Profile.inventory;
            for (int i = inventory.Count - 1; i >= 0; i--)
                if (inventory[i] != null)
                {
                    if(IsEquipped(inventory[i]))continue;
                    unequippedCount++;
                    if (inventoryFilter < 0 || (int)inventory[i].slot == inventoryFilter) bagItems.Add(inventory[i]);
                }
            bagItems.Sort(CompareInventoryItems);
        }

        private int CompareInventoryItems(ItemData a, ItemData b)
        {
            int comparison=IsEquipmentUpgrade(b).CompareTo(IsEquipmentUpgrade(a));
            if(comparison!=0)return comparison;
            comparison=EquipmentPreviewScore(b).CompareTo(EquipmentPreviewScore(a));
            if (comparison != 0) return comparison;
            comparison = b.level.CompareTo(a.level);
            if (comparison != 0) return comparison;
            comparison = b.rarity.CompareTo(a.rarity);
            return comparison != 0 ? comparison : string.CompareOrdinal(a.id, b.id);
        }

        private ItemData ResolveSelectedItem()
        {
            List<ItemData> inventory = session.Progression.Profile.inventory;
            for (int i = 0; i < inventory.Count; i++)
                if (inventory[i] != null && inventory[i].id == selectedItem && (IsEquipped(inventory[i]) || bagItems.Contains(inventory[i]))) return inventory[i];
            ItemData replacement = bagItems.Count > 0 ? bagItems[0] : session.Progression.Equipped(ItemSlot.Weapon);
            if (replacement == null)
                for (int i = inventory.Count - 1; i >= 0; i--)
                    if (inventory[i] != null) { replacement = inventory[i]; break; }
            selectedItem = replacement == null ? null : replacement.id;
            return replacement;
        }

        private void SellInventoryItem(string id)
        {
            if(!MerchantServiceActive){Feedback(false,"请在商人处出售装备。");return;}
            int row = bagItems.FindIndex(item => item.id == id);
            ItemData item = session.Progression.Profile.inventory.Find(entry=>entry!=null&&entry.id==id);
            if(item==null)return;
            if (IsEquipped(item)) return;
            int before = session.Progression.Profile.gold;
            bool sold = session.Progression.Sell(id);
            int gained = session.Progression.Profile.gold - before;
            Feedback(sold, "已出售 " + item.name + " · +" + gained + " 金币");
            if (!sold) return;
            bool replaceSelection = selectedItem == id;
            RebuildBagItems();
            if (replaceSelection)
                selectedItem = bagItems.Count == 0 ? null : bagItems[Mathf.Clamp(row,0,bagItems.Count-1)].id;
            ResolveSelectedItem();
            inventoryScroll.y = Mathf.Clamp(inventoryScroll.y, 0, Mathf.Max(0, bagItems.Count * 76 + 4 - 330));
        }

        private void StatLine(float x, float y, string label, string value, Color color)
        {
            Text(new Rect(x + 2, y, 151, 22), label, 13, muted);
            Text(new Rect(x + 157, y, 78, 27), value, 19, color, true, false, TextAnchor.UpperRight);
        }

        private void ReturnToInventory()
        {
            panel = Panel.Inventory;
            session.SetUIBlocking(true);
            RebuildBagItems();
            ResolveSelectedItem();
            inventoryScroll.y = Mathf.Clamp(inventoryScroll.y, 0, Mathf.Max(0, bagItems.Count * 76 + 4 - 330));
        }

        private void ItemStat(float x, float y, string name, int value, int previous, bool equipped)
        {
            Text(new Rect(x, y, 90, 24), name, 14, muted);
            Text(new Rect(x + 104, y, 177, 27), equipped ? value.ToString() : previous + " → " + value, 17, pale, true, false, TextAnchor.UpperRight);
            int diff = value - previous;
            string delta = equipped || diff == 0 ? "—" : (diff > 0 ? "+" : "") + diff;
            Text(new Rect(x + 296, y, 92, 27), delta, 18, diff >= 0 ? jade : new Color(1f, .49f, .42f), true, false, TextAnchor.UpperRight);
        }

        private bool IsEquipped(ItemData item)
        {
            GameProfile profile = session.Progression.Profile;
            return item != null && (profile.weaponId == item.id || profile.armorId == item.id || profile.relicId == item.id);
        }

        private bool IsEquipmentUpgrade(ItemData item)
        { return item!=null&&Attention.HigherScoreItems.Contains(item.id); }

        private string EquipmentUpgradeHint(ItemData item)
        {
            return (session.Progression.Equipped(item.slot) == null ? "此部位尚未穿戴装备。" : "继承部位强化后的综合评分更高（含机制估值）；请同时比较机制效果。") +
                (item.level > session.Progression.Profile.level ? "\n需要角色等级 " + item.level + "；目前等级不足。" : "");
        }

        private void DrawEquipmentUpgradeTag(Rect r)
        {
            Fill(r, new Color(.065f, .22f, .16f));
            Border(r, new Color(.3f, .85f, .54f, .45f));
            Text(r, "↑ 属性", 10, new Color(.57f, 1f, .67f), true, false, TextAnchor.MiddleCenter);
        }

        private static string ItemTitle(ItemData item) { return item.name + (item.upgradeLevel > 0 ? " +" + item.upgradeLevel : ""); }

        private static string Money(int amount)
        {
            if (amount >= 100000000) return (amount / 100000000f).ToString("0.#") + " 亿";
            if (amount >= 10000) return (amount / 10000f).ToString("0.#") + " 万";
            return amount.ToString();
        }

        private void DrawSkills()
        {
            if(DrawSkillSubsurface())return;
            if(skillSection==2){DrawClassSelectionTab();return;}
            if(skillSection==1){DrawSkillDevelopment();return;}
            if(MobileControls.Active){DrawMobileSkills();return;}
            GameProfile p = session.Progression.Profile;
            selectedSkill = Mathf.Clamp(selectedSkill, 0, GameBalance.SkillCount - 1);
            Rect w = Modal(1160, 660, GameBalance.ClassName(p.heroClass) + " · 技能", "");
            if (PopupCloseButton(new Rect(w.xMax - 69, w.y + 20, 44, 32))) ClosePanel();
            Text(new Rect(w.x + 763, w.y + 28, 296, 32), "精通点 " + p.skillPoints + "   /   角色 Lv." + p.level, 18, gold, true, false, TextAnchor.MiddleRight);
            DrawSkillTabs(new Rect(w.x+330,w.y+20,264,36));
            Rect branchHeading = new Rect(w.x + 24, w.y + 112, 267, 24);
            Text(branchHeading, "职业分支", 15, jade, true);
            if (branchHeading.Contains(Mouse)) tooltip = "达到对应等级自动习得与进阶。";
            Rect viewport = new Rect(w.x + 24, w.y + 147, 506, 468);
            Fill(viewport, new Color(.025f, .05f, .075f));
            Rect content = new Rect(0, 0, 490, 468);
            GUIStyle priorThumb = GUI.skin.verticalScrollbarThumb;
            GUI.skin.verticalScrollbarThumb = scrollThumb;
            skillScroll.y = Mathf.Clamp(skillScroll.y, 0, content.height - viewport.height);
            skillScroll = BeginTouchScroll("skills",viewport,skillScroll,content);
            for (int skill = 0; skill < GameBalance.SkillCount; skill++)
            {
                Rect node = SkillNodeRect(skill);
                int[] parents = GameBalance.SkillPrerequisites[skill];
                for (int i = 0; i < parents.Length; i++)
                {
                    Rect parent = SkillNodeRect(parents[i]);
                    Color connection = p.skillRanks[parents[i]] > 0 ? new Color(.25f, .61f, .53f) : new Color(.23f, .30f, .36f);
                    float bend = node.y - 13 - i * 5;
                    Fill(new Rect(parent.center.x - 1, parent.yMax, 2, bend - parent.yMax), connection);
                    Fill(new Rect(Mathf.Min(parent.center.x, node.center.x), bend, Mathf.Max(2, Mathf.Abs(parent.center.x - node.center.x)), 2), connection);
                    Fill(new Rect(node.center.x - 1, bend, 2, node.y - bend), connection);
                    Fill(new Rect(node.center.x - 3, node.y - 5, 6, 5), connection);
                }
            }
            for (int i = 0; i < GameBalance.SkillCount; i++)
            {
                int rank = p.skillRanks[i];
                int required = GameBalance.SkillRequiredLevels[i];
                bool passive = GameBalance.IsPassive(i);
                bool prerequisitesMet = session.Progression.PrerequisitesMet(i);
                bool canLearn = string.IsNullOrEmpty(session.Progression.SkillLockReason(i));
                Rect node = SkillNodeRect(i);
                Color accent = rank > 0 ? jade : canLearn ? gold : muted;
                Surface(node, selectedSkill == i ? new Color(.12f, .20f, .23f) : rank > 0 ? new Color(.06f, .145f, .15f) : card);
                SurfaceFrame(node, selectedSkill == i ? gold : new Color(accent.r, accent.g, accent.b, rank > 0 || canLearn ? .7f : .25f));
                DrawSkillIdentity(new Rect(node.x+5,node.y+7,24,24),p.heroClass,i,rank,rank>0||canLearn,24);
                Text(new Rect(node.x + 32, node.y + 7, 107, 24), GameBalance.SkillName(p.heroClass, i), 14, rank > 0 || canLearn ? pale : muted, true, false, TextAnchor.MiddleCenter);
                Text(new Rect(node.x + 5, node.y + 35, 134, 18), "Lv." + required + " · " + (rank>0?GameBalance.SkillRankName(rank):"未解锁"), 11, passive ? new Color(.82f, .74f, .98f) : muted, false, false, TextAnchor.MiddleCenter);
                string state = rank > 0 ? GameBalance.SkillRankName(rank) + (canLearn ? " · 可进阶" : " · 已学习") : canLearn ? "可学习" : p.level < required ? "等级未达" : "自动习得";
                if(node.Contains(Mouse))tooltip=state;
                Badge(node,Attention.LearnableSkills.Contains(i));
                if (GUI.Button(node, GUIContent.none, invisibleButton)) selectedSkill = i;
            }
            EndTouchScroll();
            GUI.skin.verticalScrollbarThumb = priorThumb;
            DrawSkillDetail(new Rect(w.x + 550, w.y + 112, 586, GameBalance.IsPassive(selectedSkill) ? 393 : 510), selectedSkill);
        }

        private static Rect SkillNodeRect(int skill)
        {
            return new Rect(10 + GameBalance.SkillTreeColumn(skill) * 160, 6 + GameBalance.SkillTreeRow(skill) * 66, 144, 54);
        }

        private void OpenBindings()
        {
            bindingReturnPanel = panel;
            bindingReturnPause = session.Paused;
            panel = Panel.Bindings;
            rebindingSlot = -1;
            session.SetUIBlocking(true);
            session.SetPaused(false);
        }

        private void HandleBindingInput()
        {
            if (panel != Panel.Bindings || rebindingSlot < 0 || Event.current.type != EventType.KeyDown) return;
            KeyCode key = Event.current.keyCode;
            bool modified = Event.current.control || Event.current.alt || Event.current.command;
            Event.current.Use();
            if (key == KeyCode.Escape) { rebindingSlot = -1; return; }
            if (modified || !GameBalance.IsBindableKey((int)key))
            {
                session.Notify("此键保留给移动或界面操作。请选择字母、数字或 F1～F12；ESC 取消。");
                return;
            }
            int slot = rebindingSlot;
            bool changed = session.Progression.SetHotbarKey(slot, (int)key);
            Feedback(changed, "技能槽 " + (slot + 1) + " 已绑定 " + GameBalance.KeyName((int)key) + "；十格配置已更新");
            if (changed) rebindingSlot = -1;
        }

        private void DrawBindings()
        {
            GameProfile p = session.Progression.Profile;
            Rect w = Modal(840, 500, "自定义快捷键", "固定 10 个位置 · 点击槽位，然后按下新的按键");
            if (PopupCloseButton(new Rect(w.xMax - 69, w.y + 20, 44, 32))) ClosePanel();
            Rect pageHeading = new Rect(w.x + 27, w.y + 114, 620, 25);
            Text(pageHeading, "十格技能栏", 15, jade, true);
            if (pageHeading.Contains(Mouse)) tooltip = "可绑定字母、数字与 F1～F12。已占用的按键会交换位置。\n移动及界面功能键保留。固定十格，可自由配置。";
            for (int i = 0; i < GameBalance.HotbarSize; i++)
            {
                Rect tile = new Rect(w.x + 24 + (i % 5) * 161, w.y + 157 + (i / 5) * 96, 148, 84);
                bool waiting = rebindingSlot == i;
                Fill(tile, waiting ? new Color(.2f, .18f, .12f) : card);
                Border(tile, waiting ? gold : jade * new Color(1, 1, 1, .3f));
                Text(new Rect(tile.x + 8, tile.y + 8, 132, 30), waiting ? "按键…" : GameBalance.KeyName(p.hotbarKeys[i]), waiting ? 21 : 25, waiting ? gold : pale, true, false, TextAnchor.MiddleCenter);
                DrawSlotIdentity(new Rect(tile.x + 17, tile.y + 46, 117, 26), p, i, pale);
                if (tile.Contains(Mouse)) tooltip = SlotSkillName(p, i) + " · " + GameBalance.KeyName(p.hotbarKeys[i]) + "\n点击后按新按键；Esc 取消。\n此设置用于十格快捷栏。";
                if (GUI.Button(tile, GUIContent.none, invisibleButton)) { rebindingSlot = i; GameAudio.Play(SoundCue.UI); }
            }
            if (rebindingSlot >= 0) Text(new Rect(w.x + 27, w.y + 360, 787, 27), "等待 " + GameBalance.KeyName(p.hotbarKeys[rebindingSlot]) + " 槽的新按键…  /  Esc 取消", 14, gold, true);
            if (DangerButton(new Rect(w.x + 24, w.y + 437, 350, 39), "恢复默认 12345 / ZXCVB", gold, rebindingSlot < 0))
            {
                Feedback(session.Progression.ResetHotbarKeys(), "已恢复默认技能按键");
            }
            if (NavigationButton(new Rect(w.x + 397, w.y + 437, 419, 39), bindingReturnPause ? "返回设置" : bindingReturnPanel == Panel.Controls ? "返回操作指南" : "返回技能研习", jade)) ClosePanel();
        }

        private int desktopPauseTab;
        private void DrawPause()
        {
            if (DrawSaveFlowConfirmation()) return;
            if (panel == Panel.SaveSelection) { DrawSaveSelection(); return; }
            if (MobileControls.Active) { DrawMobilePause(); return; }
            Rect w = Modal(780, 620, "设置", desktopPauseTab==0?"手动保存需确认覆盖":"");
            Rect close=new Rect(w.xMax-60,w.y+20,44,44);

            if(PopupCloseButton(close)){session.SetPaused(false);BlockUITransition();return;}
            string[] tabs = { "存档", "声音与画面", "键盘与操作" };
            int[] tabOrder={0,1,2};
            Fill(new Rect(w.x+24,w.y+110,152,470),new Color(.025f,.05f,.065f,.65f));
            for (int i = 0; i < tabs.Length; i++)
                if (PauseSidebarTab(new Rect(w.x+24,w.y+110+i*54,152,48), tabs[tabOrder[i]], desktopPauseTab == tabOrder[i],1) && desktopPauseTab != tabOrder[i])
                { desktopPauseTab = tabOrder[i]; }
            int exitCount=session.InDungeon?3:2;float exitButtonWidth=(536-12*(exitCount-1))/exitCount;
            if(session.InDungeon)DrawPauseDungeonExitButton(new Rect(w.x+212,w.yMax-70,exitButtonWidth,48));
            float exitStart=w.x+212+(session.InDungeon?exitButtonWidth+12:0);
            if(PrimaryButton(new Rect(exitStart,w.yMax-70,exitButtonWidth,48),session.InDungeon?"返回主菜单":"保存并返回主菜单",jade))RequestExit(true);
            if(PrimaryButton(new Rect(exitStart+12+exitButtonWidth,w.yMax-70,exitButtonWidth,48),"保存并退出",gold))RequestExit(false);
            if(desktopPauseTab==0)
            {
                if(PrimaryButton(new Rect(w.x+212,w.y+210,536,48),"保存",gold))RequestManualSave();
                if(NavigationButton(new Rect(w.x+212,w.y+272,536,48),"读取存档",jade))OpenSaveSelection();
                if(NavigationButton(new Rect(w.x+212,w.y+334,536,48),"存档位置 / 迁移",jade))
                {saveReturnPause=true;panel=Panel.SaveLocation;session.SetUIBlocking(true);session.SetPaused(false);}
            }
            else if (desktopPauseTab == 1)
            {
                if (ToggleButton(new Rect(w.x + 212, w.y + 210, 260, 42), GameAudio.Muted ? "声音：已静音" : "声音：已开启", !GameAudio.Muted))
                { GameAudio.Muted = !GameAudio.Muted; if (!GameAudio.Muted) GameAudio.Play(SoundCue.UI); }
                DrawAccessibilityStrip(new Rect(w.x + 212, w.y + 280, 536, 35));
            }
            else
            {
                if (NavigationButton(new Rect(w.x + 212, w.y + 210, 260, 42), "自定义快捷键", gold)) OpenBindings();
                if (NavigationButton(new Rect(w.x + 488, w.y + 210, 260, 42), "操作指南", jade)) OpenControls();
                if (NavigationButton(new Rect(w.x+212,w.y+272,536,44),"技能按键配置",jade))
                {session.SetPaused(false);skillSection=0;TogglePanel(Panel.Skills);BlockUITransition();}
            }
        }

        private void OpenControls()
        {
            controlsReturnPause = session.Paused;
            panel = Panel.Controls;
            session.SetUIBlocking(true);
            session.SetPaused(false);
        }

        private void DrawControls()
        {
            if(MobileControls.Active){DrawMobileGuide();return;}
            GameProfile p = session.Progression.Profile;
            Rect w = Modal(1060, 638, "操作指南", "键盘与鼠标 · 当前技能键帽会跟随你的自定义设置");
            if (PopupCloseButton(new Rect(w.xMax - 69, w.y + 20, 44, 32))) ClosePanel();
            Rect keyboard = new Rect(w.x + 24, w.y + 112, 650, 442);
            Fill(keyboard, card);
            Text(new Rect(keyboard.x + 18, keyboard.y + 12, 610, 25), "移动与战斗", 16, jade, true);
            DrawKeyCap(new Rect(keyboard.x + 81, keyboard.y + 47, 52, 49), "W", "前", jade);
            DrawKeyCap(new Rect(keyboard.x + 23, keyboard.y + 103, 52, 49), "A", "左", jade);
            DrawKeyCap(new Rect(keyboard.x + 81, keyboard.y + 103, 52, 49), "S", "后", jade);
            DrawKeyCap(new Rect(keyboard.x + 139, keyboard.y + 103, 52, 49), "D", "右", jade);
            DrawKeyCap(new Rect(keyboard.x + 249, keyboard.y + 47, 72, 49), "J", "普通攻击", gold);
            DrawKeyCap(new Rect(keyboard.x + 331, keyboard.y + 47, 72, 49), GameBalance.KeyName(p.hotbarKeys[System.Array.IndexOf(p.equippedSkills,GameBalance.HotbarPotion,p.hotbarPage*GameBalance.HotbarSize,GameBalance.HotbarSize)>=0?System.Array.IndexOf(p.equippedSkills,GameBalance.HotbarPotion,p.hotbarPage*GameBalance.HotbarSize,GameBalance.HotbarSize)%GameBalance.HotbarSize:9]), "生命药剂", gold);
            DrawKeyCap(new Rect(keyboard.x + 249, keyboard.y + 103, 82, 49), "SPACE", "跳跃", gold);
            DrawKeyCap(new Rect(keyboard.x + 341, keyboard.y + 103, 72, 49), "SHIFT", "闪现", gold);
            Rect mouse = new Rect(keyboard.x + 447, keyboard.y + 46, 175, 106);
            Fill(mouse, new Color(.035f, .065f, .1f));
            Border(mouse, new Color(.29f, .43f, .51f));
            Fill(new Rect(mouse.center.x, mouse.y, 1, 64), new Color(.29f, .43f, .51f));
            Text(new Rect(mouse.x + 4, mouse.y + 10, 79, 20), "左键", 16, pale, true, false, TextAnchor.MiddleCenter);
            Text(new Rect(mouse.x + 91, mouse.y + 10, 79, 20), "右键", 16, pale, true, false, TextAnchor.MiddleCenter);
            Text(new Rect(mouse.x + 2, mouse.y + 36, 83, 17), "攻击 / 确认", 10, muted, false, false, TextAnchor.MiddleCenter);
            Text(new Rect(mouse.x + 89, mouse.y + 36, 83, 17), "单击取消", 10, muted, false, false, TextAnchor.MiddleCenter);
            Text(new Rect(mouse.x + 7, mouse.y + 74, 161, 18), "滚轮 · 镜头缩放", 11, jade, false, false, TextAnchor.MiddleCenter);
            Text(new Rect(keyboard.x + 23, keyboard.y + 167, 598, 42), "移动方向跟随镜头；左键 / J 普攻，按技能键或点击快捷栏施放。\n地面技能先选点再左键确认；蓄力技能完成后生效。", 12, pale, false, true);
            Rule(keyboard.x + 20, keyboard.y + 222, 609, jade);
            Text(new Rect(keyboard.x + 22, keyboard.y + 236, 438, 22), "十格技能快捷键", 16, jade, true);
            for (int i = 0; i < GameBalance.HotbarSize; i++)
            {
                Rect keycap = new Rect(keyboard.x + 23 + (i % 5) * 123, keyboard.y + 273 + (i / 5) * 59, 112, 50);
                Fill(keycap, new Color(.08f, .13f, .18f));
                Border(keycap, new Color(jade.r, jade.g, jade.b, .4f));
                Text(new Rect(keycap.x + 7, keycap.y + 3, 98, 20), GameBalance.KeyName(p.hotbarKeys[i]), 16, pale, true);
                DrawSlotIdentity(new Rect(keycap.x + 7, keycap.y + 24, 98, 23), p, i, muted);
            }
            Text(new Rect(keyboard.x + 23, keyboard.y + 397, 604, 36), "快捷栏：点击使用，拖动换位；拖到栏外取消。右键打开配置。\n背包内可将生命药剂放入快捷栏。", 12, muted, false, true);
            float right = w.x + 698;
            Text(new Rect(right, w.y + 114, 332, 23), "界面与冒险", 16, jade, true);
            string[] keys = { "I", "K", "G", "H", "T", "Esc" };
            string[] actions = { "行囊、装备与补给", "技能、学习与配置", "靠近营地人物后对话", "远离敌人后返回营地", "进入传送门 / 房间北门", "取消选点或蓄力 / 返回" };
            for (int i = 0; i < keys.Length; i++)
            {
                float rowY = w.y + 153 + i * 34;
                Rect key = new Rect(right, rowY, 84, 30);
                Fill(key, card);
                Border(key, new Color(.25f, .38f, .46f));
                Text(key, keys[i], 13, pale, true, false, TextAnchor.MiddleCenter);
                Text(new Rect(right + 99, rowY + 4, 234, 25), actions[i], 13, muted);
            }
            Text(new Rect(right, w.y + 472, 330, 71), "按住右键：左右环绕，上下调整俯仰。\n右键单击：取消选点或蓄力；滚轮缩放。\n空格跳跃，无冷却；移动时跳跃可跨窄河，Shift 闪现。\n背包与技能界面会暂停战斗。", 12, muted, false, true);
            if (NavigationButton(new Rect(w.x + 24, w.y + 575, 650, 39), "自定义技能按键", gold)) OpenBindings();
            if (controlsReturnPause && NavigationButton(new Rect(right, w.y + 575, 338, 39), "返回设置", jade)) ClosePanel();
        }

        private void DrawKeyCap(Rect r, string key, string action, Color accent)
        {
            Fill(new Rect(r.x + 2, r.y + 3, r.width, r.height), new Color(.015f, .028f, .045f));
            Fill(r, new Color(.08f, .13f, .18f));
            Border(r, new Color(accent.r, accent.g, accent.b, .4f));
            Text(new Rect(r.x + 4, r.y + 5, r.width - 8, 23), key, 18, pale, true, false, TextAnchor.MiddleCenter);
            Text(new Rect(r.x + 4, r.y + 31, r.width - 8, 14), action, 10, muted, false, false, TextAnchor.MiddleCenter);
        }

        private void DrawSaveLocation()
        {
            if(MobileControls.Active){DrawMobileSaveLocation();return;}
            Rect w = Modal(800, 500, "存档位置与迁移", "备份与迁移角色进度");
            if (PopupCloseButton(new Rect(w.xMax - 69, w.y + 20, 44, 32))) ClosePanel();
            string path = session.Progression.SaveDirectory;
            Fill(new Rect(w.x + 24, w.y + 113, 752, 79), card);
            Text(new Rect(w.x + 40, w.y + 123, 720, 17), "当前存档文件夹", 11, jade, true);
            Text(new Rect(w.x + 40, w.y + 147, 720, 37), "角色进度保存在本机，可打开文件夹进行备份。", 13, pale, false, true);
            if (NavigationButton(new Rect(w.x + 24, w.y + 207, 367, 40), "打开存档文件夹", jade))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(path);
                    string absolute = System.IO.Path.GetFullPath(path).TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
                    Application.OpenURL(new System.Uri(absolute).AbsoluteUri);
                }
                catch (System.Exception exception) { session.Notify("无法打开存档目录：" + exception.Message); }
            }
            if (Button(new Rect(w.x + 409, w.y + 207, 367, 40), "复制目录路径", gold))
            {
                GUIUtility.systemCopyBuffer = path;
                session.Notify("已复制存档目录路径");
            }
            Text(new Rect(w.x + 28, w.y + 307, 744, 96), "迁移前，请退出两台设备上的游戏，并备份整个存档文件夹。\n\n将完整文件夹复制到新设备的存档位置，不要遗漏其中的文件。重新启动游戏后，选择角色继续冒险。", 14, pale, false, true);
            Text(new Rect(w.x + 28, w.y + 415, 744, 21), "建议迁移前保留一份备份；新电脑的存档目录也可从此页面打开。", 12, muted);
            if (NavigationButton(new Rect(w.x + 24, w.y + 451, 752, 31), "返回设置", jade)) ClosePanel();
        }

        private void DrawDeath()
        {
            if(DrawStructuredRunRecap(true)){ClosePanel();session.Respawn();}
        }

        private void DrawNotification()
        {
            if(panel==Panel.Chapter||panel==Panel.HubDialogue||session.ChapterFinished)return;
            if (string.IsNullOrEmpty(session.Notification)) return;
            if(MobileControls.Active && (MobilePanelOwnsNotification || session.Paused || panel==Panel.Controls || panel==Panel.SaveSelection || panel==Panel.TravelMap || panel==Panel.Summary || session.IsDead || session.ModeFinished)) return;
            if(MobileControls.Active)
            {
                if(panel!=Panel.None||session.InputBlocked)return;
                DrawMobileBattleNotice();return;
            }
            bool overlay = panel != Panel.None || session.Paused || session.IsDead;
            Rect r = new Rect((width - 550) * .5f, overlay ? height - 49 : 26, 550, 40);
            Box(r, gold);
            Text(new Rect(r.x + 14, r.y + 3, r.width - 28, 34), PlatformText(session.Notification), 14, pale, true, true, TextAnchor.MiddleCenter);
        }

        private void DrawTooltip()
        {
            if (string.IsNullOrEmpty(tooltip)) return;
            if (tooltipAnchorText == tooltip && !string.IsNullOrEmpty(tooltipKey))
            {
                Rect tip = new Rect(Mathf.Clamp(tooltipAnchor.center.x-94,12,width-200),
                    tooltipAnchor.yMax+54<height?tooltipAnchor.yMax+10:tooltipAnchor.y-54,188,44);
                Box(tip,jade);
                Text(new Rect(tip.x+12,tip.y+8,116,28),tooltip,14,pale,true,false,TextAnchor.MiddleLeft);
                Rect keycap=new Rect(tip.xMax-52,tip.y+10,40,24);
                Fill(keycap,card);Border(keycap,muted);
                Text(keycap,tooltipKey,12,jade,true,false,TextAnchor.MiddleCenter);
                return;
            }
            float boxHeight = Style(13, false, true).CalcHeight(new GUIContent(tooltip), 288) + 22;
            Vector2 mouse = Mouse;
            Rect r = new Rect(Mathf.Clamp(mouse.x - 154, 12, width - 324), Mathf.Clamp(mouse.y - boxHeight - 14, 12, height - boxHeight - 12), 312, boxHeight);
            if(tooltipAnchorText==tooltip)
            {
                float gap=10;
                r.x=Mathf.Clamp(tooltipAnchor.center.x-r.width*.5f,12,width-r.width-12);
                if(tooltipAnchor.yMax+gap+r.height<=height-12)r.y=tooltipAnchor.yMax+gap;
                else if(tooltipAnchor.y-gap-r.height>=12)r.y=tooltipAnchor.y-gap-r.height;
                else
                {
                    r.x=tooltipAnchor.x-r.width-gap>=12?tooltipAnchor.x-r.width-gap:tooltipAnchor.xMax+gap;
                    r.y=Mathf.Clamp(tooltipAnchor.center.y-r.height*.5f,12,height-r.height-12);
                }
            }
            Box(r, jade);
            Text(new Rect(r.x + 12, r.y + 10, 288, boxHeight - 20), tooltip, 13, pale, false, true);
        }

        private void TogglePanel(Panel value)
        {
            if(!CanSwitchFunction)return;
            bool same=panel==value&&!MerchantServiceActive&&!SmithServiceActive&&!session.Paused;
            PrepareFunctionSwitch();panel=same?Panel.None:value;
            if(panel==Panel.Skills){skillSection=0;ResetMobileSkillNavigation();}
            session.SetUIBlocking(panel!=Panel.None);BlockUITransition();
        }

        private void ClosePanel()
        {
            if(smithFashionQuote!=null){ReleaseFashionSmithPreview();return;}
            if(CloseTopPopup())return;
            if(SmithServiceActive&&smithPreviewMechanic!=EquipmentMechanic.None){smithPreviewMechanic=EquipmentMechanic.None;return;}
            if(MerchantServiceActive||SmithServiceActive){inventoryHubNpc=HubNpcKind.None;merchantExchangeOpen=false;panel=Panel.None;session.SetUIBlocking(false);BlockUITransition();return;}
            if(CloseChapterSelection())return;
            if(CloseRouteSkill())return;
            if(CloseMobileInventoryDetail())return;
            if(CloseMobileSkillDetail())return;
            if(CloseProgressionGoalSurface())return;
            if(CloseClassSwitchSurface())return;
            if(CloseBuildPlanSurface())return;
            if(CloseTravelMap())return;
            if(CancelSaveDeletion())return;
            if(CancelActiveSaveFlow())return;
            if (panel == Panel.SaveSelection)
            {
                bool returnPaused = saveSelectionFromPause && session.HasStarted;
                panel = Panel.None; saveSelectionFromPause = false;
                session.SetUIBlocking(false);
                if (session.HasStarted) session.SetPaused(returnPaused);
                BlockUITransition();
                return;
            }
            rebindingSlot = -1;
            if (panel == Panel.Chests)
            {
                if (chestDetails) { chestDetails=false;return; }
                DismissChestPanel();return;
            }
            if (panel == Panel.Fashion)
            {
                panel = Panel.Inventory;
                return;
            }
            if (panel == Panel.PotionAssignment)
            {
                ReturnToInventory();
                return;
            }
            if (panel == Panel.Bindings)
            {
                panel = bindingReturnPanel;
                session.SetUIBlocking(panel != Panel.None);
                session.SetPaused(bindingReturnPause);
                bindingReturnPause = false;
                return;
            }
            if (panel == Panel.SaveLocation)
            {
                panel = Panel.None;
                session.SetUIBlocking(false);
                session.SetPaused(saveReturnPause);
                saveReturnPause = false;
                return;
            }
            if (panel == Panel.Controls)
            {
                panel = Panel.None;
                session.SetUIBlocking(false);
                session.SetPaused(controlsReturnPause);
                controlsReturnPause = false;
                return;
            }
            panel = Panel.None;
            session.SetUIBlocking(false);
        }

        private void Feedback(bool success, string message)
        {
            if(success&&(message=="装备已穿戴"||message=="外观已穿戴"||message=="时装已穿戴"||message=="已穿戴"))GameAudio.Play(SoundCue.Loot);
            string issue = session.Progression.LastError;
            session.Notify(success ? message + (string.IsNullOrEmpty(issue) ? "" : " · " + issue) : (string.IsNullOrEmpty(issue) ? "当前无法执行此操作" : issue));
        }
    }
}
