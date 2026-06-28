using System.Collections.Generic;
using Godot;
using Xunit;

// LR1: the road network connectivity + distance gate. Pure Core, so these run headless like the rest.
public class RoadGraphTests
{
    [Fact]
    public void Two_tiles_linked_by_a_contiguous_road_are_connected()
    {
        // A(0,0) --[1,0][2,0][3,0]-- B(4,0): each road tile abuts the next and a capital at each end.
        var roads = new HashSet<Vector2I> { new(1, 0), new(2, 0), new(3, 0) };
        var graph = new RoadGraph(roads);

        Assert.True(graph.AreConnected(new Vector2I(0, 0), new Vector2I(4, 0)));
    }

    [Fact]
    public void Tiles_with_no_road_between_them_are_not_connected()
    {
        var graph = new RoadGraph(new HashSet<Vector2I>());

        Assert.False(graph.AreConnected(new Vector2I(0, 0), new Vector2I(4, 0)));
    }

    [Fact]
    public void Removing_a_middle_tile_severs_the_connection()
    {
        // The gap at (2,0) splits the run into two stubs that no longer reach across.
        var roads = new HashSet<Vector2I> { new(1, 0), new(3, 0) };
        var graph = new RoadGraph(roads);

        Assert.False(graph.AreConnected(new Vector2I(0, 0), new Vector2I(4, 0)));
    }

    [Fact]
    public void Diagonally_adjacent_road_tiles_do_not_connect()
    {
        // (1,1) only touches (0,0) and (2,2) at a corner — not a 4-neighbour link, so no path.
        var roads = new HashSet<Vector2I> { new(1, 1) };
        var graph = new RoadGraph(roads);

        Assert.False(graph.AreConnected(new Vector2I(0, 0), new Vector2I(2, 2)));
    }

    [Fact]
    public void Shortest_path_length_counts_both_capital_steps()
    {
        // step onto road (1) + two hops across (2,3) + step off (4) = 4.
        var roads = new HashSet<Vector2I> { new(1, 0), new(2, 0), new(3, 0) };
        var graph = new RoadGraph(roads);

        Assert.Equal(4, graph.ShortestPathLength(new Vector2I(0, 0), new Vector2I(4, 0)));
    }

    [Fact]
    public void Shortest_path_ignores_a_longer_detour()
    {
        // Direct row plus a longer loop off it; the direct length (4) must win.
        var roads = new HashSet<Vector2I>
        {
            new(1, 0), new(2, 0), new(3, 0),                          // direct
            new(1, 1), new(1, 2), new(2, 2), new(3, 2), new(3, 1),    // detour
        };
        var graph = new RoadGraph(roads);

        Assert.Equal(4, graph.ShortestPathLength(new Vector2I(0, 0), new Vector2I(4, 0)));
    }

    [Fact]
    public void Unreachable_tiles_report_negative_length()
    {
        var graph = new RoadGraph(new HashSet<Vector2I> { new(1, 0) });

        Assert.Equal(-1, graph.ShortestPathLength(new Vector2I(0, 0), new Vector2I(40, 40)));
    }
}
