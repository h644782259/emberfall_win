using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        // One presentation-only resource treatment shared by chest receipts and
        // committed run recaps. It never grants, rolls, exchanges or acknowledges.
        private void DrawRewardToken(Rect area,int kind,int amount,float unit)
        {
            if(amount==0)return;
            if(area.width<=16*unit||area.height<=8*unit)return;
            float icon=Mathf.Max(1,Mathf.Min(area.height-6*unit,38*unit));
            Color tint=kind==0?gold:kind==1?new Color(.52f,.86f,1):kind==2?new Color(.87f,.72f,1):jade;
            DrawIcon(new Rect(area.x,area.y+(area.height-icon)*.5f,icon,icon),UIIconAtlas.Reward(kind),tint);
            string number=(amount>0?"+":"")+amount;
            int font=Mathf.RoundToInt(Mathf.Clamp((area.width-icon-8*unit)/Mathf.Max(1,number.Length)*1.5f,Mathf.Min(11*unit,area.height-6*unit),Mathf.Min(23*unit,area.height-6*unit)));
            Text(new Rect(area.x+icon+8*unit,area.y,Mathf.Max(1,area.width-icon-8*unit),area.height),number,font,tint,true);
        }
        private void DrawChestResourceVisuals(Rect area,ChestReward reward)
        {
            if(reward==null)return;
            if(!reward.hasCurrencyDeltas){DrawChestGoldReward(area,reward,gold);return;}
            float unit=MobileControls.Active?TouchRatio:1;
            int count=(reward.goldDelta>0?1:0)+(reward.materialKind==RewardMaterialKind.StarAshFragment&&reward.materialsDelta>0?1:0)+(reward.threadsDelta>0?1:0);
            if(count==0)return;
            float row=Mathf.Min(64*unit,area.height/count),y=area.y+(area.height-row*count)*.5f;
            if(reward.goldDelta>0){DrawRewardToken(new Rect(area.x+8*unit,y,area.width-16*unit,row),0,reward.goldDelta,unit);y+=row;}
            if(reward.materialKind==RewardMaterialKind.StarAshFragment&&reward.materialsDelta>0){DrawRewardToken(new Rect(area.x+8*unit,y,area.width-16*unit,row),1,reward.materialsDelta,unit);y+=row;}
            if(reward.threadsDelta>0)DrawRewardToken(new Rect(area.x+8*unit,y,area.width-16*unit,row),2,reward.threadsDelta,unit);
        }
        private float DrawChestRewardContents(float width,float u,ChestReward reward,string error,bool draw)
        {
            float y=8;
            if(!string.IsNullOrEmpty(error))
            {
                float h=Style(Mathf.RoundToInt(13*u),false,true).CalcHeight(new GUIContent(error),(width-8)*u)/u;
                if(draw)Text(new Rect(4*u,y*u,(width-8)*u,h*u),error,Mathf.RoundToInt(13*u),new Color(1,.55f,.45f),false,true);
                y+=h+12;
            }
            if(reward==null)return y;
            int coins=reward.hasCurrencyDeltas?reward.goldDelta:reward.Gold;
            int shards=reward.hasCurrencyDeltas&&reward.materialKind==RewardMaterialKind.StarAshFragment?reward.materialsDelta:0;
            int threads=reward.hasCurrencyDeltas?reward.threadsDelta:0;
            int[] amounts={coins,shards,threads};string[] labels={reward.hasCurrencyDeltas?"金币":"金币（记录值）","星烬碎片","星纹"};
            if(coins>0||shards>0||threads>0)
            {
                if(draw)Text(new Rect(4*u,y*u,(width-8)*u,24*u),"资源",Mathf.RoundToInt(15*u),pale,true);
                y+=28;
                for(int i=0;i<amounts.Length;i++)if(amounts[i]>0)
                {
                    if(draw)
                    {
                        Color tint=i==0?gold:i==1?new Color(.52f,.86f,1):new Color(.87f,.72f,1);
                        DrawIcon(new Rect(4*u,(y+6)*u,32*u,32*u),UIIconAtlas.Reward(i),tint);
                        Text(new Rect(44*u,y*u,(width-124)*u,44*u),labels[i],Mathf.RoundToInt(13*u),pale);
                        Text(new Rect((width-80)*u,y*u,76*u,44*u),"+"+amounts[i],Mathf.RoundToInt(16*u),tint,true,false,TextAnchor.MiddleRight);
                    }
                    y+=48;
                }
                y+=8;
            }
            if(reward.Rarity.HasValue&&reward.Slot.HasValue&&!reward.Duplicate)
            {
                string name=reward.Name+"\n"+GameBalance.RarityName(reward.Rarity.Value)+" · "+(reward.Slot.Value==FashionSlot.Weapon?"兵装":"羽翼");
                float h=Mathf.Max(60,Style(Mathf.RoundToInt(14*u),false,true).CalcHeight(new GUIContent(name),(width-80)*u)/u);
                if(draw)
                {
                    Text(new Rect(4*u,y*u,(width-8)*u,24*u),"外观物品",Mathf.RoundToInt(15*u),pale,true);
                    Rect icon=new Rect(4*u,(y+30)*u,56*u,56*u);Color tint=GameBalance.RarityColor(reward.Rarity.Value);
                    Fill(icon,card);Border(icon,tint);DrawIcon(new Rect(icon.x+8*u,icon.y+8*u,40*u,40*u),UIIconAtlas.FashionCardIcon(reward.Slot.Value,reward.appearanceTier>=0?reward.appearanceTier:(int)reward.Rarity.Value,session.Progression.Profile.heroClass),tint);
                    Text(new Rect(72*u,(y+30)*u,(width-80)*u,h*u),name,Mathf.RoundToInt(14*u),pale,false,true);
                }
                y+=30+h+12;
            }
            return y;
        }
        private void DrawChestCommittedReward(Rect area,ChestReward reward,Color accent)
        {
            float unit=MobileControls.Active?TouchRatio:1;
            if(reward!=null&&reward.equipmentIds!=null&&reward.equipmentIds.Length>0){
                float size=Mathf.Min(56*unit,(area.width-8*unit)/reward.equipmentIds.Length);
                for(int i=0;i<reward.equipmentIds.Length;i++){
                    var item=session.Progression.Profile.inventory.Find(v=>v.id==reward.equipmentIds[i]);
                    if(item!=null)DrawInventoryIcon(new Rect(area.x+i*(size+2*unit),area.y,size,size),item,unit);
                }
                area=new Rect(area.x,area.y+size+8*unit,area.width,Mathf.Max(1,area.height-size-8*unit));
            }
            if(reward!=null&&reward.Rarity.HasValue)
            {
                int count=(reward.goldDelta>0?1:0)+(reward.materialKind==RewardMaterialKind.StarAshFragment&&reward.materialsDelta>0?1:0)+(reward.threadsDelta>0?1:0);
                float strip=reward.hasCurrencyDeltas?Mathf.Min(area.height*.6f,count*42*unit):Mathf.Min(area.height*.35f,64*unit);
                if(DrawChestRewardModel(new Rect(area.x,area.y,area.width,area.height-strip),reward))
                {DrawChestResourceVisuals(new Rect(area.x,area.yMax-strip,area.width,strip),reward);return;}
            }
            DrawChestResourceVisuals(area,reward);
        }
    }
}
