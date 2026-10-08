using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    /// <summary>Resolution-independent runtime interface; no scene or package dependencies.</summary>
    public sealed partial class GameUI : MonoBehaviour
    {
        private enum Panel { None, Inventory, Skills, Bindings, SaveLocation, Controls, SaveSelection, PotionAssignment, Fashion, Chests, Camp, Summary, TravelMap, Notice, Chapter, HubDialogue }
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
        private readonly Color ink = new Color(.035f, .065f, .10f, .97f);
        private readonly Color card = new Color(.06f, .105f, .15f, .96f);
        private readonly Color jade = new Color(.32f, .91f, .77f);
        private readonly Color gold = new Color(1f, .76f, .37f);
        private readonly Color muted = new Color(.76f, .82f, .89f);
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
            scale = HudLogicalScale.For(safe.width, safe.height);
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
                    hotbarBounds.y + (mobile ? 27 : 17) + (1 - slot / 5) * (mobile ? 65 : 62), mobile ? 64 : 48, mobile ? 61 : 46);
        }

        private bool PauseUtilityVisible {get{return panel==Panel.Controls&&controlsReturnPause||panel==Panel.SaveLocation&&saveReturnPause||panel==Panel.Bindings&&(bindingReturnPause||bindingReturnPanel==Panel.Controls&&controlsReturnPause)||panel==Panel.TravelMap&&travelReturnPause;}}

        private void Update()
        {
            RefreshLayout();
            ReconcileMobileScroll();
            ReconcileCollectionPreview();
            if (session == null || session.BackgroundPaused) return;
            ReconcileHubNpcConversation();
            if(session.PracticeActive){if(Input.GetKeyDown(KeyCode.Escape))session.EndPractice("主动离开 · 记录提前结束");return;}
            ReconcileBuildPlanSurface();ReconcileClassSwitchSurface();
            ReconcileProgressionGoalSurface();
            bool gameplayBackAllowed=GameplayBackAllowed;
            if(Input.GetKeyDown(KeyCode.Escape))backConsumedFrame=Time.frameCount;
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
            if ((session.Progression.Profile.pendingFashionChest || session.Progression.Profile.pendingChestReveal) && panel == Panel.None && !session.Paused)
            {
                ResetChestReveal();
                panel = Panel.Chests;
                session.SetUIBlocking(true);
            }
            if ((session.DungeonSelectionOpen || session.RunChoices.AwaitingChoice) && !session.Paused && !PauseUtilityVisible)
            {if(Input.GetKeyDown(KeyCode.Escape)){if(session.DungeonSelectionOpen)session.CancelDungeonSelection();else session.SetPaused(true);}return;}
            if (rebindingSlot >= 0) return;
            if(!MobileControls.Active&&Input.GetKeyDown(KeyCode.M)&&session.HasStarted&&!session.IsDead){if(panel==Panel.TravelMap)CloseTravelMap();else if(panel==Panel.None)OpenTravelMap();return;}
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
                if (panel == Panel.Chests && (!chestDetails || session.Paused)) { session.SetPaused(!session.Paused); BlockUITransition(); }
                else if (panel != Panel.None) ClosePanel();
                else session.SetPaused(!session.Paused);
            }
            if (session.Paused || session.DungeonSelectionOpen || session.RunChoices.AwaitingChoice) return;
            if(panel==Panel.HubDialogue){HandleHubNpcShortcut(Input.GetKeyDown(KeyCode.G));return;}
            if (panel == Panel.Controls || panel == Panel.SaveLocation || panel == Panel.Bindings || panel == Panel.PotionAssignment || panel == Panel.Chests || panel == Panel.Camp || panel == Panel.TravelMap || panel == Panel.Notice || panel == Panel.Chapter) return;
            if (Input.GetKeyDown(KeyCode.I)) TogglePanel(Panel.Inventory);
            if (Input.GetKeyDown(KeyCode.K)) TogglePanel(Panel.Skills);
            HandleHubNpcShortcut(Input.GetKeyDown(KeyCode.G));
        }

        private void OnDestroy()
        {
            if(session!=null&&session.Progression!=null)session.Progression.Changed-=InvalidateAttention;
            if(attentionDot!=null)Destroy(attentionDot);
            ClearRewardMoment();
            ReleaseChestTextures();
            ReleaseCollectionPreview();
            if(terrainMap!=null)Destroy(terrainMap);
            if (thumbTexture != null) Destroy(thumbTexture);
            if (trackTexture != null) Destroy(trackTexture);
            for (int i = 0; i < crestTextures.Length; i++) if (crestTextures[i] != null) Destroy(crestTextures[i]);
            UIIconAtlas.Clear();
            GameFont.Release(ref font);
        }

        private void OnGUI()
        {
            if (session == null || session.Progression == null) return;
            RefreshLayout();
            if (font == null) font = GameFont.Shared;
            if (invisibleButton == null) BuildStyles();
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            Color oldContentColor = GUI.contentColor;
            bool oldEnabled = GUI.enabled;
            GUI.matrix = Matrix4x4.TRS(guiOffset, Quaternion.identity, new Vector3(scale, scale, 1));
            GUI.color = Color.white;
            GUI.contentColor = Color.white;
            GUI.enabled = !session.BackgroundPaused && !LifecycleTouchBlocked && !UITransitionBlocked;
            blockedRects.Clear();
            tooltip = null;
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
                PrepareRewardMoment();
                bool priorEnabled = GUI.enabled;
                GUI.enabled = priorEnabled && panel == Panel.None && !session.InputBlocked;
                DrawHUD();
                GUI.enabled = priorEnabled;
                if (panel == Panel.None && !session.Paused && !session.IsDead) DrawTargetingHint();
                if (session.Paused) DrawPause();
                else if(PauseUtilityVisible)
                {if(panel==Panel.Controls)DrawControls();else if(panel==Panel.Bindings)DrawBindings();else if(panel==Panel.SaveLocation)DrawSaveLocation();else DrawTravelMap();}
                else if (session.IsDead) {if(session.ChapterFinished)DrawChapterResult();else DrawDeath();}
                else if (session.DungeonSelectionOpen) DrawDungeonSelection();
                else if (session.RoomBranchChoiceOpen) DrawRoomBranchChoice();
                else if (session.RunChoices.AwaitingChoice) DrawBlessingChoice();
                else if(session.ChapterFinished)DrawChapterResult();
                else if(session.ModeFinished){if(DrawStructuredRunRecap(false))session.ReturnToCamp();}
                else if (DrawPresetSaleConfirmation()) {}
                else if (panel == Panel.Inventory) DrawInventory();
                else if (panel == Panel.Skills) DrawSkills();
                else if (panel == Panel.Bindings) DrawBindings();
                else if (panel == Panel.SaveLocation) DrawSaveLocation();
                else if (panel == Panel.Controls) DrawControls();
                else if (panel == Panel.PotionAssignment) DrawPotionAssignment();
                else if (panel == Panel.Fashion) DrawFashion();
                else if (panel == Panel.Chests) DrawChests();
                else if (panel == Panel.Camp) DrawCampWorkshop();
                else if (panel == Panel.Summary) DrawRunSummary();
                else if (panel == Panel.TravelMap) DrawTravelMap();
                else if (panel == Panel.Notice) DrawMobileNotice();
                else if (panel == Panel.Chapter) DrawChapterSelection();
                else if (panel == Panel.HubDialogue) DrawHubNpcDialogue();
                DrawNotification();
                DrawRewardMoment();
            }
            if (hotbarDragging && hotbarPointerSkill != -1)
            {
                tooltip = null;
                DrawIcon(new Rect(Mouse.x + 11, Mouse.y + 11, 36, 36), HotbarIcon(session.Progression.Profile, hotbarPointerSkill), Color.white);
            }
            DrawTooltip();
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

        private GUIStyle Style(int size, bool bold = false, bool wrap = false, TextAnchor align = TextAnchor.UpperLeft)
        {
            if(!MobileControls.Active)size=Mathf.Max(8,Mathf.RoundToInt(size*EffectPreferences.CombatTextScale/1.25f));
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
            if(!wrap&&!MobileControls.Active)
            {
                int fitted=Mathf.Min(style.fontSize,Mathf.FloorToInt(rect.height/1.15f));
                if(fitted<style.fontSize&&fitted>=8)
                {
                    int raw=Mathf.RoundToInt(fitted*1.25f/EffectPreferences.CombatTextScale);
                    style=Style(raw,bold,false,align);
                }
            }
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
            if (shadow) Fill(new Rect(rect.x + 5, rect.y + 6, rect.width, rect.height), new Color(0, 0, 0, .20f));
            Fill(rect, ink);
            Border(rect, new Color(accent.r, accent.g, accent.b, .28f));
        }

        private bool Button(Rect rect, string caption, Color accent, bool enabled = true, string hint = null, bool primary = false)
        {
            return DrawButton(rect, caption, primary ? ButtonRole.Primary : ButtonRole.Action, enabled, hint);
        }

        private void Rule(float x, float y, float length, Color color)
        {
            Fill(new Rect(x, y, length, 1), new Color(color.r, color.g, color.b, .25f));
        }

        private void DrawTitle()
        {
            if(MobileControls.Active){DrawMobileTitle();return;}
            if (saveSlotsDirty) RefreshSaveSlots();
            // The title is a static, opaque composition; world geometry cannot leak through it.
            Fill(new Rect(0, 0, width, height), new Color(.018f, .029f, .048f, 1f));
            float x = (width - 1040) * .5f;
            float y = (height - 438) * .5f;
            Fill(new Rect(x + 7, y + 9, 1040, 438), new Color(.006f, .012f, .021f, 1f));
            Fill(new Rect(x, y, 1040, 438), new Color(.048f, .074f, .112f, 1f));
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
            bool canContinue = saveSlots.Count > 0;
            if (NavigationButton(new Rect(x + 314, y + 359, 186, 50), "继续冒险", jade, canContinue, canContinue ? "选择要继续的角色存档。" : "创建角色后可从存档选择页继续冒险。")) OpenSaveSelection();
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
                saveSelectionError = string.IsNullOrEmpty(session.SaveLoadError) ? "无法读取该存档，请刷新列表后重试。" : session.SaveLoadError;
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
            Fill(new Rect(0, 0, width, height), new Color(.018f, .029f, .048f, 1f));
            Rect w = Modal(900, 570, "选择存档", "");
            Text(new Rect(w.x + 620, w.y + 29, 186, 23), saveSlots.Count + " 份存档", 13, muted, false, false, TextAnchor.MiddleRight);
            if (NavigationButton(new Rect(w.xMax - 69, w.y + 20, 44, 32), "×", jade)) ClosePanel();
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
                Text(new Rect(row.x + 81, row.y + 43, 307, 20), (slot.CanLoad ? "编号 " : "损坏 · 编号 ") + (slot.Id=="legacy"?"旧存档":slot.Id.Substring(0,8)), 13, accent);
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
            if (NavigationButton(new Rect(w.x + 24, w.y + 506, 188, 40), "返回", jade)) ClosePanel();
            if (Button(new Rect(w.x + 232, w.y + 506, 118, 40), "刷新", muted)) { RefreshSaveSlots(); saveSelectionError = null; }
            DrawDeleteSaveButton(new Rect(w.x + 372, w.y + 506, 246, 40));
            if (Button(new Rect(w.x + 640, w.y + 506, 236, 40), selectedSlot != null && selectedSlot.RecoveredFromBackup ? "从备份读取" : "读取存档", gold, selectedSlot != null, null, true)) ContinueSelectedSave();
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

        private void DrawHUD()
        {
            if(MobileControls.Active){DrawMobileHUD();return;}
            GameProfile p = session.Progression.Profile;
            Color accent = GameBalance.ClassColor(p.heroClass);
            Rect playerRect = new Rect(16, 16, 240, 88);
            blockedRects.Add(playerRect);
            Box(playerRect, accent);
            Fill(new Rect(16, 16, 2, 88), accent);
            Text(new Rect(28, 24, 139, 22), GameBalance.ClassName(p.heroClass) + " · Lv." + p.level, 16, pale, true);
            Text(new Rect(169, 26, 74, 20), Money(p.gold) + " 金", 12, gold, true, false, TextAnchor.UpperRight);
            float hp = session.Player == null ? 0 : session.Player.Health;
            float maxHp = session.Player == null ? 1 : session.Player.MaxHealth;
            Bar(new Rect(28, 54, 216, 10), hp / Mathf.Max(1, maxHp), new Color(.26f, .77f, .61f));
            float energy = session.Player == null ? 0 : session.Player.Energy;
            float maxEnergy = session.Player == null ? 100 : session.Player.MaxEnergy;
            Bar(new Rect(28, 71, 216, 7), energy / Mathf.Max(1, maxEnergy), new Color(.28f, .57f, .91f));
            bool maxLevel = p.level >= ProgressionService.MaximumLevel;
            Bar(new Rect(28, 88, 216, 3), maxLevel ? 1 : p.xp / (float)GameBalance.XpToNext(p.level), gold);
            if (playerRect.Contains(Mouse) && GUI.enabled)
                tooltip = "生命 " + Mathf.CeilToInt(hp) + " / " + Mathf.CeilToInt(maxHp) + "\n" + GameBalance.EnergyName(p.heroClass) + " " + Mathf.FloorToInt(energy) + " / " + Mathf.RoundToInt(maxEnergy) + "\n" + (maxLevel ? "已达最高等级" : "经验 " + p.xp + " / " + GameBalance.XpToNext(p.level)) + "\n金币 " + p.gold + " · 生命药剂 " + p.potions;
            string objectiveText = session.SpecialAdventure?session.ModeName:session.InDungeon
                ? session.DungeonCleared ? "沉星遗迹已通关" : "击败本轮敌人"
                : p.level < 2 ? "击败原野怪物，升至 2 级"
                : p.skillRanks[0] == 0 ? "学习首个职业技能"
                : "前往北方的沉星遗迹";
            string objectiveProgress = session.SpecialAdventure?session.ModeObjectiveStatus:session.InDungeon
                ? session.DungeonCleared ? "返回营地整备 · T" : "第 " + session.DungeonWave + " / " + session.TotalWaves + " 波 · 剩余 " + session.Enemies.Count + " 个敌人"
                : p.level < 2 ? "经验 " + p.xp + " / " + GameBalance.XpToNext(p.level)
                : p.skillRanks[0] == 0 ? "可用技能点 " + p.skillPoints + " · K"
                : "收集装备，进入传送门 · T";
            if(session.ChapterActive){objectiveText=ChapterDefinition.Get(session.ActiveChapterNode).Name;objectiveProgress=session.ChapterObjectiveStatus;}
            string growthTitle,growthDetail;if(!session.ChapterActive&&TryGrowthHudHint(out growthTitle,out growthDetail)){objectiveText=growthTitle;objectiveProgress=growthDetail;}
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
            Rect objective=new Rect(16,114,282,measured.Height);blockedRects.Add(objective);Box(objective,jade,false);
            Fill(new Rect(objective.x,objective.y,3,objective.height),jade);
            Text(new Rect(objective.x+13,objective.y+measured.HeadingY,255,17),"当前目标",11,jade,true);
            Text(new Rect(objective.x+13,objective.y+measured.BodyY,255,bodyHeight),objectiveText,15,pale,true,true);
            if(showSeals)DrawChapterSeals(new Rect(objective.x+13,objective.y+measured.ProgressY,255,44),1,chapterFirstSeal,session.ChapterSealView(1));
            else if(showRoomSeals)DrawRoomSeals(new Rect(objective.x+13,objective.y+measured.ProgressY,255,66),1,false,roomFirstSeal,session.RoomSealView(1),session.RoomObjectiveView);
            else Text(new Rect(objective.x+13,objective.y+measured.ProgressY,255,progressHeight),progressText,12,muted,false,true);
            if(showCharge)Text(new Rect(objective.x+13,objective.y+measured.ChargeY,255,chargeHeight),chargeText,17,gold,true,true);
            if (objective.Contains(Mouse) && GUI.enabled)
                tooltip = PlatformText(session.Objective + (session.InDungeon ? "\n通关后按 T 返回营地。远离敌人后可按 H 提前撤离。" : "\n靠近紫色传送门按 T 进入副本。远离敌人后可按 H 回营。"));
            DrawMinimap();
            DrawHotbar();
            DrawCompanionCommands();
            Text(new Rect(hotbarBounds.x-170,hotbarBounds.y-22,622,18),CurrentCombatOpportunity(),12,gold,true,false,TextAnchor.MiddleCenter);
            Text(new Rect(hotbarBounds.x-170,hotbarBounds.y-43,622,18),CurrentCombatResult(),11,pale,true,false,TextAnchor.MiddleCenter);
            DrawChargeProgress();
            DrawDungeonStatus();
            DrawEdgeActions();
            DrawExpeditionHUD();
            DrawGrowthHudCard();
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

        private void DrawMinimap()
        {
            float x = width - 166;
            Rect map = new Rect(x, 16, 150, 154);
            blockedRects.Add(map);
            Box(map, jade);
            Text(new Rect(x + 8, 23, 134, 19), session.ZoneName, 11, pale, true, false, TextAnchor.MiddleCenter);
            Rect field = new Rect(x + 11, 49, 128, 109);
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
            if (map.Contains(Mouse) && GUI.enabled)
                tooltip = session.ZoneName + "\n点击打开城镇旅行地图\n青色：你 · 青色菱形：传送门 · 金色：营地 / 精英\n绿色：中立普通怪 · 橙色：反击中 · 红色：首领";
            if (GUI.Button(map, GUIContent.none, invisibleButton)) OpenTravelMap();
            if (session.InDungeon)
                Text(new Rect(x, 178, 150, 18), "波次 " + Mathf.Min(session.DungeonWave, session.TotalWaves) + " / " + session.TotalWaves, 11, gold, false, false, TextAnchor.MiddleRight);
        }

        private void MapDot(Rect map, Vector3 position, Color color, float size)
        {
            float radius = Mathf.Max(1f, session.ArenaRadius);
            float x = map.x + map.width * Mathf.InverseLerp(-radius, radius, position.x);
            float y = map.yMax - map.height * Mathf.InverseLerp(-radius, radius, position.z);
            Fill(new Rect(x - size * .5f - 1, y - size * .5f - 1, size + 2, size + 2), ink);
            Fill(new Rect(x - size * .5f, y - size * .5f, size, size), color);
        }

        private void DrawDungeonStatus()
        {
            if (!session.InDungeon || session.ChapterFinished) return;
            if (session.DungeonCleared)
            {
                if (panel == Panel.Chests) return;
                float victoryWidth=Mathf.Min(494,width-32);
                Rect victory = new Rect((width-victoryWidth)*.5f,87,victoryWidth,159);
                blockedRects.Add(victory);
                Box(victory, gold);
                Fill(new Rect(victory.x, victory.y, victory.width, 3), gold);
                Text(new Rect(victory.x + 18, victory.y + 14, victory.width-36, 37), "遗迹肃清", 29, gold, true, false, TextAnchor.MiddleCenter);
                Text(new Rect(victory.x + 18, victory.y + 63, victory.width-36, 22), session.DungeonRewardPending?"奖励待保存 · 可重试结算或打开存档菜单":"三波挑战完成 · 战利品已解锁", 13, pale, false, false, TextAnchor.MiddleCenter);
                if (PrimaryButton(new Rect(victory.center.x+12,victory.y+105,(victory.width-76)*.5f,36), session.DungeonRewardPending?"保存奖励并回营":MobileControls.Active ? "返回营地整备" : "返回营地整备  /  T", gold, true, session.LastRunSummary, true)) session.ReturnToCamp();
                if(NavigationButton(new Rect(victory.x+26,victory.y+105,(victory.width-76)*.5f,36), "战斗复盘", jade)) { panel=Panel.Summary;session.SetUIBlocking(true); }
                return;
            }
            EnemyController boss = null;
            for (int i = 0; i < session.Enemies.Count; i++)
            {
                EnemyController enemy = session.Enemies[i];
                if (enemy != null && enemy.IsBoss && !enemy.IsDead) { boss = enemy; break; }
            }
            if (boss == null) return;
            Rect bossBar = new Rect((width - 462) * .5f, 86, 462, 67);
            blockedRects.Add(bossBar);
            Box(bossBar, new Color(1f, .4f, .32f));
            Text(new Rect(bossBar.x + 14, bossBar.y + 9, 432, 25), boss.DisplayName + "  /  遗迹首领", 16, gold, true, false, TextAnchor.MiddleCenter);
            Bar(new Rect(bossBar.x + 17, bossBar.y + 44, 428, 8), boss.Health / Mathf.Max(1, boss.MaxHealth), new Color(.89f, .33f, .28f));
        }

        private void DrawHotbar()
        {
            GameProfile p = session.Progression.Profile;
            bool mobile = MobileControls.Active;
            Rect bar = hotbarBounds;
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
                    DrawIcon(identity,potion?HotbarIcon(p,skill):UIIconAtlas.Skill(p.heroClass,skill,48),
                        !ready?new Color(.5f,.55f,.6f,.85f):Color.white);
                }
                else Text(new Rect(slot.x, slot.y + 9, slot.width, 32), "+", 20, new Color(.34f, .44f, .53f), false, false, TextAnchor.MiddleCenter);
                if (cooldown > .01f)
                {
                    float cover = slot.height * Mathf.Clamp01(cooldown / GameBalance.EffectiveCooldown(p.heroClass, skill, rank));
                    Fill(new Rect(slot.x + 1, slot.yMax - cover, slot.width - 2, cover), new Color(0, .025f, .04f, .76f));
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
                    float countWidth = Mathf.Max(17, count.Length * 8 + 4);
                    Fill(new Rect(slot.xMax - countWidth - 2, slot.yMax - 17, countWidth, 15), new Color(.015f, .025f, .04f, .94f));
                    Text(new Rect(slot.xMax - countWidth - 3, slot.yMax - 18, countWidth, 17), count, 11, locked ? muted : pale, true, false, TextAnchor.MiddleRight);
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
                    tooltip = empty ? "未配置" : potion ? PotionTooltip(p) : SkillTooltip(p, skill, rank);
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
            Color previous = GUI.color;
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
            Badge(r,icon=="inventory"?NewEquipmentAttention||Attention.LootPending:icon=="skills"?Attention.Skills:icon=="camp"?Attention.Rewards:false);
            if (!MobileControls.Active) Text(new Rect(r.x + 3, r.y + 1, r.width - 6, 12), key, 8, pale, true);
            if (!string.IsNullOrEmpty(badge))
            {
                Rect label = new Rect(r.xMax - 21, r.yMax - 15, 20, 14);
                Fill(label, new Color(.06f, .08f, .10f));
                Text(label, badge, 9, gold, true, false, TextAnchor.MiddleCenter);
            }
            if (hover) tooltip = PlatformText(hint);
            bool clicked = GUI.Button(r, GUIContent.none, invisibleButton);
            if (clicked) GameAudio.Play(SoundCue.UI);
            return clicked;
        }

        private void DrawEdgeActions()
        {
            GameProfile p = session.Progression.Profile;
            float x = width - (MobileControls.Active ? 284 : 238);
            float y = height - 54;
            DrawHubActions(x, y - 50);
            Rect catalog=new Rect(x-46,y,38,38);
            if(IconButton(new Rect(width-216,18,38,38),"confirm","","目标 · 实战试炼与成长进度",gold))OpenProgressionGoals();

            if (IconButton(new Rect(x, y, 38, 38), "inventory", "I", "行囊 · I\n查看属性、穿戴装备与时装，使用已有补给。交易请找商人，强化请找铁匠。", jade))
                TogglePanel(Panel.Inventory);
            if (IconButton(new Rect(x + 46, y, 38, 38), "skills", "K", "技能 · K\n按分支学习或进阶技能，配置十格快捷栏。\n可用技能点：" + p.skillPoints, gold, p.skillPoints > 0 ? "+" + p.skillPoints : null))
                TogglePanel(Panel.Skills);
            if (IconButton(new Rect(x + 92, y, 38, 38), "camp", "H", "返回营地 · H\n附近没有敌人时可以返回营地整备。", jade))
                session.ReturnToCamp();
            if (IconButton(new Rect(x + 138, y, 38, 38), "portal", "T", session.InDungeon ? "返回营地 · T\n通关后返回营地；提前撤离需要远离敌人。" : "进入副本 · T\n靠近北面的青色传送门后进入副本。", gold))
            {
                if(session.NearRoomExit)session.EnterNextRoom();
                else if (session.InDungeon) session.ReturnToCamp();
                else session.EnterDungeon();
            }
            if (IconButton(new Rect(x + 184, y, 38, 38), "help", "", "操作指南\n查看移动、战斗、技能施法与自定义快捷键。", muted)) OpenControls();
            if (MobileControls.Active && IconButton(new Rect(x + 230, y, 38, 38), "pause", "", "暂停冒险", muted)) session.SetPaused(true);
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
            Fill(new Rect(0, 0, width, height), new Color(.012f, .025f, .04f, .72f));
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
            if(session.ActiveHubNpc==HubNpcKind.Merchant||session.ActiveHubNpc==HubNpcKind.Blacksmith){DrawHubEquipmentService();return;}
            if(MobileControls.Active){DrawMobileInventory();return;}
            PrepareInventoryPopupInput();
            ProgressionService progression = session.Progression;
            GameProfile p = progression.Profile;
            RebuildBagItems();
            ItemData picked = ResolveSelectedItem();
            Rect w = Modal(1160, 638, HubInventoryTitle, "");
            Text(new Rect(w.x + 24, w.y + 59, 238, 31), HubInventoryHint, 11, muted, false, true);
            Text(new Rect(w.x + 530, w.y + 55, 600, 31), HubNpcServiceSubtitle(""), 13, pale, false, true);
            if (NavigationButton(new Rect(w.xMax - 69, w.y + 20, 44, 32), "×", jade)) ClosePanel();
            Text(new Rect(w.x + 789, w.y + 28, 268, 30), Money(p.gold) + " 金币", 21, gold, true, false, TextAnchor.MiddleRight);
            float left = w.x + 24;
            Text(new Rect(left, w.y + 112, 112, 23), "身上装备", 16, jade, true);
            DrawCurrentWear(new Rect(left,w.y+148,232,232),1);
            StatBlock stats = progression.GetStats();
            Rule(left, w.y + 389, 232, jade);
            Text(new Rect(left, w.y + 402, 232, 22), "角色属性 · Lv." + p.level, 15, jade, true);
            StatLine(left, w.y + 433, "攻击", Mathf.RoundToInt(stats.Damage).ToString(), gold);
            StatLine(left, w.y + 461, "防御", Mathf.RoundToInt(stats.Armor).ToString(), pale);
            StatLine(left, w.y + 489, "生命上限", Mathf.RoundToInt(stats.MaxHealth).ToString(), pale);
            if(new Rect(left,w.y+459,232,27).Contains(Mouse))tooltip="当前护甲减伤 "+((1f-CombatBalance.ArmorDamageMultiplier(stats.Armor,p.level))*100f).ToString("0.0")+"% · 护甲上限70%\n等级越高，护甲换算尺度越高；含临时防御最多84%减伤，真正闪避无敌帧除外。";
            StatLine(left, w.y + 517, "暴击几率", Mathf.RoundToInt(stats.CritChance * 100) + "%", pale);
            if (new Rect(left, w.y + 514, 232, 29).Contains(Mouse))
            {
                int baseCrit = p.heroClass == HeroClass.Ranger ? 14 : (p.heroClass == HeroClass.Arcanist || p.heroClass == HeroClass.Summoner) ? 10 : 8;
                int totalCrit = Mathf.RoundToInt(stats.CritChance * 100);
                int rank = p.skillRanks == null || p.skillRanks.Length <= 3 ? 0 : p.skillRanks[3];
                int passiveCrit = p.heroClass == HeroClass.Ranger ? (rank <= 0 ? 0 : rank == 1 ? 4 : rank == 2 ? 7 : 12) : 0;
                FashionData weaponFashion = progression.StrongestFashion(FashionSlot.Weapon);
                int fashionPercent = weaponFashion == null ? 0 : ProgressionService.WeaponFashionPercent(weaponFashion.rarity);
                tooltip = "暴击几率来源\n" + GameBalance.ClassName(p.heroClass) + "基础：" + baseCrit + "%\n游侠被动：+" + passiveCrit + " 个百分点" +
                    "\n武器时装：乘以 " + (100 + fashionPercent) + "%\n当前：" + totalCrit + "%\n暴击伤害为普通伤害的 1.65 倍。\n" + BuildCatalog.DamageRules;
            }

            float middle = w.x + 272;
            Rect bagArea=new Rect(middle,w.y+144,864,462);
            if(inventoryFashionOpen){mobileInventoryTab=3;inventoryFashionOpen=false;inventoryComparisonOpen=false;}
            if(QuietAction(new Rect(middle+460,w.y+108,64,32),"装备",true,null,mobileInventoryTab==0))mobileInventoryTab=0;
            if(QuietAction(new Rect(middle+532,w.y+108,64,32),"补给",true,null,mobileInventoryTab==2))mobileInventoryTab=2;
            if(QuietAction(new Rect(middle+604,w.y+108,64,32),"时装",true,null,mobileInventoryTab==3))mobileInventoryTab=3;
            if(mobileInventoryTab==3){DrawBagFashion(InventoryArea(bagArea));return;}
            if(mobileInventoryTab==2){DrawBagSupplies(InventoryArea(bagArea));return;}
            Text(new Rect(middle, w.y + 112, 280, 23), "装备 · " + bagItems.Count + " 件", 16, jade, true);
            Text(new Rect(middle + 280, w.y + 117, 144, 17), "总容量 " + p.inventory.Count + " / " + ProgressionService.InventoryCapacity, 11, muted, false, false, TextAnchor.MiddleRight);
            bool changed = false;
            string[] filters = { "全部", "武器", "护甲", "饰品" };
            for (int i = 0; i < filters.Length; i++)
                if (QuietAction(new Rect(middle + i * 76, w.y + 144, 68, 27),filters[i],true,null,inventoryFilter==i-1))
                {
                    inventoryFilter = i - 1;
                    inventoryScroll = Vector2.zero;
                    changed = true;
                }
            if (changed) { RebuildBagItems(); ResolveSelectedItem(); }
            Rect viewport=new Rect(middle,w.y+180,864,426);
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
            if (NavigationButton(new Rect(w.xMax - 69, w.y + 20, 44, 32), "×", jade)) ClosePanel();
            for (int slot = 0; slot < GameBalance.HotbarSize; slot++)
            {
                int entry = LearnedSkillAtSlot(p, slot);
                bool current = entry == GameBalance.HotbarPotion;
                Rect tile = new Rect(w.x + 24 + slot % 5 * 156, w.y + 114 + (1 - slot / 5) * 76, 148, 64);
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
                    if(!IsEquipped(inventory[i]))unequippedCount++;
                    if (inventoryFilter < 0 || (int)inventory[i].slot == inventoryFilter) bagItems.Add(inventory[i]);
                }
            bagItems.Sort(CompareInventoryItems);
        }

        private int CompareInventoryItems(ItemData a, ItemData b)
        {
            int comparison=EquipmentPreviewScore(b).CompareTo(EquipmentPreviewScore(a));
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

        private void SellInventoryItem(string id,bool confirmed=false)
        {
            if(!MerchantServiceActive){Feedback(false,"请在商人处选择并出售装备。");return;}
            if(!confirmed&&session.Progression.PresetReferences(id).Length>0){RequestPresetSale(id);return;}
            int row = bagItems.FindIndex(item => item.id == id);
            if (row < 0) return;
            ItemData item = bagItems[row];
            if (IsEquipped(item)) return;
            int before = session.Progression.Profile.gold;
            bool sold = session.Progression.Sell(id,confirmed);
            int gained = session.Progression.Profile.gold - before;
            Feedback(sold, "已出售 " + item.name + " · +" + gained + " 金币");
            if (!sold) return;
            bool replaceSelection = selectedItem == id;
            RebuildBagItems();
            if (replaceSelection)
                selectedItem = bagItems.Count == 0 ? null : bagItems[Mathf.Min(row, bagItems.Count - 1)].id;
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
            return (session.Progression.Equipped(item.slot) == null ? "此部位尚未穿戴装备。" : "自动继承部位强化后的基础属性评分更高；请同时比较机制效果。") +
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
            if(skillSection==1){DrawSkillDevelopment();return;}
            if(MobileControls.Active){DrawMobileSkills();return;}
            GameProfile p = session.Progression.Profile;
            selectedSkill = Mathf.Clamp(selectedSkill, 0, GameBalance.SkillCount - 1);
            Rect w = Modal(1160, 660, GameBalance.ClassName(p.heroClass) + " · 技能", "");
            if (NavigationButton(new Rect(w.xMax - 69, w.y + 20, 44, 32), "×", jade)) ClosePanel();
            Text(new Rect(w.x + 763, w.y + 28, 296, 32), "技能点 " + p.skillPoints + "   /   角色 Lv." + p.level, 18, gold, true, false, TextAnchor.MiddleRight);
            DrawSkillTabs(new Rect(w.x+24,w.y+72,506,30));
            Rect branchHeading = new Rect(w.x + 24, w.y + 112, 267, 24);
            Text(branchHeading, "职业分支", 15, jade, true);
            if (branchHeading.Contains(Mouse)) tooltip = "沿分支从上到下学习，需先掌握前置技能。\n滚动查看高阶技能；每升一级获得 1 技能点。";
            if (!MobileControls.Active && NavigationButton(new Rect(w.x + 325, w.y + 108, 205, 29), "自定义快捷键", gold)) OpenBindings();
            Rect viewport = new Rect(w.x + 24, w.y + 147, 506, 468);
            Fill(viewport, new Color(.025f, .05f, .075f));
            Rect content = new Rect(0, 0, 490, 738);
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
                Fill(node, selectedSkill == i ? new Color(.12f, .20f, .23f) : rank > 0 ? new Color(.06f, .145f, .15f) : card);
                Border(node, selectedSkill == i ? gold : new Color(accent.r, accent.g, accent.b, rank > 0 || canLearn ? .7f : .25f), selectedSkill == i ? 2 : 1);
                DrawSkillIdentity(new Rect(node.x+5,node.y+7,24,24),p.heroClass,i,rank,rank>0||canLearn,24);
                Text(new Rect(node.x + 32, node.y + 7, 107, 24), GameBalance.SkillName(p.heroClass, i), 14, rank > 0 || canLearn ? pale : muted, true, false, TextAnchor.MiddleCenter);
                Text(new Rect(node.x + 5, node.y + 35, 134, 18), "Lv." + required + " / " + (passive ? "被动" : "主动"), 11, passive ? new Color(.82f, .74f, .98f) : muted, false, false, TextAnchor.MiddleCenter);
                string state = rank > 0 ? GameBalance.SkillRankName(rank) + (canLearn ? " · 可进阶" : " · 已学习") : canLearn ? "可学习" : !prerequisitesMet ? "需要前置" : p.level < required ? "等级未达" : "需要技能点";
                Text(new Rect(node.x + 5, node.y + 57, 134, 17), state, 11, accent, true, false, TextAnchor.MiddleCenter);
                Badge(node,Attention.LearnableSkills.Contains(i));
                Rect visibleNode = new Rect(viewport.x + node.x, viewport.y + node.y - skillScroll.y, node.width, node.height);
                if (viewport.Contains(Mouse) && visibleNode.Contains(Mouse))
                    tooltip = SkillTooltip(p, i, rank);
                if (GUI.Button(node, GUIContent.none, invisibleButton)) selectedSkill = i;
            }
            EndTouchScroll();
            GUI.skin.verticalScrollbarThumb = priorThumb;
            DrawSkillDetail(new Rect(w.x + 550, w.y + 112, 586, GameBalance.IsPassive(selectedSkill) ? 393 : 510), selectedSkill);
        }

        private static Rect SkillNodeRect(int skill)
        {
            return new Rect(10 + GameBalance.SkillTreeColumn(skill) * 160, 18 + GameBalance.SkillTreeRow(skill) * 102, 144, 82);
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
            if (NavigationButton(new Rect(w.xMax - 69, w.y + 20, 44, 32), "×", jade)) ClosePanel();
            Rect pageHeading = new Rect(w.x + 27, w.y + 114, 620, 25);
            Text(pageHeading, "十格技能栏", 15, jade, true);
            if (pageHeading.Contains(Mouse)) tooltip = "可绑定字母、数字与 F1～F12。已占用的按键会交换位置。\n移动及界面功能键保留。固定十格，可自由配置。";
            for (int i = 0; i < GameBalance.HotbarSize; i++)
            {
                Rect tile = new Rect(w.x + 24 + (i % 5) * 161, w.y + 157 + (1 - i / 5) * 96, 148, 84);
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
            if (NavigationButton(new Rect(w.x + 397, w.y + 437, 419, 39), bindingReturnPause ? "返回暂停菜单" : bindingReturnPanel == Panel.Controls ? "返回操作指南" : "返回技能研习", jade)) ClosePanel();
        }

        private void DrawPause()
        {
            if (DrawSaveFlowConfirmation()) return;
            if (panel == Panel.SaveSelection) { DrawSaveSelection(); return; }
            if (MobileControls.Active) { DrawMobilePause(); return; }
            Rect w = Modal(600, 620, "冒险暂停", "自动保存持续写入当前角色 · 手动保存需确认覆盖");
            Text(new Rect(w.x + 32, w.y + 111, 536, 30), session.ZoneName + "  ·  " + ActiveCharacterName(), 17, jade, true, false, TextAnchor.MiddleCenter);
            if (Button(new Rect(w.x + 32, w.y + 156, 536, 48), "继续冒险", jade, true, "Esc 也可继续冒险。", true)) session.SetPaused(false);
            if (Button(new Rect(w.x + 32, w.y + 218, 260, 48), "保存", gold)) RequestManualSave();
            if (NavigationButton(new Rect(w.x + 308, w.y + 218, 260, 48), "读取存档", jade)) OpenSaveSelection();
            if (DangerButton(new Rect(w.x + 32, w.y + 280, 536, 44), "返回主菜单", muted)) RequestExit(true);
            if (ToggleButton(new Rect(w.x + 32, w.y + 338, 260, 42), GameAudio.Muted ? "声音：已静音" : "声音：已开启", !GameAudio.Muted))
            { GameAudio.Muted = !GameAudio.Muted; if (!GameAudio.Muted) GameAudio.Play(SoundCue.UI); }
            if (NavigationButton(new Rect(w.x + 308, w.y + 338, 260, 42), "自定义快捷键", gold)) OpenBindings();
            if (NavigationButton(new Rect(w.x + 32, w.y + 394, 260, 42), "存档位置 / 迁移", jade))
            { saveReturnPause = true; panel = Panel.SaveLocation; session.SetUIBlocking(true); session.SetPaused(false); }
            if (NavigationButton(new Rect(w.x + 308, w.y + 394, 260, 42), "操作指南", jade)) OpenControls();
#if !UNITY_IOS && !UNITY_ANDROID
            if (DangerButton(new Rect(w.x + 32, w.y + 450, 536, 44), "退出游戏", muted)) RequestExit(false);
#endif
            DrawAccessibilityStrip(new Rect(w.x + 32, w.y + 520, 536, 35));
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
            if (NavigationButton(new Rect(w.xMax - 69, w.y + 20, 44, 32), "×", jade)) ClosePanel();
            Rect keyboard = new Rect(w.x + 24, w.y + 112, 650, 442);
            Fill(keyboard, card);
            Text(new Rect(keyboard.x + 18, keyboard.y + 12, 610, 25), "移动与战斗", 16, jade, true);
            DrawKeyCap(new Rect(keyboard.x + 81, keyboard.y + 47, 52, 49), "W", "前", jade);
            DrawKeyCap(new Rect(keyboard.x + 23, keyboard.y + 103, 52, 49), "A", "左", jade);
            DrawKeyCap(new Rect(keyboard.x + 81, keyboard.y + 103, 52, 49), "S", "后", jade);
            DrawKeyCap(new Rect(keyboard.x + 139, keyboard.y + 103, 52, 49), "D", "右", jade);
            DrawKeyCap(new Rect(keyboard.x + 249, keyboard.y + 47, 72, 49), "J", "普通攻击", gold);
            DrawKeyCap(new Rect(keyboard.x + 331, keyboard.y + 47, 72, 49), "F", "生命药剂", gold);
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
                Rect keycap = new Rect(keyboard.x + 23 + (i % 5) * 123, keyboard.y + 273 + (1 - i / 5) * 59, 112, 50);
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
                float rowY = w.y + 153 + i * 43;
                Rect key = new Rect(right, rowY, 84, 30);
                Fill(key, card);
                Border(key, new Color(.25f, .38f, .46f));
                Text(key, keys[i], 13, pale, true, false, TextAnchor.MiddleCenter);
                Text(new Rect(right + 99, rowY + 4, 234, 25), actions[i], 13, muted);
            }
            Text(new Rect(right, w.y + 472, 330, 71), "按住右键：左右环绕，上下调整俯仰。\n右键单击：取消选点或蓄力；滚轮缩放。\n空格跳跃，无冷却；移动时跳跃可跨窄河，Shift 闪现。\n背包与技能界面会暂停战斗。", 12, muted, false, true);
            if (NavigationButton(new Rect(w.x + 24, w.y + 575, 650, 39), "自定义技能按键", gold)) OpenBindings();
            if (controlsReturnPause && NavigationButton(new Rect(right, w.y + 575, 338, 39), "返回暂停菜单", jade)) ClosePanel();
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
            Rect w = Modal(800, 500, "存档位置与迁移", "游戏安装目录与角色存档分开保存，重新安装游戏可继续原有冒险。");
            if (NavigationButton(new Rect(w.xMax - 69, w.y + 20, 44, 32), "×", jade)) ClosePanel();
            string path = session.Progression.SaveDirectory;
            Fill(new Rect(w.x + 24, w.y + 113, 752, 79), card);
            Text(new Rect(w.x + 40, w.y + 123, 720, 17), "当前存档文件夹", 11, jade, true);
            Text(new Rect(w.x + 40, w.y + 147, 720, 37), path, 13, pale, false, true);
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
            Text(new Rect(w.x + 28, w.y + 268, 744, 23), "角色文件：" + System.IO.Path.GetFileName(session.Progression.SaveFilePath), 14, jade, true);
            Text(new Rect(w.x + 28, w.y + 307, 744, 96), "同一台电脑可将游戏安装或移动到任意目录，存档仍从上面的固定位置读取。\n\n迁移前先退出两端游戏并备份完整存档目录，再复制角色 .json、对应 .bak、可恢复 .tmp 与 .delete-pending 删除标记。不要单独恢复旧备份或省略删除标记。启动后从存档列表读取角色。", 14, pale, false, true);
            Text(new Rect(w.x + 28, w.y + 415, 744, 21), "建议迁移前保留一份备份；新电脑的存档目录也可从此页面打开。", 12, muted);
            if (NavigationButton(new Rect(w.x + 24, w.y + 451, 752, 31), "返回暂停菜单", jade)) ClosePanel();
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
            float boxHeight = Style(13, false, true).CalcHeight(new GUIContent(tooltip), 288) + 22;
            Vector2 mouse = Mouse;
            Rect r = new Rect(Mathf.Clamp(mouse.x - 154, 12, width - 324), Mathf.Clamp(mouse.y - boxHeight - 14, 12, height - boxHeight - 12), 312, boxHeight);
            Box(r, jade);
            Text(new Rect(r.x + 12, r.y + 10, 288, boxHeight - 20), tooltip, 13, pale, false, true);
        }

        private void TogglePanel(Panel value)
        {
            if (session == null || !session.HasStarted || session.IsDead || session.Paused) return;
            panel = panel == value ? Panel.None : value;
            if(panel==Panel.Skills)ResetMobileSkillNavigation();
            session.SetUIBlocking(panel != Panel.None);
        }

        private void ClosePanel()
        {
            if(presetSaleOpen){CancelPresetSale();return;}
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
                if (session.Progression.Profile.pendingFashionChest) return;
                if (session.Progression.Profile.pendingChestReveal)
                {
                    if(!ChestAnimationDone)chestRevealedAt=Time.unscaledTime-ChestDuration;
                    else FinishChestReveal();
                    return;
                }
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
            string issue = session.Progression.LastError;
            session.Notify(success ? message + (string.IsNullOrEmpty(issue) ? "" : " · " + issue) : (string.IsNullOrEmpty(issue) ? "当前无法执行此操作" : issue));
        }
    }
}
