using System.Collections.Generic;
using Godot;
using Xunit;

// Diplomacy stance management. Blockade/tariff behavior through export is covered by EconomyExportTests.
// Plain .NET (no engine).
public class EconomyDiplomacyTests
{
    private static Dictionary<Vector2I, Vector2I> TwoNationLand(int size, out Vector2I capA, out Vector2I capB)
    {
        capA = new Vector2I(0, 0);
        capB = new Vector2I(size - 1, 0);
        var land = new Dictionary<Vector2I, Vector2I>();
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                land[new Vector2I(x, y)] = x < size / 2 ? capA : capB;
        return land;
    }

    [Fact]
    public void Default_stance_is_neutral()
    {
        var land = TwoNationLand(16, out var capA, out var capB);
        var e = new Economy(land, new List<Vector2I> { capA, capB }, 2024);

        Assert.Equal(DiplomaticStance.Neutral, e.GetRelation(capA, capB));
        Assert.Equal(DiplomaticStance.Neutral, e.GetRelation(capB, capA)); // symmetric
    }
}
