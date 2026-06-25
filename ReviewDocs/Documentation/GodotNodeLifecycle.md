# Godot Node Lifecycle (for people used to owning their objects)

**Who this is for:** you're comfortable doing `var thing = new Thing(); thing.Init(); thing.Run();` —
you create an object, set it up, and call it. Godot feels strange because it takes objects *from you*
and calls *them* on its own schedule. This guide explains exactly when that happens, so the engine stops
feeling like magic.

The one correction up front, because it's the thing everyone gets wrong:

> The **C# constructor** runs the moment the object is created (`new` / `Instantiate()`).
> **`_Ready()` runs later** — once, when the node is added into the **scene tree** — *after* all of its
> children are themselves ready. They are two different moments. Setup that touches other nodes belongs
> in `_Ready()`, not the constructor.

---

## 1. Two moments: created vs "in the tree"

A Godot node has two distinct birth events:

| Moment | What's true | Safe to do |
|--------|-------------|------------|
| **Constructor** (`public Foo()`) | The C# object exists in memory. It is **not** in the scene tree yet. It has no parent, its `GetNode` paths resolve to nothing, exported fields may not be wired. | Set plain fields, store ctor arguments. **Do not** call `GetNode`, touch other nodes, or assume the scene exists. |
| **`_Ready()`** | The node has been added to the tree (it has a parent), and **all its children have already had their own `_Ready()` called.** | `GetNode<T>("…")`, read `[Export]` values, connect signals, position things, kick off work. This is your real "Init()". |

Why two moments? Because in Godot you build a *tree* of nodes, and a node usually needs its parent and
children to exist before it can wire itself up. Godot guarantees a node's children are ready before the
node itself, so by the time your `_Ready()` runs, everything *below* you is already set up.

### How nodes get created in this project

There are two paths, and both appear here:

1. **The engine creates it from a scene (`.tscn`).** When the main scene loads, Godot instantiates the
   nodes saved in it and calls their lifecycle methods. `Main`
   ([`../../Systems/Game/Main.cs`](../../Systems/Game/Main.cs)) is created this way — you never write
   `new Main()`; Godot does it when `Scenes/main.tscn` loads, then calls `Main._Ready()`.

2. **You create it in code and add it to the tree yourself.** This is the path that feels familiar:

   ```csharp
   // Main._Ready():
   controls = new Controls();   // constructor runs now — object exists, NOT in tree
   AddChild(controls);          // NOW it enters the tree → Godot calls controls._Ready()
   ```

   `Main._Ready()` does exactly this for `Controls`, `MapManager`, the `DepositOverlay`, the
   `TradeNetworkVisualizer`, and a `Timer`. Each `new X()` runs that class's constructor immediately; the
   matching `AddChild(x)` is what actually triggers `x._Ready()`. So the order you see effects is "all
   constructors as you `new` them, then each `_Ready` as you `AddChild`."

   Note `MapManager` and `Controls` take constructor arguments (collaborators they need). That's the
   normal way to hand a code-created node its dependencies — store them in the ctor, *use* them in
   `_Ready()`.

---

## 2. The lifecycle callbacks (the `_`-prefixed virtuals)

These are methods Godot calls on your node. You don't call them; you *override* them and Godot invokes
them at the right time. The ones you'll meet here:

### `_EnterTree()`
Fires when the node enters the tree, **before** its children are ready. Parent's `_EnterTree` runs before
children's. Use it only for things that must happen top-down before children initialize. **Rarely needed
— this project doesn't use it.** Prefer `_Ready()`.

### `_Ready()`  ← the important one
Fires **once**, after the node and all its children are in the tree and the children are ready
(bottom-up: children ready before parent). This is your initialization hook.

Examples in this codebase:
- `Main._Ready()` — the whole bootstrap: reads the seed (or a pending save), builds the world via
  `CreateObjs()`, wires input signals, draws roads. See [Architecture.md](Architecture.md).
- `MapManager._Ready()` ([`../../Systems/Map/MapManager.cs`](../../Systems/Map/MapManager.cs)) —
  `GetNode`s its sibling nodes (`../Provinces`, `../UIManager`, the road layer) and connects to the
  `Controls` signals. None of that could happen in the constructor — the siblings don't exist yet there.
- `ColoredProvincesSprite._Ready()`
  ([`../../Systems/Map/ColoredProvincesSprite.cs`](../../Systems/Map/ColoredProvincesSprite.cs)) —
  subscribes to a static event (`Main.MapTextureCreated`). See the footgun note in
  [EventSystem.md](EventSystem.md).

### `_Process(double delta)`
Fires **every rendered frame**, with `delta` = seconds since the last frame (variable). Use for things
that should update as fast as the screen does. Keep it cheap — it runs constantly.
- `Main._Process` forwards to the tick manager: `gameContext.tickManager.Update((float)delta)` — the
  game's whole heartbeat is pumped from here (see [TickLoop.md](TickLoop.md)).
- `Camera._Process` ([`../../Systems/Cameras/Camera.cs`](../../Systems/Cameras/Camera.cs)) reads pan/zoom
  input and moves the camera each frame.

### `_PhysicsProcess(double delta)`
Like `_Process` but on the **fixed physics timestep** (consistent `delta`, default 60/s), decoupled from
framerate. Use for physics/deterministic stepping. **Not used here** — the simulation runs on the
1-second logic tick instead (see [TickLoop.md](TickLoop.md)), and `_Process` handles the camera.

