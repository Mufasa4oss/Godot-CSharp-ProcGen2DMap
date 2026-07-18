using Godot;
using System;
using System.Collections.Generic;

// Boot: generate and render the map. Gameplay (economy/finance/pops/logistics) has been removed — this is
// now just the world: procedural terrain, the blended ground shader, and one capital per nation placed on
// the map, with the nation-colour overlay available (hidden by default). A tick heartbeat now drives
// per-road vehicle/bottleneck checks; still no save/load or HUD.
public partial class Main : Node2D
{
	[Export] NoiseTexture2D Nht;
	[Export] int numNations = 1;
	UIManager UI;

	Noise noise;
	TileMapLayer tml;
	int worldSeed;

	// The player-built road network; each road tile holds the goods currently sitting on it.
	// Nothing drives it on the tick yet — that's the revamp in progress.
	Roads roads;

	// The buildings the player has placed; each produces into its own stockpile every tick.
	Buildings buildings;

	// Every capital is a city consumer, keyed by its tile so a click can look it up.
	public Dictionary<Vector2I, City> cities = new();

	// Map config — read by Camera / ColoredNationsSprite for sizing. Runtime values, seeded from the
	// read-only defaults in GlobalConfig (so a settings menu could later override them at boot).
	public static int MapSizeX = GlobalConfig.MapSizeX;
	public static int MapSizeY = GlobalConfig.MapSizeY;

	// Fired once the nation-colour texture is built; ColoredNationsSprite (the "Nations" overlay) listens.
	public static event Action<Texture2D> MapTextureCreated;
	
	public override void _Ready()
	{
		noise = Nht.Noise;
		FastNoiseLite noiseTexture = noise as FastNoiseLite;

		worldSeed = new Random().Next();
		noiseTexture.Set("seed", worldSeed);
		GD.Print("Seed: " + worldSeed);

		Nht.Height = MapSizeY;
		Nht.Width = MapSizeX;

		tml = GetNode<TileMapLayer>("0zTileMapLayer");

		GenerateWorld();

		// Start the game heartbeat. Tick just fires Ticked; each system subscribes to do its own work.
		roads = new Roads();
		var tick = new Tick();
		AddChild(tick);
		WireTickSubscribers(tick);

		// Player builds roads by clicking (press R to toggle build mode).
		var roadPlacer = new RoadPlacer();
		AddChild(roadPlacer);
		roadPlacer.Initialize(roads, tml);

		PopupSceneLoad();
	}

	// ─────────────────────────────────────────────────────────────────────────────────────────────
	//  EVENT SYSTEM
	// ─────────────────────────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Wires every system that needs to run on the game clock into <see cref="Tick"/>'s heartbeat.
	/// This method is the one and only place those connections are made.
	///
	/// <para>
	/// HOW THE EVENT SYSTEM WORKS.
	/// <see cref="Tick"/> is the heartbeat: once per second it fires its <c>Ticked</c> event and hands
	/// every listener the current tick number. Tick does no game work itself — it just announces "a beat
	/// happened." Each system decides what to do about that beat by subscribing here. This is the
	/// publish/subscribe pattern: Tick is the publisher, the systems are the subscribers, and neither
	/// side knows about the other. Main is the switchboard that plugs them together.
	/// </para>
	///
	/// <para>
	/// WHY IT'S SET UP THIS WAY. Because Tick never names its subscribers, it never has to change when
	/// the game grows. Adding a new clock-driven system is a one-line edit *here*, and Tick.cs is left
	/// untouched. Systems stay decoupled — Roads, City and the UI don't know the clock exists; they just
	/// expose a method, and Main hands that method to the tick.
	/// </para>
	///
	/// <para>
	/// TO TIE A NEW SYSTEM INTO THE TICK:
	/// <list type="number">
	///   <item>Give the system a method to run each beat, e.g. <c>population.Grow()</c> or
	///         <c>economy.Settle(long tick)</c>.</item>
	///   <item>Add one line to <see cref="OnTick"/> below, in the position you want it to run.</item>
	///   <item>Done. You never touch Tick.cs — Tick keeps broadcasting and your system now hears it.</item>
	/// </list>
	/// Exactly one handler is subscribed — <see cref="OnTick"/> — and it calls everything in written
	/// order. That order IS the simulation: produce before consume, or a city starves on goods it should
	/// have had. A pile of separate subscribers would leave the order implicit in how they were added.
	/// <c>n</c> is the tick number the event hands the handler; pass it on to whatever wants it.
	/// </para>
	///
	/// <para>
	/// GOTCHA — LIFETIME. The event holds a reference to whatever you subscribe, so a subscriber can't be
	/// garbage-collected while it's still attached. If a system is ever destroyed mid-game
	/// (<c>QueueFree</c>), unsubscribe it first with <c>tick.Ticked -= OnTick</c>, or it will be kept
	/// alive and keep firing. Nothing here is freed today, so it isn't a problem yet — but it's the trap
	/// to remember once cities or roads can disappear at runtime.
	/// </para>
	/// </summary>
	private void WireTickSubscribers(Tick tick)
	{
		tick.Ticked += OnTick;
	}

