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

    // ---- Sub-rung 2: build-gated extraction ----

    [Fact]
    public void Unbuilt_deposit_produces_nothing()
    {
        var economy = PlayerEconomy(out _);
        Assert.NotEmpty(economy.Deposits);
        Assert.Empty(economy.TileBuildings); // M4.5: no auto-placed extractors

        for (int i = 0; i < 10; i++) economy.OnTick();

        Assert.Equal(0f, economy.Nations.Values.Sum(n => n.Stockpile.Values.Sum()), 3);
    }

    [Fact]
    public void TryBuildExtractor_places_the_matching_type_charges_and_starts_the_flow()
    {
        var economy = PlayerEconomy(out _);
        economy.PlayerNation.Treasury = 1000f;
        Vector2I tile = FirstDepositTile(economy);
        Good good = economy.TileDeposits[tile].Good;
        float before = economy.PlayerNation.Treasury;

        Assert.True(economy.TryBuildExtractor(tile));

        // Mine on an Ore deposit, Farm on a Grain deposit — auto-picked from the deposit.
        BuildingType expected = good == Good.Ore ? BuildingType.Mine : BuildingType.Farm;
        Assert.Equal(expected, economy.TileBuildings[tile].Type);
        Assert.Equal(before - Building.ExtractorCost, economy.PlayerNation.Treasury, 3);

        // Flow starts: nothing until the first producing tick (every other tick), then +1.
        economy.OnTick();
        economy.OnTick();
        Assert.Equal(1f, economy.PlayerNation.Stockpile.GetValueOrDefault(good), 3);
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

        economy.OnTick(); // odd tick: throttled off → no gain
        Assert.Empty(economy.ProducedThisTick);

        economy.OnTick(); // even tick: produces → one event for the source tile
        Assert.Contains(economy.ProducedThisTick, e => e.Tile == tile && e.Good == good && e.Amount == 1f);
    }
}
