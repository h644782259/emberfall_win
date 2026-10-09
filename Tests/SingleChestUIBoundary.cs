using System;using System.IO;using System.Collections.Generic;using System.Linq;using Emberfall;using UnityEngine;
namespace UnityEngine{
 public enum TextAnchor{MiddleRight,MiddleCenter,MiddleLeft}
 public class GUIContent{public string text;public GUIContent(string s){text=s;}}
 public class GUIStyle{public float CalcHeight(GUIContent c,float width)=>Math.Max(20,(float)Math.Ceiling(c.text.Length*12/Math.Max(1,width))*16);}
 public struct Vector2{public float x,y;public static Vector2 zero=>new Vector2();}
 public struct Rect{public float x,y,width,height;public Vector2 center=>new Vector2{x=x+width/2,y=y+height/2};public float xMax=>x+width;public float yMax=>y+height;public Rect(float a,float b,float w,float h){x=a;y=b;width=w;height=h;}}
 public static class Mathf{public static int Min(int a,int b)=>Math.Min(a,b);public static float Clamp01(float v)=>Math.Min(1,Math.Max(0,v));public static float SmoothStep(float a,float b,float t)=>a+(b-a)*t;public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int RoundToInt(float x)=>(int)Math.Round(x);public static float Clamp(float x,float a,float b)=>Math.Min(b,Math.Max(a,x));public static int Clamp(int x,int a,int b)=>Math.Min(b,Math.Max(a,x));}
 public static class Time{public static float unscaledTime;}
 public static class GUI{public static bool enabled=true;}
}
namespace Emberfall{
 public static class UIIconAtlas{public static int Utility(string name)=>10;public static int Reward(int kind)=>kind;public static int FashionCardIcon(FashionSlot slot,int appearanceTier=3,HeroClass hero=HeroClass.Vanguard)=>4+(int)slot;}
 public static class MobileControls{public static bool Active;public static MobilePanelLayout Layout;}
 public static class EffectPreferences{public static bool ReducedEffects;}
 public enum SoundCue{UI,Cast,Loot,LevelUp,Victory}public static class GameAudio{public static void Play(SoundCue s){}}
 public class PlayerController{public int CombatEpoch;}
 public class GameSession{public RunRecapSnapshot LastRunRecap;public bool FinishedResultDismissed;public void DismissFinishedResult(){FinishedResultDismissed=true;InputBlocked=false;}public PlayerController Player=new PlayerController();public bool ChapterRewardPending;public bool TrySettleChapterReward(){if(FailSettlement)return false;ChapterRewardPending=false;return true;}public bool DungeonRewardPending,ModeRewardPending,FailSettlement;public bool TrySettleDungeonReward(){if(FailSettlement)return false;DungeonRewardPending=false;return true;}public bool TrySettleArenaReward(){if(FailSettlement)return false;ModeRewardPending=false;return true;}public bool HasStarted=true,Paused,IsDead,InputBlocked,InDungeon,NearDungeonReturn;public ProgressionService Progression;public void LogSystem(string s){}public void SetUIBlocking(bool b){InputBlocked=b;}public void SetPaused(bool b){}}
 public sealed partial class GameUI{
  private ChestReward settlementChest;private PlayerController settlementOwner;private string settlementSlot;private int settlementEpoch=-1;
  bool SmithServiceActive=>true;
  bool MerchantServiceActive=>true;enum Panel{DungeonExit,Summary,None,Chests,Camp,Skills,Inventory}Panel panel=Panel.Chests;GameSession session;float width,height,TouchRatio=1;List<Rect> blockedRects=new List<Rect>();void Box(Rect r,Color c){}Color gold,jade,pale,muted,card;GUIStyle Style(int size,bool bold=false,bool wrap=false)=>new GUIStyle();
  bool chestDetails,chestOpening,rewardSoundPlayed;int revealedChest=-1;string chestRevealResult,chestReceiptId,chestQualificationId,mobileChestError;float chestRevealedAt;Rect chestRevealOrigin;Vector2 desktopChestResultScroll,mobileChestScroll,mobileChestArtScroll;
  readonly List<(string caption,Rect bounds)> buttons=new List<(string,Rect)>();string click;bool clicked;int scrollDepth;
  bool Button(Rect r,string s,Color c,bool enabled=true,string reason=null,bool highlight=false){buttons.Add((s,r));if(!enabled||clicked||click!=s)return false;clicked=true;return true;}
  // Graphics boundary for the existing 483b47b button-role API; click semantics stay in Button.
  enum ButtonRole { Action, Primary, Navigation, Danger, Tab, SelectedTab, Toggle, ActiveToggle, Row, SelectedRow }
  bool DrawButton(Rect r,string s,ButtonRole role,bool enabled=true,string hint=null,int fontSize=0,string controlName=null)=>Button(r,s,gold,enabled,hint);
  bool NavigationButton(Rect r,string s,Color accent,bool enabled=true,string hint=null,bool primary=false)=>DrawButton(r,s,ButtonRole.Navigation,enabled,hint);
  bool PrimaryButton(Rect r,string s,Color accent,bool enabled=true,string hint=null,bool primary=false)=>DrawButton(r,s,ButtonRole.Primary,enabled,hint);
  readonly List<int> resourceIcons=new List<int>();readonly List<string> resourceNumbers=new List<string>();int legacyDraws;
  int equipmentDraws;
  class EntryRewardPreview{public string Key,Name,Description;public Rarity Rarity;public Color Tint;public int Icon;}
  EntryRewardPreview ActualEquipmentPreview(ItemData item)=>new EntryRewardPreview();
  void InspectRewardItem(Rect r,EntryRewardPreview p){}void DrawEntryRewardIcon(Rect r,EntryRewardPreview p,float u){}
  string GemDropDescription(EquipmentMechanic m,bool b)=>"";
  void DrawInventoryIcon(Rect r,ItemData item,float u){equipmentDraws++;}void DrawIcon(Rect r,int icon,Color c){resourceIcons.Add(icon);}void DrawChestGoldReward(Rect r,ChestReward reward,Color c){legacyDraws++;}bool DrawChestRewardModel(Rect r,ChestReward reward)=>false;
  void Fill(Rect r,Color c){}void Border(Rect r,Color c){}void Text(Rect r,string s,int size,Color c,bool bold=false,bool wrap=false,TextAnchor anchor=default){resourceNumbers.Add(s);}
  void DrawRewardChest(Rect r,bool opened,float alpha,float progress){}void DrawDesktopChestRules(Rect r){}void DrawDesktopChestResult(Rect r,ChestReward reward,Color c){}void DrawChestRevealTransition(Rect r,ChestReward reward,Rect target){}
  void DrawMobileChestDetails(MobilePanelLayout l){}void DrawMobileChestResult(MobilePanelLayout l,ChestReward r,Color c,bool complete){}
  bool DrawMobilePanelChrome(MobilePanelLayout l,string title,string subtitle,bool showClose=true,bool menu=true)=>false;
  MobilePanelLayout MobilePanelGeometry()=>new MobilePanelLayout(width/TouchRatio,height/TouchRatio);
  Rect MobilePanelRect(MobilePanelLayout.Area a)=>TouchRect(a.X,a.Y,a.Width,a.Height);
  Rect TouchRect(float x,float y,float w,float h)=>new Rect(x*TouchRatio,y*TouchRatio,w*TouchRatio,h*TouchRatio);
  float MeasureMobileParagraph(string s,float w,int font)=>string.IsNullOrEmpty(s)?0:(float)Math.Ceiling(s.Length*font/Math.Max(1,w))*(font+2);
  float DrawMobileParagraph(float x,float y,float w,string s,int font,Color color)=>MeasureMobileParagraph(s,w,font);
  Vector2 BeginTouchScroll(string id,Rect r,Vector2 position,Rect content){scrollDepth++;return position;}void EndTouchScroll(){scrollDepth--;}
  void CancelMobileScroll(){}void BlockUITransition(){}void Feedback(bool ok,string text){}void ClosePanel(){panel=Panel.Camp;}bool AcceptChestForTrial(){throw new Exception("unexpected trial request");}
  private ProgressionService dismissedChestOwner;private string dismissedChestSlot,dismissedChestId;private bool chestRecoveryService,merchantShopOpen;
  bool PopupCloseButton(Rect r)=>Button(r,"关闭",gold);
  void OpenChestRecoveryService(){}
  void Frame(string action=null){MobileControls.Layout=new MobilePanelLayout(width/TouchRatio,height/TouchRatio);click=action;clicked=false;buttons.Clear();DrawChests();if(scrollDepth!=0)throw new Exception("actual page left a scroll scope open");}
  static int n;static void C(bool ok,string message){n++;if(!ok)throw new Exception(message);}
  public static void Run(string root){
   {
    var p=new ProgressionService(Path.Combine(root,"inline"));p.NewGame(HeroClass.Vanguard);p.PrepareDungeonChest();
    var ui=new GameUI{session=new GameSession{Progression=p,InDungeon=true}};ui.RefreshSettlementChest();
    ui.session.DungeonRewardPending=true;ui.session.FailSettlement=true;ui.CollectSettlementRewards();C(p.Profile.pendingFashionChest&&ui.settlementChest==null,"base save failure blocks chest grant");
    ui.session.FailSettlement=false;Directory.CreateDirectory(p.SaveFilePath+".tmp");ui.CollectSettlementRewards(2);Directory.Delete(p.SaveFilePath+".tmp");C(p.Profile.pendingFashionChest&&ui.settlementChest==null,"inline failed write keeps entitlement");
    ui.CollectSettlementRewards(2);C(!p.Profile.pendingFashionChest&&p.Profile.pendingChestReveal&&ui.settlementChest!=null&&!ui.ChestAnimationDone,"inline committed reward starts animation before acknowledgement");
    string saved=JsonUtility.ToJson(p.Profile,true),id=ui.settlementChest.id;ui.CollectSettlementRewards();ui.RefreshSettlementChest();C(saved==JsonUtility.ToJson(p.Profile,true)&&ui.settlementChest.id==id,"repeat claim and reopen never grant twice");
    ui.CloseSettlement();C(!p.Profile.pendingChestReveal&&!ui.session.InputBlocked,"closing acknowledges and releases input");ui.session.Player.CombatEpoch++;ui.RefreshSettlementChest();C(ui.settlementChest==null,"new run cannot display previous chest");
    p.PrepareDungeonChest();p.OpenDungeonChest();ui.RefreshSettlementChest();C(ui.settlementChest!=null,"pending durable reveal recovers inline");
    Directory.CreateDirectory(p.SaveFilePath+".tmp");ui.CloseSettlement();Directory.Delete(p.SaveFilePath+".tmp");C(p.Profile.pendingChestReveal&&ui.settlementChest!=null&&!ui.session.InputBlocked,"failed acknowledgement preserves receipt but never traps player");ui.CloseSettlement();C(!p.Profile.pendingChestReveal,"acknowledgment retries in place");
   }
   {
    var p=new ProgressionService(Path.Combine(root,"variant"));p.NewGame(HeroClass.Arcanist);p.Profile.mechanicMaterials=40;p.Profile.attachments.Add(new MechanicAttachment{id=Guid.NewGuid().ToString("N"),mechanic=EquipmentMechanic.CinderTrail,ascensionRank=1,variantUnlocked=true});p.Save();
    var ui=new GameUI{session=new GameSession{Progression=p}};
    C(ui.SmithVariantDescription(EquipmentMechanic.CinderTrail,0)!=ui.SmithVariantDescription(EquipmentMechanic.CinderTrail,1),"elementalist forms show distinct descriptions");
    ui.SelectSmithVariant(EquipmentMechanic.CinderTrail,1);C(p.Attachment(EquipmentMechanic.CinderTrail).variant==1&&p.Profile.mechanicMaterials==40,"click directly selects unlocked form");
    ui.SelectSmithVariant(EquipmentMechanic.CinderTrail,1);C(p.Profile.mechanicMaterials==40&&p.Attachment(EquipmentMechanic.CinderTrail).variant==1,"selected form click is inert");
    ui.SelectSmithVariant(EquipmentMechanic.CinderTrail,0);C(p.Profile.mechanicMaterials==40&&p.Attachment(EquipmentMechanic.CinderTrail).variant==0,"unlocked switching is immediate and free");
    Directory.CreateDirectory(p.SaveFilePath+".tmp");ui.SelectSmithVariant(EquipmentMechanic.CinderTrail,1);Directory.Delete(p.SaveFilePath+".tmp");C(p.Attachment(EquipmentMechanic.CinderTrail).variant==0,"failed write retains previous selected form");
   }

   foreach(var size in new[]{new[]{568f,320f,1f},new[]{844f,390f,1f},new[]{1024f,768f,1f},new[]{1280f,720f,1f},new[]{1136f,640f,2f}})
   {
    var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));p.NewGame(HeroClass.Ranger);p.PrepareDungeonChest();MobileControls.Active=size[0]<1200;Time.unscaledTime=0;
    var ui=new GameUI{session=new GameSession{Progression=p,InDungeon=true},width=size[0],height=size[1],TouchRatio=size[2]};
    ui.session.LastRunRecap=new RunRecapSnapshot(true,true,false,3,5,5,1,2,1,null,0,new[]{new KeyValuePair<string,int>("damage-total",12345)},null,null,true,false);
    ui.DrawVictorySettlement();C(ui.scrollDepth==0&&ui.buttons.Count(b=>b.caption=="开启")==3,"unified settlement offers three chests without scrolling");
    C(!ui.buttons.Any(b=>b.caption=="继续拾取"||b.caption=="返回营地"),"settlement does not include navigation");
    foreach(var b in ui.buttons)C(b.bounds.x>=0&&b.bounds.y>=0&&b.bounds.xMax<=ui.width&&b.bounds.yMax<=ui.height,"settlement actions remain in screen bounds");
    string unopened=JsonUtility.ToJson(p.Profile,true);ui.DrawVictorySettlement();C(unopened==JsonUtility.ToJson(p.Profile,true),"drawing choices does not mutate rewards");
    ui.click="开启";ui.clicked=false;ui.DrawVictorySettlement();C(p.Profile.pendingChestReveal&&ui.settlementChest!=null&&!ui.ChestAnimationDone,"choice starts persisted reward animation in same settlement");
    Time.unscaledTime+=10;string opened=JsonUtility.ToJson(p.Profile,true);ui.click=null;ui.DrawVictorySettlement();C(opened==JsonUtility.ToJson(p.Profile,true),"animation completion never regrants items");
    ui.CloseSettlement();C(!p.Profile.pendingChestReveal&&ui.session.FinishedResultDismissed&&!ui.session.InputBlocked,"close dismisses result and unlocks world");
   }
   foreach(bool mobile in new[]{false,true})foreach(bool reduced in new[]{false,true})foreach(float ratio in new[]{1f,2f}){
    var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));p.NewGame(HeroClass.Vanguard);p.PrepareDungeonChest();int events=0;p.Changed+=()=>events++;
    MobileControls.Active=mobile;EffectPreferences.ReducedEffects=reduced;Time.unscaledTime=0;
    var ui=new GameUI{session=new GameSession{Progression=p},TouchRatio=ratio,width=mobile?568*ratio:1280,height=mobile?320*ratio:720};
    ui.session.InDungeon=true;ui.panel=Panel.None;ui.EnsurePendingChestPanel();C(ui.panel==Panel.None&&!ui.session.InputBlocked,"dungeon chest never opens automatically");ui.OpenDungeonExit();C(ui.panel==Panel.None,"portal menu rejects distant interaction");ui.session.NearDungeonReturn=true;ui.OpenDungeonExit();C(ui.panel==Panel.DungeonExit&&ui.session.InputBlocked,"near portal opens menu only on interaction");ui.session.InDungeon=false;ui.session.SetUIBlocking(false);ui.panel=Panel.Chests;
    ui.Frame();C(ui.buttons.Count(x=>x.caption.StartsWith("宝箱 "))==3,"three equivalent chest choices");C(!ui.buttons.Any(x=>x.caption=="返回"),"unopened chest has no bypass action");C(!ui.buttons.Any(x=>x.caption=="兵装"||x.caption=="羽翼"||x.caption=="补给"),"no choice buttons");
    var opener=ui.buttons.Single(x=>x.caption=="宝箱 2");C(opener.bounds.height>=(mobile?48*ratio:42)&&opener.bounds.width>0,"opener keeps platform action height");
    string before=JsonUtility.ToJson(p.Profile,true);Directory.CreateDirectory(p.SaveFilePath+".tmp");ui.Frame("宝箱 2");Directory.Delete(p.SaveFilePath+".tmp");
    C(!ui.chestOpening&&p.Profile.pendingFashionChest&&!p.Profile.pendingChestReveal&&events==0&&JsonUtility.ToJson(p.Profile,true)==before,"actual UI failed write resets latch and grants nothing");
    if(mobile)ui.DismissChestPanel();else ui.Frame("关闭");C(ui.panel==Panel.None&&!ui.session.InputBlocked&&p.Profile.pendingFashionChest,"failed unopened chest can close without losing entitlement");ui.EnsurePendingChestPanel();C(ui.panel==Panel.None,"dismissed chest does not reopen every frame");
    ui.click="领取宝箱";ui.clicked=false;ui.DrawPendingChestReturn();C(ui.panel==Panel.Chests&&p.Profile.pendingFashionChest,"manual reopening preserves pending chest");
    ui.Frame();C(ui.buttons.Count(x=>x.caption=="继续开启")==1,"failed draw offers one continue action");ui.Frame("继续开启");C(events==1&&p.Profile.pendingChestReveal,"one click persists entire reward before reveal");
    ui.resourceIcons.Clear();ui.resourceNumbers.Clear();string visualBefore=JsonUtility.ToJson(p.Profile,true);
    ui.DrawChestCommittedReward(new Rect(0,0,240*ratio,220*ratio),p.LastChestReward,ui.gold);
    C(ui.resourceIcons.Count==0&&ui.equipmentDraws==0,"left art does not duplicate reward totals or equipment");ui.DrawChestRewardContents(300,ratio,p.LastChestReward,null,true);
    C(ui.equipmentDraws==p.LastChestReward.equipmentIds.Length,"all awarded equipment appears in the right column");
    C(ui.resourceIcons.Take(3).SequenceEqual(new[]{0,1,2}),"actual receipt draws coin, fragment and thread pictograms");
    C(ui.resourceNumbers.Where(s=>s.StartsWith("+")).SequenceEqual(new[]{"+"+p.LastChestReward.goldDelta,"+"+p.LastChestReward.materialsDelta,"+"+p.LastChestReward.threadsDelta}),"actual resource numbers use committed deltas");
    C(JsonUtility.ToJson(p.Profile,true)==visualBefore&&events==1,"resource rendering never grants or acknowledges");
    ui.resourceIcons.Clear();ui.resourceNumbers.Clear();ui.DrawChestResourceVisuals(new Rect(0,0,240,220),new ChestReward{hasCurrencyDeltas=true});C(ui.resourceIcons.Count==0&&ui.resourceNumbers.Count==0,"zero actual capped deltas draw no invented gain");
    ui.DrawChestResourceVisuals(new Rect(0,0,240,220),new ChestReward());C(ui.legacyDraws==1,"legacy receipt without deltas keeps legacy presentation");
    int gold=p.Profile.gold,materials=p.Profile.mechanicMaterials,threads=p.Profile.fashionThreads;string saved=JsonUtility.ToJson(p.Profile,true),id=p.LastChestReward.id;ui.Frame("继续开启");ui.Frame("跳过动画");C(events==1&&JsonUtility.ToJson(p.Profile,true)==saved&&p.LastChestReward.id==id,"repeat/skip only changes presentation");
    Time.unscaledTime+=10;ui.Frame();
    C(!ui.buttons.Any(x=>x.caption=="收下并查看时装"),"removed fashion action stays absent");
    ui.Frame("收下");C(events==2&&!p.Profile.pendingChestReveal&&p.LastChestReward.id==id&&p.Profile.gold==gold&&p.Profile.mechanicMaterials==materials&&p.Profile.fashionThreads==threads,"one normal acknowledgement retains receipt and amounts");
   }
   Console.WriteLine("PASS "+n+" actual desktop/mobile three-choice, failure/retry, skip and acknowledgement assertions; managed GUI boundaries");
  }
 }
}
