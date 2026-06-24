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
		// world (terrain, provinces, deposits, MST roads) regenerates identically.
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

		// Restore tick state on load, now that the TickManager exists.
		if (loaded != null)
		{
			gameContext.tickManager.currentTick  = loaded.Tick;
			gameContext.tickManager.tickInterval = loaded.tickInterval;
			gameContext.tickManager.tickTimer    = loaded.tickTimer;
		}

		PopupSceneLoad();
		mst = BuildMst();

		DrawMSTLines();

		TradeNetworkVisualizer tradeVisualizer = new TradeNetworkVisualizer();
		AddChild(tradeVisualizer);
		tradeVisualizer.Initialize(mst, gameContext.voronoiDiagram.GetCapitals(), tml);

		//CHANGES 23-Jun-26 tiles// deposit dots overlay (gray=ore, neon-yellow=grain)
		DepositOverlay depositOverlay = new DepositOverlay();
		AddChild(depositOverlay);
		depositOverlay.Initialize(gameContext.economy, tml);
		//END CHANGES 23-Jun-26 tiles//
	}

	private GameContext CreateObjs()
	{
		List<Vector2I> mapCoordsLand = new Initialize(tml, noise, MapSizeX, MapSizeY).PlaceTiles();

		Bitmask bitmaskObj = new Bitmask(tml, MapSizeX, MapSizeY);
		bitmaskObj.OrientCells();

		VoronoiDiagram vD = new VoronoiDiagram(MapSizeX, MapSizeY, numProvinces, mapCoordsLand, tml, worldSeed);

		//CHANGES 23-Jun-26 tiles// build economy with the single worldSeed; add land cross-check + deposits summary
		Economy economy = new Economy(vD.provincesDictionary, vD.GetCapitals(), worldSeed);

		int sumProvinceTiles = 0;
		foreach (var prov in economy.Provinces.Values) sumProvinceTiles += prov.Tiles.Count;
		string tilesOk = sumProvinceTiles == mapCoordsLand.Count ? "OK" : "MISMATCH";
		GD.Print($"Economy: {economy.Provinces.Count} provinces, {mapCoordsLand.Count} land tiles");
		GD.Print($"tiles check: sum(province tiles)={sumProvinceTiles} ({tilesOk})");

		int oreDeposits = 0, grainDeposits = 0, depositTiles = 0;
		foreach (var dep in economy.Deposits)
		{
			depositTiles += dep.Size;
			if (dep.Good == Good.Ore) oreDeposits++; else grainDeposits++;
		}
		GD.Print($"Deposits: {economy.Deposits.Count} deposits over {depositTiles} tiles (ore={oreDeposits}, grain={grainDeposits})");

		TickManager tickManager = new TickManager(economy);

		MapManager mapManager = new MapManager(vD, tml, Nht, numProvinces, tickManager, economy, controls);
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
	// its minimum spanning tree (the backbone the roads are drawn along).
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
		TileMapLayer roadLayer = tml.GetNode<TileMapLayer>("RoadLayer");
		HashSet<Vector2I> roadTiles = new HashSet<Vector2I>();

		foreach (var edge in mst)
			foreach (Vector2I tile in GetTilesAlongLine(capitals[edge.From], capitals[edge.To]))
				if (!capitalSet.Contains(tile))
					roadTiles.Add(tile);

		foreach (Vector2I tile in roadTiles)
			roadLayer.SetCell(tile, RoadTiles.Rails, Vector2I.Zero);

		new RoadBitmask().OrientRoadCells(roadLayer, roadTiles, capitalSet, tml);
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
