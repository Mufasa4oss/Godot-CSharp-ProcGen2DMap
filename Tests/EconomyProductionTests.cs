using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Xunit;

// Production + M3 agency: extractors fill the owning nation's stockpile (depleting Reserve); the player
// spends treasury to build Workshops that convert Ore→Tools. Plain .NET (no engine).
public class EconomyProductionTests
{
    // A square block of land, every tile owned by one capital at (0,0) — that nation is the player.
    private static Dictionary<Vector2I, Vector2I> SquareLand(int size, out Vector2I capital)
    {
        capital = new Vector2I(0, 0);
        var land = new Dictionary<Vector2I, Vector2I>();
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                land[new Vector2I(x, y)] = capital;
        return land;
    }

    // Two side-by-side nations: left half owned by capA (player), right half by capB (AI).
    private static Dictionary<Vector2I, Vector2I> TwoNationLand(int size, out Vector2I capA, out Vector2I capB)
    {
        capA = new Vector2I(0, 0);
        capB = new Vector2I(size - 1, 0);
        var land = new Dictionary<Vector2I, Vector2I>();
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                land[new Vector2I(x, y)] = x < size / 2 ? capA : capB;
        return land;
    }

    private static Vector2I FirstEmptyTile(Economy e, IEnumerable<Vector2I> tiles)
    {
        foreach (var t in tiles)
            if (!e.TileBuildings.ContainsKey(t)) return t;
        throw new InvalidOperationException("no empty tile available");
    }

    // M4.5: extraction is build-gated, so production tests must build an extractor first. Picks the
    // lowest-coordinate player-owned deposit tile (deterministic from the seed) and builds the matching
    // Mine/Farm; bumps treasury so cost never blocks the test. Returns the tile and the good it yields.
    // LR6: also builds the matching Granary/Warehouse (on the capital tile) since Pass 1 won't run an
    // extractor without one — every test using this helper is about the extraction/haul/sell flow, not the
    // storage-gate itself (see EconomyBuildGateTests for that).
    private static (Vector2I tile, Good good) BuildPlayerExtractor(Economy e)
    {
        e.PlayerNation.Treasury += Building.ExtractorCost;
        foreach (Vector2I tile in e.TileDeposits.Keys.OrderBy(t => t.X).ThenBy(t => t.Y))
            if (e.TileNation.TryGetValue(tile, out Nation n) && n.IsPlayer && !e.TileBuildings.ContainsKey(tile))
            {
                Assert.True(e.TryBuildExtractor(tile));
                // LR: output only banks into the stockpile when a road links the deposit to the capital, so
                // lay one — every production test here is about the flow, not the connectivity gate.
                ConnectToCapital(e, tile, n.Capital);
                BuildMatchingStorage(e, e.TileDeposits[tile].Good);
                return (tile, e.TileDeposits[tile].Good);
            }
        throw new InvalidOperationException("no player-owned deposit tile available");
    }

    // LR6: build enough Granary/Warehouse capacity to gate a good's extraction wide open, on empty owned
    // tiles — NOT assumed to be the capital, since the deposit tile BuildPlayerExtractor picked can itself
    // be the capital (the lowest-coordinate owned deposit, which the capital tile can be). Two storage
    // buildings (2 * StorageCapacityPerBuilding = 600) comfortably exceeds any single deposit's max
    // possible reserve (MaxSize 5 * ReservePerTile 100 = 500), so tests that run a deposit fully dry (e.g.
    // Reserve_depletes_and_caps_extractor_output) aren't capped by storage instead. Tops up treasury first
    // so the storage cost never blocks it.
    private static void BuildMatchingStorage(Economy e, Good good)
    {
        BuildingType storageType = good == Good.Ore ? BuildingType.Warehouse : BuildingType.Granary;
        for (int i = 0; i < 2; i++)
        {
            Vector2I tile = e.PlayerNation.Tiles.First(t => !e.TileBuildings.ContainsKey(t));
            e.PlayerNation.Treasury += Building.Of(storageType).Cost;
            bool built = storageType == BuildingType.Granary ? e.TryBuildGranary(tile) : e.TryBuildWarehouse(tile);
            Assert.True(built, "test setup: matching storage building must place somewhere in the nation's territory");
        }
    }

