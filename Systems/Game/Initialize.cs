using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

//places the tiles

public partial class Initalize : Node2D
{

	private  TileMapLayer tml;
	Noise noise;
	int msx;
	int msy;
	const int sID_water = 0;
	const int sID_sand = 1;
	const int sID_grass = 2;
	private List<Vector2I> mapCoordsLand = new List<Vector2I>();
	public Initalize(TileMapLayer t, Noise n, int x, int y)
	{
		// Constructor
		// Initialize any variables or state here if needed
		this.tml = t; // Assign the TileMapLayer instance to the local variable
		this.noise = n; // Assign the Noise instance to the local variable
		this.msx = x;
		this.msy = y;
	}

	//place tiles unordered only happens once
	public List<Vector2I> PlaceTiles() // Make it return MapsCoordsLand?
	{

		Vector2I StartingAtlasVector = new Vector2I(0, 0);
		//rewrite first overlay with default tile, then correct tile orientation
		for (int y = 0; y < msx; y++)
		{
			for (int x = 0; x < msy; x++)
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
		return (mapCoordsLand);
	}

}
