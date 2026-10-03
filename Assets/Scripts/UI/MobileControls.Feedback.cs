using UnityEngine;
namespace Emberfall
{
    public sealed partial class MobileControls
    {
        private GUIStyle controlLabel;
        private readonly MobileOpportunityMeter counterMeter=new MobileOpportunityMeter(),comboMeter=new MobileOpportunityMeter();
        private bool LimitedHealing {get{return session.ChallengeRun&&session.InDungeon;}}
        private int PotionCount {get{return LimitedHealing?session.HealingCharges:session.Progression.Profile.potions;}}
        private void CheckPotionFeedback()
        {
            var hero=session.Player;if(hero==null)return;
            string reason=MobileCombatPresentation.Potion(PotionCount,LimitedHealing,hero.Health>=hero.MaxHealth-.5f);
            if(reason.Length>0)session.ReportControlFailure("potion",reason);
        }
        private void CheckDodgeFeedback()
        {
            var hero=session.Player;if(hero==null)return;
            string reason=MobileCombatPresentation.Dodge(hero.DodgeCooldown,hero.IsJumping);
            if(reason.Length>0)session.ReportControlFailure("dodge",reason);
        }
        private void DrawAvailability()
        {
            if(controlLabel==null)controlLabel=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=12,fontStyle=FontStyle.Bold,font=GameFont.Shared};
            controlLabel.normal.textColor=Color.white;
            var hero=session.Player;
            string potionState=MobileCombatPresentation.Potion(PotionCount,LimitedHealing,hero.Health>=hero.MaxHealth-.5f);
            string dodgeState=MobileCombatPresentation.Dodge(hero.DodgeCooldown,hero.IsJumping);
            if(potionState.Length>0)Circle(Potion,new Color(.015f,.035f,.04f,.67f),"");
            if(dodgeState.Length>0)Circle(Dodge,new Color(.015f,.035f,.04f,.67f),"");
            LabelControl(Potion,LimitedHealing?"疗 "+PotionCount:"×"+PotionCount,false);
            if(dodgeState.Length>0)LabelControl(Dodge,hero.DodgeCooldown>.01f?hero.DodgeCooldown.ToString("0.0"):dodgeState,true);
            string failure=session.ControlFailure("potion");if(!string.IsNullOrEmpty(failure))LabelControl(Potion,failure,true);
            else if(potionState=="满血")LabelControl(Potion,potionState,true);
            var combo=hero.BasicOpportunityWindow(true);var ca=Layout.ComboOpportunity;
            comboMeter.Draw(new Rect(ca.X,ca.Y,ca.Width,ca.Height),combo,hero,hero.CombatEpoch,1,EffectPreferences.TouchOpacity);
            var counter=hero.BasicOpportunityWindow();var co=Layout.CounterOpportunity;
            counterMeter.Draw(new Rect(co.X,co.Y,co.Width,co.Height),counter,hero,hero.CombatEpoch,1,EffectPreferences.TouchOpacity);
            string basicReason=hero.MobilePinnedActionReason(-1);
            failure=session.ControlFailure("attack");if(!string.IsNullOrEmpty(failure))basicReason=failure;
            if(basicReason.Length==0)basicReason=counter.Window?counter.BlockReason:combo.Window?combo.BlockReason:"";
            if(basicReason.Length>0)LabelControl(Attack,basicReason,true);
            var pinned=hero.MobilePinnedTarget;
            if(pinned!=null&&Camera.main!=null)
            {
                Vector3 screen=Camera.main.WorldToScreenPoint(pinned.transform.position+Vector3.up*2.6f);
                if(screen.z>Camera.main.nearClipPlane)
                {
                    Vector2 p=ToUI(new Vector2(screen.x,screen.y));
                    var pending=hero.GetComponent<SkillChargeController>();
                    string title=pinned.DisplayName+(pending!=null&&pending.IsCharging?" · 下次固定":" · 固定目标");
                    GUI.Label(new Rect(Mathf.Clamp(p.x-60,0,Layout.Width-120),Mathf.Clamp(p.y-20,80,Layout.Height-120),120,20),title,controlLabel);
                }
            }
            failure=session.ControlFailure("dodge");if(!string.IsNullOrEmpty(failure))LabelControl(Dodge,failure,true);
        }
        private static Rect VisualRect(Rect hit)
        {float ratio=EffectPreferences.TouchVisualScale;return new Rect(hit.center.x-hit.width*ratio*.5f,hit.center.y-hit.height*ratio*.5f,hit.width*ratio,hit.height*ratio);}
        private void LabelControl(Rect area,string text,bool center,bool available=true)
        {
            area=VisualRect(area);
            Rect r=center?new Rect(area.x,area.center.y-9,area.width,18):new Rect(area.x,area.yMax-18,area.width,16);
            GUI.color=new Color(.015f,.025f,.04f,.9f*EffectPreferences.TouchOpacity);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;controlLabel.normal.textColor=available?Color.white:new Color(.58f,.61f,.65f);GUI.Label(r,text,controlLabel);controlLabel.normal.textColor=Color.white;
        }
    }
}
