using System;using System.IO;using System.Collections.Generic;using System.Linq;using Emberfall;using UnityEngine;
namespace UnityEngine{
 public struct Vector2{public float x,y;public static Vector2 zero=>new Vector2();}
 public class Texture{}public struct Matrix4x4{public float m00,m11;}public enum ScaleMode{ScaleToFit}public enum EventType{MouseDown,MouseUp,MouseDrag,ScrollWheel,Repaint,Used}public class Event{public static Event current=new Event{type=EventType.Repaint};public EventType type;public void Use(){type=EventType.Used;}}
 public struct Rect{public bool Contains(Vector2 p)=>p.x>=x&&p.x<=xMax&&p.y>=y&&p.y<=yMax;public float x,y,width,height;public float xMax=>x+width;public float yMax=>y+height;public Rect(float a,float b,float w,float h){x=a;y=b;width=w;height=h;}}
 public static class Mathf{public static float Lerp(float a,float b,float t)=>a+(b-a)*t;public static float Abs(float x)=>Math.Abs(x);public static float Clamp01(float x)=>Math.Max(0,Math.Min(1,x));public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int RoundToInt(float x)=>(int)Math.Round(x);public static float Clamp(float x,float a,float b)=>Math.Min(b,Math.Max(a,x));public static int Clamp(int x,int a,int b)=>Math.Min(b,Math.Max(a,x));}
 public static class Time{public static float unscaledTime;}
 public static class GUI{public static bool enabled=true;public static Matrix4x4 matrix=new Matrix4x4{m00=1,m11=1};public static void BeginGroup(Rect r){}public static void EndGroup(){}public static void DrawTexture(Rect r,Texture t,ScaleMode m,bool a){}}
}
namespace Emberfall{
 public static class UIIconAtlas{public static int Reward(int kind)=>kind;public static int Utility(string name)=>100;}
 public static class MobileControls{public static bool Active;}
 public static class EffectPreferences{public static bool ReducedEffects;}
 public enum SoundCue{UI,Cast,Loot,LevelUp,Victory}public static class GameAudio{public static bool Muted;public static List<SoundCue> Calls=new List<SoundCue>();public static void Play(SoundCue s){if(!Muted)Calls.Add(s);}}
 public enum CollectionPreviewComposition{Full,Weapon,Back}public class CollectionModelPreview{public static int Renders,Disposed,Slot;public static ItemData Shown;public void Dispose(){Disposed++;}public void SetComposition(CollectionPreviewComposition c){}public void SetEquipmentFraming(bool a,bool b){}public void SetEquipmentHighlight(int slot){Slot=slot;}public void SetYaw(float y){}public void SetViewport(float w,float h,bool mobile){}public Texture Render(HeroClass hero,ItemData a,ItemData b,ItemData c,FashionData d,FashionData e){Renders++;Shown=Slot==0?a:Slot==1?b:c;return new Texture();}}
 public class GameSession{public bool IsInCamp=true,Paused,IsDead,PracticeActive;public ProgressionService Progression;public void LogSystem(string s){}public void SetUIBlocking(bool b){}public void SetPaused(bool b){}}
 public sealed partial class GameUI{
  enum Panel{None,Chests,Camp,Fashion,Inventory}Panel panel=Panel.Chests;GameSession session;float width,height,TouchRatio=1;Color gold,jade,pale,muted;Vector2 Mouse,mobileChestArtScroll;string collectionReceiptKey;
 readonly List<Rect> actualModels=new List<Rect>();readonly List<(Rect bounds,int font,string text)> actualNumbers=new List<(Rect,int,string)>();
 void SetCollectionAngle(FashionSlot slot){}void DrawCollectionModel(Rect r,FashionData f,bool controls){actualModels.Add(r);}
 void DrawRewardRadiance(Rect r,Color c,float progress){}
  bool chestDetails,chestOpening,rewardSoundPlayed;int revealedChest=-1;string chestRevealResult,chestReceiptId,mobileChestError;float chestRevealedAt;Rect chestRevealOrigin;Vector2 desktopChestResultScroll,mobileChestScroll;
  readonly List<(string caption,Rect bounds)> buttons=new List<(string,Rect)>();string click;bool clicked;int scrollDepth;
  bool Button(Rect r,string s,Color c,bool enabled=true,string reason=null,bool highlight=false){buttons.Add((s,r));if(!enabled||clicked||click!=s)return false;clicked=true;return true;}
  readonly List<int> resourceIcons=new List<int>();readonly List<string> resourceNumbers=new List<string>();int legacyDraws;
  void DrawIcon(Rect r,int icon,Color c){resourceIcons.Add(icon);}void DrawChestGoldReward(Rect r,ChestReward reward,Color c){legacyDraws++;}
  void Fill(Rect r,Color c){}void Border(Rect r,Color c){}void Text(Rect r,string s,int size,Color c,bool bold=false,bool wrap=false){resourceNumbers.Add(s);if(s.StartsWith("+")||s.StartsWith("-"))actualNumbers.Add((r,size,s));}
  void DrawRewardChest(Rect r,bool opened,float alpha,float progress){}void DrawDesktopChestRules(Rect r){}void DrawDesktopChestResult(Rect r,ChestReward reward,Color c){}
  void DrawMobileChestDetails(MobilePanelLayout l){}
  bool DrawMobilePanelChrome(MobilePanelLayout l,string title,string subtitle,bool close=true,bool menu=true)=>false;
  MobilePanelLayout MobilePanelGeometry()=>new MobilePanelLayout(width/TouchRatio,height/TouchRatio);
  Rect MobilePanelRect(MobilePanelLayout.Area a)=>TouchRect(a.X,a.Y,a.Width,a.Height);
  Rect TouchRect(float x,float y,float w,float h)=>new Rect(x*TouchRatio,y*TouchRatio,w*TouchRatio,h*TouchRatio);
  float MeasureMobileParagraph(string s,float w,int font,bool bold=false)=>string.IsNullOrEmpty(s)?0:(float)Math.Ceiling(s.Length*font/Math.Max(1,w))*(font+2);
  float DrawMobileParagraph(float x,float y,float w,string s,int font,Color color,bool bold=false)=>MeasureMobileParagraph(s,w,font);
  Vector2 BeginTouchScroll(string id,Rect r,Vector2 position,Rect content){scrollDepth++;return position;}void EndTouchScroll(){scrollDepth--;}
  void CancelMobileScroll(){}void BlockUITransition(){}void Feedback(bool ok,string text){}void ClosePanel(){panel=Panel.Camp;}bool AcceptChestForTrial(){throw new Exception("unexpected trial request");}
  void Frame(string action=null){click=action;clicked=false;buttons.Clear();DrawChests();if(scrollDepth!=0)throw new Exception("actual page left a scroll scope open");}
  static int n;static void C(bool ok,string message){n++;if(!ok)throw new Exception(message);}
  public static void Run(string root){
   foreach(var size in new[]{(568f,320f),(640f,360f),(844f,390f),(1024f,768f)})foreach(float ratio in new[]{1f,1.5f,2f})foreach(bool duplicate in new[]{false,true})foreach(bool reduced in new[]{false,true}){
    var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));p.NewGame(HeroClass.Vanguard);MobileControls.Active=true;EffectPreferences.ReducedEffects=reduced;
    var ui=new GameUI{session=new GameSession{Progression=p},TouchRatio=ratio,width=size.Item1*ratio,height=size.Item2*ratio};
    var receipt=new ChestReward{id="actual-layout",rulesRevision=2,rewardKind=ChestRewardKind.SingleChest,rarityIndex=3,slotIndex=1,duplicate=duplicate,hasCurrencyDeltas=true,goldDelta=1100,materialsDelta=1,materialKind=RewardMaterialKind.StarAshFragment,threadsDelta=9};
    string before=JsonUtility.ToJson(p.Profile,true);ui.DrawMobileChestResult(ui.MobilePanelGeometry(),receipt,ui.gold,true);
    C(ui.actualModels.Count==1&&ui.actualModels[0].height>=140*ratio,"actual compact mobile result reserves readable model height");
    C(ui.actualNumbers.Count==3&&ui.actualNumbers.All(x=>x.bounds.height>=32*ratio&&x.font<=x.bounds.height-6*ratio),"actual compact result gives all three numbers full font line height");
    C(ui.actualNumbers.Select(x=>x.text).SequenceEqual(new[]{"+1100","+1","+9"})&&ui.scrollDepth==0,"actual result draws every actual delta with balanced scrolling");
    C(JsonUtility.ToJson(p.Profile,true)==before,"real result rendering does not mutate progression");
   }
   foreach(bool mobile in new[]{false,true})foreach(bool mutedAudio in new[]{false,true}){
    var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));p.NewGame(HeroClass.Vanguard);MobileControls.Active=mobile;EffectPreferences.ReducedEffects=false;GameAudio.Muted=mutedAudio;GameAudio.Calls.Clear();Time.unscaledTime=0;
    var ui=new GameUI{session=new GameSession{Progression=p},panel=Panel.Camp,width=mobile?568:1280,height=mobile?320:720};ui.PrepareRewardMoment();
    p.Profile.fashionThreads=12;p.Save();var quote=p.QuoteThreadMaterialExchange();Directory.CreateDirectory(p.SaveFilePath+".tmp");C(!p.ExchangeThreadsForMaterial(quote,true),"real failed exchange rejected");Directory.Delete(p.SaveFilePath+".tmp");ui.PrepareRewardMoment();C(ui.rewardMoment==null&&GameAudio.Calls.Count==0,"failed action never plays visual or audio");
    C(p.ExchangeThreadsForMaterial(quote,true),"real exchange commits");string saved=JsonUtility.ToJson(p.Profile,true);ui.PrepareRewardMoment();ui.DrawRewardMoment();C(ui.rewardMoment.Kind==RewardMomentKind.MaterialExchange&&ui.actualNumbers.Select(x=>x.text).SequenceEqual(new[]{"+1","-6"}),"actual moment shows gain and spend, not invented positive totals");
    C(GameAudio.Calls.Count==(mutedAudio?0:1),"one graded cue respects mute boundary");ui.PrepareRewardMoment();p.ExchangeThreadsForMaterial(quote,true);ui.PrepareRewardMoment();C(GameAudio.Calls.Count==(mutedAudio?0:1),"idempotent transaction and redraw never replay sound");
    var skip=ui.RewardMomentSkipRect(ui.RewardMomentRect());ui.Mouse=new Vector2{x=skip.x+1,y=skip.y+1};Event.current.type=EventType.MouseDown;ui.PrepareRewardMoment();C(ui.rewardMoment==null&&Event.current.type==EventType.Used&&JsonUtility.ToJson(p.Profile,true)==saved,"skip consumes pointer before underlying panel and never changes receipt");Event.current.type=EventType.Repaint;
    p.Profile.fashionThreads=30;p.Save();C(p.ChooseLegendaryFashion(FashionSlot.Weapon,true),"real legendary exchange commits");ui.PrepareRewardMoment();int models=CollectionModelPreview.Renders;ui.DrawRewardMoment();C(CollectionModelPreview.Renders==models+1,"safe panel actually invokes dedicated model renderer");int dispose=CollectionModelPreview.Disposed;Time.unscaledTime+=4;ui.PrepareRewardMoment();C(ui.rewardMoment==null&&CollectionModelPreview.Disposed==dispose+1,"expired moment releases dedicated model");
    ui.panel=Panel.None;ui.session.IsInCamp=false;p.Profile.fashionThreads=6;p.Save();p.ExchangeThreadsForMaterial(p.QuoteThreadMaterialExchange(),true);ui.PrepareRewardMoment();models=CollectionModelPreview.Renders;ui.DrawRewardMoment();C(CollectionModelPreview.Renders==models&&!ui.session.Paused&&ui.panel==Panel.None,"combat moment stays local, does not create model pause or change panel");
   }
   GameAudio.Muted=false;
   foreach(bool mobile in new[]{false,true})foreach(bool duplicate in new[]{false,true})foreach(bool reduced in new[]{false,true}){
    var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));p.NewGame(HeroClass.Vanguard);p.PrepareDungeonChest();p.OpenDungeonChest();var receipt=p.LastChestReward;receipt.rarityIndex=3;receipt.slotIndex=0;receipt.duplicate=duplicate;receipt.hasCurrencyDeltas=true;receipt.goldDelta=1100;receipt.materialsDelta=1;receipt.materialKind=RewardMaterialKind.StarAshFragment;receipt.threadsDelta=9;
    MobileControls.Active=mobile;EffectPreferences.ReducedEffects=reduced;GameAudio.Calls.Clear();Time.unscaledTime=0;
    var ui=new GameUI{session=new GameSession{Progression=p},width=568,height=320,chestReceiptId=receipt.Id,chestRevealResult=receipt.summary};
    C(Math.Abs(ui.ChestDuration-(reduced?.15f:duplicate?.7f:1.94f))<.001f,"first and duplicate use distinct duration; reduced mode stays brief");
    ui.DrawChestRevealTransition(new Rect(0,0,300,300),receipt,new Rect(0,0,300,300));C(ui.actualNumbers.Count==(duplicate?3:0),"actual duplicate transition shows conversion resource glyphs; first acquisition retains chest reveal");
    ui.actualNumbers.Clear();receipt.hasCurrencyDeltas=false;ui.DrawChestRevealTransition(new Rect(0,0,300,300),receipt,new Rect(0,0,300,300));C(ui.actualNumbers.Count==0,"legacy transition never invents actual resource deltas");receipt.hasCurrencyDeltas=true;
    string saved=JsonUtility.ToJson(p.Profile,true);ui.Frame("跳过动画");C(ui.ChestAnimationDone&&JsonUtility.ToJson(p.Profile,true)==saved,"skip advances presentation without acknowledging or altering saved receipt");
    ui.Frame();C(GameAudio.Calls.Count==1&&GameAudio.Calls[0]==(duplicate?SoundCue.UI:SoundCue.Victory),"actual desktop/mobile first acquisition and duplicate completion use distinct audio");ui.Frame();C(GameAudio.Calls.Count==1,"completed receipt repaints never replay audio");
   }
   foreach(bool mobile in new[]{false,true}){
    MobileControls.Active=mobile;EffectPreferences.ReducedEffects=false;var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));p.NewGame(HeroClass.Vanguard);p.Profile.level=45;p.Profile.bestFloor=5;p.Profile.highestAdventureTier=5;p.Profile.clearedRuns=1;p.Profile.pendingFirstClearReward=true;p.Profile.mechanicMaterials=100;p.Save();
    var ui=new GameUI{session=new GameSession{Progression=p},panel=Panel.Camp,width=568,height=320};ui.PrepareRewardMoment();
    var mechanic=Enum.GetValues(typeof(EquipmentMechanic)).Cast<EquipmentMechanic>().First(m=>m!=EquipmentMechanic.None&&BuildCatalog.MechanicClass(m)==p.Profile.heroClass);
    foreach(int action in new[]{0,1,2}){
     bool ok=action==0?p.ClaimFirstClearReward(mechanic):action==1?p.ExchangeMechanic(mechanic):p.AscendMechanic(p.LastRewardMoment.Item.id,true);C(ok,"real core/exchange/ascend action commits");
     GameAudio.Calls.Clear();ui.PrepareRewardMoment();ui.DrawRewardMoment();C(CollectionModelPreview.Shown.id==p.LastRewardMoment.Item.id&&CollectionModelPreview.Slot==(int)p.LastRewardMoment.Item.slot,"safe result renderer receives actual committed item and highlighted slot");C(GameAudio.Calls.Single()==(action==0?SoundCue.Victory:action==1?SoundCue.Loot:SoundCue.LevelUp),"actual core/exchange/ascend moment audio is graded");
    }
    ui.panel=Panel.Inventory;ui.PrepareRewardMoment();C(ui.rewardMoment==null,"leaving source panel clears transient reveal");
   }

   Console.WriteLine("PASS "+n+" actual mobile result geometry and reward-moment input/model/resource/audio policy assertions; managed rendering/audio boundaries");
  }
 }
}
