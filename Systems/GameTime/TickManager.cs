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

	// Fires right after each Economy.OnTick(). The view subscribes to drain per-tick data (e.g.
	// Economy.ProducedThisTick → the "+1" gain flash) exactly when a tick runs (M4.5 sub-rung 4).
	public event System.Action TickProcessed;

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
			TickProcessed?.Invoke();

			// Alive-check heartbeat: global goods totals climb each tick as extractors produce. StoredAmountOf
			// covers both economies (player's physical TileInventory + AI's abstract Stockpile) so the player's
			// output isn't invisible here the way a raw Stockpile sum would leave it.
			float grain = 0f, ore = 0f, tools = 0f;
			foreach (var nation in economy.Nations.Values)
			{
				grain += economy.StoredAmountOf(nation, Good.Grain);
				ore   += economy.StoredAmountOf(nation, Good.Ore);
				tools += economy.StoredAmountOf(nation, Good.Tools);
			}
			GD.Print($"tick {currentTick} | nations={economy.Nations.Count} | grain={grain:0} ore={ore:0} tools={tools:0}");
		}
		//END CHANGES 23-Jun-26//
	}
}
