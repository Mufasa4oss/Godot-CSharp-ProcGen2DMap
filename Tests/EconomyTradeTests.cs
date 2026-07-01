using System.Collections.Generic;
using System.Linq;
using Godot;
using Xunit;

// M6 phase-1: the AI are live economic actors (pops + auto-built extractors + tax), every nation prices
// goods off its own local scarcity. Inter-nation abstract arbitrage (RunTrade) was retired in LR-cleanup;
// export is now physical (RunExport, covered by EconomyExportTests). Plain .NET (no engine).
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

        // LR6: the AI now has to save up for a Granary/Warehouse (75g) before an extractor can produce at
        // all — on top of the extractor itself (50g) — so bootstrapping from the 50g starting treasury takes
        // a few tax intervals (50g/10 ticks) longer than the old build-and-go. 150 ticks gives ample margin.
        for (int i = 0; i < 150; i++) economy.OnTick();

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
        economy.EnableAiAutonomy = false;
        Nation glut = economy.Nations[capA], starved = economy.Nations[capB];

        // Player (glut): put lots of grain at capital — physical stock that drives player pricing.
        economy.TileInventory.Add(glut.Capital, Good.Grain, 1000f);
        // AI (starved): clear the starting grain so its price is maximum.
        starved.Stockpile[Good.Grain] = 0f;

        economy.OnTick();

        Assert.True(economy.PriceOf(glut, Good.Grain) < economy.PriceOf(starved, Good.Grain));
    }
}
