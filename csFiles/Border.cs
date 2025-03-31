using Godot;
using kingsofresource;
using System;
using System.Collections.Generic;


public partial class Border : Line2D
{

    public Border()
    {
        
    }

    public List<Vector2I> GetBorderTiles(Vector2I capital, Dictionary<Vector2I,Vector2I> provincesDictionary)
    {
        List<Vector2I> rval = new List<Vector2I>();
        Dictionary<Vector2I, List<Vector2I>> allProvinces = MyGlobalClass.AllCoordsWithCapital(capital, provincesDictionary);

        allProvinces.TryGetValue(capital, out List<Vector2I> list);// <Vector2I> list of all coords associated with this capital
        foreach (Vector2I coord in list)
        {
            List<Vector2I> surroundingTiles = GetSurroundingTilesTLRB(coord);//len = 4
            foreach (Vector2I tile in surroundingTiles)
            {
                if (!list.Contains(tile))
                {
                    rval.Add(tile);
                }
            }
        }
        
        return rval;
    }

    public List<Vector2I> GetSurroundingTilesTLRB(Vector2I coord)//top left right bottom
    {
        List<Vector2I> surroundingTiles = new List<Vector2I>();
        Vector2I top = new Vector2I(coord.X, coord.Y - 1);
        Vector2I left = new Vector2I(coord.X - 1, coord.Y);
        Vector2I right = new Vector2I(coord.X + 1, coord.Y);
        Vector2I bottom = new Vector2I(coord.X, coord.Y + 1);

        surroundingTiles.Add(coord + top);
        surroundingTiles.Add(coord + left);
        surroundingTiles.Add(coord + right);
        surroundingTiles.Add(coord + bottom);

      /*  foreach (Vector2I tile in surroundingTiles)
        {
            GD.Print(tile.ToString());
        }*/
        //GD.Print(surroundingTiles.ToString());
        return surroundingTiles;
    }

    public override void _Ready()
    {
        Closed = true;
        DefaultColor = new Color(0, 0, 0, 1);
        Width = 2;
        ZAsRelative = true;
        ZIndex = 1;
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
