using Godot;
using System.Collections.Generic;

// The game's heartbeat: every WaitTime seconds it advances the clock and runs its
// per-tick checks. Right now that's one question — "is any road jammed?" — asked only
// of the tiles that are roads, never the whole map.
//
// A tick is a fixed game-logic heartbeat (once per second), not _Process (which fires
// every rendered frame). Keep game logic on the tick so it runs at a steady pace.
public partial class Tick : Timer
{
	public long GameTick { get; private set; }

	// The roads to inspect each tick. Main creates one shared Roads and hands it here.
	public Roads Roads { get; set; }

	// The cities that consume each tick. Main creates one per capital and hands them here.
	public IEnumerable<City> Cities { get; set; }

	public override void _Ready()
	{
		WaitTime = 1.0;   // one tick per second
		OneShot = false;  // repeat forever
		Timeout += Advance;
		Start();
	}

	// ---- one heartbeat ----
	private void Advance()
	{
		GameTick++;

		// Road bottlenecks: walk ONLY the road tiles and flag any carrying more than it can handle.
		if (Roads != null)
			foreach (var (tile, vehicles) in Roads.All)
				if (vehicles > Roads.Capacity)
					GD.Print($"[tick {GameTick}] bottleneck: road {tile} has {vehicles}/{Roads.Capacity} vehicles");

		// City demand: each city consumes one tick of demand from its stockpile.
		if (Cities != null)
			foreach (var c in Cities)
				c.ConsumeDemand();
	}
}
