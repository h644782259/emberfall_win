using UnityEngine;
namespace Emberfall
{
    // One clock outside each action. The arc uses the source timer's grant duration;
    // observing midway never invents a full window or changes gameplay state.
    internal sealed class MobileOpportunityMeter
    {
        private object lastOwner;
        private int lastEpoch;
        private CombatOpportunityKind lastKind;
        private bool hadWindow;
        private float emphasisUntil;
        private GUIStyle style;
        internal void Draw(Rect area,CombatOpportunityState state,object owner,int epoch,float unit,float opacity)
        {
            bool changed=lastOwner!=owner||lastEpoch!=epoch;
            if(state.Window&&(changed||!hadWindow||lastKind!=state.Kind))emphasisUntil=Time.unscaledTime+.35f;
            lastOwner=owner;lastEpoch=epoch;lastKind=state.Kind;hadWindow=state.Window;
            if(!state.Window)return;
            if(style==null)style=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold,font=GameFont.Shared};
            style.fontSize=Mathf.RoundToInt(9*unit);
            Color previous=GUI.color;
            GUI.color=new Color(.025f,.045f,.06f,.95f*opacity);GUI.DrawTexture(area,Texture2D.whiteTexture);
            Color tint=state.Actionable?new Color(.35f,.86f,.64f,opacity):new Color(.58f,.61f,.65f,opacity);
            style.normal.textColor=tint;GUI.color=Color.white;
            // The one-character mechanism seal is distinct from the skill identity.
            GUI.Label(new Rect(area.x+unit,area.y,13*unit,area.height),state.Symbol,style);
            GUI.Label(new Rect(area.x+16*unit,area.y,area.width-16*unit,area.height),state.Remaining.ToString("0.0"),style);
            float radius=6*unit,cx=area.x+7.5f*unit,cy=area.y+area.height*.5f;
            if(state.Duration>0)
                for(int i=0;i<24;i++)
                {
                    float angle=(i/24f*2f-.5f)*Mathf.PI;
                    GUI.color=i/24f<state.Fraction?tint:new Color(.16f,.21f,.24f,opacity);
                    GUI.DrawTexture(new Rect(cx+Mathf.Cos(angle)*radius-.6f*unit,cy+Mathf.Sin(angle)*radius-.6f*unit,1.2f*unit,1.2f*unit),Texture2D.whiteTexture);
                }
            // A single steady entrance underline; never blink every OnGUI/repaint.
            if(Time.unscaledTime<emphasisUntil)
            {GUI.color=tint;GUI.DrawTexture(new Rect(area.x,area.yMax-unit,area.width,unit),Texture2D.whiteTexture);}
            GUI.color=previous;
        }
    }
}
