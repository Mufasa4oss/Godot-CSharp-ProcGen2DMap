using System.Collections.Generic;
using Godot;
using Xunit;

// LR5: recurring upkeep — the gold sink. Every TaxIntervalTicks each nation pays UpkeepPerBuilding per building
// it owns, and the player pays UpkeepPerRoad per road tile. Plain .NET (no engine).
public class EconomyUpkeepTests
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

    private static Economy Quiet(out Vector2I cap)
    {
        var land = SquareLand(12, out cap);
        var e = new Economy(land, new List<Vector2I> { cap }, 99);
        e.TileBuildings.Clear();
        e.PlayerNation.Population = 0f; // silence tax + grain so the only treasury move is upkeep
        return e;
    }

    [Fact]
    public void Buildings_and_roads_cost_upkeep_each_interval()
    {
        var e = Quiet(out Vector2I cap);
        e.TileBuildings[new Vector2I(5, 5)] = Building.Of(BuildingType.Workshop); // 1 building
        e.Railroads.Add(new Vector2I(1, 0));
        e.Railroads.Add(new Vector2I(2, 0));                                      // 2 roads
        e.PlayerNation.Treasury = 1000f;
        float expected = Economy.UpkeepPerBuilding + 2 * Economy.UpkeepPerRoad;

        for (int i = 0; i < Economy.TaxIntervalTicks; i++) e.OnTick();            // one interval

        Assert.Equal(expected, e.UpkeepPaidLastInterval, 3);
        Assert.Equal(1000f - expected, e.PlayerNation.Treasury, 3);
    }

    [Fact]
    public void No_buildings_or_roads_means_no_upkeep()
    {
        var e = Quiet(out _);
        e.PlayerNation.Treasury = 1000f;

        for (int i = 0; i < Economy.TaxIntervalTicks; i++) e.OnTick();

        Assert.Equal(0f, e.UpkeepPaidLastInterval, 3);
        Assert.Equal(1000f, e.PlayerNation.Treasury, 3);
    }

    [Fact]
    public void Upkeep_cannot_push_the_treasury_below_zero()
    {
        var e = Quiet(out _);
        e.TileBuildings[new Vector2I(4, 4)] = Building.Of(BuildingType.Workshop);
        e.PlayerNation.Treasury = 1f; // less than one building's upkeep

        for (int i = 0; i < Economy.TaxIntervalTicks; i++) e.OnTick();

        Assert.Equal(0f, e.PlayerNation.Treasury, 3); // floored, no negative debt
    }
}
