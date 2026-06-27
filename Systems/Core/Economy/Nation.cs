using System.Collections.Generic;
using Godot;

// A nation/government: one of the worldgen's capitals plus the tiles it owns, its treasury, and its
// stockpile. (10 nations for L1 — one player, the rest passive AI. Territory grows via war later.)
// Each capital seeds one Nation; the geographic Voronoi region it sits in is its starting territory.
// Economic entities (deposits, buildings) live on tiles in Economy, not here.
public class Nation
{
    public Vector2I Capital { get; }
    public int Owner { get; set; }       // 0 = the player; 1.. = AI
    public bool IsPlayer => Owner == 0;
    public HashSet<Vector2I> Tiles { get; }

    // Money the nation spends to build (M3). Income (selling goods) arrives at M4.
    public float Treasury { get; set; }

    // M5: a minimal demand sink — pops that eat grain each tick. Just a count for now (no labor sim);
    // deterministic from construction, so it isn't serialized (regenerated like Tiles on load).
    public float Population { get; set; }

    // Goods this nation holds. Filled by production (M2/M3), drawn down by workshops (M3), pop (M5) and
    // trade (M6). Persisted in the save and restored on load.
    public Dictionary<Good, float> Stockpile { get; } = new();

    // M6: this nation's per-good scarcity price multiplier on the base price (1 = base). Recomputed each tick
    // from its own local supply/demand, so different nations price the same good differently — the gradient
    // that trade chases. Live display state, not serialized (regenerated each tick).
    public Dictionary<Good, float> PriceMultiplier { get; } = new();

    public Nation(Vector2I capital, HashSet<Vector2I> tiles)
    {
        Capital = capital;
        Tiles = tiles;
    }

    // Single accumulate path shared by production and any future producer.
    public void AddGood(Good good, float amount)
    {
        Stockpile.TryGetValue(good, out float current);
        Stockpile[good] = current + amount;
    }
}
