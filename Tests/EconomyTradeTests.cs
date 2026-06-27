using System.Collections.Generic;
using System.Linq;
using Godot;
using Xunit;

// M6 phase-1: the AI are live economic actors (pops + auto-built extractors + tax), every nation prices
// goods off its own local scarcity, and goods auto-arbitrage between connected nations. Plain .NET (no engine).
public class EconomyTradeTests
{
    // capA (0,0) = player, owns the left half; capB (size-1,0) = AI, owns the right half.
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

    // A single MST edge connecting the two nations, distance `dist`.
    private static void Connect(Economy e, Vector2I capA, Vector2I capB, float dist = 10f) =>
        e.SetTradeGraph(
            new List<PrimsAlgorithmSimple.Edge> { new PrimsAlgorithmSimple.Edge(0, 1, dist) },
            new List<Vector2I> { capA, capB });

    // ---- Rung 1: AI come alive ----

    [Fact]
    public void Every_nation_starts_with_population()
    {
        var land = TwoNationLand(16, out var capA, out var capB);
        var economy = new Economy(land, new List<Vector2I> { capA, capB }, 2024);

        Assert.True(economy.Nations[capA].Population > 0f); // player
        Assert.True(economy.Nations[capB].Population > 0f); // AI
    }

    [Fact]
    public void Ai_develops_its_territory_and_produces()
    {
        var land = TwoNationLand(16, out var capA, out var capB);
        var economy = new Economy(land, new List<Vector2I> { capA, capB }, 2024);
        Nation ai = economy.Nations[capB];
        Assert.Contains(economy.TileDeposits.Keys, t => economy.TileNation.GetValueOrDefault(t) == ai); // AI has a deposit

        for (int i = 0; i < 40; i++) economy.OnTick();

        // The AI auto-built at least one extractor on its own land and accumulated a stockpile from it.
        Assert.Contains(economy.TileBuildings.Keys, t => economy.TileNation.GetValueOrDefault(t) == ai);
        Assert.True(ai.Stockpile.Values.Sum() > 0f);
    }

    [Fact]
    public void Tax_lands_for_every_nation()
    {
        var land = TwoNationLand(16, out var capA, out var capB);
        var economy = new Economy(land, new List<Vector2I> { capA, capB }, 2024);
        Nation ai = economy.Nations[capB];
        ai.Treasury = 0f; // below extractor cost, so it can't build before the first tax lands

        for (int i = 0; i < Economy.TaxIntervalTicks; i++) economy.OnTick();

        Assert.Equal(Economy.TaxPerInterval, ai.Treasury, 3); // one poll-tax levy
    }

    [Fact]
    public void Inert_ai_does_nothing_when_autonomy_is_off()
    {
        var land = TwoNationLand(16, out var capA, out var capB);
        var economy = new Economy(land, new List<Vector2I> { capA, capB }, 2024);
        economy.EnableAiAutonomy = false;

        for (int i = 0; i < 30; i++) economy.OnTick();

        Nation ai = economy.Nations[capB];
        Assert.DoesNotContain(economy.TileBuildings.Keys, t => economy.TileNation.GetValueOrDefault(t) == ai);
        Assert.Equal(0f, ai.Stockpile.Values.Sum(), 3);
    }

    // ---- Rung 2: per-nation prices ----

    [Fact]
    public void Nations_price_the_same_good_by_their_own_scarcity()
    {
        var land = TwoNationLand(16, out var capA, out var capB);
        var economy = new Economy(land, new List<Vector2I> { capA, capB }, 2024);
        economy.EnableAiAutonomy = false; // hold stockpiles still
        Nation glut = economy.Nations[capA], starved = economy.Nations[capB];

        glut.AddGood(Good.Grain, 1000f); // plenty → cheap
        starved.Stockpile[Good.Grain] = 0f; // none, but pops want it → dear

        economy.OnTick();

        Assert.True(economy.PriceOf(glut, Good.Grain) < economy.PriceOf(starved, Good.Grain));
    }

    // ---- Rung 3: inter-nation trade ----

    [Fact]
    public void Grain_flows_from_surplus_to_shortage_and_gold_flows_back()
    {
        var land = TwoNationLand(16, out var capA, out var capB);
        var economy = new Economy(land, new List<Vector2I> { capA, capB }, 2024);
        economy.EnableAiAutonomy = false;
        Connect(economy, capA, capB);

        Nation seller = economy.Nations[capA], buyer = economy.Nations[capB];
        seller.AddGood(Good.Grain, 1000f); // surplus → cheap
        buyer.Stockpile[Good.Grain] = 0f;  // shortage → dear
        buyer.Treasury = 1000f;
        float sellerGoldBefore = seller.Treasury, buyerGoldBefore = buyer.Treasury;

        economy.OnTick();

        Assert.True(buyer.Stockpile.GetValueOrDefault(Good.Grain) > 0f); // imported grain
        Assert.True(seller.Treasury > sellerGoldBefore);                 // earned gold
        Assert.True(buyer.Treasury < buyerGoldBefore);                   // paid gold
        Assert.Contains(economy.TradedThisTick, t => t.Good == Good.Grain && t.From == capA && t.To == capB);
    }

    [Fact]
    public void Disconnected_nations_do_not_trade()
    {
        var land = TwoNationLand(16, out var capA, out var capB);
        var economy = new Economy(land, new List<Vector2I> { capA, capB }, 2024);
        economy.EnableAiAutonomy = false;
        // No SetTradeGraph → no routes.

        Nation a = economy.Nations[capA], b = economy.Nations[capB];
        a.AddGood(Good.Grain, 1000f);
        b.Stockpile[Good.Grain] = 0f;
        b.Treasury = 1000f;

        economy.OnTick();

        Assert.Equal(0f, b.Stockpile.GetValueOrDefault(Good.Grain), 3); // nothing imported
        Assert.Empty(economy.TradedThisTick);
    }

    [Fact]
    public void A_sole_supplier_earns_from_a_connected_consumer()
    {
        var land = TwoNationLand(16, out var capA, out var capB);
        var economy = new Economy(land, new List<Vector2I> { capA, capB }, 2024);
        economy.EnableAiAutonomy = false;
        Connect(economy, capA, capB);

        Nation supplier = economy.Nations[capA], consumer = economy.Nations[capB];
        supplier.AddGood(Good.Ore, 1000f);                              // the only ore around (no ore demand → base price)
        economy.TileBuildings[ConsumerTile(economy, consumer)] = Building.Of(BuildingType.Workshop); // consumer needs ore
        consumer.Treasury = 1000f;
        float supplierGoldBefore = supplier.Treasury;

        for (int i = 0; i < 5; i++) economy.OnTick(); // < tax interval, so the gain is pure trade income

        Assert.True(supplier.Treasury > supplierGoldBefore);              // monopolist drains the consumer's treasury
        Assert.True(supplier.Stockpile.GetValueOrDefault(Good.Ore) < 1000f); // shipped ore out
    }

    // A land tile the consumer owns with no building yet (for placing its workshop).
    private static Vector2I ConsumerTile(Economy e, Nation consumer) =>
        consumer.Tiles.First(t => !e.TileBuildings.ContainsKey(t));
}
