namespace Emberfall
{
 public static class HubTravelRules
 {
  public const int Count=3;
  public static int UnlockedMask(int stored,int level,int clears)
  {int result=(stored&7)|1;if(level>=5)result|=2;if(level>=12||clears>0)result|=4;return result;}
  public static bool IsUnlocked(int mask,int hub){return hub>=0&&hub<Count&&(mask&(1<<hub))!=0;}
  public static int SafeCurrent(int requested,int mask){return IsUnlocked(mask,requested)?requested:0;}
  public static bool CanTravel(bool started,bool dungeon,bool dead,bool inCombat,bool changing)
  {return started&&!dungeon&&!dead&&!inCombat&&!changing;}
  public static string Name(int hub){return hub==1?"赤岩驿站":hub==2?"星望城":"风语营地";}
  public static string UnlockHint(int hub){return hub==1?"角色5级解锁":hub==2?"角色12级或首次通关解锁":"默认开放";}
 }
 public enum HubNpcKind{None,Merchant,Blacksmith,Exchange}
}
