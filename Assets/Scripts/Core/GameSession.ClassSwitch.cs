using System;
using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameSession
    {
        private readonly PlayerController.ClassRuntimeArchive[] classRuntimeArchives=new PlayerController.ClassRuntimeArchive[4];
        private ProgressionService classRuntimeOwner;private PlayerController classRuntimeHero;private string classRuntimeSlot;
        private bool classSwitchBusy;private int classSwitchFrame=-1;
        public string ClassSwitchError {get;private set;}
        public string ClassSwitchLockReason(){return ClassSwitchLockReason(false);}
        private string ClassSwitchLockReason(bool preparing)
        {
            if(classSwitchBusy&&!preparing)return "正在切换职业。";
            if(!HasStarted||IsDead||Player==null||Paused||BackgroundPaused)return "当前无法切换职业。";
            if(InCombat)return "正在战斗，请先脱离战斗。";
            if(!IsInCamp||InDungeon||PracticeActive)return "请在营地切换职业。";
            if(ui!=null&&ui.ClassSwitchHasPendingEdit)return "请先完成或取消当前草稿与确认。";
            if(Player.HasClassSwitchTransientState)return "请等施法、动作与临时效果结束后切换。";
            if(SummonedCompanion.HasPracticeTimedState(Player))return "请等伙伴的契约强化、护契或共鸣结束。";
            foreach(var root in gameObject.scene.GetRootGameObjects())
                if(root.activeSelf&&(root.GetComponentInChildren<CombatProjectile>()!=null||root.GetComponentInChildren<CombatArea>()!=null||
                    root.GetComponentInChildren<AdvancedSkillSequence>()!=null||root.GetComponentInChildren<SummonerSpell>()!=null))return "请等场上的飞行弹体或持续技能结束。";
            return string.Empty;
        }
        private static void ClassSwitchCleanup(Action cleanup){try{cleanup();}catch(Exception error){Debug.LogException(error);}}
        public bool TrySwitchClass(HeroClass target)
        {
            var randomBefore=UnityEngine.Random.state;
            ClassSwitchError=ClassSwitchLockReason();if(ClassSwitchError.Length>0)return false;
            if(classSwitchFrame==Time.frameCount){ClassSwitchError="本次点击已处理，请松开后重试。";return false;}
            classSwitchFrame=Time.frameCount;
            var transaction=Progression.PrepareClassSwitch(target,IsInCamp);
            if(transaction==null){ClassSwitchError=Progression.LastError;return false;}
            var old=Player;var oldRuntime=old.CaptureClassRuntime();GameObject stagedObject=null;
            classSwitchBusy=true;
            try
            {
                if(classRuntimeOwner!=Progression||classRuntimeHero!=old||classRuntimeSlot!=Progression.CurrentSlotId)
                {Array.Clear(classRuntimeArchives,0,4);classRuntimeOwner=Progression;classRuntimeHero=old;classRuntimeSlot=Progression.CurrentSlotId;}
                // Build all fallible visual components while the original owner remains active.
                stagedObject=new GameObject("Prepared class - "+GameBalance.ClassName(target));stagedObject.SetActive(false);
                stagedObject.transform.position=old.transform.position;stagedObject.transform.rotation=old.transform.rotation;
                var prepared=stagedObject.AddComponent<PlayerController>();prepared.Initialize(this,target);
                prepared.InstallClassRuntime(oldRuntime,classRuntimeArchives[(int)target],transaction.Candidate,transaction.PreviewStats);
                ClassSwitchError=ClassSwitchLockReason(true);if(ClassSwitchError.Length>0)return false;
                if(!Progression.CommitClassSwitch(transaction,IsInCamp)){ClassSwitchError=Progression.LastError;return false;}
                Player=prepared;
                classRuntimeArchives[(int)old.HeroClass]=oldRuntime;classRuntimeHero=prepared;
                // From here the persisted owner is authoritative. Presentation cleanup
                // cannot roll back or destroy the newly installed controller.
                stagedObject=null;
                ClassSwitchCleanup(()=>old.RetireForClassSwitch());
                ClassSwitchCleanup(()=>SummonedCompanion.RetireOwner(old));
                old.gameObject.SetActive(false);Destroy(old.gameObject);prepared.gameObject.SetActive(true);
                ClassSwitchCleanup(()=>MobileControls.ResetInput());if(ui!=null)ClassSwitchCleanup(()=>ui.OnClassSwitched());
                Progression.PublishClassSwitch(transaction);
                Notify("已切换为"+GameBalance.ClassName(target)+"；装备与共享成长保留。");return true;
            }
            catch(Exception error)
            {
                Debug.LogException(error);
                if(transaction.Committed){Progression.PublishClassSwitch(transaction);ClassSwitchError="职业已保存；界面刷新异常，请重新打开工坊。";return true;}
                ClassSwitchError="职业切换未保存，请重试或检查日志。";return false;
            }
            finally
            {
                try { if(stagedObject!=null)Destroy(stagedObject); }
                finally
                {
                    // Failed preparation must not alter the shared combat RNG stream.
                    if(!transaction.Committed)UnityEngine.Random.state=randomBefore;
                    classSwitchBusy=false;
                }
            }
        }
    }
}
