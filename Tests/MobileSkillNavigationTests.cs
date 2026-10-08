using System;
using Emberfall;
namespace UnityEngine
{
 public struct Rect{}
 public static class Time{public static int frameCount;}
 public sealed class GUIContent{public static GUIContent none=new GUIContent();}
 public static class GUI{public static bool enabled=true,Clicked=true;public static int Calls;public static bool Button(Rect r,GUIContent c,object s){Calls++;return Clicked;}}
}
namespace Emberfall
{
 public static class MobileControls{public static bool Active=true;public sealed class LayoutValue{public float Width=568;}public static LayoutValue Layout=new LayoutValue();}
 public sealed partial class GameUI
 {
  private bool MerchantServiceActive=>true;enum Panel{Skills,Inventory}private Panel panel=Panel.Skills;private bool mobileSkillDetail;private int touchScrollSuppressed=-1,blocks,cancels;private object invisibleButton;
  private object routeSkillOwner;private string routeSkillSlot;
  private float mobileSkillListScroll=173,mobileSkillDetailScroll=81;
  private void CancelMobileScroll(){cancels++;}private void BlockUITransition(){blocks++;}
  public static int Verify(){int n=0;Action<bool,string> check=(ok,why)=>{n++;if(!ok)throw new Exception(why);};
   foreach(float width in new[]{568,667,799,800,1024,1366}){
    MobileControls.Layout.Width=width;var ui=new GameUI{mobileSkillDetail=true};bool expected=width<800;
    check(ui.CloseMobileSkillDetail()==expected,"only narrow skill detail owns first Back");
    check(ui.mobileSkillListScroll==173&&ui.mobileSkillDetailScroll==81,"first Back preserves both scroll anchors");
    check(!ui.CloseMobileSkillDetail(),"second Back falls through to outer ClosePanel");
    check(ui.blocks==(expected?1:0)&&ui.cancels==(expected?1:0),"only consumed Back cancels active scroll and latches release");
    ui.ResetMobileSkillNavigation();check(!ui.mobileSkillDetail&&ui.mobileSkillListScroll==173,"reopen goes to list without losing scroll");
   }
   MobileControls.Layout.Width=568;var other=new GameUI{panel=Panel.Inventory,mobileSkillDetail=true};check(!other.CloseMobileSkillDetail(),"inventory owns its own navigation");
   MobileControls.Active=false;other.panel=Panel.Skills;check(!other.CloseMobileSkillDetail(),"desktop keeps its tree navigation");MobileControls.Active=true;
   foreach(bool horizontal in new[]{false,true}){
    var ui=new GameUI();var scroll=new TouchScrollGesture();scroll.Begin(11,0,0,120,500,9);
    UnityEngine.Time.frameCount++;bool dragging=scroll.Advance(11,horizontal?20:0,horizontal?0:-20,false,false);
    if(dragging)ui.touchScrollSuppressed=UnityEngine.Time.frameCount;
    int calls=UnityEngine.GUI.Calls;check(!ui.MobileSkillRowClicked(new UnityEngine.Rect())&&UnityEngine.GUI.Calls==calls,"drag cannot invoke row button");
    UnityEngine.Time.frameCount++;bool release=scroll.Advance(11,horizontal?20:0,horizontal?0:-20,true,false);
    if(release)ui.touchScrollSuppressed=UnityEngine.Time.frameCount;
    check(!ui.MobileSkillRowClicked(new UnityEngine.Rect())&&UnityEngine.GUI.Calls==calls,"drag release cannot select detail even if underlying GUI reports click");
    UnityEngine.Time.frameCount++;check(ui.MobileSkillRowClicked(new UnityEngine.Rect()),"fresh following tap remains reachable");
   }
   UnityEngine.GUI.enabled=false;check(!new GameUI().MobileSkillRowClicked(new UnityEngine.Rect()),"disabled/background UI cannot select row");UnityEngine.GUI.enabled=true;
   return n;
  }
 }
}
public static class MobileSkillNavigationTests{public static string Run(){return "PASS: "+GameUI.Verify()+" production mobile skill navigation assertions";}}
