using UnityEngine;
namespace Emberfall
{
 public sealed partial class GameUI
 {
  private string entryRewardSelection,entryRewardContext;
  private Rect entryRewardViewport;
  private sealed class EntryRewardPreview
  {
   public string Key,Name,Description;public Texture2D Icon;public Color Tint;public Rarity Rarity;public bool Clear;
  }
  private System.Collections.Generic.List<EntryRewardPreview> EntryRewardPreviews(int mode,int tier,bool chapter)
  {
   var result=new System.Collections.Generic.List<EntryRewardPreview>();int level=ProgressionService.EquipmentGenerationLevel(session.Progression.Profile.level);
   int enemyTier=chapter?session.SelectedChapterTier:tier;
   bool hasBoss=chapter?session.SelectedChapterNode==ChapterNode.StarPlatform:mode==-1||mode==2||mode==3;
   // Each reachable quality has its own identity and graphic. Slot count comes from the completed-tier table.
   for(int slotIndex=0;slotIndex<3;slotIndex++)
   {
    var slot=(ItemSlot)slotIndex;int count=DropPreviewRules.SlotCount(mode,tier,slot);if(count==0)continue;
    foreach(var rarity in DropPreviewRules.ClearRarities(mode,tier))
     result.Add(new EntryRewardPreview{Key="clear:"+slot+":"+rarity,Clear=true,Rarity=rarity,Name=GameBalance.SlotName(slot)+" · "+GameBalance.RarityName(rarity),Icon=UIIconAtlas.EquipmentCardIcon(slot,level,rarity,session.Progression.Profile.heroClass),Tint=GameBalance.RarityColor(rarity),Description="通关宝箱必得 "+GameBalance.SlotName(slot)+" ×"+count+"，至少"+GameBalance.RarityName(AdventureRewardRules.MinimumRarity(mode))+"。\n本图展示可能品质："+GameBalance.RarityName(rarity)+"，不是各品质各送一件。\n当前等级档 Lv."+level+"；按开启宝箱时角色等级生成。"});
   }
   result.Add(new EntryRewardPreview{Key="shard",Clear=true,Rarity=Rarity.Rare,Name="星烬碎片",Icon=UIIconAtlas.Utility("shard"),Tint=jade,Description="通关宝箱 · 星烬碎片 ×"+AdventureRewardRules.Materials(mode,tier)+"\n用于宝石与机制变体；章节完成奖励另列。"});
   result.Add(new EntryRewardPreview{Key="gold",Clear=true,Rarity=Rarity.Common,Name="金币",Icon=UIIconAtlas.Reward(0),Tint=gold,Description="通关宝箱金币："+TierRewardRules.ChestGoldMinimum(tier)+"～"+(TierRewardRules.ChestGoldMinimum(tier)+40)});
   if(mode==-1&&!chapter)foreach(var rarity in DropPreviewRules.ClearRarities(-1,tier))foreach(var slot in new[]{FashionSlot.Weapon,FashionSlot.Wings})
    result.Add(new EntryRewardPreview{Key="fashion:"+slot+":"+rarity,Clear=true,Rarity=Rarity.Legendary,Name=ProgressionService.FashionName(slot,rarity,session.Progression.Profile.heroClass)+" · 传说",Icon=UIIconAtlas.FashionCardIcon(slot,(int)rarity,session.Progression.Profile.heroClass),Tint=GameBalance.RarityColor(Rarity.Legendary),Description="通关宝箱必得一个传说外观，此图为可能部位与外观款式（保存 appearanceTier 身份）。\n重复外观转为资源，不是每图各送一个外观。"});
   for(int slotIndex=0;slotIndex<3;slotIndex++)foreach(var rarity in DropPreviewRules.EnemyRarities(enemyTier,hasBoss,false))
   {
    var slot=(ItemSlot)slotIndex;
    result.Add(new EntryRewardPreview{Key="enemy:"+slot+":"+rarity,Rarity=rarity,Name=GameBalance.SlotName(slot)+" · "+GameBalance.RarityName(rarity),Icon=UIIconAtlas.EquipmentCardIcon(slot,level,rarity,session.Progression.Profile.heroClass),Tint=GameBalance.RarityColor(rarity),Description="敌人可能掉落 · "+GameBalance.SlotName(slot)+" · "+GameBalance.RarityName(rarity)+"\n当前等级档 Lv."+level+"，基础属性在获得时生成；不是通关保底。"});
   }
   foreach(var mechanic in BuildCatalog.MechanicsFor(session.Progression.Profile.heroClass))foreach(var rarity in DropPreviewRules.EnemyRarities(enemyTier,hasBoss,true))
   {
    string effect=BuildCatalog.MechanicDescription(mechanic);int variant=effect.IndexOf("变体");if(variant>0)effect=effect.Substring(0,variant).Trim();
    result.Add(new EntryRewardPreview{Key="mechanic:"+mechanic+":"+rarity,Rarity=rarity,Name=BuildCatalog.MechanicName(mechanic)+" · "+GameBalance.RarityName(rarity),Icon=UIIconAtlas.EquipmentCardIcon(BuildCatalog.MechanicSlot(mechanic),level,rarity,session.Progression.Profile.heroClass),Tint=GameBalance.RarityColor(rarity),Description="敌人可能掉落 · "+BuildCatalog.MechanicName(mechanic)+" · "+GameBalance.RarityName(rarity)+"\n当前等级档 Lv."+level+"\n"+effect});
   }
   return result;
  }
  private float DrawEntryRewardPreviews(float available,float u,int mode,int tier,bool chapter,bool draw)
  {
   string context=chapter?"chapter"+session.SelectedChapterNode+":"+session.SelectedChapterDifficulty+":"+session.SelectedChapterTier:mode+":"+tier;
   if(entryRewardContext!=context){entryRewardContext=context;entryRewardSelection=null;}
   var items=EntryRewardPreviews(mode,tier,chapter);int columns=Mathf.Max(1,Mathf.FloorToInt(available/82));float cell=available/columns;
   float end=0;
   for(int group=0;group<2;group++)
   {
    bool clear=group==0;var section=items.FindAll(item=>item.Clear==clear);
    if(draw)Text(new Rect(8*u,end*u,(available-16)*u,26*u),clear?"通关宝箱 · 必得装备，品质随机":"敌人掉落 · 可能获得",Mathf.RoundToInt(13*u),clear?gold:jade,true);
    end+=30;
    for(int row=0;row*columns<section.Count;row++)
    {
     float labelHeight=28;
     for(int col=0;col<columns&&row*columns+col<section.Count;col++)
      labelHeight=Mathf.Max(labelHeight,Style(Mathf.RoundToInt(11*u),false,true).CalcHeight(new GUIContent(section[row*columns+col].Name),(cell-8)*u)/u);
     float rowHeight=52+labelHeight+6;
     for(int col=0;col<columns&&row*columns+col<section.Count;col++)
     {
      var item=section[row*columns+col];Rect hit=new Rect((col*cell+4)*u,end*u,(cell-8)*u,(rowHeight-6)*u);
      if(!draw)continue;
      Rect icon=new Rect(hit.center.x-24*u,hit.y,48*u,48*u);Fill(icon,card);Border(icon,item.Tint,2*u);
      DrawIcon(new Rect(icon.x+5*u,icon.y+5*u,38*u,38*u),item.Icon,item.Tint);
      for(int mark=0;mark<=(int)item.Rarity;mark++)Fill(new Rect(icon.x+4*u+mark*6*u,icon.y+3*u,4*u,3*u),pale);
      Text(new Rect(hit.x,hit.y+50*u,hit.width,labelHeight*u),item.Name,Mathf.RoundToInt(11*u),pale,false,true,TextAnchor.MiddleCenter);
      if(!MobileControls.Active&&entryRewardViewport.Contains(Mouse)&&hit.Contains(Event.current.mousePosition)&&GUI.enabled)tooltip=item.Description;
      if(MobileControls.Active&&GUI.Button(hit,GUIContent.none,invisibleButton))entryRewardSelection=entryRewardSelection==item.Key?null:item.Key;
     }
     end+=rowHeight;
    }
    end+=8;
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
     if(PopupCloseButton(new Rect(box.xMax-44*u,box.y,44*u,44*u)))entryRewardSelection=null;
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
   if(PopupCloseButton(new Rect((l.X+l.Frame.Width-44)*u,l.Y*u,44*u,36*u))){adventureChapterSelected=false;session.CancelDungeonSelection();BlockUITransition();return;}
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
   adventureDetailScroll=BeginTouchScroll("adventure-detail",AdventureRect(l.Details,u),adventureDetailScroll,new Rect(0,0,contentWidth*u,Mathf.Max(l.Details.Height*u,(48+DrawEntryRewardPreviews(contentWidth,u,mode,tier,false,false))*u)));
   Text(new Rect(8*u,4*u,(contentWidth-12)*u,32*u),detail,Mathf.RoundToInt(18*u),pale,true);
   if(!adventureChapterSelected){entryRewardViewport=AdventureRect(l.Details,u);GUI.BeginGroup(new Rect(0,48*u,contentWidth*u,DrawEntryRewardPreviews(contentWidth,u,mode,tier,false,false)*u));DrawEntryRewardPreviews(contentWidth,u,mode,tier,false,true);GUI.EndGroup();}
   EndTouchScroll();
   float x=l.X,y=l.FooterY;

   bool normal=!adventureChapterSelected;
   if(Button(new Rect((x)*u,y*u,44*u,48*u),"−",jade,normal&&tier>1))session.SelectedDungeonTier--;
   Text(new Rect((x+44)*u,y*u,80*u,48*u),"第"+tier+"阶",Mathf.RoundToInt(13*u),gold,true,false,TextAnchor.MiddleCenter);
   if(Button(new Rect((x+124)*u,y*u,44*u,48*u),"+",jade,normal&&tier<session.MaximumDungeonTier))session.SelectedDungeonTier++;
   if(PrimaryButton(new Rect((x+176)*u,y*u,(l.Frame.Width-176)*u,48*u),adventureChapterSelected?"选择章节":"进入挑战",gold))
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
