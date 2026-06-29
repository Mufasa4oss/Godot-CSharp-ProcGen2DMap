using System.Collections.Generic;
using System.Linq;
using Godot;
using Xunit;

// LR4: export. The player's delivered surplus (sitting in the capital inventory after the home market) is sold
// to ROAD-CONNECTED neighbour nations at their price, gated by diplomacy — Friendly free-trades, Neutral
// tariffs, Hostile blockades. Plain .NET (no engine).
public class EconomyExportTests
{
    // capA (0,0) = player (left half), capB (size-1,0) = AI (right half).
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

    // A quiet two-nation economy: AI inert, player has no pops (so the home grain market is silent and the ore
    // we deliver mostly survives to export).
    private static Economy Quiet(int size, out Vector2I capA, out Vector2I capB)
    {
        var land = TwoNationLand(size, out capA, out capB);
        var e = new Economy(land, new List<Vector2I> { capA, capB }, 2024);
        e.EnableAiAutonomy = false;
        e.PlayerNation.Population = 0f;
        return e;
    }

    // Lay a contiguous road from beside capA to beside capB along row 0.
    private static void Road(Economy e, int size)
    {
        for (int x = 1; x <= size - 2; x++) e.Railroads.Add(new Vector2I(x, 0));
    }

    private static float ExportPrice(Economy e, Vector2I from, Vector2I to, Good good)
    {
        foreach (var t in e.TradedThisTick)
            if (t.From == from && t.To == to && t.Good == good) return t.Price;
        return -1f;
    }

    [Fact]
    public void Surplus_exports_to_a_road_connected_neighbour_for_gold()
    {
        var e = Quiet(16, out var capA, out var capB);
        Road(e, 16);
        e.TileInventory.Add(capA, Good.Ore, 8f); // delivered surplus at the capital
        Nation buyer = e.Nations[capB];
        buyer.Treasury = 1000f;
        float sellerBefore = e.PlayerNation.Treasury;

        e.OnTick();

        Assert.True(e.ExportSoldLastTick.GetValueOrDefault(Good.Ore) > 0f);      // shipped abroad
        Assert.True(buyer.Stockpile.GetValueOrDefault(Good.Ore) > 0f);          // the neighbour received it
        Assert.True(e.PlayerNation.Treasury > sellerBefore);                    // earned gold
        Assert.Contains(e.TradedThisTick, t => t.From == capA && t.To == capB && t.Good == Good.Ore);
    }

    [Fact]
    public void No_road_means_no_export()
    {
        var e = Quiet(16, out var capA, out var capB);
        // No road laid.
        e.TileInventory.Add(capA, Good.Ore, 8f);
        e.Nations[capB].Treasury = 1000f;

        e.OnTick();

        Assert.Equal(0f, e.ExportSoldLastTick.GetValueOrDefault(Good.Ore), 3);
        Assert.Equal(0f, e.Nations[capB].Stockpile.GetValueOrDefault(Good.Ore), 3);
    }

    [Fact]
    public void A_hostile_neighbour_is_blockaded()
    {
        var e = Quiet(16, out var capA, out var capB);
        Road(e, 16);
        e.TileInventory.Add(capA, Good.Ore, 8f);
        e.Nations[capB].Treasury = 1000f;
        e.SetPlayerStance(capB, DiplomaticStance.Hostile); // blockade

        e.OnTick();

        Assert.Equal(0f, e.ExportSoldLastTick.GetValueOrDefault(Good.Ore), 3); // nothing crosses a blockade
    }

    [Fact]
    public void Friendly_terms_net_more_than_tariffed_neutral_terms()
    {
        var friendly = Quiet(16, out var fa, out var fb);
        Road(friendly, 16);
        friendly.TileInventory.Add(fa, Good.Ore, 8f);
        friendly.Nations[fb].Treasury = 1000f;
        friendly.SetPlayerStance(fb, DiplomaticStance.Friendly);
        friendly.OnTick();

        var neutral = Quiet(16, out var na, out var nb);
        Road(neutral, 16);
        neutral.TileInventory.Add(na, Good.Ore, 8f);
        neutral.Nations[nb].Treasury = 1000f; // stance defaults to Neutral
        neutral.OnTick();

        float friendlyNet = ExportPrice(friendly, fa, fb, Good.Ore);
        float neutralNet  = ExportPrice(neutral, na, nb, Good.Ore);
        Assert.True(friendlyNet > 0f && neutralNet > 0f);
        Assert.True(friendlyNet > neutralNet, // the tariff docks the Neutral export
            $"friendly net {friendlyNet} should beat tariffed neutral net {neutralNet}");
    }
}
