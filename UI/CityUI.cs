using Godot;
using System;
using System.Collections.Generic;

// The city panel: the labels for the selected city, the build buttons that sit under them, and dragging
// the window around. Attached to the CityPanelContainer node, the same way BuildingUI is attached to
// BuildingPanelContainer. UIManager only decides *when* to show a city; everything the panel does itself
// lives here.
public partial class CityUI : PanelContainer
{
    private VBoxContainer _vbox;
    private Label _header;
    private Label _totalPopLabel;

    private readonly Dictionary<ResourceType, Label> _demandLabels = new();
    private readonly Dictionary<Population.PopTypes, Label> _popTypeLabels = new();

    private City _selected;

    // The build buttons, and which type each one places. Kept so a single shared handler can ask the
    // group which button is pressed and look the answer up here. Keyed by Godot's instance id rather
    // than the button object: the id is a plain number that is guaranteed stable for the node's life,
    // so the lookup can't miss even if Godot hands us a different C# wrapper for the same button.
    private ButtonGroup _buildGroup;
    private readonly Dictionary<ulong, Building.BuildingType> _buildButtons = new();
    private Building.BuildingType? _placingType;

    // Which building type the player has armed, or null if none. UIManager reads this when the map is
    // clicked to decide whether the click places something.
    public Building.BuildingType? PlacingType
    {
        get { return _placingType; }
    }

    // Panel-drag state (same as BuildingUI).
    private bool _dragging;
    private Vector2 _grabOffset;

    public override void _Ready()
    {
        // The panel and its VBox already exist in the scene; everything below them is built here.
        _vbox = GetNode<VBoxContainer>("VBox");

        // Make the whole panel a draggable window: the panel itself catches every click (and shields the
        // map underneath), while its contents ignore the mouse so a grab anywhere on it starts a drag.
        // The buttons keep their own filter, so they still take clicks normally.
        MouseFilter = MouseFilterEnum.Stop;
        _vbox.MouseFilter = MouseFilterEnum.Ignore;
        GuiInput += OnPanelInput;

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

        Visible = false; // hidden until a city is clicked
    }

    public void ShowCity(City city)
    {
        _selected = city;
        Visible = true;
        Refresh();
    }

    public void Refresh()
    {
        if (_selected == null) return;

        _header.Text = $"City {_selected.Location}";

        var demand = _selected.Demand.PerResourceDemand();
        foreach (ResourceType type in Enum.GetValues<ResourceType>())
            _demandLabels[type].Text = $"{type} Demand: {demand[type]}";

        _totalPopLabel.Text = $"Total Population: {_selected.Population.totalPopulation}";

        foreach (var kvp in _selected.Population.Composition)
            _popTypeLabels[kvp.Key].Text = $"{kvp.Key}: {kvp.Value}";
    }

    public void HidePanel()
    {
        Visible = false;
        _selected = null;
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
    public void CancelPlacement()
    {
        BaseButton pressed = _buildGroup.GetPressedButton();
        if (pressed != null)
            pressed.ButtonPressed = false;

        _placingType = null;
    }

    // Drag the whole panel by pressing and moving the mouse anywhere on it. While the button is held,
    // Godot keeps sending moves here even if the cursor slips off it.
    private void OnPanelInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            _dragging = mb.Pressed;
            if (mb.Pressed)
                _grabOffset = GetGlobalMousePosition() - GlobalPosition;
        }
        else if (_dragging && @event is InputEventMouseMotion)
        {
            GlobalPosition = GetGlobalMousePosition() - _grabOffset;
        }
    }
}
