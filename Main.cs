using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;


public partial class Main : Node2D
{
	const int sID_water = 0;
	const int sID_sand = 1;
	const int sID_grass = 2;
	Vector2I grassAtlas;
	Vector2I waterAtlas;
	Vector2I sandAtlas;

	[Export] NoiseTexture2D Nht;
	//[Export] Texture2D coloredProMap;
	Noise noise;
	TileMapLayer tml;
	//TileMapLayer bhl;
	TextureRect textureRect;
    private List<Vector2I> mapCoordsLand = new List<Vector2I>();

	public static int MapSizeX = 512;
	public static int MapSizeY = 512; //in tiles

	public Main() { }

	public override void _Ready()

	{
		noise = Nht.Noise; // noise is set the noise parameter of the NoiseTexture2D Nht.

		Nht.Height = MapSizeY;
		Nht.Width = MapSizeX;
		// h = Nht.Height; //gets the noise textures dimensions
		//w = Nht.Width;

		tml = GetNode<TileMapLayer>("0zTileMapLayer");
        textureRect = GetNode<TextureRect>("TextureRect");

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
		var vD = new VoronoiDiagram(MapSizeX, MapSizeY, 25, mapCoordsLand, tml);
		//coloredProMap = vD.coloredProMap;


		//work in progress
		Texture2D provinceTexture = vD.coloredProMap;
		TextureRect tRect = GetNode<TextureRect>("TextureRect");
		tRect.Texture = provinceTexture;

		ShaderMaterial shade = (ShaderMaterial)tRect.Material;
		shade.SetShaderParameter("pm", provinceTexture);
		shade.SetShaderParameter("screenTextureSize", new Vector2(MapSizeX, MapSizeY));

		////
	}


	//QueueRedraw();

	public override void _Draw()
	{
	}
}
		
	
