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
        public readonly Area Joystick, MoveZone, Attack, Dodge, Potion, Jump, Cancel, Menu, Inventory, SkillsMenu, Catalog, Interact;
        public readonly Area EncounterText, BossHealth, Notice, AdventureStatus, FocusCommand, RecallCommand, CombatView, PlayerStatus, PlayerHealth, PlayerEnergy, Map;
        public readonly Area[] Skills = new Area[8];
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
            Attack=Centered(Width-98,Height-55,76);
            Dodge=Centered(Width-30,Height-30,48);
            Jump=Centered(Width-30,Height-99,48);Cancel=Jump;
            float bottomLift=Width<700?36:0;
            Potion=Centered(Width*.5f-108,Height-26,44);
            // Width already excludes Screen.safeArea insets. Never subtract them again.
            Menu=Centered(Width-30,32,48);Inventory=Centered(Width-84,32,48);
            SkillsMenu=Centered(Width-138,32,48);Catalog=Centered(Width-192,32,48);
            Interact=new Area(Width-68,92,60,48);
            float shift=positionPreset<0?-Math.Min(8,Math.Max(0,Height-320)):positionPreset>0?0:0;
            float[] dx={180,180,98,234,234,180,288,240};
            float[] dy={30,97,163,97,163,163,97,30};
            int[] identities={0,1,2,4,5,6,7,9};
            for(int i=0;i<Skills.Length;i++)
            {
                float size=i==7?54:48;
                Skills[i]=Centered(Width-dx[i],Height-dy[i]+shift-bottomLift,size);
                Area key=Skills[i];
                SkillOpportunities[identities[i]]=i==7?new Area(key.X-52,key.Y+17,48,14):
                    new Area(key.X,(i==0||i==1||i==5)?key.Y-15:key.Y+key.Height+1,key.Width,14);
            }
            CounterOpportunity=new Area(Attack.X,Attack.Y+Attack.Height+1,Attack.Width/2,13);
            ComboOpportunity=new Area(Attack.X+Attack.Width/2,Attack.Y+Attack.Height+1,Attack.Width/2,13);
            FocusCommand=new Area(204,Height-170-bottomLift,48,48);RecallCommand=new Area(204,Height-120-bottomLift,48,48);
            PlayerStatus=new Area(Width*.5f-80,Height-35,160,18);
            PlayerHealth=new Area(PlayerStatus.X,PlayerStatus.Y,160,12);
            PlayerEnergy=new Area(PlayerStatus.X,PlayerStatus.Y+15,160,3);Map=new Area(12,12,72,56);
            AdventureStatus=new Area(12,70,188,76);
            // Transient notices replace this left-side information slot, never the battlefield.
            Notice=AdventureStatus;
            EncounterText=new Area(Width-172,60,164,18);BossHealth=new Area(Width-172,81,164,5);
            CombatView=ChooseCombatView(Width*.5f-12,Height*.3f-32,96,64);
        }

        private Area ChooseCombatView(float x,float y,float w,float h)
        {
            Area best=new Area(x+(w-96)/2,y+(h-64)/2,96,64);float bestScore=float.MaxValue;
            for(int i=-1;i<121;i++)
            {
                float cx=i<0?best.X+48:Width*(.25f+(i%11)*.05f),cy=i<0?best.Y+32:Height*(.25f+(i/11)*.05f);
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
            foreach(var control in new[]{MoveZone,Attack,Dodge,Potion,Jump,Menu,Inventory,SkillsMenu,Catalog,Interact,FocusCommand,RecallCommand,Notice,BossHealth,EncounterText,AdventureStatus,PlayerStatus,Map})
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
