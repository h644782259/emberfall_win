using UnityEngine;
namespace Emberfall {
 public sealed partial class GameSession {
  private void SpawnGroundSupplies(Vector3 position,int gold,int potions) {
   var root=new GameObject("Ground supplies");root.transform.SetParent(world.transform,false);root.transform.position=WorldTraversal.NearestWalkable(position+Vector3.right*.7f,.25f);
   root.AddComponent<GroundSupplyPickup>().Initialize(this,gold,potions);
  }
 }
}