### `_Input(InputEvent e)` and `_UnhandledInput(InputEvent e)`
Both deliver input events, but at different points in Godot's input pipeline:
- `_Input` sees **every** event first.
- `_UnhandledInput` sees only events that **nothing else consumed** — in particular, after the UI
  (Control nodes, buttons) has had its chance.

This project uses **`_UnhandledInput`** on purpose, in
[`../../Systems/Controls/Controls.cs`](../../Systems/Controls/Controls.cs) (and historically in
`MapManager`), so that clicking a button doesn't also register as a map click. If you want input that the
UI should be able to "eat" first, use `_UnhandledInput`; if you need to intercept everything (e.g. a
global hotkey that must always fire), use `_Input`. See [EventSystem.md](EventSystem.md) for how Controls
turns these raw events into semantic signals.

### `_Draw()`
Fires when the node needs to (re)render its custom 2D drawing. You don't call it directly — you call
`QueueRedraw()` to request it, and Godot calls `_Draw()`. Inside `_Draw()` you issue immediate-mode draw
calls (`DrawCircle`, `DrawLine`, …).
- `DepositOverlay` ([`../../Systems/Map/DepositOverlay.cs`](../../Systems/Map/DepositOverlay.cs)) builds
  a list of dots once in its `Initialize(...)`, calls `QueueRedraw()`, and draws them in `_Draw()`. Because
  deposits don't move, it draws **once** — no per-frame cost. That's the pattern to copy for static
  overlays.
- `TradeNetworkVisualizer` ([`../../Systems/ResourcePopup/TradeNetworkVisualizer.cs`](../../Systems/ResourcePopup/TradeNetworkVisualizer.cs))
  animates, so it calls `QueueRedraw()` each frame.

### `_ExitTree()`
Fires when the node leaves the tree (removed or freed). Use it to undo anything `_EnterTree`/`_Ready` set
up that won't clean itself up. **You rarely need it for signals** — see the cleanup note below.

### `_Notification(int what)`
The low-level catch-all: Godot sends numeric notifications for many events (`NOTIFICATION_READY`,
`NOTIFICATION_WM_CLOSE_REQUEST`, predelete, etc.) and the typed callbacks above are conveniences on top of
it. You'll only override this for notifications that don't have a dedicated method. **Not used here**;
mentioned so you recognize it.

**Ordering recap:** for a subtree, `_EnterTree` is top-down (parent before child); `_Ready` is bottom-up
(child before parent). `_Process`/`_PhysicsProcess` then run every frame/tick for as long as the node is
in the tree.

---

## 3. Plain C# classes vs Nodes — and why this project has both

Not everything should be a node. A `Node` buys you the lifecycle, the scene tree, signals, and editor
integration — but it also means **Godot owns its creation and lifetime**, and it can't be unit-tested
without the engine.

So the project splits deliberately (see [Architecture.md](Architecture.md) and the
[Documentation README](README.md)):

- **Plain C# classes** — `Economy`, `Nation`, `Deposit`, `Building`, `PrimsAlgorithmSimple`,
  `VoronoiAssignment`, `TileResolver` (all under `Systems/Core/`). **You** `new` them, **you** call them,
  they have no `_Ready`/`_Process`. They're pure logic, so they're fast to reason about and unit-tested
  outside Godot. This is the familiar "I own my objects" model — and it's where the *game rules* live.
- **Nodes** (`partial class : Node/Node2D/...`) — `Main`, `MapManager`, `Camera`, `Controls`, the
  overlays. These exist because they must render, take input, or sit in the scene. They're the **thin
  adapter** around the Core logic.

Choosing: if a class needs to draw, receive input, be placed in a scene, or emit Godot signals → make it
a `Node`. Otherwise (it's rules/data/algorithms) → make it a plain class in Core and have a node call it.

---

## 4. Freeing, deferring, and the scene reload

A few engine mechanics that show up in this codebase and interact with the lifecycle:

- **`QueueFree()`** — asks Godot to delete a node at the end of the current frame (safe; not immediate).
  `Main` uses it on the popup's `Timer` after it fires.
- **`CallDeferred(method)`** — runs `method` a bit later, at a safe point (idle), instead of right now.
  Used in the load flow so we don't tear down the scene in the middle of handling an input signal:
  `GetTree().CallDeferred(SceneTree.MethodName.ReloadCurrentScene)` in `Main.OnLoadRequested`.
- **`ReloadCurrentScene()`** — throws away the current scene and rebuilds it from the `.tscn`. Loading a
  save uses this: stash the save in a `static` field (`Main.PendingLoad`, which survives the reload
  because statics aren't part of the scene), reload, and let the fresh `Main._Ready()` pick it up and
  regenerate the world from the saved seed. See [SaveLoad.md](SaveLoad.md).
- **Signals auto-disconnect on free.** When a node is freed (including by the scene reload), its signal
  connections drop automatically. That's a key reason `Controls` uses **Godot signals** rather than
  static C# events — there are no stale handlers to clean up after a reload. The static-event subscriber
  (`ColoredProvincesSprite`) has to manage that by hand (`-=`), which is exactly why it's flagged as a
  footgun. See [EventSystem.md](EventSystem.md).

---

## TL;DR

- Constructor = "object exists." `_Ready` = "object is in the tree and its children are ready" → do setup
  here.
- `new X()` runs the ctor; `AddChild(x)` triggers `x._Ready()`.
- `_Process` = every frame (camera, tick pump). `_Draw` = custom 2D, triggered by `QueueRedraw()`.
- `_UnhandledInput` = input the UI didn't consume (what Controls listens to).
- Rules/data → plain Core classes you own. Rendering/input/scene → Nodes Godot owns.
