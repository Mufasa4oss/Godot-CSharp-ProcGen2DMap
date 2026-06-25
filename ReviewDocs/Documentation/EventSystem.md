# The Event System

**Purpose:** how separate parts of the game tell each other that something happened, without each part
having to know about, poll, or directly own the others. "Player pressed save." "The map texture is
ready." "A second of game time elapsed." Each of those is a message that one piece sends and another
reacts to.

This project uses **three different dispatch mechanisms**, on purpose, for three different situations.
Knowing which is which — and when to reach for each — is the whole point of this doc.

| Mechanism | Example here | Lifetime / cleanup | Use it when |
|-----------|--------------|--------------------|-------------|
| **Godot signal** | `Controls` input intents | Auto-drops when either node frees | A node broadcasts to 0..N listeners; listeners come and go with the scene |
| **Static C# event** | `Main.MapTextureCreated` | **Manual** (`-=`) — survives scene reload | A one-time, cross-cutting hand-off at startup between things that don't reference each other |
| **Direct call loop** | `TickManager → Economy.OnTick()` | N/A (a plain method call) | One known consumer, every tick, order matters |

---

## 1. Godot signals — the input hub (the main pattern)

This is the project's primary, preferred event mechanism. It lives in
[`../../Systems/Controls/Controls.cs`](../../Systems/Controls/Controls.cs), a `Node` whose entire job is
to be the **single source of truth for input** and to turn raw key/mouse events into **semantic
signals**.

### What a Godot signal is

A signal is Godot's built-in observer/pub-sub. A node *declares* a signal, *emits* it when something
happens, and any number of other objects *connect* to it to be notified. In C# you declare one with a
specially-named delegate and the `[Signal]` attribute:

```csharp
[Signal] public delegate void SaveRequestedEventHandler();   // declares a "SaveRequested" signal
```

The `…EventHandler` suffix is a Godot convention — the source generator strips it, so the signal's name
is `SaveRequested`, exposed as `SignalName.SaveRequested` and as a C# event you can `+=`.

### How Controls works

```csharp
public partial class Controls : Node
{
    // 1. One place that names every input action (these strings must match project.godot [input]).
    public const string Save = "f5";
    public const string Load = "f9";
    public const string ToggleProvinces = "QPressed";
    public const string MapClick = "LeftClick";
    public const string ZoomIn = "ZoomIn";   // …pan/zoom names too
    // …

    // 2. The semantic intents it broadcasts.
    [Signal] public delegate void SaveRequestedEventHandler();
    [Signal] public delegate void LoadRequestedEventHandler();
    [Signal] public delegate void ProvincesToggleRequestedEventHandler();
    [Signal] public delegate void MapClickRequestedEventHandler();

    // 3. Raw input → intent. _UnhandledInput so the UI gets first dibs (see below).
    public override void _UnhandledInput(InputEvent @event)
    {
        if (Input.IsActionJustPressed(Save))            EmitSignal(SignalName.SaveRequested);
        if (Input.IsActionJustPressed(Load))            EmitSignal(SignalName.LoadRequested);
        if (Input.IsActionJustPressed(ToggleProvinces)) EmitSignal(SignalName.ProvincesToggleRequested);
        if (Input.IsActionJustPressed(MapClick))        EmitSignal(SignalName.MapClickRequested);
    }
}
```

Two design choices worth understanding:

- **Action-name constants, not magic strings.** Godot maps physical inputs (the F5 key, left mouse
  button) to named *actions* in `project.godot`'s `[input]` section. Code refers to those actions by
  string. Centralizing the strings as `Controls.*` constants means there's exactly one place to change a
  name, and other nodes (e.g. `Camera` reading `Controls.PanUp`/`ZoomIn` for its per-frame movement)
  share them instead of re-typing `"PanUp"`.
- **`_UnhandledInput`, not `_Input`.** `_UnhandledInput` only receives events the UI didn't already
  consume, so a click on a button doesn't also fire `MapClickRequested`. (See the lifecycle doc for the
  `_Input` vs `_UnhandledInput` distinction.)

### Who listens

Subscribers connect with `+=` in their own `_Ready()`:

- `Main` ([`../../Systems/Game/Main.cs`](../../Systems/Game/Main.cs)) — creates `Controls` first
  (so collaborators can connect), then:
  ```csharp
  controls.SaveRequested += OnSaveRequested;   // → writes a save
  controls.LoadRequested += OnLoadRequested;   // → stash + reload scene from seed
  ```
- `MapManager` ([`../../Systems/Map/MapManager.cs`](../../Systems/Map/MapManager.cs)) — gets the same
  `Controls` instance via its constructor and connects:
  ```csharp
  _controls.MapClickRequested      += OnMapClick;         // inspect/highlight a province (or lay track)
  _controls.ProvincesToggleRequested += OnToggleProvinces; // show/hide the Voronoi overlay
  ```

### Why Godot signals here, not C# events

