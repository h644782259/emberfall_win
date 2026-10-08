
using System;using System.Collections.Generic;using System.Linq;using UnityEngine;
namespace UnityEngine {
 public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 public struct Vector3{public float x,y,z;public static Vector3 up=>new Vector3();public static Vector3 operator+(Vector3 a,Vector3 b)=>a;public static Vector3 operator*(Vector3 a,float b)=>a;}
 public struct Rect{public float x,y,width,height;public float xMax=>x+width;public float yMax=>y+height;public Vector2 center=>new Vector2(x+width/2,y+height/2);public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}}
 public struct Color{public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color white=>new Color(1,1,1);}
 public enum TextAnchor{MiddleCenter}public enum FontStyle{Bold}public class Font{}
 public class RectOffset{public RectOffset(int a,int b,int c,int d){}}public class GUIContent{public string text;public GUIContent(string s){text=s;}}public enum TextClipping{Clip,Overflow}public class GUIStyleState{public Color textColor;}public class GUIStyle{public GUIStyle(){}public GUIStyle(GUIStyle s){}public RectOffset padding;public Vector2 CalcSize(GUIContent c)=>new Vector2(c.text.Length*fontSize*.6f,fontSize+3);public float CalcHeight(GUIContent c,float width)=>(float)Math.Ceiling(CalcSize(c).x/width)*(fontSize+3);public bool wordWrap;public TextClipping clipping;public TextAnchor alignment;public int fontSize;public FontStyle fontStyle;public Font font;public GUIStyleState normal=new GUIStyleState();}
 public class Skin{public GUIStyle label=new GUIStyle();}public class Texture2D{public static Texture2D whiteTexture=new Texture2D();}
 public static class GUI{public struct Draw{public Rect Rect;public string Text;public Color Color;public int Font;public float EffectiveAlpha;public bool Wrap;public TextClipping Clipping;}public static List<Draw> Fills=new List<Draw>();public static List<Draw> Draws=new List<Draw>();public static Skin skin=new Skin();public static Color color;public static void DrawTexture(Rect r,Texture2D t){Fills.Add(new Draw{Rect=r,Color=color});}public static void Label(Rect r,GUIContent c,GUIStyle style)=>Label(r,c.text,style);public static void Label(Rect r,string text,GUIStyle style){Draws.Add(new Draw{Rect=r,Text=text,Color=style.normal.textColor,Font=style.fontSize,EffectiveAlpha=style.normal.textColor.a*color.a,Wrap=style.wordWrap,Clipping=style.clipping});}}
 public class Camera{public static Camera main;public float nearClipPlane;public Vector3 WorldToScreenPoint(Vector3 p)=>p;}public static class Time{public static float unscaledTime;}
 public static class Mathf{public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public const float PI=(float)Math.PI;public static float Sin(float x)=>(float)Math.Sin(x);public static float Cos(float x)=>(float)Math.Cos(x);public static int RoundToInt(float x)=>(int)Math.Round(x);public static float Clamp(float v,float a,float b)=>Math.Max(a,Math.Min(v,b));}
 public class Transform{public Vector3 position;}
}
namespace Emberfall {
 public static class EffectPreferences{public static float TouchVisualScale=1,TouchOpacity=1;}public static class GameFont{public static Font Shared=new Font();}
 public class SkillChargeController{public bool IsCharging;public int SkillIndex;public float Progress=.5f;}public class EnemyController{public Transform transform=new Transform();public string DisplayName;}
 public class PlayerController{
 public int CombatEpoch=1;public float Energy=100;public float Cooldown;public float SkillCooldownRemaining(int skill)=>Cooldown;public SkillChargeController Charge=new SkillChargeController();public CombatOpportunityState SkillWindow;public int LastSkill;public CombatOpportunityState SkillOpportunityWindow(int skill){LastSkill=skill;return skill==3||skill==8?default:SkillWindow;}
 public float Health=50,MaxHealth=100,DodgeCooldown;public bool IsJumping;public EnemyController MobilePinnedTarget;public CombatOpportunityState Counter,Combo;public int LegacyQueries;public string Reason="";
 public CombatOpportunityState BasicOpportunityWindow(bool mastery=false)=>mastery?Combo:Counter;
 // Reproduces the old actor contract: readiness ignores absent/remote/occluded targets.
 public CombatOpportunityState BasicOpportunity(){LegacyQueries++;return new CombatOpportunityState(CombatOpportunityKind.Counter,1.2f);}
 public string MobilePinnedActionReason(int skill)=>Reason;public T GetComponent<T>()where T:class=>Charge as T;
 }
 public enum HeroClass{Arcanist}public static class GameBalance{public static bool IsPassive(int skill)=>skill==3||skill==8;public static float SkillEnergyCost(HeroClass c,int skill)=>10;}public class Profile{public int potions=3;public int[] skillRanks={1,1,1,1,1,1,1,1,1,1};public HeroClass heroClass;}public class Progression{public Profile Profile=new Profile();}public class GameSession{public bool InputBlocked;public bool ChallengeRun,InDungeon;public int HealingCharges;public PlayerController Player=new PlayerController();public Progression Progression=new Progression();public string Failure="";public string ControlFailure(string key)=>key=="attack"||key.StartsWith("skill")?Failure:"";public void ReportControlFailure(string key,string reason){}}
 public sealed partial class MobileControls{
 GameSession session=new GameSession();Rect PotionVisualRect(){float size=36*EffectPreferences.TouchVisualScale;return new Rect(Potion.center.x-size/2,Potion.center.y-size/2,size,size);}Rect Potion=new Rect(23,82,54,54),Dodge=new Rect(385,233,62,62),Attack=new Rect(Layout.Attack.X,Layout.Attack.Y,Layout.Attack.Width,Layout.Attack.Height);public static MobileControlLayout Layout=new MobileControlLayout(568,320,163);Vector2 ToUI(Vector2 p)=>p;void Circle(Rect r,Color c,string text){}
 static int n;static void C(bool b,string s){n++;if(!b)throw new Exception(s);}
 public static void Run(){var v=new MobileControls();var hero=v.session.Player;
 foreach(var reason in new[]{"无目标","距离不足","被遮挡"})foreach(float scale in new[]{.86f,1f}){
 EffectPreferences.TouchVisualScale=scale;hero.Counter=new CombatOpportunityState(CombatOpportunityKind.Counter,1.2f,blockReason:reason,duration:3);hero.Reason=reason=="无目标"?"":reason;v.session.Failure=reason;hero.Combo=new CombatOpportunityState(CombatOpportunityKind.MasteryCombo,4.3f,blockReason:reason,duration:6);GUI.Draws.Clear();v.DrawAvailability();
 var clocks=GUI.Draws.Where(d=>d.Text=="1.2").ToArray();C(clocks.Length==1,"one authoritative counter clock for blocked target");C(clocks[0].Color.r<.7f&&clocks[0].Color.g<.7f,"blocked counter is muted with no white executable duplicate");C(clocks[0].Rect.y>=v.Attack.yMax,"counter clock stays outside attack control");C(GUI.Draws.Count(d=>d.Text==reason)==1&&GUI.Draws.Count(d=>d.Text=="4.3")==1,"one rejection and separate mastery clock coexist");C(hero.LegacyQueries==0,"legacy counter readiness is never queried");}
 hero.Combo=default;hero.Counter=new CombatOpportunityState(CombatOpportunityKind.Counter,.5f,duration:3);hero.Reason="";v.session.Failure="";GUI.Draws.Clear();v.DrawAvailability();var ready=GUI.Draws.Where(d=>d.Text=="0.5").ToArray();C(ready.Length==1&&ready[0].Color.g>.7f,"legal target gives one bright clock");hero.Counter=default;GUI.Draws.Clear();v.DrawAvailability();C(!GUI.Draws.Any(d=>d.Text=="反"),"expiry removes every counter seal");hero.MobilePinnedTarget=new EnemyController{DisplayName=new string('目',200)};hero.MobilePinnedTarget.transform.position=new Vector3{x=500,y=180,z=2};Camera.main=new Camera{nearClipPlane=.1f};GUI.Draws.Clear();v.DrawAvailability();var name=GUI.Draws.Single(d=>d.Text.Contains(hero.MobilePinnedTarget.DisplayName));C(name.Wrap&&name.Clipping==TextClipping.Overflow&&name.Rect.x>=8&&name.Rect.xMax<=MobileControls.Layout.Width-8&&name.Rect.y>=80&&name.Rect.yMax<=MobileControls.Layout.Height-8&&name.Rect.height>20,"long pinned identity has measured wrapped bounds");Camera.main=null;Console.WriteLine("PASS "+n+" actual mobile basic draw assertions");}
 }
}

