using Xunit;

// Locks the 16 cardinal-neighbour cases of TileResolver (the rewrite of the old DetermineTileType,
// which had a dead/duplicated branch). N=1, E=2, S=4, W=8.
public class TileResolverTests
{
    [Theory]
    [InlineData(0, "island")]
    [InlineData(TileResolver.N, "botSpecial")]
    [InlineData(TileResolver.E, "leftSpecial")]
    [InlineData(TileResolver.N | TileResolver.E, "botLeft")]
    [InlineData(TileResolver.S, "topSpecial")]
    [InlineData(TileResolver.N | TileResolver.S, "midSpecialVert")]
    [InlineData(TileResolver.E | TileResolver.S, "topLeft")]
    [InlineData(TileResolver.N | TileResolver.E | TileResolver.S, "left")]
    [InlineData(TileResolver.W, "rightSpecial")]
    [InlineData(TileResolver.N | TileResolver.W, "botRight")]
    [InlineData(TileResolver.E | TileResolver.W, "midSpecialHoriz")]
    [InlineData(TileResolver.N | TileResolver.E | TileResolver.W, "bot")]
    [InlineData(TileResolver.S | TileResolver.W, "topRight")]
    [InlineData(TileResolver.N | TileResolver.S | TileResolver.W, "right")]
    [InlineData(TileResolver.E | TileResolver.S | TileResolver.W, "top")]
    [InlineData(TileResolver.N | TileResolver.E | TileResolver.S | TileResolver.W, "surrounded")]
    public void FromCardinals_maps_every_combo(int cardinals, string expected)
    {
        Assert.Equal(expected, TileResolver.FromCardinals(cardinals));
    }

    [Fact]
    public void FromCardinals_covers_all_16_distinct_inputs()
    {
        // Every 0..15 input resolves to a non-null key (no gaps in the table).
        for (int i = 0; i < 16; i++)
            Assert.False(string.IsNullOrEmpty(TileResolver.FromCardinals(i)));
    }

    // Resolve() must extract cardinals from the full 8-bit surrounding mask
    // (topLeft=1, top=2, topRight=4, left=8, right=16, botLeft=32, bot=64, botRight=128)
    // and ignore the diagonal bits, exactly like the original code.
    [Fact]
    public void Resolve_ignores_diagonals()
    {
        // Only diagonals set -> treated as island.
        int diagonalsOnly = 1 | 4 | 32 | 128;
        Assert.Equal("island", TileResolver.Resolve(diagonalsOnly));
    }

    [Fact]
    public void Resolve_all_neighbours_is_surrounded()
    {
        Assert.Equal("surrounded", TileResolver.Resolve(0b1111_1111));
    }

    [Fact]
    public void Resolve_only_top_neighbour_is_botSpecial()
    {
        // top bit = value 2; plus a diagonal that must be ignored.
        Assert.Equal("botSpecial", TileResolver.Resolve(2 | 1));
    }
}
