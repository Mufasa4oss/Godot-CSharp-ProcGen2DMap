using Godot;

// Covers the screen with grass/sand pockets that blend smoothly into each other,
// driven by a noise mask in ground_blend.gdshader. No edge-tile art required.
// [Tool] so the blend also previews live in the editor, not just at runtime.
[Tool]
public partial class GroundBlend : Sprite2D
{
	[Export] public Texture2D SandTexture;
	[Export] public Texture2D GrassTexture;

	// How many times the ground textures repeat across the screen.
	[Export] public float GroundTiles = 8.0f;

	// Size of the grass/sand pockets. Lower = bigger blobs, higher = smaller patches.
	[Export(PropertyHint.Range, "0.002,0.05,0.001")] public float PocketScale = 0.006f;

	// Higher = more sand, lower = more grass.
	[Export(PropertyHint.Range, "0,1,0.01")] public float Threshold = 0.5f;

	// Width of the blended border. 0 = hard edge, higher = softer transition.
	[Export(PropertyHint.Range, "0,0.5,0.01")] public float Softness = 0.1f;

	// 0 = different layout every run.
	[Export] public int Seed = 0;

	public override void _Ready()
	{
		SandTexture ??= GD.Load<Texture2D>("res://Images/sandSeamless.png");
		GrassTexture ??= GD.Load<Texture2D>("res://Images/grassSeamless.png");

		// Use the sand texture only to give the sprite a rect to draw into;
		// the shader overrides the actual pixels. Anchor it at the top-left and
		// stretch it to fill the whole viewport.
		Texture = SandTexture;
		Centered = false;
		Position = Vector2.Zero;
		CoverViewport();

		// Seamless noise so the pocket mask tiles without visible joins.
		var noise = new FastNoiseLite
		{
			NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
			Seed = Seed != 0 ? Seed : (int)GD.Randi(),
			Frequency = PocketScale,
		};
		var maskTexture = new NoiseTexture2D
		{
			Noise = noise,
			Seamless = true,
			Width = 512,
			Height = 512,
		};

		var material = new ShaderMaterial
		{
			Shader = GD.Load<Shader>("res://Shaders/ground_blend.gdshader"),
		};
		material.SetShaderParameter("sand_tex", SandTexture);
		material.SetShaderParameter("grass_tex", GrassTexture);
		material.SetShaderParameter("mask_noise", maskTexture);
		material.SetShaderParameter("tiles", GroundTiles);
		material.SetShaderParameter("mask_tiles", 1.0f);
		material.SetShaderParameter("threshold", Threshold);
		material.SetShaderParameter("softness", Softness);

		Material = material;
	}

	private void CoverViewport()
	{
		if (Texture == null)
			return;

		Vector2 viewport = GetViewportRect().Size;
		Scale = new Vector2(viewport.X / Texture.GetWidth(), viewport.Y / Texture.GetHeight());
	}
}
