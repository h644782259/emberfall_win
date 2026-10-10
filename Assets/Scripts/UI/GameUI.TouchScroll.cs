using UnityEngine;
namespace Emberfall
{
 public sealed partial class GameUI
 {
  private readonly TouchScrollGesture touchScroll=new TouchScrollGesture();
  private string touchScrollOwner;private Panel touchScrollPanel;private int touchScrollFrame=-1,touchScrollSuppressed=-1;
  private bool scrollPriorEnabled;
  private Vector2 BeginTouchScroll(string owner,Rect viewport,Vector2 position,Rect content,bool horizontal=false,bool vertical=true,bool showScrollbar=true)
  {
   scrollPriorEnabled=GUI.enabled;
   if(MobileControls.Active)
   {
    if(touchScroll.Finger!=-1000&&touchScrollPanel!=panel){touchScroll.Cancel();touchScrollOwner=null;}
    if(touchScrollFrame!=Time.frameCount||touchScrollOwner!=owner)
    {
     for(int i=0;i<Input.touchCount;i++)
     {
      Touch t=Input.GetTouch(i);Vector2 pointer=ScreenToUI(t.position);
      if(owner!="entry-reward-popup"&&entryRewardPopupVisible&&entryRewardPopupRect.Contains(pointer)&&t.phase==TouchPhase.Began)continue;
      if(t.phase==TouchPhase.Began&&touchScroll.Finger==-1000&&GUI.enabled&&viewport.Contains(pointer)&&pointer.x<viewport.xMax-16)
      {touchScroll.Begin(t.fingerId,pointer.x,pointer.y,position.y,Mathf.Max(0,content.height-viewport.height),9*TouchRatio);touchScrollOwner=owner;touchScrollPanel=panel;}
      if(touchScrollOwner!=owner||touchScroll.Finger!=t.fingerId)continue;
      bool consume=touchScroll.Advance(t.fingerId,pointer.x,pointer.y,t.phase==TouchPhase.Ended,t.phase==TouchPhase.Canceled);
      position.y=touchScroll.Position;touchScrollFrame=Time.frameCount;
      if(consume||t.phase==TouchPhase.Canceled){touchScrollSuppressed=Time.frameCount;GUIUtility.hotControl=0;}
     }
    }
    else if(touchScrollOwner==owner)position.y=touchScroll.Position;
    bool suppress=touchScrollSuppressed==Time.frameCount||touchScrollOwner==owner&&touchScroll.Dragging;
    if(suppress)
    {
     // Consume only input events. Repaint keeps the normal enabled appearance.
     if(Event.current.isMouse&&Event.current.type!=EventType.Repaint&&Event.current.type!=EventType.Layout)Event.current.Use();
    }
   }
   bool overflow=vertical&&content.height>viewport.height+.5f;
   return GUI.BeginScrollView(viewport,position,content,horizontal,false,GUIStyle.none,overflow&&showScrollbar?scrollBar:GUIStyle.none);
  }
  private void EndTouchScroll(){GUI.EndScrollView();GUI.enabled=scrollPriorEnabled;}
  private void ReconcileMobileScroll()
  {
   if(touchScroll.Finger==-1000)return;
   if(touchScrollPanel!=panel||!MobileControls.Active){CancelMobileScroll();return;}
   bool found=false;for(int i=0;i<Input.touchCount;i++)if(Input.GetTouch(i).fingerId==touchScroll.Finger){found=true;break;}
   if(!found)CancelMobileScroll();
  }
  private void CancelMobileScroll(){GUIUtility.hotControl=0;touchScroll.Cancel();touchScrollOwner=null;touchScrollSuppressed=-1;}
 }
}
