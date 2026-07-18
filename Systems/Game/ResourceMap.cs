using Godot;
using System;
using System.Collections.Generic;

// Places raw resources on the map and paints them as tiles on the ResourcesLayer so you can see where
// each good is. Placement is a deterministic scatter for now: a handful of each kind on random land
// tiles. (Buildings are painted onto their own layer the same way — see Tiles.)
public partial class ResourceMap : Node2D
{
    private readonly Dictionary<Vector2I, ResourceType> _deposits = new();

    public IReadOnlyDictionary<Vector2I, ResourceType> Deposits
    {
        get { return _deposits; }
    }

    // Scatter a few of each resource across random land tiles (seeded so the world reproduces), then
    // paint each one onto the ResourcesLayer.
    public void Initialize(TileMapLayer resourcesLayer, List<Vector2I> landTiles, int worldSeed)
    {
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

        // Paint each deposit onto the resources layer. If you haven't assigned that layer's tileset yet,
        // the deposits are still recorded (the cities read them) — they just won't be drawn.
        if (resourcesLayer.TileSet == null)
        {
            GD.Print("ResourcesLayer has no TileSet yet — deposits recorded but not drawn. Assign the resources tileset to see them.");
            return;
        }

        foreach (var (tile, type) in _deposits)
        {
            TileId t = Tiles.ForResource(type);
            resourcesLayer.SetCell(tile, t.Source, t.AtlasCoords, t.Alternative);
        }
    }
}
