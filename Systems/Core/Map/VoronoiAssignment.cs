using System.Collections.Generic;
using Godot;

// Core (pure: Vector2I struct only, no TileMap/engine). The brute-force Voronoi assignment lifted out
// of VoronoiDiagram so it can be unit-tested: given land tiles and capital positions, map each tile to
// its nearest capital. The Godot class keeps the tilemap reads and the texture painting.
public static class VoronoiAssignment
{
    public static Dictionary<Vector2I, Vector2I> AssignNearest(
        IEnumerable<Vector2I> tiles, IReadOnlyList<Vector2I> capitals)
    {
        var result = new Dictionary<Vector2I, Vector2I>();
        if (capitals == null || capitals.Count == 0) return result;

        foreach (var tile in tiles)
        {
            Vector2I closest = capitals[0];
            float minDistance = float.MaxValue;
            foreach (var capital in capitals)
            {
                float distance = tile.DistanceTo(capital);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = capital;
                }
            }
            result[tile] = closest;
        }
        return result;
    }
}
