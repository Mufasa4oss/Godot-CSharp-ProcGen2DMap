using System.Collections.Generic;
using Godot;

// A province is a thin spatial / ownership node: a capital id, an owner, and the set of tiles
// that belong to it. Economic entities (deposits, buildings) live on tiles in Economy, not here.
// Pop / market state return at their own milestones (pop M3, prices M4).
public class Province
{
    public Vector2I Capital { get; }
    public int Owner { get; set; }
    public HashSet<Vector2I> Tiles { get; }

    public Province(Vector2I capital, HashSet<Vector2I> tiles)
    {
        Capital = capital;
        Tiles = tiles;
    }
}
