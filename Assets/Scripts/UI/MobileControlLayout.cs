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
        public readonly float Scale, Width, Height, UiZoom;
        public readonly bool Tablet;
        public readonly Area Joystick, MoveZone, Attack, Dodge, Potion, Jump, Cancel, Menu, Inventory, SkillsMenu, Catalog, Interact, SkillPage, DungeonEntrance, Shop, Smith;
        public readonly Area EncounterText, BossHealth, Notice, AdventureStatus, FocusCommand, RecallCommand, CombatView, PlayerStatus, PlayerHealth, PlayerEnergy, Map;
        public readonly Area[] Skills = new Area[5];
        public readonly Area[] SkillOpportunities = new Area[10];
        public readonly Area CounterOpportunity, ComboOpportunity;
        public MobileControlLayout(float pixelWidth,float pixelHeight,float dpi,int positionPreset=0, bool ipad=false)
        {
            pixelWidth=Math.Max(1,pixelWidth);pixelHeight=Math.Max(1,pixelHeight);
            Tablet=pixelWidth/pixelHeight<1.65f;
            float fallback=pixelHeight/(Tablet?768f:390f);
            // Screen.dpi is advisory; reject missing/implausible values and constrain
            // density so 320-point compact phones still fit the full combat cluster.
            float density=dpi>=120&&dpi<=700?dpi/163f:fallback;
            float nativeScale=Math.Max(.25f,Math.Min(density,Math.Min(pixelHeight/320f,pixelWidth/568f)));
            // One device factor for rendering, fonts and input. Preserve the minimum
            // landscape viewport instead of enlarging controls beyond its edges.
            UiZoom=ipad?Math.Max(1f,Math.Min(1.4f,Math.Min(pixelWidth/(568f*nativeScale),pixelHeight/(320f*nativeScale)))):1f;
            Scale=nativeScale*UiZoom;
            Width=pixelWidth/Scale;Height=pixelHeight/Scale;
            Joystick=Centered(90,Height-86,128);
            float moveTop=Math.Max(164,Height-172);
            MoveZone=new Area(12,moveTop,175,Height-12-moveTop);
            Attack=Centered(Width-98,Height-55,76);
            Dodge=Centered(Width-30,Height-30,48);
            Jump=Centered(Width-30,Height-99,48);Cancel=Jump;
            float bottomLift=Width<700?36:0;
            Potion=Centered(Width*.5f-108,Height-30,52);
            // Width already excludes Screen.safeArea insets. Never subtract them again.
            Menu=Centered(Width-28,32,44);Inventory=Centered(Width-74,32,44);
            SkillsMenu=Centered(Width-120,32,44);Catalog=Centered(Width-166,32,44);
            Shop=Centered(Width-258,32,44);Smith=Centered(Width-212,32,44);
            Interact=new Area(Width-54,92,48,48);
            DungeonEntrance=new Area(Width*.5f-58,60,116,44);
            float shift=positionPreset<0?-Math.Min(8,Math.Max(0,Height-320)):positionPreset>0?0:0;
            // Equal-size skills follow a 30-degree arc with one shared chord length.
            const float radius=132,diameter=48;
            float chord=2*radius*(float)Math.Sin(Math.PI/12);
            for(int i=0;i<4;i++)
            {
                double angle=i*Math.PI/6;
                Skills[i]=Centered(Width-78-radius*(float)Math.Cos(angle),Height-30-radius*(float)Math.Sin(angle)+shift-bottomLift,diameter);
            }
            Skills[4]=Centered(Width-78-radius-chord,Height-30+shift-bottomLift,diameter);
            SkillPage=Centered(Width-24,Height-158,44);
            if(ipad)
            {
                Attack=ScalePadCombat(Attack);Dodge=ScalePadCombat(Dodge);Jump=ScalePadCombat(Jump);Cancel=Jump;
                // Give the left skill arc more breathing room; keep a 30.9-unit gap to the right controls.
                Attack=new Area(Math.Min(Attack.X+12,Dodge.X-Attack.Width-30.9f),Attack.Y,Attack.Width,Attack.Height);
                Area page=ScalePadCombat(SkillPage);
                SkillPage=Centered(Width-45,page.Y+page.Height*.5f,68.64f);
                // Move the arc inward as one unit, preserving equal button spacing.
                for(int i=0;i<Skills.Length;i++)
                {
                    Area skill=ScalePadCombat(Skills[i]);skill.X+=10;Skills[i]=skill;
                }
                // Ultimate stays fixed above the page switch, freeing the bottom HUD.
                Skills[4]=Centered(SkillPage.X+SkillPage.Width*.5f,SkillPage.Y-10-31.2f,62.4f);
                Menu=Centered(Width-38,38,52.8f);Inventory=Centered(Width-93.2f,38,52.8f);
                SkillsMenu=Centered(Width-148.4f,38,52.8f);Catalog=Centered(Width-203.6f,38,52.8f);
                Smith=Centered(Width-258.8f,38,52.8f);Shop=Centered(Width-314,38,52.8f);
                DungeonEntrance=new Area(Width*.5f-58,76,116,44);
                Potion=Centered(Width*.5f-151,Height-34,62.4f);
            }
            int[] opportunityIdentities={0,1,2,4,5,6,7,9};
            for(int index=0;index<opportunityIdentities.Length;index++)
            {
                int skill=opportunityIdentities[index],button=index==7?4:index%4;
                Area key=Skills[button];SkillOpportunities[skill]=new Area(key.X,button==0?key.Y-14:button==3&&key.Y-15<140?key.Y+key.Height+1:key.Y-15,key.Width,14);
            }
            CounterOpportunity=new Area(Attack.X,Attack.Y+Attack.Height+1,Attack.Width/2,13);
            ComboOpportunity=new Area(Attack.X+Attack.Width/2,Attack.Y+Attack.Height+1,Attack.Width/2,13);
            float commandLift=Width<700?22:0;
            FocusCommand=new Area(204,Height-170-commandLift,48,48);RecallCommand=new Area(204,Height-120-commandLift,48,48);
            float phoneHealthWidth=Math.Min(160,Math.Max(110,Attack.X-8-(Width*.5f-80)-50));
            PlayerStatus=new Area(Width*.5f-(ipad?112:80),Height-(ipad?41:35),ipad?234:phoneHealthWidth+50,ipad?23:18);
            PlayerHealth=new Area(PlayerStatus.X,PlayerStatus.Y,ipad?184:phoneHealthWidth,ipad?16:12);
            PlayerEnergy=new Area(PlayerStatus.X,PlayerStatus.Y+(ipad?20:15),ipad?184:phoneHealthWidth,3);Map=new Area(12,12,ipad?158.4f:88,ipad?122.4f:68);
            AdventureStatus=new Area(12,ipad?140.4f:86,188,76);
            // Transient notices replace this left-side information slot, never the battlefield.
            Notice=AdventureStatus;
            EncounterText=new Area(Width-(ipad?238:172),ipad?76:60,164,18);BossHealth=new Area(Width-(ipad?238:172),ipad?97:81,164,5);
            CombatView=ChooseCombatView(Width*.5f-12,Height*.3f-32,96,64);
        }

        private Area ScalePadCombat(Area r)
        {return new Area(Width-24-(Width-r.X)*1.3f,Height-36-(Height-r.Y)*1.3f,r.Width*1.3f,r.Height*1.3f);}

        private readonly Area[] lootNoticeAreas=new Area[2];
        private bool lootNoticesMeasured;
        public Area LootNotice(int index)
        {
            if(index<0||index>=lootNoticeAreas.Length)return new Area();
            if(lootNoticesMeasured)return lootNoticeAreas[index];
            lootNoticesMeasured=true;
            var placed=new System.Collections.Generic.List<Area>();
            float w=Math.Min(174,Width*.35f);
            for(float y=Math.Max(Map.Y+Map.Height,Math.Max(EncounterText.Y+EncounterText.Height,BossHealth.Y+BossHealth.Height))+12;y+44<Height-70;y+=8)
            for(float x=Width-w-12;x>=Math.Max(Map.X+Map.Width+12,Width*.3f);x-=12)
            {
                if(placed.Count>0&&(x!=placed[0].X||y<placed[placed.Count-1].Y+placed[placed.Count-1].Height+8))continue;
                Area candidate=new Area(x,y,w,44);bool blocked=false;
                foreach(var r in new[]{Map,EncounterText,BossHealth,Menu,Inventory,SkillsMenu,Catalog,Shop,Smith,Interact,Attack,Dodge,Jump,SkillPage,PlayerStatus,Potion})if(candidate.Overlaps(r)){blocked=true;break;}
                if(!blocked)foreach(var r in Skills)if(candidate.Overlaps(r)){blocked=true;break;}
                if(!blocked)foreach(var r in placed)if(candidate.Overlaps(new Area(r.X-4,r.Y-4,r.Width+8,r.Height+8))){blocked=true;break;}
                if(blocked)continue;
                placed.Add(candidate);lootNoticeAreas[placed.Count-1]=candidate;if(placed.Count==lootNoticeAreas.Length)return lootNoticeAreas[index];
            }
            return lootNoticeAreas[index];
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
            if(area.X<0||area.Y<56||area.X+area.Width>Width||area.Y+area.Height>Height)return false;
            foreach(var control in new[]{MoveZone,Attack,Dodge,Potion,Jump,Menu,Inventory,SkillsMenu,Catalog,Interact,FocusCommand,RecallCommand,Notice,BossHealth,EncounterText,AdventureStatus,PlayerStatus,Map,SkillPage,DungeonEntrance,Shop,Smith})
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
