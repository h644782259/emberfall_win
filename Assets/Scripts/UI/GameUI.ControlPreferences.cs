using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Rect MobileVisualRect(Rect hit)
        {float k=EffectPreferences.TouchVisualScale;return new Rect(hit.center.x-hit.width*k*.5f,hit.center.y-hit.height*k*.5f,hit.width*k,hit.height*k);}
        private void DrawMobileControlPreferences(float x,float y)
        {
            string position=EffectPreferences.TouchPosition<0?"偏上":EffectPreferences.TouchPosition>0?"偏下":"标准";
            if(Button(TouchRect(x+12,y+57,240,48),"技能组位置："+position,jade))EffectPreferences.TouchPosition=EffectPreferences.TouchPosition==1?-1:EffectPreferences.TouchPosition+1;
            if(Button(TouchRect(x+268,y+57,240,48),"按钮透明度："+Mathf.RoundToInt(EffectPreferences.TouchOpacity*100)+"%",jade))EffectPreferences.TouchOpacity=EffectPreferences.TouchOpacity>.9f?.7f:EffectPreferences.TouchOpacity>.6f?.5f:1f;
            if(Button(TouchRect(x+12,y+115,240,48),"图标："+(EffectPreferences.TouchVisualScale<.9f?"紧凑":"标准"),jade))EffectPreferences.TouchVisualScale=EffectPreferences.TouchVisualScale<.9f?1f:.86f;
            if(DangerButton(TouchRect(x+268,y+115,240,48), "恢复默认布局", gold)){EffectPreferences.TouchPosition=0;EffectPreferences.TouchOpacity=1;EffectPreferences.TouchVisualScale=1;}
            Text(TouchRect(x+12,y+183,496,30),"图标变小，触控范围仍保持至少48。",TouchFont(14),pale,true);
            Text(TouchRect(x+12,y+222,496,50),"布局为角色与近身动作保留中央空隙。\n设置保存在本设备，不改变角色存档。",TouchFont(12),muted,false,true);
        }
    }
}
