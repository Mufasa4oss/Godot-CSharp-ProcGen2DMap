using Godot;

// Player road-building input. Press R to toggle build mode; while it's ON, a left-click paints a road
// tile on the RailroadLayer at the clicked cell and registers it (Roads.Build). While it's OFF,
// left-clicks do nothing. Build mode starts OFF.
//
// Replaces the old RegionSelector as the left-click handler. Uses _UnhandledInput so the UI consumes
// clicks first (the same rule the camera follows).
public partial class RoadPlacer : Node
{
    // Which tile to paint, in the RailroadLayer's tileset (res://tilesets/road.tres):
    //   source 0 = narrow rails, source 1 = dirt road. (0,0) is the first piece in that atlas.
    private const int RoadSource = 0;
    private static readonly Vector2I RoadAtlas = Vector2I.Zero;

    private Roads _roads;
    private TileMapLayer _tileLayer;  // terrain layer; converts the mouse position to a tile cell
    private TileMapLayer _railLayer;  // the RailroadLayer we paint road tiles onto
    private bool _enabled;            // road-build mode; starts off

    public void Initialize(Roads roads, TileMapLayer tileLayer)
    {
        _roads = roads;
        _tileLayer = tileLayer;
        _railLayer = tileLayer.GetNode<TileMapLayer>("RailroadLayer");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_roads == null) return; // not wired up yet

        // R toggles road-build mode on/off.
        if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.R)
        {
            _enabled = !_enabled;
            GD.Print($"Road placement: {(_enabled ? "ON" : "OFF")}");
            return;
        }

        // While ON, a left-click builds a road on the tile under the cursor.
        if (_enabled && @event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            Vector2I cell = _tileLayer.LocalToMap(_tileLayer.GetLocalMousePosition());
            _roads.Build(cell);
            _railLayer.SetCell(cell, RoadSource, RoadAtlas);
        }
    }
}
