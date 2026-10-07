using System;using System.IO;using System.Collections.Generic;using System.Linq;using Emberfall;using UnityEngine;
namespace UnityEngine{
 public struct Vector2{public float x,y;public static Vector2 zero=>new Vector2();}
 public struct Rect{public float x,y,width,height;public float xMax=>x+width;public float yMax=>y+height;public Rect(float a,float b,float w,float h){x=a;y=b;width=w;height=h;}}
 public static class Mathf{public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int RoundToInt(float x)=>(int)Math.Round(x);public static float Clamp(float x,float a,float b)=>Math.Min(b,Math.Max(a,x));public static int Clamp(int x,int a,int b)=>Math.Min(b,Math.Max(a,x));}
 public static class Time{public static float unscaledTime;}
 public static class GUI{public static bool enabled=true;}
}
namespace Emberfall{
 public static class UIIconAtlas{public static int Reward(int kind)=>kind;}
 public static class MobileControls{public static bool Active;}
 public static class EffectPreferences{public static bool ReducedEffects;}
 public enum SoundCue{UI,Cast,Loot,LevelUp,Victory}public static class GameAudio{public static void Play(SoundCue s){}}
 public class GameSession{public ProgressionService Progression;public void LogSystem(string s){}public void SetUIBlocking(bool b){}public void SetPaused(bool b){}}
 public sealed partial class GameUI{
  enum Panel{None,Chests,Camp}Panel panel=Panel.Chests;GameSession session;float width,height,TouchRatio=1;Color gold,jade,pale,muted;
  bool chestDetails,chestOpening,rewardSoundPlayed;int revealedChest=-1;string chestRevealResult,chestReceiptId,mobileChestError;float chestRevealedAt;Rect chestRevealOrigin;Vector2 desktopChestResultScroll,mobileChestScroll,mobileChestArtScroll;
  readonly List<(string caption,Rect bounds)> buttons=new List<(string,Rect)>();string click;bool clicked;int scrollDepth;
  bool Button(Rect r,string s,Color c,bool enabled=true,string reason=null,bool highlight=false){buttons.Add((s,r));if(!enabled||clicked||click!=s)return false;clicked=true;return true;}
  // Graphics boundary for the existing 483b47b button-role API; click semantics stay in Button.
  enum ButtonRole { Action, Primary, Navigation, Danger, Tab, SelectedTab, Toggle, ActiveToggle, Row, SelectedRow }
  bool DrawButton(Rect r,string s,ButtonRole role,bool enabled=true,string hint=null,int fontSize=0,string controlName=null)=>Button(r,s,gold,enabled,hint);
  bool NavigationButton(Rect r,string s,Color accent,bool enabled=true,string hint=null,bool primary=false)=>DrawButton(r,s,ButtonRole.Navigation,enabled,hint);
  bool PrimaryButton(Rect r,string s,Color accent,bool enabled=true,string hint=null,bool primary=false)=>DrawButton(r,s,ButtonRole.Primary,enabled,hint);
  readonly List<int> resourceIcons=new List<int>();readonly List<string> resourceNumbers=new List<string>();int legacyDraws;
  void DrawIcon(Rect r,int icon,Color c){resourceIcons.Add(icon);}void DrawChestGoldReward(Rect r,ChestReward reward,Color c){legacyDraws++;}bool DrawChestRewardModel(Rect r,ChestReward reward)=>false;
  void Fill(Rect r,Color c){}void Border(Rect r,Color c){}void Text(Rect r,string s,int size,Color c,bool bold=false,bool wrap=false){resourceNumbers.Add(s);}
  void DrawRewardChest(Rect r,bool opened,float alpha,float progress){}void DrawDesktopChestRules(Rect r){}void DrawDesktopChestResult(Rect r,ChestReward reward,Color c){}void DrawChestRevealTransition(Rect r,ChestReward reward,Rect target){}
  void DrawMobileChestDetails(MobilePanelLayout l){}void DrawMobileChestResult(MobilePanelLayout l,ChestReward r,Color c,bool complete){}
  bool DrawMobilePanelChrome(MobilePanelLayout l,string title,string subtitle,bool close=true,bool menu=true)=>false;
  MobilePanelLayout MobilePanelGeometry()=>new MobilePanelLayout(width/TouchRatio,height/TouchRatio);
  Rect MobilePanelRect(MobilePanelLayout.Area a)=>TouchRect(a.X,a.Y,a.Width,a.Height);
  Rect TouchRect(float x,float y,float w,float h)=>new Rect(x*TouchRatio,y*TouchRatio,w*TouchRatio,h*TouchRatio);
  float MeasureMobileParagraph(string s,float w,int font)=>string.IsNullOrEmpty(s)?0:(float)Math.Ceiling(s.Length*font/Math.Max(1,w))*(font+2);
  float DrawMobileParagraph(float x,float y,float w,string s,int font,Color color)=>MeasureMobileParagraph(s,w,font);
  Vector2 BeginTouchScroll(string id,Rect r,Vector2 position,Rect content){scrollDepth++;return position;}void EndTouchScroll(){scrollDepth--;}
  void CancelMobileScroll(){}void BlockUITransition(){}void Feedback(bool ok,string text){}void ClosePanel(){panel=Panel.Camp;}bool AcceptChestForTrial(){throw new Exception("unexpected trial request");}
  void Frame(string action=null){click=action;clicked=false;buttons.Clear();DrawChests();if(scrollDepth!=0)throw new Exception("actual page left a scroll scope open");}
  static int n;static void C(bool ok,string message){n++;if(!ok)throw new Exception(message);}
  public static void Run(string root){
   foreach(bool mobile in new[]{false,true})foreach(bool reduced in new[]{false,true})foreach(float ratio in new[]{1f,2f}){
    var p=new ProgressionService(Path.Combine(root,Guid.NewGuid().ToString("N")));p.NewGame(HeroClass.Vanguard);p.PrepareDungeonChest();int events=0;p.Changed+=()=>events++;
    MobileControls.Active=mobile;EffectPreferences.ReducedEffects=reduced;Time.unscaledTime=0;
    var ui=new GameUI{session=new GameSession{Progression=p},TouchRatio=ratio,width=mobile?568*ratio:1280,height=mobile?320*ratio:720};
    ui.Frame();C(ui.buttons.Count(x=>x.caption=="开启宝箱")==1,"one actual opener, no type or part selection");C(!ui.buttons.Any(x=>x.caption=="兵装"||x.caption=="羽翼"||x.caption=="补给"),"no choice buttons");
    var opener=ui.buttons.Single(x=>x.caption=="开启宝箱");C(opener.bounds.height>=(mobile?48*ratio:42)&&opener.bounds.width>0,"opener keeps platform action height");
    string before=JsonUtility.ToJson(p.Profile,true);Directory.CreateDirectory(p.SaveFilePath+".tmp");ui.Frame("开启宝箱");Directory.Delete(p.SaveFilePath+".tmp");
    C(!ui.chestOpening&&p.Profile.pendingFashionChest&&!p.Profile.pendingChestReveal&&events==0&&JsonUtility.ToJson(p.Profile,true)==before,"actual UI failed write resets latch and grants nothing");
    ui.Frame();C(ui.buttons.Count(x=>x.caption=="继续开启")==1,"failed draw offers one continue action");ui.Frame("继续开启");C(events==1&&p.Profile.pendingChestReveal,"one click persists entire reward before reveal");
    ui.resourceIcons.Clear();ui.resourceNumbers.Clear();string visualBefore=JsonUtility.ToJson(p.Profile,true);
    ui.DrawChestCommittedReward(new Rect(0,0,240*ratio,220*ratio),p.LastChestReward,ui.gold);
    C(ui.resourceIcons.SequenceEqual(new[]{0,1,2}),"actual receipt draws coin, fragment and thread pictograms");
    C(ui.resourceNumbers.SequenceEqual(new[]{"+"+p.LastChestReward.goldDelta,"+"+p.LastChestReward.materialsDelta,"+"+p.LastChestReward.threadsDelta}),"actual resource numbers use committed deltas");
    C(JsonUtility.ToJson(p.Profile,true)==visualBefore&&events==1,"resource rendering never grants or acknowledges");
    ui.resourceIcons.Clear();ui.resourceNumbers.Clear();ui.DrawChestResourceVisuals(new Rect(0,0,240,220),new ChestReward{hasCurrencyDeltas=true});C(ui.resourceIcons.Count==0&&ui.resourceNumbers.Count==0,"zero actual capped deltas draw no invented gain");
    ui.DrawChestResourceVisuals(new Rect(0,0,240,220),new ChestReward());C(ui.legacyDraws==1,"legacy receipt without deltas keeps legacy presentation");
    int gold=p.Profile.gold,materials=p.Profile.mechanicMaterials,threads=p.Profile.fashionThreads;string saved=JsonUtility.ToJson(p.Profile,true),id=p.LastChestReward.id;ui.Frame("继续开启");ui.Frame("跳过动画");C(events==1&&JsonUtility.ToJson(p.Profile,true)==saved&&p.LastChestReward.id==id,"repeat/skip only changes presentation");
    ui.Frame("收下");C(events==2&&!p.Profile.pendingChestReveal&&p.LastChestReward.id==id&&p.Profile.gold==gold&&p.Profile.mechanicMaterials==materials&&p.Profile.fashionThreads==threads,"one normal acknowledgement retains receipt and amounts");
   }
   Console.WriteLine("PASS "+n+" actual desktop/mobile single-opener, failure/retry, skip and acknowledgement assertions; managed GUI boundaries");
  }
 }
}
