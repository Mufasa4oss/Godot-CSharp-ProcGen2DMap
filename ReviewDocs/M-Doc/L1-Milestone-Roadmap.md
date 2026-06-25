# L1 Milestone Roadmap (the "M-Doc")

**What "M" means:** *Milestone.* L1 (the first playable economy) is built as an ordered ladder of
milestones, M0 → M8. Each milestone ends in a **runnable, inspectable** state with an "alive
check" you can see; you don't start the next rung until the current one's check passes. This keeps
the project always-working and stops you debugging two unknowns at once.

**Why this order (re-scoped for an MVP, 2026-06-25):** L1 is a **strategy game**, so after M2 the
priority is **meaningful economic choices**, not bottom-up simulation. M3–M7 are reframed around the
smallest *playable* loop (a few deposits, player-built workshops, visible value, one shortage, one
capacity road — the "fun backlog" goal in [`../Build-History.md`](../Build-History.md)). **Player agency
+ economy UI come first (M3)**; the full population/labor sim is **deferred to minimal**; logistics (the
long-term differentiator/spine) lands *after* the choice loop is fun. Each rung still depends on the one
below: you can't sell a good (M4) before it's produced (M2), can't create a shortage (M5) before goods
have value (M4), can't route goods between provinces (M6) before there's a price gradient to chase.

---

## The ladder

