using System.Collections.Generic;
using Godot;
// MyGlobalClass.GetSurroundingTiles (global namespace)

// Owns the economy graph. Provinces are thin ownership nodes; the real economic entities
// (deposits, buildings) live on tiles in TileDeposits / TileBuildings. Everything is built once
// from the Voronoi province map + the single world seed. Production (M2) plugs into OnTick.
public class Economy
{
    public Dictionary<Vector2I, Province> Provinces { get; } = new();
    public List<Deposit> Deposits { get; } = new();
    public Dictionary<Vector2I, Deposit> TileDeposits { get; } = new();
    public Dictionary<Vector2I, Building> TileBuildings { get; } = new();

    // --- tunables (deposit placement) ---
    private const int DepositCountTarget = 300;
    private const int MinSize = 1;
    private const int MaxSize = 5;
    private const float OreShare = 0.5f;     // fraction of deposits that are Ore (rest Grain)
    private const float ReservePerTile = 100f;
    private const float RatePerTile = 1f;

    private long tickCount = 0;

    public Economy(Dictionary<Vector2I, Vector2I> provincesDictionary, List<Vector2I> capitals, int worldSeed)
    {
        // 1. Thin provinces: invert tile -> capital into capital -> tiles.
        foreach (var kvp in provincesDictionary)
        {
            Vector2I tile = kvp.Key;
            Vector2I capital = kvp.Value;
            if (!Provinces.TryGetValue(capital, out Province province))
            {
                province = new Province(capital, new HashSet<Vector2I>());
                Provinces[capital] = province;
            }
            province.Tiles.Add(tile);
        }
        foreach (var capital in capitals)
            if (Provinces.TryGetValue(capital, out Province p))
                p.Owner = 0; // single player-state for L1

        // 2. Tile-bound deposits, deterministic from a sub-seed derived from the one world seed.
        PlaceDeposits(provincesDictionary, worldSeed);
    }

    private void PlaceDeposits(Dictionary<Vector2I, Vector2I> land, int worldSeed)
    {
        // System.Random (not Godot's RNG): deterministic from the one world seed AND usable in
        // plain .NET unit tests with no engine runtime.
        var rng = new System.Random(DeriveSeed(worldSeed));

        var landTiles = new List<Vector2I>(land.Keys);
        var used = new HashSet<Vector2I>();
        if (landTiles.Count == 0) return;

        int attempts = 0;
        int maxAttempts = DepositCountTarget * 20;
        while (Deposits.Count < DepositCountTarget && attempts < maxAttempts)
        {
            attempts++;
            // System.Random.Next upper bound is exclusive; Godot's RandiRange was inclusive, so +1
            // on the size range and use Count (not Count-1) for the index.
            Vector2I start = landTiles[rng.Next(0, landTiles.Count)];
            if (used.Contains(start)) continue;

            int targetSize = rng.Next(MinSize, MaxSize + 1);
            HashSet<Vector2I> cluster = GrowCluster(start, targetSize, land, used);
            if (cluster.Count == 0) continue;

            Good good = rng.NextDouble() < OreShare ? Good.Ore : Good.Grain;
            var deposit = new Deposit(good, cluster, cluster.Count * ReservePerTile, cluster.Count * RatePerTile);
            Deposits.Add(deposit);

            foreach (var t in cluster)
            {
                used.Add(t);
                TileDeposits[t] = deposit;
            }

            // Extractor sits on the deposit: Mine for ore, Farm for grain. Workshop is player-placed (M6).
            BuildingType extractor = good == Good.Ore ? BuildingType.Mine : BuildingType.Farm;
            TileBuildings[start] = Building.Of(extractor);
        }
    }

    // Flood-grow from `start` over adjacent UNUSED land tiles, up to targetSize (8-neighbour).
    private HashSet<Vector2I> GrowCluster(Vector2I start, int targetSize,
        Dictionary<Vector2I, Vector2I> land, HashSet<Vector2I> used)
    {
        var cluster = new HashSet<Vector2I>();
        if (used.Contains(start) || !land.ContainsKey(start)) return cluster;

        var frontier = new Queue<Vector2I>();
        frontier.Enqueue(start);
        cluster.Add(start);

        while (frontier.Count > 0 && cluster.Count < targetSize)
        {
            Vector2I cur = frontier.Dequeue();
            foreach (Vector2I n in MyGlobalClass.GetSurroundingTiles(cur))
            {
                if (cluster.Count >= targetSize) break;
                if (cluster.Contains(n) || used.Contains(n) || !land.ContainsKey(n)) continue;
                cluster.Add(n);
                frontier.Enqueue(n);
            }
        }
        return cluster;
    }

    // Mix the one world seed into a distinct, deterministic deposit seed so deposit placement is
    // reproducible from the single seed but not pattern-correlated with the terrain noise.
    private static int DeriveSeed(int worldSeed)
    {
        unchecked { return worldSeed * (int)0x9E3779B1 + 0x7F4A7C15; }
    }

    // M1 heartbeat: advance the tick and walk every node. No economic logic yet (M2+).
    public long OnTick()
    {
        tickCount++;
        foreach (var province in Provinces.Values)
        {
            // intentionally empty until M2
        }
        return tickCount;
    }
}
