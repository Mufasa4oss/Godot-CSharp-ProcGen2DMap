using Godot;
using System.Collections.Generic;

// M7 legibility: draws the trade backbone as roads colored by REAL congestion (last tick's flow vs the
// route's capacity) — idle = dim gray, flowing = green, near-capacity = hot orange, cut/blockaded = red — and
// widened by capacity (player connector rails fatten the road). It also spawns a colored packet for each
// actual inter-nation trade (fed from Economy.TradedThisTick each tick), so goods are seen moving along the
// routes (no more fake/random packets). Add as a child of Main; Initialize() once after the economy exists,
// and OnTradesProcessed() each tick.
public partial class TradeNetworkVisualizer : Node2D
{
    private Economy _economy;
    private TileMapLayer _tml;

    private class Packet
    {
        public Vector2 Start, End;
        public float Progress;
        public Color Color;
    }
    private readonly List<Packet> _packets = new();
    private const float PacketSpeed   = 0.6f; // fraction of edge per second
    private const float PacketRadius  = 24f;
    private const float RoadBaseWidth = 5f;
    private const float RoadCapWidth  = 5f;   // extra width per unit of capacity

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

    // One per tick: spawn a moving packet for each inter-nation trade that happened.
    public void OnTradesProcessed(IReadOnlyList<(Vector2I From, Vector2I To, Good Good, float Amount, float Price)> trades)
    {
        if (_tml == null || trades == null) return;
        foreach (var t in trades)
            _packets.Add(new Packet
            {
                Start = _tml.MapToLocal(t.From),
                End = _tml.MapToLocal(t.To),
                Progress = 0f,
                Color = GoodColor(t.Good),
            });
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        for (int i = _packets.Count - 1; i >= 0; i--)
        {
            _packets[i].Progress += PacketSpeed * dt;
            if (_packets[i].Progress >= 1f) _packets.RemoveAt(i);
        }
        QueueRedraw(); // roads recolor per tick; packets move per frame
    }

    public override void _Draw()
    {
        if (_economy == null || _tml == null) return;

        // Roads: one line per route, colored by congestion and widened by capacity.
        foreach (TradeRoute route in _economy.TradeRoutes)
        {
            Vector2 a = _tml.MapToLocal(route.A.Capital);
            Vector2 b = _tml.MapToLocal(route.B.Capital);
            float cap = Mathf.Max(route.CapacityLastTick, 0f);
            float congestion = cap > 0.01f ? Mathf.Clamp(route.FlowLastTick / cap, 0f, 1f) : 0f;

            bool blocked = route.Blocked ||
                _economy.GetRelation(route.A.Capital, route.B.Capital) == DiplomaticStance.Hostile;
            Color color;
            if (blocked)
                color = new Color(0.85f, 0.15f, 0.15f, 0.9f);                                   // cut — red
            else if (route.FlowLastTick <= 0f)
                color = new Color(0.35f, 0.35f, 0.40f, 0.6f);                                   // idle — dim gray
            else
                color = new Color(0.30f, 0.90f, 0.40f).Lerp(new Color(1f, 0.55f, 0.10f), congestion); // green→orange

            DrawLine(a, b, color, RoadBaseWidth + RoadCapWidth * cap);
        }

        // Packets: real trades in motion.
        foreach (Packet p in _packets)
            DrawCircle(p.Start.Lerp(p.End, p.Progress), PacketRadius, p.Color);
    }
}
