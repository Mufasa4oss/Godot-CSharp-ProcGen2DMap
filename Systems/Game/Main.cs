using Godot;
using System;
using System.Collections.Generic;

public partial class Main : Node2D
{
	// --- Godot exports & scene node refs ---
	[Export] NoiseTexture2D Nht;
	[Export] int numProvinces = 1;
	Noise noise;
	TileMapLayer tml;
	UIManager control;
	//CHANGES 23-Jun-26 tiles// one canonical world seed (terrain + deposits)
	int worldSeed;
	//END CHANGES 23-Jun-26 tiles//

	// --- Game state ---
	GameContext gameContext;
	List<PrimsAlgorithmSimple.Edge> mst;
	Controls controls; // input hub
	WorkshopSprites workshopSprites; // animated sprites for player-built workshops
	GainFeedback gainFeedback; // fading "+N" flashes over producing buildings (M4.5 sub-rung 4)

	// Carries a loaded save across ReloadCurrentScene(): static lives on the type, so it survives the
	// scene teardown; _Ready reads it to start worldgen from the saved seed, then clears it.
	public static GameData PendingLoad;

	// --- Map config ---
	public static int MapSizeX = 256;
	public static int MapSizeY = 128;
	public static event Action<Texture2D> MapTextureCreated;

	public Main() { }

	public override void _Ready()
	{
		noise = Nht.Noise;
		FastNoiseLite noiseTexture = noise as FastNoiseLite;

		// Input hub, created first so collaborators (MapManager) can connect to its signals.
		controls = new Controls();
		AddChild(controls);
		controls.SaveRequested += OnSaveRequested;
		controls.LoadRequested += OnLoadRequested;

		// New game = fresh random seed. Loaded game = re-run worldgen from the saved seed so the whole
		// world (terrain, provinces, deposits, MST railroads) regenerates identically.
		GameData loaded = PendingLoad;
		PendingLoad = null;
		if (loaded != null)
		{
			MapSizeX = loaded.Width;
			MapSizeY = loaded.Height;
			numProvinces = loaded.NumProvinces;
			worldSeed = loaded.Seed;
			noiseTexture.Set("frequency", loaded.Frequency); // reload reset the SubResource to scene defaults
		}
		else
		{
			worldSeed = new Random().Next();
		}
		noiseTexture.Set("seed", worldSeed);
		GD.Print("Seed: " + worldSeed);

		Nht.Height = MapSizeY;
		Nht.Width = MapSizeX;

		tml     = GetNode<TileMapLayer>("0zTileMapLayer");
		control = GetNode<UIManager>("UIManager");

		gameContext = CreateObjs();

		// Hand the UI the economy so it can show the player nation's treasury + stockpile + flows.
		control.SetEconomy(gameContext.economy);

		// Restore tick state on load, now that the TickManager exists.
		if (loaded != null)
		{
			gameContext.tickManager.currentTick  = loaded.Tick;
			gameContext.tickManager.tickInterval = loaded.tickInterval;
			gameContext.tickManager.tickTimer    = loaded.tickTimer;
			// Worldgen rebuilt deposits (full reserves) + nations (empty stockpiles) from the seed;
			// overwrite that accumulated state with the saved values (incl. re-placing player buildings).
			gameContext.economy.ApplySavedState(loaded.DepositReserves, loaded.NationStocks, loaded.Buildings, loaded.Railroads);
		}

		// Spawn sprites for any workshops now in the economy (loaded games; none on a new game).
		workshopSprites.Initialize();

		// "+N" gain flashes: after each tick, drain the economy's production events onto their tiles.
		// (gainFeedback itself is created in CreateObjs so MapManager can flash "-cost" on a build.)
		gameContext.tickManager.TickProcessed += () =>
		{
			foreach (var (tile, good, amount) in gameContext.economy.ProducedThisTick)
				gainFeedback.Flash(tile, good, amount);
		};

		PopupSceneLoad();
		mst = BuildMst();

		// M6: hand the economy the trade backbone (the MST over capitals) so goods auto-arbitrage between
		// connected nations. Rebuilt deterministically from the seed, so this runs for new and loaded games.
		// M6 phase 2: also hand in each route's corridor (the L-path tiles between its two capitals) so player
		// railroad tiles laid along a route raise its trade capacity (a "connector road").
		var tradeCapitals = gameContext.voronoiDiagram.GetCapitals();
		var routeCorridors = new List<HashSet<Vector2I>>();
		foreach (var edge in mst)
			routeCorridors.Add(new HashSet<Vector2I>(GetTilesAlongLine(tradeCapitals[edge.From], tradeCapitals[edge.To])));
		gameContext.economy.SetTradeGraph(mst, tradeCapitals, routeCorridors);

		DrawMSTLines();

		TradeNetworkVisualizer tradeVisualizer = new TradeNetworkVisualizer();
		AddChild(tradeVisualizer);
		tradeVisualizer.Initialize(gameContext.economy, tml);
		// M7: each tick, hand the visualizer that tick's real inter-nation trades so it animates actual flows
		// (and recolors roads by congestion from the live TradeRoutes).
		gameContext.tickManager.TickProcessed += () => tradeVisualizer.OnTradesProcessed(gameContext.economy.TradedThisTick);

		//CHANGES 23-Jun-26 tiles// deposit dots overlay (gray=ore, neon-yellow=grain)
		DepositOverlay depositOverlay = new DepositOverlay();
		AddChild(depositOverlay);
		depositOverlay.Initialize(gameContext.economy, tml);
		//END CHANGES 23-Jun-26 tiles//

		// Red outline around the player nation's territory.
		TerritoryOutline territoryOutline = new TerritoryOutline();
		AddChild(territoryOutline);
		territoryOutline.Initialize(gameContext.economy, tml);
	}

