using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameSession
    {
        public sealed class SystemMessage { public string Text; public float Time; }
        private readonly List<SystemMessage> systemMessages = new List<SystemMessage>();
        private IList<SystemMessage> systemMessagesView;
        public IList<SystemMessage> SystemMessages
        { get { return systemMessagesView ?? (systemMessagesView = systemMessages.AsReadOnly()); } }
        public void LogSystem(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            if (systemMessages.Count >= 32) systemMessages.RemoveAt(0);
            systemMessages.Add(new SystemMessage { Text = message, Time = Time.unscaledTime });
        }
        public void SpawnMechanismText(Vector3 position,string value,Color tint)
        { FloatingNumber.Spawn(position,value,tint,isMechanism:true); }
        public void SpawnCombatDamage(Vector3 position, string value, bool critical)
        {
            FloatingNumber.Spawn(position,value,
                critical ? new Color(1f,.72f,.23f) : new Color(1f,.96f,.85f), critical,
                critical ? "Critical Damage" : "Damage");
        }
        private RunRecapSnapshot lastRunRecap;
        private string recapSlotId;
        public RunRecapSnapshot LastRunRecap
        {
            get { return Progression!=null && Progression.CurrentSlotId==recapSlotId ? lastRunRecap : null; }
            private set { lastRunRecap=value; recapSlotId=Progression.CurrentSlotId; }
        }
        private int recapGoldLost;
        public void RecordRecapGoldLoss(int amount)
        {
            recapGoldLost=Mathf.Max(0,amount);
            if(IsDead)LastRunSummary=BuildRunSummary(false);
        }
        private string BuildRunSummary(bool won,string explicitFailure=null)
        {
            if(won)recapGoldLost=0;
            var blessings=new List<string>();
            foreach(RunBlessing blessing in RunChoices.Active)blessings.Add(Emberfall.RunChoices.Name(blessing));
            var mechanics=new List<string>();
            foreach(EquipmentMechanic mechanic in BuildCatalog.MechanicsFor(Progression.Profile.heroClass))
                if(Progression.HasMechanic(mechanic))mechanics.Add(BuildCatalog.MechanicName(mechanic));
            LastRunRecap=new RunRecapSnapshot(won,InDungeon,ChallengeRun,DungeonTier,DungeonWave,TotalWaves,runSeed,
                Progression.Profile.mechanicMaterials,ProgressionService.MechanicExchangeCost,lastDamageSource,lastDamageAmount,
                combatActions,mechanics,blessings,!SpecialAdventure&&Progression.Profile.pendingFashionChest,!SpecialAdventure&&Progression.Profile.pendingFirstClearReward,
                won?0:recapGoldLost,SpecialAdventure?ModeName:null,explicitFailure??(RoomChainRun!=null?RoomChainRun.Failure.ToString():ModeRun==null?(IsDead?"Death":null):ModeRun.Failure.ToString()),
                modeGoldReward,modeXpReward,modeMaterialReward,
                new RunFailureEvidence(runDamageTaken,runHealingReceived,RoomChainRun!=null&&RoomChainRun.Room!=null?RoomChainRun.Room.Index+1:DungeonWave,
                    RoomChainRun!=null&&RoomChainRun.Room!=null?RoomTactics.Name(RoomChainRun.Room.Objective)+" · 已完成印记 "+RoomChainRun.Seals+" · 当前进度 "+(RoomChainRun.Room.Objective==RoomObjective.Purify?"A "+RoomChainRun.SealProgress(0).ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+"/3秒 · B "+RoomChainRun.SealProgress(1).ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+"/3秒":RoomChainRun.Progress.ToString("0.0")+"秒"):ModeRun!=null?"阶段 "+(ModeRun.PhaseIndex+1)+" · 目标 "+Mathf.RoundToInt(ModeRun.ObjectiveProgress*100)+"%":""),MechanismEvidence.Snapshot(),modeRewardDetailsUnavailable,RoomGenerationFailureDetail);
            // Retain a concise compatibility summary for older consumers. The UI renders the snapshot.
            return won?"遗迹通关 · 第 "+DungeonTier+" 阶":"本次止步 · 最后受击："+lastDamageSource;
        }
    }
}
