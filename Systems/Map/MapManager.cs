using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using kingsofresource;

public partial class MapManager : TileMapLayer
{
	private VoronoiDiagram vD;
	private TileMapLayer tml;
	private Sprite2D provinces;
	private HashSet<Vector2I> previousTiles = null;
	private TickManager tickManager;
	private int numProvinces;
	private NoiseTexture2D noiseTexture;



	public MapManager(VoronoiDiagram vD, TileMapLayer tml, NoiseTexture2D nht, int numProvince, TickManager tm)
	{
		this.vD = vD;
		this.tml = tml;
		this.tickManager = tm;
		this.numProvinces = numProvince;
		this.noiseTexture = nht;
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
			if (previousTiles != null)
			{
				//GD.Print(previousTiles.Count);
				foreach (Vector2I tile in previousTiles)
				{
					var atlasCoords = tml.GetCellAtlasCoords(tile);
					tml.SetCell(tile, 2, atlasCoords);
				}
			}
			Vector2I capital;
			Vector2 mousePosition = tml.GetLocalMousePosition();
			Dictionary<Vector2I, Vector2I> provincesDictionary = vD.provincesDictionary; //key is any coord and the value is the (coords of) closest province center
			Vector2I tileCoords = tml.LocalToMap(tml.ToLocal(mousePosition));
			provincesDictionary.TryGetValue(tileCoords, out capital);
			Dictionary<Vector2I, HashSet<Vector2I>> allCoords = MyGlobalClass.AllCoordsWithCapital(capital, provincesDictionary); //capital:[allcordswithcapital]
																																  //Material material = ResourceLoader.Load<ShaderMaterial>("res://Shaders/BorderGlowEffect.tres");

			foreach (Vector2I tile in allCoords[capital])
			{
				var atlasCoords = tml.GetCellAtlasCoords(tile);
				tml.SetCell(tile, 4, atlasCoords);
				previousTiles = allCoords[capital];
			}
		}
		if (Input.IsActionJustPressed("QPressed"))
		{
			//GD.Print("Q pressed");
			//GD.Print("provinces: " + provinces);
			provinces.Visible = !provinces.Visible;
		}
		
		
	}
}
