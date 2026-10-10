using UnityEngine;
using System.Collections.Generic;
namespace Emberfall
{
    // Independent visual teardown: no reference to EnemyController, reward, damage,
    // collision or attack state. The existing model retains its owned palette/mesh.
    internal sealed class LargeBossShutdownVisual:MonoBehaviour
    {
        private static int live;
        private static readonly List<LargeBossShutdownVisual> active=new List<LargeBossShutdownVisual>();
        private static GameSession skippedSession;private static PlayerController skippedOwner;private static int skippedEpoch;
        private bool leased;
        private CombatModel model;private LargeBossRig rig;private GameSession session;private PlayerController owner;
        private int epoch;private float age;private bool finalBoss;
        private const float Duration=2.5f;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry(){live=0;active.Clear();skippedSession=null;skippedOwner=null;}
        internal static bool IsPresenting(GameSession game)
        {foreach(var v in active)if(v!=null&&v.leased&&v.gameObject.activeInHierarchy&&v.finalBoss&&v.session==game&&v.owner!=null&&game!=null&&game.Player==v.owner&&v.owner.CombatEpoch==v.epoch)return true;return false;}
        internal static void Skip(GameSession game)
        {if(game==null)return;FilledSkillVfx.SkipFinales(game);skippedSession=game;skippedOwner=game.Player;skippedEpoch=skippedOwner==null?-1:skippedOwner.CombatEpoch;foreach(var v in active.ToArray())if(v!=null&&v.session==game)v.Retire();}
        internal static bool Detach(CombatModel model,LargeBossRig rig,GameSession session)
        {
            if(model==null||rig==null||session==null||model.GetComponent<LargeBossShutdownVisual>()!=null)return false;
            if(session.FinalBossEncounter&&live>=2)foreach(var prior in active.ToArray())if(prior!=null&&!prior.finalBoss)prior.Retire();
            if(live>=2)return false;
            if(skippedSession==session&&skippedOwner!=null&&session.Player==skippedOwner&&skippedOwner.CombatEpoch==skippedEpoch)return false;
            model.transform.SetParent(null,true);
            var visual=model.gameObject.AddComponent<LargeBossShutdownVisual>();visual.leased=true;live++;active.Add(visual);
            visual.finalBoss=session.FinalBossEncounter;
            visual.model=model;visual.rig=rig;visual.session=session;visual.owner=session.Player;
            visual.epoch=visual.owner!=null?visual.owner.CombatEpoch:0;
            rig.BeginShutdown();model.BeginDeath();return true;
        }
        private void Update()
        {
            if(session==null||!session.HasStarted||owner==null||session.Player!=owner||owner.CombatEpoch!=epoch||model==null||rig==null){Retire();return;}
            // Completion UI may pause gameplay; this already-dead pure visual still finishes.
            if(session.Paused||session.BackgroundPaused)return;
            age+=Time.unscaledDeltaTime;float duration=finalBoss?Duration:1.35f;float t=Mathf.Clamp01(age/duration);
            rig.SampleShutdown(t);model.SetDeathOpacity(1-Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.55f)/.45f)));
            if(age>=duration)Retire();
        }
        private void OnApplicationPause(bool paused){if(paused)Retire();}
        private void Retire(){gameObject.SetActive(false);Destroy(gameObject);}
        private void OnDisable(){Release();}
        private void OnDestroy(){Release();}
        private void Release(){if(!leased)return;leased=false;active.Remove(this);live=Mathf.Max(0,live-1);}
    }
}
