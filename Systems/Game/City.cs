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
    public Population Population { get; } // the city owns its people



    // Main hands us the deposits and the tile->capital ownership map; we count the deposits that fall in
    // our own Voronoi territory and let the population form around them.
    public City(Vector2I location, IReadOnlyDictionary<Vector2I, ResourceType> deposits,
                IReadOnlyDictionary<Vector2I, Vector2I> tileToCapital)
    {
        Location = location;
        Population = new Population(CountOwned(deposits, tileToCapital));

    }

    public void GrowPopulation()
    {
        Population.totalPopulation += (int)(Population.totalPopulation * 0.1f);
        //GD.Print("Population grew");
    }
    // How many deposits of each resource sit on tiles this city owns (its Voronoi region).
    private Dictionary<ResourceType, int> CountOwned(
        IReadOnlyDictionary<Vector2I, ResourceType> deposits,
        IReadOnlyDictionary<Vector2I, Vector2I> tileToCapital)
    {
        var owned = new Dictionary<ResourceType, int>();
        foreach (var (tile, type) in deposits)
            if (tileToCapital.TryGetValue(tile, out Vector2I capital) && capital == Location)
            {
                owned.TryGetValue(type, out int count);
                owned[type] = count + 1;
            }
        return owned;
    }

    // How much of a resource this city wants each tick (uniform across resources for now).
    public int DemandFor(ResourceType type) => GlobalConfig.CityDemandPerTick;

    // Take one tick of demand from the stockpile; return the resources there wasn't enough of.
    public List<ResourceType> ConsumeDemand()
    {
        var unmet = new List<ResourceType>();
        foreach (ResourceType type in Enum.GetValues<ResourceType>())
            if (!Stockpile.TryRemove(type, GlobalConfig.CityDemandPerTick))
                unmet.Add(type);
        return unmet;
    }
}
