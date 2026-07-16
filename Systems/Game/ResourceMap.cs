using Godot;
using System;
using System.Collections.Generic;

// Places raw resources on the map and draws them as coloured dots so you can see where each good is.
// Placement is a deterministic scatter for now: a handful of each kind on random land tiles.
public partial class ResourceMap : Node2D
{
    private TileMapLayer _tml;
    private readonly Dictionary<Vector2I, ResourceType> _deposits = new();

    public IReadOnlyDictionary<Vector2I, ResourceType> Deposits => _deposits;

    // Scatter a few of each resource across random land tiles, seeded so the world reproduces.
    public void Initialize(TileMapLayer tml, List<Vector2I> landTiles, int worldSeed)
    {
        _tml = tml;

        var shuffled = new List<Vector2I>(landTiles);
        var rng = new Random(worldSeed + 4241);
        for (int i = 0; i < shuffled.Count; i++) // Fisher–Yates shuffle
        {
            int j = rng.Next(i, shuffled.Count);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        // Take distinct tiles off the shuffled list, DepositsPerResource of each kind, so none collide.
        int index = 0;
        foreach (ResourceType type in Enum.GetValues<ResourceType>())
            for (int n = 0; n < GlobalConfig.DepositsPerResource && index < shuffled.Count; n++, index++)
                _deposits[shuffled[index]] = type;

        QueueRedraw(); // ask Godot to (re)run _Draw
    }

    public override void _Draw()
    {
        if (_tml == null) return;
        foreach (var (tile, type) in _deposits)
            DrawCircle(_tml.MapToLocal(tile), GlobalConfig.ResourceDotRadiusPx, ColorOf(type));
    }

    // The colour each resource is drawn in.
    private static Color ColorOf(ResourceType type) => type switch
    {
        ResourceType.Grain => Colors.Yellow,
        ResourceType.Iron => Colors.Gray,
        ResourceType.Water => Colors.DodgerBlue,
        ResourceType.Wood => Colors.SaddleBrown,
        _ => Colors.White,
    };
}
