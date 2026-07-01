using System;

public enum BuildingType
{
    Farm,
    Mine,
    Workshop,
    Granary,
    Warehouse
}

// A transformation node: consumes Inputs and produces Outputs at Rate per tick.
// Cost is the treasury price to build it. M4.5: raw extractors (Farm/Mine) are player-built too now
// (no auto-placement), so they cost ExtractorCost; the Workshop stays the pricier refined-good builder.
// Granary/Warehouse (LR6) are a third kind: no inputs/outputs at all, just a StorageGood + StorageCapacity —
// they gate and cap how much of that good the owning nation can hold (see Economy's Pass 1 capacity check).
public class Building
{
    // Placeholder build costs — balanced for real in M7. Economy.StartingTreasury is sized to exactly
    // one extractor (M4.5 sub-rung 3) so the first build is a real choice, and a Workshop is only
    // reachable after selling raws.
    public const float ExtractorCost = 50f;
    public const float WorkshopCost  = 100f;
    public const float StorageCost   = 75f;
    public const float StorageCapacityPerBuilding = 300f;

    public BuildingType Type { get; }
    public Good[] Inputs { get; }
    public Good[] Outputs { get; }
    public float Rate { get; }
    public float Cost { get; }
    public Good? StorageGood { get; }
    public float StorageCapacity { get; }

    public Building(BuildingType type, Good[] inputs, Good[] outputs, float rate, float cost,
        Good? storageGood = null, float storageCapacity = 0f)
    {
        Type = type;
        Inputs = inputs;
        Outputs = outputs;
        Rate = rate;
        Cost = cost;
        StorageGood = storageGood;
        StorageCapacity = storageCapacity;
    }

    // Recipe table. Rates/costs are placeholders to be balanced later (M7). Mine/Farm/Workshop are
    // player-built (M4.5): Mine/Farm gate raw extraction on a deposit; the Workshop refines Ore→Tools.
    // Granary/Warehouse (LR6) are pure storage: no recipe, just a per-good capacity.
    public static Building Of(BuildingType type) => type switch
    {
        BuildingType.Farm      => new Building(type, Array.Empty<Good>(), new[] { Good.Grain }, 2f, ExtractorCost),
        BuildingType.Mine      => new Building(type, Array.Empty<Good>(), new[] { Good.Ore },   2f, ExtractorCost),
        BuildingType.Workshop  => new Building(type, new[] { Good.Ore },  new[] { Good.Tools }, 1f, WorkshopCost),
        BuildingType.Granary   => new Building(type, Array.Empty<Good>(), Array.Empty<Good>(), 0f, StorageCost,
                                       Good.Grain, StorageCapacityPerBuilding),
        BuildingType.Warehouse => new Building(type, Array.Empty<Good>(), Array.Empty<Good>(), 0f, StorageCost,
                                       Good.Ore, StorageCapacityPerBuilding),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}
