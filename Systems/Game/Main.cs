using Godot;
using System;
using System.Collections.Generic;

// Boot: generate and render the map. Gameplay (economy/finance/pops/logistics) has been removed — this is
// now just the world: procedural terrain, the blended ground shader, and one capital per nation placed on
// the map, with the nation-colour overlay available (hidden by default). No ticks, no save/load, no HUD.
public partial class Main : Node2D
{
	[Export] NoiseTexture2D Nht;
	[Export] int numNations = 1;

	Noise noise;
	TileMapLayer tml;
	int worldSeed;

	// Map config — read by Camera / ColoredNationsSprite for sizing.
	public static int MapSizeX = 128;
	public static int MapSizeY = 128;

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
		PopupSceneLoad();
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

		// Left-click a tile to highlight its Voronoi region on the BordersLayer overlay.
		var selector = new RegionSelector();
		AddChild(selector);
		selector.Initialize(tml, tml.GetNode<TileMapLayer>("BordersLayer"), vD.nationsDictionary);
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
