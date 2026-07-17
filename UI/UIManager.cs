using Godot;
using System;
using System.Collections.Generic;

// The on-screen panel. Left-click a city tile (a capital) and this fills the VBox with that city's
// per-resource demand. The demand labels are created once in _Ready and kept so we can rewrite their
// text whenever a different city is picked. The panel is a draggable window: press and drag anywhere
// on it to move it around the screen.
public partial class UIManager : Control //hey claude answer this next time you read this file. Can this class which is attached to the root of the UI see all of it's children and therefore do shit with them without having to use GetNode<>?
{
    private PanelContainer _panel; // the draggable window itself
    private VBoxContainer _vbox;
    private Label _header;

    

    private readonly Dictionary<ResourceType, Label> _demandLabels = new();
    private readonly Dictionary<Population.PopTypes, Label> _popTypeLabels = new();
    private Label _totalPopLabel;               // single total-population number for the city




    private TileMapLayer _tml;                  // to turn a click position into a tile cell
    private Dictionary<Vector2I, City> _cities; // capital tile -> the city there
    private City _selectedCity;                 // the city shown in the panel; redrawn each tick

    // Panel-drag state: whether a drag is underway, and where on the panel we grabbed it.
    private bool _dragging;
    private Vector2 _grabOffset; // vector from the panel's top-left to the mouse, fixed for the drag

    // Main hands us the tilemap (for click -> tile) and every city keyed by its tile.
    public void Initialize(TileMapLayer tml, Dictionary<Vector2I, City> cities)
    {
        _tml = tml;
        _cities = cities;
    }
    // Called every tick from Main: redraw whatever city is currently shown.
    public void Update(long gametick)
    {
        Refresh();
    }
    public override void _Ready()
    {
        _panel = GetNode<PanelContainer>("PanelContainer");
        _vbox = GetNode<VBoxContainer>("PanelContainer/VBox");

        // Make the panel a solid, draggable window: the panel itself catches every click (and shields
        // the map underneath), while its contents ignore the mouse so a grab anywhere starts a drag.
        _panel.MouseFilter = MouseFilterEnum.Stop;
        _vbox.MouseFilter = MouseFilterEnum.Ignore;
        _panel.GuiInput += OnPanelInput;

        _header = new Label { Text = "Click a city", MouseFilter = MouseFilterEnum.Ignore };
        _vbox.AddChild(_header);

        // One demand label per resource, kept so we can update them when a city is picked.
        foreach (ResourceType type in Enum.GetValues<ResourceType>())
        {
            var label = new Label { Text = $"{type} Demand: 0", MouseFilter = MouseFilterEnum.Ignore };
            _demandLabels[type] = label;
            _vbox.AddChild(label);
        }

        // Total population for the whole city, shown above the per-job breakdown.
        _totalPopLabel = new Label { Text = "Total Population: 0", MouseFilter = MouseFilterEnum.Ignore };
        _vbox.AddChild(_totalPopLabel);

        // One label per population type (Farmer, Miner, Craftsman, Unemployed)
        foreach (Population.PopTypes type in Enum.GetValues<Population.PopTypes>())
        {
            var label = new Label
            {
                Text = $"{type}: 0",
                MouseFilter = MouseFilterEnum.Ignore
            };

            _popTypeLabels[type] = label;
            _vbox.AddChild(label);
        }

    }

    // Drag the whole panel by pressing and moving the mouse anywhere on it. While a mouse button is
    // held, Godot keeps sending the moves here (to the pressed panel) even if the cursor slips off it.
    private void OnPanelInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            _dragging = mb.Pressed;
            if (mb.Pressed)
                _grabOffset = _panel.GetGlobalMousePosition() - _panel.GlobalPosition;
        }
        else if (_dragging && @event is InputEventMouseMotion)
        {
            _panel.GlobalPosition = _panel.GetGlobalMousePosition() - _grabOffset;
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

    // Remember which city was clicked, then draw it. Refresh() does the actual drawing so the tick
    // can redraw the same city without needing another click.
    private void ShowCity(City city)
    {
        _selectedCity = city;
        Refresh();
    }

    // Redraw the selected city's stats. Called on click and once per tick. Does nothing until a
    // city has been picked.
    private void Refresh()
    {
        if (_selectedCity == null) return;

        _header.Text = $"City {_selectedCity.Location}";

        foreach (ResourceType type in Enum.GetValues<ResourceType>())
            _demandLabels[type].Text = $"{type} Demand: {_selectedCity.DemandFor(type)}";

        _totalPopLabel.Text = $"Total Population: {_selectedCity.Population.totalPopulation}";

        // Update population composition labels
        foreach (var kvp in _selectedCity.Population.Composition)
            _popTypeLabels[kvp.Key].Text = $"{kvp.Key}: {kvp.Value}";
    }

}