| M | Name | What it adds | Alive check | Status |
|---|------|--------------|-------------|--------|
| **M0** | Data model + nodes | `Systems/Core/Economy/` classes; provinces built from the map | Click a province → its data prints | ✅ done |
| **M1** | Tick on | Real 1-second tick driving `Economy.OnTick()` | `tick N` heartbeat climbs | ✅ done |
| *(pre-M2)* | **Tile refactor** | Resources & buildings move onto **tiles** (multi-tile deposits w/ rate+reserve); single world seed; deposit dots | Gray/yellow dots render; click a tile → deposit info | ✅ done |
| **M1.5** | **Fixes & hardening (GATE)** | Codebase-review backlog: Voronoi `worldSeed` seeding, square-map cleanup, dead-code removal, dedup, tile-source constants, Core/Godot split + xUnit tests | Same seed → identical world twice; no dead/fake code | ✅ done 2026-06-24 |
| *(then)* | Save/load + Controls hub | Reproduce-from-seed save/load (fix broken load path); input signal hub; tick→long | Save → reload → same world + tick resumes | ✅ done 2026-06-24 |
| **M2** | Production | Buildings consume inputs → outputs into stockpiles; deposits' `Reserve` depletes | A worked tile's output climbs each tick | ✅ done 2026-06-25 (extractors; Workshop chain → M6) |
| **M3** | **Agency + Economy UI** | Treasury (money); player **builds Workshops** (Ore→Tools, the M2-deferred chain); each capital is a **nation** with its own stockpile (player = first capital); UI shows stockpile + flows (production in, Ore→Workshop out) | Build a workshop → treasury drops, Ore drains while Tools climb, UI shows both flows | ✅ done 2026-06-25 (`Province`→`Nation`; animated workshop sprite; in-engine check pending) |
| **M4** | **Value / Prices** | Each good has a value; a simple market lets goods be **sold for treasury income** → closes the build→produce→sell→reinvest loop | Producing + selling Tools grows the treasury faster than selling raw Ore | ✅ done 2026-06-25 (static price table; `SellAll` per good; UI sell buttons + price/**per-good income breakdown**; **in-engine verified** — build/produce/sell loop + treasury math confirmed) |
| **M5** | **Demand + scarcity** | A **minimal demand sink** consumes goods (the "one shortage"); prices respond to supply vs demand; grain→pops flow appears in UI. *(Minimal pop — full labor sim deferred.)* | A shortage drives a good's price up; player re-allocates production | ⬜ |
| **M6** | **Logistics (the spine)** | Per-province stockpiles return; MST edges get **capacity**; goods flow high→low price; **connector roads** tap the nearest MST edge → deposit location gains value | Cut a road → the two sides' prices diverge / a cut-off province starves | ⬜ |
| **M7** | Inspector + legibility + balance | Full UI panel (no console); economy legible on the map (tint by price, pulse roads, flash depleted deposits); tune to fun | Play ~10 min making choices entirely through the UI | ⬜ |
| **M8** | **Obfuscation (final / ship)** | Two layers: (1) encrypt save files via `FileAccess.OpenEncryptedWithPass`, keeping the **seed in a plain header** so it stays shareable; (2) obfuscate the compiled C# assembly with a .NET obfuscator | Save file is unreadable in a text editor (except the seed); decompiled assembly is obscured | ⬜ |

*(The "tile refactor" rung has no M-number — it's prep slotted between M1 and M2 because M2's
production needs to read tile-bound deposits/buildings and deplete reserves.)*

---

## Milestone detail (M3–M7)

The MVP target is **one tight, fun economic-choice loop** before breadth. Each milestone below is a
playable increment toward it. References: the "fun backlog" and design history in
[`../Build-History.md`](../Build-History.md); the MST-highway + connector-road model in
[`../../claude/DesignNotes.md`](../../claude/DesignNotes.md) (note its 7-resource list is **stale** — the
current goods are Grain/Ore/Tools). System docs: [`../Documentation/`](../Documentation/).

### M3 — Agency + Economy UI (the first meaningful choice) — ✅ done 2026-06-25

- **Goal / the choice.** Give the player their first real decision: spend from a **treasury** to build a
  **Workshop** that converts Ore→Tools. "Is upgrading ore to tools worth the cost?" is the seed of the
  whole game. Until now the player only watched numbers climb — M3 lets them *act*.
- **Player-facing UI.** An economy panel showing the player nation's stockpile **and the flows** — goods
  *added* by production and *removed* by consumption (Ore → Workshops) — plus a Treasury readout and a
  Build-Workshop toggle. (The grain → pops flow shows here once pops exist at M5.)
- **What shipped.** Each capital became a **`Nation`** (renamed from `Province`) with its own
  stockpile + `Treasury`; **per-nation stockpile kept** (no global pool). Player = the **first placed
  capital** (`Owner 0`), the rest passive AI. `Building` gained a `Cost`; `Economy.TryBuildWorkshop(tile)`
  validates *owned + empty + affordable*. `OnTick` is now **two-pass** (extractors produce → workshops
  consume, same tick). The Workshop renders via `WorkshopSprites` (an `AnimatedSprite2D` built in code
  from 60 Blender frames). Save/load persists `Treasury` + player-built `Workshops`.
- **Key files.** `Systems/Core/Economy/{Nation,NationStock,Economy,Building}.cs`,
  `Systems/Map/{WorkshopSprites,MapManager}.cs`, `UI/UIManager.cs` + `Scenes/main.tscn`,
  `Systems/SaveLoad/*`.
- **Dependencies.** M2 production + stockpiles. **Deferred:** prices/value, pop, logistics, AI.
- **Alive check.** Build a workshop → treasury drops, Ore drains while Tools climb, the UI shows both
  flows.

### M4 — Value / Prices (the payoff signal that makes M3 meaningful) — ✅ done 2026-06-25

- **Goal / the choice.** Make the M3 choice *quantifiable*: each good has a value, and a simple market
  lets goods be **sold for treasury income**, closing the loop **build → produce → sell → reinvest**.
  Now "Ore→Tools" has a number attached and the player can judge it.
- **Player-facing UI.** Per-good value/price readout; a sell action; treasury income visible.
- **What shipped.** A static price table in `Economy` (Grain 1, Ore 2, **Tools 6** — priced above the Ore
  a Workshop consumes, so refining pays). `Economy.SellAll(Good)` moves the player's whole stockpile of a
  good into the treasury at its price and accumulates `TotalIncome` (a display stat — not persisted; the
  Treasury is the real saved state). The HUD gained an **Income** readout, a price on each good line
  (`Ore: 80 @2`), and three **Sell** buttons (Grain/Ore/Tools) wired straight to `SellAll`. No save/load
  changes needed (prices static; treasury already persists). New tests in `Tests/EconomyValueTests.cs`.
- **New state & types.** Per-good price/value; a sell path that converts stockpiled goods → treasury.
  (Prices start static/tabled here; supply/demand response arrives at M5.)
- **Key files.** `Systems/Core/Economy/Economy.cs` (prices, `SellAll`, `TotalIncome`),
  `UI/UIManager.cs` + `Scenes/main.tscn`, `Tests/EconomyValueTests.cs`.
- **Dependencies.** M3 stockpile + treasury. **Deferred:** dynamic supply/demand pricing (M5).
- **Alive check.** Producing + selling Tools grows the treasury faster than selling raw Ore.
  **Verified in-engine 2026-06-25:** built a workshop (treasury 500→400), watched Tools climb from Ore,
  sold Ore and Tools, treasury math exact (`500 − 100 + income`); the per-good `earned` breakdown reads
  correctly in the HUD.
- **Balance note (surfaced by the breakdown, for M7/M5).** A single workshop consumes **1 ore/tick**
  against ~113 ore/tick mined, so refined-Tools income is a rounding error next to raw-Ore volume even
  though refining wins **per unit** (6 vs 2). The Ore→Tools choice only bites once you can convert a
  meaningful *share* of ore — i.e. workshop throughput (`Building.Rate`) is mis-scaled vs ore output, or
  raw ore needs to be worth less. A tuning knob for M7; M5's demand sink may also pull raw-ore value down.

### M5 — Demand + scarcity (the "one shortage" / pressure)

- **Goal / the choice.** Introduce a reason goods *run out*: a **minimal demand sink** consumes goods,
  creating a shortage the player must solve by re-allocating what they build/produce. Prices now respond
  to supply vs demand. This is the *minimal* population (a consumption node) — the full labor-gates-
  production simulation stays deferred.
- **Player-facing UI.** The grain → pops consumption flow appears in the panel; a shortage is visible
  (a good's price spiking / stockpile draining).
- **New state & types.** A demand/consumption node (minimal `Pop` or a consumer); price-from-supply/
  demand formula.
- **Key files.** `Economy.cs` (consumption in `OnTick`, dynamic prices), UI.
- **Dependencies.** M4 value/prices. **Deferred:** full pop model, labor pool, labor-limited production.
- **Alive check.** A shortage drives a good's price up; the player re-allocates production in response.

### M6 — Logistics (geography becomes a choice; the long-term spine)

- **Goal / the choice.** Make *where* you build matter. **Per-province stockpiles return**; MST edges get
  **capacity**; goods flow high-price → low-price along the MST; **connector roads** tap the nearest MST
  edge (per DesignNotes), so a deposit's distance to the network sets its value. This is the
  "supply lines carry econ (and later army)" differentiator — the spine the rest hangs on.
- **Player-facing UI.** Build-connector action; per-province stockpiles/prices; flow/congestion on roads.
- **New state & types.** Edge capacity on MST edges; connector-road model; per-province stockpiles
  (un-pool the M3 national stockpile); a flow/transport step.
- **Key files.** `Economy.cs` + a logistics/flow module; `Main`/road drawing for connectors;
  `MapManager`/UI for build-connector input.
- **Dependencies.** M4 prices (the gradient goods chase) + M5 shortages (what makes routing matter).
- **Alive check.** Cut a road → the two sides' prices diverge / a cut-off province starves.

### M7 — Inspector + legibility + balance (playable without the console)

- **Goal.** Replace the console with a real UI; make the economy **legible on the map** (tint provinces
  by price, pulse roads by congestion, flash depleted deposits — the fun backlog's "half the fun
  budget"); tune numbers until the loop is genuinely fun.
- **Key files.** UI panels; map overlays (extend `DepositOverlay`/province shader); balance tunables in
  `Economy`.
- **Dependencies.** M3–M6. **Alive check.** Play ~10 min making choices entirely through the UI, no
  console.

> **Explicitly deferred (out of L1):** AI factions / warfare / province conquest. Revisit after the
> single-player economic loop is fun.

---

## How to use this doc

- Each finished chunk appends a dated section to [`../Build-History.md`](../Build-History.md) (the single
  chronological record), so you can audit and modify the work. (`minorFixes.md` holds the active cleanup
  backlog.)
- "Where are we" also lives in agent memory (`l1-build-progress`), but **this file is the
  human-readable source of truth** for the plan.
- Numbers/scope can change — this is a map, not a contract. Update it when the plan shifts (M3–M7 were
  re-scoped 2026-06-25 toward an MVP "one tight loop").
