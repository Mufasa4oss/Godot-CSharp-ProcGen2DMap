using Godot;
using System.Collections.Generic;

// Owns the two panels and turns map clicks into "place a building" or "inspect this thing".
// The panels themselves — their labels, their dragging, the build buttons — live in CityUI and BuildingUI.
public partial class UIManager : Control
{
    // --- Panels (each is its own script on its own node) ---
    private CityUI _cityUI;
    private BuildingUI _buildingUI;

    // --- Game Data ---
    private TileMapLayer _tml;
    private Dictionary<Vector2I, City> _cities;

    private Buildings _buildings;
    private TileMapLayer _buildingsLayer;

    // --- Initialization from Main ---
    public void Initialize(TileMapLayer tml, Dictionary<Vector2I, City> cities,
                           Buildings buildings, TileMapLayer buildingsLayer)
    {
        _tml = tml;
        _cities = cities;
        _buildings = buildings;
        _buildingsLayer = buildingsLayer;
    }

    public override void _Ready()
    {
        _cityUI = GetNode<CityUI>("CityPanelContainer");
        _buildingUI = GetNode<BuildingUI>("BuildingPanelContainer");
    }

    public void Update(long gametick)
    {
        _cityUI.Refresh();
        _buildingUI.Refresh();
    }

    // --- Input Handling ---
    public override void _UnhandledInput(InputEvent @event)
    {
        if (_cities == null) return;
        if (@event is not InputEventMouseButton mb || !mb.Pressed) return;

        // Right-click anywhere closes the building panel and cancels build mode.
        if (mb.ButtonIndex == MouseButton.Right)
        {
            _buildingUI.HidePanel();
            _cityUI.CancelPlacement();
            return;
        }

        if (mb.ButtonIndex != MouseButton.Left) return;

        Vector2I cell = _tml.LocalToMap(_tml.GetLocalMousePosition());

        // Building placement
        if (_cityUI.PlacingType is Building.BuildingType type)
        {
            if (_buildings.Place(type, cell))
            {
                TileId tile = Tiles.ForBuilding(type);
                _buildingsLayer.SetCell(cell, tile.Source, tile.AtlasCoords, tile.Alternative);
            }
            return;
        }

        // Inspect building
        if (_buildings.At(cell) is Building building)
        {
            _cityUI.HidePanel();
            _buildingUI.ShowBuilding(building);
            return;
        }

        // Inspect city
        if (_cities.TryGetValue(cell, out City city))
            _cityUI.ShowCity(city);
    }
}
