using System;

public enum BuildingType
{
    Farm,
    Mine,
    Workshop
}

// A transformation node: consumes Inputs and produces Outputs at Rate per tick.
// Cost is the treasury price to build it. M4.5: raw extractors (Farm/Mine) are player-built too now
// (no auto-placement), so they cost ExtractorCost; the Workshop stays the pricier refined-good builder.
public class Building
{
    // Placeholder build costs — balanced for real in M7. Economy.StartingTreasury is sized to exactly
    // one extractor (M4.5 sub-rung 3) so the first build is a real choice, and a Workshop is only
    // reachable after selling raws.
    public const float ExtractorCost = 50f;
    public const float WorkshopCost  = 100f;

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

    // Recipe table. Rates/costs are placeholders to be balanced later (M7). All three are player-built
    // (M4.5): Mine/Farm gate raw extraction on a deposit; the Workshop refines Ore→Tools.
    public static Building Of(BuildingType type) => type switch
    {
        BuildingType.Farm     => new Building(type, Array.Empty<Good>(),     new[] { Good.Grain }, 2f, ExtractorCost),
        BuildingType.Mine     => new Building(type, Array.Empty<Good>(),     new[] { Good.Ore },   2f, ExtractorCost),
        BuildingType.Workshop => new Building(type, new[] { Good.Ore },      new[] { Good.Tools }, 1f, WorkshopCost),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}
