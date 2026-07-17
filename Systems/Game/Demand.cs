using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// A city's demand for resources, worked out from its population.
// Barebones for now: it just remembers the population figures; the real demand maths comes later.
public class Demand
{
    Population Population;
    Resources Resources;

    public Demand(Population population, Resources resources)
    {
        Population = population;
        Resources = resources;
    }

    // Total resources this city wants each tick, scaled by its (live) population.
    // Grain and Water are population-driven; Iron and Wood are not (see notes).
    public Dictionary<ResourceType, int> PerResourceDemand()
    {
        var result = new Dictionary<ResourceType, int>();

        foreach (ResourceType type in Enum.GetValues<ResourceType>())
        {
            int demand = type switch
            {
                ResourceType.Grain => Population.workingPopulation * GlobalConfig.GrainPerWorkerRatio
                                    + Population.dependentPopulation * GlobalConfig.GrainPerDependentRatio,
                ResourceType.Water => Population.workingPopulation * GlobalConfig.WaterPerWorkerRatio,
                _ => 0, // Iron & Wood aren't population-driven yet — see notes
            };

            result[type] = demand;
        }

        return result;
    }

    //town type based demand

    //trading is Out of scope
}
