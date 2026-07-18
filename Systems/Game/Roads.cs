using Godot;
using System.Collections.Generic;

// The roads the player has built, plus the goods sitting on each one.
//
// One dictionary does both jobs: a tile is a road only if it's a key here, and its value is that
// tile's OWN stockpile — the cargo currently on it. Because goods can only sit on a road, iterating
// this dictionary IS iterating every tile worth checking; we never scan the whole map.
public class Roads
{
	// road tile -> the goods currently sitting on that tile
	private readonly Dictionary<Vector2I, Resources> _cargoByTile = new();

	// Player builds a road on this tile. It starts empty — every road tile owns its own stockpile.
	public void Build(Vector2I tile)
	{
		if (!_cargoByTile.ContainsKey(tile))
			_cargoByTile[tile] = new Resources();
	}

	// The goods on a road tile, or null if there's no road there.
	public Resources CargoOn(Vector2I tile)
	{
		if (_cargoByTile.TryGetValue(tile, out Resources cargo))
			return cargo;

		return null;
	}
}
