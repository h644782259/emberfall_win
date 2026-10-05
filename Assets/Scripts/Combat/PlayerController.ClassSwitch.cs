using UnityEngine;
namespace Emberfall
{
    public sealed partial class PlayerController
    {
        internal sealed class ClassRuntimeArchive
        {
            internal SkillRuntime Skills;internal MasteryCoreRuntime Core;
            internal float At,HealthFraction,Dodge,Attack,Passive,Returning,Venom,Frost,StarterRetry;
            internal int NextCast,Epoch;
        }
        internal bool HasClassSwitchTransientState
        {
            get
            {
                return IsDead||jumping||executingChargedSkill||charge!=null&&charge.IsCharging||targeting!=null&&targeting.IsTargeting||
                    attackAnimation>0||hurtTimer>0||invulnerability>0||guardTime>0||healingProtectionTime>0||mobilityTime>0||slowTime>0||
                    movementSkillLock>0||passiveTime>0||perfectDodgeCounterTime>0||blinkBufferTime>0||perfectDodgeWindow>0||counterTime>0||
                    dodgeShockTime>0||chargedWardTime>0||pursuitTime>0||classDodgeTime>0||burnStrideTime>0||coreWardTime>0||focusTime>0||skillBasicRecovery.Blocked||masteryCore.ComboRemaining>0;
            }
        }
        internal ClassRuntimeArchive CaptureClassRuntime()
        {
            return new ClassRuntimeArchive{Skills=skillRuntime.CopyForClass(HeroClass,0,Energy),Core=masteryCore.CopyForClassArchive(),At=Time.time,
                HealthFraction=MaxHealth>0?Health/MaxHealth:0,StarterRetry=starterRetry,Dodge=dodgeCooldown,Attack=attackCooldown,Passive=passiveCooldown,
                Returning=returningBladeProc.Remaining,Venom=venomSpreadProc.Remaining,Frost=openingFrostProc.Remaining,NextCast=nextCastId,Epoch=CombatEpoch};
        }
        internal void InstallClassRuntime(ClassRuntimeArchive shared,ClassRuntimeArchive previous,GameProfile candidate,StatBlock preparedStats)
        {
            var own=previous??shared;float elapsed=Mathf.Max(0,Time.time-own.At);
            skillRuntime=own.Skills.CopyForClass(HeroClass,elapsed,shared.Skills.Energy,shared.Skills);
            stats=preparedStats;MaxHealth=Mathf.Max(1,stats.MaxHealth);Health=Mathf.Clamp01(shared.HealthFraction)*MaxHealth;
            int core=candidate.masteryCore;masteryCore.Configure(core,core>=0&&core<4?candidate.masteryRanks[core]:0);
            masteryCore.RestoreClassArchive(previous==null?null:own.Core,elapsed,shared.Core);
            starterRetry=HeroClass==HeroClass.Summoner?Mathf.Max(0,own.StarterRetry-elapsed):0;
            dodgeCooldown=shared.Dodge;attackCooldown=shared.Attack;passiveCooldown=Mathf.Max(shared.Passive,Mathf.Max(0,own.Passive-elapsed));
            returningBladeProc.TryTrigger(Mathf.Max(shared.Returning,Mathf.Max(0,own.Returning-elapsed)));
            venomSpreadProc.TryTrigger(Mathf.Max(shared.Venom,Mathf.Max(0,own.Venom-elapsed)));
            openingFrostProc.TryTrigger(Mathf.Max(shared.Frost,Mathf.Max(0,own.Frost-elapsed)));
            nextCastId=System.Math.Max(shared.NextCast,own.NextCast);CombatEpoch=shared.Epoch+1;
            suppressBasicUntilReleased=true;aimPoint=transform.position+transform.forward*5;
        }
        internal void RetireForClassSwitch()
        {
            CombatEpoch++;EnsureCastReceiptEpoch();ClearMobilePinnedTarget();CancelCombatPose();
            if(targeting!=null)targeting.Cancel();if(charge!=null)charge.Cancel();
            CancelTransientInput();AimTarget=null;focusedEnemy=null;aimGeometry.Clear();
            SummonedCompanion.RetireOwner(this);
        }
    }
}
