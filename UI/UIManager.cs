using Godot;
using System.Collections.Generic;
using System.Linq;

// HUD for the player nation: treasury + stockpile (with per-tick flows), the build-mode toggles, and a
// simple all-nations stockpile panel (M4.5 sub-rung 5). Reads the economy each frame (values change once
// per tick, so this is cheap).
public partial class UIManager : Control
{
	private Label _treasuryLabel, _incomeLabel, _grainLabel, _oreLabel, _toolsLabel;
	private Button _buildRailroadButton, _buildWorkshopButton, _buildExtractorButton;
	private Button[] _buildButtons;                              // the mutually-exclusive build toggles
	private Button _sellGrainButton, _sellOreButton, _sellToolsButton;

	private PanelContainer _panel;                               // the draggable HUD panel
	private VBoxContainer _vbox;                                 // host for the dynamic nation rows
	private readonly List<(Nation nation, Label label)> _nationRows = new();

	private bool _dragging;
	private Vector2 _dragOffset;

	public bool IsRailroadBuildMode { get; private set; } = false;
	public bool IsWorkshopBuildMode { get; private set; } = false;
	public bool IsExtractorBuildMode { get; private set; } = false;

	private Economy _economy;
	public void SetEconomy(Economy economy)
	{
		_economy = economy;
		BuildNationRows();
	}

	public override void _Ready()
	{
		// Double the default UI font size for legibility (Godot's default is 16). The Theme propagates to
		// every child Control (labels + buttons) that doesn't override its own font size.
		Theme = new Theme { DefaultFontSize = 32 };

		_panel = GetNode<PanelContainer>("PanelContainer");
		_vbox = GetNode<VBoxContainer>("PanelContainer/VBox");
		_treasuryLabel = GetNode<Label>("PanelContainer/VBox/TreasuryLabel");
		_incomeLabel   = GetNode<Label>("PanelContainer/VBox/IncomeLabel");
		_grainLabel    = GetNode<Label>("PanelContainer/VBox/GrainLabel");
		_oreLabel      = GetNode<Label>("PanelContainer/VBox/OreLabel");
		_toolsLabel    = GetNode<Label>("PanelContainer/VBox/ToolsLabel");

		// Let the panel be dragged by its background/labels: make the container + display labels ignore the
		// mouse so the press falls through to the PanelContainer (which handles the drag). Buttons keep
		// capturing their own clicks, so dragging never starts on a button.
		_panel.GuiInput += OnPanelGuiInput;
		_vbox.MouseFilter = MouseFilterEnum.Ignore;
		foreach (Label l in new[] { _treasuryLabel, _incomeLabel, _grainLabel, _oreLabel, _toolsLabel })
			l.MouseFilter = MouseFilterEnum.Ignore;

		_buildRailroadButton = GetNode<Button>("PanelContainer/VBox/RailroadButton");
		_buildWorkshopButton = GetNode<Button>("PanelContainer/VBox/WorkshopButton");
		_buildExtractorButton = GetNode<Button>("PanelContainer/VBox/ExtractorButton");
		_buildButtons = new[] { _buildRailroadButton, _buildWorkshopButton, _buildExtractorButton };
		foreach (Button b in _buildButtons)
		{
			Button self = b; // capture per-iteration for the closure
			self.Toggled += pressed => OnBuildToggled(self, pressed);
		}

		// M4: sell the whole stockpile of a good for treasury income. Plain (non-toggle) buttons — one
		// press = one sale. The economy is the single owner of the sell logic; the UI just triggers it.
		_sellGrainButton = GetNode<Button>("PanelContainer/VBox/SellGrainButton");
		_sellGrainButton.Pressed += () => _economy?.SellAll(Good.Grain);
		_sellOreButton = GetNode<Button>("PanelContainer/VBox/SellOreButton");
		_sellOreButton.Pressed += () => _economy?.SellAll(Good.Ore);
		_sellToolsButton = GetNode<Button>("PanelContainer/VBox/SellToolsButton");
		_sellToolsButton.Pressed += () => _economy?.SellAll(Good.Tools);
	}

	// Build modes are mutually exclusive: turning one toggle on turns the other two off (setting their
	// ButtonPressed = false re-fires this with pressed=false, which clears their state). Modes + tints are
	// recomputed from the button states each time, so the flags always match what's shown.
	private void OnBuildToggled(Button active, bool pressed)
	{
		if (pressed)
			foreach (Button b in _buildButtons)
				if (b != active) b.ButtonPressed = false;

		IsRailroadBuildMode      = _buildRailroadButton.ButtonPressed;
		IsWorkshopBuildMode  = _buildWorkshopButton.ButtonPressed;
		IsExtractorBuildMode = _buildExtractorButton.ButtonPressed;

		foreach (Button b in _buildButtons)
			b.Modulate = b.ButtonPressed ? new Color(0.5f, 1f, 0.5f) : Colors.White;
	}

