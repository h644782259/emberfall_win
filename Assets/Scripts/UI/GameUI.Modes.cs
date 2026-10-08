using UnityEngine;
namespace Emberfall
{
 public sealed partial class GameUI
 {
  private void DrawArenaSelection()
  {
   float u=MobileControls.Active?TouchRatio:Mathf.Clamp(Mathf.Min(width/760f,height/480f),1f,1.35f);
   var layout=new AdventureSelectionLayout(width/u,height/u);
   float x=layout.X,y=layout.Y;
   Fill(new Rect(0,0,width,height),new Color(.012f,.025f,.04f,.92f));
   blockedRects.Add(new Rect(0,0,width,height));
   var f=layout.Frame;Rect frame=new Rect(f.X*u,f.Y*u,f.Width*u,f.Height*u);Fill(frame,new Color(.035f,.06f,.085f));Border(frame,jade*.5f);
   Text(new Rect((x+240)*u,y*u,280*u,24*u),"已解锁最高 "+session.MaximumDungeonTier+" 阶",Mathf.RoundToInt(13*u),muted,false,false,TextAnchor.MiddleRight);
   Text(new Rect(x*u,y*u,520*u,26*u),"选择冒险",Mathf.RoundToInt(22*u),pale,true);
   string[] names={"沉星遗迹","守望林庭","烬河突围","蚀星斗场","回廊远征"};
   for(int i=0;i<names.Length;i++)
   {
    var a=layout.Entry(i);Rect r=new Rect(a.X*u,a.Y*u,a.Width*u,a.Height*u);bool chosen=session.SelectedArenaMode==i-1;
    Fill(r,chosen?new Color(.11f,.2f,.21f):card);Border(r,chosen?gold:jade*.4f);
    var title=layout.EntryTitle(i);var reward=layout.EntryReward(i);var encounter=layout.EntryEncounter(i);
    int titleFont=Mathf.RoundToInt(Mathf.Lerp(13,16,layout.Spaciousness)*u);
    int bodyFont=Mathf.RoundToInt(Mathf.Lerp(10,11,layout.Spaciousness)*u);
    Text(new Rect(title.X*u,title.Y*u,title.Width*u,title.Height*u),names[i],titleFont,chosen?gold:pale,true);
    Text(new Rect(reward.X*u,reward.Y*u,reward.Width*u,reward.Height*u),AdventureEntryPresentation.RewardLine(i-1,session.SelectedDungeonTier),bodyFont,jade);
    Text(new Rect(encounter.X*u,encounter.Y*u,encounter.Width*u,encounter.Height*u),AdventureEntryPresentation.EncounterLine(i-1),bodyFont,muted);
    if(GUI.Button(r,GUIContent.none,invisibleButton))session.SelectedArenaMode=i-1;
   }
   // The spare sixth cell carries the same selected goal as camp and results.
   var g=layout.Entry(5);Rect goal=new Rect(g.X*u,g.Y*u,g.Width*u,g.Height*u);
   Fill(goal,card);
   var selectedGoal=session.Progression.SelectedProgressionGoal();
   float goalPadding=layout.TextPadding*u;
   float goalTitleHeight=Mathf.Lerp(14,18,layout.Spaciousness)*u;
   float goalBodyOffset=goalTitleHeight+Mathf.Lerp(3,6,layout.Spaciousness)*u;
   Text(new Rect(goal.x+12*u,goal.y+goalPadding,goal.width-24*u,goalTitleHeight),selectedGoal.Title,Mathf.RoundToInt(11*u),jade,true);
   Text(new Rect(goal.x+12*u,goal.y+goalPadding+goalBodyOffset,goal.width-24*u,goal.height-2*goalPadding-goalBodyOffset),AdventureEntryPresentation.GoalFit(session.Progression.Profile,selectedGoal,session.SelectedArenaMode,session.SelectedDungeonTier),Mathf.RoundToInt(10*u),pale,false,true);
   float options=layout.OptionsY;
   Text(new Rect(x*u,options*u,180*u,25*u),"第 "+session.SelectedDungeonTier+" 阶",Mathf.RoundToInt(18*u),gold,true,false,TextAnchor.MiddleLeft);
   
   Text(new Rect(x*u,(options+27)*u,180*u,18*u),"通关碎片 "+AdventureEntryPresentation.Materials(session.SelectedArenaMode,session.SelectedDungeonTier),Mathf.RoundToInt(12*u),jade);
   if(Button(new Rect((x+184)*u,options*u,48*u,48*u),"−",jade,session.SelectedDungeonTier>1))session.SelectedDungeonTier--;
   if(Button(new Rect((x+240)*u,options*u,48*u,48*u),"+",jade,session.SelectedDungeonTier<session.MaximumDungeonTier))session.SelectedDungeonTier++;
   if(ToggleButton(new Rect((x+300)*u,options*u,220*u,48*u), session.SelectedChallengeMode?"限疗挑战 ✓":"普通治疗", session.SelectedChallengeMode))session.SelectedChallengeMode=!session.SelectedChallengeMode;
   if(NavigationButton(new Rect(x*u,layout.FooterY*u,190*u,48*u), "返回", muted))session.CancelDungeonSelection();
   if(PrimaryButton(new Rect((x+208)*u,layout.FooterY*u,312*u,48*u), "进入挑战", gold, true, null, true))session.ConfirmDungeonSelection();
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
