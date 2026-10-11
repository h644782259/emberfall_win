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
            // One identity-colored inner frame; no common green/blue outer rim.
            if(MobileControls.Active)
            {
                DrawIcon(rect,UIIconAtlas.ControlDisc(),new Color(.02f,.035f,.05f,.94f));
                Color frame=UIIconAtlas.SkillColor(session.Progression.Profile.heroClass,skill);
                if(ready)DrawIcon(rect,UIIconAtlas.ControlRing(true),new Color(frame.r,frame.g,frame.b,.2f));
                DrawIcon(rect,UIIconAtlas.ControlRing(),new Color(frame.r,frame.g,frame.b,ready?.9f:.45f));
            }
            if(remaining>.01f&&period>0)
            {
                float fill=1-Mathf.Clamp01(remaining/period),filled=rect.height*fill;
                DrawIcon(rect,AuthoredIconArt.Cooldown(icon),new Color(.13f,.16f,.20f,.9f));
                if(filled>0)
                {
                    GUI.BeginGroup(new Rect(rect.x,rect.yMax-filled,rect.width,filled));
                    DrawIcon(new Rect(0,filled-rect.height,rect.width,rect.height),icon,Color.white);
                    GUI.EndGroup();
                    // Clip the moving boundary to the circular backing.
                    float edge=Mathf.Max(1,rect.width*.035f);
                    GUI.BeginGroup(new Rect(rect.x,rect.yMax-filled,rect.width,Mathf.Min(edge,filled)));
                    DrawIcon(new Rect(0,filled-rect.height,rect.width,rect.height),UIIconAtlas.ControlDisc(),new Color(.72f,.94f,1f));
                    GUI.EndGroup();
                }
                if(MobileControls.Active)
                {
                    Rect track=new Rect(rect.x+rect.width*.2f,rect.yMax-rect.height*.18f,rect.width*.6f,Mathf.Max(2,rect.height*.06f));
                    Fill(track,new Color(.01f,.02f,.035f));
                    Fill(new Rect(track.x,track.y,track.width*fill,track.height),new Color(.48f,.86f,1f));
                }
            }
            else
            {
                // Fully restored material colors provide the ready highlight.
                DrawIcon(rect,ready?icon:AuthoredIconArt.Cooldown(icon),ready?Color.white:new Color(.62f,.67f,.73f,.85f));
            }
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
            Color accent = passive ? new Color(.75f,.65f,.94f) : GameBalance.ClassColor(hero);
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
