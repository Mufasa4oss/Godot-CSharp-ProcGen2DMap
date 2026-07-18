// Core (pure: no Godot). Decides a cell's TerrainType from its height + moisture noise values, so
// the rules can be unit-tested without the engine. Godot's FastNoiseLite.GetNoise2D returns ~[-1, 1].
//
// Layering, low -> high elevation:
//   height <= WaterLevel              -> Water
//   WaterLevel < height < BeachLine   -> Sand  (BEACH: the thin low-coast band just above the
//                                               waterline. Steep coasts jump past the band in one
//                                               cell, so they stay grass/cliff -> sandy stretches,
//                                               not a uniform ring. Coastal by construction.)
//   else & moisture < DryThreshold    -> Sand  (DESERT: dry inland. A separate low-frequency
//                                               moisture field -> organic arid patches, like real
//                                               deserts forming from dryness, not elevation.)
//   else                              -> Grass
public static class TerrainRules
{
    // Matches the original land/water split (Initialize used height > 0.1 => land).
    public const float WaterLevel = 0.1f;
    // Top of the beach band. Thin, so only gentle low coasts turn sandy.
    public const float BeachLine = 0.135f;
    // Land drier than this (and not already beach) becomes desert sand. Lower = rarer deserts.
    // FastNoiseLite Perlin clusters tightly near 0, so small changes here move the desert fraction a
    // lot; -0.20 gives a clear-but-minority spread of arid patches (grass still dominates inland).
    public const float DryThreshold = -0.20f;

    public static TerrainType Classify(float height, float moisture)
    {
        if (height <= WaterLevel) return TerrainType.Water;
        if (height < BeachLine) return TerrainType.Sand;      // beach
        if (moisture < DryThreshold) return TerrainType.Sand; // desert
        return TerrainType.Grass;
    }

    // Is a (land) cell at this height a beach rather than a desert? Lets generation/economy tell the
    // two sand kinds apart without re-deriving the band. Only meaningful when Classify == Sand.
    public static bool IsBeach(float height)
    {
        return height > WaterLevel && height < BeachLine;
    }
}
