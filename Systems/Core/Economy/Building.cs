using System;

public enum BuildingType
{
    Farm,
    Mine,
    Workshop
}

// A transformation node: consumes Inputs and produces Outputs at Rate per tick.
// Cost is the treasury price to build it (0 for seed-placed extractors; the Workshop is player-built).
public class Building
{
    public BuildingType Type { get; }
    public Good[] Inputs { get; }
    public Good[] Outputs { get; }
    public float Rate { get; }
    public float Cost { get; }

    public Building(BuildingType type, Good[] inputs, Good[] outputs, float rate, float cost)
    {
        Type = type;
        Inputs = inputs;
        Outputs = outputs;
        Rate = rate;
        Cost = cost;
    }

    // Recipe table. Rates/costs are placeholders to be balanced later (M7). Farm/Mine are auto-placed
    // on deposits (cost 0); the Workshop is the player-built one (M3).
    public static Building Of(BuildingType type) => type switch
    {
        BuildingType.Farm     => new Building(type, Array.Empty<Good>(),     new[] { Good.Grain }, 2f, 0f),
        BuildingType.Mine     => new Building(type, Array.Empty<Good>(),     new[] { Good.Ore },   2f, 0f),
        BuildingType.Workshop => new Building(type, new[] { Good.Ore },      new[] { Good.Tools }, 1f, 100f),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}
