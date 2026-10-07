using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
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
