using Godot;
using System.Collections.Generic;

// LR6: a square marker on every built Granary/Warehouse tile — tan = Granary (Grain), gray = Warehouse
// (Ore) — so storage buildings are visible on the map without new art assets. Mirrors DepositOverlay's
// cheap _Draw()-based approach; markers are static (no live pulse) since capacity/stored state is shown
// in the export panel when you click the building, not on the map itself.
public partial class StorageOverlay : Node2D
{
    private Economy _economy;
    private TileMapLayer _tml;
    private readonly List<(Vector2 Pos, Color Color)> _markers = new();
    private const float HalfSize = 10f;

    private static readonly Color GranaryColor   = new Color(0.75f, 0.6f, 0.35f); // tan
    private static readonly Color WarehouseColor = new Color(0.5f, 0.5f, 0.55f);  // gray

    // Same split as WorkshopSprites: SetContext wires the refs early (before MapManager exists, so a click
    // during play can call AddStorage right away); Initialize() does the one-time scan for buildings that
    // already existed at boot (a loaded save, or an AI that already built one), called after
    // Economy.ApplySavedState so a loaded Granary/Warehouse actually shows up.
    public void SetContext(Economy economy, TileMapLayer tml)
    {
        _economy = economy;
        _tml = tml;
    }

    public void Initialize()
    {
        foreach (var (tile, building) in _economy.TileBuildings)
            if (building.Type == BuildingType.Granary || building.Type == BuildingType.Warehouse)
                AddStorage(tile, building.Type);
    }

    public void AddStorage(Vector2I tile, BuildingType type)
    {
        Color color = type == BuildingType.Granary ? GranaryColor : WarehouseColor;
        _markers.Add((_tml.MapToLocal(tile), color));
        QueueRedraw();
    }

    public override void _Draw()
    {
        var half = new Vector2(HalfSize, HalfSize);
        foreach (var (pos, color) in _markers)
            DrawRect(new Rect2(pos - half, half * 2f), color);
    }
}
