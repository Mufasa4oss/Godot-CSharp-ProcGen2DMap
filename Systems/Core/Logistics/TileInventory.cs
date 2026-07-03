using System.Collections.Generic;
using System.Linq;
using Godot; // Vector2I struct only — Core stays engine-free (mirrors RoadGraph / VoronoiAssignment).

// LR2: goods AT REST, located on specific tiles. The PLAYER's entire goods economy lives here (the AI keep
// the abstract Nation.Stockpile) — a producer drops its output on its OWN tile, and it stays put until a
// hauler carries it off. A good with no road path to a sink just piles up where it was made = visibly
// stranded (geography finally bites). Pure Core (Vector2I keys only), so it unit-tests headless like the
// rest of Logistics.
//
// This layer is load-bearing: a nation is only credited (internal market / export) for goods physically
// delivered to its capital. Read player holdings via Economy.StoredAmountOf, never Nation.Stockpile.
public class TileInventory
{
    private readonly Dictionary<Vector2I, Dictionary<Good, float>> _byTile = new();

    // Add `amount` of `good` on `tile` (negative removes). Floors at 0 and prunes emptied entries so a drained
    // tile drops out of Tiles (the dispatch scan + the renderer only ever see tiles that actually hold goods).
    public void Add(Vector2I tile, Good good, float amount)
    {
        if (amount == 0f) return;
        if (!_byTile.TryGetValue(tile, out Dictionary<Good, float> goods))
        {
            if (amount < 0f) return; // nothing here to remove
            goods = new Dictionary<Good, float>();
            _byTile[tile] = goods;
        }

        goods.TryGetValue(good, out float current);
        float next = current + amount;
        if (next <= 0f) goods.Remove(good);
        else goods[good] = next;

        if (goods.Count == 0) _byTile.Remove(tile);
    }

    public float AmountAt(Vector2I tile, Good good) =>
        _byTile.TryGetValue(tile, out Dictionary<Good, float> goods) ? goods.GetValueOrDefault(good) : 0f;

    // Total units of `good` across every tile that holds any. Only the PLAYER's goods ever live in this
    // inventory (AI keep the abstract Nation.Stockpile), so this is the player's physical holding of the good
    // AT REST — on producer tiles and delivered to the capital. NOTE: goods riding a hauler are removed from
    // the inventory for the trip (see Dispatcher), so in-transit units are NOT counted here. O(populated
    // tiles), a handful in practice, so it's cheap to poll from the HUD each frame.
    public float TotalOf(Good good)
    {
        float total = 0f;
        foreach (Dictionary<Good, float> goods in _byTile.Values)
            total += goods.GetValueOrDefault(good);
        return total;
    }

    // Total units sitting on a tile (across all goods) — used by the renderer to size the "pile" marker.
    public float TotalAt(Vector2I tile) =>
        _byTile.TryGetValue(tile, out Dictionary<Good, float> goods) ? goods.Values.Sum() : 0f;

    // The goods present on a tile (read-only view for the dispatch scan + rendering).
    public IReadOnlyDictionary<Good, float> At(Vector2I tile) =>
        _byTile.TryGetValue(tile, out Dictionary<Good, float> goods) ? goods : Empty;

    private static readonly Dictionary<Good, float> Empty = new();

    // Tiles that currently hold any goods. Materialized so callers can mutate the inventory while iterating.
    public IReadOnlyList<Vector2I> Tiles => _byTile.Keys.ToList();
}
