using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    public sealed partial class GameSession
    {
        public RunMechanismEvidence MechanismEvidence {get;}=new RunMechanismEvidence();
        public RunChoices RunChoices { get; private set; } = new RunChoices();
        public bool DungeonSelectionOpen { get; private set; }
        public int SelectedDungeonTier { get; set; } = 1;
        public int MaximumDungeonTier { get { return Progression.HighestUnlockedAdventureTier; } }
        public bool SelectedChallengeMode { get; set; }
        public bool ChallengeRun { get; private set; }
        public int HealingCharges { get; private set; }
        public int DungeonLayout { get; private set; }
        public int DungeonEntryLevel { get; private set; } = 2;
        public string LastRunSummary { get; private set; } = "";
        public bool IsInCamp { get { return !PracticeActive && HasStarted && !InDungeon && !IsDead && Player != null && (Vector3.Distance(Player.transform.position, new Vector3(0,0,-10)) < 7f || NearbyHubNpc!=HubNpcKind.None); } }
        public bool SideEventAvailable { get { return InDungeon && !ChapterActive && ModeRun==null && (RoomChainRun==null || RoomChainRun.Room.Index==RoomTactics.EventRoom(runSeed)&&!RoomChainRun.Finished) && !DungeonCleared && sideCrystal!=null && !sideEventStarted && Player != null && Vector3.Distance(Player.transform.position, sideEventPosition) < 3.5f; } }
        private int runSeed, wavePopulation;
        private readonly Queue<EncounterSpawn> reinforcementQueue=new Queue<EncounterSpawn>();
        private float nextReinforcementAt;
        private readonly Dictionary<string, int> combatActions = new Dictionary<string, int>();
        private readonly HashSet<EnemyController> sideEventEnemies = new HashSet<EnemyController>();
        private Vector3 sideEventPosition = new Vector3(12,0,-3);
        private string lastDamageSource = "未记录";
        private float lastDamageAmount, lastInterruptAt = -10;
        private float runDamageTaken,runHealingReceived;
        public void RecordActualHealing(float amount){if(PracticeActive){PracticeRecord.Healing(amount);return;}if(InDungeon&&HasStarted&&!IsDead&&amount>0&&!float.IsNaN(amount)&&!float.IsInfinity(amount))runHealingReceived+=amount;}
        private bool objectiveHealedThisWave, sideEventStarted;
        private GameObject sideCrystal;

        public bool HasBlessing(RunBlessing blessing) { return InDungeon && RunChoices.Has(blessing); }
        public void CancelDungeonSelection() { DungeonSelectionOpen = false; UpdateTimeScale(); }
        public void ConfirmDungeonSelection()
        {
            if (!DungeonSelectionOpen || InDungeon || IsDead || !NearPortal()) return;
            SelectedDungeonTier = Mathf.Clamp(SelectedDungeonTier, 1, MaximumDungeonTier);
            DungeonSelectionOpen = false;
            bool previousChallengeRun = ChallengeRun;
            ChallengeRun = SelectedChallengeMode;
            if(!ChangeZone(true)){ChallengeRun=previousChallengeRun;DungeonSelectionOpen=true;UpdateTimeScale();Notify(Progression.LastError);return;}
            UpdateTimeScale();
            Notify(ModeName+" · " + DungeonTier + " 阶 · " + (RoomChainRun!=null?RoomTactics.Name(RoomChainRun.Room.Objective):DungeonLayout == 0 ? "双廊" : "断柱") + (ChallengeRun ? " · 限疗挑战" : " · 普通模式"));
        }

        private void ResetExpedition(bool dungeon)
        {
            ClearDungeonSettlement();
            if(!enteringChapter)ResetChapterRun();
            MechanismEvidence.Reset();RunChoices.Reset(); reinforcementQueue.Clear(); nextReinforcementAt=0; DungeonSelectionOpen = false; AbandonSideEvent();
            runDamageTaken=runHealingReceived=0;
            combatActions.Clear(); lastDamageSource = "未记录"; lastDamageAmount = 0; lastInterruptAt = -10; recapGoldLost = 0;
            if (dungeon)
            {
                DungeonEntryLevel = Mathf.Clamp(Progression.Profile.level,2,100);
                runSeed = retryingRoomChain ? roomRetrySeed : Random.Range(0, 1000000);
                DungeonLayout = runSeed % 2;
                HealingCharges = 3;

            }
            else { ChallengeRun = false; HealingCharges = 0; }
            ResetArenaMode(dungeon);
        }

        private void TrySpawnReinforcements()
        {
            if(!InDungeon || IsDead || reinforcementQueue.Count==0 || Time.time<nextReinforcementAt || Enemies.Count>6)return;
            nextReinforcementAt=Time.time+2f;
            int spawned=0;
            while(reinforcementQueue.Count>0 && spawned<4 && Enemies.Count<EncounterPlan.MaximumSimultaneous)
            {
                EncounterSpawn next=reinforcementQueue.Peek(); Vector3 position;
                if(!TrySafeSpawn(new Vector3(next.X,0,next.Z),next.Kind==EnemyKind.Guardian?.65f:.5f,7f,out position))break;
                reinforcementQueue.Dequeue(); SpawnEnemy(next.Kind,DungeonEntryLevel,position,false); spawned++;
            }
            if(spawned>0)Notify("遗迹援军接近 · "+spawned+" 名");
        }

        public bool ConfirmBlessing(int index)
        {
            if(ChapterActive)return false;
            if(RoomChainRun!=null)return ConfirmRoomInterlude(index);
            if(ModeRun!=null)return ConfirmArenaBlessing(index);
            if (!InDungeon || IsDead || DungeonCleared || Enemies.Count > 0 || reinforcementQueue.Count>0 || !RunChoices.Choose(index)) return false;
            DungeonWave++; SpawnDungeonWave(); UpdateTimeScale();
            Notify(DungeonWave == TotalWaves ? "最终波 · 星蚀巨像" : "第 " + DungeonWave + " 波");
            return true;
        }

        public bool TrySpendHealingCharge()
        {
            if (!InDungeon || !ChallengeRun) return true;
            if (HealingCharges <= 0) { ReportControlFailure("skill6","限疗空"); Notify("治疗充能已耗尽；击败守卫完成目标或推进波次可补充。"); return false; }
            HealingCharges--; return true;
        }

        public void RecordCombatAction(string key)
        {
            if(PracticeActive){PracticeRecord.Mechanism(key);return;}
            if (string.IsNullOrEmpty(key)) return;
            int tutorialBit=key=="普攻回能"?1:key=="完美闪避"?2:key=="换装"?8:0;
            if(tutorialBit!=0&&(Progression.Profile.tutorialMask&tutorialBit)==0) { if(Progression.RecordTutorialEvidence(tutorialBit))LogSystem("实战试炼完成一项 · "+key); }
            int count; combatActions.TryGetValue(key, out count); combatActions[key] = Mathf.Min(9999, count + 1);
        }
        public bool ClassTutorialVisible {get{return Progression.Profile.classTutorialCompleted || (Progression.Profile.heroClass==HeroClass.Summoner ? Player!=null && (SummonedCompanion.Count(Player)>0 || SummonedCompanion.Count(Player,true)>0) : Progression.ClassTutorialUsable);}}
        public void RecordClassTutorial(HeroClass hero)
        {
            if(PracticeActive)return;
            if(!HasStarted || Player==null || IsDead || Progression.Profile.classTutorialCompleted)return;
            if(Progression.RecordClassTutorialEvidence(hero))LogSystem("实战试炼 · "+Progression.ClassTutorialText);
        }
        public void RecordIncomingDamage(string source, float amount)
        {
            if (amount <= 0||float.IsNaN(amount)||float.IsInfinity(amount)) return;
            if(PracticeActive){PracticeRecord.IncomingDamage(amount);return;}
            if(InDungeon)runDamageTaken+=amount;
            lastDamageSource = source; lastDamageAmount = amount;
            // Death may be signalled inside TakeDamage before this telemetry callback.
            if (IsDead) LastRunSummary = BuildRunSummary(false);
        }
        public void OnEnemyInterrupted(EnemyController enemy)
        {
            if (enemy == null || Player == null) return;
            if(PracticeActive){PracticeRecord.Mechanism("打断");return;}
            if(!enemy.IsLargeBossCounterWindow)RecordChapterInterrupt(enemy);
            RecordCombatAction("打断");
            if (HasBlessing(RunBlessing.InterruptFlow) && Time.time - lastInterruptAt >= 1f)
            {
                lastInterruptAt = Time.time; Player.RestoreSkillEnergy(12); Notify("断势回流 · +12 能量");
            }
        }

        private void OnExpeditionEnemyKilled(EnemyController enemy)
        {
            if (enemy.StatusEffects != null && enemy.StatusEffects.IsMarked) Player.OnMarkedEnemyKilled();
            if (!InDungeon) return;
            if (!objectiveHealedThisWave && enemy.Kind == EnemyKind.Guardian)
            {
                objectiveHealedThisWave = true;
                if (ChallengeRun) HealingCharges = Mathf.Min(3, HealingCharges + 1);
                if (HasBlessing(RunBlessing.ExecutionMend)) Player.Heal(Player.MaxHealth * .12f);
            }
            RecordSideEventDefeat(enemy);
        }

        private SideEventRun sideEventRun;
        private sealed class PendingSideReward
        {public SideEventRun Run;public ProgressionService Source;public PlayerController Owner;public int Epoch;}
        private readonly List<PendingSideReward> pendingSideRewards=new List<PendingSideReward>();
        private float nextSideRewardRetry;
        private bool sideEventOfferShown;
        private object SideEventContext {get{return RoomChainRun==null?(object)Player:RoomChainRun.Room;}}
        public bool IsSideEventEnemy(EnemyController enemy)
        {return enemy!=null&&!enemy.IsDead&&enemy.gameObject.activeInHierarchy&&sideEventRun!=null&&sideEventRun.Contains(enemy,SideEventContext,Player,Player==null?-1:Player.CombatEpoch,HasStarted&&InDungeon&&!IsDead&&!CombatEnded);}
        public int SideEventEnemiesRemaining
        {get{int count=0;foreach(var enemy in sideEventEnemies)if(IsSideEventEnemy(enemy))count++;return count;}}
        public bool SideEventRewardPending {get{return pendingSideRewards.Count>0;}}
        private void AbandonSideEvent()
        {
            if(sideEventRun!=null)sideEventRun.Abandon();sideEventRun=null;
            sideEventEnemies.Clear();sideEventStarted=false;sideCrystal=null;sideEventOfferShown=false;
        }
        private void RecordSideEventDefeat(EnemyController enemy)
        {
            if(sideEventRun==null||!sideEventRun.Defeat(enemy,SideEventContext,Player,Player==null?-1:Player.CombatEpoch,
                HasStarted&&InDungeon&&!IsDead&&!CombatEnded))return;
            sideEventEnemies.Remove(enemy);
            if(!sideEventRun.Completed)return;
            pendingSideRewards.Add(new PendingSideReward{Run=sideEventRun,Source=Progression,Owner=Player,Epoch=Player.CombatEpoch});
            TrySettleSideEventRewards();
        }
        public bool TrySettleSideEventRewards()
        {
            for(int i=pendingSideRewards.Count-1;i>=0;i--)
            {
                var pending=pendingSideRewards[i];
                // An explicit discard/load must never redirect an old reward into another role.
                if(!object.ReferenceEquals(pending.Source,Progression))continue;
                bool already=pending.Run.Claimed,newlyCommitted=false;
                if(!pending.Run.TryClaim(receipt=>pending.Source.TryGrantSideEventReward(receipt,out newlyCommitted)))
                {Notify("晶核奖励待保存 · 保留领取记录，可重试："+pending.Source.LastError);return false;}
                pendingSideRewards.RemoveAt(i);
                if(already||!newlyCommitted)continue;
                if(Player==pending.Owner&&!IsDead&&Player!=null&&!Player.IsDead&&Player.CombatEpoch==pending.Epoch)
                {if(ChallengeRun)HealingCharges=Mathf.Min(3,HealingCharges+1);else Player.Heal(Player.MaxHealth*.15f);}
                RecordCombatAction("支线");LogSystem("晶核支线完成 · 材料 +1 已保存 · 补给仅恢复原场战斗");
            }
            return true;
        }
        private void DiscardForeignSideEventRewards()
        {pendingSideRewards.RemoveAll(pending=>!object.ReferenceEquals(pending.Source,Progression));}
        private void TickSideEvent()
        {
            if(SideEventAvailable&&!sideEventOfferShown)
            {sideEventOfferShown=true;Notify("可选晶核 · 唤醒2敌，全灭得1材料+补给；北门开启后可随时放弃");}
            if(HasStarted&&SideEventRewardPending&&Time.unscaledTime>=nextSideRewardRetry)
            {nextSideRewardRetry=Time.unscaledTime+2;TrySettleSideEventRewards();}
        }

        private void BuildSideEvent()
        {
            sideEventPosition = RoomChainRun==null?new Vector3(12,0,-3):new Vector3(-RoomTactics.Mirror(runSeed)*12,0,-6);
            if(RoomChainRun!=null&&!WorldTraversal.CanReach(TacticalRoomGeometry.Entrance,sideEventPosition,.65f))return;
            sideCrystal = WorldBuilder.MakeSideEventCrystal(sideEventPosition);
            sideCrystal.name = "Optional power crystal"; transientObjects.Add(sideCrystal);
        }
        public bool StartSideEvent()
        {
            if (!SideEventAvailable || InputBlocked) return false;
            if (RoomChainRun!=null?!SideEventRun.HasRoomCapacity(Enemies.Count):Enemies.Count>EncounterPlan.MaximumSimultaneous-2) { Notify("先清理部分敌人，再唤醒2名晶核守卫；战术房最多8敌。"); return false; }
            Vector3 guardPosition, wispPosition;
            if (!TrySafeSpawn(sideEventPosition+new Vector3(-2,0,2),.65f,5.5f,out guardPosition) ||
                !TrySafeSpawn(sideEventPosition+new Vector3(-1,0,6),.5f,5.5f,out wispPosition) ||
                Vector3.Distance(guardPosition,wispPosition)<2.5f)
            { Notify("晶核附近暂时没有安全来袭位置，请拉开距离后重试。"); return false; }
            sideEventRun=new SideEventRun(SideEventContext,Player,Player.CombatEpoch,System.Guid.NewGuid().ToString("N"));
            int level = DungeonEntryLevel;
            try
            {
                SpawnEnemy(EnemyKind.Guardian,level,guardPosition,false);
                var guardian=Enemies[Enemies.Count-1];sideEventEnemies.Add(guardian);sideEventRun.Register(guardian);
                SpawnEnemy(EnemyKind.Wisp,level,wispPosition,false);
                var wisp=Enemies[Enemies.Count-1];sideEventEnemies.Add(wisp);sideEventRun.Register(wisp);
            }
            catch(System.Exception error)
            {
                foreach(var spawned in sideEventEnemies){Enemies.Remove(spawned);spawned.gameObject.SetActive(false);Destroy(spawned.gameObject);}
                sideEventEnemies.Clear();sideEventRun.Abandon();sideEventRun=null;
                Notify("晶核唤醒失败，可稍后重试："+error.Message);return false;
            }
            foreach(var enemy in sideEventEnemies)SideEventEnemyMarker.Attach(enemy,this);
            sideEventStarted = true;
            if (sideCrystal != null) { Destroy(sideCrystal); sideCrystal = null; }
            Notify("晶核支线2敌 · 全灭得1材料+补给 · 北门已开可放弃"); return true;
        }


    }
}
