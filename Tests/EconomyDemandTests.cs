using System.Collections.Generic;
using Godot;
using Xunit;

// M5 demand + scarcity: the player's pops eat grain each tick (a minimal demand sink), and prices respond
// to scarcity — a good in short supply gets pricier, a glut cheaper, a good nothing demands stays at base.
// Plain .NET (no engine).
public class EconomyDemandTests
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

    private static readonly Vector2I Capital = new(0, 0);

    // A single-player economy with no buildings, so the only thing touching the grain is the demand sink.
    // Player grain is physical (TileInventory at the capital), so empty the starting larder here and let each
    // test set the supply explicitly.
    private static Economy DemandOnlyEconomy(int seed = 99)
    {
        var land = SquareLand(12, out var capital);
        var economy = new Economy(land, new List<Vector2I> { capital }, seed);
        economy.TileBuildings.Clear();
        economy.TileInventory.Add(capital, Good.Grain, -economy.TileInventory.AmountAt(capital, Good.Grain));
        return economy;
    }

    // ---- Demand sink: pops eat grain ----

    [Fact]
    public void Player_has_a_population_with_grain_demand()
    {
        var economy = DemandOnlyEconomy();
        Assert.True(economy.PlayerNation.Population > 0f);

        economy.OnTick();
        Assert.True(economy.PlayerDemandLastTick.GetValueOrDefault(Good.Grain) > 0f);
    }

    [Fact]
    public void Pops_eat_their_full_grain_demand_when_supply_is_ample()
    {
        var economy = DemandOnlyEconomy();
        economy.TileInventory.Add(Capital, Good.Grain, 100f); // ample grain delivered to the capital

        economy.OnTick();

        float demand   = economy.PlayerDemandLastTick.GetValueOrDefault(Good.Grain);
        float consumed = economy.PlayerConsumedLastTick.GetValueOrDefault(Good.Grain);
        Assert.Equal(demand, consumed, 3);                                                  // ate the full appetite
        Assert.Equal(100f - consumed, economy.TileInventory.AmountAt(Capital, Good.Grain), 3);
    }

    [Fact]
    public void Grain_stockpile_floors_at_zero_when_demand_outstrips_supply()
    {
        var economy = DemandOnlyEconomy();
        economy.TileInventory.Add(Capital, Good.Grain, 0.05f); // less than one tick of demand

        economy.OnTick();

        Assert.Equal(0f, economy.TileInventory.AmountAt(Capital, Good.Grain), 3);          // no negative grain
        Assert.Equal(0.05f, economy.PlayerConsumedLastTick.GetValueOrDefault(Good.Grain), 3); // ate what was there
    }

    // ---- Scarcity pricing ----

    [Fact]
    public void A_grain_shortage_drives_grain_price_above_base()
    {
        var economy = DemandOnlyEconomy();
        float basePrice = economy.PriceOf(Good.Grain); // before any tick: multiplier 1 = base
        // Empty stockpile + standing pop demand = a shortage.

        economy.OnTick();

        Assert.True(economy.PriceOf(Good.Grain) > basePrice,
            "an empty grain stockpile against pop demand should push grain's price above base");
    }

    [Fact]
    public void A_grain_glut_drives_grain_price_below_base()
    {
        var economy = DemandOnlyEconomy();
        float basePrice = economy.PriceOf(Good.Grain);
        economy.TileInventory.Add(Capital, Good.Grain, 1000f); // far more than the buffer demand wants

        economy.OnTick();

        Assert.True(economy.PriceOf(Good.Grain) < basePrice,
            "a large grain surplus should pull grain's price below base");
    }

    [Fact]
    public void A_good_nothing_demands_stays_at_base_price()
    {
        var economy = DemandOnlyEconomy();
        float baseTools = economy.PriceOf(Good.Tools); // no consumer for Tools (no workshop here)

        for (int i = 0; i < 5; i++) economy.OnTick();

        Assert.Equal(baseTools, economy.PriceOf(Good.Tools), 3); // unmoved: zero demand ⇒ multiplier 1
    }

    // ---- Pop tax ----

    [Fact]
    public void Pops_pay_a_poll_tax_into_the_treasury_every_interval()
    {
        var economy = DemandOnlyEconomy();
        float start = economy.PlayerNation.Treasury;

        for (int i = 0; i < Economy.TaxIntervalTicks - 1; i++) economy.OnTick();
        Assert.Equal(start, economy.PlayerNation.Treasury, 3);            // nothing yet before the interval
        Assert.Equal(0f, economy.TaxCollected, 3);

        economy.OnTick();                                                // hits the interval → one levy
        Assert.Equal(start + Economy.TaxPerInterval, economy.PlayerNation.Treasury, 3);

        for (int i = 0; i < Economy.TaxIntervalTicks; i++) economy.OnTick(); // a second levy
        Assert.Equal(start + 2 * Economy.TaxPerInterval, economy.PlayerNation.Treasury, 3);
        Assert.Equal(2 * Economy.TaxPerInterval, economy.TaxCollected, 3);
    }

    [Fact]
    public void No_pops_means_no_tax()
    {
        var economy = DemandOnlyEconomy();
        economy.PlayerNation.Population = 0f;
        float start = economy.PlayerNation.Treasury;

        for (int i = 0; i < 3 * Economy.TaxIntervalTicks; i++) economy.OnTick();

        Assert.Equal(start, economy.PlayerNation.Treasury, 3);
        Assert.Equal(0f, economy.TaxCollected, 3);
    }

    [Fact]
    public void Building_more_grain_supply_relieves_the_shortage_price()
    {
        var economy = DemandOnlyEconomy();
        economy.OnTick();
        float shortagePrice = economy.PriceOf(Good.Grain); // empty stock ⇒ elevated

        // Stock a healthy grain buffer (as if farms had been feeding it) and let the price re-settle.
        economy.TileInventory.Add(Capital, Good.Grain, 50f);
        economy.OnTick();

        Assert.True(economy.PriceOf(Good.Grain) < shortagePrice,
            "raising the grain buffer should bring the scarcity price back down (the re-allocation payoff)");
    }
}
