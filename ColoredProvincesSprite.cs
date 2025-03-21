using Godot;
using System;

public partial class ColoredProvincesSprite : Sprite2D
{
	private bool isInitialized = false;
	Sprite2D provinceSprite;
	//Camera2D camera;

	public void InitializeProvinces(Texture2D texture)
	{
		Texture = texture;

		Vector2 textureSize = new Vector2(texture.GetWidth(), texture.GetHeight());
		Vector2 targetSize = new Vector2(512 * 64, 512 * 64); // Match your TileMap size

		Scale = targetSize / textureSize; // Scale to fit the map
		Position = targetSize / 2; // Center it
								   //camera = GetNode<Camera2D>("/root/Main/Camera2D");
								   //shader stuff really no idea how it works. But this makes thick black provincial borders
		ShaderMaterial shade = new ShaderMaterial();
		shade.Shader = ResourceLoader.Load<Shader>("res://tr.gdshader");
		Material = shade;
		shade.SetShaderParameter("province_map", Texture);
		shade.SetShaderParameter("screen_texture_size", new Vector2(512, 512));
		Main.PropertyAssigned -= Main_PropertyAssigned;

	}
	//configuration
	public override void _Ready()
	{
		Main.PropertyAssigned += Main_PropertyAssigned;
	}


	private void Main_PropertyAssigned(Texture2D t)
	{
		//GD.Print("Event triggered");
		isInitialized = true;
		InitializeProvinces(t);
	}
}
