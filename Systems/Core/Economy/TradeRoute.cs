using System.Collections.Generic;
using Godot;

// M6 phase 2: one trade link between two nations' capitals (an MST edge). Goods auto-arbitrage across it
// (cheaper nation -> dearer), but only up to a per-tick Capacity (total units, all goods) and only when it
// isn't Blocked (a cut/blockade). Capacity = a base (the always-present MST backbone) plus a bonus for every
// player-laid railroad tile that lies in the route's Corridor — so laying track on a route is a "connector
// road" that raises its throughput, finally giving railroads an economic purpose. FlowLastTick is how many
// units actually crossed last tick (for the view to draw congestion). Engine-free except Vector2I.
public class TradeRoute
{
    public Nation A { get; }
    public Nation B { get; }
    public float Distance { get; }

    // The tiles along this route (the MST L-path between the two capitals), handed in from the Godot layer.
    // Player railroad tiles in here raise the route's capacity.
    public HashSet<Vector2I> Corridor { get; }

    // A hard cut: a blocked route moves nothing (M6 phase-2 "cut a road"; driven by hostility in phase 3).
    public bool Blocked { get; set; }

    // Units that actually crossed last tick — view-only congestion signal, recomputed each trade pass.
    public float FlowLastTick { get; set; }

    public TradeRoute(Nation a, Nation b, float distance, HashSet<Vector2I> corridor)
    {
        A = a;
        B = b;
        Distance = distance;
        Corridor = corridor ?? new HashSet<Vector2I>();
    }

    // Endpoint-unordered match on capitals (routes are undirected).
    public bool Connects(Vector2I cap1, Vector2I cap2) =>
        (A.Capital == cap1 && B.Capital == cap2) || (A.Capital == cap2 && B.Capital == cap1);

    // Per-tick throughput: the backbone base plus a bonus per player railroad tile in the corridor. Computed
    // live from the (persisted) railroad set, so connector capacity reproduces on load with no extra save state.
    public float CapacityWith(HashSet<Vector2I> railroads, float baseCapacity, float perRail)
    {
        if (Blocked) return 0f;
        int rails = 0;
        foreach (Vector2I tile in Corridor)
            if (railroads.Contains(tile)) rails++;
        return baseCapacity + rails * perRail;
    }
}
