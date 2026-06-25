using Godot;
using System.Collections.Generic;

// HUD for the player nation: treasury + stockpile (with per-tick flows), and the build-mode toggles.
// Reads the economy each frame (values change once per tick, so this is cheap).
public partial class UIManager : Control
{
	private Label _treasuryLabel, _incomeLabel, _grainLabel, _oreLabel, _toolsLabel;
	private Button _buildRoadButton, _buildWorkshopButton;
	private Button _sellGrainButton, _sellOreButton, _sellToolsButton;

	public bool IsRoadBuildMode { get; private set; } = false;
	public bool IsWorkshopBuildMode { get; private set; } = false;

	private Economy _economy;
	public void SetEconomy(Economy economy) => _economy = economy;

	public override void _Ready()
	{
		_treasuryLabel = GetNode<Label>("PanelContainer/VBox/TreasuryLabel");
		_incomeLabel   = GetNode<Label>("PanelContainer/VBox/IncomeLabel");
		_grainLabel    = GetNode<Label>("PanelContainer/VBox/GrainLabel");
		_oreLabel      = GetNode<Label>("PanelContainer/VBox/OreLabel");
		_toolsLabel    = GetNode<Label>("PanelContainer/VBox/ToolsLabel");

		_buildRoadButton = GetNode<Button>("PanelContainer/VBox/Button");
		_buildRoadButton.Toggled += pressed => {
			IsRoadBuildMode = pressed;
			_buildRoadButton.Modulate = pressed ? new Color(0.5f, 1f, 0.5f) : Colors.White;
		};

		_buildWorkshopButton = GetNode<Button>("PanelContainer/VBox/WorkshopButton");
		_buildWorkshopButton.Toggled += pressed => {
			IsWorkshopBuildMode = pressed;
			_buildWorkshopButton.Modulate = pressed ? new Color(0.5f, 1f, 0.5f) : Colors.White;
		};

		// M4: sell the whole stockpile of a good for treasury income. Plain (non-toggle) buttons — one
		// press = one sale. The economy is the single owner of the sell logic; the UI just triggers it.
		_sellGrainButton = GetNode<Button>("PanelContainer/VBox/SellGrainButton");
		_sellGrainButton.Pressed += () => _economy?.SellAll(Good.Grain);
		_sellOreButton = GetNode<Button>("PanelContainer/VBox/SellOreButton");
		_sellOreButton.Pressed += () => _economy?.SellAll(Good.Ore);
		_sellToolsButton = GetNode<Button>("PanelContainer/VBox/SellToolsButton");
		_sellToolsButton.Pressed += () => _economy?.SellAll(Good.Tools);
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
