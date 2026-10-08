// Actual DrawCollectionModel/DrawCollectionControls are extracted unchanged; this
// fixture substitutes GUI/preview rendering, and records real issued rectangles.
using System;using System.Collections.Generic;using UnityEngine;using Emberfall;
namespace UnityEngine
{
 public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>new Vector2();}
 public struct Rect{public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}public float xMax=>x+width;public float yMax=>y+height;}
 public struct Color{public Color(float r,float g,float b){}}
 public class Texture{}public enum ScaleMode{ScaleToFit,ScaleAndCrop}
 public static class Mathf{public static float Abs(float x)=>Math.Abs(x);}
 public struct Matrix{public float m00,m11;}public static class GUI{public static Matrix matrix=new Matrix{m00=1,m11=1};public static Rect Image;public static void DrawTexture(Rect r,Texture t,ScaleMode m,bool alpha){Image=r;}}
}
namespace Emberfall
{
 public enum HeroClass{Arcanist}public enum FashionSlot{Wings,Weapon}public enum ItemSlot{Weapon,Armor,Relic}
 public class ItemData{}public class FashionData{public FashionSlot slot;}public static class MobileControls{public static bool Active;}
 public class Profile{public HeroClass heroClass;}public class Progression{public Profile Profile=new Profile();public FashionData EquippedFashion(FashionSlot slot)=>null;public ItemData Equipped(ItemSlot slot)=>null;}
 public class Session{public Progression Progression=new Progression();}
 public class CollectionModelPreview{public float Width,Height;public CollectionPreviewAction PreviewAction;public void SetEquipmentFraming(bool enabled,bool detail){if(enabled)throw new Exception("Collection layout must clear equipment framing");}public void SetYaw(float v){}public void SetComposition(CollectionPreviewComposition v){}public void SetViewport(float w,float h,bool mobile){Width=w;Height=h;}public string LastError=>null;public Texture RenderSafe(HeroClass h,ItemData w,ItemData a,ItemData r,FashionData f,FashionData g)=>new Texture();public void Play(CollectionPreviewAction a){PreviewAction=a;}}
 public sealed partial class GameUI
 {
  Session session=new Session();CollectionModelPreview collectionModel;CollectionViewingState collectionViewing=new CollectionViewingState();float collectionPreviewYaw;Color gold,jade,muted;public List<Rect> Buttons=new List<Rect>();
  void Text(Rect r,string s,int size,Color c,bool bold=false,bool wrap=false){throw new Exception("unexpected preview fallback");}void Fill(Rect r,Color c){}bool TabButton(Rect r,string text,bool selected)=>Button(r,text,new Color());bool Button(Rect r,string text,Color c){Buttons.Add(r);return false;}void BlockUITransition(){}
  public void Draw(Rect area,bool rotate){Buttons.Clear();DrawCollectionModel(area,null,rotate);}
  public void Controls(CollectionPreviewLayout layout,Vector2 origin,float scale){Buttons.Clear();DrawCollectionControls(layout,origin,scale);}
  public float NativeWidth=>collectionModel.Width;public float NativeHeight=>collectionModel.Height;
 }
}
public static class CollectionCompactGeometryProductionTests
{
 static int n;static void Check(bool ok,string why){n++;if(!ok)throw new Exception(why);}
 static bool Overlap(Rect a,Rect b)=>a.x<b.xMax&&a.xMax>b.x&&a.y<b.yMax&&a.yMax>b.y;
 static Rect Rect(MobilePanelLayout.Area a,float scale)=>new Rect(a.X*scale,a.Y*scale,a.Width*scale,a.Height*scale);
 static void VerifyImage(GameUI ui,Rect expected)
 {
  Check(GUI.Image.x==expected.x&&GUI.Image.y==expected.y&&GUI.Image.width==expected.width&&GUI.Image.height==expected.height,"actual texture draw uses unobscured model rectangle");
  Check(Math.Abs(ui.NativeWidth-GUI.Image.width*Math.Abs(GUI.matrix.m00))<.001&&Math.Abs(ui.NativeHeight-GUI.Image.height*Math.Abs(GUI.matrix.m11))<.001,"native viewport matches actual unobscured image area");
 }
 public static string Run()
 {
  foreach(var screen in new[]{new[]{568f,320f},new[]{800f,450f},new[]{1024f,768f}})foreach(float scale in new[]{1f,1.5f,2f})
  {
   var panel=new MobilePanelLayout(screen[0],screen[1]);var layout=CollectionPreviewLayout.Mobile(panel.BodyLeft,panel.BodyRight);var ui=new GameUI();MobileControls.Active=true;GUI.matrix=new Matrix{m00=1.2f,m11=.9f};
   ui.Draw(Rect(layout.Model,scale),true);VerifyImage(ui,Rect(panel.BodyLeft,scale));Check(ui.Buttons.Count==0,"compact image has no overlay control draws");Check(GUI.Image.height>=188*scale,"compact model retains entire 188-unit height");
   ui.Controls(layout,new Vector2(panel.BodyRight.X,panel.BodyRight.Y),scale);Check(ui.Buttons.Count==8,"all view action and rotation buttons remain reachable");
   for(int i=0;i<ui.Buttons.Count;i++)
   {
    var local=ui.Buttons[i];var absolute=new Rect(local.x+panel.BodyRight.X*scale,local.y+panel.BodyRight.Y*scale,local.width,local.height);
    Check(local.width>=48*scale&&local.height==48*scale,"compact controls retain full touch targets");
    Check(local.x>=0&&local.y>=0&&local.xMax<=panel.BodyRight.Width*scale&&local.yMax<=panel.BodyRight.Height*scale,"three control groups initially fit within actual detail viewport");
    Check(!Overlap(absolute,GUI.Image),"all compact controls are disjoint from model image");
    for(int j=0;j<i;j++)Check(!Overlap(local,ui.Buttons[j]),"actual issued compact button rectangles do not overlap");
   }
   ui.Draw(Rect(panel.BodyLeft,scale),false);Check(ui.Buttons.Count==0,"receipt-only preview adds no controls");VerifyImage(ui,Rect(panel.BodyLeft,scale));
  }
  MobileControls.Active=false;var desktop=new GameUI();var frame=new Rect(24,110,270,365);desktop.Draw(frame,true);Check(desktop.Buttons.Count==8,"desktop retains all eight controls");Check(GUI.Image.height==221,"desktop reserves a separate control bank");VerifyImage(desktop,new Rect(24,110,270,221));foreach(var button in desktop.Buttons)Check(!Overlap(button,GUI.Image),"desktop control bank cannot obscure model");
  return "PASS: "+n+" actual compact preview draw/control geometry assertions; managed GUI rectangles, not rendered pixels";
 }
}
