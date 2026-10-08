using UnityEngine;
namespace Emberfall
{
 public sealed partial class GameUI
 {
  private Vector2 adventureListScroll,adventureDetailScroll;
  private bool adventureChapterSelected;
  private Rect AdventureRect(MobilePanelLayout.Area a,float u){return new Rect(a.X*u,a.Y*u,a.Width*u,a.Height*u);}
  private void DrawArenaSelection()
  {
   float u=MobileControls.Active?TouchRatio:1f;var l=new AdventureSelectionLayout(width/u,height/u);
   Fill(new Rect(0,0,width,height),new Color(.012f,.025f,.04f,.96f));blockedRects.Add(new Rect(0,0,width,height));
   Text(new Rect(l.X*u,l.Y*u,l.Frame.Width*u,36*u),"选择冒险",Mathf.RoundToInt(20*u),pale,true);
   string[] names={"沉星遗迹","守望林庭","烬河突围","蚀星斗场","回廊远征","星路章节"};
   adventureListScroll=BeginTouchScroll("adventure-list",AdventureRect(l.List,u),adventureListScroll,new Rect(0,0,(l.List.Width-18)*u,360*u));
   for(int i=0;i<6;i++)
   {
    var a=l.Entry(i);Rect r=AdventureRect(a,u);bool chosen=i==5?adventureChapterSelected:!adventureChapterSelected&&session.SelectedArenaMode==i-1;
    Fill(r,chosen?new Color(.11f,.2f,.21f):card);if(chosen)Fill(new Rect(r.x,r.y,3*u,r.height),gold);
    Text(new Rect(r.x+8*u,r.y+6*u,r.width-16*u,25*u),names[i],Mathf.RoundToInt(14*u),chosen?gold:pale,true);
    Text(new Rect(r.x+8*u,r.y+33*u,r.width-16*u,18*u),i==5?"双印路线":i==0?"三波 · 首领":i==1?"守点":i==2?"限时":i==3?"首领连战":"五房远征",Mathf.RoundToInt(11*u),muted);
    if(GUI.Button(r,GUIContent.none,invisibleButton))
    {adventureChapterSelected=i==5;if(i<5)session.SelectedArenaMode=i-1;adventureDetailScroll=Vector2.zero;adventureRewardHint=null;CancelMobileScroll();BlockUITransition();}
   }
   EndTouchScroll();
   int mode=session.SelectedArenaMode,tier=session.SelectedDungeonTier;float contentWidth=l.Details.Width-18;
   string detail=adventureChapterSelected?"星路章节 · 双印路线":names[mode+1]+" · 第 "+tier+" 阶";
   adventureDetailScroll=BeginTouchScroll("adventure-detail",AdventureRect(l.Details,u),adventureDetailScroll,new Rect(0,0,contentWidth*u,Mathf.Max(l.Details.Height*u,(MobileControls.Active&&!string.IsNullOrEmpty(adventureRewardHint)?390:282)*u)));
   Text(new Rect(8*u,4*u,(contentWidth-12)*u,32*u),detail,Mathf.RoundToInt(18*u),pale,true);
   if(!adventureChapterSelected)DrawAdventureRewards(mode,tier,contentWidth,u);
   EndTouchScroll();
   float x=l.X,y=l.FooterY;
   if(InventoryPictogramAction(new Rect(x*u,y*u,48*u,48*u),"返回",UIIconAtlas.Utility("cancel"))){adventureChapterSelected=false;session.CancelDungeonSelection();return;}
   bool normal=!adventureChapterSelected;
   if(Button(new Rect((x+56)*u,y*u,44*u,48*u),"−",jade,normal&&tier>1))session.SelectedDungeonTier--;
   Text(new Rect((x+100)*u,y*u,80*u,48*u),"第"+tier+"阶",Mathf.RoundToInt(13*u),gold,true,false,TextAnchor.MiddleCenter);
   if(Button(new Rect((x+180)*u,y*u,44*u,48*u),"+",jade,normal&&tier<session.MaximumDungeonTier))session.SelectedDungeonTier++;
   if(PrimaryButton(new Rect((x+232)*u,y*u,(l.Frame.Width-232)*u,48*u),adventureChapterSelected?"选择章节":"进入挑战",gold))
   {if(adventureChapterSelected){adventureChapterSelected=false;session.CancelDungeonSelection();OpenChapterSelection();BlockUITransition();}else session.ConfirmDungeonSelection();}
  }
  private void DrawMobileModeStatus(Rect r)
  {
   float y=r.y;
   if(session.ChapterActive)
   {
    var first=session.ChapterSealView(0);var second=session.ChapterSealView(1);
    DrawMobileObjectiveText(r,ref y,first!=null&&session.ChapterRun.DoorUnlocked?"双印完成 · 前往出口":ChapterDefinition.Get(session.ActiveChapterNode).Name,11,gold,true,true);
    if(first!=null){DrawMobileSealText(r,ref y,first);DrawMobileSealText(r,ref y,second);}
    else DrawMobileObjectiveText(r,ref y,session.ChapterObjectiveCompact,10,pale);
    return;
   }
   if(session.RoomChainRun!=null)
   {
    var objective=session.RoomObjectiveView;var first=session.RoomSealView(0);
    DrawMobileObjectiveText(r,ref y,first!=null&&session.RoomChainRun.DoorUnlocked?"双印完成 · 前往北门":objective.Title,11,gold,true,true);
    if(first!=null){DrawMobileSealText(r,ref y,first);DrawMobileSealText(r,ref y,session.RoomSealView(1));}
    else {DrawMobileObjectiveText(r,ref y,objective.ProgressText,10,pale);DrawMobileObjectiveText(r,ref y,objective.Hint,10,session.RoomCaptureContested?gold:jade);}
    DrawMobileObjectiveText(r,ref y,objective.SupportHint,10,muted);
    return;
   }
   DrawMobileObjectiveText(r,ref y,session.ModeName,11,gold,true,true);
   DrawMobileObjectiveText(r,ref y,"阶段 "+session.DungeonWave+" / 3 · "+Mathf.CeilToInt(session.ModeRun.RemainingSeconds)+"秒",10,pale);
   DrawMobileObjectiveText(r,ref y,(session.ModeRun.Mode==ExpeditionModeKind.HoldPoint?session.ModeRun.HoldStateLabel+" ":"")+Mathf.RoundToInt(session.ModeRun.ObjectiveProgress*100)+"%",10,jade);
  }
  private void DrawMobileSealText(Rect r,ref float y,ChapterSealPresentation seal)
  {
   if(seal==null)return;
   DrawMobileObjectiveText(r,ref y,seal.Label,10,seal.Complete?jade:seal.Contested?gold:pale,seal.Occupied);
  }
 }
}
