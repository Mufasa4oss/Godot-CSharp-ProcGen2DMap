using Godot;
using System;
using System.Collections.Generic;

public partial class Main : Node2D
{
	// --- Godot exports & scene node refs ---
	[Export] NoiseTexture2D Nht;
	[Export] int numNations = 1;
	Noise noise;
	TileMapLayer tml;
	UIManager control;
	//CHANGES 23-Jun-26 tiles// one canonical world seed (terrain + deposits)
	int worldSeed;
	//END CHANGES 23-Jun-26 tiles//

	// --- Game state ---
	GameContext gameContext;
	Controls controls; // input hub
	WorkshopSprites workshopSprites; // animated sprites for player-built workshops
	GainFeedback gainFeedback; // fading "+N" flashes over producing buildings (M4.5 sub-rung 4)
	StorageOverlay storageOverlay; // LR6: marker for player/AI-built Granary/Warehouse tiles

	// Carries a loaded save across ReloadCurrentScene(): static lives on the type, so it survives the
	// scene teardown; _Ready reads it to start worldgen from the saved seed, then clears it.
	public static GameData PendingLoad;

	// --- Map config ---
	public static int MapSizeX = 128;
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
		// world (terrain, nations, deposits) regenerates identically; the player's paid-for roads are
		// restored from the save (ApplySavedState), not seed-derived.
		GameData loaded = PendingLoad;
		PendingLoad = null;
		if (loaded != null)
		{
			MapSizeX = loaded.Width;
			MapSizeY = loaded.Height;
			numNations = loaded.NumNations;
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
		// LR6: same idea for any Granary/Warehouse already built (loaded games, or an AI that beat the
		// player to one) — must run after ApplySavedState so a loaded storage building actually shows up.
		storageOverlay.Initialize();

		// "+N" gain flashes: after each tick, drain the economy's production events onto their tiles.
		// (gainFeedback itself is created in CreateObjs so MapManager can flash "-cost" on a build.)
		gameContext.tickManager.TickProcessed += () =>
		{
			foreach (var (tile, good, amount) in gameContext.economy.ProducedThisTick)
				gainFeedback.Flash(tile, good, amount);
		};

		PopupSceneLoad();

		DrawPlayerRoads();

		// LR2: the physical goods layer — auto-haulers crawling the player's roads carrying produced goods to the
		// capital, plus at-rest piles where goods are stranded (no road) or delivered (the capital warehouse). It
		// reads the economy's live Dispatcher/TileInventory every frame, so no per-tick hook is needed.
		Haulers haulers = new Haulers();
		AddChild(haulers);
		haulers.Initialize(gameContext.economy, tml);

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

		VoronoiDiagram vD = new VoronoiDiagram(MapSizeX, MapSizeY, numNations, mapCoordsLand, tml, worldSeed);

		//CHANGES 23-Jun-26 tiles// build economy with the single worldSeed; add land cross-check + deposits summary
		Economy economy = new Economy(vD.nationsDictionary, vD.GetCapitals(), worldSeed);

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

		// LR6: storage building marker + export panel — both are MapManager dependencies (it calls
		// AddStorage/Show on player clicks), so created here rather than alongside the other overlays in
		// _Ready(), mirroring the workshopSprites/gainFeedback split above. The marker's actual population
		// scan happens later, in _Ready() after ApplySavedState (see storageOverlay.Initialize()).
		storageOverlay = new StorageOverlay();
		AddChild(storageOverlay);
		storageOverlay.SetContext(economy, tml);

		var exportLayer = new CanvasLayer();
		AddChild(exportLayer);
		var exportPanel = new ExportPanel();
		exportLayer.AddChild(exportPanel);
		exportPanel.Initialize(economy);

		MapManager mapManager = new MapManager(vD, tml, Nht, numNations, tickManager, economy, controls, workshopSprites, gainFeedback, storageOverlay, exportPanel);
		AddChild(mapManager);
		//END CHANGES 23-Jun-26 tiles//

		var nationTexture = vD.coloredNationMap;
		MapTextureCreated?.Invoke(nationTexture);

		return new GameContext(vD, economy, tickManager, mapManager, nationTexture, noise, MapSizeX, MapSizeY, tml);
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

	// LR1: draw the player's paid-for road tiles onto the railroad layer (none on a new game; a loaded game
	// restored them before this runs). Replaces DrawMSTLines now that there's no deterministic MST backbone to
	// paint — the only track on the map is what the player bought.
	private void DrawPlayerRoads()
	{
		var roadTiles = new HashSet<Vector2I>(gameContext.economy.Railroads);
		if (roadTiles.Count == 0) return;

		TileMapLayer railroadLayer = tml.GetNode<TileMapLayer>("RailroadLayer");
		foreach (Vector2I tile in roadTiles)
			railroadLayer.SetCell(tile, RailroadTiles.Rails, Vector2I.Zero);

		// Orient them into connected rail sprites (straights/corners/junctions), skipping capital tiles the
		// same way the old backbone draw did.
		var capitalSet = new HashSet<Vector2I>(gameContext.voronoiDiagram.GetCapitals());
		new RailroadBitmask().OrientRailroadCells(railroadLayer, roadTiles, capitalSet, tml);
	}
}
