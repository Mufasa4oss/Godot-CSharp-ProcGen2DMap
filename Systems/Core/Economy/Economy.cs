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

    // LR2+: the physical goods layer. Production drops the player's output onto the producing tile;
    // the Dispatcher auto-dispatches haulers that carry surplus along the road network to the capital.
    // Goods have a location and a travel time — nothing teleports. Not serialized (in-flight haulers and
    // at-rest piles regenerate from production on load).
    public TileInventory TileInventory { get; } = new();
    public Dispatcher Dispatcher { get; } = new();
    public IReadOnlyList<Hauler> Haulers => Dispatcher.Haulers;

    // The player's nation (Owner 0 = the first placed capital). Cached; ownership doesn't change in L1.
    public Nation PlayerNation { get; private set; }

    // M6: when true the non-player nations are live economic actors (their pops collect tax + pay upkeep). A
    // single switch so extraction-isolation tests can keep them inert (mirrors the `Population = 0f` pattern).
    public bool EnableAiAutonomy { get; set; } = true;

    // Per-good flow into / out of the PLAYER's stockpile during the last tick, so the UI can show
    // "+produced / -consumed". Recomputed each OnTick.
    public Dictionary<Good, float> PlayerProducedLastTick { get; } = new();
    public Dictionary<Good, float> PlayerConsumedLastTick { get; } = new();

    // LR3: per-good amount the internal market bought from the capital's delivered goods last tick (the home
    // economy "selling to your pops"). Drives the HUD's live income readout. Recomputed each OnTick.
    public Dictionary<Good, float> InternalSoldLastTick { get; } = new();

    // LR4: per-good amount EXPORTED from the capital to road-connected neighbours last tick (the foreign half of
    // income). Recomputed each OnTick.
    public Dictionary<Good, float> ExportSoldLastTick { get; } = new();

    // M5: the player's *desired* demand per good last tick (pops want grain, workshops want ore) — the full
    // appetite, NOT just what was satisfied, so unmet demand still pushes the price up. Drives the dynamic
    // price multiplier; recomputed each OnTick.
    public Dictionary<Good, float> PlayerDemandLastTick { get; } = new();

    // Per-tick production events (tile, good, amount) so the Godot view can flash a fading "+N" on the
    // source building (M4.5 sub-rung 4 — see where & why a gain happened). Core stays engine-free: this
    // is plain data (Vector2I only, no signals) that the view drains after each OnTick. Recomputed each tick.
    public readonly List<(Vector2I Tile, Good Good, float Amount)> ProducedThisTick = new();

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

    // Pop dynamics: every nation opens with StartingGrain in the larder. Each tick the population responds to the
    // grain stockpile's *trajectory*, not its level — a genuine surplus (the larder ended the tick higher than it
    // started) grows pops by PopGrowthRate; an empty larder (famine) shrinks them by PopStarveRate; a larder that
    // merely holds steady or draws down but isn't yet empty leaves the population unchanged. Both rates compound
    // per tick. Placeholders, tuned later.
    private const float StartingGrain = 10f;
    private const float PopGrowthRate = 0.01f; // +1%/tick when grain is in surplus (stockpile rose this tick)
    private const float PopStarveRate = 0.05f; // -5%/tick when grain hit zero (famine)
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

    // LR4 export tunables. One unit exported per neighbour/good/tick; transport cost scales with road path
    // length, keeping geography meaningful (far buyers cost more to reach than near ones).
    private const float TradeUnit        = 1f;
    private const float TradeCostPerTile = 0.02f;

    // LR3 internal market: the player's pops/economy BUY goods the haulers physically delivered to the capital,
    // paying the treasury — the new income faucet that replaces the manual Sell button. Demand is capped per
    // tick (grain scales with pops; raws/refined have a flat industrial+luxury appetite), so income is bounded
    // by what you actually produce and DELIVER (no roads ⇒ goods strand at the mine ⇒ no market income, only
    // tax). Whatever the internal market doesn't absorb stays in the capital inventory for export (LR4).
    private const float InternalRawDemandPerTick = 1f; // ore/tools the home market soaks up each tick

    // LR5 upkeep (the missing gold sink). Charged every TaxIntervalTicks: each building a nation owns costs
    // UpkeepPerBuilding, and every road tile the player laid costs UpkeepPerRoad. Sized well under the poll tax
    // (TaxPerInterval) so a lean operation still nets positive, but a sprawling one stops being free — the
    // pressure that keeps gold meaningful. Public so the HUD can explain the cadence.
    public const float UpkeepPerBuilding = 5f;
    public const float UpkeepPerRoad     = 0.5f;

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

    // LR5: the upkeep the player paid at the last interval — a HUD stat so the gold sink is legible (resets on
    // load like the other display stats).
    public float UpkeepPaidLastInterval { get; private set; }
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

        // Every nation opens with a small grain larder so its pops don't starve before the first farm feeds them.
        // The player's larder is physical grain at their capital (TileInventory); AI use the abstract Stockpile.
        foreach (Nation nation in Nations.Values)
        {
            if (nation.IsPlayer)
                TileInventory.Add(nation.Capital, Good.Grain, StartingGrain);
            else
                nation.AddGood(Good.Grain, StartingGrain);
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

    // LR6: a Granary/Warehouse is a pure storage building — no recipe, just a per-good capacity that gates
    // and caps the nation's raw extraction (see the Pass 1 capacity check in OnTick). Built on any owned,
    // empty land tile, same as a Workshop — it doesn't need to sit on a deposit or next to an extractor.
    public bool TryBuildGranary(Vector2I tile) => TryBuildGranary(tile, PlayerNation);
    public bool TryBuildWarehouse(Vector2I tile) => TryBuildWarehouse(tile, PlayerNation);
    private bool TryBuildGranary(Vector2I tile, Nation builder) => TryBuild(tile, Building.Of(BuildingType.Granary), builder);
    private bool TryBuildWarehouse(Vector2I tile, Nation builder) => TryBuild(tile, Building.Of(BuildingType.Warehouse), builder);

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

    // LR3: the manual Sell action (M4's `SellAll`) is GONE. Income now comes from the internal market
    // (RunInternalMarket) buying the goods your haulers physically deliver to the capital — you earn by
    // supplying your population, not by dumping a stockpile into an abstract market with a button.

    // M3: advance the tick and run production in two passes so workshops can consume what extractors
    // produced the same tick. M5 adds a third pass — pops consume grain — and recomputes scarcity prices.
    // Every nation taxes + consumes; the player exports its surplus to road-connected buyer nations.
    public long OnTick()
    {
        tickCount++;
        PlayerProducedLastTick.Clear();
        PlayerConsumedLastTick.Clear();
        PlayerDemandLastTick.Clear();
        InternalSoldLastTick.Clear();
        ExportSoldLastTick.Clear();
        ProducedThisTick.Clear();
        TradedThisTick.Clear();

        // Pop dynamics: snapshot each nation's grain BEFORE this tick's production/eating/trade so the population
        // pass at the end can tell a real surplus (the larder rose) from a drawdown (it fell).
        // Player grain lives in TileInventory at their capital; AI grain lives in their Stockpile.
        var grainAtTickStart = new Dictionary<Nation, float>();
        foreach (Nation nation in Nations.Values)
            grainAtTickStart[nation] = nation.IsPlayer
                ? TileInventory.AmountAt(nation.Capital, Good.Grain)
                : nation.Stockpile.GetValueOrDefault(Good.Grain);

        // Pass 1 — raw extractors (no inputs): build-gated (only a player-built Mine/Farm produces) and
        // throttled to keep raw goods scarce (M4.5 sub-rung 3) — a flat 1 unit, and only on alternating
        // ticks — so a deposit isn't effectively free and the one build choice bites. The deposit's
        // reserve still depletes by what's pulled; its Rate is intentionally bypassed for raws.
        // LR6: also gated on nation-wide storage (Granary/Warehouse) — no storage building of the matching
        // type anywhere in the nation's territory means the extractor can't operate at all, and once stored
        // goods reach that storage's capacity, extraction pauses (the reserve is left untouched, not wasted).
        bool rawTickActive = tickCount % 2 == 0;
        foreach (var (tile, building) in TileBuildings)
        {
            if (building.Inputs.Length != 0 || building.Outputs.Length == 0) continue; // skip workshops + storage buildings
            if (!rawTickActive) continue;
            if (!TileDeposits.TryGetValue(tile, out Deposit deposit)) continue;
            if (deposit.Reserve <= 0f) continue;

            Nation nation = TileNation[tile];
            Good good = building.Outputs[0];
            float capacity = StorageCapacityFor(nation, good);
            if (capacity <= 0f) continue;                        // no matching storage building built yet
            if (TotalStored(nation, good) >= capacity) continue; // storage full — pause, reserve stays put

            float amount = System.Math.Min(1f, deposit.Reserve);
            deposit.Reserve -= amount; // extraction is physical — the reserve depletes whether or not a road exists
            ProducedThisTick.Add((tile, good, amount)); // the "+N" flash fires at the mine regardless (it was dug up)

            if (nation.IsPlayer)
            {
                TileInventory.Add(tile, good, amount); // physical goods drop on the mine/farm tile
                AddFlow(PlayerProducedLastTick, good, amount);
            }
            else
            {
                nation.AddGood(good, amount); // AI use the abstract Stockpile pool
            }
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
                float have = nation.IsPlayer
                    ? TileInventory.AmountAt(nation.Capital, input)
                    : nation.Stockpile.GetValueOrDefault(input);
                if (have < building.Rate) { canRun = false; break; }
            }
            if (!canRun) continue;

            foreach (Good input in building.Inputs)
            {
                if (nation.IsPlayer)
                {
                    TileInventory.Add(nation.Capital, input, -building.Rate); // consume delivered inputs
                    AddFlow(PlayerConsumedLastTick, input, building.Rate);
                }
                else
                {
                    nation.AddGood(input, -building.Rate);
                }
            }
            foreach (Good output in building.Outputs)
            {
                ProducedThisTick.Add((tile, output, building.Rate));
                if (nation.IsPlayer)
                {
                    AddFlow(PlayerProducedLastTick, output, building.Rate);
                    TileInventory.Add(tile, output, building.Rate); // physical output on the workshop tile
                }
                else
                {
                    nation.AddGood(output, building.Rate);
                }
            }
        }

        // Pass 3 — demand sink (M5/M6): AI nations' pops eat grain from their Stockpile. The player's pops
        // are handled by RunInternalMarket (they "buy" grain physically delivered to the capital).
        foreach (Nation nation in Nations.Values)
        {
            if (nation.Population <= 0f) continue;
            if (nation.IsPlayer) continue; // player grain handled by RunInternalMarket + TileInventory

            float desired = nation.Population * GrainPerPopPerTick;
            nation.Stockpile.TryGetValue(Good.Grain, out float onHand);
            float eaten = System.Math.Min(desired, onHand);
            if (eaten > 0f) nation.AddGood(Good.Grain, -eaten);
        }

        // M5/M6: poll tax — a steady government income from each nation's pops, TaxPerInterval gold every
        // TaxIntervalTicks ticks (no pops ⇒ no tax). Lets a nation earn without selling, so an early grain
        // shortage isn't a dead end (save up for a farm / imports). The AI's tax is what funds their building.
        if (tickCount % TaxIntervalTicks == 0)
        {
            foreach (Nation nation in Nations.Values)
            {
                if (nation.Population <= 0f) continue;
                if (!nation.IsPlayer && !EnableAiAutonomy) continue; // inert AI don't collect tax
                nation.Treasury += TaxPerInterval;
                if (nation.IsPlayer) TaxCollected += TaxPerInterval; // a player HUD stat
            }

            // LR5: recurring UPKEEP — the gold sink, charged on the same interval so income and outgo pulse
            // together. Each nation pays for what it operates (its buildings; the player also pays per road
            // tile), so an over-built network bleeds the treasury instead of gold only ever climbing.
            RunUpkeep();
        }

        // M6: recompute every nation's per-good scarcity price from its own local demand + stock.
        RecomputePriceMultipliers();

        // LR2+: the physical layer. Roll the trucks already on the road (delivering arrivals, stalling any whose
        // road got cut), then dispatch fresh ones to drain this tick's new surplus toward the player's capital.
        // Advance-before-dispatch so a good delivered to the capital this tick isn't instantly re-shipped.
        Dispatcher.Advance(TileInventory, Railroads);
        Dispatcher.Dispatch(TileInventory, Railroads, SinkOf);

        // LR3: the internal market — your pops buy the goods the haulers just delivered to your capital, paying
        // the treasury. This is the income faucet that replaced the manual Sell button: earn by *supplying your
        // population*, which requires physically delivering (build the road or the goods strand at the mine).
        RunInternalMarket();

        // LR4: export — whatever the home market left over at the capital is sold to road-connected neighbours
        // at their price, docked by a distance-based transport cost. Income layered: home first, foreign second.
        RunExport();

        // Pop dynamics: with all of this tick's grain movement settled, grow/shrink each nation's population from
        // how its larder fared — surplus grows, famine shrinks, holding-or-drawing-down stays put.
        UpdatePopulations(grainAtTickStart);
        return tickCount;
    }

    // Grow or shrink each nation's population from its grain trajectory this tick (see the pop-dynamics tunables).
    // A famine (grain ended at zero) shrinks it by PopStarveRate; a surplus (the grain stockpile ended higher than
    // it started) grows it by PopGrowthRate; a steady or merely-drawing-down-but-non-empty larder leaves it
    // unchanged. Applies to every nation with pops, matching the grain demand sink (which also ignores AI autonomy).
    private void UpdatePopulations(Dictionary<Nation, float> grainAtTickStart)
    {
        foreach (Nation nation in Nations.Values)
        {
            if (nation.Population <= 0f) continue;
            float grainNow = nation.IsPlayer
                ? TileInventory.AmountAt(nation.Capital, Good.Grain)
                : nation.Stockpile.GetValueOrDefault(Good.Grain);
            float grainStart = grainAtTickStart.GetValueOrDefault(nation);

            if (grainNow <= 0f)
                nation.Population *= 1f - PopStarveRate;   // famine: the larder is empty
            else if (grainNow > grainStart)
                nation.Population *= 1f + PopGrowthRate;    // surplus: the larder grew this tick
            // else steady: holding, or drawing down but not yet empty
        }
    }

    // LR3: sell the player's delivered (capital-inventory) goods to the home market for treasury income, capped
    // per good by local demand so income tracks production+delivery rather than a button press. Anything beyond
    // the home appetite stays in the capital inventory as export surplus (LR4). Priced at the player nation's
    // live scarcity price, and attributed per good (the HUD's income breakdown). Player-only — the AI keep the
    // abstract Stockpile economy.
    private void RunInternalMarket()
    {
        Nation p = PlayerNation;
        if (p == null) return;
        Vector2I capital = p.Capital;

        // Grain demand replaces Pass 3 for the player: pops "buy" grain here instead of eating from Stockpile.
        if (p.Population > 0f)
            AddFlow(PlayerDemandLastTick, Good.Grain, InternalDemandOf(Good.Grain));

        foreach (Good good in BasePrice.Keys)
        {
            float delivered = TileInventory.AmountAt(capital, good);
            if (delivered <= 0f) continue;

            float demand = InternalDemandOf(good);
            float sold = System.Math.Min(delivered, demand);
            if (sold <= 0f) continue;

            float revenue = sold * PriceOf(p, good);
            TileInventory.Add(capital, good, -sold);
            p.Treasury += revenue;
            AddFlow(IncomeByGood, good, revenue);
            AddFlow(InternalSoldLastTick, good, sold);
            if (good == Good.Grain) AddFlow(PlayerConsumedLastTick, Good.Grain, sold);
        }
    }

    // LR3: how much of a good the player's home market will buy per tick. Grain is food (scales with pops);
    // ore + tools are a flat industrial/luxury appetite. Sized small so a real surplus spills over to export.
    private float InternalDemandOf(Good good) =>
        good == Good.Grain ? PlayerNation.Population * GrainPerPopPerTick : InternalRawDemandPerTick;

    // LR4: export the player's leftover delivered surplus (sitting in the capital inventory after the home
    // market) to road-connected neighbour nations. Each connected neighbour buys up to one unit per good per
    // tick at *their* price, less the road transport cost — so a near neighbour is the best customer. A real
    // gold+goods transfer (the buyer pays from its treasury and receives the good, so its own prices respond), drained from
    // the same capital surplus the home market didn't take. Player-only; deterministic (neighbours sorted).
    private void RunExport()
    {
        Nation seller = PlayerNation;
        if (seller == null) return;
        Vector2I cap = seller.Capital;

        var graph = new RoadGraph(Railroads);
        List<Nation> buyers = ConnectedBuyers(cap, graph);
        if (buyers.Count == 0) return;

        foreach (Good good in BasePrice.Keys)
            foreach (Nation buyer in buyers)
            {
                if (TileInventory.AmountAt(cap, good) < TradeUnit) break; // capital's surplus of this good is spent
                TrySellUnitTo(seller, cap, buyer, graph, good);
            }
    }

    // LR6: the player's manual bulk export — clicking a built Granary/Warehouse lets them pick an amount to
    // sell instead of waiting for the automatic TradeUnit trickle. Reuses the same buyer-routing and transport
    // math as RunExport (via TrySellUnitTo/ConnectedBuyers), except a single buyer
    // can absorb several units if they can afford it, instead of at most one per tick. Returns how much was
    // actually sold — the request is clamped to what's physically at the capital and to buyer affordability.
    public float TryExportManual(Good good, float amount)
    {
        Nation seller = PlayerNation;
        if (seller == null || amount <= 0f) return 0f;
        Vector2I cap = seller.Capital;

        var graph = new RoadGraph(Railroads);
        List<Nation> buyers = ConnectedBuyers(cap, graph);
        if (buyers.Count == 0) return 0f;

        float requested = System.Math.Min(amount, TileInventory.AmountAt(cap, good));
        float sold = 0f;
        foreach (Nation buyer in buyers)
        {
            while (sold < requested && TileInventory.AmountAt(cap, good) >= TradeUnit)
            {
                if (TrySellUnitTo(seller, cap, buyer, graph, good) == null) break; // this buyer's tapped out — next
                sold += TradeUnit;
            }
            if (sold >= requested) break;
        }
        return sold;
    }

    // The road-connected AI nations `seller`'s capital can export to, sorted deterministically (lowest
    // capital coordinate first) so both RunExport and TryExportManual visit buyers in the same order.
    private List<Nation> ConnectedBuyers(Vector2I sellerCapital, RoadGraph graph)
    {
        var buyers = new List<Nation>();
        foreach (Nation n in Nations.Values)
            if (!n.IsPlayer && graph.AreConnected(sellerCapital, n.Capital)) buyers.Add(n);
        buyers.Sort((a, b) => a.Capital.X != b.Capital.X ? a.Capital.X - b.Capital.X : a.Capital.Y - b.Capital.Y);
        return buyers;
    }

    // Shared per-unit sale math for RunExport (automatic) and TryExportManual (player bulk export): try to
    // sell exactly TradeUnit of `good` from the capital to `buyer` at the buyer's price minus the transport
    // cost. Performs the transfer and returns the proceeds on success; null (no-op) if this buyer can't take
    // it right now — the margin doesn't clear costs, or they can't afford it.
    private float? TrySellUnitTo(Nation seller, Vector2I cap, Nation buyer, RoadGraph graph, Good good)
    {
        float transport = TradeCostPerTile * graph.ShortestPathLength(cap, buyer.Capital);
        float proceeds = PriceOf(buyer, good) - transport;                            // what the player nets
        if (proceeds <= 0f) return null;            // the gap doesn't clear costs — not worth exporting
        if (buyer.Treasury < proceeds) return null;  // the buyer can't pay

        TileInventory.Add(cap, good, -TradeUnit);
        buyer.AddGood(good, TradeUnit);             // they receive it (their scarcity price responds)
        seller.Treasury += proceeds;
        buyer.Treasury  -= proceeds;
        AddFlow(IncomeByGood, good, proceeds);
        AddFlow(ExportSoldLastTick, good, TradeUnit);
        TradedThisTick.Add((cap, buyer.Capital, good, TradeUnit, proceeds));
        return proceeds;
    }

    // Where a producer tile's goods should be hauled — its owning nation's capital. Player tiles only
    // (the player owns the road network); AI tiles return null (their goods live in Nation.Stockpile).
    private Vector2I? SinkOf(Vector2I tile)
    {
        if (!TileNation.TryGetValue(tile, out Nation owner) || !owner.IsPlayer) return null;
        return owner.Capital;
    }

    // LR6: public read-only views onto a nation's storage state, for UI (the export panel shows the player's
    // own numbers; kept general so it also works for inspecting an AI nation later if that's ever wanted).
    public float StorageCapacityOf(Nation nation, Good good) => StorageCapacityFor(nation, good);
    public float StoredAmountOf(Nation nation, Good good) => TotalStored(nation, good);

    // LR6: which storage building type gates/caps a given good. Only raw goods have one today (Grain via
    // Granary, Ore via Warehouse); a good with no storage type is uncapped.
    private static BuildingType? StorageTypeFor(Good good) => good switch
    {
        Good.Grain => BuildingType.Granary,
        Good.Ore   => BuildingType.Warehouse,
        _ => null,
    };

    // Sum of StorageCapacity over every storage building of the matching type the nation owns, anywhere in
    // its territory — storage is nation-wide, not per-tile (a Granary can't sit on the same tile as the
    // Farm it serves, since one tile holds one building). Zero means no storage of that type exists yet,
    // which Pass 1 reads as "this extractor can't operate."
    private float StorageCapacityFor(Nation nation, Good good)
    {
        BuildingType? storageType = StorageTypeFor(good);
        if (storageType == null) return float.PositiveInfinity; // no storage type for this good ⇒ uncapped
        float capacity = 0f;
        foreach (Vector2I tile in nation.Tiles)
            if (TileBuildings.TryGetValue(tile, out Building b) && b.Type == storageType)
                capacity += b.StorageCapacity;
        return capacity;
    }

    // How much of a good the nation currently holds, wherever it physically sits: for the player, summed
    // across every owned tile's TileInventory (goods can be mid-haul, at rest on a producer tile, or at the
    // capital); for AI, their abstract Stockpile pool.
    private float TotalStored(Nation nation, Good good)
    {
        if (!nation.IsPlayer) return nation.Stockpile.GetValueOrDefault(good);
        float total = 0f;
        foreach (Vector2I tile in nation.Tiles)
            total += TileInventory.AmountAt(tile, good);
        return total;
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

                float stock = nation.IsPlayer
                    ? TileInventory.AmountAt(nation.Capital, good)
                    : nation.Stockpile.GetValueOrDefault(good);
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

    // LR5: charge each nation upkeep on what it operates — UpkeepPerBuilding per building it owns, plus (for the
    // player) UpkeepPerRoad per road tile laid. Treasury floors at 0 (no debt/bankruptcy in L1 — a broke nation
    // simply can't spend). Mirrors the tax rule: inert AI (autonomy off) don't pay. Deterministic.
    private void RunUpkeep()
    {
        // Count each nation's buildings once.
        var buildingCount = new Dictionary<Nation, int>();
        foreach (Vector2I tile in TileBuildings.Keys)
            if (TileNation.TryGetValue(tile, out Nation owner))
                buildingCount[owner] = buildingCount.GetValueOrDefault(owner) + 1;

        foreach (Nation nation in Nations.Values)
        {
            if (!nation.IsPlayer && !EnableAiAutonomy) continue; // inert AI don't pay (mirrors tax)

            float upkeep = buildingCount.GetValueOrDefault(nation) * UpkeepPerBuilding;
            if (nation.IsPlayer) upkeep += Railroads.Count * UpkeepPerRoad; // the road network is the player's
            if (upkeep <= 0f) continue;

            nation.Treasury -= upkeep;
            if (nation.Treasury < 0f) nation.Treasury = 0f;
            if (nation.IsPlayer) UpkeepPaidLastInterval = upkeep;
        }
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
