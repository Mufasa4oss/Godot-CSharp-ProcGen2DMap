using Godot;
using System;
using System.Collections.Generic;

public class GameData
{

	/// <summary>
	/// Notice that all variables are public Json serializer requires this as well as {get; set;} to work. {get; set;} marks the variable as a property as opposed to just a field
	/// </summary>
	public string Name { get; set; }
	public long Tick { get; set; } // long: never overflows even at fast tick rates
	public float tickInterval
		{ get; set; }
	public float tickTimer
		{ get; set; }
	public int Width { get; set; }
	public int Height { get; set; }

	public int Seed { get; set; }
	public float Frequency { get; set; }
	public string NoiseType { get; set; } = "Perlin";
	public int NumProvinces { get; set; }
	// Capitals are NOT stored: they regenerate deterministically from Seed. Persisting derived state
	// would risk a saved-vs-regenerated drift bug.

	// Accumulated economy state. Unlike the world (regenerated from Seed), these change as the game is
	// played, so they must be persisted. DepositReserves is parallel to Economy.Deposits order
	// (deterministic from Seed). NationStocks is keyed by nation Capital (a value, not a dict key, so the
	// Vector2IConverter handles it) and carries treasury + stockpile. Workshops are the player-built
	// tiles (not seed-derived → must be saved); owners are recomputed from capital order, not saved.
	public List<float> DepositReserves { get; set; } = new();
	public List<NationStock> NationStocks { get; set; } = new();
	public List<Vector2I> Workshops { get; set; } = new();

	public GameData() { } //Required only for deserialization (Serialization.Load). DO NOT USE OTHERWISE.
	public GameData(string name, long tick, float tickInterval, float tickTimer, int x, int y, int seed, float frequency, int numProvinces,
		String NoiseType)
	{
		this.Name = name;
		this.Tick = tick;
		this.tickInterval = tickInterval;
		this.tickTimer = tickTimer;
		this.Width = x;
		this.Height = y;
		this.Seed = seed;
		this.Frequency = frequency;
		this.NoiseType = NoiseType;
		this.NumProvinces = numProvinces;
	}
}