namespace Emberfall {
 public sealed partial class GameUI {int mobileSkillPage;
 internal GameSession session=new GameSession();float TouchRatio=1;Color gold=new Color(1,.8f,.2f),jade=new Color(.2f,.8f,.5f);
 Rect TouchRect(MobileControlLayout.Area a)=>new Rect(a.X*TouchRatio,a.Y*TouchRatio,a.Width*TouchRatio,a.Height*TouchRatio);
 int TouchFont(float s)=>Mathf.RoundToInt(s*TouchRatio);
 void Fill(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);}void Bar(Rect r,float f,Color c){}
 void Text(Rect r,string t,int font,Color c,bool bold,bool wrap,TextAnchor anchor){var s=new GUIStyle();s.fontSize=font;s.normal.textColor=c;GUI.Label(r,t,s);}
 static void C(bool value,string why){if(!value)throw new Exception(why);}
 public static void Run(){var view=new GameUI();var hero=view.session.Player;var a=MobileControls.Layout.Skills[1];var slot=view.TouchRect(a);
 foreach(float unit in new[]{1f,1.5f,2f})foreach(float compact in new[]{.86f,1f}) {
 view.TouchRatio=unit;EffectPreferences.TouchVisualScale=compact;slot=view.TouchRect(a);slot=new Rect(slot.center.x-slot.width*compact/2,slot.center.y-slot.height*compact/2,slot.width*compact,slot.height*compact);
 foreach(string reason in new[]{"","无目标","目标被遮挡","距离不足","缺能"}) {
 hero.SkillWindow=new CombatOpportunityState(CombatOpportunityKind.Shatter,1.25f,blockReason:reason,duration:4);hero.Reason=reason;hero.Energy=reason=="缺能"?0:100;GUI.Draws.Clear();view.DrawMobileSkillAvailability(slot,1);
 C(GUI.Draws.Count(x=>x.Text=="碎")==1&&GUI.Draws.Count(x=>x.Text=="1.3")==1,"one outer seal and true clock without inner duplicate");
 C(GUI.Draws.Where(x=>x.Text=="碎"||x.Text=="1.3").All(x=>x.Rect.yMax<=slot.y||x.Rect.y>=slot.yMax),"opportunity only outside button");
 C(reason.Length==0?GUI.Draws.Count==2:GUI.Draws.Count==3,"inner button has only one unavailable reason");
 C(GUI.Draws.All(x=>x.Text.Sum(c=>c>127?1.1f:.65f)*x.Font+2*unit<=x.Rect.width),"minimum compact captions fit scaled width");
 }
 }
 view.TouchRatio=1;hero.Reason="";hero.Energy=100;hero.SkillWindow=default;GUI.Draws.Clear();view.DrawMobileSkillAvailability(slot,1);C(GUI.Draws.Count==0,"expired opportunity clears outer and inner captions");
 foreach(string reason in new[]{"目标被遮挡","距离不足"}){view.session.Failure=reason;hero.Reason="无目标";GUI.Draws.Clear();view.DrawMobileSkillAvailability(slot,1);C(GUI.Draws.Count==1&&GUI.Draws[0].Text==MobileCombatPresentation.SkillRejectionCaption(reason),"explicit rejection has single caption priority");}
 view.session.Failure="";hero.Reason="";hero.Cooldown=1.5f;GUI.Draws.Clear();view.DrawMobileSkillAvailability(slot,1);C(GUI.Draws.Count==1&&GUI.Draws[0].Text=="1.5","cooldown has one true remaining caption");hero.Cooldown=0;hero.Charge.IsCharging=true;hero.Charge.SkillIndex=1;GUI.Draws.Clear();view.DrawMobileSkillAvailability(slot,1);C(GUI.Draws.Count==1&&GUI.Draws[0].Text=="蓄力","charge stays charging");hero.Charge.IsCharging=false;
 hero.SkillWindow=new CombatOpportunityState(CombatOpportunityKind.EmpoweredContract,7.5f,blockReason:"无目标",duration:16);GUI.Draws.Clear();view.DrawMobileSkillAvailability(slot,4);C(hero.LastSkill==4&&GUI.Draws.Count(x=>x.Text=="7.5")==1&&GUI.Draws.Any(x=>x.Text=="无目标"),"contract retains blocked true clock on actual skill");
 GUI.Draws.Clear();view.DrawMobileSkillAvailability(slot,3);C(GUI.Draws.Count==0,"passive retains original identity");
 var pauseView=new GameUI();var pauseHero=pauseView.session.Player;Time.unscaledTime=10;
 pauseHero.SkillWindow=new CombatOpportunityState(CombatOpportunityKind.Shatter,2,duration:4);pauseView.DrawMobileSkillAvailability(slot,1);
 var until=typeof(MobileOpportunityMeter).GetField("emphasisUntil",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
 float granted=(float)until.GetValue(pauseView.mobileOpportunityMeters[1]);
 pauseView.session.InputBlocked=true;pauseHero.SkillWindow=default;Time.unscaledTime=11;GUI.Draws.Clear();pauseView.DrawMobileSkillAvailability(slot,1);C(!GUI.Draws.Any(x=>x.Text=="碎"),"paused HUD hides opportunity clocks");
 pauseView.session.InputBlocked=false;pauseHero.SkillWindow=new CombatOpportunityState(CombatOpportunityKind.Shatter,2,duration:4);pauseView.DrawMobileSkillAvailability(slot,1);C((float)until.GetValue(pauseView.mobileOpportunityMeters[1])==granted,"pause resume same window never replays acquisition emphasis");
 Console.WriteLine("PASS 30 scaled skill-slot scenarios plus priority/charge/expiry/identity/pause regressions");
 }
 }
}
class Program {
 static void C(bool b,string s){if(!b)throw new Exception(s);}
 static void Main(){Emberfall.GameUI.Run();Emberfall.MobileControls.Run();
 var meter=new Emberfall.MobileOpportunityMeter();var box=new Rect(0,0,48,14);object owner=new object();Time.unscaledTime=0;
 var state=new Emberfall.CombatOpportunityState(Emberfall.CombatOpportunityKind.Counter,1.5f,duration:3);GUI.Fills.Clear();meter.Draw(box,state,owner,1,1,1);C(GUI.Fills.Count==26,"new opportunity receives one entrance emphasis");Time.unscaledTime=1;
 GUI.Fills.Clear();meter.Draw(box,state,owner,1,1,1);C(GUI.Fills.Count==25,"persistent window never restarts emphasis");C(GUI.Fills.Skip(1).Count(x=>x.Color.g>.7f)==12,"arc uses actual total grant duration");
 state=new Emberfall.CombatOpportunityState(Emberfall.CombatOpportunityKind.EmpoweredContract,8,duration:16);GUI.Fills.Clear();meter.Draw(box,state,owner,1,1,1);C(GUI.Fills.Skip(1).Take(24).Count(x=>x.Color.g>.7f)==12,"different sixteen second window uses own denominator");C(state.Blocked("缺能").Duration==16,"blocking preserves grant duration");
 Time.unscaledTime=2;meter.Draw(box,default,owner,1,1,1);GUI.Fills.Clear();meter.Draw(box,state,owner,1,1,1);C(GUI.Fills.Count==26,"new window after expiry emphasizes once");
 GUI.Draws.Clear();meter.Draw(box,state,owner,1,1,.5f);C(GUI.Draws.All(x=>Math.Abs(x.EffectiveAlpha-.5f)<.0001f),"outer clock opacity applied once");
 foreach(var dims in new[]{new[]{568f,320f,163f},new[]{844f,390f,163f},new[]{1024f,768f,163f},new[]{2272f,1280f,326f}})foreach(int preset in new[]{-1,0,1}) foreach(int page in new[]{0,1}) {
 var l=new Emberfall.MobileControlLayout(dims[0],dims[1],dims[2],preset);var visible=page==0?new[]{0,1,2,4,9}:new[]{5,6,7,9};var hints=visible.Select(skill=>l.SkillOpportunities[skill]).Concat(new[]{l.CounterOpportunity,l.ComboOpportunity}).ToArray();
 foreach(var h in hints){C(h.X>=0&&h.Y>=0&&h.X+h.Width<=l.Width&&h.Y+h.Height<=l.Height,"all outer hints stay in safe-area coordinates");foreach(var slot in l.Skills)C(!h.Overlaps(slot),"all ten slots unobstructed");foreach(var control in new[]{l.Attack,l.Dodge,l.Jump,l.Potion,l.Interact,l.Menu,l.Inventory,l.SkillsMenu})C(!h.Overlaps(control),"outer hints never cover action controls: "+dims[0]+"/"+dims[1]+" preset "+preset+" hint "+h.X+","+h.Y+" control "+control.X+","+control.Y);}
 for(int i=0;i<hints.Length;i++)for(int j=i+1;j<hints.Length;j++)C(!hints[i].Overlaps(hints[j]),"simultaneous outer windows never overlap: "+i+"/"+j+" dims "+dims[0]+"x"+dims[1]+" preset "+preset);
 }
 Console.WriteLine("PASS actual meter arc/once-emphasis/owner-state and 12 layouts, 10 slots + 2 basic windows; managed GUI recorder, not Unity visual acceptance");
 }
}
