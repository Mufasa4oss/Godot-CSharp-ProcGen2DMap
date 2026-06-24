using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

//places the tiles

public partial class Initialize : Node2D
{

	private  TileMapLayer tml;
	Noise noise;
	int msx;
	int msy;
	private List<Vector2I> mapCoordsLand = new List<Vector2I>();
	public Initialize(TileMapLayer t, Noise n, int x, int y)
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
		// msx = width (X), msy = height (Y). Loop y over height and x over width so a
		// non-square map (e.g. 256x128) isn't transposed/clipped.
		for (int y = 0; y < msy; y++)
		{
			for (int x = 0; x < msx; x++)
			{
				Vector2I currentTile = new Vector2I(x, y);
				float nV = noise.GetNoise2D(x, y);
				//noiseMap[x,y] = nV;
				//we have all elevations for the map so now depending on the elevation we determine water, sand, or grass
				if (nV > 0.1f)//grass
				{
					tml.SetCell(currentTile, TerrainTiles.Grass, new Vector2I(3, 3)); //default is set to island
					mapCoordsLand.Add(currentTile);
				}
				else
				{
					tml.SetCell(new Vector2I(x, y), TerrainTiles.Water, new Vector2I(0, 0));
					//tiles.Append<Vector2I>(currentTile);
				}
			}
			
		}
		return (mapCoordsLand);
	}

}
