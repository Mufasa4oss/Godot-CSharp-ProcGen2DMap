using System.Collections.Generic;
using Godot;
using Xunit;

// Pop dynamics: every nation opens with a grain larder, and each tick its population responds to the grain
// stockpile's trajectory — a surplus (the larder rose) grows pops, a famine (empty larder) shrinks them, and a
// larder that merely holds or draws down but isn't empty leaves them steady. Plain .NET (no engine).
public class EconomyPopulationTests
{
    private static readonly Vector2I Capital = new(0, 0);
    private static readonly Vector2I Farm    = new(0, 2);
    private static readonly Vector2I Link    = new(0, 1); // the single road tile between farm and capital

    private static Dictionary<Vector2I, Vector2I> SquareLand(int size, Vector2I capital)
    {
        var land = new Dictionary<Vector2I, Vector2I>();
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                land[new Vector2I(x, y)] = capital;
        return land;
    }

    // A one-nation economy with no auto-deposits/buildings interfering, so the only grain in play is what the
    // test sets up.
    private static Economy Bare()
    {
        var economy = new Economy(SquareLand(8, Capital), new List<Vector2I> { Capital }, 1);
        economy.TileBuildings.Clear();
        economy.TileDeposits.Clear();
        return economy;
    }

    [Fact]
    public void Every_nation_starts_with_a_grain_larder_of_ten()
    {
        var capA = new Vector2I(0, 0);
        var capB = new Vector2I(15, 0);
        var land = new Dictionary<Vector2I, Vector2I>();
        for (int x = 0; x < 16; x++)
            for (int y = 0; y < 16; y++)
                land[new Vector2I(x, y)] = x < 8 ? capA : capB;

        var economy = new Economy(land, new List<Vector2I> { capA, capB }, 2024);

        // Player larder is physical grain at the capital (TileInventory); AI is abstract (Stockpile).
        Assert.Equal(10f, economy.TileInventory.AmountAt(capA, Good.Grain), 3); // player
        Assert.Equal(10f, economy.Nations[capB].Stockpile.GetValueOrDefault(Good.Grain), 3); // AI
    }

    [Fact]
    public void Population_holds_steady_while_grain_draws_down_but_is_not_empty()
    {
        var economy = Bare();
        // Bare() constructor already placed StartingGrain at the capital TileInventory; nothing to top up.
        economy.PlayerNation.Population = 5f;

        // RunInternalMarket buys 0.5 grain (5 pops × 0.1): larder falls 10 → 9.5 (drawing down, not empty).
        economy.OnTick();

        Assert.Equal(5f, economy.PlayerNation.Population, 3);                             // steady
        Assert.True(economy.TileInventory.AmountAt(Capital, Good.Grain) < 10f);           // larder drew down
    }

    [Fact]
    public void Population_shrinks_five_percent_per_tick_during_a_famine()
    {
        var economy = Bare();
        // Empty the capital TileInventory of its starting grain — no food at all, no farm.
        economy.TileInventory.Add(Capital, Good.Grain, -100f); // floor at 0
        economy.PlayerNation.Population = 5f;

        economy.OnTick();
        Assert.Equal(5f * 0.95f, economy.PlayerNation.Population, 3);          // -5%

        economy.OnTick();
        Assert.Equal(5f * 0.95f * 0.95f, economy.PlayerNation.Population, 3);  // compounds
    }

    [Fact]
    public void Population_grows_when_a_roaded_farm_runs_a_grain_surplus()
    {
        var economy = Bare();
        economy.PlayerNation.Population = 1f; // a small population that one farm easily out-feeds (0.1 grain/tick)

        // A grain deposit + farm at Farm, roaded to the capital so haulers carry grain there.
        economy.TileDeposits[Farm]  = new Deposit(Good.Grain, new HashSet<Vector2I> { Farm }, 100f, 1f);
        economy.TileBuildings[Farm] = Building.Of(BuildingType.Farm);
        economy.TileBuildings[Capital] = Building.Of(BuildingType.Granary); // LR6: gates the farm's output
        economy.Railroads.Add(Link);

        float startPop = economy.PlayerNation.Population;
        for (int i = 0; i < 8; i++) economy.OnTick(); // haulers deliver more grain than RunInternalMarket sells

        Assert.True(economy.PlayerNation.Population > startPop,
            "a grain surplus from a roaded farm should grow the population");
    }
}
