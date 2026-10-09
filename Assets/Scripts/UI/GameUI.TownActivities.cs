using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private void DrawTownActivityEntry()
        {
            if(MobileControls.Active||!session.IsInCamp||session.CurrentHub==0)return;
            Rect r=new Rect(width-248,236,230,42);blockedRects.Add(r);
            if(NavigationButton(r,session.CurrentHub==1?"赤岩锻造委托":"星望观测星图",gold)){TogglePanel(Panel.Camp);campTab=4;}
        }
        private void DrawTownActivitySurface()
        {
            bool quarry=session.CurrentHub==1;var p=session.Progression;
            Rect w=Modal(Mathf.Min(width-24,800),Mathf.Min(height-24,450),quarry?"赤岩驿站 · 锻造委托":"星望城 · 观测星图",quarry?"用多余补给完成锻造运输，每个角色等级可结算一次":"记录冒险阶数，为星图点亮新的星座");
            if(PopupCloseButton(new Rect(w.xMax-64,w.y+20,40,32))){campTab=0;ClosePanel();return;}
            DrawIcon(new Rect(w.x+30,w.y+120,64,64),UIIconAtlas.Utility(quarry?"attack":"skills"),gold);
            Text(new Rect(w.x+116,w.y+122,w.width-146,60),quarry?"锻造物资：80金币 + 4药剂 → 3星烬碎片":"每首次记录一阶冒险，领取2星纹",20,pale,true,true);
            Text(new Rect(w.x+30,w.y+202,w.width-60,76),quarry?"已结算至角色 "+p.Profile.quarryWorkLevel+" 级 · 当前 "+p.Profile.level+" 级\n金币 "+p.Profile.gold+" · 药剂 "+p.Profile.potions:"星图已记录第 "+p.Profile.starChartTier+" 阶 · 最高通关 "+p.HighestAdventureTier+" 阶\n未领取的阶数会一次结算，读取存档不会重复领取",15,jade,false,true);
            bool ready=session.IsInCamp&&(quarry?p.Profile.quarryWorkLevel<p.Profile.level&&p.Profile.gold>=80&&p.Profile.potions>=4:p.Profile.starChartTier<p.HighestAdventureTier);
            if(PrimaryButton(new Rect(w.x+30,w.y+298,w.width-60,42),quarry?"完成锻造委托":"记录并领取星纹",gold,ready))Feedback(p.CompleteTownActivity(session.CurrentHub,session.IsInCamp),quarry?"锻造委托已结算":"星图奖励已保存");
        }
    }
}
