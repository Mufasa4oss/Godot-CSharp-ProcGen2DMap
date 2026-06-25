using Godot;

// Renders the player's workshop animation on each workshop tile. Built entirely in code: one shared
// SpriteFrames (the 60 Blender frames) reused by an AnimatedSprite2D per workshop, positioned via the
// tilemap. Same "add to Main, read tml" pattern as DepositOverlay.
//
// TEMPORARY asset: the 60 frames are full-res (~1.1 MB each) — heavy VRAM for a tile sprite. A packed,
// downscaled sprite sheet would be the real version, and would only touch this file.
public partial class WorkshopSprites : Node2D
{
    private const int TileSize = 64;
    private const int FrameCount = 60;
    private const int Fps = 12;
    private const float SpriteTiles = 1f; // render size in tiles
    private const string FramePathFormat =
        "res://Images/Blender & Krita Assets/Blender/SpriteFrames/{0:0000}.png";

    private static SpriteFrames _shared; // built once, reused by every workshop

    private Economy _economy;
    private TileMapLayer _tml;

    public void SetContext(Economy economy, TileMapLayer tml)
    {
        _economy = economy;
        _tml = tml;
    }

    // Spawn sprites for workshops already placed (a loaded game). A new game has none yet.
    // Also warms the shared SpriteFrames now (at boot, behind the startup popup) so the first
    // workshop placed in-game doesn't pay the ~1s cost of decoding all 60 frames on click.
    public void Initialize()
    {
        GetSharedFrames();
        foreach (var (tile, building) in _economy.TileBuildings)
            if (building.Type == BuildingType.Workshop)
                AddWorkshop(tile);
    }

    public void AddWorkshop(Vector2I tile)
    {
        SpriteFrames frames = GetSharedFrames();
        Texture2D first = frames.GetFrameTexture("default", 0);
        float scale = first != null && first.GetWidth() > 0
            ? TileSize * SpriteTiles / first.GetWidth()
            : 1f;

        var sprite = new AnimatedSprite2D
        {
            SpriteFrames = frames,
            Position = _tml.MapToLocal(tile),
            Scale = Vector2.One * scale,
        };
        AddChild(sprite);
        sprite.Play("default");
    }

    private static SpriteFrames GetSharedFrames()
    {
        if (_shared != null) return _shared;

        var frames = new SpriteFrames(); // ships with a "default" animation that loops
        frames.SetAnimationSpeed("default", Fps);
        for (int i = 1; i <= FrameCount; i++)
        {
            var tex = ResourceLoader.Load<Texture2D>(string.Format(FramePathFormat, i));
            if (tex != null) frames.AddFrame("default", tex);
        }
        _shared = frames;
        return _shared;
    }
}
