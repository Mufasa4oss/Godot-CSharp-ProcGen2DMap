using Godot;

// A serializable (tile, building-type) pair for persisting player-built buildings. M4.5: extractors are
// player-built too now (not seed-derived), so every building on a tile must be saved and re-placed on
// load. Lives in Core (Godot-free bar Vector2I) so Economy.ApplySavedState stays unit-testable; the
// BuildingType round-trips through JSON as an int.
public class TileBuilding
{
    public Vector2I Tile { get; set; }
    public BuildingType Type { get; set; }

    public TileBuilding() { } // for deserialization
    public TileBuilding(Vector2I tile, BuildingType type)
    {
        Tile = tile;
        Type = type;
    }
}
