using UnityEngine;
namespace Emberfall
{
 public sealed partial class GameUI
 {
  private string entryRewardSelection,entryRewardContext,entryRewardHoverKey;
  private Rect entryRewardViewport,entryRewardPopupRect,entryRewardAnchor;
  private EntryRewardPreview entryRewardPopup;
  private Vector2 entryRewardScreenAnchor,entryRewardScreenEnd,entryRewardPopupScroll;
  private bool entryRewardPopupVisible,entryRewardAnchorIsRoot;
  private Vector2 entryRewardContentOrigin;
  private void BeginEntryRewardPopup()
  {
   entryRewardPopup=null;entryRewardAnchorIsRoot=false;
   if(!MobileControls.Active||!entryRewardPopupVisible||Event.current.type!=EventType.MouseDown)return;
   Vector2 point=Event.current.mousePosition;
   if(entryRewardPopupRect.Contains(point))
   {
    if(new Rect(entryRewardPopupRect.xMax-44*TouchRatio,entryRewardPopupRect.y,44*TouchRatio,44*TouchRatio).Contains(point))entryRewardSelection=null;
    Event.current.Use();
   }
   else if(!entryRewardAnchor.Contains(point))entryRewardSelection=null;
  }
  private void DrawEntryRewardPopup()
  {
   entryRewardPopupVisible=entryRewardPopup!=null;
   if(entryRewardPopup==null)return;
   float u=MobileControls.Active?TouchRatio:1f;
   if(!entryRewardAnchorIsRoot){Vector2 at=GUIUtility.ScreenToGUIPoint(entryRewardScreenAnchor),end=GUIUtility.ScreenToGUIPoint(entryRewardScreenEnd);entryRewardAnchor=new Rect(at.x,at.y,end.x-at.x,end.y-at.y);}
   if(!entryRewardViewport.Overlaps(entryRewardAnchor)){entryRewardPopupVisible=false;return;}
   float w=Mathf.Min(320*u,width-24*u),textWidth=w-40*u;
   float contentHeight=DrawEntryRewardRows(entryRewardPopup,textWidth/u,u,false)*u;
   float h=Mathf.Min(contentHeight+108*u,height-24*u);
   float x=entryRewardAnchor.xMax+10*u;
   if(x+w>width-12*u)x=entryRewardAnchor.x-w-10*u;
   x=Mathf.Clamp(x,12*u,Mathf.Max(12*u,width-w-12*u));
   float y=Mathf.Clamp(entryRewardAnchor.y,12*u,Mathf.Max(12*u,height-h-12*u));
   Rect box=new Rect(x,y,w,h);entryRewardPopupRect=box;blockedRects.Add(box);
   Fill(box,ink);Border(box,entryRewardPopup.Tint,2*u);
   DrawEntryRewardIcon(new Rect(x+12*u,y+12*u,48*u,48*u),entryRewardPopup,u);
   Text(new Rect(x+70*u,y+10*u,w-122*u,44*u),entryRewardPopup.Name,Mathf.RoundToInt(14*u),entryRewardPopup.Tint,true,true);
   string level=System.Text.RegularExpressions.Regex.Match(entryRewardPopup.Description,@"Lv\.?\d+").Value;
   string[] tags={EntryRewardKind(entryRewardPopup),GameBalance.RarityName(entryRewardPopup.Rarity),level};
   float tagX=x+12*u;
   foreach(string tag in tags)if(tag.Length>0)
   {float tagW=Mathf.Max(48,tag.Length*12+16)*u;Rect chip=new Rect(tagX,y+66*u,tagW,23*u);Fill(chip,new Color(entryRewardPopup.Tint.r,entryRewardPopup.Tint.g,entryRewardPopup.Tint.b,.18f));Text(chip,tag,Mathf.RoundToInt(10*u),pale,true,false,TextAnchor.MiddleCenter);tagX+=tagW+6*u;}
   if(MobileControls.Active&&PopupCloseButton(new Rect(box.xMax-44*u,y,44*u,44*u)))entryRewardSelection=null;
   entryRewardPopupScroll=BeginTouchScroll("entry-reward-popup",new Rect(x+12*u,y+98*u,w-24*u,h-108*u),entryRewardPopupScroll,new Rect(0,0,textWidth,contentHeight));
   DrawEntryRewardRows(entryRewardPopup,textWidth/u,u,true);
   EndTouchScroll();
  }
  private string GemDropDescription(EquipmentMechanic gem,bool preview)
  {return BuildCatalog.GemName(gem)+"\n"+GameBalance.ClassName(BuildCatalog.MechanicClass(gem))+"专用 · "+GameBalance.SlotName(BuildCatalog.MechanicSlot(gem))+"槽\n"+(preview?"通关宝箱必得整件；已拥有则转为3枚星烬碎片。\n":"")+"机制\n"+BuildCatalog.MechanicDescription(gem);}
  private void InspectRewardItem(Rect hit,EntryRewardPreview item)
  {
   if(MobileControls.Active&&GUI.Button(hit,GUIContent.none,invisibleButton)){entryRewardSelection=entryRewardSelection==item.Key?null:item.Key;entryRewardPopupScroll=Vector2.zero;}
   bool hover=hit.Contains(Event.current.mousePosition)&&GUI.enabled;
   if(!MobileControls.Active&&hover)entryRewardHoverKey=item.Key;
   if(MobileControls.Active?entryRewardSelection==item.Key:hover||entryRewardPopupVisible&&entryRewardHoverKey==item.Key&&entryRewardPopupRect.Contains(Mouse))
   {
    tooltip=null;entryRewardAnchorIsRoot=false;entryRewardViewport=new Rect(0,0,width,height);entryRewardPopup=item;
    entryRewardScreenAnchor=GUIUtility.GUIToScreenPoint(hit.position);entryRewardScreenEnd=GUIUtility.GUIToScreenPoint(new Vector2(hit.xMax,hit.yMax));
   }
  }
  private EntryRewardPreview ActualEquipmentPreview(ItemData item)
  {
   string description=item.name+"\n"+GameBalance.RarityName(item.rarity)+" · Lv"+item.level+"\n实际属性\n攻击  "+item.attack+"\n防御  "+item.defense+"\n生命  "+item.health;
   if(item.criticalChance>0)description+="\n暴击率  "+(item.criticalChance*100).ToString("0.##")+"%";
   if(item.criticalDamageBonus>0)description+="\n暴击伤害  "+(item.criticalDamageBonus*100).ToString("0.##")+"%";
   description+="\n机制\n"+(item.mechanic==EquipmentMechanic.None?"无特殊机制":BuildCatalog.MechanicDescription(item.mechanic));
   return new EntryRewardPreview{Key="obtained:"+item.id,Name=item.name,Description=description,Rarity=item.rarity,Tint=GameBalance.RarityColor(item.rarity),Icon=UIIconAtlas.EquipmentCardIcon(item.slot,item.level,item.rarity,session.Progression.Profile.heroClass)};
  }
  private string EntryRewardKind(EntryRewardPreview item)
  {return item.Key.StartsWith("fashion:")?"时装":item.Key.StartsWith("gem:")?"宝石":item.Key=="shard"?"材料":item.Key=="gold"?"货币":"装备";}
  private void DrawEntryRewardIcon(Rect icon,EntryRewardPreview item,float u)
  {
   bool fashion=item.Key.StartsWith("fashion:");
   if(fashion){DrawIcon(icon,UIIconAtlas.ControlDisc(),new Color(.23f,.10f,.33f));DrawIcon(icon,UIIconAtlas.ControlRing(),new Color(.87f,.65f,1));}
   else {Fill(icon,card);Border(icon,item.Tint,2*u);}
   DrawIcon(new Rect(icon.x+7*u,icon.y+5*u,34*u,34*u),item.Icon,item.Tint);
   Rect label=new Rect(icon.x+2*u,icon.yMax-14*u,icon.width-4*u,12*u);Fill(label,fashion?new Color(.36f,.16f,.48f):new Color(.08f,.15f,.19f));
   Text(label,EntryRewardKind(item),Mathf.RoundToInt(9*u),fashion?new Color(.94f,.78f,1):pale,true,false,TextAnchor.MiddleCenter);
  }
  private float DrawEntryRewardRows(EntryRewardPreview item,float available,float u,bool draw)
  {
   float y=0;string[] lines=item.Description.Split('\n');
   for(int i=1;i<lines.Length;i++)
   foreach(string raw in (item.Key.StartsWith("fashion:")?lines[i].Split(new[]{" · "},System.StringSplitOptions.RemoveEmptyEntries):new[]{lines[i]}))
   {
    string line=raw.Trim();if(line.Length==0||line.StartsWith("Lv")||line==GameBalance.RarityName(item.Rarity)||line=="兵装"||line=="羽翼")continue;
    bool section=line.StartsWith("实际属性")||line.StartsWith("基础属性")||line.StartsWith("随机附加")||line.StartsWith("机制");
    var stat=System.Text.RegularExpressions.Regex.Match(line,@"^(攻击|防御|生命|暴击率|暴击伤害|数量)[ ：]*(.+)$");
    if(stat.Success)
    {
     if(draw){Rect row=new Rect(0,y*u,available*u,30*u);Fill(row,new Color(.07f,.12f,.16f));string name=stat.Groups[1].Value;
      DrawIcon(new Rect(5*u,(y+5)*u,20*u,20*u),UIIconAtlas.Utility(name=="攻击"?"attack":name=="防御"?"defense":name=="生命"?"health":name=="数量"?"shard":"core"),jade);
      Text(new Rect(30*u,y*u,90*u,30*u),name,Mathf.RoundToInt(11*u),muted,false,false,TextAnchor.MiddleLeft);
      Text(new Rect(120*u,y*u,(available-126)*u,30*u),stat.Groups[2].Value,Mathf.RoundToInt(12*u),pale,true,false,TextAnchor.MiddleRight);}
     y+=34;continue;
    }
    float h=Mathf.Max(24*u,Style(Mathf.RoundToInt(section?12*u:11*u),section,true).CalcHeight(new GUIContent(line),(available-16)*u)+8*u);
    if(draw){if(section)Fill(new Rect(0,(y+5)*u,3*u,h-10*u),item.Tint);Text(new Rect(8*u,y*u,(available-16)*u,h),line,Mathf.RoundToInt(section?12*u:11*u),section?item.Tint:muted,section,true);}
    y+=h/u+4;
   }
   return y;
  }
  private sealed class EntryRewardPreview
  {
   public string Key,Name,Description;public Texture2D Icon;public Color Tint;public Rarity Rarity;public bool Clear;
  }
  private readonly System.Collections.Generic.Dictionary<string,System.Collections.Generic.List<EntryRewardPreview>> entryPreviewCache = new System.Collections.Generic.Dictionary<string,System.Collections.Generic.List<EntryRewardPreview>>();
  private System.Collections.Generic.List<EntryRewardPreview> EntryRewardPreviews(int mode,int tier,bool chapter)
  {
   string cacheKey=session.Progression.CurrentSlotId+":"+session.Progression.Profile.heroClass+":"+session.Progression.Profile.level+":"+mode+":"+tier+":"+chapter+":"+session.SelectedChapterNode+":"+session.SelectedChapterDifficulty+":"+session.SelectedChapterTier;
   System.Collections.Generic.List<EntryRewardPreview> cached;
   if(entryPreviewCache.TryGetValue(cacheKey,out cached))return cached;
   var result=new System.Collections.Generic.List<EntryRewardPreview>();int level=chapter?ProgressionService.EquipmentGenerationLevel(session.Progression.Profile.level):AdventureRewardRules.DungeonLevel(tier);
   int enemyTier=chapter?session.SelectedChapterTier:tier;
   bool hasBoss=chapter?session.SelectedChapterNode==ChapterNode.StarPlatform:mode==-1||mode==2||mode==3;
   // Each reachable quality has its own identity and graphic. Slot count comes from the completed-tier table.
   for(int slotIndex=0;slotIndex<3;slotIndex++)
   {
    var slot=(ItemSlot)slotIndex;int count=DropPreviewRules.SlotCount(mode,tier,slot);if(count==0)continue;
    foreach(var rarity in DropPreviewRules.ClearRarities(mode,tier))
     result.Add(new EntryRewardPreview{Key="clear:"+slot+":"+rarity,Clear=true,Rarity=rarity,Name=GameBalance.SlotName(slot),Icon=UIIconAtlas.EquipmentCardIcon(slot,level,rarity,session.Progression.Profile.heroClass),Tint=GameBalance.RarityColor(rarity),Description=ProgressionService.EquipmentDropPreview(slot,rarity,level)});
   }
   if(chapter&&session.SelectedChapterNode==ChapterNode.StarPlatform)
   foreach(var gem in BuildCatalog.GemsFor(session.Progression.Profile.heroClass))foreach(var rarity in new[]{Rarity.Epic,Rarity.Legendary})
    result.Add(new EntryRewardPreview{Key="gem:"+gem+":"+rarity,Clear=true,Rarity=rarity,Name=BuildCatalog.GemName(gem),Icon=UIIconAtlas.Utility("gem"),Tint=GameBalance.RarityColor(rarity),Description=BuildCatalog.GemName(gem)+"\n星台封印专属产出 · 每次通关随机1颗\n史诗80% · 传说20%\n"+BuildCatalog.MechanicDescription(gem)});
   if(chapter)result.Add(new EntryRewardPreview{Key="refinement",Clear=true,Rarity=Rarity.Epic,Name="装备洗练石",Icon=UIIconAtlas.Utility("gem"),Tint=jade,Description="装备洗练石 × "+ProgressionService.ChapterRefinementStones(session.SelectedChapterNode,tier)+"\n铁匠洗练：只升不降，最高达到装备数值上限。\n主要产地：赤岩断供。"});
   result.Add(new EntryRewardPreview{Key="shard",Clear=true,Rarity=Rarity.Rare,Name="星烬碎片",Icon=UIIconAtlas.Utility("shard"),Tint=jade,Description="星烬碎片\n数量："+AdventureRewardRules.Materials(mode,tier)+"\n用于机制宝石兑换、升阶与升华。"});
   result.Add(new EntryRewardPreview{Key="gold",Clear=true,Rarity=Rarity.Common,Name="金币",Icon=UIIconAtlas.Reward(0),Tint=gold,Description="金币\n数量："+TierRewardRules.ChestGoldMinimum(tier)+"～"+(TierRewardRules.ChestGoldMinimum(tier)+40)});
   if(mode==-1&&!chapter)foreach(var rarity in new[]{Rarity.Legendary})foreach(var slot in new[]{FashionSlot.Weapon,FashionSlot.Wings})
    result.Add(new EntryRewardPreview{Key="fashion:"+slot+":"+rarity,Clear=true,Rarity=rarity,Name=ProgressionService.FashionName(slot,rarity,session.Progression.Profile.heroClass),Icon=UIIconAtlas.FashionCardIcon(slot,(int)rarity,session.Progression.Profile.heroClass),Tint=GameBalance.RarityColor(rarity),Description=ProgressionService.FashionName(slot,rarity,session.Progression.Profile.heroClass)+"\n"+(slot==FashionSlot.Weapon?"兵装":"羽翼")+" · "+GameBalance.RarityName(rarity)+"\n"+ProgressionService.FashionBonus(slot,rarity)});
   if(entryPreviewCache.Count>=64)entryPreviewCache.Clear();
   entryPreviewCache[cacheKey]=result;return result;
  }
  private float DrawEntryRewardPreviews(float available,float u,int mode,int tier,bool chapter,bool draw)
  {
   string context=chapter?"chapter"+session.SelectedChapterNode+":"+session.SelectedChapterDifficulty+":"+session.SelectedChapterTier:mode+":"+tier;
   if(entryRewardContext!=context){entryRewardContext=context;entryRewardSelection=null;entryRewardHoverKey=null;entryRewardPopupScroll=Vector2.zero;}
   var items=EntryRewardPreviews(mode,tier,chapter);int columns=Mathf.Max(1,Mathf.FloorToInt(available/82));float cell=available/columns;
   float end=0;
   for(int group=0;group<1;group++)
   {
    bool clear=group==0;var section=items.FindAll(item=>item.Clear==clear);
    if(draw)Text(new Rect(8*u,end*u,(available-16)*u,26*u),"通关宝箱 · 三选一",Mathf.RoundToInt(13*u),clear?gold:jade,true);
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
      Rect icon=new Rect(hit.center.x-24*u,hit.y,48*u,48*u);DrawEntryRewardIcon(icon,item,u);
      Text(new Rect(hit.x,hit.y+50*u,hit.width,labelHeight*u),item.Name,Mathf.RoundToInt(11*u),pale,false,true,TextAnchor.MiddleCenter);
      if(MobileControls.Active&&GUI.Button(hit,GUIContent.none,invisibleButton)){entryRewardSelection=entryRewardSelection==item.Key?null:item.Key;entryRewardPopupScroll=Vector2.zero;}
      Rect rootHit=new Rect(entryRewardContentOrigin.x+hit.x,entryRewardContentOrigin.y+hit.y,hit.width,hit.height);
      bool hover=entryRewardViewport.Contains(Mouse)&&rootHit.Contains(Mouse)&&GUI.enabled;
      if(!MobileControls.Active&&hover)entryRewardHoverKey=item.Key;
      bool show=MobileControls.Active?entryRewardSelection==item.Key:hover||entryRewardPopupVisible&&entryRewardHoverKey==item.Key&&entryRewardPopupRect.Contains(Mouse);
      if(show)
      {
       entryRewardPopup=item;entryRewardAnchorIsRoot=true;
       entryRewardAnchor=new Rect(entryRewardContentOrigin.x+icon.x,entryRewardContentOrigin.y+icon.y,icon.width,icon.height);
      }
     }
     end+=rowHeight;
    }
    end+=8;
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
    {adventureChapterSelected=i==5;if(i<5)session.SelectedArenaMode=i-1;adventureDetailScroll=Vector2.zero;adventureRewardHint=null;CancelMobileScroll();}
   }
   EndTouchScroll();
   if(adventureChapterSelected)
   {
    DrawInlineChapterEntry(AdventureRect(l.Details,u),u);
    if(PrimaryButton(new Rect(l.Details.X*u,l.FooterY*u,l.Details.Width*u,48*u),"进入 "+ChapterDefinition.Get(session.SelectedChapterNode).Name,gold,ChapterProgression.IsUnlocked(session.Progression.Profile,session.SelectedChapterNode)))ConfirmSelectedChapter();
    return;
   }
   int mode=session.SelectedArenaMode,tier=session.SelectedDungeonTier;float contentWidth=l.Details.Width-18;
   string detail=adventureChapterSelected?"星路章节 · 双印路线":names[mode+1]+" · Lv"+AdventureRewardRules.DungeonLevel(tier);
   float rewardHeight=adventureChapterSelected?0:DrawEntryRewardPreviews(contentWidth,u,mode,tier,false,false);
   adventureDetailScroll=BeginTouchScroll("adventure-detail",AdventureRect(l.Details,u),adventureDetailScroll,new Rect(0,0,contentWidth*u,Mathf.Max(l.Details.Height*u,(48+rewardHeight)*u)));
   Text(new Rect(8*u,4*u,(contentWidth-12)*u,32*u),detail,Mathf.RoundToInt(18*u),pale,true);
   if(!adventureChapterSelected){entryRewardViewport=AdventureRect(l.Details,u);entryRewardContentOrigin=new Vector2(entryRewardViewport.x-adventureDetailScroll.x,entryRewardViewport.y+48*u-adventureDetailScroll.y);GUI.BeginGroup(new Rect(0,48*u,contentWidth*u,rewardHeight*u));DrawEntryRewardPreviews(contentWidth,u,mode,tier,false,true);GUI.EndGroup();}
   EndTouchScroll();
   float x=l.X,y=l.FooterY;

   bool normal=!adventureChapterSelected;
   if(Button(new Rect((x)*u,y*u,44*u,48*u),"−",jade,normal&&tier>1))session.SelectedDungeonTier--;
   Text(new Rect((x+44)*u,y*u,80*u,48*u),"Lv"+AdventureRewardRules.DungeonLevel(tier),Mathf.RoundToInt(13*u),gold,true,false,TextAnchor.MiddleCenter);
   if(Button(new Rect((x+124)*u,y*u,44*u,48*u),"+",jade,normal&&tier<session.MaximumDungeonTier))session.SelectedDungeonTier++;
   if(PrimaryButton(new Rect((x+176)*u,y*u,(l.Frame.Width-176)*u,48*u),"进入挑战",gold))
   {session.ConfirmDungeonSelection();}
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
