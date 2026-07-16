using System.Collections.Generic;
using System.Linq;
using Godot;
using Xunit;

public class RoadsTests
{
    [Fact]
    public void Building_a_road_registers_the_tile_with_zero_vehicles()
    {
        var roads = new Roads();
        var tile = new Vector2I(3, 4);

        roads.Build(tile);

        Assert.True(roads.IsRoad(tile));
        Assert.Equal(0, roads.VehiclesOn(tile));
    }

    [Fact]
    public void A_tile_without_a_road_never_enters_the_registry()
    {
        var roads = new Roads();
        roads.Build(new Vector2I(1, 1));

        // Adding a vehicle to a non-road tile must do nothing — no entry appears.
        roads.AddVehicle(new Vector2I(9, 9));

        var roadTiles = roads.All.Select(pair => pair.Key).ToList();
        Assert.Equal(new List<Vector2I> { new Vector2I(1, 1) }, roadTiles);
        Assert.False(roads.IsRoad(new Vector2I(9, 9)));
        Assert.Equal(0, roads.VehiclesOn(new Vector2I(9, 9)));
    }

    [Fact]
    public void Vehicles_accumulate_on_the_road_they_are_added_to()
    {
        var roads = new Roads();
        var tile = new Vector2I(2, 2);
        roads.Build(tile);

        roads.AddVehicle(tile);
        roads.AddVehicle(tile);

        Assert.Equal(2, roads.VehiclesOn(tile));
    }

    [Fact]
    public void Removing_a_vehicle_never_goes_below_zero()
    {
        var roads = new Roads();
        var tile = new Vector2I(0, 0);
        roads.Build(tile);

        roads.RemoveVehicle(tile);   // already at 0

        Assert.Equal(0, roads.VehiclesOn(tile));
    }

    [Fact]
    public void A_road_over_capacity_is_the_bottleneck_the_tick_flags()
    {
        var roads = new Roads { Capacity = 5 };
        var tile = new Vector2I(7, 7);
        roads.Build(tile);

        for (int i = 0; i < 6; i++)   // 6 vehicles > capacity 5
            roads.AddVehicle(tile);

        // Exactly the condition Tick.Advance() checks each beat.
        var bottlenecks = roads.All.Where(pair => pair.Value > roads.Capacity).ToList();

        Assert.Single(bottlenecks);
        Assert.Equal(tile, bottlenecks[0].Key);
        Assert.Equal(6, bottlenecks[0].Value);
    }
}
