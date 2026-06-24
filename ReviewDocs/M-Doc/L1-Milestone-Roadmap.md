# L1 Milestone Roadmap (the "M-Doc")

**What "M" means:** *Milestone.* L1 (the first playable economy) is built as an ordered ladder of
milestones, M0 → M8. Each milestone ends in a **runnable, inspectable** state with an "alive
check" you can see; you don't start the next rung until the current one's check passes. This keeps
the project always-working and stops you debugging two unknowns at once.

**Why this order:** each rung depends on the one below it — you can't price a good (M4) before goods
are produced (M2), can't move goods on roads (M5) before they have prices to chase, etc.

---

## The ladder

| M | Name | What it adds | Alive check | Status |
|---|------|--------------|-------------|--------|
| **M0** | Data model + nodes | `Systems/Core/Economy/` classes; provinces built from the map | Click a province → its data prints | ✅ done |
| **M1** | Tick on | Real 1-second tick driving `Economy.OnTick()` | `tick N` heartbeat climbs | ✅ done |
| *(pre-M2)* | **Tile refactor** | Resources & buildings move onto **tiles** (multi-tile deposits w/ rate+reserve); single world seed; deposit dots | Gray/yellow dots render; click a tile → deposit info | ✅ done |
| **M1.5** | **Fixes & hardening (GATE)** | Codebase-review backlog: Voronoi `worldSeed` seeding, square-map cleanup, dead-code removal, dedup, tile-source constants, Core/Godot split + xUnit tests | Same seed → identical world twice; no dead/fake code | ✅ done 2026-06-24 |
| *(then)* | Save/load + Controls hub | Reproduce-from-seed save/load (fix broken load path); input signal hub; tick→long | Save → reload → same world + tick resumes | 🔄 in progress |
| **M2** | Production | Buildings consume inputs → outputs into stockpiles; deposits' `Reserve` depletes | A worked tile's output climbs each tick | ⬜ |
| **M3** | Population | Pop eats grain, supplies a labor pool | Unfed area's pop falls | ⬜ |
| **M4** | Prices | Local market price per good (supply/demand) | Prices move as stock changes | ⬜ |
| **M5** | Logistics | Road (MST) edges get capacity; goods flow high→low price | Cut a road → prices diverge | ⬜ |
| **M6** | Government | Treasury + build / develop / tax-rate buttons | Press build → it costs + appears | ⬜ |
| **M7** | Inspector + balance | Real UI panel; tune until it's fun | Play 10 min without the console | ⬜ |
| **M8** | **Obfuscation (final / ship)** | Two layers: (1) encrypt save files via `FileAccess.OpenEncryptedWithPass`, keeping the **seed in a plain header** so it stays shareable; (2) obfuscate the compiled C# assembly with a .NET obfuscator | Save file is unreadable in a text editor (except the seed); decompiled assembly is obscured | ⬜ |

*(The "tile refactor" rung has no M-number — it's prep slotted between M1 and M2 because M2's
production needs to read tile-bound deposits/buildings and deplete reserves.)*

---

## How to use this doc

- Each completed rung gets a dated **review checklist** in `ReviewDocs/` (e.g.
  `2026-06-23_Economy_M0-M1_Review.md`) so you can audit and modify the work.
- "Where are we" also lives in agent memory (`l1-build-progress`), but **this file is the
  human-readable source of truth** for the plan.
- Numbers/scope can change — this is a map, not a contract. Update it when the plan shifts.
