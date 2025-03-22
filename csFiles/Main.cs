using Godot;
using System;
using System.Collections.Generic;
using System.IO;
/*Map and ModeView Mode Generation*/


public partial class Main : Node2D
{
	const int sID_water = 0;
	const int sID_sand = 1;
	const int sID_grass = 2;
	Vector2I grassAtlas;
	Vector2I waterAtlas;
	Vector2I sandAtlas;

	[Export] NoiseTexture2D Nht;
	[Export] int numProvinces = 1;
	//[Export] Texture2D coloredProMap;
	Noise noise;
	TileMapLayer tml;
	VoronoiDiagram vD;
	//TileMapLayer bhl;
	TextureRect textureRect;
	private List<Vector2I> mapCoordsLand = new List<Vector2I>();

	public static int MapSizeX = 512;
	public static int MapSizeY = 512; //in tiles
									  //public event Ree Created;

	public static event Action<Texture2D> MapTextureCreated; //event that is triggered when the property is assigned this is a delgate auto defined returns null one arguement
	public static event Action ProvincesSceneInitalized;
	Sprite2D provinceSprite;

	public Main() { }

	public override void _Ready()

	{
		//

		//

		noise = Nht.Noise; // noise is set the noise parameter of the NoiseTexture2D Nht.

		Nht.Height = MapSizeY;
		Nht.Width = MapSizeX;
		// h = Nht.Height; //gets the noise textures dimensions
		//w = Nht.Width;

		tml = GetNode<TileMapLayer>("0zTileMapLayer");
		//textureRect = GetNode<TextureRect>("TextureRect");

		//bhl = GetNode<TileMapLayer>("BorderHighlightLayer");

		//var viewportSize = GetViewport().GetVisibleRect().Size;
		//Vector2I tileSize = tml.TileSet.TileSize;
		//Camera2D c = GetNode<Camera2D>("Camera2D");
		//c.Align();
		//MapSizeX = 512; // this value represents the number of cells created
		//MapSizeY = 512;
		//GD.Print(viewportSize);

		GenerateWorldNoise(Nht.Width, Nht.Height);

		//_Draw();
	}



	public void GenerateWorldNoise(int width, int height)
	{

		Vector2I StartingAtlasVector = new Vector2I(0, 0);
		//rewrite first overlay with default tile, then correct tile orientation
		for (int y = 0; y < MapSizeX; y++)
		{
			for (int x = 0; x < MapSizeY; x++)
			{
				Vector2I currentTile = new Vector2I(x, y);
				float nV = noise.GetNoise2D(x, y);
				//noiseMap[x,y] = nV;
				//we have all elevations for the map so now depending on the elevation we determine water, sand, or grass
				if (nV > 0f)//grass
				{
					tml.SetCell(currentTile, sID_grass, new Vector2I(3, 3)); //default is set to island
					mapCoordsLand.Add(currentTile);
				}
				else
				{
					tml.SetCell(new Vector2I(x, y), sID_water, new Vector2I(0, 0));
					//tiles.Append<Vector2I>(currentTile);
				}
			}

			
		}
		Bitmask bitmaskObj = new Bitmask(tml, MapSizeX, MapSizeY);
		//Dictionary<string, Vector2I> atlas = bitmaskObj.atlasDict;
		bitmaskObj.OrientCells();
		vD = new VoronoiDiagram(MapSizeX, MapSizeY, numProvinces, mapCoordsLand, tml);
		var provinceTexture = vD.coloredProMap;
		MapTextureCreated?.Invoke(provinceTexture);
		MapManager mapManager = new MapManager(vD, tml);
		AddChild(mapManager);
		


	}
	

	//MapManager mapManager = new MapManager(vD, tml);
	

	//Created?.Invoke(); //triggers the event
	//Province Colored Map is created here
	//coloredProMap = vD.coloredProMap;
	////
}
		
	
