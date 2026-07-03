using System.Collections.Generic;
using System.Linq;
using Godot;
using Xunit;

// M4.5 sub-rungs 2–4: raw extraction is build-gated (an un-built deposit produces nothing; a player-built
// Mine/Farm starts the flow), throttled + treasury-bounded so one build choice bites, and each gain is
// recorded for the "+1" feedback. Plain .NET (no engine).
public class EconomyBuildGateTests
{
    private static Dictionary<Vector2I, Vector2I> SquareLand(int size, out Vector2I capital)
    {
        capital = new Vector2I(0, 0);
        var land = new Dictionary<Vector2I, Vector2I>();
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                land[new Vector2I(x, y)] = capital;
        return land;
    }

    private static Economy PlayerEconomy(out Vector2I capital, int seed = 99)
    {
        var land = SquareLand(12, out capital);
        return new Economy(land, new List<Vector2I> { capital }, seed);
    }

    // Lowest-coordinate deposit tile the player owns (deterministic from the seed).
    private static Vector2I FirstDepositTile(Economy e) =>
        e.TileDeposits.Keys
            .Where(t => e.TileNation.TryGetValue(t, out Nation n) && n.IsPlayer)
            .OrderBy(t => t.X).ThenBy(t => t.Y)
            .First();

    // LR: lay a contiguous L-shaped road from `tile` to `capital` so the deposit's output passes the
    // deposit→capital connectivity gate and banks into the national stockpile.
    private static void ConnectToCapital(Economy e, Vector2I tile, Vector2I capital)
    {
        int x = tile.X, y = tile.Y;
        while (x != capital.X) { e.Railroads.Add(new Vector2I(x, y)); x += x < capital.X ? 1 : -1; }
        while (y != capital.Y) { e.Railroads.Add(new Vector2I(x, y)); y += y < capital.Y ? 1 : -1; }
    }

    // LR6: a Farm/Mine can't produce without its matching Granary/Warehouse built somewhere in the nation's
    // territory — build it on any empty owned tile (NOT assumed to be the capital, since the caller's
    // extractor can itself sit on the lowest-coordinate owned deposit, which the capital tile can be),
    // topping up treasury first so the storage cost never blocks it or muddies a caller's own assertions.
    private static void BuildMatchingStorage(Economy e, Good good)
    {
        BuildingType storageType = good == Good.Ore ? BuildingType.Warehouse : BuildingType.Granary;
        Vector2I tile = e.PlayerNation.Tiles.First(t => !e.TileBuildings.ContainsKey(t));
        e.PlayerNation.Treasury += Building.Of(storageType).Cost;
        bool built = storageType == BuildingType.Granary ? e.TryBuildGranary(tile) : e.TryBuildWarehouse(tile);
        Assert.True(built, "test setup: matching storage building must place somewhere in the nation's territory");
    }

    // ---- Sub-rung 2: build-gated extraction ----

    [Fact]
    public void Unbuilt_deposit_produces_nothing()
    {
        var economy = PlayerEconomy(out _);
        economy.PlayerNation.Population = 0f; // silence grain demand so the starting larder can't earn income
        Assert.NotEmpty(economy.Deposits);
        Assert.Empty(economy.TileBuildings); // M4.5: no auto-placed extractors
        for (int i = 0; i < 10; i++) economy.OnTick();

        // No buildings → no production → nothing in TileInventory, no haulers, no income.
        Assert.Equal(0f, economy.TotalIncome, 3);
        Assert.Empty(economy.Haulers);
    }

