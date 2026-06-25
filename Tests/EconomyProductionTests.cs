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

    private static float TotalGoods(Economy e) =>
        e.Nations.Values.Sum(n => n.Stockpile.Values.Sum());

    private static Vector2I FirstEmptyTile(Economy e, IEnumerable<Vector2I> tiles)
    {
        foreach (var t in tiles)
            if (!e.TileBuildings.ContainsKey(t)) return t;
        throw new InvalidOperationException("no empty tile available");
    }

    // ---- Production (extractors) ----

    [Fact]
    public void Worked_tiles_output_climbs_each_tick()
    {
        var land = SquareLand(12, out var capital);
        var economy = new Economy(land, new List<Vector2I> { capital }, 99);
        Assert.NotEmpty(economy.Deposits);

        // Reserve = 100/tile, Rate = 1/tile → every deposit lasts 100 ticks, so for small N nothing
        // depletes and each tick adds exactly the sum of deposit rates. No workshops auto-placed.
        float perTick = economy.Deposits.Sum(d => d.Rate);

        economy.OnTick();
        Assert.Equal(perTick, TotalGoods(economy), 3);
        economy.OnTick();
        Assert.Equal(2 * perTick, TotalGoods(economy), 3);
    }

    [Fact]
    public void Reserve_depletes_and_caps_total_output()
    {
        var land = SquareLand(12, out var capital);
        var economy = new Economy(land, new List<Vector2I> { capital }, 7);
        float initialReserve = economy.Deposits.Sum(d => d.Reserve);

        for (int i = 0; i < 200; i++) economy.OnTick();

        Assert.All(economy.Deposits, d => Assert.Equal(0f, d.Reserve, 3));
        Assert.Equal(initialReserve, TotalGoods(economy), 3);
    }

    [Fact]
    public void Output_lands_in_the_owning_nation()
    {
        var land = TwoNationLand(16, out var capA, out var capB);
        var economy = new Economy(land, new List<Vector2I> { capA, capB }, 2024);
        Assert.NotEmpty(economy.Deposits);

        for (int i = 0; i < 5; i++) economy.OnTick();

        var expected = new Dictionary<Vector2I, float>();
        foreach (var (tile, building) in economy.TileBuildings)
        {
            Vector2I cap = economy.TileNation[tile].Capital;
            expected.TryGetValue(cap, out float cur);
            expected[cap] = cur + economy.TileDeposits[tile].Rate * 5; // 5 ticks, no depletion
        }

        foreach (var nation in economy.Nations.Values)
            Assert.Equal(expected.GetValueOrDefault(nation.Capital), nation.Stockpile.Values.Sum(), 3);
    }

    [Fact]
    public void Same_seed_reproduces_identical_stockpiles()
    {
        var landA = SquareLand(12, out var capA);
        var landB = SquareLand(12, out var capB);
        var a = new Economy(landA, new List<Vector2I> { capA }, 2024);
        var b = new Economy(landB, new List<Vector2I> { capB }, 2024);

        for (int i = 0; i < 30; i++) { a.OnTick(); b.OnTick(); }

        Assert.Equal(StockSignature(a), StockSignature(b));
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
        var tile = new Vector2I(5, 5);
        economy.TileBuildings[tile] = Building.Of(BuildingType.Workshop);

        // No ore → no run.
        economy.OnTick();
        Assert.Equal(0f, economy.PlayerNation.Stockpile.GetValueOrDefault(Good.Tools), 3);

        // With ore → consume 1 Ore, produce 1 Tools (Workshop Rate = 1).
        economy.PlayerNation.AddGood(Good.Ore, 5f);
        economy.OnTick();
        Assert.Equal(1f, economy.PlayerNation.Stockpile.GetValueOrDefault(Good.Tools), 3);
        Assert.Equal(4f, economy.PlayerNation.Stockpile.GetValueOrDefault(Good.Ore), 3);
        Assert.Equal(1f, economy.PlayerConsumedLastTick.GetValueOrDefault(Good.Ore), 3);
        Assert.Equal(1f, economy.PlayerProducedLastTick.GetValueOrDefault(Good.Tools), 3);
    }

    // ---- M3: persistence ----

    [Fact]
    public void ApplySavedState_restores_treasury_stockpiles_and_workshops()
    {
        var land = SquareLand(12, out var cap);
        var played = new Economy(land, new List<Vector2I> { cap }, 7);
        for (int i = 0; i < 10; i++) played.OnTick();

        Vector2I ws = FirstEmptyTile(played, played.PlayerNation.Tiles);
        played.TryBuildWorkshop(ws);

        var reserves = played.Deposits.Select(d => d.Reserve).ToList();
        Nation p = played.PlayerNation;
        var stocks = new List<NationStock>
        {
            new NationStock(cap, p.Treasury,
                p.Stockpile.GetValueOrDefault(Good.Grain),
                p.Stockpile.GetValueOrDefault(Good.Ore),
                p.Stockpile.GetValueOrDefault(Good.Tools))
        };
        var workshops = new List<Vector2I> { ws };

        var land2 = SquareLand(12, out var cap2);
        var loaded = new Economy(land2, new List<Vector2I> { cap2 }, 7);
        loaded.ApplySavedState(reserves, stocks, workshops);

        Assert.Equal(reserves, loaded.Deposits.Select(d => d.Reserve).ToList());
        Assert.Equal(p.Treasury, loaded.PlayerNation.Treasury, 3);
        Assert.Equal(p.Stockpile.GetValueOrDefault(Good.Ore),
                     loaded.PlayerNation.Stockpile.GetValueOrDefault(Good.Ore), 3);
        Assert.Equal(BuildingType.Workshop, loaded.TileBuildings[ws].Type);
    }

    [Fact]
    public void ApplySavedState_skips_reserves_on_count_mismatch()
    {
        var land = SquareLand(12, out var cap);
        var economy = new Economy(land, new List<Vector2I> { cap }, 7);
        var before = economy.Deposits.Select(d => d.Reserve).ToList();

        economy.ApplySavedState(new List<float> { 1f, 2f }, null, null); // wrong length → no-op, no throw

        Assert.Equal(before, economy.Deposits.Select(d => d.Reserve).ToList());
    }

    // Canonical description of every nation's stockpile: capital + sorted good:amount pairs.
    private static string StockSignature(Economy e)
    {
        var parts = e.Nations.Values
            .OrderBy(n => n.Capital.X).ThenBy(n => n.Capital.Y)
            .Select(n => $"{n.Capital.X}.{n.Capital.Y}=" + string.Join(",",
                n.Stockpile.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}:{kv.Value}")));
        return string.Join("|", parts);
    }
}
