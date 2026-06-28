using System.Collections.Generic;
using System.Linq;
using Godot;
// MyGlobalClass.GetSurroundingTiles (global namespace)

// Owns the economy graph. Nations are the thin ownership/government nodes; the real economic entities
// (deposits, buildings) live on tiles in TileDeposits / TileBuildings. Everything is built once from the
// Voronoi nation map + the single world seed. Production + player building plug into OnTick / the
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

    // M6: when true the AI nations are live economic actors (pops + auto-built extractors + tax). A single
    // switch so extraction-isolation tests can keep the AI inert (mirrors the `Population = 0f` pattern).
    public bool EnableAiAutonomy { get; set; } = true;

    // Per-good flow into / out of the PLAYER's stockpile during the last tick, so the UI can show
    // "+produced / -consumed". Recomputed each OnTick.
    public Dictionary<Good, float> PlayerProducedLastTick { get; } = new();
    public Dictionary<Good, float> PlayerConsumedLastTick { get; } = new();

    // M5: the player's *desired* demand per good last tick (pops want grain, workshops want ore) — the full
    // appetite, NOT just what was satisfied, so unmet demand still pushes the price up. Drives the dynamic
    // price multiplier; recomputed each OnTick.
    public Dictionary<Good, float> PlayerDemandLastTick { get; } = new();

    // Per-tick production events (tile, good, amount) so the Godot view can flash a fading "+N" on the
    // source building (M4.5 sub-rung 4 — see where & why a gain happened). Core stays engine-free: this
    // is plain data (Vector2I only, no signals) that the view drains after each OnTick. Recomputed each tick.
    public readonly List<(Vector2I Tile, Good Good, float Amount)> ProducedThisTick = new();

    // M6: the trade backbone (the MST connecting capitals) as TradeRoute objects, handed in from the Godot
    // layer via SetTradeGraph. Empty until set, so Core stays testable with an injected graph.
    private readonly List<TradeRoute> _tradeRoutes = new();

    // Read-only view of the trade backbone for the Godot layer (draw routes; color by flow/capacity).
    public IReadOnlyList<TradeRoute> TradeRoutes => _tradeRoutes;

    // M6 phase 3: pairwise diplomatic stance between nations, keyed by an ordered capital pair so a<->b is one
    // entry. Defaults to Neutral. Live political state — not serialized yet (resets to Neutral on load).
    private readonly Dictionary<(Vector2I, Vector2I), DiplomaticStance> _relations = new();

    // M6: per-tick inter-nation trade events (seller capital → buyer capital, good, amount, unit price) so the
    // view can later draw real flows. Plain data (Vector2I capitals only). Recomputed each tick.
    public readonly List<(Vector2I From, Vector2I To, Good Good, float Amount, float Price)> TradedThisTick = new();

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
    // Public so the HUD can label the Build-Railroad button and the spend flash can read the price.
    // LR1: cheap, because the player now has to lay the WHOLE network by hand (no free MST backbone) — a
    // road should be an easy, frequent click, not a hard purchase. Tuned for real in LR5.
    public const float RailroadCost = 2f;

    // M5 demand-sink tunables (M7-placeholders). The player's pops eat GrainPerPopPerTick each tick; demand
    // (= Population * rate = 0.5 grain/tick) is sized to roughly one farm's throttled output (1 unit every
    // other tick), so "feed the pops vs. sell the grain" is a real allocation choice from the first build.
    private const float StartingPopulation  = 5f;
    private const float GrainPerPopPerTick   = 0.1f;
    // M5: a flat poll tax on the player's pops — TaxPerInterval gold into the treasury every
    // TaxIntervalTicks ticks, a steady government income on top of selling goods (placeholders, M7). Public
    // so the HUD can label the cadence.
    public  const float TaxPerInterval   = 50f;
    public  const int   TaxIntervalTicks = 10;
    // M5 dynamic-price clamp: a good's price swings between half and triple its base as scarcity moves.
    private const float MinPriceMultiplier = 0.5f;
    private const float MaxPriceMultiplier = 3f;
    // A good is "balanced" (price at base) when the stockpile holds about this many ticks of demand; less
    // in stock ⇒ scarce ⇒ price climbs toward the ceiling, more ⇒ glut ⇒ price sinks toward the floor.
    private const float PriceBufferTicks = 5f;

    // M6 trade tunables (M7 placeholders). Goods auto-arbitrage cheaper-nation → dearer-nation along the MST:
    // one unit per route/good/tick, but only when the price gap clears the distance-scaled transport cost —
    // so near nations trade on small gaps and far nations only on big ones (geography starts to matter).
    private const float TradeUnit        = 1f;
    private const float TradeCostPerTile = 0.02f;

    // M6 phase 2: per-route throughput. A route moves at most BaseRouteCapacity units/tick (the always-present
    // MST backbone) plus CapacityPerRail for every player railroad tile laid along it — so building track on a
    // route is a "connector road" that raises its capacity. A low base makes capacity bite: a sole supplier
    // can't flood a distant market until the player invests in the road. Capacity is derived live from the
    // (persisted) Railroads set, so it needs no extra save state.
    private const float BaseRouteCapacity = 1f;
    private const float CapacityPerRail   = 0.5f;

    // M6 phase 3: a Neutral importer levies this tariff (fraction of a good's base price) on imports — a pure
    // barrier that raises the price gap a trade must clear (it suppresses imports / denies a rival export
    // income). Friendly = 0 (free trade); Hostile = full blockade (handled in RunTrade).
    private const float NeutralTariffRate = 0.25f;

    // M4: each good's *base* sale value. Tools is worth more than the Ore a Workshop consumes to make it
    // (1 Ore → 1 Tools), so refining Ore→Tools and selling Tools out-earns selling raw Ore — the payoff that
    // makes the M3 build choice worth it and closes the build → produce → sell → reinvest loop.
    // M5: the live price is this base times a per-good scarcity multiplier (see _priceMultiplier).
    private static readonly Dictionary<Good, float> BasePrice = new()
    {
        { Good.Grain, 1f },
        { Good.Ore,   2f },
        { Good.Tools, 6f },
    };

    // M6: a good's live price in a given nation = base × that nation's scarcity multiplier (1 = base price:
    // the value before the first tick and whenever a good has no local demand). >1 when local demand
    // outstrips local stock (a shortage drives the price up), <1 on a glut. Different nations price the same
    // good differently — the gradient trade flows along.
    public float PriceOf(Nation nation, Good good) =>
        BasePrice.GetValueOrDefault(good) * (nation?.PriceMultiplier.GetValueOrDefault(good, 1f) ?? 1f);

    // The player's local price — what the HUD shows and what SellAll transacts at.
    public float PriceOf(Good good) => PriceOf(PlayerNation, good);

    // Live player prices for the HUD (base × the player nation's current multiplier).
    public IReadOnlyDictionary<Good, float> Prices
    {
        get
        {
            var live = new Dictionary<Good, float>();
            foreach (var kvp in BasePrice) live[kvp.Key] = PriceOf(kvp.Key);
            return live;
        }
    }

    // Lifetime treasury the player has earned by selling EACH good — a breakdown so it's legible where
    // income comes from (refined Tools vs raw Ore vs Grain), not just a lump sum. Display stat for the
    // UI; the real state is the Treasury, which is what the save persists, so this isn't serialized and
    // resets on load.
    public Dictionary<Good, float> IncomeByGood { get; } = new();
    public float IncomeOf(Good good) => IncomeByGood.GetValueOrDefault(good);

    // M5: lifetime gold collected from the pop tax — a display stat (the real state is the Treasury it
    // feeds, which the save persists), so it isn't serialized and resets on load like IncomeByGood.
    public float TaxCollected { get; private set; }
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

    public Economy(Dictionary<Vector2I, Vector2I> nationsDictionary, List<Vector2I> capitals, int worldSeed)
    {
        // 1. Thin nations: invert tile -> capital into capital -> tiles.
        foreach (var kvp in nationsDictionary)
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
        // from the seed. Every nation starts with the same treasury (one extractor's worth).
        for (int i = 0; i < capitals.Count; i++)
            if (Nations.TryGetValue(capitals[i], out Nation n))
            {
                n.Owner = i;
                n.Treasury = StartingTreasury;
                n.Population = StartingPopulation; // M6: every nation has pops (demand + tax base)
                if (n.IsPlayer) PlayerNation = n;
            }

        // 2. Tile-bound deposits, deterministic from a sub-seed derived from the one world seed.
        PlaceDeposits(nationsDictionary, worldSeed);
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
    public bool TryBuildWorkshop(Vector2I tile) => TryBuild(tile, Building.Of(BuildingType.Workshop), PlayerNation);

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

    // LR1: remove one of the player's road tiles — the "cut a road" lever. No refund (track is cheap, and
    // undoing a misclick shouldn't pay out). False if there was no road there. Callers rebuild the road
    // routes afterwards so a severed link stops carrying trade.
    public bool RemoveRailroad(Vector2I tile) => Railroads.Remove(tile);

    // M4.5 sub-rung 2: player builds the matching extractor on a deposit tile — Mine on Ore, Farm on
    // Grain. Raw goods are build-gated: no extractor on the deposit → no output. The type is auto-picked
    // from the deposit so the UI needs no Mine-vs-Farm choice. No-op (false) if the tile has no deposit.
    public bool TryBuildExtractor(Vector2I tile) => TryBuildExtractor(tile, PlayerNation);

    // M6: build the matching Mine/Farm on a deposit tile for a given nation (the player via map click, or an
    // AI via auto-build). No-op if the tile has no deposit.
    private bool TryBuildExtractor(Vector2I tile, Nation builder)
    {
        if (!TileDeposits.TryGetValue(tile, out Deposit deposit)) return false; // extractors need a deposit
        BuildingType type = deposit.Good == Good.Ore ? BuildingType.Mine : BuildingType.Farm;
        return TryBuild(tile, Building.Of(type), builder);
    }

    // Shared build path: valid only on a land tile `builder` owns that has no building yet, and only if the
    // builder can afford it. Charges the builder's treasury and places the building. False (no-op) otherwise.
    // (The player can't build on AI land — they don't own those tiles; the ownership check enforces it.)
    private bool TryBuild(Vector2I tile, Building building, Nation builder)
    {
        if (builder == null) return false;
        if (!TileNation.TryGetValue(tile, out Nation owner) || owner != builder) return false; // builder must own it
        if (TileBuildings.ContainsKey(tile)) return false;                  // tile already has a building
        if (builder.Treasury < building.Cost) return false;                 // can't afford

        builder.Treasury -= building.Cost;
        TileBuildings[tile] = building;
        return true;
    }

    // M4: sell the player's entire stockpile of `good` at its (local) price, moving the proceeds into the
    // treasury. Returns the revenue (0 if there's nothing to sell). This is the manual "dump to the abstract
    // market" action, kept alongside M6's automatic inter-nation trade (the auto/manual split is M7's call).
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
    // produced the same tick. M5 adds a third pass — pops consume grain — and recomputes scarcity prices.
    // M6: the AI develop (auto-build), every nation taxes + consumes, and goods trade between nations.
    public long OnTick()
    {
        tickCount++;
        PlayerProducedLastTick.Clear();
        PlayerConsumedLastTick.Clear();
        PlayerDemandLastTick.Clear();
        ProducedThisTick.Clear();
        TradedThisTick.Clear();

        // M6: the AI develop their territory — each affordable AI nation builds one extractor per tick on its
        // own deposits. Runs before production so a fresh extractor can produce this tick.
        RunAiBuild();

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

            // M5: a workshop *wants* its inputs whether or not it can run — record that desired demand so an
            // ore shortage still lifts ore's price (the price reads appetite, not just satisfied consumption).
            if (nation.IsPlayer)
                foreach (Good input in building.Inputs)
                    AddFlow(PlayerDemandLastTick, input, building.Rate);

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

        // Pass 3 — demand sink (M5/M6): every nation's pops eat grain. They *want* the full Population-scaled
        // amount (recorded as demand so an unmet appetite still drives grain's price up), but can only eat
        // what's on hand — the stockpile floors at 0. An empty grain stockpile + a high grain price is the
        // visible "shortage" a nation solves by building more farms (or importing). No starvation penalty yet.
        foreach (Nation nation in Nations.Values)
        {
            if (nation.Population <= 0f) continue;
            float desired = nation.Population * GrainPerPopPerTick;

            nation.Stockpile.TryGetValue(Good.Grain, out float onHand);
            float eaten = System.Math.Min(desired, onHand);
            if (eaten > 0f) nation.AddGood(Good.Grain, -eaten);

            if (nation.IsPlayer) // the HUD shows the player's grain appetite + drain
            {
                AddFlow(PlayerDemandLastTick, Good.Grain, desired);
                if (eaten > 0f) AddFlow(PlayerConsumedLastTick, Good.Grain, eaten);
            }
        }

        // M5/M6: poll tax — a steady government income from each nation's pops, TaxPerInterval gold every
        // TaxIntervalTicks ticks (no pops ⇒ no tax). Lets a nation earn without selling, so an early grain
        // shortage isn't a dead end (save up for a farm / imports). The AI's tax is what funds their building.
        if (tickCount % TaxIntervalTicks == 0)
            foreach (Nation nation in Nations.Values)
            {
                if (nation.Population <= 0f) continue;
                if (!nation.IsPlayer && !EnableAiAutonomy) continue; // inert AI don't collect tax
                nation.Treasury += TaxPerInterval;
                if (nation.IsPlayer) TaxCollected += TaxPerInterval; // a player HUD stat
            }

        // M6: recompute every nation's per-good scarcity price from its own local demand + stock.
        RecomputePriceMultipliers();

        // M6: goods auto-arbitrage between connected nations (cheaper → dearer) along the trade backbone.
        RunTrade();
        return tickCount;
    }

    // M6: set every nation's per-good price multiplier from its own scarcity — a good is balanced
    // (multiplier 1, base price) when the nation holds ~PriceBufferTicks worth of its demand; a low stock
    // relative to demand drives it toward the ceiling (shortage), a high stock toward the floor (glut). No
    // local demand ⇒ multiplier 1, which keeps the M4 sell math intact for un-demanded goods (e.g. Tools).
    private void RecomputePriceMultipliers()
    {
        foreach (Nation nation in Nations.Values)
        {
            Dictionary<Good, float> demand = DemandOf(nation);
            foreach (Good good in BasePrice.Keys)
            {
                float d = demand.GetValueOrDefault(good);
                if (d <= 0f) { nation.PriceMultiplier[good] = 1f; continue; }

                float stock = nation.Stockpile.GetValueOrDefault(good);
                float ratio = (d * PriceBufferTicks) / System.Math.Max(stock, 1f);
                nation.PriceMultiplier[good] = System.Math.Clamp(ratio, MinPriceMultiplier, MaxPriceMultiplier);
            }
        }
    }

    // A nation's desired demand per good this tick: its pops want grain, and each of its workshops wants its
    // inputs (whether or not they can run — unmet appetite still lifts the price).
    private Dictionary<Good, float> DemandOf(Nation nation)
    {
        var demand = new Dictionary<Good, float>();
        if (nation.Population > 0f) AddFlow(demand, Good.Grain, nation.Population * GrainPerPopPerTick);
        foreach (var (tile, building) in TileBuildings)
        {
            if (building.Inputs.Length == 0) continue;
            if (TileNation.GetValueOrDefault(tile) != nation) continue;
            foreach (Good input in building.Inputs) AddFlow(demand, input, building.Rate);
        }
        return demand;
    }

    // M6: each AI nation develops one extractor per tick on its own deposits when it can afford one. Greedy +
    // deterministic (nations in Owner order, lowest-coordinate un-built owned deposit first) so saves/replays
    // match. Treasury-gated, so AI grow organically off tax income. Extractors only — no AI refining yet.
    private void RunAiBuild()
    {
        if (!EnableAiAutonomy) return;
        foreach (Nation nation in Nations.Values.OrderBy(n => n.Owner))
        {
            if (nation.IsPlayer) continue;
            if (nation.Treasury < Building.ExtractorCost) continue;

            Vector2I? target = null;
            foreach (Vector2I tile in TileDeposits.Keys)
            {
                if (TileNation.GetValueOrDefault(tile) != nation) continue;
                if (TileBuildings.ContainsKey(tile)) continue;
                if (target == null || tile.X < target.Value.X ||
                    (tile.X == target.Value.X && tile.Y < target.Value.Y))
                    target = tile;
            }
            if (target.HasValue) TryBuildExtractor(target.Value, nation);
        }
    }

    // M6: hand in the trade backbone — the MST over capitals (edge endpoints are indices into `capitals`),
    // stored as nation pairs + distance so OnTick's trade pass can route goods. Called from the Godot layer
    // after worldgen; tests inject a small graph directly. Safe to call before/again — it rebuilds the list.
    public void SetTradeGraph(List<PrimsAlgorithmSimple.Edge> mstEdges, List<Vector2I> capitals,
        List<HashSet<Vector2I>> corridors = null)
    {
        _tradeRoutes.Clear();
        if (mstEdges == null || capitals == null) return;
        for (int i = 0; i < mstEdges.Count; i++)
        {
            var edge = mstEdges[i];
            if (edge.From < 0 || edge.From >= capitals.Count || edge.To < 0 || edge.To >= capitals.Count) continue;
            if (Nations.TryGetValue(capitals[edge.From], out Nation a) &&
                Nations.TryGetValue(capitals[edge.To], out Nation b))
            {
                HashSet<Vector2I> corridor = (corridors != null && i < corridors.Count) ? corridors[i] : null;
                _tradeRoutes.Add(new TradeRoute(a, b, (float)edge.Weight, corridor));
            }
        }
    }

    // LR1: rebuild the trade backbone from the player's ROAD network instead of the old MST. Two capitals get
    // a trade route exactly when a contiguous path of road tiles connects them (RoadGraph BFS), and the
    // route's Distance is that path's length so a longer haul costs more transport. This replaces the auto-MST
    // entirely: a fresh game has no roads and therefore no inter-nation trade until the player lays one, and
    // pulling a tile out of a link drops the route (the "cut a road" alive check). Call after any road change
    // (build / remove / load) and once after worldgen. Deterministic — capitals sorted — so saves/replays
    // match. RunTrade still consumes _tradeRoutes unchanged; only their *source* moved from MST to roads.
    public void RebuildRoadRoutes()
    {
        _tradeRoutes.Clear();
        var graph = new RoadGraph(Railroads);
        var capitals = Nations.Keys.OrderBy(c => c.X).ThenBy(c => c.Y).ToList();
        for (int i = 0; i < capitals.Count; i++)
            for (int j = i + 1; j < capitals.Count; j++)
            {
                int distance = graph.ShortestPathLength(capitals[i], capitals[j]);
                if (distance < 0) continue; // not road-connected ⇒ no route ⇒ no trade between them
                _tradeRoutes.Add(new TradeRoute(Nations[capitals[i]], Nations[capitals[j]], distance, null));
            }
    }

    // M6 phase 2/3: find the route linking two capitals (undirected), or null.
    public TradeRoute FindRoute(Vector2I capA, Vector2I capB)
    {
        foreach (TradeRoute r in _tradeRoutes)
            if (r.Connects(capA, capB)) return r;
        return null;
    }

    // M6 phase 2: cut (or restore) a route. A blocked route carries nothing, so the far side's imported good
    // runs dry and its price diverges (the "cut a road" alive check). Phase 3 also blockades via hostility.
    public bool SetRouteBlocked(Vector2I capA, Vector2I capB, bool blocked)
    {
        TradeRoute r = FindRoute(capA, capB);
        if (r == null) return false;
        r.Blocked = blocked;
        return true;
    }

    // M6 phase 3 — diplomacy. Order a capital pair so (a,b) and (b,a) map to one relations entry.
    private static (Vector2I, Vector2I) RelationKey(Vector2I a, Vector2I b) =>
        (a.X < b.X || (a.X == b.X && a.Y <= b.Y)) ? (a, b) : (b, a);

    public DiplomaticStance GetRelation(Vector2I capA, Vector2I capB) =>
        _relations.GetValueOrDefault(RelationKey(capA, capB), DiplomaticStance.Neutral);

    public void SetRelation(Vector2I capA, Vector2I capB, DiplomaticStance stance) =>
        _relations[RelationKey(capA, capB)] = stance;

    // The player's stance toward another nation — the diplomatic lever: Friendly = free trade (cheap imports
    // but enriches the partner), Neutral = tariffed trade, Hostile = blockade (deny a rival all trade).
    public void SetPlayerStance(Vector2I otherCapital, DiplomaticStance stance)
    {
        if (PlayerNation != null) SetRelation(PlayerNation.Capital, otherCapital, stance);
    }

    // M6: along each route, every good flows one unit from the cheaper nation to the dearer one when the price
    // gap clears the route's transport cost and the buyer can pay — a pure gold+goods transfer (no gold
    // created), so a sole supplier's income is drained straight from its customers' treasuries. Pops have
    // already eaten this tick (Pass 3), so only post-consumption surplus is exported. Deterministic: routes in
    // MST order, goods in enum order.
    private void RunTrade()
    {
        foreach (TradeRoute route in _tradeRoutes)
        {
            route.FlowLastTick = 0f;
            // M6 phase 2: the route's per-tick throughput (backbone + player connector rails; 0 if blocked).
            float capacity = route.CapacityWith(Railroads, BaseRouteCapacity, CapacityPerRail);
            route.CapacityLastTick = capacity; // surfaced for the view's congestion coloring
            if (capacity < TradeUnit) continue; // cut, or below one unit of throughput

            Nation a = route.A, b = route.B;
            if (GetRelation(a.Capital, b.Capital) == DiplomaticStance.Hostile) continue; // blockade — no trade
            float transportCost = TradeCostPerTile * route.Distance;
            foreach (Good good in BasePrice.Keys)
            {
                if (route.FlowLastTick + TradeUnit > capacity) break; // route saturated this tick

                float priceA = PriceOf(a, good);
                float priceB = PriceOf(b, good);
                Nation seller = priceA <= priceB ? a : b;
                Nation buyer  = priceA <= priceB ? b : a;

                // M6 phase 3: the buyer's import tariff (from its stance toward the seller) — a pure barrier that
                // raises the price gap a trade must clear. Not a gold transfer: the buyer nation IS its own
                // government, so taxing its own imports would be a wash (and minting it would create money).
                float tariff = TariffRate(buyer, seller) * BasePrice.GetValueOrDefault(good);

                if (PriceOf(buyer, good) - PriceOf(seller, good) <= transportCost + tariff) continue; // gap too small
                if (seller.Stockpile.GetValueOrDefault(good) < TradeUnit) continue;                   // nothing to ship

                float unitPrice = (PriceOf(seller, good) + PriceOf(buyer, good)) / 2f;                // split the surplus
                float cost = TradeUnit * unitPrice;
                if (buyer.Treasury < cost) continue;                                                  // can't afford it

                seller.AddGood(good, -TradeUnit);
                buyer.AddGood(good, TradeUnit);
                seller.Treasury += cost;
                buyer.Treasury  -= cost;
                route.FlowLastTick += TradeUnit;
                TradedThisTick.Add((seller.Capital, buyer.Capital, good, TradeUnit, unitPrice));
            }
        }
    }

    // M6 phase 3: the buyer's import tariff toward the seller, a fraction of base price (Friendly free-trades,
    // Neutral levies NeutralTariffRate; Hostile never reaches here — it's blockaded in RunTrade).
    private float TariffRate(Nation buyer, Nation seller) => GetRelation(buyer.Capital, seller.Capital) switch
    {
        DiplomaticStance.Friendly => 0f,
        DiplomaticStance.Neutral  => NeutralTariffRate,
        _ => 0f,
    };

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
