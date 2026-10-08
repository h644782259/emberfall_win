using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameUI
    {
        private Vector2 desktopDetailScroll;
        private int desktopDetailSkill=-1;
        private float DesktopParagraphHeight(string value,float availableWidth,int size)
        {return Mathf.Ceil(Style(size,false,true).CalcHeight(new GUIContent(value),availableWidth))+2;}
        private void DrawSkillDetail(Rect r, int skill)
        {
            GameProfile p = session.Progression.Profile;
            int rank = p.skillRanks[skill];
            int nextRank = Mathf.Min(3, rank + 1);
            bool passive = GameBalance.IsPassive(skill);
            SkillCategory category = GameBalance.GetSkillCategory(p.heroClass, skill);
            bool showOffenseScale = !passive && category != SkillCategory.Healing && category != SkillCategory.Defense;
            Color accent = UIIconAtlas.SkillColor(p.heroClass, skill);
            Fill(r, card);
            Fill(new Rect(r.x, r.y, r.width, 3), accent);
            DrawSkillIdentity(new Rect(r.x+18,r.y+15,48,48),p.heroClass,skill,rank,rank>0,48);
            Text(new Rect(r.x + 78, r.y + 15, 490, 35), GameBalance.SkillName(p.heroClass, skill), 27, pale, true);
            Text(new Rect(r.x + 78, r.y + 54, 490, 20), (passive ? "被动" : "主动") + " / " + GameBalance.CategoryName(category) + " / " + GameBalance.SkillRankName(rank), 12, accent, true);
            if (passive && new Rect(r.x + 18, r.y + 15, 550, 60).Contains(Mouse))
                tooltip = SkillTooltip(p, skill, rank);
            if (desktopDetailSkill != skill) { desktopDetailSkill=skill; desktopDetailScroll=Vector2.zero; }
            string description=BuildCatalog.VenomSkillOverride(p,skill,rank);
            if(description.Length==0)description=GameBalance.SkillDescription(p.heroClass,skill);
            string prerequisite=GameBalance.PrerequisiteDescription(p.heroClass,skill);
            float descriptionHeight=DesktopParagraphHeight(description,530,13);
            float prerequisiteHeight=DesktopParagraphHeight(prerequisite,530,12);
            string[] evolutions=new string[3];
            float evolutionHeight=0;
            bool scaleLine=showOffenseScale && !(skill==2&&p.heroClass!=HeroClass.Summoner)&&!(p.heroClass==HeroClass.Ranger&&skill==0&&BuildCatalog.ConcentratedVenomEquipped(p));
            for(int stage=1;stage<=3;stage++)
            {
                evolutions[stage-1]=skill==2&&p.heroClass!=HeroClass.Summoner?SkillBudgetHint(p.heroClass,skill,stage):GameBalance.SkillEvolution(p.heroClass,skill,stage);
                string venom=BuildCatalog.VenomSkillOverride(p,skill,stage);if(venom.Length>0)evolutions[stage-1]=venom;
                evolutionHeight=Mathf.Max(evolutionHeight,DesktopParagraphHeight(evolutions[stage-1],154,scaleLine?10:11));
            }
            float statsY=descriptionHeight+10,prerequisiteY=statsY+73;
            float evolutionY=prerequisiteY+prerequisiteHeight+10;
            float evolutionCardHeight=(scaleLine?50:32)+evolutionHeight+9;
            Rect viewport=new Rect(r.x+18,r.y+80,550,245);
            desktopDetailScroll=BeginTouchScroll("desktop-skill-detail",viewport,desktopDetailScroll,
                new Rect(0,0,532,Mathf.Max(viewport.height,evolutionY+evolutionCardHeight+6)));
            Text(new Rect(0,0,530,descriptionHeight),description,13,muted,false,true);
            string[] labels = { "当前冷却", "资源消耗", "初习解锁", "进阶成长" };
            string[] values = { passive ? "自动生效" : skill==SkillStockRules.Skill(p.heroClass)?"储存2次 · "+SkillStockRules.Seconds(p.heroClass)+"秒/次":GameBalance.EffectiveCooldown(p.heroClass, skill, rank).ToString("0.#") + " 秒", passive || GameBalance.SkillEnergyCost(p.heroClass, skill) == 0 ? "无需能量" : GameBalance.SkillEnergyCost(p.heroClass, skill).ToString("0") + " " + GameBalance.EnergyName(p.heroClass), "Lv." + GameBalance.SkillRequiredLevels[skill], "强化 → 觉醒" };
            for (int i = 0; i < 4; i++)
            {
                Rect stat = new Rect(i*134,statsY,128,43);
                Fill(stat, new Color(.035f, .075f, .11f));
                Text(new Rect(stat.x + 9, stat.y + 4, 110, 14), labels[i], 10, muted);
                Text(new Rect(stat.x + 9, stat.y + 23, 110, 18), values[i], 12, i == 1 ? jade : pale, true);
            }
            Text(new Rect(0,statsY+53,530,18),"学习前置",11,jade,true);
            Text(new Rect(0,prerequisiteY,530,prerequisiteHeight),prerequisite,12,session.Progression.PrerequisitesMet(skill)?pale:gold,false,true);
            for (int stage = 1; stage <= 3; stage++)
            {
                Rect evolution = new Rect((stage-1)*179,evolutionY,174,evolutionCardHeight);
                bool current = stage == rank;
                Fill(evolution, current ? new Color(.13f, .2f, .2f) : new Color(.045f, .08f, .115f));
                Border(evolution, new Color(accent.r, accent.g, accent.b, current ? .75f : .18f));
                string stageName = GameBalance.SkillRankName(stage) + " / Lv." + GameBalance.SkillRankRequiredLevel(skill, stage);
                Text(new Rect(evolution.x+9,evolution.y+7,154,18),stageName+(current?" ✓":""),12,stage<=rank?gold:pale,true);
                if(scaleLine)
                    Text(new Rect(evolution.x+9,evolution.y+31,154,16),"阶级系数 "+(100+(stage-1)*30)+"% · 范围 "+Mathf.RoundToInt(GameBalance.SkillRangeMultiplier(stage)*100)+"%",10,jade);
                Text(new Rect(evolution.x+9,evolution.y+(scaleLine?50:32),154,evolutionHeight),evolutions[stage-1],scaleLine?10:11,scaleLine?muted:jade,false,true);
            }
            EndTouchScroll();
            float actionX = r.x + 18;
            string reason = session.Progression.SkillLockReason(skill);
            bool canLearn = string.IsNullOrEmpty(reason);
            string caption = rank == 3 ? "已完全觉醒" : rank == 0 ? "学习初习 · 1 技能点" : "进阶" + GameBalance.SkillRankName(nextRank) + " · 1 技能点";
            if (Button(new Rect(actionX, r.y + 341, 236, 39), caption, gold, canLearn, reason, canLearn))
                Feedback(session.Progression.LearnSkill(skill), GameBalance.SkillName(p.heroClass, skill) + "已达到" + GameBalance.SkillRankName(session.Progression.Profile.skillRanks[skill]));
            Text(new Rect(r.x + 271, r.y + 341, 296, 40), rank == 3 ? "" : canLearn ? "下一阶段：" + GameBalance.SkillRankName(nextRank) + " · Lv." + GameBalance.SkillRankRequiredLevel(skill, nextRank) : reason, 12, muted, false, true);
            if (passive) return;
            if(MobileControls.Active){Text(new Rect(actionX,r.y+398,550,48),rank>0?"已在战斗界面直接显示 · 轻点自动瞄准施放":"学会后直接出现在战斗界面，无需配置或翻页",18,jade,true,true);return;}
            Rect loadoutHeading = new Rect(actionX, r.y + 397, 365, 21);
            Text(loadoutHeading, "十格快捷栏", 12, jade, true);
            if (loadoutHeading.Contains(Mouse)) tooltip = "“+” 配置到槽位，“×” 卸下。\n升级保留快捷栏位置，冷却按技能保留。";
            for (int slot = 0; slot < GameBalance.HotbarSize; slot++)
            {
                int equipped = LearnedSkillAtSlot(p, slot);
                bool current = equipped == skill;
                Rect target = new Rect(actionX + (slot % 5) * 112, r.y + 424 + (1 - slot / 5) * 40, 102, 36);
                string key = GameBalance.KeyName(p.hotbarKeys[slot]);
                detailSlots[slot] = target;
                string hint = key + " · " + SlotSkillName(p, slot) + "\n" + (rank == 0 ? "先学习这项技能。" : current ? "点击从此槽卸下；不会清除技能冷却。" : "将" + GameBalance.SkillName(p.heroClass, skill) + "配置到此槽。") + "\n拖动已配置技能可移动或交换，拖到栏外取消。";
                Fill(target, current ? new Color(.13f, .2f, .2f) : new Color(.035f, .075f, .11f));
                Border(target, current || hotbarDragging && hotbarPointerConfiguring && (slot == hotbarPointerSlot || target.Contains(Mouse)) ? gold : new Color(jade.r, jade.g, jade.b, .4f));
                if (target.Contains(Mouse)) tooltip = hint;
                Text(new Rect(target.x + 6, target.y + 1, 70, 13), key, 10, pale, true);
                Text(new Rect(target.xMax - 17, target.y + 1, 12, 13), current ? "×" : "+", 11, current ? gold : jade, true);
                DrawSlotIdentity(new Rect(target.x + 6, target.y + 14, target.width - 12, 21), p, slot, equipped == -1 ? muted : pale);
            }
            HandleHotbarPointer(detailSlots, true);
        }

    }
}
