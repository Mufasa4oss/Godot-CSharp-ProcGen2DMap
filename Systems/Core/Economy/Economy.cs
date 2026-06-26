using System.Collections.Generic;
using Godot;
// MyGlobalClass.GetSurroundingTiles (global namespace)

// Owns the economy graph. Nations are the thin ownership/government nodes; the real economic entities
// (deposits, buildings) live on tiles in TileDeposits / TileBuildings. Everything is built once from the
// Voronoi province map + the single world seed. Production + player building plug into OnTick / the
// build methods.
public class Economy
{
    public Dictionary<Vector2I, Nation> Nations { get; } = new();
    public List<Deposit> Deposits { get; } = new();
    public Dictionary<Vector2I, Deposit> TileDeposits { get; } = new();
    public Dictionary<Vector2I, Building> TileBuildings { get; } = new();
    // Reverse map: which nation owns each land tile. Lets production route a building's output to the
    // nation that owns its tile without scanning every nation's Tiles set.
    public Dictionary<Vector2I, Nation> TileNation { get; } = new();

    // Player-built railroad tiles (paid for, not part of the deterministic MST backbone). Persisted so the
    // track you bought survives save/load, and used to dedupe so you're never charged twice for a tile.
    public HashSet<Vector2I> Railroads { get; } = new();

    // The player's nation (Owner 0 = the first placed capital). Cached; ownership doesn't change in L1.
    public Nation PlayerNation { get; private set; }

    // Per-good flow into / out of the PLAYER's stockpile during the last tick, so the UI can show
    // "+produced / -consumed". Recomputed each OnTick.
    public Dictionary<Good, float> PlayerProducedLastTick { get; } = new();
    public Dictionary<Good, float> PlayerConsumedLastTick { get; } = new();

    // Per-tick production events (tile, good, amount) so the Godot view can flash a fading "+N" on the
    // source building (M4.5 sub-rung 4 — see where & why a gain happened). Core stays engine-free: this
    // is plain data (Vector2I only, no signals) that the view drains after each OnTick. Recomputed each tick.
    public readonly List<(Vector2I Tile, Good Good, float Amount)> ProducedThisTick = new();

    // --- tunables (deposit placement) ---
    private const int DepositCountTarget = 300;
    private const int MinSize = 1;
    private const int MaxSize = 5;
    private const float OreShare = 0.5f;     // fraction of deposits that are Ore (rest Grain)
    private const float ReservePerTile = 100f;
    private const float RatePerTile = 1f;

    // --- tunables (economy) ---
    // M4.5 sub-rung 3: exactly one extractor's worth, so the first build is a real choice (can't afford
    // two raws at once) and a Workshop is only reachable after selling. Placeholder — tuned in M7.
    private const float StartingTreasury = Building.ExtractorCost;
    // M4.5: laying one railroad tile costs treasury too, so track is a real economic spend (placeholder).
    private const float RailroadCost = 10f;

    // M4: each good's sale value. A static price table for now — supply/demand response arrives at M5.
    // Tools is worth more than the Ore a Workshop consumes to make it (1 Ore → 1 Tools), so refining
    // Ore→Tools and selling Tools out-earns selling raw Ore — the payoff that makes the M3 build choice
    // worth it and closes the build → produce → sell → reinvest loop.
    private static readonly Dictionary<Good, float> PriceTable = new()
    {
        { Good.Grain, 1f },
        { Good.Ore,   2f },
        { Good.Tools, 6f },
    };
    public IReadOnlyDictionary<Good, float> Prices => PriceTable;
    public float PriceOf(Good good) => PriceTable.GetValueOrDefault(good);

    // Lifetime treasury the player has earned by selling EACH good — a breakdown so it's legible where
    // income comes from (refined Tools vs raw Ore vs Grain), not just a lump sum. Display stat for the
    // UI; the real state is the Treasury, which is what the save persists, so this isn't serialized and
    // resets on load.
    public Dictionary<Good, float> IncomeByGood { get; } = new();
    public float IncomeOf(Good good) => IncomeByGood.GetValueOrDefault(good);
    public float TotalIncome
    {
        get
        {
            float sum = 0f;
            foreach (float v in IncomeByGood.Values) sum += v;
            return sum;
        }
    }

