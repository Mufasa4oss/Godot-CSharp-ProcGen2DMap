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