    [Fact]
    public void TryBuildExtractor_places_the_matching_type_charges_and_starts_the_flow()
    {
        var economy = PlayerEconomy(out _);
        economy.PlayerNation.Treasury = 1000f;
        economy.PlayerNation.Population = 0f;   // isolate extraction from the M5 pop demand sink
        Vector2I tile = FirstDepositTile(economy);
        Good good = economy.TileDeposits[tile].Good;
        float before = economy.PlayerNation.Treasury;

        Assert.True(economy.TryBuildExtractor(tile));
        ConnectToCapital(economy, tile, economy.PlayerNation.Capital); // road the deposit so the hauler can move

        // Mine on an Ore deposit, Farm on a Grain deposit — auto-picked from the deposit.
        BuildingType expected = good == Good.Ore ? BuildingType.Mine : BuildingType.Farm;
        Assert.Equal(expected, economy.TileBuildings[tile].Type);
        Assert.Equal(before - Building.ExtractorCost, economy.PlayerNation.Treasury, 3);

        BuildMatchingStorage(economy, good); // LR6: the gate needs it before Pass 1 will run this extractor

        // Flow starts: nothing until the first producing tick (every other tick), then a hauler is dispatched.
        economy.OnTick(); // tick 1 (odd: throttled — no output)
        Assert.Empty(economy.ProducedThisTick);
        economy.OnTick(); // tick 2 (even: produces)
        // The producing tick recorded a gain for this tile (whether the good then hauls, sits, or sells).
        Assert.Contains(economy.ProducedThisTick, e => e.Tile == tile && e.Good == good);
    }

    [Fact]
    public void TryBuildExtractor_fails_on_a_tile_without_a_deposit()
    {
        var economy = PlayerEconomy(out _);
        economy.PlayerNation.Treasury = 1000f;
        Vector2I bare = new Vector2I(9999, 9999); // off-map → no deposit (the gate's first check)

        Assert.False(economy.TryBuildExtractor(bare));
        Assert.False(economy.TileBuildings.ContainsKey(bare));
    }

    // ---- Sub-rung 3: throttle + small starting treasury ----

    [Fact]
    public void Starting_treasury_affords_exactly_one_extractor()
    {
        var economy = PlayerEconomy(out _);
        Assert.Equal(Building.ExtractorCost, economy.PlayerNation.Treasury, 3);

        var depositTiles = economy.TileDeposits.Keys
            .Where(t => economy.TileNation.TryGetValue(t, out Nation n) && n.IsPlayer)
            .OrderBy(t => t.X).ThenBy(t => t.Y).ToList();

        Assert.True(economy.TryBuildExtractor(depositTiles[0]));   // first: affordable
        Assert.Equal(0f, economy.PlayerNation.Treasury, 3);        // treasury spent
        Assert.False(economy.TryBuildExtractor(depositTiles[1]));  // second: can't afford
    }

    // ---- Railroads as a treasury spend ----

    [Fact]
    public void TryBuildRailroad_charges_treasury_once_per_tile_and_respects_affordability()
    {
        var economy = PlayerEconomy(out _);
        economy.PlayerNation.Treasury = 1000f;
        var tile = new Vector2I(3, 4);

        float before = economy.PlayerNation.Treasury;
        Assert.True(economy.TryBuildRailroad(tile));   // first lay: charged
        Assert.True(economy.PlayerNation.Treasury < before);
        Assert.Contains(tile, economy.Railroads);

        float afterFirst = economy.PlayerNation.Treasury;
        Assert.False(economy.TryBuildRailroad(tile));  // same tile: no double-charge
        Assert.Equal(afterFirst, economy.PlayerNation.Treasury, 3);

        economy.PlayerNation.Treasury = 0f;
        Assert.False(economy.TryBuildRailroad(new Vector2I(7, 8))); // can't afford
        Assert.DoesNotContain(new Vector2I(7, 8), economy.Railroads);
    }

    // ---- Sub-rung 4: per-gain feedback events ----

    [Fact]
    public void Production_records_a_gain_event_on_a_producing_tick()
    {
        var economy = PlayerEconomy(out _);
        economy.PlayerNation.Treasury = 1000f;
        Vector2I tile = FirstDepositTile(economy);
        Good good = economy.TileDeposits[tile].Good;
        economy.TryBuildExtractor(tile);
        BuildMatchingStorage(economy, good); // LR6: the gate needs it before Pass 1 will run this extractor

        economy.OnTick(); // odd tick: throttled off → no gain
        Assert.Empty(economy.ProducedThisTick);

        economy.OnTick(); // even tick: produces → one event for the source tile
        Assert.Contains(economy.ProducedThisTick, e => e.Tile == tile && e.Good == good && e.Amount == 1f);
    }
}
