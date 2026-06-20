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

	// --- Game state ---
	GameContext gameContext;
	List<PrimsAlgorithmSimple.Edge> mst;

	// --- Map config ---
	public static int MapSizeX = 512;
	public static int MapSizeY = 512;
	public static event Action<Texture2D> MapTextureCreated;

	public Main() { }

	public override void _Ready()
	{
		noise = Nht.Noise;
		FastNoiseLite noiseTexture = noise as FastNoiseLite;
		int num = new Random().Next();
		noiseTexture.Set("seed", num);
		GD.Print("Seed: " + num);

		Nht.Height = MapSizeY;
		Nht.Width = MapSizeX;

		tml     = GetNode<TileMapLayer>("0zTileMapLayer");
		control = GetNode<UIManager>("UIManager");

		gameContext = CreateObjs();
		PopupSceneLoad();
		mst = run();

		DrawMSTLines();

		TradeNetworkVisualizer tradeVisualizer = new TradeNetworkVisualizer();
		AddChild(tradeVisualizer);
		tradeVisualizer.Initialize(mst, gameContext.voronoiDiagram.GetCapitals(), tml);
	}

	private GameContext CreateObjs()
	{
		List<Vector2I> mapCoordsLand = new Initalize(tml, noise, MapSizeX, MapSizeY).PlaceTiles();

		Bitmask bitmaskObj = new Bitmask(tml, MapSizeX, MapSizeY);
		bitmaskObj.OrientCells();

		VoronoiDiagram vD = new VoronoiDiagram(MapSizeX, MapSizeY, numProvinces, mapCoordsLand, tml);

		ResourceManager resourceManager = new ResourceManager(mapCoordsLand, vD.GetCapitals());

		TickManager tickManager = new TickManager(resourceManager, vD.placedCords, control);

		MapManager mapManager = new MapManager(vD, tml, Nht, numProvinces, tickManager);
		AddChild(mapManager);

		var provinceTexture = vD.coloredProMap;
		MapTextureCreated?.Invoke(provinceTexture);

		return new GameContext(bitmaskObj, vD, resourceManager, resourceManager.tilesResources, tickManager, mapManager, provinceTexture, noise, MapSizeX, MapSizeY, tml);
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

	public List<PrimsAlgorithmSimple.Edge> run()
	{
		return new Example(gameContext).Main();
	}

	public class Example
	{
		int capitalCount;
		List<Vector2I> capitals;

		public Example(GameContext gc)
		{
			capitals     = gc.voronoiDiagram.GetCapitals();
			capitalCount = capitals.Count;
		}

		public List<PrimsAlgorithmSimple.Edge> Main()
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

	public override void _Input(InputEvent @event)
	{
		if (Input.IsActionJustPressed("f5") || Input.IsActionJustPressed("f9"))
		{
			new InitSaveAndLoad(gameContext).initializeSave();
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
			roadLayer.SetCell(tile, 0, Vector2I.Zero);

		new RoadBitmask().OrientRoadCells(roadLayer, roadTiles, capitalSet);
	}

	private List<Vector2I> GetTilesAlongLine(Vector2I from, Vector2I to)
	{
		var tiles = new List<Vector2I>();
		int dx = Math.Abs(to.X - from.X);
		int dy = Math.Abs(to.Y - from.Y);
		int sx = from.X < to.X ? 1 : -1;
		int sy = from.Y < to.Y ? 1 : -1;
		int err = dx - dy;
		int x = from.X, y = from.Y;

		while (true)
		{
			tiles.Add(new Vector2I(x, y));
			if (x == to.X && y == to.Y) break;
			int e2 = 2 * err;
			bool stepX = e2 > -dy;
			bool stepY = e2 < dx;
			if (stepX && stepY)
				tiles.Add(new Vector2I(x + sx, y));
			if (stepX) { err -= dy; x += sx; }
			if (stepY) { err += dx; y += sy; }
		}

		return tiles;
	}
}