**Because connections auto-drop when either node is freed.** Loading a save calls
`ReloadCurrentScene()`, which destroys and rebuilds every node. With Godot signals there are *no stale
handlers* to clean up — the old `Controls` and its old subscribers are gone together. That safety is the
deciding reason this is the default mechanism. (Contrast the static event in §2, which doesn't get this.)

---

## 2. Static C# event — the startup hand-off (use sparingly)

The second mechanism is a plain **.NET `static event`**, used exactly once, for a problem signals would
have made awkward: handing the freshly-built province texture from `Main` to a sprite node that `Main`
doesn't hold a reference to.

```csharp
// Main.cs
public static event Action<Texture2D> MapTextureCreated;
// …later, inside CreateObjs():
MapTextureCreated?.Invoke(provinceTexture);
```

```csharp
// ColoredProvincesSprite.cs  (../../Systems/Map/ColoredProvincesSprite.cs)
public override void _Ready()
{
    Main.MapTextureCreated += MainEventMethod;     // subscribe
}
private void MainEventMethod(Texture2D t)
{
    InitializeProvinces(t);                         // apply the texture + border shader
    Main.MapTextureCreated -= MainEventMethod;      // MUST unsubscribe by hand
}
```

It's `static` so the sprite can subscribe to "Main, whenever you exist" without a direct reference, and
it carries a payload (`Texture2D`) via `Action<Texture2D>`.

### ⚠️ The footgun (tracked in [`../minorFixes.md`](../minorFixes.md))

This works today but is fragile, for two compounding reasons:

1. **It relies on `_Ready` ordering.** `Main` fires `MapTextureCreated` *during its own `_Ready`* (inside
   `CreateObjs`). The sprite only receives it because Godot runs **child `_Ready` before parent
   `_Ready`**, so the sprite has already subscribed by the time `Main` fires. Change the node hierarchy or
   the order of operations and the event can fire into the void.
2. **`static` + manual cleanup.** A static event's subscriber list lives on the *type*, not an instance,
   so it survives a `ReloadCurrentScene()`. The `-=` in the handler is what prevents a leak across
   reloads — forget it and you accumulate dead subscribers.

**Guidance:** prefer a **Godot signal** or a **direct method call** for new cross-node hand-offs. A
direct call (Main already creates the texture — it could call the sprite directly if it held the
reference) or a Godot signal both avoid the static lifetime and the ordering dependency. Treat this static
event as legacy, not a pattern to copy.

---

## 3. The tick "event" — a direct call loop (and why it's *not* pub/sub)

It's tempting to think of "every second, the economy updates" as an event. In this codebase it
deliberately **isn't** — it's a plain method call, because there is exactly one consumer and the timing
must be deterministic.

```csharp
// Main._Process(delta)  → pumps the clock every frame
gameContext.tickManager.Update((float)delta);

// TickManager.Update(delta)  → accumulates real time, and on each whole second:
economy.OnTick();   // a direct call, not an EmitSignal
```

See [TickLoop.md](TickLoop.md) for the timing detail. The point for *this* doc: when there's a single
known consumer and you care about ordering/determinism (and you want it unit-testable in Core with no
engine), a **direct call** is clearer and safer than broadcasting an event. Don't reach for signals just
because something happens repeatedly.

---

## 4. How-to recipes

### Add a new input action → intent signal

1. **Define the action** in `project.godot` under `[input]` (or via the editor's Input Map): e.g.
   `BuildWorkshop` bound to a key.
2. **Add the name constant** to `Controls`: `public const string BuildWorkshop = "BuildWorkshop";`
3. **Declare the signal:** `[Signal] public delegate void BuildWorkshopRequestedEventHandler();`
4. **Emit it** in `Controls._UnhandledInput`:
   `if (Input.IsActionJustPressed(BuildWorkshop)) EmitSignal(SignalName.BuildWorkshopRequested);`
5. **Subscribe** from whoever should react, in that node's `_Ready()`:
   `_controls.BuildWorkshopRequested += OnBuildWorkshop;` (a node that gets `Controls` via ctor or
   `GetNode`). No manual cleanup needed — it drops on free.

To carry data on a signal, give the delegate parameters, e.g.
`delegate void MapClickRequestedEventHandler(Vector2I tile)` and `EmitSignal(SignalName.MapClickRequested,
tile)`. (Today `MapClickRequested` is parameterless and the handler re-reads the mouse position itself.)

### Choosing a mechanism for something new

- **Node broadcasts to other nodes, listeners live and die with the scene** → **Godot signal.** (Default.)
- **One known consumer, every tick, order matters, want it testable in Core** → **direct method call.**
- **A static event** → avoid for new code; if you think you need it, you probably want a Godot signal or
  to pass a reference and call directly.

---

## Files at a glance

- [`../../Systems/Controls/Controls.cs`](../../Systems/Controls/Controls.cs) — the input hub (constants +
  `[Signal]`s + `_UnhandledInput`).
- [`../../Systems/Game/Main.cs`](../../Systems/Game/Main.cs) — connects Save/Load signals; declares and
  fires the static `MapTextureCreated`.
- [`../../Systems/Map/MapManager.cs`](../../Systems/Map/MapManager.cs) — connects MapClick / ProvincesToggle.
- [`../../Systems/Map/ColoredProvincesSprite.cs`](../../Systems/Map/ColoredProvincesSprite.cs) — the lone
  static-event subscriber.
- [`../../Systems/Cameras/Camera.cs`](../../Systems/Cameras/Camera.cs) — reads `Controls.*` action
  constants directly (per-frame pan/zoom; no signal).
- [`../../Systems/GameTime/TickManager.cs`](../../Systems/GameTime/TickManager.cs) — the direct tick call
  loop.
