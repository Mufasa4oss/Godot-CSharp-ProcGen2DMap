using System;
using Godot;
using System.Collections.Generic;
using System.Linq;



namespace kingsofresource;

public static class MyGlobalClass
{
	static int tileSize = 64;
	public static HashSet<Vector2> ArrayConvertToWorldCoords(HashSet<Vector2I> tiles)
	{

		HashSet<Vector2> worldPos = new HashSet<Vector2>();
		for (int i = 0; i < tiles.Count; i++)
		{
			var v2 = new Vector2(tiles.ElementAt(i).X * tileSize, tiles.ElementAt(i).Y * tileSize);
			worldPos.Add(v2);
		}
		return worldPos;
	}

	public static Vector2 ConvertToWorldCoords(Vector2I tile)
	{
		{
			Vector2 worldPos;
			worldPos = new Vector2(tile.X * tileSize, tile.Y * tileSize);

			return worldPos;
		}
	}
	public static Dictionary<Vector2I,HashSet<Vector2I>> AllCoordsWithCapital(Vector2I capital, System.Collections.Generic.Dictionary<Godot.Vector2I, Godot.Vector2I> proDict) //check // Province coords: Capital coords
	{
		Dictionary<Vector2I, HashSet<Vector2I>> matchingKeys = new Dictionary<Vector2I, HashSet<Vector2I>>(); // capital: [all coords associated with this capital]
		//create a list of all the keys that have the same value
		List<Vector2I> keys = new List<Vector2I>();

		HashSet<Vector2I> set = (from key in proDict.Keys
								  where (proDict[key] == capital)
								  select key).ToHashSet<Vector2I>();

		//list.Sort();
		
		matchingKeys.Add(capital, set);
		// Example: Print all keys that share the same value
		return matchingKeys;

	}
	
	private static Vector2I[] GetSurroundingTiles(Vector2I thisTileCoords) //returns a [] of coord values of surrounding tiles
	{
		Vector2I[] surroundingTiles = new Vector2I[8];
		surroundingTiles[0] = new Vector2I(thisTileCoords.X - 1, thisTileCoords.Y - 1); // top-left   // 0000 0001
		surroundingTiles[1] = new Vector2I(thisTileCoords.X, thisTileCoords.Y - 1);     // top 00     //0000 0010
		surroundingTiles[2] = new Vector2I(thisTileCoords.X + 1, thisTileCoords.Y - 1); // top-right  //0000 0100
		surroundingTiles[3] = new Vector2I(thisTileCoords.X - 1, thisTileCoords.Y);     // left       //0000 1000
		surroundingTiles[4] = new Vector2I(thisTileCoords.X + 1, thisTileCoords.Y);     // right      //0001 0000
		surroundingTiles[5] = new Vector2I(thisTileCoords.X - 1, thisTileCoords.Y + 1); // bottom-left    //0010 0000
		surroundingTiles[6] = new Vector2I(thisTileCoords.X, thisTileCoords.Y + 1);     // bottom         //0100 0000
		surroundingTiles[7] = new Vector2I(thisTileCoords.X + 1, thisTileCoords.Y + 1); // bottom-right   //1000 0000
		return (surroundingTiles);
	}

}