    // LR: lay a contiguous L-shaped road from `tile` to `capital` (walk the column, then the row) so the
    // deposit's output passes the deposit→capital connectivity gate and reaches the national stockpile.
    private static void ConnectToCapital(Economy e, Vector2I tile, Vector2I capital)
    {
        int x = tile.X, y = tile.Y;
        while (x != capital.X) { e.Railroads.Add(new Vector2I(x, y)); x += x < capital.X ? 1 : -1; }
        while (y != capital.Y) { e.Railroads.Add(new Vector2I(x, y)); y += y < capital.Y ? 1 : -1; }
    }

    // ---- Production (build-gated extractors, M4.5) ----

    [Fact]
    public void Built_extractor_output_climbs_every_other_tick()
    {
        var land = SquareLand(12, out var capital);
        var economy = new Economy(land, new List<Vector2I> { capital }, 99);
        Assert.NotEmpty(economy.Deposits);
        economy.PlayerNation.Population = 0f; // isolate extraction from the M5 pop demand sink

        BuildPlayerExtractor(economy);

        // M4.5 throttle: a flat 1 unit, only on alternating ticks — no ProducedThisTick event on odd ticks.
        economy.OnTick(); // tick 1 (odd: throttled)
        Assert.Empty(economy.ProducedThisTick);

        economy.OnTick(); // tick 2 (even: produces)
        Assert.Contains(economy.ProducedThisTick, e => e.Amount == 1f);

        economy.OnTick(); // tick 3 (odd: throttled again)
        Assert.Empty(economy.ProducedThisTick);

        economy.OnTick(); // tick 4 (even: produces again)
        Assert.Contains(economy.ProducedThisTick, e => e.Amount == 1f);
    }

    // LR: without a road, extracted goods strand on the mine tile. Lay a road and haulers carry them to the
    // capital where the internal market earns income. Player-only requirement.
    [Fact]
    public void Extractor_output_strands_without_road_and_delivers_when_roaded()
    {
        var land = SquareLand(12, out var capital);
        var economy = new Economy(land, new List<Vector2I> { capital }, 99);
        economy.PlayerNation.Population = 0f; // isolate extraction from the M5 pop demand sink

        // Build the extractor WITHOUT calling BuildPlayerExtractor (which lays a road for us).
        economy.PlayerNation.Treasury += Building.ExtractorCost;
        Vector2I tile = economy.TileDeposits.Keys
            .Where(t => t != capital && economy.TileNation.TryGetValue(t, out Nation n) && n.IsPlayer)
            .OrderBy(t => t.X).ThenBy(t => t.Y).First();
        Good good = economy.TileDeposits[tile].Good;
        Assert.True(economy.TryBuildExtractor(tile));
        BuildMatchingStorage(economy, good); // LR6: the gate needs it before Pass 1 will run this extractor

        // No road yet → reserve depletes but goods strand at the mine (no hauler dispatched).
        float reserveBefore = economy.TileDeposits[tile].Reserve;
        for (int i = 0; i < 6; i++) economy.OnTick();
        Assert.True(economy.TileDeposits[tile].Reserve < reserveBefore, "deposit should still deplete");
        Assert.True(economy.TileInventory.AmountAt(tile, good) > 0f, "unroaded goods strand on the mine tile");
        Assert.Empty(economy.Haulers); // no hauler without a road

        // Lay a road → haulers start carrying goods; after enough ticks income arrives.
        ConnectToCapital(economy, tile, capital);
        for (int i = 0; i < 30; i++) economy.OnTick(); // enough for hauler travel + market sale
        Assert.True(economy.TotalIncome > 0f, "a roaded deposit must deliver goods to the capital for income");
    }

    [Fact]
    public void Reserve_depletes_and_caps_extractor_output()
    {
        var land = SquareLand(12, out var capital);
        var economy = new Economy(land, new List<Vector2I> { capital }, 7);
        // Pops left at default so a grain deposit still finds a buyer (grain's home demand scales with
        // population); an ore deposit sells regardless. Either way the reserve depletes and earns income.

        var (tile, _) = BuildPlayerExtractor(economy);
        float reserve = economy.TileDeposits[tile].Reserve;

        // 1 unit every other tick → the deposit empties in ~2*reserve ticks; a margin guarantees it.
        // After all that, enough ticks have passed for haulers to deliver and the market to sell.
        for (int i = 0; i < (int)(2 * reserve) + 40; i++) economy.OnTick();

        Assert.Equal(0f, economy.TileDeposits[tile].Reserve, 3); // fully depleted
        Assert.True(economy.TotalIncome > 0f, "extracted goods must have been delivered and sold");
    }

