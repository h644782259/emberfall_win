using UnityEngine;
namespace Emberfall
{
 public sealed partial class GameSession
 {
  public int CurrentHub {get{return Progression==null?0:Progression.Profile.currentHub;}}
  public bool CanOpenTravelMap {get{return CanTravelNow();}}
  private HubNpcKind nearbyHubNpc;
  private HubNpcKind activeHubNpc;
  public HubNpcKind ActiveHubNpc
  {get{return HasStarted&&!InDungeon&&!IsDead&&!PracticeActive&&uiBlocking&&Player!=null&&NearbyHubNpc==activeHubNpc?activeHubNpc:HubNpcKind.None;}}
  internal void BeginHubNpcConversation(HubNpcKind kind)
  {
   if(kind==HubNpcKind.None||!uiBlocking||Paused||NearbyHubNpc!=kind)return;
   activeHubNpc=kind;
   GameAudio.PlayNpcGreeting(kind);
  }
  internal void EndHubNpcConversation()
  {activeHubNpc=HubNpcKind.None;GameAudio.StopNpcGreeting();}
  public HubNpcKind NearbyHubNpc
  {
   get
   {
    if(!HasStarted||InDungeon||IsDead||PracticeActive||Player==null){nearbyHubNpc=HubNpcKind.None;return nearbyHubNpc;}
    // Ground proximity is unaffected by jumping. Keep the current prompt through
    // a small edge buffer so tiny movement cannot toggle its IMGUI control away.
    if(nearbyHubNpc!=HubNpcKind.None&&nearbyHubNpc!=HubNpcKind.Exchange&&CombatFx.Flat(Player.transform.position-HubNpcPosition((int)nearbyHubNpc-1)).sqrMagnitude<3.15f*3.15f)return nearbyHubNpc;
    HubNpcKind kind=HubNpcKind.None;float nearest=2.65f;
    for(int index=0;index<2;index++){float distance=CombatFx.Flat(Player.transform.position-HubNpcPosition(index)).magnitude;if(distance<nearest){nearest=distance;kind=(HubNpcKind)(index+1);}}
    nearbyHubNpc=kind;
    return kind;
   }
  }
  public static Vector3 HubNpcPosition(int index){return HubSettlementPlan.Npc(index);}
  private bool CanTravelNow()
  {
   bool nearby=false;if(Player!=null)foreach(var enemy in Enemies)if(enemy!=null&&!enemy.IsDead&&Vector3.Distance(Player.transform.position,enemy.transform.position)<7){nearby=true;break;}
   return HubTravelRules.CanTravel(HasStarted,InDungeon,IsDead,nearby,changingZone);
  }
  public bool TravelToHub(int hub)
  {
   if(!CanTravelNow()){Notify("先离开挑战并远离敌人，再旅行。");return false;}
   if(hub==CurrentHub)return true;
   if(!PreserveWorldLoot()||!Progression.TravelToHub(hub)){Notify(Progression.LastError);return false;}
   loadingSaveSnapshot=true;
   try {if(!ChangeZone(false))return false;}
   finally {loadingSaveSnapshot=false;}
   Player.Teleport(WorldTraversal.NearestWalkable(new Vector3(0,0,-14),.45f));
   Notify("已抵达"+HubTravelRules.Name(CurrentHub));return true;
  }
 }
}
