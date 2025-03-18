using System;
using System.Collections.Generic;
using System.Linq;
using Godot;


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
}