    private long tickCount = 0;

    public Economy(Dictionary<Vector2I, Vector2I> provincesDictionary, List<Vector2I> capitals, int worldSeed)
    {
        // 1. Thin nations: invert tile -> capital into capital -> tiles.
        foreach (var kvp in provincesDictionary)
        {
            Vector2I tile = kvp.Key;
            Vector2I capital = kvp.Value;
            if (!Nations.TryGetValue(capital, out Nation nation))
            {
                nation = new Nation(capital, new HashSet<Vector2I>());
                Nations[capital] = nation;
            }
            nation.Tiles.Add(tile);
            TileNation[tile] = nation;
        }

        // Owner by placement order: capitals[0] is the player (Owner 0), the rest are AI. Deterministic
        // from the seed. Every nation starts with the same treasury (only the player's is spent in M3).
        for (int i = 0; i < capitals.Count; i++)
            if (Nations.TryGetValue(capitals[i], out Nation n))
            {
                n.Owner = i;
                n.Treasury = StartingTreasury;
                if (n.IsPlayer) PlayerNation = n;
            }

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
            // M4.5: no auto-placed extractor. Raw extraction is build-gated — the player must build a
            // Mine/Farm on the deposit (TryBuildExtractor) before it produces.
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

    // Player builds a Workshop (refines Ore→Tools) on any owned, empty land tile they can afford.
    public bool TryBuildWorkshop(Vector2I tile) => TryBuild(tile, Building.Of(BuildingType.Workshop));

    // M4.5: lay one railroad tile for RailroadCost — charges the player's treasury and records the tile,
    // so track is a real spend like the gathering buildings. No-op (false) if the tile is already track or
    // the player can't afford it. Track may cross any territory (it connects nations), so no ownership check.
    public bool TryBuildRailroad(Vector2I tile)
    {
        if (PlayerNation == null) return false;
        if (Railroads.Contains(tile)) return false;            // already track — don't double-charge
        if (PlayerNation.Treasury < RailroadCost) return false; // can't afford

        PlayerNation.Treasury -= RailroadCost;
        Railroads.Add(tile);
        return true;
    }

    // M4.5 sub-rung 2: player builds the matching extractor on a deposit tile — Mine on Ore, Farm on
    // Grain. Raw goods are build-gated: no extractor on the deposit → no output. The type is auto-picked
    // from the deposit so the UI needs no Mine-vs-Farm choice. No-op (false) if the tile has no deposit.
    public bool TryBuildExtractor(Vector2I tile)
    {
        if (!TileDeposits.TryGetValue(tile, out Deposit deposit)) return false; // extractors need a deposit
        BuildingType type = deposit.Good == Good.Ore ? BuildingType.Mine : BuildingType.Farm;
        return TryBuild(tile, Building.Of(type));
    }

    // Shared build path: valid only on a land tile the player's nation owns that has no building yet, and
    // only if the player can afford it. Charges the treasury and places the building. False (no-op) otherwise.
    private bool TryBuild(Vector2I tile, Building building)
    {
        if (!TileNation.TryGetValue(tile, out Nation nation)) return false; // not land / not owned
        if (!nation.IsPlayer) return false;                                 // only your own territory
        if (TileBuildings.ContainsKey(tile)) return false;                  // tile already has a building
        if (nation.Treasury < building.Cost) return false;                  // can't afford

        nation.Treasury -= building.Cost;
        TileBuildings[tile] = building;
        return true;
    }

    // M4: sell the player's entire stockpile of `good` at its price, moving the proceeds into the
    // treasury. Returns the revenue (0 if there's nothing to sell). Player-only in L1 — the AI nations
    // don't trade yet. This is the "sell" half of the build → produce → sell → reinvest loop.
    public float SellAll(Good good)
    {
        Nation nation = PlayerNation;
        if (nation == null) return 0f;

        nation.Stockpile.TryGetValue(good, out float amount);
        if (amount <= 0f) return 0f;

        float revenue = amount * PriceOf(good);
        nation.AddGood(good, -amount);
        nation.Treasury += revenue;
        AddFlow(IncomeByGood, good, revenue);
        return revenue;
    }

    // M3: advance the tick and run production in two passes so workshops can consume what extractors
    // produced the same tick.
    public long OnTick()
    {
        tickCount++;
        PlayerProducedLastTick.Clear();
        PlayerConsumedLastTick.Clear();
        ProducedThisTick.Clear();

        // Pass 1 — raw extractors (no inputs): build-gated (only a player-built Mine/Farm produces) and
        // throttled to keep raw goods scarce (M4.5 sub-rung 3) — a flat 1 unit, and only on alternating
        // ticks — so a deposit isn't effectively free and the one build choice bites. The deposit's
        // reserve still depletes by what's pulled; its Rate is intentionally bypassed for raws.
        bool rawTickActive = tickCount % 2 == 0;
        foreach (var (tile, building) in TileBuildings)
        {
            if (building.Inputs.Length != 0) continue;
            if (!rawTickActive) continue;
            if (!TileDeposits.TryGetValue(tile, out Deposit deposit)) continue;
            if (deposit.Reserve <= 0f) continue;

            float amount = System.Math.Min(1f, deposit.Reserve);
            deposit.Reserve -= amount;
            Nation nation = TileNation[tile];
            Good good = building.Outputs[0];
            nation.AddGood(good, amount);
            ProducedThisTick.Add((tile, good, amount));
            if (nation.IsPlayer) AddFlow(PlayerProducedLastTick, good, amount);
        }

        // Pass 2 — workshops (have inputs): consume inputs and produce outputs from/into the owning
        // nation, but only if every input is available at the full rate (no partial runs).
        foreach (var (tile, building) in TileBuildings)
        {
            if (building.Inputs.Length == 0) continue;
            Nation nation = TileNation[tile];

            bool canRun = true;
            foreach (Good input in building.Inputs)
            {
                nation.Stockpile.TryGetValue(input, out float have);
                if (have < building.Rate) { canRun = false; break; }
            }
            if (!canRun) continue;

            foreach (Good input in building.Inputs)
            {
                nation.AddGood(input, -building.Rate);
                if (nation.IsPlayer) AddFlow(PlayerConsumedLastTick, input, building.Rate);
            }
            foreach (Good output in building.Outputs)
            {
                nation.AddGood(output, building.Rate);
                ProducedThisTick.Add((tile, output, building.Rate));
                if (nation.IsPlayer) AddFlow(PlayerProducedLastTick, output, building.Rate);
            }
        }
        return tickCount;
    }

    private static void AddFlow(Dictionary<Good, float> flows, Good good, float amount)
    {
        flows.TryGetValue(good, out float current);
        flows[good] = current + amount;
    }

    // Overwrite the freshly-regenerated economy with saved state on load: deposit reserves (by index,
    // deterministic order), each nation's treasury + stockpile (by capital), the player-built buildings —
    // extractors and workshops (M4.5: none are seed-derived, so they must be re-placed) — and the
    // player-built railroad tiles (also paid-for, not seed-derived).
    public void ApplySavedState(List<float> reserves, List<NationStock> nationStocks, List<TileBuilding> buildings, List<Vector2I> railroads)
    {
        if (reserves != null)
        {
            if (reserves.Count == Deposits.Count)
                for (int i = 0; i < Deposits.Count; i++)
                    Deposits[i].Reserve = reserves[i];
            else
                // Core stays Godot-free (no GD); Console.Error still surfaces in Godot's output.
                System.Console.Error.WriteLine($"ApplySavedState: reserve count {reserves.Count} != deposits {Deposits.Count}; skipping reserve restore.");
        }

        if (nationStocks != null)
            foreach (NationStock ns in nationStocks)
                if (Nations.TryGetValue(ns.Capital, out Nation nation))
                {
                    nation.Treasury = ns.Treasury;
                    nation.Stockpile[Good.Grain] = ns.Grain;
                    nation.Stockpile[Good.Ore]   = ns.Ore;
                    nation.Stockpile[Good.Tools] = ns.Tools;
                }

        if (buildings != null)
            foreach (TileBuilding b in buildings)
                TileBuildings[b.Tile] = Building.Of(b.Type);

        if (railroads != null)
        {
            Railroads.Clear();
            foreach (Vector2I tile in railroads) Railroads.Add(tile);
        }
    }
}
