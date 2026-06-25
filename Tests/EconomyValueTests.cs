using System.Collections.Generic;
using System.Linq;
using Godot;
using Xunit;

// M4 value/prices: each good has a sale value, and selling moves the player's stockpile into the
// treasury at that price. The headline alive check — refining Ore→Tools and selling Tools out-earns
// selling raw Ore — is what makes the M3 build choice worthwhile. Plain .NET (no engine).
public class EconomyValueTests
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

    private static Economy SinglePlayerEconomy(out Vector2I capital, int seed = 99)
    {
        var land = SquareLand(12, out capital);
        return new Economy(land, new List<Vector2I> { capital }, seed);
    }

    [Fact]
    public void Tools_is_priced_above_the_ore_it_consumes()
    {
        var economy = SinglePlayerEconomy(out _);
        Assert.True(economy.PriceOf(Good.Tools) > economy.PriceOf(Good.Ore),
            "a Workshop must earn more selling Tools than the Ore it consumed, or refining is pointless");
    }

    [Fact]
    public void SellAll_moves_stockpile_to_treasury_at_price()
    {
        var economy = SinglePlayerEconomy(out _);
        Nation p = economy.PlayerNation;
        economy.TileBuildings.Clear(); // isolate from extractors so the stockpile is exactly what we set

        p.Stockpile.Clear();
        p.AddGood(Good.Ore, 10f);
        float before = p.Treasury;

        float revenue = economy.SellAll(Good.Ore);

        Assert.Equal(10f * economy.PriceOf(Good.Ore), revenue, 3);
        Assert.Equal(before + revenue, p.Treasury, 3);
        Assert.Equal(0f, p.Stockpile.GetValueOrDefault(Good.Ore), 3); // sold out
        Assert.Equal(revenue, economy.TotalIncome, 3);
        Assert.Equal(revenue, economy.IncomeOf(Good.Ore), 3); // attributed to the good sold
    }

    // The income breakdown the HUD shows: each good's earnings are tracked separately, and the total is
    // their sum — so the player can see Tools out-earning Ore rather than just a lump figure.
    [Fact]
    public void Income_is_tracked_per_good()
    {
        var economy = SinglePlayerEconomy(out _);
        Nation p = economy.PlayerNation;
        economy.TileBuildings.Clear();
        p.Stockpile.Clear();

        p.AddGood(Good.Ore, 10f);
        p.AddGood(Good.Tools, 10f);
        economy.SellAll(Good.Ore);
        economy.SellAll(Good.Tools);

        float oreIncome   = 10f * economy.PriceOf(Good.Ore);
        float toolsIncome = 10f * economy.PriceOf(Good.Tools);

        Assert.Equal(oreIncome, economy.IncomeOf(Good.Ore), 3);
        Assert.Equal(toolsIncome, economy.IncomeOf(Good.Tools), 3);
        Assert.Equal(0f, economy.IncomeOf(Good.Grain), 3);            // never sold any
        Assert.Equal(oreIncome + toolsIncome, economy.TotalIncome, 3); // total is the sum of the parts
        Assert.True(economy.IncomeOf(Good.Tools) > economy.IncomeOf(Good.Ore)); // the legible comparison
    }

    [Fact]
    public void SellAll_is_a_noop_when_there_is_nothing_to_sell()
    {
        var economy = SinglePlayerEconomy(out _);
        Nation p = economy.PlayerNation;
        p.Stockpile.Clear();
        float before = p.Treasury;

        Assert.Equal(0f, economy.SellAll(Good.Tools), 3);
        Assert.Equal(before, p.Treasury, 3);
        Assert.Equal(0f, economy.TotalIncome, 3);
    }

    // The alive check: starting from the same Ore, refining it to Tools and selling the Tools grows the
    // treasury faster than selling the raw Ore.
    [Fact]
    public void Selling_refined_tools_out_earns_selling_raw_ore()
    {
        const float ore = 20f;

        // Path A — sell the ore raw.
        var rawEconomy = SinglePlayerEconomy(out _);
        rawEconomy.TileBuildings.Clear();
        rawEconomy.PlayerNation.Stockpile.Clear();
        rawEconomy.PlayerNation.AddGood(Good.Ore, ore);
        float rawTreasuryBefore = rawEconomy.PlayerNation.Treasury;
        rawEconomy.SellAll(Good.Ore);
        float rawGain = rawEconomy.PlayerNation.Treasury - rawTreasuryBefore;

        // Path B — refine the ore to tools through a workshop, then sell the tools.
        var refineEconomy = SinglePlayerEconomy(out _);
        refineEconomy.TileBuildings.Clear();
        refineEconomy.TileBuildings[new Vector2I(5, 5)] = Building.Of(BuildingType.Workshop);
        refineEconomy.PlayerNation.Stockpile.Clear();
        refineEconomy.PlayerNation.AddGood(Good.Ore, ore);
        float refineTreasuryBefore = refineEconomy.PlayerNation.Treasury;
        for (int i = 0; i < (int)ore; i++) refineEconomy.OnTick(); // Workshop Rate = 1 → 1 Ore→Tools/tick
        refineEconomy.SellAll(Good.Tools);
        refineEconomy.SellAll(Good.Ore); // sell any unrefined remainder too, for a fair comparison
        float refineGain = refineEconomy.PlayerNation.Treasury - refineTreasuryBefore;

        Assert.True(refineGain > rawGain,
            $"refining should out-earn selling raw ore (refined {refineGain} vs raw {rawGain})");
    }
}
