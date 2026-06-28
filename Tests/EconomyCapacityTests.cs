using System.Collections.Generic;
using System.Linq;
using Godot;
using Xunit;

// M6 phase 2: trade routes carry at most a per-tick capacity (backbone base + player connector rails along the
// route's corridor) and can be cut. Plain .NET (no engine).
public class EconomyCapacityTests
{
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

    // Two nations on one short route. Seller gluts grain + ore (both cheap); buyer wants both (pops eat grain,
    // a workshop wants ore) and can pay — so two goods want to cross and capacity decides how many do.
    private static Economy TwoTradingNations(out Nation seller, out Nation buyer, out Vector2I capA, out Vector2I capB,
        HashSet<Vector2I> corridor = null)
    {
        var land = TwoNationLand(16, out capA, out capB);
        var economy = new Economy(land, new List<Vector2I> { capA, capB }, 2024);
        economy.EnableAiAutonomy = false; // hold the AI still so we control the books
        economy.SetTradeGraph(
            new List<PrimsAlgorithmSimple.Edge> { new PrimsAlgorithmSimple.Edge(0, 1, 5f) },
            new List<Vector2I> { capA, capB },
            new List<HashSet<Vector2I>> { corridor ?? new HashSet<Vector2I>() });

        seller = economy.Nations[capA];
        buyer  = economy.Nations[capB];
        seller.AddGood(Good.Grain, 1000f);
        seller.AddGood(Good.Ore, 1000f);
        buyer.Stockpile[Good.Grain] = 0f;
        buyer.Stockpile[Good.Ore] = 0f;
        buyer.Treasury = 100000f;
        buyer.AddGood(Good.Ore, 0f);
        economy.TileBuildings[buyer.Tiles.First(t => !economy.TileBuildings.ContainsKey(t))] = Building.Of(BuildingType.Workshop);
        return economy;
    }

    [Fact]
    public void Base_capacity_caps_units_per_route_per_tick()
    {
        var economy = TwoTradingNations(out _, out _, out var capA, out var capB);
        economy.OnTick();
        TradeRoute route = economy.FindRoute(capA, capB);
        // Two goods could flow, but the backbone base capacity (1) lets only one unit cross this tick.
        Assert.Equal(1f, route.FlowLastTick, 3);
        Assert.Single(economy.TradedThisTick);
    }

    [Fact]
    public void Connector_rails_raise_capacity_so_more_flows()
    {
        var corridor = new HashSet<Vector2I> { new Vector2I(7, 0), new Vector2I(8, 0) };
        var economy = TwoTradingNations(out _, out _, out var capA, out var capB, corridor);
        // Lay two rails along the route's corridor → +1.0 capacity → 2 units/tick (both goods cross).
        foreach (var t in corridor) economy.Railroads.Add(t);

        economy.OnTick();
        TradeRoute route = economy.FindRoute(capA, capB);
        Assert.Equal(2f, route.FlowLastTick, 3);
        Assert.Equal(2, economy.TradedThisTick.Count);
    }

    [Fact]
    public void A_cut_route_carries_nothing()
    {
        var economy = TwoTradingNations(out _, out var buyer, out var capA, out var capB);
        Assert.True(economy.SetRouteBlocked(capA, capB, true));

        economy.OnTick();

        TradeRoute route = economy.FindRoute(capA, capB);
        Assert.Equal(0f, route.FlowLastTick, 3);
        Assert.Empty(economy.TradedThisTick);
        Assert.Equal(0f, buyer.Stockpile.GetValueOrDefault(Good.Grain), 3); // nothing imported
    }

    [Fact]
    public void Cutting_a_route_keeps_prices_diverged()
    {
        // Cut: the buyer's grain stays starved + dear, so the price gap stays wide.
        var cut = TwoTradingNations(out var sellerC, out var buyerC, out var capA, out var capB);
        cut.SetRouteBlocked(capA, capB, true);
        for (int i = 0; i < 10; i++) cut.OnTick();
        float gapCut = cut.PriceOf(buyerC, Good.Grain) - cut.PriceOf(sellerC, Good.Grain);

        // Open: grain flows each tick, feeding the buyer and narrowing the gap.
        var open = TwoTradingNations(out var sellerO, out var buyerO, out _, out _);
        for (int i = 0; i < 10; i++) open.OnTick();
        float gapOpen = open.PriceOf(buyerO, Good.Grain) - open.PriceOf(sellerO, Good.Grain);

        Assert.True(gapCut > gapOpen);
    }
}
