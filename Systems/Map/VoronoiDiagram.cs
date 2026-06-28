using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
public partial class VoronoiDiagram
{
	//private int width;
	//private int height;
	private int desiredPointCount;
	private List<Vector2I> allPossibleCoords = new List<Vector2I>(); //coords of all land cells
	public System.Collections.Generic.List<Vector2I> placedCords = new List<Vector2I>(); // all capital coords
	private TileMapLayer tml;
	private int mapSizeX;
	private int mapSizeY;
	private int worldSeed; // one canonical seed so the same world reproduces (save/load determinism)
	public Texture2D coloredNationMap;
	public Dictionary<Vector2I, Vector2I> nationsDictionary; //key is any coord and the value is the (coords of) closest nation center (capital)



	//private List<Vector2I> borderLines = new List<Vector2I>();

	public List<Vector2I> GetCapitals()
	{
		return placedCords;
	}
	public VoronoiDiagram(int mapsizex, int mapsizey, int numCapitals, List<Vector2I> mapCoords, TileMapLayer tilemaplayer, int worldSeed)
	{

		this.desiredPointCount = numCapitals;
		this.allPossibleCoords = mapCoords;
		this.tml = tilemaplayer;
		this.mapSizeX = mapsizex;
		this.mapSizeY = mapsizey; //tiles
		this.worldSeed = worldSeed;
		GeneratePoints();
		this.coloredNationMap = CreateNationMap();
		

	}
	//GD.Print($"{points.Count} points generated.");


	private void GeneratePoints() // placesCityCenters
	{
		List<int> indices = new List<int>();
		// Seeded from the one world seed so capital placement is reproducible per seed.
		System.Random rng = new System.Random(worldSeed);
		for (int i = 0; i < allPossibleCoords.Count; i++)
		{
			indices.Add(i);
		}
		for (int i = 0; i < indices.Count; i++) //shuffles the indices list
		{
			int swapIndex = rng.Next(i, indices.Count);
			(indices[i], indices[swapIndex]) = (indices[swapIndex], indices[i]);
		}
		for (int i = 0; i < desiredPointCount; i++)
		{
			//places the desired point count up to desiredPointCount indicies
			Vector2I coord = allPossibleCoords[indices[i]];
			placedCords.Add(coord);
			tml.SetCell(coord, TerrainTiles.Capital, new Vector2I(0, 0));
		}
	}


	private Dictionary<Vector2I, Vector2I> CreateDictionaryOfTilesCapitals() //Currently a brute-force voronoi diagram generation
	{
		// Skip any water cells (defensive — allPossibleCoords is land), then run the pure
		// nearest-capital assignment (VoronoiAssignment, Core) which holds the actual math.
		var landTiles = new List<Vector2I>();
		foreach (var coord in allPossibleCoords)
			if (tml.GetCellSourceId(coord) != TerrainTiles.Water)
				landTiles.Add(coord);

		return VoronoiAssignment.AssignNearest(landTiles, placedCords);
	}





	//need to make a texture2d of the nations with unique colors

	// Mix the world seed into a distinct deterministic seed for color generation so colors are
	// reproducible from the one seed without sharing a stream with capital placement.
	private static int DeriveColorSeed(int worldSeed)
	{
		unchecked { return worldSeed * (int)0x9E3779B1 + 0x632BE5AB; }
	}

	private HashSet<Color> CreateUniqueColorSet()
	{
		int numNations = placedCords.Count;
		Color lastColor = new Color(0, 0, 0);
		// Derived sub-seed so nation colors are deterministic but not correlated with capital placement.
		var random = new System.Random(DeriveColorSeed(worldSeed));

		HashSet<Color> colorSet = new HashSet<Color>();
		for (int i = 0; i < numNations; i++)
		{
			Color color;
			do
			{
				float hue = ((float)i / numNations) + ((float)random.NextDouble() * 0.05f);
				hue = hue % 1.0f;
				color = Color.FromHsv(hue, 1f, 1f, 1f);

			} while (colorSet.Contains(color));

			colorSet.Add(color);
		}
		return colorSet;
	}
	private Texture2D CreateNationMap()
	{
		int pixelSizeX = mapSizeX;
		int pixelSizeY = mapSizeY;
		HashSet<Color> colorSet = CreateUniqueColorSet();
		Image image = Image.CreateEmpty(pixelSizeX, pixelSizeY, false, Image.Format.Rgba8);
		Dictionary<Vector2I, Vector2I> nationMap = CreateDictionaryOfTilesCapitals();
		nationsDictionary = nationMap;
		Dictionary<Vector2I, Color> nationColors = new Dictionary<Vector2I, Color>();



		for (int i = 0; i < placedCords.Count; i++)
		{
			nationColors[placedCords[i]] = colorSet.ElementAt(i);
		}


		//color each pixel
		foreach (var dictItem in nationMap)
		{
			Vector2I tile = dictItem.Key;
			Vector2I capital = dictItem.Value;

			if (nationColors.TryGetValue(capital, out Color pc))
			{
				image.SetPixel(tile.X, tile.Y, pc);
			}
		}
		//image.SavePng("C:\\Users\\lyonp\\Desktop\\Images\\file.png");
		Texture2D rVal = ImageTexture.CreateFromImage(image);
		return rVal;
	}
}
