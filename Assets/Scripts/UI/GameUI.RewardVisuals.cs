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
        private void DrawChestCommittedReward(Rect area,ChestReward reward,Color accent)
        {
            float unit=MobileControls.Active?TouchRatio:1;
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
