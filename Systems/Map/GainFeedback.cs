using Godot;

// Flashes a fading "+N" over a building tile each time it produces a unit (M4.5 sub-rung 4) so the
// player sees *where & why* a gain happened — pairing with the animated building sprite. Same
// "add to Main, read tml" pattern as WorkshopSprites: Main drains Economy.ProducedThisTick after each
// tick (via TickManager.TickProcessed) and calls Flash() per event.
public partial class GainFeedback : Node2D
{
    private TileMapLayer _tml;

    public void SetContext(TileMapLayer tml) => _tml = tml;

    public void Flash(Vector2I tile, Good good, float amount)
    {
        if (_tml == null) return;

        var label = new FloatingLabel
        {
            Text = $"+{amount:0} {good}",
            // Start a little above the tile centre so it reads as rising off the building.
            Position = _tml.MapToLocal(tile) - new Vector2(0f, 32f),
            ZIndex = 2,
        };
        label.AddThemeFontSizeOverride("font_size", 48);
        label.AddThemeColorOverride("font_color", GoodColor(good));
        AddChild(label);
    }

    // Red "-cost" spend flash over a tile when the player pays to build — the counterpart to the +N gain
    // flash, so spending reads on the map the same way gaining does. Same rise+fade FloatingLabel; the
    // Modulate alpha just multiplies this red font color as it fades.
    public void FlashCost(Vector2I tile, float amount)
    {
        if (_tml == null) return;

        var label = new FloatingLabel
        {
            Text = $"-{amount:0}",
            Position = _tml.MapToLocal(tile) - new Vector2(0f, 32f),
            ZIndex = 2,
        };
        label.AddThemeFontSizeOverride("font_size", 48);
        label.AddThemeColorOverride("font_color", new Color(1f, 0.4f, 0.4f)); // red = treasury spent
        AddChild(label);
    }

    private static Color GoodColor(Good good) => good switch
    {
        Good.Grain => new Color(1f, 0.9f, 0.3f),   // wheat yellow
        Good.Ore   => new Color(0.8f, 0.8f, 0.85f), // grey
        Good.Tools => new Color(0.5f, 0.85f, 1f),   // cyan (refined)
        _ => Colors.White,
    };
}

// A self-fading, upward-drifting Label that frees itself when fully transparent.
public partial class FloatingLabel : Label
{
    private const float RiseSpeed = 40f; // pixels/sec the label drifts up
    private const float Lifetime  = 1.2f; // seconds to fully fade
    private float _elapsed;

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _elapsed += dt;
        Position -= new Vector2(0f, RiseSpeed * dt);
        float alpha = Mathf.Clamp(1f - _elapsed / Lifetime, 0f, 1f);
        Modulate = new Color(1f, 1f, 1f, alpha);
        if (_elapsed >= Lifetime) QueueFree();
    }
}
