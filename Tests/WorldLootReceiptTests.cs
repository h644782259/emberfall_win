// Executable service regressions under unique fake-save directories. No Unity runtime.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Emberfall;

public static class WorldLootReceiptTests
{
    private static int checks;
    private static void Check(bool okay, string why) { checks++; if (!okay) throw new InvalidOperationException(why); }
    private static HashSet<string> Receipts(ProgressionService p)
    { return (HashSet<string>)typeof(ProgressionService).GetField("collectedLootIds", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(p); }
    private static ItemData Item(string id, Rarity rarity = Rarity.Epic)
    { return new ItemData { id = id, name = "Isolated receipt fixture", rarity = rarity, slot = ItemSlot.Armor, level = 2, health = 10 }; }
    private static ProgressionService Fresh(string root)
    {
        var p = new ProgressionService(Path.Combine(root, Guid.NewGuid().ToString("N")));
        Check(p.CreateNewSlot(HeroClass.Summoner), "isolated slot created"); return p;
    }
    public static string Run(string directory)
    {
        checks = 0;
        string root = Path.Combine(directory, "world-loot-" + Guid.NewGuid().ToString("N"));
        FailedSaveCanRetrySameIdentity(root); ProtectedCapacityCanRetrySameIdentity(root);
        BoundaryGuardsAndDurableIdentity(root); RepeatedWorldRetirement(root);
        return "PASS: " + checks + " loot retry/receipt-boundary assertions in isolated fake saves";
    }
    private static void FailedSaveCanRetrySameIdentity(string root)
    {
        var p = Fresh(root); ItemData loot = p.RollLoot(2, true); string id = loot.id;
        int count = p.Profile.inventory.Count, gold = p.Profile.gold; int events = 0;
        p.Changed += () => events++;
        string primary = File.ReadAllText(p.SaveFilePath), backup = File.ReadAllText(p.SaveFilePath + ".bak");
        File.Copy(p.SaveFilePath, p.SaveFilePath + ".tmp");
        for (int retry = 0; retry < 3; retry++)
        {
            Check(!p.CollectLoot(loot) && loot.id == id && p.Profile.inventory.Count == count && p.Profile.gold == gold,
                "failed pickup leaves exact rolled object available without granting inventory/gold");
            Check(Receipts(p).Count == 0 && events == 0 && File.ReadAllText(p.SaveFilePath) == primary &&
                File.ReadAllText(p.SaveFilePath + ".bak") == backup, "failed collection publishes no receipt/event or durable mutation");
        }
        File.Delete(p.SaveFilePath + ".tmp");
        Check(p.CollectLoot(loot) && p.Profile.inventory.Count == count + 1 && p.Profile.inventory.Count(x => x.id == id) == 1 && events == 1,
            "retry of retained identity commits exactly once");
        Check(!p.CollectLoot(loot) && p.Profile.inventory.Count == count + 1 && events == 1, "repeated successful receipt cannot duplicate acquisition");
        Check(p.LoadSlot(p.CurrentSlotId) && p.Profile.inventory.Count(x => x.id == id) == 1, "successful retry survives actual reload");
    }
    private static void ProtectedCapacityCanRetrySameIdentity(string root)
    {
        var p = Fresh(root);
        while (p.Profile.inventory.Count < ProgressionService.MaximumRetainedEquipment) p.Profile.inventory.Add(Item("bag-" + p.Profile.inventory.Count));
        p.Save(); ItemData loot = Item("retained-protected"); int gold = p.Profile.gold;
        Check(!p.CollectLoot(loot) && p.Profile.gold == gold && !Receipts(p).Contains(loot.id), "retention safety boundary leaves rejected world identity available");
        p.Profile.inventory.RemoveAt(p.Profile.inventory.Count-1);p.Save();
        Check(p.CollectLoot(loot) && p.Profile.inventory.Count(x => x.id == loot.id) == 1 && p.Profile.gold == gold, "same world identity can retry when visible capacity is freed");
        Check(!p.CollectLoot(loot), "successful admission is idempotent");
    }
    private static void BoundaryGuardsAndDurableIdentity(string root)
    {
        var p = Fresh(root); p.Profile.autoSellCommon = true;
        ItemData sold = Item("world-autosold", Rarity.Common); int gold = p.Profile.gold;
        Check(p.CollectLoot(sold) && p.Profile.gold == gold && !p.Profile.autoSellCommon && p.Sell(sold.id), "legacy autosell is disabled; explicit sale creates an absent-item receipt");
        int soldGold = p.Profile.gold;
        ItemData held = Item("world-held"), recovered = Item("world-recovered");
        Check(p.CollectLoot(held) && p.PreserveGroundLoot(new[] { recovered }), "fixture covers bag and recovery identities");
        int receipts = Receipts(p).Count;
        for (int flags = 0; flags < 16; flags++)
        {
            bool committed = (flags & 1) != 0, pendingEmpty = (flags & 2) != 0;
            bool producers = (flags & 4) != 0, epoch = (flags & 8) != 0;
            if (flags == 15) continue;
            Check(!p.TryRetireWorldLootReceipts(committed, pendingEmpty ? 0 : 1, producers, epoch) && Receipts(p).Count == receipts,
                "every incomplete boundary retains all collection receipts, flags " + flags);
        }
        p.Save();
        Check(!p.CollectLoot(sold) && p.Profile.gold == soldGold && Receipts(p).Count == receipts, "ordinary saves never release a manual-sale receipt");
        ItemData sellLater = Item("world-manual-sale", Rarity.Rare);
        Check(p.CollectLoot(sellLater) && p.Sell(sellLater.id), "fixture collects then manually sells an unprotected item");
        int afterSale = p.Profile.gold;
        Check(!p.CollectLoot(sellLater) && p.Profile.gold == afterSale, "manual sale cannot release its world receipt");
        string disk = File.ReadAllText(p.SaveFilePath), backup = File.ReadAllText(p.SaveFilePath + ".bak");
        int events = 0; p.Changed += () => events++;
        Check(p.TryRetireWorldLootReceipts(true, 0, true, true) && Receipts(p).Count == 0, "complete boundary releases receipts");
        Check(p.TryRetireWorldLootReceipts(true, 0, true, true) && events == 0 && File.ReadAllText(p.SaveFilePath) == disk &&
            File.ReadAllText(p.SaveFilePath + ".bak") == backup, "boundary retirement is idempotent and never saves or emits a profile change");
        Check(!p.CollectLoot(held) && !p.CollectLoot(recovered), "durable bag/recovery identities still reject duplicates after receipt retirement");
        int recoveryCount = p.Profile.recoveryLoot.Count;
        Check(p.PreserveGroundLoot(new[] { held, recovered }) && p.Profile.recoveryLoot.Count == recoveryCount,
            "preservation still deduplicates against durable storage after retirement");
    }
    private static void RepeatedWorldRetirement(string root)
    {
        var p = Fresh(root); p.Profile.autoSellCommon = true;
        for (int world = 0; world < 24; world++)
        {
            for (int drop = 0; drop < 4; drop++) Check(p.CollectLoot(Item("world-" + world + "-drop-" + drop, Rarity.Common)), "new world drop commits");
            Check(Receipts(p).Count == 4, "receipt set contains only this world's four new identities");
            Check(p.TryRetireWorldLootReceipts(true, 0, true, true) && Receipts(p).Count == 0, "successful world retirement prevents history growth");
        }
    }
}
