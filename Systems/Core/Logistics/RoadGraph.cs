using System.Collections.Generic;
using Godot; // Vector2I struct only — Core stays engine-free (mirrors VoronoiAssignment / TileResolver).

// LR1: the player's road network as a graph for connectivity + distance queries. Pure Core (no Godot Node),
// so it unit-tests headless like VoronoiAssignment.
//
// Mental model: roads are tiles on a grid. Two road tiles are neighbours when they touch edge-to-edge
// (4-directional — diagonals don't connect, matching how the rails read on screen). A capital isn't a road
// tile itself; it's plugged into the network when a road tile sits on it or right next to it. So two capitals
// are connected exactly when you can walk capital -> road -> road -> ... -> road -> capital without ever
// leaving the road tiles. Pull out a tile in the middle and the walk can break — that's the "cut a road"
// mechanic the trade gate rides on.
public class RoadGraph
{
    private static readonly Vector2I[] Cardinals =
        { new Vector2I(1, 0), new Vector2I(-1, 0), new Vector2I(0, 1), new Vector2I(0, -1) };

    // Copied, not aliased: the caller (Economy.Railroads) keeps mutating its set as the player builds, and a
    // graph should be a snapshot. The copy is cheap relative to the BFS it powers.
    private readonly HashSet<Vector2I> _roads;

    public RoadGraph(IEnumerable<Vector2I> roadTiles)
    {
        _roads = new HashSet<Vector2I>(roadTiles);
    }

    // The road tiles that plug `tile` into the network: the tile itself if it's road, plus any of its four
    // neighbours that are road. Empty means nothing touches `tile`, so it isn't on the network at all.
    private List<Vector2I> NetworkRoadsAt(Vector2I tile)
    {
        var touching = new List<Vector2I>();
        if (_roads.Contains(tile)) touching.Add(tile);
        foreach (Vector2I d in Cardinals)
            if (_roads.Contains(tile + d)) touching.Add(tile + d);
        return touching;
    }

    // Is this tile wired into the road network (on a road or beside one)?
    public bool IsOnNetwork(Vector2I tile) => NetworkRoadsAt(tile).Count > 0;

    // True iff a contiguous run of road tiles links the two tiles (typically two capitals).
    public bool AreConnected(Vector2I from, Vector2I to) => ShortestPathLength(from, to) >= 0;

    // Length, in tiles stepped, of the shortest road path linking `from` to `to`, or -1 if none exists.
    // Counts the step onto the first road and off the last one, so two capitals sharing a single adjacent
    // road tile are distance 2. Used both for the connectivity gate and as a route's transport distance
    // (a longer road costs more to ship across — geography finally bites).
    public int ShortestPathLength(Vector2I from, Vector2I to)
    {
        if (from == to) return 0;
        List<Vector2I> path = ShortestPath(from, to);
        // path holds the road tiles between the ends; + 1 for the final step off the last road onto `to`
        // (the step from `from` onto the first road is already counted by that first road being in the path).
        return path.Count == 0 ? -1 : path.Count + 1;
    }

    // The road tiles forming a shortest road path linking `from` to `to` (typically two capitals), in order,
    // or an empty list if none exists (or from == to). Same 4-neighbour BFS as the length query, but it tracks
    // parents so it can hand back the actual track carrying the link — used as a route's corridor so the
    // capacity model counts the very tiles laid for it, not a now-nonexistent MST corridor.
    public List<Vector2I> ShortestPath(Vector2I from, Vector2I to)
    {
        var path = new List<Vector2I>();
        if (from == to) return path;

        var goals  = new HashSet<Vector2I>(NetworkRoadsAt(to));
        var starts = NetworkRoadsAt(from);
        if (goals.Count == 0 || starts.Count == 0) return path; // a road touches neither end ⇒ unreachable

        var parent  = new Dictionary<Vector2I, Vector2I>();
        var visited = new HashSet<Vector2I>();
        var frontier = new Queue<Vector2I>();
        foreach (Vector2I s in starts)
            if (visited.Add(s)) frontier.Enqueue(s); // start roads are roots — no parent entry

        while (frontier.Count > 0)
        {
            Vector2I tile = frontier.Dequeue();
            if (goals.Contains(tile))
            {
                for (Vector2I cur = tile; ; )
                {
                    path.Add(cur);
                    if (!parent.TryGetValue(cur, out Vector2I p)) break; // reached a root start tile
                    cur = p;
                }
                path.Reverse();
                return path;
            }
            foreach (Vector2I d in Cardinals)
            {
                Vector2I next = tile + d;
                if (_roads.Contains(next) && visited.Add(next))
                {
                    parent[next] = tile;
                    frontier.Enqueue(next);
                }
            }
        }
        return path; // empty: exhausted the component without reaching a goal
    }
}
