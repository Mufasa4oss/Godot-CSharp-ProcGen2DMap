using System;
using System.Collections.Generic;

// The kinds of resource in the game — one entry per raw good the economy moves around.
public enum ResourceType
{
    Grain,
    Iron,
    Water,
    Wood,
}

// A stockpile of resources: how much of each ResourceType something is holding right now.
// Anything that stores goods — a city, a building, a cart — owns one of these.
public class Resources
{
    // resource kind -> amount held
    private readonly Dictionary<ResourceType, int> _amounts = new();

    // How much of this resource is held (0 if none).
    public int Get(ResourceType type)
    {
        if (_amounts.TryGetValue(type, out int amount))
            return amount;

        return 0;
    }

    // Add some of a resource to the stockpile.
    public void Add(ResourceType type, int amount)
    {
        _amounts[type] = Get(type) + amount;
    }

    // Take some out, but only if there's enough. Returns false and changes nothing if short.
    public bool TryRemove(ResourceType type, int amount)
    {
        if (Get(type) < amount) return false;
        _amounts[type] = Get(type) - amount;
        return true;
    }
    public Dictionary<ResourceType, int> GetDictionaryResourceType()
    {
        var result = new Dictionary<ResourceType, int>();

        foreach (ResourceType r in Enum.GetValues<ResourceType>())
            result[r] = Get(r);

        return result;
    }

}
