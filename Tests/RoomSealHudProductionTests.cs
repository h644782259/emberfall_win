using System;using System.Collections.Generic;using System.Linq;using UnityEngine;
namespace UnityEngine
{
 public struct Rect {public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}public float xMax=>x+width;public float yMax=>y+height;public bool Contains(Vector2 p)=>p.x>=x&&p.x<=xMax&&p.y>=y&&p.y<=yMax;}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}}
 public enum TextAnchor {MiddleCenter,MiddleLeft,UpperCenter}
 public static class Mathf {public static int RoundToInt(float v)=>(int)Math.Round(v);public static int CeilToInt(float v)=>(int)Math.Ceiling(v);public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);}
 public static class GUI {public static bool enabled=true;public static bool Button(Rect r,GUIContent c,object style)=>false;}
 public class GUIContent {public string text;public GUIContent(string s){text=s;}public static GUIContent none=new GUIContent("");}
}
namespace Emberfall
{
 public enum ExpeditionModeKind {HoldPoint,Other} public enum HoldPointState {Contested,Other}
 public class ModeStub {public float RemainingSeconds=20,ObjectiveProgress=.5f;public ExpeditionModeKind Mode;public HoldPointState HoldState;public string HoldStateLabel="占领";}
 public class Profile {public int level=5,xp,skillPoints=1;public int[] skillRanks=new[]{1};}
 public static class GameBalance {public static int XpToNext(int level)=>100;}
 public class ChapterDefinition {public string Name="章节";public static ChapterDefinition Get(int node)=>new ChapterDefinition();}
 public class GameSession
 {
  public List<object> Enemies=new List<object>();public int RoomAQueries,RoomBQueries,ChapterAQueries,ChapterBQueries;public Profile Profile=new Profile();public bool ChapterActive,ChapterOpen,Paused;public int ActiveChapterNode;public string ChapterObjectiveCompact="章节目标",ChapterObjectiveStatus="章节目标";
  public RoomChainState ChapterRun=new RoomChainState(0),RoomChainRun;public ModeStub ModeRun=new ModeStub();public int Occupied=-1,Contested=-1;public string ModeName="远征",Objective="目标";public int DungeonWave=1,TotalWaves=5,HealingCharges=3;
  public bool SpecialAdventure=true,InDungeon=true,DungeonCleared,ChallengeRun;public string ModeObjectiveStatus=>RoomObjectiveView.ProgressText;
  public bool RoomCaptureContested=>Contested>=0;public RoomSupportSnapshot Support=new RoomSupportSnapshot(true,5,RoomTargetSupport.Supplier);
  public RoomObjectivePresentation RoomObjectiveView=>RoomObjectivePresentation.Create(RoomChainRun,Occupied>=0,Contested>=0?1:0,Paused,Support);
  public ChapterSealPresentation RoomSealView(int index){if(index==0)RoomAQueries++;else RoomBQueries++;return !ChapterActive&&RoomChainRun!=null&&!RoomChainRun.Finished&&RoomChainRun.Room.Objective==RoomObjective.Purify?new ChapterSealPresentation(index,RoomChainRun.SealProgress(index),Occupied==index,Contested==index,RoomChainRun.SealComplete(index),Paused):null;}
  public ChapterSealPresentation ChapterSealView(int index){if(index==0)ChapterAQueries++;else ChapterBQueries++;return ChapterActive&&ChapterOpen?new ChapterSealPresentation(index,index+.5f,index==0,false,false,false):null;}
 }
 public sealed partial class GameUI
 {
  GameSession session=new GameSession();float TouchRatio=1;Color jade=new Color(0,1,0),gold=new Color(1,1,0),pale=new Color(1,1,1),muted=new Color(.5f,.5f,.5f);Vector2 Mouse=new Vector2(-100,-100);string tooltip;object invisibleButton;void OpenTravelMap(){}
  List<Rect> blockedRects=new List<Rect>();public struct Label {public Rect Rect;public string Value;public bool Bold;public int Font;public Color Tint;}
  List<Label> labels=new List<Label>();List<Tuple<Rect,float>> bars=new List<Tuple<Rect,float>>();List<Rect> fills=new List<Rect>();
  void Text(Rect r,string s,int size,Color c,bool bold=false,bool wrap=false,TextAnchor anchor=TextAnchor.MiddleCenter){if(c.r==0&&c.g==0&&c.b==0)return;labels.Add(new Label{Rect=r,Value=s,Bold=bold,Font=size,Tint=c});}
  void Fill(Rect r,Color c){fills.Add(r);}void Bar(Rect r,float value,Color c){bars.Add(Tuple.Create(r,value));}void Box(Rect r,Color c,bool unused){}int TouchFont(int size)=>Mathf.RoundToInt(size*TouchRatio);
  string PlatformText(string s)=>s;bool TryGrowthHudHint(out string a,out string b){a=b="";return false;}
  class MeasuredStyle {public int Size;public Vector2 CalcSize(GUIContent content)=>new Vector2(TextBudget(content.text,Size),Size+2);public float CalcHeight(GUIContent content,float width)=>(float)Math.Ceiling(TextBudget(content.text,Size)/width)*(Size+2);}
  MeasuredStyle Style(int size,bool bold,bool wrap)=>new MeasuredStyle{Size=size};
  static int n;static void Check(bool b,string why){n++;if(!b)throw new Exception(why);}
  void Clear(){session.RoomAQueries=session.RoomBQueries=session.ChapterAQueries=session.ChapterBQueries=0;labels.Clear();bars.Clear();fills.Clear();blockedRects.Clear();}
  static bool Inside(Rect a,Rect b)=>a.x>=b.x-.001f&&a.y>=b.y-.001f&&a.xMax<=b.xMax+.001f&&a.yMax<=b.yMax+.001f;
  static bool Overlap(Rect a,Rect b)=>a.x<b.xMax&&a.xMax>b.x&&a.y<b.yMax&&a.yMax>b.y;
  static void Register(RoomChainState r){for(int i=0;i<r.Room.EnemyCount;i++)r.Register(r.Room,i);}
  static float TextBudget(string s,int font){float width=0;foreach(char c in s)width+=c<128?font*.62f:font;return width;}
  public static void Run()
  {
   var v=new GameUI();
   foreach(var dims in new[]{new[]{568f,320f},new[]{1024f,768f},new[]{1366f,1024f}})foreach(float u in new[]{.86f,1f,1.2f})foreach(int occupied in new[]{0,1})
   {
    v.TouchRatio=u;var layout=new MobileControlLayout(dims[0],dims[1],163);var a=layout.AdventureStatus;var card=new Rect(a.X*u,a.Y*u,a.Width*u,a.Height*u);
    foreach(var control in layout.Skills.Concat(new[]{layout.Attack,layout.Dodge,layout.Potion,layout.Jump,layout.Interact,layout.FocusCommand,layout.RecallCommand}))Check(!a.Overlaps(control),"unchanged mode card never expands into touch controls");
    var run=new RoomChainState(0);Register(run);for(int i=0;i<6;i++){run.AdvanceSeal(0,.25f,true,true,false);run.AdvanceSeal(1,.25f,true,true,false);}v.session.RoomChainRun=run;v.session.Occupied=occupied;v.session.Contested=1-occupied;
    v.Clear();v.DrawMobileModeStatus(card);
    Check(v.session.RoomAQueries==1&&v.session.RoomBQueries==1,"room snapshots read exactly once per mobile draw event");
    Check(v.labels.Count==4&&v.bars.Count==0,"actual Purify draw contains title two seal texts and support without bars");
    foreach(var label in v.labels){Check(Inside(label.Rect,card),"compact actual draw stays inside existing mode card");Check(TextBudget(label.Value,label.Font)<=label.Rect.width,"compact text stays within conservative glyph budget");}
    foreach(var bar in v.bars)Check(Inside(bar.Item1,card),"compact actual draw stays inside existing mode card");
    for(int i=0;i<v.labels.Count;i++)for(int j=i+1;j<v.labels.Count;j++)Check(!Overlap(v.labels[i].Rect,v.labels[j].Rect),"title seal text and support never overlap");
    foreach(var label in v.labels)foreach(var bar in v.bars)Check(!Overlap(label.Rect,bar.Item1),"text and capture bars never overlap");
    var rows=v.labels.Where(l=>l.Value.Contains("/3秒")).ToArray();Check(rows.Length==2&&rows[occupied].Bold&&!rows[1-occupied].Bold&&rows[1-occupied].Value.Contains("争夺"),"actual rows preserve independent occupancy and contest");Check(v.bars.Count==0,"mobile text surface has no panel bars");
    v.session.Paused=true;v.session.Contested=-1;v.session.Occupied=1-occupied;v.Clear();v.DrawMobileModeStatus(card);
    Check(v.labels.Count(l=>l.Value.Contains("暂停"))==2&&v.labels.Where(l=>l.Value.Contains("/3秒")).ToArray()[1-occupied].Bold,"next event observes changed pause contest and occupancy without cached state");
    v.session.Paused=false;v.session.Contested=1-occupied;v.session.Occupied=occupied;
    v.Clear();v.Desktop();Check(v.session.RoomAQueries==1&&v.session.RoomBQueries==1,"room snapshots read exactly once at desktop site");Check(v.labels.Count(l=>l.Value.Contains("/3秒"))==2&&v.bars.Count==3,"actual desktop site draws independent rows and aggregate");foreach(var label in v.labels)Check(Inside(label.Rect,v.blockedRects[0]),"desktop labels remain in measured objective card");
    for(int i=0;i<6;i++){run.AdvanceSeal(1,.25f,true,true,false);run.AdvanceSeal(0,.25f,true,true,false);}v.Clear();v.DrawMobileModeStatus(card);Check(v.labels[0].Value=="双印完成 · 前往北门"&&v.labels.Count(l=>l.Value.Contains("完成"))==3,"completed room shows both identities and exit instruction");
    run.Fail();v.Clear();v.DrawMobileModeStatus(card);Check(!v.labels.Any(l=>l.Value.Contains("/3秒"))&&v.labels.Any(l=>l.Value=="远征失败"),"terminal event drops both rows rather than retaining previous snapshots");
   }
   foreach(int seed in new[]{1,2}){v.session.RoomChainRun=new RoomChainState(seed);Register(v.session.RoomChainRun);v.Clear();v.DrawMobileModeStatus(new Rect(0,0,236,76));Check(!v.labels.Any(l=>l.Value.Contains("/3秒"))&&v.bars.Count==0,"Hunt and Escape keep original generic mode card");}
   v.session.ChapterActive=true;v.session.ChapterOpen=true;v.TouchRatio=1;v.Clear();v.DrawMobileModeStatus(new Rect(0,0,188,76));Check(v.labels.Count==3&&v.bars.Count==0&&v.labels[1].Rect.y>v.labels[0].Rect.yMax&&v.labels[2].Rect.y>v.labels[1].Rect.yMax,"chapter seal drawing remains unchanged");Check(v.session.ChapterAQueries==1&&v.session.ChapterBQueries==1&&v.session.RoomAQueries==0,"chapter precedence reads only its own pair once");
   v.session.ChapterOpen=false;v.Clear();v.DrawMobileModeStatus(new Rect(0,0,188,76));Check(v.bars.Count==0&&v.labels.Any(l=>l.Value=="章节目标"),"null chapter snapshot switches immediately to objective text");
   Console.WriteLine("PASS: "+n+" actual room HUD Draw/rectangle/state assertions; managed UI recorder, not Unity/font screenshots");
  }
 }
}
