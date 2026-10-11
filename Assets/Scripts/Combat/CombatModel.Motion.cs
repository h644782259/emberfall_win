using UnityEngine;
namespace Emberfall
{
    public sealed partial class CombatModel
    {
        private readonly LocomotionPoseState locomotion = new LocomotionPoseState();
        private EnemyPosePhase enemyActionPhase;
        private float enemyActionProgress;
        public void SetLocomotion(Vector3 localDisplacement,float delta,float referenceSpeed,bool walking,bool airborne=false,float jumpProgress=0)
        { RecordPilotWalking(localDisplacement,delta,referenceSpeed,walking,airborne); pilotAirborne=airborne; locomotion.Advance(localDisplacement.x,localDisplacement.z,delta,referenceSpeed,walking,airborne,jumpProgress); }
        public void ResetLocomotion() { performanceRemaining=0;pilotWalkingKnown=false;pilotAcceptedWalkingWorld=Vector3.zero;locomotion.Reset(); visualMotion.Reset(); visualYawReady=false; visualMotionFrame=-1; recoveryAge=1; }
        public void SetEnemyAttackPose(EnemyPosePhase state,float progress)
        { enemyActionPhase=state;enemyActionProgress=Mathf.Clamp01(progress); }
        private void ApplyHeroLocomotion()
        {
            HeroMotionStyle style=HeroMotionStyle.For(heroClass);
            float stride=Mathf.Sin(locomotion.Phase),jump=locomotion.JumpTuck,land=locomotion.Landing;
            float forward=stride*locomotion.Forward,side=stride*locomotion.Side;
            leftLeg.localRotation=Quaternion.Euler(forward*style.Step-jump*30-land*12,0,-style.Stance+side*style.SideStep);
            rightLeg.localRotation=Quaternion.Euler(-forward*style.Step-jump*20-land*12,0,style.Stance-side*style.SideStep);
            leftKnee.localRotation=Quaternion.Euler(Mathf.Max(0,-forward)*42+jump*62+land*25,0,0);
            rightKnee.localRotation=Quaternion.Euler(Mathf.Max(0,forward)*42+jump*52+land*25,0,0);
            float load=visualMotion.SideLoad;
            pelvis.localPosition+=new Vector3(load*.045f,-style.Crouch-land*.065f-Mathf.Abs(load)*.012f,0);
            leftKnee.localRotation*=Quaternion.Euler(Mathf.Max(0,-load)*11,0,0);
            rightKnee.localRotation*=Quaternion.Euler(Mathf.Max(0,load)*11,0,0);
            spine.localRotation*=Quaternion.Euler(locomotion.CloakPitch*.16f*style.Torso,0,-locomotion.Side*5*style.Torso);
            float clothSide=(locomotion.CloakSide-visualMotion.Turn*10)*style.Cloth;
            float clothPitch=(locomotion.CloakPitch+visualMotion.ForwardDrag*3)*style.Cloth;
            spine.localRotation*=Quaternion.Euler(0,-visualMotion.Turn*4*style.Torso,-load*3*style.Torso);
            cloak.localRotation=Quaternion.Euler(8+clothPitch+jump*8,clothSide,side*3);
            if(tailoredCloth!=null)tailoredCloth.SetInertia(clothPitch,clothSide);
            ApplyAnatomicalGait(actionDuration>0&&actionAge<actionDuration||pilotCharging);
        }
    }
}
