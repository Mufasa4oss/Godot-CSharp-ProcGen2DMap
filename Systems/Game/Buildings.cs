using Godot;
using System.Collections.Generic;

// One building and the registry of all of them. They're small and always used together, so they share a
// file — the same way Resources.cs holds both the ResourceType enum and the Resources stockpile.

// A building the player has placed on the map. It sits on one tile and, each tick, produces a fixed
// amount of one resource into its OWN stockpile. Nothing hauls those goods away yet — this is only the
// producer end of the chain (the city consumer end lives in City/Demand).
public class Building
{
	public enum BuildingType { Farm, Mine, Well, Forestry }

	public BuildingType Type { get; }
	public Vector2I Location { get; }
	public Resources Stockpile { get; } = new Resources();

	public Building(BuildingType type, Vector2I location)
	{
		Type = type;
		Location = location;
	}

	// Which resource a building of this type makes — and therefore the deposit it has to be built on.
	// Static because Buildings.Place needs to check the tile *before* it creates the building.
	public static ResourceType OutputOf(BuildingType type)
	{
		switch (type)
		{
			case BuildingType.Farm:     return ResourceType.Grain;
			case BuildingType.Mine:     return ResourceType.Iron;
			case BuildingType.Well:     return ResourceType.Water;
			case BuildingType.Forestry: return ResourceType.Wood;
			default:                    return ResourceType.Grain;
		}
	}

	// Which resource this building makes.
	public ResourceType Output
	{
		get { return OutputOf(Type); }
	}

	// How much it makes per tick (from GlobalConfig's production rates).
	private int RatePerTick
	{
		get
		{
			switch (Type)
			{
				case BuildingType.Farm:     return GlobalConfig.FarmGrainProductionPerTick;
				case BuildingType.Mine:     return GlobalConfig.MineIronProductionPerTick;
				case BuildingType.Well:     return GlobalConfig.WellWaterProductionPerTick;
				case BuildingType.Forestry: return GlobalConfig.ForestryLumberProductionPerTick;
				default:                    return 0;
			}
		}
	}

	// One tick of work: add this building's output to its own stockpile.
	public void Produce()
	{
		Stockpile.Add(Output, RatePerTick);
	}
}

// Every building the player has placed, keyed by the tile it sits on. This is just the registry plus
// per-tick production; painting the tile onto the map is the UI's job (the same split Roads/RoadPlacer
// use — Roads records the network, RoadPlacer paints it).
public class Buildings
{
	private readonly Dictionary<Vector2I, Building> _byTile = new();

	// The map's raw deposits (ResourceMap.Deposits). Held as a live reference, not a copy, so the rules
	// below always read the current map — if deposits ever change at runtime, placement follows.
	private readonly IReadOnlyDictionary<Vector2I, ResourceType> _deposits;

	// The cities, keyed by tile. Only used to keep buildings off a capital's own tile.
	private readonly IReadOnlyDictionary<Vector2I, City> _cities;

	public Buildings(IReadOnlyDictionary<Vector2I, ResourceType> deposits,
	                 IReadOnlyDictionary<Vector2I, City> cities)
	{
		_deposits = deposits;
		_cities = cities;
	}

	public IReadOnlyDictionary<Vector2I, Building> All
	{
		get { return _byTile; }
	}

	// Put a new building on a tile. A building has to sit ON the resource it harvests, so all four
	// rules must pass; any failure returns false and changes nothing.
	//
	// The city rule matters more than it looks: deposits are scattered without avoiding capitals, so a
	// capital CAN sit on one. Without this check you could build there, and since the UI tests for a
	// building before a city, that city's panel would become unclickable for the rest of the game.
	public bool Place(Building.BuildingType type, Vector2I tile)
	{
		if (_byTile.ContainsKey(tile)) return false;                            // tile already built on
		if (_cities.ContainsKey(tile)) return false;                            // a city sits here
		if (!_deposits.TryGetValue(tile, out ResourceType deposit)) return false; // no deposit here
		if (deposit != Building.OutputOf(type)) return false;                   // wrong deposit for this type

		_byTile[tile] = new Building(type, tile);
		return true;
	}

	// The building on a tile, or null if there isn't one.
	public Building At(Vector2I tile)
	{
		if (_byTile.TryGetValue(tile, out Building building))
			return building;

		return null;
	}

	// One tick: every placed building produces into its own stockpile.
	public void ProduceAll()
	{
		foreach (Building b in _byTile.Values)
			b.Produce();
	}
}
