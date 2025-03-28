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
	public Texture2D coloredProMap;
	public Dictionary<Vector2I, Vector2I> provincesDictionary; //key is any coord and the value is the (coords of) closest province center



	//[Export] private Polygon2D[] provincePolygons;

	//private List<Vector2I> borderLines = new List<Vector2I>();

	public List<Vector2I> GetCapitals()
	{
		return placedCords;
	}
	public VoronoiDiagram(int mapsizex, int mapsizey, int numCapitals, List<Vector2I> mapCoords, TileMapLayer tilemaplayer)
	{
		
		this.desiredPointCount = numCapitals;
		this.allPossibleCoords = mapCoords;
		this.tml = tilemaplayer;
		this.mapSizeX = mapsizex;
		this.mapSizeY = mapsizey; //tiles
		GeneratePoints();
		this.coloredProMap = CreateProvinceMap();
		

	}
	//GD.Print($"{points.Count} points generated.");


	private void GeneratePoints() // placesCityCenters
	{
		List<int> indices = new List<int>();
		Random rng = new Random();
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
			tml.SetCell(coord, 3, new Vector2I(0, 0));
		}
	}


	private Dictionary<Vector2I, Vector2I> CreateDictionaryOfTilesCapitals() //Currently a brute-force voronoi diagram generation
	{
		Dictionary<Vector2I, Vector2I> coordsProvinceCenter = new Dictionary<Vector2I, Vector2I>(); //key is any coord and the value is the (coords of) closest province center

		foreach (var coord in allPossibleCoords)
		{
			if (tml.GetCellSourceId(coord) == 0) //if water continue
			{
				continue;
			}

			Vector2I closestProvinceCenter = placedCords[0];
			float minDistance = float.MaxValue; //biggest possible float value

			foreach (var provinceCenter in placedCords)
			{
				float distance = coord.DistanceTo(provinceCenter);
				if (distance < minDistance)
				{
					minDistance = distance;
					closestProvinceCenter = provinceCenter;
				}
			}
			//need to store results
			coordsProvinceCenter[coord] = closestProvinceCenter;

		}
		return (coordsProvinceCenter);
	}





	//need to make a texture2d of the provinces with unique colors

	private HashSet<Color> CreateUniqueColorSet()
	{
		int numProvinces = placedCords.Count;
		Color lastColor = new Color(0, 0, 0);
		var random = new RandomNumberGenerator();

		HashSet<Color> colorSet = new HashSet<Color>();
		for (int i = 0; i < numProvinces; i++)
		{
			Color color;
			do
			{
				float hue = ((float)i / numProvinces) + (random.Randf() * 0.05f);
				hue = hue % 1.0f;
				color = Color.FromHsv(hue, 1f, 1f, 1f);

			} while (colorSet.Contains(color));

			colorSet.Add(color);
		}
		return colorSet;
	}
	private Texture2D CreateProvinceMap()
	{
		int pixelSizeX = mapSizeX;
		int pixelSizeY = mapSizeX;
		HashSet<Color> colorSet = CreateUniqueColorSet();
		Image image = Image.CreateEmpty(pixelSizeX, pixelSizeY, false, Image.Format.Rgba8);
		Dictionary<Vector2I, Vector2I> provinceMap = CreateDictionaryOfTilesCapitals();
		provincesDictionary = provinceMap;
		Dictionary<Vector2I, Color> provinceColors = new Dictionary<Vector2I, Color>();



		for (int i = 0; i < placedCords.Count; i++)
		{
			provinceColors[placedCords[i]] = colorSet.ElementAt(i);
		}


		//color each pixel
		foreach (var dictItem in provinceMap)
		{
			Vector2I tile = dictItem.Key;
			Vector2I capital = dictItem.Value;

			if (provinceColors.TryGetValue(capital, out Color pc))
			{
				image.SetPixel(tile.X, tile.Y, pc);
			}
		}
		//image.SavePng("C:\\Users\\lyonp\\Desktop\\Images\\file.png");
		Texture2D rVal = ImageTexture.CreateFromImage(image);
		return rVal;
	}
}
