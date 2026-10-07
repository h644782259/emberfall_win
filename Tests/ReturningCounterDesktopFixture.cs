// Actual desktop workshop branch is extracted unchanged. Only GUI delivery is substituted.
using System;using System.IO;using UnityEngine;
namespace UnityEngine{
 public struct Rect{public float x,y,width,height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}public bool Contains(Vector2 p)=>false;}
 public struct Vector2{public float x,y;}
}
namespace Emberfall{public sealed class GameUI{
 class Context{public ProgressionService Progression;public bool IsInCamp=true;}Context session;Color card,pale,muted,gold,jade;Vector2 Mouse;string tooltip,click;int variantButtons;
 void Text(Rect r,string s,int size,Color c,bool bold=false,bool wrap=false){}void Fill(Rect r,Color c){}void OpenReforgeSurface(string id){}
 bool NavigationButton(Rect r,string label,Color c,bool enabled=true,string hint=null)=>Button(r,label,c,enabled,hint);
 bool Button(Rect r,string label,Color c,bool enabled=true,string hint=null,bool emphasis=false){if(label.StartsWith("变体"))variantButtons++;if(enabled&&label==click){click=null;return true;}return false;}
 void Feedback(bool ok,string s){if(!ok)throw new Exception(session.Progression.LastError);}
 public static string Verify(string root){int n=0;Action<bool,string> check=(ok,msg)=>{n++;if(!ok)throw new Exception(msg);};var p=new ProgressionService(Path.Combine(root,"counter-desktop"));check(p.CreateNewSlot(HeroClass.Vanguard),"desktop create sword save");p.Profile.level=20;p.Profile.mechanicMaterials=8;p.Save();var item=p.CreateMechanicItem(EquipmentMechanic.ReturningBlade);check(p.CollectLoot(item)&&p.Equip(item.id),"desktop equip sword mechanism");var ui=new GameUI{session=new Context{Progression=p}};
 ui.click="变体 · 4";ui.DrawMechanismWorkshop();check(p.Equipped(ItemSlot.Weapon).mechanicVariantUnlocked&&p.Equipped(ItemSlot.Weapon).mechanicVariant==1&&p.Profile.mechanicMaterials==4,"desktop actual button unlocksB");
 ui.click="变体 B";ui.DrawMechanismWorkshop();check(p.Equipped(ItemSlot.Weapon).mechanicVariant==0&&p.Profile.mechanicMaterials==4,"desktop actual button free B to A");
 ui.click="变体 A";ui.DrawMechanismWorkshop();check(p.Equipped(ItemSlot.Weapon).mechanicVariant==1&&p.Profile.mechanicMaterials==4,"desktop actual button free A to B");
 ui.session.IsInCamp=false;ui.click="变体 B";ui.DrawMechanismWorkshop();check(p.Equipped(ItemSlot.Weapon).mechanicVariant==1,"desktop outsidecamp disabled");check(ui.variantButtons==4,"desktop exposes both locked and unlocked controls");return "PASS "+n+" actual desktop workshop button unlock and free variant roundtrip assertions";}
 // GENERATED_BRANCH
}}
