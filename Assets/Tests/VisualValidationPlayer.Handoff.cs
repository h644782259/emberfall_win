#if EMBERFALL_VISUAL_VALIDATION
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Emberfall
{
    public sealed partial class VisualValidationPlayer
    {
        private void SessionInvoke(string name,params object[] args)
        {typeof(GameSession).GetMethod(name,PrivateInstance).Invoke(session,args);}
        private IEnumerator HandoffCapture(string name)
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--logic-only")>=0)yield break;
            yield return Capture(name);
        }

        private IEnumerator VerifyWindowsHandoff()
        {
            yield return SetResolution(1280,720);
            SetField("selectedClass",HeroClass.Arcanist);Invoke("StartSelectedHero");
            session.Player.enabled=false;session.Progression.GrantExperience(40000);session.ReturnToCamp();
            var p=session.Progression;
            Check(System.Text.RegularExpressions.Regex.IsMatch(File.ReadAllText(p.SaveFilePath),"\"version\"\\s*:\\s*4"),"Independent attachment saves protect investments from older readers");
            p.Profile.mechanicMaterials=200;p.Profile.highestAdventureTier=15;p.Profile.classTutorialCompleted=true;
            Check(p.ExchangeMechanic(EquipmentMechanic.FrostEcho),"Acquire attachment");
            int currency=p.Profile.mechanicMaterials;
            Check(!p.ExchangeMechanic(EquipmentMechanic.FrostEcho)&&p.Profile.mechanicMaterials==currency,"Duplicate rejection preserves currency");
            p.Save();
            var runtime=(SkillRuntime)typeof(PlayerController).GetField("skillRuntime",PrivateInstance).GetValue(session.Player);
            runtime.TryConsume(0,1);float energy=runtime.Energy,cooldown=runtime.Remaining(0);
            typeof(PlayerController).GetProperty("Health").SetValue(session.Player,.5f);
            Check(p.UpgradeAttachment(EquipmentMechanic.FrostEcho,true),"Upgrade attachment");
            Check(runtime.Energy==energy&&runtime.Remaining(0)==cooldown&&session.Player.Health==.5f,"Upgrade preserves fractional HP, energy and cooldown");
            var original=p.Profile;currency=original.mechanicMaterials;
            using(var locked=new FileStream(p.SaveFilePath,FileMode.Open,FileAccess.Read,FileShare.None))
                Check(!p.UpgradeAttachment(EquipmentMechanic.FrostEcho,true)&&ReferenceEquals(original,p.Profile)&&currency==p.Profile.mechanicMaterials,"Failed write preserves profile and materials");
            Check(p.ToggleAttachmentVariant(EquipmentMechanic.FrostEcho,true),"Unlock variant");
            Check(p.SaveBuildPreset(0,true),"Save mounted plan A");
            Check(p.SetAttachmentMounted(EquipmentMechanic.FrostEcho,false,true)&&p.SaveBuildPreset(1,true),"Save unmounted plan B");
            Check(p.ApplyBuildPreset(0,true)&&p.HasMechanic(EquipmentMechanic.FrostEcho)&&p.AttachmentVariant(EquipmentMechanic.FrostEcho)==1,"Apply mounted plan and variant");
            Check(p.ApplyBuildPreset(1,true)&&!p.HasMechanic(EquipmentMechanic.FrostEcho),"Apply unmounted plan");
            p.SetAttachmentMounted(EquipmentMechanic.FrostEcho,true,true);
            typeof(PlayerController).GetProperty("Health").SetValue(session.Player,.5f);
            original=p.Profile;currency=original.mechanicMaterials;
            using(var locked=new FileStream(p.SaveFilePath,FileMode.Open,FileAccess.Read,FileShare.None))
                Check(!p.AdvanceAutomaticGrowth()&&ReferenceEquals(original,p.Profile)&&currency==p.Profile.mechanicMaterials,"Failed growth award retains receipts and currency");
            Check(p.AdvanceAutomaticGrowth(),"Commit automatic rewards");
            Check(session.Player.Health==.5f&&runtime.Energy==energy&&runtime.Remaining(0)==cooldown,"Automatic reward cannot refresh upgrade resources");
            int gold=p.Profile.gold,materials=p.Profile.mechanicMaterials;
            Check(p.AdvanceAutomaticGrowth()&&p.Profile.gold==gold&&p.Profile.mechanicMaterials==materials,"Automatic rewards are idempotent");
            var reload=new ProgressionService(p.SaveDirectory);
            Check(reload.LoadSlot(p.CurrentSlotId)&&reload.AdvanceAutomaticGrowth()&&reload.Profile.gold==gold&&reload.Profile.mechanicMaterials==materials,"Growth receipt survives restart");
            Check(reload.SelectProgressionGoal(ProgressionGoalKind.Tier,null,20),"Select manual class goal");
            var change=reload.PrepareClassSwitch(HeroClass.Ranger,true);
            Check(change!=null&&reload.CommitClassSwitch(change,true)&&reload.Profile.automaticGrowth,"New class starts automatic growth independently");
            reload.SelectProgressionGoal(ProgressionGoalKind.Tier,null,40);
            change=reload.PrepareClassSwitch(HeroClass.Arcanist,true);
            Check(change!=null&&reload.CommitClassSwitch(change,true)&&!reload.Profile.automaticGrowth&&reload.Profile.progressionGoalTier==20,"Returning class keeps manual preference and goal");
            reload.ResumeAutomaticGrowth();change=reload.PrepareClassSwitch(HeroClass.Ranger,true);
            Check(change!=null&&reload.CommitClassSwitch(change,true)&&!reload.Profile.automaticGrowth&&reload.Profile.progressionGoalTier==40,"Other class manual preference survives automatic resume");
            var legacy=JsonUtility.FromJson<GameProfile>(JsonUtility.ToJson(p.Profile));
            legacy.attachmentRevision=0;legacy.attachments.Clear();
            var item=legacy.inventory.Find(x=>x.id==legacy.relicId);
            item.mechanic=EquipmentMechanic.FrostEcho;item.mechanicVariantUnlocked=true;item.mechanicVariant=1;
            legacy.pendingLoot.Add(JsonUtility.FromJson<ItemData>(JsonUtility.ToJson(item)));int attack=item.attack;
            var normalize=typeof(ProgressionService).GetMethod("ValidateProfile",BindingFlags.NonPublic|BindingFlags.Static);
            normalize.Invoke(null,new object[]{legacy});
            Check(legacy.attachments.Count==1&&legacy.attachments[0].mounted&&legacy.attachments[0].variant==1&&item.attack==attack,"Legacy duplicate migration preserves equipped variant and stats");
            string once=JsonUtility.ToJson(legacy);normalize.Invoke(null,new object[]{legacy});
            Check(once==JsonUtility.ToJson(legacy),"Migration is idempotent");
            legacy.attachments.Add(new MechanicAttachment{mechanic=EquipmentMechanic.FrostEcho,upgradeRank=5,rarity=Rarity.Legendary,level=100,variantUnlocked=true});
            normalize.Invoke(null,new object[]{legacy});
            Check(legacy.attachments.Count==1&&legacy.attachments[0].upgradeRank==5&&legacy.attachments[0].rarity==Rarity.Legendary,"Stored duplicate attachments preserve maximum investment");
            while(p.Profile.inventory.Count<ProgressionService.InventoryCapacity)p.Profile.inventory.Add(p.RollLoot(p.Profile.level,false));
            p.Profile.pendingLoot.Add(p.RollLoot(p.Profile.level,false));
            var attention=ProgressionAttention.Evaluate(p,true);
            Check(attention.LootPending&&attention.Rewards&&!attention.LootClaimable,"Full bag keeps pending badge");
            p.Save();
            Check(!p.TravelToHub(2)&&p.TravelToHub(1)&&p.TravelToHub(2)&&p.TravelToHub(1)&&p.TravelToHub(0),"Sequential travel preserves unlocks");
            float savedScale=EffectPreferences.CombatTextScale;
            try
            {
                EffectPreferences.CombatTextScale=1f;
                var style=typeof(GameUI).GetMethod("Style",PrivateInstance);
                int small=((GUIStyle)style.Invoke(ui,new object[]{15,false,false,TextAnchor.UpperLeft})).fontSize;
                expectedPause=true;session.SetPaused(true);yield return HandoffCapture("font-100-pause");
                EffectPreferences.CombatTextScale=1.8f;
                int large=((GUIStyle)style.Invoke(ui,new object[]{15,false,false,TextAnchor.UpperLeft})).fontSize;
                Check(large>small*1.6f,"UI font setting changes size");yield return HandoffCapture("font-180-pause");ResetPanels();
                OpenPanel("Camp");SetField("campTab",1);yield return HandoffCapture("font-180-attachments");ResetPanels();
                OpenPanel("Fashion");Invoke("TrialFashion",FashionSlot.Wings,Rarity.Legendary);yield return HandoffCapture("font-180-collection");ResetPanels();
                EffectPreferences.CombatTextScale=1.25f;
                var colliders=UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None);
                int authoredPlatforms=0;
                foreach(var collider in colliders)
                    if(collider.name.StartsWith("Climbable"))
                    {
                        authoredPlatforms++;
                        Check(Mathf.Abs(collider.bounds.max.y-WorldTraversal.SurfaceHeight(collider.transform.position))<.02f,"Authored Unity collider matches traversal top");
                    }
                Check(authoredPlatforms>0,"Actual low platforms have Unity colliders");
                WorldTraversal.Reset(ZoneKind.Wilderness);
                Check(WorldTraversal.AddPlatform(new Vector3(4,0,0),new Vector2(2.4f,2.4f),.95f)!=null,"Register platform");
                Vector3 landing;
                Check(WorldTraversal.TryResolvePlatformJump(Vector3.zero,Vector3.right,5.5f,.45f,out landing)&&landing.y==.95f,"Jump onto platform");
                Check(WorldTraversal.CanStand(landing)&&WorldTraversal.Move(landing,Vector3.right*5,.45f).y==.95f,"Stand and move on top");
                Check(WorldTraversal.TryResolvePlatformJump(landing,Vector3.left,5.5f,.45f,out landing)&&landing.y==0,"Jump down");
                Check(!WorldTraversal.HeightAttackAllowed(Vector3.zero,new Vector3(0,.95f,0),false)&&WorldTraversal.HeightAttackAllowed(Vector3.zero,new Vector3(0,.95f,0),true),"Height attack rules");
                SessionInvoke("ChangeZone",false);
                foreach(RoomBranch branch in new[]{RoomBranch.Seal,RoomBranch.Supply})
                {
                    session.Player.Teleport(new Vector3(0,0,11));session.EnterDungeon();session.SelectedArenaMode=3;
                    session.SelectedDungeonTier=15;session.SelectedChallengeMode=true;session.ConfirmDungeonSelection();
                    Check(session.RoomChainRun!=null&&!session.RoomChainRun.Failed&&session.DungeonTier==15&&session.ChallengeRun,"Native tier15 limited-healing entry");
                    for(int room=0;room<5;room++)
                    {
                        Check(session.RoomChainRun.Room.Index==room&&!session.RoomChainRun.Failed,"Native room "+branch+" "+room);
                        foreach(var enemy in session.Enemies.ToArray())
                        {
                            Check(WorldTraversal.CanReach(TacticalRoomGeometry.Entrance,enemy.transform.position,enemy.IsBoss?1.3f:.65f),"Reachable native enemy spawn");
                            enemy.enabled=false;enemy.TakeDamage(10000000,Vector3.zero);
                        }
                        yield return null;
                        if(room==2&&branch==RoomBranch.Seal)
                        {
                            for(int seal=0;seal<2;seal++)for(int tick=0;tick<12;tick++)session.RoomChainRun.AdvanceSeal(seal,.25f,true,true,false);
                            SessionInvoke("OpenRoomGate");yield return HandoffCapture("tier15-seal-room3");
                        }
                        if(session.RunChoices.AwaitingChoice)Check(session.ConfirmBlessing(0),"Blessing confirmation");
                        if(room==4)break;
                        session.Player.Teleport(new Vector3(0,0,14));session.EnterNextRoom();
                        if(room==1)Check(session.ConfirmRoomBranch(branch),"Native side-branch selection");
                    }
                    Check(session.RoomChainRun.Finished&&!session.RoomChainRun.Failed&&!session.ModeRewardPending,"Native branch completes durably");
                    yield return HandoffCapture("expedition-success-"+branch);
                    if(branch==RoomBranch.Supply)
                        Check(session.ChallengeNextTier()&&session.DungeonTier==16&&!session.RoomChainRun.Failed,"Next tier re-enters same expedition after durable rewards");
                    SessionInvoke("ChangeZone",false);yield return null;
                }
                Check(GameBalance.ArcanistFinaleRadius==9.5f&&GameBalance.ArcanistPulseRadius==8f,"Shared ultimate radii");
            }
            finally {EffectPreferences.CombatTextScale=savedScale;}
        }
    }
}
#endif
