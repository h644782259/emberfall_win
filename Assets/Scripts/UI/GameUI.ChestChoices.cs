using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private void DrawSingleChestCard(Rect r,float unit)
        {
            Fill(r,new Color(.055f,.08f,.11f));Border(r,gold);
            Text(new Rect(r.x+12*unit,r.y+8*unit,r.width-24*unit,24*unit),"通关宝箱",Mathf.RoundToInt(16*unit),pale,true);
        }
        private Rect InteractiveChestArt(Rect r,float unit)
        {return new Rect(r.x+10*unit,r.y+36*unit,r.width-20*unit,Mathf.Max(24*unit,r.height-48*unit));}
        private bool ChestOpenButton(Rect area,float unit,bool enabled)
        {
            float side=Mathf.Min(area.width,area.height);
            Rect hit=new Rect(area.center.x-side*.5f,area.center.y-side*.5f,side,side);
            bool previous=GUI.enabled;GUI.enabled=previous&&enabled;
            bool hover=GUI.enabled&&!MobileControls.Active&&hit.Contains(Event.current.mousePosition);
            Rect art=hit;
            if(hover){Fill(hit,new Color(gold.r,gold.g,gold.b,.12f));Border(hit,new Color(gold.r,gold.g,gold.b,.7f),2*unit);art=new Rect(hit.x-side*.025f,hit.y-side*.025f,side*1.05f,side*1.05f);}
            DrawRewardChest(art,false,GUI.enabled?1f:.35f,0);
            bool clicked=GUI.Button(hit,GUIContent.none,invisibleButton);
            GUI.enabled=previous;return clicked;
        }
        private Color ChestChoiceAccent(int choice)
        {return choice==0?new Color(.91f,.62f,.36f):choice==1?new Color(.48f,.81f,.94f):gold;}
        private Rect ChestChoiceArt(Rect r,float scale)
        {return new Rect(r.x+10*scale,r.y+30*scale,r.width-20*scale,Mathf.Max(24*scale,r.height-134*scale));}
        // Pure presentation: no RNG, grant, save, equipment, or combat calls.
        private void DrawChestChoiceCard(Rect r,int choice,GameProfile profile,float scale)
        {
            Color accent=ChestChoiceAccent(choice);
            Fill(r,new Color(.055f,.08f,.11f));Border(r,accent);
            Fill(new Rect(r.x,r.y,4*scale,r.height),accent);
            Text(new Rect(r.x+10*scale,r.y+7*scale,r.width-20*scale,20*scale),ProgressionService.ChestChoiceName(choice),Mathf.RoundToInt(14*scale),accent,true);
            Rect art=ChestChoiceArt(r,scale);
            DrawRewardChest(art,false,1,0);
            DrawChestChoiceEmblem(art,choice,accent);
            Text(new Rect(r.x+8*scale,r.yMax-100*scale,r.width-16*scale,42*scale),ChestRevealPresentation.ChoiceDetail(profile,choice),Mathf.RoundToInt(12*scale),pale,false,true);
        }
        private void DrawChestChoiceEmblem(Rect area,int choice,Color accent)
        {
            float unit=Mathf.Min(area.width,area.height)/100f;
            float x=area.center.x,y=area.center.y;
            if(choice==0)
            {
                Fill(new Rect(x-5*unit,y-39*unit,10*unit,55*unit),accent);
                Fill(new Rect(x-2*unit,y-35*unit,3*unit,49*unit),pale);
                Fill(new Rect(x-23*unit,y+12*unit,46*unit,7*unit),gold);
                Fill(new Rect(x-4*unit,y+19*unit,8*unit,20*unit),accent);
                Fill(new Rect(x-8*unit,y+36*unit,16*unit,6*unit),gold);
            }
            else if(choice==1)
            {
                for(int side=-1;side<=1;side+=2)
                    for(int feather=0;feather<4;feather++)
                    {
                        float offset=(9+feather*9)*unit;
                        Fill(new Rect(x+side*offset-4*unit,y-30*unit+feather*5*unit,8*unit,(55-feather*9)*unit),accent);
                        Fill(new Rect(x+side*offset-2*unit,y-28*unit+feather*5*unit,2*unit,17*unit),pale);
                    }
                Fill(new Rect(x-4*unit,y+9*unit,8*unit,22*unit),gold);
            }
            else
            {
                for(int stack=0;stack<3;stack++)
                    for(int coin=0;coin<=stack;coin++)
                    {
                        Rect bar=new Rect(x+(stack-1)*27*unit-12*unit,y+(24-coin*14)*unit,24*unit,11*unit);
                        Fill(bar,accent);Border(bar,pale);
                    }
            }
        }
        private void DrawChestGoldReward(Rect area,ChestReward reward,Color accent)
        {
            DrawChestGold(area,accent);
            Text(new Rect(area.x+8,area.y+8,area.width-16,42),ChestRevealPresentation.GoldHeadline(reward),20,gold,true,true);
        }
    }
}
