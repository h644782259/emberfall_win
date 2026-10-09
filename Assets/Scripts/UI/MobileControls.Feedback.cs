using UnityEngine;
namespace Emberfall
{
    public sealed partial class MobileControls
    {
        private GUIStyle controlLabel,pinnedTargetLabel;
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
            // The bottle remains visible even at full health; state is outside its icon.
            Rect potionVisual=PotionVisualRect();
            var countContent=new GUIContent(PotionCount.ToString());
            Vector2 countSize=controlLabel.CalcSize(countContent);
            float countWidth=Mathf.Max(24,countSize.x+6),countHeight=Mathf.Max(22,countSize.y+4);
            float countRight=Mathf.Min(potionVisual.xMax,Layout.Width-4),countBottom=Mathf.Min(potionVisual.yMax,Layout.Height-4);
            Rect countBadge=new Rect(countRight-countWidth,countBottom-countHeight,countWidth,countHeight);
            GUI.color=new Color(.015f,.025f,.04f,EffectPreferences.TouchOpacity);GUI.DrawTexture(countBadge,Texture2D.whiteTexture);GUI.color=Color.white;
            GUI.Label(countBadge,countContent,controlLabel);
            string failure=session.ControlFailure("potion");
            string potionCaption=!string.IsNullOrEmpty(failure)?failure:potionState;
            if(potionCaption.Length>0&&potionCaption!="满血")GUI.Label(new Rect(Potion.x-8,Potion.y-18,Potion.width+16,16),potionCaption,controlLabel);
            if(dodgeState.Length>0)LabelControl(Dodge,hero.DodgeCooldown>.01f?hero.DodgeCooldown.ToString("0.0"):dodgeState,true);
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
                    if(pinnedTargetLabel==null)pinnedTargetLabel=new GUIStyle(controlLabel)
                    {wordWrap=true,clipping=TextClipping.Overflow,padding=new RectOffset(4,4,3,3)};
                    pinnedTargetLabel.fontSize=controlLabel.fontSize;
                    pinnedTargetLabel.normal.textColor=Color.white;
                    string title=pinned.DisplayName+"\n"+(pending!=null&&pending.IsCharging?"下次固定":"固定目标");
                    var content=new GUIContent(title);
                    float labelWidth=Mathf.Min(Layout.Width-16,Mathf.Max(140,Mathf.Min(240,pinnedTargetLabel.CalcSize(content).x+8)));
                    float labelHeight=pinnedTargetLabel.CalcHeight(content,labelWidth)+4;
                    Rect labelRect=new Rect(Mathf.Clamp(p.x-labelWidth*.5f,8,Layout.Width-labelWidth-8),
                        Mathf.Clamp(p.y-labelHeight,80,Layout.Height-labelHeight-8),labelWidth,labelHeight);
                    GUI.Label(labelRect,content,pinnedTargetLabel);
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
