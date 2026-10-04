using System;using System.IO;using System.Linq;using Emberfall;
namespace Emberfall{public partial class GameSession{public bool IsCollecting(string id)=>pendingLoot.ContainsKey(id)&&pendingLoot[id].Collecting;}}
class CallbackProgram{
static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
static void Main(string[] args){
 foreach(bool sold in new[]{false,true}){
  var p=new ProgressionService(Path.Combine(args[0],Guid.NewGuid().ToString("N")));Check(p.CreateNewSlot(HeroClass.Ranger),"create real save");p.Profile.autoSellCommon=sold;p.Save();
  var game=new GameSession{Progression=p};var item=new ItemData{id="callback-loot",name="callback loot",slot=ItemSlot.Armor,rarity=sold?Rarity.Common:Rarity.Epic,level=2,health=10};var pickup=game.Drop(item);int gold=p.Profile.gold;
  Action observer=()=>{throw new InvalidOperationException("injected postcommit observer");};p.Changed+=observer;bool threw=false;try{game.TryCollectGroundLoot(item.id);}catch(InvalidOperationException e){threw=e.Message=="injected postcommit observer";}p.Changed-=observer;
  Check(threw,"postcommit observer exception remains visible");
  Check(!game.IsCollecting(item.id)&&game.PendingCount==0&&pickup.Retired,"committed callback failure retires pickup and unlocks pending receipt");
  Check(sold?p.Profile.gold>gold:p.Profile.inventory.Count(x=>x.id==item.id)==1,"committed item or sale remains credited once");
  string disk=File.ReadAllText(p.SaveFilePath);int credited=p.Profile.gold;Check(!game.TryCollectGroundLoot(item.id)&&p.Profile.gold==credited&&File.ReadAllText(p.SaveFilePath)==disk,"retry cannot award committed pickup again");
  Check(p.LoadSlot(p.CurrentSlotId)&&(sold?p.Profile.gold==credited:p.Profile.inventory.Count(x=>x.id==item.id)==1),"credit survives real reload");
 }
 {
  var p=new ProgressionService(Path.Combine(args[0],Guid.NewGuid().ToString("N")));Check(p.CreateNewSlot(HeroClass.Vanguard),"precommit slot");var game=new GameSession{Progression=p};var item=new ItemData{id="before-commit",name="retry",slot=ItemSlot.Armor,rarity=Rarity.Epic,level=2};var pickup=game.Drop(item);var inventory=p.Profile.inventory;string disk=File.ReadAllText(p.SaveFilePath);
  // Invalid dependency is restored after injection; no production hook or copied transaction.
  p.Profile.inventory=null;bool threw=false;try{game.TryCollectGroundLoot(item.id);}catch(NullReferenceException){threw=true;}finally{p.Profile.inventory=inventory;}
  Check(threw&&!game.IsCollecting(item.id)&&game.PendingCount==1&&!pickup.Retired&&File.ReadAllText(p.SaveFilePath)==disk,"precommit exception unlocks exact world item without retirement");
  Check(game.TryCollectGroundLoot(item.id)&&pickup.Retired&&game.PendingCount==0&&p.Profile.inventory.Count(x=>x.id==item.id)==1,"precommit exception can retry actual collection once");
 }
 Console.WriteLine("PASS "+checks+" actual ground-loot callback commit/retry assertions; managed JSON and scene boundaries, not Unity");
}}
