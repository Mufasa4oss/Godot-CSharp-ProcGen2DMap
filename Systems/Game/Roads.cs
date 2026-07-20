using Godot;
using System;
using System.Collections.Generic;


// The roads the player has built, plus the goods sitting on each one.
//
// One dictionary does both jobs: a tile is a road only if it's a key here, and its value is that
// tile's OWN stockpile — the cargo currently on it. Because goods can only sit on a road, iterating
// this dictionary IS iterating every tile worth checking; we never scan the whole map.
public class Roads
{
	// road tile -> the goods currently sitting on that tile
	private readonly Dictionary<Vector2I, Resources> cargoByTile = new();

	// Player builds a road on this tile. It starts empty — every road tile owns its own stockpile.
	public void Build(Vector2I tile)
	{
		if (!cargoByTile.ContainsKey(tile))
			cargoByTile[tile] = new Resources();
	}

	// The goods on a road tile, or null if there's no road there.
	public Resources CargoOn(Vector2I tile)
	{
		if (cargoByTile.TryGetValue(tile, out Resources cargo))
			return cargo;

		return null;
	}

	// Every kind of good on this tile added together, ignoring what kind each one is.
	// 0 if the tile is bare road, and 0 if there's no road there at all.
	public int SumOfResourcesOnTile(Vector2I tile)
	{
		Resources cargo = CargoOn(tile);
		if (cargo == null)
			return 0;

		int total = 0;
		foreach (KeyValuePair<ResourceType, int> entry in cargo.GetDictionaryResourceType())
		{
			total += entry.Value;
		}

		return total;
	}
}