	private GameContext CreateObjs()
	{
		// Second noise field for deserts: a low-frequency aridity map, seeded from the one world seed
		// (offset so it's decorrelated from the height field) so deserts reproduce on save/load.
		var moisture = new FastNoiseLite
		{
			NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
			Seed = worldSeed + 7919,
			// Higher than the height field so several distinct dry pockets land per map (a very low
			// frequency made deserts feast-or-famine — a single trough either hit a continent or missed).
			Frequency = 0.03f,
		};

		var init = new Initialize(tml, noise, moisture, MapSizeX, MapSizeY);
		List<Vector2I> mapCoordsLand = init.PlaceTiles();

		// Phase 2: realistic blended ground, rendered from the terrain mask. Sits under the tilemap, so
		// it's hidden behind the (still opaque) ground tiles until Phase 3 makes them transparent.
		var ground = new GroundRenderer();
		AddChild(ground);
		ground.Initialize(init.TerrainMask, MapSizeX, MapSizeY);

		// Bitmask still built (GameContext carries it), but grass autotiling is no longer rendered:
		// ground tiles are transparent and the look comes from GroundRenderer's shader, so we skip
		// OrientCells() (it would also fail now that Grass is a single-tile source).
		Bitmask bitmaskObj = new Bitmask(tml, MapSizeX, MapSizeY);

		VoronoiDiagram vD = new VoronoiDiagram(MapSizeX, MapSizeY, numProvinces, mapCoordsLand, tml, worldSeed);

		//CHANGES 23-Jun-26 tiles// build economy with the single worldSeed; add land cross-check + deposits summary
		Economy economy = new Economy(vD.provincesDictionary, vD.GetCapitals(), worldSeed);

		int sumNationTiles = 0;
		foreach (var nation in economy.Nations.Values) sumNationTiles += nation.Tiles.Count;
		string tilesOk = sumNationTiles == mapCoordsLand.Count ? "OK" : "MISMATCH";
		GD.Print($"Economy: {economy.Nations.Count} nations, {mapCoordsLand.Count} land tiles");
		GD.Print($"tiles check: sum(nation tiles)={sumNationTiles} ({tilesOk})");

		int oreDeposits = 0, grainDeposits = 0, depositTiles = 0;
		foreach (var dep in economy.Deposits)
		{
			depositTiles += dep.Size;
			if (dep.Good == Good.Ore) oreDeposits++; else grainDeposits++;
		}
		GD.Print($"Deposits: {economy.Deposits.Count} deposits over {depositTiles} tiles (ore={oreDeposits}, grain={grainDeposits})");

		TickManager tickManager = new TickManager(economy);

		// Animated workshop sprites (created before MapManager so a successful build can spawn one).
		workshopSprites = new WorkshopSprites();
		AddChild(workshopSprites);
		workshopSprites.SetContext(economy, tml);

		// "+N"/"-cost" floating flashes (created before MapManager so a build click can flash its cost).
		gainFeedback = new GainFeedback();
		AddChild(gainFeedback);
		gainFeedback.SetContext(tml);

		MapManager mapManager = new MapManager(vD, tml, Nht, numProvinces, tickManager, economy, controls, workshopSprites, gainFeedback);
		AddChild(mapManager);
		//END CHANGES 23-Jun-26 tiles//

		var provinceTexture = vD.coloredProMap;
		MapTextureCreated?.Invoke(provinceTexture);

		//CHANGES 23-Jun-26// GameContext now carries economy instead of resourceManager + tilesResources
		return new GameContext(bitmaskObj, vD, economy, tickManager, mapManager, provinceTexture, noise, MapSizeX, MapSizeY, tml);
		//END CHANGES 23-Jun-26//
	}

