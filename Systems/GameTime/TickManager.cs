using System.Collections.Generic;
using Godot;

public class TickManager
{
	// Manages tick timing and dispatches updates to the economy.

	public long currentTick = 0; // long: never overflows even at fast tick rates
	public float tickInterval = 1.0f; // seconds between ticks
	public float tickTimer = 0.0f;

	//CHANGES 23-Jun-26// TickManager now drives the Economy; dropped ResourceManager/capitals/UIManager
	private Economy economy;

	public TickManager(Economy economy)
	{
		this.economy = economy;
	}
	//END CHANGES 23-Jun-26//

	public void Update(float delta)
	{
		//CHANGES 23-Jun-26// real 1s tick: advance the economy + heartbeat (was a commented-out stub)
		tickTimer += delta;
		if (tickTimer >= tickInterval)
		{
			tickTimer -= tickInterval;
			currentTick++;
			economy.OnTick();

			// Alive-check heartbeat: global stockpile totals climb each tick as extractors produce.
			float grain = 0f, ore = 0f, tools = 0f;
			foreach (var nation in economy.Nations.Values)
			{
				grain += nation.Stockpile.GetValueOrDefault(Good.Grain);
				ore   += nation.Stockpile.GetValueOrDefault(Good.Ore);
				tools += nation.Stockpile.GetValueOrDefault(Good.Tools);
			}
			GD.Print($"tick {currentTick} | nations={economy.Nations.Count} | grain={grain:0} ore={ore:0} tools={tools:0}");
		}
		//END CHANGES 23-Jun-26//
	}
}
