using Godot;
using System;
using System.Collections.Generic;

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
	//CHANGES 23-Jun-26// economy ref for click-inspect
	private Economy _economy;
	//END CHANGES 23-Jun-26//
	private Controls _controls; // input hub: map-click + provinces-toggle signals
	private WorkshopSprites _workshopSprites; // spawns a sprite when the player builds a workshop

	public MapManager(VoronoiDiagram vD, TileMapLayer tml, NoiseTexture2D nht, int numProvince, TickManager tm, Economy economy, Controls controls, WorkshopSprites workshopSprites)
	{
		this.vD = vD;
		this.tml = tml;
		this.tickManager = tm;
		this.numProvinces = numProvince;
		this.noiseTexture = nht;
		this._economy = economy;
		this._controls = controls;
		this._workshopSprites = workshopSprites;
	}
	public override void _Ready()
	{
		provinces = GetNode<Sprite2D>("../Provinces");
		_uiManager = GetNode<UIManager>("../UIManager");
		_roadLayer = GetNode<TileMapLayer>("../0zTileMapLayer/RoadLayer");
		// Connections drop automatically when this node frees (e.g. on load's scene reload).
		_controls.MapClickRequested += OnMapClick;
		_controls.ProvincesToggleRequested += OnToggleProvinces;
	}
	private void Initialize()
	{
		//provinces = GetNode<Sprite2D>("Provinces");
		provinces.Visible = false;
		//Main.ProvincesSceneInitalized -= EventMethod;

	}
	// Left-click: in road-build mode lay track; otherwise inspect + highlight the clicked province.
	private void OnMapClick()
	{
		Vector2 mousePosition = tml.GetLocalMousePosition();
		Vector2I tileCoords = tml.LocalToMap(tml.ToLocal(mousePosition));

		// Build modes take precedence over inspect/highlight. Workshop first.
		if (_uiManager.IsWorkshopBuildMode)
		{
			if (_economy != null && _economy.TryBuildWorkshop(tileCoords))
			{
				_workshopSprites.AddWorkshop(tileCoords);
				GD.Print($"Built workshop at {tileCoords} | treasury={_economy.PlayerNation.Treasury}");
			}
			else
			{
				GD.Print($"Can't build workshop at {tileCoords} (must be your own empty land tile, and affordable)");
			}
			return;
		}

		if (_uiManager.IsRoadBuildMode)
		{
			_roadLayer.SetCell(tileCoords, RoadTiles.Rails, Vector2I.Zero);
			return;
		}

		if (previousTiles != null)
		{
			foreach (Vector2I tile in previousTiles)
			{
				var atlasCoords = tml.GetCellAtlasCoords(tile);
				tml.SetCell(tile, TerrainTiles.Grass, atlasCoords);
			}
		}
		Vector2I capital;
		Dictionary<Vector2I, Vector2I> provincesDictionary = vD.provincesDictionary;
		provincesDictionary.TryGetValue(tileCoords, out capital);
		//CHANGES 23-Jun-26 tiles// tile-based click-inspect: deposit + building on the clicked tile, plus owner
		if (_economy != null)
		{
			string owner = _economy.Nations.TryGetValue(capital, out Nation nation)
				? $"owner={nation.Owner} nationTiles={nation.Tiles.Count}"
				: "no nation";

			if (_economy.TileDeposits.TryGetValue(tileCoords, out Deposit dep))
			{
				string bld = _economy.TileBuildings.TryGetValue(tileCoords, out Building b) ? b.Type.ToString() : "none";
				GD.Print($"Tile {tileCoords}: {dep.Good} deposit size={dep.Size} reserve={dep.Reserve} rate={dep.Rate} building={bld} | {owner}");
			}
			else
			{
				GD.Print($"Tile {tileCoords}: no deposit | {owner}");
			}
		}
		//END CHANGES 23-Jun-26 tiles//
		// Reuse the province's precomputed tile set instead of LINQ-scanning all land tiles per
		// click (Tell-Don't-Ask: ask the Economy that owns the data).
		HashSet<Vector2I> nationTiles =
			_economy != null && _economy.Nations.TryGetValue(capital, out Nation clicked)
				? clicked.Tiles
				: new HashSet<Vector2I>();
		foreach (Vector2I tile in nationTiles)
		{
			if (tile == capital) continue; // don't recolor the capital/castle tile (TerrainTiles.Capital)
			var atlasCoords = tml.GetCellAtlasCoords(tile);
			tml.SetCell(tile, TerrainTiles.Highlight, atlasCoords);
		}
		// Track highlighted tiles for restore, but never the capital so the castle survives.
		previousTiles = new HashSet<Vector2I>(nationTiles);
		previousTiles.Remove(capital);
	}

	private void OnToggleProvinces()
	{
		provinces.Visible = !provinces.Visible;
	}
}
