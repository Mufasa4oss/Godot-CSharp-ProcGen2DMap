using System.Collections.Generic;
using Godot;
using Xunit;

// LR6 storage (reworked): a Granary/Warehouse is a stockpile ceiling, not a gate. A Mine/Farm produces from
// the first tick up to a base capacity; each storage building raises that ceiling. At the cap, extraction
// pauses without wasting the deposit reserve. Also covers the player's manual bulk export (TryExportManual),
// which reuses RunExport's buyer-routing math for a player-chosen amount. Plain .NET (no engine).
public class EconomyStorageTests
{
    private static readonly Vector2I Capital = new(0, 0);
    private static readonly Vector2I Farm    = new(0, 2);
    private static readonly Vector2I Link    = new(0, 1); // the single road tile between Farm and capital

    private static Dictionary<Vector2I, Vector2I> SquareLand(int size, Vector2I capital)
    {
        var land = new Dictionary<Vector2I, Vector2I>();
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                land[new Vector2I(x, y)] = capital;
        return land;
    }

    // A one-nation economy with no auto-deposits/buildings interfering and no pop demand, so the only
    // production/consumption in play is what the test sets up.
    private static Economy Bare()
    {
        var economy = new Economy(SquareLand(8, Capital), new List<Vector2I> { Capital }, 1);
        economy.TileBuildings.Clear();
        economy.TileDeposits.Clear();
        economy.PlayerNation.Population = 0f; // isolate extraction from the grain-demand sink
        return economy;
    }

    // ---- Base capacity: extraction runs without a storage building, capped ----

    [Fact]
    public void Farm_produces_without_a_granary_up_to_base_capacity()
    {
        var economy = Bare();
        economy.TileDeposits[Farm] = new Deposit(Good.Grain, new HashSet<Vector2I> { Farm }, 1000f, 1f);
        economy.TileBuildings[Farm] = Building.Of(BuildingType.Farm);
        economy.Railroads.Add(Link);

        // No Granary, no consumption (pop 0): a Farm still runs, filling the base capacity then pausing.
        for (int i = 0; i < 400; i++) economy.OnTick();

        float stored = economy.StoredAmountOf(economy.PlayerNation, Good.Grain);
        Assert.True(stored > 0f, "a Farm should produce even with no Granary (base storage capacity)");
        Assert.True(stored <= Economy.BaseStorageCapacity + 1f,
            $"stored ({stored}) should cap at the base capacity ({Economy.BaseStorageCapacity})");
        Assert.True(economy.TileDeposits[Farm].Reserve > 0f, "the reserve isn't wasted once extraction pauses");
    }

    [Fact]
    public void Building_a_granary_raises_the_storage_ceiling()
    {
        var economy = Bare();
        Nation p = economy.PlayerNation;
        Assert.Equal(Economy.BaseStorageCapacity, economy.StorageCapacityOf(p, Good.Grain), 3);

        p.Treasury += Building.Of(BuildingType.Granary).Cost;
        Assert.True(economy.TryBuildGranary(Capital));

        Assert.Equal(Economy.BaseStorageCapacity + Building.StorageCapacityPerBuilding,
            economy.StorageCapacityOf(p, Good.Grain), 3);
    }

    // ---- Cap: stockpile full ⇒ extraction pauses without wasting the reserve ----

    [Fact]
    public void Production_pauses_once_storage_is_full_without_depleting_the_deposit()
    {
        var economy = Bare();
        economy.TileDeposits[Farm] = new Deposit(Good.Grain, new HashSet<Vector2I> { Farm }, 1000f, 1f);
        economy.TileBuildings[Farm] = Building.Of(BuildingType.Farm);
        economy.TileBuildings[Capital] = Building.Of(BuildingType.Granary);
        economy.Railroads.Add(Link);

        float cap = Economy.BaseStorageCapacity + Building.StorageCapacityPerBuilding;
        int ticks = (int)(cap * 2) + 40;
        for (int i = 0; i < ticks; i++) economy.OnTick();

        float stored = economy.StoredAmountOf(economy.PlayerNation, Good.Grain);
        Assert.True(stored <= cap + 1f, $"stored ({stored}) should cap near capacity ({cap})");
        Assert.True(economy.TileDeposits[Farm].Reserve > 0f,
            "the deposit shouldn't fully deplete once storage caps further extraction");
    }

    // ---- Manual export ----

    // capA (0,0) = player (left half), capB (size-1,0) = AI (right half) — same shape as EconomyExportTests.
    private static Dictionary<Vector2I, Vector2I> TwoNationLand(int size, out Vector2I capA, out Vector2I capB)
    {
        capA = new Vector2I(0, 0);
        capB = new Vector2I(size - 1, 0);
        var land = new Dictionary<Vector2I, Vector2I>();
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                land[new Vector2I(x, y)] = x < size / 2 ? capA : capB;
        return land;
    }

    private static Economy QuietTwoNation(int size, out Vector2I capA, out Vector2I capB)
    {
        var land = TwoNationLand(size, out capA, out capB);
        var e = new Economy(land, new List<Vector2I> { capA, capB }, 2024);
        e.EnableAiAutonomy = false;
        e.PlayerNation.Population = 0f;
        return e;
    }

    private static void Road(Economy e, int size)
    {
        for (int x = 1; x <= size - 2; x++) e.Railroads.Add(new Vector2I(x, 0));
    }

    [Fact]
    public void TryExportManual_sells_the_requested_amount_to_a_connected_buyer()
    {
        var e = QuietTwoNation(16, out var capA, out var capB);
        Road(e, 16);
        e.TileInventory.Add(capA, Good.Ore, 10f);
        Nation buyer = e.Nations[capB];
        buyer.Treasury = 1000f;
        float sellerBefore = e.PlayerNation.Treasury;

        float sold = e.TryExportManual(Good.Ore, 5f);

        Assert.Equal(5f, sold, 3);
        Assert.Equal(5f, e.TileInventory.AmountAt(capA, Good.Ore), 3); // 10 - 5 sold
        Assert.True(e.PlayerNation.Treasury > sellerBefore, "manual export should have earned gold");
        Assert.True(buyer.Stockpile.GetValueOrDefault(Good.Ore) > 0f, "the buyer should have received it");
    }

    [Fact]
    public void TryExportManual_clamps_to_what_is_physically_at_the_capital()
    {
        var e = QuietTwoNation(16, out var capA, out var capB);
        Road(e, 16);
        e.TileInventory.Add(capA, Good.Ore, 3f); // less than the requested amount
        e.Nations[capB].Treasury = 1000f;

        float sold = e.TryExportManual(Good.Ore, 100f);

        Assert.True(sold <= 3f, $"sold ({sold}) should never exceed what was physically at the capital (3)");
        Assert.Equal(0f, e.TileInventory.AmountAt(capA, Good.Ore), 3); // sold out, nothing left over
    }

    [Fact]
    public void TryExportManual_returns_zero_with_no_road()
    {
        var e = QuietTwoNation(16, out var capA, out var capB);
        // No road laid.
        e.TileInventory.Add(capA, Good.Ore, 10f);
        e.Nations[capB].Treasury = 1000f;

        float sold = e.TryExportManual(Good.Ore, 5f);

        Assert.Equal(0f, sold, 3);
    }
}
