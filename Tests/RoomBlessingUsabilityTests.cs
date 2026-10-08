using System;
using Emberfall;
public static class RoomBlessingUsabilityTests
{
 static int checks;
 static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
 // Independent of RunChoices.IsCompatible/UsableRanks: actually exercise reachable casts.
 static bool Reachable(GameProfile profile,bool mobile,int skill)
 {return profile.skillRanks[skill]>0&&!GameBalance.IsPassive(skill)&&(mobile||Array.IndexOf(profile.equippedSkills,skill)>=0);}
 static bool Useful(RunBlessing card,GameProfile profile,bool mobile)
 {
  if(card==RunBlessing.FlowingEssence||card==RunBlessing.QuickRecovery||card==RunBlessing.ChargedWard||card==RunBlessing.InterruptFlow||card==RunBlessing.MarkedPursuit)
  {
   for(int skill=0;skill<GameBalance.SkillCount;skill++)if(Reachable(profile,mobile,skill))
   {
    var normal=new SkillRuntime(profile.heroClass);var faster=new SkillRuntime(profile.heroClass);
    if(!normal.TryConsume(skill,profile.skillRanks[skill]))continue;
    faster.TryConsume(skill,profile.skillRanks[skill],.85f);
    if(card==RunBlessing.FlowingEssence&&normal.Energy<SkillRuntime.MaximumEnergy)return true;
    if(card==RunBlessing.QuickRecovery&&(skill==SkillRuntime.StockSkill(profile.heroClass)?faster.RechargeRemaining(skill)<normal.RechargeRemaining(skill):faster.Remaining(skill)<normal.Remaining(skill)))return true;
    if(card==RunBlessing.ChargedWard&&SkillDamageBudgets.ChargeSeconds(profile.heroClass,skill)>0)return true;
    if(card==RunBlessing.InterruptFlow&&EnemyControlPolicy.IsInterruptSkill(profile.heroClass,skill))return true;
    if(card==RunBlessing.MarkedPursuit&&profile.heroClass==HeroClass.Ranger&&skill==7)return true;
   }
   return false;
  }
  // Remaining cards change ordinary attacks, incoming damage, dodge reactions or kill/clear rewards.
  return true;
 }
 static void OfferUseful(RunBlessing[] offer,GameProfile profile,bool mobile,string where)
 {
  int useful=0;foreach(var card in offer)if(Useful(card,profile,mobile))useful++;
  Check(useful>=2,"independent cast effects require two useful offers: "+where+" / "+string.Join(",",offer));
  foreach(var card in offer)Check(Useful(card,profile,mobile),"unusable resource or mechanic card should be filtered: "+where+" / "+card);
 }
 public static string Run()
 {
  checks=0;
  var reproductions=new[]{new[]{(int)HeroClass.Vanguard,17},new[]{(int)HeroClass.Arcanist,1},new[]{(int)HeroClass.Summoner,16}};
  foreach(var reproduction in reproductions)
  {
   var p=new GameProfile{heroClass=(HeroClass)reproduction[0],level=2,skillPoints=1};
   var run=new RunChoices();run.PrepareRoomChoice(1,p,false,reproduction[1]);
   OfferUseful(run.Offer,p,false,"frozen review reproduction "+p.heroClass+" seed="+reproduction[1]);
  }

  foreach(HeroClass hero in Enum.GetValues(typeof(HeroClass)))foreach(bool mobile in new[]{false,true})
  for(int config=0;config<3;config++)for(int seed=0;seed<512;seed++)
  {
   // Legal level-2 profiles: point unspent, first skill learned but unequipped, or equipped.
   var p=new GameProfile{heroClass=hero,level=2,skillPoints=config==0?1:0,equippedSkills=new[]{-1,-1,-1,-1,-1,-1,-1,-1,-1,-1}};
   if(config>0)p.skillRanks[0]=1;if(config==2)p.equippedSkills[0]=0;
   string witness=hero+" mobile="+mobile+" config="+config+" seed="+seed;
   var opening=new RunChoices();opening.PrepareRoomChoice(1,p,mobile,seed);OfferUseful(opening.Offer,p,mobile,witness);
   for(int choice=0;choice<3;choice++)
   {
    var room=new RunChoices();room.PrepareRoomChoice(1,p,mobile,seed);room.Choose(choice);room.PrepareRoomChoice(2,p,mobile,seed);
    OfferUseful(room.Offer,p,mobile,witness+" rest after "+choice);
   }
   var ordinary=new RunChoices();
   for(int stage=1;stage<=2;stage++)
   {
    int[] bound=new int[GameBalance.SkillCount];for(int skill=0;skill<bound.Length;skill++)if(Reachable(p,mobile,skill))bound[skill]=p.skillRanks[skill];
    ordinary.Prepare(stage,hero,bound,seed);OfferUseful(ordinary.Offer,p,mobile,witness+" ordinary "+stage);ordinary.Choose(seed%3);
   }
  }
  return "PASS: "+checks+" independently evaluated low-level blessing usefulness assertions (production cast effects, not playtesting)";
 }
}
