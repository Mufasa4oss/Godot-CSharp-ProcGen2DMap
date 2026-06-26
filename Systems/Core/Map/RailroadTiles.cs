// Core. Catalog for the railroad TileSet (res://tilesets/road.tres), used by the "RailroadLayer".
// sources/0 = rails_narrow (the track variants), sources/1 = dirt_road. Atlas coords for each
// orientation (straights, corners, junctions, bridge variants) are resolved in RailroadBitmask.
public static class RailroadTiles
{
    public const int Rails = 0;     // sources/0 in road.tres
    public const int DirtRoad = 1;  // sources/1 in road.tres
}
