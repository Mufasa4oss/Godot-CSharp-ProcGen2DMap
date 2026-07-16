using Godot;
using System;
using System.Runtime.CompilerServices;

public partial class Camera : Camera2D
{
	
	[Export] public float PanSpeed = 1500f;
	[Export] public float ZoomSpeed = 0.1f;
	[Export] public Vector2 ZoomLimits = new Vector2(0.1f, 3f);
	[Export] public bool DebugZoom = true; // print the zoom level to the console whenever it changes
	ShaderMaterial shaderMat = new ShaderMaterial();
	private const int TileSize = GlobalConfig.TileSizePx; // pixels per tile (matches the tileset cell size)



	public override void _Ready()
	{
		// Map size in pixels (tiles * pixels-per-tile), derived from the map dims so it works for any
		// map size instead of being hardcoded.
		Vector2 mapPixels = new Vector2(Main.MapSizeX * TileSize, Main.MapSizeY * TileSize);

		// Start dead-center on the map so there's no initial scroll from a far-off corner.
		Position = mapPixels / 2f;
		Align();

		LimitLeft = -1000;
		LimitTop = -1000;
		LimitRight = (int)mapPixels.X + 1000;  // map width in px + margin to peek past the edge
		LimitBottom = (int)mapPixels.Y + 1000; // map height in px + margin
												   //GD.Print(Position);
												   //GD.Print(size);

		shaderMat.Shader = ResourceLoader.Load<Shader>("res://Shaders/tr.gdshader");

		shaderMat.SetShaderParameter("screen_texture_size", new Vector2(Main.MapSizeX * TileSize, Main.MapSizeY * TileSize) * Zoom);
		

	}

	public override void _Process(double delta)
	{
	   
		
		shaderMat.SetShaderParameter("screen_texture_size", new Vector2(Main.MapSizeX * TileSize, Main.MapSizeY * TileSize) * Zoom);


		float adjustedPanSpeed = PanSpeed/Zoom.X;
		Vector2 pan = Vector2.Zero;
		if (Input.IsActionJustPressed(Controls.ZoomOut) && Zoom.X > ZoomLimits.X)
		{
			Zoom = Zoom * (1f - ZoomSpeed);
			if (DebugZoom) GD.Print($"Zoom: {Zoom.X:0.###}");
		}
		if (Input.IsActionJustPressed(Controls.ZoomIn) && Zoom.X < ZoomLimits.Y)
		{
			Zoom = Zoom * (1f + ZoomSpeed);
			if (DebugZoom) GD.Print($"Zoom: {Zoom.X:0.###}");
		}
		if (Input.IsActionPressed(Controls.PanUp) && Position.Y > LimitTop)
		{
			pan.Y -= adjustedPanSpeed * (float)delta;
		}
		if (Input.IsActionPressed(Controls.PanDown) && Position.Y <= LimitBottom)
		{
			pan.Y += adjustedPanSpeed * (float)delta;
		}
		if (Input.IsActionPressed(Controls.PanRight) && Position.X <= LimitRight)
		{
			pan.X += adjustedPanSpeed * (float)delta;
		}
		if (Input.IsActionPressed(Controls.PanLeft) && Position.X > LimitLeft)
		{
			pan.X -= adjustedPanSpeed * (float)delta;
		}
		Position += pan;
		//GD.Print(Position);
		//GD.Print("Zoom is:" + Zoom); //un-comment for posistion and zoom value tracking

	}

	// Middle-mouse drag panning: hold the middle button and move the mouse to grab the map. Dividing the
	// screen-space motion by Zoom keeps the grabbed point under the cursor at any zoom level. Uses
	// _UnhandledInput so the UI (which consumes events over the HUD panel) takes precedence.
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion motion && motion.ButtonMask.HasFlag(MouseButtonMask.Middle))
		{
			Position -= motion.Relative / Zoom;
		}
	}
}
