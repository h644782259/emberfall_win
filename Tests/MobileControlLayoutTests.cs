using System;
using System.Collections.Generic;
using Emberfall;
public static class MobileControlLayoutTests
{
    static int checks;
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    public static string Run()
    {
        checks=0;
        float[][] devices={new[]{568f,320f,163f},new[]{1334f,750f,326f},new[]{2208f,1080f,401f},new[]{2250f,1125f,458f},new[]{2340f,1080f,460f},new[]{2048f,1536f,264f},new[]{2388f,1668f,264f},new[]{2732f,2048f,264f},new[]{1280f,720f,0f}};
        foreach(var preset in new[]{-1,0,1})foreach(var d in devices)
        {
            var l=new MobileControlLayout(d[0],d[1],d[2],preset);
            var targets=new List<MobileControlLayout.Area>{l.Joystick,l.Attack,l.Dodge,l.Potion,l.Jump,l.Menu,l.Inventory,l.SkillsMenu,l.Catalog,l.Interact,l.FocusCommand,l.RecallCommand};targets.AddRange(l.Skills);
            foreach(var r in targets)
            {
                Check(r.Width>=(r.Width==l.Potion.Width?44:48)&&r.Height>=(r.Width==l.Potion.Width?44:48),"minimum 48 touch targets; compact potion retains 44");
                Check(r.X>=0&&r.Y>=0&&r.X+r.Width<=l.Width+.01f&&r.Y+r.Height<=l.Height+.01f,"safe-area contained controls");
            }
            for(int i=0;i<targets.Count;i++)for(int j=i+1;j<targets.Count;j++)Check(!targets[i].Overlaps(targets[j]),"non-overlapping touch hitboxes "+i+"/"+j+" at "+d[0]);
            Check(Math.Abs(l.PlayerStatus.X+l.PlayerStatus.Width/2-l.Width/2)<.01&&l.PlayerStatus.Width==160&&l.PlayerStatus.Height==18&&Math.Abs(l.Height-l.PlayerStatus.Y-l.PlayerStatus.Height-17)<.01,"compact centered bottom vitals");
            foreach(var target in targets)Check(!l.PlayerStatus.Overlaps(target),"vitals clear all action targets");
            foreach(var hint in l.SkillOpportunities)Check(!l.PlayerStatus.Overlaps(hint),"vitals clear opportunity captions");
            Check(!l.PlayerStatus.Overlaps(l.CounterOpportunity)&&!l.PlayerStatus.Overlaps(l.ComboOpportunity),"vitals clear attack feedback");
            Check(l.CombatView.Width>=96&&l.CombatView.Height>=64,"Explicit hero/melee feedback clear window");
            foreach(var target in targets)Check(!l.CombatView.Overlaps(target),"Clear window cannot cover any action hitbox");
            foreach(var overlay in new[]{l.MoveZone,l.Notice,l.AdventureStatus,l.BossHealth,l.EncounterText,l.PlayerStatus,l.Map})Check(!l.CombatView.Overlaps(overlay),"Clear window avoids HUD and joystick zone");
            Check((l.CombatView.X+48)/l.Width>=.25f&&(l.CombatView.X+48)/l.Width<=.75f&&(l.CombatView.Y+32)/l.Height>=.25f&&(l.CombatView.Y+32)/l.Height<=.75f,"Anchor remains central without blind pan");
            Check(l.Skills.Length==8,"seven normal active skills and one ultimate; passive IDs excluded from action targets");
            Check(Math.Abs(l.Width-l.Dodge.X-l.Dodge.Width-6)<.01&&Math.Abs(l.Width-l.Jump.X-l.Jump.Width-6)<.01,"right controls use safe width once, with exactly six units inset");
            Check(l.Height-l.Potion.Y-l.Potion.Height<=28&&l.Potion.Width==44,"potion stays compact near bottom edge");
            Check(Math.Abs(l.Potion.X+l.Potion.Width+6-l.PlayerStatus.X)<.01&&Math.Abs(l.Potion.Y+l.Potion.Height/2-l.PlayerStatus.Y-l.PlayerStatus.Height/2)<.01,"potion hit area stays six units left of centered vitals");
            Check(l.AdventureStatus.X==12&&l.AdventureStatus.Y>=l.Map.Y+l.Map.Height,"objectives follow upper-left minimap");
            Check(l.Interact.X>=l.Width-108&&!l.Interact.Overlaps(l.Potion),"context stays on right edge away from potion");
            Check(l.Skills[7].Width<=l.Skills[0].Width+6,"ultimate identity uses color and ring instead of large size");
            foreach(var skill in l.Skills)Check(skill.Y>=l.Height-231&&skill.X>=l.Width-315,"compact lower-right skill cluster");
            for(int h=0;h<l.SkillOpportunities.Length;h++)foreach(var target in targets)Check(!l.SkillOpportunities[h].Overlaps(target),"opportunity affordance stays outside action targets");
            Check(l.Attack.X>l.Width/2&&l.Joystick.X<l.Width/2,"separate thumb zones");
            Check(l.Cancel.X==l.Jump.X&&l.Cancel.Y==l.Jump.Y,"cancel replaces jump without additional overlap");
            foreach(var feedback in new[]{l.EncounterText,l.BossHealth,l.Notice,l.AdventureStatus})
            {
                Check(feedback.X>=0&&feedback.Y>=0&&feedback.X+feedback.Width<=l.Width&&feedback.Y+feedback.Height<=l.Height,"encounter feedback stays in safe area");
                foreach(var target in targets)Check(!feedback.Overlaps(target),"wave/boss feedback cannot cover a skill or action target");
            }
            Check(!l.AdventureStatus.Overlaps(l.BossHealth)&&!l.AdventureStatus.Overlaps(l.EncounterText),"objective card cannot cover boss bar/label");
            Check(!l.AdventureStatus.Overlaps(l.PlayerStatus)&&!l.AdventureStatus.Overlaps(l.Map),"objective remains below minimap and outside status");
            Check(!l.AdventureStatus.Overlaps(l.MoveZone)&&l.AdventureStatus.X==l.Notice.X&&l.AdventureStatus.Y==l.Notice.Y,"notices reuse the left information slot and never cover movement");
            Check(l.AdventureStatus.Width>=188&&l.AdventureStatus.Height==76,"four lines fit compact phone/iPad objective card");
            Check(!l.EncounterText.Overlaps(l.BossHealth),"wave label and boss health remain separate");
            Check(!l.Notice.Overlaps(l.MoveZone)&&l.Notice.Width>=100&&l.Notice.Height>=48,"full-message touch target stays outside the entire movement zone");
        }
        Check(MobileControlLayout.DeadZone(.1f)==0,"deadzone rejects drift");
        Check(MobileControlLayout.DeadZone(1)==1&&MobileControlLayout.DeadZone(-2)==-1,"movement saturates safely");
        return checks+" mobile control geometry assertions passed";
    }
}
