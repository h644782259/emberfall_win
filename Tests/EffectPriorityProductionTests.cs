using System;using System.Linq;using UnityEngine;using Emberfall;
namespace UnityEngine {public class Camera:Component{public static Camera main;}public partial struct Color{public static Color white=>new Color(1,1,1);public static Color Lerp(Color a,Color b,float t)=>new Color(a.r+(b.r-a.r)*t,a.g+(b.g-a.g)*t,a.b+(b.b-a.b)*t,a.a+(b.a-a.a)*t);}public static partial class Mathf{public static float Sqrt(float a)=>(float)Math.Sqrt(a);public static int RoundToInt(float a)=>(int)Math.Round(a);}}
public static class EffectPriorityProductionTests{
 static int n;static void Check(bool b,string m){n++;if(!b)throw new Exception(m);}static GameObject[] Live()=>GameObject.All.Where(o=>!o.Destroyed&&o.activeInHierarchy).ToArray();
 static PlayerController Reset(bool reduced){foreach(var g in GameObject.All.ToArray())UnityEngine.Object.Destroy(g);GameObject.All.Clear();Check(CombatVisualLease.Active==0,"retirement releases global leases");EffectPreferences.ReducedEffects=reduced;Application.isMobilePlatform=true;var h=new GameObject("hero").AddComponent<PlayerController>();GameSession.Instance=new GameSession{Player=h,HasStarted=true};return h;}
 static GameObject Fill(CombatVisualPriority p,int cap){GameObject first=null;for(int i=0;i<cap;i++){var o=new GameObject("existing "+p);if(first==null)first=o;Check(CombatVisualLease.Attach(o,p)!=null,"initial priority pool fills");}return first;}
 public static void Main(){foreach(bool reduced in new[]{false,true}){int cap=reduced?12:20;var h=Reset(reduced);Fill(CombatVisualPriority.SustainedBackground,cap);CombatFx.Slash(Vector3.zero,Vector3.forward,2,new Color(1,1,1));Check(!Live().Any(o=>o.name=="Filled Crescent effect"),"decorative Slash cannot displace sustained background");Check(CombatVisualLease.Active==cap,"rejected ornament cannot expand global cap");
 h=Reset(reduced);var first=Fill(CombatVisualPriority.Decoration,cap);HitFeedback.Spawn(Vector3.zero,Vector3.forward,1,priority:CombatVisualPriority.RealContact);Check(!first.activeInHierarchy&&Live().Any(o=>o.GetComponent<HitFeedback>()!=null)&&CombatVisualLease.Active==cap,"real contact evicts decoration under the global cap");
 h=Reset(reduced);Fill(CombatVisualPriority.RealContact,cap);FilledSkillVfx.Impact(h,Vector3.zero,3,FilledVfxKind.Fire,new Color(1,1,1),CombatVisualPriority.Finale);Time.deltaTime=.15f;foreach(var fx in Live().Where(o=>o.GetComponent<FilledSkillVfx>()!=null).ToArray())fx.Call("Update");foreach(var label in new[]{"Primary Fire","Contact flash","Finale short tail"})Check(Live().Any(o=>o.name.EndsWith(label)),label=="Finale short tail"?"element finale retains its minimum short tail":"element finale retains primary/contact");Check(CombatVisualLease.Active==cap,"finale preemption remains bounded");

 // Actual secondary emitter, dense cashout shape, same global lease used by RecordBurnCash.
 for(int i=0;i<40;i++)typeof(PlayerController).GetMethod("RecordBurnCash",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(h,new object[]{91,h.CombatEpoch,new Vector3(i*.01f,0,0)});
 Check(Live().Any(o=>o.name.EndsWith("Primary Fire")),"dense cash contacts cannot evict their own finale main");
 Check(Live().Any(o=>o.name.EndsWith("Finale short tail"))&&CombatVisualLease.Active<=cap,"cash contacts retain essential tail under bounded budget");
 for(int i=0;i<cap+2;i++)FilledSkillVfx.ArrowRain(h,Vector3.zero,3,new Color(1,1,1),true,CombatVisualPriority.Finale);Check(Live().Any(o=>o.name.EndsWith("Primary falling arrow"))&&Live().Any(o=>o.name.EndsWith("Arrow landing contact"))&&Live().Any(o=>o.name.EndsWith("Short embedded arrow")),"new arrow finale retains main contact and short tail when finales fill budget");Check(CombatVisualLease.Active==cap,"same-priority latest finale cannot grow pool");
 }

 foreach(bool confirmed in new[]{false,true})foreach(string exit in new[]{"result","epoch","owner","title","death","skip"}){
 var h=Reset(true);FilledSkillVfx.Impact(h,Vector3.zero,3,FilledVfxKind.Fire,new Color(1,1,1),CombatVisualPriority.Finale,91);
 if(confirmed)FilledSkillVfx.ConfirmFinale(h,91);
 GameSession.Instance.ModeFinished=true;Time.deltaTime=0;Time.unscaledDeltaTime=.15f;
 if(exit=="skip")FilledSkillVfx.SkipFinales(GameSession.Instance);if(exit=="epoch")h.CombatEpoch++;if(exit=="owner")GameSession.Instance.Player=null;if(exit=="title")GameSession.Instance.HasStarted=false;if(exit=="death")h.IsDead=true;
 foreach(var g in Live().Where(o=>o.GetComponent<FilledSkillVfx>()!=null).ToArray())g.Call("Update");
 Check(Live().Any(o=>o.GetComponent<FilledSkillVfx>()!=null)==(confirmed&&exit=="result"),"only confirmed finale survives result; skips and owner exits retire immediately");
 for(int f=0;f<6;f++)foreach(var g in Live().Where(o=>o.GetComponent<FilledSkillVfx>()!=null).ToArray())g.Call("Update");
 Check(CombatVisualLease.Active==0,"terminal visual tail expires with scaled time zero");
 }
 var hero=Reset(false);Application.isMobilePlatform=false;for(int i=0;i<30;i++)HitFeedback.Spawn(Vector3.zero,Vector3.forward,1,priority:CombatVisualPriority.RealContact);Check(Live().Count(o=>o.GetComponent<HitFeedback>()!=null)==24,"hit feedback retains independent 24 cap");Time.deltaTime=1;foreach(var g in Live())if(g.GetComponent<HitFeedback>()!=null)g.Call("Update");Check(CombatVisualLease.Active==0,"expired real contacts release global and local budget");Reset(true);Console.WriteLine("PASS: "+n+" actual priority emission/global cap/contact/finale checks; engine doubles, not GPU");}
}
