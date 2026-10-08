using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private int smithCategory,smithSelectedSlot;
        private Vector2 smithDetailScroll;
        private void DrawSmithService()
        {
            var p=session.Progression;float u=MobileControls.Active?TouchRatio:1;
            var l=new SmithServiceLayout(width/u,height/u);
            Fill(new Rect(0,0,width,height),new Color(.018f,.031f,.048f,.985f));blockedRects.Add(new Rect(0,0,width,height));
            Text(BuildPlanRect(l.Header,u),"铁匠 · 装备强化与镶嵌",Mathf.RoundToInt(22*u),gold,true);
            if(NavigationButton(BuildPlanRect(l.Close,u),"×",jade)){ClosePanel();return;}
            string[] categories={"强化","镶嵌","继承"};
            for(int i=0;i<categories.Length;i++)
                if(TabButton(BuildPlanRect(l.Category(i),u),categories[i],smithCategory==i)){smithCategory=i;smithDetailScroll=Vector2.zero;BlockUITransition();}
            smithSelectedSlot=Mathf.Clamp(smithSelectedSlot,0,2);
            for(int i=0;i<3;i++)
            {
                var slot=(ItemSlot)i;var gear=p.Equipped(slot);Rect row=BuildPlanRect(l.Equipment(i),u);
                if(QuietAction(row,"",gear!=null,null,smithSelectedSlot==i)){smithSelectedSlot=i;smithDetailScroll=Vector2.zero;BlockUITransition();}
                Text(new Rect(row.x+46*u,row.y+6*u,row.width-52*u,row.height-12*u),GameBalance.SlotName(slot)+"\n+"+p.SlotUpgradeRank(slot),Mathf.RoundToInt(12*u),pale,true,true);
                DrawIcon(new Rect(row.x+8*u,row.y+8*u,30*u,30*u),UIIconAtlas.EquipmentCardIcon(slot),gear==null?muted:GameBalance.RarityColor(gear.rarity));
            }
            var item=p.Equipped((ItemSlot)smithSelectedSlot);
            if(item==null){Text(BuildPlanRect(l.Detail,u),"先在行囊中穿戴这个部位的装备。",Mathf.RoundToInt(16*u),muted,false,true);return;}
            float contentHeight=DrawSmithDetail(item,l.Detail.Width-18,u,false);
            smithDetailScroll=BeginTouchScroll("smith-selected-detail",BuildPlanRect(l.Detail,u),smithDetailScroll,new Rect(0,0,(l.Detail.Width-18)*u,Mathf.Max(l.Detail.Height,contentHeight)*u));
            DrawSmithDetail(item,l.Detail.Width-18,u,true);EndTouchScroll();
            if(smithCategory==0)
            {
                var quote=p.PrepareSmithUpgrade(item.slot,SmithServiceActive);
                string caption=quote==null?(p.SlotUpgradeRank(item.slot)>=ProgressionService.MaximumUpgrade?"部位已满级":"暂不能强化"):"强化装备 · "+quote.GoldCost+" 金币";
                if(PrimaryButton(BuildPlanRect(l.Primary,u),caption,gold,quote!=null,p.LastError))
                {Feedback(p.UpgradeAtSmith(quote,SmithServiceActive),"部位强化已保存，换装自动继承");BlockUITransition();}
            }
            else Text(BuildPlanRect(l.Primary,u),smithCategory==1?"兑换请找商人 · 已有挂件在此镶嵌或更换":"同部位换装免费继承等级 · 不转移或叠加属性",Mathf.RoundToInt(13*u),jade,false,true);
        }
        private float DrawSmithDetail(ItemData item,float width,float u,bool draw)
        {
            var p=session.Progression;float y=8;
            if(draw)DrawIcon(new Rect((width-64)*.5f*u,y*u,64*u,64*u),UIIconAtlas.EquipmentCardIcon(item.slot),GameBalance.RarityColor(item.rarity));y+=72;
            GoalParagraph(ref y,width,u,item.name+" · Lv."+item.level+" · "+GameBalance.RarityName(item.rarity),17,gold,true,draw);
            if(smithCategory==0)
            {
                int rank=p.SlotUpgradeRank(item.slot);var next=p.PreviewUpgrade(item,Mathf.Min(rank+1,ProgressionService.MaximumUpgrade));
                GoalParagraph(ref y,width,u,GameBalance.SlotName(item.slot)+"强化等级  +"+rank+" → +"+Mathf.Min(rank+1,ProgressionService.MaximumUpgrade),15,pale,true,draw);
                GoalParagraph(ref y,width,u,"攻击  "+item.attack+" → "+next.attack+"\n防御  "+item.defense+" → "+next.defense+"\n生命  "+item.health+" → "+next.health,15,jade,false,draw);
                GoalParagraph(ref y,width,u,"费用："+p.UpgradeCost(item)+" 金币 · 拥有 "+p.Profile.gold+"\n永久提升此部位，换装自动继承。",13,muted,false,draw);
            }
            else if(smithCategory==2)
            {
                GoalParagraph(ref y,width,u,"此部位已投入 +"+p.SlotUpgradeRank(item.slot)+"。换上同部位新装备时免费沿用等级，以新装备自身基础属性重新计算。",15,pale,false,draw);
                GoalParagraph(ref y,width,u,"原物品编号、锁定、机制与方案引用保留。更换不转移金币、不重复叠加属性；不同部位各自独立。",13,muted,false,draw);
            }
            else
            {
                bool any=false;
                foreach(var mechanic in BuildCatalog.MechanicsFor(p.Profile.heroClass))
                {
                    if(BuildCatalog.MechanicSlot(mechanic)!=item.slot)continue;
                    var a=p.Attachment(mechanic);if(a==null)continue;any=true;
                    GoalParagraph(ref y,width,u,BuildCatalog.MechanicName(mechanic)+" · +"+a.upgradeRank+" · "+(a.mounted?"已镶嵌":"未镶嵌"),15,pale,true,draw);
                    GoalParagraph(ref y,width,u,BuildCatalog.MechanicDescription(mechanic),13,muted,false,draw);
                    if(draw&&QuietAction(new Rect(8*u,y*u,(width-16)*u,40*u),a.mounted?"卸下挂件":"镶嵌 / 更换挂件",SmithServiceActive))
                    {Feedback(p.SetAttachmentMounted(mechanic,!a.mounted,SmithServiceActive),"挂件装配已保存");BlockUITransition();}y+=48;
                    string reason=p.AttachmentUpgradeLock(mechanic,SmithServiceActive);
                    if(draw&&QuietAction(new Rect(8*u,y*u,(width-16)*u,40*u),"挂件升阶 · 6碎片",reason.Length==0,reason))
                    {Feedback(p.UpgradeAttachment(mechanic,SmithServiceActive),"挂件升阶已保存");BlockUITransition();}y+=48;
                    if(BuildCatalog.HasMechanicVariant(mechanic))
                    {
                        if(draw&&QuietAction(new Rect(8*u,y*u,(width-16)*u,40*u),a.variantUnlocked?"切换变体 A / B":"解锁变体 · 4碎片",SmithServiceActive&&(a.variantUnlocked||p.Profile.mechanicMaterials>=4)))
                        {Feedback(p.ToggleAttachmentVariant(mechanic,SmithServiceActive),"挂件变体已保存");BlockUITransition();}y+=48;
                    }
                    if(draw&&QuietAction(new Rect(8*u,y*u,(width-16)*u,40*u),a.rarity==Rarity.Legendary?"已升华":"升华 · 24碎片",SmithServiceActive&&a.rarity==Rarity.Epic&&p.HighestAdventureTier>=5&&p.Profile.mechanicMaterials>=24))
                    {Feedback(p.AscendAttachment(mechanic,SmithServiceActive),"挂件升华已保存");BlockUITransition();}y+=48;
                }
                if(!any)GoalParagraph(ref y,width,u,"此部位暂无已拥有的本职业挂件。商人兑换后会出现在这里。",14,muted,false,draw);
                GoalParagraph(ref y,width,u,"星烬碎片 × "+p.Profile.mechanicMaterials+" · 挂件与装备基础属性分开保留",13,gold,false,draw);
            }
            return y+8;
        }
    }
}
