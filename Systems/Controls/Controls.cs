using Godot;

// Central input layer. Two jobs:
//   1. Single source of truth for input action-name strings (defined in project.godot). Other nodes
//      (e.g. Camera's per-frame pan/zoom) reference these constants instead of magic strings.
//   2. Turns raw discrete input into semantic Godot signals so systems don't each poll Input.
//
// Godot signals (not static C# events) are used on purpose: signal connections auto-drop when either
// node frees, so the load flow's ReloadCurrentScene() can't leak stale handlers.
public partial class Controls : Node
{
    // --- action names (must match project.godot [input]) ---
    public const string Save = "f5";
    public const string Load = "f9";
    public const string ToggleNations = "QPressed";
    public const string MapClick = "LeftClick";
    public const string ZoomIn = "ZoomIn";
    public const string ZoomOut = "ZoomOut";
    public const string PanUp = "PanUp";
    public const string PanDown = "PanDown";
    public const string PanLeft = "PanLeft";
    public const string PanRight = "PanRight";

    // --- discrete intent signals ---
    [Signal] public delegate void SaveRequestedEventHandler();
    [Signal] public delegate void LoadRequestedEventHandler();
    [Signal] public delegate void NationsToggleRequestedEventHandler();
    [Signal] public delegate void MapClickRequestedEventHandler();

    public override void _UnhandledInput(InputEvent @event)
    {
        // _UnhandledInput (not _Input) so the UI still consumes clicks first.
        if (Input.IsActionJustPressed(Save)) EmitSignal(SignalName.SaveRequested);
        if (Input.IsActionJustPressed(Load)) EmitSignal(SignalName.LoadRequested);
        if (Input.IsActionJustPressed(ToggleNations)) EmitSignal(SignalName.NationsToggleRequested);
        if (Input.IsActionJustPressed(MapClick)) EmitSignal(SignalName.MapClickRequested);
    }
}
