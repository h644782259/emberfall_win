using UnityEngine;
namespace Emberfall {
 public sealed partial class GameUI {
  private string adventureRewardHint;
  private void AdventureRewardIcon(Rect tile,Texture2D icon,Rarity rarity,string count,string hint,float u) {
   Color c=GameBalance.RarityColor(rarity);Fill(tile,card);Border(tile,c);
   DrawIcon(new Rect(tile.x+8*u,tile.y+8*u,tile.width-16*u,tile.height-22*u),icon,c);
   for(int i=0;i<=(int)rarity;i++)Fill(new Rect(tile.x+4*u+i*5*u,tile.y+3*u,3*u,3*u),pale);
   Text(new Rect(tile.x,tile.yMax-17*u,tile.width-5*u,16*u),count,Mathf.RoundToInt(10*u),pale,false,false,TextAnchor.MiddleRight);
   if(tile.Contains(Mouse))tooltip=hint;
   if(GUI.Button(tile,GUIContent.none,invisibleButton))adventureRewardHint=hint;
  }
  private void DrawAdventureRewards(int mode,int tier,float w,float u,bool showEncounter=true) {
   Text(new Rect(8*u,42*u,(w-16)*u,24*u),"怪物掉落",Mathf.RoundToInt(13*u),muted);
   float size=Mathf.Min(52,(w-48)/5),step=size+8;
   AdventureRewardIcon(new Rect(8*u,72*u,size*u,58*u),UIIconAtlas.EquipmentCardIcon(ItemSlot.Weapon),Rarity.Common,"", "随机武器 / 护甲 / 饰品\n普通、稀有、史诗、传说；阶数提高高品质概率\n装备等级匹配角色",u);
   AdventureRewardIcon(new Rect((8+step)*u,72*u,size*u,58*u),UIIconAtlas.Reward(0),Rarity.Common,"", "金币 · 击杀后掉落地面",u);
   AdventureRewardIcon(new Rect((8+step*2)*u,72*u,size*u,58*u),UIIconAtlas.Utility("potion"),Rarity.Common,"", "生命药剂 · 掉落概率 "+AdventureRewardRules.PotionChance(tier)+"%",u);
   Text(new Rect(8*u,144*u,(w-16)*u,24*u),"通关宝箱",Mathf.RoundToInt(13*u),gold);
   int count=AdventureRewardRules.EquipmentCount(mode,tier);
   int column=0;
   AdventureRewardIcon(new Rect((8+step*column++)*u,174*u,size*u,58*u),UIIconAtlas.EquipmentCardIcon(AdventureRewardRules.EquipmentSlot(mode,0)),AdventureRewardRules.MinimumRarity(mode),"×"+(mode==3?(count+1)/2:count),AdventureRewardRules.EquipmentSummary(mode,tier)+"\n数量 "+count+" · Lv."+session.Progression.Profile.level,u);
   if(mode==3)AdventureRewardIcon(new Rect((8+step*column++)*u,174*u,size*u,58*u),UIIconAtlas.EquipmentCardIcon(ItemSlot.Relic),Rarity.Rare,"×"+(count/2),AdventureRewardRules.EquipmentSummary(mode,tier),u);
   AdventureRewardIcon(new Rect((8+step*column++)*u,174*u,size*u,58*u),UIIconAtlas.Reward(1),Rarity.Rare,"×"+AdventureRewardRules.Materials(mode,tier),"星烬碎片 · "+AdventureRewardRules.Materials(mode,tier)+"枚",u);
   if(mode==-1)AdventureRewardIcon(new Rect((8+step*column++)*u,174*u,size*u,58*u),UIIconAtlas.FashionCardIcon(FashionSlot.Wings),Rarity.Rare,"", "时装：保底稀有 · "+AdventureRewardRules.UpgradeChance(-1,tier)+"%史诗 · "+AdventureRewardRules.LegendaryChance(tier)+"%传说",u);
   AdventureRewardIcon(new Rect((8+step*column++)*u,174*u,size*u,58*u),UIIconAtlas.Reward(0),Rarity.Common,"", "金币 · "+TierRewardRules.ChestGoldMinimum(tier)+"～"+(TierRewardRules.ChestGoldMinimum(tier)+40),u);
   AdventureRewardIcon(new Rect((8+step*column)*u,174*u,size*u,58*u),UIIconAtlas.Reward(2),Rarity.Rare,"×1", "星纹 · 时装重复时额外转化星纹与金币",u);
   if(showEncounter)Text(new Rect(8*u,244*u,(w-16)*u,36*u),mode==-1?"三波 · 首领":mode==0?"守点 · 三阶段":mode==1?"限时突破":mode==2?"三首领":"五房远征",Mathf.RoundToInt(13*u),jade);
   if(MobileControls.Active&&!string.IsNullOrEmpty(adventureRewardHint))Text(new Rect(8*u,282*u,(w-16)*u,100*u),adventureRewardHint,Mathf.RoundToInt(12*u),pale,false,true);
  }
 }
}
