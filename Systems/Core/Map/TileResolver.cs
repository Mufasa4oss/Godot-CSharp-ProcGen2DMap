// Core (pure: no Godot). Maps a tile's land-neighbour pattern to the autotile graphic it should use.
//
// Replaces the old Bitmask.DetermineTileType, which was an order-dependent list of overlapping
// (mask & X) == 0 checks (and had a dead, duplicated branch). A grass tile's graphic depends ONLY on
// which of its 4 cardinal neighbours are land (diagonals were always ignored), so there are exactly
// 2^4 = 16 cases — one per atlas tile. This is a direct lookup, mirroring RoadBitmask's table style.
public static class TileResolver
{
    // Cardinal bits used by the 16-entry table.
    public const int N = 1, E = 2, S = 4, W = 8;

    // Bit positions inside the 8-bit surrounding mask produced by Bitmask.GetBitmaskAllSurroundingTiles
    // (topLeft=1, top=2, topRight=4, left=8, right=16, botLeft=32, bot=64, botRight=128). Only the four
    // cardinals matter here.
    private const int MaskTop = 1 << 1;   // 2
    private const int MaskLeft = 1 << 3;  // 8
    private const int MaskRight = 1 << 4; // 16
    private const int MaskBot = 1 << 6;   // 64

    // Indexed by (N | E | S | W). Each entry is the atlas key in Bitmask.atlasDict.
    private static readonly string[] Table =
    {
        "island",          // 0  : no land neighbours
        "botSpecial",      // 1  : N            (only neighbour is above)
        "leftSpecial",     // 2  : E
        "botLeft",         // 3  : N+E
        "topSpecial",      // 4  : S
        "midSpecialVert",  // 5  : N+S          (vertical strip middle)
        "topLeft",         // 6  : E+S
        "left",            // 7  : N+E+S        (water only to the west)
        "rightSpecial",    // 8  : W
        "botRight",        // 9  : N+W
        "midSpecialHoriz", // 10 : E+W          (horizontal strip middle)
        "bot",             // 11 : N+E+W        (water only to the south)
        "topRight",        // 12 : S+W
        "right",           // 13 : N+S+W        (water only to the east)
        "top",             // 14 : E+S+W        (water only to the north)
        "surrounded",      // 15 : all four
    };

    // Pure table lookup from a 4-bit cardinal mask (N|E|S|W), 0..15.
    public static string FromCardinals(int cardinals) => Table[cardinals & 0b1111];

    // Convenience for the existing Bitmask path: extract the cardinals from the full 8-bit
    // surrounding mask, then look up the graphic.
    public static string Resolve(int neighborMask)
    {
        int cardinals = 0;
        if ((neighborMask & MaskTop) != 0) cardinals |= N;
        if ((neighborMask & MaskRight) != 0) cardinals |= E;
        if ((neighborMask & MaskBot) != 0) cardinals |= S;
        if ((neighborMask & MaskLeft) != 0) cardinals |= W;
        return FromCardinals(cardinals);
    }
}
