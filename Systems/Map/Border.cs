using Godot;
using kingsofresource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
//Not used//Not used//Not used//Not used//Not used//Not used//Not used

public partial class Border : Line2D
{
	//Not used
	//I am thinking more and more that it would just be so much easier overall to just use tilemaps for borders ask Christine to draw fortifications pictures like walls and shit

	public static List<Vector2I> GetBorderTiles(Vector2I capital, Dictionary<Vector2I, Vector2I> provincesDictionary)
	{
		List<Vector2I> rval = new List<Vector2I>();
		Dictionary<Vector2I, HashSet<Vector2I>> allProvinces = MyGlobalClass.AllCoordsWithCapital(capital, provincesDictionary); //capital:[associated tile coords]

		allProvinces.TryGetValue(capital, out HashSet<Vector2I> set);// <Vector2I> list of all coords associated with this capital
		foreach (Vector2I coord in set)
		{
			
			// GD.Print(coord);
			List<Vector2I> surroundingTiles = GetSurroundingTilesTLRB(coord); //len = 4
			foreach (Vector2I tile in surroundingTiles)
			{
				
				if (!set.Contains(tile))//if tile is not a part of this provinces coordinates
				{
						rval.Add(coord);
				}
			}
			}
		return rval;
	}


	public static List<Vector2I> OrderPoints(HashSet<Vector2I> points)
	{
		HashSet<Vector2I> remaining = new HashSet<Vector2I>(points);
		List<Vector2I> orderedPoints = new List<Vector2I>();

		Vector2I current = points.OrderBy(p => p.Y).ThenBy(p => p.X).First();
		orderedPoints.Add(current);
		remaining.Remove(current);

		while (remaining.Count > 0)
		{
			// Find the closest valid neighbor
			Vector2I next = remaining.OrderBy(p => current.DistanceTo(p)).First();
			orderedPoints.Add(next);
			remaining.Remove(next);
			current = next;
		}
		return orderedPoints;
	}

	public static List<Vector2I> GetCorners(HashSet<Vector2I> listOfBorderTiles, TileMapLayer tml, Dictionary<Vector2I, Vector2I> province_capital)
	{
		List<Vector2I> rVal = new List<Vector2I>();

		province_capital.TryGetValue(listOfBorderTiles.ElementAt(0),out Vector2I thisCapital);
		
		foreach (Vector2I tile in listOfBorderTiles)
		{
			int ofProvince = 0;
			int notOfProvince = 0;

			List<Vector2I> tileTLRB_thisTile = GetSurroundingTilesTLRB(tile); //count = 4
			
			 //need to check its assigned color or its associated capital
			

			foreach (Vector2I t in tileTLRB_thisTile)
			{
				bool capitalExists = province_capital.TryGetValue(t, out Vector2I surroundingTilesCapital);


				if (thisCapital.Equals(surroundingTilesCapital))
				{
					//GD.Print("ofProvince!: " + ofProvince);
					ofProvince++;
				}
				else //cases: capital is null or capital is a different vector2I. if null capitalExists is false. if capital is different capital exists is true
				{
					notOfProvince++;
				}
			}
			bool isCornerTile = (ofProvince == 2 && notOfProvince == 2);
			//GD.Print(isCornerTile);
			if (isCornerTile)
			{
				rVal.Add(tile);
				ofProvince = 0;
				notOfProvince = 0;
			}
		}
		return rVal;

	}
	private static List<Vector2I> GetSurroundingTilesTLRB(Vector2I coord)//top left right bottom
	{
		List<Vector2I> surroundingTiles = new List<Vector2I>();
		Vector2I top = new Vector2I(coord.X, coord.Y - 1);
		Vector2I left = new Vector2I(coord.X - 1, coord.Y);
		Vector2I right = new Vector2I(coord.X + 1, coord.Y);
		Vector2I bottom = new Vector2I(coord.X, coord.Y + 1);

		surroundingTiles.Add(top);
		surroundingTiles.Add(left);
		surroundingTiles.Add(right);
		surroundingTiles.Add(bottom);

	  /*  foreach (Vector2I tile in surroundingTiles)
		{
			GD.Print(tile.ToString());
		}*/
		//GD.Print(surroundingTiles.ToString());
		return surroundingTiles;
	}

	/*foreach (Vector2I capital in vD.placedCords)
		{
			Line2D line = new Line2D();
	line.Closed = true;
			line.DefaultColor = new Color(0, 0, 0, 1);
	line.Width = 2;
			line.ZAsRelative = true;
			line.ZIndex = 1;
	}*/
}
