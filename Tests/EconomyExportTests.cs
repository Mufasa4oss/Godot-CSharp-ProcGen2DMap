using System.Collections.Generic;
using System.Linq;
using Godot;
using Xunit;

// LR4: export. The player's delivered surplus (sitting in the capital inventory after the home market) is sold
// to ROAD-CONNECTED neighbour nations at their price, minus a distance-based transport cost. Plain .NET (no engine).
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
}
