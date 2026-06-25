# The Tick Loop

**Purpose:** the heartbeat that advances the simulation. Real frame time accumulates into discrete
**1-second game ticks**, and each tick drives the economy forward once.

---

## How it works

[`../../Systems/GameTime/TickManager.cs`](../../Systems/GameTime/TickManager.cs) is a plain class (not a
Node) that owns the clock and the one consumer it drives:

```csharp
public long  currentTick  = 0;     // long: never overflows, even at fast tick rates
public float tickInterval = 1.0f;  // seconds between ticks
public float tickTimer    = 0.0f;  // accumulates frame delta

public void Update(float delta)
{
    tickTimer += delta;
    if (tickTimer >= tickInterval)
    {
        tickTimer -= tickInterval;     // subtract, don't reset — keeps long-term timing accurate
        currentTick++;
        economy.OnTick();              // advance the economy one step (see Economy.md)
        GD.Print($"tick {currentTick} | nations={…} | grain={…} ore={…} tools={…}");  // alive-check heartbeat
    }
}
```

`Update` is pumped every frame from `Main._Process(delta)`:

```csharp
public override void _Process(double delta)
    => gameContext.tickManager.Update((float)delta);
```

So the flow is: **frame → `_Process` → `TickManager.Update` accumulates `delta` → on each whole second,
`Economy.OnTick()`**. (See [GodotNodeLifecycle.md](GodotNodeLifecycle.md) for `_Process` timing.)

### Why a direct call, not an event

`Update` calls `economy.OnTick()` directly rather than emitting a signal. There's exactly one consumer
and the timing must be deterministic, so a direct call is clearer, ordered, and keeps the whole loop
testable in Core with no engine. This contrast is covered in [EventSystem.md](EventSystem.md) §3.

### Subtract, don't reset

`tickTimer -= tickInterval` (rather than `= 0`) carries the leftover fraction of a second into the next
interval, so ticks don't slowly drift late when frames don't land exactly on second boundaries. If a
frame is very long, only one tick fires that frame (the leftover rolls forward); the loop doesn't try to
catch up multiple ticks at once.

---

## Time, save/load, and determinism

- `currentTick` is `long` and is **saved/restored** (`GameData.Tick`), along with `tickInterval` and
  `tickTimer`, so the clock resumes exactly. See [SaveLoad.md](SaveLoad.md).
- Because `OnTick()` has **no randomness**, the economy state at tick *N* is a pure function of the seed
  and *N*. That property underpins both the determinism test and the reproduce-from-seed save model.

---

## The heartbeat print (dev visibility, gate before ship)

The per-tick `GD.Print` is the current "alive check" — it shows the tick climbing and the global
grain/ore stockpile totals rising as extractors produce. It's intentional dev visibility for now;
[`../minorFixes.md`](../minorFixes.md) flags gating it before shipping (a real UI panel replaces it
around M7).

---

## How to interact

- **Change the tick rate:** set `tickInterval` (smaller = faster). It's saved, so a loaded game keeps the
  rate it was saved with.
- **Run logic each tick:** put it in `Economy.OnTick()` (the single dispatch point), not in a new timer.
  Keep it deterministic (no RNG, no frame-time dependence) so save/load and tests stay valid.
- **Pause:** stop calling `Update` (or gate it) — the manager has no internal "running" flag today.
