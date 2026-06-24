using Godot;

// Core (no Godot Node; Vector2I struct only).
// One addressable tile inside a TileSet (.tres). These are exactly the arguments
// TileMapLayer.SetCell takes: the source id (sources/N in the .tres), the atlas coordinates of the
// tile within that source, and the alternative-tile id. The TileSet itself is implied by the layer.
public readonly struct TileId
{
    public readonly int Source;          // sources/N in the .tres ("AtlasID")
    public readonly Vector2I AtlasCoords; // cell within that atlas source
    public readonly int Alternative;      // alternative-tile id (the "/N" suffix in the .tres)

    public TileId(int source, Vector2I atlasCoords, int alternative = 0)
    {
        Source = source;
        AtlasCoords = atlasCoords;
        Alternative = alternative;
    }
}
