using Godot;

// Phase 2: draws the realistic blended ground for the whole map from a per-cell terrain mask, via
// terrain_ground.gdshader. One full-map sprite at the BOTTOM (z below the tilemap). Until the
// tilemap's ground tiles are made transparent (Phase 3), this renders hidden behind them.
public partial class GroundRenderer : Sprite2D
{
	private const int TilePx = GlobalConfig.TileSizePx; // map cell size in pixels (matches the TileSet tile_size)

	// mask: one texel per cell (R = sandiness, G = water), built by Initialize from the same noise
	// that decides gameplay terrain — so the look always matches the data grid.
	public void Initialize(Texture2D mask, int mapSizeX, int mapSizeY)
	{
		Vector2 mapPx = new Vector2(mapSizeX * TilePx, mapSizeY * TilePx);

		Texture2D grass = GD.Load<Texture2D>("res://Images/grassSeamless.png");
		Texture2D sand  = GD.Load<Texture2D>("res://Images/sandSeamless.png");

		// The sprite texture only defines the rect; the shader paints every pixel. Anchor top-left at
		// the origin and stretch to cover the whole map (cell (x,y) -> world (x*64, y*64)).
		Texture  = grass;
		Centered = false;
		Position = Vector2.Zero;
		Scale    = new Vector2(mapPx.X / grass.GetWidth(), mapPx.Y / grass.GetHeight());
		ZIndex   = -100; // render under the tilemap (and everything else)

		var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/terrain_ground.gdshader") };
		material.SetShaderParameter("grass_tex", grass);
		material.SetShaderParameter("sand_tex", sand);
		material.SetShaderParameter("terrain_mask", mask);
		material.SetShaderParameter("map_px", mapPx);
		Material = material;
	}
}