    [Fact]
    public void Output_lands_in_the_player_nation()
    {
        var land = TwoNationLand(16, out var capA, out var capB);
        var economy = new Economy(land, new List<Vector2I> { capA, capB }, 2024);
        Assert.NotEmpty(economy.Deposits);
        economy.EnableAiAutonomy = false;     // M6: keep the AI inert so this tests player extraction alone
        // Pops left at default so a grain deposit's output is bought by the home market (grain demand needs
        // population); an ore deposit sells regardless.

        BuildPlayerExtractor(economy);
        // Run enough ticks for hauler delivery and market income to settle (3 producing ticks + travel time).
        for (int i = 0; i < 40; i++) economy.OnTick();

        // Player extracted goods → delivered to capital → sold through internal market.
        Assert.True(economy.TotalIncome > 0f, "player extraction must yield income");
        // Whole-world build-gating: the AI nation can't build, so it produces nothing.
        Assert.Equal(0f, economy.Nations[capB].Stockpile.Values.Sum(), 3);
    }

    [Fact]
    public void Same_seed_reproduces_identical_income()
    {
        var landA = SquareLand(12, out var capA);
        var landB = SquareLand(12, out var capB);
        var a = new Economy(landA, new List<Vector2I> { capA }, 2024);
        var b = new Economy(landB, new List<Vector2I> { capB }, 2024);

        BuildPlayerExtractor(a); // same seed → same deposit layout → same tile chosen
        BuildPlayerExtractor(b);

        for (int i = 0; i < 60; i++) { a.OnTick(); b.OnTick(); }

        // Same seed, same inputs → deterministic income.
        Assert.Equal(a.TotalIncome, b.TotalIncome, 3);
    }

    // ---- M3: owners ----

    [Fact]
    public void First_placed_capital_is_the_player()
    {
        var land = TwoNationLand(16, out var capA, out var capB);
        var economy = new Economy(land, new List<Vector2I> { capA, capB }, 2024);

        Assert.Equal(0, economy.Nations[capA].Owner);
        Assert.True(economy.Nations[capA].IsPlayer);
        Assert.Equal(1, economy.Nations[capB].Owner);
        Assert.False(economy.Nations[capB].IsPlayer);
        Assert.Same(economy.Nations[capA], economy.PlayerNation);
    }

    // ---- M3: building a workshop ----

    [Fact]
    public void TryBuildWorkshop_succeeds_on_owned_empty_tile_and_charges_treasury()
    {
        var land = SquareLand(12, out var cap);
        var economy = new Economy(land, new List<Vector2I> { cap }, 99);
        economy.PlayerNation.Treasury = 1000f; // M4.5: starting treasury only affords one extractor
        float before = economy.PlayerNation.Treasury;
        Vector2I tile = FirstEmptyTile(economy, economy.PlayerNation.Tiles);

        Assert.True(economy.TryBuildWorkshop(tile));
        Assert.Equal(BuildingType.Workshop, economy.TileBuildings[tile].Type);
        Assert.Equal(before - Building.Of(BuildingType.Workshop).Cost, economy.PlayerNation.Treasury, 3);

        Assert.False(economy.TryBuildWorkshop(tile));                  // tile now occupied
        Assert.False(economy.TryBuildWorkshop(new Vector2I(9999, 9999))); // off-map / not owned
    }

    [Fact]
    public void TryBuildWorkshop_fails_when_unaffordable()
    {
        var land = SquareLand(12, out var cap);
        var economy = new Economy(land, new List<Vector2I> { cap }, 99);
        economy.PlayerNation.Treasury = 0f;

        Vector2I tile = FirstEmptyTile(economy, economy.PlayerNation.Tiles);
        Assert.False(economy.TryBuildWorkshop(tile));
        Assert.False(economy.TileBuildings.ContainsKey(tile));
    }

    [Fact]
    public void TryBuildWorkshop_fails_on_other_nations_land()
    {
        var land = TwoNationLand(16, out var capA, out var capB);
        var economy = new Economy(land, new List<Vector2I> { capA, capB }, 2024);

        Vector2I aiTile = FirstEmptyTile(economy, economy.Nations[capB].Tiles);
        Assert.False(economy.TryBuildWorkshop(aiTile)); // capB is AI; only player territory is buildable
    }

    // ---- M3: workshop production (Ore → Tools) ----