	// One beat of the world. Everything the clock drives happens here, in this order.
	private void OnTick(long n)
	{
		buildings.ProduceAll();          // every placed building produces
		foreach (City c in cities.Values)
		{
			c.ConsumeDemand();           // each city consumes a tick of demand
			c.GrowPopulation();          // each city grows its population
		}
		UI.Update(n);                    // refresh the on-screen panel
	}

	// Terrain tiles + the realistic blended ground + capitals and the nation-colour map. Pure worldgen.
	private void GenerateWorld()
	{
		// Second noise field for deserts: a low-frequency aridity map, offset from the height seed so the
		// two are decorrelated.
		var moisture = new FastNoiseLite
		{
			NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
			Seed = worldSeed + 7919,
			Frequency = 0.03f,
		};

		var init = new Initialize(tml, noise, moisture, MapSizeX, MapSizeY);
		List<Vector2I> mapCoordsLand = init.PlaceTiles();

		// Blended ground (shader), rendered under the tilemap.
		var ground = new GroundRenderer();
		AddChild(ground);
		ground.Initialize(init.TerrainMask, MapSizeX, MapSizeY);

		// Place capitals + build the coloured nation map (the Q-toggle overlay reads this texture).
		var vD = new VoronoiDiagram(MapSizeX, MapSizeY, numNations, mapCoordsLand, tml, worldSeed);
		GD.Print($"Map: {mapCoordsLand.Count} land tiles, {vD.GetCapitals().Count} capitals");

		MapTextureCreated?.Invoke(vD.coloredNationMap);

		// Scatter raw resources on the map and paint them as tiles on the ResourcesLayer.
		var resourceMap = new ResourceMap();
		tml.AddChild(resourceMap);
		resourceMap.Initialize(tml.GetNode<TileMapLayer>("ResourcesLayer"), mapCoordsLand, worldSeed);

		// Every capital is a city — a consumer that demands all four resources each tick.
		foreach (var capital in vD.GetCapitals())
			cities[capital] = new City(capital, resourceMap.Deposits, vD.nationsDictionary);

		// A building has to sit on a matching deposit and off a city tile, so the registry needs both
		// maps to check placements against. Built here (not in _Ready) because they only exist now.
		buildings = new Buildings(resourceMap.Deposits, cities);

		// Hand the cities + tilemap to the UI so left-clicking a city shows its demand.
		// The UI lives under a CanvasLayer so it stays fixed on screen instead of panning with the map.
		UI = GetNode<UIManager>("CanvasLayer/UIManager");
		UI.Initialize(tml, cities, buildings, tml.GetNode<TileMapLayer>("ResourcesLayer/BuildingsLayer"));

	}

	// The splash logo and its countdown, kept as fields so OnSplashTimeout can reach them without a
	// closure over PopupSceneLoad's locals.
	private Node splashScene;
	private Timer splashTimer;

	// Startup splash logo that fades out after a few seconds.
	private void PopupSceneLoad()
	{
		splashScene = ResourceLoader.Load<PackedScene>("res://Scenes/ResourcePopup.tscn").Instantiate();
		AddChild(splashScene);

		splashTimer = new Timer { WaitTime = 5.0f, OneShot = true };
		AddChild(splashTimer);
		splashTimer.Start();
		splashTimer.Timeout += OnSplashTimeout;
	}

	// The splash timer ran out: fade the logo and throw the timer away.
	private void OnSplashTimeout()
	{
		((ResourcePopup)splashScene).StartFadeOut();
		splashTimer.QueueFree();
	}
}
