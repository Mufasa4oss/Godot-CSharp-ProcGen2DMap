using Godot;
using System;
using System.Collections.Generic;

// A city consumes resources. For now it's a pure sink: every tick it demands all four resources, and
// that demand goes unmet (nothing delivers yet), so ConsumeDemand reports what it's short on. This is
// the consumer end of the eventual supply chain.
public class City
{
    public Vector2I Location { get; }
    public Resources Stockpile { get; }
    public Population Population { get; } // the city owns its people

    public Demand Demand { get; }
    
    //Enum CityType //based on type of resources as of current

    // Main hands us the deposits and the tile->capital ownership map; we count the deposits that fall in
    // our own Voronoi territory and let the population form around them.
    public City(Vector2I location, IReadOnlyDictionary<Vector2I, ResourceType> deposits,
                IReadOnlyDictionary<Vector2I, Vector2I> tileToCapital)
    {
        Location = location;
        Population = new Population(CountOwned(deposits, tileToCapital));
        Stockpile = new Resources();
        Demand = new Demand(Population, Stockpile);
    }

    public void GrowPopulation()
    {
        Population.workingPopulation += (int)(Population.workingPopulation * 0.01f);
        Population.dependentPopulation =
            (int)Math.Floor(Population.workingPopulation * GlobalConfig.WorkerToDependentRatio);
        Population.totalPopulation = Population.workingPopulation + Population.dependentPopulation;
        //GD.Print("Population grew");
    }
    public void ShrinkPopulation()
    {
        Population.totalPopulation -= (int)(Population.totalPopulation * 0.01f);
        //GD.Print("Population shrank");
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
    public void Delivered()
    {

    }
    // Take one tick of demand out of the stockpile. Nothing delivers yet, so for now every
    // resource is empty and these removals simply fail (the city goes unfed).
    public void ConsumeDemand()
    {
        foreach (var (type, amount) in Demand.PerResourceDemand())
            Stockpile.TryRemove(type, amount);
    }
    //switch(case)//for city type implementation e.g. lumber town, mining town
}
