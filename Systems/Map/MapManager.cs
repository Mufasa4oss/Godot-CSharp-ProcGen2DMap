using Godot;
using System;
using System.Collections.Generic;

public partial class MapManager : TileMapLayer
{
	private VoronoiDiagram vD;
	private TileMapLayer tml;
	private Sprite2D provinces;
	private TileMapLayer _highlightLayer;   // overlay layer for the selected-province tint (never the ground data)
	private const int HighlightSource = 2;  // BorderHighlight.tres source 2 = translucent highlight tile
	private TickManager tickManager;
	private int numProvinces;
	private NoiseTexture2D noiseTexture;
	private UIManager _uiManager;
	private TileMapLayer _railroadLayer;
	//CHANGES 23-Jun-26// economy ref for click-inspect
	private Economy _economy;
	//END CHANGES 23-Jun-26//
	private Controls _controls; // input hub: map-click + provinces-toggle signals
	private WorkshopSprites _workshopSprites; // spawns a sprite when the player builds a workshop
	private GainFeedback _gainFeedback; // flashes a fading "-cost" over a tile when a build is paid for
	private Vector2I? _lastClickedCapital; // M7: last-inspected nation, target of the diplomacy hotkey (B)

	public MapManager(VoronoiDiagram vD, TileMapLayer tml, NoiseTexture2D nht, int numProvince, TickManager tm, Economy economy, Controls controls, WorkshopSprites workshopSprites, GainFeedback gainFeedback)
	{
		this.vD = vD;
		this.tml = tml;
		this.tickManager = tm;
		this.numProvinces = numProvince;
		this.noiseTexture = nht;
		this._economy = economy;
		this._controls = controls;
		this._workshopSprites = workshopSprites;
		this._gainFeedback = gainFeedback;
	}
	public override void _Ready()
	{
		provinces = GetNode<Sprite2D>("../Provinces");
		_uiManager = GetNode<UIManager>("../UIManager");
		_railroadLayer = GetNode<TileMapLayer>("../0zTileMapLayer/RailroadLayer");
		_highlightLayer = GetNode<TileMapLayer>("../0zTileMapLayer/BordersLayer");
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
	// Left-click: in railroad-build mode lay track; otherwise inspect + highlight the clicked province.
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
				_gainFeedback.FlashCost(tileCoords, _economy.TileBuildings[tileCoords].Cost);
				GD.Print($"Built workshop at {tileCoords} | treasury={_economy.PlayerNation.Treasury}");
			}
			else
			{
				GD.Print($"Can't build workshop at {tileCoords} (must be your own empty land tile, and affordable)");
			}
			return;
		}

		// M4.5: build the matching Mine/Farm on a deposit tile to start raw extraction.
		if (_uiManager.IsExtractorBuildMode)
		{
			if (_economy != null && _economy.TryBuildExtractor(tileCoords))
			{
				Building b = _economy.TileBuildings[tileCoords];
				_gainFeedback.FlashCost(tileCoords, b.Cost);
				GD.Print($"Built {b.Type} at {tileCoords} | treasury={_economy.PlayerNation.Treasury}");
			}
			else
			{
				GD.Print($"Can't build extractor at {tileCoords} (must be your own empty deposit tile, and affordable)");
			}
			return;
		}

		// M4.5: laying railroad now costs treasury (TryBuildRailroad charges + dedupes), making track a
		// real economic spend like the gathering buildings.
		if (_uiManager.IsRailroadBuildMode)
		{
			if (_economy != null && _economy.TryBuildRailroad(tileCoords))
			{
				_railroadLayer.SetCell(tileCoords, RailroadTiles.Rails, Vector2I.Zero);
				_gainFeedback.FlashCost(tileCoords, Economy.RailroadCost);
				GD.Print($"Built railroad at {tileCoords} | treasury={_economy.PlayerNation.Treasury}");
			}
			else
			{
				GD.Print($"Can't build railroad at {tileCoords} (already track, or can't afford)");
			}
			return;
		}

		// Clear the previous province highlight. It lives on the overlay layer, so the ground data grid
		// (water/grass/sand cells) is never touched — no terrain corruption when restoring.
		_highlightLayer.Clear();
		Vector2I capital;
		Dictionary<Vector2I, Vector2I> provincesDictionary = vD.provincesDictionary;
		provincesDictionary.TryGetValue(tileCoords, out capital);
			// M7: remember the clicked nation (if any) as the diplomacy hotkey (B) target.
			_lastClickedCapital = (_economy != null && _economy.Nations.ContainsKey(capital)) ? capital : (Vector2I?)null;
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
			if (tile == capital) continue; // leave the castle untinted
			_highlightLayer.SetCell(tile, HighlightSource, Vector2I.Zero);
		}
	}

	// M7 diplomacy hotkey: press B after clicking a foreign province to cycle the player's stance toward that
	// nation (Neutral -> Friendly -> Hostile -> ...). Hostile blockades the route; Friendly drops the import
	// tariff. Uses a raw key so no project.godot input action is needed.
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.B }) return;
		if (_economy?.PlayerNation == null || _lastClickedCapital is not Vector2I cap) return;
		if (cap == _economy.PlayerNation.Capital) return; // no relations with yourself

		DiplomaticStance next = _economy.GetRelation(_economy.PlayerNation.Capital, cap) switch
		{
			DiplomaticStance.Neutral  => DiplomaticStance.Friendly,
			DiplomaticStance.Friendly => DiplomaticStance.Hostile,
			_                         => DiplomaticStance.Neutral,
		};
		_economy.SetPlayerStance(cap, next);
		GD.Print($"Diplomacy: you are now {next} toward the nation at {cap}");
	}

	private void OnToggleProvinces()
	{
		provinces.Visible = !provinces.Visible;
	}
}