    [Fact]
    public void Workshop_converts_ore_to_tools_only_when_ore_is_available()
    {
        var land = SquareLand(12, out var cap);
        var economy = new Economy(land, new List<Vector2I> { cap }, 99);

        // Isolate the workshop from the random extractors so the test is deterministic.
        economy.TileBuildings.Clear();
        var workshopTile = new Vector2I(5, 5);
        economy.TileBuildings[workshopTile] = Building.Of(BuildingType.Workshop);

        // No ore at capital → no run.
        economy.OnTick();
        Assert.Equal(0f, economy.TileInventory.AmountAt(workshopTile, Good.Tools), 3);

        // Deliver ore to the capital (as a hauler would) → consume 1 Ore, produce 1 Tools (Workshop Rate = 1).
        economy.TileInventory.Add(cap, Good.Ore, 5f);
        economy.OnTick();
        Assert.Equal(1f, economy.TileInventory.AmountAt(workshopTile, Good.Tools), 3); // output on workshop tile
        Assert.Equal(3f, economy.TileInventory.AmountAt(cap, Good.Ore), 3);            // 5 - 1 (workshop) - 1 (home market)
        Assert.Equal(1f, economy.PlayerConsumedLastTick.GetValueOrDefault(Good.Ore), 3);
        Assert.Equal(1f, economy.PlayerProducedLastTick.GetValueOrDefault(Good.Tools), 3);
    }

    // ---- M3: persistence ----

    [Fact]
    public void ApplySavedState_restores_treasury_stockpiles_and_buildings()
    {
        var land = SquareLand(12, out var cap);
        var played = new Economy(land, new List<Vector2I> { cap }, 7);
        played.PlayerNation.Treasury = 1000f;

        // A player-built extractor (raw) and a workshop (refined) — M4.5 persists both, not just workshops.
        var (mine, _) = BuildPlayerExtractor(played);
        Vector2I ws = FirstEmptyTile(played, played.PlayerNation.Tiles);
        played.TryBuildWorkshop(ws);
        var rail = new Vector2I(3, 4);
        played.TryBuildRailroad(rail); // paid-for railroad also persists
        for (int i = 0; i < 10; i++) played.OnTick(); // deplete reserve + accumulate stockpile

        var reserves = played.Deposits.Select(d => d.Reserve).ToList();
        Nation p = played.PlayerNation;
        // Player goods are physical — save the real holdings (StoredAmountOf), not the empty abstract pool.
        var stocks = new List<NationStock>
        {
            new NationStock(cap, p.Treasury,
                played.StoredAmountOf(p, Good.Grain),
                played.StoredAmountOf(p, Good.Ore),
                played.StoredAmountOf(p, Good.Tools))
        };
        var buildings = new List<TileBuilding>
        {
            new TileBuilding(mine, played.TileBuildings[mine].Type),
            new TileBuilding(ws, BuildingType.Workshop),
        };
        var railroads = played.Railroads.ToList();

        var land2 = SquareLand(12, out var cap2);
        var loaded = new Economy(land2, new List<Vector2I> { cap2 }, 7);
        loaded.ApplySavedState(reserves, stocks, buildings, railroads);

        Assert.Equal(reserves, loaded.Deposits.Select(d => d.Reserve).ToList());
        Assert.Equal(p.Treasury, loaded.PlayerNation.Treasury, 3);
        Assert.Equal(BuildingType.Workshop, loaded.TileBuildings[ws].Type);
        Assert.Equal(played.TileBuildings[mine].Type, loaded.TileBuildings[mine].Type);
        Assert.Contains(rail, loaded.Railroads);
        // Player goods restored physically (into the capital's TileInventory), not the dead Stockpile.
        Nation lp = loaded.PlayerNation;
        Assert.Equal(played.StoredAmountOf(p, Good.Grain), loaded.StoredAmountOf(lp, Good.Grain), 3);
        Assert.Equal(played.StoredAmountOf(p, Good.Ore),   loaded.StoredAmountOf(lp, Good.Ore), 3);
        Assert.Equal(played.StoredAmountOf(p, Good.Tools), loaded.StoredAmountOf(lp, Good.Tools), 3);
    }

    [Fact]
    public void ApplySavedState_skips_reserves_on_count_mismatch()
    {
        var land = SquareLand(12, out var cap);
        var economy = new Economy(land, new List<Vector2I> { cap }, 7);
        var before = economy.Deposits.Select(d => d.Reserve).ToList();

        economy.ApplySavedState(new List<float> { 1f, 2f }, null, null, null); // wrong length → no-op, no throw

        Assert.Equal(before, economy.Deposits.Select(d => d.Reserve).ToList());
    }

}
