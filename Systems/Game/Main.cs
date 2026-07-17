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

	// The player-built road network + vehicles on it; the tick inspects this each beat.
	Roads roads;

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
	///   <item>Add one line below: <c>tick.Ticked += n =&gt; yourSystem.DoThing();</c></item>
	///   <item>Done. You never touch Tick.cs — Tick keeps broadcasting and your system now hears it.</item>
	/// </list>
	/// <c>n</c> is the tick number the event hands every subscriber. We name it <c>n</c> on every line for
	/// consistency: pass it on when the method wants it (<c>n =&gt; economy.Settle(n)</c>), and simply
	/// leave it unused when it doesn't (<c>n =&gt; population.Grow()</c>). You may also see <c>_ =&gt;</c>
	/// elsewhere, which is the same thing written to say "I deliberately ignore this value."
	/// </para>
	///
	/// <para>
	/// GOTCHA — LIFETIME. The event holds a reference to whatever you subscribe, so a subscriber can't be
	/// garbage-collected while it's still attached. If a system is ever destroyed mid-game
	/// (<c>QueueFree</c>), unsubscribe it first with <c>tick.Ticked -= handler</c>, or it will be kept
	/// alive and keep firing. Nothing here is freed today, so it isn't a problem yet — but it's the trap
	/// to remember once cities or roads can disappear at runtime.
	/// </para>
	/// </summary>
	private void WireTickSubscribers(Tick tick)
	{
		tick.Ticked += n => roads.ReportBottlenecks(n);  // road bottleneck check each tick
		tick.Ticked += n => UI.Update(n);                // refresh the on-screen panel each tick
		foreach (var c in cities.Values)
		{
			tick.Ticked += n => c.ConsumeDemand();       // each city consumes a tick of demand
			tick.Ticked += n => c.GrowPopulation();      // each city grows its population each tick
		}
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

		// Scatter raw resources on the map and draw them as coloured dots.
		var resourceMap = new ResourceMap();
		tml.AddChild(resourceMap);
		resourceMap.Initialize(tml, mapCoordsLand, worldSeed);

		// Every capital is a city — a consumer that demands all four resources each tick.
		foreach (var capital in vD.GetCapitals())
			cities[capital] = new City(capital, resourceMap.Deposits, vD.nationsDictionary);

		// Hand the cities + tilemap to the UI so left-clicking a city shows its demand.
		UI = GetNode<UIManager>("UIManager");
		UI.Initialize(tml, cities);

	}

	// Startup splash logo that fades out after a few seconds.
	private void PopupSceneLoad()
	{
		var popupScene = ResourceLoader.Load<PackedScene>("res://Scenes/ResourcePopup.tscn").Instantiate();
		AddChild(popupScene);

		var timer = new Timer { WaitTime = 5.0f, OneShot = true };
		AddChild(timer);
		timer.Start();
		timer.Timeout += () =>
		{
			((ResourcePopup)popupScene).StartFadeOut();
			timer.QueueFree();
		};
	}
}
