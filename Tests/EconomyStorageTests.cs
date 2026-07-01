using System.Collections.Generic;
using System.Linq;
using Godot;
using Xunit;

// LR6: storage buildings (Granary/Warehouse) gate and cap raw extraction — a Farm/Mine can't produce at all
// without the matching storage built somewhere in the nation's territory, and once built, total stored
// goods can't exceed its capacity (extraction pauses, doesn't waste the deposit reserve). Also covers the
// player's manual bulk export (TryExportManual), which reuses RunExport's buyer-routing math for a
// player-chosen amount instead of the automatic TradeUnit trickle. Plain .NET (no engine).
public class EconomyStorageTests
{
    private static readonly Vector2I Capital = new(0, 0);
    private static readonly Vector2I Farm    = new(0, 2);
    private static readonly Vector2I Mine    = new(0, 3);
    private static readonly Vector2I Link    = new(0, 1); // the single road tile between Farm/Mine and capital

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

    // ---- Gate: no storage ⇒ no production ----

    [Fact]
    public void Farm_without_a_granary_produces_nothing()
    {
        var economy = Bare();
        economy.TileDeposits[Farm] = new Deposit(Good.Grain, new HashSet<Vector2I> { Farm }, 100f, 1f);
        economy.TileBuildings[Farm] = Building.Of(BuildingType.Farm);
        economy.Railroads.Add(Link);

        for (int i = 0; i < 6; i++) economy.OnTick();

        Assert.Equal(100f, economy.TileDeposits[Farm].Reserve, 3); // untouched — extraction never ran
        Assert.Equal(0f, economy.TileInventory.AmountAt(Farm, Good.Grain), 3);
        Assert.Empty(economy.Haulers);
    }

    [Fact]
    public void Mine_without_a_warehouse_produces_nothing()
    {
        var economy = Bare();
        economy.TileDeposits[Mine] = new Deposit(Good.Ore, new HashSet<Vector2I> { Mine }, 100f, 1f);
        economy.TileBuildings[Mine] = Building.Of(BuildingType.Mine);
        economy.Railroads.Add(Link);
        economy.Railroads.Add(new Vector2I(0, 2)); // extend the link one further tile to reach Mine at y=3

        for (int i = 0; i < 6; i++) economy.OnTick();

        Assert.Equal(100f, economy.TileDeposits[Mine].Reserve, 3);
        Assert.Equal(0f, economy.TileInventory.AmountAt(Mine, Good.Ore), 3);
    }

    [Fact]
    public void Building_a_granary_unblocks_farm_production()
    {
        var economy = Bare();
        economy.TileDeposits[Farm] = new Deposit(Good.Grain, new HashSet<Vector2I> { Farm }, 100f, 1f);
        economy.TileBuildings[Farm] = Building.Of(BuildingType.Farm);
        economy.Railroads.Add(Link);

        economy.OnTick();
        Assert.Equal(100f, economy.TileDeposits[Farm].Reserve, 3); // still gated, no Granary yet

        economy.PlayerNation.Treasury += Building.Of(BuildingType.Granary).Cost;
        Assert.True(economy.TryBuildGranary(Capital));
        for (int i = 0; i < 4; i++) economy.OnTick(); // a couple of producing (even) ticks

        Assert.True(economy.TileDeposits[Farm].Reserve < 100f, "a built Granary should unblock extraction");
    }

    // ---- Cap: storage full ⇒ production pauses without wasting the reserve ----

    [Fact]
    public void Production_pauses_once_storage_is_full_without_depleting_the_deposit()
    {
        var economy = Bare();
        economy.TileDeposits[Farm] = new Deposit(Good.Grain, new HashSet<Vector2I> { Farm }, 1000f, 1f);
        economy.TileBuildings[Farm] = Building.Of(BuildingType.Farm);
        economy.TileBuildings[Capital] = Building.Of(BuildingType.Granary); // capacity = StorageCapacityPerBuilding
        economy.Railroads.Add(Link);

        // Run far longer than it would take, uncapped, to reach the Granary's capacity.
        int ticks = (int)(Building.StorageCapacityPerBuilding * 2) + 40;
        for (int i = 0; i < ticks; i++) economy.OnTick();

        float stored = economy.TileInventory.AmountAt(Farm, Good.Grain) + economy.TileInventory.AmountAt(Capital, Good.Grain);
        Assert.True(stored <= Building.StorageCapacityPerBuilding + 1f,
            $"stored ({stored}) should have capped near the Granary's capacity ({Building.StorageCapacityPerBuilding})");
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
    public void TryExportManual_is_blockaded_like_automatic_export()
    {
        var e = QuietTwoNation(16, out var capA, out var capB);
        Road(e, 16);
        e.TileInventory.Add(capA, Good.Ore, 10f);
        e.Nations[capB].Treasury = 1000f;
        e.SetPlayerStance(capB, DiplomaticStance.Hostile);

        float sold = e.TryExportManual(Good.Ore, 5f);

        Assert.Equal(0f, sold, 3);
        Assert.Equal(10f, e.TileInventory.AmountAt(capA, Good.Ore), 3); // nothing crossed the blockade
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

    // ---- AI bootstrapping under the new gate ----

    [Fact]
    public void Ai_eventually_builds_storage_and_produces_under_the_gate()
    {
        var land = TwoNationLand(16, out var capA, out var capB);
        var economy = new Economy(land, new List<Vector2I> { capA, capB }, 2024);
        Nation ai = economy.Nations[capB];
        Assert.Contains(economy.TileDeposits.Keys, t => economy.TileNation.GetValueOrDefault(t) == ai);

        // Bootstrapping now costs an extractor (50g) AND its matching storage (75g) — several tax intervals
        // (50g/10 ticks) beyond the 50g starting treasury. 200 ticks gives ample margin either order.
        for (int i = 0; i < 200; i++) economy.OnTick();

        Assert.True(
            economy.TileBuildings.Any(kv => economy.TileNation.GetValueOrDefault(kv.Key) == ai &&
                                             (kv.Value.Type == BuildingType.Granary || kv.Value.Type == BuildingType.Warehouse)),
            "the AI should have built a matching storage building by now");
        Assert.True(ai.Stockpile.Values.Sum() > 0f, "the AI should have produced something once storage unblocked it");
    }
}
