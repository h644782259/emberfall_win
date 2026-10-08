using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Rect MobileVisualRect(Rect hit)
        {float k=EffectPreferences.TouchVisualScale;return new Rect(hit.center.x-hit.width*k*.5f,hit.center.y-hit.height*k*.5f,hit.width*k,hit.height*k);}
        private void DrawMobileControlPreferences(float x,float y,float width)
        {
            float column=(width-12)*.5f;
            string position=EffectPreferences.TouchPosition<0?"偏上":EffectPreferences.TouchPosition>0?"偏下":"标准";
            if(Button(TouchRect(x,y,column,48),"技能组位置："+position,jade))EffectPreferences.TouchPosition=EffectPreferences.TouchPosition==1?-1:EffectPreferences.TouchPosition+1;
            if(Button(TouchRect(x+column+12,y,column,48),"按钮透明度："+Mathf.RoundToInt(EffectPreferences.TouchOpacity*100)+"%",jade))EffectPreferences.TouchOpacity=EffectPreferences.TouchOpacity>.9f?.7f:EffectPreferences.TouchOpacity>.6f?.5f:1f;
            if(Button(TouchRect(x,y+58,column,48),"图标："+(EffectPreferences.TouchVisualScale<.9f?"紧凑":"标准"),jade))EffectPreferences.TouchVisualScale=EffectPreferences.TouchVisualScale<.9f?1f:.86f;
            if(Button(TouchRect(x+column+12,y+58,column,48),"恢复默认布局",gold)){EffectPreferences.TouchPosition=0;EffectPreferences.TouchOpacity=1;EffectPreferences.TouchVisualScale=1;}
            if(Button(TouchRect(x,y+116,column,48),"界面字号："+Mathf.RoundToInt(EffectPreferences.InterfaceTextScale*100)+"%",jade))EffectPreferences.CycleInterfaceTextScale();
        }
    }
}
