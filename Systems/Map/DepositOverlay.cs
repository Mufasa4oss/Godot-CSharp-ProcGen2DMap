using Godot;
using System.Collections.Generic;

// M7 legibility: a dot on every deposit tile — gray = ore, neon-yellow = grain — that reflects its live state:
// dim when idle, brighter + bigger when an extractor is built on it (being worked), and a pulsing dark-red
// when its reserve is played out, so a depleted mine reads at a glance. Redrawn a few times a second for the
// pulse (cheap — a few hundred dots).
public partial class DepositOverlay : Node2D
{
    private Economy _economy;
    private readonly List<(Deposit Deposit, Vector2 Pos, Vector2I Tile, Color Base)> _dots = new();
    private const float DotRadius = 10f;
    private float _time;

    private static readonly Color OreColor      = new Color(0.5f, 0.5f, 0.5f);  // gray
    private static readonly Color GrainColor    = new Color(0.85f, 1f, 0.1f);   // neon-yellow
    private static readonly Color DepletedColor = new Color(0.65f, 0.1f, 0.1f); // played-out red

    public void Initialize(Economy economy, TileMapLayer tml)
    {
        _economy = economy;
        foreach (var kvp in economy.TileDeposits)
        {
            Vector2 pos = tml.MapToLocal(kvp.Key);
            Color color = kvp.Value.Good == Good.Ore ? OreColor : GrainColor;
            _dots.Add((kvp.Value, pos, kvp.Key, color));
        }
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float blink = 0.5f + 0.5f * Mathf.Sin(_time * 6f); // 0..1 pulse for depleted dots
        foreach (var (deposit, pos, tile, baseColor) in _dots)
        {
            Color color;
            float radius = DotRadius;
            if (deposit.Reserve <= 0f)
            {
                color = DepletedColor.Lerp(Colors.Black, blink); // pulse played-out deposits
            }
            else
            {
                bool worked = _economy != null && _economy.TileBuildings.ContainsKey(tile);
                color = worked ? baseColor.Lerp(Colors.White, 0.35f) : baseColor * 0.7f; // bright if mined, dim if idle
                if (worked) radius = DotRadius * 1.3f;
            }
            DrawCircle(pos, radius, color);
        }
    }
}
