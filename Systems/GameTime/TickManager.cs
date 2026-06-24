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
			GD.Print($"tick {currentTick} | provinces={economy.Provinces.Count}");
		}
		//END CHANGES 23-Jun-26//
	}
}
