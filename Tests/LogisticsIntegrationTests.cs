using System.Collections.Generic;
using Godot;
using Xunit;

// LR2–LR5 end to end through a real OnTick loop: a mine produces ore onto its tile, an auto-hauler carries it
// down a player-laid road to the capital, and the internal market buys it for income. Cut the road and the
// same ore strands at the mine and earns nothing. This is the spine of the whole logistics rework wired
// together (not just each part in isolation). Plain .NET (no engine).
public class LogisticsIntegrationTests
{
    private static readonly Vector2I Capital = new(0, 0);
    private static readonly Vector2I Mine    = new(0, 2);
    private static readonly Vector2I Link    = new(0, 1); // the single road tile between mine and capital

    // A one-nation economy with a hand-placed ore deposit + mine at Mine, no pops (so the only treasury
    // movement is the internal market buying delivered ore).
    private static Economy WithMine(out Economy economy)
    {
        var land = new Dictionary<Vector2I, Vector2I>();
        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
                land[new Vector2I(x, y)] = Capital;

        economy = new Economy(land, new List<Vector2I> { Capital }, 1);
        economy.TileBuildings.Clear();
        economy.PlayerNation.Population = 0f;
        economy.PlayerNation.Stockpile.Clear();
        economy.PlayerNation.Treasury = 0f;

        economy.TileDeposits[Mine] = new Deposit(Good.Ore, new HashSet<Vector2I> { Mine }, 100f, 1f);
        economy.TileBuildings[Mine] = Building.Of(BuildingType.Mine);
        return economy;
    }

    [Fact]
    public void Ore_is_mined_hauled_to_the_capital_and_sold_for_income()
    {
        WithMine(out Economy economy);
        economy.Railroads.Add(Link); // mine -> [Link] -> capital

        for (int i = 0; i < 30; i++) economy.OnTick();

        // The only way the treasury can have grown is: produce on the mine tile → hauler delivers to the
        // capital → internal market buys it. Every link in the chain had to work.
        Assert.True(economy.IncomeOf(Good.Ore) > 0f, "delivered ore should have produced market income");
        Assert.True(economy.PlayerNation.Treasury > 0f);
    }

    [Fact]
    public void Without_a_road_the_ore_strands_at_the_mine_and_earns_nothing()
    {
        WithMine(out Economy economy);
        // No road laid — the mine can't reach the capital.

        for (int i = 0; i < 30; i++) economy.OnTick();

        Assert.Equal(0f, economy.TotalIncome, 3);                              // nothing delivered ⇒ no income
        Assert.True(economy.TileInventory.AmountAt(Mine, Good.Ore) > 0f,       // it piled up where it was made
            "ore with no road out should strand on the mine tile");
    }

    [Fact]
    public void Cutting_the_road_stops_new_income()
    {
        WithMine(out Economy economy);
        economy.Railroads.Add(Link);
        for (int i = 0; i < 30; i++) economy.OnTick();
        float earnedWithRoad = economy.IncomeOf(Good.Ore);
        Assert.True(earnedWithRoad > 0f);

        // Sever the link and drain any goods already at the capital, then run on.
        economy.Railroads.Remove(Link);
        while (economy.TileInventory.AmountAt(Capital, Good.Ore) > 0f) economy.OnTick();
        float earnedAfterCut = economy.IncomeOf(Good.Ore);

        for (int i = 0; i < 30; i++) economy.OnTick();

        Assert.Equal(earnedAfterCut, economy.IncomeOf(Good.Ore), 3); // no fresh deliveries ⇒ income frozen
        Assert.True(economy.TileInventory.AmountAt(Mine, Good.Ore) > 0f); // new ore strands at the mine again
    }
}
