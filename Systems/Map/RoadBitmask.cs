using Godot;
using System.Collections.Generic;

public class RoadBitmask
{
    private static readonly Dictionary<int, Vector2I> Atlas = new Dictionary<int, Vector2I>
    {
        { 0b1001, new Vector2I(0, 0) }, // up+right  → "left to up" corner  (enters from right, exits top)
        { 0b0101, new Vector2I(1, 0) }, // up+left   → "right to up" corner (enters from left, exits top)
        { 0b1100, new Vector2I(2, 0) }, // left+right → horizontal straight
        { 0b0011, new Vector2I(3, 0) }, // up+down   → vertical straight
        { 0b1010, new Vector2I(4, 0) }, // down+right → "up to right" corner
        { 0b0110, new Vector2I(5, 0) }, // down+left  → "up to left" corner
    };

    private static readonly Vector2I FallbackHoriz = new Vector2I(2, 0);
    private static readonly Vector2I FallbackVert  = new Vector2I(3, 0);

    public void OrientRoadCells(TileMapLayer roadLayer, HashSet<Vector2I> roadTiles, HashSet<Vector2I> capitalSet)
    {
        foreach (Vector2I tile in roadTiles)
        {
            int mask = GetCardinalMask(tile, roadTiles, capitalSet);
            roadLayer.SetCell(tile, 0, ResolveAtlas(mask));
        }
    }

    private int GetCardinalMask(Vector2I tile, HashSet<Vector2I> roadTiles, HashSet<Vector2I> capitalSet)
    {
        int mask = 0;
        if (IsRoadOrCapital(tile.X,     tile.Y - 1, roadTiles, capitalSet)) mask |= 0b0001; // up
        if (IsRoadOrCapital(tile.X,     tile.Y + 1, roadTiles, capitalSet)) mask |= 0b0010; // down
        if (IsRoadOrCapital(tile.X - 1, tile.Y,     roadTiles, capitalSet)) mask |= 0b0100; // left
        if (IsRoadOrCapital(tile.X + 1, tile.Y,     roadTiles, capitalSet)) mask |= 0b1000; // right
        return mask;
    }

    private bool IsRoadOrCapital(int x, int y, HashSet<Vector2I> roadTiles, HashSet<Vector2I> capitalSet)
    {
        var pos = new Vector2I(x, y);
        return roadTiles.Contains(pos) || capitalSet.Contains(pos);
    }

    private Vector2I ResolveAtlas(int mask)
    {
        if (Atlas.TryGetValue(mask, out Vector2I coord))
            return coord;

        // Fallbacks for end caps, T-junctions, crossings, island
        bool hasVertical = (mask & 0b0011) != 0;
        return hasVertical ? FallbackVert : FallbackHoriz;
    }
}
