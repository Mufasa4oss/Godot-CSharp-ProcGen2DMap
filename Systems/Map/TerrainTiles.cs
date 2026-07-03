using Godot;

// Core. Catalog for the terrain TileSet: res://tilesets/tilesetTest.tres, used by the
// "0zTileMapLayer" terrain layer. Each constant is a source id (sources/N) in that .tres.
//
// Water and Capital are single fixed tiles, so they get ready-to-use TileId values. Grass and
// Highlight are autotiled: their atlas coords come from the bitmask resolver at paint time, so call
// sites pass just the source id (Grass / Highlight) plus the resolved coords. Sand is reserved
// (not placed yet).
public static class TerrainTiles
{
    public const int Water = 0;
    public const int Sand = 1;
    public const int Grass = 2;
    public const int Capital = 3;
    public const int Highlight = 4;

    public static readonly TileId WaterTile = new TileId(Water, Vector2I.Zero);
    public static readonly TileId CapitalTile = new TileId(Capital, Vector2I.Zero);
}
