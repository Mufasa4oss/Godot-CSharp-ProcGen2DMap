using System;
using System.Collections.Generic;
using Godot; // Vector2I struct only — Core stays engine-free.

// LR2: one truck. It carries a fixed load of a single good along a fixed tile Walk (from a producer to a
// sink), crawling forward a little each tick. When it reaches the end it unloads; if the road ahead of it is
// torn up mid-journey it simply stops where it is, still holding its load — the physical "stranded goods" the
// milestone is about (nothing teleports across the gap). The renderer reads Walk + Progress to draw it moving.
public class Hauler
{
    public Vector2I Origin { get; }       // the producer tile it picked up from
    public Vector2I Destination { get; }  // the sink tile it delivers to (a nation's capital)
    public Good Good { get; }
    public float Amount { get; }
    public IReadOnlyList<Vector2I> Walk { get; }

    // How far along Walk the truck has crawled, in tiles: 0 = sitting on Walk[0], 1 = on Walk[1], etc.
    public float Progress { get; set; }

    public Hauler(Vector2I origin, Vector2I destination, Good good, float amount, List<Vector2I> walk)
    {
        Origin = origin;
        Destination = destination;
        Good = good;
        Amount = amount;
        Walk = walk;
    }

    // Reached the far end of the walk ⇒ ready to unload at the Destination.
    public bool Arrived => Progress >= Walk.Count - 1;

    // Is the road still intact AHEAD of the truck? Only the interior tiles are road (the two endpoints are the
    // producer + the capital); a cut already behind the truck doesn't strand it, but a cut on a tile it still
    // has to cross does. False ⇒ stalled this tick (holds position + load until the road is rebuilt).
    public bool PathAheadIntact(HashSet<Vector2I> roads)
    {
        int here = Math.Max(1, (int)Math.Floor(Progress)); // the tile it's on (or just left); skip endpoint 0
        for (int i = here; i < Walk.Count - 1; i++)        // ... up to but not including the destination
            if (!roads.Contains(Walk[i])) return false;
        return true;
    }
}
