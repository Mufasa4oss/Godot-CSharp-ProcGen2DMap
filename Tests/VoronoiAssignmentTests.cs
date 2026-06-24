using System.Collections.Generic;
using Godot;
using Xunit;

public class VoronoiAssignmentTests
{
    [Fact]
    public void Each_tile_maps_to_its_nearest_capital()
    {
        var capitals = new List<Vector2I> { new Vector2I(0, 0), new Vector2I(10, 0) };
        var tiles = new List<Vector2I>
        {
            new Vector2I(1, 0),  // nearer (0,0)
            new Vector2I(4, 0),  // nearer (0,0)
            new Vector2I(6, 0),  // nearer (10,0)
            new Vector2I(9, 0),  // nearer (10,0)
        };

        var result = VoronoiAssignment.AssignNearest(tiles, capitals);

        Assert.Equal(new Vector2I(0, 0), result[new Vector2I(1, 0)]);
        Assert.Equal(new Vector2I(0, 0), result[new Vector2I(4, 0)]);
        Assert.Equal(new Vector2I(10, 0), result[new Vector2I(6, 0)]);
        Assert.Equal(new Vector2I(10, 0), result[new Vector2I(9, 0)]);
    }

    [Fact]
    public void Equidistant_tile_breaks_tie_to_first_capital()
    {
        var capitals = new List<Vector2I> { new Vector2I(0, 0), new Vector2I(10, 0) };
        var tiles = new List<Vector2I> { new Vector2I(5, 0) }; // exactly between

        var result = VoronoiAssignment.AssignNearest(tiles, capitals);

        Assert.Equal(new Vector2I(0, 0), result[new Vector2I(5, 0)]);
    }

    [Fact]
    public void Every_tile_is_assigned()
    {
        var capitals = new List<Vector2I> { new Vector2I(0, 0), new Vector2I(5, 5) };
        var tiles = new List<Vector2I>();
        for (int x = 0; x < 6; x++)
            for (int y = 0; y < 6; y++)
                tiles.Add(new Vector2I(x, y));

        var result = VoronoiAssignment.AssignNearest(tiles, capitals);

        Assert.Equal(tiles.Count, result.Count);
        foreach (var t in tiles)
            Assert.Contains(result[t], capitals);
    }

    [Fact]
    public void No_capitals_yields_empty()
    {
        var result = VoronoiAssignment.AssignNearest(
            new List<Vector2I> { new Vector2I(1, 1) }, new List<Vector2I>());
        Assert.Empty(result);
    }
}
