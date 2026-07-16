using Godot;
using System;
using System.Collections.Generic;

// The on-screen panel. Left-click a city tile (a capital) and this fills the VBox with that city's
// per-resource demand. The demand labels are created once in _Ready and kept so we can rewrite their
// text whenever a different city is picked.
public partial class UIManager : Control
{
    private VBoxContainer _vbox;
    private Label _header;
    private readonly Dictionary<ResourceType, Label> _demandLabels = new();

    private TileMapLayer _tml;                  // to turn a click position into a tile cell
    private Dictionary<Vector2I, City> _cities; // capital tile -> the city there

    // Main hands us the tilemap (for click -> tile) and every city keyed by its tile.
    public void Initialize(TileMapLayer tml, Dictionary<Vector2I, City> cities)
    {
        _tml = tml;
        _cities = cities;
    }

    public override void _Ready()
    {
        _vbox = GetNode<VBoxContainer>("PanelContainer/VBox");

        _header = new Label { Text = "Click a city" };
        _vbox.AddChild(_header);

        // One demand label per resource, kept so we can update them when a city is picked.
        foreach (ResourceType type in Enum.GetValues<ResourceType>())
        {
            var label = new Label { Text = $"{type} Demand: 0" };
            _demandLabels[type] = label;
            _vbox.AddChild(label);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_cities == null) return;

        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            Vector2I cell = _tml.LocalToMap(_tml.GetLocalMousePosition());
            if (_cities.TryGetValue(cell, out City city))
                ShowCity(city);
        }
    }

    // Fill the panel with one city's demand stats.
    private void ShowCity(City city)
    {
        _header.Text = $"City {city.Location}";
        foreach (ResourceType type in Enum.GetValues<ResourceType>())
            _demandLabels[type].Text = $"{type} Demand: {city.DemandFor(type)}";
    }
}
