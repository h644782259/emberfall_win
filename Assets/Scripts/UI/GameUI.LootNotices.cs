using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private sealed class LootNotice { public string Id,Name;public ItemSlot Slot;public Rarity Rarity;public int Level;public float Started=-1; }
        private readonly List<LootNotice> lootNotices=new List<LootNotice>();
        private ProgressionService lootNoticeOwner;private string lootNoticeSlot;
        private void BindLootNotices()
        {
            var p=session.Progression;
            if(lootNoticeOwner==p&&lootNoticeSlot==p.CurrentSlotId)return;
            ReleaseLootNotices();lootNoticeOwner=p;lootNoticeSlot=p.CurrentSlotId;p.LootCollected+=QueueLootNotice;
        }
        private void ReleaseLootNotices()
        {if(lootNoticeOwner!=null)lootNoticeOwner.LootCollected-=QueueLootNotice;lootNoticeOwner=null;lootNotices.Clear();}
        private void QueueLootNotice(ItemData item)
        {
            if(item==null||!session.InDungeon||session.IsDead||session.PracticeActive||lootNoticeOwner!=session.Progression||lootNoticeSlot!=session.Progression.CurrentSlotId)return;
            if(lootNotices.Exists(n=>n.Id==item.id))return;
            lootNotices.Add(new LootNotice{Id=item.id,Name=item.name,Slot=item.slot,Rarity=item.rarity,Level=item.level});
        }
        private bool LootNoticesVisible {get{return session.InDungeon&&!session.IsDead&&!session.Paused&&!session.PracticeActive&&panel==Panel.None&&!session.InputBlocked;}}
        private int VisibleLootNoticeCount {get{return Mathf.Min(lootNotices.Count,MobileControls.Active?2:3);}}
        private Rect LootNoticeRect(int index)
        {float u=MobileControls.Active?TouchRatio:1,w=Mathf.Min((MobileControls.Active?174:208)*u,width*.35f);return new Rect(width-w-12*u,(64+index*(MobileControls.Active?48:60))*u,w,(MobileControls.Active?44:54)*u);}
        private Rect LootNoticeEquipRect(Rect r)
        {float u=MobileControls.Active?TouchRatio:1;return new Rect(r.xMax-52*u,r.y+(MobileControls.Active?0:5)*u,48*u,44*u);}
        private ItemData LootNoticeUpgrade(LootNotice n)
        {var item=session.Progression.Profile.inventory.Find(v=>v!=null&&v.id==n.Id);return IsEquipmentUpgrade(item)?item:null;}
        private void PrepareLootNotices()
        {
            BindLootNotices();
            if(!session.InDungeon||session.IsDead||session.PracticeActive){lootNotices.Clear();return;}
            if(!LootNoticesVisible){foreach(var n in lootNotices)n.Started=-1;return;}
            lootNotices.RemoveAll(n=>n.Started>=0&&Time.time-n.Started>=(MobileControls.Active?3.5f:5f));
            for(int i=0;i<VisibleLootNoticeCount;i++)
            {
                var notice=lootNotices[i];if(notice.Started<0)notice.Started=Time.time;
                Rect r=LootNoticeRect(i);
                if(LootNoticeUpgrade(notice)==null)continue;
                Rect hit=LootNoticeEquipRect(r);blockedRects.Add(hit);
                if(hit.Contains(Mouse)&&(Event.current.type==EventType.MouseDown||Event.current.type==EventType.MouseUp||Event.current.type==EventType.MouseDrag||Event.current.type==EventType.ScrollWheel))
                {
                    if(GUI.enabled&&Event.current.type==EventType.MouseDown&&LootNoticeEquipRect(r).Contains(Mouse))
                    {var item=LootNoticeUpgrade(notice);if(item!=null){bool saved=session.Progression.Equip(item.id);Feedback(saved,saved?"装备已穿戴":session.Progression.LastError);}}
                    Event.current.Use();
                }
            }
        }
        private void DrawLootNotices()
        {
            if(!LootNoticesVisible)return;
            float u=MobileControls.Active?TouchRatio:1;
            for(int i=0;i<VisibleLootNoticeCount;i++)
            {
                var notice=lootNotices[i];Rect r=LootNoticeRect(i);Color tint=GameBalance.RarityColor(notice.Rarity);bool upgrade=LootNoticeUpgrade(notice)!=null;
                Fill(r,new Color(.025f,.045f,.065f,.94f));Border(r,tint);
                float iconSize=MobileControls.Active?28:36;float textX=MobileControls.Active?40:48;
                Rect icon=new Rect(r.x+6*u,r.center.y-iconSize*u/2,iconSize*u,iconSize*u);Border(icon,tint);DrawIcon(icon,UIIconAtlas.EquipmentCardIcon(notice.Slot,notice.Level,notice.Rarity,session.Progression.Profile.heroClass),tint);
                Text(new Rect(r.x+textX*u,r.y+3*u,r.width-(textX+(upgrade?56:6))*u,(MobileControls.Active?20:26)*u),notice.Name,Mathf.RoundToInt(11*u),pale,false,false);
                Text(new Rect(r.x+textX*u,r.y+(MobileControls.Active?24:32)*u,r.width-(textX+(upgrade?56:6))*u,17*u),(MobileControls.Active?"":"已拾取 · ")+GameBalance.RarityName(notice.Rarity),Mathf.RoundToInt(10*u),tint);
                if(upgrade){Rect action=LootNoticeEquipRect(r);Fill(action,new Color(.08f,.3f,.18f));Border(action,jade);Text(action,"穿戴",Mathf.RoundToInt(12*u),pale,true,false,TextAnchor.MiddleCenter);}
            }
        }
    }
}
