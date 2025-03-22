using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;

public partial class MapManager : TileMapLayer
{
	private VoronoiDiagram vD;
	private TileMapLayer tml;
	private Sprite2D provinces;
	
	public MapManager(VoronoiDiagram vD, TileMapLayer tml)
	{
		this.vD = vD;
		this.tml = tml;
		//this.provinces = proSprite;
	}
	public override void _Ready()
	{
		//Main.ProvincesSceneInitalized += EventMethod;
		provinces = GetNode<Sprite2D>("../Provinces");
	}
	private void Initialize()
	{
		//provinces = GetNode<Sprite2D>("Provinces");
		provinces.Visible = false;
		//Main.ProvincesSceneInitalized -= EventMethod;

	}
	public override void _Input(InputEvent @event)
	{
		
			 // Prevent input handling if not yet initialized
		if (Input.IsActionJustPressed("LeftClick"))
		{
			Vector2I x;
			Vector2 mousePosition = tml.GetLocalMousePosition();
			Dictionary<Vector2I, Vector2I> provincesDictionary = vD.provincesDictionary; //key is any coord and the value is the (coords of) closest province center
			Vector2I tileCoords = tml.LocalToMap(tml.ToLocal(mousePosition));
			//GD.Print("tileCoords: " + tileCoords);
			provincesDictionary.TryGetValue(tileCoords, out x);
			//GD.Print("capital at: " + x);
		}
		if (Input.IsActionJustPressed("QPressed"))
		{
			//GD.Print("Q pressed");
			//GD.Print("provinces: " + provinces);
			provinces.Visible = !provinces.Visible;
		}
	}
}
