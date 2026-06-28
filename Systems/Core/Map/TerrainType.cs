// Core (pure: no Godot). The terrain category of a map cell, decided by TerrainRules from a
// cell's height + moisture noise. Sand covers BOTH beaches (the low coast band) and deserts (dry
// inland) — they share the sand graphic, so the renderer treats them identically; the beach/desert
// split only matters to generation (and, later, the economy).
public enum TerrainType
{
    Water,
    Grass,
    Sand,
}
