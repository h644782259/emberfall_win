using System;using System.Linq;using System.Collections.Generic;using UnityEngine;
namespace UnityEngine
{
 public struct Vector3{public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}}
 public struct Quaternion{public static Quaternion identity=>new Quaternion();}
 public struct Matrix4x4{float x,y,sx,sy;public static Matrix4x4 identity=>new Matrix4x4{sx=1,sy=1};public static Matrix4x4 TRS(Vector2 p,Quaternion q,Vector3 s)=>new Matrix4x4{x=p.x,y=p.y,sx=s.x,sy=s.y};public Rect Apply(Rect r)=>new Rect(x+r.x*sx,y+r.y*sy,r.width*sx,r.height*sy);}
 public static class Screen{public static float height;}
}
namespace Emberfall
{
 public sealed partial class GameUI
 {
  struct Control{public Rect Rect;public string Caption;public bool Scroll;public int Font;}
  readonly List<Control> geometryButtons=new List<Control>();bool geometryScrolling;Rect geometryView,geometryContent,geometryFrame;
  float scale=1;Vector2 guiOffset;Rect hotbarBounds;Rect[] hotbarSlots=new Rect[10];void ObserveTouchViewport(Rect r){}
  void CancelMobileCast(){}void CancelHotbarPointer(){}void DrawMobileHotbar(){}void DrawHotbar(){}void DrawCompanionCommands(){}void DrawChargeProgress(){}void DrawTargetingHint(){}
  static int geometryChecks;static void G(bool value,string why){geometryChecks++;if(!value)throw new Exception(why);}
  static bool Inside(Rect r,Rect outer)=>r.x>=outer.x-.01f&&r.y>=outer.y-.01f&&r.xMax<=outer.xMax+.01f&&r.yMax<=outer.yMax+.01f;
  void GeometryButton(Rect r,string caption)
  {geometryButtons.Add(new Control{Rect=r,Caption=caption,Scroll=geometryScrolling,Font=MobileControls.Active&&r.height>=44*TouchRatio?TouchFont(15):15});}
  public static void Geometry(ProgressionService p)
  {
   p.Profile.level=45;p.Save();p.SaveBuildPreset(0,true);p.SaveBuildPreset(1,true);
   var sizes=new[]{(240f,135f),(360f,203f),(568f,320f),(844f,390f),(1280f,720f),(1366f,768f),(2560f,1440f)};
   var fonts=new HashSet<int>();var scales=new HashSet<float>();
   foreach(bool mobile in new[]{false,true})foreach(var size in sizes)foreach(float dpi in new[]{0f,120f,163f,261f,348f,700f})foreach(int disclosure in new[]{0,1,2})
   {
    MobileControls.Active=mobile;MobileControls.SafeArea=new Rect(23,41,size.Item1,size.Item2);Screen.height=size.Item2+99;
    MobileControls.Layout=new MobileControlLayout(size.Item1,size.Item2,dpi);
    var ui=new GameUI{session=new GameSession{Progression=p}};ui.session.Bind();ui.OpenBuildPlans();
    ui.buildPlanDetails=disclosure==1?0:-1;ui.practiceChoicesOpen=disclosure==2;
    ui.GeometryFrame();float unit=mobile?ui.TouchRatio:1;fonts.Add(mobile?ui.TouchFont(15):15);scales.Add(ui.scale);
    var safe=new Rect(23,Screen.height-MobileControls.SafeArea.yMax,size.Item1,size.Item2);
    G(Inside(GUI.matrix.Apply(ui.geometryFrame),safe),"actual full dialog fits transformed safe area");
    var footer=ui.geometryButtons.Where(x=>!x.Scroll).ToArray();G(footer.Length==2,"fixed footer has both return and reset actions");
    foreach(var button in footer){G(Inside(GUI.matrix.Apply(button.Rect),safe),"fixed footer fits physical safe area");G(button.Rect.y>=ui.geometryView.yMax,"footer lies below scroll clip");G(button.Font==15||mobile,"desktop button policy stays fixed at15 logical units");}
    var primary=ui.geometryButtons.Where(x=>x.Scroll&&x.Caption!=null&&(x.Caption.StartsWith("覆盖方案")||x.Caption.StartsWith("应用方案"))).ToArray();
    G(primary.Length==4,"whole page exposes both A/B record and apply actions");
    G(primary[0].Caption.EndsWith("A")&&primary[1].Caption.EndsWith("A")&&primary[2].Caption.EndsWith("B")&&primary[3].Caption.EndsWith("B"),"bounded slot names remain A/B");
    foreach(var button in ui.geometryButtons.Where(x=>x.Scroll))
    {G(button.Rect.x>=0&&button.Rect.xMax<=ui.geometryContent.width+.01f&&button.Rect.y>=0&&button.Rect.height>=48*unit-.01f,"whole page controls stay in content width with scaled touch height");}
    foreach(var button in primary.Concat(footer))G((button.Caption??"").Length*button.Font<=button.Rect.width-8+.01f,"primary unwrapped captions fit explicit managed glyph estimate");
    float cw=new MobileDialogLayout(ui.width/unit,ui.height/unit).Body.Width-18;
    float measured=ui.DrawBuildPlanContent(cw,unit,false),drawn=ui.DrawBuildPlanContent(cw,unit,true);
    G(Math.Abs(measured-drawn)<.01f,"whole page measured and drawn flow agree across actual unit scales");
    G(primary[3].Rect.y<ui.geometryContent.height,"B action is included in scroll content, not below its extent");
    ui.RequestBuildPlanAction(BuildPlanAction.Save,0);ui.GeometryFrame();footer=ui.geometryButtons.Where(x=>!x.Scroll).ToArray();
    G(footer.Length==2&&footer[0].Caption=="取消"&&footer[1].Caption=="确认记录方案","actual confirmation footer remains fixed and reviewable");
    foreach(var button in footer)G(Inside(GUI.matrix.Apply(button.Rect),safe)&&button.Rect.y>=ui.geometryView.yMax,"confirmation actions outside clipped content");
   }
   G(fonts.Contains(15)&&fonts.Contains(24)&&fonts.Contains(32),"actual mobile TouchFont policy exercises 15/24/32 logical fonts");G(scales.Count>3,"actual RefreshLayout exercises multiple GUI matrix scales");
   Console.WriteLine("PASS "+geometryChecks+" full build-plan/card/confirmation/footer geometry assertions; managed font and scroll boundaries only, NOT Unity rendering or touch acceptance");
   Console.WriteLine("POLICY: desktop button15 logical units; mobile Round(15*TouchRatio); physical coordinates use actual RefreshLayout and OnGUI matrix. Small physical viewports prove bounds only, not readability or minimum physical touch size.");
  }
 }
}
