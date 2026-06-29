using Godot;
using Xunit;

// LR2: goods at rest on tiles. Pure Core, runs headless like the rest of the Logistics suite.
public class TileInventoryTests
{
    [Fact]
    public void Adding_then_reading_returns_the_amount()
    {
        var inv = new TileInventory();
        inv.Add(new Vector2I(3, 4), Good.Ore, 2f);

        Assert.Equal(2f, inv.AmountAt(new Vector2I(3, 4), Good.Ore), 3);
        Assert.Equal(0f, inv.AmountAt(new Vector2I(3, 4), Good.Grain), 3); // other goods untouched
    }

    [Fact]
    public void Removing_floors_at_zero_and_prunes_the_tile()
    {
        var tile = new Vector2I(1, 1);
        var inv = new TileInventory();
        inv.Add(tile, Good.Ore, 1f);
        inv.Add(tile, Good.Ore, -5f); // over-draw

        Assert.Equal(0f, inv.AmountAt(tile, Good.Ore), 3);
        Assert.DoesNotContain(tile, inv.Tiles); // emptied tile drops out of the scan
    }

    [Fact]
    public void Only_tiles_holding_goods_are_listed()
    {
        var inv = new TileInventory();
        inv.Add(new Vector2I(0, 0), Good.Grain, 3f);
        inv.Add(new Vector2I(9, 9), Good.Tools, 1f);

        Assert.Equal(2, inv.Tiles.Count);
        Assert.Equal(4f, inv.TotalAt(new Vector2I(0, 0)) + inv.TotalAt(new Vector2I(9, 9)), 3);
    }
}
