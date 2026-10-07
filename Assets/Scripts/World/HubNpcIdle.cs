using UnityEngine;
namespace Emberfall
{
    // Fixed rigs; only the actor rotates, while shelves, anvil and lectern stay put.
    internal sealed class HubNpcIdle : MonoBehaviour
    {
        private Transform body,head,arm,otherArm,dial;
        private int role;
        private float age,angle,conversationAge,engagement;
        private bool conversing;
        private Quaternion homeFacing;

        public void Initialize(int kind,Transform actor,Transform face,Transform right,Transform left,Transform ornament)
        {
            role=kind;body=actor;head=face;arm=right;otherArm=left;dial=ornament;
            homeFacing=body.localRotation;
            ApplyPose(0);
        }
        private void Update()
        {
            GameSession session=GameSession.Instance;
            if(session==null||!session.HasStarted||session.BackgroundPaused||session.Paused||session.IsDead)return;
            bool talking=session.ActiveHubNpc==(HubNpcKind)(role+1);
            // Service panels pause combat. Their owner still acknowledges the player.
            float delta=talking?Time.unscaledDeltaTime:Time.deltaTime;
            if(delta<=0)return;
            if(talking&&!conversing)conversationAge=0;
            conversing=talking;
            age+=delta;
            if(talking)conversationAge+=delta;
            engagement=Mathf.MoveTowards(engagement,talking?1:0,delta*4);
            Quaternion facing=homeFacing;
            if(talking&&session.Player!=null)
            {
                Vector3 direction=CombatFx.Flat(session.Player.transform.position-body.position);
                if(direction.sqrMagnitude>.001f)
                    facing=Quaternion.Inverse(body.parent.rotation)*Quaternion.LookRotation(direction,Vector3.up);
            }
            body.localRotation=Quaternion.RotateTowards(body.localRotation,facing,delta*300);
            ApplyPose(delta);
        }

        private void ApplyPose(float delta)
        {
            float swing=role==1?Mathf.Pow(Mathf.Max(0,Mathf.Sin(age*2.2f)),3)*-58:
                role==0?Mathf.Sin(age*1.3f)*14-18:Mathf.Sin(age*.8f)*8-35;
            Quaternion idleRight=Quaternion.Euler(swing,role==0?Mathf.Sin(age*.65f)*14:0,-8);
            Quaternion idleLeft=Quaternion.Euler(role==2?-28:role==1?-12:Mathf.Sin(age*.9f)*6,0,8);
            // A brief greeting, then softer conversational gestures; the smith lowers his hammer.
            float greet=Mathf.Clamp01(1-conversationAge/1.6f);
            float gesture=Mathf.Sin(conversationAge*(greet>0?8:2.3f));
            Quaternion talkRight=role==0?Quaternion.Euler(-65-greet*30,gesture*12,-24-gesture*greet*14):
                role==1?Quaternion.Euler(-8,0,-10):Quaternion.Euler(-58+gesture*7,-18,-22);
            Quaternion talkLeft=Quaternion.Euler(role==1?-48+gesture*9:-25+gesture*6,8,22);
            if(arm!=null)arm.localRotation=Quaternion.Slerp(idleRight,talkRight,engagement);
            if(otherArm!=null)otherArm.localRotation=Quaternion.Slerp(idleLeft,talkLeft,engagement);
            if(head!=null)head.localRotation=Quaternion.Euler(engagement*(Mathf.Sin(conversationAge*3.6f)*5+3),
                engagement*Mathf.Sin(conversationAge*1.4f)*4,0);
            if(body!=null)body.localScale=new Vector3(1,1+Mathf.Sin(age*1.8f)*.004f,1);
            if(dial!=null){angle=Mathf.Repeat(angle+delta*18,360);dial.localRotation=Quaternion.Euler(70,angle,0);}
        }
    }
}
