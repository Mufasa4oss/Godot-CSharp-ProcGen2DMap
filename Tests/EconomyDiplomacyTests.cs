using System.Collections.Generic;
using System.Linq;
using Godot;
using Xunit;

// M6 phase 3: diplomacy drives trade — Hostile = blockade (no trade), Neutral = trade with an import tariff
// barrier, Friendly = free trade. Plain .NET (no engine).
public class EconomyDiplomacyTests
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

    private static Economy Connected(out Nation a, out Nation b, out Vector2I capA, out Vector2I capB, float dist = 1f)
    {
        var land = TwoNationLand(16, out capA, out capB);
        var e = new Economy(land, new List<Vector2I> { capA, capB }, 2024);
        e.EnableAiAutonomy = false;
        e.SetTradeGraph(
            new List<PrimsAlgorithmSimple.Edge> { new PrimsAlgorithmSimple.Edge(0, 1, dist) },
            new List<Vector2I> { capA, capB });
        a = e.Nations[capA];
        b = e.Nations[capB];
        return e;
    }

    [Fact]
    public void Default_stance_is_neutral()
    {
        var e = Connected(out _, out _, out var capA, out var capB);
        Assert.Equal(DiplomaticStance.Neutral, e.GetRelation(capA, capB));
        Assert.Equal(DiplomaticStance.Neutral, e.GetRelation(capB, capA)); // symmetric
    }

    [Fact]
    public void Hostile_nations_do_not_trade()
    {
        var e = Connected(out var a, out var b, out var capA, out var capB);
        a.AddGood(Good.Grain, 1000f);  // surplus, cheap
        b.Stockpile[Good.Grain] = 0f;  // shortage (pops want grain), dear
        b.Treasury = 10000f;
        e.SetRelation(capA, capB, DiplomaticStance.Hostile);

        e.OnTick();

        Assert.Empty(e.TradedThisTick);
        Assert.Equal(0f, b.Stockpile.GetValueOrDefault(Good.Grain), 3);
    }

    [Fact]
    public void Player_can_blockade_then_reopen_trade()
    {
        var e = Connected(out var a, out var b, out var capA, out var capB);
        a.AddGood(Good.Grain, 1000f);
        b.Stockpile[Good.Grain] = 0f;
        b.Treasury = 10000f;

        e.SetPlayerStance(capB, DiplomaticStance.Hostile); // player (capA) blockades capB
        e.OnTick();
        Assert.Empty(e.TradedThisTick);

        e.SetPlayerStance(capB, DiplomaticStance.Friendly); // reopen as free-trade partners
        e.OnTick();
        Assert.Contains(e.TradedThisTick, t => t.Good == Good.Grain);
    }

    [Fact]
    public void Neutral_tariff_blocks_a_marginal_trade_that_friendly_allows()
    {
        // Engineer a small ore price gap (~0.22): the seller has no ore demand (price = base 2), the buyer's
        // workshop wants ore (slightly dearer). The gap clears the friendly threshold (~transport) but not the
        // neutral one (transport + 0.25*baseOre). Population 0 isolates ore (no grain dynamics / tax).
        Economy Setup(DiplomaticStance stance)
        {
            var e = Connected(out var a, out var b, out var capA, out var capB);
            a.Population = 0f; b.Population = 0f;
            a.AddGood(Good.Ore, 1000f);   // seller: no ore demand -> price stays base
            b.Stockpile[Good.Ore] = 5.5f; // buyer: workshop demand -> a touch dearer after it consumes
            b.Treasury = 10000f;
            e.TileBuildings[b.Tiles.First(t => !e.TileBuildings.ContainsKey(t))] = Building.Of(BuildingType.Workshop);
            e.SetRelation(capA, capB, stance);
            return e;
        }

        var friendly = Setup(DiplomaticStance.Friendly);
        friendly.OnTick();
        Assert.Contains(friendly.TradedThisTick, t => t.Good == Good.Ore);

        var neutral = Setup(DiplomaticStance.Neutral);
        neutral.OnTick();
        Assert.DoesNotContain(neutral.TradedThisTick, t => t.Good == Good.Ore);
    }
}
