using Godot;

public static class MyGlobalClass
{
	// Public for Economy's deposit flood-grow (and the deposit-cluster tests).
	public static Vector2I[] GetSurroundingTiles(Vector2I thisTileCoords) //returns a [] of coord values of surrounding tiles
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
