using System;using System.Linq;using System.Reflection;using Emberfall;using UnityEngine;
class Program{
static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
static Transform Find(EnemyController e,string name)=>e.GetComponentsInChildren<Transform>(true).First(x=>x.name==name);
static void Sync(EnemyController e)=>GameObject.Call(e.GetComponent<EnemyStatusVisual>(),"LateUpdate");
static void Main(){
foreach(var kind in new[]{EnemyKind.Slime,EnemyKind.Goblin,EnemyKind.Wisp,EnemyKind.Guardian})foreach(bool large in new[]{false,true}){
 if(large&&kind!=EnemyKind.Guardian)continue;
 var root=new GameObject("actual enemy");var e=root.AddComponent<EnemyController>();e.Kind=kind;e.IsBoss=large;
 var m=large?CombatModel.LargeExpedition(root.transform):CombatModel.Enemy(root.transform,kind,false);
 EnemyStatusVisual.Attach(e);GameSession.Instance.Player.AimTarget=e;e.StatusEffects.HasFrostMark=e.StatusEffects.IsMarked=e.StatusEffects.IsFrozen=true;e.StatusEffects.Notify();
 var frost=Find(e,"Frost mark diamond");var weak=Find(e,"Vulnerability slash");string expected=large?"Suspended astrolabe chassis":kind==EnemyKind.Slime?"Slime Body":kind==EnemyKind.Wisp?"Spirit Core":"Breastplate";
 Check(frost.parent.name==expected&&weak.parent==frost.parent,"status symbol follows actual body parent");
 var body=frost.parent;Check(frost.localPosition.x<-.65f&&weak.localPosition.x>.65f&&frost.localPosition.y<0,"actual body flank markers remain below face and outside center");
 Vector3 frostLocal=frost.localPosition;var origin=frost.position;Transform foot=large?null:Find(e,"Frozen ankle crystal");var ground=foot==null?Vector3.zero:foot.position;
 if(!large){e.StatusEffects.AirborneHeight=2;Time.frameCount++;m.Animate(.2f,0,false);Sync(e);Check((frost.position-origin).magnitude>1.5f,"actual Animate airborne moves body symbol");Check((foot.position-ground).magnitude<.0001f&&foot.parent==root.transform,"ground crystals remain at feet when body airborne");}
 else{body.localRotation=Quaternion.Euler(12,20,8);body.localPosition+=Vector3.up*.4f;Sync(e);Check((frost.position-origin).magnitude>.1f,"large authored chassis carries status without core attachment");Check(!e.GetComponentsInChildren<Transform>(true).Any(x=>x.name=="Frozen ankle crystal"),"boss never displays hard-freeze crystal");}
 if(kind==EnemyKind.Goblin&&!large){e.StatusEffects.AirborneHeight=0;Time.frameCount++;m.Animate(0,0,false);m.TryAnimateKnockdown(1,false,.1f);Sync(e);Check((frost.position-body.TransformPoint(frostLocal)).magnitude<.0001f,"actual knockdown preserves body-local attachment");Check((foot.position-ground).magnitude<.0001f,"knockdown does not rotate ground cue");}
 int objects=e.GetComponentsInChildren<Transform>(true).Length;e.StatusEffects.HasFrostMark=false;e.StatusEffects.Notify();Check(!frost.gameObject.activeSelf,"consumption hides body symbol synchronously");e.StatusEffects.HasFrostMark=true;e.StatusEffects.Notify();Check(frost.gameObject.activeSelf&&objects==e.GetComponentsInChildren<Transform>(true).Length,"same frame refresh reuses body symbols");e.IsDead=true;Sync(e);Check(!frost.gameObject.activeSelf&&!weak.gameObject.activeSelf,"dead owner clears body symbols");
}
Console.WriteLine("PASS "+checks+" actual slime/goblin/wisp/guardian/large-boss factory, airborne and knockdown status attachment assertions; managed TRS not engine rendering");}}
