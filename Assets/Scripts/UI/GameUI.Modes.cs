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
    Text(new Rect(r.x+8*u,r.y+33*u,r.width-16*u,18*u),i==5?"章节 · 双印路线":i==0?"武器 · 外观":i==1?"护甲 · 守点":i==2?"饰品 · 限时":i==3?"史诗武器 · 首领":"双装备 · 五房",Mathf.RoundToInt(11*u),muted);
    if(GUI.Button(r,GUIContent.none,invisibleButton))
    {adventureChapterSelected=i==5;if(i<5)session.SelectedArenaMode=i-1;adventureDetailScroll=Vector2.zero;CancelMobileScroll();BlockUITransition();}
   }
   EndTouchScroll();
   int mode=session.SelectedArenaMode,tier=session.SelectedDungeonTier;float contentWidth=l.Details.Width-18;
   string detail=adventureChapterSelected?"星路章节\n\n选择章节与节点，完成双印路线并到达出口。\n章节解锁、难度、阶数和治疗限制在章节页设置。\n奖励按所选节点展示并结算。":
    names[mode+1]+"\n\n通关保底\n"+AdventureRewardRules.EquipmentSummary(mode,tier)+"\n装备等级 "+ProgressionService.EquipmentGenerationLevel(session.Progression.Profile.level)+" · 直接入行囊\n星烬碎片 × "+AdventureRewardRules.Materials(mode,tier)+"\n金币 × "+AdventureRewardRules.Gold(mode,tier,false)+" · 经验 × "+AdventureRewardRules.Experience(mode,tier)+(mode==-1?"\n外观宝箱 × 1（完成后开启）":"")+
    "\n\n遭遇与目标\n"+AdventureEntryPresentation.EncounterLine(mode)+"\n"+(mode==0?"三阶段：圈内无人争夺时推进占领，清敌并占领后进入下一阶段。":mode==1?"三阶段限时突破，战斗暂停不消耗时间。":mode==2?"连续击败三个不同攻击模式的首领。":mode==3?"完成五个房间目标，满足封印条件后前往出口。":"完成三波战斗并击败终局首领。")+
    "\n\n进入条件与进度\n当前开放至第 "+session.MaximumDungeonTier+" 阶 · 本次第 "+tier+" 阶\n"+(session.SelectedChallengeMode?"限疗挑战 · 每次冒险仅 3 次治疗":"普通治疗")+"\n"+AdventureEntryPresentation.GoalFit(session.Progression.Profile,session.Progression.SelectedProgressionGoal(),mode,tier)+"\n\n敌人随机掉落与通关保底分别结算；风险契约祝福会额外提高金币。";
   float h=Style(Mathf.RoundToInt(14*u),false,true).CalcHeight(new GUIContent(detail),contentWidth*u)+16*u;
   adventureDetailScroll=BeginTouchScroll("adventure-detail",AdventureRect(l.Details,u),adventureDetailScroll,new Rect(0,0,contentWidth*u,Mathf.Max(l.Details.Height*u,h)));
   Text(new Rect(8*u,4*u,(contentWidth-12)*u,h),detail,Mathf.RoundToInt(14*u),pale,false,true);EndTouchScroll();
   float x=l.X,y=l.FooterY;
   if(InventoryPictogramAction(new Rect(x*u,y*u,48*u,48*u),"返回",UIIconAtlas.Utility("cancel"))){adventureChapterSelected=false;session.CancelDungeonSelection();return;}
   bool normal=!adventureChapterSelected;
   if(Button(new Rect((x+56)*u,y*u,44*u,48*u),"−",jade,normal&&tier>1))session.SelectedDungeonTier--;
   Text(new Rect((x+100)*u,y*u,80*u,48*u),"第"+tier+"阶",Mathf.RoundToInt(13*u),gold,true,false,TextAnchor.MiddleCenter);
   if(Button(new Rect((x+180)*u,y*u,44*u,48*u),"+",jade,normal&&tier<session.MaximumDungeonTier))session.SelectedDungeonTier++;
   if(Button(new Rect((x+232)*u,y*u,116*u,48*u),session.SelectedChallengeMode?"限疗挑战":"普通治疗",jade,normal))session.SelectedChallengeMode=!session.SelectedChallengeMode;
   if(PrimaryButton(new Rect((x+356)*u,y*u,(l.Frame.Width-356)*u,48*u),adventureChapterSelected?"选择章节":"进入挑战",gold))
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
