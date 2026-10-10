using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private void DrawRecoveringSkill(Rect rect,Texture2D icon,Color tint,int skill,bool ready)
       {
           var player=session.Player;
           float remaining=player==null?0:player.SkillCooldownRemaining(skill);
           float period=GameBalance.EffectiveCooldown(session.Progression.Profile.heroClass,skill,session.Progression.Profile.skillRanks[skill]);
           if(player!=null&&skill==SkillStockRules.Skill(session.Progression.Profile.heroClass)&&player.SkillCharges(skill)==0)
           {remaining=player.SkillRechargeRemaining(skill);period=player.SkillRechargePeriod(skill);}
           if(remaining>.01f&&period>0)
           {
               DrawIcon(rect,icon,new Color(.28f,.32f,.37f,.65f));
               float fill=1-Mathf.Clamp01(remaining/period),filled=rect.height*fill;
               if(filled>0)
               {
                   GUI.BeginGroup(new Rect(rect.x,rect.yMax-filled,rect.width,filled));
                   DrawIcon(new Rect(0,filled-rect.height,rect.width,rect.height),icon,tint);
                   GUI.EndGroup();
                   Fill(new Rect(rect.x,rect.yMax-filled,rect.width,1),jade);
               }
           }
           else {DrawIcon(rect,icon,tint);if(ready)Border(rect,new Color(.35f,1f,.72f,.8f));}
       }
       private void DrawExperienceBadge(Rect rect,float unit)
       {
           Rect badge=new Rect(rect.x+unit,rect.y+unit,24*unit,12*unit);
           Fill(badge,new Color(.015f,.035f,.055f,.95f));
           Text(badge,"EXP",Mathf.RoundToInt(8*unit),new Color(.45f,1f,.8f),true,false,TextAnchor.MiddleCenter);
       }

        // Same identity on lists, tree nodes, details and battle controls. State
        // captions live outside the glyph; rank marks are not replacement text.
        private void DrawSkillIdentity(Rect r, HeroClass hero, int skill, int rank, bool available, int rasterSize = 32)
        {
            bool passive = GameBalance.IsPassive(skill);
            Color accent = UIIconAtlas.SkillColor(hero, skill);
            Fill(r, new Color(.035f,.065f,.085f,.9f));
            Border(r, available ? accent : muted * .55f);
            float unit = Mathf.Max(.5f,r.width/32f);
            if (passive) Border(new Rect(r.x+2*unit,r.y+2*unit,r.width-4*unit,r.height-4*unit),available?accent*.65f:muted*.4f);
            float inset = 4*unit;
            DrawIcon(new Rect(r.x+inset,r.y+inset,r.width-2*inset,r.height-2*inset),UIIconAtlas.Skill(hero,skill,rasterSize),available?Color.white:new Color(.5f,.55f,.6f,.85f));
            for(int mark=0;mark<3;mark++)
                Fill(new Rect(r.center.x+(mark-1)*6*unit-1.5f*unit,r.yMax-4*unit,3*unit,2*unit),mark<rank?accent:muted*.35f);
        }
    }
}
