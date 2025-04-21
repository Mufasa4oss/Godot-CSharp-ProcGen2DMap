using Godot;
using Godot.NativeInterop;
using kingsofresource;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static System.Net.WebRequestMethods;
/*Map and ModeView Mode Generation*/


public partial class Main : Node2D
{
	const int sID_water = 0;
	const int sID_sand = 1;
	const int sID_grass = 2;
	Vector2I grassAtlas;
	Vector2I waterAtlas;
	Vector2I sandAtlas;
	TickManager tickManager;
	[Export] NoiseTexture2D Nht;
	[Export] int numProvinces = 1;
	Noise noise;
	TileMapLayer tml;
	VoronoiDiagram vD;
	//TileMapLayer bhl;
	TextureRect textureRect;
	private List<Vector2I> mapCoordsLand = new List<Vector2I>();//coords of all placed tiles not water

	public static int MapSizeX = 512;
	public static int MapSizeY = 512; //in tiles
									  //public event Ree Created;
	UIManager control;
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

		tml = GetNode<TileMapLayer>("0zTileMapLayer");
		control = GetNode<UIManager>("UIManager");
		GenerateWorldNoise(Nht.Width, Nht.Height);


		//_Draw();
	}

	/// <summary>
	/// ////////////
	/// </summary>

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

		//AddChild(line);


		//
		//var holyDict = vD.provincesDictionary;
		//var cApital = vD.GetCapitals();
		var borderLayer = GetNode<TileMapLayer>("0zTileMapLayer/BordersLayer");
		//new section this draws the borders of the provinces
		//var borderTileImage = ResourceLoader.Load<Texture2D>("res://Images/35%_OpacityBlack.png");


		var capitalResources = initCapitalResource();
		ResourceManager resourceManager = new ResourceManager(capitalResources);
		
		tickManager = new TickManager(resourceManager,vD.placedCords, control);
		//Control control = GetNode<Control>("UIManager");
	}
	/// <summary>
	/// /////////////////////////////////
	/// </summary>
	
	private Dictionary<Vector2I, ResourceItem> initCapitalResource()
	{
		RandomNumberGenerator rng = new RandomNumberGenerator();
		Dictionary<Vector2I, ResourceItem> capitalsResources = new Dictionary<Vector2I, ResourceItem>();
		foreach (Vector2I capital in vD.placedCords)
		{
			float distro = rng.Randf(); //random number between 0 and 1
			if (distro < 0.33f)
			{
				ResourceItem resourceItem = new ResourceItem(0, "Wood", 1);
				capitalsResources.Add(capital, resourceItem);
			}
			else if (distro < 0.66f)
			{
				ResourceItem resourceItem = new ResourceItem(0, "Stone", 1);
				capitalsResources.Add(capital, resourceItem);
			}
			else
			{
				ResourceItem resourceItem = new ResourceItem(0, "Grain", 1);
				capitalsResources.Add(capital, resourceItem);
			}
		}
		return capitalsResources;
	}

	/// <summary>
	/// ///////////////// Time and Date System Calls
	/// </summary>
	
	
	public override void _Process(double delta)
	{
		
		tickManager.Update((float)delta);
	}// Update the tick manager with the time since the last frame. Handles time passing

	



}//EOF
