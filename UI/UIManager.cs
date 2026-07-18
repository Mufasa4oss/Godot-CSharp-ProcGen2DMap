using Godot;
using System;
using System.Collections.Generic;

public partial class UIManager : Control
{
    // --- City Panel ---
    private PanelContainer _panel;
    private VBoxContainer _vbox;
    private Label _header;

    private readonly Dictionary<ResourceType, Label> _demandLabels = new();
    private readonly Dictionary<Population.PopTypes, Label> _popTypeLabels = new();
    private Label _totalPopLabel;

    // --- Building Panel (now external UI script) ---
    private BuildingUI _buildingUI;

    // --- Game Data ---
    private TileMapLayer _tml;
    private Dictionary<Vector2I, City> _cities;
    private City _selectedCity;

    private Buildings _buildings;
    private TileMapLayer _buildingsLayer;
    private Building.BuildingType? _placingType;

    // The build buttons, and which type each one places. Kept so a single shared handler can ask the
    // group which button is pressed and look the answer up here. Keyed by Godot's instance id rather
    // than the button object: the id is a plain number that is guaranteed stable for the node's life,
    // so the lookup can't miss even if Godot hands us a different C# wrapper for the same button.
    private ButtonGroup _buildGroup;
    private readonly Dictionary<ulong, Building.BuildingType> _buildButtons = new();

    // --- Dragging ---
    private bool _dragging;
    private Vector2 _grabOffset;

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
        // --- City Panel Setup ---
        _panel = GetNode<PanelContainer>("PanelContainer");
        _vbox = GetNode<VBoxContainer>("PanelContainer/VBox");

        _panel.MouseFilter = MouseFilterEnum.Stop;
        _vbox.MouseFilter = MouseFilterEnum.Ignore;
        _panel.GuiInput += OnPanelInput;

        _header = new Label { Text = "Click a city", MouseFilter = MouseFilterEnum.Ignore };
        _vbox.AddChild(_header);

        // Resource demand labels
        foreach (ResourceType type in Enum.GetValues<ResourceType>())
        {
            var label = new Label { Text = $"{type} Demand: 0", MouseFilter = MouseFilterEnum.Ignore };
            _demandLabels[type] = label;
            _vbox.AddChild(label);
        }

        // Total population
        _totalPopLabel = new Label { Text = "Total Population: 0", MouseFilter = MouseFilterEnum.Ignore };
        _vbox.AddChild(_totalPopLabel);

        // Population breakdown
        foreach (Population.PopTypes type in Enum.GetValues<Population.PopTypes>())
        {
            var label = new Label { Text = $"{type}: 0", MouseFilter = MouseFilterEnum.Ignore };
            _popTypeLabels[type] = label;
            _vbox.AddChild(label);
        }

        // --- Build Buttons ---
        var buildRow = new HBoxContainer();
        _vbox.AddChild(buildRow);

        _buildGroup = new ButtonGroup { AllowUnpress = true };

        foreach (Building.BuildingType type in Enum.GetValues<Building.BuildingType>())
        {
            var button = new Button
            {
                Text = type.ToString(),
                ToggleMode = true,
                ButtonGroup = _buildGroup
            };

            _buildButtons[button.GetInstanceId()] = type;
            button.Toggled += OnBuildToggled;
            buildRow.AddChild(button);
        }

        // --- Building UI (external panel) ---
        _buildingUI = GetNode<BuildingUI>("BuildingPanelContainer");
    }

    public void Update(long gametick)
    {
        RefreshCity();
        _buildingUI.Refresh();
    }

    // A build button was toggled. Every button shares this one handler, so instead of each button
    // remembering its own type, we ask the group which button is pressed right now and look it up.
    // That also makes the order the press/unpress signals arrive in irrelevant — we only ever read the
    // final state. The 'on' flag is ignored for the same reason.
    private void OnBuildToggled(bool on)
    {
        BaseButton pressed = _buildGroup.GetPressedButton();
        if (pressed == null)
        {
            _placingType = null;   // nothing armed — clicking the armed button again unpresses it
            return;
        }

        _placingType = _buildButtons[pressed.GetInstanceId()];
    }

    // Disarm build mode and pop the pressed button back out, so the armed state and what the player
    // sees can't disagree. Unpressing fires OnBuildToggled, which clears _placingType on its own; the
    // assignment below covers the case where nothing was pressed to begin with.
    private void CancelPlacement()
    {
        BaseButton pressed = _buildGroup.GetPressedButton();
        if (pressed != null)
            pressed.ButtonPressed = false;

        _placingType = null;
    }

    // --- Dragging the City Panel ---
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

    // --- Input Handling ---
    public override void _UnhandledInput(InputEvent @event)
    {
        if (_cities == null) return;
        if (@event is not InputEventMouseButton mb || !mb.Pressed) return;

        // Right-click anywhere closes the building panel and cancels build mode.
        if (mb.ButtonIndex == MouseButton.Right)
        {
            _buildingUI.HidePanel();
            CancelPlacement();
            return;
        }

        if (mb.ButtonIndex != MouseButton.Left) return;

        Vector2I cell = _tml.LocalToMap(_tml.GetLocalMousePosition());

        // Building placement
        if (_placingType is Building.BuildingType type)
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
            _buildingUI.ShowBuilding(building);
            return;
        }

        // Inspect city
        if (_cities.TryGetValue(cell, out City city))
        {
            ShowCity(city);
        }
    }

    // --- City UI ---
    private void ShowCity(City city)
    {
        _selectedCity = city;
        RefreshCity();
    }

    private void RefreshCity()
    {
        if (_selectedCity == null) return;

        _header.Text = $"City {_selectedCity.Location}";

        var demand = _selectedCity.Demand.PerResourceDemand();
        foreach (ResourceType type in Enum.GetValues<ResourceType>())
            _demandLabels[type].Text = $"{type} Demand: {demand[type]}";

        _totalPopLabel.Text = $"Total Population: {_selectedCity.Population.totalPopulation}";

        foreach (var kvp in _selectedCity.Population.Composition)
            _popTypeLabels[kvp.Key].Text = $"{kvp.Key}: {kvp.Value}";
    }
}
