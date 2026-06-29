using Godot;
using System.Collections.Generic;

// LR2 visual: the physical goods layer made watchable. Draws every auto-dispatched hauler as a little truck
// crawling tile-by-tile along the road it was routed down (colored by the good it carries), and a small crate
// "pile" on any tile where produced goods are sitting at rest — a mine with no road out, or your capital
// warehouse filling up as deliveries land. This is the evolved cousin of the old trade-packet visualizer: it
// lerps along a real multi-tile path instead of teleporting a dot across a straight line.
//
// Core (Economy.Dispatcher) moves haulers one tile per 1-second tick; this renderer eases their drawn position
// toward that target every frame so the trucks glide instead of hopping. Added as a child of Main; Initialize()
// once after the economy exists.
public partial class Haulers : Node2D
{
    private Economy _economy;
    private TileMapLayer _tml;

    // Smoothed on-screen position per hauler, eased toward the tile the Core entity has reached. Rebuilt each
    // frame against the live hauler list so delivered/retired trucks drop out.
    private readonly Dictionary<Hauler, Vector2> _shown = new();
    private const float EaseSpeed   = 4f;  // how fast the drawn truck catches its target (per second)
    private const float TruckRadius = 15f;
    private const float CrateSize   = 12f;

    private static Color GoodColor(Good good) => good switch
    {
        Good.Grain => new Color(0.70f, 0.95f, 0.25f), // yellow-green
        Good.Ore   => new Color(0.70f, 0.72f, 0.78f), // steel gray
        Good.Tools => new Color(1.00f, 0.80f, 0.10f), // gold
        _ => Colors.White,
    };

    public void Initialize(Economy economy, TileMapLayer tml)
    {
        _economy = economy;
        _tml = tml;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_economy == null || _tml == null) return;

        float t = Mathf.Clamp((float)delta * EaseSpeed, 0f, 1f);
        var live = new HashSet<Hauler>();
        foreach (Hauler h in _economy.Haulers)
        {
            live.Add(h);
            Vector2 target = WorldPos(h);
            _shown[h] = _shown.TryGetValue(h, out Vector2 cur) ? cur.Lerp(target, t) : target; // new truck: snap on
        }

        // Drop trucks that delivered + retired (or stranded out of the list). Pruned every frame so a churn of
        // equal adds+removes can't leave a ghost behind.
        if (_shown.Count > live.Count)
            foreach (Hauler gone in new List<Hauler>(_shown.Keys))
                if (!live.Contains(gone)) _shown.Remove(gone);

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_economy == null || _tml == null) return;

        // At-rest piles: produced goods sitting on a tile (a mine waiting for a road, the capital warehouse
        // filling). Drawn under the trucks so a passing hauler reads on top.
        foreach (Vector2I tile in _economy.TileInventory.Tiles)
        {
            float total = _economy.TileInventory.TotalAt(tile);
            if (total <= 0f) continue;
            DrawPile(_tml.MapToLocal(tile), DominantGood(tile), total);
        }

        // Trucks: one per in-flight hauler, at its eased position.
        foreach (var kvp in _shown)
        {
            Vector2 pos = kvp.Value;
            DrawCircle(pos, TruckRadius + 3f, new Color(0.1f, 0.1f, 0.12f, 0.9f)); // dark chassis outline
            DrawCircle(pos, TruckRadius, GoodColor(kvp.Key.Good));                 // cargo color
        }
    }

    // Where the Core hauler currently sits in world space: lerp between the two tiles its fractional Progress
    // straddles.
    private Vector2 WorldPos(Hauler h)
    {
        int i = Mathf.Clamp((int)Mathf.Floor(h.Progress), 0, h.Walk.Count - 2);
        float frac = Mathf.Clamp(h.Progress - i, 0f, 1f);
        return _tml.MapToLocal(h.Walk[i]).Lerp(_tml.MapToLocal(h.Walk[i + 1]), frac);
    }

    // A small stack of crates whose count hints at how much is piled up (capped so a big warehouse doesn't
    // sprawl), colored by the good with the most units on the tile.
    private void DrawPile(Vector2 center, Good good, float total)
    {
        int crates = Mathf.Clamp(Mathf.CeilToInt(total), 1, 5);
        Color color = GoodColor(good);
        for (int i = 0; i < crates; i++)
        {
            float x = center.X - 24f + i * (CrateSize + 2f);
            var rect = new Rect2(x, center.Y + 14f, CrateSize, CrateSize);
            DrawRect(rect, color);
            DrawRect(rect, new Color(0.1f, 0.1f, 0.12f, 0.8f), filled: false, width: 2f);
        }
    }

    private Good DominantGood(Vector2I tile)
    {
        Good best = Good.Ore;
        float bestAmt = -1f;
        foreach (var kvp in _economy.TileInventory.At(tile))
            if (kvp.Value > bestAmt) { bestAmt = kvp.Value; best = kvp.Key; }
        return best;
    }
}
