using System.Collections.Generic;
using Godot;
using Xunit;

// M4 value/prices + LR3 income. Each good has a sale value, and income now comes from the INTERNAL MARKET:
// your pops buy the goods your haulers physically delivered to the capital (no manual Sell button). The
// headline alive check — refined Tools out-earn the raw Ore they came from — still holds, now via the market.
// Plain .NET (no engine).
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

    // A quiet single-player economy: no buildings, no pops (so no production/tax noise), so the only thing
    // touching the treasury is the internal market buying what we hand-deliver to the capital.
    private static Economy QuietEconomy(out Vector2I capital, int seed = 99)
    {
        var land = SquareLand(12, out capital);
        var economy = new Economy(land, new List<Vector2I> { capital }, seed);
        economy.TileBuildings.Clear();
        economy.PlayerNation.Stockpile.Clear();
        economy.PlayerNation.Population = 0f;
        return economy;
    }

    [Fact]
    public void Tools_is_priced_above_the_ore_it_consumes()
    {
        var economy = QuietEconomy(out _);
        Assert.True(economy.PriceOf(Good.Tools) > economy.PriceOf(Good.Ore),
            "a Workshop must earn more selling Tools than the Ore it consumed, or refining is pointless");
    }

    // Goods physically delivered to the capital are bought by the internal market for treasury income, capped
    // at the home market's per-tick appetite — the rest waits (for export, LR4).
    [Fact]
    public void Delivered_goods_are_bought_by_the_internal_market_for_income()
    {
        var economy = QuietEconomy(out Vector2I capital);
        economy.TileInventory.Add(capital, Good.Ore, 5f); // as if haulers delivered 5 ore
        float before = economy.PlayerNation.Treasury;
        float price = economy.PriceOf(Good.Ore);

        economy.OnTick();

        Assert.Equal(1f, economy.InternalSoldLastTick.GetValueOrDefault(Good.Ore), 3); // demand-capped to 1/tick
        Assert.Equal(before + price, economy.PlayerNation.Treasury, 3);                 // paid into the treasury
        Assert.Equal(price, economy.IncomeOf(Good.Ore), 3);
        Assert.Equal(4f, economy.TileInventory.AmountAt(capital, Good.Ore), 3);         // the rest waits at the capital
    }

    // No delivery ⇒ no market income (the "undelivered good is unsold" half of the alive check).
    [Fact]
    public void Undelivered_goods_earn_no_market_income()
    {
        var economy = QuietEconomy(out _);
        // Nothing handed to the capital, and a raw pile sitting out on some tile isn't the capital market.
        economy.TileInventory.Add(new Vector2I(5, 5), Good.Ore, 50f);

        for (int i = 0; i < 5; i++) economy.OnTick();

        Assert.Equal(0f, economy.TotalIncome, 3);
    }

    // The income breakdown the HUD shows: each good's earnings are tracked separately and the total is the sum.
    [Fact]
    public void Income_is_tracked_per_good()
    {
        var economy = QuietEconomy(out Vector2I capital);
        economy.TileInventory.Add(capital, Good.Ore, 10f);
        economy.TileInventory.Add(capital, Good.Tools, 10f);
        float orePrice = economy.PriceOf(Good.Ore), toolsPrice = economy.PriceOf(Good.Tools);

        economy.OnTick(); // market buys one of each (demand cap 1/tick)

        Assert.Equal(orePrice, economy.IncomeOf(Good.Ore), 3);
        Assert.Equal(toolsPrice, economy.IncomeOf(Good.Tools), 3);
        Assert.Equal(0f, economy.IncomeOf(Good.Grain), 3);                  // never delivered any
        Assert.Equal(orePrice + toolsPrice, economy.TotalIncome, 3);        // total is the sum of the parts
        Assert.True(economy.IncomeOf(Good.Tools) > economy.IncomeOf(Good.Ore)); // the legible comparison
    }

    // The alive check: from the same ore, refining it to Tools and delivering those earns more through the
    // market than delivering the raw ore — because Tools price higher. (Refining is worth it.)
    [Fact]
    public void Delivering_refined_tools_out_earns_delivering_raw_ore()
    {
        const float units = 6f;

        var raw = QuietEconomy(out Vector2I rawCap);
        raw.TileInventory.Add(rawCap, Good.Ore, units);
        float rawBefore = raw.PlayerNation.Treasury;
        for (int i = 0; i < (int)units; i++) raw.OnTick(); // market soaks up 1/tick

        var refined = QuietEconomy(out Vector2I refCap);
        refined.TileInventory.Add(refCap, Good.Tools, units);
        float refBefore = refined.PlayerNation.Treasury;
        for (int i = 0; i < (int)units; i++) refined.OnTick();

        float rawGain = raw.PlayerNation.Treasury - rawBefore;
        float refGain = refined.PlayerNation.Treasury - refBefore;
        Assert.True(refGain > rawGain, $"refined Tools should out-earn raw Ore (tools {refGain} vs ore {rawGain})");
    }
}
