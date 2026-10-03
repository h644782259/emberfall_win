using System;

namespace Emberfall
{
    /// <summary>Pure geometry in density-independent touch units. No gameplay state or device APIs.</summary>
    public sealed class MobileControlLayout
    {
        public struct Area
        {
            public float X, Y, Width, Height;
            public Area(float x, float y, float w, float h) { X=x;Y=y;Width=w;Height=h; }
            public bool Contains(float x,float y) { return x>=X&&x<X+Width&&y>=Y&&y<Y+Height; }
            public bool Overlaps(Area r) { return X<r.X+r.Width&&X+Width>r.X&&Y<r.Y+r.Height&&Y+Height>r.Y; }
        }
        public readonly float Scale, Width, Height;
        public readonly bool Tablet;
        public readonly Area Joystick, MoveZone, Attack, Dodge, Potion, Jump, Cancel, Menu, Inventory, SkillsMenu, Interact;
        public readonly Area EncounterText, BossHealth, Notice, AdventureStatus, FocusCommand, RecallCommand, CombatView;
        public readonly Area[] Skills = new Area[10];
        public readonly Area[] SkillOpportunities = new Area[10];
        public readonly Area CounterOpportunity, ComboOpportunity;
        public MobileControlLayout(float pixelWidth,float pixelHeight,float dpi,int positionPreset=0)
        {
            pixelWidth=Math.Max(1,pixelWidth);pixelHeight=Math.Max(1,pixelHeight);
            Tablet=pixelWidth/pixelHeight<1.65f;
            float fallback=pixelHeight/(Tablet?768f:390f);
            // Screen.dpi is advisory; reject missing/implausible values and constrain
            // density so 320-point compact phones still fit the full combat cluster.
            float density=dpi>=120&&dpi<=700?dpi/163f:fallback;
            Scale=Math.Max(.25f,Math.Min(density,Math.Min(pixelHeight/320f,pixelWidth/568f)));
            Width=pixelWidth/Scale;Height=pixelHeight/Scale;
            Joystick=Centered(90,Height-86,128);
            MoveZone=new Area(12,Height-172,175,160);
            Attack=Centered(Width-61,Height-57,84);
            Dodge=Centered(Width-152,Height-56,62);
            Jump=Centered(Width-224,Height-56,56);
            Cancel=Jump; // Same thumb position, mutually exclusive with jump.
            Potion=Centered(50,Height-211,54);
            Interact=new Area(78,Height-244,108,48);
            float groupShift=positionPreset<0?-Math.Min(16,Height-316):positionPreset>0?2:0;
            for(int i=0;i<Skills.Length;i++)
                Skills[i]=Centered(Width-252+(i%5)*54,Height-(i<5?204:140)+groupShift,48);
            for(int i=0;i<Skills.Length;i++)SkillOpportunities[i]=new Area(Skills[i].X,Skills[i].Y+Skills[i].Height+1,Skills[i].Width,14);
            CounterOpportunity=new Area(Attack.X,Attack.Y+Attack.Height+1,Attack.Width/2,13);
            ComboOpportunity=new Area(Attack.X+Attack.Width/2,Attack.Y+Attack.Height+1,Attack.Width/2,13);
            FocusCommand=new Area(Skills[0].X-102,Skills[0].Y+1,48,48);
            RecallCommand=new Area(Skills[0].X-50,Skills[0].Y+1,48,48);
            Menu=Centered(Width-32,32,48);
            Inventory=Centered(Width-91,32,48);
            SkillsMenu=Centered(Width-150,32,48);
            // Keep encounter feedback below the menu row, above the skill strip.
            // A centered bar at y=98 crosses the first skill row on 320-unit phones.
            EncounterText=new Area(Width-184,62,172,18);
            BossHealth=new Area(Width-184,83,172,5);
            // Four short objective lines + a real capture bar. At 568x320 the
            // card ends at y=84, above interaction and the first skill row.
            float objectiveWidth=Math.Min(236,Width-380);
            AdventureStatus=new Area((Width-objectiveWidth)/2,8,objectiveWidth,76);
            // The short feedback card uses the gap between the movement zone and
            // jump button, below the skill strip, never covering an action target.
            Notice=new Area(198,Height-106+Math.Max(0,groupShift),Math.Min(320,Jump.X-210),94-Math.Max(0,groupShift));
            float viewLeft=MoveZone.X+MoveZone.Width+3,viewTop=FocusCommand.Y+FocusCommand.Height+4;
            CombatView=ChooseCombatView(viewLeft,viewTop,Skills[0].X-viewLeft-4,Notice.Y-viewTop-4);
        }
        private Area ChooseCombatView(float x,float y,float w,float h)
        {
            Area best=new Area(x+(w-96)/2,y+(h-64)/2,96,64);float bestScore=float.MaxValue;
            for(int i=-1;i<25;i++)
            {
                float cx=i<0?best.X+48:Width*(.35f+(i%5)*.075f),cy=i<0?best.Y+32:Height*(.35f+(i/5)*.075f);
                Area candidate=new Area(cx-48,cy-32,96,64);
                if(!ClearView(candidate))continue;
                float dx=cx/Width-.5f,dy=cy/Height-.5f,score=dx*dx+dy*dy;
                if(score<bestScore){best=candidate;bestScore=score;}
            }
            return best;
        }
        private bool ClearView(Area area)
        {
            if(area.X<0||area.Y<0||area.X+area.Width>Width||area.Y+area.Height>Height)return false;
            foreach(var control in new[]{MoveZone,Attack,Dodge,Potion,Jump,Menu,Inventory,SkillsMenu,Interact,FocusCommand,RecallCommand,Notice,BossHealth,EncounterText,AdventureStatus,new Area(12,12,175,58)})
                if(area.Overlaps(control))return false;
            foreach(var skill in Skills)if(area.Overlaps(skill))return false;
            foreach(var hint in SkillOpportunities)if(area.Overlaps(hint))return false;
            if(area.Overlaps(CounterOpportunity)||area.Overlaps(ComboOpportunity))return false;
            return true;
        }
        private static Area Centered(float x,float y,float size) { return new Area(x-size/2,y-size/2,size,size); }
        public static float DeadZone(float value,float dead=.14f)
        { float magnitude=Math.Abs(value);return magnitude<=dead?0:Math.Sign(value)*Math.Min(1,(magnitude-dead)/(1-dead)); }
    }
}
