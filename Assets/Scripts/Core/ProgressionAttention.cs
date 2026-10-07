using System;
using System.Collections.Generic;
namespace Emberfall
{
    /// <summary>Actionable state, computed after progression changes rather than on every repaint.</summary>
    public sealed class ProgressionAttention
    {
        public readonly HashSet<string> HigherScoreItems=new HashSet<string>(StringComparer.Ordinal);
        public readonly HashSet<string> MechanismTradeoffs=new HashSet<string>(StringComparer.Ordinal);
        public readonly HashSet<int> LearnableSkills=new HashSet<int>();
        public bool LootClaimable,LootPending,FirstClearClaimable,ChestClaimable;
        public bool Equipment {get{return HigherScoreItems.Count>0;}}
        public bool Skills {get{return LearnableSkills.Count>0;}}
        public bool Rewards {get{return LootPending||FirstClearClaimable||ChestClaimable;}}
        public static bool LevelEligible(GameProfile p,ItemData item){return p!=null&&item!=null&&item.level<=p.level;}
        public static bool HigherScore(float candidate,float current)
        {return !float.IsNaN(candidate)&&!float.IsInfinity(candidate)&&candidate-current>Math.Max(.1f,Math.Abs(current)*.001f);}
        public static ProgressionAttention Evaluate(ProgressionService progression,bool inCamp)
        {
            var state=new ProgressionAttention();if(progression==null)return state;var p=progression.Profile;
            foreach(ItemData item in p.inventory)
            {
                if(!LevelEligible(p,item)||item.id==p.weaponId||item.id==p.armorId||item.id==p.relicId)continue;
                ItemData current=progression.Equipped(item.slot),preview=progression.PreviewEquippedItem(item);
                if(!HigherScore(ProgressionService.EquipmentScore(preview),ProgressionService.EquipmentScore(current)))continue;
                state.HigherScoreItems.Add(item.id);
                if(p.attachmentRevision<1&&current!=null&&current.mechanic!=item.mechanic&&(current.mechanic!=EquipmentMechanic.None||item.mechanic!=EquipmentMechanic.None))state.MechanismTradeoffs.Add(item.id);
            }
            if(p.skillPoints>0)for(int i=0;i<GameBalance.SkillCount;i++)if(string.IsNullOrEmpty(progression.SkillLockReason(i)))state.LearnableSkills.Add(i);
            state.LootPending=p.pendingLoot.Count>0||p.recoveryLoot.Count>0;
            state.LootClaimable=p.inventory.Count<ProgressionService.InventoryCapacity&&(p.pendingLoot.Count>0||p.recoveryLoot.Count>0);
            state.FirstClearClaimable=inCamp&&p.pendingFirstClearReward;
            state.ChestClaimable=p.pendingFashionChest||p.pendingChestReveal;
            return state;
        }
    }
}
