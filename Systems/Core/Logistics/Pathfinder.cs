using System.Collections.Generic;
using Godot; // Vector2I struct only — Core stays engine-free.

// LR2 hauler routing. RoadGraph answers "which road tiles link A to B?"; a hauler needs the WHOLE walk it
// physically follows — its source tile, every road tile between, then the destination tile — so the entity
// can crawl through each step and the renderer can lerp a truck along it. This thin layer turns the road-only
// path into that complete, contiguous tile walk.
//
// Mental model: RoadGraph gives you the paved middle of the journey; Pathfinder bolts on the driveway at each
// end (the producer's tile and the capital). Empty walk = the two ends aren't road-connected, so no hauler is
// dispatched (the good stays stranded on its tile).
public class Pathfinder
{
    private readonly RoadGraph _graph;

    public Pathfinder(RoadGraph graph) { _graph = graph; }

    // The full tile-by-tile walk a hauler follows from `from` to `to`, or an empty list when they aren't
    // road-connected. Every consecutive pair is 4-adjacent (or identical, which is deduped): `from` abuts the
    // first road tile, the road tiles chain edge-to-edge, and the last road tile abuts `to` — so a renderer can
    // lerp straight through it and the stranding check can read interior road tiles directly.
    public List<Vector2I> FindWalk(Vector2I from, Vector2I to)
    {
        var walk = new List<Vector2I>();
        if (from == to) return walk;

        List<Vector2I> roads = _graph.ShortestPath(from, to);
        if (roads.Count == 0) return walk; // not road-connected ⇒ no walk ⇒ no hauler

        Append(walk, from);
        foreach (Vector2I r in roads) Append(walk, r); // skips a road tile sitting on `from` (deduped)
        Append(walk, to);                              // skips it again if a road sits on `to`
        return walk;
    }

    // Append unless it would repeat the last tile (the endpoints can coincide with an end road tile when the
    // player laid track right on a mine or a capital).
    private static void Append(List<Vector2I> walk, Vector2I tile)
    {
        if (walk.Count == 0 || walk[walk.Count - 1] != tile) walk.Add(tile);
    }
}
