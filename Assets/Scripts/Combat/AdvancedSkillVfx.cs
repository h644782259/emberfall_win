using UnityEngine;

namespace Emberfall
{
    // Cancellable owner of charge envelopes. FilledSkillVfx supplies the reusable
    // world-space volumes; attack event code remains the sole source of damage.
    internal sealed class AdvancedSkillVfx : MonoBehaviour
    {
        private const int MaximumEffects=36;
        private static int activeEffects;
        private PlayerController owner;
        private int epoch;
        private float age,duration;
        private bool follow,registered;
        private System.Func<bool> stateActive;
        private int protectionChannel=-1;
        private static readonly System.Collections.Generic.Dictionary<PlayerController,AdvancedSkillVfx[]> protections=new System.Collections.Generic.Dictionary<PlayerController,AdvancedSkillVfx[]>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCount(){activeEffects=0;protections.Clear();}

        public static AdvancedSkillVfx Rune(PlayerController hero,Vector3 at,float size,Color color,float lifetime,int detail,bool followHero=false,int identity=0,bool protectionEnvelope=false,int protectionStyle=0)
        {
            if(hero==null||hero.IsDead||activeEffects>=MaximumEffects)return null;
            var obj=new GameObject("Cancellable filled charge envelope");obj.transform.position=at;
            var fx=obj.AddComponent<AdvancedSkillVfx>();fx.owner=hero;fx.epoch=hero.CombatEpoch;
            fx.duration=Mathf.Clamp(lifetime,.12f,12);fx.follow=followHero;fx.registered=true;activeEffects++;
            if(identity==5)
            {
                var rim=CombatFx.Ring(at,size,color,fx.duration,.12f,false,true);
                if(rim!=null)rim.transform.SetParent(fx.transform,true);
            }
            else FilledSkillVfx.Charge(fx.transform,hero,at,size,color,fx.duration,identity,protectionEnvelope,protectionStyle);
            return fx;
        }
        // This non-pooled anchor owns only its own child hierarchy, never a rented
        // FilledSkillVfx reference. State cancellation is evaluated even while paused.
        internal static AdvancedSkillVfx Protection(PlayerController hero,Vector3 at,float radius,Color color,float lifetime,int detail,System.Func<bool> active,bool passive=false)
        {
            if(hero==null)return null;
            int channel=passive?1:0;AdvancedSkillVfx[] previous;
            if(protections.TryGetValue(hero,out previous)&&previous[channel]!=null)previous[channel].Retire();
            var fx=Rune(hero,at,radius,color,lifetime,detail,true,4,true,passive?1:0);
            if(fx!=null)
            {
                AdvancedSkillVfx[] channels;if(!protections.TryGetValue(hero,out channels)){channels=new AdvancedSkillVfx[3];protections[hero]=channels;}
                fx.stateActive=active;fx.protectionChannel=channel;channels[channel]=fx;
            }
            return fx;
        }
        // Healing is a third state channel, independent of guard/passive protection.
        internal static AdvancedSkillVfx Healing(PlayerController hero,float radius,Color color,float lifetime,int detail,System.Func<bool> active)
        {
            if(hero==null)return null;
            AdvancedSkillVfx[] channels;
            if(protections.TryGetValue(hero,out channels)&&channels[2]!=null)channels[2].Retire();
            var fx=Rune(hero,hero.transform.position,radius,color,lifetime,detail,true,4,true,2);
            if(fx!=null)
            {
                if(!protections.TryGetValue(hero,out channels)){channels=new AdvancedSkillVfx[3];protections[hero]=channels;}
                fx.stateActive=active;fx.protectionChannel=2;channels[2]=fx;
            }
            return fx;
        }
        internal void Stop(){Retire();}
        public static void Beam(PlayerController hero,Vector3 start,Vector3 end,Color color,float lifetime,float width=.18f)
        {FilledSkillVfx.Thrust(hero,start,end,color,lifetime,width,CombatVisualPriority.ActionBody);}
        public static void FallingBlade(PlayerController hero,Vector3 at,Color color,float scale=1f)
        {
            // Called on the real hit: the descending streak and ground rupture are
            // immediate. There is no cosmetic delayed arrival after damage has landed.
            FilledSkillVfx.Impact(hero,at,1.6f*scale,FilledVfxKind.Sword,color,CombatVisualPriority.RealContact);
        }
        private void Update()
        {
            var game=GameSession.Instance;
            if(owner==null||owner.IsDead||owner.CombatEpoch!=epoch||game==null||game.Player!=owner||!game.HasStarted||game.ModeFinished)
            {Retire();return;}
            if(stateActive!=null&&!stateActive()){Retire();return;}
            if(game.InputBlocked||Time.deltaTime<=0)return;
            age+=Time.deltaTime;if(age>=duration){Retire();return;}
            if(follow)transform.position=owner.transform.position;
        }
        private void Retire(){gameObject.SetActive(false);Destroy(gameObject);}
        private void Release()
        {
            if(registered){registered=false;activeEffects=Mathf.Max(0,activeEffects-1);}
            AdvancedSkillVfx[] channels;
            if(protectionChannel>=0&&!object.ReferenceEquals(owner,null)&&protections.TryGetValue(owner,out channels)&&channels[protectionChannel]==this)
            {channels[protectionChannel]=null;if(channels[0]==null&&channels[1]==null&&channels[2]==null)protections.Remove(owner);}
        }
        private void OnDisable(){Release();}
        private void OnDestroy(){Release();}
    }
}
