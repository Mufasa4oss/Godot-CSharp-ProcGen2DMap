using Godot;
using System.Collections.Generic;
using System.Linq;

// LR4: a first-class diplomacy panel — the real UI that replaces LR0's lone "press N to cycle" hotkey. Press
// N to open/close it; it lists every other nation with its road-connection status (can you even export to
// them?), the player's current stance, and Friendly / Neutral / Hostile buttons that set export terms straight
// away (Friendly = free trade, Neutral = tariffed, Hostile = blockade). Built programmatically (like
// UIManager's nation rows) and parented to a CanvasLayer so it sits in screen space, not under the camera.
public partial class DiplomacyPanel : PanelContainer
{
    private Economy _economy;
    private VBoxContainer _vbox;
    private readonly List<(Nation Nation, Label Status)> _rows = new();

    public void Initialize(Economy economy) => _economy = economy;

    public override void _Ready()
    {
        Theme = new Theme { DefaultFontSize = 28 };
        Position = new Vector2(80, 140);
        Visible = false;

        _vbox = new VBoxContainer();
        AddChild(_vbox);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.N }) return;
        if (_economy?.PlayerNation == null) return;

        Visible = !Visible;
        if (Visible)
        {
            if (_rows.Count == 0) BuildRows();
            Refresh();
        }
    }

    // One row per other nation: a status label + the three stance buttons. Built once, the first time the
    // panel opens (the economy + nations exist by then).
    private void BuildRows()
    {
        _vbox.AddChild(new Label { Text = "— Diplomacy (N to close) —" });

        foreach (Nation n in _economy.Nations.Values.Where(n => !n.IsPlayer).OrderBy(n => n.Owner))
        {
            var row = new HBoxContainer();
            var status = new Label { CustomMinimumSize = new Vector2(440, 0) };
            row.AddChild(status);

            Nation target = n; // capture per row for the button closures
            foreach (DiplomaticStance stance in new[]
                     { DiplomaticStance.Friendly, DiplomaticStance.Neutral, DiplomaticStance.Hostile })
            {
                DiplomaticStance s = stance;
                var btn = new Button { Text = stance.ToString() };
                btn.Pressed += () =>
                {
                    _economy.SetPlayerStance(target.Capital, s);
                    Refresh();
                };
                row.AddChild(btn);
            }

            _vbox.AddChild(row);
            _rows.Add((n, status));
        }
    }

    // Refresh each row's status text from live state (current stance + whether a road actually reaches them,
    // since export only flows to road-connected neighbours).
    private void Refresh()
    {
        var graph = new RoadGraph(_economy.Railroads);
        Vector2I cap = _economy.PlayerNation.Capital;
        foreach (var (nation, status) in _rows)
        {
            DiplomaticStance stance = _economy.GetRelation(cap, nation.Capital);
            string link = graph.AreConnected(cap, nation.Capital) ? "road ✓" : "no road";
            status.Text = $"AI {nation.Owner} @ ({nation.Capital.X},{nation.Capital.Y}):  {stance}   [{link}]";
        }
    }
}
