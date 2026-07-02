using Godot;
using System;
using System.Collections.Generic;

public partial class MapManager : TileMapLayer
{
	private VoronoiDiagram vD;
	private TileMapLayer tml;
	private Sprite2D nations;
	private TileMapLayer _highlightLayer;   // overlay layer for the selected-nation tint (never the ground data)
	private const int HighlightSource = 2;  // BorderHighlight.tres source 2 = translucent highlight tile
	private TickManager tickManager;
	private int numNations;
	private NoiseTexture2D noiseTexture;
	private UIManager _uiManager;
	private TileMapLayer _railroadLayer;
	//CHANGES 23-Jun-26// economy ref for click-inspect
	private Economy _economy;
	//END CHANGES 23-Jun-26//
	private Controls _controls; // input hub: map-click + nations-toggle signals
	private WorkshopSprites _workshopSprites; // spawns a sprite when the player builds a workshop
	private GainFeedback _gainFeedback; // flashes a fading "-cost" over a tile when a build is paid for
	private StorageOverlay _storageOverlay; // LR6: draws a marker when the player builds a Granary/Warehouse
	private ExportPanel _exportPanel; // LR6: opens when the player clicks an owned Granary/Warehouse
	private Vector2I? _lastRoadDragTile;   // LR1: anchor for drag-to-lay road interpolation (null = no active drag)

	public MapManager(VoronoiDiagram vD, TileMapLayer tml, NoiseTexture2D nht, int numNation, TickManager tm, Economy economy, Controls controls, WorkshopSprites workshopSprites, GainFeedback gainFeedback, StorageOverlay storageOverlay, ExportPanel exportPanel)
	{
		this.vD = vD;
		this.tml = tml;
		this.tickManager = tm;
		this.numNations = numNation;
		this.noiseTexture = nht;
		this._economy = economy;
		this._controls = controls;
		this._workshopSprites = workshopSprites;
		this._gainFeedback = gainFeedback;
		this._storageOverlay = storageOverlay;
		this._exportPanel = exportPanel;
	}
	public override void _Ready()
	{
		nations = GetNode<Sprite2D>("../Nations");
		_uiManager = GetNode<UIManager>("../UIManager");
		_railroadLayer = GetNode<TileMapLayer>("../0zTileMapLayer/RailroadLayer");
		_highlightLayer = GetNode<TileMapLayer>("../0zTileMapLayer/BordersLayer");
		// Connections drop automatically when this node frees (e.g. on load's scene reload).
		_controls.MapClickRequested += OnMapClick;
		_controls.NationsToggleRequested += OnToggleNations;
	}
	private void Initialize()
	{
		//nations = GetNode<Sprite2D>("Nations");
		nations.Visible = false;
		//Main.NationsSceneInitalized -= EventMethod;

	}
	// Left-click: in railroad-build mode lay track; otherwise inspect + highlight the clicked nation.
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

		// LR6: Granary/Warehouse are pure storage — no deposit needed, just an owned empty land tile, same
		// affordability/ownership rules as Workshop above.
		if (_uiManager.IsGranaryBuildMode)
		{
			if (_economy != null && _economy.TryBuildGranary(tileCoords))
			{
				_storageOverlay.AddStorage(tileCoords, BuildingType.Granary);
				_gainFeedback.FlashCost(tileCoords, _economy.TileBuildings[tileCoords].Cost);
				GD.Print($"Built Granary at {tileCoords} | treasury={_economy.PlayerNation.Treasury}");
			}
			else
			{
				GD.Print($"Can't build Granary at {tileCoords} (must be your own empty land tile, and affordable)");
			}
			return;
		}

		if (_uiManager.IsWarehouseBuildMode)
		{
			if (_economy != null && _economy.TryBuildWarehouse(tileCoords))
			{
				_storageOverlay.AddStorage(tileCoords, BuildingType.Warehouse);
				_gainFeedback.FlashCost(tileCoords, _economy.TileBuildings[tileCoords].Cost);
				GD.Print($"Built Warehouse at {tileCoords} | treasury={_economy.PlayerNation.Treasury}");
			}
			else
			{
				GD.Print($"Can't build Warehouse at {tileCoords} (must be your own empty land tile, and affordable)");
			}
			return;
		}

		// M4.5/LR1: laying road costs treasury (TryBuildRailroad charges + dedupes), a real economic spend.
		// A single click lays one tile here; holding the button and dragging lays a continuous run (see
		// _UnhandledInput). Both go through TryLayRoad so they behave identically.
		if (_uiManager.IsRailroadBuildMode)
		{
			TryLayRoad(tileCoords);
			return;
		}

		// LR6: clicking an owned Granary/Warehouse (outside build mode) opens the export panel for its
		// good instead of the usual inspect+highlight — the control panel for that resource lives on the
		// building itself.
		if (_economy != null && _economy.TileBuildings.TryGetValue(tileCoords, out Building clickedBuilding) &&
			clickedBuilding.StorageGood.HasValue &&
			_economy.TileNation.TryGetValue(tileCoords, out Nation clickedOwner) && clickedOwner.IsPlayer)
		{
			_exportPanel.Show(clickedBuilding.StorageGood.Value);
			return;
		}

		// Clear the previous nation highlight. It lives on the overlay layer, so the ground data grid
		// (water/grass/sand cells) is never touched — no terrain corruption when restoring.
		_highlightLayer.Clear();
		Vector2I capital;
		Dictionary<Vector2I, Vector2I> nationsDictionary = vD.nationsDictionary;
		nationsDictionary.TryGetValue(tileCoords, out capital);
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
		// Reuse the nation's precomputed tile set instead of LINQ-scanning all land tiles per
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

	public override void _UnhandledInput(InputEvent @event)
	{
		// LR1: ergonomic road building, active only while Build-Road mode is on so it never fights inspect/pan.
		// Left-drag lays a continuous road (each new tile under the cursor); right-click cuts a tile.
		if (_uiManager != null && _uiManager.IsRailroadBuildMode)
		{
			if (@event is InputEventMouseMotion motion && (motion.ButtonMask & MouseButtonMask.Left) != 0)
			{
				TryLayRoad(MouseTile());
				return;
			}
			if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true })
			{
				RemoveRoad(MouseTile());
				return;
			}
		}

	}

	// LR1: lay one road tile (from a click or a drag). Charges RailroadCost and paints the cell.
	// Idempotent + cheap: TryBuildRailroad no-ops on an existing tile, so dragging back over laid road
	// never double-charges.
	private void TryLayRoad(Vector2I tile)
	{
		if (_economy == null || !_economy.TryBuildRailroad(tile)) return;
		_railroadLayer.SetCell(tile, RailroadTiles.Rails, Vector2I.Zero);
		_gainFeedback.FlashCost(tile, Economy.RailroadCost);
	}

	// LR1: cut one road tile (right-click) — the "sever a road" lever.
	private void RemoveRoad(Vector2I tile)
	{
		if (_economy == null || !_economy.RemoveRailroad(tile)) return;
		_railroadLayer.EraseCell(tile);
	}

	private Vector2I MouseTile() => tml.LocalToMap(tml.ToLocal(tml.GetLocalMousePosition()));

	private void OnToggleNations()
	{
		nations.Visible = !nations.Visible;
	}
}
