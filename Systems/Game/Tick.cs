using Godot;
using System;

// The game's heartbeat: every WaitTime seconds it advances the clock and fires Ticked.
// Tick itself does no game work — it just broadcasts the beat. Any system that needs to
// update on a steady pace subscribes to Ticked (see Main for the wiring).
//
// A tick is a fixed game-logic heartbeat (once per second), not _Process (which fires
// every rendered frame). Keep game logic on the tick so it runs at a steady pace.
public partial class Tick : Timer
{
	public long GameTick { get; private set; }

	// Fired once per tick, carrying the new GameTick. Subscribers do the actual work.
	public event Action<long> Ticked;

	public override void _Ready()
	{
		WaitTime = GlobalConfig.TickSeconds; // one tick per second
		OneShot = false;  // repeat forever
		Timeout += Advance;
		Start();
	}

	// ---- one heartbeat ----
	private void Advance()
	{
		GameTick++;
		Ticked?.Invoke(GameTick);
	}
}
