using Godot;
using System.Collections.Generic;

public class RailroadBitmask
{
    // Row 0 = base track, Row 1 = water-bridge variant (same column), Row 2 = junctions/overpass.
    private static readonly Dictionary<int, Vector2I> Atlas = new Dictionary<int, Vector2I>
    {
        { 0b1001, new Vector2I(0, 0) }, // up+right  → "left to up" corner  (enters from right, exits top)
        { 0b0101, new Vector2I(1, 0) }, // up+left   → "right to up" corner (enters from left, exits top)
        { 0b1100, new Vector2I(2, 0) }, // left+right → horizontal straight
        { 0b0011, new Vector2I(3, 0) }, // up+down   → vertical straight
        { 0b1010, new Vector2I(4, 0) }, // down+right → "up to right" corner
        { 0b0110, new Vector2I(5, 0) }, // down+left  → "up to left" corner

        { 0b1011, new Vector2I(0, 2) }, // up+down+right  → T-junction
        { 0b0111, new Vector2I(1, 2) }, // up+down+left   → T-junction
        { 0b1101, new Vector2I(2, 2) }, // left+right+up  → T-junction
        { 0b1110, new Vector2I(3, 2) }, // left+right+down → T-junction
        { 0b1111, new Vector2I(4, 2) }, // 4-way          → overpass (bridge)
    };

    private static readonly Vector2I FallbackHoriz = new Vector2I(2, 0);
    private static readonly Vector2I FallbackVert  = new Vector2I(3, 0);

    public void OrientRailroadCells(TileMapLayer railroadLayer, HashSet<Vector2I> railroadTiles, HashSet<Vector2I> capitalSet, TileMapLayer terrainLayer)
    {
        foreach (Vector2I tile in railroadTiles)
        {
            int mask = GetCardinalMask(tile, railroadTiles, capitalSet);
            railroadLayer.SetCell(tile, RailroadTiles.Rails, ResolveAtlas(mask, tile, terrainLayer));
        }
    }

    private int GetCardinalMask(Vector2I tile, HashSet<Vector2I> railroadTiles, HashSet<Vector2I> capitalSet)
    {
        int mask = 0;
        if (IsRailroadOrCapital(tile.X,     tile.Y - 1, railroadTiles, capitalSet)) mask |= 0b0001; // up
        if (IsRailroadOrCapital(tile.X,     tile.Y + 1, railroadTiles, capitalSet)) mask |= 0b0010; // down
        if (IsRailroadOrCapital(tile.X - 1, tile.Y,     railroadTiles, capitalSet)) mask |= 0b0100; // left
        if (IsRailroadOrCapital(tile.X + 1, tile.Y,     railroadTiles, capitalSet)) mask |= 0b1000; // right
        return mask;
    }

    private bool IsRailroadOrCapital(int x, int y, HashSet<Vector2I> railroadTiles, HashSet<Vector2I> capitalSet)
    {
        var pos = new Vector2I(x, y);
        return railroadTiles.Contains(pos) || capitalSet.Contains(pos);
    }

    private Vector2I ResolveAtlas(int mask, Vector2I tile, TileMapLayer terrainLayer)
    {
        // 3-way (T) and 4-way (overpass) junctions take priority over terrain styling.
        if (BitCount(mask) >= 3 && Atlas.TryGetValue(mask, out Vector2I junction))
            return junction;

        // Straights / curves (and 1-bit end-cap / island fallbacks).
        Vector2I coord;
        if (!Atlas.TryGetValue(mask, out coord))
        {
            bool hasVertical = (mask & 0b0011) != 0;
            coord = hasVertical ? FallbackVert : FallbackHoriz;
        }

        // Over water → use the bridge variant on the row below.
        if (IsWater(terrainLayer, tile))
            coord.Y += 1;

        return coord;
    }

    private bool IsWater(TileMapLayer terrainLayer, Vector2I tile)
    {
        return terrainLayer != null && terrainLayer.GetCellSourceId(tile) == TerrainTiles.Water;
    }

    private static int BitCount(int mask)
    {
        int count = 0;
        while (mask != 0)
        {
            count += mask & 1;
            mask >>= 1;
        }
        return count;
    }
}
