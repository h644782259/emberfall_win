using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameUI
    {

        // All inventory comparisons use the attributes the item will have after
        // equipping. A stored per-item rank is not the persistent slot rank.
        private ItemData EquipmentPreview(ItemData item)
        {
            return session.Progression.PreviewEquippedItem(item);
        }

        private float EquipmentPreviewScore(ItemData item)
        {
            return ProgressionService.EquipmentScore(EquipmentPreview(item));
        }

        private void DrawPersistentMechanismDetail(Rect area,ItemData current,ItemData candidate)
        {
            if(session.Progression.Profile.attachmentRevision>=1)
            {
                string mounts="";
                foreach(var a in session.Progression.Profile.attachments)
                    if(a.mounted&&BuildCatalog.MechanicClass(a.mechanic)==session.Progression.Profile.heroClass)mounts+=(mounts.Length>0?" / ":"")+BuildCatalog.MechanicName(a.mechanic)+" · "+a.upgradeRank+"阶";
                Fill(area,new Color(.025f,.05f,.065f));
                string text="挂件换装沿用 · "+(mounts.Length>0?mounts:"当前未挂载")+"\n旧装备属性与部位强化按换装比较；到营地工坊管理挂件与变体。";
                Text(new Rect(area.x+8,area.y+6,area.width-16,area.height-12),text,12,jade,false,true);
                if(area.Contains(Mouse))tooltip=text;
                return;
            }
            var hero=session.Progression.Profile.heroClass;float half=(area.width-8)*.5f;
            bool lost=!EquipmentComparisonPresentation.SameMechanism(current,candidate,hero)&&EquipmentComparisonPresentation.ActiveMechanic(current,hero)!=EquipmentMechanic.None;
            DrawMechanismTradeoff(new Rect(area.x,area.y,half,area.height),current,hero,lost?"原机制 · 将失去":"当前机制",lost?gold:muted);
            DrawMechanismTradeoff(new Rect(area.x+half+8,area.y,half,area.height),candidate,hero,"换装后机制",jade);
            if(area.Contains(Mouse))tooltip=EquipmentComparisonPresentation.Changes(current,candidate,hero)+"\n"+EquipmentComparisonPresentation.Description(candidate,hero);
        }
        private void DrawMechanismTradeoff(Rect area,ItemData item,HeroClass hero,string label,Color accent)
        {
            Fill(area,new Color(.025f,.05f,.065f));
            string copy=label+" · "+MechanicBadgePresentation.Title(item,hero)+"\n收益："+MechanicBadgePresentation.Benefit(item,hero)+"\n代价："+MechanicBadgePresentation.Cost(item,hero);
            int size=11;
            if(Style(size,false,true).CalcHeight(new GUIContent(copy),area.width-8)>area.height-6)size=10;
            Text(new Rect(area.x+4,area.y+3,area.width-8,area.height-6),copy,size,accent,false,true);
        }

        private void DrawEquipmentComparison(Rect r, ItemData equipped, ItemData candidate)
        {
            float current=ProgressionService.EquipmentScore(equipped);
            ItemData preview=EquipmentPreview(candidate);
            float next=ProgressionService.EquipmentScore(preview);
            float diff=next-current;
            float half=(r.width-12)*.5f;
            Fill(new Rect(r.x,r.y,half,r.height),new Color(.035f,.075f,.1f));
            Fill(new Rect(r.x+half+12,r.y,half,r.height),new Color(.065f,.115f,.14f));
            Text(new Rect(r.x+10,r.y+5,half-20,17),equipped==null?"当前装备 · 空槽":"当前装备评分",12,muted);
            Text(new Rect(r.x+10,r.y+25,half-20,31),current.ToString("0.#"),25,pale,true);
            Text(new Rect(r.x+half+22,r.y+5,half-20,17),"换装后评分",12,jade);
            Text(new Rect(r.x+half+22,r.y+25,half-80,31),next.ToString("0.#"),25,pale,true);
            string delta=Mathf.Approximately(diff,0)?"±0":(diff>0?"+":"")+diff.ToString("0.#");
            Text(new Rect(r.xMax-83,r.y+28,73,27),delta,18,diff<0?new Color(1,.48f,.42f):diff>0?jade:muted,true,false,TextAnchor.MiddleRight);
            if(r.Contains(Mouse))tooltip="当前："+(equipped==null?"空槽":ItemTitle(equipped))+"\n换装后："+ItemTitle(preview)+"\n自动继承部位强化 +"+session.Progression.SlotUpgradeRank(candidate.slot)+"，加成按该装备自身基础属性计算。";
        }
    }
}
