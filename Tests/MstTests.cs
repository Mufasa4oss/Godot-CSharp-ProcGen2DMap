using System.Collections.Generic;
using System.Linq;
using Xunit;

public class MstTests
{
    [Fact]
    public void ConnectedGraph_yields_V_minus_1_edges()
    {
        // 4 fully-connected vertices -> MST has 3 edges.
        var prim = new PrimsAlgorithmSimple(4);
        for (int i = 0; i < 4; i++)
            for (int j = i + 1; j < 4; j++)
                prim.AddEdge(i, j, 1.0);

        var mst = prim.FindMST();
        Assert.Equal(3, mst.Count);
    }

    [Fact]
    public void Picks_minimum_total_weight()
    {
        // Square 0-1-2-3 with cheap perimeter edges (1) and expensive diagonals (5).
        // The MST should be a chain of three weight-1 edges = total 3.
        var prim = new PrimsAlgorithmSimple(4);
        prim.AddEdge(0, 1, 1);
        prim.AddEdge(1, 2, 1);
        prim.AddEdge(2, 3, 1);
        prim.AddEdge(0, 3, 1);
        prim.AddEdge(0, 2, 5);
        prim.AddEdge(1, 3, 5);

        var mst = prim.FindMST();

        Assert.Equal(3, mst.Count);
        Assert.Equal(3.0, mst.Sum(e => e.Weight));
        Assert.DoesNotContain(mst, e => e.Weight == 5);
    }

    [Fact]
    public void Reaches_every_vertex()
    {
        var prim = new PrimsAlgorithmSimple(5);
        for (int i = 0; i < 5; i++)
            for (int j = i + 1; j < 5; j++)
                prim.AddEdge(i, j, i + j);

        var mst = prim.FindMST();

        var reached = new HashSet<int> { 0 };
        foreach (var e in mst) { reached.Add(e.From); reached.Add(e.To); }
        Assert.Equal(5, reached.Count);
    }

    [Fact]
    public void DisconnectedGraph_stops_without_throwing()
    {
        // 3 vertices but only 0-1 connected; vertex 2 is unreachable.
        var prim = new PrimsAlgorithmSimple(3);
        prim.AddEdge(0, 1, 1);

        var mst = prim.FindMST();
        Assert.Single(mst); // only the reachable edge, then it bails
    }

    [Fact]
    public void FewerThanTwoVertices_is_empty()
    {
        Assert.Empty(new PrimsAlgorithmSimple(1).FindMST());
    }
}
