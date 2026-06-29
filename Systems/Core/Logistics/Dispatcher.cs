using System;
using System.Collections.Generic;
using System.Linq;
using Godot; // Vector2I struct only — Core stays engine-free.

// LR2: the automatic hauling layer (honours the locked "automate the micro, agency in the macro" rule — the
// player lays roads; the sim runs the trucks). Each tick it spawns haulers to drain surplus tiles toward their
// sink and advances the ones already rolling. No manual routing, no vehicle placement.
//
// Two phases, called once per tick from the economy:
//   Advance — roll every in-flight hauler forward; deliver the ones that arrived; stall the ones whose road
//             got cut ahead of them.
//   Dispatch — for each tile holding a haulable surplus with a clear road to its sink, send one new hauler
//             (picking the load up off the tile). One hauler per origin tile at a time, so a backlog piles up
//             at the source rather than spawning a swarm.
public class Dispatcher
{
    public List<Hauler> Haulers { get; } = new();

    private const float HaulUnit = 1f; // load per truck
    public  const float Speed    = 1f; // tiles crawled per tick (slow enough to watch)

    // Roll every hauler forward one tick. A hauler whose road ahead is intact advances by Speed; one that's
    // arrived unloads its cargo into the sink tile's inventory and leaves the road. A hauler whose road was cut
    // ahead of it just holds — stranded with its load until the link is rebuilt.
    public void Advance(TileInventory inventory, IReadOnlyCollection<Vector2I> roadTiles)
    {
        var roads = roadTiles as HashSet<Vector2I> ?? new HashSet<Vector2I>(roadTiles);
        for (int i = Haulers.Count - 1; i >= 0; i--)
        {
            Hauler h = Haulers[i];
            if (!h.PathAheadIntact(roads)) continue; // road torn up under it — stranded, no progress

            h.Progress += Speed;
            if (h.Arrived)
            {
                inventory.Add(h.Destination, h.Good, h.Amount); // unload at the capital warehouse
                Haulers.RemoveAt(i);
            }
        }
    }

    // Send new haulers. `sinkOf` maps a producer tile to where its goods should go (its nation's capital), or
    // null if that tile shouldn't ship. Deterministic: tiles in coordinate order, goods in enum order, so saves
    // and replays match.
    public void Dispatch(TileInventory inventory, IReadOnlyCollection<Vector2I> roadTiles,
        Func<Vector2I, Vector2I?> sinkOf)
    {
        var pathfinder = new Pathfinder(new RoadGraph(roadTiles));
        var origins = new HashSet<Vector2I>(Haulers.Select(h => h.Origin)); // tiles already being served

        foreach (Vector2I tile in inventory.Tiles.OrderBy(t => t.X).ThenBy(t => t.Y))
        {
            if (origins.Contains(tile)) continue;        // one hauler per origin at a time
            Vector2I? sink = sinkOf(tile);
            if (sink is not Vector2I dest || dest == tile) continue; // no sink, or the tile IS the sink

            foreach (Good good in inventory.At(tile).Keys.OrderBy(g => (int)g).ToList())
            {
                if (inventory.AmountAt(tile, good) < HaulUnit) continue;
                List<Vector2I> walk = pathfinder.FindWalk(tile, dest);
                if (walk.Count == 0) continue; // no road path ⇒ the good stays put (stranded on its tile)

                inventory.Add(tile, good, -HaulUnit); // load it onto the truck
                Haulers.Add(new Hauler(tile, dest, good, HaulUnit, walk));
                origins.Add(tile);
                break; // one load per tile per tick
            }
        }
    }
}
