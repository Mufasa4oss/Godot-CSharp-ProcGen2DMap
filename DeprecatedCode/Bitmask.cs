using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
/// <summary>
/// Class used for getting a Bitmask Dictionary <bitmask, Vector2I TileSet Atlas coords)>
/// 
/// </summary>
public class Bitmask
{
	public Dictionary<string, Vector2I> atlasDict;
	public int SourceID;
	public int width;
	public int height;
	public TileMapLayer tml;
	
	public Bitmask(TileMapLayer t, int mapSizeX, int mapSizeY) //sourceID,float 2d array, width, height
	{
		this.tml = t;
		this.atlasDict = new Dictionary<string, Vector2I>();
		this.width = mapSizeX;
		this.height = mapSizeY;
		BuildBitmaskAtlas();
		
	}

    private Vector2I[] GetSurroundingTiles(Vector2I thisTileCoords) //returns a [] of coord values of surrounding tiles
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

	
	public int GetBitmaskAllSurroundingTiles(Vector2I tile)//based off of tile we are view we get the bitmasks of every surrounding tile
	{  // the bitmask is an integer. When coverted to a byte format it looks like so 0010 0000. where one represents land. Zero no land
		int bitmask = 0;// 0000 0000 -topLeft,top,topRight,Left,Right,BottomLeft,Bottom,BottomRight
		//now we have a map of correct tiles all with sourceIDs assigned so I need to loop through every tile and assign tiles based on the surrounding tiles sourceids.
		Vector2I[] surrTiles = GetSurroundingTiles(tile);
		for (int i = 0; i < surrTiles.Length; i++)//order topLeft->top->topRight->Left etc.
		{
			int sid = tml.GetCellSourceId(surrTiles[i]);
			if (sid == 0 || sid == -1) //Continues if the tile is water or null
			{
				continue;
			}
			else
			{
				switch (i)
				{
					case 0: bitmask |= 1; break;
					case 1: bitmask |= 2; break;
					case 2: bitmask |= 4; break;
					case 3: bitmask |= 8; break;
					case 4: bitmask |= 16; break;
					case 5: bitmask |= 32; break;
					case 6: bitmask |= 64; break;
					case 7: bitmask |= 128; break;
				}
			}
		}
		return bitmask;
	}
	//Builds bitmask dictionary//atlasDict
	private void BuildBitmaskAtlas() //adds all possible grass tiles to atlas dictionary
	{
		//Needs bitmask, Vector2I coords topLeft -> topRight...etc
		this.atlasDict.Add("topLeft", new Vector2I(0, 0));
		this.atlasDict.Add("top", new Vector2I(1, 0));
		this.atlasDict.Add("topRight", new Vector2I(2, 0));
		this.atlasDict.Add("topSpecial",new Vector2I(3, 0));//refers to the tile that is the only(o) tile(s) present in relation to this tile.
		this.atlasDict.Add("left",new Vector2I(0, 1));
		this.atlasDict.Add("surrounded", new Vector2I(1, 1));
		this.atlasDict.Add("right", new Vector2I(2, 1));
		this.atlasDict.Add("midSpecialVert", new Vector2I(3, 1));
		this.atlasDict.Add("botLeft", new Vector2I(0, 2));
		this.atlasDict.Add("bot", new Vector2I(1, 2));
		this.atlasDict.Add("botRight", new Vector2I(2, 2));
		this.atlasDict.Add("botSpecial", new Vector2I(3, 2));
		this.atlasDict.Add("leftSpecial", new Vector2I(0, 3));
		this.atlasDict.Add("midSpecialHoriz", new Vector2I(1, 3));
		this.atlasDict.Add("rightSpecial", new Vector2I(2, 3));
		this.atlasDict.Add("island", new Vector2I(3, 3));
	}

	private string DetermineTileType(int bitmask)
	{
		if ((bitmask & 0b01011010) == 0) { return "island"; }
		if ((bitmask & 0b01011000) == 0) { return "botSpecial"; }
		if ((bitmask & 0b00011010) == 0) { return "topSpecial"; }
		if ((bitmask & 0b01001010) == 0) { return "leftSpecial"; }
		if ((bitmask & 0b01010010) == 0) { return "rightSpecial"; }
		if ((bitmask & 0b00011000) == 0) { return "midSpecialVert"; }
		if ((bitmask & 0b01000010) == 0) { return "midSpecialHoriz"; }
		if ((bitmask & 0b01011000) == 0) { return "botSpecial"; }
		if ((bitmask & 0b01001000) == 0) { return "botLeft"; }
		if ((bitmask & 0b01010000) == 0) { return "botRight"; }
		if ((bitmask & 0b01011010) == 0b01011010) { return "surrounded"; }
		if ((bitmask & 0b1010) == 0) { return "topLeft"; }
		if ((bitmask & 0b00010010) == 0) { return "topRight"; }
		if ((bitmask & 0b0010) == 0) { return "top"; }
		if ((bitmask & 0b00010000) == 0) { return "right"; }
		if ((bitmask & 0b1000) == 0) { return "left"; }
		if ((bitmask & 0b01000000) == 0) { return "bot"; }


		return null; // No matching tile type
	}

	public void OrientCells()
	{
		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{

				Vector2I current = new Vector2I(x, y);
				int bitmask = GetBitmaskAllSurroundingTiles(current);
				int sid = tml.GetCellSourceId(current);
				if (sid == 0)
				{
					continue;
				}
				else // not water
				{
					string s = DetermineTileType(bitmask);
					switch (s)
					{
						case ("surrounded"): tml.SetCell(current, sid, atlasDict["surrounded"]); break;
						case ("topLeft"): tml.SetCell(current, sid, atlasDict["topLeft"]); break;
						case ("top"): tml.SetCell(current, sid, atlasDict["top"]); break;
						case ("topRight"): tml.SetCell(current, sid, atlasDict["topRight"]); break;
						case ("topSpecial"): tml.SetCell(current, sid, atlasDict["topSpecial"]); break;
						case ("left"): tml.SetCell(current, sid, atlasDict["left"]); break;
						case ("right"): tml.SetCell(current, sid, atlasDict["right"]); break;
						case ("midSpecialVert"): tml.SetCell(current, sid, atlasDict["midSpecialVert"]); break;
						case ("botLeft"): tml.SetCell(current, sid, atlasDict["botLeft"]); break;
						case ("bot"): tml.SetCell(current, sid, atlasDict["bot"]); break;
						case ("botRight"): tml.SetCell(current, sid, atlasDict["botRight"]); break;
						case ("botSpecial"): tml.SetCell(current, sid, atlasDict["botSpecial"]); break;
						case ("leftSpecial"): tml.SetCell(current, sid, atlasDict["leftSpecial"]); break;
						case ("midSpecialHoriz"): tml.SetCell(current, sid, atlasDict["midSpecialHoriz"]); break;
						case ("rightSpecial"): tml.SetCell(current, sid, atlasDict["rightSpecial"]); break;
						case ("island"): tml.SetCell(current, sid, atlasDict["island"]); break;
					}

				}
			}
		}
	}

}
