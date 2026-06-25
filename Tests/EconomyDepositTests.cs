using System.Collections.Generic;
using System.Linq;
using Godot;
using Xunit;

public class EconomyDepositTests
{
    // A square block of land, every tile owned by one capital at (0,0).
    private static Dictionary<Vector2I, Vector2I> SquareLand(int size, out Vector2I capital)
    {
        capital = new Vector2I(0, 0);
        var land = new Dictionary<Vector2I, Vector2I>();
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                land[new Vector2I(x, y)] = capital;
        return land;
    }

    [Fact]
    public void Provinces_invert_tiles_under_their_capital()
    {
        var land = SquareLand(10, out var capital);
        var economy = new Economy(land, new List<Vector2I> { capital }, 42);

        Assert.True(economy.Nations.ContainsKey(capital));
        Assert.Equal(100, economy.Nations[capital].Tiles.Count);
    }

    [Fact]
    public void Deposits_sit_only_on_land()
    {
        var land = SquareLand(10, out var capital);
        var economy = new Economy(land, new List<Vector2I> { capital }, 7);

        foreach (var dep in economy.Deposits)
            foreach (var tile in dep.Tiles)
                Assert.True(land.ContainsKey(tile), $"deposit tile {tile} is not land");
    }

    [Fact]
    public void Cluster_sizes_are_in_range_and_contiguous()
    {
        var land = SquareLand(12, out var capital);
        var economy = new Economy(land, new List<Vector2I> { capital }, 99);

        Assert.NotEmpty(economy.Deposits);
        foreach (var dep in economy.Deposits)
        {
            Assert.InRange(dep.Tiles.Count, 1, 5);   // MinSize..MaxSize
            Assert.True(IsContiguous(dep.Tiles), "deposit cluster is not 8-connected");
        }
    }

    [Fact]
    public void Tiles_belong_to_at_most_one_deposit()
    {
        var land = SquareLand(12, out var capital);
        var economy = new Economy(land, new List<Vector2I> { capital }, 123);

        int totalTiles = economy.Deposits.Sum(d => d.Tiles.Count);
        Assert.Equal(totalTiles, economy.TileDeposits.Count); // no overlaps
    }

    [Fact]
    public void Same_seed_reproduces_identical_deposits()
    {
        var landA = SquareLand(12, out var capA);
        var landB = SquareLand(12, out var capB);

        var a = new Economy(landA, new List<Vector2I> { capA }, 2024);
        var b = new Economy(landB, new List<Vector2I> { capB }, 2024);

        Assert.Equal(Signature(a), Signature(b));
    }

    [Fact]
    public void Different_seeds_generally_differ()
    {
        var land1 = SquareLand(12, out var c1);
        var land2 = SquareLand(12, out var c2);

        var a = new Economy(land1, new List<Vector2I> { c1 }, 1);
        var b = new Economy(land2, new List<Vector2I> { c2 }, 2);

        Assert.NotEqual(Signature(a), Signature(b));
    }

    // Canonical, reference-independent description of every deposit: good + sorted tiles.
    private static string Signature(Economy e)
    {
        var parts = e.Deposits.Select(d =>
            d.Good + ":" + string.Join(",", d.Tiles
                .OrderBy(t => t.X).ThenBy(t => t.Y)
                .Select(t => $"{t.X}.{t.Y}")));
        return string.Join("|", parts);
    }

    private static bool IsContiguous(HashSet<Vector2I> tiles)
    {
        if (tiles.Count <= 1) return true;
        var seen = new HashSet<Vector2I>();
        var queue = new Queue<Vector2I>();
        Vector2I start = tiles.First();
        queue.Enqueue(start);
        seen.Add(start);
        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            foreach (var n in MyGlobalClass.GetSurroundingTiles(cur))
                if (tiles.Contains(n) && seen.Add(n))
                    queue.Enqueue(n);
        }
        return seen.Count == tiles.Count;
    }
}
