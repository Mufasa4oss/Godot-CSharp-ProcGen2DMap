using Godot;
using System.Collections.Generic;

// Moves goods along the road network, one hop per tick.
//
// THE MODEL — a conveyor belt, not a delivery van. Nothing walks the whole route. Each tick, every road
// tile hands a little of its cargo to the neighbouring tile that's one step closer to a city. Goods that
// start eight tiles out take eight ticks to arrive, because they get passed along eight times. That's
// what makes distance cost something without any extra rule: a long road is simply slow.
//
// BACKPRESSURE — a road tile holds only GlobalConfig.RoadCapacity units in total. If the tile ahead is
// full, the handoff doesn't happen and the goods wait exactly where they are. A jam at the city gate
// therefore backs up along the road the way cars stack up at a red light. Nothing is ever destroyed,
// dropped, or teleported — it only ever waits.
//
// Roads stores the network and the cargo; this class is what pushes that cargo around. Same split as
// Roads/RoadPlacer (Roads records, RoadPlacer paints), and it keeps Roads free of any knowledge of
// cities or buildings.
public class Haulage
{
	private readonly Roads _roads;
	private readonly Buildings _buildings;

	// Where the goods are headed. Held as a live reference, not a copy, so cities added later are seen.
	private readonly IReadOnlyDictionary<Vector2I, City> _cities;

	public Haulage(Roads roads, Buildings buildings, IReadOnlyDictionary<Vector2I, City> cities)
	{
		_roads = roads;
		_buildings = buildings;
		_cities = cities;
	}

	// One tick of hauling: work out where everything is headed, shuffle it one hop, then let buildings
	// top up the roads beside them.
	public void HaulOneTick()
	{
		Dictionary<Vector2I, Vector2I> nextHop = new Dictionary<Vector2I, Vector2I>();
		List<Vector2I> nearestFirst = new List<Vector2I>();
		BuildRoutes(nextHop, nearestFirst);

		MoveAlongRoads(nextHop, nearestFirst);
		LoadFromBuildings();
	}

	// Works out, for every road tile, which neighbour is the next step toward the nearest city.
	//
	// Picture dropping a stone into each city and watching the ripple spread outward along the roads. The
	// tile a ripple reaches a tile *from* is that tile's next hop, and because the ripple spreads evenly
	// in all directions it always arrives by the shortest route first. That's a breadth-first search.
	//
	// A road tile the ripple never reaches has no path to any city at all, so it gets no next hop and its
	// goods sit there forever. That's deliberate: a road to nowhere visibly holds its cargo, which is how
	// you spot that you never connected it.
	//
	// nearestFirst comes back in the order the ripple visited — closest to a city first. Moving goods in
	// that order is what makes a jam drain properly: the tile at the front empties before the tile behind
	// it is asked to move, so a packed road shifts forward as a whole instead of staying stuck. It also
	// means no unit can move twice in one tick, since its destination was already dealt with.
	private void BuildRoutes(Dictionary<Vector2I, Vector2I> nextHop, List<Vector2I> nearestFirst)
	{
		Queue<Vector2I> frontier = new Queue<Vector2I>();

		foreach (Vector2I cityTile in _cities.Keys)
		{
			frontier.Enqueue(cityTile);
		}

		while (frontier.Count > 0)
		{
			Vector2I current = frontier.Dequeue();

			foreach (Vector2I neighbour in Neighbours(current))
			{
				if (_roads.CargoOn(neighbour) == null) continue;  // not a road tile
				if (nextHop.ContainsKey(neighbour)) continue;     // already reached by a shorter route
				if (_cities.ContainsKey(neighbour)) continue;     // a city is a destination, never a step

				nextHop[neighbour] = current;
				nearestFirst.Add(neighbour);
				frontier.Enqueue(neighbour);
			}
		}
	}

	// Every road tile that has somewhere to send its goods sends what it can, nearest the city first.
	private void MoveAlongRoads(Dictionary<Vector2I, Vector2I> nextHop, List<Vector2I> nearestFirst)
	{
		foreach (Vector2I tile in nearestFirst)
		{
			Vector2I destination = nextHop[tile];
			Resources cargo = _roads.CargoOn(tile);

			if (_cities.TryGetValue(destination, out City city))
			{
				// Arrived. A city has no cargo limit — it's the drain at the end of the pipe.
				Handover(cargo, city.Stockpile, GlobalConfig.HaulPerTick);
			}
			else
			{
				Handover(cargo, _roads.CargoOn(destination), AllowanceInto(destination));
			}
		}
	}

	// Buildings hand their output to a road tile beside them — the on-ramp onto the network. A building
	// with no road next to it just keeps filling its own stockpile, which is how an unconnected mine
	// shows itself.
	//
	// Runs after the road pass so freshly loaded goods wait a tick before setting off, rather than being
	// produced, loaded and moved all in the same beat.
	private void LoadFromBuildings()
	{
		foreach (Building building in _buildings.All.Values)
		{
			foreach (Vector2I neighbour in Neighbours(building.Location))
			{
				Resources road = _roads.CargoOn(neighbour);
				if (road == null) continue;  // no road on this side

				int allowance = AllowanceInto(neighbour);
				if (allowance <= 0) continue;  // that tile is full; try another side

				Handover(building.Stockpile, road, allowance);
				break;  // one on-ramp per building per tick is enough
			}
		}
	}

	// How much a tile may accept this tick: the per-tick haul rate, or the space left on it, whichever
	// runs out first. This is where backpressure actually happens.
	private int AllowanceInto(Vector2I tile)
	{
		int room = GlobalConfig.RoadCapacity - _roads.SumOfResourcesOnTile(tile);

		if (room < GlobalConfig.HaulPerTick)
			return room;

		return GlobalConfig.HaulPerTick;
	}

	// Move up to `allowance` units from one stockpile to another, taking resource kinds in enum order.
	// Which kind gets to travel first when a tile holds several is arbitrary for now.
	//
	// Safe to remove from `from` while looping, because GetDictionaryResourceType hands back a fresh
	// dictionary each call rather than the stockpile's own.
	private static void Handover(Resources from, Resources to, int allowance)
	{
		foreach (KeyValuePair<ResourceType, int> entry in from.GetDictionaryResourceType())
		{
			if (allowance <= 0) return;

			int amount = entry.Value;
			if (amount > allowance) amount = allowance;
			if (amount <= 0) continue;

			from.TryRemove(entry.Key, amount);
			to.Add(entry.Key, amount);
			allowance -= amount;
		}
	}

	// The four tiles sharing an edge with this one. Roads connect straight, never diagonally.
	private static List<Vector2I> Neighbours(Vector2I tile)
	{
		List<Vector2I> result = new List<Vector2I>();
		result.Add(new Vector2I(tile.X + 1, tile.Y));
		result.Add(new Vector2I(tile.X - 1, tile.Y));
		result.Add(new Vector2I(tile.X, tile.Y + 1));
		result.Add(new Vector2I(tile.X, tile.Y - 1));
		return result;
	}
}
