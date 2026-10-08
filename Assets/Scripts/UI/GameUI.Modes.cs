using UnityEngine;
namespace Emberfall
{
 public sealed partial class GameUI
 {
  private string entryRewardSelection,entryRewardContext;
  private Rect entryRewardViewport;
  private sealed class EntryRewardPreview
  {
   public string Key,Name,Description;public Texture2D Icon;public Color Tint;
  }
  private System.Collections.Generic.List<EntryRewardPreview> EntryRewardPreviews(int mode,int tier,bool chapter)
  {
   var result=new System.Collections.Generic.List<EntryRewardPreview>();int level=ProgressionService.EquipmentGenerationLevel(session.Progression.Profile.level);
   if(!chapter)for(int i=0;i<AdventureRewardRules.EquipmentCount(mode);i++)
   {
    var slot=AdventureRewardRules.EquipmentSlot(mode,i);var rarity=AdventureRewardRules.MinimumRarity(mode);
    result.Add(new EntryRewardPreview{Key="gear"+i,Name=GameBalance.SlotName(slot),Icon=UIIconAtlas.EquipmentCardIcon(slot,level),Tint=GameBalance.RarityColor(rarity),Description="通关保底 · "+GameBalance.SlotName(slot)+"\n等级：按结算时角色等级对应的十级档生成，当前 "+level+" 级\n稀有度："+GameBalance.RarityName(rarity)+"，有机会升为"+GameBalance.RarityName(mode==2?Rarity.Legendary:Rarity.Epic)+"\n机制：无；基础属性在获得时生成。"});
   }
   result.Add(new EntryRewardPreview{Key="shard",Name="星烬碎片",Icon=UIIconAtlas.Utility("shard"),Tint=jade,Description="星烬碎片\n用于兑换机制宝石、升级宝石及解锁机制变体。\n通关获得，数量以当前副本奖励为准。"});
   if(!chapter&&mode==-1)result.Add(new EntryRewardPreview{Key="fashion",Name="外观宝箱",Icon=UIIconAtlas.FashionCardIcon(FashionSlot.Wings),Tint=gold,Description="外观宝箱\n通关后开启，可能获得兵装或羽翼及穿戴加成。\n品质随机；重复外观转为资源。"});
   foreach(var mechanic in BuildCatalog.MechanicsFor(session.Progression.Profile.heroClass))
   {
    string effect=BuildCatalog.MechanicDescription(mechanic);int variant=effect.IndexOf("变体");if(variant>0)effect=effect.Substring(0,variant).Trim();
    result.Add(new EntryRewardPreview{Key="mechanic"+mechanic,Name=BuildCatalog.MechanicName(mechanic),Icon=UIIconAtlas.EquipmentCardIcon(BuildCatalog.MechanicSlot(mechanic)),Tint=gold,Description=BuildCatalog.MechanicName(mechanic)+" · 敌人随机掉落\n等级与稀有度随实际掉落生成。\n"+effect});
   }
   return result;
  }
  private float DrawEntryRewardPreviews(float available,float u,int mode,int tier,bool chapter,bool draw)
  {
   string context=chapter?"chapter"+session.SelectedChapterNode+":"+session.SelectedChapterDifficulty+":"+session.SelectedChapterTier:mode+":"+tier;
   if(entryRewardContext!=context){entryRewardContext=context;entryRewardSelection=null;}
   var items=EntryRewardPreviews(mode,tier,chapter);int columns=Mathf.Max(1,Mathf.FloorToInt(available/82));float cell=available/columns;
   float end=28+Mathf.Ceil(items.Count/(float)columns)*88;
   if(draw)Text(new Rect(8*u,0,available*u,24*u),"掉落预览",Mathf.RoundToInt(14*u),gold,true);
   for(int i=0;i<items.Count;i++)
   {
    var item=items[i];Rect hit=new Rect((i%columns*cell+4)*u,(28+i/columns*88)*u,(cell-8)*u,80*u);
    if(!draw)continue;
    Rect icon=new Rect(hit.center.x-24*u,hit.y,48*u,48*u);Fill(icon,card);Border(icon,item.Tint);
    DrawIcon(new Rect(icon.x+5*u,icon.y+5*u,38*u,38*u),item.Icon,item.Tint);
    Text(new Rect(hit.x,hit.y+50*u,hit.width,28*u),item.Name,Mathf.RoundToInt(11*u),pale,false,true,TextAnchor.MiddleCenter);
    if(!MobileControls.Active&&entryRewardViewport.Contains(Mouse)&&hit.Contains(Event.current.mousePosition)&&GUI.enabled)tooltip=item.Description;
    if(MobileControls.Active&&GUI.Button(hit,GUIContent.none,invisibleButton))entryRewardSelection=entryRewardSelection==item.Key?null:item.Key;
   }
   var selected=MobileControls.Active?items.Find(item=>item.Key==entryRewardSelection):null;
   if(selected!=null)
   {
    float textWidth=Mathf.Max(80,available-64)*u;
    float h=Mathf.Max(52*u,Style(Mathf.RoundToInt(12*u),false,true).CalcHeight(new GUIContent(selected.Description),textWidth)+20*u);
    if(draw)
    {
     Rect box=new Rect(4*u,end*u,(available-8)*u,h);Fill(box,ink);Border(box,selected.Tint);
     Text(new Rect(box.x+10*u,box.y+10*u,textWidth,h-20*u),selected.Description,Mathf.RoundToInt(12*u),pale,false,true);
     if(NavigationButton(new Rect(box.xMax-44*u,box.y,44*u,44*u),"×",jade))entryRewardSelection=null;
    }
    end+=h/u+8;
   }
   return end+8;
  }
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
