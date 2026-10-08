"""Actual icon/popup input and sort methods with explicit GUI/service boundaries, not device QA."""
from pathlib import Path
import tempfile,subprocess,sys,os
root=Path(__file__).resolve().parents[1]
def member(s,k):
 a=s.index(k);b=s.index('{',a)+1;n=1
 while n:n+=(s[b]=='{')-(s[b]=='}');b+=1
 return s[a:b]
grid=(root/'Assets/Scripts/UI/GameUI.InventoryGrid.cs').read_text();wear=(root/'Assets/Scripts/UI/GameUI.WearMap.cs').read_text();ui=(root/'Assets/Scripts/UI/GameUI.cs').read_text()
methods=''.join(member(grid,k) for k in ['private void PrepareInventoryPopupInput(','private void OpenInventoryPopup(','private void DrawInventoryPopup(','private bool QuietInventoryAction(','private bool InventoryPictogramAction('])+member(wear,'private void DrawBagFashion(')+member(ui,'private int CompareInventoryItems(')
fixture=r'''
using System;using System.Collections.Generic;using UnityEngine;
namespace UnityEngine{
 public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>default;public static bool operator==(Vector2 a,Vector2 b)=>a.x==b.x&&a.y==b.y;public static bool operator!=(Vector2 a,Vector2 b)=>!(a==b);public override bool Equals(object o)=>o is Vector2 v&&this==v;public override int GetHashCode()=>0;}
 public struct Rect{public float x,y,width,height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}public float xMax=>x+width;public float yMax=>y+height;public Vector2 center=>new Vector2(x+width/2,y+height/2);public bool Contains(Vector2 p)=>p.x>=x&&p.x<xMax&&p.y>=y&&p.y<yMax;}
 public class Texture2D{}public struct Color{public float r,g,b;public Color(float a,float b,float c,float d=1){r=a;g=b;this.b=c;}public static Color white=>default;}
 public enum TextAnchor{MiddleRight,MiddleCenter}public enum EventType{MouseDown,ScrollWheel,Repaint,Used}public class Event{public static Event current=new Event();public EventType type;public void Use(){type=EventType.Used;}}
 public class GUIContent{public GUIContent(string s){}public static GUIContent none=new GUIContent("");}public static class GUI{public static bool enabled=true;public static int Buttons;public static string Click,Caption;public static bool Button(Rect r,GUIContent c,object style){Buttons++;return enabled&&Caption==Click;}}
 public static class Time{public static int frameCount;public static float unscaledTime;}
 public static class Mathf{public static int RoundToInt(float f)=>(int)Math.Round(f);public static float Max(float a,float b)=>Math.Max(a,b);}
}
namespace Emberfall{
 enum ItemSlot{Weapon,Armor,Relic}enum FashionSlot{Wings,Weapon}enum Rarity{Common,Rare,Epic,Legendary}
 class ItemData{public string id,name;public int level=1,attack=1,defense,health;public bool locked;public Rarity rarity;public ItemSlot slot;public float score;}
 class FashionData{public string id,name;public FashionSlot slot;public Rarity rarity;}
 class Profile{public int level=50,potions=3,heroClass;public List<ItemData> inventory=new List<ItemData>();public List<FashionData> fashions=new List<FashionData>();}
 class Progress{public Profile Profile=new Profile();public ItemData worn;public FashionData fashion;public int Equips,Locks,FashionChanges;public ItemData Equipped(ItemSlot s)=>worn;public ItemData PreviewEquippedItem(ItemData i)=>i;public bool Equip(string id){worn=Profile.inventory.Find(i=>i.id==id);Equips++;return true;}public bool SetItemLocked(string id,bool v){Profile.inventory.Find(i=>i.id==id).locked=v;Locks++;return true;}public FashionData EquippedFashion(FashionSlot s)=>fashion!=null&&fashion.slot==s?fashion:null;public bool EquipFashion(string id){fashion=Profile.fashions.Find(f=>f.id==id);FashionChanges++;return fashion!=null;}public bool UnequipFashion(FashionSlot s){fashion=null;FashionChanges++;return true;}}
 class Player{public float Health=20,MaxHealth=100;}class Session{public Progress Progression=new Progress();public Player Player=new Player();public int Uses;public void DrinkPotion(){Uses++;Progression.Profile.potions--;Player.Health+=50;}}
 static class ProgressionService{public static string FashionName(FashionSlot s,Rarity r,int hero)=>"时装名称";}
 static class GameBalance{public static Color RarityColor(Rarity r)=>default;public static string RarityName(Rarity r)=>r.ToString();}static class MobileControls{public static bool Active=true;}static class UIIconAtlas{public static Texture2D EquipmentCardIcon(ItemSlot s)=>new Texture2D();public static Texture2D EquipmentLock(bool l)=>new Texture2D();public static Texture2D FashionCardIcon(FashionSlot s)=>new Texture2D();public static Texture2D Utility(string s)=>new Texture2D();}enum SoundCue{UI}static class GameAudio{public static void Play(SoundCue s){}}
 static class EquipmentComparisonPresentation{public static string Changes(ItemData a,ItemData b,int hero)=>"mechanic";public static string Description(ItemData a,int hero)=>"description";}
 partial class GameUI{
 Session session=new Session();Rect inventoryPopupAnchor,inventoryPopupRect;string inventoryPopupItem,selectedItem;bool inventoryComparisonOpen,inventoryPopupCompare;int inventoryPopupOpened=-1,inventoryPopupDismissed=-1;float inventoryActionUntil=-1;Vector2 inventoryComparisonScroll,mobileFashionScroll;float TouchRatio=>1;Color jade,pale,muted,card,gold;string tooltip;object invisibleButton;Vector2 Mouse;int canceled,blocked;string Click="";bool LockClick;List<string> Labels=new List<string>();
 void CancelMobileScroll(){canceled++;}void BlockUITransition(){blocked++;}void ReviewEquipment(ItemData i){}bool IsEquipped(ItemData i)=>session.Progression.worn==i;float EquipmentPreviewScore(ItemData i)=>i.score;
 bool QuietAction(Rect r,string s,bool enabled=true,string reason=null,bool selected=false){Labels.Add(s);return GUI.enabled&&enabled&&s==Click;}bool DrawInventoryLock(Rect r,bool locked)=>GUI.enabled&&LockClick;
 void MobileInventoryResult(bool ok,string s){}void MobileFashionResult(bool ok,string s){}void Fill(Rect r,Color c){}void Border(Rect r,Color c){}void DrawIcon(Rect r,object icon,Color c){}void Text(Rect r,string s,int font,Color c,bool bold=false,bool wrap=false,TextAnchor align=TextAnchor.MiddleRight){Labels.Add(s);GUI.Caption=s;GUI.Click=Click;}
 class Measurement{public float CalcHeight(GUIContent s,float w)=>120;}Measurement Style(int n,bool bold=false,bool wrap=false)=>new Measurement();Vector2 BeginTouchScroll(string key,Rect bounds,Vector2 position,Rect content)=>position;void EndTouchScroll(){}Rect MobilePanelRect(MobilePanelLayout.Area a)=>new Rect(a.X,a.Y,a.Width,a.Height);
 static int checks;static void C(bool b,string s){checks++;if(!b)throw new Exception(s);}void Next(){Time.frameCount++;Time.unscaledTime+=1;Click="";LockClick=false;Labels.Clear();GUI.enabled=true;}
 public static void Main(){var u=new GameUI();var bounds=new Rect(210,68,340,210);var anchor=new Rect(500,220,44,44);var a=new ItemData{id="a",name="A",score=20};u.session.Progression.Profile.inventory.Add(a);u.OpenInventoryPopup("a",anchor);u.Click="穿戴";u.DrawInventoryPopup(bounds,1);C(u.session.Progression.Equips==0,"opening event cannot activate popup action");u.Next();u.Click="穿戴";u.DrawInventoryPopup(bounds,1);u.DrawInventoryPopup(bounds,1);C(u.session.Progression.Equips==1&&u.session.Progression.worn==a,"equip once updates actual selected identity");u.Next();u.Click="对比";u.DrawInventoryPopup(bounds,1);C(u.inventoryPopupCompare,"comparison expands inside same popup");u.Next();u.Click="锁定";u.DrawInventoryPopup(bounds,1);C(a.locked,"lock accessible in contextual action row");
 u.Next();u.Mouse=new Vector2(1,1);Event.current.type=EventType.MouseDown;u.PrepareInventoryPopupInput();C(!u.inventoryComparisonOpen&&Event.current.type==EventType.Used&&u.inventoryPopupDismissed==Time.frameCount,"outside press closes and cannot click through");
 u.OpenInventoryPopup("a",anchor);Event.current.type=EventType.ScrollWheel;u.PrepareInventoryPopupInput();C(!u.inventoryComparisonOpen,"scroll closes stale anchor");
 u.Next();u.OpenInventoryPopup("@potion",anchor);u.Next();u.Click="使用";u.DrawInventoryPopup(bounds,1);u.DrawInventoryPopup(bounds,1);C(u.session.Uses==1&&u.session.Progression.Profile.potions==2,"repeated event cannot double-use consumable");
 u.Next();u.session.Player.Health=100;u.Click="使用";u.DrawInventoryPopup(bounds,1);C(u.session.Uses==1,"full health disables use");
 var f=new FashionData{id="fashion-0-3",name="Wings",slot=FashionSlot.Wings,rarity=Rarity.Legendary};u.session.Progression.Profile.fashions.Add(f);u.Next();u.OpenInventoryPopup("@fashion:"+f.id,anchor);u.Next();u.Click="穿戴";u.DrawInventoryPopup(bounds,1);C(u.session.Progression.fashion==f&&!u.Labels.Contains("对比"),"owned fashion equips directly without meaningless combat comparison");u.Next();u.Click="卸下";u.DrawInventoryPopup(bounds,1);C(u.session.Progression.fashion==null&&u.session.Progression.Profile.fashions.Count==1,"unequip preserves ownership");
 u.Next();u.inventoryComparisonOpen=false;GUI.Buttons=0;u.DrawBagFashion(new MobilePanelLayout.Area(210,68,340,210));C(GUI.Buttons==1&&!u.Labels.Exists(s=>s.Contains("试穿")||s.Contains("未解锁")),"fashion grid draws owned entries only");
 u.Next();u.session.Progression.Profile.inventory.Clear();u.OpenInventoryPopup("a",anchor);u.Next();u.DrawInventoryPopup(bounds,1);C(!u.inventoryComparisonOpen,"deleted item cannot leave stale actions");
 var items=new List<ItemData>{new ItemData{id="c",score=10,slot=ItemSlot.Weapon},new ItemData{id="b",score=20,slot=ItemSlot.Relic},new ItemData{id="a",score=20,slot=ItemSlot.Armor}};items.Sort(u.CompareInventoryItems);C(items[0].id=="a"&&items[1].id=="b"&&items[2].id=="c","score descending across categories with ordinal stable ties");items.Reverse();items.Sort(u.CompareInventoryItems);C(items[0].id=="a"&&items[1].id=="b","equal scores remain deterministic after input reorder");Console.WriteLine("PASS "+checks+" actual popup ownership/input/action/sorting assertions; managed GUI/service boundaries");}
 METHODS
 }
}
'''.replace('METHODS',methods)
with tempfile.TemporaryDirectory(prefix='inventory-popup-') as tmp:
 p=Path(tmp);(p/'Fixture.cs').write_text(fixture)
 for name in ['InventoryGridGeometry','MobilePanelLayout']:(p/(name+'.cs')).write_text((root/'Assets/Scripts/UI'/(name+'.cs')).read_text())
 (p/'Test.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NuGetAudit>false</NuGetAudit><NoWarn>0649;0414</NoWarn></PropertyGroup></Project>');(p/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
 subprocess.run([sys.argv[1] if len(sys.argv)>1 else 'dotnet','run','--project',str(p/'Test.csproj')],env=dict(os.environ,DOTNET_CLI_HOME=str(p/'cli'),DOTNET_NOLOGO='1'),check=True)
