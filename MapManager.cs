using Godot;
using System;
using System.Collections.Generic;

public partial class MapManager : TileMapLayer
{
	private VoronoiDiagram vD;
	private TileMapLayer tml;

	public MapManager(VoronoiDiagram vD, TileMapLayer tml)
	{
		this.vD = vD;
		this.tml = tml;
	}
	public override void _Input(InputEvent @event)
	{
		if (Input.IsActionJustPressed("LeftClick"))
		{
			Vector2I x;
			Vector2 mousePosition = tml.GetLocalMousePosition();
			Dictionary<Vector2I, Vector2I> provincesDictionary = vD.provincesDictionary; //key is any coord and the value is the (coords of) closest province center
			Vector2I tileCoords = tml.LocalToMap(tml.ToLocal(mousePosition));
			GD.Print("tileCoords: " + tileCoords);
			//GD.Print((provincesDictionary.Values).ToString());
			provincesDictionary.TryGetValue(tileCoords, out x);
			
			GD.Print("capital at: " + x);
			
		}


	}

}
