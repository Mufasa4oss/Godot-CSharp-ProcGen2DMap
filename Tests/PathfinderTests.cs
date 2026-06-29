using System.Collections.Generic;
using Godot;
using Xunit;

// LR2: the full hauler walk (source tile → road tiles → destination tile) built on top of RoadGraph.
public class PathfinderTests
{
    private static Pathfinder Over(HashSet<Vector2I> roads) => new Pathfinder(new RoadGraph(roads));

    [Fact]
    public void Walk_runs_from_source_through_roads_to_destination()
    {
        // mine(0,0) --[1,0][2,0][3,0]-- capital(4,0)
        var roads = new HashSet<Vector2I> { new(1, 0), new(2, 0), new(3, 0) };
        List<Vector2I> walk = Over(roads).FindWalk(new Vector2I(0, 0), new Vector2I(4, 0));

        Assert.Equal(
            new[] { new Vector2I(0, 0), new(1, 0), new(2, 0), new(3, 0), new(4, 0) },
            walk);
    }

    [Fact]
    public void Every_step_of_the_walk_is_adjacent()
    {
        var roads = new HashSet<Vector2I> { new(1, 0), new(2, 0), new(3, 0) };
        List<Vector2I> walk = Over(roads).FindWalk(new Vector2I(0, 0), new Vector2I(4, 0));

        for (int i = 1; i < walk.Count; i++)
        {
            Vector2I step = walk[i] - walk[i - 1];
            Assert.Equal(1, Mathf.Abs(step.X) + Mathf.Abs(step.Y)); // exactly one 4-neighbour hop
        }
    }

    [Fact]
    public void No_walk_when_the_ends_are_not_road_connected()
    {
        var roads = new HashSet<Vector2I> { new(1, 0), new(3, 0) }; // gap at (2,0)
        Assert.Empty(Over(roads).FindWalk(new Vector2I(0, 0), new Vector2I(4, 0)));
    }

    [Fact]
    public void Road_laid_on_an_endpoint_is_not_duplicated()
    {
        // Track sits right on the mine tile (0,0); the walk must not list (0,0) twice.
        var roads = new HashSet<Vector2I> { new(0, 0), new(1, 0), new(2, 0) };
        List<Vector2I> walk = Over(roads).FindWalk(new Vector2I(0, 0), new Vector2I(3, 0));

        Assert.Equal(
            new[] { new Vector2I(0, 0), new(1, 0), new(2, 0), new(3, 0) },
            walk);
    }
}
