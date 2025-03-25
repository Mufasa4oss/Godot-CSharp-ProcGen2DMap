using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

public partial class Line2d : Line2D
{
    private TileMapLayer tml;
    int mapSizeX;
    int mapSizeY;
    System.Collections.Generic.Dictionary<Vector2I, Vector2I> provincesDictionary; //key is any coord and the value is the (coords of) closest province center
    List<Vector2I> capitals;
    public Line2d(TileMapLayer t, int x, int y, System.Collections.Generic.Dictionary<Vector2I, Vector2I> pd, List<Vector2I> placedCoords)
    {
        this.tml = t;
        this.mapSizeX = x;
        this.mapSizeY = y;
        this.provincesDictionary = pd; //capital:provinces associated
        this.capitals = placedCoords;
    }

    private List<int> GetMinMaxXandY()
    {
        int minX;
        int maxX;
        int minY;
        int maxY;
        List<int> minMax = new List<int>();

    }

    public void DrawBordersPerProvince(Vector2I province)
    {
        for (int x = 0; x < mapSizeX; x++)
        {
            for (int y = 0; y < mapSizeY; y++)
            {
                List<int> minMaxList = GetMinMaxXandY();

            }

        }
    }
}
/*
 needs to get the tml world coords and draw correctly dependent on other tiles assocaited capital provinces. so get border tiles which is just a list of all border tiles. Right, Left, Up, Down?

to solve problem above we can rely upon the tile type that is in Bitmask. Meaning we can use the tileset properties to determine where to draw the borders. not true for landlocked areas.
capital:all associated tile coords - > get min x, max x, min y, max y. (tile coords) from the capital:provinces dictionary in vd
 */