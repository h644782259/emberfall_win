using System;
using System.Collections.Generic;
namespace Emberfall
{
    // Suggestions describe existing skills/equipment; they never unlock a second progression system.
    public enum CampRouteStage { BasicMissing, BasicReady, EnhancedReady }
    public enum CampRouteAction { None, Skill, Specialization, SummonerRoute, Inventory, TrackCore }
    public sealed class CampRouteCard
    {
        public readonly string Name, Loop, Requirements, Enhancement, NextStep;
        public readonly CampRouteStage Stage;
        public readonly bool Ready;
        public readonly CampRouteAction NextAction;
        public readonly int NextSkill;
        public readonly EquipmentMechanic Mechanic;
        public CampRouteCard(string name,string loop,string requirements,bool ready,CampRouteStage stage=CampRouteStage.BasicMissing,string enhancement="",string nextStep="",CampRouteAction action=CampRouteAction.None,int nextSkill=-1,EquipmentMechanic mechanic=EquipmentMechanic.None)
        {Name=name;Loop=loop;Requirements=requirements;Ready=ready;Stage=stage;Enhancement=enhancement;NextStep=nextStep;NextAction=action;NextSkill=nextSkill;Mechanic=mechanic;}
    }
    public static class CampRouteCards
    {
        public static CampRouteCard Describe(GameProfile profile,bool mobile,int index)
        {
            if(profile==null||index<0||index>1)return new CampRouteCard("","","",false);
            string name,loop;int[] skills;EquipmentMechanic mechanic=EquipmentMechanic.None;
            switch(profile.heroClass)
            {
                case HeroClass.Vanguard:
                    name=index==0?"闪避反击":"破阵控场";
                    loop=index==0?"完美闪避 → 普攻反击 → 普攻回能":"裂地控敌 → 风暴清场 → 普攻回能";
                    skills=index==0?new int[0]:new[]{1,2};if(index==0)mechanic=EquipmentMechanic.ReturningBlade;break;
                case HeroClass.Arcanist:
                    name=index==0?"碎冰连锁":"灼燃留场";
                    loop=index==0?"新星冻敌 → 陨星碎冰":"陨星点燃 → 护盾续燃 → 普攻回能";
                    skills=index==0?new[]{0,1}:new[]{1,5};mechanic=index==0?EquipmentMechanic.FrostEcho:EquipmentMechanic.CinderTrail;break;
                case HeroClass.Ranger:
                    name=index==0?"叠毒引爆":"猎印追击";
                    loop=index==0?"普攻叠毒 → 扇形箭引爆":"狩猎标记 → 集火击杀 → 拉开距离";
                    skills=index==0?new[]{0}:new[]{7,4};if(index==0)mechanic=EquipmentMechanic.VenomSpread;break;
                default:
                    name=index==0?"强契协同":"群契围攻";
                    loop=index==0?"炮台远射 → 灵狼追击 → 灵能冲击":"荆棘控场 → 双狼追击 → 树灵重击";
                    skills=new[]{2,4};if(index==0)mechanic=EquipmentMechanic.TwinSummonResonance;break;
            }
            var missing=new List<string>();int nextSkill=-1;int[] usable=RunChoices.UsableRanks(profile,mobile);
            foreach(int skill in skills)if(usable[skill]<=0)
            {if(nextSkill<0)nextSkill=skill;missing.Add((profile.skillRanks!=null&&profile.skillRanks.Length>skill&&profile.skillRanks[skill]>0?"装入":"学习")+GameBalance.SkillName(profile.heroClass,skill));}
            bool switchElement=profile.heroClass==HeroClass.Arcanist&&(index==0?profile.specialization==ElementalistSpecialization.Burn:profile.specialization!=ElementalistSpecialization.Burn);
            bool switchSummon=profile.heroClass==HeroClass.Summoner&&(int)profile.summonerRoute!=index;
            bool replaceCore=profile.heroClass==HeroClass.Summoner&&index==1&&Equipped(profile,EquipmentMechanic.TwinSummonResonance);
            if(switchElement)missing.Add("切换"+(index==0?"碎冰或均衡":"灼燃"));
            if(switchSummon)missing.Add("切换"+(index==0?"双契":"群契"));
            if(replaceCore)missing.Add("在行囊换下双契共鸣以展开兽群");
            bool enhanced=mechanic==EquipmentMechanic.None||Equipped(profile,mechanic);
            var stage=missing.Count>0?CampRouteStage.BasicMissing:enhanced?CampRouteStage.EnhancedReady:CampRouteStage.BasicReady;
            string enhancement=mechanic==EquipmentMechanic.None?"无需额外机制装备":(enhanced?"强化已就绪：":"可选强化：穿戴")+BuildCatalog.MechanicName(mechanic);
            string requirements=missing.Count>0?"基础不足："+string.Join(" / ",missing):enhanced?"机制强化齐备":"基础循环可用 · 机制装备非必需";
            CampRouteAction action=nextSkill>=0?CampRouteAction.Skill:switchElement?CampRouteAction.Specialization:switchSummon?CampRouteAction.SummonerRoute:replaceCore?CampRouteAction.Inventory:!enhanced?CampRouteAction.TrackCore:CampRouteAction.None;
            return new CampRouteCard(name,loop,requirements,missing.Count==0,stage,enhancement,missing.Count>0?missing[0]:enhanced?"按上方循环练习":"可追踪"+BuildCatalog.MechanicName(mechanic),action,nextSkill,mechanic);
        }
        private static bool Equipped(GameProfile profile,EquipmentMechanic mechanic)
        {
            if(profile.inventory==null)return false;
            string id=BuildCatalog.MechanicSlot(mechanic)==ItemSlot.Weapon?profile.weaponId:profile.relicId;
            foreach(var item in profile.inventory)if(item!=null&&item.id==id&&item.mechanic==mechanic&&item.slot==BuildCatalog.MechanicSlot(mechanic)&&item.level<=profile.level)return true;
            return false;
        }
    }
}
