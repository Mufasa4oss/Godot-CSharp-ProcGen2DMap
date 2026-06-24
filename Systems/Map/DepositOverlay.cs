using Godot;
using System.Collections.Generic;

// Draws a small colored dot on every deposit tile: gray = ore, neon-yellow = grain.
// Static overlay — built once in Initialize and drawn once (deposits don't move), so there is
// no per-frame cost. Same pattern as TradeNetworkVisualizer.
public partial class DepositOverlay : Node2D
{
    private readonly List<(Vector2 Pos, Color Color)> _dots = new();
    private const float DotRadius = 10f;

    private static readonly Color OreColor   = new Color(0.5f, 0.5f, 0.5f);  // gray
    private static readonly Color GrainColor = new Color(0.85f, 1f, 0.1f);   // neon-yellow

    public void Initialize(Economy economy, TileMapLayer tml)
    {
        foreach (var kvp in economy.TileDeposits)
        {
            Vector2 pos = tml.MapToLocal(kvp.Key);
            Color color = kvp.Value.Good == Good.Ore ? OreColor : GrainColor;
            _dots.Add((pos, color));
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        foreach (var (pos, color) in _dots)
            DrawCircle(pos, DotRadius, color);
    }
}
