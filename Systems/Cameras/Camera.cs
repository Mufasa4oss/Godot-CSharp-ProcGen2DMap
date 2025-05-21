using Godot;
using System;
using System.Runtime.CompilerServices;

public partial class Camera : Camera2D
{
	
	[Export] public float PanSpeed = 1500f;
	[Export] public float ZoomSpeed = 0.1f;
	[Export] public Vector2 ZoomLimits = new Vector2(0.5f, 3f);
	ShaderMaterial shaderMat = new ShaderMaterial();
	


	public override void _Ready()
	{
		//Main m = new Main();
		//Position = new Vector2(1000,1000);
		Align();
		var size = GetViewport().GetVisibleRect();
		LimitLeft = -1000;
		LimitTop = -1000;
		LimitRight = (Main.MapSizeX * 64) + 1000; // number of tiles * resolution of tiles + random number to see past map into null zone
		LimitBottom = (Main.MapSizeX * 64) + 1000; // I multiply the map size by 64 because I need the number of pixels not tiles
												   //GD.Print(Position);
												   //GD.Print(size);

		shaderMat.Shader = ResourceLoader.Load<Shader>("res://Shaders/tr.gdshader");

		shaderMat.SetShaderParameter("screen_texture_size", new Vector2(Main.MapSizeX * 64, Main.MapSizeY * 64) * Zoom);

		/*ShaderMaterial shade = new ShaderMaterial();
	shade.Shader = ResourceLoader.Load<Shader>("res://tr.gdshader");
	tRect.Material = shade;
	shade.SetShaderParameter("province_map", provinceTexture);
	shade.SetShaderParameter("screen_texture_size", new Vector2(MapSizeX, MapSizeY));*/

	}

	public override void _Process(double delta)
	{
	   
		
		shaderMat.SetShaderParameter("screen_texture_size", new Vector2(Main.MapSizeX * 64, Main.MapSizeX * 64) * Zoom);
		

		float adjustedPanSpeed = PanSpeed/Zoom.X;
		Vector2 pan = Vector2.Zero;
		if (Input.IsActionJustPressed("ZoomOut") && Zoom.X > ZoomLimits.X)
		{
			Zoom = Zoom * (1f - ZoomSpeed);
		}
		if (Input.IsActionJustPressed("ZoomIn") && Zoom.X < ZoomLimits.Y)
		{
			Zoom = Zoom * (1f + ZoomSpeed);
		}
		if (Input.IsActionPressed("PanUp") && Position.Y > LimitTop)
		{
			pan.Y -= adjustedPanSpeed * (float)delta;
		}
		if (Input.IsActionPressed("PanDown") && Position.Y <= LimitBottom)
		{
			pan.Y += adjustedPanSpeed * (float)delta;
		}
		if (Input.IsActionPressed("PanRight") && Position.X <= LimitRight)
		{
			pan.X += adjustedPanSpeed * (float)delta;
		}
		if (Input.IsActionPressed("PanLeft") && Position.X > LimitLeft)
		{
			pan.X -= adjustedPanSpeed * (float)delta;
		}
		Position += pan;
		//GD.Print(Position);
		//GD.Print("Zoom is:" + Zoom); //un-comment for posistion and zoom value tracking

	}
}
