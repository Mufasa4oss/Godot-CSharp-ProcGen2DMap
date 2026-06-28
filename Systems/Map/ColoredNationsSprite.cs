using Godot;
using System;

public partial class ColoredNationsSprite : Sprite2D
{
	//private bool isInitialized = false;
	Sprite2D nationSprite;
	//Camera2D camera;
	//public static event Action NationsSceneInitalized;

	public void InitializeNations(Texture2D texture)
	{
		Texture = texture;

		Vector2 textureSize = new Vector2(texture.GetWidth(), texture.GetHeight());
		// Match the TileMap size in pixels (tiles * 64). Derived from the map dims so a
		// non-square map scales correctly instead of being stretched to a square.
		Vector2 targetSize = new Vector2(Main.MapSizeX * 64, Main.MapSizeY * 64);

		Scale = targetSize / textureSize; // Scale to fit the map
		Position = targetSize / 2; // Center it
								   //camera = GetNode<Camera2D>("/root/Main/Camera2D");
								   //shader stuff really no idea how it works. But this makes thick black national borders
		ShaderMaterial shade = new ShaderMaterial();
		shade.Shader = ResourceLoader.Load<Shader>("res://Shaders/tr.gdshader");
		Material = shade;
		shade.SetShaderParameter("nation_map", Texture);
		shade.SetShaderParameter("screen_texture_size", new Vector2(Main.MapSizeX, Main.MapSizeY));
		Main.MapTextureCreated -= MainEventMethod;
		//NationsSceneInitalized?.Invoke();
	}
	//configuration
	public override void _Ready()
	{
		GD.Print("ColoredNationsSprite ready");

		Main.MapTextureCreated += MainEventMethod;
	}


	private void MainEventMethod(Texture2D t)
	{
		//GD.Print("Event triggered");
		//isInitialized = true;
		InitializeNations(t);
	}
}