	// Drag the whole HUD panel by holding the left button on its background/labels and moving the mouse.
	// Using GetGlobalMousePosition keeps the grab point under the cursor regardless of camera zoom; the
	// target is clamped so the panel can never be dragged off-screen and lost.
	private void OnPanelGuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
		{
			_dragging = mb.Pressed;
			if (mb.Pressed) _dragOffset = _panel.GetGlobalMousePosition() - _panel.GlobalPosition;
		}
		else if (@event is InputEventMouseMotion && _dragging)
		{
			_panel.GlobalPosition = ClampToViewport(_panel.GetGlobalMousePosition() - _dragOffset);
		}
	}

	// Keep the panel fully inside the visible viewport. The HUD lives under the Camera2D, so positions are
	// in canvas/world space — convert the screen rect into that space (inverse canvas transform) and clamp
	// the panel's top-left so its whole rect stays on screen.
	private Vector2 ClampToViewport(Vector2 worldPos)
	{
		Transform2D toWorld = _panel.GetCanvasTransform().AffineInverse(); // screen → world
		Vector2 viewTopLeft     = toWorld * Vector2.Zero;
		Vector2 viewBottomRight = toWorld * GetViewportRect().Size;
		Vector2 maxPos = viewBottomRight - _panel.Size;
		return new Vector2(
			Mathf.Clamp(worldPos.X, viewTopLeft.X, Mathf.Max(viewTopLeft.X, maxPos.X)),
			Mathf.Clamp(worldPos.Y, viewTopLeft.Y, Mathf.Max(viewTopLeft.Y, maxPos.Y)));
	}

	public override void _Process(double delta)
	{
		Nation p = _economy?.PlayerNation;
		if (p == null) return;
		_treasuryLabel.Text = $"Treasury: {p.Treasury:0}";
		_incomeLabel.Text = $"Income: {_economy.TotalIncome:0}";
		_grainLabel.Text = FormatGood("Grain", Good.Grain, p);
		_oreLabel.Text   = FormatGood("Ore",   Good.Ore,   p);
		_toolsLabel.Text = FormatGood("Tools", Good.Tools, p);

		// M4.5 sub-rung 5: all nations' stockpiles, player's row distinct.
		foreach (var (nation, label) in _nationRows)
		{
			string tag = nation.IsPlayer ? "▶ You" : $"AI {nation.Owner}";
			label.Text = $"{tag}:  G {nation.Stockpile.GetValueOrDefault(Good.Grain):0}" +
						 $"  O {nation.Stockpile.GetValueOrDefault(Good.Ore):0}" +
						 $"  T {nation.Stockpile.GetValueOrDefault(Good.Tools):0}";
		}
	}

	// Build one row per nation (player first), once the economy is known. The rows hold references to the
	// Nation objects, so they keep reflecting state even after a load mutates those nations in place.
	private void BuildNationRows()
	{
		if (_economy == null || _vbox == null) return;
		foreach (var (_, label) in _nationRows) label.QueueFree();
		_nationRows.Clear();

		var header = new Label { Text = "— Nations —", MouseFilter = MouseFilterEnum.Ignore };
		_vbox.AddChild(header);
		foreach (Nation n in _economy.Nations.Values.OrderBy(n => n.Owner))
		{
			var row = new Label { MouseFilter = MouseFilterEnum.Ignore };
			if (n.IsPlayer) row.AddThemeColorOverride("font_color", new Color(0.6f, 1f, 0.6f));
			_vbox.AddChild(row);
			_nationRows.Add((n, row));
		}
	}

	// "Ore: 80 @2 (+10 / -3) — earned 160" — current stockpile, the good's unit price, last tick's
	// production (+) / consumption (-), then the lifetime treasury earned by selling this good. The
	// per-good "earned" is the breakdown that shows where income comes from (e.g. refined Tools out-
	// earning raw Ore), not just a lump total.
	private string FormatGood(string name, Good good, Nation p)
	{
		float amount   = p.Stockpile.GetValueOrDefault(good);
		float produced = _economy.PlayerProducedLastTick.GetValueOrDefault(good);
		float consumed = _economy.PlayerConsumedLastTick.GetValueOrDefault(good);

		string flow = "";
		if (produced > 0f || consumed > 0f)
		{
			var parts = new List<string>();
			if (produced > 0f) parts.Add($"+{produced:0}");
			if (consumed > 0f) parts.Add($"-{consumed:0}");
			flow = $" ({string.Join(" / ", parts)})";
		}

		float earned = _economy.IncomeOf(good);
		string earnedStr = earned > 0f ? $"  — earned {earned:0}" : "";
		return $"{name}: {amount:0} @{_economy.PriceOf(good):0}{flow}{earnedStr}";
	}
}
