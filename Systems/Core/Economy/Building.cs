using System;

public enum BuildingType
{
    Farm,
    Mine,
    Workshop
}

// A transformation node: consumes Inputs and produces Outputs at Rate per tick.
// Data only for now — the production logic lands in M2.
public class Building
{
    public BuildingType Type { get; }
    public Good[] Inputs { get; }
    public Good[] Outputs { get; }
    public float Rate { get; }

    public Building(BuildingType type, Good[] inputs, Good[] outputs, float rate)
    {
        Type = type;
        Inputs = inputs;
        Outputs = outputs;
        Rate = rate;
    }

    // Recipe table. Rates are placeholders to be balanced later (M7).
    public static Building Of(BuildingType type) => type switch
    {
        BuildingType.Farm     => new Building(type, Array.Empty<Good>(),     new[] { Good.Grain }, 2f),
        BuildingType.Mine     => new Building(type, Array.Empty<Good>(),     new[] { Good.Ore },   2f),
        BuildingType.Workshop => new Building(type, new[] { Good.Ore },      new[] { Good.Tools }, 1f),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}