	public void PopupSceneLoad()
	{
		var popupScene = ResourceLoader.Load<PackedScene>("res://Scenes/ResourcePopup.tscn").Instantiate();
		AddChild(popupScene);

		Timer timer = new Timer();
		timer.WaitTime = 5.0f;
		timer.OneShot = true;
		AddChild(timer);
		timer.Start();
		timer.Timeout += () =>
		{
			((ResourcePopup)popupScene).StartFadeOut();
			timer.QueueFree();
		};
	}

	public List<PrimsAlgorithmSimple.Edge> BuildMst()
	{
		return new MstBuilder(gameContext).Build();
	}

	// Builds a fully-connected graph of capitals weighted by straight-line distance, then returns
	// its minimum spanning tree (the backbone the railroads are drawn along).
	public class MstBuilder
	{
		int capitalCount;
		List<Vector2I> capitals;

		public MstBuilder(GameContext gc)
		{
			capitals     = gc.voronoiDiagram.GetCapitals();
			capitalCount = capitals.Count;
		}

		public List<PrimsAlgorithmSimple.Edge> Build()
		{
			PrimsAlgorithmSimple prim = new PrimsAlgorithmSimple(capitalCount);

			for (int i = 0; i < capitalCount; i++)
				for (int j = i + 1; j < capitalCount; j++)
					prim.AddEdge(i, j, (capitals[i] - capitals[j]).Length());

			return prim.FindMST();
		}
	}

	public override void _Process(double delta)
	{
		gameContext.tickManager.Update((float)delta);
	}

	// F5 — save current state. Driven by the Controls signal hub.
	private void OnSaveRequested()
	{
		new InitSaveAndLoad(gameContext).initializeSave();
	}

	// F9 — load: stash the save, then reload the scene (deferred so we don't tear down mid-signal).
	// _Ready picks up PendingLoad and regenerates the world from the saved seed.
	private void OnLoadRequested()
	{
		GameData data = Serialization.Load();
		if (data != null)
		{
			PendingLoad = data;
			GetTree().CallDeferred(SceneTree.MethodName.ReloadCurrentScene);
		}
	}

	private void DrawMSTLines()
	{
		var capitals   = gameContext.voronoiDiagram.GetCapitals();
		var capitalSet = new HashSet<Vector2I>(capitals);
		TileMapLayer railroadLayer = tml.GetNode<TileMapLayer>("RailroadLayer");
		HashSet<Vector2I> railroadTiles = new HashSet<Vector2I>();

		foreach (var edge in mst)
			foreach (Vector2I tile in GetTilesAlongLine(capitals[edge.From], capitals[edge.To]))
				if (!capitalSet.Contains(tile))
					railroadTiles.Add(tile);

		foreach (Vector2I tile in railroadTiles)
			railroadLayer.SetCell(tile, RailroadTiles.Rails, Vector2I.Zero);

		new RailroadBitmask().OrientRailroadCells(railroadLayer, railroadTiles, capitalSet, tml);

		// Re-draw the player's paid-for railroads (a loaded game; none on a new game). Plain cells, matching
		// the live placement in MapManager — these aren't part of the deterministic MST backbone.
		foreach (Vector2I tile in gameContext.economy.Railroads)
			railroadLayer.SetCell(tile, RailroadTiles.Rails, Vector2I.Zero);
	}

	private List<Vector2I> GetTilesAlongLine(Vector2I from, Vector2I to)
	{
		// Railroad-style L-path: one straight run along the major axis, a single
		// 90° corner, then a straight run along the minor axis. This avoids the
		// per-tile zig-zag a diagonal Bresenham walk produces, so track reads as
		// long straight lines like real rail instead of stair-stepped corners.
		var tiles = new List<Vector2I>();
		int x = from.X, y = from.Y;
		int sx = to.X >= from.X ? 1 : -1;
		int sy = to.Y >= from.Y ? 1 : -1;
		bool horizontalFirst = Math.Abs(to.X - from.X) >= Math.Abs(to.Y - from.Y);

		if (horizontalFirst)
		{
			while (x != to.X) { tiles.Add(new Vector2I(x, y)); x += sx; }
			while (y != to.Y) { tiles.Add(new Vector2I(x, y)); y += sy; }
		}
		else
		{
			while (y != to.Y) { tiles.Add(new Vector2I(x, y)); y += sy; }
			while (x != to.X) { tiles.Add(new Vector2I(x, y)); x += sx; }
		}

		tiles.Add(new Vector2I(x, y)); // destination
		return tiles;
	}
}
