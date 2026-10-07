using System;
using System.Collections.Generic;
namespace Emberfall
{
    public partial class ProgressionService
    {
        public const int AttachmentUpgradeCost=6,MaximumAttachmentRank=5;
        public MechanicAttachment Attachment(EquipmentMechanic mechanic)
        {if(Profile.attachments!=null)foreach(var a in Profile.attachments)if(a!=null&&a.mechanic==mechanic)return a;return null;}
        public int AttachmentVariant(EquipmentMechanic mechanic)
        {var a=Attachment(mechanic);return a!=null&&a.mounted&&a.variantUnlocked?a.variant:0;}
        private ProgressionGoalState MigratedAttachmentGoal(bool inCamp)
        {
            bool core=Profile.progressionGoal==ProgressionGoalKind.Core;
            bool missing=FindItem(Profile.progressionGoalItemId)==null&&(Profile.progressionGoal==ProgressionGoalKind.Variant||Profile.progressionGoal==ProgressionGoalKind.Ascension||Profile.progressionGoal==ProgressionGoalKind.Reforge);
            if(!core&&!missing)return null;
            var a=Attachment(Profile.progressionGoalMechanic);
            if(a==null&&missing)a=Profile.attachments.Find(x=>x.legacySourceId==Profile.progressionGoalItemId);
            if(a==null)return null;
            var g=new ProgressionGoalState{Identity=core?"Core/"+(int)a.mechanic+"/"+(int)Profile.progressionGoalMinimumRarity:"attachment/"+Profile.progressionGoal+"/"+(int)a.mechanic,ItemId=a.id,Title=BuildCatalog.MechanicName(a.mechanic)};
            if(core&&a.rarity>=Profile.progressionGoalMinimumRarity)
            {g.Done=true;g.Step="挂件已获得，换装后继续沿用";return g;}
            switch(Profile.progressionGoal)
            {
                case ProgressionGoalKind.Variant:g.Done=a.variantUnlocked;g.Action=g.Done?ProgressionGoalAction.None:ProgressionGoalAction.UnlockVariant;g.MaterialCost=VariantCost;break;
                case ProgressionGoalKind.Ascension:g.Done=a.rarity==Rarity.Legendary;g.Action=g.Done?ProgressionGoalAction.None:ProgressionGoalAction.Ascend;g.MaterialCost=AscensionCost;g.RequiredAdventureTier=5;break;
                default:g.Done=a.upgradeRank>=MaximumAttachmentRank;g.Action=g.Done?ProgressionGoalAction.None:ProgressionGoalAction.UpgradeAttachment;g.MaterialCost=AttachmentUpgradeCost;break;
            }
            g.CanAct=inCamp&&!g.Done&&(g.Action==ProgressionGoalAction.UpgradeAttachment?AttachmentUpgradeLock(a.mechanic,true).Length==0:Profile.mechanicMaterials>=g.MaterialCost&&(g.Action!=ProgressionGoalAction.Ascend||a.rarity==Rarity.Epic&&HighestAdventureTier>=5));
            g.Step=g.Done?"挂件目标已达成，原装备丢失不影响挂件":"旧装备目标已转为独立挂件 · 点击定位营地工坊";
            return g;
        }
        public float MechanicPowerMultiplier(EquipmentMechanic mechanic)
        {var a=Attachment(mechanic);return a!=null&&a.mounted?1f+.08f*a.upgradeRank+(a.upgradeRank>=5?.2f:0)+(a.rarity==Rarity.Legendary?.15f:0):1f;}
        public float MechanicRangeMultiplier(EquipmentMechanic mechanic)
        {var a=Attachment(mechanic);return a!=null&&a.mounted&&a.upgradeRank>=3?1.2f:1f;}
        private static void NormalizeAttachments(GameProfile profile)
        {
            if(profile.attachmentRevision<0||profile.attachmentRevision>1)throw new ArgumentException("挂件存档版本不受支持，原文件保留。");
            var result=new List<MechanicAttachment>();
            if(profile.attachments!=null)foreach(var a in profile.attachments)
            {
                if(a==null||a.mechanic==EquipmentMechanic.None||!Enum.IsDefined(typeof(EquipmentMechanic),a.mechanic))continue;
                if(string.IsNullOrEmpty(a.id)||a.id.Length>80)a.id=Guid.NewGuid().ToString("N");
                a.level=Clamp(a.level,1,MaximumLevel);a.upgradeRank=Clamp(a.upgradeRank,0,MaximumAttachmentRank);
                a.rarity=(Rarity)Clamp((int)a.rarity,0,3);a.variant=a.variantUnlocked?Clamp(a.variant,0,1):0;
                var existing=result.Find(x=>x.mechanic==a.mechanic);
                if(existing!=null)
                {
                    existing.level=Math.Max(existing.level,a.level);existing.upgradeRank=Math.Max(existing.upgradeRank,a.upgradeRank);
                    existing.rarity=(Rarity)Math.Max((int)existing.rarity,(int)a.rarity);
                    if(!existing.mounted&&a.mounted)existing.variant=a.variant;
                    existing.mounted|=a.mounted;existing.variantUnlocked|=a.variantUnlocked;
                    if(string.IsNullOrEmpty(existing.legacySourceId))existing.legacySourceId=a.legacySourceId;
                }
                else result.Add(a);
            }
            profile.attachments=result;
            {
                var items=new List<ItemData>(profile.inventory);items.AddRange(profile.pendingLoot);items.AddRange(profile.recoveryLoot);
                foreach(var item in items)
                {
                    if(item==null||item.mechanic==EquipmentMechanic.None||!Enum.IsDefined(typeof(EquipmentMechanic),item.mechanic)||item.slot!=BuildCatalog.MechanicSlot(item.mechanic))continue;
                    var a=result.Find(x=>x.mechanic==item.mechanic);
                    bool worn=IsEquipped(profile,item.id);
                    if(a==null)
                    {a=new MechanicAttachment{id=Guid.NewGuid().ToString("N"),legacySourceId=item.id,mechanic=item.mechanic,level=item.level,rarity=item.rarity,mounted=profile.attachmentRevision==0?worn:true,variant=item.mechanicVariant,variantUnlocked=item.mechanicVariantUnlocked};result.Add(a);}
                    else
                    {a.level=Math.Max(a.level,item.level);a.rarity=(Rarity)Math.Max((int)a.rarity,(int)item.rarity);if(profile.attachmentRevision==0&&worn){a.mounted=true;a.variant=item.mechanicVariant;}a.variantUnlocked|=item.mechanicVariantUnlocked;}
                }
            }
            profile.quarryWorkLevel=Clamp(profile.quarryWorkLevel,0,100);profile.starChartTier=Clamp(profile.starChartTier,0,Math.Max(profile.highestAdventureTier,profile.bestFloor));
            if(profile.attachmentRevision==0)
            {
                var plans=new List<BuildPreset>();
                if(profile.buildPresets!=null)plans.AddRange(profile.buildPresets);
                if(profile.classStates!=null)foreach(var state in profile.classStates)
                    if(state!=null&&state.buildPresets!=null)plans.AddRange(state.buildPresets);
                foreach(var preset in plans)
                {
                    if(preset==null||!preset.populated||preset.mountedAttachments!=null)continue;
                    var mounted=new List<EquipmentMechanic>();var variants=new List<int>();
                    string[] ids={preset.weaponId,preset.armorId,preset.relicId};
                    for(int slot=0;slot<3;slot++)
                    {
                        var item=profile.inventory.Find(x=>x.id==ids[slot]);
                        EquipmentMechanic m=item!=null?item.mechanic:preset.equipmentMechanics!=null&&slot<preset.equipmentMechanics.Length&&(preset.equipmentMechanicKnownMask&(1<<slot))!=0?preset.equipmentMechanics[slot]:EquipmentMechanic.None;
                        if(m==EquipmentMechanic.None||BuildCatalog.MechanicClass(m)!=preset.heroClass||!result.Exists(x=>x.mechanic==m)||mounted.Contains(m))continue;
                        mounted.Add(m);variants.Add(preset.equipmentVariants!=null&&slot<preset.equipmentVariants.Length&&preset.equipmentVariants[slot]>=0?preset.equipmentVariants[slot]:item!=null?item.mechanicVariant:0);
                    }
                    preset.mountedAttachments=mounted.ToArray();preset.attachmentVariants=variants.ToArray();
                }
            }
            foreach(var a in result)
            {
                if(profile.variantKnowledge.Contains(a.mechanic))a.variantUnlocked=true;
                if(a.variantUnlocked&&!profile.variantKnowledge.Contains(a.mechanic))profile.variantKnowledge.Add(a.mechanic);
            }
            profile.attachmentRevision=1;
            if(profile.growthRevision<1){profile.automaticGrowth=profile.progressionGoal==ProgressionGoalKind.None;profile.growthRevision=1;}
            foreach(var a in result)if(!profile.discoveredMechanics.Contains(a.mechanic))profile.discoveredMechanics.Add(a.mechanic);
            if(profile.growthRewardReceipts==null)profile.growthRewardReceipts=new List<string>();
            var receipts=new HashSet<string>(StringComparer.Ordinal);
            profile.growthRewardReceipts.RemoveAll(x=>string.IsNullOrEmpty(x)||x.Length>120||!receipts.Add(x));
            if(profile.growthRewardReceipts.Count>512)throw new ArgumentException("成长奖励记录超出安全容量，原文件保留。");
        }
        public bool SetAttachmentMounted(EquipmentMechanic mechanic,bool mounted,bool inCamp)
        {
            var a=Attachment(mechanic);if(!inCamp||a==null||BuildCatalog.MechanicClass(mechanic)!=Profile.heroClass)return Fail("请在营地操作本职业挂件。");
            var candidate=Snapshot();candidate.attachments.Find(x=>x.mechanic==mechanic).mounted=mounted;return CommitCandidate(candidate,true);
        }
        private bool GrantAttachment(EquipmentMechanic mechanic,bool first)
        {
            if(Attachment(mechanic)!=null)return Fail("已拥有同机制挂件，请升级现有挂件。");
            var candidate=Snapshot();
            if(first){candidate.firstClearRewardClaimed=true;candidate.pendingFirstClearReward=false;}
            else candidate.mechanicMaterials-=MechanicExchangeCost;
            candidate.attachments.Add(new MechanicAttachment{id=Guid.NewGuid().ToString("N"),mechanic=mechanic,level=EquipmentGenerationLevel(Profile.level)});
            if(!candidate.discoveredMechanics.Contains(mechanic))candidate.discoveredMechanics.Add(mechanic);
            if(!CommitCandidate(candidate,true))return false;
            PublishRewardMoment(first?RewardMomentKind.FirstCore:RewardMomentKind.MechanicExchange,materials:first?0:-MechanicExchangeCost,attachment:Attachment(mechanic));return true;
        }
        public string AttachmentUpgradeLock(EquipmentMechanic mechanic,bool inCamp)
        {
            var a=Attachment(mechanic);
            if(!inCamp||a==null||BuildCatalog.MechanicClass(mechanic)!=Profile.heroClass)return "请在营地升级本职业挂件。";
            if(a.upgradeRank>=MaximumAttachmentRank)return "挂件已满阶。";
            int required=1+(a.upgradeRank+1)*5;
            if(Profile.level<required)return "角色达到 "+required+" 级后可升下一阶。";
            return Profile.mechanicMaterials<AttachmentUpgradeCost?"需6枚星烬碎片作为升级道具。":"";
        }
        public bool UpgradeAttachment(EquipmentMechanic mechanic,bool inCamp)
        {
            string reason=AttachmentUpgradeLock(mechanic,inCamp);if(reason.Length>0)return Fail(reason);
            var candidate=Snapshot();var a=candidate.attachments.Find(x=>x.mechanic==mechanic);
            a.upgradeRank++;if(a.upgradeRank>=3&&a.rarity<Rarity.Epic)a.rarity=Rarity.Epic;a.level=Math.Max(a.level,Math.Min(Profile.level,1+a.upgradeRank*5));candidate.mechanicMaterials-=AttachmentUpgradeCost;
            return CommitCandidate(candidate,true);
        }
        public bool ToggleAttachmentVariant(EquipmentMechanic mechanic,bool inCamp)
        {
            var a=Attachment(mechanic);
            if(!inCamp||a==null||BuildCatalog.MechanicClass(mechanic)!=Profile.heroClass||!BuildCatalog.HasMechanicVariant(mechanic))return Fail("请在营地选择支持变体的本职业挂件。");
            if(!a.variantUnlocked&&Profile.mechanicMaterials<VariantCost)return Fail("首次学习变体需4枚星烬碎片。");
            var candidate=Snapshot();a=candidate.attachments.Find(x=>x.mechanic==mechanic);
            if(!a.variantUnlocked)candidate.mechanicMaterials-=VariantCost;
            a.variantUnlocked=true;a.variant=1-a.variant;
            if(!candidate.variantKnowledge.Contains(mechanic))candidate.variantKnowledge.Add(mechanic);
            return CommitCandidate(candidate,true);
        }
        public bool AscendAttachment(EquipmentMechanic mechanic,bool inCamp)
        {
            var a=Attachment(mechanic);
            if(!inCamp||a==null||BuildCatalog.MechanicClass(mechanic)!=Profile.heroClass||a.rarity!=Rarity.Epic)return Fail("请在营地选择本职业史诗挂件。");
            if(HighestAdventureTier<AscensionMilestone||Profile.mechanicMaterials<AscensionCost)return Fail("需通关第5阶并准备24枚碎片。");
            var candidate=Snapshot();candidate.attachments.Find(x=>x.mechanic==mechanic).rarity=Rarity.Legendary;candidate.mechanicMaterials-=AscensionCost;
            if(!CommitCandidate(candidate,true))return false;
            PublishRewardMoment(RewardMomentKind.Ascension,materials:-AscensionCost,attachment:Attachment(mechanic));return true;
        }
        public bool CompleteTownActivity(int hub,bool inCamp)
        {
            if(!inCamp||Profile.currentHub!=hub||hub<1||hub>2)return Fail("请到对应城镇服务点办理。");
            var candidate=Snapshot();
            if(hub==1)
            {
                if(candidate.quarryWorkLevel>=candidate.level||candidate.gold<80||candidate.potions<4)return Fail("本等级委托已结算，或物资不足。");
                candidate.gold-=80;candidate.potions-=4;candidate.mechanicMaterials=Math.Min(999999,candidate.mechanicMaterials+3);candidate.quarryWorkLevel=candidate.level;
            }
            else
            {
                if(candidate.starChartTier>=HighestAdventureTier)return Fail("先通关新的冒险阶数，再来记录星图。");
                candidate.fashionThreads=Math.Min(999999,candidate.fashionThreads+(HighestAdventureTier-candidate.starChartTier)*2);candidate.starChartTier=HighestAdventureTier;
            }
            return CommitCandidate(candidate,true);
        }
        private static void CaptureAttachmentPreset(GameProfile profile,BuildPreset preset)
        {
            var mounted=new List<EquipmentMechanic>();var variants=new List<int>();
            foreach(var a in profile.attachments)if(a.mounted&&BuildCatalog.MechanicClass(a.mechanic)==profile.heroClass){mounted.Add(a.mechanic);variants.Add(a.variantUnlocked?a.variant:0);}
            preset.mountedAttachments=mounted.ToArray();preset.attachmentVariants=variants.ToArray();
        }
        private static void ApplyAttachmentPreset(GameProfile candidate,BuildPreset preset)
        {
            if(preset.mountedAttachments==null)return; // Old plans retain the migrated selection.
            foreach(var a in candidate.attachments)
            {
                if(BuildCatalog.MechanicClass(a.mechanic)!=candidate.heroClass)continue;
                int index=Array.IndexOf(preset.mountedAttachments,a.mechanic);a.mounted=index>=0;
                if(index>=0&&a.variantUnlocked&&preset.attachmentVariants!=null&&index<preset.attachmentVariants.Length)a.variant=Clamp(preset.attachmentVariants[index],0,1);
            }
        }
    }
}
