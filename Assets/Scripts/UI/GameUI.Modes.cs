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
   if(suppressRewardHover&&!dismissedRewardAnchor.Contains(Mouse))suppressRewardHover=false;
   if((!MobileControls.Active&&!(entryRewardSelection??" ").StartsWith("merchant:"))||!entryRewardPopupVisible||Event.current.type!=EventType.MouseDown)return;
   Vector2 point=Event.current.mousePosition;
   if(entryRewardPopupRect.Contains(point))
   {
    if(new Rect(entryRewardPopupRect.xMax-22*(MobileControls.Active?TouchRatio:1),entryRewardPopupRect.y,22*(MobileControls.Active?TouchRatio:1),22*(MobileControls.Active?TouchRatio:1)).Contains(point))entryRewardSelection=null;
    if(entryRewardSelection==null)Event.current.Use();
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
   bool comparing=entryRewardPopup.GearSlot.HasValue||entryRewardPopup.AppearanceSlot.HasValue;
   bool compact=CompactRewardItem(entryRewardPopup);
   float w=Mathf.Min((comparing?(IsWornReward(entryRewardPopup)?340:620):compact?InventoryGridGeometry.ItemPopupWidth:320)*u,width-24*u),textWidth=w-40*u;
   float contentHeight=DrawRewardDetailRows(entryRewardPopup,textWidth/u,u,false)*u;
   float headerHeight=comparing?0:compact?72:98;
   float comparisonHeight=0;
   float h=Mathf.Min(compact?InventoryGridGeometry.ItemPopupHeight*u:contentHeight+(headerHeight+10)*u+comparisonHeight,height-24*u);
   float x=entryRewardAnchor.xMax+10*u;
   if(x+w>width-12*u)x=entryRewardAnchor.x-w-10*u;
   x=Mathf.Clamp(x,12*u,Mathf.Max(12*u,width-w-12*u));
   float y=Mathf.Clamp(entryRewardAnchor.y,12*u,Mathf.Max(12*u,height-h-12*u));
   Rect box=new Rect(x,y,w,h);entryRewardPopupRect=box;blockedRects.Add(box);
   if(!comparing){Fill(box,ink);Border(box,entryRewardPopup.QualityColor,2*u);}
   if(compact)
   {
    var quantity=System.Text.RegularExpressions.Regex.Match(entryRewardPopup.Description??"",@"数量[ ：]*(.+)");
    Text(new Rect(x+w-120*u,y+4*u,94*u,20*u),"数量："+(quantity.Success?quantity.Groups[1].Value:entryRewardPopup.Quantity.ToString()),Mathf.RoundToInt(11*u),pale,true,false,TextAnchor.MiddleRight);
    DrawIcon(new Rect(x+10*u,y+27*u,32*u,32*u),entryRewardPopup.Icon,entryRewardPopup.QualityColor);
    Text(new Rect(x+50*u,y+24*u,w-66*u,24*u),entryRewardPopup.Name,Mathf.RoundToInt(13*u),pale,true,false,TextAnchor.MiddleLeft);
    DrawDetailTag(new Rect(x+50*u,y+48*u,62*u,21*u),GameBalance.RarityName(entryRewardPopup.Rarity),entryRewardPopup.QualityColor,u);
   }
   else if(!comparing)
   {
   DrawEntryRewardIcon(new Rect(x+12*u,y+12*u,48*u,48*u),entryRewardPopup,u);
   Text(new Rect(x+70*u,y+10*u,w-122*u,44*u),entryRewardPopup.Name,Mathf.RoundToInt(14*u),entryRewardPopup.QualityColor,true,true);
   string level=System.Text.RegularExpressions.Regex.Match(entryRewardPopup.Description,@"Lv\.?\d+").Value;
   string[] tags={EntryRewardKind(entryRewardPopup),GameBalance.RarityName(entryRewardPopup.Rarity),level};
   float tagX=x+12*u;
   foreach(string tag in tags)if(tag.Length>0)
   {float tagW=Mathf.Max(48,tag.Length*12+16)*u;Rect chip=new Rect(tagX,y+66*u,tagW,23*u);Fill(chip,new Color(entryRewardPopup.QualityColor.r,entryRewardPopup.QualityColor.g,entryRewardPopup.QualityColor.b,.18f));Text(chip,tag,Mathf.RoundToInt(10*u),pale,true,false,TextAnchor.MiddleCenter);tagX+=tagW+6*u;}
   }
   entryRewardPopupScroll=BeginTouchScroll("entry-reward-popup",new Rect(x+12*u,y+headerHeight*u+comparisonHeight,w-24*u,h-(headerHeight+10)*u-comparisonHeight),entryRewardPopupScroll,new Rect(0,0,textWidth,contentHeight));
   DrawRewardDetailRows(entryRewardPopup,textWidth/u,u,true);
   EndTouchScroll();
   if((MobileControls.Active||(entryRewardSelection??" ").StartsWith("merchant:"))&&PopupCloseButton(new Rect(box.xMax-22*u,y,22*u,22*u)))entryRewardSelection=null;
  }
  private string GemRewardDescription(EquipmentMechanic gem,Rarity rarity,int rank=0)
  {
   string attribute=BuildCatalog.IsAttributeGem(gem)?BuildCatalog.GemAttributeSummary(gem,rarity,rank):BuildCatalog.AttributeLabel(BuildCatalog.MechanicAttribute(gem))+" +"+(BuildCatalog.MechanicAttributeValue(gem,rank)*100).ToString("0.#")+"%";
   return BuildCatalog.GemName(gem)+"\n"+GameBalance.SlotName(BuildCatalog.MechanicSlot(gem))+"槽\n基础属性\n"+attribute;
  }
  private string GemDropDescription(EquipmentMechanic gem,bool preview)
  {return BuildCatalog.GemName(gem)+"\n"+GameBalance.ClassName(BuildCatalog.MechanicClass(gem))+"专用 · "+GameBalance.SlotName(BuildCatalog.MechanicSlot(gem))+"槽\n"+(preview?"通关宝箱必得整件；已拥有则转为3枚星烬碎片。\n":"")+"机制\n"+BuildCatalog.MechanicDescription(gem);}
  private void InspectRewardItem(Rect hit,EntryRewardPreview item,bool clickToInspect=false)
  {
   if(!MobileControls.Active&&suppressRewardHover)return;
   if((MobileControls.Active||clickToInspect)&&GUI.Button(hit,GUIContent.none,invisibleButton)){entryRewardSelection=entryRewardSelection==item.Key?null:item.Key;entryRewardPopupScroll=Vector2.zero;}
   bool hover=hit.Contains(Event.current.mousePosition)&&GUI.enabled;
   if(!MobileControls.Active&&hover)entryRewardHoverKey=item.Key;
   if(MobileControls.Active||clickToInspect?entryRewardSelection==item.Key:hover||entryRewardPopupVisible&&entryRewardHoverKey==item.Key&&entryRewardPopupRect.Contains(Mouse))
   {
    tooltip=null;entryRewardAnchorIsRoot=false;entryRewardViewport=new Rect(0,0,width,height);entryRewardPopup=item;
    entryRewardScreenAnchor=GUIUtility.GUIToScreenPoint(hit.position);entryRewardScreenEnd=GUIUtility.GUIToScreenPoint(new Vector2(hit.xMax,hit.yMax));
   }
  }
  private EntryRewardPreview ActualEquipmentPreview(ItemData item)
  {
   item=session.Progression.PreviewUpgrade(item,item.upgradeLevel);
   string description=ItemTitle(item)+"\n"+GameBalance.RarityName(item.rarity)+" · Lv"+item.level+"\n基础词条\n攻击  "+item.baseAttack+"\n防御  "+item.baseDefense+"\n生命  "+item.baseHealth;
   if(item.criticalChance>0)description+="\n暴击率  "+(item.criticalChance*100).ToString("0.##")+"%";
   if(item.criticalDamageBonus>0)description+="\n暴击伤害  "+(item.criticalDamageBonus*100).ToString("0.##")+"%";
   if(item.attackPercent>0)description+="\n攻击加成  "+(item.attackPercent*100).ToString("0.##")+"%";
   return new EntryRewardPreview{Key="obtained:"+item.id,Equipment=item,GearSlot=item.slot,Name=ItemTitle(item),Description=description,Rarity=item.rarity,Tint=GameBalance.RarityColor(item.rarity),Icon=UIIconAtlas.EquipmentCardIcon(item.slot,item.level,item.rarity,session.Progression.Profile.heroClass)};
  }
  private string EntryRewardKind(EntryRewardPreview item)
  {return item.Key.StartsWith("fashion:")?"时装":item.Key.Contains("gem:")?"宝石":item.Key=="shard"||item.Key=="thread"||(item.Key.Contains("refinement")||item.Key.Contains("affix-reforge"))?"材料":item.Key=="gold"?"货币":item.Key.Contains("potion")?"药剂":"装备";}
  private void DrawEntryRewardIcon(Rect icon,EntryRewardPreview item,float u)
  {
   bool fashion=item.Key.StartsWith("fashion:");
   if(fashion){DrawIcon(icon,UIIconAtlas.ControlDisc(),card);DrawIcon(icon,UIIconAtlas.ControlRing(),item.QualityColor);}
   else {Fill(icon,card);Border(icon,item.QualityColor,2*u);}
   float artSize=Mathf.Max(1,Mathf.Min(icon.width-12*u,icon.height-22*u));
   DrawIcon(new Rect(icon.center.x-artSize*.5f,icon.y+(icon.height-14*u-artSize)*.5f,artSize,artSize),item.Icon,item.QualityColor);
   Rect label=new Rect(icon.x+2*u,icon.yMax-14*u,icon.width-4*u,12*u);Fill(label,new Color(.08f,.15f,.19f));
   Text(label,EntryRewardKind(item),Mathf.RoundToInt(9*u),item.QualityColor,true,false,TextAnchor.MiddleCenter);
  }
  private bool CompactRewardItem(EntryRewardPreview item)
  {return (item.Key.Contains("refinement")||item.Key.Contains("affix-reforge"))||item.Key=="shard"||item.Key=="thread"||item.Key=="gold"||item.Key=="experience"||item.Key.Contains("potion");}
  private float DrawEntryRewardRows(EntryRewardPreview item,float available,float u,bool draw)
  {
   float y=0;string[] lines=item.Description.Split('\n');
   for(int i=1;i<lines.Length;i++)
   foreach(string raw in (item.Key.StartsWith("fashion:")?lines[i].Split(new[]{" · "},System.StringSplitOptions.RemoveEmptyEntries):new[]{lines[i]}))
   {
    string line=raw.Trim();if(line.Length==0||line.StartsWith("Lv")||line==GameBalance.RarityName(item.Rarity)||line=="兵装"||line=="羽翼")continue;
    if(CompactRewardItem(item)&&line.StartsWith("数量"))continue;
    bool section=line.StartsWith("实际属性")||line.StartsWith("基础属性")||line.StartsWith("随机附加")||line.StartsWith("机制");
    var stat=System.Text.RegularExpressions.Regex.Match(line,@"^(攻击加成|攻击|防御|生命|暴击率|暴击几率|暴击伤害|移动速度|受到伤害减免|技能冷却缩减|能量回复|护甲|数量)[ ：]*(.+)$");
    if(stat.Success)
    {
     if(draw){Rect row=new Rect(0,y*u,available*u,30*u);Fill(row,new Color(.07f,.12f,.16f));string name=stat.Groups[1].Value;
      DrawIcon(new Rect(5*u,(y+5)*u,20*u,20*u),UIIconAtlas.Utility(name=="攻击"?"attack":name=="防御"?"defense":name=="生命"?"health":name=="数量"?"shard":"core"),jade);
      Text(new Rect(30*u,y*u,Mathf.Min(76,available*.45f)*u,30*u),name,Mathf.RoundToInt(11*u),muted,false,false,TextAnchor.MiddleLeft);
      Text(new Rect((30+Mathf.Min(76,available*.45f))*u,y*u,Mathf.Max(36,available-36-Mathf.Min(76,available*.45f))*u,30*u),stat.Groups[2].Value,Mathf.RoundToInt(12*u),pale,true,false,TextAnchor.MiddleRight);}
     y+=34;continue;
    }
    if(CompactRewardItem(item))continue;
    float h=Mathf.Max(24*u,Style(Mathf.RoundToInt(section?12*u:11*u),section,true).CalcHeight(new GUIContent(line),(available-16)*u)+8*u);
    if(draw){if(section)Fill(new Rect(0,(y+5)*u,3*u,h-10*u),item.QualityColor);Text(new Rect(8*u,y*u,(available-16)*u,h),line,Mathf.RoundToInt(section?12*u:11*u),section?item.QualityColor:muted,section,true);}
    y+=h/u+4;
   }
   return y;
  }
  // Retain range/affix notes in previews while using the same stat rows for actual rewards.
  private bool IsWornReward(EntryRewardPreview item)
  {
   if(item.Equipment!=null)return IsEquipped(item.Equipment);
   if(item.AppearanceSlot.HasValue&&item.Key!=null){var worn=session.Progression.EquippedFashion(item.AppearanceSlot.Value);return worn!=null&&item.Key=="fashion:"+worn.id;}
   return false;
  }
  private float DrawRewardDetailRows(EntryRewardPreview item,float available,float u,bool draw)
  {
   if(CompactRewardItem(item)){if(draw)Text(new Rect(0,0,available*u,44*u),ResourceDescription(item.Key),Mathf.RoundToInt(12*u),muted,false,true);return 44;}
   if(!item.GearSlot.HasValue&&!item.AppearanceSlot.HasValue)return DrawEntryRewardRows(item,available,u,draw);
   string current="";ItemData currentEquipment=null;
   if(item.GearSlot.HasValue){var equipped=session.Progression.Equipped(item.GearSlot.Value);if(equipped!=null){var preview=ActualEquipmentPreview(equipped);current=preview.Description;currentEquipment=preview.Equipment;}}
   else {var equipped=session.Progression.EquippedFashion(item.AppearanceSlot.Value);if(equipped!=null)current=ProgressionService.FashionBonus(equipped);}
   var baseline=RewardStatValues(current);var selected=RewardStatValues(item.Description);
   foreach(var pair in baseline)if(!selected.ContainsKey(pair.Key))selected[pair.Key]=pair.Value.Contains("×")?"×100%":pair.Value.Contains("%")?"0%":"0";
   bool selectedWorn=IsWornReward(item);int firstColumn=selectedWorn?1:0;
   float gap=2,cardWidth=selectedWorn?available:(available-gap)*.5f,y=92;
   EntryRewardPreview worn=null;
   if(currentEquipment!=null)worn=ActualEquipmentPreview(currentEquipment);
   if(item.AppearanceSlot.HasValue){var f=session.Progression.EquippedFashion(item.AppearanceSlot.Value);if(f!=null)worn=new EntryRewardPreview{Name=f.name,Rarity=f.rarity,AppearanceSlot=f.slot};}
   if(draw){Fill(new Rect(0,0,available*u,DrawRewardDetailRows(item,available,u,false)*u),ink);if(!selectedWorn)Fill(new Rect(cardWidth*u,0,gap*u,DrawRewardDetailRows(item,available,u,false)*u),new Color(.15f,.29f,.31f));}
   if(draw)for(int col=firstColumn;col<2;col++){float x=(col-firstColumn)*(cardWidth+gap);Rect cardRect=new Rect(x*u,0,cardWidth*u,DrawRewardDetailRows(item,available,u,false)*u);Fill(cardRect,ink);Border(cardRect,col==0?muted:jade);}
   var progression=session.Progression;
   ItemData owned=item.Equipment==null?null:progression.Profile.inventory.Find(v=>v!=null&&v.id==item.Equipment.id);
   if(draw)for(int col=firstColumn;col<2;col++)
   {
    var value=col==0?worn:item;float x=(col-firstColumn)*(cardWidth+gap);Color accent=value==null?muted:GameBalance.RarityColor(value.Rarity);
    Fill(new Rect(x*u,0,cardWidth*u,86*u),new Color(.045f,.09f,.12f));
    Text(new Rect((x+10)*u,2*u,(cardWidth-20)*u,20*u),col==0||selectedWorn?"当前穿戴":"所选物品",Mathf.RoundToInt(10*u),col==0?muted:jade,true);
    string name=value==null?"未穿戴":string.IsNullOrEmpty(value.Name)?"所选时装":value.Name;
    string level=value==null?"":System.Text.RegularExpressions.Regex.Match(value.Description??"",@"Lv\.?\d+").Value;
    if(level.Length>0)name=level+"  "+name;
    Text(new Rect((x+10)*u,24*u,(cardWidth-20)*u,32*u),name,Mathf.RoundToInt(14*u),accent,true,true);
    if(value!=null){DrawDetailTag(new Rect((x+8)*u,60*u,60*u,22*u),GameBalance.RarityName(value.Rarity),accent,u);DrawDetailTag(new Rect((x+74)*u,60*u,Mathf.Max(36,cardWidth-82)*u,22*u),item.GearSlot.HasValue?GameBalance.SlotName(item.GearSlot.Value):item.AppearanceSlot==FashionSlot.Wings?"羽翼":"兵装",jade,u);}
   }
   FashionData ownedFashion=item.AppearanceSlot.HasValue&&item.Key!=null&&item.Key.StartsWith("fashion:")?progression.Profile.fashions.Find(v=>v!=null&&"fashion:"+v.id==item.Key):null;
   FashionData currentFashion=item.AppearanceSlot.HasValue?progression.EquippedFashion(item.AppearanceSlot.Value):null;
   FashionData selectedFashion=ownedFashion??(item.AppearanceSlot.HasValue?new FashionData{slot=item.AppearanceSlot.Value,rarity=item.Rarity}:null);
   for(int section=0;section<(item.GearSlot.HasValue?2:1);section++)
   {
    bool any=false;foreach(var stat in selected)if(!item.GearSlot.HasValue||(IsRandomEquipmentStat(stat.Key)?1:0)==section)any=true;
    if(!any)continue;
    if(draw)for(int col=firstColumn;col<2;col++){
     float x=(col-firstColumn)*(cardWidth+gap);
     Fill(new Rect(x*u,y*u,cardWidth*u,28*u),new Color(.07f,.13f,.16f));
     Fill(new Rect((x+8)*u,(y+8)*u,3*u,12*u),jade);
     Text(new Rect((x+18)*u,y*u,(cardWidth-24)*u,28*u),item.GearSlot.HasValue?(section==0?"基础词条":col==1&&item.Clear?"随机词条 · 最多"+ProgressionService.EquipmentAffixLimit(item.Rarity)+"项":"随机词条"):"基础属性 / 升阶加成",Mathf.RoundToInt(12*u),pale,true,false,TextAnchor.MiddleLeft);
    }
    y+=32;
    foreach(var pair in selected)
    {
     if(item.GearSlot.HasValue&&(IsRandomEquipmentStat(pair.Key)?1:0)!=section)continue;
     string before;if(!baseline.TryGetValue(pair.Key,out before))before=pair.Value.Contains("×")?"×100%":pair.Value.Contains("%")?"0%":"0";
     if(draw)for(int col=firstColumn;col<2;col++){
      float x=(col-firstColumn)*(cardWidth+gap);Fill(new Rect(x*u,y*u,cardWidth*u,34*u),card);
      DrawIcon(new Rect((x+8)*u,(y+9)*u,16*u,16*u),UIIconAtlas.Utility(pair.Key=="攻击"?"attack":pair.Key=="防御"?"defense":pair.Key=="生命"?"health":"core"),jade);
      Text(new Rect((x+30)*u,y*u,Mathf.Min(72,cardWidth*.28f)*u,34*u),pair.Key,Mathf.RoundToInt(11*u),muted,true,false,TextAnchor.MiddleLeft);
      if(item.AppearanceSlot.HasValue)
      {
       var f=col==0?currentFashion:selectedFashion;
       int basis=ProgressionService.FashionBaseStat(f,pair.Key),rank=ProgressionService.FashionRankStat(f,pair.Key);
       float start=34+Mathf.Min(72,cardWidth*.28f),space=cardWidth-start-6;
       string prefix=pair.Key=="暴击几率"?"×":"";
       DrawComparedValue(new Rect((x+start)*u,y*u,space*.55f*u,34*u),prefix+basis+"%",col==0||selectedWorn?0:basis.CompareTo(ProgressionService.FashionBaseStat(currentFashion,pair.Key)),u,false);
       DrawComparedValue(new Rect((x+start+space*.55f)*u,y*u,space*.45f*u,34*u),"+"+rank+"%",item.Clear||item.UnenhancedReward||col==0||selectedWorn?0:rank.CompareTo(ProgressionService.FashionRankStat(currentFashion,pair.Key)),u,true);
       continue;
      }
      int bonus=EquipmentStatBonus(col==0?currentEquipment:item.Equipment,pair.Key),oldBonus=EquipmentStatBonus(currentEquipment,pair.Key);
      bool bonusVisible=bonus>0&&!(item.Clear&&col==1)||!item.Clear&&!selectedWorn&&col==1&&oldBonus>0;
      float valueX=34+Mathf.Min(72,cardWidth*.28f),room=cardWidth-valueX-6;
      float valueWidth=bonusVisible?room*.56f:room;
      DrawComparedValue(new Rect((x+valueX)*u,y*u,valueWidth*u,34*u),col==0?before:pair.Value,col==0||selectedWorn?0:EquipmentComparisonPresentation.StatDirection(pair.Value,before),u,false);
      if(bonusVisible)DrawComparedValue(new Rect((x+valueX+valueWidth)*u,y*u,(room-valueWidth)*u,34*u),"+"+bonus,item.Clear||item.UnenhancedReward||col==0||selectedWorn?0:bonus.CompareTo(oldBonus),u,true);
     }
     y+=38;
    }
   }
   if(item.AllowActions&&(owned!=null||ownedFashion!=null))
   {
    if(draw)
    {
     float actionX=selectedWorn?0:cardWidth+gap;
     if(InventoryPictogramAction(new Rect((actionX+cardWidth*.15f)*u,y*u,cardWidth*.7f*u,40*u),selectedWorn?"卸下":"穿戴",UIIconAtlas.Utility("confirm"),selectedWorn||owned==null||owned.level<=progression.Profile.level,selectedWorn,true))
     {
      bool saved=owned!=null?(selectedWorn?progression.Unequip(owned.slot):progression.Equip(owned.id)):(selectedWorn?progression.UnequipFashion(ownedFashion.slot):progression.EquipFashion(ownedFashion.id));
      Feedback(saved,selectedWorn?"已卸下":"已穿戴");if(saved)RebuildBagItems();
     }
    }
    y+=48;
   }
   return y;
  }
  private void DrawDetailTag(Rect r,string label,Color tint,float u)
  {Fill(r,new Color(tint.r,tint.g,tint.b,.15f));Border(r,new Color(tint.r,tint.g,tint.b,.3f));Text(r,label,Mathf.RoundToInt(10*u),tint,true,false,TextAnchor.MiddleCenter);}
  private void DrawComparedValue(Rect r,string value,int direction,float u,bool enhancement)
  {
   Color tint=direction>0?new Color(.3f,.94f,.55f):direction<0?new Color(1f,.38f,.36f):enhancement?gold:pale;
   if(direction!=0)r.width-=18*u;
   int size=Mathf.RoundToInt(15*u);while(size>Mathf.RoundToInt(9*u)&&Style(size,true,false).CalcSize(new GUIContent(value)).x>r.width)size--;
   Text(r,value,size,tint,true,false,TextAnchor.MiddleLeft);
   if(direction!=0)DrawIcon(new Rect(r.x+Mathf.Min(r.width,Style(size,true,false).CalcSize(new GUIContent(value)).x)+2*u,r.center.y-7*u,14*u,14*u),UIIconAtlas.StatTrendArrow(direction>0),tint);
  }
  private static bool IsRandomEquipmentStat(string name)
  {return name=="攻击加成"||name=="暴击率"||name=="暴击伤害";}
  private static int EquipmentStatBonus(ItemData item,string name)
  {return item==null?0:name=="攻击"?item.attack-item.baseAttack:name=="防御"?item.defense-item.baseDefense:name=="生命"?item.health-item.baseHealth:0;}
  private void DrawEquipmentStatParts(Rect area,string basis,int bonus,float u,Color tint)
  {
   string extra=bonus>0?" +"+bonus:"";int size=Mathf.RoundToInt(11*u);
   while(size>Mathf.RoundToInt(8*u)&&Style(size,true,false).CalcSize(new GUIContent(basis+extra)).x>area.width)size--;
   float extraWidth=Style(size,true,false).CalcSize(new GUIContent(extra)).x;
   Text(new Rect(area.x,area.y,Mathf.Max(0,area.width-extraWidth),area.height),basis,size,tint,true,false,TextAnchor.MiddleRight);
   if(bonus>0)Text(new Rect(area.xMax-extraWidth,area.y,extraWidth,area.height),extra,size,jade,true,false,TextAnchor.MiddleRight);
  }
  private static System.Collections.Generic.Dictionary<string,string> RewardStatValues(string description)
  {
   var values=new System.Collections.Generic.Dictionary<string,string>();
   foreach(string raw in description.Split(new[]{"\n"," · "},System.StringSplitOptions.RemoveEmptyEntries)){
    var match=System.Text.RegularExpressions.Regex.Match(raw.Trim(),@"^(攻击加成|攻击|防御|生命|暴击率|暴击几率|暴击伤害|移动速度|受到伤害减免)[ ：]*(.+)$");
    if(match.Success)values[match.Groups[1].Value]=match.Groups[2].Value;
   }
   return values;
  }
  private sealed class EntryRewardPreview
  {
   public ItemData Equipment;public string Key,Name,Description;public Texture2D Icon;public Color Tint;public Color QualityColor {get{return GameBalance.RarityColor(Rarity);}}public Rarity Rarity;public bool Clear;public bool UnenhancedReward;public bool AllowActions;public int Quantity=1;public ItemSlot? GearSlot;public FashionSlot? AppearanceSlot;
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
     result.Add(new EntryRewardPreview{Key="clear:"+slot+":"+rarity,GearSlot=slot,Clear=true,Rarity=rarity,Name=GameBalance.SlotName(slot),Icon=UIIconAtlas.EquipmentCardIcon(slot,level,rarity,session.Progression.Profile.heroClass),Tint=GameBalance.RarityColor(rarity),Description=ProgressionService.EquipmentDropPreview(slot,rarity,level)});
   }
   if(chapter&&session.SelectedChapterNode==ChapterNode.StarPlatform)
   foreach(var gem in BuildCatalog.GemsFor(session.Progression.Profile.heroClass))foreach(var rarity in new[]{Rarity.Epic,Rarity.Legendary})
    result.Add(new EntryRewardPreview{Key="gem:"+gem+":"+rarity,Clear=true,Rarity=rarity,Name=BuildCatalog.GemName(gem),Icon=UIIconAtlas.Utility("gem"),Tint=GameBalance.RarityColor(rarity),Description=GemRewardDescription(gem,rarity)});
   if(!(chapter&&session.SelectedChapterNode==ChapterNode.StarPlatform))result.Add(new EntryRewardPreview{Key="refinement",Clear=true,Rarity=Rarity.Epic,Name="装备洗练石",Icon=UIIconAtlas.Utility("gem"),Tint=GameBalance.RarityColor(Rarity.Epic),Description="装备洗练石\n数量  "+ProgressionService.ChestStackMinimum(true,tier)+"～"+ProgressionService.ChestStackMaximum(true,tier)+"\n提高装备属性数值，不会降低。"});
   result.Add(new EntryRewardPreview{Key="shard",Clear=true,Rarity=Rarity.Rare,Name="星烬碎片",Icon=UIIconAtlas.Utility("shard"),Tint=jade,Description="星烬碎片\n数量："+ProgressionService.ChestStackMinimum(false,tier)+"～"+ProgressionService.ChestStackMaximum(false,tier)+"\n用于机制宝石兑换、升阶与升华。"});
   if(!(chapter&&session.SelectedChapterNode==ChapterNode.StarPlatform))foreach(var rarity in new[]{Rarity.Legendary})foreach(var slot in new[]{FashionSlot.Weapon,FashionSlot.Wings})
    result.Add(new EntryRewardPreview{Key="fashion:"+slot+":"+rarity,AppearanceSlot=slot,Clear=true,Rarity=rarity,Name=ProgressionService.FashionName(slot,rarity,session.Progression.Profile.heroClass),Icon=UIIconAtlas.FashionCardIcon(slot,(int)rarity,session.Progression.Profile.heroClass),Tint=GameBalance.RarityColor(rarity),Description=ProgressionService.FashionName(slot,rarity,session.Progression.Profile.heroClass)+"\n"+(slot==FashionSlot.Weapon?"兵装":"羽翼")+" · "+GameBalance.RarityName(rarity)+"\n"+ProgressionService.FashionBonus(slot,rarity)});
   var reforge=ResourceItemPreview("affix-reforge",1);reforge.Clear=true;result.Add(reforge);
   if(entryPreviewCache.Count>=64)entryPreviewCache.Clear();
   entryPreviewCache[cacheKey]=result;return result;
  }
  private float DrawEntryRewardPreviews(float available,float u,int mode,int tier,bool chapter,bool draw)
  {
   string context=chapter?"chapter"+session.SelectedChapterNode+":"+session.SelectedChapterDifficulty+":"+session.SelectedChapterTier:mode+":"+tier;
   if(entryRewardContext!=context){entryRewardContext=context;entryRewardSelection=null;entryRewardHoverKey=null;entryRewardPopupScroll=Vector2.zero;}
   var items=EntryRewardPreviews(mode,tier,chapter);int columns=Mathf.Max(1,Mathf.FloorToInt(available/82));float cell=available/columns;
   float end=0;
   for(int group=0;group<2;group++)
   {
    bool clear=group==1;var section=items.FindAll(item=>item.Clear==clear);if(section.Count==0)continue;
    if(draw){Text(new Rect(8*u,end*u,(available-16)*u,26*u),clear?"通关宝箱":"通关固定掉落",Mathf.RoundToInt(13*u),gold,true);if(clear)Text(new Rect(8*u,(end+26)*u,(available-16)*u,24*u),"传说装备 4% · "+(AdventureRewardRules.LegendaryPityChests-session.Progression.Profile.legendaryEquipmentMisses)+"次内必出",Mathf.RoundToInt(11*u),muted);}
    end+=clear?54:28;
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
      bool show=!suppressRewardHover&&(MobileControls.Active?entryRewardSelection==item.Key:hover||entryRewardPopupVisible&&entryRewardHoverKey==item.Key&&entryRewardPopupRect.Contains(Mouse));
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
  private Texture2D dungeonEntryAtlas;
  private void DrawDungeonEntryArtwork(Rect area,int index)
  {
   if(dungeonEntryAtlas==null)dungeonEntryAtlas=Resources.Load<Texture2D>("UI/DungeonEntryAtlas");
   Fill(area,card);
   if(dungeonEntryAtlas!=null)
   {
    index=Mathf.Clamp(index,0,7);Rect uv=new Rect((index%2)*.5f,1-(index/2+1)*.25f,.5f,.25f);
    float sourceAspect=dungeonEntryAtlas.width*.5f/(dungeonEntryAtlas.height*.25f),aspect=area.width/Mathf.Max(1,area.height);
    if(aspect>sourceAspect){float crop=uv.height*sourceAspect/aspect;uv.y+=(uv.height-crop)*.5f;uv.height=crop;}
    else {float crop=uv.width*aspect/sourceAspect;uv.x+=(uv.width-crop)*.5f;uv.width=crop;}
    GUI.DrawTextureWithTexCoords(area,dungeonEntryAtlas,uv);
   }
   Border(area,new Color(gold.r,gold.g,gold.b,.4f));
  }
  private Vector2 adventureListScroll,adventureDetailScroll;
  private bool adventureChapterSelected;
  private Rect AdventureRect(MobilePanelLayout.Area a,float u){return new Rect(a.X*u,a.Y*u,a.Width*u,a.Height*u);}
  private void DrawArenaSelection()
  {
   float u=MobileControls.Active?TouchRatio:1f,inset=MobileControls.Active?0:24;var l=new AdventureSelectionLayout(width/u,height/u,inset);
   blockedRects.Add(new Rect(0,0,width,height));
   Box(AdventureRect(l.Frame,u),jade,false);
   Text(new Rect((l.X+inset)*u,(l.Y+inset)*u,(l.Frame.Width-2*inset-48)*u,36*u),"选择冒险",Mathf.RoundToInt(20*u),pale,true);
   if(PopupCloseButton(new Rect((l.X+l.Frame.Width-44-inset)*u,(l.Y+inset)*u,44*u,36*u))){adventureChapterSelected=false;session.CancelDungeonSelection();BlockUITransition();return;}
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
   float imageHeight=Mathf.Clamp(contentWidth*.5f,96,MobileControls.Active?160:216),rewardY=48+imageHeight+12;
   adventureDetailScroll=BeginTouchScroll("adventure-detail",AdventureRect(l.Details,u),adventureDetailScroll,new Rect(0,0,contentWidth*u,Mathf.Max(l.Details.Height*u,(rewardY+rewardHeight)*u)));
   Text(new Rect(8*u,4*u,(contentWidth-12)*u,32*u),detail,Mathf.RoundToInt(18*u),pale,true);
   DrawDungeonEntryArtwork(new Rect(8*u,44*u,(contentWidth-16)*u,imageHeight*u),mode+1);
   if(!adventureChapterSelected){entryRewardViewport=AdventureRect(l.Details,u);entryRewardContentOrigin=new Vector2(entryRewardViewport.x-adventureDetailScroll.x,entryRewardViewport.y+rewardY*u-adventureDetailScroll.y);GUI.BeginGroup(new Rect(0,rewardY*u,contentWidth*u,rewardHeight*u));DrawEntryRewardPreviews(contentWidth,u,mode,tier,false,true);GUI.EndGroup();}
   EndTouchScroll();
   float x=l.X+inset,y=l.FooterY;

   bool normal=!adventureChapterSelected;
   if(Button(new Rect((x)*u,y*u,44*u,48*u),"−",jade,normal&&tier>1))session.SelectedDungeonTier--;
   Text(new Rect((x+44)*u,y*u,80*u,48*u),"Lv"+AdventureRewardRules.DungeonLevel(tier),Mathf.RoundToInt(13*u),gold,true,false,TextAnchor.MiddleCenter);
   if(Button(new Rect((x+124)*u,y*u,44*u,48*u),"+",jade,normal&&tier<session.MaximumDungeonTier))session.SelectedDungeonTier++;
   if(PrimaryButton(new Rect((x+176)*u,y*u,(l.Frame.Width-2*inset-176)*u,48*u),"进入挑战",gold))
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
