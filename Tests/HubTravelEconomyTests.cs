using System;
using System.IO;
using System.Text.Json.Nodes;
using Emberfall;
using UnityEngine;

public static class HubTravelEconomyTests
{
    private static int assertions;
    private static void Check(bool value, string message) { assertions++; if (!value) throw new Exception(message); }
    public static string Run(string directory)
    {
        assertions = 0;
        Policy();
        string root = Path.Combine(directory, "hub-economy-" + Guid.NewGuid().ToString("N"));
        MigrationAndTravel(Path.Combine(root, "travel"));
        Economy(Path.Combine(root, "shop"));
        return assertions + " hub travel and atomic economy assertions passed";
    }
    private static void Policy()
    {
        Check(HubTravelRules.Count == 3, "three supported towns");
        Check(HubTravelRules.Name(0) == "风语营地" && HubTravelRules.Name(1) == "赤岩驿站" && HubTravelRules.Name(2) == "星望城", "stable town names");
        for (int stored = -2; stored <= 15; stored++)
        for (int level = 1; level <= 15; level++)
        for (int clears = 0; clears <= 1; clears++)
        {
            int mask = HubTravelRules.UnlockedMask(stored, level, clears);
            Check((mask & 1) != 0 && (mask & ~7) == 0, "migration always keeps home and bounds bits");
            Check((mask & (stored & 7)) == (stored & 7), "earned unlocks never removed");
            Check(level < 5 || (mask & 2) != 0, "level five unlock");
            Check((level < 12 && clears == 0) || (mask & 4) != 0, "level twelve or first clear unlock");
            for (int hub = -1; hub <= 3; hub++)
            {
                int safe = HubTravelRules.SafeCurrent(hub, mask);
                Check(safe == (HubTravelRules.IsUnlocked(mask, hub) ? hub : 0), "invalid or locked current falls back to home");
            }
        }
        Check(HubTravelRules.UnlockedMask(0, 4, 0) == 1 && HubTravelRules.UnlockedMask(0, 5, 0) == 3 &&
            HubTravelRules.UnlockedMask(0, 11, 0) == 3 && HubTravelRules.UnlockedMask(0, 12, 0) == 7 &&
            HubTravelRules.UnlockedMask(0, 1, 1) == 5, "exact unlock boundaries");
        for (int flags = 0; flags < 32; flags++)
            Check(HubTravelRules.CanTravel((flags&1)!=0,(flags&2)!=0,(flags&4)!=0,(flags&8)!=0,(flags&16)!=0) == (flags==1),
                "travel requires active, alive, noncombat, nonchallenge, stable scene");
        Check(!HubTravelRules.IsUnlocked(7, int.MinValue) && !HubTravelRules.IsUnlocked(7, int.MaxValue), "invalid IDs cannot overflow shift");
    }
    private static ProgressionService Fresh(string root)
    {
        var service = new ProgressionService(root);
        Check(service.CreateNewSlot(HeroClass.Ranger), "temporary fixture created");
        return service;
    }
    private static string State(ProgressionService service) { return JsonUtility.ToJson(service.Profile, true); }
    private static void MigrationAndTravel(string root)
    {
        var service = Fresh(root);
        string id = service.CurrentSlotId, path = service.SaveFilePath;
        JsonObject file = JsonNode.Parse(File.ReadAllText(path)).AsObject();
        JsonObject profile = file["profile"].AsObject();
        profile.Remove("currentHub"); profile.Remove("unlockedHubMask");
        string legacy = file.ToJsonString();
        File.WriteAllText(path, legacy); File.WriteAllText(path + ".bak", legacy);
        Check(service.LoadSlot(id) && service.Profile.currentHub == 0 && service.Profile.unlockedHubMask == 1, "old save missing fields migrates safely");
        Check(File.ReadAllText(path) == legacy, "migration is read-only until normal save");
        profile["currentHub"] = 2; profile["unlockedHubMask"] = 0;
        File.WriteAllText(path, file.ToJsonString());
        Check(service.LoadSlot(id) && service.Profile.currentHub == 0 && service.Profile.unlockedHubMask == 1, "locked imported town falls back to zero");
        profile["currentHub"] = -2147483648; profile["unlockedHubMask"] = 8;
        File.WriteAllText(path, file.ToJsonString());
        Check(service.LoadSlot(id) && service.Profile.currentHub == 0 && service.Profile.unlockedHubMask == 1, "invalid current and unknown-only mask migrate");
        service.Save();
        string before = State(service), disk = File.ReadAllText(path), backup = File.ReadAllText(path + ".bak");
        int events = 0; service.Changed += () => events++;
        Check(!service.TravelToHub(1) && !service.TravelToHub(2) && !service.TravelToHub(-1) && !service.TravelToHub(3), "locked and invalid destinations rejected");
        Check(State(service) == before && File.ReadAllText(path) == disk && File.ReadAllText(path + ".bak") == backup && events == 0, "rejected travel preserves memory and both files");
        service.Profile.level = 5; service.Profile.gold = 765; service.Profile.potions = 9; service.Save();
        string gear = JsonUtility.ToJson(service.Profile.inventory, true), weapon = service.Profile.weaponId;
        Check(service.TravelToHub(1) && events == 1 && service.Profile.currentHub == 1 && service.Profile.unlockedHubMask == 3, "unlocked travel publishes once");
        Check(service.Profile.gold == 765 && service.Profile.potions == 9 && service.Profile.weaponId == weapon && JsonUtility.ToJson(service.Profile.inventory,true) == gear,
            "travel preserves gold, potions, equipment IDs and all item stats without reroll");
        Check(service.LoadSlot(id) && service.Profile.currentHub == 1 && service.Profile.unlockedHubMask == 3, "destination and unlocks persist in selected slot");
        service.Profile.level = 1; service.Profile.clearedRuns = 1; service.Save();
        Check(service.TravelToHub(2) && service.Profile.unlockedHubMask == 7, "first clear unlocks final town while retaining earlier town");
        Check(!service.TravelToHub(0)&&service.TravelToHub(1)&&service.TravelToHub(0) && service.Profile.gold == 765 && service.CurrentSlotId == id, "sequential return home stays same character and currency");
        before = State(service); disk = File.ReadAllText(path); backup = File.ReadAllText(path + ".bak"); events = 0;
        Directory.CreateDirectory(path + ".tmp");
        Check(!service.TravelToHub(1), "failed adjacent destination write rejects travel");
        Check(State(service) == before && File.ReadAllText(path) == disk && File.ReadAllText(path + ".bak") == backup && events == 0, "failed travel retains old hub, gear and files");
        Directory.Delete(path + ".tmp");
        Check(service.TravelToHub(1)&&service.TravelToHub(2) && service.LoadSlot(id) && service.Profile.currentHub == 2, "travel retry succeeds after storage recovers");
    }
    private static ItemData Item(string name, Rarity rarity = Rarity.Common)
    {
        return new ItemData { id = Guid.NewGuid().ToString("N"), name = name, slot = ItemSlot.Weapon, rarity = rarity, level = 1, attack = 5, balanceRevision = 1 };
    }
    private static void Economy(string root)
    {
        var service = Fresh(root);
        service.Profile.gold = 1000;
        var sale = Item("sale"); var common = Item("common"); var rare = Item("rare", Rarity.Rare);
        var locked = Item("locked"); locked.locked = true;
        var enhanced = Item("enhanced"); enhanced.upgradeLevel = 1;
        var mechanic = Item("mechanic"); mechanic.mechanic = EquipmentMechanic.ReturningBlade; mechanic.slot = BuildCatalog.MechanicSlot(mechanic.mechanic);
        var epic = Item("epic", Rarity.Epic); var legendary = Item("legendary", Rarity.Legendary);
        service.Profile.inventory.AddRange(new[] { sale, common, rare, locked, enhanced, mechanic, epic, legendary });
        Check(ProgressionService.IsProtectedLoot(enhanced), "raw enhanced gear is protected before migration");
        service.Profile.slotUpgradesInitialized = false; service.Save();
        string path = service.SaveFilePath, disk = File.ReadAllText(path), backup = File.ReadAllText(path + ".bak"), before = State(service);
        GameProfile original = service.Profile;
        int events = 0; service.Changed += () => events++;
        Check(!service.Sell(service.Profile.weaponId) && !service.Sell(locked.id) && !service.Sell("missing"), "single sale protects equipped, locked and missing IDs");
        Check(State(service) == before && events == 0, "protected sale changes nothing");
        Directory.CreateDirectory(path + ".tmp");
        Check(!service.Sell(sale.id) && !service.BuyPotion() && service.BulkSellLowQuality() == 0, "storage failure rejects every economy route");
        Check(ReferenceEquals(service.Profile, original) && State(service) == before && events == 0, "failed sale, potion and bulk publish no memory or events");
        Check(File.ReadAllText(path) == disk && File.ReadAllText(path + ".bak") == backup, "failed economy preserves primary and backup bytes");
        Directory.Delete(path + ".tmp");
        int value = service.SellValue(sale), gold = service.Profile.gold, potions = service.Profile.potions;
        Check(service.Sell(sale.id) && service.Profile.gold == gold + value && events == 1, "single sale commits proceeds and inventory once");
        gold = service.Profile.gold; before = State(service); disk = File.ReadAllText(path);
        Check(!service.Sell(sale.id) && State(service) == before && File.ReadAllText(path) == disk && events == 1, "repeat sale cannot pay twice or rewrite");
        Check(service.BuyPotion() && service.Profile.gold == gold - ProgressionService.PotionPrice && service.Profile.potions == potions + 1 && events == 2,
            "potion cost and count commit together");
        gold = service.Profile.gold; value = service.SellValue(common) + service.SellValue(rare);
        Check(service.BulkSellLowQuality() == 2 && service.Profile.gold == gold + value && events == 3, "bulk commits exactly eligible two items");
        Check(service.Profile.inventory.Exists(i=>i.id==locked.id) && service.Profile.inventory.Exists(i=>i.id==enhanced.id) &&
            service.Profile.inventory.Exists(i=>i.id==mechanic.id) && service.Profile.inventory.Exists(i=>i.id==epic.id) &&
            service.Profile.inventory.Exists(i=>i.id==legendary.id) && service.Profile.inventory.Exists(i=>i.id==service.Profile.weaponId), "bulk retains every protection class");
        before = State(service); disk = File.ReadAllText(path); backup = File.ReadAllText(path + ".bak");
        Check(service.BulkSellLowQuality() == 0 && State(service) == before && File.ReadAllText(path) == disk && File.ReadAllText(path + ".bak") == backup && events == 3,
            "repeated empty bulk is read-only");
        var reader = new ProgressionService(root);
        Check(reader.LoadSlot(service.CurrentSlotId) && reader.Profile.gold == service.Profile.gold && reader.Profile.potions == service.Profile.potions &&
            !reader.Profile.inventory.Exists(i=>i.id==sale.id||i.id==common.id||i.id==rare.id), "economy persists into same stable slot");
        service.Profile.gold = ProgressionService.PotionPrice - 1; before = State(service);
        Check(!service.BuyPotion() && State(service) == before, "insufficient money cannot become negative");
        service.Profile.gold = 100; service.Profile.potions = 99; before = State(service);
        Check(!service.BuyPotion() && State(service) == before, "potion cap rejects charge");
        service.Profile.potions = 0; service.Profile.gold = ProgressionService.PotionPrice;
        Check(service.BuyPotion() && service.Profile.gold == 0 && service.Profile.potions == 1 && !service.BuyPotion(), "last affordable potion leaves zero gold, second rejected");
        var capped = Item("capped"); service.Profile.inventory.Add(capped); service.Profile.gold = 999999995;
        Check(service.Sell(capped.id) && service.Profile.gold == 999999999, "single sale saturates gold cap safely");
        service.Profile.inventory.Add(Item("bulk-cap"));
        Check(service.BulkSellLowQuality() == 1 && service.Profile.gold == 999999999, "bulk cannot overflow gold cap");
    }
}
