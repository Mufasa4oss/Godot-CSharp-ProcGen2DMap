using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using ProceduralMapGenerator;

public partial class MapManager : TileMapLayer
{
	private VoronoiDiagram vD;
	private TileMapLayer tml;
	private Sprite2D provinces;
	private HashSet<Vector2I> previousTiles = null;
	private TickManager tickManager;
	private int numProvinces;
	private NoiseTexture2D noiseTexture;
	private UIManager _uiManager;
	private TileMapLayer _roadLayer;



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
		provinces = GetNode<Sprite2D>("../Provinces");
		_uiManager = GetNode<UIManager>("../UIManager");
		_roadLayer = GetNode<TileMapLayer>("../0zTileMapLayer/RoadLayer");
	}
	private void Initialize()
	{
		//provinces = GetNode<Sprite2D>("Provinces");
		provinces.Visible = false;
		//Main.ProvincesSceneInitalized -= EventMethod;

	}
	public override void _UnhandledInput(InputEvent @event)
	{

		if (Input.IsActionJustPressed("LeftClick"))
		{
			Vector2 mousePosition = tml.GetLocalMousePosition();
			Vector2I tileCoords = tml.LocalToMap(tml.ToLocal(mousePosition));

			if (_uiManager.IsRoadBuildMode)
			{
				_roadLayer.SetCell(tileCoords, 0, Vector2I.Zero);
				return;
			}

			if (previousTiles != null)
			{
				foreach (Vector2I tile in previousTiles)
				{
					var atlasCoords = tml.GetCellAtlasCoords(tile);
					tml.SetCell(tile, 2, atlasCoords);
				}
			}
			Vector2I capital;
			Dictionary<Vector2I, Vector2I> provincesDictionary = vD.provincesDictionary;
			provincesDictionary.TryGetValue(tileCoords, out capital);
			Dictionary<Vector2I, HashSet<Vector2I>> allCoords = MyGlobalClass.AllCoordsWithCapital(capital, provincesDictionary);

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
