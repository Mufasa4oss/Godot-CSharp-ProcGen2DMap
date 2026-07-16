# CLAUDE.md

## Project

Godot 4.7 2D strategy/economy game (C#) about **logistics bottlenecks**: resources are produced on a
procedural map, roads carry them, cities consume them, and the game is finding where the flow breaks
down. See [README.md](README.md) for the current systems, controls, and layout; `gameplan.md` for the
vision.

## How I want you to work

- **Simplicity over cleverness.** I need to understand every line. Code runs away and gets too big too
  fast — keep it small and readable, and prefer the obvious approach over the efficient one.
- **Don't over-build or assume scope.** Implement exactly what's asked — no extra files, no demo/seed
  data, no "while I'm here" additions. When something's ambiguous, ask or flag it instead of guessing.
- **One change at a time.** Small, focused edits I can follow, not sweeping rewrites.
- **Plain mental models first.** Explain concepts (ticks vs frames, the Godot loop, …) with a simple
  analogy before any jargon; keep it concise unless I ask for depth.

## Build, test, verify

- Build: `dotnet build ProceduralMapGenerator.csproj` (a hook also builds after every edit).
- Test: `dotnet test Tests/Tests.csproj` — pure logic only. The test project links the engine-free
  sources and stays Godot-`Node`-free on purpose (if that logic takes a `Node` dependency, tests stop
  compiling).
- C# changes need a `dotnet build` before they show up in the Godot editor; I verify behaviour in-engine.

## Workflow

Milestones are numbered (M0, M1, …). Implement → verify (passing tests + clean build) → document major
systems → commit/push when I ask.
