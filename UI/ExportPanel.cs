using Godot;

// LR6: the manual bulk-export panel. Clicking a built Granary/Warehouse (see MapManager.OnMapClick) opens
// this instead of the usual inspect+highlight — it shows how much of that building's good is stored versus
// its nation-wide capacity, and lets the player pick an amount to sell via a SpinBox. Export still auto-
// routes to buyers using Economy's existing road/tariff/blockade rules (TryExportManual); the player only
// controls how much, not who buys it. Built programmatically and parented to a CanvasLayer (like
// DiplomacyPanel) so it sits in screen space, not under the camera.
public partial class ExportPanel : PanelContainer
{
    private Economy _economy;
    private Label _statusLabel;
    private Label _resultLabel;
    private SpinBox _amountSpin;
    private Good _good;

    public void Initialize(Economy economy) => _economy = economy;

    public override void _Ready()
    {
        Theme = new Theme { DefaultFontSize = 28 };
        Position = new Vector2(80, 140);
        Visible = false;

        var vbox = new VBoxContainer();
        AddChild(vbox);

        _statusLabel = new Label();
        vbox.AddChild(_statusLabel);

        var row = new HBoxContainer();
        _amountSpin = new SpinBox { MinValue = 0, Step = 1, CustomMinimumSize = new Vector2(160, 0) };
        row.AddChild(_amountSpin);

        var exportButton = new Button { Text = "Export" };
        exportButton.Pressed += OnExportPressed;
        row.AddChild(exportButton);

        var closeButton = new Button { Text = "Close" };
        closeButton.Pressed += () => Visible = false;
        row.AddChild(closeButton);

        vbox.AddChild(row);

        _resultLabel = new Label();
        vbox.AddChild(_resultLabel);
    }

    // Called by MapManager when the player clicks a Granary/Warehouse they own — opens the panel scoped to
    // that building's good (Grain for a Granary, Ore for a Warehouse).
    public void Show(Good good)
    {
        _good = good;
        _resultLabel.Text = "";
        Visible = true;
        Refresh();
    }

    private void OnExportPressed()
    {
        float sold = _economy.TryExportManual(_good, (float)_amountSpin.Value);
        _resultLabel.Text = sold > 0f ? $"Sold {sold:0} {_good}." : "No buyer took it (check roads/blockades/price).";
        Refresh();
    }

    // Refresh the stored/capacity readout and clamp the SpinBox to what's actually available to sell.
    // "Stored" is the nation-wide total (what the Granary/Warehouse capacity gates); "available" is what's
    // physically reached the capital and can actually be exported (TryExportManual only sells from there,
    // same as the automatic RunExport) — goods still mid-haul from a distant extractor count toward storage
    // but aren't sellable yet.
    private void Refresh()
    {
        if (_economy?.PlayerNation == null) return;
        Nation player = _economy.PlayerNation;

        float stored = _economy.StoredAmountOf(player, _good);
        float capacity = _economy.StorageCapacityOf(player, _good);
        float available = _economy.TileInventory.AmountAt(player.Capital, _good);
        string capText = float.IsPositiveInfinity(capacity) ? "∞" : $"{capacity:0}";
        _statusLabel.Text = $"— Export {_good} ({stored:0} stored / {capText} capacity, {available:0} at capital) —";

        _amountSpin.MaxValue = Mathf.Max(available, 0f);
        if (_amountSpin.Value > _amountSpin.MaxValue) _amountSpin.Value = _amountSpin.MaxValue;
    }
}
