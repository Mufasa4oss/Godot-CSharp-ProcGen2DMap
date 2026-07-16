using Godot;
using System;
using System.Collections.Generic;

// A city consumes resources. For now it's a pure sink: every tick it demands all four resources, and
// that demand goes unmet (nothing delivers yet), so ConsumeDemand reports what it's short on. This is
// the consumer end of the eventual supply chain.
public class City
{
    public Vector2I Location { get; }
    public Resources Stockpile { get; } = new Resources();

    private const int DemandPerTick = 1; // units of each resource the city wants per tick

    public City(Vector2I location)
    {
        Location = location;
    }

    // How much of a resource this city wants each tick (uniform across resources for now).
    public int DemandFor(ResourceType type) => DemandPerTick;

    // Take one tick of demand from the stockpile; return the resources there wasn't enough of.
    public List<ResourceType> ConsumeDemand()
    {
        var unmet = new List<ResourceType>();
        foreach (ResourceType type in Enum.GetValues<ResourceType>())
            if (!Stockpile.TryRemove(type, DemandPerTick))
                unmet.Add(type);
        return unmet;
    }
}
