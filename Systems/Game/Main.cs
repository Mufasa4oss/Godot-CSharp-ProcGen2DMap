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
	Dictionary<Vector2I, ResourceItem> tilesResources;
	public static int MapSizeX = 512;
	public static int MapSizeY = 512; //in tiles
									  //public event Ree Created;
	UIManager control;
	public static event Action<Texture2D> MapTextureCreated; //event that is triggered when the property is assigned this is a delgate auto defined returns null one arguement
															 //public static event Action ProvincesSceneInitalized;
	Sprite2D provinceSprite;
	GameContext gameContext;
	//private List<Vector2I> mapCoordsLand;

	//new
	//Node2D popupInstance;
	public Main() { }

	public override void _Ready()

	{
		noise = Nht.Noise; // noise is set the noise parameter of the NoiseTexture2D Nht.

		Nht.Height = MapSizeY;
		Nht.Width = MapSizeX;

		tml = GetNode<TileMapLayer>("0zTileMapLayer");
		control = GetNode<UIManager>("UIManager");
		//PlaceTiles();
		
		gameContext = CreateObjs();
		PopupSceneLoad();

		//_Draw();
	}

	/// <summary>
	/// ////////////World Generation
	/// </summary>


	//Dependencies of PlaceTiles are //noise, tml, MapSizeX, MapSizeY also mapCoordsLand and const int s for sID_water, sID_sand, sID_grass
	/*
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
	*/
	private GameContext CreateObjs()
	{
		Initalize init = new Initalize(tml, noise, MapSizeX, MapSizeY);
		mapCoordsLand = init.PlaceTiles(); // Place tiles and get the coordinates of land tiles
		Bitmask bitmaskObj = new Bitmask(tml, MapSizeX, MapSizeY);
		//Dictionary<string, Vector2I> atlas = bitmaskObj.atlasDict;
		bitmaskObj.OrientCells();
		//
		vD = new VoronoiDiagram(MapSizeX, MapSizeY, numProvinces, mapCoordsLand, tml);
		//
		ResourceManager resourceManager = new ResourceManager(mapCoordsLand, vD.GetCapitals());
		tilesResources = resourceManager.tilesResources;
		//
		tickManager = new TickManager(resourceManager, vD.placedCords, control);
		//
		MapManager mapManager = new MapManager(vD, tml, Nht, numProvinces, tickManager);
		AddChild(mapManager);
		//end object creation

		var provinceTexture = vD.coloredProMap;
		MapTextureCreated?.Invoke(provinceTexture);

		//test
		

		return new GameContext(bitmaskObj, vD, resourceManager, tilesResources, tickManager, mapManager, provinceTexture, noise, MapSizeX, MapSizeY, tml); // used to pass dependencies. Easiest thing to do would be to save items that are loaded into this object

	}

	// Load the popup scene
	public void PopupSceneLoad()
	{
		var popupScene = ResourceLoader.Load<PackedScene>("res://Scenes/ResourcePopup.tscn").Instantiate();
		
		//popupScene
		AddChild(popupScene);

		//removal
		Timer timer = new Timer();
		timer.WaitTime = 5.0f;
		timer.OneShot = true;
		AddChild(timer);
		timer.Start();
		timer.Timeout += () =>
		{
			ResourcePopup rP = (ResourcePopup)popupScene;
			rP.StartFadeOut(); // Start the fade out effect
			//popupScene.QueueFree();
			timer.QueueFree();
			//this happends in ResourcePopup.cs -> Remove the popup scene after fading out
		};
	}
	// Instance it
	//Node popupSceneInstance = popupScene.Instantiate();

	// Optionally: position it somewhere on the screen
	// Adjust as needed

	// Add it as a child so it appears in your main scene
	
	/// <summary>
	/// ////////////////Resource System rough outline moved to resource manager
	/// </summary>



	/// <summary>
	/// ///////////////// Time and Date System Calls
	/// </summary>


	public override void _Process(double delta)
	{
		tickManager.Update((float)delta);
	}
	public override void _Input(InputEvent @event)
	{
		if (Input.IsActionJustPressed("f5"))
		{
			var obj = new InitSaveAndLoad(gameContext);
			obj.initializeSave();
		}
		if (Input.IsActionJustPressed("f9"))
		{
			var obj = new InitSaveAndLoad(gameContext);
			obj.initializeSave();
		}

	}
}
	
	// Update the tick manager with the time since the last frame. Handles time passing


	///
	/// /////////// Save & Load
	/// 
   
