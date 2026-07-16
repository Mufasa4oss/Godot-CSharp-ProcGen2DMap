using Godot;
using System.Collections.Generic;

// The roads the player has built, plus how many vehicles sit on each one.
//
// One dictionary does both jobs: a tile is a road only if it's a key here, and its
// value is the vehicle count. Because a vehicle can only be on a road, iterating this
// dictionary IS iterating every tile worth checking — we never scan the whole map.
public class Roads
{
	// How many vehicles a road tile can hold before it counts as a bottleneck.
	public int Capacity = GlobalConfig.RoadCapacity;

	// road tile -> number of vehicles currently on it
	private readonly Dictionary<Vector2I, int> _vehiclesByTile = new();

	// Player builds a road on this tile (starts with no vehicles).
	public void Build(Vector2I tile)
	{
		if (!_vehiclesByTile.ContainsKey(tile))
			_vehiclesByTile[tile] = 0;
	}

	// Is there a road on this tile?
	public bool IsRoad(Vector2I tile) => _vehiclesByTile.ContainsKey(tile);

	// Add a vehicle to a road tile. Does nothing if the tile isn't a road.
	public void AddVehicle(Vector2I tile)
	{
		if (_vehiclesByTile.ContainsKey(tile))
			_vehiclesByTile[tile]++;
	}

	// Remove a vehicle from a road tile, never going below zero.
	public void RemoveVehicle(Vector2I tile)
	{
		if (_vehiclesByTile.TryGetValue(tile, out int count) && count > 0)
			_vehiclesByTile[tile] = count - 1;
	}

	// How many vehicles are on this tile (0 if none, or if it isn't a road).
	public int VehiclesOn(Vector2I tile) =>
		_vehiclesByTile.TryGetValue(tile, out int count) ? count : 0;

	// Every road tile and its vehicle count — what the tick walks over.
	public IEnumerable<KeyValuePair<Vector2I, int>> All => _vehiclesByTile;
}
