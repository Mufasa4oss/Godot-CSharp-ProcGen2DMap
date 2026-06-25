using Godot;
using System.Collections.Generic;

// Draws a red outline around the player nation's territory: for every player-owned tile, any cardinal
// edge whose neighbour is NOT player-owned is a border edge and gets a line. Built once (territory is
// static in M3) and drawn once — same no-per-frame-cost pattern as DepositOverlay.
public partial class TerritoryOutline : Node2D
{
    private const int TileSize = 64;          // world px per tile (matches the tileset)
    private const float LineWidth = 10f;      // world px; thick enough to read at the default zoom-out
    private static readonly Color OutlineColor = new Color(1f, 0.1f, 0.1f); // red

    private readonly List<(Vector2 From, Vector2 To)> _edges = new();

    // 4 cardinal neighbour offsets, paired with the tile edge they expose (relative to the cell center).
    private static readonly (Vector2I Offset, Vector2 A, Vector2 B)[] Sides =
    {
        (new Vector2I(0, -1), new Vector2(-1, -1), new Vector2( 1, -1)), // top
        (new Vector2I(1,  0), new Vector2( 1, -1), new Vector2( 1,  1)), // right
        (new Vector2I(0,  1), new Vector2(-1,  1), new Vector2( 1,  1)), // bottom
        (new Vector2I(-1, 0), new Vector2(-1, -1), new Vector2(-1,  1)), // left
    };

    public void Initialize(Economy economy, TileMapLayer tml)
    {
        _edges.Clear();
        HashSet<Vector2I> tiles = economy?.PlayerNation?.Tiles;
        if (tiles == null) return;

        float half = TileSize / 2f;
        foreach (Vector2I tile in tiles)
        {
            Vector2 center = tml.MapToLocal(tile);
            foreach (var (offset, a, b) in Sides)
                if (!tiles.Contains(tile + offset)) // edge faces non-player tile → it's a border
                    _edges.Add((center + a * half, center + b * half));
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        foreach (var (from, to) in _edges)
            DrawLine(from, to, OutlineColor, LineWidth);
    }
}
