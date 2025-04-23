using Godot;

using Godot.NativeInterop;
using kingsofresource;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
	Dictionary<Vector2I, ResourceItem> capitalResources;
	public static int MapSizeX = 512;
	public static int MapSizeY = 512; //in tiles
									  //public event Ree Created;
	UIManager control;
	public static event Action<Texture2D> MapTextureCreated; //event that is triggered when the property is assigned this is a delgate auto defined returns null one arguement
	//public static event Action ProvincesSceneInitalized;
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
		PlaceTiles();
		CreateObjs();

		//_Draw();
	}

	/// <summary>
	/// ////////////World Generation
	/// </summary>

	private void PlaceTiles()
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

		//metric shitton of object creation warning order matters way too much remade into an objectinstancing method


		

		//var borderLayer = GetNode<TileMapLayer>("0zTileMapLayer/BordersLayer");
	}
	private void CreateObjs()
	{
		Bitmask bitmaskObj = new Bitmask(tml, MapSizeX, MapSizeY);
		//Dictionary<string, Vector2I> atlas = bitmaskObj.atlasDict;
		bitmaskObj.OrientCells();
		//
		vD = new VoronoiDiagram(MapSizeX, MapSizeY, numProvinces, mapCoordsLand, tml);
		//
		ResourceManager resourceManager = new ResourceManager(vD.placedCords);
		capitalResources = resourceManager.capitalsResources;
		//
		tickManager = new TickManager(resourceManager, vD.placedCords, control);
		//
		MapManager mapManager = new MapManager(vD, tml, Nht, numProvinces, tickManager);
		AddChild(mapManager);
		//end object creation

		var provinceTexture = vD.coloredProMap;
		MapTextureCreated?.Invoke(provinceTexture);
	}


	/// <summary>
	/// ////////////////Resource System rough outline moved to resource manager
	/// </summary>

	

	/// <summary>
	/// ///////////////// Time and Date System Calls
	/// </summary>


	public override void _Process(double delta)
	{
		tickManager.Update((float)delta);
	}// Update the tick manager with the time since the last frame. Handles time passing


	///
	/// /////////// Save & Load
	/// 
   
public void initializeSave()
	{
		//tickManager
		int ct = tickManager.currentTick;
		float tickInterval = tickManager.tickInterval;
		float tickTimer = tickManager.tickTimer;
		//Main Class items needed to recreate Nht NoiseTexture2D! (seed, frequency and everything else that I make customizeable) Rn it is seed, freq, and noiseType = Perlin
		FastNoiseLite noise = (FastNoiseLite)Nht.Noise;
		int seed = noise.Seed;
		float freq = noise.Frequency;
		String NoiseType = "Perlin";
		//List of capitals
		List<Vector2I> capitals = vD.placedCords;
		//ResourceManager Class
		var capitalResourceDictionary = capitalResources; //Dictionary<Vector2I, ResourceItem> Need to figure out how to save complicated types better
														  //EVERYTHING I am going to save for now just so that I can test loading it.
		
		GameData data = new GameData("nerd",ct,tickInterval,tickTimer,MapSizeX,MapSizeY,seed,freq,numProvinces,capitals, capitalResourceDictionary, NoiseType);
		Serialization.Save(data);
}
public void initializeLoad()
{
Serialization serial = new Serialization();
GameData gamedata = serial.Load();
//GD.Print("Game Data loaded: " + gamedata);
//pause game first?

//Main Class
MapSizeX = gamedata.Width;
MapSizeY = gamedata.Height;
FastNoiseLite noise = (FastNoiseLite)Nht.Noise;
noise.Seed = gamedata.Seed;
noise.Frequency = gamedata.Frequency;
numProvinces = gamedata.NumProvinces;
//TickManager Class
tickManager.currentTick = gamedata.Tick;
//Resource Manager class
}

public override void _Input(InputEvent @event)
{
if (Input.IsActionJustPressed("f5"))
{
	initializeSave();
}
		if (Input.IsActionJustPressed("f9"))
		{
			initializeLoad();
		}

	}

}//EOF
