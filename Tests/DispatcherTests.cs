using System.Collections.Generic;
using Godot;
using Xunit;

// LR2: the auto-hauling layer — dispatch matches a surplus tile to a road-connected sink and sends a truck;
// advance rolls it, delivers on arrival, and strands it when the road ahead is cut. Pure Core, headless.
public class DispatcherTests
{
    private static readonly Vector2I Mine    = new(0, 0);
    private static readonly Vector2I Capital = new(4, 0);

    // mine(0,0) --[1,0][2,0][3,0]-- capital(4,0)
    private static HashSet<Vector2I> ConnectedRoads() => new() { new(1, 0), new(2, 0), new(3, 0) };

    // Every non-capital tile ships to the capital; the capital itself is a sink, not a source.
    private static Vector2I? ToCapital(Vector2I tile) => tile == Capital ? (Vector2I?)null : Capital;

    [Fact]
    public void Dispatch_sends_one_hauler_and_picks_the_load_off_the_tile()
    {
        var inv = new TileInventory();
        inv.Add(Mine, Good.Ore, 3f);
        var d = new Dispatcher();

        d.Dispatch(inv, ConnectedRoads(), ToCapital);

        Assert.Single(d.Haulers);
        Assert.Equal(Good.Ore, d.Haulers[0].Good);
        Assert.Equal(Capital, d.Haulers[0].Destination);
        Assert.Equal(2f, inv.AmountAt(Mine, Good.Ore), 3); // one unit loaded onto the truck
    }

    [Fact]
    public void Disconnected_surplus_is_not_dispatched_and_stays_on_its_tile()
    {
        var inv = new TileInventory();
        inv.Add(Mine, Good.Ore, 3f);
        var d = new Dispatcher();

        var cut = new HashSet<Vector2I> { new(1, 0), new(3, 0) }; // gap at (2,0): mine can't reach the capital
        d.Dispatch(inv, cut, ToCapital);

        Assert.Empty(d.Haulers);
        Assert.Equal(3f, inv.AmountAt(Mine, Good.Ore), 3); // stranded — nothing picked up
    }

    [Fact]
    public void A_hauler_advances_along_the_road_and_delivers_to_the_sink()
    {
        var roads = ConnectedRoads();
        var inv = new TileInventory();
        inv.Add(Mine, Good.Ore, 1f);
        var d = new Dispatcher();
        d.Dispatch(inv, roads, ToCapital);

        // Walk is mine,(1,0),(2,0),(3,0),capital = 4 hops at Speed 1/tick.
        for (int i = 0; i < 4; i++) d.Advance(inv, roads);

        Assert.Empty(d.Haulers);                              // arrived + retired
        Assert.Equal(1f, inv.AmountAt(Capital, Good.Ore), 3); // unloaded at the capital warehouse
    }

    [Fact]
    public void Cutting_the_road_ahead_strands_the_hauler_with_its_load()
    {
        var roads = ConnectedRoads();
        var inv = new TileInventory();
        inv.Add(Mine, Good.Ore, 1f);
        var d = new Dispatcher();
        d.Dispatch(inv, roads, ToCapital);

        d.Advance(inv, roads); // roll onto (1,0)
        roads.Remove(new Vector2I(3, 0)); // tear up the road still ahead of the truck
        for (int i = 0; i < 10; i++) d.Advance(inv, roads);

        Assert.Single(d.Haulers);                            // still out there, holding its load
        Assert.Equal(0f, inv.AmountAt(Capital, Good.Ore), 3); // nothing teleported across the gap
    }

    [Fact]
    public void Only_one_hauler_per_origin_at_a_time()
    {
        var inv = new TileInventory();
        inv.Add(Mine, Good.Ore, 5f);
        var d = new Dispatcher();

        d.Dispatch(inv, ConnectedRoads(), ToCapital);
        d.Dispatch(inv, ConnectedRoads(), ToCapital); // same tile already served — no second truck

        Assert.Single(d.Haulers);
        Assert.Equal(4f, inv.AmountAt(Mine, Good.Ore), 3); // only one unit taken
    }
}
